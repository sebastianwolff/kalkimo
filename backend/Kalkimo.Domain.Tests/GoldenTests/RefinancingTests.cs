using Kalkimo.Domain.Calculators;
using Kalkimo.Domain.Models;
using Kalkimo.Domain.Tests.TestHelpers;

namespace Kalkimo.Domain.Tests.GoldenTests;

/// <summary>
/// Golden Tests für Anschlussfinanzierung und tilgungsfreie Anlaufjahre
/// </summary>
public class RefinancingTests
{
    private static readonly YearMonth Start = new(2025, 1);
    private static readonly YearMonth End = new(2044, 12);

    private static Loan CreateLoan(RefinancingScenario? refinancing = null, int graceYears = 0) => new()
    {
        Id = "loan-1",
        Name = "Darlehen",
        Type = LoanType.Annuity,
        Principal = Money.Euro(300_000m),
        DisbursementDate = new DateOnly(2025, 1, 1),
        InterestRatePercent = 3.5m,
        InitialRepaymentPercent = 2m,
        FixedInterestPeriodMonths = 120,
        TilgungsfreieAnlaufjahre = graceYears,
        Refinancing = refinancing
    };

    [Fact]
    public void Refinancing_PaymentIsFixedOnceAtEndOfFixedInterestPeriod()
    {
        var loan = CreateLoan(new RefinancingScenario { InterestRatePercent = 5m, RepaymentPercent = 2m });

        var schedule = FinancingCalculator.CalculateLoanSchedule(loan, Start, End);

        var refinancingStart = new YearMonth(2035, 2);
        var balanceAtSwitch = schedule.OutstandingBalance[new YearMonth(2035, 1)];
        var expectedPayment = FinancingCalculator.CalculateAnnuityPayment(balanceAtSwitch, 5m, 2m);

        // Rate bleibt über die gesamte Anschlussphase konstant (echte Annuität)
        Assert.Equal(expectedPayment.Amount, schedule.TotalPayments[refinancingStart].Amount, 1);
        Assert.Equal(expectedPayment.Amount, schedule.TotalPayments[new YearMonth(2040, 6)].Amount, 1);
        Assert.Equal(expectedPayment.Amount, schedule.TotalPayments[new YearMonth(2044, 12)].Amount, 1);

        // Zinsanteil sinkt, Tilgungsanteil steigt
        Assert.True(schedule.PrincipalPayments[new YearMonth(2044, 12)] > schedule.PrincipalPayments[refinancingStart]);
    }

    [Fact]
    public void Refinancing_WithTerm_RepaysFullyWithinTerm()
    {
        var loan = CreateLoan(new RefinancingScenario { InterestRatePercent = 4m, TermMonths = 60 });

        var schedule = FinancingCalculator.CalculateLoanSchedule(loan, Start, End);

        Assert.True(schedule.OutstandingBalance[new YearMonth(2039, 12)].Amount > 0);
        Assert.True(schedule.OutstandingBalance[new YearMonth(2040, 2)].Amount < 1m);
    }

    [Fact]
    public void GracePeriod_AppliesToAnnuityLoans()
    {
        var loan = CreateLoan(graceYears: 2);

        var schedule = FinancingCalculator.CalculateLoanSchedule(loan, Start, End);

        Assert.Equal(0m, schedule.PrincipalPayments[new YearMonth(2026, 6)].Amount);
        Assert.Equal(300_000m, schedule.OutstandingBalance[new YearMonth(2026, 12)].Amount);
        Assert.True(schedule.PrincipalPayments[new YearMonth(2027, 6)].Amount > 0);
    }

    [Fact]
    public void Orchestrator_WarnsWhenFixedInterestEndsWithoutRefinancing()
    {
        var orchestrator = new CalculationOrchestrator(new TaxCalculator(new FixedDateProvider(new DateOnly(2025, 1, 1))));
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2040);

        var result = orchestrator.Calculate(project);

        Assert.Contains(result.Warnings, w => w.Type == WarningType.RefinancingAssumed);
    }

    [Fact]
    public void Orchestrator_NoRefinancingWarning_WhenFixedInterestCoversHorizon()
    {
        var orchestrator = new CalculationOrchestrator(new TaxCalculator(new FixedDateProvider(new DateOnly(2025, 1, 1))));
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2034);

        var result = orchestrator.Calculate(project);

        Assert.DoesNotContain(result.Warnings, w => w.Type == WarningType.RefinancingAssumed);
    }
}
