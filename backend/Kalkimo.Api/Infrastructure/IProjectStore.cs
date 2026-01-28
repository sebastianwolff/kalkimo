using System.Collections.Concurrent;
using System.Text.Json;
using Kalkimo.Domain.Models;

namespace Kalkimo.Api.Infrastructure;

/// <summary>
/// Interface für den verschlüsselten Flatfile-Speicher
/// </summary>
public interface IProjectStore
{
    /// <summary>
    /// Speichert einen Projekt-Snapshot
    /// </summary>
    Task SaveSnapshotAsync(string projectId, Project project, CancellationToken ct = default);

    /// <summary>
    /// Lädt den aktuellen Projekt-Snapshot
    /// </summary>
    Task<Project?> LoadSnapshotAsync(string projectId, CancellationToken ct = default);

    /// <summary>
    /// Fügt ein Event zum Eventlog hinzu (append-only)
    /// </summary>
    Task AppendEventAsync(string projectId, ProjectEvent evt, CancellationToken ct = default);

    /// <summary>
    /// Lädt Events seit einer bestimmten Version
    /// </summary>
    Task<IReadOnlyList<ProjectEvent>> LoadEventsAsync(string projectId, int sinceVersion, CancellationToken ct = default);

    /// <summary>
    /// Listet alle Projekte eines Benutzers
    /// </summary>
    Task<IReadOnlyList<ProjectMetadata>> ListProjectsAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Löscht ein Projekt (soft delete)
    /// </summary>
    Task DeleteProjectAsync(string projectId, CancellationToken ct = default);

    /// <summary>
    /// Prüft ob ein Projekt existiert
    /// </summary>
    Task<bool> ExistsAsync(string projectId, CancellationToken ct = default);

    /// <summary>
    /// Atomically check version and apply update. Returns (success, currentVersion).
    /// If expectedVersion matches, saves the project and returns (true, newVersion).
    /// If not, returns (false, actualVersion).
    /// </summary>
    Task<(bool Success, int ActualVersion)> TryUpdateWithVersionCheckAsync(
        string projectId,
        int expectedVersion,
        Project updatedProject,
        CancellationToken ct = default);
}

/// <summary>
/// Event/Changeset für das Eventlog
/// </summary>
public record ProjectEvent
{
    public required string ChangeSetId { get; init; }
    public required string ProjectId { get; init; }
    public required string UserId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required int BaseVersion { get; init; }
    public required int ResultVersion { get; init; }
    public required IReadOnlyList<JsonPatchOperation> Operations { get; init; }
    public string? Description { get; init; }
    public bool IsUndo { get; init; }
    public string? UndoOfChangeSetId { get; init; }
}

/// <summary>
/// JSON Patch Operation
/// </summary>
public record JsonPatchOperation
{
    public required string Op { get; init; }
    public required string Path { get; init; }
    public JsonElement? Value { get; init; }
    public string? From { get; init; }
    public JsonElement? OldValue { get; init; }
}

/// <summary>
/// Projekt-Metadaten (für Listing)
/// </summary>
public record ProjectMetadata
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int Version { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required string OwnerId { get; init; }
}

/// <summary>
/// Flatfile-Implementierung des Project Store
/// </summary>
public class FlatfileProjectStore : IProjectStore
{
    private readonly string _dataRoot;
    private readonly IEncryptionService _encryption;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _projectLocks = new();

    public FlatfileProjectStore(string dataRoot, IEncryptionService encryption)
    {
        _dataRoot = dataRoot;
        _encryption = encryption;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    private SemaphoreSlim GetProjectLock(string projectId)
    {
        return _projectLocks.GetOrAdd(projectId, _ => new SemaphoreSlim(1, 1));
    }

    public async Task SaveSnapshotAsync(string projectId, Project project, CancellationToken ct = default)
    {
        var projectDir = GetProjectDirectory(projectId);
        Directory.CreateDirectory(projectDir);

        var json = JsonSerializer.SerializeToUtf8Bytes(project, _jsonOptions);
        var encrypted = await _encryption.EncryptAsync(json, projectId, ct);

        var snapshotPath = Path.Combine(projectDir, $"snapshot_{project.Version}.json.enc");
        await WriteEncryptedFileAsync(snapshotPath, encrypted, ct);

        // Aktualisiere Metadaten
        await SaveMetadataAsync(projectId, new ProjectMetadata
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Version = project.Version,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            OwnerId = project.CreatedBy ?? "unknown"
        }, ct);
    }

