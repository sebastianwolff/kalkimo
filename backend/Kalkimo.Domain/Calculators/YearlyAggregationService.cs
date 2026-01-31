using Kalkimo.Domain.Models;

namespace Kalkimo.Domain.Calculators;

/// <summary>
/// Aggregiert monatliche MoneyTimeSeries zu Jahreswerten für das Frontend.
/// </summary>
public static class YearlyAggregationService
{
    /// <summary>
    /// Aggregiert alle Zeitreihen zu jährlichen Cashflow-Zeilen
    /// </summary>
    public static IReadOnlyList<YearlyCashflowRow> AggregateToYearlyCashflowRows(
        CalculationResult result,
        MoneyTimeSeries grossRent,
        MoneyTimeSeries capExPayments,
        MoneyTimeSeries maintenanceExpense,
        MoneyTimeSeries propertyValue)
    {
        var rows = new List<YearlyCashflowRow>();
        var startYear = result.GrossRent.StartPeriod.Year;
        var endYear = result.GrossRent.EndPeriod.Year;
        decimal cumulativeCashflow = 0;

        for (var year = startYear; year <= endYear; year++)
        {
            var yearGrossRent = grossRent.SumForYear(year).Amount;
            var yearEffectiveRent = result.EffectiveRent.SumForYear(year).Amount;
            var vacancyLoss = yearGrossRent - yearEffectiveRent;
            var yearServiceCharge = result.ServiceChargeIncome.SumForYear(year).Amount;
            var yearOpCosts = result.OperatingCosts.SumForYear(year).Amount;
            var yearNoi = result.NetOperatingIncome.SumForYear(year).Amount;
            var yearDebtService = result.DebtService.SumForYear(year).Amount;
            var yearInterest = result.InterestExpense.SumForYear(year).Amount;
            var yearPrincipal = result.PrincipalRepayment.SumForYear(year).Amount;
            var yearCapex = capExPayments.SumForYear(year).Amount;
            var yearCfBeforeTax = result.CashflowBeforeTax.SumForYear(year).Amount;
            var yearDepreciation = result.DepreciationDeduction.SumForYear(year).Amount;
            var yearMaintenanceDeduction = maintenanceExpense.SumForYear(year).Amount;
            var yearTaxableIncome = result.TaxableIncome.SumForYear(year).Amount;
            var yearTax = result.TaxPayment.SumForYear(year).Amount;
            var yearCfAfterTax = result.CashflowAfterTax.SumForYear(year).Amount;

            cumulativeCashflow += yearCfAfterTax;

            // Outstanding debt: take last month of year
            var lastMonth = Math.Min(12, result.OutstandingDebt.EndPeriod.Month);
            if (year < endYear) lastMonth = 12;
            var debtPeriod = new YearMonth(year, lastMonth);
            var outstandingDebt = result.OutstandingDebt.HasValue(debtPeriod)
                ? result.OutstandingDebt[debtPeriod].Amount
                : 0m;

            // Property value at year-end
            var pvPeriod = new YearMonth(year, lastMonth);
            var yearPropertyValue = propertyValue.HasValue(pvPeriod)
                ? propertyValue[pvPeriod].Amount
                : 0m;

            // LTV
            var ltvPercent = yearPropertyValue > 0 ? outstandingDebt / yearPropertyValue * 100 : 0;

            // DSCR (annual)
            var dscrYear = yearDebtService > 0 ? yearNoi / yearDebtService : 0;

            // ICR (annual)
            var icrYear = yearInterest > 0 ? yearNoi / yearInterest : 0;

            // Reserve balance tracking
            decimal reserveBalanceStart = 0;
            decimal reserveBalanceEnd = 0;
            decimal yearMaintenanceReserve = 0;
            decimal capexFromReserve = 0;
            decimal capexFromCashflow = 0;

            if (result.ReserveBalance != null)
            {
                reserveBalanceStart = year == startYear
                    ? (result.ReserveBalance.HasValue(result.ReserveBalance.StartPeriod)
                        ? result.ReserveBalance[result.ReserveBalance.StartPeriod].Amount
                        : 0m)
                    : result.ReserveBalance[new YearMonth(year - 1, 12)].Amount;

                // For start year, use balance BEFORE first contribution/withdrawal
                // which is the InitialBalance (approximated by first period balance + first capex - first contribution)
                // Simplified: use value at end of previous year, or 0 for first year
                if (year == startYear)
                {
                    // The first period already includes the first month's contribution minus withdrawal.
                    // The true start is the initial balance before any operations.
                    // We approximate by computing: contributions = end - start + capex, so start is implicit.
                    // For year 1, use 0 (or initial balance if available from the time series)
                    reserveBalanceStart = 0m;
                }

                reserveBalanceEnd = result.ReserveBalance.HasValue(debtPeriod)
                    ? result.ReserveBalance[debtPeriod].Amount
                    : 0m;

                yearMaintenanceReserve = reserveBalanceEnd - reserveBalanceStart + yearCapex;

                // Determine how much CapEx came from reserve vs. cashflow
                // If reserve ends >= 0, all CapEx was covered from reserve
                // If reserve ends < 0, the deficit came from cashflow
                if (yearCapex > 0)
                {
                    var availableInReserve = reserveBalanceEnd + yearCapex;
                    capexFromReserve = Math.Max(0, Math.Min(yearCapex, availableInReserve));
                    capexFromCashflow = yearCapex - capexFromReserve;
                }
            }

            rows.Add(new YearlyCashflowRow
            {
                Year = year,
                GrossRent = yearGrossRent,
                VacancyLoss = vacancyLoss,
                EffectiveRent = yearEffectiveRent,
                ServiceChargeIncome = yearServiceCharge,
                OperatingCosts = yearOpCosts,
                MaintenanceReserve = yearMaintenanceReserve,
                NetOperatingIncome = yearNoi,
                DebtService = yearDebtService,
                InterestPortion = yearInterest,
                PrincipalPortion = yearPrincipal,
                CapexPayments = yearCapex,
                ReserveBalanceStart = reserveBalanceStart,
                CapexFromReserve = capexFromReserve,
                CapexFromCashflow = capexFromCashflow,
                ReserveBalanceEnd = reserveBalanceEnd,
                CashflowBeforeTax = yearCfBeforeTax,
                Depreciation = yearDepreciation,
                InterestDeduction = yearInterest,
                MaintenanceDeduction = yearMaintenanceDeduction,
                TaxableIncome = yearTaxableIncome,
                TaxPayment = yearTax,
                CashflowAfterTax = yearCfAfterTax,
                CumulativeCashflow = cumulativeCashflow,
                OutstandingDebt = outstandingDebt,
                LtvPercent = ltvPercent,
                DscrYear = dscrYear,
                IcrYear = icrYear
            });
        }

        return rows;
    }

