using Kalkimo.Domain.Models;

namespace Kalkimo.Domain.Calculators;

/// <summary>
/// Berechnet Rendite- und Risikokennzahlen
/// </summary>
public static class MetricsCalculator
{
    /// <summary>
    /// Berechnet den Internen Zinsfuß (IRR) aus einer Cashflow-Zeitreihe
    /// Newton-Raphson Verfahren
    /// </summary>
    public static decimal CalculateIrr(
        Money initialInvestment,
        MoneyTimeSeries cashflows,
        Money? terminalValue = null)
    {
        var cfList = new List<decimal> { -initialInvestment.Amount };

        foreach (var (period, value) in cashflows)
        {
            cfList.Add(value.Amount);
        }

        // Terminalwert (z.B. Verkaufserlös) zum letzten Cashflow addieren
        if (terminalValue.HasValue)
        {
            cfList[^1] += terminalValue.Value.Amount;
        }

        return CalculateIrrNewtonRaphson(cfList.ToArray());
    }

    private static decimal CalculateIrrNewtonRaphson(decimal[] cashflows, int maxIterations = 100, decimal tolerance = 0.0001m)
    {
        if (cashflows.Length < 2)
            return 0;

        // Startschätzung
        decimal rate = 0.01m; // Start with 1% monthly (12% annually)

        try
        {
            for (int i = 0; i < maxIterations; i++)
            {
                var (npv, derivative) = CalculateNpvAndDerivative(cashflows, rate);

                if (Math.Abs(derivative) < 1e-10m)
                    break;

                var newRate = rate - npv / derivative;

                if (Math.Abs(newRate - rate) < tolerance)
                    return newRate * 12 * 100; // Monatlich → Jährlich in %

                rate = newRate;

                // Begrenze Rate um Divergenz und Overflow zu vermeiden
                // -0.5 bis 1.0 monthly = -600% bis 1200% annually
                rate = Math.Max(-0.5m, Math.Min(1.0m, rate));
            }
        }
        catch (OverflowException)
        {
            // Return 0 if calculation overflows (e.g., very unusual cashflows)
            return 0;
        }

        return rate * 12 * 100;
    }

    private static (decimal npv, decimal derivative) CalculateNpvAndDerivative(decimal[] cashflows, decimal rate)
    {
        decimal npv = 0;
        decimal derivative = 0;
        var onePlusRate = 1 + rate;

        // Guard against division by zero or very small numbers
        if (Math.Abs(onePlusRate) < 0.001m)
            return (0, 0);

        for (int t = 0; t < cashflows.Length; t++)
        {
            var discountPower = Math.Pow((double)onePlusRate, -t);

            // Avoid overflow with large time periods
            if (double.IsInfinity(discountPower) || double.IsNaN(discountPower))
                return (0, 0);

            var discountFactor = (decimal)discountPower;
            npv += cashflows[t] * discountFactor;

            if (t > 0)
            {
                derivative -= t * cashflows[t] * discountFactor / onePlusRate;
            }
        }

        return (npv, derivative);
    }

    /// <summary>
    /// Berechnet den Kapitalwert (NPV)
    /// </summary>
    public static Money CalculateNpv(
        Money initialInvestment,
        MoneyTimeSeries cashflows,
        decimal annualDiscountRatePercent,
        Money? terminalValue = null,
        string currency = "EUR")
    {
        var monthlyRate = annualDiscountRatePercent / 100 / 12;
        var npv = -initialInvestment.Amount;

        int month = 1;
        foreach (var (period, value) in cashflows)
        {
            var discountFactor = (decimal)Math.Pow(1 + (double)monthlyRate, -month);
            npv += value.Amount * discountFactor;
            month++;
        }

        // Terminalwert diskontieren
        if (terminalValue.HasValue)
        {
            var discountFactor = (decimal)Math.Pow(1 + (double)monthlyRate, -month);
            npv += terminalValue.Value.Amount * discountFactor;
        }

        return new Money(npv, currency).Round();
    }

    /// <summary>
    /// Berechnet die Debt Service Coverage Ratio (DSCR)
    /// DSCR = NOI / Kapitaldienst
    /// </summary>
    public static TimeSeries<decimal> CalculateDscr(
        MoneyTimeSeries noi,
        MoneyTimeSeries debtService,
        YearMonth startPeriod,
        YearMonth endPeriod)
    {
        var result = new TimeSeries<decimal>(startPeriod, endPeriod);

        foreach (var period in result.Periods)
        {
            var ds = debtService[period].Amount;
            if (ds > 0)
            {
                result[period] = Math.Round(noi[period].Amount / ds, 2);
            }
            else
            {
                result[period] = decimal.MaxValue; // Kein Kapitaldienst → unendlich
            }
        }

        return result;
    }

