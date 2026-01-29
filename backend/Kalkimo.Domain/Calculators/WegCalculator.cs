using Kalkimo.Domain.Models;

namespace Kalkimo.Domain.Calculators;

/// <summary>
/// Berechnet WEG-spezifische Kosten und Zeitreihen
/// </summary>
public static class WegCalculator
{
    /// <summary>
    /// Berechnet die monatlichen Hausgeld-Zahlungen als Zeitreihe
    /// </summary>
    public static MoneyTimeSeries CalculateHausgeldTimeSeries(
        WegConfiguration wegConfig,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);
        var baseAmount = wegConfig.Hausgeld.MonthlyTotal;
        var annualIncrease = wegConfig.Hausgeld.AnnualIncreasePercent;

        foreach (var period in result.Periods)
        {
            var yearsElapsed = period.Year - startPeriod.Year;
            var inflatedAmount = baseAmount * (decimal)Math.Pow(1 + (double)annualIncrease / 100, yearsElapsed);
            result[period] = inflatedAmount.Round();
        }

        return result;
    }

    /// <summary>
    /// Berechnet die WEG-Rücklagenzuführung als Zeitreihe
    /// </summary>
    public static MoneyTimeSeries CalculateReserveContributionTimeSeries(
        WegConfiguration wegConfig,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);
        var baseAmount = wegConfig.Hausgeld.MonthlyReserveContribution;
        var annualIncrease = wegConfig.Hausgeld.AnnualIncreasePercent;

        foreach (var period in result.Periods)
        {
            var yearsElapsed = period.Year - startPeriod.Year;
            var inflatedAmount = baseAmount * (decimal)Math.Pow(1 + (double)annualIncrease / 100, yearsElapsed);
            result[period] = inflatedAmount.Round();
        }

        return result;
    }

    /// <summary>
    /// Berechnet die umlagefähigen WEG-Kosten als Zeitreihe
    /// </summary>
    public static MoneyTimeSeries CalculateTransferableWegCostsTimeSeries(
        WegConfiguration wegConfig,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);
        var baseAmount = wegConfig.Hausgeld.TransferableAmount;
        var annualIncrease = wegConfig.Hausgeld.AnnualIncreasePercent;

        foreach (var period in result.Periods)
        {
            var yearsElapsed = period.Year - startPeriod.Year;
            var inflatedAmount = baseAmount * (decimal)Math.Pow(1 + (double)annualIncrease / 100, yearsElapsed);
            result[period] = inflatedAmount.Round();
        }

        return result;
    }

    /// <summary>
    /// Berechnet die nicht-umlagefähigen WEG-Kosten als Zeitreihe (Eigentümer-Last)
    /// </summary>
    public static MoneyTimeSeries CalculateNonTransferableWegCostsTimeSeries(
        WegConfiguration wegConfig,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);
        var baseAmount = wegConfig.Hausgeld.NonTransferableAmount;
        var annualIncrease = wegConfig.Hausgeld.AnnualIncreasePercent;

        foreach (var period in result.Periods)
        {
            var yearsElapsed = period.Year - startPeriod.Year;
            var inflatedAmount = baseAmount * (decimal)Math.Pow(1 + (double)annualIncrease / 100, yearsElapsed);
            result[period] = inflatedAmount.Round();
        }

        return result;
    }

    /// <summary>
    /// Berechnet die Sonderumlagen als Zeitreihe
    /// </summary>
    public static MoneyTimeSeries CalculateSonderumlagenTimeSeries(
        WegConfiguration wegConfig,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);

        foreach (var umlage in wegConfig.Sonderumlagen)
        {
            if (umlage.DueDate >= startPeriod && umlage.DueDate <= endPeriod)
            {
                result[umlage.DueDate] += umlage.Amount;
            }
        }

        return result;
    }

    /// <summary>
    /// Berechnet den WEG-Rücklagenstand als Zeitreihe
    /// </summary>
    public static MoneyTimeSeries CalculateWegReserveBalanceTimeSeries(
        WegConfiguration wegConfig,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);
        var balance = wegConfig.CurrentReserveBalance ?? Money.Zero(currency);
        var baseContribution = wegConfig.Hausgeld.MonthlyReserveContribution;
        var annualIncrease = wegConfig.Hausgeld.AnnualIncreasePercent;

        foreach (var period in result.Periods)
        {
            // Add monthly contribution (with inflation)
            var yearsElapsed = period.Year - startPeriod.Year;
            var inflatedContribution = baseContribution * (decimal)Math.Pow(1 + (double)annualIncrease / 100, yearsElapsed);
            balance += inflatedContribution;

            // Subtract any Sonderumlagen (special levies often come from reserves)
            var sonderumlagenInPeriod = wegConfig.Sonderumlagen
                .Where(s => s.DueDate == period)
                .Aggregate(Money.Zero(currency), (sum, s) => sum + s.Amount);

            balance -= sonderumlagenInPeriod;

            result[period] = balance.Round();
        }

        return result;
    }

    /// <summary>
    /// Berechnet den effektiven Kostenanteil basierend auf Verteilungsschlüssel
    /// </summary>
    public static decimal CalculateCostShare(
        WegConfiguration wegConfig,
        DistributionKeyType keyType,
        decimal? customShare = null)
    {
        return keyType switch
        {
            DistributionKeyType.Mea => wegConfig.MeaPerMille / wegConfig.TotalMeaPerMille,
            DistributionKeyType.Custom => customShare ?? wegConfig.SharePercent / 100,
            // For other types, default to MEA share (caller should provide correct data)
            _ => wegConfig.SharePercent / 100
        };
    }

    /// <summary>
    /// Erstellt eine vollständige WEG-Kostenaufstellung
    /// </summary>
    public static WegCostSummary CalculateWegCostSummary(
        WegConfiguration wegConfig,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var hausgeld = CalculateHausgeldTimeSeries(wegConfig, startPeriod, endPeriod, currency);
        var reserveContribution = CalculateReserveContributionTimeSeries(wegConfig, startPeriod, endPeriod, currency);
        var transferable = CalculateTransferableWegCostsTimeSeries(wegConfig, startPeriod, endPeriod, currency);
        var nonTransferable = CalculateNonTransferableWegCostsTimeSeries(wegConfig, startPeriod, endPeriod, currency);
        var sonderumlagen = CalculateSonderumlagenTimeSeries(wegConfig, startPeriod, endPeriod, currency);
        var reserveBalance = CalculateWegReserveBalanceTimeSeries(wegConfig, startPeriod, endPeriod, currency);

        return new WegCostSummary
        {
            TotalHausgeld = hausgeld,
            ReserveContribution = reserveContribution,
            TransferableCosts = transferable,
            NonTransferableCosts = nonTransferable,
            Sonderumlagen = sonderumlagen,
            ReserveBalance = reserveBalance,
            TotalHausgeldSum = hausgeld.Sum(),
            TotalReserveContributionSum = reserveContribution.Sum(),
            TotalTransferableCostsSum = transferable.Sum(),
            TotalNonTransferableCostsSum = nonTransferable.Sum(),
            TotalSonderumlagenSum = sonderumlagen.Sum()
        };
    }
}

