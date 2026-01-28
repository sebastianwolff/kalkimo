using Kalkimo.Domain.Calculators;
using Kalkimo.Domain.Models;
using Xunit;

namespace Kalkimo.Domain.Tests.GoldenTests;

/// <summary>
/// Golden Tests für die 15%-Regel (anschaffungsnahe Herstellungskosten)
/// </summary>
public class AcquisitionRelatedCostsTests
{
    private readonly TaxCalculator _calculator;

    public AcquisitionRelatedCostsTests()
    {
        _calculator = new TaxCalculator(new FixedDateProvider(new DateOnly(2025, 1, 1)));
    }

    [Fact]
    public void CheckAcquisitionRelatedCosts_DoesNotTrigger_WhenBelowThreshold()
    {
        // Arrange: Gebäudewert 300.000€, Schwelle 15% = 45.000€
        // Renovierung 40.000€ innerhalb 3 Jahren → unter Schwelle
        var purchase = CreatePurchase(400_000m, 100_000m, new DateOnly(2024, 1, 1));
        var measures = new[]
        {
            CreateMaintenanceMeasure(40_000m, new YearMonth(2024, 6))
        };

        // Act
        var result = _calculator.CheckAcquisitionRelatedCosts(purchase, measures);

        // Assert
        Assert.False(result.IsTriggered);
        Assert.Equal(45_000m, result.Threshold.Amount);
        Assert.Equal(40_000m, result.ActualCosts.Amount);
    }

    [Fact]
    public void CheckAcquisitionRelatedCosts_Triggers_WhenAboveThreshold()
    {
        // Arrange: Gebäudewert 300.000€, Schwelle 15% = 45.000€
        // Renovierung 50.000€ innerhalb 3 Jahren → über Schwelle
        var purchase = CreatePurchase(400_000m, 100_000m, new DateOnly(2024, 1, 1));
        var measures = new[]
        {
            CreateMaintenanceMeasure(30_000m, new YearMonth(2024, 6)),
            CreateMaintenanceMeasure(20_000m, new YearMonth(2025, 3))
        };

        // Act
        var result = _calculator.CheckAcquisitionRelatedCosts(purchase, measures);

        // Assert
        Assert.True(result.IsTriggered);
        Assert.Equal(45_000m, result.Threshold.Amount);
        Assert.Equal(50_000m, result.ActualCosts.Amount);
        Assert.Equal(5_000m, result.ExcessAmount.Amount);
    }

    [Fact]
    public void CheckAcquisitionRelatedCosts_IgnoresCosts_After3Years()
    {
        // Arrange: Renovierung nach 3-Jahres-Frist zählt nicht
        var purchase = CreatePurchase(400_000m, 100_000m, new DateOnly(2024, 1, 1));
        var measures = new[]
        {
            CreateMaintenanceMeasure(30_000m, new YearMonth(2024, 6)),  // Zählt
            CreateMaintenanceMeasure(50_000m, new YearMonth(2027, 6))   // Zählt NICHT (nach 3 Jahren)
        };

        // Act
        var result = _calculator.CheckAcquisitionRelatedCosts(purchase, measures);

        // Assert
        Assert.False(result.IsTriggered);
        Assert.Equal(30_000m, result.ActualCosts.Amount);
    }

    [Fact]
    public void CheckAcquisitionRelatedCosts_OnlyCountsMaintenanceExpenses()
    {
        // Arrange: Herstellungskosten zählen nicht zur 15%-Regel
        var purchase = CreatePurchase(400_000m, 100_000m, new DateOnly(2024, 1, 1));
        var measures = new[]
        {
            CreateMaintenanceMeasure(20_000m, new YearMonth(2024, 6)),
            new CapExMeasure
            {
                Id = "manufacturing",
                Name = "Dachgeschossausbau",
                Category = CapExCategory.Interior,
                PlannedPeriod = new YearMonth(2025, 1),
                EstimatedCost = Money.Euro(80_000m),
                TaxClassification = TaxClassification.ManufacturingCosts // Herstellungskosten
            }
        };

        // Act
        var result = _calculator.CheckAcquisitionRelatedCosts(purchase, measures);

        // Assert
        Assert.False(result.IsTriggered);
        Assert.Equal(20_000m, result.ActualCosts.Amount); // Nur Erhaltungsaufwand
    }

    [Fact]
    public void CheckAcquisitionRelatedCosts_IncludesDistributedMaintenance()
    {
        // Arrange: §82b-verteilter Erhaltungsaufwand zählt auch
        var purchase = CreatePurchase(400_000m, 100_000m, new DateOnly(2024, 1, 1));
        var measures = new[]
        {
            CreateMaintenanceMeasure(20_000m, new YearMonth(2024, 6)),
            new CapExMeasure
            {
                Id = "distributed",
                Name = "Fassadensanierung",
                Category = CapExCategory.Facade,
                PlannedPeriod = new YearMonth(2025, 1),
                EstimatedCost = Money.Euro(30_000m),
                TaxClassification = TaxClassification.MaintenanceExpenseDistributed,
                DistributionYears = 5
            }
        };

        // Act
        var result = _calculator.CheckAcquisitionRelatedCosts(purchase, measures);

        // Assert
        Assert.True(result.IsTriggered);
        Assert.Equal(50_000m, result.ActualCosts.Amount);
    }

    private static Purchase CreatePurchase(decimal purchasePrice, decimal landValue, DateOnly purchaseDate)
    {
        return new Purchase
        {
            PurchasePrice = Money.Euro(purchasePrice),
            LandValue = Money.Euro(landValue),
            PurchaseDate = purchaseDate,
            AcquisitionCosts = Array.Empty<AcquisitionCost>()
        };
    }

    private static CapExMeasure CreateMaintenanceMeasure(decimal cost, YearMonth period)
    {
        return new CapExMeasure
        {
            Id = $"measure-{period}",
            Name = "Erhaltungsaufwand",
            Category = CapExCategory.Other,
            PlannedPeriod = period,
            EstimatedCost = Money.Euro(cost),
            TaxClassification = TaxClassification.MaintenanceExpense
        };
    }
}
