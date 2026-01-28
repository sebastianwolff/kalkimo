namespace Kalkimo.Domain.Models;

/// <summary>
/// Laufende Kosten-Konfiguration
/// </summary>
public record CostConfiguration
{
    /// <summary>Einzelne Kostenpositionen</summary>
    public required IReadOnlyList<CostItem> Items { get; init; }

    /// <summary>Rücklagenkonto-Konfiguration</summary>
    public ReserveAccountConfig? ReserveAccount { get; init; }
}

/// <summary>
/// Einzelne Kostenposition
/// </summary>
public record CostItem
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required CostClassification Classification { get; init; }

    /// <summary>Monatlicher Betrag (Basis)</summary>
    public required Money MonthlyAmount { get; init; }

    /// <summary>Oder: Betrag pro m² pro Jahr</summary>
    public decimal? AmountPerSqmPerYear { get; init; }

    /// <summary>Steuerlich absetzbar?</summary>
    public bool IsTaxDeductible { get; init; } = true;

    /// <summary>Jährliche Dynamisierung (%)</summary>
    public decimal AnnualInflationPercent { get; init; }

    /// <summary>Startdatum</summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>Enddatum</summary>
    public DateOnly? EndDate { get; init; }
}

/// <summary>
/// Rücklagenkonto-Konfiguration
/// </summary>
public record ReserveAccountConfig
{
    /// <summary>Anfangsstand</summary>
    public Money InitialBalance { get; init; } = Money.Zero();

    /// <summary>Monatliche Zuführung (fix)</summary>
    public Money? MonthlyContribution { get; init; }

    /// <summary>Oder: Zuführung pro m² pro Jahr</summary>
    public decimal? ContributionPerSqmPerYear { get; init; }

    /// <summary>Oder: % der Nettomiete</summary>
    public decimal? ContributionPercentOfRent { get; init; }

    /// <summary>Mindestschwelle (Warnung wenn unterschritten)</summary>
    public Money? MinimumThreshold { get; init; }

    /// <summary>Jährliche Dynamisierung der Zuführung (%)</summary>
    public decimal AnnualInflationPercent { get; init; }
}
