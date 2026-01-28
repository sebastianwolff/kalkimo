using Kalkimo.Domain.Calculators;
using Kalkimo.Domain.Models;
using Xunit;

namespace Kalkimo.Domain.Tests.GoldenTests;

/// <summary>
/// Golden Tests für §82b EStDV (Verteilung größerer Erhaltungsaufwendungen)
/// </summary>
public class MaintenanceDistributionTests
{
    private readonly TaxCalculator _calculator;

    public MaintenanceDistributionTests()
    {
        _calculator = new TaxCalculator(new FixedDateProvider(new DateOnly(2025, 1, 1)));
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    [InlineData(1, false)]
    [InlineData(6, false)]
    public void IsValidDistribution_ValidatesYearRange(int years, bool expected)
    {
        var result = MaintenanceDistributionRule.IsValidDistribution(years);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateAnnualDeduction_DistributesEvenly_Over5Years()
    {
        // Arrange: 50.000€ Erhaltungsaufwand über 5 Jahre verteilen
        var measure = new CapExMeasure
        {
            Id = "facade",
            Name = "Fassadensanierung",
            Category = CapExCategory.Facade,
            PlannedPeriod = new YearMonth(2025, 6),
            EstimatedCost = Money.Euro(50_000m),
            TaxClassification = TaxClassification.MaintenanceExpenseDistributed,
            DistributionYears = 5
        };

        // Act
        var deductions = _calculator.CalculateMaintenanceDistribution(measure, 5);

        // Assert
        Assert.Equal(5, deductions.Count);
        Assert.All(deductions, d => Assert.Equal(10_000m, d.Amount.Amount));
        Assert.Equal(2025, deductions[0].Year);
        Assert.Equal(2029, deductions[4].Year);
    }

    [Fact]
    public void CalculateAnnualDeduction_DistributesEvenly_Over2Years()
    {
        // Arrange: 30.000€ über 2 Jahre
        var measure = new CapExMeasure
        {
            Id = "heating",
            Name = "Heizungstausch",
            Category = CapExCategory.Heating,
            PlannedPeriod = new YearMonth(2025, 1),
            EstimatedCost = Money.Euro(30_000m),
            TaxClassification = TaxClassification.MaintenanceExpenseDistributed,
            DistributionYears = 2
        };

        // Act
        var deductions = _calculator.CalculateMaintenanceDistribution(measure, 2);

        // Assert
        Assert.Equal(2, deductions.Count);
        Assert.All(deductions, d => Assert.Equal(15_000m, d.Amount.Amount));
    }

    [Fact]
    public void CalculateAnnualDeduction_HandlesOddAmounts()
    {
        // Arrange: 10.000€ über 3 Jahre → 3.333,33€ pro Jahr
        var measure = new CapExMeasure
        {
            Id = "windows",
            Name = "Fenstertausch",
            Category = CapExCategory.Windows,
            PlannedPeriod = new YearMonth(2025, 3),
            EstimatedCost = Money.Euro(10_000m),
            TaxClassification = TaxClassification.MaintenanceExpenseDistributed,
            DistributionYears = 3
        };

        // Act
        var deductions = _calculator.CalculateMaintenanceDistribution(measure, 3);

        // Assert
        Assert.Equal(3, deductions.Count);
        // Rundung auf 2 Nachkommastellen
        Assert.Equal(3333.33m, deductions[0].Amount.Amount);
    }

    [Fact]
    public void CalculateAnnualDeduction_ThrowsForInvalidYears()
    {
        var measure = new CapExMeasure
        {
            Id = "test",
            Name = "Test",
            Category = CapExCategory.Other,
            PlannedPeriod = new YearMonth(2025, 1),
            EstimatedCost = Money.Euro(10_000m),
            TaxClassification = TaxClassification.MaintenanceExpenseDistributed
        };

        // Invalid: 1 Jahr
        Assert.Throws<ArgumentException>(() =>
            _calculator.CalculateMaintenanceDistribution(measure, 1));

        // Invalid: 6 Jahre
        Assert.Throws<ArgumentException>(() =>
            _calculator.CalculateMaintenanceDistribution(measure, 6));
    }
}
