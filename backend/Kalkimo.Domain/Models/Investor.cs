namespace Kalkimo.Domain.Models;

/// <summary>
/// Multi-Investor Konfiguration
/// </summary>
public record InvestorConfiguration
{
    /// <summary>Investoren</summary>
    public required IReadOnlyList<Investor> Investors { get; init; }

    /// <summary>Gesellschafterdarlehen</summary>
    public IReadOnlyList<ShareholderLoan> ShareholderLoans { get; init; } = [];

    /// <summary>Ausschüttungspolitik</summary>
    public DistributionPolicy DistributionPolicy { get; init; } = new();
}

/// <summary>
/// Einzelner Investor
/// </summary>
public record Investor
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    /// <summary>Beteiligungsquote (%)</summary>
    public required decimal SharePercent { get; init; }

    /// <summary>Individuelles Steuerprofil (optional)</summary>
    public TaxProfile? TaxProfile { get; init; }
}

/// <summary>
/// Gesellschafterdarlehen
/// </summary>
public record ShareholderLoan
{
    public required string Id { get; init; }
    public required string InvestorId { get; init; }
    public required Money Principal { get; init; }
    public required DateOnly DisbursementDate { get; init; }

    /// <summary>Zinssatz p.a.</summary>
    public required decimal InterestRatePercent { get; init; }

    /// <summary>Monatliche Tilgung</summary>
    public Money? MonthlyRepayment { get; init; }

    /// <summary>Laufzeit in Monaten</summary>
    public int? TermMonths { get; init; }
}

/// <summary>
/// Ausschüttungspolitik
/// </summary>
public record DistributionPolicy
{
    /// <summary>Mindest-Liquiditätsreserve vor Ausschüttung</summary>
    public Money MinimumReserve { get; init; } = Money.Zero();

    /// <summary>Ausschüttungsfrequenz</summary>
    public DistributionFrequency Frequency { get; init; } = DistributionFrequency.Annual;

    /// <summary>Ausschüttungsquote (% des verfügbaren Cashflows)</summary>
    public decimal DistributionRatePercent { get; init; } = 100;
}

public enum DistributionFrequency
{
    Monthly,
    Quarterly,
    Annual,
    OnDemand
}
