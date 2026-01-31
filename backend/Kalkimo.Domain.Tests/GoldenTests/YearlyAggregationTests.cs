using Kalkimo.Domain.Calculators;
using Kalkimo.Domain.Models;
using Kalkimo.Domain.Tests.TestHelpers;

namespace Kalkimo.Domain.Tests.GoldenTests;

/// <summary>
/// Golden Tests für YearlyAggregationService.
/// Testet Aggregation monatlicher MoneyTimeSeries zu Jahreswerten,
/// kumulative Cashflows, Partial Years und CapEx-Timeline.
/// </summary>
public class YearlyAggregationTests
{
    private static MoneyTimeSeries CreateConstantSeries(int startYear, int endYear, decimal monthlyValue)
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

    private static CalculationResult CreateMinimalResult(int startYear, int endYear, decimal monthlyValue = 1000m)
    {
        var start = new YearMonth(startYear, 1);
        var end = new YearMonth(endYear, 12);

        return new CalculationResult
        {
            ProjectId = "test",
            ScenarioId = "base",
            CalculatedAt = start,
            EngineVersion = "2.0.0",
            GrossRent = CreateConstantSeries(startYear, endYear, monthlyValue),
            EffectiveRent = CreateConstantSeries(startYear, endYear, monthlyValue * 0.97m),
            ServiceChargeIncome = CreateConstantSeries(startYear, endYear, 300m),
            TransferableCosts = CreateConstantSeries(startYear, endYear, 200m),
            NonTransferableCosts = CreateConstantSeries(startYear, endYear, 100m),
            OperatingCosts = CreateConstantSeries(startYear, endYear, 300m),
            NetOperatingIncome = CreateConstantSeries(startYear, endYear, monthlyValue * 0.97m - 300m),
            DebtService = CreateConstantSeries(startYear, endYear, 500m),
            InterestExpense = CreateConstantSeries(startYear, endYear, 300m),
            PrincipalRepayment = CreateConstantSeries(startYear, endYear, 200m),
            CashflowBeforeTax = CreateConstantSeries(startYear, endYear, 200m),
            TaxPayment = CreateConstantSeries(startYear, endYear, 50m),
            CashflowAfterTax = CreateConstantSeries(startYear, endYear, 150m),
            CumulativeCashflow = CreateConstantSeries(startYear, endYear, 150m), // simplified
            ReserveBalance = CreateConstantSeries(startYear, endYear, 10_000m),
            OutstandingDebt = CreateConstantSeries(startYear, endYear, 300_000m),
            PropertyValue = CreateConstantSeries(startYear, endYear, 400_000m),
            DepreciationDeduction = CreateConstantSeries(startYear, endYear, 500m),
            TaxableIncome = CreateConstantSeries(startYear, endYear, 400m),
            Metrics = new InvestmentMetrics
            {
                NpvBeforeTax = Money.Euro(50_000m),
                NpvAfterTax = Money.Euro(40_000m),
                BreakEvenRent = Money.Euro(1_500m)
            },
            TaxSummary = new TaxSummary
            {
                TotalDepreciation = Money.Euro(60_000m),
                TotalInterestDeduction = Money.Euro(36_000m),
                TotalMaintenanceDeduction = Money.Euro(5_000m),
                AcquisitionRelatedCostsAmount = Money.Zero(),
                TotalTaxPayment = Money.Euro(6_000m)
            }
        };
    }

    #region AggregateToYearlyCashflowRows

    [Fact]
    public void AggregateToYearlyCashflowRows_ReturnsOneRowPerYear()
    {
        var result = CreateMinimalResult(2025, 2035);
        var grossRent = CreateConstantSeries(2025, 2035, 2_000m);
        var capex = CreateConstantSeries(2025, 2035, 0m);
        var maintenance = CreateConstantSeries(2025, 2035, 100m);
        var propertyValue = CreateConstantSeries(2025, 2035, 400_000m);

        var rows = YearlyAggregationService.AggregateToYearlyCashflowRows(
            result, grossRent, capex, maintenance, propertyValue);

        Assert.Equal(11, rows.Count); // 2025-2035 inclusive
    }

    [Fact]
    public void AggregateToYearlyCashflowRows_YearlyGrossRent_Is12TimesMonthly()
    {
        var result = CreateMinimalResult(2025, 2027);
        var grossRent = CreateConstantSeries(2025, 2027, 2_000m);
        var capex = CreateConstantSeries(2025, 2027, 0m);
        var maintenance = CreateConstantSeries(2025, 2027, 0m);
        var propertyValue = CreateConstantSeries(2025, 2027, 400_000m);

        var rows = YearlyAggregationService.AggregateToYearlyCashflowRows(
            result, grossRent, capex, maintenance, propertyValue);

        Assert.Equal(24_000m, rows[0].GrossRent); // 2000 × 12
    }

