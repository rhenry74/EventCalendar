using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.Linq;

namespace EventCalendar.API.Middleware
{
    /// <summary>
    /// Middleware that validates JWT tokens issued by the application.
    /// It extracts the token from the Authorization header, validates it,
    /// and attaches the authenticated user principal to HttpContext.
    /// </summary>
    public class JwtAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TokenValidationParameters _validationParameters;

        public JwtAuthMiddleware(RequestDelegate next, TokenValidationParameters validationParameters)
        {
            _next = next;
            _validationParameters = validationParameters;
        }

        public async Task Invoke(HttpContext context)
        {
            // Allow unauthenticated requests to proceed; the authorization
            // attribute on controllers will still block unauthenticated users.
            var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ReplaceWithSecureLongSecretKey12345!"));
                    _validationParameters.IssuerSigningKey = key;
                    var handler = new JwtSecurityTokenHandler();
                    var principal = await handler.ValidateTokenAsync(token, _validationParameters);

                    // If validation succeeds, replace the principal.
                    if (principal.IsValid)
                    {
                        context.User = principal.ClaimsPrincipal;
                    }
                }
                catch
                {
                    // Token validation failed – do not modify context.User
                }
            }

            await _next(context);
        }
    }

    /// <summary>
    /// Extension methods for easy registration of the JWT auth middleware.
    /// </summary>
    public static class JwtAuthExtensions
    {
        public static IApplicationBuilder UseJwtAuth(this IApplicationBuilder builder, TokenValidationParameters validationParameters)
        {
            return builder.UseMiddleware<JwtAuthMiddleware>(validationParameters);
        }
    }
}