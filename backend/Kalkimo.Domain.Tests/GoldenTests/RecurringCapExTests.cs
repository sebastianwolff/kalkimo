using Kalkimo.Domain.Calculators;
using Kalkimo.Domain.Models;
using Kalkimo.Domain.Tests.TestHelpers;

namespace Kalkimo.Domain.Tests.GoldenTests;

/// <summary>
/// Golden Tests für RecurringCapExExpander.
/// Testet Expansion wiederkehrender Maßnahmen, Intervall-Logik,
/// Kosten-Berechnung und Configuration-Merge.
/// </summary>
public class RecurringCapExTests
{
    #region Expand - Grundlogik

    [Fact]
    public void Expand_NullConfig_ReturnsEmpty()
    {
        var property = TestProjectBuilder.CreateStandardProject().Property;
        var result = RecurringCapExExpander.Expand(null, property, 2025, 2035);

        Assert.Empty(result);
    }

    [Fact]
    public void Expand_NoRecurringMeasures_ReturnsEmpty()
    {
        var config = new CapExConfiguration
        {
            Measures = new[]
            {
                TestProjectBuilder.CreateMeasure("m1", CapExCategory.Heating, 25_000m, 2027, isRecurring: false)
            }
        };
        var property = TestProjectBuilder.CreateStandardProject().Property;

        var result = RecurringCapExExpander.Expand(config, property, 2025, 2035);

        Assert.Empty(result);
    }

    [Fact]
    public void Expand_RecurringMeasure_GeneratesOccurrences()
    {
        var config = new CapExConfiguration
        {
            Measures = new[]
            {
                TestProjectBuilder.CreateRecurringMeasure("r1", CapExCategory.Heating, intervalPercent: 40, costPercent: 25)
            }
        };
        var property = TestProjectBuilder.CreateStandardProject().Property;

        var result = RecurringCapExExpander.Expand(config, property, 2025, 2035);

        Assert.True(result.Count > 0, "Should generate at least one occurrence");
    }

    [Fact]
    public void Expand_Occurrences_HaveCorrectSourceMeasureId()
    {
        var config = new CapExConfiguration
        {
            Measures = new[]
            {
                TestProjectBuilder.CreateRecurringMeasure("my-recurring", CapExCategory.Heating)
            }
        };
        var property = TestProjectBuilder.CreateStandardProject().Property;

        var result = RecurringCapExExpander.Expand(config, property, 2025, 2035);

        Assert.All(result, occ => Assert.Equal("my-recurring", occ.SourceMeasureId));
    }

    [Fact]
    public void Expand_Occurrences_HaveCorrectCategory()
    {
        var config = new CapExConfiguration
        {
            Measures = new[]
            {
                TestProjectBuilder.CreateRecurringMeasure("r1", CapExCategory.Windows)
            }
        };
        var property = TestProjectBuilder.CreateStandardProject().Property;

        var result = RecurringCapExExpander.Expand(config, property, 2025, 2035);

        Assert.All(result, occ => Assert.Equal(CapExCategory.Windows, occ.Category));
    }

    [Fact]
    public void Expand_Occurrences_HavePositiveCost()
    {
        var config = new CapExConfiguration
        {
            Measures = new[]
            {
                TestProjectBuilder.CreateRecurringMeasure("r1", CapExCategory.Heating, costPercent: 25)
            }
        };
        var property = TestProjectBuilder.CreateStandardProject().Property;

        var result = RecurringCapExExpander.Expand(config, property, 2025, 2035);

        Assert.All(result, occ => Assert.True(occ.Amount.Amount > 0, $"Cost should be positive: {occ.Amount}"));
    }

    #endregion

    #region Intervall-Berechnung

    [Fact]
    public void Expand_Occurrences_AreRegularlySpaced()
    {
        var config = new CapExConfiguration
        {
            Measures = new[]
            {
                TestProjectBuilder.CreateRecurringMeasure("r1", CapExCategory.Heating, intervalPercent: 40)
            }
        };
        var property = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2060).Property;

        var result = RecurringCapExExpander.Expand(config, property, 2025, 2060);