    [Fact]
    public void AggregateToYearlyCashflowRows_VacancyLoss_IsGrossMinusEffective()
    {
        var result = CreateMinimalResult(2025, 2027);
        var grossRent = CreateConstantSeries(2025, 2027, 2_000m);
        var capex = CreateConstantSeries(2025, 2027, 0m);
        var maintenance = CreateConstantSeries(2025, 2027, 0m);
        var propertyValue = CreateConstantSeries(2025, 2027, 400_000m);

        var rows = YearlyAggregationService.AggregateToYearlyCashflowRows(
            result, grossRent, capex, maintenance, propertyValue);

        foreach (var row in rows)
        {
            Assert.Equal(row.GrossRent - row.EffectiveRent, row.VacancyLoss);
        }
    }

    [Fact]
    public void AggregateToYearlyCashflowRows_CumulativeCashflow_Accumulates()
    {
        var result = CreateMinimalResult(2025, 2027);
        var grossRent = CreateConstantSeries(2025, 2027, 2_000m);
        var capex = CreateConstantSeries(2025, 2027, 0m);
        var maintenance = CreateConstantSeries(2025, 2027, 0m);
        var propertyValue = CreateConstantSeries(2025, 2027, 400_000m);

        var rows = YearlyAggregationService.AggregateToYearlyCashflowRows(
            result, grossRent, capex, maintenance, propertyValue);

        // Cumulative should grow: row[1].cumulative = row[0].cumulative + row[1].cashflowAfterTax
        for (int i = 1; i < rows.Count; i++)
        {
            var expected = rows[i - 1].CumulativeCashflow + rows[i].CashflowAfterTax;
            Assert.Equal(expected, rows[i].CumulativeCashflow);
        }
    }

    [Fact]
    public void AggregateToYearlyCashflowRows_YearsAreSequential()
    {
        var result = CreateMinimalResult(2025, 2035);
        var grossRent = CreateConstantSeries(2025, 2035, 2_000m);
        var capex = CreateConstantSeries(2025, 2035, 0m);
        var maintenance = CreateConstantSeries(2025, 2035, 0m);
        var propertyValue = CreateConstantSeries(2025, 2035, 400_000m);

        var rows = YearlyAggregationService.AggregateToYearlyCashflowRows(
            result, grossRent, capex, maintenance, propertyValue);

        for (int i = 0; i < rows.Count; i++)
        {
            Assert.Equal(2025 + i, rows[i].Year);
        }
    }

    [Fact]
    public void AggregateToYearlyCashflowRows_DebtService_IsSumOfInterestAndPrincipal()
    {
        var result = CreateMinimalResult(2025, 2027);
        var grossRent = CreateConstantSeries(2025, 2027, 2_000m);
        var capex = CreateConstantSeries(2025, 2027, 0m);
        var maintenance = CreateConstantSeries(2025, 2027, 0m);
        var propertyValue = CreateConstantSeries(2025, 2027, 400_000m);

        var rows = YearlyAggregationService.AggregateToYearlyCashflowRows(
            result, grossRent, capex, maintenance, propertyValue);

        foreach (var row in rows)
        {
            Assert.Equal(row.InterestPortion + row.PrincipalPortion, row.DebtService);
        }
    }

    #endregion

    #region AggregateToTaxBridgeRows

    [Fact]
    public void AggregateToTaxBridgeRows_ReturnsOneRowPerYear()
    {
        var result = CreateMinimalResult(2025, 2035);
        var grossRent = CreateConstantSeries(2025, 2035, 2_000m);
        var maintenance = CreateConstantSeries(2025, 2035, 100m);

        var rows = YearlyAggregationService.AggregateToTaxBridgeRows(result, grossRent, maintenance);

        Assert.Equal(11, rows.Count);
    }

    [Fact]
    public void AggregateToTaxBridgeRows_GrossIncome_IsYearlySum()
    {
        var result = CreateMinimalResult(2025, 2027);
        var grossRent = CreateConstantSeries(2025, 2027, 2_000m);
        var maintenance = CreateConstantSeries(2025, 2027, 0m);

        var rows = YearlyAggregationService.AggregateToTaxBridgeRows(result, grossRent, maintenance);

        Assert.Equal(24_000m, rows[0].GrossIncome);
    }

    [Fact]
    public void AggregateToTaxBridgeRows_MaintenanceReserve_IsAlwaysZero()
    {
        // Reserve is not a tax deduction
        var result = CreateMinimalResult(2025, 2027);
        var grossRent = CreateConstantSeries(2025, 2027, 2_000m);
        var maintenance = CreateConstantSeries(2025, 2027, 100m);

        var rows = YearlyAggregationService.AggregateToTaxBridgeRows(result, grossRent, maintenance);

        Assert.All(rows, row => Assert.Equal(0m, row.MaintenanceReserve));
    }

    #endregion

    #region BuildCapExTimeline

    [Fact]
    public void BuildCapExTimeline_NullConfig_ReturnsEmpty()
    {
        var result = YearlyAggregationService.BuildCapExTimeline(null, null);
        Assert.Empty(result);
    }

