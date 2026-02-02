/**
 * Renovation Forecast Service
 *
 * Generates renovation/renewal measure suggestions based on building component
 * ages, condition, and standard lifecycle data. Mirrors the backend
 * RenovationForecastGenerator logic for offline/local use.
 */
import type { Property, Unit, CapExCategory, Condition, ComponentCondition } from '@/stores/types';
import { BUILDING_LEVEL_CATEGORIES, UNIT_LEVEL_CATEGORIES } from '@/stores/types';

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
  unitId?: string;
  unitName?: string;
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
 * === Gebäudeebene ===
 * Heating:    Gas-Brennwert 10–18k€, Wärmepumpe 18–40k€ → ~80–250 €/m² Wohnfläche
 * Roof:       Neueindeckung inkl. Dämmung/Lattung → 150–350 €/m² Dachfläche
 * Facade:     WDVS + Putz/Anstrich → 120–300 €/m² Fassadenfläche
 * Windows:    3-fach Verglasung inkl. Einbau → 600–1.400 € pro Fenstereinheit
 * Electrical: Komplett-Erneuerung UV + Leitungen → 80–160 €/m² Wohnfläche
 * Plumbing:   Steigleitungen + Bäder → 100–220 €/m² Wohnfläche
 * Interior:   Böden, Wände, Türen, Decken → 60–150 €/m² Wohnfläche
 * Exterior:   Wege, Entwässerung, Begrünung → 60–180 €/m² Grundstücksfläche
 *
 * === Mieteinheit-Ebene (pro m² Einheitsfläche) ===
 * Kitchen:        Einbauküche komplett → 80–250 €/m²
 * Bathroom:       Sanitärausstattung (Keramik, Armaturen, Fliesen) → 120–350 €/m²
 * UnitRenovation: Grundlegende Renovierung (Wände, Böden, Malerarbeiten) → 40–150 €/m²
 * UnitOther:      Sonstige wohnungsbezogene Ausstattung → 20–80 €/m²
 */
export const DEFAULT_COMPONENT_CYCLES: Record<CapExCategory, ComponentCycleData> = {
  // Gebäudeebene
  Heating:    { minYears: 15, maxYears: 25, costPerSqmMin: 80,  costPerSqmMax: 250, areaMode: 'living' },
  Roof:       { minYears: 30, maxYears: 60, costPerSqmMin: 150, costPerSqmMax: 350, areaMode: 'total' },
  Facade:     { minYears: 25, maxYears: 50, costPerSqmMin: 120, costPerSqmMax: 300, areaMode: 'total' },
  Windows:    { minYears: 20, maxYears: 40, costPerSqmMin: 600, costPerSqmMax: 1400, areaMode: 'perUnit', unitsPerSqm: 1 / 8 },
  Electrical: { minYears: 30, maxYears: 50, costPerSqmMin: 80,  costPerSqmMax: 160, areaMode: 'living' },
  Plumbing:   { minYears: 30, maxYears: 50, costPerSqmMin: 100, costPerSqmMax: 220, areaMode: 'living' },
  Interior:   { minYears: 15, maxYears: 30, costPerSqmMin: 60,  costPerSqmMax: 150, areaMode: 'living' },
  Exterior:   { minYears: 20, maxYears: 40, costPerSqmMin: 60,  costPerSqmMax: 180, areaMode: 'total' },
  Other:      { minYears: 20, maxYears: 40, costPerSqmMin: 80,  costPerSqmMax: 200, areaMode: 'living' },

  // Mieteinheit-Ebene (Kosten pro m² Einheitsfläche)
  Kitchen:        { minYears: 15, maxYears: 25, costPerSqmMin: 80,  costPerSqmMax: 250, areaMode: 'living' },
  Bathroom:       { minYears: 20, maxYears: 30, costPerSqmMin: 120, costPerSqmMax: 350, areaMode: 'living' },
  UnitRenovation: { minYears: 10, maxYears: 20, costPerSqmMin: 40,  costPerSqmMax: 150, areaMode: 'living' },
  UnitOther:      { minYears: 15, maxYears: 30, costPerSqmMin: 20,  costPerSqmMax: 80,  areaMode: 'living' },
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
  // Gebäudeebene
  Heating:    'Heizungserneuerung',
  Roof:       'Dachsanierung',
  Facade:     'Fassadensanierung',
  Windows:    'Fenstererneuerung',
  Electrical: 'Elektrikerneuerung',
  Plumbing:   'Sanitärerneuerung',
  Interior:   'Innenausbau-Erneuerung',
  Exterior:   'Außenanlagen-Erneuerung',
  Other:      'Sonstige Sanierung',
  // Mieteinheit-Ebene
  Kitchen:        'Küchenerneuerung',
  Bathroom:       'Sanitärausstattung-Erneuerung',
  UnitRenovation: 'Grundrenovierung',
  UnitOther:      'Sonstige Wohnungssanierung',
};

/**
 * Default economic impact data per category.
 * costSavingsBase100: monthly operating cost savings for 100m² living area (EUR)
 * rentIncreasePercent: potential rent increase at re-letting (%)
 * delayMonths: typical construction time (months)
 */
interface DefaultImpactData {
  costSavingsBase100: number;
  rentIncreasePercent: number;
  delayMonths: number;
}

