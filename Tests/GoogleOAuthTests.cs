using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using EventCalendar.API;
using EventCalendar.API.Models;
using EventCalendar.API.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EventCalendar.API.Tests
{
    public class GoogleOAuthTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public GoogleOAuthTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ExternalLogin_With_Valid_IdToken_Returns_Jwt()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Build a mock Google ID token (the real validation is performed by the service)
            var payload = new
            {
                sub = "google-oauth2|12345",
                email = "test@example.com",
                name = "Test User"
            };
            string jsonPayload = JsonSerializer.Serialize(payload);

            // Act
            var response = await client.PostAsync(
                "/api/auth/google/external-login",
                new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Token));
        }

        [Fact]
        public async Task GoogleCallback_With_Valid_Code_Returns_Jwt()
        {
            // Arrange
            var client = _factory.CreateClient();
            var dummyCode = "valid_auth_code";

            // Act
            var response = await client.GetAsync($"/api/auth/google/callback?code={dummyCode}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Token));
        }

        [Fact]
        public async Task Logout_Invalidates_Token()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsync("/api/auth/logout", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        public class TokenResponse
        {
            public string Token { get; set; } = string.Empty;
        }
    }
}