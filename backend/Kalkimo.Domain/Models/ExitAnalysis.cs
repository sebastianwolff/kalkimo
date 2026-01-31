namespace Kalkimo.Domain.Models;

/// <summary>
/// Ergebnis der Exit-Analyse (Multi-Szenario)
/// </summary>
public record ExitAnalysisResult
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
    public decimal SaleCostsPercent { get; init; }
    public required IReadOnlyList<ExitScenario> Scenarios { get; init; }
}

/// <summary>
/// Einzelnes Exit-Szenario
/// </summary>
public record ExitScenario
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
