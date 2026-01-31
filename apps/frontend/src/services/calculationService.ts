/**
 * Frontend Calculation Service
 *
 * Pure-function calculation engine that computes investment metrics
 * from project data entirely client-side. Mirrors the backend
 * Kalkimo.Domain.Calculators logic in simplified form.
 *
 * === Steuerrechtliche Quellen ===
 *
 * AfA (Abschreibung):
 *   - §7 Abs. 4 EStG – lineare Gebäude-AfA (2%, 2,5%, 3%)
 *   - §7 Abs. 5a EStG – degressive AfA 5% (Wachstumschancengesetz 2024, hier nicht implementiert)
 *   - Haufe: https://www.haufe.de/steuern/finanzverwaltung/gebaeude-abgrenzung-ak-und-hk-erhaltungsaufwendungen_164_673640.html
 *
 * Anschaffungsnahe Herstellungskosten (15%-Regel):
 *   - §6 Abs. 1 Nr. 1a EStG – 15%-Grenze innerhalb 3 Jahren nach Anschaffung
 *   - BMF-Schreiben 26.01.2026 – Abgrenzung AK/HK/Erhaltungsaufwand (ersetzt BMF 18.07.2003 + 20.10.2017)
 *   - BFH-Urteilstrilogie 14.06.2016 – weite Auslegung „Instandsetzungs-/Modernisierungsmaßnahmen"
 *   - Haufe: https://www.haufe.de/steuern/steuerwissen-tipps/anschaffungsnahe-herstellungskosten-bei-gebaeuden_170_275760.html
 *   - Finanztip: https://www.finanztip.de/erhaltungsaufwand/
 *
 * Erhaltungsaufwand / Herstellungskosten Abgrenzung:
 *   - §255 Abs. 2 HGB – Herstellungskosten bei Erweiterung oder wesentlicher Verbesserung
 *   - Standardhebung: 3 von 4 Kernbereichen (Heizung, Sanitär, Elektrik, Fenster) in 5 Jahren
 *   - JUHN: https://www.juhn.com/fachwissen/besteuerung-immobilien/die-standardhebung/
 *
 * §82b EStDV – Verteilung größeren Erhaltungsaufwands:
 *   - Gleichmäßige Verteilung auf 2–5 Jahre (Wahlrecht pro Maßnahme)
 *   - Nur bei überwiegend Wohnzwecken dienenden Gebäuden im Privatvermögen
 *   - dejure.org: https://dejure.org/gesetze/EStDV/82b.html
 *
 * §23 EStG – Spekulationssteuer:
 *   - 10-Jahres-Frist für private Veräußerungsgeschäfte bei Immobilien
 *   - Freigrenze 1.000 EUR (ab 2024, Wachstumschancengesetz) – kein Freibetrag!
 *   - Finanztip: https://www.finanztip.de/spekulationssteuer/
 *   - Lohnsteuer-kompakt: https://www.lohnsteuer-kompakt.de/steuerwissen/private-veraeusserungsgeschaefte-erhoehung-der-freigrenze-auf-1-000-euro/
 */
import type {
  Project,
  CalculationResult,
  YearlyCashflowRow,
  TaxBridgeRow,
  CapExTimelineItem,
  InvestmentMetrics,
  TaxSummary,
  CalculationWarning,
  ExitAnalysis,
  ExitScenario,
  PropertyValueForecast,
  PropertyValueScenario,
  PropertyValueRow,
  MarketComparison,
  ForecastDriver,
  ComponentDeteriorationRow,
  ComponentDeteriorationSummary,
  RecurringMaintenanceInfo,
  RecurringMeasureConfig,
  Property,
  CapExCategory,
  TaxClassification,
  Money,
  Loan,
  YearMonth,
  AmountMode
} from '@/stores/types';
import { DEFAULT_COMPONENT_CYCLES } from '@/services/renovationForecastService';

// === Helper functions ===

function ymToMonths(ym: YearMonth): number {
  return ym.year * 12 + ym.month;
}

function monthsBetween(start: YearMonth, end: YearMonth): number {
  return ymToMonths(end) - ymToMonths(start) + 1;
}

function addMonths(ym: YearMonth, months: number): YearMonth {
  const total = ym.year * 12 + (ym.month - 1) + months;
  return { year: Math.floor(total / 12), month: (total % 12) + 1 };
}

function ymInRange(ym: YearMonth, start: YearMonth, end: YearMonth): boolean {
  const m = ymToMonths(ym);
  return m >= ymToMonths(start) && m <= ymToMonths(end);
}

function money(amount: number, currency: string): Money {
  return { amount, currency };
}

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

// === AfA (Depreciation) ===
// §7 Abs. 4 EStG – lineare Gebäude-AfA für Wohngebäude
// Nr. 2a: Fertigstellung ab 01.01.2023 → 3%
// Nr. 2b: Fertigstellung 01.01.1925–31.12.2022 → 2%
// Nr. 2c: Fertigstellung vor 01.01.1925 → 2,5%

function getAfaRate(constructionYear: number, override?: number): number {
  if (override && override > 0) return override;
  if (constructionYear >= 2023) return 3.0;   // §7 Abs. 4 Nr. 2a EStG
  if (constructionYear >= 1925) return 2.0;   // §7 Abs. 4 Nr. 2b EStG
  return 2.5;                                  // §7 Abs. 4 Nr. 2c EStG (vor 1925)
}

function getPurchaseCostAmount(purchasePrice: number, cost: { amount: { amount: number }; mode?: AmountMode }): number {
  const mode = (cost as any).mode as AmountMode | undefined;
  if (mode === 'percent') {
    return purchasePrice * cost.amount.amount / 100;
  }
  return cost.amount.amount;
}

function getDepreciationBasis(project: Project): number {
  const purchasePrice = project.purchase.purchasePrice.amount;

  const acquisitionCosts = project.purchase.costs
    .filter(c => c.taxClassification === 'AcquisitionCost')
    .reduce((sum, c) => sum + getPurchaseCostAmount(purchasePrice, c), 0);

  const totalBasis = purchasePrice + acquisitionCosts;
  const landPercent = project.purchase.landValuePercent || 0;
  return totalBasis * (1 - landPercent / 100);
}

function getTotalPurchaseCosts(project: Project): number {
  const purchasePrice = project.purchase.purchasePrice.amount;
  return project.purchase.costs.reduce(
    (sum, c) => sum + getPurchaseCostAmount(purchasePrice, c), 0
  );
}

// === Financing ===

interface MonthlyLoanPayment {
  interest: number;
  principal: number;
  totalPayment: number;
  remainingBalance: number;
}

function calculateLoanSchedule(
  loan: Loan,
  projectStart: YearMonth,
  projectEnd: YearMonth
): Map<string, MonthlyLoanPayment> {
  const schedule = new Map<string, MonthlyLoanPayment>();
  const monthlyRate = loan.interestRatePercent / 100 / 12;
  const monthlyAnnuity = (loan.interestRatePercent / 100 + loan.initialRepaymentPercent / 100)
    * loan.principal.amount / 12;

  let balance = loan.principal.amount;
  const totalMonths = monthsBetween(projectStart, projectEnd);

  for (let m = 0; m < totalMonths; m++) {
    const period = addMonths(projectStart, m);
    const periodKey = `${period.year}-${period.month}`;

    if (ymToMonths(period) < ymToMonths(loan.startDate)) {
      schedule.set(periodKey, {
        interest: 0, principal: 0, totalPayment: 0,
        remainingBalance: loan.principal.amount
      });
      continue;
    }

    if (balance <= 0) {
      schedule.set(periodKey, {
        interest: 0, principal: 0, totalPayment: 0, remainingBalance: 0
      });
      continue;
    }

    const interest = balance * monthlyRate;
    let principalPayment = monthlyAnnuity - interest;
    if (principalPayment > balance) principalPayment = balance;
    balance -= principalPayment;

    schedule.set(periodKey, {
      interest,
      principal: principalPayment,
      totalPayment: interest + principalPayment,
      remainingBalance: Math.max(0, balance)
    });
  }

  return schedule;
}

// === 15% Rule (Anschaffungsnahe Herstellungskosten) ===
// §6 Abs. 1 Nr. 1a EStG: Instandsetzungs-/Modernisierungsaufwendungen innerhalb von
// 3 Jahren nach Anschaffung, die ohne USt 15% der Gebäude-AK übersteigen, werden zu
// Herstellungskosten (Fallbeil-Wirkung: gesamter Betrag wird reklassifiziert).
// Ausgenommen: Erweiterungen (§255 Abs. 2 S. 1 HGB) und jährlich üblicher Erhaltungsaufwand.
// BMF-Schreiben 26.01.2026: Dreijahreszeitraum taggenau ab Übergang wirtschaftl. Eigentum.

