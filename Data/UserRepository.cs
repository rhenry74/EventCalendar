using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventCalendar.API.Models;

namespace EventCalendar.API.Data
{
    /// <summary>
    /// Repository for managing User entities backed by a JSON file with
    /// file‑locking semantics.
    /// </summary>
    public class UserRepository
    {
        private readonly JsonFileLockService<User> _lockService;

        public UserRepository(JsonFileLockService<User> lockService)
        {
            _lockService = lockService;
        }

        /// <summary>
        /// Retrieves all users from the JSON file.
        /// </summary>
        public async Task<IEnumerable<User>> GetAll()
        {
            return await _lockService.ReadAsync();
        }

        /// <summary>
        /// Retrieves a single user by their unique identifier.
        /// </summary>
        public async Task<User?> GetById(string id)
        {
            var users = await _lockService.ReadAsync();
            return users?.FirstOrDefault(u => u.Id == id);
        }

        /// <summary>
        /// Adds a new user to the JSON store.
        /// </summary>
        public async Task Add(User user)
        {
            var users = (await _lockService.ReadAsync()) ?? new List<User>();
            users.Add(user);
            await _lockService.WriteAsync(users);
        }

        /// <summary>
        /// Updates an existing user (identified by Id).
        /// </summary>
        public async Task Update(User user)
        {
            var users = await _lockService.ReadAsync();
            var existing = users?.FirstOrDefault(u => u.Id == user.Id);
            if (existing == null) return;

            // Remove the old entry and add the updated one
            users = users?.Where(u => u.Id != user.Id).ToList();
            users?.Add(user);
            await _lockService.WriteAsync(users);
        }

        /// <summary>
        /// Deletes a user by their identifier.
        /// </summary>
        public async Task Delete(string id)
        {
            var users = await _lockService.ReadAsync();
            users = users?.Where(u => u.Id != id).ToList();
            await _lockService.WriteAsync(users);
        }
    }
}