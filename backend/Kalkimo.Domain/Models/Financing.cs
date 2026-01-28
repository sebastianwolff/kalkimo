namespace Kalkimo.Domain.Models;

/// <summary>
/// Gesamtfinanzierung eines Projekts
/// </summary>
public record Financing
{
    /// <summary>Eigenkapital-Einlagen</summary>
    public required IReadOnlyList<EquityContribution> EquityContributions { get; init; }

    /// <summary>Darlehen</summary>
    public required IReadOnlyList<Loan> Loans { get; init; }

    /// <summary>Gesamtes Eigenkapital</summary>
    public Money TotalEquity =>
        EquityContributions.Aggregate(Money.Zero(), (sum, eq) => sum + eq.Amount);

    /// <summary>Gesamtes Fremdkapital (Darlehenssummen)</summary>
    public Money TotalDebt =>
        Loans.Aggregate(Money.Zero(), (sum, loan) => sum + loan.Principal);

    /// <summary>Gesamtkapital</summary>
    public Money TotalCapital => TotalEquity + TotalDebt;

    /// <summary>Eigenkapitalquote</summary>
    public decimal EquityRatio => TotalCapital.Amount > 0
        ? TotalEquity.Amount / TotalCapital.Amount
        : 0;
}

/// <summary>
/// Eigenkapital-Einlage
/// </summary>
public record EquityContribution
{
    public required string InvestorId { get; init; }
    public required Money Amount { get; init; }
    public required DateOnly ContributionDate { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Einzelnes Darlehen
/// </summary>
public record Loan
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required LoanType Type { get; init; }

    /// <summary>Darlehensbetrag</summary>
    public required Money Principal { get; init; }

    /// <summary>Auszahlungsdatum</summary>
    public required DateOnly DisbursementDate { get; init; }

    /// <summary>Nominalzins p.a.</summary>
    public required decimal InterestRatePercent { get; init; }

    /// <summary>Anfängliche Tilgung p.a. (für Annuität)</summary>
    public decimal InitialRepaymentPercent { get; init; }

    /// <summary>Monatliche Rate (wenn fest vorgegeben)</summary>
    public Money? FixedMonthlyPayment { get; init; }

    /// <summary>Zinsbindungsdauer in Monaten</summary>
    public int FixedInterestPeriodMonths { get; init; }

    /// <summary>Sondertilgung pro Jahr erlaubt</summary>
    public Money? AnnualSpecialRepaymentAllowed { get; init; }

    /// <summary>Bereitstellungszinsen (% p.a. auf nicht abgerufenen Betrag)</summary>
    public decimal? CommitmentFeePercent { get; init; }

    /// <summary>Bereitstellungsfreie Zeit in Monaten</summary>
    public int CommitmentFreeMonths { get; init; }

    /// <summary>Geplante Sondertilgungen</summary>
    public IReadOnlyList<SpecialRepayment> SpecialRepayments { get; init; } = [];

    /// <summary>Anschlussfinanzierung</summary>
    public RefinancingScenario? Refinancing { get; init; }
}

/// <summary>
/// Geplante Sondertilgung
/// </summary>
public record SpecialRepayment
{
    public required YearMonth Period { get; init; }
    public required Money Amount { get; init; }
}

/// <summary>
/// Anschlussfinanzierungs-Szenario
/// </summary>
public record RefinancingScenario
{
    /// <summary>Anschlusszins p.a.</summary>
    public required decimal InterestRatePercent { get; init; }

    /// <summary>Tilgungssatz p.a.</summary>
    public decimal RepaymentPercent { get; init; }

    /// <summary>Laufzeit in Monaten</summary>
    public int? TermMonths { get; init; }

    /// <summary>Monatliche Rate (wenn fest)</summary>
    public Money? FixedMonthlyPayment { get; init; }
}
