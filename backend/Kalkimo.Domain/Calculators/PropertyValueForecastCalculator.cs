using Kalkimo.Domain.Models;

namespace Kalkimo.Domain.Calculators;

/// <summary>
/// Berechnet die Immobilienwert-Prognose mit Multi-Szenario-Modell.
///
/// Kombiniert:
/// - Marktwertentwicklung (3 Szenarien: 0%, 1,5%, 3% p.a.)
/// - Zustandsfaktor-Modell (bauteilbasierte Degradation)
/// - Bauteil-Verschlechterung (kostenbasiert, exponentielle Alterung)
/// - Mean-Reversion zum regionalen Marktwert
/// - Improvement Value Factor (70% der CapEx-Investitionen)
/// </summary>
public static class PropertyValueForecastCalculator
{
    private const decimal ImprovementValueFactor = 0.70m;
    private const decimal BaseAgingRate = 0.005m;
    private const decimal AgingAcceleration = 0.005m;
    private const int ReferenceLifecycle = 60;
    private const int MeanReversionHalfLife = 7;
    private const int DeteriorationExponent = 4;

    private static readonly (string Label, decimal Rate)[] ScenarioDefs =
    [
        ("conservative", 0.0m),
        ("base", 1.5m),
        ("optimistic", 3.0m)
    ];