    public async Task<Project?> LoadSnapshotAsync(string projectId, CancellationToken ct = default)
    {
        var projectDir = GetProjectDirectory(projectId);
        if (!Directory.Exists(projectDir))
            return null;

        // Finde neuesten Snapshot
        var snapshots = Directory.GetFiles(projectDir, "snapshot_*.json.enc")
            .OrderByDescending(f => f)
            .FirstOrDefault();

        if (snapshots == null)
            return null;

        var encrypted = await ReadEncryptedFileAsync(snapshots, ct);
        var json = await _encryption.DecryptAsync(encrypted, projectId, ct);

        return JsonSerializer.Deserialize<Project>(json, _jsonOptions);
    }

    public async Task AppendEventAsync(string projectId, ProjectEvent evt, CancellationToken ct = default)
    {
        var projectLock = GetProjectLock(projectId);
        await projectLock.WaitAsync(ct);
        try
        {
            var projectDir = GetProjectDirectory(projectId);
            Directory.CreateDirectory(projectDir);

            var yearMonth = evt.Timestamp.ToString("yyyyMM");
            var eventLogPath = Path.Combine(projectDir, $"events_{yearMonth}.jsonl.enc");

            var eventJson = JsonSerializer.SerializeToUtf8Bytes(evt, _jsonOptions);
            var encrypted = await _encryption.EncryptAsync(eventJson, projectId, ct);

            // Append to JSONL file
            var line = Convert.ToBase64String(SerializeEncryptedData(encrypted)) + "\n";
            await File.AppendAllTextAsync(eventLogPath, line, ct);
        }
        finally
        {
            projectLock.Release();
        }
    }

    public async Task<IReadOnlyList<ProjectEvent>> LoadEventsAsync(string projectId, int sinceVersion, CancellationToken ct = default)
    {
        var projectDir = GetProjectDirectory(projectId);
        if (!Directory.Exists(projectDir))
            return Array.Empty<ProjectEvent>();

        var events = new List<ProjectEvent>();
        var eventFiles = Directory.GetFiles(projectDir, "events_*.jsonl.enc").OrderBy(f => f);

        foreach (var file in eventFiles)
        {
            var lines = await File.ReadAllLinesAsync(file, ct);
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var encryptedBytes = Convert.FromBase64String(line);
                var encrypted = DeserializeEncryptedData(encryptedBytes);
                var json = await _encryption.DecryptAsync(encrypted, projectId, ct);
                var evt = JsonSerializer.Deserialize<ProjectEvent>(json, _jsonOptions);

                if (evt != null && evt.BaseVersion >= sinceVersion)
                {
                    events.Add(evt);
                }
            }
        }

