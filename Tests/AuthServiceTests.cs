using System;
using System.Threading.Tasks;
using EventCalendar.API.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventCalendar.API.Tests
{
    public class AuthServiceTests
    {
        private readonly IJwtService _jwtService;
        private readonly Mock<ILogger<JwtService>> _loggerMock;

        public AuthServiceTests()
        {
            _loggerMock = new Mock<ILogger<JwtService>>();
            _jwtService = new JwtService();
        }

        [Fact]
        public async Task CreateToken_Returns_Jwt_Token()
        {
            // Arrange
            string googleId = "google-oauth2|12345";
            string email = "test@example.com";
            string name = "Test User";

            // Act
            var token = await _jwtService.CreateTokenAsync(googleId, email, name);

            // Assert
            Assert.False(string.IsNullOrEmpty(token));
            Assert.Contains("jwt", token); // token contains 'jwt' in its prefix
        }

        [Fact]
        public async Task CreateTokenFromCode_Returns_Empty()
        {
            // Arrange
            string code = "temp_auth_code";

            // Act
            var token = await _jwtService.CreateTokenFromCodeAsync(code);

            // Assert
            Assert.Equal(string.Empty, token);
        }
    }
}