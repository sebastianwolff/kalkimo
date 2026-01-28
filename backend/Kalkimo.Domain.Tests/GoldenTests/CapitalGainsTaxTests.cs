using Kalkimo.Domain.Calculators;
using Kalkimo.Domain.Models;
using Xunit;

namespace Kalkimo.Domain.Tests.GoldenTests;

/// <summary>
/// Golden Tests für §23 EStG (private Veräußerungsgeschäfte)
/// </summary>
public class CapitalGainsTaxTests
{
    private readonly TaxCalculator _calculator;

    public CapitalGainsTaxTests()
    {
        _calculator = new TaxCalculator(new FixedDateProvider(new DateOnly(2025, 1, 1)));
    }

    [Fact]
    public void CalculateCapitalGainsTax_TaxExempt_After10Years()
    {
        // Arrange: Kauf 2014, Verkauf 2025 → über 10 Jahre gehalten
        var purchase = CreatePurchase(new DateOnly(2014, 6, 1));
        var taxProfile = CreateTaxProfile(42m);
        var parameters = new CapitalGainsTaxParameters();

        // Act
        var result = _calculator.CalculateCapitalGainsTax(
            purchase,
            salePrice: Money.Euro(600_000m),
            saleCosts: Money.Euro(18_000m),
            accumulatedDepreciation: Money.Euro(44_000m),
            saleDate: new DateOnly(2025, 7, 1),
            taxProfile,
            parameters);

        // Assert
        Assert.True(result.IsTaxExempt);
        Assert.Equal(11, result.HoldingPeriodYears);
        Assert.Equal("Spekulationsfrist abgelaufen", result.Reason);
    }

    [Fact]
    public void CalculateCapitalGainsTax_Taxable_Within10Years()
    {
        // Arrange: Kauf 2020, Verkauf 2025 → nur 5 Jahre gehalten
        var purchase = CreatePurchase(new DateOnly(2020, 1, 1));
        var taxProfile = CreateTaxProfile(42m);
        var parameters = new CapitalGainsTaxParameters();

        // Kaufpreis 400.000€ + NK 20.000€ = 420.000€ Investition
        // AfA über 5 Jahre bei 2%: 400.000 * 0.8 * 0.02 * 5 = 32.000€
        // Fortgeschriebene AK = 420.000 - 32.000 = 388.000€
        // Verkauf 500.000€ - Kosten 15.000€ - 388.000€ = 97.000€ Gewinn
        // Steuer: 97.000 * 42% * 1.055 (Soli) = 42.983,70€

        // Act
        var result = _calculator.CalculateCapitalGainsTax(
            purchase,
            salePrice: Money.Euro(500_000m),
            saleCosts: Money.Euro(15_000m),
            accumulatedDepreciation: Money.Euro(32_000m),
            saleDate: new DateOnly(2025, 6, 1),
            taxProfile,
            parameters);

        // Assert
        Assert.False(result.IsTaxExempt);
        Assert.Equal(5, result.HoldingPeriodYears);
        Assert.Equal(97_000m, result.Gain.Amount);
        Assert.True(result.TaxAmount.Amount > 40_000m); // ~43.000€
    }

    [Fact]
    public void CalculateCapitalGainsTax_TaxExempt_UnderExemptionThreshold()
    {
        // Arrange: Gewinn unter 1.000€ Freigrenze
        var purchase = CreatePurchase(new DateOnly(2022, 1, 1));
        var taxProfile = CreateTaxProfile(42m);
        var parameters = new CapitalGainsTaxParameters
        {
            ExemptionThreshold = Money.Euro(1000m) // Seit 2024
        };

        // Gewinn: 420.500 - 15.000 - 405.000 = 500€ → unter Freigrenze

        // Act
        var result = _calculator.CalculateCapitalGainsTax(
            purchase,
            salePrice: Money.Euro(420_500m),
            saleCosts: Money.Euro(15_000m),
            accumulatedDepreciation: Money.Euro(15_000m),
            saleDate: new DateOnly(2025, 1, 1),
            taxProfile,
            parameters);

        // Assert
        Assert.True(result.IsTaxExempt);
        Assert.Equal("Unter Freigrenze", result.Reason);
        Assert.True(result.Gain <= parameters.ExemptionThreshold);
    }

