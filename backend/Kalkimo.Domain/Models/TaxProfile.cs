namespace Kalkimo.Domain.Models;

/// <summary>
/// Steuerprofil für die Berechnung
/// </summary>
public record TaxProfile
{
    /// <summary>Haltungsform</summary>
    public required OwnershipType OwnershipType { get; init; }

    /// <summary>Persönlicher Grenzsteuersatz (%)</summary>
    public required decimal MarginalTaxRatePercent { get; init; }

    /// <summary>Solidaritätszuschlag (%)</summary>
    public decimal SolidaritySurchargePercent { get; init; } = 5.5m;

    /// <summary>Kirchensteuer (%)</summary>
    public decimal? ChurchTaxPercent { get; init; }

    /// <summary>AfA-Satz überschreiben (für kürzere Nutzungsdauer per Gutachten)</summary>
    public decimal? CustomDepreciationRatePercent { get; init; }

    /// <summary>Verlustverrechnung aktiviert?</summary>
    public bool LossOffsetEnabled { get; init; } = true;

    /// <summary>Verlustvortrag aus Vorjahren</summary>
    public Money LossCarryforward { get; init; } = Money.Zero();

    /// <summary>Gewerbesteuer-Hebesatz in % (nur bei Corporation, default 400%)</summary>
    public decimal? TradeTaxMultiplier { get; init; }

    /// <summary>Zusammenveranlagung (für Soli-Freigrenzen)</summary>
    public bool JointAssessment { get; init; } = false;

    /// <summary>Effektiver Steuersatz inkl. Soli/Kirchensteuer</summary>
    public decimal EffectiveTaxRatePercent
    {
        get
        {
            var soli = MarginalTaxRatePercent * SolidaritySurchargePercent / 100;
            var church = ChurchTaxPercent.HasValue
                ? MarginalTaxRatePercent * ChurchTaxPercent.Value / 100
                : 0;
            return MarginalTaxRatePercent + soli + church;
        }
    }
}

/// <summary>
/// AfA-Berechnung nach deutschem Steuerrecht
/// </summary>
public static class DepreciationRates
{
    /// <summary>
    /// Ermittelt den AfA-Satz basierend auf dem Fertigstellungsjahr
    /// </summary>
    public static decimal GetAnnualRate(int constructionYear)
    {
        // Nach 31.12.2022 fertiggestellt: 3% p.a.
        if (constructionYear >= 2023)
            return 3.0m;

        // Vor 01.01.1925 fertiggestellt: 2.5% p.a.
        if (constructionYear < 1925)
            return 2.5m;

        // Zwischen 01.01.1925 und 31.12.2022: 2% p.a.
        return 2.0m;
    }

    /// <summary>
    /// Ermittelt die Nutzungsdauer in Jahren
    /// </summary>
    public static int GetUsefulLifeYears(int constructionYear) =>
        (int)(100 / GetAnnualRate(constructionYear));
}

/// <summary>
/// Parameter für §23 EStG (private Veräußerungsgeschäfte)
/// </summary>
public record CapitalGainsTaxParameters
{
    /// <summary>Spekulationsfrist in Jahren</summary>
    public int HoldingPeriodYears { get; init; } = 10;

    /// <summary>Freigrenze (seit 2024: 1.000€)</summary>
    public Money ExemptionThreshold { get; init; } = Money.Euro(1000);

    /// <summary>Eigennutzung im Jahr des Verkaufs + 2 Vorjahren (steuerbefreit)?</summary>
    public bool OwnerOccupiedExemption { get; init; }
}

/// <summary>
/// 15%-Regel für anschaffungsnahe Herstellungskosten
/// </summary>
public static class AcquisitionRelatedCostsRule
{
    /// <summary>Zeitraum nach Anschaffung (3 Jahre)</summary>
    public const int PeriodYears = 3;

    /// <summary>Schwelle (15% des Gebäudewerts)</summary>
    public const decimal ThresholdPercent = 15m;

    /// <summary>
    /// Prüft ob die 15%-Regel greift
    /// </summary>
    public static bool IsTriggered(
        Money buildingValue,
        Money renovationCostsWithinPeriod)
    {
        var threshold = buildingValue * (ThresholdPercent / 100);
        return renovationCostsWithinPeriod > threshold;
    }

    /// <summary>
    /// Berechnet die Schwelle
    /// </summary>
    public static Money CalculateThreshold(Money buildingValue) =>
        buildingValue * (ThresholdPercent / 100);
}

/// <summary>
/// §82b EStDV - Verteilung größerer Erhaltungsaufwendungen
/// </summary>
public static class MaintenanceDistributionRule
{
    /// <summary>Minimale Verteilungsjahre</summary>
    public const int MinYears = 2;

    /// <summary>Maximale Verteilungsjahre</summary>
    public const int MaxYears = 5;

    /// <summary>
    /// Validiert die Verteilungsjahre
    /// </summary>
    public static bool IsValidDistribution(int years) =>
        years >= MinYears && years <= MaxYears;

    /// <summary>
    /// Berechnet den jährlichen Abzugsbetrag bei Verteilung
    /// </summary>
    public static Money CalculateAnnualDeduction(Money totalAmount, int years)
    {
        if (!IsValidDistribution(years))
            throw new ArgumentException($"Distribution must be {MinYears}-{MaxYears} years");

        return totalAmount / years;
    }
}
