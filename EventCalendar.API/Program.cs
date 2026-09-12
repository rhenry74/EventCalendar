using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);
var frontendOrigin = builder.Configuration["Frontend:Origin"] ?? "http://localhost:5173";
var allowedOrigins = new[] { "http://localhost:5173", frontendOrigin }.Distinct().ToArray();

builder.Services.AddCors(options => options.AddPolicy("AllowVite", policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials()));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "eventcalendar.auth";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["OAuth:ClientId"]
            ?? throw new InvalidOperationException("OAuth:ClientId must be configured for Google login.");
        options.ClientSecret = builder.Configuration["OAuth:ClientSecret"]
            ?? throw new InvalidOperationException("OAuth:ClientSecret must be configured for Google login.");
    });
builder.Services.AddAuthorization();

var eventStorePath = builder.Configuration["Storage:EventStorePath"] ?? "Data/events.json";
if (!Path.IsPathRooted(eventStorePath)) eventStorePath = Path.Combine(builder.Environment.ContentRootPath, eventStorePath);
var legacyEventsPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "public", "events.json"));
var store = new EventStore(eventStorePath, legacyEventsPath);

builder.Services.AddSingleton(store);
builder.Services.AddSingleton<McpTokenStore>();
builder.Services.AddHttpContextAccessor();
builder.Services
    .AddMcpServer(options => options.ServerInfo = new() { Name = "EventCalendar", Version = "1.0.0" })
    .WithTools<CalendarMcpTools>()
    .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless);

var app = builder.Build();
var mcpTokenStore = app.Services.GetRequiredService<McpTokenStore>();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("AllowVite");
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/mcp"))
    {
        await next();
        return;
    }

    if (await McpApiKeyMiddleware.AuthenticateAsync(context, mcpTokenStore)) await next();
});

await store.InitializeAsync();

app.MapGet("/api/auth/login", () => Results.Challenge(
    new AuthenticationProperties { RedirectUri = frontendOrigin }, [GoogleDefaults.AuthenticationScheme]));

app.MapPost("/api/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
}).RequireAuthorization();

app.MapGet("/api/auth/me", (ClaimsPrincipal user) => Results.Ok(new
{
    id = GetUserId(user),
    name = user.Identity?.Name,
    email = user.FindFirstValue(ClaimTypes.Email)
})).RequireAuthorization();

app.MapPost("/api/mcp/token", (ClaimsPrincipal user, McpTokenStore tokenStore) =>
{
    var token = tokenStore.Create(user);
    return Results.Ok(new { token, userId = GetUserId(user) });
}).RequireAuthorization();

var events = app.MapGroup("/api/events").RequireAuthorization();

events.MapGet("", async (ClaimsPrincipal user) =>
{
    var userId = GetUserId(user);
    var visibleEvents = await store.GetVisibleEventsAsync(userId);
    return Results.Ok(visibleEvents.Select(item => ToResponse(item, userId)));
});

events.MapPost("", async (EventInput input, ClaimsPrincipal user) =>
{
    var userId = GetUserId(user);
    if (string.IsNullOrWhiteSpace(input.Title)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["title"] = ["Title is required."] });

    var eventItem = new CalendarEvent
    {
        Id = Guid.NewGuid().ToString("N"), OwnerId = userId,
        OwnerName = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.Email) ?? "Unknown user",
        Title = input.Title.Trim(), Description = input.Description?.Trim() ?? string.Empty,
        Journal = input.Journal ?? string.Empty,
        Date = input.Date, Location = input.Location?.Trim(), Category = input.Category?.Trim(),
        IsPublic = input.IsPublic, EndDate = input.EndDate
    };
    await store.AddAsync(eventItem);
    return Results.Created($"/api/events/{eventItem.Id}", ToResponse(eventItem, userId));
});

events.MapPut("/{id}", async (string id, EventInput input, ClaimsPrincipal user) =>
{
    var userId = GetUserId(user);
    var existing = await store.GetByIdAsync(id);
    if (existing is null) return Results.NotFound();
    if (existing.OwnerId != userId) return Results.Forbid();
    if (string.IsNullOrWhiteSpace(input.Title)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["title"] = ["Title is required."] });

    existing.Title = input.Title.Trim();
    existing.Description = input.Description?.Trim() ?? string.Empty;
    existing.Journal = input.Journal ?? string.Empty;
    existing.Date = input.Date; existing.Location = input.Location?.Trim();
    existing.Category = input.Category?.Trim(); existing.IsPublic = input.IsPublic;
    existing.EndDate = input.EndDate;
    await store.UpdateAsync(existing);
    return Results.Ok(ToResponse(existing, userId));
});