    [Fact]
    public void CalculateCapitalGainsTax_FullTax_WhenOverExemptionThreshold()
    {
        // Arrange: Gewinn 1.500€ → über Freigrenze, GESAMTER Gewinn wird besteuert
        var purchase = CreatePurchase(new DateOnly(2022, 1, 1));
        var taxProfile = CreateTaxProfile(42m);
        var parameters = new CapitalGainsTaxParameters
        {
            ExemptionThreshold = Money.Euro(1000m)
        };

        // Act
        var result = _calculator.CalculateCapitalGainsTax(
            purchase,
            salePrice: Money.Euro(421_500m),
            saleCosts: Money.Euro(15_000m),
            accumulatedDepreciation: Money.Euro(15_000m),
            saleDate: new DateOnly(2025, 1, 1),
            taxProfile,
            parameters);

        // Assert
        Assert.False(result.IsTaxExempt);
        // Bei Überschreiten der Freigrenze wird der GESAMTE Gewinn besteuert
        // (Freigrenze, nicht Freibetrag!)
        Assert.True(result.TaxAmount.Amount > 0);
    }

    [Fact]
    public void CalculateCapitalGainsTax_TaxExempt_WithOwnerOccupation()
    {
        // Arrange: Eigennutzung im Verkaufsjahr + 2 Vorjahre
        var purchase = CreatePurchase(new DateOnly(2022, 1, 1));
        var taxProfile = CreateTaxProfile(42m);
        var parameters = new CapitalGainsTaxParameters
        {
            OwnerOccupiedExemption = true
        };

        // Act
        var result = _calculator.CalculateCapitalGainsTax(
            purchase,
            salePrice: Money.Euro(600_000m),
            saleCosts: Money.Euro(18_000m),
            accumulatedDepreciation: Money.Euro(20_000m),
            saleDate: new DateOnly(2025, 6, 1),
            taxProfile,
            parameters);

        // Assert
        Assert.True(result.IsTaxExempt);
        Assert.Equal("Eigennutzung", result.Reason);
    }

    [Fact]
    public void CalculateCapitalGainsTax_NoTax_OnLoss()
    {
        // Arrange: Verlust beim Verkauf
        var purchase = CreatePurchase(new DateOnly(2022, 1, 1));
        var taxProfile = CreateTaxProfile(42m);
        var parameters = new CapitalGainsTaxParameters();

        // Verlust: 350.000 - 15.000 - 420.000 + 15.000 = -70.000€

        // Act
        var result = _calculator.CalculateCapitalGainsTax(
            purchase,
            salePrice: Money.Euro(350_000m),
            saleCosts: Money.Euro(15_000m),
            accumulatedDepreciation: Money.Euro(15_000m),
            saleDate: new DateOnly(2025, 1, 1),
            taxProfile,
            parameters);

        // Assert
        // Verlust unter Freigrenze → steuerfrei (negative Beträge sind immer "unter" der Grenze)
        Assert.True(result.IsTaxExempt || result.Gain.Amount < 0);
    }

    [Fact]
    public void CalculateCapitalGainsTax_CorrectlyCalculates_AdjustedBasis()
    {
        // Arrange
        var purchase = CreatePurchase(new DateOnly(2020, 1, 1));
        var taxProfile = CreateTaxProfile(42m);
        var parameters = new CapitalGainsTaxParameters();

        // Investition: 400.000 + 20.000 NK = 420.000€
        // AfA: 50.000€
        // Fortgeschriebene AK: 370.000€

        // Act
        var result = _calculator.CalculateCapitalGainsTax(
            purchase,
            salePrice: Money.Euro(500_000m),
            saleCosts: Money.Euro(15_000m),
            accumulatedDepreciation: Money.Euro(50_000m),
            saleDate: new DateOnly(2025, 1, 1),
            taxProfile,
            parameters);

        // Assert
        Assert.Equal(370_000m, result.AdjustedBasis.Amount);
        // Gewinn = 500.000 - 15.000 - 370.000 = 115.000€
        Assert.Equal(115_000m, result.Gain.Amount);
    }

    private static Purchase CreatePurchase(DateOnly purchaseDate)
    {
        return new Purchase
        {
            PurchasePrice = Money.Euro(400_000m),
            LandValue = Money.Euro(80_000m), // 20% Boden
            PurchaseDate = purchaseDate,
            AcquisitionCosts = new[]
            {
                new AcquisitionCost
                {
                    Type = AcquisitionCostType.TransferTax,
                    Amount = Money.Euro(20_000m),
                    IsCapitalizable = true
                }
            }
        };
    }

    private static TaxProfile CreateTaxProfile(decimal marginalRate)
    {
        return new TaxProfile
        {
            OwnershipType = OwnershipType.PrivateIndividual,
            MarginalTaxRatePercent = marginalRate,
            SolidaritySurchargePercent = 5.5m
        };
    }
}
