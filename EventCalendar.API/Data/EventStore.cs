using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EventCalendar.API.Data;

public class EventStore
{
    public string _filePath;
    private readonly IFileLock<List<Event>>? _fileLock;

    public EventStore(string filePath, IFileLock<List<Event>>? fileLock = null)
    {
        _filePath = filePath;
        _fileLock = fileLock;
    }

    public async Task<List<Event>?> GetEventsAsync()
    {
        if (_fileLock != null)
        {
            return await _fileLock.Load();
        }

        try
        {
            var content = File.ReadAllText(_filePath);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.Deserialize<List<Event>>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            });
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Error loading events: {ex.Message}");
            return null;
        }
    }

    public async Task SaveEvent(Event eventItem)
    {
        if (_fileLock != null)
        {
            var events = await _fileLock.Load();
            if (events == null)
            {
                events = new List<Event>();
            }

            events.RemoveAll(e => e.Id == eventItem.Id);
            events.Add(eventItem);

            // Preserve order by sorting by date, then add new event at the end
            events.Sort((a, b) => System.DateTime.Parse(a.Date).CompareTo(System.DateTime.Parse(b.Date)));

            await _fileLock.Save(events);
        }
        else
        {
            var events = await GetEventsAsync();
            if (events == null)
            {
                events = new List<Event>();
            }
            events.RemoveAll(e => e.Id == eventItem.Id);
            events.Add(eventItem);

            // Preserve order by sorting by date, then add new event at the end
            events.Sort((a, b) => System.DateTime.Parse(a.Date).CompareTo(System.DateTime.Parse(b.Date)));

            await File.WriteAllTextAsync(
                _filePath,
                JsonSerializer.Serialize(events, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
                }));
        }
    }

    // Proper async return type with null coalescing
    public async Task<List<Event>> GetAllEvents()
    {
        var events = await GetEventsAsync();
        return events ?? new List<Event>();
    }
}