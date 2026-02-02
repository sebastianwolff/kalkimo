using Kalkimo.Domain.Calculators;
using Kalkimo.Domain.Models;
using Kalkimo.Domain.Tests.TestHelpers;

namespace Kalkimo.Domain.Tests.GoldenTests;

/// <summary>
/// Golden Tests für Unit-Level-Bauteile (Mieteinheit-Ebene).
/// Testet RenovationForecastGenerator, PropertyValueForecastCalculator
/// und RecurringCapExExpander mit Einheit-Komponenten (Kitchen, Bathroom,
/// UnitRenovation, UnitOther).
/// </summary>
public class UnitComponentTests
{
    #region CapExCategory Extensions

    [Theory]
    [InlineData(CapExCategory.Kitchen, true)]
    [InlineData(CapExCategory.Bathroom, true)]
    [InlineData(CapExCategory.UnitRenovation, true)]
    [InlineData(CapExCategory.UnitOther, true)]
    [InlineData(CapExCategory.Heating, false)]
    [InlineData(CapExCategory.Roof, false)]
    [InlineData(CapExCategory.Facade, false)]
    [InlineData(CapExCategory.Windows, false)]
    [InlineData(CapExCategory.Electrical, false)]
    [InlineData(CapExCategory.Plumbing, false)]
    public void IsUnitLevel_ReturnsCorrectResult(CapExCategory category, bool expected)
    {
        Assert.Equal(expected, category.IsUnitLevel());
    }

    [Theory]
    [InlineData(CapExCategory.Kitchen)]
    [InlineData(CapExCategory.Bathroom)]
    [InlineData(CapExCategory.UnitRenovation)]
    [InlineData(CapExCategory.UnitOther)]
    public void IsBuildingLevel_UnitCategories_ReturnFalse(CapExCategory category)
    {
        Assert.False(category.IsBuildingLevel());
    }

    #endregion

    #region DefaultComponentCycles

    [Fact]
    public void DefaultComponentCycles_Kitchen_HasExpectedRange()
    {
        var (min, max, costMin, costMax) = DefaultComponentCycles.GetCycle(CapExCategory.Kitchen);

        Assert.Equal(15, min);
        Assert.Equal(25, max);
        Assert.Equal(80m, costMin.Amount);
        Assert.Equal(250m, costMax.Amount);
    }

    [Fact]
    public void DefaultComponentCycles_Bathroom_HasExpectedRange()
    {
        var (min, max, costMin, costMax) = DefaultComponentCycles.GetCycle(CapExCategory.Bathroom);

        Assert.Equal(20, min);
        Assert.Equal(30, max);
        Assert.Equal(120m, costMin.Amount);
        Assert.Equal(350m, costMax.Amount);
    }

    [Fact]
    public void DefaultComponentCycles_UnitRenovation_HasExpectedRange()
    {
        var (min, max, costMin, costMax) = DefaultComponentCycles.GetCycle(CapExCategory.UnitRenovation);

        Assert.Equal(10, min);
        Assert.Equal(20, max);
        Assert.Equal(40m, costMin.Amount);
        Assert.Equal(150m, costMax.Amount);
    }

    [Fact]
    public void DefaultComponentCycles_UnitOther_HasExpectedRange()
    {
        var (min, max, costMin, costMax) = DefaultComponentCycles.GetCycle(CapExCategory.UnitOther);

        Assert.Equal(15, min);
        Assert.Equal(30, max);
        Assert.Equal(20m, costMin.Amount);
        Assert.Equal(80m, costMax.Amount);
    }

    #endregion

    #region RenovationForecastGenerator - Unit Forecast

    [Fact]
    public void GenerateForecast_WithUnitComponents_IncludesUnitMeasures()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();

        var forecast = RenovationForecastGenerator.GenerateForecast(
            project.Property,
            new YearMonth(2025, 1),
            new YearMonth(2045, 12));

