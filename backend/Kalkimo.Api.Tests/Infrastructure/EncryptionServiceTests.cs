using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Kalkimo.Api.Infrastructure;
using Xunit;

namespace Kalkimo.Api.Tests.Infrastructure;

/// <summary>
/// Umfassende Tests für den Encryption Service
/// Testet: Encrypt, Decrypt, Key Generation, Key Rotation, Edge Cases
/// </summary>
public class EncryptionServiceTests
{
    private readonly LocalDevEncryptionService _service;

    public EncryptionServiceTests()
    {
        _service = new LocalDevEncryptionService();
    }

    #region Basic Encryption/Decryption

    [Fact]
    public async Task Encrypt_ThenDecrypt_ReturnsOriginalData()
    {
        // Arrange
        var projectId = "test-project-1";
        var originalData = Encoding.UTF8.GetBytes("Hello, World! Dies ist ein Test mit Umlauten: äöüß");

        // Act
        var encrypted = await _service.EncryptAsync(originalData, projectId);
        var decrypted = await _service.DecryptAsync(encrypted, projectId);

        // Assert
        decrypted.Should().BeEquivalentTo(originalData);
        Encoding.UTF8.GetString(decrypted).Should().Be("Hello, World! Dies ist ein Test mit Umlauten: äöüß");
    }

    [Fact]
    public async Task Encrypt_ProducesDifferentCiphertextForSameData()
    {
        // Arrange - Nonce sollte jedes Mal anders sein
        var projectId = "test-project-1";
        var data = Encoding.UTF8.GetBytes("Same data");

        // Act
        var encrypted1 = await _service.EncryptAsync(data, projectId);
        var encrypted2 = await _service.EncryptAsync(data, projectId);

        // Assert - Ciphertext sollte unterschiedlich sein (wegen unterschiedlicher Nonce)
        encrypted1.Ciphertext.Should().NotBeEquivalentTo(encrypted2.Ciphertext);
        encrypted1.Nonce.Should().NotBeEquivalentTo(encrypted2.Nonce);
    }

    [Fact]
    public async Task Encrypt_DifferentProjects_UseDifferentKeys()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Same data for both projects");

        // Act
        var encrypted1 = await _service.EncryptAsync(data, "project-1");
        var encrypted2 = await _service.EncryptAsync(data, "project-2");

