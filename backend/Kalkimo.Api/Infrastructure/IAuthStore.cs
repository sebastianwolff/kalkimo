using System.Collections.Concurrent;
using System.Text.Json;

namespace Kalkimo.Api.Infrastructure;

/// <summary>
/// Interface für persistente Auth-Datenspeicherung
/// </summary>
public interface IAuthStore
{
    /// <summary>
    /// Speichert einen Benutzer
    /// </summary>
    Task<bool> TryAddUserAsync(string emailKey, UserRecord user, CancellationToken ct = default);

    /// <summary>
    /// Lädt einen Benutzer anhand der E-Mail
    /// </summary>
    Task<UserRecord?> GetUserByEmailAsync(string emailKey, CancellationToken ct = default);

    /// <summary>
    /// Lädt einen Benutzer anhand der ID
    /// </summary>
    Task<UserRecord?> GetUserByIdAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Speichert einen Refresh-Token
    /// </summary>
    Task<bool> TryAddRefreshTokenAsync(string token, RefreshTokenRecord record, CancellationToken ct = default);

    /// <summary>
    /// Entfernt und gibt einen Refresh-Token zurück (atomic)
    /// </summary>
    Task<RefreshTokenRecord?> TryRemoveRefreshTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Entfernt alle Refresh-Tokens eines Benutzers
    /// </summary>
    Task RemoveAllRefreshTokensForUserAsync(string userId, CancellationToken ct = default);
}