/// <summary>
/// Zusammenfassung der WEG-Kosten
/// </summary>
public record WegCostSummary
{
    /// <summary>Hausgeld-Zeitreihe</summary>
    public required MoneyTimeSeries TotalHausgeld { get; init; }

    /// <summary>Rücklagenzuführung-Zeitreihe</summary>
    public required MoneyTimeSeries ReserveContribution { get; init; }

    /// <summary>Umlagefähige Kosten-Zeitreihe</summary>
    public required MoneyTimeSeries TransferableCosts { get; init; }

    /// <summary>Nicht-umlagefähige Kosten-Zeitreihe</summary>
    public required MoneyTimeSeries NonTransferableCosts { get; init; }

    /// <summary>Sonderumlagen-Zeitreihe</summary>
    public required MoneyTimeSeries Sonderumlagen { get; init; }

    /// <summary>Rücklagenstand-Zeitreihe</summary>
    public required MoneyTimeSeries ReserveBalance { get; init; }

    /// <summary>Summe Hausgeld über Gesamtzeitraum</summary>
    public required Money TotalHausgeldSum { get; init; }

    /// <summary>Summe Rücklagenzuführung über Gesamtzeitraum</summary>
    public required Money TotalReserveContributionSum { get; init; }

    /// <summary>Summe umlagefähige Kosten über Gesamtzeitraum</summary>
    public required Money TotalTransferableCostsSum { get; init; }

    /// <summary>Summe nicht-umlagefähige Kosten über Gesamtzeitraum</summary>
    public required Money TotalNonTransferableCostsSum { get; init; }

    /// <summary>Summe Sonderumlagen über Gesamtzeitraum</summary>
    public required Money TotalSonderumlagenSum { get; init; }
}
