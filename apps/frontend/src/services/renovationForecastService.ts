/**
 * Renovation Forecast Service
 *
 * Generates renovation/renewal measure suggestions based on building component
 * ages, condition, and standard lifecycle data. Mirrors the backend
 * RenovationForecastGenerator logic for offline/local use.
 */
import type { Property, CapExCategory, Condition, ComponentCondition } from '@/stores/types';

export type MeasurePriority = 'Critical' | 'High' | 'Medium' | 'Low';

export interface SuggestedImpact {
  costSavingsMonthly: number;
  rentIncreasePercent: number;
  delayMonths: number;
}

export interface ForecastedMeasure {
  category: CapExCategory;
  name: string;
  estimatedCost: number;
  plannedYear: number;
  plannedMonth: number;
  priority: MeasurePriority;
  componentAge: number;
  cycleYears: number;
  reasoning: string;
  suggestedImpact?: SuggestedImpact;
}

interface ComponentCycleData {
  minYears: number;
  maxYears: number;
  costPerSqmMin: number;
  costPerSqmMax: number;
  /** Area calculation mode: 'living' uses livingArea, 'total' uses totalArea, 'perUnit' uses estimated unit count */
  areaMode: 'living' | 'total' | 'perUnit';
  /** For 'perUnit': estimated units per sqm of living area */
  unitsPerSqm?: number;
}

/**
 * Standard component lifecycle data matching backend DefaultComponentCycles.
 * Costs reflect German market prices (2024/2025 level, excl. MwSt.).
 *
 * Heating:    Gas-Brennwert 10–18k€, Wärmepumpe 18–40k€ → ~80–250 €/m² Wohnfläche
 * Roof:       Neueindeckung inkl. Dämmung/Lattung → 150–350 €/m² Dachfläche
 * Facade:     WDVS + Putz/Anstrich → 120–300 €/m² Fassadenfläche
 * Windows:    3-fach Verglasung inkl. Einbau → 600–1.400 € pro Fenstereinheit
 * Electrical: Komplett-Erneuerung UV + Leitungen → 80–160 €/m² Wohnfläche
 * Plumbing:   Steigleitungen + Bäder → 100–220 €/m² Wohnfläche
 * Interior:   Böden, Wände, Türen, Decken → 60–150 €/m² Wohnfläche
 * Exterior:   Wege, Entwässerung, Begrünung → 60–180 €/m² Grundstücksfläche
 */
export const DEFAULT_COMPONENT_CYCLES: Record<CapExCategory, ComponentCycleData> = {
  Heating:    { minYears: 15, maxYears: 25, costPerSqmMin: 80,  costPerSqmMax: 250, areaMode: 'living' },
  Roof:       { minYears: 30, maxYears: 60, costPerSqmMin: 150, costPerSqmMax: 350, areaMode: 'total' },
  Facade:     { minYears: 25, maxYears: 50, costPerSqmMin: 120, costPerSqmMax: 300, areaMode: 'total' },
  Windows:    { minYears: 20, maxYears: 40, costPerSqmMin: 600, costPerSqmMax: 1400, areaMode: 'perUnit', unitsPerSqm: 1 / 8 },
  Electrical: { minYears: 30, maxYears: 50, costPerSqmMin: 80,  costPerSqmMax: 160, areaMode: 'living' },
  Plumbing:   { minYears: 30, maxYears: 50, costPerSqmMin: 100, costPerSqmMax: 220, areaMode: 'living' },
  Interior:   { minYears: 15, maxYears: 30, costPerSqmMin: 60,  costPerSqmMax: 150, areaMode: 'living' },
  Exterior:   { minYears: 20, maxYears: 40, costPerSqmMin: 60,  costPerSqmMax: 180, areaMode: 'total' },
  Other:      { minYears: 20, maxYears: 40, costPerSqmMin: 80,  costPerSqmMax: 200, areaMode: 'living' },
};

