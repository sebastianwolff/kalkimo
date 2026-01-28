namespace Kalkimo.Domain.Models;

/// <summary>
/// Wertentwicklungs- und Verkaufskonfiguration
/// </summary>
public record ValuationConfiguration
{
    /// <summary>Marktwertentwicklung p.a. (%)</summary>
    public decimal MarketGrowthRatePercent { get; init; }

    /// <summary>Geplanter Verkaufszeitpunkt (optional)</summary>
    public YearMonth? PlannedSaleDate { get; init; }

    /// <summary>Verkaufskosten (% vom Verkaufspreis)</summary>
    public decimal SaleCostsPercent { get; init; } = 3m;

    /// <summary>Wertabschlag bei unterlassenen Maßnahmen</summary>
    public DeferredMaintenanceImpact DeferredMaintenanceImpact { get; init; } = new();

    /// <summary>§23 EStG Parameter</summary>
    public CapitalGainsTaxParameters CapitalGainsTax { get; init; } = new();
}

/// <summary>
/// Wertabschlag bei unterlassenen Maßnahmen
/// </summary>
public record DeferredMaintenanceImpact
{
    /// <summary>Abschlag pro Jahr Überfälligkeit (%)</summary>
    public decimal DiscountPerOverdueYearPercent { get; init; } = 1m;

    /// <summary>Maximaler Gesamtabschlag (%)</summary>
    public decimal MaxTotalDiscountPercent { get; init; } = 20m;

    /// <summary>Faktor nach Priorität</summary>
    public decimal GetPriorityFactor(MeasurePriority priority) => priority switch
    {
        MeasurePriority.Critical => 2.0m,
        MeasurePriority.High => 1.5m,
        MeasurePriority.Medium => 1.0m,
        MeasurePriority.Low => 0.5m,
        _ => 1.0m
    };
}