function check15PercentRule(project: Project): { triggered: boolean; amount: number } {
  const purchaseDate = project.purchase.purchaseDate;
  const threeYearsLater = addMonths(purchaseDate, 36);
  const buildingValue = getDepreciationBasis(project);

  const maintenanceInPeriod = project.capex
    .filter(m =>
      m.taxClassification === 'MaintenanceExpense' &&
      ymInRange(m.scheduledDate, purchaseDate, threeYearsLater)
    )
    .reduce((sum, m) => sum + m.amount.amount, 0);

  const threshold = buildingValue * 0.15;
  return {
    triggered: maintenanceInPeriod > threshold,
    amount: maintenanceInPeriod
  };
}

// === Risk Scores ===

function calculateMaintenanceRiskScore(project: Project): number {
  const startYear = project.startPeriod.year;
  const endYear = project.endPeriod.year;

  if (project.property.components.length === 0) {
    // No component data: exponential risk from building age at end of holding period
    const ageAtEnd = endYear - project.property.constructionYear;
    return Math.round(ageFraction(ageAtEnd, REFERENCE_LIFECYCLE) * 100);
  }

  // Index CapEx renewals by category (earliest renewal per category)
  const capexByCategory = new Map<string, number>();
  const recurringByCategory = new Map<string, RecurringMeasureConfig>();
  for (const measure of project.capex) {
    if (measure.isRecurring && measure.recurringConfig) {
      recurringByCategory.set(measure.category, measure.recurringConfig);
    }
    // Track one-time capex (including recurring measures that also have a one-time amount)
    if (measure.amount.amount > 0) {
      const y = measure.scheduledDate.year;
      if (y >= startYear && y <= endYear) {
        const existing = capexByCategory.get(measure.category);
        if (existing === undefined || y < existing) {
          capexByCategory.set(measure.category, y);
        }
      }
    }
  }

  let totalRisk = 0;
  for (const comp of project.property.components) {
    const lastReno = comp.lastRenovationYear || project.property.constructionYear;
    const cycle = comp.expectedCycleYears;
    const capexYear = capexByCategory.get(comp.category);
    const recurring = recurringByCategory.get(comp.category);

    // Effective cycle with recurring maintenance
    const effectiveCycle = recurring
      ? cycle * (1 + recurring.cycleExtensionPercent / 100)
      : cycle;

    // Age at end of holding period, accounting for renewal
    const ageAtEnd = capexYear ? (endYear - capexYear) : (endYear - lastReno);

    // Exponential risk: (age/effectiveCycle)^4 × 100
    const componentRisk = ageFraction(ageAtEnd, effectiveCycle) * 100;
    totalRisk += componentRisk;
  }

  return Math.round(clamp(totalRisk / project.property.components.length, 0, 100));
}

function calculateLiquidityRiskScore(
  yearlyCashflows: YearlyCashflowRow[],
  dscrMin: number
): number {
  let score = 0;

  // Negative cashflow years increase risk
  const negativeYears = yearlyCashflows.filter(r => r.cashflowAfterTax < 0).length;
  score += (negativeYears / yearlyCashflows.length) * 40;

  // Low DSCR increases risk
  if (dscrMin < 1.0) score += 40;
  else if (dscrMin < 1.2) score += 25;
  else if (dscrMin < 1.5) score += 10;

  // High LTV increases risk
  const initialLtv = yearlyCashflows[0]?.ltvPercent || 0;
  if (initialLtv > 90) score += 20;
  else if (initialLtv > 80) score += 10;

  return Math.round(clamp(score, 0, 100));
}

// === Property Value Forecast ===

function conditionToFactor(condition: string): number {
  switch (condition) {
    case 'New': return 1.0;
    case 'Good': return 0.95;
    case 'Fair': return 0.85;
    case 'Poor': return 0.70;
    case 'NeedsRenovation': return 0.55;
    default: return 0.85;
  }
}

// Forecast constants
const IMPROVEMENT_VALUE_FACTOR = 0.70;
const BASE_AGING_RATE = 0.005;
const AGING_ACCELERATION = 0.005;
const REFERENCE_LIFECYCLE = 60;
const MEAN_REVERSION_HALF_LIFE = 7;

/** Three-tier degradation rate based on component lifecycle position */
function componentDegradationRate(age: number, cycle: number): number {
  const ratio = age / cycle;
  if (ratio <= 0.7) return 0.003;
  if (ratio <= 1.0) return 0.008;
  return 0.015;
}

/** Estimate full renewal cost for a component category using max cost (full renewal) */
function calculateComponentRenewalCost(
  category: CapExCategory,
  property: Property,
): number {
  const cycleData = DEFAULT_COMPONENT_CYCLES[category];
  if (!cycleData) return 0;

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

  return Math.round((cycleData.costPerSqmMax * unitCount) / 100) * 100;
}

/**
 * Exponential aging fraction: maps age/cycle ratio to a 0–1 fraction
 * representing the proportion of renewal cost that is "consumed".
 * Exponent 4 gives slow early aging, accelerating toward cycle end.
 *
 * Examples (exp=4): 30%→0.8%, 50%→6.3%, 70%→24%, 90%→65.6%, 100%→100%
 */
const DETERIORATION_EXPONENT = 4;

function ageFraction(age: number, cycle: number): number {
  if (cycle <= 0) return 1;
  return Math.min(1, Math.pow(Math.max(0, age) / cycle, DETERIORATION_EXPONENT));
}