    /// <summary>
    /// Berechnet die vollständige Wertprognose für ein Projekt
    /// </summary>
    public static PropertyValueForecastResult Calculate(
        Project project,
        int startYear,
        int endYear)
    {
        var purchasePrice = project.Purchase.PurchasePrice.Amount;
        var livingArea = project.Property.LivingArea;
        var totalYears = endYear - startYear;
        var components = project.Property.Components;
        var capexMeasures = project.CapEx?.Measures ?? [];

        // === Market comparison ===
        MarketComparison? marketComparison = null;
        if (project.Property.RegionalPricePerSqm.HasValue &&
            project.Property.RegionalPricePerSqm.Value > 0)
        {
            var fmv = project.Property.RegionalPricePerSqm.Value * livingArea;
            var ratio = fmv > 0 ? purchasePrice / fmv : 1m;
            var assessment = ratio < 0.95m ? "below" : ratio > 1.05m ? "above" : "at";
            marketComparison = new MarketComparison
            {
                RegionalPricePerSqm = project.Property.RegionalPricePerSqm.Value,
                LivingArea = livingArea,
                FairMarketValue = fmv,
                PurchasePriceToMarketRatio = ratio,
                Assessment = assessment
            };
        }

        var fairMarketValue = marketComparison?.FairMarketValue;
        var useMeanReversion = fairMarketValue.HasValue && fairMarketValue.Value > 0;

        // === Initial condition factor (Gebäude- + Einheit-Bauteile) ===
        var allComponents = components
            .Concat(project.Property.Units.SelectMany(u => u.Components))
            .ToList();

        decimal initialConditionFactor;
        if (allComponents.Count > 0)
        {
            var factors = allComponents.Select(c =>
            {
                var baseFactor = ConditionToFactor(c.Condition);
                var lastReno = c.LastRenovationYear ?? project.Property.ConstructionYear;
                var age = startYear - lastReno;
                var overdue = Math.Max(0, age - c.ExpectedCycleYears);
                var agingPenalty = overdue * 0.015m;
                return Math.Max(0.5m, baseFactor - agingPenalty);
            }).ToList();
            initialConditionFactor = factors.Average();
        }
        else
        {
            initialConditionFactor = ConditionToFactor(project.Property.OverallCondition);
        }

        // === Overdue components (Gebäude + Einheiten) ===
        var overdueComponents = allComponents
            .Select(c =>
            {
                var lastReno = c.LastRenovationYear ?? project.Property.ConstructionYear;
                var age = startYear - lastReno;
                return new { Name = c.Category.ToString(), OverdueYears = age - c.ExpectedCycleYears };
            })
            .Where(x => x.OverdueYears > 0)
            .ToList();

        // === Index CapEx by year ===
        var capexByYear = new Dictionary<int, (decimal Improvement, decimal Maintenance, Dictionary<CapExCategory, string> Categories)>();
        decimal totalCapexImprovement = 0;
        decimal totalCapexMaintenance = 0;
        int capexMeasureCount = 0;

        foreach (var measure in capexMeasures)
        {
            var y = measure.PlannedPeriod.Year;
            if (y < startYear || y > endYear || measure.EstimatedCost.Amount == 0) continue;
            capexMeasureCount++;

            if (!capexByYear.TryGetValue(y, out var entry))
            {
                entry = (0, 0, new Dictionary<CapExCategory, string>());
                capexByYear[y] = entry;
            }

            var isImprovement =
                measure.TaxClassification == TaxClassification.ManufacturingCosts ||
                measure.TaxClassification == TaxClassification.AcquisitionRelatedCosts;

            if (isImprovement)
            {
                entry.Improvement += measure.EstimatedCost.Amount;
                totalCapexImprovement += measure.EstimatedCost.Amount;
                entry.Categories[measure.Category] = "improvement";
            }
            else if (measure.TaxClassification == TaxClassification.MaintenanceExpense ||
                     measure.TaxClassification == TaxClassification.MaintenanceExpenseDistributed)
            {
                entry.Maintenance += measure.EstimatedCost.Amount;
                totalCapexMaintenance += measure.EstimatedCost.Amount;
                if (!entry.Categories.ContainsKey(measure.Category))
                    entry.Categories[measure.Category] = "maintenance";
            }

            capexByYear[y] = entry;
        }

        // === Component deterioration ===
        var componentDet = CalculateComponentDeterioration(
            project.Property, capexMeasures, startYear, endYear);
        var useComponentDeterioration = componentDet != null && componentDet.Components.Count > 0;

        // === Build scenarios ===
        decimal baseEndConditionFactor = initialConditionFactor;
        decimal baseTotalConditionBoost = 0;

        var scenarios = ScenarioDefs.Select(def =>
        {
            var yearlyValues = new List<PropertyValueRow>();
            decimal cumulativeImprovement = 0;

            // Per-component tracking
            var componentStates = components.Select(c =>
            {
                var lastReno = c.LastRenovationYear ?? project.Property.ConstructionYear;
                var age = startYear - lastReno;
                var overdue = Math.Max(0, age - c.ExpectedCycleYears);
                return new ComponentState
                {
                    Category = c.Category,
                    Age = age,
                    Cycle = c.ExpectedCycleYears,
                    Factor = Math.Max(0.5m, ConditionToFactor(c.Condition) - overdue * 0.015m)
                };
            }).ToList();

            // Simple-model tracking
            var simpleConditionFactor = initialConditionFactor;
            var simpleBuildingAge = startYear - project.Property.ConstructionYear;
            var conditionFactor = initialConditionFactor;
            decimal conditionBoostFromCapex = 0;

            for (var year = startYear; year <= endYear; year++)
            {
                var yearsFromStart = year - startYear;
                capexByYear.TryGetValue(year, out var capex);
                var previousConditionFactor = conditionFactor;

                // Improvement uplift
                if (capex.Improvement > 0)
                    cumulativeImprovement += capex.Improvement * ImprovementValueFactor;

                // Condition factor update
                if (componentStates.Count > 0)
                {
                    foreach (var comp in componentStates)
                    {
                        comp.Age++;
                        string? capexType = null;
                        capex.Categories?.TryGetValue(comp.Category, out capexType);

                        if (capexType == "improvement")
                        {
                            var boost = Math.Min(1.0m - comp.Factor, 0.30m);
                            comp.Factor = Math.Min(1.0m, comp.Factor + 0.30m);
                            comp.Age = 0;
                            conditionBoostFromCapex += boost;
                        }
                        else if (capexType == "maintenance")
                        {
                            var boost = Math.Min(1.0m - comp.Factor, 0.15m);
                            comp.Factor = Math.Min(1.0m, comp.Factor + 0.15m);
                            comp.Age /= 2;
                            conditionBoostFromCapex += boost;
                        }
                        else
                        {
                            var degradation = ComponentDegradationRate(comp.Age, comp.Cycle);
                            comp.Factor = Math.Max(0.50m, comp.Factor - degradation);
                        }
                    }
                    conditionFactor = componentStates.Average(c => c.Factor);
                }
                else
                {
                    simpleBuildingAge++;
                    var degradation = BaseAgingRate + AgingAcceleration * ((decimal)simpleBuildingAge / ReferenceLifecycle);
                    simpleConditionFactor = Math.Max(0.50m, simpleConditionFactor - degradation);

                    if (capex.Maintenance > 0)
                    {
                        var boost = Math.Min(1.0m - simpleConditionFactor, 0.05m);
                        simpleConditionFactor = Math.Min(1.0m, simpleConditionFactor + 0.05m);
                        conditionBoostFromCapex += boost;
                    }
                    if (capex.Improvement > 0)
                    {
                        var boost = Math.Min(1.0m - simpleConditionFactor, 0.08m);
                        simpleConditionFactor = Math.Min(1.0m, simpleConditionFactor + 0.08m);
                        conditionBoostFromCapex += boost;
                    }
                    conditionFactor = simpleConditionFactor;
                }

                var conditionDelta = conditionFactor - previousConditionFactor;

                // Market value (pure appreciation)
                var marketValue = purchasePrice * (decimal)Math.Pow((double)(1 + def.Rate / 100), yearsFromStart);

                // Component deterioration (cost-based, EUR)
                decimal componentDeteriorationCumulative = useComponentDeterioration
                    ? YearlyComponentDeterioration(componentDet!, year, startYear)
                    : 0;

                // Estimated value
                decimal estimatedValue;
                if (useComponentDeterioration)
                {
                    estimatedValue = marketValue + componentDeteriorationCumulative + cumulativeImprovement;
                }
                else
                {
                    var conditionRatio = initialConditionFactor > 0 ? conditionFactor / initialConditionFactor : 1;
                    estimatedValue = marketValue * conditionRatio + cumulativeImprovement;
                }

                // Mean reversion
                decimal meanReversionAdjustment = 0;
                if (useMeanReversion && fairMarketValue.HasValue)
                {
                    var gap = fairMarketValue.Value - purchasePrice;
                    var gapAtYear = gap * (decimal)Math.Pow((double)(1 + def.Rate / 100), yearsFromStart);
                    var closedPortion = 1m - (decimal)Math.Pow(0.5, (double)yearsFromStart / MeanReversionHalfLife);
                    meanReversionAdjustment = gapAtYear * closedPortion;
                    estimatedValue += meanReversionAdjustment;
                }

                yearlyValues.Add(new PropertyValueRow
                {
                    Year = year,
                    MarketValue = marketValue,
                    ConditionFactor = conditionFactor,
                    ConditionDelta = conditionDelta,
                    ImprovementUplift = cumulativeImprovement,
                    MeanReversionAdjustment = meanReversionAdjustment,
                    ComponentDeteriorationCumulative = componentDeteriorationCumulative,
                    EstimatedValue = estimatedValue
                });
            }

            if (def.Label == "base")
            {
                baseEndConditionFactor = conditionFactor;
                baseTotalConditionBoost = conditionBoostFromCapex;
            }

            return new PropertyValueScenario
            {
                Label = def.Label,
                AnnualAppreciationPercent = def.Rate,
                YearlyValues = yearlyValues,
                FinalValue = yearlyValues.Count > 0 ? yearlyValues[^1].EstimatedValue : purchasePrice
            };
        }).ToList();

        // === Forecast drivers ===
        var drivers = BuildForecastDrivers(
            project, components, scenarios, componentDet,
            initialConditionFactor, baseEndConditionFactor, baseTotalConditionBoost,
            overdueComponents, capexMeasureCount,
            totalCapexImprovement, totalCapexMaintenance,
            purchasePrice, totalYears, marketComparison, useMeanReversion);

        return new PropertyValueForecastResult
        {
            PurchasePrice = purchasePrice,
            ImprovementValueFactor = ImprovementValueFactor,
            InitialConditionFactor = initialConditionFactor,
            MarketComparison = marketComparison,
            ComponentDeterioration = componentDet,
            Drivers = drivers,
            Scenarios = scenarios
        };
    }

