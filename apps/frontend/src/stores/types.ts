// Domain types matching backend Kalkimo.Domain.Models

export type PropertyType =
  | 'SingleFamily'      // EFH
  | 'MultiFamily'       // MFH
  | 'Condominium'       // ETW
  | 'Commercial'        // Gewerbe
  | 'Mixed';            // Gemischt

export type Condition = 'New' | 'Good' | 'Fair' | 'Poor' | 'NeedsRenovation';

export type UnitType = 'Residential' | 'Commercial' | 'Parking' | 'Storage';
export type UnitStatus = 'Rented' | 'Vacant' | 'OwnerOccupied' | 'Renovation';

export type TaxClassification =
  | 'MaintenanceExpense'    // Erhaltungsaufwand (sofort absetzbar)
  | 'AcquisitionCost'       // Anschaffungskosten (AfA)
  | 'ImprovementCost'       // Herstellungskosten (AfA)
  | 'NotDeductible';        // Nicht absetzbar

export type CapExCategory =
  | 'Roof'                  // Dach
  | 'Facade'                // Fassade
  | 'Windows'               // Fenster
  | 'Heating'               // Heizung
  | 'Electrical'            // Elektrik
  | 'Plumbing'              // Sanitär
  | 'Interior'              // Innenausbau
  | 'Exterior'              // Außenanlagen
  | 'Other';                // Sonstiges

export type DistributionKeyType =
  | 'Mea'           // Nach MEA
  | 'LivingArea'    // Nach Wohnfläche
  | 'PersonCount'   // Nach Personenzahl
  | 'Consumption'   // Nach Verbrauch
  | 'PerUnit'       // Nach Einheitenzahl
  | 'Custom';       // Individuell

// Money value object
export interface Money {
  amount: number;
  currency: string;
}

// Year-Month period
export interface YearMonth {
  year: number;
  month: number;
}

// Property models
export interface Unit {
  id: string;
  name: string;
  type: UnitType;
  area: number;
  rooms?: number;
  floor?: string;
  status: UnitStatus;
}

export interface ComponentCondition {
  category: CapExCategory;
  condition: Condition;
  lastRenovationYear?: number;
  expectedCycleYears: number;
}

export interface Property {
  id: string;
  type: PropertyType;
  constructionYear: number;
  overallCondition: Condition;
  totalArea: number;
  livingArea: number;
  landArea?: number;
  unitCount: number;
  units: Unit[];
  components: ComponentCondition[];
  wegConfiguration?: WegConfiguration;
  regionalPricePerSqm?: number;
}

// WEG models
export interface HausgeldConfiguration {
  monthlyTotal: Money;
  monthlyReserveContribution: Money;
  monthlyAdministration?: Money;
  monthlyMaintenance?: Money;
  monthlyHeating?: Money;
  monthlyOperatingCosts?: Money;
  annualIncreasePercent: number;
}

export interface Sonderumlage {
  id: string;
  description: string;
  amount: Money;
  dueDate: YearMonth;
  relatedMeasure?: string;
  isTaxDeductible: boolean;
  taxClassification: TaxClassification;
}

export interface CostDistributionKey {
  name: string;
  type: DistributionKeyType;
  customShare?: number;
  description?: string;
  applicableCostTypes: string[];
}

export interface WegConfiguration {
  wegName?: string;
  meaPerMille: number;
  totalMeaPerMille: number;
  hausgeld: HausgeldConfiguration;
  currentReserveBalance?: Money;
  sonderumlagen: Sonderumlage[];
  distributionKeys: CostDistributionKey[];
}

// Purchase models
export interface PurchaseCostItem {
  id: string;
  name: string;
  amount: Money;
  isDeductible: boolean;
  taxClassification: TaxClassification;
}

export type AmountMode = 'percent' | 'fixed';

export interface Purchase {
  purchasePrice: Money;
  purchaseDate: YearMonth;
  landValuePercent: number;
  landValueMode?: AmountMode;
  costs: PurchaseCostItem[];
}

// Financing models
export interface Loan {
  id: string;
  name: string;
  principal: Money;
  interestRatePercent: number;
  initialRepaymentPercent: number;
  fixedInterestYears: number;
  startDate: YearMonth;
  specialRepaymentPercentPerYear?: number;
}

export interface Financing {
  equity: Money;
  loans: Loan[];
}

// Rent models
export interface RentUnit {
  unitId: string;
  monthlyRent: Money;
  monthlyServiceCharge: Money;
  annualRentIncreasePercent: number;
}

export interface RentConfiguration {
  units: RentUnit[];
  vacancyRatePercent: number;
  rentLossReserveMonths: number;
}

