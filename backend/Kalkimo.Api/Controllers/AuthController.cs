using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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

    // Thread-safe In-Memory User Store für MVP (später durch echte Datenbank ersetzen)
    // WARNING: Data is lost on restart - use proper persistence in production
    private static readonly ConcurrentDictionary<string, UserRecord> _users = new();
    private static readonly ConcurrentDictionary<string, RefreshTokenRecord> _refreshTokens = new();

    public AuthController(IConfiguration config, ILogger<AuthController> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Benutzer-Registrierung
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
    {
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
        if (!_users.TryAdd(emailKey, newUser))
        {
            return BadRequest(new { error = "Email already registered" });
        }

        _logger.LogInformation("User registered: {Email}", request.Email);

        return await GenerateTokens(userId, request.Email, request.Name, new[] { "User" });
    }

    /// <summary>
    /// Benutzer-Login
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var emailKey = request.Email.ToLowerInvariant();

        if (!_users.TryGetValue(emailKey, out var user))
        {
            return Unauthorized(new { error = "Invalid credentials" });
        }

        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { error = "Invalid credentials" });
        }

        _logger.LogInformation("User logged in: {Email}", request.Email);

        return await GenerateTokens(user.Id, user.Email, user.Name, user.Roles);
    }

    /// <summary>
    /// Token-Refresh
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshRequest request)
    {
        // Atomic removal - if we can't remove it, it's already been used or doesn't exist
        if (!_refreshTokens.TryRemove(request.RefreshToken, out var tokenRecord))
        {
            return Unauthorized(new { error = "Invalid refresh token" });
        }

        if (tokenRecord.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return Unauthorized(new { error = "Refresh token expired" });
        }

        var user = _users.Values.FirstOrDefault(u => u.Id == tokenRecord.UserId);
        if (user == null)
        {
            return Unauthorized(new { error = "User not found" });
        }

        return await GenerateTokens(user.Id, user.Email, user.Name, user.Roles);
    }

    /// <summary>
    /// Logout (Token invalidieren)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Alle Refresh Tokens des Users invalidieren
        var tokensToRemove = _refreshTokens
            .Where(kv => kv.Value.UserId == userId)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var token in tokensToRemove)
        {
            _refreshTokens.TryRemove(token, out _);
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

    private Task<ActionResult<LoginResponse>> GenerateTokens(string userId, string email, string name, string[] roles)
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
        } while (!_refreshTokens.TryAdd(refreshToken, tokenRecord));

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

        return Task.FromResult<ActionResult<LoginResponse>>(Ok(response));
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

    private record UserRecord
    {
        public required string Id { get; init; }
        public required string Email { get; init; }
        public required string Name { get; init; }
        public required string PasswordHash { get; init; }
        public required string[] Roles { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }

    private record RefreshTokenRecord
    {
        public required string UserId { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset ExpiresAt { get; init; }
    }
}