/// <summary>
/// Benutzer-Datensatz
/// </summary>
public record UserRecord
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required string PasswordHash { get; init; }
    public required string[] Roles { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Refresh-Token-Datensatz
/// </summary>
public record RefreshTokenRecord
{
    public required string UserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
}

/// <summary>
/// Flatfile-Implementierung des Auth Store mit Verschlüsselung
/// </summary>
public class FlatfileAuthStore : IAuthStore
{
    private readonly string _dataRoot;
    private readonly IEncryptionService _encryption;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _usersLock = new(1, 1);
    private readonly SemaphoreSlim _tokensLock = new(1, 1);

    // In-memory cache for fast lookups
    private readonly ConcurrentDictionary<string, UserRecord> _usersCache = new();
    private readonly ConcurrentDictionary<string, RefreshTokenRecord> _tokensCache = new();
    private bool _cacheInitialized;

    public FlatfileAuthStore(string dataRoot, IEncryptionService encryption)
    {
        _dataRoot = dataRoot;
        _encryption = encryption;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    private async Task EnsureCacheInitializedAsync(CancellationToken ct)
    {
        if (_cacheInitialized) return;

        await _usersLock.WaitAsync(ct);
        try
        {
            if (_cacheInitialized) return;

            // Load users
            var usersFile = GetUsersFilePath();
            if (File.Exists(usersFile))
            {
                try
                {
                    var encryptedBytes = await File.ReadAllBytesAsync(usersFile, ct);
                    var encrypted = DeserializeEncryptedData(encryptedBytes);
                    var json = await _encryption.DecryptAsync(encrypted, "auth-users", ct);
                    var users = JsonSerializer.Deserialize<Dictionary<string, UserRecord>>(json, _jsonOptions);
                    if (users != null)
                    {
                        foreach (var kvp in users)
                        {
                            _usersCache.TryAdd(kvp.Key, kvp.Value);
                        }
                    }
                }
                catch (System.Security.Cryptography.AuthenticationTagMismatchException)
                {
                    // Korrupte/inkompatible verschlüsselte Datei - löschen und neu starten
                    File.Delete(usersFile);
                }
            }

            // Load tokens
            var tokensFile = GetTokensFilePath();
            if (File.Exists(tokensFile))
            {
                try
                {
                    var encryptedBytes = await File.ReadAllBytesAsync(tokensFile, ct);
                    var encrypted = DeserializeEncryptedData(encryptedBytes);
                    var json = await _encryption.DecryptAsync(encrypted, "auth-tokens", ct);
                    var tokens = JsonSerializer.Deserialize<Dictionary<string, RefreshTokenRecord>>(json, _jsonOptions);
                    if (tokens != null)
                    {
                        // Only load non-expired tokens
                        var now = DateTimeOffset.UtcNow;
                        foreach (var kvp in tokens.Where(t => t.Value.ExpiresAt > now))
                        {
                            _tokensCache.TryAdd(kvp.Key, kvp.Value);
                        }
                    }
                }
                catch (System.Security.Cryptography.AuthenticationTagMismatchException)
                {
                    // Korrupte/inkompatible verschlüsselte Datei - löschen und neu starten
                    File.Delete(tokensFile);
                }
            }

            _cacheInitialized = true;
        }
        finally
        {
            _usersLock.Release();
        }
    }

    public async Task<bool> TryAddUserAsync(string emailKey, UserRecord user, CancellationToken ct = default)
    {
        await EnsureCacheInitializedAsync(ct);

        if (!_usersCache.TryAdd(emailKey, user))
        {
            return false;
        }

        await PersistUsersAsync(ct);
        return true;
    }

    public async Task<UserRecord?> GetUserByEmailAsync(string emailKey, CancellationToken ct = default)
    {
        await EnsureCacheInitializedAsync(ct);
        return _usersCache.TryGetValue(emailKey, out var user) ? user : null;
    }

    public async Task<UserRecord?> GetUserByIdAsync(string userId, CancellationToken ct = default)
    {
        await EnsureCacheInitializedAsync(ct);
        return _usersCache.Values.FirstOrDefault(u => u.Id == userId);
    }

    public async Task<bool> TryAddRefreshTokenAsync(string token, RefreshTokenRecord record, CancellationToken ct = default)
    {
        await EnsureCacheInitializedAsync(ct);

        if (!_tokensCache.TryAdd(token, record))
        {
            return false;
        }

        await PersistTokensAsync(ct);
        return true;
    }

    public async Task<RefreshTokenRecord?> TryRemoveRefreshTokenAsync(string token, CancellationToken ct = default)
    {
        await EnsureCacheInitializedAsync(ct);

        if (!_tokensCache.TryRemove(token, out var record))
        {
            return null;
        }

        await PersistTokensAsync(ct);
        return record;
    }

    public async Task RemoveAllRefreshTokensForUserAsync(string userId, CancellationToken ct = default)
    {
        await EnsureCacheInitializedAsync(ct);

        var tokensToRemove = _tokensCache
            .Where(kv => kv.Value.UserId == userId)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var token in tokensToRemove)
        {
            _tokensCache.TryRemove(token, out _);
        }

        if (tokensToRemove.Count > 0)
        {
            await PersistTokensAsync(ct);
        }
    }

    private async Task PersistUsersAsync(CancellationToken ct)
    {
        await _usersLock.WaitAsync(ct);
        try
        {
            var authDir = GetAuthDirectory();
            Directory.CreateDirectory(authDir);

            var usersDict = _usersCache.ToDictionary(kv => kv.Key, kv => kv.Value);
            var json = JsonSerializer.SerializeToUtf8Bytes(usersDict, _jsonOptions);
            var encrypted = await _encryption.EncryptAsync(json, "auth-users", ct);
            var bytes = SerializeEncryptedData(encrypted);

            await WriteWithRetryAsync(GetUsersFilePath(), bytes, ct);
        }
        finally
        {
            _usersLock.Release();
        }
    }

    private async Task PersistTokensAsync(CancellationToken ct)
    {
        await _tokensLock.WaitAsync(ct);
        try
        {
            var authDir = GetAuthDirectory();
            Directory.CreateDirectory(authDir);

            var tokensDict = _tokensCache.ToDictionary(kv => kv.Key, kv => kv.Value);
            var json = JsonSerializer.SerializeToUtf8Bytes(tokensDict, _jsonOptions);
            var encrypted = await _encryption.EncryptAsync(json, "auth-tokens", ct);
            var bytes = SerializeEncryptedData(encrypted);

            await WriteWithRetryAsync(GetTokensFilePath(), bytes, ct);
        }
        finally
        {
            _tokensLock.Release();
        }
    }

    private static async Task WriteWithRetryAsync(string path, byte[] bytes, CancellationToken ct, int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await File.WriteAllBytesAsync(path, bytes, ct);
                return;
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                // Warte kurz und versuche erneut (File-Lock bei parallelen Tests)
                await Task.Delay(50 * (i + 1), ct);
            }
        }
    }

    private string GetAuthDirectory()
    {
        return Path.Combine(_dataRoot, "auth");
    }

    private string GetUsersFilePath()
    {
        return Path.Combine(GetAuthDirectory(), "users.enc");
    }

    private string GetTokensFilePath()
    {
        return Path.Combine(GetAuthDirectory(), "tokens.enc");
    }

    private static byte[] SerializeEncryptedData(EncryptedData data)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        var keyIdBytes = System.Text.Encoding.UTF8.GetBytes(data.KeyId);
        writer.Write(keyIdBytes.Length);
        writer.Write(keyIdBytes);
        writer.Write(data.KeyVersion);
        writer.Write(data.Nonce.Length);
        writer.Write(data.Nonce);
        writer.Write(data.Tag.Length);
        writer.Write(data.Tag);
        writer.Write(data.Ciphertext);

        return ms.ToArray();
    }

    private static EncryptedData DeserializeEncryptedData(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms);

        var keyIdLength = reader.ReadInt32();
        var keyIdBytes = reader.ReadBytes(keyIdLength);
        var keyId = System.Text.Encoding.UTF8.GetString(keyIdBytes);
        var keyVersion = reader.ReadInt32();
        var nonceLength = reader.ReadInt32();
        var nonce = reader.ReadBytes(nonceLength);
        var tagLength = reader.ReadInt32();
        var tag = reader.ReadBytes(tagLength);
        var ciphertext = reader.ReadBytes((int)(ms.Length - ms.Position));

        return new EncryptedData
        {
            KeyId = keyId,
            KeyVersion = keyVersion,
            Nonce = nonce,
            Tag = tag,
            Ciphertext = ciphertext
        };
    }
}