    /// <summary>
    /// Berechnet die Interest Coverage Ratio (ICR)
    /// ICR = NOI / Zinsaufwand
    /// </summary>
    public static TimeSeries<decimal> CalculateIcr(
        MoneyTimeSeries noi,
        MoneyTimeSeries interestExpense,
        YearMonth startPeriod,
        YearMonth endPeriod)
    {
        var result = new TimeSeries<decimal>(startPeriod, endPeriod);

        foreach (var period in result.Periods)
        {
            var interest = interestExpense[period].Amount;
            if (interest > 0)
            {
                result[period] = Math.Round(noi[period].Amount / interest, 2);
            }
            else
            {
                result[period] = decimal.MaxValue;
            }
        }

        return result;
    }

    /// <summary>
    /// Berechnet den Loan-to-Value (LTV)
    /// LTV = Restschuld / Objektwert
    /// </summary>
    public static TimeSeries<decimal> CalculateLtv(
        MoneyTimeSeries outstandingDebt,
        MoneyTimeSeries propertyValue,
        YearMonth startPeriod,
        YearMonth endPeriod)
    {
        var result = new TimeSeries<decimal>(startPeriod, endPeriod);

        foreach (var period in result.Periods)
        {
            var value = propertyValue[period].Amount;
            if (value > 0)
            {
                result[period] = Math.Round(outstandingDebt[period].Amount / value * 100, 1);
            }
            else
            {
                result[period] = 0;
            }
        }

        return result;
    }

    /// <summary>
    /// Berechnet die Cash-on-Cash Rendite
    /// CoC = Jährlicher Cashflow / Eingesetztes Eigenkapital
    /// </summary>
    public static decimal CalculateCashOnCash(
        Money annualCashflow,
        Money equity)
    {
        if (equity.Amount <= 0)
            return 0;

        return Math.Round(annualCashflow.Amount / equity.Amount * 100, 2);
    }

    /// <summary>
    /// Berechnet den Equity Multiple
    /// EM = Gesamtrückflüsse / Eigenkapital
    /// </summary>
    public static decimal CalculateEquityMultiple(
        Money totalDistributions,
        Money terminalValue,
        Money equity)
    {
        if (equity.Amount <= 0)
            return 0;

        return Math.Round((totalDistributions.Amount + terminalValue.Amount) / equity.Amount, 2);
    }

    /// <summary>
    /// Berechnet die Break-Even Miete
    /// Mindestmiete, bei der Cashflow >= 0
    /// </summary>
    public static Money CalculateBreakEvenRent(
        Money monthlyOperatingCosts,
        Money monthlyDebtService)
    {
        return (monthlyOperatingCosts + monthlyDebtService).Round();
    }

    /// <summary>
    /// Berechnet den Sanierungsrisiko-Score (0-100)
    /// </summary>
    public static int CalculateMaintenanceRiskScore(
        Property property,
        CapExConfiguration? capExConfig,
        MoneyTimeSeries reserveBalance,
        YearMonth currentPeriod)
    {
        var score = 0;

        // Bauteilalter-Risiko
        foreach (var component in property.Components)
        {
            var age = currentPeriod.Year - (component.LastRenovationYear ?? property.ConstructionYear);
            var overdueYears = age - component.ExpectedCycleYears;

            if (overdueYears > 0)
            {
                score += component.Condition switch
                {
                    Condition.Poor => Math.Min(30, overdueYears * 5),
                    Condition.Fair => Math.Min(20, overdueYears * 3),
                    _ => Math.Min(10, overdueYears * 2)
                };
            }
        }

        // Rücklagen-Risiko
        if (reserveBalance.HasValue(currentPeriod))
        {
            var balance = reserveBalance[currentPeriod].Amount;
            if (balance < 0)
                score += 20;
            else if (balance < 5000)
                score += 10;
        }

        // Nicht durchgeführte notwendige Maßnahmen
        if (capExConfig != null)
        {
            var overdueMeasures = capExConfig.Measures
                .Count(m => m.IsNecessary && !m.IsExecuted && m.PlannedPeriod <= currentPeriod);

            score += overdueMeasures * 10;
        }

        return Math.Min(100, score);
    }