/** Calculate per-component deterioration summary for the holding period */
function calculateComponentDeterioration(
  project: Project,
  startYear: number,
  endYear: number,
): ComponentDeteriorationSummary | undefined {
  if (project.property.components.length === 0) return undefined;

  const property = project.property;
  const holdingYears = endYear - startYear;

  // Index one-time CapEx by category for matching
  const capexByCategory = new Map<string, { year: number; amount: number }[]>();
  // Index recurring measures by category
  const recurringByCategory = new Map<string, { name: string; config: RecurringMeasureConfig }>();
  for (const measure of project.capex) {
    if (measure.isRecurring && measure.recurringConfig) {
      recurringByCategory.set(measure.category, { name: measure.name, config: measure.recurringConfig });
    }
    // Track one-time capex (including recurring measures that also have a one-time amount)
    if (measure.amount.amount > 0) {
      const y = measure.scheduledDate.year;
      if (y >= startYear && y <= endYear) {
        const list = capexByCategory.get(measure.category) || [];
        list.push({ year: y, amount: measure.amount.amount });
        capexByCategory.set(measure.category, list);
      }
    }
  }

  const rows: ComponentDeteriorationRow[] = [];
  let totalValueImpact = 0;
  let totalRenewalCostIfAllDone = 0;
  let coveredByCapex = 0;
  let uncoveredDeterioration = 0;

  for (const comp of property.components) {
    const lastReno = comp.lastRenovationYear || property.constructionYear;
    const ageAtStart = startYear - lastReno;
    const cycle = comp.expectedCycleYears;
    let dueYear = lastReno + cycle;

    // Renewal cost estimate (max cost = full renewal)
    const renewalCost = calculateComponentRenewalCost(comp.category, property);
    totalRenewalCostIfAllDone += renewalCost;

    // Check if CapEx addresses this component
    const capexMeasures = capexByCategory.get(comp.category) || [];
    let matchingCapex: { year: number; amount: number } | null = null;
    if (capexMeasures.length > 0) {
      matchingCapex = capexMeasures.find(c => c.year >= dueYear - 1) || capexMeasures[0];
    }

    // Check for recurring maintenance
    const recurring = recurringByCategory.get(comp.category);
    const effectiveCycle = recurring
      ? cycle * (1 + recurring.config.cycleExtensionPercent / 100)
      : cycle;

    // Exponential aging model: value impact = renewalCost × (endFraction - startFraction)
    const startFraction = ageFraction(ageAtStart, effectiveCycle);
    let ageAtEnd: number;
    let valueImpact: number;
    let statusAtEnd: ComponentDeteriorationRow['statusAtEnd'];

    if (matchingCapex) {
      // Component is renewed: age resets at renewal year, new due = renewal + effectiveCycle
      ageAtEnd = endYear - matchingCapex.year;
      dueYear = matchingCapex.year + Math.round(effectiveCycle);
      // Post-renewal aging: component ages from 0 to ageAtEnd after renewal
      const postRenewalFraction = ageFraction(ageAtEnd, effectiveCycle);
      valueImpact = postRenewalFraction > 0.001 ? -renewalCost * postRenewalFraction : 0;
      coveredByCapex += matchingCapex.amount;
      statusAtEnd = 'Renewed';
    } else {
      ageAtEnd = ageAtStart + holdingYears;
      const endFraction = ageFraction(ageAtEnd, effectiveCycle);
      const delta = endFraction - startFraction;
      valueImpact = delta > 0.001 ? -renewalCost * delta : 0;
      uncoveredDeterioration += Math.abs(valueImpact);

      if (startFraction >= 0.999) {
        statusAtEnd = 'OverdueAtPurchase';
      } else if (endFraction >= 0.999) {
        statusAtEnd = 'Overdue';
      } else {
        statusAtEnd = 'OK';
      }
    }

    // Calculate recurring maintenance info
    let recurringMaintenance: RecurringMaintenanceInfo | undefined;
    if (recurring) {
      const intervalYears = Math.round(cycle * recurring.config.intervalPercent / 100);
      const costPerOccurrence = Math.round(renewalCost * recurring.config.costPercent / 100 / 100) * 100;
      // Calculate occurrences: from reference date (capex renewal or lastReno) at interval steps
      const refYear = matchingCapex?.year ?? lastReno;
      let occurrences = 0;
      if (intervalYears > 0) {
        for (let age = intervalYears; refYear + age <= endYear; age += intervalYears) {
          if (refYear + age >= startYear) {
            occurrences++;
          }
        }
      }
      // Value improvement: difference between original and extended cycle deterioration
      const fractionOriginal = matchingCapex
        ? ageFraction(ageAtEnd, cycle)
        : ageFraction(ageAtEnd, cycle) - ageFraction(ageAtStart, cycle);
      const fractionExtended = matchingCapex
        ? ageFraction(ageAtEnd, effectiveCycle)
        : ageFraction(ageAtEnd, effectiveCycle) - ageFraction(ageAtStart, effectiveCycle);
      const valueImprovement = renewalCost * Math.max(0, fractionOriginal - fractionExtended);

      recurringMaintenance = {
        name: recurring.name,
        intervalYears,
        costPerOccurrence,
        occurrencesInPeriod: occurrences,
        totalCostInPeriod: occurrences * costPerOccurrence,
        effectiveCycleYears: Math.round(effectiveCycle),
        valueImprovement: Math.round(valueImprovement),
      };
    }

    totalValueImpact += valueImpact;

    rows.push({
      category: comp.category,
      ageAtStart,
      ageAtEnd,
      cycleYears: recurring ? Math.round(effectiveCycle) : cycle,
      dueYear,
      renewalCostEstimate: renewalCost,
      capexAddressedYear: matchingCapex?.year ?? null,
      valueImpact,
      statusAtEnd,
      recurringMaintenance,
    });
  }

  return {
    components: rows,
    totalValueImpact,
    totalRenewalCostIfAllDone,
    coveredByCapex,
    uncoveredDeterioration,
  };
}

/** Calculate per-year component deterioration using exponential aging model */
function yearlyComponentDeterioration(
  componentDet: ComponentDeteriorationSummary,
  year: number,
  startYear: number,
): number {
  let cumulative = 0;
  for (const row of componentDet.components) {
    // Use effective cycle (already stored in row.cycleYears when recurring maintenance exists)
    const cycle = row.cycleYears;

    if (row.capexAddressedYear !== null && year >= row.capexAddressedYear) {
      // After renewal: component ages from 0 starting at capex year
      const postRenewalAge = year - row.capexAddressedYear;
      const postRenewalFraction = ageFraction(postRenewalAge, cycle);
      if (postRenewalFraction > 0.001) {
        cumulative += -row.renewalCostEstimate * postRenewalFraction;
      }
      continue;
    }

    // Pre-renewal or no-renewal: aging from original start
    const currentAge = row.ageAtStart + (year - startYear);
    const currentFraction = ageFraction(currentAge, cycle);
    const startFraction = ageFraction(row.ageAtStart, cycle);
    const delta = currentFraction - startFraction;

    if (delta > 0.001) {
      cumulative += -row.renewalCostEstimate * delta;
    }
  }
  return cumulative;
}