// Cost models
export interface CostItem {
  id: string;
  name: string;
  monthlyAmount: Money;
  isTransferable: boolean;
  annualIncreasePercent: number;
}

export interface CostConfiguration {
  items: CostItem[];
  maintenanceReserveMonthly: Money;
  maintenanceReserveAnnualIncreasePercent: number;
}

// CapEx models

/** Wirtschaftliche Auswirkung einer Maßnahme (Betriebskostenersparnis, Mietpotenzial) */
export interface MeasureImpact {
  /** Monatliche Betriebskostenersparnis (z.B. geringere Heizkosten) */
  costSavingsMonthly?: Money;
  /** Mieterhöhung absolut pro Monat (z.B. Modernisierungsumlage) */
  rentIncreaseMonthly?: Money;
  /** Mieterhöhung relativ (%) */
  rentIncreasePercent?: number;
  /** Verzögerung in Monaten (Bauzeit, erst danach wirken Einsparungen) */
  delayMonths?: number;
}

export interface RecurringMeasureConfig {
  intervalPercent: number;          // Interval as % of component cycle (e.g., 40)
  costPercent: number;              // Cost per occurrence as % of full renewal (e.g., 25)
  cycleExtensionPercent: number;    // Effective cycle extension (default = intervalPercent)
}

export interface CapExMeasure {
  id: string;
  name: string;
  category: CapExCategory;
  amount: Money;
  scheduledDate: YearMonth;
  taxClassification: TaxClassification;
  distributionYears?: number; // for §82b
  impact?: MeasureImpact;
  isRecurring?: boolean;
  recurringConfig?: RecurringMeasureConfig;
}

// Tax profile
export interface TaxProfile {
  marginalTaxRatePercent: number;
  churchTaxPercent?: number;
  isCorporate: boolean;
  depreciationRatePercent?: number; // Override for AfA
  use82bDistribution: boolean;
  distributionYears82b: number;
}

// Main Project model
export interface Project {
  id: string;
  name: string;
  description?: string;
  currency: string;
  startPeriod: YearMonth;
  endPeriod: YearMonth;
  property: Property;
  purchase: Purchase;
  financing: Financing;
  rent: RentConfiguration;
  costs: CostConfiguration;
  capex: CapExMeasure[];
  taxProfile: TaxProfile;
  createdAt: string;
  updatedAt: string;
}

// === Calculation Results ===

// Yearly cashflow row for table display
export interface YearlyCashflowRow {
  year: number;
  grossRent: number;
  vacancyLoss: number;
  effectiveRent: number;
  serviceChargeIncome: number;
  operatingCosts: number;
  maintenanceReserve: number;
  netOperatingIncome: number;
  debtService: number;
  interestPortion: number;
  principalPortion: number;
  capexPayments: number;
  reserveBalanceStart: number;
  capexFromReserve: number;
  capexFromCashflow: number;
  reserveBalanceEnd: number;
  cashflowBeforeTax: number;
  depreciation: number;
  interestDeduction: number;
  maintenanceDeduction: number;
  taxableIncome: number;
  taxPayment: number;
  cashflowAfterTax: number;
  cumulativeCashflow: number;
  outstandingDebt: number;
  ltvPercent: number;
  dscrYear: number;
  icrYear: number;
}

// Steuer-Bridge: waterfall of tax drivers per year
export interface TaxBridgeRow {
  year: number;
  grossIncome: number;
  depreciation: number;
  interestExpense: number;
  maintenanceExpense: number;
  operatingExpenses: number;
  maintenanceReserve: number;
  taxableIncome: number;
  taxPayment: number;
}

// CapEx timeline item for Gantt-style display
export interface CapExTimelineItem {
  id: string;
  name: string;
  category: CapExCategory;
  year: number;
  month: number;
  amount: number;
  taxClassification: TaxClassification;
  distributionYears?: number;
}

// Investment metrics
export interface InvestmentMetrics {
  irrBeforeTaxPercent: number;
  irrAfterTaxPercent: number;
  npvBeforeTax: Money;
  npvAfterTax: Money;
  cashOnCashPercent: number;
  equityMultiple: number;
  dscrMin: number;
  dscrAvg: number;
  icrMin: number;
  ltvInitialPercent: number;
  ltvFinalPercent: number;
  breakEvenRent: Money;
  roiPercent: number;
  maintenanceRiskScore: number;   // 0-100
  liquidityRiskScore: number;     // 0-100
}

