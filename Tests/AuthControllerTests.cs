using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EventCalendar.API.Controllers;
using EventCalendar.API.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EventCalendar.API.Tests
{
    public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public AuthControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ExternalLogin_Returns_Jwt_When_Valid_Token()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Prepare a mock Google ID token payload (the real validation is omitted for brevity)
            var payload = new
            {
                sub = "google-oauth2|12345",
                email = "test@example.com",
                name = "Test User"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/auth/google/external-login", payload);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result!.Token));
        }

        [Fact]
        public async Task GoogleCallback_Returns_Jwt_When_Code_Provided()
        {
            // Arrange
            var client = _factory.CreateClient();
            var dummyCode = "dummy-auth-code";

            // Act
            var response = await client.GetAsync($"/api/auth/google/callback?code={dummyCode}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result!.Token));
        }

        [Fact]
        public async Task Logout_Returns_Ok()
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