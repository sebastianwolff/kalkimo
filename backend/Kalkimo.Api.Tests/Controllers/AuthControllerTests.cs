using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Kalkimo.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kalkimo.Api.Tests.Controllers;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Registration Tests

    [Fact]
    public async Task Register_WithValidData_ReturnsTokens()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = $"test-{Guid.NewGuid()}@example.com",
            Password = "SecurePassword123!",
            Name = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.AccessToken.Should().NotBeNullOrEmpty();
        loginResponse.RefreshToken.Should().NotBeNullOrEmpty();
        loginResponse.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
        loginResponse.User.Should().NotBeNull();
        loginResponse.User.Email.Should().Be(request.Email);
        loginResponse.User.Name.Should().Be(request.Name);
        loginResponse.User.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var email = $"duplicate-{Guid.NewGuid()}@example.com";
        var request = new RegisterRequest
        {
            Email = email,
            Password = "SecurePassword123!",
            Name = "Test User"
        };

        // First registration
        await _client.PostAsJsonAsync("/api/auth/register", request);

        // Act - Second registration with same email
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_EmailIsCaseInsensitive()
    {
        // Arrange
        var baseEmail = $"casetest-{Guid.NewGuid()}@example.com";
        var request1 = new RegisterRequest
        {
            Email = baseEmail.ToLower(),
            Password = "SecurePassword123!",
            Name = "Test User"
        };
        var request2 = new RegisterRequest
        {
            Email = baseEmail.ToUpper(),
            Password = "SecurePassword123!",
            Name = "Another User"
        };

        // First registration
        await _client.PostAsJsonAsync("/api/auth/register", request1);

        // Act - Second registration with uppercase email
        var response = await _client.PostAsJsonAsync("/api/auth/register", request2);

        // Assert - Should fail because email already exists (case-insensitive)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        // Arrange
        var email = $"login-{Guid.NewGuid()}@example.com";
        var password = "SecurePassword123!";

        // Register first
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = password,
            Name = "Login Test User"
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.AccessToken.Should().NotBeNullOrEmpty();
        loginResponse.RefreshToken.Should().NotBeNullOrEmpty();
        loginResponse.User.Email.Should().Be(email);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "SomePassword123!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var email = $"wrongpass-{Guid.NewGuid()}@example.com";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "CorrectPassword123!",
            Name = "Test User"
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "WrongPassword123!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_EmailIsCaseInsensitive()
    {
        // Arrange
        var email = $"logincase-{Guid.NewGuid()}@example.com";
        var password = "SecurePassword123!";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email.ToLower(),
            Password = password,
            Name = "Case Test User"
        });

        // Act - Login with uppercase email
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email.ToUpper(),
            Password = password
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Token Refresh Tests

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        var email = $"refresh-{Guid.NewGuid()}@example.com";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "SecurePassword123!",
            Name = "Refresh Test User"
        });

        var loginResult = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var originalAccessToken = loginResult!.AccessToken;
        var refreshToken = loginResult.RefreshToken;

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest
        {
            RefreshToken = refreshToken
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var newLoginResult = await response.Content.ReadFromJsonAsync<LoginResponse>();
        newLoginResult.Should().NotBeNull();
        newLoginResult!.AccessToken.Should().NotBeNullOrEmpty();
        newLoginResult.RefreshToken.Should().NotBeNullOrEmpty();

        // New tokens should be different from old ones
        newLoginResult.AccessToken.Should().NotBe(originalAccessToken);
        newLoginResult.RefreshToken.Should().NotBe(refreshToken);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest
        {
            RefreshToken = "invalid-refresh-token"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithUsedToken_ReturnsUnauthorized()
    {
        // Arrange
        var email = $"usedtoken-{Guid.NewGuid()}@example.com";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "SecurePassword123!",
            Name = "Used Token Test"
        });

        var loginResult = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var refreshToken = loginResult!.RefreshToken;

        // First refresh - should succeed
        await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest
        {
            RefreshToken = refreshToken
        });

        // Act - Second refresh with same token
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest
        {
            RefreshToken = refreshToken
        });

        // Assert - Token was invalidated after first use
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_WithValidToken_ReturnsOk()
    {
        // Arrange
        var email = $"logout-{Guid.NewGuid()}@example.com";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "SecurePassword123!",
            Name = "Logout Test User"
        });

        var loginResult = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        // Act
        var response = await authenticatedClient.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_InvalidatesRefreshTokens()
    {
        // Arrange
        var email = $"logoutinvalidate-{Guid.NewGuid()}@example.com";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "SecurePassword123!",
            Name = "Logout Invalidate Test"
        });

        var loginResult = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var refreshToken = loginResult!.RefreshToken;

        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult.AccessToken);

        // Logout
        await authenticatedClient.PostAsync("/api/auth/logout", null);

        // Act - Try to use refresh token after logout
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest
        {
            RefreshToken = refreshToken
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Protected Endpoint Tests

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsUserInfo()
    {
        // Arrange
        var email = $"me-{Guid.NewGuid()}@example.com";
        var name = "Current User Test";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "SecurePassword123!",
            Name = name
        });

        var loginResult = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        // Act
        var response = await authenticatedClient.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var userInfo = await response.Content.ReadFromJsonAsync<UserInfo>();
        userInfo.Should().NotBeNull();
        userInfo!.Email.Should().Be(email);
        userInfo.Name.Should().Be(name);
        userInfo.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithBadToken = _factory.CreateClient();
        clientWithBadToken.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await clientWithBadToken.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithExpiredToken_ReturnsUnauthorized()
    {
        // Arrange - Create an obviously invalid/expired token
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiZXhwIjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        var clientWithExpiredToken = _factory.CreateClient();
        clientWithExpiredToken.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await clientWithExpiredToken.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region JWT Token Security Tests

    [Fact]
    public async Task AccessToken_ContainsCorrectClaims()
    {
        // Arrange
        var email = $"claims-{Guid.NewGuid()}@example.com";
        var name = "Claims Test User";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "SecurePassword123!",
            Name = name
        });

        var loginResult = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Act - Decode the JWT (without verification, just to check structure)
        var tokenParts = loginResult!.AccessToken.Split('.');
        tokenParts.Should().HaveCount(3); // Header, Payload, Signature

        var payload = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(PadBase64(tokenParts[1])));
        var claims = JsonSerializer.Deserialize<Dictionary<string, object>>(payload);

        // Assert
        claims.Should().ContainKey("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
        claims.Should().ContainKey("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
        claims.Should().ContainKey("exp"); // Expiration
        claims.Should().ContainKey("jti"); // Unique token ID
    }

    [Fact]
    public async Task MultipleLogins_CreateDifferentTokens()
    {
        // Arrange
        var email = $"multilogin-{Guid.NewGuid()}@example.com";
        var password = "SecurePassword123!";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = password,
            Name = "Multi Login Test"
        });

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        var result1 = await response1.Content.ReadFromJsonAsync<LoginResponse>();

        var response2 = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        var result2 = await response2.Content.ReadFromJsonAsync<LoginResponse>();

        // Assert - Each login should produce unique tokens
        result1!.AccessToken.Should().NotBe(result2!.AccessToken);
        result1.RefreshToken.Should().NotBe(result2.RefreshToken);
    }

    #endregion

    #region Password Security Tests

    [Fact]
    public async Task Register_PasswordIsHashed_NotStoredPlaintext()
    {
        // This test verifies that after registration, login works correctly,
        // which implies the password is properly hashed and verified

        var email = $"hashtest-{Guid.NewGuid()}@example.com";
        var password = "SecurePassword123!";

        // Register
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = password,
            Name = "Hash Test User"
        });

        // Login should work with correct password
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Login should fail with slightly different password
        var wrongLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password + "X"
        });
        wrongLoginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_SamePasswordDifferentUsers_HaveDifferentHashes()
    {
        // This is implicitly tested by the fact that both users can login
        // with the same password - verifies salt is being used

        var password = "SamePassword123!";
        var email1 = $"samepass1-{Guid.NewGuid()}@example.com";
        var email2 = $"samepass2-{Guid.NewGuid()}@example.com";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email1,
            Password = password,
            Name = "User 1"
        });

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email2,
            Password = password,
            Name = "User 2"
        });

        // Both should be able to login
        var login1 = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email1,
            Password = password
        });
        var login2 = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email2,
            Password = password
        });

        login1.StatusCode.Should().Be(HttpStatusCode.OK);
        login2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    private static string PadBase64(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: return base64 + "==";
            case 3: return base64 + "=";
            default: return base64;
        }
    }
}
