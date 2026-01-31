using Kalkimo.Domain.Calculators;
using Kalkimo.Domain.Models;
using Kalkimo.Domain.Tests.TestHelpers;

namespace Kalkimo.Domain.Tests.GoldenTests;

/// <summary>
/// Golden Tests für PropertyValueForecastCalculator.
/// Testet Multi-Szenario-Wertprognose mit Zustandsfaktor, Mean-Reversion,
/// Bauteil-Degradation und CapEx-Impact.
/// </summary>
public class PropertyValueForecastTests
{
    #region Szenarien-Grundlagen

    [Fact]
    public void Calculate_ReturnsThreeScenarios()
    {
        var project = TestProjectBuilder.CreateStandardProject();
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        Assert.Equal(3, result.Scenarios.Count);
        Assert.Contains(result.Scenarios, s => s.Label == "conservative");
        Assert.Contains(result.Scenarios, s => s.Label == "base");
        Assert.Contains(result.Scenarios, s => s.Label == "optimistic");
    }

    [Fact]
    public void Calculate_ConservativeScenario_HasZeroAppreciation()
    {
        var project = TestProjectBuilder.CreateStandardProject();
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        var conservative = result.Scenarios.First(s => s.Label == "conservative");
        Assert.Equal(0m, conservative.AnnualAppreciationPercent);
    }

    [Fact]
    public void Calculate_BaseScenario_Has1_5PercentAppreciation()
    {
        var project = TestProjectBuilder.CreateStandardProject();
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        var baseScenario = result.Scenarios.First(s => s.Label == "base");
        Assert.Equal(1.5m, baseScenario.AnnualAppreciationPercent);
    }

    [Fact]
    public void Calculate_OptimisticScenario_Has3PercentAppreciation()
    {
        var project = TestProjectBuilder.CreateStandardProject();
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        var optimistic = result.Scenarios.First(s => s.Label == "optimistic");
        Assert.Equal(3m, optimistic.AnnualAppreciationPercent);
    }

