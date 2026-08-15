using System.Threading.Tasks;

namespace EventCalendar.API.Services
{
    public interface IJwtService
    {
        /// <summary>
        /// Creates a JWT for the specified Google user.
        /// </summary>
        Task<string> CreateTokenAsync(string googleId, string email, string name);

        /// <summary>
        /// Optionally creates a token from an authorization code.
        /// </summary>
        Task<string> CreateTokenFromCodeAsync(string code);
    }
}