namespace Kalkimo.Domain.Models;

/// <summary>
/// Ergebnis der Immobilienwert-Prognose (Multi-Szenario)
/// </summary>
public record PropertyValueForecastResult
{
    public required decimal PurchasePrice { get; init; }
    public required decimal ImprovementValueFactor { get; init; }
    public required decimal InitialConditionFactor { get; init; }
    public MarketComparison? MarketComparison { get; init; }
    public ComponentDeteriorationSummary? ComponentDeterioration { get; init; }
    public required IReadOnlyList<ForecastDriver> Drivers { get; init; }
    public required IReadOnlyList<PropertyValueScenario> Scenarios { get; init; }
}

/// <summary>
/// Einzelnes Szenario der Wertprognose
/// </summary>
public record PropertyValueScenario
{
    public required string Label { get; init; }
    public required decimal AnnualAppreciationPercent { get; init; }
    public required IReadOnlyList<PropertyValueRow> YearlyValues { get; init; }
    public required decimal FinalValue { get; init; }
}

/// <summary>
/// Jahreszeile der Wertprognose
/// </summary>
public record PropertyValueRow
{
    public int Year { get; init; }
    public decimal MarketValue { get; init; }
    public decimal ConditionFactor { get; init; }
    public decimal ConditionDelta { get; init; }
    public decimal ImprovementUplift { get; init; }
    public decimal MeanReversionAdjustment { get; init; }
    public decimal ComponentDeteriorationCumulative { get; init; }
    public decimal EstimatedValue { get; init; }
}

/// <summary>
/// Marktvergleich (Kaufpreis vs. regionaler Durchschnitt)
/// </summary>
public record MarketComparison
{
    public decimal RegionalPricePerSqm { get; init; }
    public decimal LivingArea { get; init; }
    public decimal FairMarketValue { get; init; }
    public decimal PurchasePriceToMarketRatio { get; init; }

    /// <summary>"below", "at", "above"</summary>
    public required string Assessment { get; init; }
}

/// <summary>
/// Zusammenfassung der Bauteil-Verschlechterung
/// </summary>
public record ComponentDeteriorationSummary
{
    public required IReadOnlyList<ComponentDeteriorationRow> Components { get; init; }
    public decimal TotalValueImpact { get; init; }
    public decimal TotalRenewalCostIfAllDone { get; init; }
    public decimal CoveredByCapex { get; init; }
    public decimal UncoveredDeterioration { get; init; }
}

/// <summary>
/// Verschlechterungszeile pro Bauteil
/// </summary>
public record ComponentDeteriorationRow
{
    public CapExCategory Category { get; init; }
    public int AgeAtStart { get; init; }
    public int AgeAtEnd { get; init; }
    public int CycleYears { get; init; }
    public int DueYear { get; init; }
    public decimal RenewalCostEstimate { get; init; }
    public int? CapexAddressedYear { get; init; }
    public decimal ValueImpact { get; init; }

    /// <summary>"OK", "Overdue", "OverdueAtPurchase", "Renewed"</summary>
    public required string StatusAtEnd { get; init; }

    public RecurringMaintenanceInfo? RecurringMaintenance { get; init; }
}

/// <summary>
/// Info über wiederkehrende Wartungsmaßnahmen eines Bauteils
/// </summary>
public record RecurringMaintenanceInfo
{
    public required string Name { get; init; }
    public int IntervalYears { get; init; }
    public decimal CostPerOccurrence { get; init; }
    public int OccurrencesInPeriod { get; init; }
    public decimal TotalCostInPeriod { get; init; }
    public int EffectiveCycleYears { get; init; }
    public decimal ValueImprovement { get; init; }
}

/// <summary>
/// Erklärungstreiber für Prognose-Transparenz
/// </summary>
public record ForecastDriver
{
    public required string Type { get; init; }
    public Dictionary<string, object> Params { get; init; } = new();
}
