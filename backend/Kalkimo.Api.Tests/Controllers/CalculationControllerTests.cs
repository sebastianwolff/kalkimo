using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Kalkimo.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kalkimo.Api.Tests.Controllers;

/// <summary>
/// Integrationstests für den anonymen CalculationController.
/// Testet POST /api/calculate ohne Authentifizierung,
/// Response-Struktur, Validierung und Fehlerfälle.
/// </summary>
public class CalculationControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CalculationControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Erzeugt einen vollständigen, gültigen Request.
    /// </summary>
    private static AnonymousCalculationRequest CreateValidRequest(
        int startYear = 2025,
        int endYear = 2035,
        decimal purchasePrice = 400_000m)
    {
        return new AnonymousCalculationRequest
        {
            Currency = "EUR",
            StartPeriod = new YearMonthDto { Year = startYear, Month = 1 },
            EndPeriod = new YearMonthDto { Year = endYear, Month = 12 },
            Property = new PropertyDto
            {
                Id = "prop-1",
                Type = "MultiFamily",
                ConstructionYear = 2000,
                OverallCondition = "Good",
                TotalArea = 500m,
                LivingArea = 400m,
                LandArea = 600m,
                UnitCount = 4,
                Components = new[]
                {
                    new ComponentConditionDto { Category = "Heating", Condition = "Fair", LastRenovationYear = 2010, ExpectedCycleYears = 20 },
                    new ComponentConditionDto { Category = "Roof", Condition = "Good", LastRenovationYear = 2000, ExpectedCycleYears = 45 }
                }
            },
            Purchase = new PurchaseDto
            {
                PurchasePrice = new MoneyInputDto { Amount = purchasePrice },
                PurchaseDate = new YearMonthDto { Year = startYear, Month = 1 },
                LandValuePercent = 20m,
                Costs = new[]
                {
                    new PurchaseCostItemDto { Id = "c1", Name = "Grunderwerbsteuer", Amount = new MoneyInputDto { Amount = purchasePrice * 0.05m } },
                    new PurchaseCostItemDto { Id = "c2", Name = "Notar und Grundbuch", Amount = new MoneyInputDto { Amount = purchasePrice * 0.015m } },
                    new PurchaseCostItemDto { Id = "c3", Name = "Maklergebühr", Amount = new MoneyInputDto { Amount = purchasePrice * 0.0357m } }
                }
            },
            Financing = new FinancingDto
            {
                Equity = new MoneyInputDto { Amount = 100_000m },
                Loans = new[]
                {
                    new LoanDto
                    {
                        Id = "loan-1",
                        Name = "Hauptdarlehen",
                        Principal = new MoneyInputDto { Amount = 340_280m },
                        InterestRatePercent = 3.5m,
                        InitialRepaymentPercent = 2m,
                        FixedInterestYears = 10,
                        StartDate = new YearMonthDto { Year = startYear, Month = 1 }
                    }
                }
            },
            Rent = new RentConfigurationDto
            {
                Units = new[]
                {
                    new RentUnitDto
                    {
                        UnitId = "unit-1",
                        MonthlyRent = new MoneyInputDto { Amount = 2_000m },
                        MonthlyServiceCharge = new MoneyInputDto { Amount = 300m },
                        AnnualRentIncreasePercent = 2m
                    }
                },
                VacancyRatePercent = 3m
            },
            Costs = new CostConfigurationDto
            {
                Items = new[]
                {
                    new CostItemDto { Id = "cost-admin", Name = "Verwaltung", MonthlyAmount = new MoneyInputDto { Amount = 100m }, AnnualIncreasePercent = 2m },
                    new CostItemDto { Id = "cost-ins", Name = "Versicherung", MonthlyAmount = new MoneyInputDto { Amount = 80m } }
                },
                MaintenanceReserveMonthly = new MoneyInputDto { Amount = 333m },
                MaintenanceReserveAnnualIncreasePercent = 2m
            },
            TaxProfile = new TaxProfileDto
            {
                MarginalTaxRatePercent = 42m,
                IsCorporate = false
            }
        };
    }

    #region Erfolgreiche Berechnung

    [Fact]
    public async Task Calculate_ValidRequest_Returns200()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/api/calculate", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Calculate_ValidRequest_ReturnsCalculationResponseDto()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        result.Should().NotBeNull();
        result!.ProjectId.Should().StartWith("anon-");
        result.CalculatedAt.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Calculate_ValidRequest_ContainsMetrics()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        result!.Metrics.Should().NotBeNull();
        result.Metrics.BreakEvenRent.Should().NotBeNull();
        result.Metrics.BreakEvenRent.Amount.Should().BeGreaterThan(0);
        result.Metrics.LtvInitialPercent.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Calculate_ValidRequest_ContainsYearlyCashflows()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest(startYear: 2025, endYear: 2035);

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        result!.YearlyCashflows.Should().HaveCount(11); // 2025-2035
        result.YearlyCashflows[0].Year.Should().Be(2025);
        result.YearlyCashflows[10].Year.Should().Be(2035);
    }

    [Fact]
    public async Task Calculate_ValidRequest_YearlyCashflowsHavePositiveGrossRent()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        result!.YearlyCashflows.Should().OnlyContain(r => r.GrossRent > 0);
    }

    [Fact]
    public async Task Calculate_ValidRequest_ContainsTaxBridge()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest(startYear: 2025, endYear: 2035);

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        result!.TaxBridge.Should().HaveCount(11);
        result.TaxBridge[0].Year.Should().Be(2025);
    }

    [Fact]
    public async Task Calculate_ValidRequest_ContainsTaxSummary()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        result!.TaxSummary.Should().NotBeNull();
        result.TaxSummary.TotalDepreciation.Amount.Should().BeGreaterThan(0);
        result.TaxSummary.TotalInterestDeduction.Amount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Calculate_ValidRequest_ContainsTotals()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        result!.TotalEquityInvested.Should().Be(100_000m);
        result.TotalCashflowBeforeTax.Should().NotBe(0);
        result.TotalCashflowAfterTax.Should().NotBe(0);
    }

    #endregion

    #region PropertyValueForecast in Response

    [Fact]
    public async Task Calculate_ValidRequest_ContainsPropertyValueForecast()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        result!.PropertyValueForecast.Should().NotBeNull();
        result.PropertyValueForecast.Scenarios.Should().HaveCount(3);
        result.PropertyValueForecast.Drivers.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Calculate_ValidRequest_ForecastScenariosHaveYearlyValues()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest(startYear: 2025, endYear: 2035);

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        foreach (var scenario in result!.PropertyValueForecast.Scenarios)
        {
            scenario.YearlyValues.Should().NotBeEmpty();
            scenario.FinalValue.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task Calculate_WithRegionalPrice_HasMarketComparison()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();
        request = request with
        {
            Property = request.Property with { RegionalPricePerSqm = 3_000m }
        };

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        result!.PropertyValueForecast.MarketComparison.Should().NotBeNull();
        result.PropertyValueForecast.MarketComparison!.RegionalPricePerSqm.Should().Be(3_000m);
    }

    #endregion

    #region ExitAnalysis in Response

    [Fact]
    public async Task Calculate_ValidRequest_ContainsExitAnalysis()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        result!.ExitAnalysis.Should().NotBeNull();
        result.ExitAnalysis.Scenarios.Should().HaveCount(3);
    }

    [Fact]
    public async Task Calculate_LongHolding_ExitNotWithinSpeculation()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest(startYear: 2025, endYear: 2036);

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        // 12 years holding → not within 10-year speculation period
        result!.ExitAnalysis.IsWithinSpeculationPeriod.Should().BeFalse();
    }

    [Fact]
    public async Task Calculate_ShortHolding_ExitWithinSpeculation()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest(startYear: 2025, endYear: 2030);

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        // 6 years holding → within 10-year speculation period
        result!.ExitAnalysis.IsWithinSpeculationPeriod.Should().BeTrue();
    }

    [Fact]
    public async Task Calculate_ValidRequest_ExitScenariosHaveReasonableValues()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest(purchasePrice: 400_000m);

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        result!.ExitAnalysis.PurchasePrice.Should().Be(400_000m);

        foreach (var scenario in result.ExitAnalysis.Scenarios)
        {
            scenario.PropertyValueAtExit.Should().BeGreaterThan(0);
            scenario.SaleCosts.Should().BeGreaterThan(0);
            scenario.NetSaleProceeds.Should().BeGreaterThan(0);
        }
    }

    #endregion

    #region CapEx in Response

    [Fact]
    public async Task Calculate_WithCapEx_CapExTimelinePopulated()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();
        request = request with
        {
            Capex = new[]
            {
                new CapExMeasureDto
                {
                    Id = "m1",
                    Name = "Heizung",
                    Category = "Heating",
                    Amount = new MoneyInputDto { Amount = 25_000m },
                    ScheduledDate = new YearMonthDto { Year = 2027, Month = 6 },
                    TaxClassification = "MaintenanceExpense"
                }
            }
        };

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        result!.CapexTimeline.Should().Contain(i => i.Id == "m1");
    }

    [Fact]
    public async Task Calculate_WithoutCapEx_CapExTimelineEmpty()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        result!.CapexTimeline.Should().BeEmpty();
    }

    #endregion

    #region Keine Authentifizierung nötig

    [Fact]
    public async Task Calculate_NoAuthentication_StillReturns200()
    {
        // Der anonyme Endpunkt braucht kein Bearer Token
        var client = _factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/api/calculate", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Warnungen

    [Fact]
    public async Task Calculate_ValidRequest_WarningsIsNotNull()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        result!.Warnings.Should().NotBeNull();
    }

    [Fact]
    public async Task Calculate_ValidRequest_WarningsHaveCorrectStructure()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        foreach (var warning in result!.Warnings)
        {
            warning.Type.Should().NotBeNullOrEmpty();
            warning.Message.Should().NotBeNullOrEmpty();
            warning.Severity.Should().NotBeNullOrEmpty();
            // Severity should be lowercase (as mapped by CalculationResultMapper)
            warning.Severity.Should().BeOneOf("info", "warning", "critical");
        }
    }

    #endregion

    #region Verschiedene Projektszenarien

    [Fact]
    public async Task Calculate_SingleYearProject_Works()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest(startYear: 2025, endYear: 2025);

        var response = await client.PostAsJsonAsync("/api/calculate", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();
        result!.YearlyCashflows.Should().HaveCount(1);
    }

    [Fact]
    public async Task Calculate_LongProject_Works()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest(startYear: 2025, endYear: 2055);

        var response = await client.PostAsJsonAsync("/api/calculate", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();
        result!.YearlyCashflows.Should().HaveCount(31); // 2025-2055
    }

    [Fact]
    public async Task Calculate_HighPurchasePrice_Works()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest(purchasePrice: 2_000_000m);

        var response = await client.PostAsJsonAsync("/api/calculate", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Calculate_WithComponents_ComponentDeteriorationInForecast()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        // With components defined, forecast should include deterioration data
        result!.PropertyValueForecast.ComponentDeterioration.Should().NotBeNull();
        result.PropertyValueForecast.ComponentDeterioration!.Components.Should().NotBeEmpty();
    }

    #endregion

    #region Konsistenz-Checks

    [Fact]
    public async Task Calculate_TwoIdenticalRequests_ReturnConsistentResults()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();

        var response1 = await client.PostAsJsonAsync("/api/calculate", request);
        var result1 = await response1.Content.ReadFromJsonAsync<CalculationResponseDto>();

        var response2 = await client.PostAsJsonAsync("/api/calculate", request);
        var result2 = await response2.Content.ReadFromJsonAsync<CalculationResponseDto>();

        // Same request should produce same metrics (deterministic engine)
        result1!.Metrics.LtvInitialPercent.Should().Be(result2!.Metrics.LtvInitialPercent);
        result1.TotalEquityInvested.Should().Be(result2.TotalEquityInvested);
        result1.YearlyCashflows.Count.Should().Be(result2.YearlyCashflows.Count);

        // Gross rent should be identical
        for (int i = 0; i < result1.YearlyCashflows.Count; i++)
        {
            result1.YearlyCashflows[i].GrossRent.Should().Be(result2.YearlyCashflows[i].GrossRent);
        }
    }

    [Fact]
    public async Task Calculate_CashflowAfterTax_LessThanBeforeTax()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        result!.TotalCashflowAfterTax.Should().BeLessThan(result.TotalCashflowBeforeTax);
    }

    [Fact]
    public async Task Calculate_LtvFinal_LessThanInitial()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/api/calculate", request);
        var result = await response.Content.ReadFromJsonAsync<CalculationResponseDto>();

        result!.Metrics.LtvFinalPercent.Should().BeLessThan(result.Metrics.LtvInitialPercent);
    }

    #endregion
}
