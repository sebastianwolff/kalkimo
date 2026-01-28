namespace Kalkimo.Domain.Models;

/// <summary>
/// CapEx/Maßnahmen-Konfiguration
/// </summary>
public record CapExConfiguration
{
    /// <summary>Geplante Maßnahmen</summary>
    public required IReadOnlyList<CapExMeasure> Measures { get; init; }
}

/// <summary>
/// Einzelne CapEx-Maßnahme
/// </summary>
public record CapExMeasure
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required CapExCategory Category { get; init; }

    /// <summary>Geplanter Durchführungszeitpunkt</summary>
    public required YearMonth PlannedPeriod { get; init; }

    /// <summary>Geschätzte Kosten</summary>
    public required Money EstimatedCost { get; init; }

    /// <summary>Risikopuffer (%)</summary>
    public decimal RiskBufferPercent { get; init; }

    /// <summary>Kosten inkl. Risikopuffer</summary>
    public Money CostWithBuffer => EstimatedCost * (1 + RiskBufferPercent / 100);

    /// <summary>Steuerliche Klassifikation</summary>
    public required TaxClassification TaxClassification { get; init; }

    /// <summary>Verteilungsjahre für §82b EStDV (2-5)</summary>
    public int? DistributionYears { get; init; }

    /// <summary>Zahlungsprofil (verteilt über Monate)</summary>
    public IReadOnlyList<PaymentScheduleItem> PaymentSchedule { get; init; } = [];

    /// <summary>Werterhöhend (vs. werterhaltend)?</summary>
    public bool IsValueEnhancing { get; init; }

    /// <summary>Wertauswirkung absolut</summary>
    public Money? ValueImpact { get; init; }

    /// <summary>Wertauswirkung relativ (%)</summary>
    public decimal? ValueImpactPercent { get; init; }

    /// <summary>Mietwirkung</summary>
    public RentImpact? RentImpact { get; init; }

    /// <summary>Als notwendig markiert (für Wertabschlag bei Nicht-Durchführung)</summary>
    public bool IsNecessary { get; init; }

    /// <summary>Durchgeführt?</summary>
    public bool IsExecuted { get; init; }

    /// <summary>Priorität</summary>
    public MeasurePriority Priority { get; init; } = MeasurePriority.Medium;
}

public enum MeasurePriority
{
    Critical,    // Kritisch
    High,        // Hoch
    Medium,      // Mittel
    Low          // Niedrig
}

/// <summary>
/// Zahlungsplan-Eintrag für verteile Zahlung
/// </summary>
public record PaymentScheduleItem
{
    public required YearMonth Period { get; init; }
    public required Money Amount { get; init; }
}

/// <summary>
/// Standard-Bauteilzyklen (editierbar)
/// </summary>
public static class DefaultComponentCycles
{
    public static (int MinYears, int MaxYears, Money CostPerSqmMin, Money CostPerSqmMax) GetCycle(CapExCategory category) => category switch
    {
        CapExCategory.Heating => (15, 25, Money.Euro(50), Money.Euro(150)),
        CapExCategory.Roof => (30, 60, Money.Euro(100), Money.Euro(250)),
        CapExCategory.Facade => (25, 50, Money.Euro(80), Money.Euro(200)),
        CapExCategory.Windows => (20, 40, Money.Euro(300), Money.Euro(600)),  // pro Fenster
        CapExCategory.Electrical => (30, 50, Money.Euro(30), Money.Euro(80)),
        CapExCategory.Plumbing => (30, 50, Money.Euro(40), Money.Euro(100)),
        CapExCategory.Interior => (15, 30, Money.Euro(20), Money.Euro(60)),
        CapExCategory.Energy => (20, 40, Money.Euro(100), Money.Euro(300)),
        _ => (20, 40, Money.Euro(50), Money.Euro(150))
    };
}
