using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventCalendar.API.Models;

namespace EventCalendar.API.Data
{
    /// <summary>
    /// Repository for managing Event entities backed by a JSON file with
    /// file‑locking semantics.
    /// </summary>
    public class EventRepository
    {
        private readonly JsonFileLockService<Event> _lockService;

        public EventRepository(JsonFileLockService<Event> lockService)
        {
            _lockService = lockService;
        }

        /// <summary>
        /// Retrieves all events from the JSON file.
        /// </summary>
        public async Task<IEnumerable<Event>> GetAll()
        {
            return await _lockService.ReadAsync();
        }

        /// <summary>
        /// Retrieves a single event by its unique identifier.
        /// </summary>
        public async Task<Event?> GetById(string id)
        {
            var events = await _lockService.ReadAsync();
            return events?.FirstOrDefault(e => e.Id == id);
        }

        /// <summary>
        /// Adds a new event to the JSON store.
        /// </summary>
        public async Task Add(Event @event)
        {
            var events = (await _lockService.ReadAsync()) ?? new List<Event>();
            events.Add(@event);
            await _lockService.WriteAsync(events);
        }

        /// <summary>
        /// Updates an existing event (identified by Id).
        /// </summary>
        public async Task Update(Event @event)
        {
            var events = await _lockService.ReadAsync();
            var existing = events?.FirstOrDefault(e => e.Id == @event.Id);
            if (existing == null) return;

            // Remove the old entry and add the updated one
            events = events?.Where(e => e.Id != @event.Id).ToList();
            events?.Add(@event);
            await _lockService.WriteAsync(events);
        }

        /// <summary>
        /// Deletes an event by its identifier.
        /// </summary>
        public async Task Delete(string id)
        {
            var events = await _lockService.ReadAsync();
            events = events?.Where(e => e.Id != id).ToList();
            await _lockService.WriteAsync(events);
        }
    }
}