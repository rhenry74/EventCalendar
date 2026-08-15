using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventCalendar.API.Data;
using EventCalendar.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventCalendar.API.Controllers
{
    [ApiController]
    [Route("api/events")]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly EventRepository _eventRepository;
        private readonly UserRepository _userRepository;

        public EventsController(EventRepository eventRepository, UserRepository userRepository)
        {
            _eventRepository = eventRepository;
            _userRepository = userRepository;
        }

        // GET: api/events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            var events = await _eventRepository.GetAll();
            if (events == null) return Ok(new List<Event>());

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var userOwned = events.Where(e => e.OwnerId == userId);
            var publicEvents = events.Where(e => e.IsPublic);
            return Ok(userOwned.Concat(publicEvents).ToList());
        }

        // GET: api/events/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEvent(string id)
        {
            var events = await _eventRepository.GetAll();
            var ev = events?.FirstOrDefault(e => e.Id == id);
            if (ev == null) return NotFound();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId != null && userId != ev.OwnerId && !ev.IsPublic) return Forbid();

            return Ok(ev);
        }

        // POST: api/events
        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent([FromBody] Event newEvent)
        {
            if (newEvent == null) return BadRequest();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            newEvent.Id = Guid.NewGuid().ToString();
            newEvent.OwnerId = userId;
            newEvent.CreatedAt = DateTime.UtcNow;
            newEvent.UpdatedAt = DateTime.UtcNow;

            await _eventRepository.Add(newEvent);

            return CreatedAtAction(nameof(GetEvent), new { id = newEvent.Id }, newEvent);
        }

        // PUT: api/events/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(string id, [FromBody] Event updatedEvent)
        {
            if (id != updatedEvent.Id) return BadRequest();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var existing = await _eventRepository.GetById(id);
            if (existing == null) return NotFound();

            if (userId != existing.OwnerId) return Forbid();

            // Copy updated fields
            existing.Title = updatedEvent.Title;
            existing.Description = updatedEvent.Description;
            existing.Start = updatedEvent.Start;
            existing.End = updatedEvent.End;
            existing.IsPublic = updatedEvent.IsPublic;
            existing.Category = updatedEvent.Category;
            existing.Recurrence = updatedEvent.Recurrence;
            existing.UpdatedAt = DateTime.UtcNow;

            await _eventRepository.Update(existing);

            return NoContent();
        }

        // DELETE: api/events/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(string id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var existing = await _eventRepository.GetById(id);
            if (existing == null) return NotFound();

            if (userId != existing.OwnerId) return Forbid();

            await _eventRepository.Delete(id);

            return NoContent();
        }

        // POST: api/events/{id}/share
        [HttpPost("{id}/share")]
        public async Task<IActionResult> ShareEvent(string id, [FromBody] ShareEventRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.UserId)) return BadRequest();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var ev = await _eventRepository.GetById(id);
            if (ev == null) return NotFound();

            if (userId != ev.OwnerId) return Forbid();

            if (ev.ShareInfo == null) ev.ShareInfo = new List<SharedUser>();
            var shareEntry = ev.ShareInfo.FirstOrDefault(s => s.UserId == request.UserId);
            if (shareEntry == null)
            {
                ev.ShareInfo.Add(new SharedUser
                {
                    UserId = request.UserId,
                    PermissionLevel = request.PermissionLevel
                });
            }
            else
            {
                shareEntry.PermissionLevel = request.PermissionLevel;
            }

            await _eventRepository.Update(ev);

            return Ok(new { message = "Event shared" });
        }
    }

    public class ShareEventRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string PermissionLevel { get; set; } = "Read"; // Read, Write, Admin
    }

    public class SharedUser
    {
        public string UserId { get; set; } = string.Empty;
        public string PermissionLevel { get; set; } = "Read";
    }
}