        return events.OrderBy(e => e.ResultVersion).ToList();
    }

    public async Task<IReadOnlyList<ProjectMetadata>> ListProjectsAsync(string userId, CancellationToken ct = default)
    {
        ValidatePathSegment(userId, nameof(userId));
        var userDir = Path.Combine(_dataRoot, "users", userId, "projects");
        ValidatePathWithinRoot(userDir);

        if (!Directory.Exists(userDir))
            return Array.Empty<ProjectMetadata>();

        var result = new List<ProjectMetadata>();
        var metaFiles = Directory.GetFiles(userDir, "*.meta.json");

        foreach (var metaFile in metaFiles)
        {
            var json = await File.ReadAllTextAsync(metaFile, ct);
            var meta = JsonSerializer.Deserialize<ProjectMetadata>(json, _jsonOptions);
            if (meta != null)
                result.Add(meta);
        }

        return result;
    }

    public async Task DeleteProjectAsync(string projectId, CancellationToken ct = default)
    {
        // First, load the project to get the owner ID for metadata cleanup
        var project = await LoadSnapshotAsync(projectId, ct);

        var projectDir = GetProjectDirectory(projectId);
        if (Directory.Exists(projectDir))
        {
            // Soft delete: rename to .deleted
            var deletedDir = projectDir + ".deleted." + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Directory.Move(projectDir, deletedDir);
        }

        // Clean up metadata file
        if (project != null && !string.IsNullOrEmpty(project.CreatedBy))
        {
            ValidatePathSegment(project.CreatedBy, "createdBy");
            ValidatePathSegment(projectId, nameof(projectId));
            var metaPath = Path.Combine(_dataRoot, "users", project.CreatedBy, "projects", $"{projectId}.meta.json");
            ValidatePathWithinRoot(metaPath);
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
        }
    }

    public Task<bool> ExistsAsync(string projectId, CancellationToken ct = default)
    {
        var projectDir = GetProjectDirectory(projectId);
        return Task.FromResult(Directory.Exists(projectDir));
    }

    public async Task<(bool Success, int ActualVersion)> TryUpdateWithVersionCheckAsync(
        string projectId,
        int expectedVersion,
        Project updatedProject,
        CancellationToken ct = default)
    {
        var projectLock = GetProjectLock(projectId);
        await projectLock.WaitAsync(ct);
        try
        {
            // Load current project to check version
            var current = await LoadSnapshotAsync(projectId, ct);
            if (current == null)
            {
                return (false, 0);
            }

            // Check version
            if (current.Version != expectedVersion)
            {
                return (false, current.Version);
            }

            // Version matches, save the update
            await SaveSnapshotAsync(projectId, updatedProject, ct);
            return (true, updatedProject.Version);
        }
        finally
        {
            projectLock.Release();
        }
    }

    private string GetProjectDirectory(string projectId)
    {
        ValidatePathSegment(projectId, nameof(projectId));
        var path = Path.Combine(_dataRoot, "projects", projectId);
        ValidatePathWithinRoot(path);
        return path;
    }

    private async Task SaveMetadataAsync(string projectId, ProjectMetadata meta, CancellationToken ct)
    {
        ValidatePathSegment(projectId, nameof(projectId));
        ValidatePathSegment(meta.OwnerId, "ownerId");

        var userDir = Path.Combine(_dataRoot, "users", meta.OwnerId, "projects");
        ValidatePathWithinRoot(userDir);
        Directory.CreateDirectory(userDir);

        var metaPath = Path.Combine(userDir, $"{projectId}.meta.json");
        ValidatePathWithinRoot(metaPath);
        var json = JsonSerializer.Serialize(meta, _jsonOptions);
        await File.WriteAllTextAsync(metaPath, json, ct);
    }

    /// <summary>
    /// Validates that a path segment doesn't contain path traversal characters
    /// </summary>
    private static void ValidatePathSegment(string segment, string paramName)
    {
        if (string.IsNullOrWhiteSpace(segment))
        {
            throw new ArgumentException("Path segment cannot be empty", paramName);
        }

        // Check for path traversal attempts
        if (segment.Contains("..") ||
            segment.Contains('/') ||
            segment.Contains('\\') ||
            segment.Contains(Path.DirectorySeparatorChar) ||
            segment.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException($"Invalid characters in {paramName}: path traversal not allowed", paramName);
        }

        // Check for invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        if (segment.IndexOfAny(invalidChars) >= 0)
        {
            throw new ArgumentException($"Invalid characters in {paramName}", paramName);
        }
    }

    /// <summary>
    /// Validates that the resulting path is within the data root
    /// </summary>
    private void ValidatePathWithinRoot(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var fullRoot = Path.GetFullPath(_dataRoot);

        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Path traversal detected: {path} is outside data root");
        }
    }

    private async Task WriteEncryptedFileAsync(string path, EncryptedData data, CancellationToken ct)
    {
        var bytes = SerializeEncryptedData(data);
        await File.WriteAllBytesAsync(path, bytes, ct);
    }

    private async Task<EncryptedData> ReadEncryptedFileAsync(string path, CancellationToken ct)
    {
        var bytes = await File.ReadAllBytesAsync(path, ct);
        return DeserializeEncryptedData(bytes);
    }

    private static byte[] SerializeEncryptedData(EncryptedData data)
    {
        // Format: [4 bytes keyId length][keyId][4 bytes keyVersion][nonce][tag][ciphertext]
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