/// <summary>
/// In-Memory Implementation für Tests
/// </summary>
public class InMemoryAuthStore : IAuthStore
{
    private readonly ConcurrentDictionary<string, UserRecord> _users = new();
    private readonly ConcurrentDictionary<string, RefreshTokenRecord> _tokens = new();

    public Task<bool> TryAddUserAsync(string emailKey, UserRecord user, CancellationToken ct = default)
    {
        return Task.FromResult(_users.TryAdd(emailKey, user));
    }

    public Task<UserRecord?> GetUserByEmailAsync(string emailKey, CancellationToken ct = default)
    {
        return Task.FromResult(_users.TryGetValue(emailKey, out var user) ? user : null);
    }

    public Task<UserRecord?> GetUserByIdAsync(string userId, CancellationToken ct = default)
    {
        return Task.FromResult(_users.Values.FirstOrDefault(u => u.Id == userId));
    }

    public Task<bool> TryAddRefreshTokenAsync(string token, RefreshTokenRecord record, CancellationToken ct = default)
    {
        return Task.FromResult(_tokens.TryAdd(token, record));
    }

    public Task<RefreshTokenRecord?> TryRemoveRefreshTokenAsync(string token, CancellationToken ct = default)
    {
        return Task.FromResult(_tokens.TryRemove(token, out var record) ? record : null);
    }

    public Task RemoveAllRefreshTokensForUserAsync(string userId, CancellationToken ct = default)
    {
        var tokensToRemove = _tokens
            .Where(kv => kv.Value.UserId == userId)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var token in tokensToRemove)
        {
            _tokens.TryRemove(token, out _);
        }

        return Task.CompletedTask;
    }
}
