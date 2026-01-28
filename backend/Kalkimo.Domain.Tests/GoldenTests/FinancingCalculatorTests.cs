using Kalkimo.Domain.Calculators;
using Kalkimo.Domain.Models;
using Xunit;

namespace Kalkimo.Domain.Tests.GoldenTests;

/// <summary>
/// Golden Tests für Finanzierungsberechnung
/// </summary>
public class FinancingCalculatorTests
{
    [Fact]
    public void CalculateMonthlyPayment_AnnuityLoan_CorrectRate()
    {
        // Arrange: 300.000€, 3% Zins, 2% Tilgung
        var loan = new Loan
        {
            Id = "loan-1",
            Name = "Hauptdarlehen",
            Type = LoanType.Annuity,
            Principal = Money.Euro(300_000m),
            DisbursementDate = new DateOnly(2025, 1, 1),
            InterestRatePercent = 3.0m,
            InitialRepaymentPercent = 2.0m,
            FixedInterestPeriodMonths = 120
        };

        // Act
        var monthlyPayment = FinancingCalculator.CalculateMonthlyPayment(loan);

        // Assert
        // (3% + 2%) * 300.000 / 12 = 1.250€
        Assert.Equal(1_250m, monthlyPayment.Amount);
    }

    [Fact]
    public void CalculateMonthlyPayment_BulletLoan_InterestOnly()
    {
        // Arrange: Endfälliges Darlehen 100.000€, 4% Zins
        var loan = new Loan
        {
            Id = "bullet-1",
            Name = "Endfälliges Darlehen",
            Type = LoanType.BulletLoan,
            Principal = Money.Euro(100_000m),
            DisbursementDate = new DateOnly(2025, 1, 1),
            InterestRatePercent = 4.0m,
            InitialRepaymentPercent = 0m,
            FixedInterestPeriodMonths = 120
        };

        // Act
        var monthlyPayment = FinancingCalculator.CalculateMonthlyPayment(loan);

        // Assert
        // 4% * 100.000 / 12 = 333,33€
        Assert.Equal(333.33m, monthlyPayment.Amount);
    }

    [Fact]
    public void CalculateLoanSchedule_CorrectlyAmortizes_Over12Months()
    {
        // Arrange: 120.000€, 3% Zins, 2% Tilgung, über 1 Jahr
        var loan = new Loan
        {
            Id = "test-loan",
            Name = "Testdarlehen",
            Type = LoanType.Annuity,
            Principal = Money.Euro(120_000m),
            DisbursementDate = new DateOnly(2025, 1, 1),
            InterestRatePercent = 3.0m,
            InitialRepaymentPercent = 2.0m,
            FixedInterestPeriodMonths = 120
        };

        var start = new YearMonth(2025, 1);
        var end = new YearMonth(2025, 12);

        // Act
        var schedule = FinancingCalculator.CalculateLoanSchedule(loan, start, end);

        // Assert
        // Monatliche Rate: (3% + 2%) * 120.000 / 12 = 500€
        Assert.Equal(120_000m, schedule.Principal.Amount);

        // Im Februar (erster Monat mit Zahlung nach Auszahlung)
        var feb = new YearMonth(2025, 2);
        var febPayment = schedule.TotalPayments[feb];
        Assert.Equal(500m, febPayment.Amount);

        // Zins im Februar: 120.000 * 3% / 12 = 300€
        var febInterest = schedule.InterestPayments[feb];
        Assert.Equal(300m, febInterest.Amount);

        // Tilgung im Februar: 500 - 300 = 200€
        var febPrincipal = schedule.PrincipalPayments[feb];
        Assert.Equal(200m, febPrincipal.Amount);

        // Restschuld nach Februar: 120.000 - 200 = 119.800€
        var febBalance = schedule.OutstandingBalance[feb];
        Assert.Equal(119_800m, febBalance.Amount);
    }

    [Fact]
    public void CalculateLoanSchedule_HandlesSpecialRepayments()
    {
        // Arrange
        var loan = new Loan
        {
            Id = "test-loan",
            Name = "Testdarlehen",
            Type = LoanType.Annuity,
            Principal = Money.Euro(100_000m),
            DisbursementDate = new DateOnly(2025, 1, 1),
            InterestRatePercent = 3.0m,
            InitialRepaymentPercent = 2.0m,
            FixedInterestPeriodMonths = 120,
            SpecialRepayments = new[]
            {
                new SpecialRepayment
                {
                    Period = new YearMonth(2025, 6),
                    Amount = Money.Euro(10_000m)
                }
            }
        };

        var start = new YearMonth(2025, 1);
        var end = new YearMonth(2025, 12);

        // Act
        var schedule = FinancingCalculator.CalculateLoanSchedule(loan, start, end);

        // Assert: Im Juni sollte die Tilgung höher sein
        var junePrincipal = schedule.PrincipalPayments[new YearMonth(2025, 6)];
        Assert.True(junePrincipal.Amount > 10_000m); // Normale Tilgung + 10.000€ Sondertilgung
    }

    [Fact]
    public void AggregateLoans_SumsCorrectly()
    {
        // Arrange: 2 Darlehen
        var loan1 = new Loan
        {
            Id = "loan-1",
            Name = "Hauptdarlehen",
            Type = LoanType.Annuity,
            Principal = Money.Euro(200_000m),
            DisbursementDate = new DateOnly(2025, 1, 1),
            InterestRatePercent = 3.0m,
            InitialRepaymentPercent = 2.0m,
            FixedInterestPeriodMonths = 120
        };

        var loan2 = new Loan
        {
            Id = "loan-2",
            Name = "KfW-Darlehen",
            Type = LoanType.Annuity,
            Principal = Money.Euro(50_000m),
            DisbursementDate = new DateOnly(2025, 1, 1),
            InterestRatePercent = 1.5m,
            InitialRepaymentPercent = 3.0m,
            FixedInterestPeriodMonths = 120
        };

        var start = new YearMonth(2025, 1);
        var end = new YearMonth(2025, 12);

        var schedule1 = FinancingCalculator.CalculateLoanSchedule(loan1, start, end);
        var schedule2 = FinancingCalculator.CalculateLoanSchedule(loan2, start, end);

        // Act
        var aggregated = FinancingCalculator.AggregateLoans(
            new[] { schedule1, schedule2 }, start, end);

        // Assert
        var feb = new YearMonth(2025, 2);

        // Loan1: 200.000 * 5% / 12 = 833,33€
        // Loan2: 50.000 * 4.5% / 12 = 187,50€
        // Total: ~1.020€
        var totalDebtService = aggregated.TotalDebtService[feb];
        Assert.True(totalDebtService.Amount > 1_000m);
        Assert.True(totalDebtService.Amount < 1_100m);

        // Outstanding: 200.000 + 50.000 - kleine Tilgung
        var totalOutstanding = aggregated.TotalOutstandingDebt[feb];
        Assert.True(totalOutstanding.Amount > 249_000m);
        Assert.True(totalOutstanding.Amount < 250_000m);
    }

    [Fact]
    public void CalculateAnnuityForFullRepayment_CorrectRate()
    {
        // Arrange: 100.000€ über 360 Monate (30 Jahre) bei 4%
        var principal = Money.Euro(100_000m);
        var annualRate = 4.0m;
        var termMonths = 360;

        // Act
        var payment = FinancingCalculator.CalculateAnnuityForFullRepayment(
            principal, annualRate, termMonths);

        // Assert
        // Annuitätenformel: ~477€ pro Monat
        Assert.True(payment.Amount > 470m);
        Assert.True(payment.Amount < 490m);
    }
}
