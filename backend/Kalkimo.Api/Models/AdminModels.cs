using Kalkimo.Api.Infrastructure;

namespace Kalkimo.Api.Models;

/// <summary>
/// Benutzer-Zusammenfassung für Admin-Dashboard
/// </summary>
public record AdminUserSummary
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public int ProjectCount { get; init; }
}

/// <summary>
/// Benutzer-Detailansicht für Admin-Verwaltung
/// </summary>
public record AdminUserDetail
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public IReadOnlyList<ProjectMetadata> Projects { get; init; } = Array.Empty<ProjectMetadata>();
}