    // === Condition to factor mapping ===

    internal static decimal ConditionToFactor(Condition condition) => condition switch
    {
        Condition.New => 1.0m,
        Condition.Good => 0.95m,
        Condition.Fair => 0.85m,
        Condition.Poor => 0.70m,
        Condition.NeedsRenovation => 0.55m,
        _ => 0.85m
    };

    // === Three-tier degradation rate ===

    private static decimal ComponentDegradationRate(int age, int cycle)
    {
        if (cycle <= 0) return 0.015m;
        var ratio = (decimal)age / cycle;
        if (ratio <= 0.7m) return 0.003m;
        if (ratio <= 1.0m) return 0.008m;
        return 0.015m;
    }

    // === Exponential aging fraction: (age/cycle)^4 capped at 1 ===

    internal static decimal AgeFraction(int age, decimal cycle)
    {
        if (cycle <= 0) return 1;
        var ratio = Math.Max(0, (double)age) / (double)cycle;
        return (decimal)Math.Min(1.0, Math.Pow(ratio, DeteriorationExponent));
    }

    // === Component renewal cost estimate ===

    internal static decimal CalculateComponentRenewalCost(CapExCategory category, Property property, Unit? unit = null)
    {
        var (_, _, _, costMax) = DefaultComponentCycles.GetCycle(category);
        var areaRef = GetUnitCountForCategory(category, property, unit);
        return Math.Round(costMax.Amount * areaRef / 100) * 100;
    }

