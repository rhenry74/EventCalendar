using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using EventCalendar.API.Data;
using Microsoft.AspNetCore.Authentication;

namespace EventCalendar.API.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly JsonStorage<User> _userStore;

    public AuthController(JsonStorage<User> userStore)
    {
        _userStore = userStore;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] LoginRequest request)
    {
        // Sign out existing session if any
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Redirect to Google OAuth
        var redirectUrl = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={request.ClientId}&redirect_uri={request.RedirectUri}&scope=openid%20email%20profile&response_type=code&state={request.State}";
        
        return Ok(new { RedirectUrl = redirectUrl });
    }

    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromForm] CallbackRequest request)
    {
        if (string.IsNullOrEmpty(request.Code))
        {
            return Unauthorized();
        }

        try
        {
            // Exchange authorization code for tokens using Microsoft.Identity.Web
            var authenticateResult = await AuthenticationHttpContextExtensions.AuthenticateAsync(HttpContext, request.State);
            
            if (!authenticateResult.Succeeded || string.IsNullOrEmpty(authenticateResult.Principal?.Claims.FirstOrDefault(c => c.Type == "access_token")?.Value))
            {
                return Unauthorized();
            }

            // Extract user info from JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var displayNameClaim = User.FindFirst(ClaimTypes.Name)?.Value;
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            var pictureClaim = User.FindFirst("https://purl.org/openid/profile/picture")?.Value;

            // Create or find user
            var newUser = new User
            {
                Subject = userIdClaim ?? "",
                DisplayName = displayNameClaim ?? "Anonymous",
                Email = emailClaim ?? "",
                Picture = pictureClaim ?? ""
            };

            var success = await _userStore.AddAsync(newUser);

            if (success)
            {
                // Use access token directly as the cookie value for stateless auth
                var accessTokenClaim = authenticateResult.Principal?.Claims.FirstOrDefault(c => c.Type == "access_token");
                string? accessToken = accessTokenClaim?.Value;
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    return Unauthorized();
                }

                HttpContext.Response.Cookies.Append(
                    CookieAuthenticationDefaults.CookiePrefix + "jwt", 
                    accessToken, 
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.UtcNow.AddHours(24)
                    }
                );

                // Redirect to frontend with access token
                var redirectUri = $"{request.RedirectUri}?token={accessToken}";
                return Ok(new { Token = accessToken, RedirectUrl = redirectUri });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during OAuth callback: {ex.Message}");
            return Unauthorized();
        }

        return Unauthorized();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<User>> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userStore.GetByIdAsync(userId);

        if (user == null)
        {
            // Try to get from claims as fallback
            var displayNameClaim = User.FindFirst(ClaimTypes.Name)?.Value;
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            var pictureClaim = User.FindFirst("https://purl.org/openid/profile/picture")?.Value;

            return Ok(new User
            {
                Subject = userId,
                DisplayName = displayNameClaim ?? "Anonymous",
                Email = emailClaim ?? "",
                Picture = pictureClaim ?? ""
            });
        }

        return Ok(user);
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<User>>> GetAllUsers()
    {
        var users = await _userStore.GetAllAsync();
        return Ok(users);
    }

        [HttpDelete("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Clear JWT cookie
            var jwtCookie = Request.Cookies[CookieAuthenticationDefaults.CookiePrefix + "jwt"];
            if (!string.IsNullOrEmpty(jwtCookie))
            {
                Response.Cookies.Delete(CookieAuthenticationDefaults.CookiePrefix + "jwt", new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddHours(-1)
                });
            }

            // Sign out from cookie authentication scheme
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(new { Message = "Logged out successfully" });
        }
}

// Request models for AuthController
public class LoginRequest
{
    public string ClientId { get; set; } = "";
    public string RedirectUri { get; set; } = "";
    public string State { get; set; } = "";
}

public class CallbackRequest
{
    public string Code { get; set; } = "";
    public string RedirectUri { get; set; } = "";
    public string State { get; set; } = "";
}