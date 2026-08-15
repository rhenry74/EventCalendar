using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EventCalendar.API.Data;

namespace EventCalendar.API.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController : ControllerBase
{
    private readonly JsonStorage<Event> _eventStore;
    private readonly JsonStorage<User> _userStore;
    
    public string UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    public EventsController(JsonStorage<Event> eventStore, JsonStorage<User> userStore)
    {
        _eventStore = eventStore;
        _userStore = userStore;
    }

    [HttpGet]
    public async Task<ActionResult<List<Event>>> GetEvents(string? userId = null)
    {
        var events = await _eventStore.GetAllAsync();
        
        // Filter by userId if provided
        if (!string.IsNullOrEmpty(userId))
        {
            events = events.Where(e => e.OwnerId == userId).ToList();
        }
        
        return Ok(events);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Event>> GetEvent(string id)
    {
        var eventItem = await _eventStore.GetByIdAsync(id);
        
        if (eventItem == null)
        {
            return NotFound();
        }
        
        // Check ownership - only the owner can view their own events
        if (!string.IsNullOrEmpty(UserId) && eventItem.OwnerId != UserId)
        {
            return Unauthorized();
        }
        
        return Ok(eventItem);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Event>> CreateEvent(Event eventItem)
    {
        // Set owner to current user if not provided
        if (string.IsNullOrEmpty(eventItem.OwnerId))
        {
            eventItem.OwnerId = UserId;
        }
        
        // Verify user is authenticated and owns the event
        if (!string.IsNullOrEmpty(UserId) && eventItem.OwnerId != UserId)
        {
            return Unauthorized();
        }
        
        var success = await _eventStore.AddAsync(eventItem);
        
        if (success)
        {
            return CreatedAtAction(nameof(GetEvent), new { id = eventItem.Id }, eventItem);
        }
        
        return BadRequest("Event with this ID already exists");
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateEvent(string id, Event eventItem)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest();
        }

        var existingEvent = await _eventStore.GetByIdAsync(id);

        if (existingEvent == null)
        {
            return NotFound();
        }

        // Verify user owns this event
        if (!string.IsNullOrEmpty(UserId) && existingEvent.OwnerId != UserId)
        {
            return Unauthorized();
        }

        // Update only the provided fields, keep OwnerId from original
        eventItem.Id = existingEvent.Id;
        eventItem.OwnerId = existingEvent.OwnerId;

        var success = await _eventStore.UpdateAsync(eventItem);
        
        if (success)
        {
            return NoContent();
        }
        
        return BadRequest("Failed to update event");
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteEvent(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest();
        }

        var existingEvent = await _eventStore.GetByIdAsync(id);

        if (existingEvent == null)
        {
            return NotFound();
        }

        // Verify user owns this event
        if (!string.IsNullOrEmpty(UserId) && existingEvent.OwnerId != UserId)
        {
            return Unauthorized();
        }

        var success = await _eventStore.DeleteAsync(id);
        
        if (success)
        {
            return NoContent();
        }
        
        return BadRequest("Failed to delete event");
    }
}