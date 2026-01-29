namespace Kalkimo.Domain.Models;

/// <summary>
/// Vollständiges Berechnungsergebnis
/// </summary>
public record CalculationResult
{
    public required string ProjectId { get; init; }
    public required string ScenarioId { get; init; }
    public required YearMonth CalculatedAt { get; init; }
    public required string EngineVersion { get; init; }

    // === Zeitreihen ===
    public required MoneyTimeSeries GrossRent { get; init; }
    public required MoneyTimeSeries EffectiveRent { get; init; }
    public required MoneyTimeSeries ServiceChargeIncome { get; init; }
    public required MoneyTimeSeries TransferableCosts { get; init; }
    public required MoneyTimeSeries NonTransferableCosts { get; init; }
    public required MoneyTimeSeries OperatingCosts { get; init; }
    public required MoneyTimeSeries NetOperatingIncome { get; init; }
    public required MoneyTimeSeries DebtService { get; init; }
    public required MoneyTimeSeries InterestExpense { get; init; }
    public required MoneyTimeSeries PrincipalRepayment { get; init; }
    public required MoneyTimeSeries CashflowBeforeTax { get; init; }
    public required MoneyTimeSeries TaxPayment { get; init; }
    public required MoneyTimeSeries CashflowAfterTax { get; init; }
    public required MoneyTimeSeries CumulativeCashflow { get; init; }
    public required MoneyTimeSeries ReserveBalance { get; init; }
    public required MoneyTimeSeries OutstandingDebt { get; init; }
    public required MoneyTimeSeries PropertyValue { get; init; }

    // === Steuerzeitreihen ===
    public required MoneyTimeSeries DepreciationDeduction { get; init; }
    public required MoneyTimeSeries TaxableIncome { get; init; }

    // === Kennzahlen ===
    public required InvestmentMetrics Metrics { get; init; }

    // === Steuersicht ===
    public required TaxSummary TaxSummary { get; init; }

    // === Warnungen ===
    public IReadOnlyList<CalculationWarning> Warnings { get; init; } = [];

    // === Investor-Ergebnisse ===
    public IReadOnlyList<InvestorResult> InvestorResults { get; init; } = [];
}

/// <summary>
/// Rendite- und Risikokennzahlen
/// </summary>
public record InvestmentMetrics
{
    // === Renditekennzahlen ===
    /// <summary>Interner Zinsfuß vor Steuern (%)</summary>
    public decimal IrrBeforeTaxPercent { get; init; }

    /// <summary>Interner Zinsfuß nach Steuern (%)</summary>
    public decimal IrrAfterTaxPercent { get; init; }

    /// <summary>Kapitalwert vor Steuern</summary>
    public Money NpvBeforeTax { get; init; }

    /// <summary>Kapitalwert nach Steuern</summary>
    public Money NpvAfterTax { get; init; }

    /// <summary>Eigenkapitalrendite p.a. (%)</summary>
    public decimal RoiPercent { get; init; }

    /// <summary>Cash-on-Cash Return (%)</summary>
    public decimal CashOnCashPercent { get; init; }

    /// <summary>Equity Multiple</summary>
    public decimal EquityMultiple { get; init; }

    // === Bankkennzahlen ===
    /// <summary>DSCR (Debt Service Coverage Ratio) Minimum</summary>
    public decimal DscrMin { get; init; }

    /// <summary>DSCR Durchschnitt</summary>
    public decimal DscrAvg { get; init; }

    /// <summary>ICR (Interest Coverage Ratio) Minimum</summary>
    public decimal IcrMin { get; init; }

    /// <summary>LTV (Loan to Value) Initial (%)</summary>
    public decimal LtvInitialPercent { get; init; }

    /// <summary>LTV am Ende (%)</summary>
    public decimal LtvFinalPercent { get; init; }

    /// <summary>Break-Even Miete (monatlich)</summary>
    public Money BreakEvenRent { get; init; }

    // === Risikokennzahlen ===
    /// <summary>Sanierungsrisiko-Score (0-100)</summary>
    public int MaintenanceRiskScore { get; init; }

    /// <summary>Liquiditätsrisiko-Score (0-100)</summary>
    public int LiquidityRiskScore { get; init; }

    /// <summary>Monate bis Liquiditätsunterdeckung (null = nie)</summary>
    public int? MonthsToLiquidityShortfall { get; init; }
}