function calculatePropertyValueForecast(
  project: Project,
  startYear: number,
  endYear: number,
): PropertyValueForecast {
  const purchasePrice = project.purchase.purchasePrice.amount;
  const livingArea = project.property.livingArea;
  const totalYears = endYear - startYear;

  // === Market comparison ===
  let marketComparison: MarketComparison | undefined;
  if (project.property.regionalPricePerSqm && project.property.regionalPricePerSqm > 0) {
    const fmv = project.property.regionalPricePerSqm * livingArea;
    const ratio = purchasePrice / fmv;
    let assessment: 'below' | 'at' | 'above';
    if (ratio < 0.95) assessment = 'below';
    else if (ratio > 1.05) assessment = 'above';
    else assessment = 'at';
    marketComparison = {
      regionalPricePerSqm: project.property.regionalPricePerSqm,
      livingArea,
      fairMarketValue: fmv,
      purchasePriceToMarketRatio: ratio,
      assessment,
    };
  }
  const fairMarketValue = marketComparison?.fairMarketValue;
  const useMeanReversion = fairMarketValue !== undefined && fairMarketValue > 0;

  // === Initial condition factor ===
  let initialConditionFactor: number;
  if (project.property.components.length > 0) {
    const componentFactors = project.property.components.map(c => {
      const baseFactor = conditionToFactor(c.condition);
      const lastReno = c.lastRenovationYear || project.property.constructionYear;
      const age = startYear - lastReno;
      const overdue = Math.max(0, age - c.expectedCycleYears);
      const agingPenalty = overdue * 0.015; // post-overdue rate applied retroactively
      return Math.max(0.5, baseFactor - agingPenalty);
    });
    initialConditionFactor = componentFactors.reduce((a, b) => a + b, 0) / componentFactors.length;
  } else {
    initialConditionFactor = conditionToFactor(project.property.overallCondition);
  }

  // === Track overdue components for drivers ===
  const overdueComponents: { name: string; overdueYears: number }[] = [];
  for (const c of project.property.components) {
    const lastReno = c.lastRenovationYear || project.property.constructionYear;
    const age = startYear - lastReno;
    if (age > c.expectedCycleYears) {
      overdueComponents.push({
        name: c.category,
        overdueYears: age - c.expectedCycleYears,
      });
    }
  }

  // === Index CapEx by year ===
  const capexByYear = new Map<number, {
    improvement: number;
    maintenance: number;
    categories: Map<string, 'improvement' | 'maintenance'>;
  }>();
  let totalCapexImprovement = 0;
  let totalCapexMaintenance = 0;
  let capexMeasureCount = 0;
  for (const measure of project.capex) {
    const y = measure.scheduledDate.year;
    if (y < startYear || y > endYear) continue;
    if (measure.amount.amount === 0) continue;
    capexMeasureCount++;
    const entry = capexByYear.get(y) || {
      improvement: 0,
      maintenance: 0,
      categories: new Map<string, 'improvement' | 'maintenance'>(),
    };
    const isImprovement =
      measure.taxClassification === 'ImprovementCost' ||
      measure.taxClassification === 'AcquisitionCost';
    if (isImprovement) {
      entry.improvement += measure.amount.amount;
      totalCapexImprovement += measure.amount.amount;
      entry.categories.set(measure.category, 'improvement');
    } else if (measure.taxClassification === 'MaintenanceExpense') {
      entry.maintenance += measure.amount.amount;
      totalCapexMaintenance += measure.amount.amount;
      if (!entry.categories.has(measure.category)) {
        entry.categories.set(measure.category, 'maintenance');
      }
    }
    capexByYear.set(y, entry);
  }

  // === Component deterioration (cost-based model) ===
  const componentDet = calculateComponentDeterioration(project, startYear, endYear);
  const useComponentDeterioration = componentDet !== undefined && componentDet.components.length > 0;

  // === Scenario definitions ===
  const scenarioDefs = [
    { label: 'conservative', rate: 0.0 },
    { label: 'base', rate: 1.5 },
    { label: 'optimistic', rate: 3.0 },
  ];

  // Track condition changes for drivers (use base scenario)
  let baseEndConditionFactor = initialConditionFactor;
  let baseTotalConditionBoost = 0;

  const scenarios: PropertyValueScenario[] = scenarioDefs.map(def => {
    const yearlyValues: PropertyValueRow[] = [];
    let cumulativeImprovement = 0;

    // Per-component tracking
    const componentStates = project.property.components.map(c => {
      const lastReno = c.lastRenovationYear || project.property.constructionYear;
      const age = startYear - lastReno;
      const overdue = Math.max(0, age - c.expectedCycleYears);
      return {
        category: c.category,
        age,
        cycle: c.expectedCycleYears,
        factor: Math.max(0.5, conditionToFactor(c.condition) - overdue * 0.015),
      };
    });

    // Simple-model tracking (no components)
    let simpleConditionFactor = initialConditionFactor;
    let simpleBuildingAge = startYear - project.property.constructionYear;

    let conditionFactor = initialConditionFactor;
    let conditionBoostFromCapex = 0;

    for (let year = startYear; year <= endYear; year++) {
      const yearsFromStart = year - startYear;
      const capex = capexByYear.get(year);
      const previousConditionFactor = conditionFactor;

      // === Improvement uplift ===
      if (capex?.improvement) {
        cumulativeImprovement += capex.improvement * IMPROVEMENT_VALUE_FACTOR;
      }

      // === Condition factor update ===
      if (componentStates.length > 0) {
        for (const comp of componentStates) {
          comp.age++;
          const capexType = capex?.categories.get(comp.category);
          if (capexType === 'improvement') {
            const boost = Math.min(1.0 - comp.factor, 0.30);
            comp.factor = Math.min(1.0, comp.factor + 0.30);
            comp.age = 0;
            conditionBoostFromCapex += boost;
          } else if (capexType === 'maintenance') {
            const boost = Math.min(1.0 - comp.factor, 0.15);
            comp.factor = Math.min(1.0, comp.factor + 0.15);
            comp.age = Math.floor(comp.age / 2);
            conditionBoostFromCapex += boost;
          } else {
            // Natural degradation: three-tier rate
            const degradation = componentDegradationRate(comp.age, comp.cycle);
            comp.factor = Math.max(0.50, comp.factor - degradation);
          }
        }
        conditionFactor = componentStates.reduce((sum, c) => sum + c.factor, 0) / componentStates.length;
      } else {
        // Simple model: accelerating degradation based on building age
        simpleBuildingAge++;
        const degradation = BASE_AGING_RATE + AGING_ACCELERATION * (simpleBuildingAge / REFERENCE_LIFECYCLE);
        simpleConditionFactor = Math.max(0.50, simpleConditionFactor - degradation);

        if (capex?.maintenance) {
          const boost = Math.min(1.0 - simpleConditionFactor, 0.05);
          simpleConditionFactor = Math.min(1.0, simpleConditionFactor + 0.05);
          conditionBoostFromCapex += boost;
        }
        if (capex?.improvement) {
          const boost = Math.min(1.0 - simpleConditionFactor, 0.08);
          simpleConditionFactor = Math.min(1.0, simpleConditionFactor + 0.08);
          conditionBoostFromCapex += boost;
        }
        conditionFactor = simpleConditionFactor;
      }

      const conditionDelta = conditionFactor - previousConditionFactor;

      // === Market value (pure appreciation) — OFF-BY-ONE FIX ===
      const marketValue = purchasePrice * Math.pow(1 + def.rate / 100, yearsFromStart);

      // === Component deterioration (cost-based, EUR) ===
      const componentDeteriorationCumulative = useComponentDeterioration
        ? yearlyComponentDeterioration(componentDet!, year, startYear)
        : 0;

      // === Estimated value before mean reversion ===
      let estimatedValue: number;
      if (useComponentDeterioration) {
        // Cost-based model: market value + cost-based deterioration + improvements
        estimatedValue = marketValue + componentDeteriorationCumulative + cumulativeImprovement;
      } else {
        // Abstract percentage model (fallback when no component data)
        const conditionRatio = initialConditionFactor > 0 ? conditionFactor / initialConditionFactor : 1;
        estimatedValue = marketValue * conditionRatio + cumulativeImprovement;
      }

      // === Mean reversion ===
      let meanReversionAdjustment = 0;
      if (useMeanReversion && fairMarketValue) {
        const gap = fairMarketValue - purchasePrice;
        const gapAtYear = gap * Math.pow(1 + def.rate / 100, yearsFromStart);
        const closedPortion = 1 - Math.pow(0.5, yearsFromStart / MEAN_REVERSION_HALF_LIFE);
        meanReversionAdjustment = gapAtYear * closedPortion;
        estimatedValue += meanReversionAdjustment;
      }

      yearlyValues.push({
        year,
        marketValue,
        conditionFactor,
        conditionDelta,
        improvementUplift: cumulativeImprovement,
        meanReversionAdjustment,
        componentDeteriorationCumulative,
        estimatedValue,
      });
    }

    // Track base scenario values for drivers
    if (def.label === 'base') {
      baseEndConditionFactor = conditionFactor;
      baseTotalConditionBoost = conditionBoostFromCapex;
    }

    return {
      label: def.label,
      annualAppreciationPercent: def.rate,
      yearlyValues,
      finalValue: yearlyValues[yearlyValues.length - 1]?.estimatedValue || purchasePrice,
    };
  });

  // === Generate forecast drivers ===
  const drivers: ForecastDriver[] = [];
  const baseScenario = scenarios.find(s => s.label === 'base');
  const baseFinalValue = baseScenario?.finalValue || purchasePrice;

  // 1. Initial condition
  drivers.push({
    type: 'initialCondition',
    params: {
      constructionYear: project.property.constructionYear,
      condition: project.property.overallCondition,
      factor: Math.round(initialConditionFactor * 100),
      componentCount: project.property.components.length,
    },
  });

  // 2. Overdue components
  if (overdueComponents.length > 0) {
    const avgOverdue = Math.round(
      overdueComponents.reduce((s, c) => s + c.overdueYears, 0) / overdueComponents.length
    );
    drivers.push({
      type: 'overdueComponents',
      params: {
        count: overdueComponents.length,
        names: overdueComponents.map(c => c.name).join(', '),
        avgOverdueYears: avgOverdue,
      },
    });
  }

  // 3. Degradation
  const totalDecline = Math.round((initialConditionFactor - baseEndConditionFactor) * 100);
  if (totalDecline > 0) {
    drivers.push({
      type: 'degradation',
      params: {
        startFactor: Math.round(initialConditionFactor * 100),
        endFactor: Math.round(baseEndConditionFactor * 100),
        totalDecline,
        years: totalYears,
      },
    });
  }

  // 3b. Component deterioration (cost-based)
  if (componentDet && componentDet.uncoveredDeterioration > 0) {
    const uncoveredCount = componentDet.components.filter(
      c => c.statusAtEnd === 'Overdue'
    ).length;
    drivers.push({
      type: 'componentDeterioration',
      params: {
        uncoveredCount,
        uncoveredAmount: Math.round(componentDet.uncoveredDeterioration),
        totalComponents: componentDet.components.length,
        coveredByCapex: Math.round(componentDet.coveredByCapex),
      },
    });
  }

  // 4. Investments
  const totalCapexAmount = totalCapexImprovement + totalCapexMaintenance;
  if (capexMeasureCount > 0) {
    drivers.push({
      type: 'investments',
      params: {
        measureCount: capexMeasureCount,
        totalAmount: Math.round(totalCapexAmount),
        valueUplift: Math.round(totalCapexImprovement * IMPROVEMENT_VALUE_FACTOR),
        conditionBoost: Math.round(baseTotalConditionBoost * 100 / Math.max(project.property.components.length, 1)),
      },
    });
  }

  // 5. Market appreciation (base scenario = 1.5%)
  const baseRate = 1.5;
  const appreciationMultiplier = Math.pow(1 + baseRate / 100, totalYears);
  const appreciationPercent = Math.round((appreciationMultiplier - 1) * 1000) / 10;
  drivers.push({
    type: 'marketAppreciation',
    params: {
      rate: baseRate,
      years: totalYears,
      appreciationPercent,
      appreciationAmount: Math.round(purchasePrice * (appreciationMultiplier - 1)),
    },
  });

  // 6. Mean reversion
  if (useMeanReversion && marketComparison) {
    const gapPercent = Math.round(Math.abs(1 - marketComparison.purchasePriceToMarketRatio) * 100);
    const lastBaseRow = baseScenario?.yearlyValues[baseScenario.yearlyValues.length - 1];
    drivers.push({
      type: 'meanReversion',
      params: {
        assessment: marketComparison.assessment,
        gapPercent,
        adjustmentAmount: Math.round(Math.abs(lastBaseRow?.meanReversionAdjustment || 0)),
        direction: marketComparison.assessment === 'below' ? 'Aufholpotenzial' : 'Dämpfung',
      },
    });
  }

  // 7. Summary
  const changeAbsolute = baseFinalValue - purchasePrice;
  const changePercent = purchasePrice > 0 ? Math.round((changeAbsolute / purchasePrice) * 1000) / 10 : 0;
  drivers.push({
    type: 'summary',
    params: {
      years: totalYears,
      purchasePrice: Math.round(purchasePrice),
      finalValue: Math.round(baseFinalValue),
      changePercent: Math.abs(changePercent),
      changeAbsolute: Math.round(Math.abs(changeAbsolute)),
      changeDirection: changeAbsolute >= 0 ? '+' : '-',
    },
  });

  return {
    purchasePrice,
    improvementValueFactor: IMPROVEMENT_VALUE_FACTOR,
    initialConditionFactor,
    marketComparison,
    componentDeterioration: componentDet,
    drivers,
    scenarios,
  };
}

