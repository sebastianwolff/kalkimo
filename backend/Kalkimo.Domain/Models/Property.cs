namespace Kalkimo.Domain.Models;

/// <summary>
/// Immobilienobjekt
/// </summary>
public record Property
{
    public required string Id { get; init; }
    public required PropertyType Type { get; init; }
    public required int ConstructionYear { get; init; }
    public required Condition OverallCondition { get; init; }

    /// <summary>Gesamtfläche in m²</summary>
    public required decimal TotalArea { get; init; }

    /// <summary>Wohnfläche in m²</summary>
    public required decimal LivingArea { get; init; }

    /// <summary>Grundstücksfläche in m²</summary>
    public decimal? LandArea { get; init; }

    /// <summary>Anzahl Einheiten (für MFH)</summary>
    public int UnitCount { get; init; } = 1;

    /// <summary>Einzelne Einheiten (für detaillierte Modellierung)</summary>
    public IReadOnlyList<Unit> Units { get; init; } = [];

    /// <summary>Bauteilzustände für Maßnahmenvorschläge</summary>
    public IReadOnlyList<ComponentCondition> Components { get; init; } = [];

    /// <summary>WEG-Rücklagenstand (nur für ETW)</summary>
    public Money? WegReserveBalance { get; init; }

    /// <summary>Miteigentumsanteil (nur für ETW)</summary>
    public decimal? Mea { get; init; }
}

/// <summary>
/// Einzelne Einheit (Wohnung, Gewerbe, Stellplatz)
/// </summary>
public record Unit
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required UnitType Type { get; init; }
    public required decimal Area { get; init; }
    public int? Rooms { get; init; }
    public string? Floor { get; init; }
    public UnitStatus Status { get; init; } = UnitStatus.Rented;
}

public enum UnitType
{
    Residential,     // Wohnen
    Commercial,      // Gewerbe
    Parking,         // Stellplatz
    Storage          // Keller/Lager
}

public enum UnitStatus
{
    Rented,          // Vermietet
    Vacant,          // Leerstand
    OwnerOccupied,   // Eigennutzung
    Renovation       // Im Umbau
}

/// <summary>
/// Bauteilzustand für Maßnahmenvorschläge
/// </summary>
public record ComponentCondition
{
    public required CapExCategory Category { get; init; }
    public required Condition Condition { get; init; }

    /// <summary>Jahr der letzten Modernisierung (null = Baujahr)</summary>
    public int? LastRenovationYear { get; init; }

    /// <summary>Erwarteter Zyklus in Jahren</summary>
    public int ExpectedCycleYears { get; init; }
}
