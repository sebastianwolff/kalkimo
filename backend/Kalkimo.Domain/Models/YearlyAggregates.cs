namespace Kalkimo.Domain.Models;

/// <summary>
/// Jährliche Cashflow-Zeile (aggregiert aus monatlichen Zeitreihen)
/// </summary>
public record YearlyCashflowRow
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
/// Jährliche Steuer-Brücke (Waterfall von Einkommen zu Steuerlast)
/// </summary>
public record TaxBridgeRow
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
public record CapExTimelineItem
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public CapExCategory Category { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal Amount { get; init; }
    public TaxClassification TaxClassification { get; init; }
    public int? DistributionYears { get; init; }
}
