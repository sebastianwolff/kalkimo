using Kalkimo.Domain.Models;

namespace Kalkimo.Domain.Calculators;

/// <summary>
/// Berechnet Investor-spezifische Ergebnisse und Ausschüttungen
/// </summary>
public static class InvestorCalculator
{
    /// <summary>
    /// Berechnet die Ergebnisse für alle Investoren
    /// </summary>
    public static IReadOnlyList<InvestorResult> CalculateInvestorResults(
        InvestorConfiguration? investorConfig,
        Financing financing,
        MoneyTimeSeries cashflowBeforeTax,
        Money? saleNetProceeds,
        decimal discountRate,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        if (investorConfig == null || !investorConfig.Investors.Any())
        {
            return [];
        }

        var policy = investorConfig.DistributionPolicy;
        var results = new List<InvestorResult>();

        // Get equity contributions by investor
        var contributionsByInvestor = financing.EquityContributions
            .GroupBy(c => c.InvestorId)
            .ToDictionary(g => g.Key, g => g.Aggregate(Money.Zero(currency), (sum, c) => sum + c.Amount));

        foreach (var investor in investorConfig.Investors)
        {
            var shareRatio = investor.SharePercent / 100m;

            // Calculate equity contribution for this investor
            var equityContribution = contributionsByInvestor.TryGetValue(investor.Id, out var contribution)
                ? contribution
                : Money.Zero(currency);

            // Calculate pro-rata cashflow
            var proRataCashflow = CalculateProRataCashflow(
                cashflowBeforeTax, shareRatio, startPeriod, endPeriod, currency);

            // Calculate distributions according to policy
            var distributions = CalculateDistributions(
                proRataCashflow, policy, startPeriod, endPeriod, currency);

            var totalDistributions = distributions.Sum();

            // Add pro-rata share of sale proceeds to last period
            if (saleNetProceeds.HasValue)
            {
                var proRataSaleProceeds = saleNetProceeds.Value * shareRatio;
                totalDistributions += proRataSaleProceeds;
            }

            // Calculate investor-specific metrics
            var irrBeforeTax = CalculateInvestorIrr(
                equityContribution, proRataCashflow, saleNetProceeds, shareRatio);

            var npvBeforeTax = CalculateInvestorNpv(
                equityContribution, proRataCashflow, saleNetProceeds, shareRatio, discountRate, currency);

            var equityMultiple = equityContribution.Amount > 0
                ? totalDistributions.Amount / equityContribution.Amount
                : 0;

            // Average annual cash-on-cash
            var years = YearMonth.MonthsBetween(startPeriod, endPeriod) / 12m;
            var annualCashflow = years > 0 ? totalDistributions.Amount / years : 0;
            var cashOnCash = equityContribution.Amount > 0
                ? annualCashflow / equityContribution.Amount * 100
                : 0;

            results.Add(new InvestorResult
            {
                InvestorId = investor.Id,
                InvestorName = investor.Name,
                SharePercent = investor.SharePercent,
                EquityContribution = equityContribution,
                ProRataCashflow = proRataCashflow,
                Distributions = distributions,
                TotalDistributions = totalDistributions,
                IrrBeforeTaxPercent = irrBeforeTax,
                IrrAfterTaxPercent = null, // Would require individual tax calculations
                NpvBeforeTax = npvBeforeTax,
                EquityMultiple = equityMultiple,
                CashOnCashPercent = cashOnCash
            });
        }

        return results;
    }

    /// <summary>
    /// Berechnet den pro-rata Cashflow basierend auf Beteiligungsquote
    /// </summary>
    private static MoneyTimeSeries CalculateProRataCashflow(
        MoneyTimeSeries totalCashflow,
        decimal shareRatio,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency)
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);

        foreach (var period in result.Periods)
        {
            result[period] = (totalCashflow[period] * shareRatio).Round();
        }

        return result;
    }

    /// <summary>
    /// Berechnet Ausschüttungen basierend auf der Ausschüttungspolitik
    /// </summary>
    private static MoneyTimeSeries CalculateDistributions(
        MoneyTimeSeries proRataCashflow,
        DistributionPolicy policy,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency)
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);
        var accumulated = Money.Zero(currency);
        var distributionRate = policy.DistributionRatePercent / 100m;

        foreach (var period in result.Periods)
        {
            accumulated += proRataCashflow[period];

            var shouldDistribute = policy.Frequency switch
            {
                DistributionFrequency.Monthly => true,
                DistributionFrequency.Quarterly => period.Month % 3 == 0,
                DistributionFrequency.Annual => period.Month == 12,
                DistributionFrequency.OnDemand => false,
                _ => false
            };

            if (shouldDistribute)
            {
                // Only distribute if above minimum reserve and positive
                var available = accumulated - policy.MinimumReserve;
                if (available.Amount > 0)
                {
                    var distribution = (available * distributionRate).Round();
                    result[period] = distribution;
                    accumulated -= distribution;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Berechnet den IRR für einen einzelnen Investor
    /// </summary>
    private static decimal CalculateInvestorIrr(
        Money equityContribution,
        MoneyTimeSeries cashflows,
        Money? saleProceeds,
        decimal shareRatio)
    {
        // Terminal value is the pro-rata share of sale proceeds
        var terminalValue = saleProceeds.HasValue
            ? saleProceeds.Value * shareRatio
            : (Money?)null;

        return MetricsCalculator.CalculateIrr(equityContribution, cashflows, terminalValue);
    }

    /// <summary>
    /// Berechnet den NPV für einen einzelnen Investor
    /// </summary>
    private static Money CalculateInvestorNpv(
        Money equityContribution,
        MoneyTimeSeries cashflows,
        Money? saleProceeds,
        decimal shareRatio,
        decimal annualDiscountRate,
        string currency)
    {
        var terminalValue = saleProceeds.HasValue
            ? saleProceeds.Value * shareRatio
            : (Money?)null;

        return MetricsCalculator.CalculateNpv(
            equityContribution,
            cashflows,
            annualDiscountRate,
            terminalValue,
            currency);
    }
}