        if (result.Count >= 2)
        {
            var interval = result[1].Year - result[0].Year;
            Assert.True(interval > 0, "Interval should be positive");

            // All intervals should be the same
            for (int i = 2; i < result.Count; i++)
            {
                var gap = result[i].Year - result[i - 1].Year;
                Assert.Equal(interval, gap);
            }
        }
    }

    [Fact]
    public void Expand_ShorterInterval_ProducesMoreOccurrences()
    {
        var property = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2060).Property;

        var configShort = new CapExConfiguration
        {
            Measures = new[]
            {
                TestProjectBuilder.CreateRecurringMeasure("r1", CapExCategory.Heating, intervalPercent: 20)
            }
        };
        var configLong = new CapExConfiguration
        {
            Measures = new[]
            {
                TestProjectBuilder.CreateRecurringMeasure("r1", CapExCategory.Heating, intervalPercent: 50)
            }
        };

        var resultShort = RecurringCapExExpander.Expand(configShort, property, 2025, 2060);
        var resultLong = RecurringCapExExpander.Expand(configLong, property, 2025, 2060);

        Assert.True(resultShort.Count >= resultLong.Count,
            $"Short interval ({resultShort.Count}) should produce >= occurrences than long ({resultLong.Count})");
    }

    [Fact]
    public void Expand_Occurrences_OnlyWithinPeriod()
    {
        var config = new CapExConfiguration
        {
            Measures = new[]
            {
                TestProjectBuilder.CreateRecurringMeasure("r1", CapExCategory.Heating, intervalPercent: 40)
            }
        };
        var property = TestProjectBuilder.CreateStandardProject().Property;

        var result = RecurringCapExExpander.Expand(config, property, 2025, 2035);

        Assert.All(result, occ =>
        {
            Assert.True(occ.Year >= 2025, $"Year {occ.Year} should be >= 2025");
            Assert.True(occ.Year <= 2035, $"Year {occ.Year} should be <= 2035");
        });
    }

    #endregion

    #region Kosten-Berechnung

    [Fact]
    public void Expand_CostIsBasedOnRenewalCost()
    {
        var config = new CapExConfiguration
        {
            Measures = new[]
            {
                TestProjectBuilder.CreateRecurringMeasure("r1", CapExCategory.Heating, costPercent: 25)
            }
        };
        var property = TestProjectBuilder.CreateStandardProject().Property;

        var result = RecurringCapExExpander.Expand(config, property, 2025, 2035);

        if (result.Count > 0)
        {
            var renewalCost = PropertyValueForecastCalculator.CalculateComponentRenewalCost(
                CapExCategory.Heating, property);
            var expected = Math.Round(renewalCost * 25m / 100 / 100) * 100; // Rounded to 100

            Assert.Equal(expected, result[0].Amount.Amount);
        }
    }

    [Fact]
    public void Expand_HigherCostPercent_HigherOccurrenceCost()
    {
        var property = TestProjectBuilder.CreateStandardProject().Property;

        var configLow = new CapExConfiguration
        {
            Measures = new[] { TestProjectBuilder.CreateRecurringMeasure("r1", CapExCategory.Heating, costPercent: 10) }
        };
        var configHigh = new CapExConfiguration
        {
            Measures = new[] { TestProjectBuilder.CreateRecurringMeasure("r1", CapExCategory.Heating, costPercent: 50) }
        };

        var resultLow = RecurringCapExExpander.Expand(configLow, property, 2025, 2035);
        var resultHigh = RecurringCapExExpander.Expand(configHigh, property, 2025, 2035);

        if (resultLow.Count > 0 && resultHigh.Count > 0)
        {
            Assert.True(resultHigh[0].Amount.Amount > resultLow[0].Amount.Amount);
        }
    }

    #endregion

    #region ExpandIntoConfiguration

    [Fact]
    public void ExpandIntoConfiguration_MergesBaseMeasures_WithRecurring()
    {
        var baseMeasure = TestProjectBuilder.CreateMeasure("base-1", CapExCategory.Windows, 30_000m, 2028);
        var recurringMeasure = TestProjectBuilder.CreateRecurringMeasure("r1", CapExCategory.Heating);

        var config = new CapExConfiguration
        {
            Measures = new[] { baseMeasure, recurringMeasure }
        };
        var property = TestProjectBuilder.CreateStandardProject().Property;

        var expanded = RecurringCapExExpander.ExpandIntoConfiguration(config, property, 2025, 2035);

        // Should contain both base measures AND expanded recurring occurrences
        Assert.Contains(expanded.Measures, m => m.Id == "base-1");
        Assert.Contains(expanded.Measures, m => m.Id == "r1"); // original recurring

        // Should also have generated measures with _recurring_ suffix
        var generated = expanded.Measures.Where(m => m.Id.Contains("_recurring_")).ToList();
        Assert.True(generated.Count > 0, "Should have generated recurring measures");
    }

    [Fact]
    public void ExpandIntoConfiguration_NullConfig_ReturnsEmpty()
    {
        var property = TestProjectBuilder.CreateStandardProject().Property;
        var result = RecurringCapExExpander.ExpandIntoConfiguration(null, property, 2025, 2035);

        Assert.Empty(result.Measures);
    }

    [Fact]
    public void ExpandIntoConfiguration_GeneratedIds_AreUnique()
    {
        var config = new CapExConfiguration
        {
            Measures = new[] { TestProjectBuilder.CreateRecurringMeasure("r1", CapExCategory.Heating) }
        };
        var property = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2060).Property;

        var expanded = RecurringCapExExpander.ExpandIntoConfiguration(config, property, 2025, 2060);
        var ids = expanded.Measures.Select(m => m.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    #endregion

    #region Naming

    [Fact]
    public void Expand_Occurrences_HaveNumberedNames()
    {
        var config = new CapExConfiguration
        {
            Measures = new[]
            {
                TestProjectBuilder.CreateRecurringMeasure("r1", CapExCategory.Heating, intervalPercent: 20)
            }
        };
        var property = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2060).Property;

        var result = RecurringCapExExpander.Expand(config, property, 2025, 2060);

        if (result.Count >= 2)
        {
            Assert.Contains("#1", result[0].Name);
            Assert.Contains("#2", result[1].Name);
        }
    }

    #endregion
}
