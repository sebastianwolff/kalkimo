using Kalkimo.Api.Mapping;
using Kalkimo.Api.Models;
using Kalkimo.Domain.Calculators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kalkimo.Api.Controllers;

/// <summary>
/// Anonymer Berechnungs-Controller.
/// Ermöglicht Berechnungen ohne Authentifizierung (Gast-Modus).
/// Rate-limited auf 20 Requests/Minute pro IP.
/// </summary>
[ApiController]
[Route("api/calculate")]
[EnableRateLimiting("calculation")]
public class CalculationController : ControllerBase
{
    private readonly CalculationOrchestrator _calculator;
    private readonly ILogger<CalculationController> _logger;

    public CalculationController(
        CalculationOrchestrator calculator,
        ILogger<CalculationController> logger)
    {
        _calculator = calculator;
        _logger = logger;
    }

    /// <summary>
    /// Anonyme Berechnung — akzeptiert Frontend-Projektdaten direkt im Body.
    /// Kein Login erforderlich (Gast-Modus).
    /// </summary>
    [HttpPost]
    public ActionResult<CalculationResponseDto> Calculate(
        [FromBody] AnonymousCalculationRequest request)
    {
        _logger.LogInformation(
            "Anonymous calculation requested: {StartYear}-{EndYear}, {Currency}",
            request.StartPeriod.Year,
            request.EndPeriod.Year,
            request.Currency);

        try
        {
            // Map frontend DTO → domain model
            var project = FrontendProjectMapper.MapToProject(request);

            // Execute calculation
            var result = _calculator.Calculate(project);

            // Map domain result → response DTO
            var response = CalculationResultMapper.MapToDto(result);

            _logger.LogInformation(
                "Anonymous calculation completed: IRR={IrrPercent}%, NPV={Npv}",
                result.Metrics.IrrAfterTaxPercent,
                result.Metrics.NpvAfterTax);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid calculation request");
            return BadRequest(new { error = "Invalid request", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anonymous calculation failed");
            return StatusCode(500, new
            {
                error = "Calculation failed",
                message = ex.Message
            });
        }
    }
}