events.MapDelete("/{id}", async (string id, ClaimsPrincipal user) =>
{
    var userId = GetUserId(user);
    var existing = await store.GetByIdAsync(id);
    if (existing is null) return Results.NotFound();
    if (existing.OwnerId != userId) return Results.Forbid();
    await store.DeleteAsync(id);
    return Results.NoContent();
});

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Message = "EventCalendar API is running" }));
app.MapMcp("/mcp");
app.MapFallbackToFile("index.html");
app.Run();

static string GetUserId(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier)
    ?? throw new UnauthorizedAccessException("Google did not provide a user identifier.");

static EventResponse ToResponse(CalendarEvent item, string userId) => new(item.Id, item.Title, item.Description ?? string.Empty,
    item.Date, item.Location, item.Category, item.IsPublic, item.OwnerName, item.OwnerId == userId,
    item.OwnerId == userId ? item.Journal ?? string.Empty : string.Empty, item.EndDate);

public class EventInput
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Journal { get; set; }
    public string Date { get; set; } = string.Empty;
    public string? EndDate { get; set; }
    public string? Location { get; set; }
    public string? Category { get; set; }
    public bool IsPublic { get; set; }
}

public sealed class CalendarEvent : EventInput
{
    public string Id { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
}

public sealed record EventResponse(string Id, string Title, string Description, string Date,
    string? Location, string? Category, bool IsPublic, string OwnerName, bool IsOwner, string Journal, string? EndDate);

public sealed class EventStore(string filePath, string legacyEventsPath)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task InitializeAsync()
    {
        if (File.Exists(filePath)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var legacyEvents = File.Exists(legacyEventsPath)
            ? await JsonSerializer.DeserializeAsync<List<EventInput>>(File.OpenRead(legacyEventsPath), _jsonOptions) ?? []
            : [];
        var migrated = legacyEvents.Select(item => new CalendarEvent
        {
            Id = Guid.NewGuid().ToString("N"), OwnerId = "system", OwnerName = "EventCalendar",
            Title = item.Title, Description = item.Description, Date = item.Date,
            Location = item.Location, Category = item.Category, IsPublic = true
        }).ToList();
        await WriteAsync(migrated);
    }

    public async Task<List<CalendarEvent>> GetVisibleEventsAsync(string userId) =>
        (await ReadAsync()).Where(item => item.IsPublic || item.OwnerId == userId).OrderBy(item => item.Date).ToList();

    public async Task<CalendarEvent?> GetByIdAsync(string id) => (await ReadAsync()).FirstOrDefault(item => item.Id == id);

    public async Task AddAsync(CalendarEvent item)
    {
        await Gate.WaitAsync();
        try { var all = await ReadUnsafeAsync(); all.Add(item); await WriteUnsafeAsync(all); }
        finally { Gate.Release(); }
    }

    public async Task UpdateAsync(CalendarEvent item)
    {
        await Gate.WaitAsync();
        try { var all = await ReadUnsafeAsync(); var index = all.FindIndex(x => x.Id == item.Id); if (index >= 0) { all[index] = item; await WriteUnsafeAsync(all); } }
        finally { Gate.Release(); }
    }

    public async Task DeleteAsync(string id)
    {
        await Gate.WaitAsync();
        try { var all = await ReadUnsafeAsync(); all.RemoveAll(item => item.Id == id); await WriteUnsafeAsync(all); }
        finally { Gate.Release(); }
    }

    private async Task<List<CalendarEvent>> ReadAsync()
    {
        await Gate.WaitAsync();
        try { return await ReadUnsafeAsync(); }
        finally { Gate.Release(); }
    }

    private async Task<List<CalendarEvent>> ReadUnsafeAsync()
    {
        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<List<CalendarEvent>>(stream, _jsonOptions) ?? [];
    }

    private async Task WriteAsync(List<CalendarEvent> events)
    {
        await Gate.WaitAsync();
        try { await WriteUnsafeAsync(events); }
        finally { Gate.Release(); }
    }

    private async Task WriteUnsafeAsync(List<CalendarEvent> events)
    {
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, events, _jsonOptions);
    }
}
