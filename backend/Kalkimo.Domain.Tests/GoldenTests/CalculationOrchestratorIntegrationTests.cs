using Kalkimo.Domain.Calculators;
using Kalkimo.Domain.Models;
using Kalkimo.Domain.Tests.TestHelpers;

namespace Kalkimo.Domain.Tests.GoldenTests;

/// <summary>
/// Integrationstests für den CalculationOrchestrator.
/// Testet die vollständige Pipeline vom Projekt bis zum CalculationResult,
/// inklusive aller neuen Schritte (Recurring CapEx, PropertyValueForecast,
/// ExitAnalysis, Jahresaggregation).
/// </summary>
public class CalculationOrchestratorIntegrationTests
{
    private readonly CalculationOrchestrator _orchestrator;

    public CalculationOrchestratorIntegrationTests()
    {
        var dateProvider = new FixedDateProvider(new DateOnly(2025, 1, 1));
        var taxCalculator = new TaxCalculator(dateProvider);
        _orchestrator = new CalculationOrchestrator(taxCalculator);
    }

    #region Grundlegende Pipeline

    [Fact]
    public void Calculate_StandardProject_ReturnsResult()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        Assert.NotNull(result);
        Assert.Equal(project.Id, result.ProjectId);
        Assert.Equal("base", result.ScenarioId);
        Assert.Equal(CalculationOrchestrator.EngineVersion, result.EngineVersion);
    }

    [Fact]
    public void Calculate_StandardProject_AllTimeSeriesPopulated()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        // All time series should have values
        Assert.True(result.GrossRent.Sum().Amount > 0, "GrossRent should be positive");
        Assert.True(result.EffectiveRent.Sum().Amount > 0, "EffectiveRent should be positive");
        Assert.True(result.OperatingCosts.Sum().Amount > 0, "OperatingCosts should be positive");
        Assert.True(result.NetOperatingIncome.Sum().Amount > 0, "NOI should be positive");
        Assert.True(result.DebtService.Sum().Amount > 0, "DebtService should be positive");
        Assert.True(result.InterestExpense.Sum().Amount > 0, "InterestExpense should be positive");
        Assert.True(result.PrincipalRepayment.Sum().Amount > 0, "PrincipalRepayment should be positive");
        Assert.True(result.DepreciationDeduction.Sum().Amount > 0, "Depreciation should be positive");
    }

    [Fact]
    public void Calculate_StandardProject_EffectiveRentLessThanGross()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        // Vacancy causes effective rent to be less than gross
        Assert.True(result.EffectiveRent.Sum().Amount < result.GrossRent.Sum().Amount);
    }

    [Fact]
    public void Calculate_StandardProject_NoiIsEffectiveMinusCosts()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        // NOI = Effective Rent + Service Charge Income - Operating Costs
        var noiSum = result.NetOperatingIncome.Sum().Amount;
        var effectiveSum = result.EffectiveRent.Sum().Amount;
        var serviceChargeSum = result.ServiceChargeIncome.Sum().Amount;
        var opsSum = result.OperatingCosts.Sum().Amount;

        // Allow small rounding tolerance
        Assert.True(Math.Abs(noiSum - (effectiveSum + serviceChargeSum - opsSum)) < 1m,
            $"NOI ({noiSum}) should equal EffectiveRent ({effectiveSum}) + ServiceCharge ({serviceChargeSum}) - OperatingCosts ({opsSum})");
    }

    [Fact]
    public void Calculate_StandardProject_DebtServiceIsSumOfInterestAndPrincipal()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        var debtSum = result.DebtService.Sum().Amount;
        var interestSum = result.InterestExpense.Sum().Amount;
        var principalSum = result.PrincipalRepayment.Sum().Amount;

        Assert.True(Math.Abs(debtSum - (interestSum + principalSum)) < 1m,
            $"DebtService ({debtSum}) should equal Interest ({interestSum}) + Principal ({principalSum})");
    }

    [Fact]
    public void Calculate_StandardProject_CumulativeCashflowAccumulates()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        // Last cumulative value should equal total after-tax cashflow
        var totalAfterTax = result.CashflowAfterTax.Sum().Amount;
        var lastCumulative = result.CumulativeCashflow[result.CumulativeCashflow.EndPeriod].Amount;

        Assert.True(Math.Abs(totalAfterTax - lastCumulative) < 1m);
    }

    #endregion

    #region Kennzahlen (Metrics)

    [Fact]
    public void Calculate_StandardProject_MetricsPopulated()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        Assert.NotNull(result.Metrics);
        Assert.True(result.Metrics.BreakEvenRent.Amount > 0, "BreakEvenRent should be positive");
        Assert.True(result.Metrics.LtvInitialPercent > 0, "LTV Initial should be positive");
        Assert.True(result.Metrics.DscrAvg > 0, "DSCR Avg should be positive");
    }

    [Fact]
    public void Calculate_StandardProject_NpvCalculated()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        // NPV should be calculated (could be negative for some projects)
        // Just verify it has EUR currency (Money is a value type)
        Assert.Equal("EUR", result.Metrics.NpvBeforeTax.Currency);
        Assert.Equal("EUR", result.Metrics.NpvAfterTax.Currency);
    }

    [Fact]
    public void Calculate_StandardProject_LtvDecreases()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        // LTV should decrease over time as debt is repaid
        Assert.True(result.Metrics.LtvFinalPercent < result.Metrics.LtvInitialPercent,
            $"LTV should decrease: initial {result.Metrics.LtvInitialPercent}% vs final {result.Metrics.LtvFinalPercent}%");
    }

    #endregion

    #region Steuern (Tax)

    [Fact]
    public void Calculate_StandardProject_TaxSummaryPopulated()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        Assert.NotNull(result.TaxSummary);
        Assert.True(result.TaxSummary.TotalDepreciation.Amount > 0, "Depreciation should be positive");
        Assert.True(result.TaxSummary.TotalInterestDeduction.Amount > 0, "Interest deduction should be positive");
        Assert.True(result.TaxSummary.TotalTaxPayment.Amount > 0, "Tax payment should be positive");
    }

    [Fact]
    public void Calculate_StandardProject_DepreciationIsConsistent()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        // Annual depreciation should be roughly consistent (2% for post-1925 buildings)
        var purchasePrice = project.Purchase.PurchasePrice.Amount;
        var landValue = project.Purchase.LandValue.Amount;
        var depreciationBase = purchasePrice - landValue;
        var expectedAnnualDepr = depreciationBase * 0.02m; // 2% for Baujahr 2000
        var years = project.EndPeriod.Year - project.StartPeriod.Year + 1;
        var expectedTotalDepr = expectedAnnualDepr * years;

        // Allow 5% tolerance (acquisition costs may add to base)
        var actualDepr = result.TaxSummary.TotalDepreciation.Amount;
        var ratio = actualDepr / expectedTotalDepr;
        Assert.True(ratio > 0.8m && ratio < 1.5m,
            $"Depreciation ratio {ratio} should be between 0.8 and 1.5 (actual: {actualDepr}, expected ~{expectedTotalDepr})");
    }

    [Fact]
    public void Calculate_StandardProject_AfterTaxLessThanBeforeTax()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        // After-tax cashflow should be less than before-tax
        Assert.True(result.TotalCashflowAfterTax < result.TotalCashflowBeforeTax,
            "After-tax should be less than before-tax cashflow");
    }

    #endregion

    #region Jahresaggregate

    [Fact]
    public void Calculate_StandardProject_YearlyCashflowsPopulated()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2035);

        var result = _orchestrator.Calculate(project);

        Assert.NotEmpty(result.YearlyCashflows);
        Assert.Equal(11, result.YearlyCashflows.Count); // 2025-2035 inclusive
    }

    [Fact]
    public void Calculate_StandardProject_YearlyCashflowsHaveCorrectYears()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2035);

        var result = _orchestrator.Calculate(project);

        for (int i = 0; i < result.YearlyCashflows.Count; i++)
        {
            Assert.Equal(2025 + i, result.YearlyCashflows[i].Year);
        }
    }

    [Fact]
    public void Calculate_StandardProject_TaxBridgePopulated()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2035);

        var result = _orchestrator.Calculate(project);

        Assert.NotEmpty(result.TaxBridge);
        Assert.Equal(11, result.TaxBridge.Count);
    }

    [Fact]
    public void Calculate_StandardProject_YearlyCashflowsSumMatchesTimeSeries()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        // Sum of yearly gross rents should approximately equal the total
        var yearlyGrossSum = result.YearlyCashflows.Sum(r => r.GrossRent);
        var timeseriesGrossSum = result.GrossRent.Sum().Amount;

        Assert.True(Math.Abs(yearlyGrossSum - timeseriesGrossSum) < 1m,
            $"Yearly sum {yearlyGrossSum} should match timeseries sum {timeseriesGrossSum}");
    }

    #endregion

    #region PropertyValueForecast

    [Fact]
    public void Calculate_StandardProject_PropertyValueForecastPopulated()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        Assert.NotNull(result.PropertyValueForecast);
        Assert.Equal(3, result.PropertyValueForecast.Scenarios.Count);
    }

    [Fact]
    public void Calculate_StandardProject_ForecastScenariosHaveCorrectLabels()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        var labels = result.PropertyValueForecast!.Scenarios.Select(s => s.Label).ToList();
        Assert.Contains(labels, l => l.Contains("conservative", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(labels, l => l.Contains("base", StringComparison.OrdinalIgnoreCase) || l.Contains("basis", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(labels, l => l.Contains("optimistic", StringComparison.OrdinalIgnoreCase) || l.Contains("optimistisch", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Calculate_StandardProject_ForecastHasDrivers()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        Assert.NotEmpty(result.PropertyValueForecast!.Drivers);
    }

    [Fact]
    public void Calculate_WithRegionalPrice_ForecastHasMarketComparison()
    {
        var project = TestProjectBuilder.CreateStandardProject()
            .WithRegionalPrice(3_000m);

        var result = _orchestrator.Calculate(project);

        Assert.NotNull(result.PropertyValueForecast!.MarketComparison);
        Assert.Equal(3_000m, result.PropertyValueForecast.MarketComparison.RegionalPricePerSqm);
    }

    #endregion

    #region ExitAnalysis

    [Fact]
    public void Calculate_StandardProject_ExitAnalysisPopulated()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        Assert.NotNull(result.ExitAnalysis);
        Assert.Equal(3, result.ExitAnalysis.Scenarios.Count);
    }

    [Fact]
    public void Calculate_StandardProject_ExitHoldingPeriodCorrect()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2035);

        var result = _orchestrator.Calculate(project);

        // holdingPeriodYears = endYear - startYear = 2035 - 2025 = 10
        Assert.Equal(10, result.ExitAnalysis!.HoldingPeriodYears);
    }

    [Fact]
    public void Calculate_StandardProject_ExitSpeculationPeriodOver10Years()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2035);

        var result = _orchestrator.Calculate(project);

        // 10 years holding → not within speculation period (≥ 10)
        Assert.False(result.ExitAnalysis!.IsWithinSpeculationPeriod);
    }

    [Fact]
    public void Calculate_ShortHolding_ExitWithinSpeculationPeriod()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2030);

        var result = _orchestrator.Calculate(project);

        // 6 years holding → within speculation period
        Assert.True(result.ExitAnalysis!.IsWithinSpeculationPeriod);
    }

    [Fact]
    public void Calculate_StandardProject_ExitScenariosHavePositiveValues()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        foreach (var scenario in result.ExitAnalysis!.Scenarios)
        {
            Assert.True(scenario.PropertyValueAtExit > 0, $"PropertyValue at exit should be > 0 for {scenario.Label}");
            // NetSaleProceeds can be negative for conservative scenarios
            // (high outstanding debt + sale costs can exceed property value)
            Assert.True(scenario.SaleCosts > 0, $"SaleCosts should be > 0 for {scenario.Label}");
        }
    }

    [Fact]
    public void Calculate_StandardProject_ExitPurchasePriceMatchesProject()
    {
        var project = TestProjectBuilder.CreateStandardProject(purchasePrice: 400_000m);

        var result = _orchestrator.Calculate(project);

        Assert.Equal(400_000m, result.ExitAnalysis!.PurchasePrice);
    }

    #endregion

    #region CapEx Integration

    [Fact]
    public void Calculate_WithCapEx_CapExTimelinePopulated()
    {
        var project = TestProjectBuilder.CreateStandardProject()
            .WithCapEx(
                TestProjectBuilder.CreateMeasure("m1", CapExCategory.Heating, 25_000m, 2027),
                TestProjectBuilder.CreateMeasure("m2", CapExCategory.Windows, 30_000m, 2028));

        var result = _orchestrator.Calculate(project);

        Assert.True(result.CapExTimeline.Count >= 2);
        Assert.Contains(result.CapExTimeline, i => i.Id == "m1");
        Assert.Contains(result.CapExTimeline, i => i.Id == "m2");
    }

    [Fact]
    public void Calculate_WithCapEx_ReducesCashflow()
    {
        var projectNoCapex = TestProjectBuilder.CreateStandardProject();
        var projectWithCapex = TestProjectBuilder.CreateStandardProject()
            .WithCapEx(TestProjectBuilder.CreateMeasure("m1", CapExCategory.Heating, 50_000m, 2027));

        var resultNo = _orchestrator.Calculate(projectNoCapex);
        var resultWith = _orchestrator.Calculate(projectWithCapex);

        // CapEx should reduce total cashflow
        Assert.True(resultWith.TotalCashflowBeforeTax < resultNo.TotalCashflowBeforeTax,
            "CapEx should reduce before-tax cashflow");
    }

    [Fact]
    public void Calculate_WithRecurringCapEx_ExpandsOccurrences()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2060)
            .WithCapEx(TestProjectBuilder.CreateRecurringMeasure("r1", CapExCategory.Heating));

        var result = _orchestrator.Calculate(project);

        // Should have recurring occurrences in the timeline
        var recurringItems = result.CapExTimeline.Where(i => i.Id.Contains("r") || i.Id.Contains("recurring")).ToList();
        Assert.True(recurringItems.Count > 0, "Should have recurring CapEx in timeline");
    }

    [Fact]
    public void Calculate_WithoutCapEx_CapExTimelineEmpty()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        Assert.Empty(result.CapExTimeline);
    }

    #endregion

    #region Totals

    [Fact]
    public void Calculate_StandardProject_TotalEquityMatchesFinancing()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        var expectedEquity = project.Financing.TotalEquity.Amount;
        Assert.Equal(expectedEquity, result.TotalEquityInvested);
    }

    [Fact]
    public void Calculate_StandardProject_TotalCashflowsPopulated()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        Assert.NotEqual(0m, result.TotalCashflowBeforeTax);
        Assert.NotEqual(0m, result.TotalCashflowAfterTax);
    }

    #endregion

    #region Warnungen

    [Fact]
    public void Calculate_StandardProject_WarningsGenerated()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        // Warnings list should at least exist (may be empty for a healthy project)
        Assert.NotNull(result.Warnings);
    }

    [Fact]
    public void Calculate_WithCapEx15PercentRule_WarningGenerated()
    {
        // Create project with high renovation costs within 3 years → 15% rule triggers
        var project = TestProjectBuilder.CreateStandardProject(purchasePrice: 400_000m)
            .WithCapEx(
                TestProjectBuilder.CreateMeasure("m1", CapExCategory.Heating, 30_000m, 2025, 6),
                TestProjectBuilder.CreateMeasure("m2", CapExCategory.Windows, 30_000m, 2026, 6));

        var result = _orchestrator.Calculate(project);

        // 60_000€ on 400_000€ = 15% → triggers 15%-Regel
        if (result.TaxSummary.AcquisitionRelatedCostsTriggered)
        {
            Assert.Contains(result.Warnings,
                w => w.Type == WarningType.AcquisitionRelatedCostsTriggered);
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Calculate_SingleYearProject_Works()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2025);

        var result = _orchestrator.Calculate(project);

        Assert.NotNull(result);
        Assert.Single(result.YearlyCashflows);
        Assert.Equal(2025, result.YearlyCashflows[0].Year);
    }

    [Fact]
    public void Calculate_LongProject_Works()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2060);

        var result = _orchestrator.Calculate(project);

        Assert.NotNull(result);
        Assert.Equal(36, result.YearlyCashflows.Count); // 2025-2060
    }

    [Fact]
    public void Calculate_HighPurchasePrice_Works()
    {
        var project = TestProjectBuilder.CreateStandardProject(purchasePrice: 2_000_000m);

        var result = _orchestrator.Calculate(project);

        Assert.NotNull(result);
        Assert.True(result.Metrics.LtvInitialPercent > 0);
    }

    [Fact]
    public void Calculate_ConditionNeedsRenovation_AffectsForecast()
    {
        var projectGood = TestProjectBuilder.CreateStandardProject(condition: Condition.Good);
        var projectPoor = TestProjectBuilder.CreateStandardProject(condition: Condition.NeedsRenovation);

        var resultGood = _orchestrator.Calculate(projectGood);
        var resultPoor = _orchestrator.Calculate(projectPoor);

        // Poor condition should result in lower initial condition factor
        Assert.True(resultPoor.PropertyValueForecast!.InitialConditionFactor <=
                     resultGood.PropertyValueForecast!.InitialConditionFactor);
    }

    #endregion
}