/** Default cycle years (midpoint) for a given category */
export function getDefaultCycleYears(category: CapExCategory): number {
  const data = DEFAULT_COMPONENT_CYCLES[category];
  return Math.round((data.minYears + data.maxYears) / 2);
}

/** Map frontend Condition to a cost interpolation factor (0 = min cost, 1 = max cost) */
function conditionCostFactor(condition: Condition): number {
  switch (condition) {
    case 'New':              return 0.0;
    case 'Good':             return 0.2;
    case 'Fair':             return 0.5;
    case 'Poor':             return 0.8;
    case 'NeedsRenovation':  return 1.0;
    default:                 return 0.5;
  }
}

/** Category display names (German) for measure names */
const CATEGORY_NAMES: Record<CapExCategory, string> = {
  Heating:    'Heizungserneuerung',
  Roof:       'Dachsanierung',
  Facade:     'Fassadensanierung',
  Windows:    'Fenstererneuerung',
  Electrical: 'Elektrikerneuerung',
  Plumbing:   'Sanitärerneuerung',
  Interior:   'Innenausbau-Erneuerung',
  Exterior:   'Außenanlagen-Erneuerung',
  Other:      'Sonstige Sanierung',
};

/**
 * Default economic impact data per category.
 * costSavingsBase100: monthly operating cost savings for 100m² living area (EUR)
 * rentIncreasePercent: potential rent increase at re-letting (%)
 * delayMonths: typical construction time (months)
 *
 * Values based on typical German energy renovation effects:
 * - Heating (Wärmepumpe): 80–150 €/Mon. Heizkostenersparnis, 3–5% Mietpotenzial
 * - Fassade (WDVS): 60–120 €/Mon. Einsparung, 5–8% Mietpotenzial
 * - Fenster (3-fach): 30–60 €/Mon. Einsparung, 2–4% Mietpotenzial
 * - Dach (mit Dämmung): 40–80 €/Mon. Einsparung, 2–3% Mietpotenzial
 * - Energetisch (Komplett): 100–200 €/Mon. Einsparung, 5–10% Mietpotenzial
 */
interface DefaultImpactData {
  costSavingsBase100: number; // monthly savings at 100m² reference
  rentIncreasePercent: number;
  delayMonths: number;
}

const DEFAULT_IMPACT_DATA: Partial<Record<CapExCategory, DefaultImpactData>> = {
  Heating:    { costSavingsBase100: 110, rentIncreasePercent: 4, delayMonths: 2 },
  Facade:     { costSavingsBase100: 90,  rentIncreasePercent: 6, delayMonths: 3 },
  Windows:    { costSavingsBase100: 45,  rentIncreasePercent: 3, delayMonths: 1 },
  Roof:       { costSavingsBase100: 60,  rentIncreasePercent: 2.5, delayMonths: 2 },
  Exterior:   { costSavingsBase100: 0,   rentIncreasePercent: 1, delayMonths: 1 },
  Electrical: { costSavingsBase100: 0,   rentIncreasePercent: 1, delayMonths: 1 },
  Plumbing:   { costSavingsBase100: 0,   rentIncreasePercent: 1.5, delayMonths: 2 },
  Interior:   { costSavingsBase100: 0,   rentIncreasePercent: 2, delayMonths: 1 },
};

/** Scale default impact to actual living area (reference: 100m²) */
function getSuggestedImpact(category: CapExCategory, livingAreaSqm: number): SuggestedImpact | undefined {
  const data = DEFAULT_IMPACT_DATA[category];
  if (!data) return undefined;
  if (data.costSavingsBase100 === 0 && data.rentIncreasePercent === 0) return undefined;

  const scaleFactor = livingAreaSqm / 100;
  return {
    costSavingsMonthly: Math.round(data.costSavingsBase100 * scaleFactor),
    rentIncreasePercent: data.rentIncreasePercent,
    delayMonths: data.delayMonths,
  };
}

function derivePriority(componentAge: number, cycleYears: number): MeasurePriority {
  const ratio = componentAge / cycleYears;
  if (ratio >= 1.0) return 'Critical';
  if (ratio >= 0.8) return 'High';
  if (ratio >= 0.6) return 'Medium';
  return 'Low';
}

