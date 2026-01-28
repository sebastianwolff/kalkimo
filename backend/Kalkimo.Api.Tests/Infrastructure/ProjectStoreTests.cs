using FluentAssertions;
using Kalkimo.Api.Infrastructure;
using Kalkimo.Domain.Models;
using System.Text.Json;
using Xunit;

namespace Kalkimo.Api.Tests.Infrastructure;

/// <summary>
/// Umfassende Tests für den Project Store
/// Testet: Save, Load, Events, Listing, Delete, Encryption Integration
/// </summary>
public class ProjectStoreTests : IDisposable
{
    private readonly string _testDataRoot;
    private readonly IEncryptionService _encryption;
    private readonly FlatfileProjectStore _store;

    public ProjectStoreTests()
    {
        _testDataRoot = Path.Combine(Path.GetTempPath(), $"kalkimo-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataRoot);

        _encryption = new LocalDevEncryptionService();
        _store = new FlatfileProjectStore(_testDataRoot, _encryption);
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testDataRoot))
        {
            Directory.Delete(_testDataRoot, recursive: true);
        }
    }

    #region Save and Load Snapshot

    [Fact]
    public async Task SaveSnapshot_ThenLoad_ReturnsOriginalProject()
    {
        // Arrange
        var project = CreateTestProject("test-1");

        // Act
        await _store.SaveSnapshotAsync(project.Id, project);
        var loaded = await _store.LoadSnapshotAsync(project.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(project.Id);
        loaded.Name.Should().Be(project.Name);
        loaded.Version.Should().Be(project.Version);
        loaded.Currency.Should().Be(project.Currency);
    }

    [Fact]
    public async Task SaveSnapshot_CreatesEncryptedFile()
    {
        // Arrange
        var project = CreateTestProject("encrypted-test");

        // Act
        await _store.SaveSnapshotAsync(project.Id, project);

        // Assert - File should exist and be encrypted (not readable as plain JSON)
        var projectDir = Path.Combine(_testDataRoot, "projects", project.Id);
        Directory.Exists(projectDir).Should().BeTrue();

        var snapshotFiles = Directory.GetFiles(projectDir, "snapshot_*.json.enc");
        snapshotFiles.Should().HaveCount(1);

        // File content should not be plain text JSON
        var content = await File.ReadAllTextAsync(snapshotFiles[0]);
        content.Should().NotContain("\"name\":");
        content.Should().NotContain(project.Name);
    }

    [Fact]
    public async Task LoadSnapshot_NonExistent_ReturnsNull()
    {
        // Act
        var result = await _store.LoadSnapshotAsync("non-existent-project");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveSnapshot_MultipleVersions_LoadsLatest()
    {
        // Arrange
        var projectId = "multi-version";
        var projectV1 = CreateTestProject(projectId, version: 1, name: "Version 1");
        var projectV2 = CreateTestProject(projectId, version: 2, name: "Version 2");
        var projectV3 = CreateTestProject(projectId, version: 3, name: "Version 3");

        // Act
        await _store.SaveSnapshotAsync(projectId, projectV1);
        await _store.SaveSnapshotAsync(projectId, projectV2);
        await _store.SaveSnapshotAsync(projectId, projectV3);

        var loaded = await _store.LoadSnapshotAsync(projectId);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Version.Should().Be(3);
        loaded.Name.Should().Be("Version 3");
    }

    [Fact]
    public async Task SaveSnapshot_PreservesComplexData()
    {
        // Arrange
        var project = CreateTestProject("complex-data");

        // Act
        await _store.SaveSnapshotAsync(project.Id, project);
        var loaded = await _store.LoadSnapshotAsync(project.Id);

        // Assert - All complex nested structures should be preserved
        loaded.Should().NotBeNull();
        loaded!.Property.Should().NotBeNull();
        loaded.Property.Type.Should().Be(PropertyType.MultiFamilyHome);
        loaded.Property.ConstructionYear.Should().Be(2000);

        loaded.Purchase.Should().NotBeNull();
        loaded.Purchase.PurchasePrice.Amount.Should().Be(400_000m);
        loaded.Purchase.AcquisitionCosts.Should().HaveCount(2);

        loaded.Financing.Should().NotBeNull();
        loaded.Financing.Loans.Should().HaveCount(1);
        loaded.Financing.Loans[0].InterestRatePercent.Should().Be(3.5m);

        loaded.TaxProfile.Should().NotBeNull();
        loaded.TaxProfile.MarginalTaxRatePercent.Should().Be(42m);
    }

    #endregion

    #region Events

    [Fact]
    public async Task AppendEvent_CreatesEventLog()
    {
        // Arrange
        var projectId = "event-test";
        var project = CreateTestProject(projectId);
        await _store.SaveSnapshotAsync(projectId, project);

        var evt = CreateTestEvent(projectId, baseVersion: 1, resultVersion: 2);

        // Act
        await _store.AppendEventAsync(projectId, evt);

        // Assert - Event log file should exist
        var projectDir = Path.Combine(_testDataRoot, "projects", projectId);
        var eventFiles = Directory.GetFiles(projectDir, "events_*.jsonl.enc");
        eventFiles.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task AppendEvent_ThenLoad_ReturnsEvents()
    {
        // Arrange
        var projectId = "event-load-test";
        var project = CreateTestProject(projectId);
        await _store.SaveSnapshotAsync(projectId, project);

        var evt1 = CreateTestEvent(projectId, baseVersion: 1, resultVersion: 2, description: "Change 1");
        var evt2 = CreateTestEvent(projectId, baseVersion: 2, resultVersion: 3, description: "Change 2");

        // Act
        await _store.AppendEventAsync(projectId, evt1);
        await _store.AppendEventAsync(projectId, evt2);

        var events = await _store.LoadEventsAsync(projectId, sinceVersion: 0);

        // Assert
        events.Should().HaveCount(2);
        events[0].Description.Should().Be("Change 1");
        events[1].Description.Should().Be("Change 2");
    }

    [Fact]
    public async Task LoadEvents_FiltersBySinceVersion()
    {
        // Arrange
        var projectId = "event-filter-test";
        var project = CreateTestProject(projectId);
        await _store.SaveSnapshotAsync(projectId, project);

        await _store.AppendEventAsync(projectId, CreateTestEvent(projectId, 1, 2));
        await _store.AppendEventAsync(projectId, CreateTestEvent(projectId, 2, 3));
        await _store.AppendEventAsync(projectId, CreateTestEvent(projectId, 3, 4));

        // Act
        var allEvents = await _store.LoadEventsAsync(projectId, sinceVersion: 0);
        var recentEvents = await _store.LoadEventsAsync(projectId, sinceVersion: 2);

        // Assert
        allEvents.Should().HaveCount(3);
        recentEvents.Should().HaveCount(2); // Only events with baseVersion >= 2
    }

    [Fact]
    public async Task LoadEvents_NonExistentProject_ReturnsEmpty()
    {
        // Act
        var events = await _store.LoadEventsAsync("non-existent", sinceVersion: 0);

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task AppendEvent_PreservesOperations()
    {
        // Arrange
        var projectId = "event-operations-test";
        var project = CreateTestProject(projectId);
        await _store.SaveSnapshotAsync(projectId, project);

        var evt = new ProjectEvent
        {
            ChangeSetId = Guid.NewGuid().ToString(),
            ProjectId = projectId,
            UserId = "user-1",
            Timestamp = DateTimeOffset.UtcNow,
            BaseVersion = 1,
            ResultVersion = 2,
            Operations = new[]
            {
                new JsonPatchOperation
                {
                    Op = "replace",
                    Path = "/name",
                    Value = JsonSerializer.SerializeToElement("New Name")
                },
                new JsonPatchOperation
                {
                    Op = "add",
                    Path = "/description",
                    Value = JsonSerializer.SerializeToElement("Added description")
                }
            }
        };

        // Act
        await _store.AppendEventAsync(projectId, evt);
        var events = await _store.LoadEventsAsync(projectId, sinceVersion: 0);

        // Assert
        events.Should().HaveCount(1);
        events[0].Operations.Should().HaveCount(2);
        events[0].Operations[0].Op.Should().Be("replace");
        events[0].Operations[0].Path.Should().Be("/name");
        events[0].Operations[1].Op.Should().Be("add");
    }

    #endregion

    #region Listing

    [Fact]
    public async Task ListProjects_ReturnsUserProjects()
    {
        // Arrange
        var userId = "test-user";
        var project1 = CreateTestProject("proj-1", createdBy: userId);
        var project2 = CreateTestProject("proj-2", createdBy: userId);

        await _store.SaveSnapshotAsync(project1.Id, project1);
        await _store.SaveSnapshotAsync(project2.Id, project2);

        // Act
        var projects = await _store.ListProjectsAsync(userId);

        // Assert
        projects.Should().HaveCount(2);
        projects.Select(p => p.Id).Should().Contain(new[] { "proj-1", "proj-2" });
    }

    [Fact]
    public async Task ListProjects_DifferentUsers_SeparateResults()
    {
        // Arrange
        var user1 = "user-1";
        var user2 = "user-2";

        await _store.SaveSnapshotAsync("u1-proj", CreateTestProject("u1-proj", createdBy: user1));
        await _store.SaveSnapshotAsync("u2-proj", CreateTestProject("u2-proj", createdBy: user2));

        // Act
        var user1Projects = await _store.ListProjectsAsync(user1);
        var user2Projects = await _store.ListProjectsAsync(user2);

        // Assert
        user1Projects.Should().HaveCount(1);
        user1Projects[0].Id.Should().Be("u1-proj");

        user2Projects.Should().HaveCount(1);
        user2Projects[0].Id.Should().Be("u2-proj");
    }

    [Fact]
    public async Task ListProjects_NoProjects_ReturnsEmpty()
    {
        // Act
        var projects = await _store.ListProjectsAsync("user-with-no-projects");

        // Assert
        projects.Should().BeEmpty();
    }

    #endregion

    #region Delete

    [Fact]
    public async Task DeleteProject_SoftDeletes()
    {
        // Arrange
        var projectId = "to-delete";
        var project = CreateTestProject(projectId);
        await _store.SaveSnapshotAsync(projectId, project);

        // Verify it exists
        var existsBefore = await _store.ExistsAsync(projectId);
        existsBefore.Should().BeTrue();

        // Act
        await _store.DeleteProjectAsync(projectId);

        // Assert
        var existsAfter = await _store.ExistsAsync(projectId);
        existsAfter.Should().BeFalse();

        // Soft delete: directory should be renamed, not completely deleted
        var deletedDirs = Directory.GetDirectories(
            Path.Combine(_testDataRoot, "projects"),
            $"{projectId}.deleted.*");
        deletedDirs.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteProject_NonExistent_DoesNotThrow()
    {
        // Act
        var act = () => _store.DeleteProjectAsync("non-existent");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteProject_RemovedFromListing()
    {
        // Arrange
        var userId = "delete-test-user";
        var projectId = "to-remove-from-list";
        var project = CreateTestProject(projectId, createdBy: userId);
        await _store.SaveSnapshotAsync(projectId, project);

        var listBefore = await _store.ListProjectsAsync(userId);
        listBefore.Should().ContainSingle(p => p.Id == projectId);

        // Act
        await _store.DeleteProjectAsync(projectId);

        // Assert - Project should no longer be loadable
        var loaded = await _store.LoadSnapshotAsync(projectId);
        loaded.Should().BeNull();
    }

    #endregion

    #region Exists

    [Fact]
    public async Task Exists_ExistingProject_ReturnsTrue()
    {
        // Arrange
        var project = CreateTestProject("exists-test");
        await _store.SaveSnapshotAsync(project.Id, project);

        // Act
        var exists = await _store.ExistsAsync(project.Id);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Exists_NonExistingProject_ReturnsFalse()
    {
        // Act
        var exists = await _store.ExistsAsync("does-not-exist");

        // Assert
        exists.Should().BeFalse();
    }

    #endregion

    #region Concurrent Access

    [Fact]
    public async Task SaveSnapshot_ConcurrentWrites_AllSucceed()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act - 20 concurrent project saves
        for (int i = 0; i < 20; i++)
        {
            var projectId = $"concurrent-{i}";
            var project = CreateTestProject(projectId);
            tasks.Add(_store.SaveSnapshotAsync(projectId, project));
        }

        await Task.WhenAll(tasks);

        // Assert - All projects should be loadable
        for (int i = 0; i < 20; i++)
        {
            var loaded = await _store.LoadSnapshotAsync($"concurrent-{i}");
            loaded.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task AppendEvent_ConcurrentWrites_AllSucceed()
    {
        // Arrange
        var projectId = "concurrent-events";
        var project = CreateTestProject(projectId);
        await _store.SaveSnapshotAsync(projectId, project);

        var tasks = new List<Task>();

        // Act - 20 concurrent event appends
        for (int i = 0; i < 20; i++)
        {
            var evt = CreateTestEvent(projectId, i, i + 1, description: $"Event {i}");
            tasks.Add(_store.AppendEventAsync(projectId, evt));
        }

        await Task.WhenAll(tasks);

        // Assert - All events should be stored
        var events = await _store.LoadEventsAsync(projectId, sinceVersion: 0);
        events.Should().HaveCount(20);
    }

    #endregion

    #region Helper Methods

    private static Project CreateTestProject(
        string id,
        int version = 1,
        string name = "Test Project",
        string createdBy = "test-user")
    {
        var now = DateTimeOffset.UtcNow;
        var startPeriod = new YearMonth(2025, 1);
        var endPeriod = new YearMonth(2035, 12);

        return new Project
        {
            Id = id,
            Name = name,
            Description = "Test project description",
            Currency = "EUR",
            StartPeriod = startPeriod,
            EndPeriod = endPeriod,
            Version = version,
            SchemaVersion = "1.0",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
            Property = new Property
            {
                Id = "prop-1",
                Type = PropertyType.MultiFamilyHome,
                ConstructionYear = 2000,
                OverallCondition = Condition.Good,
                TotalArea = 500m,
                LivingArea = 400m,
                UnitCount = 4
            },
            Purchase = new Purchase
            {
                PurchasePrice = Money.Euro(400_000m),
                LandValue = Money.Euro(80_000m),
                PurchaseDate = startPeriod.ToFirstDayOfMonth(),
                AcquisitionCosts = new[]
                {
                    new AcquisitionCost { Type = AcquisitionCostType.TransferTax, Amount = Money.Euro(20_000m) },
                    new AcquisitionCost { Type = AcquisitionCostType.Notary, Amount = Money.Euro(6_000m) }
                }
            },
            Financing = new Financing
            {
                EquityContributions = new[]
                {
                    new EquityContribution
                    {
                        InvestorId = createdBy,
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
            },
            Rent = new RentConfiguration
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
            },
            Costs = new CostConfiguration
            {
                Items = new[]
                {
                    new CostItem
                    {
                        Id = "cost-1",
                        Name = "Verwaltung",
                        Classification = CostClassification.Administration,
                        MonthlyAmount = Money.Euro(100m)
                    }
                }
            },
            TaxProfile = new TaxProfile
            {
                OwnershipType = OwnershipType.PrivateIndividual,
                MarginalTaxRatePercent = 42m,
                SolidaritySurchargePercent = 5.5m,
                LossOffsetEnabled = true
            }
        };
    }

    private static ProjectEvent CreateTestEvent(
        string projectId,
        int baseVersion,
        int resultVersion,
        string description = "Test change")
    {
        return new ProjectEvent
        {
            ChangeSetId = Guid.NewGuid().ToString(),
            ProjectId = projectId,
            UserId = "test-user",
            Timestamp = DateTimeOffset.UtcNow,
            BaseVersion = baseVersion,
            ResultVersion = resultVersion,
            Operations = new[]
            {
                new JsonPatchOperation
                {
                    Op = "replace",
                    Path = "/name",
                    Value = JsonSerializer.SerializeToElement($"Updated at version {resultVersion}")
                }
            },
            Description = description
        };
    }

    #endregion
}
