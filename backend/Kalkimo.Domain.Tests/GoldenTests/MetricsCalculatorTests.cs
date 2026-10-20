using Kalkimo.Domain.Calculators;
using Kalkimo.Domain.Models;
using Kalkimo.Domain.Tests.TestHelpers;

namespace Kalkimo.Domain.Tests.GoldenTests;

/// <summary>
/// Golden Tests für Rendite-Kennzahlen (Eigenkapital-Sicht):
/// - IRR/NPV mit Eigenkapital als Anfangsauszahlung
/// - Endwert nach Ablösung der Restschuld
/// - Keine Cashflows nach der Exit-Periode
/// - Break-Even-Miete
/// - DSCR/ICR ohne Fremdfinanzierung
/// </summary>
public class MetricsCalculatorTests
{
    private readonly CalculationOrchestrator _orchestrator;

    public MetricsCalculatorTests()
    {
        var dateProvider = new FixedDateProvider(new DateOnly(2025, 1, 1));
        _orchestrator = new CalculationOrchestrator(new TaxCalculator(dateProvider));
    }

    #region IRR / NPV

    [Fact]
    public void CalculateIrr_OneMonthAtOnePercent_Returns12PercentNominal()
    {
        // 1.000 € Einsatz, nach einem Monat 1.010 € zurück → 1 % p.M. → 12 % p.a. nominal
        var cashflows = new MoneyTimeSeries(new YearMonth(2025, 1), new YearMonth(2025, 1));

        var irr = MetricsCalculator.CalculateIrr(Money.Euro(1_000m), cashflows, Money.Euro(1_010m));

        Assert.Equal(12m, Math.Round(irr, 2));
    }

    [Fact]
    public void CalculateIrr_IgnoresCashflowsAfterExitPeriod()
    {
        var cashflows = new MoneyTimeSeries(new YearMonth(2025, 1), new YearMonth(2025, 3));
        cashflows[new YearMonth(2025, 2)] = Money.Euro(1_000_000m);
        cashflows[new YearMonth(2025, 3)] = Money.Euro(1_000_000m);

        var irr = MetricsCalculator.CalculateIrr(
            Money.Euro(1_000m), cashflows, Money.Euro(1_010m), exitPeriod: new YearMonth(2025, 1));

        Assert.Equal(12m, Math.Round(irr, 2));
    }

    [Fact]
    public void CalculateNpv_TerminalValueDiscountedInExitPeriod_MatchesIrrConvention()
    {
        // Bei Diskontierung mit dem IRR (12 % p.a. = 1 % p.M.) muss der NPV 0 sein
        var cashflows = new MoneyTimeSeries(new YearMonth(2025, 1), new YearMonth(2025, 3));
        cashflows[new YearMonth(2025, 2)] = Money.Euro(1_000_000m);

        var npv = MetricsCalculator.CalculateNpv(
            Money.Euro(1_000m), cashflows, 12m, Money.Euro(1_010m), exitPeriod: new YearMonth(2025, 1));

        Assert.Equal(0m, npv.Amount);
    }

    #endregion

    #region Exit-Erlös

    [Fact]
    public void Calculate_NoPlannedSale_AssumesExitAtHorizonEnd_NetOfDebt()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        var exit = project.EndPeriod;
        Assert.False(result.Metrics.IsExitPlanned);
        Assert.Equal(exit, result.Metrics.ExitPeriod);

        // Haltedauer > 10 Jahre → steuerfrei; Erlös = Wert − 5 % Verkaufskosten − Restschuld
        var salePrice = result.PropertyValue[exit];
        var expected = salePrice - salePrice * 5m / 100 - result.OutstandingDebt[exit];

        Assert.True(result.OutstandingDebt[exit].Amount > 0, "Testprojekt sollte am Ende noch Restschuld haben");
        Assert.Equal(expected.Amount, result.Metrics.ExitEquityProceeds!.Value.Amount, 2);
    }

    [Fact]
    public void Calculate_PlannedSaleWithinHorizon_UsesSaleDateDebtAndTax()
    {
        var saleDate = new YearMonth(2030, 6);
        var project = TestProjectBuilder.CreateStandardProject();
        project = project with { Valuation = project.Valuation with { PlannedSaleDate = saleDate } };

        var result = _orchestrator.Calculate(project);

        Assert.True(result.Metrics.IsExitPlanned);
        Assert.Equal(saleDate, result.Metrics.ExitPeriod);

        // Verkauf innerhalb der Spekulationsfrist → Steuer wird ausgewiesen und abgezogen
        var tax = result.TaxSummary.CapitalGainsTax;
        Assert.NotNull(tax);
        Assert.False(result.TaxSummary.SaleTaxExempt);

        var salePrice = result.PropertyValue[saleDate];
        var expected = salePrice - salePrice * 5m / 100 - tax!.Value - result.OutstandingDebt[saleDate];
        Assert.Equal(expected.Amount, result.Metrics.ExitEquityProceeds!.Value.Amount, 2);
    }

    [Fact]
    public void Calculate_PlannedSale_EquityMultipleIgnoresCashflowsAfterSale()
    {
        var saleDate = new YearMonth(2030, 6);
        var project = TestProjectBuilder.CreateStandardProject();
        project = project with { Valuation = project.Valuation with { PlannedSaleDate = saleDate } };

        var result = _orchestrator.Calculate(project);

        var cashflowUntilSale = result.CashflowAfterTax
            .Where(p => p.Period <= saleDate)
            .Sum(p => p.Value.Amount);
        var equity = project.Financing.TotalEquity.Amount;
        var expected = Math.Round((cashflowUntilSale + result.Metrics.ExitEquityProceeds!.Value.Amount) / equity, 2);

        Assert.Equal(expected, result.Metrics.EquityMultiple);
    }

    #endregion

    #region Bankkennzahlen

    [Fact]
    public void Calculate_BreakEvenRent_EqualsRentAtWhichCashflowBeforeTaxIsZero()
    {
        // Ohne CapEx: CF vor Steuern = Effektivmiete − (nicht gedeckte BK + Kapitaldienst)
        // → Break-Even = Effektivmiete − CF vor Steuern (Referenzjahr = erstes volles Jahr)
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        var expected = (result.EffectiveRent.SumForYear(2026) - result.CashflowBeforeTax.SumForYear(2026)).Amount / 12;
        Assert.Equal(Math.Round(expected, 2), result.Metrics.BreakEvenRent.Amount);
    }

    [Fact]
    public void Calculate_WithoutLoans_DoesNotThrow_AndCoverageRatiosAreNull()
    {
        var project = TestProjectBuilder.CreateStandardProject();
        project = project with
        {
            Financing = project.Financing with
            {
                Loans = [],
                EquityContributions =
                [
                    new EquityContribution
                    {
                        InvestorId = "investor-1",
                        Amount = project.Purchase.TotalInvestment,
                        ContributionDate = project.StartPeriod.ToFirstDayOfMonth()
                    }
                ]
            }
        };

        var result = _orchestrator.Calculate(project);

        Assert.Null(result.Metrics.DscrMin);
        Assert.Null(result.Metrics.DscrAvg);
        Assert.Null(result.Metrics.IcrMin);
        Assert.Equal(0m, result.OutstandingDebt[project.EndPeriod].Amount);
        Assert.True(result.Metrics.IrrAfterTaxPercent > 0, "Unbelastetes Objekt mit positiver Miete sollte positiven IRR haben");
    }

    #endregion
}
