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
}

// CapEx models
export interface CapExMeasure {
  id: string;
  name: string;
  category: CapExCategory;
  amount: Money;
  scheduledDate: YearMonth;
  taxClassification: TaxClassification;
  distributionYears?: number; // for §82b
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
  operatingCosts: number;
  netOperatingIncome: number;
  debtService: number;
  capexPayments: number;
  cashflowBeforeTax: number;
  depreciation: number;
  interestDeduction: number;
  maintenanceDeduction: number;
  taxableIncome: number;
  taxPayment: number;
  cashflowAfterTax: number;
  cumulativeCashflow: number;
  outstandingDebt: number;
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
  ltvInitialPercent: number;
  ltvFinalPercent: number;
  breakEvenRent: Money;
  roiPercent: number;
}

// Tax summary
export interface TaxSummary {
  totalDepreciation: Money;
  totalInterestDeduction: Money;
  totalMaintenanceDeduction: Money;
  acquisitionRelatedCostsTriggered: boolean;
  acquisitionRelatedCostsAmount: Money;
  totalTaxPayment: Money;
  annualDepreciation: Money;
  depreciationRatePercent: number;
  depreciationBasis: Money;
}

// Calculation warning
export type WarningSeverity = 'info' | 'warning' | 'critical';

export interface CalculationWarning {
  type: string;
  message: string;
  severity: WarningSeverity;
}

// Full calculation result
export interface CalculationResult {
  projectId: string;
  calculatedAt: string;
  metrics: InvestmentMetrics;
  yearlyCashflows: YearlyCashflowRow[];
  taxSummary: TaxSummary;
  warnings: CalculationWarning[];
  totalEquityInvested: number;
  totalCashflowBeforeTax: number;
  totalCashflowAfterTax: number;
}
