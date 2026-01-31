using System.Text.Json.Serialization;

namespace Kalkimo.Api.Models;

// ===========================================
// Response DTOs für Berechnungsergebnisse
// Exakt auf Frontend-TypeScript-Interfaces abgestimmt.
// MoneyTimeSeries wird NICHT exponiert — nur Jahresaggregate.
// ===========================================

/// <summary>
/// Haupt-Response-DTO für Berechnungsergebnisse
/// </summary>
public record CalculationResponseDto
{
    public required string ProjectId { get; init; }
    public required string CalculatedAt { get; init; }
    public required InvestmentMetricsDto Metrics { get; init; }
    public required IReadOnlyList<YearlyCashflowRowDto> YearlyCashflows { get; init; }
    public required IReadOnlyList<TaxBridgeRowDto> TaxBridge { get; init; }
    public required IReadOnlyList<CapExTimelineItemDto> CapexTimeline { get; init; }
    public required TaxSummaryDto TaxSummary { get; init; }
    public required IReadOnlyList<CalculationWarningDto> Warnings { get; init; }
    public decimal TotalEquityInvested { get; init; }
    public decimal TotalCashflowBeforeTax { get; init; }
    public decimal TotalCashflowAfterTax { get; init; }
    public required ExitAnalysisDto ExitAnalysis { get; init; }
    public required PropertyValueForecastDto PropertyValueForecast { get; init; }
}

/// <summary>
/// Rendite- und Risikokennzahlen
/// </summary>
public record InvestmentMetricsDto
{
    public decimal IrrBeforeTaxPercent { get; init; }
    public decimal IrrAfterTaxPercent { get; init; }
    public required MoneyDto NpvBeforeTax { get; init; }
    public required MoneyDto NpvAfterTax { get; init; }
    public decimal CashOnCashPercent { get; init; }
    public decimal EquityMultiple { get; init; }
    public decimal DscrMin { get; init; }
    public decimal DscrAvg { get; init; }
    public decimal IcrMin { get; init; }
    public decimal LtvInitialPercent { get; init; }
    public decimal LtvFinalPercent { get; init; }
    public required MoneyDto BreakEvenRent { get; init; }
    public decimal RoiPercent { get; init; }
    public int MaintenanceRiskScore { get; init; }
    public int LiquidityRiskScore { get; init; }
}

/// <summary>
/// Jährliche Cashflow-Zeile
/// </summary>
public record YearlyCashflowRowDto
{
    public int Year { get; init; }
    public decimal GrossRent { get; init; }
    public decimal VacancyLoss { get; init; }
    public decimal EffectiveRent { get; init; }
    public decimal ServiceChargeIncome { get; init; }
    public decimal OperatingCosts { get; init; }
    public decimal MaintenanceReserve { get; init; }
    public decimal NetOperatingIncome { get; init; }
    public decimal DebtService { get; init; }
    public decimal InterestPortion { get; init; }
    public decimal PrincipalPortion { get; init; }
    public decimal CapexPayments { get; init; }
    public decimal ReserveBalanceStart { get; init; }
    public decimal CapexFromReserve { get; init; }
    public decimal CapexFromCashflow { get; init; }
    public decimal ReserveBalanceEnd { get; init; }
    public decimal CashflowBeforeTax { get; init; }
    public decimal Depreciation { get; init; }
    public decimal InterestDeduction { get; init; }
    public decimal MaintenanceDeduction { get; init; }
    public decimal TaxableIncome { get; init; }
    public decimal TaxPayment { get; init; }
    public decimal CashflowAfterTax { get; init; }
    public decimal CumulativeCashflow { get; init; }
    public decimal OutstandingDebt { get; init; }
    public decimal LtvPercent { get; init; }
    public decimal DscrYear { get; init; }
    public decimal IcrYear { get; init; }
}

/// <summary>
/// Steuer-Brücke: Waterfall pro Jahr
/// </summary>
public record TaxBridgeRowDto
{
    public int Year { get; init; }
    public decimal GrossIncome { get; init; }
    public decimal Depreciation { get; init; }
    public decimal InterestExpense { get; init; }
    public decimal MaintenanceExpense { get; init; }
    public decimal OperatingExpenses { get; init; }
    public decimal MaintenanceReserve { get; init; }
    public decimal TaxableIncome { get; init; }
    public decimal TaxPayment { get; init; }
}

/// <summary>
/// CapEx-Timeline Eintrag
/// </summary>
public record CapExTimelineItemDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal Amount { get; init; }
    public required string TaxClassification { get; init; }
    public int? DistributionYears { get; init; }
}

/// <summary>
/// Steuerliche Zusammenfassung
/// </summary>
public record TaxSummaryDto
{
    public required MoneyDto TotalDepreciation { get; init; }
    public required MoneyDto TotalInterestDeduction { get; init; }
    public required MoneyDto TotalMaintenanceDeduction { get; init; }
    public required MoneyDto TotalOperatingDeduction { get; init; }
    public bool AcquisitionRelatedCostsTriggered { get; init; }
    public required MoneyDto AcquisitionRelatedCostsAmount { get; init; }
    public required MoneyDto TotalTaxPayment { get; init; }
    public required MoneyDto TotalTaxSavings { get; init; }
    public required MoneyDto AnnualDepreciation { get; init; }
    public decimal DepreciationRatePercent { get; init; }
    public required MoneyDto DepreciationBasis { get; init; }
    public decimal EffectiveTaxRatePercent { get; init; }
    public required MoneyDto TotalMaintenanceReserve { get; init; }
}

