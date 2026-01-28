namespace Kalkimo.Domain.Models;

/// <summary>
/// Hauptentität: Investitionsprojekt
/// </summary>
public record Project
{
    // === Metadaten ===
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Currency { get; init; }

    // === Zeitraum ===
    public required YearMonth StartPeriod { get; init; }
    public required YearMonth EndPeriod { get; init; }
    public int HorizonYears => (EndPeriod.Year - StartPeriod.Year) +
        (EndPeriod.Month >= StartPeriod.Month ? 1 : 0);

    // === Kernkomponenten ===
    public required Property Property { get; init; }
    public required Purchase Purchase { get; init; }
    public required Financing Financing { get; init; }
    public required RentConfiguration Rent { get; init; }
    public required CostConfiguration Costs { get; init; }
    public required TaxProfile TaxProfile { get; init; }

    // === Optionale Komponenten ===
    public CapExConfiguration? CapEx { get; init; }
    public InvestorConfiguration? Investors { get; init; }
    public ValuationConfiguration Valuation { get; init; } = new();

    // === Szenarien ===
    public IReadOnlyList<Scenario> Scenarios { get; init; } = [];

    // === Versionierung ===
    public int Version { get; init; } = 1;
    public string SchemaVersion { get; init; } = "1.0";
    public string? EngineVersion { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

/// <summary>
/// Szenario-Variante eines Projekts
/// </summary>
public record Scenario
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsBase { get; init; }

    /// <summary>Parameter-Overrides für dieses Szenario</summary>
    public ScenarioParameters Parameters { get; init; } = new();
}

/// <summary>
/// Überschreibbare Parameter für Szenarien
/// </summary>
public record ScenarioParameters
{
    /// <summary>Override Marktwertentwicklung p.a. (%)</summary>
    public decimal? MarketGrowthRatePercent { get; init; }

    /// <summary>Override Mietentwicklung p.a. (%)</summary>
    public decimal? RentGrowthRatePercent { get; init; }

    /// <summary>Override Leerstandsquote (%)</summary>
    public decimal? VacancyRatePercent { get; init; }

    /// <summary>Override Anschlusszins (%)</summary>
    public decimal? RefinancingInterestRatePercent { get; init; }

    /// <summary>Override Kostenentwicklung p.a. (%)</summary>
    public decimal? CostInflationPercent { get; init; }

    /// <summary>Zusätzliche CapEx-Schocks</summary>
    public IReadOnlyList<CapExMeasure>? AdditionalCapEx { get; init; }

    /// <summary>Zusätzliche Leerstandsereignisse</summary>
    public IReadOnlyList<VacancyEvent>? AdditionalVacancyEvents { get; init; }
}