/**
 * Generate renovation forecast measures based on building component data.
 *
 * For each component category, calculates when renewal is expected and
 * estimates costs based on building size, condition, and price level.
 */
export function generateForecast(
  property: Property,
  startYear: number,
  endYear: number,
  currency: string
): ForecastedMeasure[] {
  const measures: ForecastedMeasure[] = [];
  const currentYear = new Date().getFullYear();
  const annualInflation = 0.03; // 3% p.a. Baukostensteigerung

  // All standard categories to evaluate
  const categories: CapExCategory[] = [
    'Heating', 'Roof', 'Facade', 'Windows',
    'Electrical', 'Plumbing', 'Interior', 'Exterior'
  ];

  for (const category of categories) {
    const cycleData = DEFAULT_COMPONENT_CYCLES[category];

    // Find component data from property (if captured)
    const component = property.components.find(c => c.category === category);

    // Determine last renovation year: explicit > component fallback > construction year
    const lastRenovationYear = component?.lastRenovationYear || property.constructionYear;

    // Determine expected cycle: explicit > default midpoint
    const cycleYears = component?.expectedCycleYears || getDefaultCycleYears(category);

    // Determine component condition for cost interpolation
    const componentCondition = component?.condition || property.overallCondition;

    // Calculate when next renewal is due
    const nextRenewalYear = lastRenovationYear + cycleYears;

    // Skip if renewal is not within the analysis period
    if (nextRenewalYear > endYear) continue;

    // Clamp the planned year to at least the start of the analysis
    const plannedYear = Math.max(nextRenewalYear, startYear);

    // Current component age (not age at planned year)
    const componentAge = currentYear - lastRenovationYear;

    // Calculate cost
    const costFactor = conditionCostFactor(componentCondition);
    const baseCostPerUnit = cycleData.costPerSqmMin +
      (cycleData.costPerSqmMax - cycleData.costPerSqmMin) * costFactor;

    // Determine relevant area/count
    let unitCount: number;
    switch (cycleData.areaMode) {
      case 'total':
        unitCount = property.totalArea;
        break;
      case 'perUnit':
        unitCount = Math.ceil(property.livingArea * (cycleData.unitsPerSqm || (1 / 8)));
        break;
      case 'living':
      default:
        unitCount = property.livingArea;
        break;
    }

    // Base cost
    let estimatedCost = baseCostPerUnit * unitCount;

    // Inflation adjustment to planned year from current year
    const yearsUntil = Math.max(0, plannedYear - currentYear);
    estimatedCost *= Math.pow(1 + annualInflation, yearsUntil);

    // Round to nearest 100
    estimatedCost = Math.round(estimatedCost / 100) * 100;

    // Priority
    const priority = derivePriority(componentAge, cycleYears);

    // Reasoning text
    const ageText = componentAge === 1 ? '1 Jahr' : `${componentAge} Jahre`;
    const reasoning =
      `${CATEGORY_NAMES[category]}: ${ageText} alt, ` +
      `Zyklus ${cycleData.minYears}–${cycleData.maxYears} Jahre` +
      (priority === 'Critical' ? ' → überfällig' :
       priority === 'High' ? ' → bald fällig' : '');

    measures.push({
      category,
      name: CATEGORY_NAMES[category],
      estimatedCost,
      plannedYear,
      plannedMonth: 1, // default to January
      priority,
      componentAge,
      cycleYears,
      reasoning,
      suggestedImpact: getSuggestedImpact(category, property.livingArea),
    });
  }

  // Sort by priority (Critical first), then by planned year
  const priorityOrder: Record<MeasurePriority, number> = {
    Critical: 0, High: 1, Medium: 2, Low: 3
  };

  measures.sort((a, b) => {
    const pDiff = priorityOrder[a.priority] - priorityOrder[b.priority];
    if (pDiff !== 0) return pDiff;
    return a.plannedYear - b.plannedYear;
  });

  return measures;
}
