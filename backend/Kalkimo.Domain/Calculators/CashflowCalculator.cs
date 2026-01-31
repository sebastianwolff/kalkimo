using Kalkimo.Domain.Models;

namespace Kalkimo.Domain.Calculators;

/// <summary>
/// Berechnet Cashflow- und Liquiditätszeitreihen
/// </summary>
public static class CashflowCalculator
{
    /// <summary>
    /// Berechnet die Mieteinnahmen-Zeitreihe
    /// </summary>
    public static MoneyTimeSeries CalculateGrossRent(
        RentConfiguration rentConfig,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);

        foreach (var period in result.Periods)
        {
            var periodDate = period.ToFirstDayOfMonth();
            var totalRent = Money.Zero(currency);

            foreach (var tenancy in rentConfig.Tenancies)
            {
                // Prüfe ob Mietverhältnis aktiv
                if (periodDate < tenancy.StartDate)
                    continue;
                if (tenancy.EndDate.HasValue && periodDate > tenancy.EndDate.Value)
                    continue;

                var rent = CalculateRentForPeriod(tenancy, period);
                totalRent += rent;
            }

            result[period] = totalRent;
        }

        return result;
    }

    /// <summary>
    /// Berechnet die Miete für ein Mietverhältnis in einem bestimmten Monat
    /// </summary>
    private static Money CalculateRentForPeriod(Tenancy tenancy, YearMonth period, decimal inflationIndexFactor = 1.02m)
    {
        var baseRent = tenancy.NetRent;
        var periodDate = period.ToFirstDayOfMonth();
        var yearsElapsed = (periodDate.Year - tenancy.StartDate.Year) +
            (periodDate.Month >= tenancy.StartDate.Month ? 0 : -1);

        return tenancy.DevelopmentModel switch
        {
            RentDevelopmentModel.Fixed => baseRent,

            RentDevelopmentModel.Annual when tenancy.AnnualIncreasePercent.HasValue =>
                baseRent * (decimal)Math.Pow(1 + (double)tenancy.AnnualIncreasePercent.Value / 100, yearsElapsed),

            RentDevelopmentModel.Graduated when tenancy.RentSteps.Any() =>
                tenancy.RentSteps
                    .Where(s => s.EffectiveDate <= periodDate)
                    .OrderByDescending(s => s.EffectiveDate)
                    .Select(s => s.NewNetRent)
                    .FirstOrDefault(baseRent),

            RentDevelopmentModel.Indexed => CalculateIndexedRent(baseRent, yearsElapsed, tenancy.IndexThresholdPercent ?? 5m, inflationIndexFactor),

            _ => baseRent
        };
    }

    /// <summary>
    /// Berechnet die Indexmiete basierend auf Inflation und Schwellenwert
    /// Index rent adjusts when cumulative inflation exceeds the threshold percentage
    /// </summary>
    private static Money CalculateIndexedRent(Money baseRent, int yearsElapsed, decimal thresholdPercent, decimal annualInflationFactor)
    {
        if (yearsElapsed <= 0)
            return baseRent;

        // Calculate cumulative inflation
        var cumulativeInflation = (decimal)Math.Pow((double)annualInflationFactor, yearsElapsed) - 1m;
        var cumulativePercent = cumulativeInflation * 100m;

        // Only adjust when threshold is exceeded
        // Each time threshold is crossed, rent adjusts proportionally
        var adjustments = (int)(cumulativePercent / thresholdPercent);
        if (adjustments <= 0)
            return baseRent;

        // Apply the threshold-based adjustments
        var adjustedRent = baseRent * (1m + (adjustments * thresholdPercent / 100m));
        return adjustedRent;
    }

    /// <summary>
    /// Berechnet die effektive Miete nach Mietausfall/Leerstand
    /// </summary>
    public static MoneyTimeSeries CalculateEffectiveRent(
        MoneyTimeSeries grossRent,
        RentConfiguration rentConfig,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);

        // Erstelle Lookup für Vacancy Events: period -> affected UnitIds (null means all units)
        var vacancyLookup = new Dictionary<YearMonth, HashSet<string?>>();
        foreach (var evt in rentConfig.VacancyEvents)
        {
            var current = evt.StartPeriod;
            for (int i = 0; i < evt.DurationMonths; i++)
            {
                if (!vacancyLookup.TryGetValue(current, out var unitIds))
                {
                    unitIds = new HashSet<string?>();
                    vacancyLookup[current] = unitIds;
                }
                unitIds.Add(evt.UnitId); // null means all units affected
                current = current.AddMonths(1);
            }
        }

        foreach (var period in result.Periods)
        {
            var periodDate = period.ToFirstDayOfMonth();
            var totalEffectiveRent = Money.Zero(currency);

            // Check if this period has vacancy events
            vacancyLookup.TryGetValue(period, out var vacantUnits);

            foreach (var tenancy in rentConfig.Tenancies)
            {
                // Check if tenancy is active
                if (periodDate < tenancy.StartDate)
                    continue;
                if (tenancy.EndDate.HasValue && periodDate > tenancy.EndDate.Value)
                    continue;

                var tenancyRent = CalculateRentForPeriod(tenancy, period);

                // Check if this tenancy's unit is affected by vacancy
                if (vacantUnits != null)
                {
                    // null in vacantUnits means ALL units affected
                    if (vacantUnits.Contains(null) || vacantUnits.Contains(tenancy.UnitId))
                    {
                        // This unit is vacant - no rent
                        continue;
                    }
                }

                // Apply general vacancy rate to non-event-affected rent
                var effectiveRent = tenancyRent * (1 - rentConfig.VacancyRatePercent / 100);
                totalEffectiveRent += effectiveRent;
            }

            result[period] = totalEffectiveRent.Round();
        }

        return result;
    }

    /// <summary>
    /// Berechnet die laufenden Kosten-Zeitreihe
    /// </summary>
    public static MoneyTimeSeries CalculateOperatingCosts(
        CostConfiguration costConfig,
        decimal livingAreaSqm,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);

        foreach (var period in result.Periods)
        {
            var totalCosts = Money.Zero(currency);
            var periodDate = period.ToFirstDayOfMonth();
            var yearsElapsed = period.Year - startPeriod.Year;

            foreach (var item in costConfig.Items)
            {
                // Prüfe Zeitraum
                if (item.StartDate.HasValue && periodDate < item.StartDate.Value)
                    continue;
                if (item.EndDate.HasValue && periodDate > item.EndDate.Value)
                    continue;

                // Basisbetrag
                var baseAmount = item.AmountPerSqmPerYear.HasValue
                    ? new Money(item.AmountPerSqmPerYear.Value * livingAreaSqm / 12, currency)
                    : item.MonthlyAmount;

                // Dynamisierung
                var inflatedAmount = baseAmount *
                    (decimal)Math.Pow(1 + (double)item.AnnualInflationPercent / 100, yearsElapsed);

                totalCosts += inflatedAmount;
            }

            result[period] = totalCosts.Round();
        }

        return result;
    }

    /// <summary>
    /// Berechnet das Net Operating Income (NOI)
    /// </summary>
    public static MoneyTimeSeries CalculateNoi(
        MoneyTimeSeries effectiveRent,
        MoneyTimeSeries serviceChargeIncome,
        MoneyTimeSeries operatingCosts,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);

        foreach (var period in result.Periods)
        {
            // NOI = Effective Rent + Service Charge Income - Operating Costs
            result[period] = effectiveRent[period] + serviceChargeIncome[period] - operatingCosts[period];
        }

        return result;
    }

    /// <summary>
    /// Berechnet den Cashflow vor Steuern
    /// </summary>
    public static MoneyTimeSeries CalculateCashflowBeforeTax(
        MoneyTimeSeries noi,
        MoneyTimeSeries debtService,
        MoneyTimeSeries capExPayments,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);

        foreach (var period in result.Periods)
        {
            result[period] = noi[period] - debtService[period] - capExPayments[period];
        }

        return result;
    }

    /// <summary>
    /// Berechnet den Cashflow nach Steuern
    /// </summary>
    public static MoneyTimeSeries CalculateCashflowAfterTax(
        MoneyTimeSeries cashflowBeforeTax,
        MoneyTimeSeries taxPayment,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);

        foreach (var period in result.Periods)
        {
            result[period] = cashflowBeforeTax[period] - taxPayment[period];
        }

        return result;
    }

    /// <summary>
    /// Berechnet den Rücklagenkontostand
    /// </summary>
    public static MoneyTimeSeries CalculateReserveBalance(
        ReserveAccountConfig? config,
        MoneyTimeSeries grossRent,
        decimal livingAreaSqm,
        MoneyTimeSeries capExPayments,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);

        if (config == null)
        {
            return result;
        }

        var balance = config.InitialBalance;
        var yearsElapsed = 0;

        foreach (var period in result.Periods)
        {
            if (period.Month == 1)
                yearsElapsed = period.Year - startPeriod.Year;

            // Monatliche Zuführung berechnen
            var contribution = CalculateMonthlyContribution(
                config, grossRent[period], livingAreaSqm, yearsElapsed, currency);

            // Entnahme für CapEx
            var withdrawal = capExPayments[period];

            balance = balance + contribution - withdrawal;

            // Kontostand kann nicht unter 0 fallen (Warnung wird separat generiert)
            if (balance.Amount < 0)
            {
                // In der Praxis würde dies zu einer Liquiditätslücke führen
                // Die wird aber separat ausgewiesen
            }

            result[period] = balance;
        }

        return result;
    }

    private static Money CalculateMonthlyContribution(
        ReserveAccountConfig config,
        Money monthlyRent,
        decimal livingAreaSqm,
        int yearsElapsed,
        string currency = "EUR")
    {
        Money baseContribution;

        if (config.MonthlyContribution.HasValue)
        {
            baseContribution = config.MonthlyContribution.Value;
        }
        else if (config.ContributionPerSqmPerYear.HasValue)
        {
            baseContribution = new Money(config.ContributionPerSqmPerYear.Value * livingAreaSqm / 12, currency);
        }
        else if (config.ContributionPercentOfRent.HasValue)
        {
            baseContribution = monthlyRent * config.ContributionPercentOfRent.Value / 100;
        }
        else
        {
            return Money.Zero(currency);
        }

        // Dynamisierung
        return baseContribution *
            (decimal)Math.Pow(1 + (double)config.AnnualInflationPercent / 100, yearsElapsed);
    }

    /// <summary>
    /// Berechnet die Nebenkostenvorauszahlungen (Service Charge Income) von Mietern
    /// </summary>
    public static MoneyTimeSeries CalculateServiceChargeIncome(
        RentConfiguration rentConfig,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);

        // Build vacancy event lookup (same logic as CalculateEffectiveRent)
        var vacancyLookup = new Dictionary<YearMonth, HashSet<string?>>();
        foreach (var evt in rentConfig.VacancyEvents)
        {
            var current = evt.StartPeriod;
            for (int i = 0; i < evt.DurationMonths; i++)
            {
                if (!vacancyLookup.TryGetValue(current, out var unitIds))
                {
                    unitIds = new HashSet<string?>();
                    vacancyLookup[current] = unitIds;
                }
                unitIds.Add(evt.UnitId);
                current = current.AddMonths(1);
            }
        }

        foreach (var period in result.Periods)
        {
            var periodDate = period.ToFirstDayOfMonth();
            var totalServiceCharge = Money.Zero(currency);

            // Check if this period has vacancy events
            vacancyLookup.TryGetValue(period, out var vacantUnits);

            foreach (var tenancy in rentConfig.Tenancies)
            {
                // Check if tenancy is active
                if (periodDate < tenancy.StartDate)
                    continue;
                if (tenancy.EndDate.HasValue && periodDate > tenancy.EndDate.Value)
                    continue;

                // Check if this tenancy's unit is affected by vacancy
                if (vacantUnits != null)
                {
                    // null in vacantUnits means ALL units affected
                    if (vacantUnits.Contains(null) || vacantUnits.Contains(tenancy.UnitId))
                        continue; // Vacant unit - no service charge income
                }

                // Apply general vacancy rate (matching CalculateEffectiveRent logic)
                var serviceCharge = tenancy.ServiceChargeAdvance * (1 - rentConfig.VacancyRatePercent / 100);
                totalServiceCharge += serviceCharge;
            }

            result[period] = totalServiceCharge;
        }

        return result;
    }

    /// <summary>
    /// Berechnet die umlagefähigen Kosten (Transferable Costs)
    /// </summary>
    public static MoneyTimeSeries CalculateTransferableCosts(
        CostConfiguration costConfig,
        decimal livingAreaSqm,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);

        foreach (var period in result.Periods)
        {
            var totalCosts = Money.Zero(currency);
            var periodDate = period.ToFirstDayOfMonth();
            var yearsElapsed = period.Year - startPeriod.Year;

            foreach (var item in costConfig.Items)
            {
                // Only transferable costs
                if (item.Classification != CostClassification.Transferable)
                    continue;

                // Check time range
                if (item.StartDate.HasValue && periodDate < item.StartDate.Value)
                    continue;
                if (item.EndDate.HasValue && periodDate > item.EndDate.Value)
                    continue;

                // Base amount
                var baseAmount = item.AmountPerSqmPerYear.HasValue
                    ? new Money(item.AmountPerSqmPerYear.Value * livingAreaSqm / 12, currency)
                    : item.MonthlyAmount;

                // Inflation adjustment
                var inflatedAmount = baseAmount *
                    (decimal)Math.Pow(1 + (double)item.AnnualInflationPercent / 100, yearsElapsed);

                totalCosts += inflatedAmount;
            }

            result[period] = totalCosts.Round();
        }

        return result;
    }

    /// <summary>
    /// Berechnet die nicht-umlagefähigen Kosten (Non-Transferable Operating Costs)
    /// </summary>
    public static MoneyTimeSeries CalculateNonTransferableCosts(
        CostConfiguration costConfig,
        decimal livingAreaSqm,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);

        foreach (var period in result.Periods)
        {
            var totalCosts = Money.Zero(currency);
            var periodDate = period.ToFirstDayOfMonth();
            var yearsElapsed = period.Year - startPeriod.Year;

            foreach (var item in costConfig.Items)
            {
                // Only non-transferable costs
                if (item.Classification == CostClassification.Transferable)
                    continue;

                // Check time range
                if (item.StartDate.HasValue && periodDate < item.StartDate.Value)
                    continue;
                if (item.EndDate.HasValue && periodDate > item.EndDate.Value)
                    continue;

                // Base amount
                var baseAmount = item.AmountPerSqmPerYear.HasValue
                    ? new Money(item.AmountPerSqmPerYear.Value * livingAreaSqm / 12, currency)
                    : item.MonthlyAmount;

                // Inflation adjustment
                var inflatedAmount = baseAmount *
                    (decimal)Math.Pow(1 + (double)item.AnnualInflationPercent / 100, yearsElapsed);

                totalCosts += inflatedAmount;
            }

            result[period] = totalCosts.Round();
        }

        return result;
    }

    /// <summary>
    /// Berechnet CapEx-Zahlungen basierend auf den Maßnahmen
    /// Only executed measures result in actual payments
    /// </summary>
    public static MoneyTimeSeries CalculateCapExPayments(
        CapExConfiguration? capExConfig,
        YearMonth startPeriod,
        YearMonth endPeriod)
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod);

        if (capExConfig == null)
            return result;

        foreach (var measure in capExConfig.Measures)
        {
            // Only executed measures generate payments
            // Non-executed measures (whether necessary or not) don't result in cashflow
            if (!measure.IsExecuted)
            {
                continue;
            }

            // Zahlungsplan verwenden falls vorhanden
            if (measure.PaymentSchedule.Any())
            {
                foreach (var payment in measure.PaymentSchedule)
                {
                    if (payment.Period >= startPeriod && payment.Period <= endPeriod)
                    {
                        result[payment.Period] += payment.Amount;
                    }
                }
            }
            else
            {
                // Einmalzahlung im geplanten Monat
                if (measure.PlannedPeriod >= startPeriod && measure.PlannedPeriod <= endPeriod)
                {
                    result[measure.PlannedPeriod] += measure.CostWithBuffer;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Berechnet Mietanpassungen und Betriebskostenersparnisse aus Maßnahmen-Impacts.
    /// Für jede ausgeführte Maßnahme mit Impact werden ab PlannedPeriod + DelayMonths
    /// die monatlichen Einsparungen/Mieterhöhungen als Zeitreihe zurückgegeben.
    /// </summary>
    public static (MoneyTimeSeries rentAdjustment, MoneyTimeSeries costSavings) CalculateMeasureImpacts(
        CapExConfiguration? capExConfig,
        MoneyTimeSeries grossRent,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var rentAdj = new MoneyTimeSeries(startPeriod, endPeriod, currency);
        var costSav = new MoneyTimeSeries(startPeriod, endPeriod, currency);

        if (capExConfig == null)
            return (rentAdj, costSav);

        foreach (var measure in capExConfig.Measures)
        {
            if (!measure.IsExecuted || measure.Impact == null)
                continue;

            var impact = measure.Impact;
            var effectiveStart = measure.PlannedPeriod.AddMonths(impact.DelayMonths);

            foreach (var period in rentAdj.Periods)
            {
                if (period < effectiveStart)
                    continue;

                // Betriebskostenersparnis
                if (impact.CostSavingsMonthly.HasValue)
                {
                    costSav[period] += impact.CostSavingsMonthly.Value;
                }

                // Mieterhöhung absolut
                if (impact.RentIncreaseMonthly.HasValue)
                {
                    rentAdj[period] += impact.RentIncreaseMonthly.Value;
                }

                // Mieterhöhung relativ (auf Basis-Bruttomiete des Monats)
                if (impact.RentIncreasePercent.HasValue && impact.RentIncreasePercent.Value != 0)
                {
                    var baseRent = grossRent[period];
                    rentAdj[period] += baseRent * (impact.RentIncreasePercent.Value / 100);
                }
            }
        }

        return (rentAdj, costSav);
    }

    /// <summary>
    /// Berechnet den Objektwert über die Zeit
    /// </summary>
    public static MoneyTimeSeries CalculatePropertyValue(
        Money initialValue,
        ValuationConfiguration valuation,
        CapExConfiguration? capExConfig,
        YearMonth startPeriod,
        YearMonth endPeriod)
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod);
        var currentValue = initialValue;

        foreach (var period in result.Periods)
        {
            // Jährliche Marktwertentwicklung (monatlich verteilt)
            var monthlyGrowth = valuation.MarketGrowthRatePercent / 100 / 12;
            currentValue *= (1 + monthlyGrowth);

            // Wertwirkung von Maßnahmen
            if (capExConfig != null)
            {
                foreach (var measure in capExConfig.Measures.Where(m => m.PlannedPeriod == period))
                {
                    if (measure.IsExecuted && measure.IsValueEnhancing)
                    {
                        if (measure.ValueImpact.HasValue)
                            currentValue += measure.ValueImpact.Value;
                        else if (measure.ValueImpactPercent.HasValue)
                            currentValue *= (1 + measure.ValueImpactPercent.Value / 100);
                    }
                }
            }

            result[period] = currentValue.Round();
        }

        // Wertabschläge für unterlassene Maßnahmen (am Ende berechnen)
        if (capExConfig != null)
        {
            var deferredMeasures = capExConfig.Measures
                .Where(m => m.IsNecessary && !m.IsExecuted && m.PlannedPeriod <= endPeriod)
                .ToList();

            if (deferredMeasures.Any())
            {
                var totalDiscount = 0m;

                foreach (var measure in deferredMeasures)
                {
                    var overdueYears = Math.Max(0, endPeriod.Year - measure.PlannedPeriod.Year);
                    var discount = overdueYears *
                        valuation.DeferredMaintenanceImpact.DiscountPerOverdueYearPercent *
                        valuation.DeferredMaintenanceImpact.GetPriorityFactor(measure.Priority);

                    totalDiscount += discount;
                }

                totalDiscount = Math.Min(totalDiscount, valuation.DeferredMaintenanceImpact.MaxTotalDiscountPercent);

                // Abschlag auf Endwert anwenden
                result[endPeriod] = result[endPeriod] * (1 - totalDiscount / 100);
            }
        }

        return result;
    }
}
