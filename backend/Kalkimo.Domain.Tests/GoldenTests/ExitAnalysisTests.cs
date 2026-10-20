using Kalkimo.Domain.Calculators;
using Kalkimo.Domain.Models;
using Kalkimo.Domain.Tests.TestHelpers;

namespace Kalkimo.Domain.Tests.GoldenTests;

/// <summary>
/// Golden Tests für ExitAnalysisCalculator.
/// Testet §23 EStG Spekulationsfrist, 1.000€ Freigrenze,
/// Multi-Szenario Exit-Berechnung und Gesamtrendite.
/// </summary>
public class ExitAnalysisTests
{
    private static PropertyValueForecastResult CreateForecast(decimal purchasePrice, params (string Label, decimal Rate, decimal FinalValue)[] scenarios)
    {
        return new PropertyValueForecastResult
        {
            PurchasePrice = purchasePrice,
            ImprovementValueFactor = 0.7m,
            InitialConditionFactor = 0.9m,
            Drivers = [],
            Scenarios = scenarios.Select(s => new PropertyValueScenario
            {
                Label = s.Label,
                AnnualAppreciationPercent = s.Rate,
                FinalValue = s.FinalValue,
                YearlyValues = []
            }).ToList()
        };
    }

    private static MoneyTimeSeries CreateTimeSeries(int startYear, int endYear, decimal monthlyValue)
    {
        var start = new YearMonth(startYear, 1);
        var end = new YearMonth(endYear, 12);
        var ts = new MoneyTimeSeries(start, end);
        foreach (var period in ts.Periods)
        {
            ts[period] = Money.Euro(monthlyValue);
        }
        return ts;
    }

    private static MoneyTimeSeries CreateDebtSeries(int startYear, int endYear, decimal startDebt, decimal monthlyPaydown)
    {
        var start = new YearMonth(startYear, 1);
        var end = new YearMonth(endYear, 12);
        var ts = new MoneyTimeSeries(start, end);
        var current = startDebt;
        foreach (var period in ts.Periods)
        {
            ts[period] = Money.Euro(current);
            current = Math.Max(0, current - monthlyPaydown);
        }
        return ts;
    }

    #region Spekulationsfrist (§23 EStG)