    [Fact]
    public void BuildCapExTimeline_WithMeasures_ReturnsItems()
    {
        var config = new CapExConfiguration
        {
            Measures = new[]
            {
                TestProjectBuilder.CreateMeasure("m1", CapExCategory.Heating, 25_000m, 2027, 6),
                TestProjectBuilder.CreateMeasure("m2", CapExCategory.Windows, 30_000m, 2028, 3)
            }
        };

        var result = YearlyAggregationService.BuildCapExTimeline(config, null);

        Assert.Equal(2, result.Count);
        Assert.Equal("m1", result[0].Id);
        Assert.Equal(2027, result[0].Year);
        Assert.Equal(6, result[0].Month);
        Assert.Equal(25_000m, result[0].Amount);
    }

    [Fact]
    public void BuildCapExTimeline_SortedByYearThenMonth()
    {
        var config = new CapExConfiguration
        {
            Measures = new[]
            {
                TestProjectBuilder.CreateMeasure("m2", CapExCategory.Windows, 30_000m, 2028, 3),
                TestProjectBuilder.CreateMeasure("m1", CapExCategory.Heating, 25_000m, 2027, 6)
            }
        };

        var result = YearlyAggregationService.BuildCapExTimeline(config, null);

        Assert.Equal(2027, result[0].Year);
        Assert.Equal(2028, result[1].Year);
    }

    [Fact]
    public void BuildCapExTimeline_WithRecurringOccurrences_IncludesBoth()
    {
        var config = new CapExConfiguration
        {
            Measures = new[]
            {
                TestProjectBuilder.CreateMeasure("m1", CapExCategory.Heating, 25_000m, 2027)
            }
        };

        var recurring = new List<RecurringCapExExpander.RecurringOccurrence>
        {
            new()
            {
                Year = 2030,
                Month = 1,
                Amount = Money.Euro(5_000m),
                Category = CapExCategory.Heating,
                TaxClassification = TaxClassification.MaintenanceExpense,
                Name = "Wartung (#1)",
                SourceMeasureId = "r1"
            }
        };

        var result = YearlyAggregationService.BuildCapExTimeline(config, recurring);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, i => i.Id == "m1");
        Assert.Contains(result, i => i.Id == "r1_r");
    }

    [Fact]
    public void BuildCapExTimeline_ExcludesRecurringGeneratedMeasures()
    {
        // Measures with _recurring_ in the ID should be excluded (they're generated by ExpandIntoConfiguration)
        var config = new CapExConfiguration
        {
            Measures = new[]
            {
                TestProjectBuilder.CreateMeasure("m1", CapExCategory.Heating, 25_000m, 2027),
                TestProjectBuilder.CreateMeasure("r1_recurring_0", CapExCategory.Heating, 5_000m, 2030)
            }
        };

        var result = YearlyAggregationService.BuildCapExTimeline(config, null);

        Assert.Single(result); // Only m1, not the _recurring_ one
        Assert.Equal("m1", result[0].Id);
    }

    #endregion

    #region LTV und DSCR

    [Fact]
    public void AggregateToYearlyCashflowRows_LTV_IsDebtOverPropertyValue()
    {
        var result = CreateMinimalResult(2025, 2027);
        var grossRent = CreateConstantSeries(2025, 2027, 2_000m);
        var capex = CreateConstantSeries(2025, 2027, 0m);
        var maintenance = CreateConstantSeries(2025, 2027, 0m);
        var propertyValue = CreateConstantSeries(2025, 2027, 400_000m);

        var rows = YearlyAggregationService.AggregateToYearlyCashflowRows(
            result, grossRent, capex, maintenance, propertyValue);

        foreach (var row in rows)
        {
            if (row.OutstandingDebt > 0 && propertyValue.SumForYear(row.Year).Amount > 0)
            {
                var expectedLtv = row.OutstandingDebt / 400_000m * 100;
                Assert.Equal(expectedLtv, row.LtvPercent);
            }
        }
    }

    [Fact]
    public void AggregateToYearlyCashflowRows_DSCR_IsNoiOverDebtService()
    {
        var result = CreateMinimalResult(2025, 2027);
        var grossRent = CreateConstantSeries(2025, 2027, 2_000m);
        var capex = CreateConstantSeries(2025, 2027, 0m);
        var maintenance = CreateConstantSeries(2025, 2027, 0m);
        var propertyValue = CreateConstantSeries(2025, 2027, 400_000m);

        var rows = YearlyAggregationService.AggregateToYearlyCashflowRows(
            result, grossRent, capex, maintenance, propertyValue);

        foreach (var row in rows)
        {
            if (row.DebtService > 0)
            {
                var expectedDscr = row.NetOperatingIncome / row.DebtService;
                Assert.Equal(expectedDscr, row.DscrYear);
            }
        }
    }

    #endregion
}
