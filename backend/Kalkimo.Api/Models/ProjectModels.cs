using Kalkimo.Domain.Models;

namespace Kalkimo.Api.Models;

/// <summary>
/// Projekt-Erstellung
/// </summary>
public record CreateProjectRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required YearMonth StartPeriod { get; init; }
    public required YearMonth EndPeriod { get; init; }
    public string Currency { get; init; } = "EUR";
}

/// <summary>
/// Projekt-Zusammenfassung (für Listen)
/// </summary>
public record ProjectSummary
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int Version { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required string Status { get; init; }
}

/// <summary>
/// Changeset-Anfrage
/// </summary>
public record ChangesetRequest
{
    public required int BaseVersion { get; init; }
    public required IReadOnlyList<PatchOperation> Operations { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Patch-Operation
/// </summary>
public record PatchOperation
{
    public required string Op { get; init; }
    public required string Path { get; init; }
    public object? Value { get; init; }
    public string? From { get; init; }
}

/// <summary>
/// Changeset-Antwort
/// </summary>
public record ChangesetResponse
{
    public required string ChangeSetId { get; init; }
    public required int NewVersion { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public bool HasConflict { get; init; }
    public ConflictInfo? Conflict { get; init; }
}

/// <summary>
/// Konflikt-Information
/// </summary>
public record ConflictInfo
{
    public required int ServerVersion { get; init; }
    public required IReadOnlyList<string> ConflictingPaths { get; init; }
}

/// <summary>
/// Berechnungs-Anfrage
/// </summary>
public record CalculateRequest
{
    public string? ScenarioId { get; init; }
}

/// <summary>
/// Projekt-Freigabe
/// </summary>
public record ShareRequest
{
    public required string Email { get; init; }
    public required string Role { get; init; } // Owner, Editor, Viewer
}