    [Fact]
    public void Calculate_EachScenario_HasYearlyValuesForEntirePeriod()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2035);
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        foreach (var scenario in result.Scenarios)
        {
            Assert.Equal(11, scenario.YearlyValues.Count); // 2025-2035 inclusive
            Assert.Equal(2025, scenario.YearlyValues.First().Year);
            Assert.Equal(2035, scenario.YearlyValues.Last().Year);
        }
    }

    [Fact]
    public void Calculate_OptimisticFinalValue_GreaterThanBase_GreaterThanConservative()
    {
        var project = TestProjectBuilder.CreateStandardProject();
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        var conservative = result.Scenarios.First(s => s.Label == "conservative").FinalValue;
        var baseVal = result.Scenarios.First(s => s.Label == "base").FinalValue;
        var optimistic = result.Scenarios.First(s => s.Label == "optimistic").FinalValue;

        Assert.True(optimistic > baseVal, "Optimistic should exceed base");
        Assert.True(baseVal > conservative, "Base should exceed conservative");
    }

    #endregion

    #region Kaufpreis und Grundwerte

    [Fact]
    public void Calculate_SetsPurchasePrice()
    {
        var project = TestProjectBuilder.CreateStandardProject(purchasePrice: 500_000m);
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        Assert.Equal(500_000m, result.PurchasePrice);
    }

    [Fact]
    public void Calculate_SetsInitialConditionFactor()
    {
        var project = TestProjectBuilder.CreateStandardProject(condition: Condition.Good);
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        // Good condition → factor ~0.90
        Assert.True(result.InitialConditionFactor > 0.8m);
        Assert.True(result.InitialConditionFactor <= 1.0m);
    }

    [Fact]
    public void Calculate_NewCondition_HasHigherInitialFactor_ThanPoor()
    {
        var projectNew = TestProjectBuilder.CreateStandardProject(condition: Condition.New);
        var projectPoor = TestProjectBuilder.CreateStandardProject(condition: Condition.Poor);

        var resultNew = PropertyValueForecastCalculator.Calculate(projectNew, 2025, 2035);
        var resultPoor = PropertyValueForecastCalculator.Calculate(projectPoor, 2025, 2035);

        Assert.True(resultNew.InitialConditionFactor > resultPoor.InitialConditionFactor,
            $"New ({resultNew.InitialConditionFactor}) should exceed Poor ({resultPoor.InitialConditionFactor})");
    }

    #endregion

    #region Zustandsfaktor und Degradation

    [Fact]
    public void Calculate_ConditionFactorDecreases_OverTime_WithoutCapEx()
    {
        var project = TestProjectBuilder.CreateStandardProject(condition: Condition.Good);
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        var baseScenario = result.Scenarios.First(s => s.Label == "base");
        var firstYear = baseScenario.YearlyValues.First();
        var lastYear = baseScenario.YearlyValues.Last();

        // Without CapEx, condition factor should decrease over time
        Assert.True(lastYear.ConditionFactor <= firstYear.ConditionFactor,
            "Condition factor should decrease without CapEx");
    }

    [Fact]
    public void Calculate_NeedsRenovation_HasLowerStartingValues_ThanGood()
    {
        var projectGood = TestProjectBuilder.CreateStandardProject(condition: Condition.Good);
        var projectBad = TestProjectBuilder.CreateStandardProject(condition: Condition.NeedsRenovation);

        var resultGood = PropertyValueForecastCalculator.Calculate(projectGood, 2025, 2035);
        var resultBad = PropertyValueForecastCalculator.Calculate(projectBad, 2025, 2035);

        var baseGood = resultGood.Scenarios.First(s => s.Label == "base").YearlyValues.First().EstimatedValue;
        var baseBad = resultBad.Scenarios.First(s => s.Label == "base").YearlyValues.First().EstimatedValue;

        Assert.True(baseGood > baseBad,
            $"Good condition value ({baseGood}) should exceed NeedsRenovation ({baseBad})");
    }

    #endregion

    #region Mean-Reversion

    [Fact]
    public void Calculate_WithRegionalPrice_SetsMeanReversionAdjustment()
    {
        var project = TestProjectBuilder.CreateStandardProject(purchasePrice: 400_000m)
            .WithRegionalPrice(1_200m); // 1200€/m² × 400m² = 480.000€ fair value (above purchase)

        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        Assert.NotNull(result.MarketComparison);
        Assert.Equal(1_200m, result.MarketComparison.RegionalPricePerSqm);
        Assert.True(result.MarketComparison.FairMarketValue > 0);
    }

    [Fact]
    public void Calculate_WithRegionalPrice_BelowMarket_HasPositiveMeanReversion()
    {
        // Purchase at 400k, fair market 480k → upward pressure
        var project = TestProjectBuilder.CreateStandardProject(purchasePrice: 400_000m)
            .WithRegionalPrice(1_200m);

        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        Assert.NotNull(result.MarketComparison);
        Assert.Equal("below", result.MarketComparison.Assessment);
    }

    [Fact]
    public void Calculate_WithRegionalPrice_AboveMarket_HasNegativeMeanReversion()
    {
        // Purchase at 400k, fair market 240k → downward pressure
        var project = TestProjectBuilder.CreateStandardProject(purchasePrice: 400_000m)
            .WithRegionalPrice(600m); // 600€/m² × 400m² = 240.000€

        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        Assert.NotNull(result.MarketComparison);
        Assert.Equal("above", result.MarketComparison.Assessment);
    }

    [Fact]
    public void Calculate_WithoutRegionalPrice_NoMarketComparison()
    {
        var project = TestProjectBuilder.CreateStandardProject();
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        Assert.Null(result.MarketComparison);
    }

    #endregion

    #region CapEx-Impact

    [Fact]
    public void Calculate_WithCapEx_IncreasesPropertyValue()
    {
        var projectNoCapex = TestProjectBuilder.CreateStandardProject();
        var projectWithCapex = TestProjectBuilder.CreateStandardProject()
            .WithCapEx(
                TestProjectBuilder.CreateMeasure("m1", CapExCategory.Heating, 25_000m, 2027),
                TestProjectBuilder.CreateMeasure("m2", CapExCategory.Windows, 30_000m, 2028)
            );

        var resultNo = PropertyValueForecastCalculator.Calculate(projectNoCapex, 2025, 2035);
        var resultWith = PropertyValueForecastCalculator.Calculate(projectWithCapex, 2025, 2035);

        var finalNoCapex = resultNo.Scenarios.First(s => s.Label == "base").FinalValue;
        var finalWithCapex = resultWith.Scenarios.First(s => s.Label == "base").FinalValue;

        Assert.True(finalWithCapex > finalNoCapex,
            $"CapEx should increase value: with={finalWithCapex}, without={finalNoCapex}");
    }

    [Fact]
    public void Calculate_ImprovementValueFactor_Is0_7()
    {
        var project = TestProjectBuilder.CreateStandardProject();
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        // Default: 70% of CapEx → value improvement
        Assert.Equal(0.70m, result.ImprovementValueFactor);
    }

    #endregion

    #region Component Deterioration

    [Fact]
    public void Calculate_WithComponents_HasDeteriorationSummary()
    {
        var project = TestProjectBuilder.CreateStandardProject();
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        Assert.NotNull(result.ComponentDeterioration);
        Assert.True(result.ComponentDeterioration.Components.Count > 0,
            "Should have component deterioration rows");
    }

    [Fact]
    public void Calculate_ComponentDeterioration_TotalValueImpact_IsNegative()
    {
        var project = TestProjectBuilder.CreateStandardProject();
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        Assert.NotNull(result.ComponentDeterioration);
        // Value impact should be negative (deterioration reduces value)
        Assert.True(result.ComponentDeterioration.TotalValueImpact <= 0,
            $"Total value impact should be non-positive: {result.ComponentDeterioration.TotalValueImpact}");
    }

    [Fact]
    public void CalculateComponentRenewalCost_ReturnsPositiveValue()
    {
        var property = TestProjectBuilder.CreateStandardProject().Property;
        var cost = PropertyValueForecastCalculator.CalculateComponentRenewalCost(CapExCategory.Heating, property);

        Assert.True(cost > 0, $"Renewal cost should be positive: {cost}");
    }

    #endregion

    #region Forecast Drivers

    [Fact]
    public void Calculate_HasForecastDrivers()
    {
        var project = TestProjectBuilder.CreateStandardProject();
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        Assert.True(result.Drivers.Count > 0, "Should have forecast drivers for transparency");
    }

    [Fact]
    public void Calculate_HasInitialConditionDriver()
    {
        var project = TestProjectBuilder.CreateStandardProject();
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        Assert.Contains(result.Drivers, d => d.Type == "initialCondition");
    }

    #endregion

    #region Randwerte

    [Fact]
    public void Calculate_SingleYearPeriod_Works()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2025);
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2025);

        Assert.Equal(3, result.Scenarios.Count);
        foreach (var s in result.Scenarios)
        {
            Assert.Single(s.YearlyValues);
        }
    }

    [Fact]
    public void Calculate_LongPeriod_20Years_Works()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2045);
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2045);

        var baseScenario = result.Scenarios.First(s => s.Label == "base");
        Assert.Equal(21, baseScenario.YearlyValues.Count);
        Assert.True(baseScenario.FinalValue > 0);
    }

    #endregion
}
