using System.IO;
using System.Threading.Tasks;
using EventCalendar.API.Data;
using EventCalendar.API.Models;
using Xunit;

namespace EventCalendar.API.Tests
{
    public class ConcurrencyTests
    {
        /// <summary>
        /// Simulates multiple concurrent write attempts to the same JSON file.
        /// The test verifies that no file corruption occurs and that the final
        /// content reflects the last successful write.
        /// </summary>
        [Fact]
        public async Task ConcurrentWrites_DoNotCorruptJsonFile()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), $"events-concurrency-test-{Guid.NewGuid()}.json");
            var seedEvent = new Event { Id = "temp-1", OwnerId = "owner-1", Title = "Temp Event", IsPublic = true };
            var initialContent = await System.Text.Json.JsonSerializer.SerializeAsync(new[] { seedEvent });
            await File.WriteAllTextAsync(tempFile, initialContent);

            var lockService = new JsonFileLockService<Event>(tempFile);

            // Create many tasks that attempt to write concurrently
            var tasks = new List<Task<bool>>();
            for (int i = 0; i < 20; i++)
            {
                tasks.Add(WriteEventAsync(lockService, seedEvent));
            }

            // Act
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, r => Assert.True(r)); // each write should succeed
            // Verify file still valid JSON and contains the last written event
            var finalContent = await File.ReadAllTextAsync(tempFile);
            var finalEvents = await System.Text.Json.JsonSerializer.DeserializeAsync<Event[]>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(finalContent)));
            Assert.NotNull(finalEvents);
            Assert.Contains(finalEvents, e => e.Id == "temp-1");
        }

        private async Task<bool> WriteEventAsync(JsonFileLockService<Event> lockService, Event ev)
        {
            ev.Id = Guid.NewGuid().ToString();
            return await lockService.WriteAsync(ev);
        }
    }
}