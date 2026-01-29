namespace Kalkimo.Domain.Models;

/// <summary>
/// Mieteinnahmen-Konfiguration
/// </summary>
public record RentConfiguration
{
    /// <summary>Mietverhältnisse</summary>
    public required IReadOnlyList<Tenancy> Tenancies { get; init; }

    /// <summary>Pauschaler Mietausfall (% der Sollmiete)</summary>
    public decimal VacancyRatePercent { get; init; }

    /// <summary>Mietausfall-Ereignisse</summary>
    public IReadOnlyList<VacancyEvent> VacancyEvents { get; init; } = [];
}

/// <summary>
/// Einzelnes Mietverhältnis
/// </summary>
public record Tenancy
{
    public required string Id { get; init; }

    /// <summary>Zugeordnete Einheit (oder null für Gesamtobjekt)</summary>
    public string? UnitId { get; init; }

    /// <summary>Mieterbezeichnung (anonymisiert)</summary>
    public string TenantLabel { get; init; } = "Mieter";

    /// <summary>Vertragsbeginn</summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>Vertragsende (null = unbefristet)</summary>
    public DateOnly? EndDate { get; init; }

    /// <summary>Nettokaltmiete pro Monat</summary>
    public required Money NetRent { get; init; }

    /// <summary>Nebenkostenvorauszahlung pro Monat</summary>
    public Money ServiceChargeAdvance { get; init; } = Money.Zero();

    /// <summary>Mietentwicklungsmodell</summary>
    public required RentDevelopmentModel DevelopmentModel { get; init; }

    /// <summary>Jährliche Steigerungsrate (für Annual-Modell)</summary>
    public decimal? AnnualIncreasePercent { get; init; }

    /// <summary>Staffelmiete-Stufen</summary>
    public IReadOnlyList<RentStep> RentSteps { get; init; } = [];

    /// <summary>Index-Schwelle (für Indexmiete, z.B. 5%)</summary>
    public decimal? IndexThresholdPercent { get; init; }

    /// <summary>Kaution</summary>
    public Money? Deposit { get; init; }

    /// <summary>Status</summary>
    public TenancyStatus Status { get; init; } = TenancyStatus.Active;
}

public enum TenancyStatus
{
    Active,        // Aktiv
    Terminated,    // Gekündigt
    Ended          // Beendet
}

/// <summary>
/// Staffelmietenstufe
/// </summary>
public record RentStep
{
    public required DateOnly EffectiveDate { get; init; }
    public required Money NewNetRent { get; init; }
}

/// <summary>
/// Mietausfall-Ereignis (z.B. Mietnomaden)
/// </summary>
public record VacancyEvent
{
    public required YearMonth StartPeriod { get; init; }
    public required int DurationMonths { get; init; }
    public string? UnitId { get; init; }
    public string? Description { get; init; }

    /// <summary>Zusätzlicher Schaden (CapEx)</summary>
    public Money? AdditionalDamage { get; init; }

    /// <summary>Versicherungserstattung</summary>
    public Money? InsuranceCoverage { get; init; }
}

/// <summary>
/// Wirtschaftliche Auswirkung einer Investitionsmaßnahme:
/// Betriebskostenersparnis (z.B. geringere Heizkosten) und Mietpotenzial.
/// </summary>
public record MeasureImpact
{
    /// <summary>Monatliche Betriebskostenersparnis (z.B. Heizkosten nach Dämmung)</summary>
    public Money? CostSavingsMonthly { get; init; }

    /// <summary>Mieterhöhung absolut pro Monat (z.B. Modernisierungsumlage)</summary>
    public Money? RentIncreaseMonthly { get; init; }

    /// <summary>Mieterhöhung relativ (%)</summary>
    public decimal? RentIncreasePercent { get; init; }

    /// <summary>Verzögerung in Monaten (Bauzeit, erst danach wirken Einsparungen)</summary>
    public int DelayMonths { get; init; }
}