        // Assert - KeyIds sollten unterschiedlich sein
        encrypted1.KeyId.Should().Be("project-1");
        encrypted2.KeyId.Should().Be("project-2");
    }

    [Fact]
    public async Task Decrypt_WithWrongProject_Fails()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Secret data");
        var encrypted = await _service.EncryptAsync(data, "project-1");

        // Act & Assert - Entschlüsselung mit anderem Projekt sollte fehlschlagen
        var act = () => _service.DecryptAsync(encrypted, "project-2");
        await act.Should().ThrowAsync<CryptographicException>();
    }

    #endregion

    #region Large Data Handling

    [Fact]
    public async Task Encrypt_LargeData_WorksCorrectly()
    {
        // Arrange - 1 MB Daten
        var projectId = "test-project";
        var largeData = new byte[1024 * 1024];
        RandomNumberGenerator.Fill(largeData);

        // Act
        var encrypted = await _service.EncryptAsync(largeData, projectId);
        var decrypted = await _service.DecryptAsync(encrypted, projectId);

        // Assert
        decrypted.Should().BeEquivalentTo(largeData);
    }

    [Fact]
    public async Task Encrypt_EmptyData_WorksCorrectly()
    {
        // Arrange
        var projectId = "test-project";
        var emptyData = Array.Empty<byte>();

        // Act
        var encrypted = await _service.EncryptAsync(emptyData, projectId);
        var decrypted = await _service.DecryptAsync(encrypted, projectId);

        // Assert
        decrypted.Should().BeEmpty();
    }

    [Fact]
    public async Task Encrypt_SingleByte_WorksCorrectly()
    {
        // Arrange
        var projectId = "test-project";
        var singleByte = new byte[] { 0x42 };

        // Act
        var encrypted = await _service.EncryptAsync(singleByte, projectId);
        var decrypted = await _service.DecryptAsync(encrypted, projectId);

        // Assert
        decrypted.Should().BeEquivalentTo(singleByte);
    }

    #endregion

    #region Key Generation

    [Fact]
    public async Task GenerateProjectKey_ReturnsValidWrappedKey()
    {
        // Arrange
        var projectId = "new-project";
        var userId = "user-1";

        // Act
        var wrappedKey = await _service.GenerateProjectKeyAsync(projectId, userId);

        // Assert
        wrappedKey.Should().NotBeNull();
        wrappedKey.KeyId.Should().Be(projectId);
        wrappedKey.Version.Should().Be(1);
        wrappedKey.WrappedKeyMaterial.Should().NotBeNullOrEmpty();
        wrappedKey.WrappedKeyMaterial.Should().HaveCount(32); // 256 bit key
        wrappedKey.WrappedByKeyId.Should().NotBeNullOrEmpty();
        wrappedKey.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GenerateProjectKey_DifferentProjects_DifferentKeys()
    {
        // Arrange & Act
        var key1 = await _service.GenerateProjectKeyAsync("project-1", "user-1");
        var key2 = await _service.GenerateProjectKeyAsync("project-2", "user-1");

        // Assert
        key1.WrappedKeyMaterial.Should().NotBeEquivalentTo(key2.WrappedKeyMaterial);
    }

    [Fact]
    public async Task GenerateProjectKey_ThenEncryptDecrypt_Works()
    {
        // Arrange
        var projectId = "generated-key-project";
        var userId = "user-1";
        var data = Encoding.UTF8.GetBytes("Test data with generated key");

        // Act
        await _service.GenerateProjectKeyAsync(projectId, userId);
        var encrypted = await _service.EncryptAsync(data, projectId);
        var decrypted = await _service.DecryptAsync(encrypted, projectId);

        // Assert
        decrypted.Should().BeEquivalentTo(data);
    }

    #endregion

    #region Key Rotation

    [Fact]
    public async Task RotateProjectKey_ChangesKey()
    {
        // Arrange
        var projectId = "rotation-test-project";
        var data = Encoding.UTF8.GetBytes("Data before rotation");

        // Encrypt with original key
        var encryptedBefore = await _service.EncryptAsync(data, projectId);

        // Act - Rotate key
        await _service.RotateProjectKeyAsync(projectId);

        // Assert - Old ciphertext cannot be decrypted with new key
        var act = () => _service.DecryptAsync(encryptedBefore, projectId);
        await act.Should().ThrowAsync<CryptographicException>();
    }

    [Fact]
    public async Task RotateProjectKey_NewDataCanBeEncryptedAndDecrypted()
    {
        // Arrange
        var projectId = "rotation-test-project-2";
        await _service.GenerateProjectKeyAsync(projectId, "user-1");

        // Act - Rotate key
        await _service.RotateProjectKeyAsync(projectId);

        // Encrypt new data with rotated key
        var newData = Encoding.UTF8.GetBytes("Data after rotation");
        var encrypted = await _service.EncryptAsync(newData, projectId);
        var decrypted = await _service.DecryptAsync(encrypted, projectId);

        // Assert
        decrypted.Should().BeEquivalentTo(newData);
    }

    #endregion

    #region Tamper Detection (AEAD)

    [Fact]
    public async Task Decrypt_TamperedCiphertext_Fails()
    {
        // Arrange
        var projectId = "tamper-test";
        var data = Encoding.UTF8.GetBytes("Original data");
        var encrypted = await _service.EncryptAsync(data, projectId);

        // Act - Tamper with ciphertext
        var tamperedCiphertext = (byte[])encrypted.Ciphertext.Clone();
        tamperedCiphertext[0] ^= 0xFF; // Flip bits

        var tamperedData = new EncryptedData
        {
            Ciphertext = tamperedCiphertext,
            Nonce = encrypted.Nonce,
            Tag = encrypted.Tag,
            KeyId = encrypted.KeyId,
            KeyVersion = encrypted.KeyVersion
        };

        // Assert
        var act = () => _service.DecryptAsync(tamperedData, projectId);
        await act.Should().ThrowAsync<CryptographicException>();
    }

    [Fact]
    public async Task Decrypt_TamperedTag_Fails()
    {
        // Arrange
        var projectId = "tamper-test-tag";
        var data = Encoding.UTF8.GetBytes("Original data");
        var encrypted = await _service.EncryptAsync(data, projectId);

        // Act - Tamper with authentication tag
        var tamperedTag = (byte[])encrypted.Tag.Clone();
        tamperedTag[0] ^= 0xFF;

        var tamperedData = new EncryptedData
        {
            Ciphertext = encrypted.Ciphertext,
            Nonce = encrypted.Nonce,
            Tag = tamperedTag,
            KeyId = encrypted.KeyId,
            KeyVersion = encrypted.KeyVersion
        };

        // Assert
        var act = () => _service.DecryptAsync(tamperedData, projectId);
        await act.Should().ThrowAsync<CryptographicException>();
    }

    [Fact]
    public async Task Decrypt_TamperedNonce_Fails()
    {
        // Arrange
        var projectId = "tamper-test-nonce";
        var data = Encoding.UTF8.GetBytes("Original data");
        var encrypted = await _service.EncryptAsync(data, projectId);

        // Act - Tamper with nonce
        var tamperedNonce = (byte[])encrypted.Nonce.Clone();
        tamperedNonce[0] ^= 0xFF;

        var tamperedData = new EncryptedData
        {
            Ciphertext = encrypted.Ciphertext,
            Nonce = tamperedNonce,
            Tag = encrypted.Tag,
            KeyId = encrypted.KeyId,
            KeyVersion = encrypted.KeyVersion
        };

        // Assert
        var act = () => _service.DecryptAsync(tamperedData, projectId);
        await act.Should().ThrowAsync<CryptographicException>();
    }

    #endregion

    #region Nonce Uniqueness

    [Fact]
    public async Task Encrypt_MultipleOperations_UniqueNonces()
    {
        // Arrange
        var projectId = "nonce-test";
        var data = Encoding.UTF8.GetBytes("Test data");
        var nonces = new HashSet<string>();

        // Act - Encrypt 100 times
        for (int i = 0; i < 100; i++)
        {
            var encrypted = await _service.EncryptAsync(data, projectId);
            var nonceHex = Convert.ToHexString(encrypted.Nonce);
            nonces.Add(nonceHex);
        }

        // Assert - All nonces should be unique
        nonces.Should().HaveCount(100);
    }

    #endregion

    #region Metadata Consistency

    [Fact]
    public async Task EncryptedData_HasCorrectMetadata()
    {
        // Arrange
        var projectId = "metadata-test";
        var data = Encoding.UTF8.GetBytes("Test");

        // Act
        var encrypted = await _service.EncryptAsync(data, projectId);

        // Assert
        encrypted.KeyId.Should().Be(projectId);
        encrypted.KeyVersion.Should().BeGreaterThan(0);
        encrypted.Nonce.Should().HaveCount(12); // AES-GCM standard nonce size
        encrypted.Tag.Should().HaveCount(16); // AES-GCM tag size
        encrypted.Ciphertext.Should().HaveCount(data.Length);
    }

    #endregion

    #region Concurrent Access

    [Fact]
    public async Task Encrypt_ConcurrentOperations_AllSucceed()
    {
        // Arrange
        var projectId = "concurrent-test";
        var tasks = new List<Task<EncryptedData>>();

        // Act - 50 concurrent encryptions
        for (int i = 0; i < 50; i++)
        {
            var data = Encoding.UTF8.GetBytes($"Data {i}");
            tasks.Add(_service.EncryptAsync(data, projectId));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(50);
        results.All(r => r.Ciphertext != null).Should().BeTrue();
    }

    [Fact]
    public async Task EncryptDecrypt_ConcurrentOperations_AllSucceed()
    {
        // Arrange
        var projectId = "concurrent-decrypt-test";
        var tasks = new List<Task>();

        // Act - 50 concurrent encrypt/decrypt cycles
        for (int i = 0; i < 50; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var data = Encoding.UTF8.GetBytes($"Concurrent data {index}");
                var encrypted = await _service.EncryptAsync(data, projectId);
                var decrypted = await _service.DecryptAsync(encrypted, projectId);
                decrypted.Should().BeEquivalentTo(data);
            }));
        }

        // Assert - All should complete without exception
        await Task.WhenAll(tasks);
    }

    #endregion

    #region Binary Data

    [Fact]
    public async Task Encrypt_BinaryData_WithAllByteValues_WorksCorrectly()
    {
        // Arrange - Data with all possible byte values
        var projectId = "binary-test";
        var data = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            data[i] = (byte)i;
        }

        // Act
        var encrypted = await _service.EncryptAsync(data, projectId);
        var decrypted = await _service.DecryptAsync(encrypted, projectId);

        // Assert
        decrypted.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task Encrypt_JsonData_PreservesContent()
    {
        // Arrange
        var projectId = "json-test";
        var json = """
        {
            "name": "Test Project",
            "value": 12345.67,
            "unicode": "Grüße aus Köln! 日本語",
            "nested": {
                "array": [1, 2, 3]
            }
        }
        """;
        var data = Encoding.UTF8.GetBytes(json);

        // Act
        var encrypted = await _service.EncryptAsync(data, projectId);
        var decrypted = await _service.DecryptAsync(encrypted, projectId);

        // Assert
        Encoding.UTF8.GetString(decrypted).Should().Be(json);
    }

    #endregion
}