/// <summary>
/// Steuerliche Zusammenfassung
/// </summary>
public record TaxSummary
{
    /// <summary>Gesamte AfA über Laufzeit</summary>
    public Money TotalDepreciation { get; init; }

    /// <summary>Gesamte Schuldzinsen</summary>
    public Money TotalInterestDeduction { get; init; }

    /// <summary>Gesamter Erhaltungsaufwand</summary>
    public Money TotalMaintenanceDeduction { get; init; }

    /// <summary>15%-Regel ausgelöst?</summary>
    public bool AcquisitionRelatedCostsTriggered { get; init; }

    /// <summary>Betrag der anschaffungsnahen HK</summary>
    public Money AcquisitionRelatedCostsAmount { get; init; }

    /// <summary>§82b Verteilungen</summary>
    public IReadOnlyList<MaintenanceDistribution> MaintenanceDistributions { get; init; } = [];

    /// <summary>Verkaufssteuer (§23 EStG)</summary>
    public Money? CapitalGainsTax { get; init; }

    /// <summary>Verkauf steuerfrei (10-Jahres-Frist)?</summary>
    public bool SaleTaxExempt { get; init; }

    /// <summary>Gesamte Steuerlast über Laufzeit</summary>
    public Money TotalTaxPayment { get; init; }

    /// <summary>Steuerliche Effekte als Zeitreihe nach Jahr</summary>
    public Dictionary<int, TaxYearSummary> YearlyTax { get; init; } = new();
}

/// <summary>
/// §82b EStDV Verteilung
/// </summary>
public record MaintenanceDistribution
{
    public required string MeasureId { get; init; }
    public required Money TotalAmount { get; init; }
    public required int DistributionYears { get; init; }
    public required int StartYear { get; init; }
    public Money AnnualDeduction => TotalAmount / DistributionYears;
}

/// <summary>
/// Steuerliche Zusammenfassung pro Jahr
/// </summary>
public record TaxYearSummary
{
    public int Year { get; init; }
    public Money GrossIncome { get; init; }
    public Money Depreciation { get; init; }
    public Money InterestExpense { get; init; }
    public Money MaintenanceExpense { get; init; }
    public Money OtherDeductions { get; init; }
    public Money TaxableIncome { get; init; }
    public Money TaxPayment { get; init; }
}

/// <summary>
/// Berechnungswarnung
/// </summary>
public record CalculationWarning
{
    public required WarningType Type { get; init; }
    public required string Message { get; init; }
    public required WarningSeverity Severity { get; init; }
    public YearMonth? Period { get; init; }
    public string? RelatedField { get; init; }
}

public enum WarningType
{
    LiquidityShortfall,
    ReserveBelowThreshold,
    DscrBelowOne,
    AcquisitionRelatedCostsTriggered,
    DeferredMaintenance,
    HighLtv,
    NegativeCashflow,
    TaxLossCarryforward
}

public enum WarningSeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// Ergebnis für einen einzelnen Investor
/// </summary>
public record InvestorResult
{
    public required string InvestorId { get; init; }
    public required string InvestorName { get; init; }

    /// <summary>Beteiligungsquote (%)</summary>
    public required decimal SharePercent { get; init; }

    /// <summary>Eigenkapital-Beitrag</summary>
    public required Money EquityContribution { get; init; }

    /// <summary>Anteilige Cashflow-Zeitreihe (vor Steuern)</summary>
    public required MoneyTimeSeries ProRataCashflow { get; init; }

    /// <summary>Anteilige Ausschüttungen</summary>
    public required MoneyTimeSeries Distributions { get; init; }

    /// <summary>Kumulierte Ausschüttungen</summary>
    public required Money TotalDistributions { get; init; }

    /// <summary>IRR vor Steuern für diesen Investor (%)</summary>
    public decimal IrrBeforeTaxPercent { get; init; }

    /// <summary>IRR nach Steuern für diesen Investor (falls individuelles Steuerprofil)</summary>
    public decimal? IrrAfterTaxPercent { get; init; }

    /// <summary>Kapitalwert vor Steuern</summary>
    public Money NpvBeforeTax { get; init; } = Money.Zero();

    /// <summary>Equity Multiple</summary>
    public decimal EquityMultiple { get; init; }

    /// <summary>Cash-on-Cash Return (%)</summary>
    public decimal CashOnCashPercent { get; init; }
}
