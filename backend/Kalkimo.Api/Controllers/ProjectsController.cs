using System.Security.Claims;
using System.Text.Json;
using Kalkimo.Api.Infrastructure;
using Kalkimo.Api.Mapping;
using Kalkimo.Api.Models;
using Kalkimo.Api.Services;
using Kalkimo.Domain.Calculators;
using Kalkimo.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kalkimo.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectStore _store;
    private readonly ILogger<ProjectsController> _logger;
    private readonly CalculationOrchestrator _calculator;
    private readonly JsonPatchApplier _patchApplier;

    public ProjectsController(
        IProjectStore store,
        ILogger<ProjectsController> logger,
        CalculationOrchestrator calculator,
        JsonPatchApplier patchApplier)
    {
        _store = store;
        _logger = logger;
        _calculator = calculator;
        _patchApplier = patchApplier;
    }

    /// <summary>
    /// Liste aller Projekte des Benutzers
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectSummary>>> GetProjects(CancellationToken ct)
    {
        var userId = GetUserId();
        var projects = await _store.ListProjectsAsync(userId, ct);

        var summaries = projects.Select(p => new ProjectSummary
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Version = p.Version,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Status = "Active"
        }).ToList();

        return Ok(summaries);
    }

    /// <summary>
    /// Neues Projekt erstellen
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProjectSummary>> CreateProject(
        [FromBody] CreateProjectRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var projectId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;

        // Minimales Projekt mit Pflichtfeldern erstellen
        var project = new Project
        {
            Id = projectId,
            Name = request.Name,
            Description = request.Description,
            Currency = request.Currency,
            StartPeriod = request.StartPeriod,
            EndPeriod = request.EndPeriod,
            Version = 1,
            SchemaVersion = "1.0",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = userId,
            UpdatedBy = userId,
            Property = CreateDefaultProperty(),
            Purchase = CreateDefaultPurchase(request.StartPeriod),
            Financing = CreateDefaultFinancing(userId, request.StartPeriod),
            Rent = CreateDefaultRent(request.StartPeriod),
            Costs = CreateDefaultCosts(),
            TaxProfile = CreateDefaultTaxProfile()
        };

        await _store.SaveSnapshotAsync(projectId, project, ct);

        _logger.LogInformation("Project created: {ProjectId} by {UserId}", projectId, userId);

        return CreatedAtAction(nameof(GetProject), new { id = projectId }, new ProjectSummary
        {
            Id = projectId,
            Name = project.Name,
            Description = project.Description,
            Version = project.Version,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            Status = "Active"
        });
    }

    /// <summary>
    /// Projekt-Snapshot abrufen
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Project>> GetProject(string id, CancellationToken ct)
    {
        try
        {
            var project = await GetAuthorizedProjectAsync(id, ct);

            if (project == null)
            {
                return NotFound(new { error = "Project not found" });
            }

            return Ok(project);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Changeset anwenden
    /// </summary>
    [HttpPost("{id}/events")]
    public async Task<ActionResult<ChangesetResponse>> ApplyChangeset(
        string id,
        [FromBody] ChangesetRequest request,
        CancellationToken ct)
    {
        Project project;
        try
        {
            var authorizedProject = await GetAuthorizedProjectAsync(id, ct);
            if (authorizedProject == null)
            {
                return NotFound(new { error = "Project not found" });
            }
            project = authorizedProject;
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        var userId = GetUserId();
        var changeSetId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        var newVersion = request.BaseVersion + 1;

        // Apply JSON Patch operations
        Project patchedProject;
        IReadOnlyList<JsonPatchOperation> operationsWithOldValues;
        try
        {
            (patchedProject, operationsWithOldValues) = _patchApplier.ApplyPatch(project, request.Operations);
        }
        catch (JsonPatchException ex)
        {
            _logger.LogWarning("JSON Patch failed for {ProjectId}: {Error}", id, ex.Message);
            return BadRequest(new { error = "Invalid patch operation", message = ex.Message });
        }

        // Update metadata
        var updatedProject = patchedProject with
        {
            Version = newVersion,
            UpdatedAt = now,
            UpdatedBy = userId
        };

        // Atomic version check and update
        var (success, actualVersion) = await _store.TryUpdateWithVersionCheckAsync(
            id, request.BaseVersion, updatedProject, ct);

        if (!success)
        {
            return Conflict(new ChangesetResponse
            {
                ChangeSetId = "",
                NewVersion = actualVersion,
                Timestamp = DateTimeOffset.UtcNow,
                HasConflict = true,
                Conflict = new ConflictInfo
                {
                    ServerVersion = actualVersion,
                    ConflictingPaths = request.Operations.Select(o => o.Path).ToList()
                }
            });
        }

        // Save event with old values for undo
        var evt = new ProjectEvent
        {
            ChangeSetId = changeSetId,
            ProjectId = id,
            UserId = userId,
            Timestamp = now,
            BaseVersion = request.BaseVersion,
            ResultVersion = newVersion,
            Operations = operationsWithOldValues,
            Description = request.Description
        };

        await _store.AppendEventAsync(id, evt, ct);

        _logger.LogInformation("Changeset applied: {ChangeSetId} to {ProjectId} ({OpCount} operations)",
            changeSetId, id, request.Operations.Count);

        return Ok(new ChangesetResponse
        {
            ChangeSetId = changeSetId,
            NewVersion = newVersion,
            Timestamp = now,
            HasConflict = false
        });
    }

    /// <summary>
    /// Events seit Version abrufen (für Sync)
    /// </summary>
    [HttpGet("{id}/events")]
    public async Task<ActionResult<IReadOnlyList<ProjectEvent>>> GetEvents(
        string id,
        [FromQuery] int since = 0,
        CancellationToken ct = default)
    {
        try
        {
            var project = await GetAuthorizedProjectAsync(id, ct);
            if (project == null)
            {
                return NotFound(new { error = "Project not found" });
            }
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        var events = await _store.LoadEventsAsync(id, since, ct);
        return Ok(events);
    }

    /// <summary>
    /// Berechnung ausführen
    /// </summary>
    [HttpPost("{id}/calculate")]
    public async Task<ActionResult<CalculationResponseDto>> Calculate(
        string id,
        [FromBody] CalculateRequest? request,
        CancellationToken ct)
    {
        Project project;
        try
        {
            var authorizedProject = await GetAuthorizedProjectAsync(id, ct);
            if (authorizedProject == null)
            {
                return NotFound(new { error = "Project not found" });
            }
            project = authorizedProject;
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        _logger.LogInformation("Calculation requested for {ProjectId}, scenario: {ScenarioId}",
            id, request?.ScenarioId ?? "base");

        try
        {
            var result = _calculator.Calculate(project, request?.ScenarioId);

            _logger.LogInformation(
                "Calculation completed for {ProjectId}: IRR={IrrPercent}%, NPV={Npv}",
                id,
                result.Metrics.IrrAfterTaxPercent,
                result.Metrics.NpvAfterTax);

            var response = CalculationResultMapper.MapToDto(result);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Calculation failed for {ProjectId}", id);
            return StatusCode(500, new
            {
                error = "Calculation failed",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Frontend-Projektdaten speichern (Raw JSON, unabhängig vom Domain-Model)
    /// </summary>
    [HttpPut("{id}/data")]
    public async Task<IActionResult> SaveProjectData(string id, CancellationToken ct)
    {
        var userId = GetUserId();

        // Check ownership if project already exists
        var exists = await _store.ExistsAsync(id, ct) || await _store.IsOwnedByAsync(id, userId, ct);
        if (exists)
        {
            var isOwner = await _store.IsOwnedByAsync(id, userId, ct);
            if (!isOwner)
            {
                _logger.LogWarning("User {UserId} tried to save data for project {ProjectId} they don't own", userId, id);
                return Forbid();
            }
        }

        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, ct);
        var rawJson = ms.ToArray();

        if (rawJson.Length == 0)
        {
            return BadRequest(new { error = "Request body is empty" });
        }

        await _store.SaveProjectDataAsync(id, userId, rawJson, ct);

        _logger.LogInformation("Project data saved: {ProjectId} by {UserId}", id, userId);

        return Ok(new { id });
    }

    /// <summary>
    /// Frontend-Projektdaten laden (Raw JSON)
    /// </summary>
    [HttpGet("{id}/data")]
    public async Task<IActionResult> GetProjectData(string id, CancellationToken ct)
    {
        var userId = GetUserId();

        // Check ownership via metadata
        var isOwner = await _store.IsOwnedByAsync(id, userId, ct);
        if (!isOwner)
        {
            return NotFound(new { error = "Project not found" });
        }

        var rawJson = await _store.LoadProjectDataAsync(id, ct);
        if (rawJson == null)
        {
            return NotFound(new { error = "Project data not found" });
        }

        return new ContentResult
        {
            Content = System.Text.Encoding.UTF8.GetString(rawJson),
            ContentType = "application/json",
            StatusCode = 200
        };
    }

    /// <summary>
    /// Projekt löschen
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(string id, CancellationToken ct)
    {
        var userId = GetUserId();

        // Check ownership via metadata (works for both domain and frontend-only projects)
        var isOwner = await _store.IsOwnedByAsync(id, userId, ct);
        if (!isOwner)
        {
            // Fallback: check via domain model for legacy projects
            try
            {
                var project = await GetAuthorizedProjectAsync(id, ct);
                if (project == null)
                {
                    return NotFound(new { error = "Project not found" });
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        await _store.DeleteProjectAsync(id, ct);

        _logger.LogInformation("Project deleted: {ProjectId} by {UserId}", id, userId);

        return NoContent();
    }

    private string? TryGetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private string GetUserId()
    {
        var userId = TryGetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userId;
    }

    /// <summary>
    /// Checks if the current user has access to the project.
    /// Returns the project if authorized, null if not found, or throws UnauthorizedAccessException.
    /// </summary>
    private async Task<Project?> GetAuthorizedProjectAsync(string projectId, CancellationToken ct)
    {
        var userId = GetUserId();
        var project = await _store.LoadSnapshotAsync(projectId, ct);

        if (project == null)
        {
            return null;
        }

        // Check if user is the owner
        if (project.CreatedBy != userId)
        {
            _logger.LogWarning(
                "Unauthorized access attempt: User {UserId} tried to access project {ProjectId} owned by {OwnerId}",
                userId, projectId, project.CreatedBy);
            throw new UnauthorizedAccessException("Access denied to this project");
        }

        return project;
    }

    /// <summary>
    /// Checks if the current user has access to the project (for endpoints that only need existence check).
    /// </summary>
    private async Task<bool> IsAuthorizedForProjectAsync(string projectId, CancellationToken ct)
    {
        try
        {
            var project = await GetAuthorizedProjectAsync(projectId, ct);
            return project != null;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    // Default-Werte für neues Projekt
    private static Property CreateDefaultProperty() => new()
    {
        Id = "property-1",
        Type = PropertyType.MultiFamilyHome,
        ConstructionYear = 2000,
        OverallCondition = Condition.Good,
        TotalArea = 500m,
        LivingArea = 400m,
        UnitCount = 1
    };

    private static Purchase CreateDefaultPurchase(YearMonth startPeriod) => new()
    {
        PurchasePrice = Money.Euro(400_000m),
        LandValue = Money.Euro(80_000m),
        PurchaseDate = startPeriod.ToFirstDayOfMonth(),
        AcquisitionCosts = new[]
        {
            new AcquisitionCost { Type = AcquisitionCostType.TransferTax, Amount = Money.Euro(20_000m) },
            new AcquisitionCost { Type = AcquisitionCostType.Notary, Amount = Money.Euro(6_000m) }
        }
    };

    private static Financing CreateDefaultFinancing(string userId, YearMonth startPeriod) => new()
    {
        EquityContributions = new[]
        {
            new EquityContribution
            {
                InvestorId = userId,
                Amount = Money.Euro(100_000m),
                ContributionDate = startPeriod.ToFirstDayOfMonth()
            }
        },
        Loans = new[]
        {
            new Loan
            {
                Id = "loan-1",
                Name = "Hauptdarlehen",
                Type = LoanType.Annuity,
                Principal = Money.Euro(326_000m),
                DisbursementDate = startPeriod.ToFirstDayOfMonth(),
                InterestRatePercent = 3.5m,
                InitialRepaymentPercent = 2m,
                FixedInterestPeriodMonths = 120
            }
        }
    };

    private static RentConfiguration CreateDefaultRent(YearMonth startPeriod) => new()
    {
        Tenancies = new[]
        {
            new Tenancy
            {
                Id = "tenancy-1",
                StartDate = startPeriod.ToFirstDayOfMonth(),
                NetRent = Money.Euro(2_000m),
                DevelopmentModel = RentDevelopmentModel.Annual,
                AnnualIncreasePercent = 2m
            }
        },
        VacancyRatePercent = 3m
    };

    private static CostConfiguration CreateDefaultCosts() => new()
    {
        Items = new[]
        {
            new CostItem
            {
                Id = "cost-admin",
                Name = "Verwaltung",
                Classification = CostClassification.Administration,
                MonthlyAmount = Money.Euro(100m),
                IsTaxDeductible = true
            },
            new CostItem
            {
                Id = "cost-insurance",
                Name = "Versicherung",
                Classification = CostClassification.Insurance,
                MonthlyAmount = Money.Euro(80m),
                IsTaxDeductible = true
            }
        },
        ReserveAccount = new ReserveAccountConfig
        {
            InitialBalance = Money.Euro(10_000m),
            ContributionPerSqmPerYear = 10m
        }
    };

    private static TaxProfile CreateDefaultTaxProfile() => new()
    {
        OwnershipType = OwnershipType.PrivateIndividual,
        MarginalTaxRatePercent = 42m,
        SolidaritySurchargePercent = 5.5m,
        LossOffsetEnabled = true
    };
}
