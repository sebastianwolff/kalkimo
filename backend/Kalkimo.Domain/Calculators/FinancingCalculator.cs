using Kalkimo.Domain.Models;

namespace Kalkimo.Domain.Calculators;

/// <summary>
/// Berechnet Finanzierungsverläufe (Annuitäten, Restschuld, Zins/Tilgung)
/// </summary>
public static class FinancingCalculator
{
    /// <summary>
    /// Berechnet den monatlichen Tilgungsplan für ein Darlehen
    /// Berücksichtigt: Bereitstellungszinsen, KfW tilgungsfreie Anlaufjahre, Disagio
    /// </summary>
    public static LoanSchedule CalculateLoanSchedule(
        Loan loan,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var schedule = new LoanSchedule
        {
            LoanId = loan.Id,
            Principal = loan.Principal,
            InterestPayments = new MoneyTimeSeries(startPeriod, endPeriod, currency),
            PrincipalPayments = new MoneyTimeSeries(startPeriod, endPeriod, currency),
            OutstandingBalance = new MoneyTimeSeries(startPeriod, endPeriod, currency),
            TotalPayments = new MoneyTimeSeries(startPeriod, endPeriod, currency),
            CommitmentFees = new MoneyTimeSeries(startPeriod, endPeriod, currency),
            DisagioAmortization = new MoneyTimeSeries(startPeriod, endPeriod, currency)
        };

        var disbursementPeriod = YearMonth.FromDate(loan.DisbursementDate);
        var fixedInterestEndPeriod = disbursementPeriod.AddMonths(loan.FixedInterestPeriodMonths);

        // Bereitstellungsfreie Zeit berechnen
        var commitmentFreeEnd = loan.CommitmentFreeMonths > 0
            ? disbursementPeriod.AddMonths(-loan.CommitmentFreeMonths)
            : disbursementPeriod;

        // KfW tilgungsfreie Periode
        var tilgungsfreieEnde = loan.TilgungsfreieAnlaufjahre > 0
            ? disbursementPeriod.AddYears(loan.TilgungsfreieAnlaufjahre)
            : disbursementPeriod;

        // Disagio-Verteilung über Zinsbindungsfrist
        var disagioPerMonth = Money.Zero(currency);
        if (loan.DisagioPercent.HasValue && loan.FixedInterestPeriodMonths > 0)
        {
            var disagioTotal = loan.Principal * loan.DisagioPercent.Value / 100;
            disagioPerMonth = disagioTotal / loan.FixedInterestPeriodMonths;
        }

        var outstandingBalance = loan.Principal;
        var monthlyRate = loan.InterestRatePercent / 100 / 12;
        var monthlyPayment = CalculateMonthlyPayment(loan);

        // Dictionary für Sondertilgungen
        var specialRepayments = loan.SpecialRepayments.ToDictionary(sr => sr.Period, sr => sr.Amount);

        foreach (var period in schedule.InterestPayments.Periods)
        {
            // Vor Auszahlung: Bereitstellungszinsen berechnen
            if (period < disbursementPeriod)
            {
                schedule.OutstandingBalance[period] = Money.Zero(currency);

                // Bereitstellungszinsen nach bereitstellungsfreier Zeit
                if (loan.CommitmentFeePercent.HasValue && period >= commitmentFreeEnd)
                {
                    var commitmentFee = loan.Principal * loan.CommitmentFeePercent.Value / 100 / 12;
                    schedule.CommitmentFees[period] = commitmentFee.Round();
                    schedule.InterestPayments[period] = commitmentFee.Round();
                    schedule.TotalPayments[period] = commitmentFee.Round();
                }
                continue;
            }

            // Auszahlungsmonat: Restschuld = Darlehensbetrag
            if (period == disbursementPeriod)
            {
                schedule.OutstandingBalance[period] = loan.Principal;
                // Erste Rate erst im Folgemonat
                continue;
            }

            // Nach Zinsbindung: Anschlussfinanzierung verwenden
            var currentRate = monthlyRate;
            var currentPayment = monthlyPayment;

            if (period > fixedInterestEndPeriod && loan.Refinancing != null)
            {
                currentRate = loan.Refinancing.InterestRatePercent / 100 / 12;
                currentPayment = loan.Refinancing.FixedMonthlyPayment
                    ?? CalculateAnnuityPayment(outstandingBalance, currentRate * 12, loan.Refinancing.RepaymentPercent);
            }

            // Zinsanteil
            var interestPayment = outstandingBalance * currentRate;

            // Disagio-Anteil (steuerlich als Zinsaufwand)
            if (period <= fixedInterestEndPeriod)
            {
                schedule.DisagioAmortization[period] = disagioPerMonth.Round();
            }

            // Tilgungsanteil - bei KfW in tilgungsfreier Zeit: keine Tilgung
            Money principalPayment;
            if (loan.Type == LoanType.KfW && period <= tilgungsfreieEnde)
            {
                // Tilgungsfreie Anlaufjahre: nur Zinsen, keine Tilgung
                principalPayment = Money.Zero(currency);
                schedule.InterestPayments[period] = interestPayment.Round();
                schedule.PrincipalPayments[period] = Money.Zero(currency);
                schedule.TotalPayments[period] = interestPayment.Round();
                schedule.OutstandingBalance[period] = outstandingBalance;
                continue;
            }

            principalPayment = currentPayment - interestPayment;

            // Sondertilgung
            if (specialRepayments.TryGetValue(period, out var specialRepayment))
            {
                principalPayment += specialRepayment;
            }

            // Nicht mehr tilgen als Restschuld
            if (principalPayment > outstandingBalance)
            {
                principalPayment = outstandingBalance;
            }

            // Neue Restschuld
            outstandingBalance -= principalPayment;
            if (outstandingBalance.Amount < 0.01m)
            {
                outstandingBalance = Money.Zero(currency);
            }

            schedule.InterestPayments[period] = interestPayment.Round();
            schedule.PrincipalPayments[period] = principalPayment.Round();
            schedule.TotalPayments[period] = (interestPayment + principalPayment).Round();
            schedule.OutstandingBalance[period] = outstandingBalance.Round();
        }

        return schedule;
    }