// === Recurring Occurrence Expansion ===

interface RecurringOccurrence {
  year: number;
  month: number;
  amount: number;
  category: CapExCategory;
  taxClassification: TaxClassification;
  name: string;
  sourceMeasureId: string;
}

function getRecurringOccurrences(project: Project): RecurringOccurrence[] {
  const occurrences: RecurringOccurrence[] = [];
  const startYear = project.startPeriod.year;
  const endYear = project.endPeriod.year;

  for (const measure of project.capex) {
    if (!measure.isRecurring || !measure.recurringConfig) continue;

    const comp = project.property.components.find(c => c.category === measure.category);
    const cycleData = DEFAULT_COMPONENT_CYCLES[measure.category];
    const cycle = comp?.expectedCycleYears
      ?? (cycleData ? Math.round((cycleData.minYears + cycleData.maxYears) / 2) : 20);
    const intervalYears = Math.round(cycle * measure.recurringConfig.intervalPercent / 100);
    if (intervalYears <= 0) continue;

    const renewalCost = calculateComponentRenewalCost(measure.category, project.property);
    const costPerEvent = Math.round(renewalCost * measure.recurringConfig.costPercent / 100 / 100) * 100;
    if (costPerEvent <= 0) continue;

    const lastReno = comp?.lastRenovationYear ?? project.property.constructionYear;

    let idx = 0;
    for (let age = intervalYears; lastReno + age <= endYear; age += intervalYears) {
      const year = lastReno + age;
      if (year >= startYear) {
        idx++;
        occurrences.push({
          year,
          month: measure.scheduledDate.month || 1,
          amount: costPerEvent,
          category: measure.category,
          taxClassification: measure.taxClassification,
          name: `${measure.name} (#${idx})`,
          sourceMeasureId: measure.id,
        });
      }
    }
  }

  return occurrences;
}

// === Main Calculation ===

