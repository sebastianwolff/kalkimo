using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Kalkimo.Api.Infrastructure;
using Kalkimo.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace Kalkimo.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;
    private readonly IAuthStore _authStore;

    public AuthController(IConfiguration config, ILogger<AuthController> logger, IAuthStore authStore)
    {
        _config = config;
        _logger = logger;
        _authStore = authStore;
    }

    /// <summary>
    /// Benutzer-Registrierung
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        // Validate password complexity
        var passwordErrors = ValidatePasswordComplexity(request.Password);
        if (passwordErrors.Count > 0)
        {
            return BadRequest(new { error = "Password does not meet complexity requirements", details = passwordErrors });
        }

        var userId = Guid.NewGuid().ToString();
        var passwordHash = HashPassword(request.Password);
        var emailKey = request.Email.ToLowerInvariant();

        var newUser = new UserRecord
        {
            Id = userId,
            Email = request.Email,
            Name = request.Name,
            PasswordHash = passwordHash,
            Roles = new[] { "User" },
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Atomic check-and-add to prevent race conditions
        if (!await _authStore.TryAddUserAsync(emailKey, newUser, ct))
        {
            return BadRequest(new { error = "Email already registered" });
        }

        _logger.LogInformation("User registered: {Email}", request.Email);

        return await GenerateTokens(userId, request.Email, request.Name, new[] { "User" }, ct);
    }

    /// <summary>
    /// Benutzer-Login
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var emailKey = request.Email.ToLowerInvariant();

        var user = await _authStore.GetUserByEmailAsync(emailKey, ct);
        if (user == null)
        {
            return Unauthorized(new { error = "Invalid credentials" });
        }

        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { error = "Invalid credentials" });
        }

        _logger.LogInformation("User logged in: {Email}", request.Email);

        return await GenerateTokens(user.Id, user.Email, user.Name, user.Roles, ct);
    }

    /// <summary>
    /// Token-Refresh
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        // Atomic removal - if we can't remove it, it's already been used or doesn't exist
        var tokenRecord = await _authStore.TryRemoveRefreshTokenAsync(request.RefreshToken, ct);
        if (tokenRecord == null)
        {
            return Unauthorized(new { error = "Invalid refresh token" });
        }

        if (tokenRecord.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return Unauthorized(new { error = "Refresh token expired" });
        }

        var user = await _authStore.GetUserByIdAsync(tokenRecord.UserId, ct);
        if (user == null)
        {
            return Unauthorized(new { error = "User not found" });
        }

        return await GenerateTokens(user.Id, user.Email, user.Name, user.Roles, ct);
    }

    /// <summary>
    /// Logout (Token invalidieren)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            // Alle Refresh Tokens des Users invalidieren
            await _authStore.RemoveAllRefreshTokensForUserAsync(userId, ct);
        }

        return Ok(new { message = "Logged out" });
    }

    /// <summary>
    /// Aktueller Benutzer
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<UserInfo> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var name = User.FindFirst(ClaimTypes.Name)?.Value;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        return Ok(new UserInfo
        {
            Id = userId ?? "",
            Email = email ?? "",
            Name = name ?? "",
            Roles = roles,
            Entitlements = Array.Empty<string>()
        });
    }

    private async Task<ActionResult<LoginResponse>> GenerateTokens(string userId, string email, string name, string[] roles, CancellationToken ct)
    {
        var jwtKey = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT signing key not configured");
        var jwtIssuer = _config["Jwt:Issuer"] ?? "Kalkimo";
        var jwtAudience = _config["Jwt:Audience"] ?? "KalkimoApp";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // Refresh Token generieren (retry if collision, though extremely unlikely)
        string refreshToken;
        var tokenRecord = new RefreshTokenRecord
        {
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        do
        {
            refreshToken = GenerateRefreshToken();
        } while (!await _authStore.TryAddRefreshTokenAsync(refreshToken, tokenRecord, ct));

        var response = new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = new UserInfo
            {
                Id = userId,
                Email = email,
                Name = name,
                Roles = roles,
                Entitlements = Array.Empty<string>()
            }
        };

        return Ok(response);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashPassword(string password)
    {
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        var combined = new byte[48];
        Array.Copy(salt, 0, combined, 0, 16);
        Array.Copy(hash, 0, combined, 16, 32);

        return Convert.ToBase64String(combined);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var combined = Convert.FromBase64String(storedHash);
        var salt = combined[..16];
        var storedHashBytes = combined[16..];

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var computedHash = pbkdf2.GetBytes(32);

        return CryptographicOperations.FixedTimeEquals(computedHash, storedHashBytes);
    }

    /// <summary>
    /// Validates password complexity requirements
    /// </summary>
    private static List<string> ValidatePasswordComplexity(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Passwort ist erforderlich");
            return errors;
        }

        if (password.Length < 8)
        {
            errors.Add("Passwort muss mindestens 8 Zeichen lang sein");
        }

        if (password.Length > 128)
        {
            errors.Add("Passwort darf maximal 128 Zeichen lang sein");
        }

        if (!Regex.IsMatch(password, @"[A-Z]"))
        {
            errors.Add("Passwort muss mindestens einen Großbuchstaben enthalten");
        }

        if (!Regex.IsMatch(password, @"[a-z]"))
        {
            errors.Add("Passwort muss mindestens einen Kleinbuchstaben enthalten");
        }

        if (!Regex.IsMatch(password, @"\d"))
        {
            errors.Add("Passwort muss mindestens eine Ziffer enthalten");
        }

        if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>_\-+=\[\]\\\/~`]"))
        {
            errors.Add("Passwort muss mindestens ein Sonderzeichen enthalten");
        }

        return errors;
    }
}
