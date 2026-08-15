using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;
using System.Linq;
using System;

namespace EventCalendar.API;

// Import types from Data folder
using EventCalendar.API.Data;

// Add using for cookie options and JWT handling
using Microsoft.AspNetCore.Http.Extensions;
using System.IdentityModel.Tokens.Jwt;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Google OAuth Authentication using Microsoft.Identity.Web
        builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration);

        // Add CORS support  
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowVite", policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .WithExposedHeaders("Location");
            });
        });

        // Register storage services with file locking
        string projectRoot = Path.GetFullPath(Environment.CurrentDirectory);
        string eventsFilePath = Path.Combine(projectRoot, "public", "events.json");
        string usersFilePath = Path.Combine(projectRoot, "users.json");

        builder.Services.AddSingleton<IFileLock<List<Event>>>(new FileLock<List<Event>>(eventsFilePath));
        builder.Services.AddSingleton<IFileLock<List<User>>>(new FileLock<List<User>>(usersFilePath));
        
        // Register JsonStorage wrappers
        var eventStore = new JsonStorage<Event>(eventsFilePath, new FileLock<List<Event>>(eventsFilePath));
        var userStore = new JsonStorage<User>(usersFilePath, new FileLock<List<User>>(usersFilePath));
        builder.Services.AddSingleton(eventStore);
        builder.Services.AddSingleton(userStore);

        // Register other necessary services
        builder.Services.AddControllers();

        var app = builder.Build();

        // Apply CORS before routing
        app.UseCors("AllowVite");

        // Configure authentication middleware
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Map OAuth callback endpoint
        app.MapPost("/api/auth/callback", async (HttpContext context, CallbackRequest request) =>
        {
            if (string.IsNullOrEmpty(request.Code))
            {
                return Results.Unauthorized();
            }

            try
            {
                // Exchange authorization code for tokens using Microsoft.Identity.Web
                var tokenResponse = await AuthenticationProperties.CreateRedirectCallback(request.Code).ExecuteAsync();
                
                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    return Results.Unauthorized();
                }

                // Extract user info from JWT claims
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var displayNameClaim = context.User.FindFirst(ClaimTypes.Name)?.Value;
                var emailClaim = context.User.FindFirst(ClaimTypes.Email)?.Value;
                var pictureClaim = context.User.FindFirst("https://purl.org/openid/profile/picture")?.Value;

                // Create or find user
                var newUser = new User
                {
                    Subject = userIdClaim ?? "",
                    DisplayName = displayNameClaim ?? "Anonymous",
                    Email = emailClaim ?? "",
                    Picture = pictureClaim ?? ""
                };

                var success = await userStore.AddAsync(newUser);

                if (success)
                {
                    // Set cookie with JWT token using Microsoft.Identity.Web
                    var jwtTokenHandler = new JwtSecurityTokenHandler();
                    var jwtTokenReader = new JwtSecurityToken(jwtTokenHandler.ReadJwtToken(tokenResponse.AccessToken));
                    string tokenId = jwtTokenReader.Claims.First(c => c.Type == "tid").Value;
                    
                    context.Response.Cookies.Append(
                        CookieAuthenticationDefaults.CookiePrefix + "jwt", 
                        tokenId, 
                        new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Lax,
                            Expires = DateTimeOffset.UtcNow.AddHours(24)
                        }
                    );

                    // Redirect to frontend
                    var redirectUri = $"{request.RedirectUri}?token={tokenId}";
                    return Results.Ok(new { Token = tokenId, RedirectUrl = redirectUri });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during OAuth callback: {ex.Message}");
                return Results.Unauthorized();
            }

            return Results.Unauthorized();
        }).WithName("OAuthCallback");

        // Health check endpoint
        app.MapGet("/health", (HttpContext context) => Results.Ok(new { Status = "Healthy", Message = "EventCalendar API is running" }))
            .WithName("HealthCheck");

        app.Run();
    }
}

// Request models for OAuth callback
public class CallbackRequest
{
    public string Code { get; set; } = "";
    public string RedirectUri { get; set; } = "";
    public string State { get; set; } = "";
}

// User model with Picture property
public class User
{
    public string Id { get; set; } = "";
    public string Subject { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Picture { get; set; }
}

// Event model with OwnerId property
public class Event
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Location { get; set; } = "";
    public string OwnerId { get; set; } = "";
}