    [Fact]
    public void Calculate_HoldingPeriod_LessThan10Years_IsWithinSpeculation()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2030);
        var forecast = CreateForecast(400_000m, ("base", 1.5m, 450_000m));

        var result = ExitAnalysisCalculator.Calculate(
            project, forecast,
            CreateTimeSeries(2025, 2030, 500),
            CreateTimeSeries(2025, 2030, 2000),
            CreateTimeSeries(2025, 2030, 300),
            CreateTimeSeries(2025, 2030, 1500),
            CreateTimeSeries(2025, 2030, 0),
            CreateTimeSeries(2025, 2030, 0),
            CreateDebtSeries(2025, 2030, 300_000, 500),
            CreateTimeSeries(2025, 2030, 0),
            CreateTimeSeries(2025, 2030, 0));

        // Januar 2025 bis einschließlich Dezember 2030 = 6 volle Jahre
        Assert.Equal(6, result.HoldingPeriodYears);
        Assert.True(result.IsWithinSpeculationPeriod);
    }

    [Fact]
    public void Calculate_HoldingPeriod_10YearsOrMore_NotWithinSpeculation()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2035);
        var forecast = CreateForecast(400_000m, ("base", 1.5m, 480_000m));

        var result = ExitAnalysisCalculator.Calculate(
            project, forecast,
            CreateTimeSeries(2025, 2035, 500),
            CreateTimeSeries(2025, 2035, 2000),
            CreateTimeSeries(2025, 2035, 300),
            CreateTimeSeries(2025, 2035, 1500),
            CreateTimeSeries(2025, 2035, 0),
            CreateTimeSeries(2025, 2035, 0),
            CreateDebtSeries(2025, 2035, 300_000, 500),
            CreateTimeSeries(2025, 2035, 0),
            CreateTimeSeries(2025, 2035, 0));

        // Januar 2025 bis einschließlich Dezember 2035 = 11 volle Jahre
        Assert.Equal(11, result.HoldingPeriodYears);
        Assert.False(result.IsWithinSpeculationPeriod);
    }

    #endregion

    #region Konsistenz mit Orchestrator und gemeinsamer Steuerlogik

    [Fact]
    public void Calculate_PlannedSale_ExitAnalysisUsesSameTaxAsMetrics()
    {
        var orchestrator = new CalculationOrchestrator(new TaxCalculator(new FixedDateProvider(new DateOnly(2025, 1, 1))));
        var saleDate = new YearMonth(2030, 6);
        var project = TestProjectBuilder.CreateStandardProject()
            .WithCapEx(TestProjectBuilder.CreateMeasure(cost: 40_000m, year: 2027, month: 3,
                taxClass: TaxClassification.ManufacturingCosts));
        project = project with { Valuation = project.Valuation with { PlannedSaleDate = saleDate } };

        var result = orchestrator.Calculate(project);

        // Gleiche Debt-/Steuerbasis: Exit-Szenario mit identischem Verkaufspreis liefert identische Steuer
        var salePrice = result.PropertyValue[saleDate].Amount;
        var taxCalculator = new TaxCalculator(new FixedDateProvider(new DateOnly(2025, 1, 1)));
        var forecast = CreateForecast(400_000m, ("base", 0m, salePrice));
        forecast = forecast with
        {
            Scenarios = forecast.Scenarios.Select(s => s with
            {
                YearlyValues = [new PropertyValueRow { Year = 2030, EstimatedValue = salePrice }]
            }).ToList()
        };

        var exit = ExitAnalysisCalculator.Calculate(
            project, forecast,
            result.CashflowAfterTax, result.GrossRent, result.OperatingCosts, result.DebtService,
            new MoneyTimeSeries(project.StartPeriod, project.EndPeriod), result.TaxPayment,
            result.OutstandingDebt, result.ReserveBalance, result.DepreciationDeduction,
            taxCalculator, saleDate,
            project.CapEx!.Measures.Where(m => m.IsExecuted));

        var scenario = exit.Scenarios.Single();
        Assert.Equal(result.TaxSummary.CapitalGainsTax!.Value.Amount, scenario.CapitalGainsTax);
        Assert.Equal(result.Metrics.ExitEquityProceeds!.Value.Amount, scenario.NetSaleProceeds, 2);
        Assert.Equal(result.OutstandingDebt[saleDate].Amount, exit.OutstandingDebtAtExit);
    }

    [Fact]
    public void Calculate_Corporation_TaxedEvenAfter10Years()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2036);
        project = project with { TaxProfile = project.TaxProfile with { OwnershipType = OwnershipType.Corporation } };
        var forecast = CreateForecast(400_000m, ("base", 2m, 600_000m));

        var result = ExitAnalysisCalculator.Calculate(
            project, forecast,
            CreateTimeSeries(2025, 2036, 500),
            CreateTimeSeries(2025, 2036, 2000),
            CreateTimeSeries(2025, 2036, 300),
            CreateTimeSeries(2025, 2036, 1500),
            CreateTimeSeries(2025, 2036, 0),
            CreateTimeSeries(2025, 2036, 0),
            CreateDebtSeries(2025, 2036, 300_000, 500),
            CreateTimeSeries(2025, 2036, 0),
            CreateTimeSeries(2025, 2036, 0));

        Assert.False(result.IsWithinSpeculationPeriod);
        Assert.True(result.Scenarios.Single().CapitalGainsTax > 0, "GmbH: Veräußerungsgewinn ist laufender Gewinn");
    }

    [Fact]
    public void Calculate_TaxExemptSale_StillReportsEconomicGain()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2036);
        var forecast = CreateForecast(400_000m, ("base", 2m, 600_000m));

        var result = ExitAnalysisCalculator.Calculate(
            project, forecast,
            CreateTimeSeries(2025, 2036, 500),
            CreateTimeSeries(2025, 2036, 2000),
            CreateTimeSeries(2025, 2036, 300),
            CreateTimeSeries(2025, 2036, 1500),
            CreateTimeSeries(2025, 2036, 0),
            CreateTimeSeries(2025, 2036, 0),
            CreateDebtSeries(2025, 2036, 300_000, 500),
            CreateTimeSeries(2025, 2036, 0),
            CreateTimeSeries(2025, 2036, 0));

        var scenario = result.Scenarios.Single();
        Assert.Equal(0m, scenario.CapitalGainsTax);
        // 600.000 − 5% Kosten − (400.000 + aktivierte NK) > 0
        Assert.True(scenario.CapitalGain > 100_000m);
    }

    #endregion

    #region Spekulationssteuer und Freigrenze

    [Fact]
    public void Calculate_NoTax_WhenHoldingPeriodExceeds10Years()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2036);
        var forecast = CreateForecast(400_000m, ("base", 2m, 500_000m));

        var result = ExitAnalysisCalculator.Calculate(
            project, forecast,
            CreateTimeSeries(2025, 2036, 500),
            CreateTimeSeries(2025, 2036, 2000),
            CreateTimeSeries(2025, 2036, 300),
            CreateTimeSeries(2025, 2036, 1500),
            CreateTimeSeries(2025, 2036, 0),
            CreateTimeSeries(2025, 2036, 0),
            CreateDebtSeries(2025, 2036, 300_000, 500),
            CreateTimeSeries(2025, 2036, 0),
            CreateTimeSeries(2025, 2036, 0));

        // After 10+ years → no capital gains tax
        Assert.All(result.Scenarios, s => Assert.Equal(0m, s.CapitalGainsTax));
    }

    [Fact]
    public void Calculate_Freigrenze_Under1000_NoTax()
    {
        // Capital gain below 1000€ → tax free (Freigrenze)
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2030);
        // Purchase 400k, acquisition costs ~40k → tax basis ~440k
        // If property value = 440.500€ → gain = 500€ < 1000€
        var forecast = CreateForecast(400_000m, ("base", 0m, 440_500m));

        var result = ExitAnalysisCalculator.Calculate(
            project, forecast,
            CreateTimeSeries(2025, 2030, 500),
            CreateTimeSeries(2025, 2030, 2000),
            CreateTimeSeries(2025, 2030, 300),
            CreateTimeSeries(2025, 2030, 1500),
            CreateTimeSeries(2025, 2030, 0),
            CreateTimeSeries(2025, 2030, 0),
            CreateDebtSeries(2025, 2030, 300_000, 500),
            CreateTimeSeries(2025, 2030, 0),
            CreateTimeSeries(2025, 2030, 0));

        // Gain below 1000€ Freigrenze → no tax
        var scenario = result.Scenarios.First();
        Assert.Equal(0m, scenario.CapitalGainsTax);
    }

    [Fact]
    public void Calculate_Freigrenze_Over1000_FullTax()
    {
        // Capital gain above 1000€ → entire gain taxed (Freigrenze, NOT Freibetrag!)
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2030);
        // Massive gain to ensure it's well over 1000€
        var forecast = CreateForecast(400_000m, ("base", 5m, 600_000m));

        var result = ExitAnalysisCalculator.Calculate(
            project, forecast,
            CreateTimeSeries(2025, 2030, 500),
            CreateTimeSeries(2025, 2030, 2000),
            CreateTimeSeries(2025, 2030, 300),
            CreateTimeSeries(2025, 2030, 1500),
            CreateTimeSeries(2025, 2030, 0),
            CreateTimeSeries(2025, 2030, 0),
            CreateDebtSeries(2025, 2030, 300_000, 500),
            CreateTimeSeries(2025, 2030, 0),
            CreateTimeSeries(2025, 2030, 0));

        // Within 5 years + gain > 1000€ → full tax
        var scenario = result.Scenarios.First();
        Assert.True(scenario.CapitalGainsTax > 0, "Should have capital gains tax");
    }

    #endregion

    #region Szenario-Ergebnisse

    [Fact]
    public void Calculate_ReturnsScenarios_FromForecast()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2035);
        var forecast = CreateForecast(400_000m,
            ("conservative", 0m, 400_000m),
            ("base", 1.5m, 465_000m),
            ("optimistic", 3m, 540_000m));

        var result = ExitAnalysisCalculator.Calculate(
            project, forecast,
            CreateTimeSeries(2025, 2035, 500),
            CreateTimeSeries(2025, 2035, 2000),
            CreateTimeSeries(2025, 2035, 300),
            CreateTimeSeries(2025, 2035, 1500),
            CreateTimeSeries(2025, 2035, 0),
            CreateTimeSeries(2025, 2035, 0),
            CreateDebtSeries(2025, 2035, 300_000, 500),
            CreateTimeSeries(2025, 2035, 0),
            CreateTimeSeries(2025, 2035, 0));

        Assert.Equal(3, result.Scenarios.Count);
        Assert.Contains(result.Scenarios, s => s.Label == "conservative");
        Assert.Contains(result.Scenarios, s => s.Label == "base");
        Assert.Contains(result.Scenarios, s => s.Label == "optimistic");
    }

    [Fact]
    public void Calculate_HigherPropertyValue_HigherTotalReturn()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2035);
        var forecast = CreateForecast(400_000m,
            ("low", 0m, 380_000m),
            ("high", 3m, 540_000m));

        var result = ExitAnalysisCalculator.Calculate(
            project, forecast,
            CreateTimeSeries(2025, 2035, 500),
            CreateTimeSeries(2025, 2035, 2000),
            CreateTimeSeries(2025, 2035, 300),
            CreateTimeSeries(2025, 2035, 1500),
            CreateTimeSeries(2025, 2035, 0),
            CreateTimeSeries(2025, 2035, 0),
            CreateDebtSeries(2025, 2035, 300_000, 500),
            CreateTimeSeries(2025, 2035, 0),
            CreateTimeSeries(2025, 2035, 0));

        var low = result.Scenarios.First(s => s.Label == "low");
        var high = result.Scenarios.First(s => s.Label == "high");

        Assert.True(high.TotalReturn > low.TotalReturn);
        Assert.True(high.AnnualizedReturnPercent > low.AnnualizedReturnPercent);
    }

    #endregion

    #region Verkaufskosten und Gesamtwerte

    [Fact]
    public void Calculate_SaleCosts_DefaultsTo5Percent()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2035);
        var forecast = CreateForecast(400_000m, ("base", 1.5m, 500_000m));

        var result = ExitAnalysisCalculator.Calculate(
            project, forecast,
            CreateTimeSeries(2025, 2035, 500),
            CreateTimeSeries(2025, 2035, 2000),
            CreateTimeSeries(2025, 2035, 300),
            CreateTimeSeries(2025, 2035, 1500),
            CreateTimeSeries(2025, 2035, 0),
            CreateTimeSeries(2025, 2035, 0),
            CreateDebtSeries(2025, 2035, 300_000, 500),
            CreateTimeSeries(2025, 2035, 0),
            CreateTimeSeries(2025, 2035, 0));

        Assert.Equal(5.0m, result.SaleCostsPercent);
        var scenario = result.Scenarios.First();
        Assert.Equal(500_000m * 0.05m, scenario.SaleCosts);
    }

    [Fact]
    public void Calculate_TotalCashflowAfterTax_MatchesSum()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2035);
        var cfAfterTax = CreateTimeSeries(2025, 2035, 500);
        var forecast = CreateForecast(400_000m, ("base", 1.5m, 480_000m));

        var result = ExitAnalysisCalculator.Calculate(
            project, forecast,
            cfAfterTax,
            CreateTimeSeries(2025, 2035, 2000),
            CreateTimeSeries(2025, 2035, 300),
            CreateTimeSeries(2025, 2035, 1500),
            CreateTimeSeries(2025, 2035, 0),
            CreateTimeSeries(2025, 2035, 0),
            CreateDebtSeries(2025, 2035, 300_000, 500),
            CreateTimeSeries(2025, 2035, 0),
            CreateTimeSeries(2025, 2035, 0));

        var expected = cfAfterTax.Sum().Amount;
        Assert.Equal(expected, result.TotalCashflowAfterTax);
    }

    [Fact]
    public void Calculate_EquityInvested_MatchesFinancing()
    {
        var project = TestProjectBuilder.CreateStandardProject();
        var forecast = CreateForecast(400_000m, ("base", 1.5m, 480_000m));

        var result = ExitAnalysisCalculator.Calculate(
            project, forecast,
            CreateTimeSeries(2025, 2035, 500),
            CreateTimeSeries(2025, 2035, 2000),
            CreateTimeSeries(2025, 2035, 300),
            CreateTimeSeries(2025, 2035, 1500),
            CreateTimeSeries(2025, 2035, 0),
            CreateTimeSeries(2025, 2035, 0),
            CreateDebtSeries(2025, 2035, 300_000, 500),
            CreateTimeSeries(2025, 2035, 0),
            CreateTimeSeries(2025, 2035, 0));

        Assert.Equal(project.Financing.TotalEquity.Amount, result.EquityInvested);
    }

    #endregion
}
