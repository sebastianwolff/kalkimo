using Kalkimo.Domain.Models;

namespace Kalkimo.Domain.Calculators;

/// <summary>
/// Generiert Sanierungsprognosen basierend auf Bauteilzuständen,
/// Gebäudedaten und Standard-Lebenszyklusdaten.
/// </summary>
public static class RenovationForecastGenerator
{
    private const decimal AnnualInflation = 0.03m; // 3% p.a. Baukostensteigerung

    /// <summary>
    /// Standard-Gebäude-Bauteilkategorien
    /// </summary>
    private static readonly CapExCategory[] BuildingCategories =
    [
        CapExCategory.Heating,
        CapExCategory.Roof,
        CapExCategory.Facade,
        CapExCategory.Windows,
        CapExCategory.Electrical,
        CapExCategory.Plumbing,
        CapExCategory.Interior,
        CapExCategory.Energy
    ];

    /// <summary>
    /// Standard-Einheit-Bauteilkategorien
    /// </summary>
    private static readonly CapExCategory[] UnitCategories =
    [
        CapExCategory.Kitchen,
        CapExCategory.Bathroom,
        CapExCategory.UnitRenovation,
        CapExCategory.UnitOther
    ];

    /// <summary>
    /// Generiert eine Liste empfohlener CapEx-Maßnahmen basierend auf den
    /// Bauteilzuständen der Immobilie (Gebäude + Einheiten) und den Standard-Lebenszyklen.
    /// </summary>
    /// <param name="property">Immobilienobjekt mit Bauteilzuständen</param>
    /// <param name="startPeriod">Beginn des Analysezeitraums</param>
    /// <param name="endPeriod">Ende des Analysezeitraums</param>
    /// <param name="currency">Währung für Kostenberechnung</param>
    /// <returns>Sortierte Liste empfohlener Maßnahmen (Priorität absteigend, dann chronologisch)</returns>
    public static IReadOnlyList<CapExMeasure> GenerateForecast(
        Property property,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var measures = new List<CapExMeasure>();

        // Gebäude-Bauteile
        measures.AddRange(GenerateBuildingForecast(property, startPeriod, endPeriod, currency));

        // Einheit-Bauteile (pro Mieteinheit)
        foreach (var unit in property.Units)
        {
            if (unit.Components.Count == 0) continue;
            measures.AddRange(GenerateUnitForecast(unit, property, startPeriod, endPeriod, currency));
        }

        // Sortierung: Priorität (Critical zuerst), dann chronologisch
        return measures
            .OrderBy(m => m.Priority)
            .ThenBy(m => m.PlannedPeriod)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Generiert Maßnahmen für Gebäude-Bauteile (Dach, Heizung, Fassade etc.)
    /// </summary>
    private static IEnumerable<CapExMeasure> GenerateBuildingForecast(
        Property property,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency)
    {
        var currentYear = DateTime.Now.Year;

        foreach (var category in BuildingCategories)
        {
            var component = property.Components.FirstOrDefault(c => c.Category == category);
            var areaRef = GetBuildingAreaReference(category, property);

            var measure = GenerateMeasureForComponent(
                category, component, property.ConstructionYear, property.OverallCondition,
                areaRef, startPeriod, endPeriod, currentYear, currency, unitId: null, unitName: null);

            if (measure != null)
                yield return measure;
        }
    }

    /// <summary>
    /// Generiert Maßnahmen für Einheit-Bauteile (Küche, Bad, Grundrenovierung etc.)
    /// </summary>
    private static IEnumerable<CapExMeasure> GenerateUnitForecast(
        Unit unit,
        Property property,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency)
    {
        var currentYear = DateTime.Now.Year;

        foreach (var category in UnitCategories)
        {
            var component = unit.Components.FirstOrDefault(c => c.Category == category);
            if (component == null) continue; // Nur erfasste Einheit-Bauteile

            var measure = GenerateMeasureForComponent(
                category, component, property.ConstructionYear, property.OverallCondition,
                unit.Area, startPeriod, endPeriod, currentYear, currency,
                unitId: unit.Id, unitName: unit.Name);

            if (measure != null)
                yield return measure;
        }
    }

    /// <summary>
    /// Generiert eine einzelne Maßnahme für ein Bauteil (Gebäude oder Einheit).
    /// </summary>
    private static CapExMeasure? GenerateMeasureForComponent(
        CapExCategory category,
        ComponentCondition? component,
        int constructionYear,
        Condition overallCondition,
        decimal areaReference,
        YearMonth startPeriod,
        YearMonth endPeriod,
        int currentYear,
        string currency,
        string? unitId,
        string? unitName)
    {
        var cycleData = DefaultComponentCycles.GetCycle(category);

        // Letzte Sanierung: explizit > Baujahr
        var lastRenovationYear = component?.LastRenovationYear ?? constructionYear;

        // Erwarteter Zyklus: explizit > Default-Mittelwert
        var cycleYears = component?.ExpectedCycleYears > 0
            ? component.ExpectedCycleYears
            : (cycleData.MinYears + cycleData.MaxYears) / 2;

        // Bauteilzustand für Kosteninterpolation
        var componentCondition = component?.Condition ?? overallCondition;

        // Nächste Erneuerung berechnen
        var nextRenewalYear = lastRenovationYear + cycleYears;

        // Nur Maßnahmen im Analysezeitraum
        if (nextRenewalYear > endPeriod.Year)
            return null;

        // Geplantes Jahr mindestens ab Analysebeginn
        var plannedYear = Math.Max(nextRenewalYear, startPeriod.Year);
        // Aktuelles Alter des Bauteils
        var componentAge = currentYear - lastRenovationYear;

        // Kostenberechnung
        var costFactor = GetConditionCostFactor(componentCondition);
        var baseCostPerUnit = cycleData.CostPerSqmMin.Amount +
            (cycleData.CostPerSqmMax.Amount - cycleData.CostPerSqmMin.Amount) * costFactor;

        var estimatedCost = baseCostPerUnit * areaReference;

        // Inflationsanpassung bis zum geplanten Jahr
        var yearsUntil = Math.Max(0, plannedYear - currentYear);
        estimatedCost *= (decimal)Math.Pow(1 + (double)AnnualInflation, yearsUntil);

        // Auf 100€ runden
        estimatedCost = Math.Round(estimatedCost / 100) * 100;

        // Priorität ableiten
        var priority = DerivePriority(componentAge, cycleYears);

        // Maßnahmenbezeichnung (mit Einheit-Präfix falls vorhanden)
        var baseName = GetMeasureName(category);
        var measureName = unitName != null ? $"{unitName}: {baseName}" : baseName;

        return new CapExMeasure
        {
            Id = Guid.NewGuid().ToString(),
            Name = measureName,
            Category = category,
            PlannedPeriod = new YearMonth(plannedYear, 1),
            EstimatedCost = new Money(estimatedCost, currency),
            TaxClassification = TaxClassification.MaintenanceExpense,
            IsNecessary = priority == MeasurePriority.Critical || priority == MeasurePriority.High,
            Priority = priority,
            IsValueEnhancing = false,
            IsExecuted = false,
            UnitId = unitId
        };
    }

    /// <summary>
    /// Bestimmt die relevante Fläche/Menge für Gebäude-Bauteile.
    /// </summary>
    private static decimal GetBuildingAreaReference(CapExCategory category, Property property) => category switch
    {
        // Dach und Fassade beziehen sich auf die Gesamtfläche
        CapExCategory.Roof => property.TotalArea,
        CapExCategory.Facade => property.TotalArea,

        // Fenster: Schätzung 1 Fenster pro 8m² Wohnfläche
        CapExCategory.Windows => Math.Ceiling(property.LivingArea / 8),

        // Energetische Sanierung bezieht sich auf Gesamtfläche
        CapExCategory.Energy => property.TotalArea,

        // Alle anderen auf Wohnfläche
        _ => property.LivingArea
    };

    /// <summary>
    /// Berechnet den Kostenfaktor basierend auf dem Bauteilzustand.
    /// 0 = Min-Kosten (gut), 1 = Max-Kosten (schlecht)
    /// </summary>
    private static decimal GetConditionCostFactor(Condition condition) => condition switch
    {
        Condition.Good => 0.2m,
        Condition.Fair => 0.5m,
        Condition.Poor => 1.0m,
        _ => 0.5m
    };

    /// <summary>
    /// Leitet die Priorität aus Alter und erwartetem Zyklus ab.
    /// </summary>
    private static MeasurePriority DerivePriority(int componentAge, int cycleYears)
    {
        if (cycleYears <= 0) return MeasurePriority.Medium;

        var ratio = (decimal)componentAge / cycleYears;
        return ratio switch
        {
            >= 1.0m => MeasurePriority.Critical,
            >= 0.8m => MeasurePriority.High,
            >= 0.6m => MeasurePriority.Medium,
            _ => MeasurePriority.Low
        };
    }

    /// <summary>
    /// Standard-Maßnahmenbezeichnung pro Kategorie.
    /// </summary>
    private static string GetMeasureName(CapExCategory category) => category switch
    {
        CapExCategory.Heating => "Heizungserneuerung",
        CapExCategory.Roof => "Dachsanierung",
        CapExCategory.Facade => "Fassadensanierung",
        CapExCategory.Windows => "Fenstererneuerung",
        CapExCategory.Electrical => "Elektrikerneuerung",
        CapExCategory.Plumbing => "Sanitärerneuerung",
        CapExCategory.Interior => "Innenausbau-Erneuerung",
        CapExCategory.Energy => "Energetische Sanierung",
        CapExCategory.Kitchen => "Küchenerneuerung",
        CapExCategory.Bathroom => "Sanitärausstattung-Erneuerung",
        CapExCategory.UnitRenovation => "Grundrenovierung",
        CapExCategory.UnitOther => "Sonstige Wohnungssanierung",
        _ => "Sonstige Sanierung"
    };
}
