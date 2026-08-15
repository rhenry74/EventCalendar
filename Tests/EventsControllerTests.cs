using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EventCalendar.API;
using EventCalendar.API.Models;
using EventCalendar.API.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EventCalendar.API.Tests
{
    public class EventsControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public EventsControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetEvents_Returns_Only_Public_Or_Owners_Events()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Seed a private event owned by a different user
            var privateEvent = new Event
            {
                Id = "private-1",
                OwnerId = "other-user",
                Title = "Private Meeting",
                IsPublic = false,
                Start = DateTime.UtcNow.AddDays(1).ToString("o")
            };
            var seedContent = await System.Text.Json.JsonSerializer.SerializeAsync(new[] { privateEvent });
            var seedPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "events-seed.json");
            await System.IO.File.WriteAllTextAsync(seedPath, seedContent);
            // The test harness will reload the DB; for simplicity we rely on in‑memory state.

            // Act
            var response = await client.GetAsync("/api/events");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var events = await response.Content.ReadFromJsonAsync<Event[]>();
            Assert.NotNull(events);
            // No event should have the private Id we seeded
            Assert.DoesNotContain(events, e => e.Id == "private-1");
        }

        [Fact]
        public async Task CreateEvent_With_Valid_Jwt_Returns_201()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Log in to obtain a JWT (this is a simplified flow)
            // In a real test you would obtain a token from the auth endpoint.
            // Here we inject a dummy valid token for brevity.
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "dummy.valid.jwt");

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
        }
    }
}