const DEFAULT_IMPACT_DATA: Partial<Record<CapExCategory, DefaultImpactData>> = {
  // Gebäudeebene
  Heating:    { costSavingsBase100: 110, rentIncreasePercent: 4, delayMonths: 2 },
  Facade:     { costSavingsBase100: 90,  rentIncreasePercent: 6, delayMonths: 3 },
  Windows:    { costSavingsBase100: 45,  rentIncreasePercent: 3, delayMonths: 1 },
  Roof:       { costSavingsBase100: 60,  rentIncreasePercent: 2.5, delayMonths: 2 },
  Exterior:   { costSavingsBase100: 0,   rentIncreasePercent: 1, delayMonths: 1 },
  Electrical: { costSavingsBase100: 0,   rentIncreasePercent: 1, delayMonths: 1 },
  Plumbing:   { costSavingsBase100: 0,   rentIncreasePercent: 1.5, delayMonths: 2 },
  Interior:   { costSavingsBase100: 0,   rentIncreasePercent: 2, delayMonths: 1 },
  // Mieteinheit-Ebene
  Kitchen:        { costSavingsBase100: 0, rentIncreasePercent: 3, delayMonths: 1 },
  Bathroom:       { costSavingsBase100: 0, rentIncreasePercent: 2.5, delayMonths: 1 },
  UnitRenovation: { costSavingsBase100: 0, rentIncreasePercent: 2, delayMonths: 1 },
};

/** Scale default impact to actual area (reference: 100m²) */
function getSuggestedImpact(category: CapExCategory, areaSqm: number): SuggestedImpact | undefined {
  const data = DEFAULT_IMPACT_DATA[category];
  if (!data) return undefined;
  if (data.costSavingsBase100 === 0 && data.rentIncreasePercent === 0) return undefined;

  const scaleFactor = areaSqm / 100;
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
 * Generate a single measure for a component (building or unit level).
 */
function generateMeasureForComponent(
  category: CapExCategory,
  component: ComponentCondition | undefined,
  constructionYear: number,
  overallCondition: Condition,
  areaReference: number,
  startYear: number,
  endYear: number,
  currentYear: number,
  unitId?: string,
  unitName?: string,
): ForecastedMeasure | null {
  const cycleData = DEFAULT_COMPONENT_CYCLES[category];

  const lastRenovationYear = component?.lastRenovationYear || constructionYear;
  const cycleYears = component?.expectedCycleYears || getDefaultCycleYears(category);
  const componentCondition = component?.condition || overallCondition;
  const nextRenewalYear = lastRenovationYear + cycleYears;

  if (nextRenewalYear > endYear) return null;

  const plannedYear = Math.max(nextRenewalYear, startYear);
  const componentAge = currentYear - lastRenovationYear;

  const costFactor = conditionCostFactor(componentCondition);
  const baseCostPerUnit = cycleData.costPerSqmMin +
    (cycleData.costPerSqmMax - cycleData.costPerSqmMin) * costFactor;

  let estimatedCost = baseCostPerUnit * areaReference;
  const yearsUntil = Math.max(0, plannedYear - currentYear);
  estimatedCost *= Math.pow(1 + 0.03, yearsUntil);
  estimatedCost = Math.round(estimatedCost / 100) * 100;

  const priority = derivePriority(componentAge, cycleYears);

  const ageText = componentAge === 1 ? '1 Jahr' : `${componentAge} Jahre`;
  const baseName = CATEGORY_NAMES[category];
  const displayName = unitName ? `${unitName}: ${baseName}` : baseName;
  const reasoning =
    `${displayName}: ${ageText} alt, ` +
    `Zyklus ${cycleData.minYears}–${cycleData.maxYears} Jahre` +
    (priority === 'Critical' ? ' → überfällig' :
     priority === 'High' ? ' → bald fällig' : '');

  return {
    category,
    name: displayName,
    estimatedCost,
    plannedYear,
    plannedMonth: 1,
    priority,
    componentAge,
    cycleYears,
    reasoning,
    suggestedImpact: getSuggestedImpact(category, areaReference),
    unitId,
    unitName,
  };
}

/**
 * Get the area reference for a building-level category.
 */
function getBuildingAreaReference(category: CapExCategory, property: Property): number {
  const cycleData = DEFAULT_COMPONENT_CYCLES[category];
  switch (cycleData.areaMode) {
    case 'total':
      return property.totalArea;
    case 'perUnit':
      return Math.ceil(property.livingArea * (cycleData.unitsPerSqm || (1 / 8)));
    case 'living':
    default:
      return property.livingArea;
  }
}

/**
 * Generate renovation forecast measures based on building and unit component data.
 *
 * For each component category, calculates when renewal is expected and
 * estimates costs based on building/unit size, condition, and price level.
 */
export function generateForecast(
  property: Property,
  startYear: number,
  endYear: number,
  currency: string
): ForecastedMeasure[] {
  const measures: ForecastedMeasure[] = [];
  const currentYear = new Date().getFullYear();

  // === Gebäude-Bauteile ===
  const buildingCategories: CapExCategory[] = [
    'Heating', 'Roof', 'Facade', 'Windows',
    'Electrical', 'Plumbing', 'Interior', 'Exterior'
  ];

  for (const category of buildingCategories) {
    const component = property.components.find(c => c.category === category);
    const areaRef = getBuildingAreaReference(category, property);

    const measure = generateMeasureForComponent(
      category, component, property.constructionYear, property.overallCondition,
      areaRef, startYear, endYear, currentYear);

    if (measure) measures.push(measure);
  }

  // === Einheit-Bauteile ===
  for (const unit of property.units) {
    if (!unit.components || unit.components.length === 0) continue;

    for (const category of UNIT_LEVEL_CATEGORIES) {
      const component = unit.components.find(c => c.category === category);
      if (!component) continue;

      const measure = generateMeasureForComponent(
        category, component, property.constructionYear, property.overallCondition,
        unit.area, startYear, endYear, currentYear,
        unit.id, unit.name);

      if (measure) measures.push(measure);
    }
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
