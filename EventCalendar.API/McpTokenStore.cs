using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;

public sealed record McpTokenIdentity(string UserId, string? Name, string? Email);

/// <summary>
/// Stores MCP bearer tokens for the lifetime of this API process.
/// Tokens are intentionally not persisted; users can issue a replacement at any time.
/// </summary>
public sealed class McpTokenStore
{
    private readonly ConcurrentDictionary<string, McpTokenIdentity> tokens = new(StringComparer.Ordinal);

    public string Create(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Google did not provide a user identifier.");
        var identity = new McpTokenIdentity(
            userId,
            user.Identity?.Name,
            user.FindFirstValue(ClaimTypes.Email));

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        tokens[token] = identity;
        return token;
    }

    public bool TryResolve(string token, out McpTokenIdentity identity) => tokens.TryGetValue(token, out identity!);
}
