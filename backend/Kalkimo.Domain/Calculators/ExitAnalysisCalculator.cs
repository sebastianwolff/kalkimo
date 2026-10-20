using Kalkimo.Domain.Models;

namespace Kalkimo.Domain.Calculators;

/// <summary>
/// Berechnet die Exit-Analyse mit Multi-Szenario-Modell.
///
/// Für jedes Szenario der PropertyValueForecast wird berechnet:
/// - Verkaufserlös abzgl. Verkaufskosten
/// - Veräußerungsgewinn und -steuer über <see cref="TaxCalculator.CalculateCapitalGainsTax"/>
///   (§23 EStG bzw. gewerbliche Einkünfte bei Kapitalgesellschaften)
/// - Netto-Verkaufserlös nach Ablösung der Restschuld
/// - Gesamtrendite und Eigenkapital-IRR
/// </summary>
public static class ExitAnalysisCalculator
{
    /// <summary>Verkaufsnebenkosten in % (~3,57% Makler + ~1,5% Notar), falls nicht konfiguriert</summary>
    private const decimal DefaultSaleCostsPercent = 5.0m;

    /// <summary>
    /// Berechnet die vollständige Exit-Analyse
    /// </summary>
    /// <param name="taxCalculator">Gemeinsame Steuerlogik (Default: neue Instanz)</param>
    /// <param name="exitPeriod">Verkaufszeitpunkt (Default: Ende des Betrachtungszeitraums)</param>
    /// <param name="executedMeasures">Durchgeführte Maßnahmen für aktivierte HK (Default: aus Projekt)</param>
    public static ExitAnalysisResult Calculate(
        Project project,
        PropertyValueForecastResult forecast,
        MoneyTimeSeries cashflowAfterTax,
        MoneyTimeSeries grossRent,
        MoneyTimeSeries operatingCosts,
        MoneyTimeSeries debtService,
        MoneyTimeSeries capExPayments,
        MoneyTimeSeries taxPayment,
        MoneyTimeSeries outstandingDebt,
        MoneyTimeSeries reserveBalance,
        MoneyTimeSeries depreciation,
        TaxCalculator? taxCalculator = null,
        YearMonth? exitPeriod = null,
        IEnumerable<CapExMeasure>? executedMeasures = null)
    {
        taxCalculator ??= new TaxCalculator(new SystemDateProvider());
        var exit = exitPeriod ?? project.EndPeriod;
        var purchase = project.Purchase;
        var purchasePeriod = YearMonth.FromDate(purchase.PurchaseDate);
        var equity = project.Financing.TotalEquity.Amount;
        var currency = project.Currency;
        var measures = (executedMeasures ?? project.CapEx?.Measures.Where(m => m.IsExecuted) ?? []).ToList();

        // Haltedauer in vollen Jahren (Anschaffungsmonat bis einschließlich Exit-Monat)
        var holdingPeriodYears = (YearMonth.MonthsBetween(purchasePeriod, exit) + 1) / 12;

        // Summen bis zum Exit
        decimal SumUntilExit(MoneyTimeSeries series) =>
            series.Where(p => p.Period <= exit).Sum(p => p.Value.Amount);

        var totalCFAfterTax = SumUntilExit(cashflowAfterTax);
        var outstandingDebtAtExit = outstandingDebt.HasValue(exit) ? outstandingDebt[exit].Amount : 0m;
        var reserveAtExit = reserveBalance.HasValue(exit) ? reserveBalance[exit].Amount : 0m;

        // Steuerliche Basis: AfA und aktivierte Maßnahmen bis zum Exit
        var accumulatedDepreciation = new Money(SumUntilExit(depreciation), currency);
        var capitalizedImprovements = taxCalculator
            .GetCapitalizedMeasureCosts(purchase, measures)
            .Where(c => c.Period <= exit)
            .Aggregate(Money.Zero(currency), (sum, c) => sum + c.Amount);

        var saleDate = exit.ToFirstDayOfMonth();
        var isWithinSpeculationPeriod = project.TaxProfile.OwnershipType != OwnershipType.Corporation
            && !project.Valuation.CapitalGainsTax.OwnerOccupiedExemption
            && saleDate <= purchase.PurchaseDate.AddYears(project.Valuation.CapitalGainsTax.HoldingPeriodYears);

        var saleCostsPercent = project.Valuation.SaleCostsPercent > 0
            ? project.Valuation.SaleCostsPercent
            : DefaultSaleCostsPercent;

        var exitScenarios = forecast.Scenarios.Select(scenario =>
        {
            var propertyValue = GetValueAtExit(scenario, exit);
            var saleCosts = propertyValue * saleCostsPercent / 100;

            var taxResult = taxCalculator.CalculateCapitalGainsTax(
                purchase,
                new Money(propertyValue, currency),
                new Money(saleCosts, currency),
                accumulatedDepreciation,
                saleDate,
                project.TaxProfile,
                project.Valuation.CapitalGainsTax,
                capitalizedImprovements);

            var capitalGainsTax = taxResult.TaxAmount.Amount;
            var netSaleProceeds = propertyValue - saleCosts - capitalGainsTax - outstandingDebtAtExit;
            var totalReturn = netSaleProceeds + totalCFAfterTax - equity;
            var totalReturnPercent = equity > 0 ? totalReturn / equity * 100 : 0;

            // Eigenkapital-IRR nach Steuern (berücksichtigt den zeitlichen Anfall der Cashflows)
            var annualizedReturn = equity > 0
                ? MetricsCalculator.CalculateIrr(
                    project.Financing.TotalEquity, cashflowAfterTax, new Money(netSaleProceeds, currency), exit)
                : 0m;

            return new ExitScenario
            {
                Label = scenario.Label,
                AnnualAppreciationPercent = scenario.AnnualAppreciationPercent,
                PropertyValueAtExit = propertyValue,
                SaleCosts = saleCosts,
                CapitalGain = taxResult.Gain.Amount,
                CapitalGainsTax = capitalGainsTax,
                NetSaleProceeds = netSaleProceeds,
                TotalReturn = totalReturn,
                TotalReturnPercent = totalReturnPercent,
                AnnualizedReturnPercent = annualizedReturn
            };
        }).ToList();

        return new ExitAnalysisResult
        {
            HoldingPeriodYears = holdingPeriodYears,
            IsWithinSpeculationPeriod = isWithinSpeculationPeriod,
            PurchasePrice = purchase.PurchasePrice.Amount,
            TotalPurchaseCosts = purchase.TotalAcquisitionCosts.Amount,
            EquityInvested = equity,
            TotalCashflowAfterTax = totalCFAfterTax,
            OutstandingDebtAtExit = outstandingDebtAtExit,
            TotalGrossIncome = SumUntilExit(grossRent),
            TotalOperatingCosts = SumUntilExit(operatingCosts),
            TotalDebtService = SumUntilExit(debtService),
            TotalCapex = SumUntilExit(capExPayments),
            TotalTaxPaid = SumUntilExit(taxPayment),
            TotalMaintenanceReserve = reserveAtExit,
            SaleCostsPercent = saleCostsPercent,
            Scenarios = exitScenarios
        };
    }

    /// <summary>
    /// Wert des Szenarios im Exit-Jahr (Jahreswerte der Prognose); Fallback: Endwert
    /// </summary>
    private static decimal GetValueAtExit(PropertyValueScenario scenario, YearMonth exit)
    {
        var row = scenario.YearlyValues.FirstOrDefault(r => r.Year == exit.Year);
        return row?.EstimatedValue ?? scenario.FinalValue;
    }
}
