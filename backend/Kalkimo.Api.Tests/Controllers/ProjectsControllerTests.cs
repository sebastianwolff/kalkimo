using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Kalkimo.Api.Infrastructure;
using Kalkimo.Api.Models;
using Kalkimo.Domain.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kalkimo.Api.Tests.Controllers;

public class ProjectsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProjectsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, LoginResponse Auth)> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"test-{Guid.NewGuid()}@example.com";

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "SecurePassword123!",
            Name = "Test User"
        });

        var auth = await response.Content.ReadFromJsonAsync<LoginResponse>();

        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        return (authenticatedClient, auth);
    }

    private static CreateProjectRequest CreateTestProjectRequest(string? name = null)
    {
        return new CreateProjectRequest
        {
            Name = name ?? $"Test Project {Guid.NewGuid():N}",
            Description = "A test project for unit testing",
            StartPeriod = new YearMonth(2024, 1),
            EndPeriod = new YearMonth(2034, 12),
            Currency = "EUR"
        };
    }

    #region Authorization Tests

    [Fact]
    public async Task GetProjects_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProject_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProject_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/projects/some-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteProject_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/projects/some-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Create Project Tests

    [Fact]
    public async Task CreateProject_WithValidData_ReturnsCreated()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var request = CreateTestProjectRequest("My Investment Property");

        // Act
        var response = await client.PostAsJsonAsync("/api/projects", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var summary = await response.Content.ReadFromJsonAsync<ProjectSummary>();
        summary.Should().NotBeNull();
        summary!.Id.Should().NotBeNullOrEmpty();
        summary.Name.Should().Be("My Investment Property");
        summary.Description.Should().Be(request.Description);
        summary.Version.Should().Be(1);
        summary.Status.Should().Be("Active");
        summary.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task CreateProject_ReturnsLocationHeader()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest());

        // Assert
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/projects/");
    }

    [Fact]
    public async Task CreateProject_SetsDefaultValues()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var request = CreateTestProjectRequest();

        // Act
        var response = await client.PostAsJsonAsync("/api/projects", request);
        var summary = await response.Content.ReadFromJsonAsync<ProjectSummary>();

        // Get the full project to verify defaults
        var projectResponse = await client.GetAsync($"/api/projects/{summary!.Id}");
        var project = await projectResponse.Content.ReadFromJsonAsync<Project>();

        // Assert
        project.Should().NotBeNull();
        project!.Property.Should().NotBeNull();
        project.Purchase.Should().NotBeNull();
        project.Financing.Should().NotBeNull();
        project.Rent.Should().NotBeNull();
        project.Costs.Should().NotBeNull();
        project.TaxProfile.Should().NotBeNull();
    }

    #endregion

    #region Get Project Tests

    [Fact]
    public async Task GetProject_ExistingProject_ReturnsProject()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest("Test Get Project"));
        var summary = await createResponse.Content.ReadFromJsonAsync<ProjectSummary>();

        // Act
        var response = await client.GetAsync($"/api/projects/{summary!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var project = await response.Content.ReadFromJsonAsync<Project>();
        project.Should().NotBeNull();
        project!.Id.Should().Be(summary.Id);
        project.Name.Should().Be("Test Get Project");
    }

    [Fact]
    public async Task GetProject_NonExistingProject_ReturnsNotFound()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/projects/non-existent-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProject_ReturnsCompleteProjectStructure()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var request = new CreateProjectRequest
        {
            Name = "Complete Structure Test",
            Description = "Testing complete structure",
            StartPeriod = new YearMonth(2024, 6),
            EndPeriod = new YearMonth(2044, 5),
            Currency = "EUR"
        };

        var createResponse = await client.PostAsJsonAsync("/api/projects", request);
        var summary = await createResponse.Content.ReadFromJsonAsync<ProjectSummary>();

        // Act
        var response = await client.GetAsync($"/api/projects/{summary!.Id}");
        var project = await response.Content.ReadFromJsonAsync<Project>();

        // Assert
        project.Should().NotBeNull();
        project!.StartPeriod.Should().Be(new YearMonth(2024, 6));
        project.EndPeriod.Should().Be(new YearMonth(2044, 5));
        project.Currency.Should().Be("EUR");
        project.SchemaVersion.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region List Projects Tests

    [Fact]
    public async Task GetProjects_NoProjects_ReturnsEmptyList()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var projects = await response.Content.ReadFromJsonAsync<List<ProjectSummary>>();
        projects.Should().NotBeNull();
        projects.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjects_WithProjects_ReturnsList()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();

        await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest("Project 1"));
        await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest("Project 2"));
        await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest("Project 3"));

        // Act
        var response = await client.GetAsync("/api/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var projects = await response.Content.ReadFromJsonAsync<List<ProjectSummary>>();
        projects.Should().HaveCount(3);
        projects!.Select(p => p.Name).Should().Contain("Project 1");
        projects.Select(p => p.Name).Should().Contain("Project 2");
        projects.Select(p => p.Name).Should().Contain("Project 3");
    }

    [Fact]
    public async Task GetProjects_DifferentUsers_SeeDifferentProjects()
    {
        // Arrange
        var (client1, _) = await CreateAuthenticatedClientAsync();
        var (client2, _) = await CreateAuthenticatedClientAsync();

        await client1.PostAsJsonAsync("/api/projects", CreateTestProjectRequest("User1 Project"));
        await client2.PostAsJsonAsync("/api/projects", CreateTestProjectRequest("User2 Project"));

        // Act
        var response1 = await client1.GetAsync("/api/projects");
        var response2 = await client2.GetAsync("/api/projects");

        var projects1 = await response1.Content.ReadFromJsonAsync<List<ProjectSummary>>();
        var projects2 = await response2.Content.ReadFromJsonAsync<List<ProjectSummary>>();

        // Assert - Each user should only see their own project
        projects1.Should().HaveCount(1);
        projects1![0].Name.Should().Be("User1 Project");

        projects2.Should().HaveCount(1);
        projects2![0].Name.Should().Be("User2 Project");
    }

    #endregion

    #region Delete Project Tests

    [Fact]
    public async Task DeleteProject_ExistingProject_ReturnsNoContent()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest());
        var summary = await createResponse.Content.ReadFromJsonAsync<ProjectSummary>();

        // Act
        var response = await client.DeleteAsync($"/api/projects/{summary!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProject_NonExistingProject_ReturnsNotFound()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.DeleteAsync("/api/projects/non-existent-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProject_AfterDelete_GetReturnsNotFound()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest());
        var summary = await createResponse.Content.ReadFromJsonAsync<ProjectSummary>();

        // Delete the project
        await client.DeleteAsync($"/api/projects/{summary!.Id}");

        // Act
        var getResponse = await client.GetAsync($"/api/projects/{summary.Id}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProject_AfterDelete_NotInList()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();

        var create1 = await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest("Keep Me"));
        var summary1 = await create1.Content.ReadFromJsonAsync<ProjectSummary>();

        var create2 = await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest("Delete Me"));
        var summary2 = await create2.Content.ReadFromJsonAsync<ProjectSummary>();

        // Delete one project
        await client.DeleteAsync($"/api/projects/{summary2!.Id}");

        // Act
        var listResponse = await client.GetAsync("/api/projects");
        var projects = await listResponse.Content.ReadFromJsonAsync<List<ProjectSummary>>();

        // Assert
        projects.Should().HaveCount(1);
        projects![0].Id.Should().Be(summary1!.Id);
    }

    #endregion

    #region Events/Changeset Tests

    [Fact]
    public async Task ApplyChangeset_WithValidData_ReturnsOk()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest());
        var summary = await createResponse.Content.ReadFromJsonAsync<ProjectSummary>();

        var changeset = new ChangesetRequest
        {
            BaseVersion = 1,
            Operations = new List<PatchOperation>
            {
                new() { Op = "replace", Path = "/name", Value = "Updated Name" }
            },
            Description = "Update project name"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/projects/{summary!.Id}/events", changeset);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ChangesetResponse>();
        result.Should().NotBeNull();
        result!.ChangeSetId.Should().NotBeNullOrEmpty();
        result.NewVersion.Should().Be(2);
        result.HasConflict.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyChangeset_VersionConflict_ReturnsConflict()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest());
        var summary = await createResponse.Content.ReadFromJsonAsync<ProjectSummary>();

        // First changeset succeeds
        await client.PostAsJsonAsync($"/api/projects/{summary!.Id}/events", new ChangesetRequest
        {
            BaseVersion = 1,
            Operations = new List<PatchOperation>
            {
                new() { Op = "replace", Path = "/name", Value = "First Update" }
            }
        });

        // Act - Second changeset with old base version
        var response = await client.PostAsJsonAsync($"/api/projects/{summary.Id}/events", new ChangesetRequest
        {
            BaseVersion = 1, // Should be 2 now
            Operations = new List<PatchOperation>
            {
                new() { Op = "replace", Path = "/description", Value = "Conflicting Update" }
            }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var result = await response.Content.ReadFromJsonAsync<ChangesetResponse>();
        result.Should().NotBeNull();
        result!.HasConflict.Should().BeTrue();
        result.Conflict.Should().NotBeNull();
        result.Conflict!.ServerVersion.Should().Be(2);
    }

    [Fact]
    public async Task ApplyChangeset_NonExistingProject_ReturnsNotFound()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/projects/non-existent/events", new ChangesetRequest
        {
            BaseVersion = 1,
            Operations = new List<PatchOperation>
            {
                new() { Op = "replace", Path = "/name", Value = "Test" }
            }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ApplyChangeset_MultipleOperations_AllRecorded()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest());
        var summary = await createResponse.Content.ReadFromJsonAsync<ProjectSummary>();

        var changeset = new ChangesetRequest
        {
            BaseVersion = 1,
            Operations = new List<PatchOperation>
            {
                new() { Op = "replace", Path = "/name", Value = "New Name" },
                new() { Op = "replace", Path = "/description", Value = "New Description" }
            },
            Description = "Multiple updates"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/projects/{summary!.Id}/events", changeset);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Get Events Tests

    [Fact]
    public async Task GetEvents_NoEvents_ReturnsEmptyList()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest());
        var summary = await createResponse.Content.ReadFromJsonAsync<ProjectSummary>();

        // Act
        var response = await client.GetAsync($"/api/projects/{summary!.Id}/events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var events = await response.Content.ReadFromJsonAsync<List<ProjectEvent>>();
        events.Should().NotBeNull();
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEvents_WithEvents_ReturnsList()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest());
        var summary = await createResponse.Content.ReadFromJsonAsync<ProjectSummary>();

        // Apply some changesets
        await client.PostAsJsonAsync($"/api/projects/{summary!.Id}/events", new ChangesetRequest
        {
            BaseVersion = 1,
            Operations = new List<PatchOperation>
            {
                new() { Op = "replace", Path = "/name", Value = "Update 1" }
            }
        });

        await client.PostAsJsonAsync($"/api/projects/{summary.Id}/events", new ChangesetRequest
        {
            BaseVersion = 2,
            Operations = new List<PatchOperation>
            {
                new() { Op = "replace", Path = "/name", Value = "Update 2" }
            }
        });

        // Act
        var response = await client.GetAsync($"/api/projects/{summary.Id}/events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var events = await response.Content.ReadFromJsonAsync<List<ProjectEvent>>();
        events.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEvents_WithSinceParameter_FiltersEvents()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest());
        var summary = await createResponse.Content.ReadFromJsonAsync<ProjectSummary>();

        // Apply changesets
        await client.PostAsJsonAsync($"/api/projects/{summary!.Id}/events", new ChangesetRequest
        {
            BaseVersion = 1,
            Operations = new List<PatchOperation> { new() { Op = "replace", Path = "/name", Value = "V2" } }
        });

        await client.PostAsJsonAsync($"/api/projects/{summary.Id}/events", new ChangesetRequest
        {
            BaseVersion = 2,
            Operations = new List<PatchOperation> { new() { Op = "replace", Path = "/name", Value = "V3" } }
        });

        // Act - Get events since version 2
        var response = await client.GetAsync($"/api/projects/{summary.Id}/events?since=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var events = await response.Content.ReadFromJsonAsync<List<ProjectEvent>>();
        events.Should().HaveCount(1);
        events![0].ResultVersion.Should().Be(3);
    }

    [Fact]
    public async Task GetEvents_NonExistingProject_ReturnsNotFound()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/projects/non-existent/events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Calculate Tests

    [Fact]
    public async Task Calculate_ExistingProject_ReturnsOk()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest());
        var summary = await createResponse.Content.ReadFromJsonAsync<ProjectSummary>();

        // Act
        var response = await client.PostAsJsonAsync($"/api/projects/{summary!.Id}/calculate", new CalculateRequest
        {
            ScenarioId = "base"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Calculate_NonExistingProject_ReturnsNotFound()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/projects/non-existent/calculate", new CalculateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Calculate_WithoutScenarioId_UsesDefault()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest());
        var summary = await createResponse.Content.ReadFromJsonAsync<ProjectSummary>();

        // Act
        var response = await client.PostAsJsonAsync($"/api/projects/{summary!.Id}/calculate", (CalculateRequest?)null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task ConcurrentCreates_AllSucceed()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();

        // Act - Create 10 projects concurrently
        var tasks = Enumerable.Range(1, 10)
            .Select(i => client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest($"Concurrent {i}")))
            .ToList();

        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        // Verify all were created
        var listResponse = await client.GetAsync("/api/projects");
        var projects = await listResponse.Content.ReadFromJsonAsync<List<ProjectSummary>>();
        projects.Should().HaveCount(10);
    }

    [Fact]
    public async Task ConcurrentChangesets_WithConflicts_HandledCorrectly()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/projects", CreateTestProjectRequest());
        var summary = await createResponse.Content.ReadFromJsonAsync<ProjectSummary>();

        // Act - Try to apply 5 changesets concurrently, all with base version 1
        var tasks = Enumerable.Range(1, 5)
            .Select(i => client.PostAsJsonAsync($"/api/projects/{summary!.Id}/events", new ChangesetRequest
            {
                BaseVersion = 1,
                Operations = new List<PatchOperation>
                {
                    new() { Op = "replace", Path = "/name", Value = $"Concurrent Update {i}" }
                }
            }))
            .ToList();

        var responses = await Task.WhenAll(tasks);

        // Assert - Only one should succeed (200), others should conflict (409)
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        successCount.Should().Be(1);
        conflictCount.Should().Be(4);
    }

    #endregion
}
