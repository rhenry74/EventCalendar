using System.Text.Json;
using System.IO;
using System.Collections.Generic;

// Resolve to public/events.json from EventCalendar root directory (parent of EventCalendar.API)
string projectRoot = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, ".."));
string eventsFilePath = Path.Combine(projectRoot, "public", "events.json");

var builder = WebApplication.CreateBuilder(args);

// Add CORS support  
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVite", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Location");
    });
});

var app = builder.Build();

// Apply CORS before routing
app.UseCors("AllowVite");

// Ensure events directory and file exist
if (!Directory.Exists(Path.GetDirectoryName(eventsFilePath)!))
{
    Directory.CreateDirectory(Path.GetDirectoryName(eventsFilePath)!);
}

// Load initial events if file doesn't exist
if (File.Exists(eventsFilePath) == false)
{
    var defaultEvents = new List<Event>
    {
        new Event { Id = "1", Title = "Tech Conference 2026", Description = "A huge conference for developers to share knowledge.", Date = "2026-08-15T10:00:00", Location = "San Francisco, CA", Category = "Tech" },
        new Event { Id = "2", Title = "Music Festival", Description = "Enjoy live music from various artists.", Date = "2026-08-20T14:00:00", Location = "Austin, TX", Category = "Entertainment" },
        new Event { Id = "3", Title = "Art Gallery Opening", Description = "New exhibition by local artists.", Date = "2026-08-25T18:00:00", Location = "New York, NY", Category = "Art" }
    };
    await JsonSerializer.SerializeAsync(File.OpenWrite(eventsFilePath), defaultEvents, typeof(List<Event>));
}

// API endpoint for events - GET all events
app.MapGet("/api/events", async () =>
{
    var store = new EventStore(eventsFilePath);
    var events = await store.GetAllEvents();
    return Results.Ok(events);
})
.WithName("GetAllEvents")
.WithOpenApi();

// API endpoint for events - POST create new event
app.MapPost("/api/events", async (Event eventItem) =>
{
    // Generate ID for new events
    eventItem.Id = Guid.NewGuid().ToString("N").Substring(0, 8);
    
    var store = new EventStore(eventsFilePath);
    var events = await store.GetAllEvents();
    events.Add(eventItem);
    
    await File.WriteAllTextAsync(
        eventsFilePath, 
        JsonSerializer.Serialize(events, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        }));
    
    return Results.Created($"/api/events/{eventItem.Id}", eventItem);
})
.WithName("CreateEvent")
.WithOpenApi();

// API endpoint for events - PUT update existing event
app.MapPut("/api/events/{id}", async (string id, Event eventItem) =>
{
    var store = new EventStore(eventsFilePath);
    var events = await store.GetAllEvents();
    
    var existingIndex = events.FindIndex(e => e.Id == id);
    if (existingIndex >= 0)
    {
        events[existingIndex] = eventItem;
        
        await File.WriteAllTextAsync(
            eventsFilePath, 
            JsonSerializer.Serialize(events, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            }));
        
        return Results.Ok(eventItem);
    }
    
    return Results.NotFound();
})
.WithName("UpdateEvent")
.WithOpenApi();

// API endpoint for events - DELETE existing event
app.MapDelete("/api/events/{id}", async (string id) =>
{
    var store = new EventStore(eventsFilePath);
    var events = await store.GetAllEvents();
    
    var existingIndex = events.FindIndex(e => e.Id == id);
    if (existingIndex >= 0)
    {
        events.RemoveAt(existingIndex);
        
        await File.WriteAllTextAsync(
            eventsFilePath, 
            JsonSerializer.Serialize(events, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            }));
        
        return Results.NoContent();
    }
    
    return Results.NotFound();
})
.WithName("DeleteEvent")
.WithOpenApi();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Message = "EventCalendar API is running" }))
.WithName("HealthCheck");

app.Run();

public class Event {
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Category { get; set; }
}

public class EventStore {
    private readonly string _filePath;
    
    public EventStore(string filePath)
    {
        _filePath = filePath;
    }
    
    public List<Event> GetEvents()
    {
        try
        {
            return JsonSerializer.Deserialize<List<Event>>(File.ReadAllText(_filePath), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            }) ?? new List<Event>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading events: {ex.Message}");
            return new List<Event>();
        }
    }

    public async Task SaveEvent(Event eventItem)
    {
        var events = GetEvents();
        events.RemoveAll(e => e.Id == eventItem.Id);
        events.Add(eventItem);
        
        // Preserve order by sorting by date, then add new event at the end
        events.Sort((a, b) => DateTime.Parse(a.Date).CompareTo(DateTime.Parse(b.Date)));
        
        await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(events, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        }));
    }

    public async Task<List<Event>> GetAllEvents() => GetEvents();
}