    /// <summary>
    /// Berechnet den Liquiditätsrisiko-Score (0-100)
    /// </summary>
    public static int CalculateLiquidityRiskScore(
        MoneyTimeSeries cashflowAfterTax,
        TimeSeries<decimal> dscr,
        YearMonth startPeriod,
        YearMonth endPeriod)
    {
        var score = 0;

        // Monate mit negativem Cashflow
        var negativeMonths = cashflowAfterTax.Count(p => p.Value.Amount < 0);
        score += Math.Min(40, negativeMonths * 2);

        // DSCR unter 1.0
        var lowDscrMonths = dscr.Count(p => p.Value < 1.0m);
        score += Math.Min(40, lowDscrMonths * 3);

        // DSCR unter 1.2 (Bankstandard)
        var marginalDscrMonths = dscr.Count(p => p.Value >= 1.0m && p.Value < 1.2m);
        score += Math.Min(20, marginalDscrMonths);

        return Math.Min(100, score);
    }

    /// <summary>
    /// Findet den ersten Monat mit Liquiditätsunterdeckung
    /// </summary>
    public static int? FindMonthsToLiquidityShortfall(
        MoneyTimeSeries cumulativeCashflow,
        YearMonth startPeriod)
    {
        int month = 0;
        foreach (var (period, value) in cumulativeCashflow)
        {
            month++;
            if (value.Amount < 0)
                return month;
        }

        return null;
    }

    /// <summary>
    /// Aggregiert alle Kennzahlen
    /// </summary>
    public static InvestmentMetrics CalculateAllMetrics(
        Money initialInvestment,
        Money equity,
        MoneyTimeSeries noi,
        MoneyTimeSeries cashflowBeforeTax,
        MoneyTimeSeries cashflowAfterTax,
        MoneyTimeSeries debtService,
        MoneyTimeSeries interestExpense,
        MoneyTimeSeries outstandingDebt,
        MoneyTimeSeries propertyValue,
        Property property,
        CapExConfiguration? capExConfig,
        MoneyTimeSeries reserveBalance,
        Money? saleNetProceeds,
        decimal discountRate,
        YearMonth startPeriod,
        YearMonth endPeriod)
    {
        var dscr = CalculateDscr(noi, debtService, startPeriod, endPeriod);
        var icr = CalculateIcr(noi, interestExpense, startPeriod, endPeriod);
        var ltv = CalculateLtv(outstandingDebt, propertyValue, startPeriod, endPeriod);

        var cumulativeCf = cashflowAfterTax.Cumulative();
        var totalCashflow = cashflowAfterTax.Sum();

        return new InvestmentMetrics
        {
            IrrBeforeTaxPercent = CalculateIrr(initialInvestment, cashflowBeforeTax, saleNetProceeds),
            IrrAfterTaxPercent = CalculateIrr(initialInvestment, cashflowAfterTax, saleNetProceeds),
            NpvBeforeTax = CalculateNpv(initialInvestment, cashflowBeforeTax, discountRate, saleNetProceeds),
            NpvAfterTax = CalculateNpv(initialInvestment, cashflowAfterTax, discountRate, saleNetProceeds),
            RoiPercent = equity.Amount > 0
                ? Math.Round(totalCashflow.Amount / equity.Amount * 100 / YearMonth.MonthsBetween(startPeriod, endPeriod) * 12, 2)
                : 0,
            CashOnCashPercent = CalculateCashOnCash(cashflowAfterTax.SumForYear(startPeriod.Year + 1), equity),
            EquityMultiple = CalculateEquityMultiple(totalCashflow, saleNetProceeds ?? Money.Zero(), equity),

            DscrMin = dscr.Where(p => p.Value < decimal.MaxValue).Min(p => p.Value),
            DscrAvg = dscr.Where(p => p.Value < decimal.MaxValue).Average(p => p.Value),
            IcrMin = icr.Where(p => p.Value < decimal.MaxValue).Min(p => p.Value),
            LtvInitialPercent = ltv[startPeriod.AddMonths(1)],
            LtvFinalPercent = ltv[endPeriod],

            BreakEvenRent = CalculateBreakEvenRent(
                noi.SumForYear(startPeriod.Year) / 12 - cashflowBeforeTax.SumForYear(startPeriod.Year) / 12,
                debtService.SumForYear(startPeriod.Year) / 12),

            MaintenanceRiskScore = CalculateMaintenanceRiskScore(property, capExConfig, reserveBalance, endPeriod),
            LiquidityRiskScore = CalculateLiquidityRiskScore(cashflowAfterTax, dscr, startPeriod, endPeriod),
            MonthsToLiquidityShortfall = FindMonthsToLiquidityShortfall(cumulativeCf, startPeriod)
        };
    }
}