// Tax summary
export interface TaxSummary {
  totalDepreciation: Money;
  totalInterestDeduction: Money;
  totalMaintenanceDeduction: Money;
  totalOperatingDeduction: Money;
  acquisitionRelatedCostsTriggered: boolean;
  acquisitionRelatedCostsAmount: Money;
  totalTaxPayment: Money;
  totalTaxSavings: Money;
  annualDepreciation: Money;
  depreciationRatePercent: number;
  depreciationBasis: Money;
  effectiveTaxRatePercent: number;
  totalMaintenanceReserve: Money;
}

// Calculation warning
export type WarningSeverity = 'info' | 'warning' | 'critical';

export interface CalculationWarning {
  type: string;
  message: string;
  severity: WarningSeverity;
}

// Component deterioration tracking
export interface RecurringMaintenanceInfo {
  name: string;
  intervalYears: number;
  costPerOccurrence: number;
  occurrencesInPeriod: number;
  totalCostInPeriod: number;
  effectiveCycleYears: number;
  valueImprovement: number;       // Positive: avoided deterioration
}

export interface ComponentDeteriorationRow {
  category: CapExCategory;
  ageAtStart: number;
  ageAtEnd: number;
  cycleYears: number;
  dueYear: number;
  renewalCostEstimate: number;
  capexAddressedYear: number | null;
  valueImpact: number;
  statusAtEnd: 'OK' | 'Overdue' | 'OverdueAtPurchase' | 'Renewed';
  recurringMaintenance?: RecurringMaintenanceInfo;
}

export interface ComponentDeteriorationSummary {
  components: ComponentDeteriorationRow[];
  totalValueImpact: number;
  totalRenewalCostIfAllDone: number;
  coveredByCapex: number;
  uncoveredDeterioration: number;
}

// Property value forecast
export interface PropertyValueRow {
  year: number;
  marketValue: number;
  improvementUplift: number;
  conditionFactor: number;
  conditionDelta: number;
  meanReversionAdjustment: number;
  componentDeteriorationCumulative: number;
  estimatedValue: number;
}

export interface PropertyValueScenario {
  label: string;
  annualAppreciationPercent: number;
  yearlyValues: PropertyValueRow[];
  finalValue: number;
}

export interface MarketComparison {
  regionalPricePerSqm: number;
  livingArea: number;
  fairMarketValue: number;
  purchasePriceToMarketRatio: number;
  assessment: 'below' | 'at' | 'above';
}

export type ForecastDriverType =
  | 'initialCondition'
  | 'overdueComponents'
  | 'degradation'
  | 'componentDeterioration'
  | 'investments'
  | 'marketAppreciation'
  | 'meanReversion'
  | 'summary';

export interface ForecastDriver {
  type: ForecastDriverType;
  params: Record<string, string | number>;
}

export interface PropertyValueForecast {
  purchasePrice: number;
  improvementValueFactor: number;
  initialConditionFactor: number;
  marketComparison?: MarketComparison;
  componentDeterioration?: ComponentDeteriorationSummary;
  drivers: ForecastDriver[];
  scenarios: PropertyValueScenario[];
}

// Exit / total return analysis
export interface ExitScenario {
  label: string;
  annualAppreciationPercent: number;
  propertyValueAtExit: number;
  saleCosts: number;
  capitalGain: number;
  capitalGainsTax: number;
  netSaleProceeds: number;
  totalReturn: number;
  totalReturnPercent: number;
  annualizedReturnPercent: number;
}

export interface ExitAnalysis {
  holdingPeriodYears: number;
  isWithinSpeculationPeriod: boolean;
  purchasePrice: number;
  totalPurchaseCosts: number;
  equityInvested: number;
  totalCashflowAfterTax: number;
  outstandingDebtAtExit: number;
  totalGrossIncome: number;
  totalOperatingCosts: number;
  totalDebtService: number;
  totalCapex: number;
  totalTaxPaid: number;
  totalMaintenanceReserve: number;
  finalReserveBalance: number;
  saleCostsPercent: number;
  scenarios: ExitScenario[];
}

// Full calculation result
export interface CalculationResult {
  projectId: string;
  calculatedAt: string;
  metrics: InvestmentMetrics;
  yearlyCashflows: YearlyCashflowRow[];
  taxBridge: TaxBridgeRow[];
  capexTimeline: CapExTimelineItem[];
  taxSummary: TaxSummary;
  warnings: CalculationWarning[];
  totalEquityInvested: number;
  totalCashflowBeforeTax: number;
  totalCashflowAfterTax: number;
  exitAnalysis: ExitAnalysis;
  propertyValueForecast: PropertyValueForecast;
}
