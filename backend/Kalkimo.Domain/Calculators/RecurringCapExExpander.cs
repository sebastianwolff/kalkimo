using Kalkimo.Domain.Models;

namespace Kalkimo.Domain.Calculators;

/// <summary>
/// Expandiert wiederkehrende CapEx-Maßnahmen in einzelne Vorkommen
/// innerhalb des Analysezeitraums.
/// </summary>
public static class RecurringCapExExpander
{
    /// <summary>
    /// Expandiertes Vorkommen einer wiederkehrenden Maßnahme
    /// </summary>
    public record RecurringOccurrence
    {
        public required int Year { get; init; }
        public required int Month { get; init; }
        public required Money Amount { get; init; }
        public required CapExCategory Category { get; init; }
        public required TaxClassification TaxClassification { get; init; }
        public required string Name { get; init; }
        public required string SourceMeasureId { get; init; }
    }

    /// <summary>
    /// Expandiert alle wiederkehrenden Maßnahmen in einzelne Vorkommen
    /// </summary>
    public static IReadOnlyList<RecurringOccurrence> Expand(
        CapExConfiguration? capExConfig,
        Property property,
        int startYear,
        int endYear,
        string currency = "EUR")
    {
        if (capExConfig == null) return [];

        var occurrences = new List<RecurringOccurrence>();

        foreach (var measure in capExConfig.Measures)
        {
            if (!measure.IsRecurring || measure.RecurringConfig == null) continue;

            var comp = property.Components.FirstOrDefault(c => c.Category == measure.Category);
            var (minYears, maxYears, _, _) = DefaultComponentCycles.GetCycle(measure.Category);
            var cycle = comp?.ExpectedCycleYears ?? (minYears + maxYears) / 2;

            var intervalYears = (int)Math.Round(cycle * measure.RecurringConfig.IntervalPercent / 100);
            if (intervalYears <= 0) continue;

            var renewalCost = PropertyValueForecastCalculator.CalculateComponentRenewalCost(
                measure.Category, property);
            var costPerEvent = Math.Round(renewalCost * measure.RecurringConfig.CostPercent / 100 / 100) * 100;
            if (costPerEvent <= 0) continue;

            var lastReno = comp?.LastRenovationYear ?? property.ConstructionYear;

            var idx = 0;
            for (var age = intervalYears; lastReno + age <= endYear; age += intervalYears)
            {
                var year = lastReno + age;
                if (year >= startYear)
                {
                    idx++;
                    occurrences.Add(new RecurringOccurrence
                    {
                        Year = year,
                        Month = measure.PlannedPeriod.Month,
                        Amount = new Money(costPerEvent, currency),
                        Category = measure.Category,
                        TaxClassification = measure.TaxClassification,
                        Name = $"{measure.Name} (#{idx})",
                        SourceMeasureId = measure.Id
                    });
                }
            }
        }

        return occurrences;
    }

    /// <summary>
    /// Erstellt eine erweiterte CapExConfiguration mit expandierten wiederkehrenden Maßnahmen
    /// </summary>
    public static CapExConfiguration ExpandIntoConfiguration(
        CapExConfiguration? original,
        Property property,
        int startYear,
        int endYear,
        string currency = "EUR")
    {
        var baseMeasures = original?.Measures.ToList() ?? [];
        var occurrences = Expand(original, property, startYear, endYear, currency);

        var expandedMeasures = occurrences.Select((occ, i) => new CapExMeasure
        {
            Id = $"{occ.SourceMeasureId}_recurring_{i}",
            Name = occ.Name,
            Category = occ.Category,
            PlannedPeriod = new YearMonth(occ.Year, occ.Month),
            EstimatedCost = occ.Amount,
            TaxClassification = occ.TaxClassification,
            IsExecuted = true,
            IsNecessary = false,
            Priority = MeasurePriority.Medium
        }).ToList();

        return new CapExConfiguration
        {
            Measures = baseMeasures.Concat(expandedMeasures).ToList()
        };
    }
}
