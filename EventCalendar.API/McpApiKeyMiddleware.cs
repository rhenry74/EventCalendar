using System.Security.Claims;

/// <summary>
/// Protects the remote MCP endpoint with a bearer token issued to a signed-in user.
/// </summary>
public static class McpApiKeyMiddleware
{
    public static async Task<bool> AuthenticateAsync(HttpContext context, McpTokenStore tokenStore)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var authorization)
            || !authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            || !tokenStore.TryResolve(authorization.ToString()[7..].Trim(), out var tokenIdentity))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            return false;
        }

        var identity = new ClaimsIdentity("McpApiKey");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, tokenIdentity.UserId));
        if (!string.IsNullOrWhiteSpace(tokenIdentity.Name)) identity.AddClaim(new Claim(ClaimTypes.Name, tokenIdentity.Name));
        if (!string.IsNullOrWhiteSpace(tokenIdentity.Email)) identity.AddClaim(new Claim(ClaimTypes.Email, tokenIdentity.Email));
        context.User = new ClaimsPrincipal(identity);

        return true;
    }
}
