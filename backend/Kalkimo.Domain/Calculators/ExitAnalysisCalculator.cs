using Kalkimo.Domain.Models;

namespace Kalkimo.Domain.Calculators;

/// <summary>
/// Berechnet die Exit-Analyse mit Multi-Szenario-Modell.
///
/// Für jedes Szenario der PropertyValueForecast wird berechnet:
/// - Verkaufserlös abzgl. Verkaufskosten
/// - Veräußerungsgewinn (§23 EStG)
/// - Spekulationssteuer (1.000€ Freigrenze, 10-Jahres-Frist)
/// - Netto-Verkaufserlös
/// - Gesamtrendite und annualisierte Rendite
/// </summary>
public static class ExitAnalysisCalculator
{
    /// <summary>Verkaufsnebenkosten in % (~3,57% Makler + ~1,5% Notar)</summary>
    private const decimal DefaultSaleCostsPercent = 5.0m;

    /// <summary>§23 Abs. 3 S. 5 EStG: Freigrenze 1.000 EUR (ab 2024, Wachstumschancengesetz)</summary>
    private const decimal CapitalGainsExemptionThreshold = 1_000m;

    /// <summary>§23 Abs. 1 S. 1 Nr. 1 EStG: 10-Jahres-Spekulationsfrist</summary>
    private const int SpeculationPeriodYears = 10;

    /// <summary>
    /// Berechnet die vollständige Exit-Analyse
    /// </summary>
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
        MoneyTimeSeries depreciation)
    {
        var startYear = project.StartPeriod.Year;
        var endYear = project.EndPeriod.Year;
        var holdingPeriodYears = endYear - startYear;
        var purchasePrice = project.Purchase.PurchasePrice.Amount;
        var purchaseCosts = project.Purchase.TotalAcquisitionCosts.Amount;
        var equity = project.Financing.TotalEquity.Amount;
        var effectiveTaxRate = project.TaxProfile.EffectiveTaxRatePercent;
        var currency = project.Currency;

        // Check speculation period — day-precise like TaxCalculator
        var purchaseDate = project.Purchase.PurchaseDate;
        var exitDate = new DateOnly(endYear, 12, 31);
        var tenYearDeadline = purchaseDate.AddYears(SpeculationPeriodYears);
        var isWithinSpeculationPeriod = exitDate <= tenYearDeadline;

        // Accumulate totals from time series
        var totalCFAfterTax = cashflowAfterTax.Sum().Amount;
        var totalGrossIncome = grossRent.Sum().Amount;
        var totalOperatingCostsAmount = operatingCosts.Sum().Amount;
        var totalDebtServiceAmount = debtService.Sum().Amount;
        var totalCapexAmount = capExPayments.Sum().Amount;
        var totalTaxPaid = taxPayment.Sum().Amount;

        // Reserve balance at end
        var endPeriod = project.EndPeriod;
        var totalMaintenanceReserveAmount = reserveBalance.HasValue(endPeriod)
            ? reserveBalance[endPeriod].Amount
            : 0m;

        // Outstanding debt at exit (last month)
        var outstandingDebtAtExit = outstandingDebt.HasValue(endPeriod)
            ? outstandingDebt[endPeriod].Amount
            : 0m;

        // Accumulated depreciation (AfA) reduces the tax basis at sale
        var accumulatedDepreciation = depreciation.Sum().Amount;

        // Tax basis at sale: Purchase + acquisition costs + capitalized improvements - accumulated AfA
        // §23 EStG: Anschaffungskosten minus bisher in Anspruch genommene AfA
        var taxBasisAtSale = purchasePrice + purchaseCosts;
        if (project.CapEx != null)
        {
            taxBasisAtSale += project.CapEx.Measures
                .Where(m => m.TaxClassification == TaxClassification.ManufacturingCosts ||
                           m.TaxClassification == TaxClassification.AcquisitionRelatedCosts)
                .Sum(m => m.EstimatedCost.Amount);
        }
        taxBasisAtSale -= accumulatedDepreciation;

        var saleCostsPercent = project.Valuation.SaleCostsPercent > 0
            ? project.Valuation.SaleCostsPercent
            : DefaultSaleCostsPercent;

        // Build exit scenarios from property value forecast scenarios
        var exitScenarios = forecast.Scenarios.Select(scenario =>
        {
            var propertyValue = scenario.FinalValue;
            var saleCosts = propertyValue * saleCostsPercent / 100;
            // §23 EStG: Veräußerungsgewinn = Veräußerungserlös - Veräußerungskosten - (um AfA geminderte Anschaffungskosten)
            var capitalGain = propertyValue - saleCosts - taxBasisAtSale;

            // §23 Abs. 3 S. 5 EStG: Freigrenze 1.000 EUR
            // Bei Gewinn > 1.000 EUR → gesamter Gewinn steuerpflichtig (kein Freibetrag!)
            decimal capitalGainsTax = 0;
            if (isWithinSpeculationPeriod && capitalGain > CapitalGainsExemptionThreshold)
            {
                capitalGainsTax = capitalGain * effectiveTaxRate / 100;
            }

            var netSaleProceeds = propertyValue - saleCosts - capitalGainsTax - outstandingDebtAtExit;
            var totalReturn = netSaleProceeds + totalCFAfterTax - equity;
            var totalReturnPercent = equity > 0 ? totalReturn / equity * 100 : 0;
            // Annualized return via CAGR: (endValue/startValue)^(1/n) - 1
            // Guard against negative endValue (net loss → Math.Pow returns NaN → decimal overflow)
            decimal annualizedReturn = 0;
            if (equity > 0 && holdingPeriodYears > 0)
            {
                var returnMultiple = (equity + totalReturn) / equity;
                if (returnMultiple > 0)
                {
                    annualizedReturn = ((decimal)Math.Pow((double)returnMultiple, 1.0 / holdingPeriodYears) - 1) * 100;
                }
            }

            return new ExitScenario
            {
                Label = scenario.Label,
                AnnualAppreciationPercent = scenario.AnnualAppreciationPercent,
                PropertyValueAtExit = propertyValue,
                SaleCosts = saleCosts,
                CapitalGain = capitalGain,
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
            PurchasePrice = purchasePrice,
            TotalPurchaseCosts = purchaseCosts,
            EquityInvested = equity,
            TotalCashflowAfterTax = totalCFAfterTax,
            OutstandingDebtAtExit = outstandingDebtAtExit,
            TotalGrossIncome = totalGrossIncome,
            TotalOperatingCosts = totalOperatingCostsAmount,
            TotalDebtService = totalDebtServiceAmount,
            TotalCapex = totalCapexAmount,
            TotalTaxPaid = totalTaxPaid,
            TotalMaintenanceReserve = totalMaintenanceReserveAmount,
            SaleCostsPercent = saleCostsPercent,
            Scenarios = exitScenarios
        };
    }
}
