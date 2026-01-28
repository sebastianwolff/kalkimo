using System.Security.Cryptography;

namespace Kalkimo.Api.Infrastructure;

/// <summary>
/// Envelope Encryption Service Interface
/// DEK (Data Encryption Key) pro Projekt
/// KEK (Key Encryption Key) pro User
/// Master KEK für Break-Glass
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Verschlüsselt Daten mit dem Projekt-DEK
    /// </summary>
    Task<EncryptedData> EncryptAsync(byte[] plaintext, string projectId, CancellationToken ct = default);

    /// <summary>
    /// Entschlüsselt Daten mit dem Projekt-DEK
    /// </summary>
    Task<byte[]> DecryptAsync(EncryptedData encrypted, string projectId, CancellationToken ct = default);

    /// <summary>
    /// Generiert einen neuen DEK für ein Projekt
    /// </summary>
    Task<WrappedKey> GenerateProjectKeyAsync(string projectId, string userId, CancellationToken ct = default);

    /// <summary>
    /// Rotiert den DEK für ein Projekt
    /// </summary>
    Task RotateProjectKeyAsync(string projectId, CancellationToken ct = default);
}

/// <summary>
/// Verschlüsselte Daten mit Metadaten
/// </summary>
public record EncryptedData
{
    public required byte[] Ciphertext { get; init; }
    public required byte[] Nonce { get; init; }
    public required byte[] Tag { get; init; }
    public required string KeyId { get; init; }
    public required int KeyVersion { get; init; }
}

/// <summary>
/// Gewrappter Schlüssel (DEK verschlüsselt mit KEK)
/// </summary>
public record WrappedKey
{
    public required string KeyId { get; init; }
    public required int Version { get; init; }
    public required byte[] WrappedKeyMaterial { get; init; }
    public required string WrappedByKeyId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Lokale Entwicklungs-Implementierung (NICHT für Produktion!)
/// </summary>
public class LocalDevEncryptionService : IEncryptionService
{
    private readonly Dictionary<string, byte[]> _projectKeys = new();
    private readonly byte[] _masterKey;

    public LocalDevEncryptionService()
    {
        // Für lokale Entwicklung: statischer Master Key (NICHT für Produktion!)
        _masterKey = new byte[32];
        RandomNumberGenerator.Fill(_masterKey);
    }

    public Task<EncryptedData> EncryptAsync(byte[] plaintext, string projectId, CancellationToken ct = default)
    {
        var key = GetOrCreateProjectKey(projectId);

        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);

        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return Task.FromResult(new EncryptedData
        {
            Ciphertext = ciphertext,
            Nonce = nonce,
            Tag = tag,
            KeyId = projectId,
            KeyVersion = 1
        });
    }

    public Task<byte[]> DecryptAsync(EncryptedData encrypted, string projectId, CancellationToken ct = default)
    {
        var key = GetOrCreateProjectKey(projectId);

        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);

        var plaintext = new byte[encrypted.Ciphertext.Length];
        aes.Decrypt(encrypted.Nonce, encrypted.Ciphertext, encrypted.Tag, plaintext);

        return Task.FromResult(plaintext);
    }

    public Task<WrappedKey> GenerateProjectKeyAsync(string projectId, string userId, CancellationToken ct = default)
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        _projectKeys[projectId] = key;

        // In lokaler Entwicklung: Key wird "gewrappt" indem er mit Master Key XOR'd wird
        var wrapped = new byte[32];
        for (int i = 0; i < 32; i++)
        {
            wrapped[i] = (byte)(key[i] ^ _masterKey[i % _masterKey.Length]);
        }

        return Task.FromResult(new WrappedKey
        {
            KeyId = projectId,
            Version = 1,
            WrappedKeyMaterial = wrapped,
            WrappedByKeyId = "master-local",
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    public Task RotateProjectKeyAsync(string projectId, CancellationToken ct = default)
    {
        // Für lokale Entwicklung: einfach neuen Key generieren
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        _projectKeys[projectId] = key;
        return Task.CompletedTask;
    }

    private byte[] GetOrCreateProjectKey(string projectId)
    {
        if (!_projectKeys.TryGetValue(projectId, out var key))
        {
            key = new byte[32];
            RandomNumberGenerator.Fill(key);
            _projectKeys[projectId] = key;
        }
        return key;
    }
}