        // Should include both building-level and unit-level measures
        var unitMeasures = forecast.Where(m => m.UnitId != null).ToList();
        Assert.True(unitMeasures.Count > 0, "Should generate unit-level measures");
    }

    [Fact]
    public void GenerateForecast_UnitMeasures_HaveCorrectUnitId()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();

        var forecast = RenovationForecastGenerator.GenerateForecast(
            project.Property,
            new YearMonth(2025, 1),
            new YearMonth(2045, 12));

        var unitMeasures = forecast.Where(m => m.UnitId != null).ToList();

        // All unit measures should reference one of the defined units
        Assert.All(unitMeasures, m =>
            Assert.True(m.UnitId == "unit-1" || m.UnitId == "unit-2",
                $"UnitId '{m.UnitId}' should be unit-1 or unit-2"));
    }

    [Fact]
    public void GenerateForecast_UnitMeasures_HaveUnitLevelCategories()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();

        var forecast = RenovationForecastGenerator.GenerateForecast(
            project.Property,
            new YearMonth(2025, 1),
            new YearMonth(2045, 12));

        var unitMeasures = forecast.Where(m => m.UnitId != null).ToList();

        Assert.All(unitMeasures, m =>
            Assert.True(m.Category.IsUnitLevel(),
                $"Unit measure category {m.Category} should be unit-level"));
    }

    [Fact]
    public void GenerateForecast_UnitMeasures_NameIncludesUnitName()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();

        var forecast = RenovationForecastGenerator.GenerateForecast(
            project.Property,
            new YearMonth(2025, 1),
            new YearMonth(2045, 12));

        var unitMeasures = forecast.Where(m => m.UnitId != null).ToList();

        // Measures for unit-1 should contain "WE 1" in their name
        var unit1Measures = unitMeasures.Where(m => m.UnitId == "unit-1").ToList();
        if (unit1Measures.Count > 0)
        {
            Assert.All(unit1Measures, m =>
                Assert.Contains("WE 1", m.Name));
        }
    }

    [Fact]
    public void GenerateForecast_BuildingMeasures_HaveNullUnitId()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();

        var forecast = RenovationForecastGenerator.GenerateForecast(
            project.Property,
            new YearMonth(2025, 1),
            new YearMonth(2045, 12));

        var buildingMeasures = forecast.Where(m => m.Category.IsBuildingLevel()).ToList();

        Assert.All(buildingMeasures, m =>
            Assert.Null(m.UnitId));
    }

    [Fact]
    public void GenerateForecast_WithoutUnitComponents_NoUnitMeasures()
    {
        var project = TestProjectBuilder.CreateStandardProject();
        // Standard project has no unit components

        var forecast = RenovationForecastGenerator.GenerateForecast(
            project.Property,
            new YearMonth(2025, 1),
            new YearMonth(2045, 12));

        var unitMeasures = forecast.Where(m => m.UnitId != null).ToList();
        Assert.Empty(unitMeasures);
    }

    [Fact]
    public void GenerateForecast_UnitMeasures_SortedByPriorityThenYear()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();

        var forecast = RenovationForecastGenerator.GenerateForecast(
            project.Property,
            new YearMonth(2025, 1),
            new YearMonth(2045, 12));

        // Verify overall sort: priority first, then year
        for (int i = 1; i < forecast.Count; i++)
        {
            var prev = forecast[i - 1];
            var curr = forecast[i];

            if (prev.Priority == curr.Priority)
            {
                Assert.True(prev.PlannedPeriod.Year <= curr.PlannedPeriod.Year ||
                            prev.PlannedPeriod.Month <= curr.PlannedPeriod.Month,
                    "Within same priority, measures should be chronologically ordered");
            }
        }
    }

    #endregion

    #region PropertyValueForecastCalculator - Unit Components

    [Fact]
    public void Calculate_WithUnitComponents_IncludesInConditionFactor()
    {
        var projectWithout = TestProjectBuilder.CreateStandardProject();
        var projectWith = TestProjectBuilder.CreateStandardProject().WithUnitComponents();

        var resultWithout = PropertyValueForecastCalculator.Calculate(projectWithout, 2025, 2035);
        var resultWith = PropertyValueForecastCalculator.Calculate(projectWith, 2025, 2035);

        // Adding unit components should change the initial condition factor
        // (because it averages over more components)
        Assert.NotEqual(resultWithout.InitialConditionFactor, resultWith.InitialConditionFactor);
    }

    [Fact]
    public void Calculate_WithUnitComponents_HasDeteriorationRows_WithUnitId()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        Assert.NotNull(result.ComponentDeterioration);

        var unitRows = result.ComponentDeterioration.Components
            .Where(r => r.UnitId != null).ToList();

        Assert.True(unitRows.Count > 0, "Should have deterioration rows with UnitId");
    }

    [Fact]
    public void Calculate_WithUnitComponents_DeteriorationRows_HaveCorrectUnitIds()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        Assert.NotNull(result.ComponentDeterioration);

        var unitRows = result.ComponentDeterioration.Components
            .Where(r => r.UnitId != null).ToList();

        Assert.All(unitRows, r =>
            Assert.True(r.UnitId == "unit-1" || r.UnitId == "unit-2",
                $"UnitId '{r.UnitId}' should be unit-1 or unit-2"));
    }

    [Fact]
    public void Calculate_WithUnitComponents_BuildingRows_HaveNullUnitId()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();
        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        Assert.NotNull(result.ComponentDeterioration);

        var buildingRows = result.ComponentDeterioration.Components
            .Where(r => r.Category.IsBuildingLevel()).ToList();

        Assert.All(buildingRows, r => Assert.Null(r.UnitId));
    }

    [Fact]
    public void Calculate_WithUnitComponents_HasMoreDeteriorationRows_ThanWithout()
    {
        var projectWithout = TestProjectBuilder.CreateStandardProject();
        var projectWith = TestProjectBuilder.CreateStandardProject().WithUnitComponents();

        var resultWithout = PropertyValueForecastCalculator.Calculate(projectWithout, 2025, 2035);
        var resultWith = PropertyValueForecastCalculator.Calculate(projectWith, 2025, 2035);

        Assert.NotNull(resultWithout.ComponentDeterioration);
        Assert.NotNull(resultWith.ComponentDeterioration);

        Assert.True(
            resultWith.ComponentDeterioration.Components.Count >
            resultWithout.ComponentDeterioration.Components.Count,
            "Unit components should add deterioration rows");
    }

    [Fact]
    public void Calculate_WithUnitComponents_TotalRenewalCost_Increases()
    {
        var projectWithout = TestProjectBuilder.CreateStandardProject();
        var projectWith = TestProjectBuilder.CreateStandardProject().WithUnitComponents();

        var resultWithout = PropertyValueForecastCalculator.Calculate(projectWithout, 2025, 2035);
        var resultWith = PropertyValueForecastCalculator.Calculate(projectWith, 2025, 2035);

        Assert.NotNull(resultWithout.ComponentDeterioration);
        Assert.NotNull(resultWith.ComponentDeterioration);

        Assert.True(
            resultWith.ComponentDeterioration.TotalRenewalCostIfAllDone >
            resultWithout.ComponentDeterioration.TotalRenewalCostIfAllDone,
            "Adding unit components should increase total renewal cost");
    }

    #endregion

    #region CalculateComponentRenewalCost - Unit Area

    [Fact]
    public void CalculateComponentRenewalCost_Kitchen_UsesUnitArea()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();
        var unit = project.Property.Units[0]; // WE 1, 85m²

        var cost = PropertyValueForecastCalculator.CalculateComponentRenewalCost(
            CapExCategory.Kitchen, project.Property, unit);

        // Kitchen: max cost 250€/m², unit area 85m²
        // Expected: round(250 * 85 / 100) * 100 = round(212.5) * 100 = 21_300
        var (_, _, _, costMax) = DefaultComponentCycles.GetCycle(CapExCategory.Kitchen);
        var expected = Math.Round(costMax.Amount * unit.Area / 100) * 100;
        Assert.Equal(expected, cost);
    }

    [Fact]
    public void CalculateComponentRenewalCost_UnitCategory_WithoutUnit_UsesLivingArea()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var cost = PropertyValueForecastCalculator.CalculateComponentRenewalCost(
            CapExCategory.Kitchen, project.Property);

        // Without unit, falls through to default (livingArea)
        Assert.True(cost > 0);
    }

    [Fact]
    public void CalculateComponentRenewalCost_SmallerUnit_HasLowerCost()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();
        var unit1 = project.Property.Units[0]; // 85m²
        var unit2 = project.Property.Units[1]; // 65m²

        var cost1 = PropertyValueForecastCalculator.CalculateComponentRenewalCost(
            CapExCategory.Bathroom, project.Property, unit1);
        var cost2 = PropertyValueForecastCalculator.CalculateComponentRenewalCost(
            CapExCategory.Bathroom, project.Property, unit2);

        Assert.True(cost1 > cost2,
            $"Larger unit (85m²) cost ({cost1}) should exceed smaller unit (65m²) cost ({cost2})");
    }

    [Fact]
    public void CalculateComponentRenewalCost_BuildingCategory_IgnoresUnit()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();
        var unit = project.Property.Units[0];

        // Building category should not use unit area even if unit is provided
        var costWithUnit = PropertyValueForecastCalculator.CalculateComponentRenewalCost(
            CapExCategory.Heating, project.Property, unit);
        var costWithoutUnit = PropertyValueForecastCalculator.CalculateComponentRenewalCost(
            CapExCategory.Heating, project.Property);

        Assert.Equal(costWithoutUnit, costWithUnit);
    }

    #endregion

    #region CapEx Measures with UnitId - Deterioration Matching

    [Fact]
    public void Calculate_UnitCapEx_MatchesByUnitId()
    {
        var project = TestProjectBuilder.CreateStandardProject()
            .WithUnitComponents()
            .WithCapEx(
                TestProjectBuilder.CreateUnitMeasure(
                    "kitchen-1", CapExCategory.Kitchen, 15_000m, 2028, unitId: "unit-1"));

        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        Assert.NotNull(result.ComponentDeterioration);

        // The Kitchen row for unit-1 should show as "Renewed"
        var unit1Kitchen = result.ComponentDeterioration.Components
            .FirstOrDefault(r => r.Category == CapExCategory.Kitchen && r.UnitId == "unit-1");

        Assert.NotNull(unit1Kitchen);
        Assert.Equal("Renewed", unit1Kitchen.StatusAtEnd);
        Assert.Equal(2028, unit1Kitchen.CapexAddressedYear);
    }

    [Fact]
    public void Calculate_UnitCapEx_DoesNotMatchDifferentUnit()
    {
        var project = TestProjectBuilder.CreateStandardProject()
            .WithUnitComponents()
            .WithCapEx(
                TestProjectBuilder.CreateUnitMeasure(
                    "kitchen-1", CapExCategory.Kitchen, 15_000m, 2028, unitId: "unit-1"));

        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        Assert.NotNull(result.ComponentDeterioration);

        // Kitchen for unit-2 should NOT be renewed (capex was for unit-1)
        var unit2Kitchen = result.ComponentDeterioration.Components
            .FirstOrDefault(r => r.Category == CapExCategory.Kitchen && r.UnitId == "unit-2");

        Assert.NotNull(unit2Kitchen);
        Assert.NotEqual("Renewed", unit2Kitchen.StatusAtEnd);
        Assert.Null(unit2Kitchen.CapexAddressedYear);
    }

    [Fact]
    public void Calculate_BuildingCapEx_DoesNotMatchUnitComponents()
    {
        var project = TestProjectBuilder.CreateStandardProject()
            .WithUnitComponents()
            .WithCapEx(
                TestProjectBuilder.CreateMeasure("heat-1", CapExCategory.Heating, 25_000m, 2027));

        var result = PropertyValueForecastCalculator.Calculate(project, 2025, 2035);

        Assert.NotNull(result.ComponentDeterioration);

        // Building-level heating CapEx should not affect unit-level components
        var unitRows = result.ComponentDeterioration.Components
            .Where(r => r.UnitId != null).ToList();

        Assert.All(unitRows, r =>
            Assert.Null(r.CapexAddressedYear));
    }

    #endregion

    #region RecurringCapExExpander - Unit-Level

    [Fact]
    public void Expand_UnitLevelRecurring_HasUnitId()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();
        var config = new CapExConfiguration
        {
            Measures = new[]
            {
                new CapExMeasure
                {
                    Id = "r-kitchen",
                    Name = "Wartung Küche",
                    Category = CapExCategory.Kitchen,
                    PlannedPeriod = new YearMonth(2025, 1),
                    EstimatedCost = Money.Euro(0),
                    TaxClassification = TaxClassification.MaintenanceExpense,
                    IsExecuted = true,
                    IsNecessary = false,
                    Priority = MeasurePriority.Medium,
                    IsRecurring = true,
                    UnitId = "unit-1",
                    RecurringConfig = new RecurringMeasureConfig
                    {
                        IntervalPercent = 40,
                        CostPercent = 25,
                        CycleExtensionPercent = 30
                    }
                }
            }
        };

        var result = RecurringCapExExpander.Expand(config, project.Property, 2025, 2045);

        Assert.True(result.Count > 0, "Should generate occurrences for unit-level recurring");
        Assert.All(result, occ => Assert.Equal("unit-1", occ.UnitId));
    }

    [Fact]
    public void Expand_UnitLevelRecurring_UsesUnitComponent()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();

        // Unit-1 Kitchen: lastReno 2010, cycle 20 years
        var config = new CapExConfiguration
        {
            Measures = new[]
            {
                new CapExMeasure
                {
                    Id = "r-kitchen",
                    Name = "Wartung Küche",
                    Category = CapExCategory.Kitchen,
                    PlannedPeriod = new YearMonth(2025, 1),
                    EstimatedCost = Money.Euro(0),
                    TaxClassification = TaxClassification.MaintenanceExpense,
                    IsExecuted = true,
                    IsNecessary = false,
                    Priority = MeasurePriority.Medium,
                    IsRecurring = true,
                    UnitId = "unit-1",
                    RecurringConfig = new RecurringMeasureConfig
                    {
                        IntervalPercent = 40,
                        CostPercent = 25,
                        CycleExtensionPercent = 30
                    }
                }
            }
        };

        var result = RecurringCapExExpander.Expand(config, project.Property, 2025, 2045);

        Assert.True(result.Count > 0);
        // Cost should be based on unit-1 area (85m²), not property living area (400m²)
        var renewalCostUnit = PropertyValueForecastCalculator.CalculateComponentRenewalCost(
            CapExCategory.Kitchen, project.Property, project.Property.Units[0]);
        var expectedCost = Math.Round(renewalCostUnit * 25m / 100 / 100) * 100;

        Assert.Equal(expectedCost, result[0].Amount.Amount);
    }

    [Fact]
    public void Expand_UnitLevelRecurring_CostLowerThan_BuildingLevel()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();

        // Same category + config but unit-level vs building-level
        var unitConfig = new CapExConfiguration
        {
            Measures = new[]
            {
                new CapExMeasure
                {
                    Id = "r-unit",
                    Name = "Wartung Küche (Unit)",
                    Category = CapExCategory.Kitchen,
                    PlannedPeriod = new YearMonth(2025, 1),
                    EstimatedCost = Money.Euro(0),
                    TaxClassification = TaxClassification.MaintenanceExpense,
                    IsExecuted = true,
                    IsNecessary = false,
                    Priority = MeasurePriority.Medium,
                    IsRecurring = true,
                    UnitId = "unit-1", // 85m²
                    RecurringConfig = new RecurringMeasureConfig
                    {
                        IntervalPercent = 40,
                        CostPercent = 25,
                        CycleExtensionPercent = 30
                    }
                }
            }
        };

        var buildingConfig = new CapExConfiguration
        {
            Measures = new[]
            {
                new CapExMeasure
                {
                    Id = "r-building",
                    Name = "Wartung Küche (Building)",
                    Category = CapExCategory.Kitchen,
                    PlannedPeriod = new YearMonth(2025, 1),
                    EstimatedCost = Money.Euro(0),
                    TaxClassification = TaxClassification.MaintenanceExpense,
                    IsExecuted = true,
                    IsNecessary = false,
                    Priority = MeasurePriority.Medium,
                    IsRecurring = true,
                    UnitId = null, // Building level → uses livingArea 400m²
                    RecurringConfig = new RecurringMeasureConfig
                    {
                        IntervalPercent = 40,
                        CostPercent = 25,
                        CycleExtensionPercent = 30
                    }
                }
            }
        };

        var unitResult = RecurringCapExExpander.Expand(unitConfig, project.Property, 2025, 2045);
        var buildingResult = RecurringCapExExpander.Expand(buildingConfig, project.Property, 2025, 2045);

        if (unitResult.Count > 0 && buildingResult.Count > 0)
        {
            Assert.True(unitResult[0].Amount.Amount < buildingResult[0].Amount.Amount,
                $"Unit cost ({unitResult[0].Amount.Amount}) should be less than building cost ({buildingResult[0].Amount.Amount})");
        }
    }

    [Fact]
    public void ExpandIntoConfiguration_UnitLevel_PreservesUnitId()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();
        var config = new CapExConfiguration
        {
            Measures = new[]
            {
                new CapExMeasure
                {
                    Id = "r-bath",
                    Name = "Wartung Bad",
                    Category = CapExCategory.Bathroom,
                    PlannedPeriod = new YearMonth(2025, 1),
                    EstimatedCost = Money.Euro(0),
                    TaxClassification = TaxClassification.MaintenanceExpense,
                    IsExecuted = true,
                    IsNecessary = false,
                    Priority = MeasurePriority.Medium,
                    IsRecurring = true,
                    UnitId = "unit-2",
                    RecurringConfig = new RecurringMeasureConfig
                    {
                        IntervalPercent = 40,
                        CostPercent = 25,
                        CycleExtensionPercent = 30
                    }
                }
            }
        };

        var expanded = RecurringCapExExpander.ExpandIntoConfiguration(config, project.Property, 2025, 2045);

        var generated = expanded.Measures.Where(m => m.Id.Contains("_recurring_")).ToList();
        Assert.True(generated.Count > 0, "Should have generated measures");
        Assert.All(generated, m => Assert.Equal("unit-2", m.UnitId));
    }

    #endregion

    #region Unit on Property

    [Fact]
    public void WithUnitComponents_CreatesExpectedUnits()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();

        Assert.Equal(2, project.Property.Units.Count);
        Assert.Equal("unit-1", project.Property.Units[0].Id);
        Assert.Equal("unit-2", project.Property.Units[1].Id);
        Assert.Equal(85m, project.Property.Units[0].Area);
        Assert.Equal(65m, project.Property.Units[1].Area);
    }

    [Fact]
    public void WithUnitComponents_UnitsHaveComponents()
    {
        var project = TestProjectBuilder.CreateStandardProject().WithUnitComponents();

        Assert.Equal(3, project.Property.Units[0].Components.Count);
        Assert.Equal(3, project.Property.Units[1].Components.Count);

        // Unit 1 has Kitchen, Bathroom, UnitRenovation
        Assert.Contains(project.Property.Units[0].Components,
            c => c.Category == CapExCategory.Kitchen);
        Assert.Contains(project.Property.Units[0].Components,
            c => c.Category == CapExCategory.Bathroom);
        Assert.Contains(project.Property.Units[0].Components,
            c => c.Category == CapExCategory.UnitRenovation);

        // Unit 2 has Kitchen, Bathroom, UnitOther
        Assert.Contains(project.Property.Units[1].Components,
            c => c.Category == CapExCategory.Kitchen);
        Assert.Contains(project.Property.Units[1].Components,
            c => c.Category == CapExCategory.Bathroom);
        Assert.Contains(project.Property.Units[1].Components,
            c => c.Category == CapExCategory.UnitOther);
    }

    #endregion
}