    /// <summary>
    /// Aggregiert zu jährlichen Tax-Bridge-Zeilen
    /// </summary>
    public static IReadOnlyList<TaxBridgeRow> AggregateToTaxBridgeRows(
        CalculationResult result,
        MoneyTimeSeries grossRent,
        MoneyTimeSeries maintenanceExpense)
    {
        var rows = new List<TaxBridgeRow>();
        var startYear = result.GrossRent.StartPeriod.Year;
        var endYear = result.GrossRent.EndPeriod.Year;

        for (var year = startYear; year <= endYear; year++)
        {
            rows.Add(new TaxBridgeRow
            {
                Year = year,
                GrossIncome = grossRent.SumForYear(year).Amount,
                Depreciation = result.DepreciationDeduction.SumForYear(year).Amount,
                InterestExpense = result.InterestExpense.SumForYear(year).Amount,
                MaintenanceExpense = maintenanceExpense.SumForYear(year).Amount,
                OperatingExpenses = result.OperatingCosts.SumForYear(year).Amount,
                MaintenanceReserve = 0, // Reserve is not a tax deduction
                TaxableIncome = result.TaxableIncome.SumForYear(year).Amount,
                TaxPayment = result.TaxPayment.SumForYear(year).Amount
            });
        }

        return rows;
    }

    /// <summary>
    /// Erstellt die CapEx-Timeline aus Maßnahmen und expandierten Vorkommen
    /// </summary>
    public static IReadOnlyList<CapExTimelineItem> BuildCapExTimeline(
        CapExConfiguration? capExConfig,
        IReadOnlyList<RecurringCapExExpander.RecurringOccurrence>? recurringOccurrences)
    {
        var items = new List<CapExTimelineItem>();

        if (capExConfig != null)
        {
            foreach (var measure in capExConfig.Measures.Where(m => !m.Id.Contains("_recurring_")))
            {
                items.Add(new CapExTimelineItem
                {
                    Id = measure.Id,
                    Name = measure.Name,
                    Category = measure.Category,
                    Year = measure.PlannedPeriod.Year,
                    Month = measure.PlannedPeriod.Month,
                    Amount = measure.EstimatedCost.Amount,
                    TaxClassification = measure.TaxClassification,
                    DistributionYears = measure.DistributionYears
                });
            }
        }

        if (recurringOccurrences != null)
        {
            foreach (var occ in recurringOccurrences)
            {
                items.Add(new CapExTimelineItem
                {
                    Id = $"{occ.SourceMeasureId}_r",
                    Name = occ.Name,
                    Category = occ.Category,
                    Year = occ.Year,
                    Month = occ.Month,
                    Amount = occ.Amount.Amount,
                    TaxClassification = occ.TaxClassification
                });
            }
        }

        return items.OrderBy(i => i.Year).ThenBy(i => i.Month).ToList();
    }
}
