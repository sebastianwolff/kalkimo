namespace Kalkimo.Domain.Models;

/// <summary>
/// Kaufdaten inkl. Nebenkosten
/// </summary>
public record Purchase
{
    /// <summary>Kaufpreis gesamt</summary>
    public required Money PurchasePrice { get; init; }

    /// <summary>Anteil Bodenwert (für AfA-Trennung)</summary>
    public required Money LandValue { get; init; }

    /// <summary>Anteil Gebäudewert (für AfA)</summary>
    public Money BuildingValue => PurchasePrice - LandValue;

    /// <summary>Kaufdatum / Übergang Nutzen und Lasten</summary>
    public required DateOnly PurchaseDate { get; init; }

    /// <summary>Zahlungszeitpunkte (bei gestaffelter Zahlung)</summary>
    public IReadOnlyList<PaymentTranche> PaymentTranches { get; init; } = [];

    /// <summary>Kaufnebenkosten</summary>
    public required IReadOnlyList<AcquisitionCost> AcquisitionCosts { get; init; }

    /// <summary>Summe aller Kaufnebenkosten</summary>
    public Money TotalAcquisitionCosts =>
        AcquisitionCosts.Aggregate(Money.Zero(), (sum, cost) => sum + cost.Amount);

    /// <summary>Gesamtinvestition (Kaufpreis + Nebenkosten)</summary>
    public Money TotalInvestment => PurchasePrice + TotalAcquisitionCosts;

    /// <summary>Aktivierungspflichtige Nebenkosten (zur Bemessungsgrundlage)</summary>
    public Money CapitalizableAcquisitionCosts =>
        AcquisitionCosts
            .Where(c => c.IsCapitalizable)
            .Aggregate(Money.Zero(), (sum, cost) => sum + cost.Amount);

    /// <summary>AfA-Bemessungsgrundlage (Gebäude + aktivierbare NK)</summary>
    public Money DepreciationBase => BuildingValue + CapitalizableAcquisitionCosts;
}

/// <summary>
/// Einzelne Kaufnebenkostenposition
/// </summary>
public record AcquisitionCost
{
    public required AcquisitionCostType Type { get; init; }
    public required Money Amount { get; init; }
    public string? Description { get; init; }

    /// <summary>Zahlungszeitpunkt</summary>
    public DateOnly? PaymentDate { get; init; }

    /// <summary>Aktivierungspflichtig (zur Bemessungsgrundlage)?</summary>
    public bool IsCapitalizable { get; init; } = true;
}

/// <summary>
/// Zahlungstranche bei gestaffeltem Kaufpreis
/// </summary>
public record PaymentTranche
{
    public required Money Amount { get; init; }
    public required DateOnly PaymentDate { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Standard-Kaufnebenkosten-Sätze nach Bundesland
/// </summary>
public static class AcquisitionCostRates
{
    public static decimal GetTransferTaxRate(string federalState) => federalState switch
    {
        "BY" => 0.035m,  // Bayern
        "SN" => 0.055m,  // Sachsen
        "BW" => 0.05m,   // Baden-Württemberg
        "HE" => 0.06m,   // Hessen
        "BE" => 0.06m,   // Berlin
        "BB" => 0.065m,  // Brandenburg
        "HH" => 0.055m,  // Hamburg
        "NI" => 0.05m,   // Niedersachsen
        "NW" => 0.065m,  // NRW
        "RP" => 0.05m,   // Rheinland-Pfalz
        "SL" => 0.065m,  // Saarland
        "SH" => 0.065m,  // Schleswig-Holstein
        "TH" => 0.065m,  // Thüringen
        _ => 0.05m       // Default
    };

    public const decimal NotaryRate = 0.015m;      // ca. 1.5% für Notar + Grundbuch
    public const decimal BrokerRateMax = 0.0357m;  // max 3.57% inkl. MwSt
}