/// <summary>
/// Berechnungswarnung
/// </summary>
public record CalculationWarningDto
{
    public required string Type { get; init; }
    public required string Message { get; init; }
    public required string Severity { get; init; }
}

/// <summary>
/// Money-DTO (Amount + Currency)
/// </summary>
public record MoneyDto
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";
}

// ===========================================
// Exit-Analyse DTOs
// ===========================================

public record ExitAnalysisDto
{
    public int HoldingPeriodYears { get; init; }
    public bool IsWithinSpeculationPeriod { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal TotalPurchaseCosts { get; init; }
    public decimal EquityInvested { get; init; }
    public decimal TotalCashflowAfterTax { get; init; }
    public decimal OutstandingDebtAtExit { get; init; }
    public decimal TotalGrossIncome { get; init; }
    public decimal TotalOperatingCosts { get; init; }
    public decimal TotalDebtService { get; init; }
    public decimal TotalCapex { get; init; }
    public decimal TotalTaxPaid { get; init; }
    public decimal TotalMaintenanceReserve { get; init; }
    public decimal FinalReserveBalance { get; init; }
    public decimal SaleCostsPercent { get; init; }
    public required IReadOnlyList<ExitScenarioDto> Scenarios { get; init; }
}

public record ExitScenarioDto
{
    public required string Label { get; init; }
    public decimal AnnualAppreciationPercent { get; init; }
    public decimal PropertyValueAtExit { get; init; }
    public decimal SaleCosts { get; init; }
    public decimal CapitalGain { get; init; }
    public decimal CapitalGainsTax { get; init; }
    public decimal NetSaleProceeds { get; init; }
    public decimal TotalReturn { get; init; }
    public decimal TotalReturnPercent { get; init; }
    public decimal AnnualizedReturnPercent { get; init; }
}

// ===========================================
// PropertyValueForecast DTOs
// ===========================================

public record PropertyValueForecastDto
{
    public decimal PurchasePrice { get; init; }
    public decimal ImprovementValueFactor { get; init; }
    public decimal InitialConditionFactor { get; init; }
    public MarketComparisonDto? MarketComparison { get; init; }
    public ComponentDeteriorationSummaryDto? ComponentDeterioration { get; init; }
    public required IReadOnlyList<ForecastDriverDto> Drivers { get; init; }
    public required IReadOnlyList<PropertyValueScenarioDto> Scenarios { get; init; }
}

public record MarketComparisonDto
{
    public decimal RegionalPricePerSqm { get; init; }
    public decimal LivingArea { get; init; }
    public decimal FairMarketValue { get; init; }
    public decimal PurchasePriceToMarketRatio { get; init; }
    public required string Assessment { get; init; }
}

public record ComponentDeteriorationSummaryDto
{
    public required IReadOnlyList<ComponentDeteriorationRowDto> Components { get; init; }
    public decimal TotalValueImpact { get; init; }
    public decimal TotalRenewalCostIfAllDone { get; init; }
    public decimal CoveredByCapex { get; init; }
    public decimal UncoveredDeterioration { get; init; }
}

public record ComponentDeteriorationRowDto
{
    public required string Category { get; init; }
    public int AgeAtStart { get; init; }
    public int AgeAtEnd { get; init; }
    public int CycleYears { get; init; }
    public int DueYear { get; init; }
    public decimal RenewalCostEstimate { get; init; }
    public int? CapexAddressedYear { get; init; }
    public decimal ValueImpact { get; init; }
    public required string StatusAtEnd { get; init; }
    public RecurringMaintenanceInfoDto? RecurringMaintenance { get; init; }
}

public record RecurringMaintenanceInfoDto
{
    public required string Name { get; init; }
    public int IntervalYears { get; init; }
    public decimal CostPerOccurrence { get; init; }
    public int OccurrencesInPeriod { get; init; }
    public decimal TotalCostInPeriod { get; init; }
    public int EffectiveCycleYears { get; init; }
    public decimal ValueImprovement { get; init; }
}

public record PropertyValueScenarioDto
{
    public required string Label { get; init; }
    public decimal AnnualAppreciationPercent { get; init; }
    public required IReadOnlyList<PropertyValueRowDto> YearlyValues { get; init; }
    public decimal FinalValue { get; init; }
}

public record PropertyValueRowDto
{
    public int Year { get; init; }
    public decimal MarketValue { get; init; }
    public decimal ImprovementUplift { get; init; }
    public decimal ConditionFactor { get; init; }
    public decimal ConditionDelta { get; init; }
    public decimal MeanReversionAdjustment { get; init; }
    public decimal ComponentDeteriorationCumulative { get; init; }
    public decimal EstimatedValue { get; init; }
}

public record ForecastDriverDto
{
    public required string Type { get; init; }
    public Dictionary<string, object> Params { get; init; } = new();
}