    /// <summary>
    /// Berechnet die monatliche Rate für ein Darlehen
    /// </summary>
    public static Money CalculateMonthlyPayment(Loan loan)
    {
        if (loan.FixedMonthlyPayment.HasValue)
            return loan.FixedMonthlyPayment.Value;

        return loan.Type switch
        {
            LoanType.Annuity => CalculateAnnuityPayment(
                loan.Principal,
                loan.InterestRatePercent,
                loan.InitialRepaymentPercent),

            LoanType.BulletLoan => CalculateInterestOnlyPayment(
                loan.Principal,
                loan.InterestRatePercent),

            _ => CalculateAnnuityPayment(
                loan.Principal,
                loan.InterestRatePercent,
                loan.InitialRepaymentPercent)
        };
    }

    /// <summary>
    /// Berechnet die Annuitätenrate aus Zins und anfänglicher Tilgung
    /// </summary>
    public static Money CalculateAnnuityPayment(
        Money principal,
        decimal annualInterestRatePercent,
        decimal initialRepaymentPercent)
    {
        // Annuität = (Zins% + Tilgung%) * Darlehen / 12
        var annualRate = (annualInterestRatePercent + initialRepaymentPercent) / 100;
        return (principal * annualRate / 12).Round();
    }

    /// <summary>
    /// Berechnet die reine Zinszahlung (für endfällige Darlehen)
    /// </summary>
    public static Money CalculateInterestOnlyPayment(
        Money principal,
        decimal annualInterestRatePercent)
    {
        return (principal * annualInterestRatePercent / 100 / 12).Round();
    }

    /// <summary>
    /// Berechnet die Annuitätenrate für vollständige Tilgung in n Monaten
    /// (mathematische Annuitätenformel)
    /// </summary>
    public static Money CalculateAnnuityForFullRepayment(
        Money principal,
        decimal annualInterestRatePercent,
        int termMonths)
    {
        var monthlyRate = annualInterestRatePercent / 100 / 12;

        if (monthlyRate == 0)
            return principal / termMonths;

        // A = P * (r * (1+r)^n) / ((1+r)^n - 1)
        var factor = (double)monthlyRate;
        var power = Math.Pow(1 + factor, termMonths);
        var payment = (double)principal.Amount * (factor * power) / (power - 1);

        return new Money((decimal)payment, principal.Currency).Round();
    }

    /// <summary>
    /// Aggregiert mehrere Darlehen zu Gesamtzeitreihen
    /// </summary>
    public static AggregatedFinancingResult AggregateLoans(
        IEnumerable<LoanSchedule> schedules,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new AggregatedFinancingResult
        {
            TotalInterest = new MoneyTimeSeries(startPeriod, endPeriod, currency),
            TotalPrincipal = new MoneyTimeSeries(startPeriod, endPeriod, currency),
            TotalDebtService = new MoneyTimeSeries(startPeriod, endPeriod, currency),
            TotalOutstandingDebt = new MoneyTimeSeries(startPeriod, endPeriod, currency)
        };

        var scheduleList = schedules.ToList();

        foreach (var period in result.TotalInterest.Periods)
        {
            var interest = Money.Zero(currency);
            var principal = Money.Zero(currency);
            var outstanding = Money.Zero(currency);

            foreach (var schedule in scheduleList)
            {
                interest += schedule.InterestPayments[period];
                principal += schedule.PrincipalPayments[period];
                outstanding += schedule.OutstandingBalance[period];
            }

            result.TotalInterest[period] = interest;
            result.TotalPrincipal[period] = principal;
            result.TotalDebtService[period] = interest + principal;
            result.TotalOutstandingDebt[period] = outstanding;
        }

        return result;
    }
}

/// <summary>
/// Tilgungsplan für ein einzelnes Darlehen
/// </summary>
public class LoanSchedule
{
    public required string LoanId { get; init; }
    public required Money Principal { get; init; }
    public required MoneyTimeSeries InterestPayments { get; init; }
    public required MoneyTimeSeries PrincipalPayments { get; init; }
    public required MoneyTimeSeries TotalPayments { get; init; }
    public required MoneyTimeSeries OutstandingBalance { get; init; }

    /// <summary>Bereitstellungszinsen (vor Auszahlung)</summary>
    public MoneyTimeSeries CommitmentFees { get; init; } = null!;

    /// <summary>Disagio-Amortisation (steuerlich als Zinsaufwand)</summary>
    public MoneyTimeSeries DisagioAmortization { get; init; } = null!;
}

/// <summary>
/// Aggregierte Finanzierungsergebnisse
/// </summary>
public class AggregatedFinancingResult
{
    public required MoneyTimeSeries TotalInterest { get; init; }
    public required MoneyTimeSeries TotalPrincipal { get; init; }
    public required MoneyTimeSeries TotalDebtService { get; init; }
    public required MoneyTimeSeries TotalOutstandingDebt { get; init; }
}
