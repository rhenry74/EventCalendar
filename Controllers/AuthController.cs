using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;

namespace EventCalendar.API.Controllers
{
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IJwtService _jwtService;
    private readonly HttpClient _httpClient;

    public AuthController(IJwtService jwtService, IHttpClientFactory httpClientFactory)
    {
        _jwtService = jwtService;
        _httpClient = httpClientFactory.CreateClient();
    }

    private async Task<(string googleId, string email, string name)> ValidateGoogleTokenAsync(string idToken)
    {
        // Call Google token info endpoint to validate ID token
        var response = await _httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        string googleId = root.GetProperty("sub").GetString();
        string email = root.GetProperty("email").GetString();
        string name = root.GetProperty("name").GetString();
        return (googleId, email, name);
    }

    [HttpPost("google/external-login")]
    public async Task<IActionResult> ExternalLogin([FromBody] GoogleExternalLoginRequest request)
    {
        // Expecting request.IdToken
        if (string.IsNullOrEmpty(request?.IdToken)) return BadRequest();
        var (googleId, email, name) = await ValidateGoogleTokenAsync(request.IdToken);
        var token = await _jwtService.CreateTokenAsync(googleId, email, name);
        return Ok(new { token });
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string code)
    {
        // Exchange code for token and create JWT (implementation left as placeholder)
        var token = await _jwtService.CreateTokenFromCodeAsync(code);
        return Ok(new { token });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Invalidate token (implementation depends on token storage)
        return Ok(new { message = "Logged out" });
    }
}

    public class GoogleExternalLoginRequest
    {
        public string GoogleId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}