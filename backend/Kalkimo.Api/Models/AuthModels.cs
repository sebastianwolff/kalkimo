using System.ComponentModel.DataAnnotations;

namespace Kalkimo.Api.Models;

/// <summary>
/// Login-Anfrage
/// </summary>
public record LoginRequest
{
    [Required(ErrorMessage = "E-Mail ist erforderlich")]
    [EmailAddress(ErrorMessage = "Ungültiges E-Mail-Format")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Passwort ist erforderlich")]
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
    [Required(ErrorMessage = "E-Mail ist erforderlich")]
    [EmailAddress(ErrorMessage = "Ungültiges E-Mail-Format")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Passwort ist erforderlich")]
    [MinLength(8, ErrorMessage = "Passwort muss mindestens 8 Zeichen lang sein")]
    [MaxLength(128, ErrorMessage = "Passwort darf maximal 128 Zeichen lang sein")]
    public required string Password { get; init; }

    [Required(ErrorMessage = "Name ist erforderlich")]
    [MinLength(2, ErrorMessage = "Name muss mindestens 2 Zeichen lang sein")]
    [MaxLength(100, ErrorMessage = "Name darf maximal 100 Zeichen lang sein")]
    public required string Name { get; init; }
}
