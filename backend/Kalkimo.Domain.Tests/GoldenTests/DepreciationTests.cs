using Kalkimo.Domain.Calculators;
using Kalkimo.Domain.Models;
using Xunit;

namespace Kalkimo.Domain.Tests.GoldenTests;

/// <summary>
/// Golden Tests für AfA-Berechnung
/// </summary>
public class DepreciationTests
{
    private readonly TaxCalculator _calculator;

    public DepreciationTests()
    {
        _calculator = new TaxCalculator(new FixedDateProvider(new DateOnly(2025, 1, 1)));
    }

    [Theory]
    [InlineData(2024, 3.0)]   // Nach 31.12.2022 → 3%
    [InlineData(2023, 3.0)]   // Nach 31.12.2022 → 3%
    [InlineData(2022, 2.0)]   // Vor 2023, nach 1924 → 2%
    [InlineData(2000, 2.0)]   // Vor 2023, nach 1924 → 2%
    [InlineData(1950, 2.0)]   // Vor 2023, nach 1924 → 2%
    [InlineData(1924, 2.5)]   // Vor 1925 → 2.5%
    [InlineData(1900, 2.5)]   // Vor 1925 → 2.5%
    public void GetAnnualRate_ReturnsCorrectRate_BasedOnConstructionYear(
        int constructionYear, decimal expectedRate)
    {
        var rate = DepreciationRates.GetAnnualRate(constructionYear);
        Assert.Equal(expectedRate, rate);
    }

    [Fact]
    public void CalculateAnnualDepreciation_CorrectlyCalculates_ForNewBuilding()
    {
        // Arrange: Neubau 2024, Kaufpreis 500.000€, davon 100.000€ Boden
        var purchase = CreatePurchase(500_000m, 100_000m, new DateOnly(2024, 6, 1));
        var property = CreateProperty(2024);
        var taxProfile = CreateTaxProfile();

        // Act
        var annualAfa = _calculator.CalculateAnnualDepreciation(purchase, property, taxProfile);

        // Assert
        // Gebäudewert: 400.000€, AfA 3% = 12.000€ p.a.
        Assert.Equal(12_000m, annualAfa.Amount);
    }

    [Fact]
    public void CalculateAnnualDepreciation_CorrectlyCalculates_ForOldBuilding()
    {
        // Arrange: Altbau 1990, Kaufpreis 300.000€, davon 60.000€ Boden
        var purchase = CreatePurchase(300_000m, 60_000m, new DateOnly(2024, 1, 1));
        var property = CreateProperty(1990);
        var taxProfile = CreateTaxProfile();

        // Act
        var annualAfa = _calculator.CalculateAnnualDepreciation(purchase, property, taxProfile);

        // Assert
        // Gebäudewert: 240.000€, AfA 2% = 4.800€ p.a.
        Assert.Equal(4_800m, annualAfa.Amount);
    }

    [Fact]
    public void CalculateAnnualDepreciation_IncludesCapitalizableAcquisitionCosts()
    {
        // Arrange: Kaufpreis 400.000€, Boden 80.000€, Nebenkosten 20.000€ (aktivierbar)
        var purchase = new Purchase
        {
            PurchasePrice = Money.Euro(400_000m),
            LandValue = Money.Euro(80_000m),
            PurchaseDate = new DateOnly(2024, 1, 1),
            AcquisitionCosts = new[]
            {
                new AcquisitionCost { Type = AcquisitionCostType.Notary, Amount = Money.Euro(5_000m), IsCapitalizable = true },
                new AcquisitionCost { Type = AcquisitionCostType.TransferTax, Amount = Money.Euro(15_000m), IsCapitalizable = true }
            }
        };
        var property = CreateProperty(2024);
        var taxProfile = CreateTaxProfile();

        // Act
        var annualAfa = _calculator.CalculateAnnualDepreciation(purchase, property, taxProfile);

        // Assert
        // Gebäudewert: 320.000€ + 20.000€ NK = 340.000€ Bemessungsgrundlage
        // AfA 3% = 10.200€ p.a.
        Assert.Equal(10_200m, annualAfa.Amount);
    }

    [Fact]
    public void CalculateAnnualDepreciation_UsesCustomRate_WhenProvided()
    {
        // Arrange: Gutachten mit kürzerer Nutzungsdauer → 4% p.a.
        var purchase = CreatePurchase(400_000m, 80_000m, new DateOnly(2024, 1, 1));
        var property = CreateProperty(2000);
        var taxProfile = new TaxProfile
        {
            OwnershipType = OwnershipType.PrivateIndividual,
            MarginalTaxRatePercent = 42m,
            CustomDepreciationRatePercent = 4m // Gutachten: 25 Jahre Restnutzungsdauer
        };

        // Act
        var annualAfa = _calculator.CalculateAnnualDepreciation(purchase, property, taxProfile);

        // Assert
        // Gebäudewert: 320.000€, AfA 4% = 12.800€ p.a.
        Assert.Equal(12_800m, annualAfa.Amount);
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

    private static Property CreateProperty(int constructionYear)
    {
        return new Property
        {
            Id = "test-property",
            Type = PropertyType.MultiFamilyHome,
            ConstructionYear = constructionYear,
            OverallCondition = Condition.Good,
            TotalArea = 500m,
            LivingArea = 400m
        };
    }

    private static TaxProfile CreateTaxProfile()
    {
        return new TaxProfile
        {
            OwnershipType = OwnershipType.PrivateIndividual,
            MarginalTaxRatePercent = 42m
        };
    }
}
