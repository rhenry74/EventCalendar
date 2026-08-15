using System;

namespace EventCalendar.API.Data;

public class User {
    public string Id { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Picture { get; set; }
}

public class Event {
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Location { get; set; }
    public string? Category { get; set; }
    public string? OwnerId { get; set; }  // User who owns this event
}
