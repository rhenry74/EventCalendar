using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;

namespace EventCalendar.API.Services
{
    public class JwtService : IJwtService
    {
        private readonly string _secretKey;

        public JwtService()
        {
            // In production, load this from configuration/secrets
            _secretKey = "ReplaceWithSecureLongSecretKey12345!";
        }

        public async Task<string> CreateTokenAsync(string googleId, string email, string name)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, googleId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim("email", email),
                new Claim("name", name)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "EventCalendar",
                audience: "EventCalendarClients",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public Task<string> CreateTokenFromCodeAsync(string code)
        {
            // Placeholder – not used for Google external login
            return Task.FromResult(string.Empty);
        }
    }
}