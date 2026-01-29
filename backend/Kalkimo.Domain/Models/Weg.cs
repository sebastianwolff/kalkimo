namespace Kalkimo.Domain.Models;

/// <summary>
/// WEG-Konfiguration (Wohnungseigentümergemeinschaft)
/// Für ETW (Eigentumswohnungen) mit Teilungserklärung
/// </summary>
public record WegConfiguration
{
    /// <summary>Name der WEG</summary>
    public string? WegName { get; init; }

    /// <summary>Miteigentumsanteil (MEA) in Tausendstel</summary>
    public required decimal MeaPerMille { get; init; }

    /// <summary>Gesamtzahl aller MEA in der WEG (normalerweise 1000 oder 10000)</summary>
    public decimal TotalMeaPerMille { get; init; } = 1000m;

    /// <summary>Anteil in Prozent (berechnet aus MEA)</summary>
    public decimal SharePercent => TotalMeaPerMille > 0 ? MeaPerMille / TotalMeaPerMille * 100 : 0;

    /// <summary>Hausgeld-Konfiguration</summary>
    public required HausgeldConfiguration Hausgeld { get; init; }

    /// <summary>Aktueller WEG-Rücklagenstand (Anteil dieser Einheit)</summary>
    public Money? CurrentReserveBalance { get; init; }

    /// <summary>Geplante Sonderumlagen</summary>
    public IReadOnlyList<Sonderumlage> Sonderumlagen { get; init; } = [];

    /// <summary>Kostenverteilungsschlüssel</summary>
    public IReadOnlyList<CostDistributionKey> DistributionKeys { get; init; } = [];
}

/// <summary>
/// Hausgeld-Konfiguration (monatliche WEG-Abrechnung)
/// </summary>
public record HausgeldConfiguration
{
    /// <summary>Monatliches Hausgeld gesamt</summary>
    public required Money MonthlyTotal { get; init; }

    /// <summary>Davon: Rücklagenzuführung (wird nicht als Kosten verbucht)</summary>
    public required Money MonthlyReserveContribution { get; init; }

    /// <summary>Davon: Verwaltungskosten</summary>
    public Money? MonthlyAdministration { get; init; }

    /// <summary>Davon: Instandhaltung (laufend)</summary>
    public Money? MonthlyMaintenance { get; init; }

    /// <summary>Davon: Heizkosten (umlagefähig)</summary>
    public Money? MonthlyHeating { get; init; }

    /// <summary>Davon: Sonstige Betriebskosten (umlagefähig)</summary>
    public Money? MonthlyOperatingCosts { get; init; }

    /// <summary>Nicht umlagefähiger Anteil des Hausgelds</summary>
    public Money NonTransferableAmount => MonthlyTotal - MonthlyReserveContribution - TransferableAmount;

    /// <summary>Umlagefähiger Anteil des Hausgelds (an Mieter weiterreichbar)</summary>
    public Money TransferableAmount =>
        (MonthlyHeating ?? Money.Zero(MonthlyTotal.Currency)) +
        (MonthlyOperatingCosts ?? Money.Zero(MonthlyTotal.Currency));

    /// <summary>Jährliche Steigerung in %</summary>
    public decimal AnnualIncreasePercent { get; init; }
}

/// <summary>
/// Sonderumlage (einmalige WEG-Zahlungen)
/// </summary>
public record Sonderumlage
{
    public required string Id { get; init; }
    public required string Description { get; init; }

    /// <summary>Gesamtbetrag der Sonderumlage (für diese Einheit)</summary>
    public required Money Amount { get; init; }

    /// <summary>Geplantes Fälligkeitsdatum</summary>
    public required YearMonth DueDate { get; init; }

    /// <summary>Auslösendes Ereignis (z.B. Fassadensanierung)</summary>
    public string? RelatedMeasure { get; init; }

    /// <summary>Steuerlich absetzbar? (typischerweise ja bei Erhaltungsaufwand)</summary>
    public bool IsTaxDeductible { get; init; } = true;

    /// <summary>Steuerliche Klassifikation</summary>
    public TaxClassification TaxClassification { get; init; } = TaxClassification.MaintenanceExpense;
}

/// <summary>
/// Kostenverteilungsschlüssel
/// </summary>
public record CostDistributionKey
{
    /// <summary>Name des Schlüssels</summary>
    public required string Name { get; init; }

    /// <summary>Art des Schlüssels</summary>
    public required DistributionKeyType Type { get; init; }

    /// <summary>Anteil dieser Einheit (für Custom-Schlüssel)</summary>
    public decimal? CustomShare { get; init; }

    /// <summary>Beschreibung welche Kosten nach diesem Schlüssel verteilt werden</summary>
    public string? Description { get; init; }

    /// <summary>Betroffene Kostenarten</summary>
    public IReadOnlyList<string> ApplicableCostTypes { get; init; } = [];
}

/// <summary>
/// Art des Verteilungsschlüssels
/// </summary>
public enum DistributionKeyType
{
    /// <summary>Nach MEA (Miteigentumsanteil)</summary>
    Mea,

    /// <summary>Nach Wohnfläche</summary>
    LivingArea,

    /// <summary>Nach Personenzahl</summary>
    PersonCount,

    /// <summary>Nach Verbrauch (Heizung, Wasser)</summary>
    Consumption,

    /// <summary>Nach Einheitenzahl (gleicher Anteil pro Einheit)</summary>
    PerUnit,

    /// <summary>Individueller Schlüssel laut Teilungserklärung</summary>
    Custom
}
