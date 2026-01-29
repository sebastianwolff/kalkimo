using System.Security.Claims;
using Kalkimo.Api.Infrastructure;
using Kalkimo.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kalkimo.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAuthStore _authStore;
    private readonly IProjectStore _projectStore;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAuthStore authStore, IProjectStore projectStore, ILogger<AdminController> logger)
    {
        _authStore = authStore;
        _projectStore = projectStore;
        _logger = logger;
    }

    /// <summary>
    /// Alle Benutzer mit Projektanzahl (Admin-Dashboard)
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserSummary>>> GetAllUsers(CancellationToken ct)
    {
        var users = await _authStore.GetAllUsersAsync(ct);

        var summaries = new List<AdminUserSummary>();
        foreach (var user in users)
        {
            var projects = await _projectStore.ListProjectsAsync(user.Id, ct);
            summaries.Add(new AdminUserSummary
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Roles = user.Roles,
                CreatedAt = user.CreatedAt,
                ProjectCount = projects.Count
            });
        }

        _logger.LogInformation("Admin {AdminId} listed all users", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(summaries);
    }

    /// <summary>
    /// Einzelnen Benutzer mit Projekten laden (Admin-Detailansicht)
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<ActionResult<AdminUserDetail>> GetUserDetail(string userId, CancellationToken ct)
    {
        var user = await _authStore.GetUserByIdAsync(userId, ct);
        if (user == null)
        {
            return NotFound(new { error = "Benutzer nicht gefunden" });
        }

        var projects = await _projectStore.ListProjectsAsync(userId, ct);

        _logger.LogInformation("Admin {AdminId} viewed user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, userId);

        return Ok(new AdminUserDetail
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Roles = user.Roles,
            CreatedAt = user.CreatedAt,
            Projects = projects
        });
    }
}
