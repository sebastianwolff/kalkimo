namespace Kalkimo.Api.Models;

/// <summary>
/// Login-Anfrage
/// </summary>
public record LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

/// <summary>
/// Login-Antwort
/// </summary>
public record LoginResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required UserInfo User { get; init; }
}

/// <summary>
/// Token-Refresh-Anfrage
/// </summary>
public record RefreshRequest
{
    public required string RefreshToken { get; init; }
}

/// <summary>
/// Benutzerinformationen
/// </summary>
public record UserInfo
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Entitlements { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Registrierungs-Anfrage
/// </summary>
public record RegisterRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string Name { get; init; }
}
