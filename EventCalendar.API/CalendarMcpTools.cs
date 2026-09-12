using System.ComponentModel;
using System.Globalization;
using System.Security.Claims;
using ModelContextProtocol;
using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class CalendarMcpTools
{
    [McpServerTool(Name = "list_categories", Title = "List event categories", ReadOnly = true, Destructive = false)]
    [Description("List the exact event category names available to the calendar, including their display metadata. Use a returned name exactly when creating or updating an event, and use these names for category filters when searching.")]
    public static Task<IReadOnlyList<EventCategory>> ListCategories(
        CategoryStore categoryStore,
        CancellationToken cancellationToken) => categoryStore.GetAllAsync(cancellationToken);

    [McpServerTool(Name = "search_events", Title = "Search calendar events", ReadOnly = true, Destructive = false)]
    [Description("Search the authenticated user's events. Public events from other users are included, but private events and journal text are only visible to their owner.")]
    public static async Task<IReadOnlyList<McpEventResult>> SearchEvents(
        EventStore store,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken,
        [Description("Optional words to find in the title, description, location, category, or journal.")] string? query = null,
        [Description("Optional exact category name from list_categories.")] string? category = null,
        [Description("Optional inclusive start date or ISO timestamp (YYYY-MM-DD).") ] string? from = null,
        [Description("Optional inclusive end date or ISO timestamp (YYYY-MM-DD).") ] string? to = null,
        [Description("Maximum number of results, from 1 to 100.")] int limit = 50)
    {
        limit = limit is < 1 or > 100 ? 50 : limit;
        var user = GetUser(httpContextAccessor);
        var fromDate = ParseDate(from);
        var toDate = ParseDate(to);
        if (from is not null && fromDate is null) throw new McpException("The 'from' date must be an ISO date or timestamp.");
        if (to is not null && toDate is null) throw new McpException("The 'to' date must be an ISO date or timestamp.");
        if (fromDate is not null && toDate is not null && fromDate > toDate) throw new McpException("The 'from' date cannot be after the 'to' date.");

        var visible = await store.GetVisibleEventsAsync(user.Id);
        var results = visible
            .Where(item => Matches(item, query, category, fromDate, toDate))
            .Take(limit)
            .Select(item => ToResult(item, user.Id))
            .ToList();
        return results;
    }

    [McpServerTool(Name = "get_event", Title = "Get one calendar event", ReadOnly = true, Destructive = false)]
    [Description("Get one event by its ID. Private events can only be read by their owner.")]
    public static async Task<McpEventResult> GetEvent(
        [Description("The event ID returned by search_events or create_event.")] string id,
        EventStore store,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var user = GetUser(httpContextAccessor);
        var item = await store.GetByIdAsync(id);
        if (item is null || (!item.IsPublic && item.OwnerId != user.Id)) throw new McpException("Event not found.");
        return ToResult(item, user.Id);
    }

    [McpServerTool(Name = "create_event", Title = "Create a calendar event", ReadOnly = false, Destructive = false, Idempotent = false)]
    [Description("Create a calendar event owned by the authenticated user. Ask for confirmation before creating it when the user's request is ambiguous.")]
    public static async Task<McpEventResult> CreateEvent(
        McpEventInput input,
        EventStore store,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var user = GetUser(httpContextAccessor);
        ValidateInput(input);
        var item = new CalendarEvent
        {
            Id = Guid.NewGuid().ToString("N"),
            OwnerId = user.Id,
            OwnerName = user.Name,
            Title = input.Title.Trim(),
            Description = input.Description?.Trim() ?? string.Empty,
            Journal = input.Journal ?? string.Empty,
            Date = NormalizeCalendarValue(input.Date),
            EndDate = NormalizeOptionalCalendarValue(input.EndDate),
            Location = input.Location?.Trim(),
            Category = input.Category?.Trim(),
            IsPublic = input.IsPublic ?? false
        };
        ValidateRange(item.Date, item.EndDate);
        await store.AddAsync(item);
        return ToResult(item, user.Id);
    }

    [McpServerTool(Name = "update_event", Title = "Update a calendar event", ReadOnly = false, Destructive = true, Idempotent = true)]
    [Description("Update an event owned by the authenticated user. Title and date are required; omitted optional fields keep their existing values.")]
    public static async Task<McpEventResult> UpdateEvent(
        [Description("The event ID to update.")] string id,
        McpEventInput input,
        EventStore store,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var user = GetUser(httpContextAccessor);
        var item = await store.GetByIdAsync(id);
        if (item is null) throw new McpException("Event not found.");
        if (item.OwnerId != user.Id) throw new McpException("Only the event owner can update this event.");
        ValidateInput(input);

        item.Title = input.Title.Trim();
        item.Description = input.Description is null ? item.Description : input.Description.Trim();
        item.Journal = input.Journal is null ? item.Journal : input.Journal.Trim();
        item.Date = NormalizeCalendarValue(input.Date);
        if (input.EndDate is not null) item.EndDate = NormalizeOptionalCalendarValue(input.EndDate);
        if (input.Location is not null) item.Location = input.Location.Trim();
        if (input.Category is not null) item.Category = input.Category.Trim();
        if (input.IsPublic.HasValue) item.IsPublic = input.IsPublic.Value;
        ValidateRange(item.Date, item.EndDate);
        await store.UpdateAsync(item);
        return ToResult(item, user.Id);
    }

    [McpServerTool(Name = "delete_event", Title = "Delete a calendar event", ReadOnly = false, Destructive = true, Idempotent = true)]
    [Description("Permanently delete an event owned by the authenticated user. Always ask for confirmation before calling this tool.")]
    public static async Task<string> DeleteEvent(
        [Description("The event ID to delete.")] string id,
        EventStore store,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var user = GetUser(httpContextAccessor);
        var item = await store.GetByIdAsync(id);
        if (item is null) throw new McpException("Event not found.");
        if (item.OwnerId != user.Id) throw new McpException("Only the event owner can delete this event.");
        await store.DeleteAsync(id);
        return $"Deleted event {id}.";
    }

    private static McpUser GetUser(IHttpContextAccessor accessor)
    {
        var principal = accessor.HttpContext?.User;
        var id = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id)) throw new McpException("The MCP request is not authenticated.");
        return new McpUser(id, principal?.Identity?.Name ?? principal?.FindFirstValue(ClaimTypes.Email) ?? "MCP user");
    }

    private static bool Matches(CalendarEvent item, string? query, string? category, DateTimeOffset? from, DateTimeOffset? to)
    {
        if (!string.IsNullOrWhiteSpace(query))
        {
            var haystack = string.Join(" ", item.Title, item.Description, item.Location, item.Category, item.Journal);
            if (!haystack.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        }

        if (!string.IsNullOrWhiteSpace(category)
            && !string.Equals(item.Category, category.Trim(), StringComparison.OrdinalIgnoreCase)) return false;

        var start = ParseDate(item.Date);
        var end = ParseDate(item.EndDate) ?? start;
        if (from is not null && (end is null || end < from)) return false;
        if (to is not null && (start is null || start > to)) return false;
        return true;
    }

    private static McpEventResult ToResult(CalendarEvent item, string userId) => new(
        item.Id, item.Title, item.Description ?? string.Empty, item.Date, item.EndDate,
        item.Location, item.Category, item.IsPublic, item.OwnerName, item.OwnerId == userId,
        item.OwnerId == userId ? item.Journal ?? string.Empty : string.Empty);

    private static void ValidateInput(McpEventInput input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.Title)) throw new McpException("Title is required.");
        if (string.IsNullOrWhiteSpace(input.Date) || ParseDate(input.Date) is null) throw new McpException("Date must be an ISO date or timestamp.");
        if (!string.IsNullOrWhiteSpace(input.EndDate) && ParseDate(input.EndDate) is null) throw new McpException("EndDate must be an ISO date or timestamp.");
    }

    private static void ValidateRange(string date, string? endDate)
    {
        if (endDate is not null && ParseDate(endDate) < ParseDate(date)) throw new McpException("EndDate cannot be before Date.");
    }

    private static string? NormalizeOptionalCalendarValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : NormalizeCalendarValue(value);

    private static string NormalizeCalendarValue(string value)
    {
        var trimmed = value.Trim();
        if (DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            && parsed.Offset == TimeSpan.Zero
            && parsed.TimeOfDay == TimeSpan.Zero
            && trimmed.Contains('T', StringComparison.OrdinalIgnoreCase))
        {
            return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return trimmed;
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed
            : null;
    }

    private sealed record McpUser(string Id, string Name);
}

public sealed class McpEventInput
{
    [Description("Event title.")]
    public string Title { get; set; } = string.Empty;

    [Description("Start date or timestamp in ISO format. Date-only values represent an all-day event.")]
    public string Date { get; set; } = string.Empty;

    [Description("Optional end date or timestamp in ISO format. Leave empty for a single-day event.")]
    public string? EndDate { get; set; }

    [Description("Optional short description shown on the calendar card.")]
    public string? Description { get; set; }

    [Description("Optional long-form Markdown journal text; it is not shown on the calendar card.")]
    public string? Journal { get; set; }

    [Description("Optional location.")]
    public string? Location { get; set; }

    [Description("Category name. Use list_categories tool to discover supported categories.")]
    public string? Category { get; set; }

    [Description("Optional: whether other authenticated users can see this event. Omit when updating to keep the current value.")]
    public bool? IsPublic { get; set; }
}

public sealed record McpEventResult(
    string Id,
    string Title,
    string Description,
    string Date,
    string? EndDate,
    string? Location,
    string? Category,
    bool IsPublic,
    string OwnerName,
    bool IsOwner,
    string Journal);