    private static decimal GetUnitCountForCategory(CapExCategory category, Property property, Unit? unit = null)
    {
        // Einheit-Bauteile: Kosten basieren auf Einheitsfläche
        if (category.IsUnitLevel() && unit != null)
            return unit.Area;

        // Gebäude-Bauteile
        return category switch
        {
            CapExCategory.Roof or CapExCategory.Facade or CapExCategory.Energy => property.TotalArea,
            CapExCategory.Windows => Math.Ceiling(property.LivingArea / 8), // ~1 Fenster pro 8m²
            _ => property.LivingArea // Heating, Electrical, Plumbing, Interior
        };
    }

    // === Component deterioration summary ===

    internal static ComponentDeteriorationSummary? CalculateComponentDeterioration(
        Property property,
        IReadOnlyList<CapExMeasure> capexMeasures,
        int startYear,
        int endYear)
    {
        // Alle Bauteile sammeln: Gebäude + Einheiten
        var componentEntries = property.Components
            .Select(c => (Component: c, UnitId: (string?)null, Unit: (Unit?)null))
            .Concat(property.Units.SelectMany(u =>
                u.Components.Select(c => (Component: c, UnitId: (string?)u.Id, Unit: (Unit?)u))))
            .ToList();

        if (componentEntries.Count == 0) return null;

        var holdingYears = endYear - startYear;

        // Index CapEx by (category, unitId) — unitId null für Gebäudeebene
        var capexKey = new Dictionary<(CapExCategory, string?), List<(int Year, decimal Amount)>>();
        var recurringKey = new Dictionary<(CapExCategory, string?), (string Name, RecurringMeasureConfig Config)>();

        foreach (var measure in capexMeasures)
        {
            var key = (measure.Category, measure.UnitId);

            if (measure.IsRecurring && measure.RecurringConfig != null)
                recurringKey[key] = (measure.Name, measure.RecurringConfig);

            if (measure.EstimatedCost.Amount > 0)
            {
                var y = measure.PlannedPeriod.Year;
                if (y >= startYear && y <= endYear)
                {
                    if (!capexKey.TryGetValue(key, out var list))
                    {
                        list = [];
                        capexKey[key] = list;
                    }
                    list.Add((y, measure.EstimatedCost.Amount));
                }
            }
        }

        var rows = new List<ComponentDeteriorationRow>();
        decimal totalValueImpact = 0;
        decimal totalRenewalCostIfAllDone = 0;
        decimal coveredByCapex = 0;
        decimal uncoveredDeterioration = 0;

        foreach (var (comp, unitId, unit) in componentEntries)
        {
            var lastReno = comp.LastRenovationYear ?? property.ConstructionYear;
            var ageAtStart = startYear - lastReno;
            var cycle = comp.ExpectedCycleYears;
            var dueYear = lastReno + cycle;

            var renewalCost = CalculateComponentRenewalCost(comp.Category, property, unit);
            totalRenewalCostIfAllDone += renewalCost;

            // Check if CapEx addresses this component (matched by category + unitId)
            var key = (comp.Category, unitId);
            var measures = capexKey.GetValueOrDefault(key, []);
            (int Year, decimal Amount)? matchingCapex = null;
            if (measures.Count > 0)
            {
                matchingCapex = measures.FirstOrDefault(c => c.Year >= dueYear - 1);
                if (matchingCapex?.Year == 0)
                    matchingCapex = measures[0];
            }

            // Recurring maintenance
            recurringKey.TryGetValue(key, out var recurring);
            var effectiveCycle = recurring.Config != null
                ? cycle * (1 + recurring.Config.CycleExtensionPercent / 100)
                : (decimal)cycle;

            var startFraction = AgeFraction(ageAtStart, effectiveCycle);
            int ageAtEnd;
            decimal valueImpact;
            string statusAtEnd;

            if (matchingCapex.HasValue)
            {
                ageAtEnd = endYear - matchingCapex.Value.Year;
                dueYear = matchingCapex.Value.Year + (int)Math.Round(effectiveCycle);
                var postRenewalFraction = AgeFraction(ageAtEnd, effectiveCycle);
                valueImpact = postRenewalFraction > 0.001m ? -renewalCost * postRenewalFraction : 0;
                coveredByCapex += matchingCapex.Value.Amount;
                statusAtEnd = "Renewed";
            }
            else
            {
                ageAtEnd = ageAtStart + holdingYears;
                var endFraction = AgeFraction(ageAtEnd, effectiveCycle);
                var delta = endFraction - startFraction;
                valueImpact = delta > 0.001m ? -renewalCost * delta : 0;
                uncoveredDeterioration += Math.Abs(valueImpact);

                if (startFraction >= 0.999m)
                    statusAtEnd = "OverdueAtPurchase";
                else if (endFraction >= 0.999m)
                    statusAtEnd = "Overdue";
                else
                    statusAtEnd = "OK";
            }

            // Recurring maintenance info
            RecurringMaintenanceInfo? recurringInfo = null;
            if (recurring.Config != null)
            {
                var intervalYears = (int)Math.Round(cycle * recurring.Config.IntervalPercent / 100);
                var costPerOccurrence = Math.Round(renewalCost * recurring.Config.CostPercent / 100 / 100) * 100;
                var refYear = matchingCapex?.Year ?? lastReno;
                var occurrences = 0;
                if (intervalYears > 0)
                {
                    for (var age = intervalYears; refYear + age <= endYear; age += intervalYears)
                    {
                        if (refYear + age >= startYear)
                            occurrences++;
                    }
                }

                var fractionOriginal = matchingCapex.HasValue
                    ? AgeFraction(ageAtEnd, cycle)
                    : AgeFraction(ageAtEnd, cycle) - AgeFraction(ageAtStart, cycle);
                var fractionExtended = matchingCapex.HasValue
                    ? AgeFraction(ageAtEnd, effectiveCycle)
                    : AgeFraction(ageAtEnd, effectiveCycle) - AgeFraction(ageAtStart, effectiveCycle);
                var valueImprovement = renewalCost * Math.Max(0, fractionOriginal - fractionExtended);

                recurringInfo = new RecurringMaintenanceInfo
                {
                    Name = recurring.Name,
                    IntervalYears = intervalYears,
                    CostPerOccurrence = costPerOccurrence,
                    OccurrencesInPeriod = occurrences,
                    TotalCostInPeriod = occurrences * costPerOccurrence,
                    EffectiveCycleYears = (int)Math.Round(effectiveCycle),
                    ValueImprovement = Math.Round(valueImprovement)
                };
            }

            totalValueImpact += valueImpact;

            rows.Add(new ComponentDeteriorationRow
            {
                Category = comp.Category,
                AgeAtStart = ageAtStart,
                AgeAtEnd = ageAtEnd,
                CycleYears = recurring.Config != null ? (int)Math.Round(effectiveCycle) : cycle,
                DueYear = dueYear,
                RenewalCostEstimate = renewalCost,
                CapexAddressedYear = matchingCapex?.Year,
                ValueImpact = valueImpact,
                StatusAtEnd = statusAtEnd,
                RecurringMaintenance = recurringInfo,
                UnitId = unitId
            });
        }

        return new ComponentDeteriorationSummary
        {
            Components = rows,
            TotalValueImpact = totalValueImpact,
            TotalRenewalCostIfAllDone = totalRenewalCostIfAllDone,
            CoveredByCapex = coveredByCapex,
            UncoveredDeterioration = uncoveredDeterioration
        };
    }