export function calculateProject(project: Project): CalculationResult {
  const currency = project.currency;
  const start = project.startPeriod;
  const end = project.endPeriod;

  // Pre-compute loan schedules
  const loanSchedules = project.financing.loans.map(loan =>
    calculateLoanSchedule(loan, start, end)
  );

  // Pre-compute AfA
  const afaRate = getAfaRate(
    project.property.constructionYear,
    project.taxProfile.depreciationRatePercent
  );
  const depreciationBasis = getDepreciationBasis(project);
  const annualDepreciation = depreciationBasis * afaRate / 100;
  const monthlyDepreciation = annualDepreciation / 12;

  // 15% rule check
  const rule15 = check15PercentRule(project);

  // Determine year range
  const startYear = start.year;
  const endYear = end.year;

  // Rent base values
  const totalMonthlyRent = project.rent.units.reduce(
    (sum, u) => sum + u.monthlyRent.amount, 0
  );
  const totalMonthlyServiceCharge = project.rent.units.reduce(
    (sum, u) => sum + u.monthlyServiceCharge.amount, 0
  );
  const avgRentIncrease = project.rent.units.length > 0
    ? project.rent.units.reduce((sum, u) => sum + u.annualRentIncreasePercent, 0) / project.rent.units.length
    : 0;
  const vacancyRate = project.rent.vacancyRatePercent / 100;

  // Cost base values
  const totalMonthlyCosts = project.costs.items.reduce(
    (sum, c) => sum + c.monthlyAmount.amount, 0
  );
  const avgCostIncrease = project.costs.items.length > 0
    ? project.costs.items.reduce((sum, c) => sum + c.annualIncreasePercent, 0) / project.costs.items.length
    : 0;

  // Maintenance reserve (Instandhaltungsrücklage) – treated as separate account
  const monthlyReserve = project.costs.maintenanceReserveMonthly?.amount || 0;
  const reserveAnnualIncrease = project.costs.maintenanceReserveAnnualIncreasePercent || 0;
  // Initial reserve balance from WEG configuration (if applicable)
  let reserveBalance = project.property.wegConfiguration?.currentReserveBalance?.amount || 0;

  // CapEx by year-month (one-time amounts)
  const capexByMonth = new Map<string, number>();
  for (const measure of project.capex) {
    const key = `${measure.scheduledDate.year}-${measure.scheduledDate.month}`;
    capexByMonth.set(key, (capexByMonth.get(key) || 0) + measure.amount.amount);
  }

  // Add recurring measure occurrences to capex tracking
  const recurringOccurrences = getRecurringOccurrences(project);
  for (const occ of recurringOccurrences) {
    const key = `${occ.year}-${occ.month}`;
    capexByMonth.set(key, (capexByMonth.get(key) || 0) + occ.amount);
  }

  // Pre-compute measure impacts: for each measure with impact, determine the
  // effective start month (scheduledDate + delayMonths) and monthly savings/increases
  interface ActiveImpact {
    effectiveStart: YearMonth;
    costSavingsMonthly: number;
    rentIncreaseMonthly: number;
    rentIncreasePercent: number;
  }
  const activeImpacts: ActiveImpact[] = [];
  for (const measure of project.capex) {
    if (!measure.impact) continue;
    const imp = measure.impact;
    const hasSavings = (imp.costSavingsMonthly?.amount ?? 0) > 0;
    const hasRentAbs = (imp.rentIncreaseMonthly?.amount ?? 0) > 0;
    const hasRentPct = (imp.rentIncreasePercent ?? 0) > 0;
    if (!hasSavings && !hasRentAbs && !hasRentPct) continue;

    const delay = imp.delayMonths ?? 0;
    const effectiveStart = addMonths(measure.scheduledDate, delay);
    activeImpacts.push({
      effectiveStart,
      costSavingsMonthly: imp.costSavingsMonthly?.amount ?? 0,
      rentIncreaseMonthly: imp.rentIncreaseMonthly?.amount ?? 0,
      rentIncreasePercent: imp.rentIncreasePercent ?? 0,
    });
  }

  // §82b EStDV distributions by year
  // Wahlrecht: Größere Erhaltungsaufwendungen auf 2–5 Jahre gleichmäßig verteilen.
  // Gilt nur für Wohngebäude im Privatvermögen (nicht Betriebsvermögen).
  // Ref: https://dejure.org/gesetze/EStDV/82b.html
  const maintenanceDistributions = new Map<number, number>();
  if (project.taxProfile.use82bDistribution) {
    for (const measure of project.capex) {
      if (measure.taxClassification === 'MaintenanceExpense' && measure.distributionYears) {
        const annualDeduction = measure.amount.amount / measure.distributionYears;
        for (let y = 0; y < measure.distributionYears; y++) {
          const year = measure.scheduledDate.year + y;
          maintenanceDistributions.set(
            year,
            (maintenanceDistributions.get(year) || 0) + annualDeduction
          );
        }
      }
    }
  }

  // === Additional AfA from CapEx Herstellungskosten ===
  // Nachträgliche Herstellungskosten (ImprovementCost) und anschaffungsnahe HK
  // (AcquisitionCost) werden über die Restnutzungsdauer abgeschrieben (§7 Abs. 4 EStG).
  // Bei Auslösung der 15%-Regel (§6 Abs. 1 Nr. 1a EStG) werden die reklassifizierten
  // Erhaltungsaufwendungen ebenfalls zur AfA-Basis addiert.
  // Renovierungskosten sind reine Gebäudekosten – kein Grundstücksanteil abzuziehen.
  const capexAfaByYear = new Map<number, number>();
  for (const measure of project.capex) {
    if (measure.taxClassification !== 'ImprovementCost' &&
        measure.taxClassification !== 'AcquisitionCost') continue;
    if (measure.amount.amount <= 0) continue;
    const annualAfa = measure.amount.amount * afaRate / 100;
    const measureYear = measure.scheduledDate.year;
    for (let y = measureYear; y <= endYear; y++) {
      capexAfaByYear.set(y, (capexAfaByYear.get(y) || 0) + annualAfa);
    }
  }

  // 15%-Regel: reklassifizierte Erhaltungsaufwendungen → zusätzliche AfA
  if (rule15.triggered) {
    const rule15AnnualAfa = rule15.amount * afaRate / 100;
    for (let y = startYear; y <= endYear; y++) {
      capexAfaByYear.set(y, (capexAfaByYear.get(y) || 0) + rule15AnnualAfa);
    }
  }

  // === Build yearly rows ===
  const yearlyCashflows: YearlyCashflowRow[] = [];
  const taxBridge: TaxBridgeRow[] = [];
  let cumulativeCF = 0;
  let totalDebtServiceAll = 0;
  let totalInterestAll = 0;
  let totalDepreciationAll = 0;
  let totalMaintenanceDeductionAll = 0;
  let totalOperatingDeductionAll = 0;
  let totalMaintenanceReserveAll = 0;
  let totalTaxAll = 0;
  let totalCFBeforeTax = 0;
  let totalCFAfterTax = 0;
  let totalEffectiveRent = 0;
  const dscrValues: number[] = [];
  const icrValues: number[] = [];

  // Capital requirement
  const equity = project.financing.equity.amount;
  const totalLoans = project.financing.loans.reduce((s, l) => s + l.principal.amount, 0);
  const purchaseCosts = getTotalPurchaseCosts(project);
  const purchaseTotal = project.purchase.purchasePrice.amount + purchaseCosts;
  const oneTimeCapexTotal = project.capex.reduce((s, m) => s + m.amount.amount, 0);
  const recurringCapexTotal = recurringOccurrences.reduce((s, o) => s + o.amount, 0);
  const totalCapexAmount = oneTimeCapexTotal + recurringCapexTotal;
  // Capital requirement uses only one-time capex (recurring costs are future periodic expenses)
  const totalCapitalRequirement = purchaseTotal + oneTimeCapexTotal;

  for (let year = startYear; year <= endYear; year++) {
    const yearsFromStart = year - startYear;
    const yearStart = year === startYear ? start.month : 1;
    const yearEnd = year === endYear ? end.month : 12;
    const monthsInYear = yearEnd - yearStart + 1;

    // Rent with annual increase
    const rentMultiplier = Math.pow(1 + avgRentIncrease / 100, yearsFromStart);
    const monthlyRent = totalMonthlyRent * rentMultiplier;
    const grossRent = monthlyRent * monthsInYear;
    const vacancyLoss = grossRent * vacancyRate;
    let effectiveRent = grossRent - vacancyLoss;
    const serviceChargeIncome = totalMonthlyServiceCharge * rentMultiplier * monthsInYear * (1 - vacancyRate);

    // Operating costs with increase
    const costMultiplier = Math.pow(1 + avgCostIncrease / 100, yearsFromStart);
    let operatingCosts = totalMonthlyCosts * costMultiplier * monthsInYear;

    // Apply measure impacts: cost savings reduce operating costs,
    // rent increases boost effective rent (per active month in this year)
    for (const impact of activeImpacts) {
      // Count how many months this impact is active within this year
      let activeMonths = 0;
      for (let month = yearStart; month <= yearEnd; month++) {
        const periodM = ymToMonths({ year, month });
        const effectiveM = ymToMonths(impact.effectiveStart);
        if (periodM >= effectiveM) activeMonths++;
      }
      if (activeMonths === 0) continue;

      // Cost savings reduce operating costs
      if (impact.costSavingsMonthly > 0) {
        operatingCosts -= impact.costSavingsMonthly * activeMonths;
      }

      // Rent increase (absolute) boosts effective rent
      if (impact.rentIncreaseMonthly > 0) {
        effectiveRent += impact.rentIncreaseMonthly * activeMonths * (1 - vacancyRate);
      }

      // Rent increase (relative) boosts effective rent based on base monthly rent
      if (impact.rentIncreasePercent > 0) {
        effectiveRent += monthlyRent * (impact.rentIncreasePercent / 100) * activeMonths * (1 - vacancyRate);
      }
    }

    // Ensure operating costs don't go negative
    operatingCosts = Math.max(0, operatingCosts);

    // Maintenance reserve with annual increase
    const reserveMultiplier = Math.pow(1 + reserveAnnualIncrease / 100, yearsFromStart);
    const yearMaintenanceReserve = monthlyReserve * reserveMultiplier * monthsInYear;

    const noi = effectiveRent - operatingCosts - yearMaintenanceReserve;

    // Debt service for this year
    let yearDebtService = 0;
    let yearInterest = 0;
    let yearPrincipal = 0;
    for (let month = yearStart; month <= yearEnd; month++) {
      const periodKey = `${year}-${month}`;
      for (const schedule of loanSchedules) {
        const payment = schedule.get(periodKey);
        if (payment) {
          yearDebtService += payment.totalPayment;
          yearInterest += payment.interest;
          yearPrincipal += payment.principal;
        }
      }
    }

    // Outstanding debt at year end
    const lastMonthKey = `${year}-${yearEnd}`;
    let outstandingDebt = 0;
    for (const schedule of loanSchedules) {
      const payment = schedule.get(lastMonthKey);
      if (payment) outstandingDebt += payment.remainingBalance;
    }

    // LTV for this year
    const ltvPercent = totalCapitalRequirement > 0
      ? (outstandingDebt / totalCapitalRequirement) * 100
      : 0;

    // DSCR and ICR for this year
    const dscrYear = yearDebtService > 0 ? noi / yearDebtService : 0;
    const icrYear = yearInterest > 0 ? noi / yearInterest : 0;

    // CapEx payments in this year
    let capexPayments = 0;
    for (let month = yearStart; month <= yearEnd; month++) {
      const key = `${year}-${month}`;
      capexPayments += capexByMonth.get(key) || 0;
    }

    // Reserve account: contributions accumulate, CapEx draws from reserve first
    const reserveBalanceStart = reserveBalance;
    reserveBalance += yearMaintenanceReserve;
    const capexFromReserve = Math.min(capexPayments, reserveBalance);
    const capexFromCashflow = capexPayments - capexFromReserve;
    reserveBalance -= capexFromReserve;

    const cashflowBeforeTax = noi - yearDebtService - capexFromCashflow;

    // Tax calculation
    // Basis-AfA (Kaufpreis-Gebäudeanteil)
    const baseDepreciation = monthlyDepreciation * monthsInYear;
    // Zusätzliche AfA aus nachträglichen HK und anschaffungsnahen HK (§7 Abs. 4 EStG)
    const capexDepreciation = capexAfaByYear.get(year) || 0;
    const depreciation = baseDepreciation + capexDepreciation;

    // Maintenance deduction (Erhaltungsaufwand)
    let maintenanceDeduction = 0;
    if (project.taxProfile.use82bDistribution) {
      // §82b EStDV: Verteilte Maßnahmen (mit distributionYears)
      maintenanceDeduction = maintenanceDistributions.get(year) || 0;
      // Maßnahmen OHNE distributionYears werden weiterhin sofort abgezogen
      for (const measure of project.capex) {
        if (
          measure.taxClassification === 'MaintenanceExpense' &&
          !measure.distributionYears &&
          measure.scheduledDate.year === year
        ) {
          maintenanceDeduction += measure.amount.amount;
        }
      }
    } else {
      // Sofortabzug: alle Erhaltungsaufwendungen im Jahr der Durchführung
      for (const measure of project.capex) {
        if (
          measure.taxClassification === 'MaintenanceExpense' &&
          measure.scheduledDate.year === year
        ) {
          maintenanceDeduction += measure.amount.amount;
        }
      }
    }

    // Add recurring maintenance occurrences (always Sofortabzug, no §82b distribution)
    for (const occ of recurringOccurrences) {
      if (occ.taxClassification === 'MaintenanceExpense' && occ.year === year) {
        maintenanceDeduction += occ.amount;
      }
    }

    // §6 Abs. 1 Nr. 1a EStG: 15%-Regel ausgelöst → kein Sofortabzug für Erhaltungsaufwand.
    // Die reklassifizierten Beträge fließen stattdessen über capexAfaByYear in die AfA ein.
    if (rule15.triggered) {
      maintenanceDeduction = 0;
    }

    const taxableIncome = effectiveRent - operatingCosts - depreciation - yearInterest - maintenanceDeduction;
    const taxPayment = taxableIncome > 0
      ? taxableIncome * project.taxProfile.marginalTaxRatePercent / 100
      : 0;

    const cashflowAfterTax = cashflowBeforeTax - taxPayment;
    cumulativeCF += cashflowAfterTax;

    // Track totals
    totalDebtServiceAll += yearDebtService;
    totalInterestAll += yearInterest;
    totalDepreciationAll += depreciation;
    totalMaintenanceDeductionAll += maintenanceDeduction;
    totalOperatingDeductionAll += operatingCosts;
    totalMaintenanceReserveAll += yearMaintenanceReserve;
    totalTaxAll += taxPayment;
    totalCFBeforeTax += cashflowBeforeTax;
    totalCFAfterTax += cashflowAfterTax;
    totalEffectiveRent += effectiveRent;

    if (yearDebtService > 0) dscrValues.push(dscrYear);
    if (yearInterest > 0) icrValues.push(icrYear);

    yearlyCashflows.push({
      year,
      grossRent,
      vacancyLoss,
      effectiveRent,
      serviceChargeIncome,
      operatingCosts,
      maintenanceReserve: yearMaintenanceReserve,
      netOperatingIncome: noi,
      debtService: yearDebtService,
      interestPortion: yearInterest,
      principalPortion: yearPrincipal,
      capexPayments,
      reserveBalanceStart,
      capexFromReserve,
      capexFromCashflow,
      reserveBalanceEnd: reserveBalance,
      cashflowBeforeTax,
      depreciation,
      interestDeduction: yearInterest,
      maintenanceDeduction,
      taxableIncome,
      taxPayment,
      cashflowAfterTax,
      cumulativeCashflow: cumulativeCF,
      outstandingDebt,
      ltvPercent,
      dscrYear,
      icrYear
    });

    // Tax bridge row
    taxBridge.push({
      year,
      grossIncome: effectiveRent,
      depreciation,
      interestExpense: yearInterest,
      maintenanceExpense: maintenanceDeduction,
      operatingExpenses: operatingCosts,
      maintenanceReserve: yearMaintenanceReserve,
      taxableIncome,
      taxPayment
    });
  }

  // === CapEx Timeline ===
  const capexTimeline: CapExTimelineItem[] = project.capex
    .filter(m => m.amount.amount > 0)
    .map(m => ({
      id: m.id,
      name: m.name,
      category: m.category,
      year: m.scheduledDate.year,
      month: m.scheduledDate.month,
      amount: m.amount.amount,
      taxClassification: m.taxClassification,
      distributionYears: m.distributionYears
    }));

  // Add recurring occurrences to timeline
  for (const occ of recurringOccurrences) {
    capexTimeline.push({
      id: `${occ.sourceMeasureId}-r-${occ.year}`,
      name: occ.name,
      category: occ.category,
      year: occ.year,
      month: occ.month,
      amount: occ.amount,
      taxClassification: occ.taxClassification,
    });
  }

  // === Compute Metrics ===
  const dscrMin = dscrValues.length > 0 ? Math.min(...dscrValues) : 0;
  const dscrAvg = dscrValues.length > 0 ? dscrValues.reduce((a, b) => a + b, 0) / dscrValues.length : 0;
  const icrMin = icrValues.length > 0 ? Math.min(...icrValues) : 0;

  const firstFullYear = yearlyCashflows.find(r => r.year === startYear + 1) || yearlyCashflows[0];
  const cashOnCash = equity > 0 ? (firstFullYear?.cashflowAfterTax || 0) / equity * 100 : 0;
  const equityMultiple = equity > 0 ? (equity + totalCFAfterTax) / equity : 0;

  const ltvInitial = totalCapitalRequirement > 0 ? totalLoans / totalCapitalRequirement * 100 : 0;
  const lastRow = yearlyCashflows[yearlyCashflows.length - 1];
  const ltvFinal = totalCapitalRequirement > 0 && lastRow
    ? lastRow.outstandingDebt / totalCapitalRequirement * 100
    : 0;

  const monthsFirstYear = startYear === endYear
    ? end.month - start.month + 1
    : 12 - start.month + 1;
  const firstYearRow = yearlyCashflows[0];
  const breakEvenMonthly = firstYearRow
    ? (firstYearRow.operatingCosts + firstYearRow.debtService) / monthsFirstYear
    : 0;

  const totalYears = endYear - startYear + 1;
  const roi = equity > 0 ? totalCFAfterTax / equity / totalYears * 100 : 0;

  const annualCFBeforeTax = yearlyCashflows.map(r => r.cashflowBeforeTax);
  const annualCFAfterTax = yearlyCashflows.map(r => r.cashflowAfterTax);
  const irrBeforeTax = calculateIRR([-totalCapitalRequirement, ...annualCFBeforeTax]);
  const irrAfterTax = calculateIRR([-totalCapitalRequirement, ...annualCFAfterTax]);

  const discountRate = 0.05;
  const npvBeforeTax = calculateNPV(discountRate, [-totalCapitalRequirement, ...annualCFBeforeTax]);
  const npvAfterTax = calculateNPV(discountRate, [-totalCapitalRequirement, ...annualCFAfterTax]);

  // Risk scores
  const maintenanceRiskScore = calculateMaintenanceRiskScore(project);

  const metrics: InvestmentMetrics = {
    irrBeforeTaxPercent: irrBeforeTax * 100,
    irrAfterTaxPercent: irrAfterTax * 100,
    npvBeforeTax: money(npvBeforeTax, currency),
    npvAfterTax: money(npvAfterTax, currency),
    cashOnCashPercent: cashOnCash,
    equityMultiple,
    dscrMin,
    dscrAvg,
    icrMin,
    ltvInitialPercent: ltvInitial,
    ltvFinalPercent: ltvFinal,
    breakEvenRent: money(breakEvenMonthly, currency),
    roiPercent: roi,
    maintenanceRiskScore,
    liquidityRiskScore: 0 // calculated after cashflows
  };

  // Liquidity risk needs cashflow data
  metrics.liquidityRiskScore = calculateLiquidityRiskScore(yearlyCashflows, dscrMin);

  // Tax summary
  const totalTaxSavings = totalDepreciationAll + totalInterestAll + totalMaintenanceDeductionAll + totalOperatingDeductionAll;
  const effectiveTaxRate = totalEffectiveRent > 0
    ? (totalTaxAll / totalEffectiveRent) * 100
    : 0;

  // Effektive AfA-Basis: Kaufpreis-Basis + ggf. anschaffungsnahe HK (15%-Regel)
  const effectiveDepreciationBasis = rule15.triggered
    ? depreciationBasis + rule15.amount
    : depreciationBasis;
  const effectiveAnnualDepreciation = effectiveDepreciationBasis * afaRate / 100;

  const taxSummary: TaxSummary = {
    totalDepreciation: money(totalDepreciationAll, currency),
    totalInterestDeduction: money(totalInterestAll, currency),
    totalMaintenanceDeduction: money(totalMaintenanceDeductionAll, currency),
    totalOperatingDeduction: money(totalOperatingDeductionAll, currency),
    acquisitionRelatedCostsTriggered: rule15.triggered,
    acquisitionRelatedCostsAmount: money(rule15.amount, currency),
    totalTaxPayment: money(totalTaxAll, currency),
    totalTaxSavings: money(totalTaxSavings * project.taxProfile.marginalTaxRatePercent / 100, currency),
    annualDepreciation: money(effectiveAnnualDepreciation, currency),
    depreciationRatePercent: afaRate,
    depreciationBasis: money(effectiveDepreciationBasis, currency),
    effectiveTaxRatePercent: effectiveTaxRate,
    totalMaintenanceReserve: money(totalMaintenanceReserveAll, currency)
  };

  // === Warnings ===
  const warnings: CalculationWarning[] = [];

  if (dscrMin > 0 && dscrMin < 1.0) {
    warnings.push({
      type: 'DscrBelowOne',
      message: 'DSCR unter 1,0 – Mieteinnahmen decken den Schuldendienst nicht vollständig',
      severity: 'critical'
    });
  }

  if (ltvInitial > 80) {
    warnings.push({
      type: 'HighLtv',
      message: `Hoher Beleihungsauslauf (${ltvInitial.toFixed(0)}%) – erhöhtes Risiko`,
      severity: 'warning'
    });
  }

  if (rule15.triggered) {
    warnings.push({
      type: 'AcquisitionRelatedCostsTriggered',
      message: '15%-Regel ausgelöst – Erhaltungsaufwand wird als Anschaffungskosten behandelt',
      severity: 'warning'
    });
  }

  const negativeYears = yearlyCashflows.filter(r => r.cashflowAfterTax < 0);
  if (negativeYears.length > 0) {
    warnings.push({
      type: 'NegativeCashflow',
      message: `Negativer Cashflow in ${negativeYears.length} Jahr(en)`,
      severity: negativeYears.length > 3 ? 'critical' : 'warning'
    });
  }

  if (equity < 0) {
    warnings.push({
      type: 'LiquidityShortfall',
      message: 'Eigenkapital ist negativ – Finanzierung reicht nicht aus',
      severity: 'critical'
    });
  }

  if (icrMin > 0 && icrMin < 1.5) {
    warnings.push({
      type: 'LowIcr',
      message: `Niedrige Zinsdeckung (ICR min. ${icrMin.toFixed(2)})`,
      severity: icrMin < 1.0 ? 'critical' : 'warning'
    });
  }

  if (maintenanceRiskScore > 60) {
    warnings.push({
      type: 'DeferredMaintenance',
      message: `Erhöhtes Sanierungsrisiko (Score: ${maintenanceRiskScore}/100)`,
      severity: maintenanceRiskScore > 80 ? 'critical' : 'warning'
    });
  }

  // Reserve insufficient warning
  const reserveShortfallYears = yearlyCashflows.filter(r => r.capexFromCashflow > 0);
  if (reserveShortfallYears.length > 0) {
    const totalExcessCapex = reserveShortfallYears.reduce((s, r) => s + r.capexFromCashflow, 0);
    warnings.push({
      type: 'ReserveInsufficient',
      message: `Rücklage reicht in ${reserveShortfallYears.length} Jahr(en) nicht aus — ${Math.round(totalExcessCapex).toLocaleString('de-DE')} € zusätzliche Liquidität benötigt`,
      severity: totalExcessCapex > 50000 ? 'critical' : 'warning'
    });
  }

  // === Property Value Forecast ===
  const propertyValueForecast = calculatePropertyValueForecast(project, startYear, endYear);

  // === Exit Analysis ===
  // §23 EStG – Private Veräußerungsgeschäfte (Spekulationssteuer)
  // Haltefrist: 10 Jahre ab Anschaffung (120 Monate)
  // Freigrenze: 1.000 EUR ab 2024 (Wachstumschancengesetz, §23 Abs. 3 S. 5 EStG)
  // ACHTUNG: Freigrenze, kein Freibetrag! Bei Überschreitung wird der GESAMTE Gewinn besteuert.
  // Ref: https://www.finanztip.de/spekulationssteuer/
  const holdingPeriodYears = totalYears;
  const purchaseDate = project.purchase.purchaseDate;
  const holdingMonths = monthsBetween(purchaseDate, end);
  const isWithinSpeculationPeriod = holdingMonths < 120;

  // Herstellungskosten und anschaffungsnahe HK erhöhen die steuerliche Bemessungsgrundlage
  // beim Verkauf (mindern den Veräußerungsgewinn). Auch bei 15%-Regel reklassifizierte
  // Erhaltungsaufwendungen fließen hier ein.
  const improvementCosts = project.capex
    .filter(m => m.taxClassification === 'ImprovementCost' || m.taxClassification === 'AcquisitionCost')
    .reduce((s, m) => s + m.amount.amount, 0);
  const rule15SaleBasisAdjustment = rule15.triggered ? rule15.amount : 0;

  const outstandingDebtAtExit = lastRow?.outstandingDebt || 0;
  const saleCostsPercent = 5.0; // ~3.57% broker + ~1.5% notary/registration
  const totalGrossIncome = yearlyCashflows.reduce((s, r) => s + r.grossRent, 0);
  const totalOperatingCostsAll = yearlyCashflows.reduce((s, r) => s + r.operatingCosts, 0);

  const purchasePrice = project.purchase.purchasePrice.amount;
  const taxBasisAtSale = purchasePrice + purchaseCosts + improvementCosts + rule15SaleBasisAdjustment;

  // Use property value forecast for exit scenarios (condition-aware values)
  const exitScenarios: ExitScenario[] = propertyValueForecast.scenarios.map(scenario => {
    const propertyValue = scenario.finalValue;
    const saleCosts = propertyValue * saleCostsPercent / 100;
    const capitalGain = propertyValue - taxBasisAtSale;

    // §23 Abs. 3 S. 5 EStG: Freigrenze 1.000 EUR (ab 2024)
    // Bei Gewinn > 1.000 EUR → gesamter Gewinn steuerpflichtig (kein Freibetrag!)
    let capitalGainsTax = 0;
    if (isWithinSpeculationPeriod && capitalGain > 1000) {
      capitalGainsTax = capitalGain * project.taxProfile.marginalTaxRatePercent / 100;
    }

    const netSaleProceeds = propertyValue - saleCosts - capitalGainsTax - outstandingDebtAtExit;
    const totalReturn = netSaleProceeds + totalCFAfterTax - equity;
    const totalReturnPercent = equity > 0 ? (totalReturn / equity) * 100 : 0;
    const annualizedReturn = equity > 0 && holdingPeriodYears > 0
      ? (Math.pow((equity + totalReturn) / equity, 1 / holdingPeriodYears) - 1) * 100
      : 0;

    return {
      label: scenario.label,
      annualAppreciationPercent: scenario.annualAppreciationPercent,
      propertyValueAtExit: propertyValue,
      saleCosts,
      capitalGain,
      capitalGainsTax,
      netSaleProceeds,
      totalReturn,
      totalReturnPercent: totalReturnPercent,
      annualizedReturnPercent: annualizedReturn,
    };
  });

  const exitAnalysis: ExitAnalysis = {
    holdingPeriodYears,
    isWithinSpeculationPeriod,
    purchasePrice,
    totalPurchaseCosts: purchaseCosts,
    equityInvested: equity,
    totalCashflowAfterTax: totalCFAfterTax,
    outstandingDebtAtExit,
    totalGrossIncome,
    totalOperatingCosts: totalOperatingCostsAll,
    totalDebtService: totalDebtServiceAll,
    totalCapex: totalCapexAmount,
    totalTaxPaid: totalTaxAll,
    totalMaintenanceReserve: totalMaintenanceReserveAll,
    finalReserveBalance: reserveBalance,
    saleCostsPercent,
    scenarios: exitScenarios,
  };

  return {
    projectId: project.id,
    calculatedAt: new Date().toISOString(),
    metrics,
    yearlyCashflows,
    taxBridge,
    capexTimeline,
    taxSummary,
    warnings,
    totalEquityInvested: equity,
    totalCashflowBeforeTax: totalCFBeforeTax,
    totalCashflowAfterTax: totalCFAfterTax,
    exitAnalysis,
    propertyValueForecast,
  };
}

// === IRR (Newton-Raphson) ===

function calculateIRR(cashflows: number[], maxIterations = 100, tolerance = 1e-7): number {
  let rate = 0.1;

  for (let i = 0; i < maxIterations; i++) {
    let npv = 0;
    let dnpv = 0;

    for (let t = 0; t < cashflows.length; t++) {
      const discountFactor = Math.pow(1 + rate, t);
      npv += cashflows[t] / discountFactor;
      if (t > 0) {
        dnpv -= t * cashflows[t] / Math.pow(1 + rate, t + 1);
      }
    }

    if (Math.abs(npv) < tolerance) return rate;
    if (Math.abs(dnpv) < 1e-10) break;

    const newRate = rate - npv / dnpv;

    if (newRate < -0.99) rate = -0.5;
    else if (newRate > 10) rate = 5;
    else rate = newRate;
  }

  return rate;
}

// === NPV ===

function calculateNPV(discountRate: number, cashflows: number[]): number {
  return cashflows.reduce((npv, cf, t) => {
    return npv + cf / Math.pow(1 + discountRate, t);
  }, 0);
}
