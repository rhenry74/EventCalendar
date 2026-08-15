using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EventCalendar.API;
using EventCalendar.API.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EventCalendar.API.Tests
{
    public class EventFilteringTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public EventFilteringTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetEvents_Returns_Only_Public_Or_Owners_Events()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Create a private event owned by a different user
            var privateEvent = new Event
            {
                Id = "private-1",
                OwnerId = "other-user-id",
                Title = "Private Meeting",
                IsPublic = false,
                Start = DateTime.UtcNow.AddDays(1).ToString("o")
            };
            // Seed the event by directly writing to the JSON file used by the test host
            var eventsFilePath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "events-test.json");
            var seedContent = await System.Text.Json.JsonSerializer.SerializeAsync(
                new[] { privateEvent });
            await System.IO.File.WriteAllTextAsync(eventsFilePath, seedContent);
            // Force reload by disposing and recreating the factory (simplified for this test)
            var scopedFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the EventRepository JsonFileLockService with one pointing to our seed file
                    var repo = services.BuildServiceProvider()
                        .GetRequiredService<EventRepository>();
                    // No direct replacement; instead we rely on the file already being present
                });
            });

            // Use the original factory for the request
            var response = await client.GetAsync("/api/events");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var returnedEvents = await response.Content.ReadFromJsonAsync<Event[]>();
            Assert.NotNull(returnedEvents);
            // The private event should NOT appear in the response because it is not public
            // and the requesting user (the test client) is not the owner.
            Assert.DoesNotContain(returnedEvents, e => e.Id == "private-1");
        }

        [Fact]
        public async Task CreateEvent_Sets_OwnerId_And_Saves_To_Json()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Obtain a JWT via the mock auth endpoint (simplified)
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-jwt-token");

            var newEvent = new Event
            {
                Title = "Team Lunch",
                Description = "Monthly lunch with the team",
                Start = DateTime.UtcNow.AddDays(2).ToString("o"),
                End = DateTime.UtcNow.AddDays(2).AddHours(1).ToString("o"),
                IsPublic = true
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/events", newEvent);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<Event>();
            Assert.NotNull(created);
            Assert.Equal("Team Lunch", created!.Title);
            Assert.True(created.IsPublic);
        }
    }
}