    // === Yearly component deterioration ===

    private static decimal YearlyComponentDeterioration(
        ComponentDeteriorationSummary componentDet,
        int year,
        int startYear)
    {
        decimal cumulative = 0;
        foreach (var row in componentDet.Components)
        {
            var cycle = (decimal)row.CycleYears;

            if (row.CapexAddressedYear.HasValue && year >= row.CapexAddressedYear.Value)
            {
                var postRenewalAge = year - row.CapexAddressedYear.Value;
                var postRenewalFraction = AgeFraction(postRenewalAge, cycle);
                if (postRenewalFraction > 0.001m)
                    cumulative += -row.RenewalCostEstimate * postRenewalFraction;
                continue;
            }

            var currentAge = row.AgeAtStart + (year - startYear);
            var currentFraction = AgeFraction(currentAge, cycle);
            var startFraction = AgeFraction(row.AgeAtStart, cycle);
            var delta = currentFraction - startFraction;

            if (delta > 0.001m)
                cumulative += -row.RenewalCostEstimate * delta;
        }
        return cumulative;
    }

    // === Forecast drivers ===

    private static IReadOnlyList<ForecastDriver> BuildForecastDrivers(
        Project project,
        IReadOnlyList<ComponentCondition> components,
        List<PropertyValueScenario> scenarios,
        ComponentDeteriorationSummary? componentDet,
        decimal initialConditionFactor,
        decimal baseEndConditionFactor,
        decimal baseTotalConditionBoost,
        IEnumerable<dynamic> overdueComponents,
        int capexMeasureCount,
        decimal totalCapexImprovement,
        decimal totalCapexMaintenance,
        decimal purchasePrice,
        int totalYears,
        MarketComparison? marketComparison,
        bool useMeanReversion)
    {
        var drivers = new List<ForecastDriver>();
        var baseScenario = scenarios.FirstOrDefault(s => s.Label == "base");
        var baseFinalValue = baseScenario?.FinalValue ?? purchasePrice;

        // 1. Initial condition
        drivers.Add(new ForecastDriver
        {
            Type = "initialCondition",
            Params = new Dictionary<string, object>
            {
                ["constructionYear"] = project.Property.ConstructionYear,
                ["condition"] = project.Property.OverallCondition.ToString(),
                ["factor"] = Math.Round(initialConditionFactor * 100),
                ["componentCount"] = components.Count
            }
        });

        // 2. Overdue components
        var overdueList = overdueComponents.ToList();
        if (overdueList.Count > 0)
        {
            var avgOverdue = (int)Math.Round(overdueList.Average((Func<dynamic, double>)(c => (double)c.OverdueYears)));
            drivers.Add(new ForecastDriver
            {
                Type = "overdueComponents",
                Params = new Dictionary<string, object>
                {
                    ["count"] = overdueList.Count,
                    ["names"] = string.Join(", ", overdueList.Select((Func<dynamic, string>)(c => (string)c.Name))),
                    ["avgOverdueYears"] = avgOverdue
                }
            });
        }

        // 3. Degradation
        var totalDecline = (int)Math.Round((initialConditionFactor - baseEndConditionFactor) * 100);
        if (totalDecline > 0)
        {
            drivers.Add(new ForecastDriver
            {
                Type = "degradation",
                Params = new Dictionary<string, object>
                {
                    ["startFactor"] = (int)Math.Round(initialConditionFactor * 100),
                    ["endFactor"] = (int)Math.Round(baseEndConditionFactor * 100),
                    ["totalDecline"] = totalDecline,
                    ["years"] = totalYears
                }
            });
        }

        // 3b. Component deterioration
        if (componentDet != null && componentDet.UncoveredDeterioration > 0)
        {
            var uncoveredCount = componentDet.Components.Count(c => c.StatusAtEnd == "Overdue");
            drivers.Add(new ForecastDriver
            {
                Type = "componentDeterioration",
                Params = new Dictionary<string, object>
                {
                    ["uncoveredCount"] = uncoveredCount,
                    ["uncoveredAmount"] = Math.Round(componentDet.UncoveredDeterioration),
                    ["totalComponents"] = componentDet.Components.Count,
                    ["coveredByCapex"] = Math.Round(componentDet.CoveredByCapex)
                }
            });
        }

        // 4. Investments
        var totalCapexAmount = totalCapexImprovement + totalCapexMaintenance;
        if (capexMeasureCount > 0)
        {
            drivers.Add(new ForecastDriver
            {
                Type = "investments",
                Params = new Dictionary<string, object>
                {
                    ["measureCount"] = capexMeasureCount,
                    ["totalAmount"] = Math.Round(totalCapexAmount),
                    ["valueUplift"] = Math.Round(totalCapexImprovement * ImprovementValueFactor),
                    ["conditionBoost"] = (int)Math.Round(baseTotalConditionBoost * 100 / Math.Max(components.Count, 1))
                }
            });
        }

        // 5. Market appreciation
        const decimal baseRate = 1.5m;
        var appreciationMultiplier = (decimal)Math.Pow((double)(1 + baseRate / 100), totalYears);
        var appreciationPercent = Math.Round((appreciationMultiplier - 1) * 1000) / 10;
        drivers.Add(new ForecastDriver
        {
            Type = "marketAppreciation",
            Params = new Dictionary<string, object>
            {
                ["rate"] = baseRate,
                ["years"] = totalYears,
                ["appreciationPercent"] = appreciationPercent,
                ["appreciationAmount"] = Math.Round(purchasePrice * (appreciationMultiplier - 1))
            }
        });

        // 6. Mean reversion
        if (useMeanReversion && marketComparison != null)
        {
            var gapPercent = (int)Math.Round(Math.Abs(1 - marketComparison.PurchasePriceToMarketRatio) * 100);
            var lastBaseRow = baseScenario?.YearlyValues.Count > 0
                ? baseScenario.YearlyValues[^1]
                : null;
            drivers.Add(new ForecastDriver
            {
                Type = "meanReversion",
                Params = new Dictionary<string, object>
                {
                    ["assessment"] = marketComparison.Assessment,
                    ["gapPercent"] = gapPercent,
                    ["adjustmentAmount"] = Math.Round(Math.Abs(lastBaseRow?.MeanReversionAdjustment ?? 0)),
                    ["direction"] = marketComparison.Assessment == "below" ? "Aufholpotenzial" : "Dämpfung"
                }
            });
        }

        // 7. Summary
        var changeAbsolute = baseFinalValue - purchasePrice;
        var changePercent = purchasePrice > 0 ? Math.Round(changeAbsolute / purchasePrice * 1000) / 10 : 0;
        drivers.Add(new ForecastDriver
        {
            Type = "summary",
            Params = new Dictionary<string, object>
            {
                ["years"] = totalYears,
                ["purchasePrice"] = Math.Round(purchasePrice),
                ["finalValue"] = Math.Round(baseFinalValue),
                ["changePercent"] = Math.Abs(changePercent),
                ["changeAbsolute"] = Math.Round(Math.Abs(changeAbsolute)),
                ["changeDirection"] = changeAbsolute >= 0 ? "+" : "-"
            }
        });

        return drivers;
    }

    // === Internal state tracking ===

    private class ComponentState
    {
        public CapExCategory Category { get; init; }
        public int Age { get; set; }
        public int Cycle { get; init; }
        public decimal Factor { get; set; }
    }
}
