using Kalkimo.Domain.Models;

namespace Kalkimo.Domain.Calculators;

/// <summary>
/// Berechnet steuerliche Auswirkungen nach deutschem Steuerrecht
/// - AfA (Gebäudeabschreibung)
/// - 15%-Regel (anschaffungsnahe HK)
/// - §82b EStDV (Verteilung Erhaltungsaufwand)
/// - §23 EStG (private Veräußerungsgeschäfte)
/// </summary>
public class TaxCalculator
{
    private readonly IDateProvider _dateProvider;

    public TaxCalculator(IDateProvider dateProvider)
    {
        _dateProvider = dateProvider;
    }

    /// <summary>
    /// Berechnet die jährliche AfA
    /// </summary>
    public Money CalculateAnnualDepreciation(
        Purchase purchase,
        Property property,
        TaxProfile taxProfile)
    {
        // Custom rate aus Gutachten überschreibt Standard
        var rate = taxProfile.CustomDepreciationRatePercent
            ?? DepreciationRates.GetAnnualRate(property.ConstructionYear);

        // AfA-Bemessungsgrundlage = Gebäudewert + aktivierbare Nebenkosten
        var depreciationBase = purchase.DepreciationBase;

        return (depreciationBase * rate / 100).Round();
    }

    /// <summary>
    /// Berechnet die monatliche AfA (für Zeitreihen)
    /// </summary>
    public Money CalculateMonthlyDepreciation(
        Purchase purchase,
        Property property,
        TaxProfile taxProfile)
    {
        return CalculateAnnualDepreciation(purchase, property, taxProfile) / 12;
    }

    /// <summary>
    /// Erstellt die AfA-Zeitreihe mit korrektem Start (Monat der Anschaffung)
    /// </summary>
    public MoneyTimeSeries CalculateDepreciationTimeSeries(
        Purchase purchase,
        Property property,
        TaxProfile taxProfile,
        YearMonth startPeriod,
        YearMonth endPeriod)
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod);
        var purchasePeriod = YearMonth.FromDate(purchase.PurchaseDate);
        var monthlyDepreciation = CalculateMonthlyDepreciation(purchase, property, taxProfile);

        // AfA-Dauer in Jahren
        var usefulLifeYears = taxProfile.CustomDepreciationRatePercent.HasValue
            ? (int)(100 / taxProfile.CustomDepreciationRatePercent.Value)
            : DepreciationRates.GetUsefulLifeYears(property.ConstructionYear);

        var depreciationEndPeriod = purchasePeriod.AddYears(usefulLifeYears);

        foreach (var period in result.Periods)
        {
            if (period >= purchasePeriod && period < depreciationEndPeriod)
            {
                result[period] = monthlyDepreciation;
            }
            else
            {
                result[period] = Money.Zero();
            }
        }

        return result;
    }

    /// <summary>
    /// Prüft und berechnet die 15%-Regel (anschaffungsnahe Herstellungskosten)
    /// </summary>
    public AcquisitionRelatedCostsResult CheckAcquisitionRelatedCosts(
        Purchase purchase,
        IEnumerable<CapExMeasure> measures)
    {
        var purchaseDate = purchase.PurchaseDate;
        var threeYearsLater = purchaseDate.AddYears(AcquisitionRelatedCostsRule.PeriodYears);

        // Summiere alle Erhaltungsaufwendungen innerhalb von 3 Jahren
        var costsWithinPeriod = measures
            .Where(m => m.TaxClassification == TaxClassification.MaintenanceExpense ||
                       m.TaxClassification == TaxClassification.MaintenanceExpenseDistributed)
            .Where(m => m.PlannedPeriod.ToFirstDayOfMonth() >= purchaseDate &&
                       m.PlannedPeriod.ToFirstDayOfMonth() < threeYearsLater)
            .Aggregate(Money.Zero(), (sum, m) => sum + m.EstimatedCost);

        var threshold = AcquisitionRelatedCostsRule.CalculateThreshold(purchase.BuildingValue);
        var isTriggered = costsWithinPeriod > threshold;

        return new AcquisitionRelatedCostsResult
        {
            IsTriggered = isTriggered,
            Threshold = threshold,
            ActualCosts = costsWithinPeriod,
            ExcessAmount = isTriggered ? costsWithinPeriod - threshold : Money.Zero()
        };
    }

    /// <summary>
    /// Klassifiziert eine Maßnahme steuerlich (unter Berücksichtigung der 15%-Regel)
    /// </summary>
    public TaxClassification ClassifyMeasure(
        CapExMeasure measure,
        AcquisitionRelatedCostsResult acquisitionRelatedResult,
        DateOnly purchaseDate)
    {
        // Wenn explizit als Herstellungskosten markiert
        if (measure.TaxClassification == TaxClassification.ManufacturingCosts)
            return TaxClassification.ManufacturingCosts;

        // Wenn 15%-Regel greift und Maßnahme in 3-Jahres-Frist
        var threeYearsLater = purchaseDate.AddYears(AcquisitionRelatedCostsRule.PeriodYears);
        if (acquisitionRelatedResult.IsTriggered &&
            measure.PlannedPeriod.ToFirstDayOfMonth() >= purchaseDate &&
            measure.PlannedPeriod.ToFirstDayOfMonth() < threeYearsLater &&
            (measure.TaxClassification == TaxClassification.MaintenanceExpense ||
             measure.TaxClassification == TaxClassification.MaintenanceExpenseDistributed))
        {
            return TaxClassification.AcquisitionRelatedCosts;
        }

        return measure.TaxClassification;
    }

    /// <summary>
    /// Berechnet die Verteilung nach §82b EStDV
    /// </summary>
    public IReadOnlyList<AnnualDeduction> CalculateMaintenanceDistribution(
        CapExMeasure measure,
        int distributionYears)
    {
        if (!MaintenanceDistributionRule.IsValidDistribution(distributionYears))
            throw new ArgumentException($"Distribution must be {MaintenanceDistributionRule.MinYears}-{MaintenanceDistributionRule.MaxYears} years");

        var annualAmount = measure.EstimatedCost / distributionYears;
        var startYear = measure.PlannedPeriod.Year;

        return Enumerable.Range(0, distributionYears)
            .Select(i => new AnnualDeduction
            {
                Year = startYear + i,
                Amount = annualAmount.Round()
            })
            .ToList();
    }

    /// <summary>
    /// Berechnet die Steuer auf einen Verkaufsgewinn (§23 EStG)
    /// </summary>
    public CapitalGainsTaxResult CalculateCapitalGainsTax(
        Purchase purchase,
        Money salePrice,
        Money saleCosts,
        Money accumulatedDepreciation,
        DateOnly saleDate,
        TaxProfile taxProfile,
        CapitalGainsTaxParameters parameters)
    {
        // Prüfe 10-Jahres-Frist
        var holdingPeriodYears = (saleDate.Year - purchase.PurchaseDate.Year) +
            ((saleDate.Month >= purchase.PurchaseDate.Month) ? 0 : -1);

        var isTaxExempt = holdingPeriodYears >= parameters.HoldingPeriodYears ||
                         parameters.OwnerOccupiedExemption;

        if (isTaxExempt)
        {
            return new CapitalGainsTaxResult
            {
                IsTaxExempt = true,
                HoldingPeriodYears = holdingPeriodYears,
                Reason = holdingPeriodYears >= parameters.HoldingPeriodYears
                    ? "Spekulationsfrist abgelaufen"
                    : "Eigennutzung"
            };
        }

        // Fortgeschriebene Anschaffungskosten = Kaufpreis + NK - AfA
        var adjustedBasis = purchase.TotalInvestment - accumulatedDepreciation;

        // Veräußerungsgewinn = Verkaufspreis - Verkaufskosten - fortgeschriebene AK
        var gain = salePrice - saleCosts - adjustedBasis;

        // Freigrenze prüfen (1.000€ seit 2024)
        if (gain <= parameters.ExemptionThreshold)
        {
            return new CapitalGainsTaxResult
            {
                IsTaxExempt = true,
                HoldingPeriodYears = holdingPeriodYears,
                Gain = gain,
                Reason = "Unter Freigrenze"
            };
        }

        // Steuer = Gewinn * persönlicher Steuersatz
        // WICHTIG: Bei Überschreiten der Freigrenze wird der GESAMTE Gewinn besteuert!
        var tax = (gain * taxProfile.EffectiveTaxRatePercent / 100).Round();

        return new CapitalGainsTaxResult
        {
            IsTaxExempt = false,
            HoldingPeriodYears = holdingPeriodYears,
            SalePrice = salePrice,
            SaleCosts = saleCosts,
            AdjustedBasis = adjustedBasis,
            Gain = gain,
            TaxAmount = tax
        };
    }

    /// <summary>
    /// Berechnet die jährliche Steuerlast (vereinfachte Version)
    /// </summary>
    public Money CalculateAnnualTax(
        Money taxableIncome,
        TaxProfile taxProfile)
    {
        if (taxableIncome <= Money.Zero())
        {
            // Negative Einkünfte → keine Steuerzahlung (Verlust)
            // Verlustverrechnung wird separat behandelt
            return Money.Zero();
        }

        return (taxableIncome * taxProfile.EffectiveTaxRatePercent / 100).Round();
    }

    /// <summary>
    /// Erstellt die vollständige Steuer-Zeitreihe
    /// </summary>
    public TaxTimeSeries CalculateTaxTimeSeries(
        Project project,
        MoneyTimeSeries grossRent,
        MoneyTimeSeries interestExpense,
        MoneyTimeSeries maintenanceExpense,
        MoneyTimeSeries otherDeductions,
        YearMonth startPeriod,
        YearMonth endPeriod)
    {
        var depreciation = CalculateDepreciationTimeSeries(
            project.Purchase,
            project.Property,
            project.TaxProfile,
            startPeriod,
            endPeriod);

        var taxableIncome = new MoneyTimeSeries(startPeriod, endPeriod);
        var taxPayment = new MoneyTimeSeries(startPeriod, endPeriod);

        // Sammle jährliche Werte für Steuerberechnung
        var yearlyData = new Dictionary<int, YearlyTaxData>();

        foreach (var period in taxableIncome.Periods)
        {
            var year = period.Year;
            if (!yearlyData.ContainsKey(year))
            {
                yearlyData[year] = new YearlyTaxData();
            }

            yearlyData[year].GrossIncome += grossRent[period];
            yearlyData[year].Depreciation += depreciation[period];
            yearlyData[year].Interest += interestExpense[period];
            yearlyData[year].Maintenance += maintenanceExpense[period];
            yearlyData[year].OtherDeductions += otherDeductions[period];
        }

        // Berechne Steuer pro Jahr und verteile auf Monate
        foreach (var (year, data) in yearlyData)
        {
            var yearlyTaxableIncome = data.GrossIncome
                - data.Depreciation
                - data.Interest
                - data.Maintenance
                - data.OtherDeductions;

            var yearlyTax = CalculateAnnualTax(yearlyTaxableIncome, project.TaxProfile);

            // Verteile auf 12 Monate (oder weniger im ersten/letzten Jahr)
            var monthsInYear = Enumerable.Range(1, 12)
                .Select(m => new YearMonth(year, m))
                .Where(p => p >= startPeriod && p <= endPeriod)
                .ToList();

            var monthlyTaxableIncome = yearlyTaxableIncome / monthsInYear.Count;
            var monthlyTax = yearlyTax / monthsInYear.Count;

            foreach (var period in monthsInYear)
            {
                taxableIncome[period] = monthlyTaxableIncome.Round();
                taxPayment[period] = monthlyTax.Round();
            }
        }

        return new TaxTimeSeries
        {
            Depreciation = depreciation,
            TaxableIncome = taxableIncome,
            TaxPayment = taxPayment
        };
    }

    private class YearlyTaxData
    {
        public Money GrossIncome { get; set; } = Money.Zero();
        public Money Depreciation { get; set; } = Money.Zero();
        public Money Interest { get; set; } = Money.Zero();
        public Money Maintenance { get; set; } = Money.Zero();
        public Money OtherDeductions { get; set; } = Money.Zero();
    }
}

/// <summary>
/// Ergebnis der 15%-Regel Prüfung
/// </summary>
public record AcquisitionRelatedCostsResult
{
    public bool IsTriggered { get; init; }
    public Money Threshold { get; init; }
    public Money ActualCosts { get; init; }
    public Money ExcessAmount { get; init; }
}

/// <summary>
/// Jährlicher Abzugsbetrag
/// </summary>
public record AnnualDeduction
{
    public int Year { get; init; }
    public Money Amount { get; init; }
}

/// <summary>
/// Ergebnis der Verkaufssteuer-Berechnung
/// </summary>
public record CapitalGainsTaxResult
{
    public bool IsTaxExempt { get; init; }
    public int HoldingPeriodYears { get; init; }
    public string? Reason { get; init; }
    public Money SalePrice { get; init; }
    public Money SaleCosts { get; init; }
    public Money AdjustedBasis { get; init; }
    public Money Gain { get; init; }
    public Money TaxAmount { get; init; }
}

/// <summary>
/// Steuer-Zeitreihen
/// </summary>
public class TaxTimeSeries
{
    public required MoneyTimeSeries Depreciation { get; init; }
    public required MoneyTimeSeries TaxableIncome { get; init; }
    public required MoneyTimeSeries TaxPayment { get; init; }
}

/// <summary>
/// Interface für Datumsabstraktion (ermöglicht deterministische Tests)
/// </summary>
public interface IDateProvider
{
    DateOnly Today { get; }
    DateTime Now { get; }
}

/// <summary>
/// Standard-Implementierung
/// </summary>
public class SystemDateProvider : IDateProvider
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
    public DateTime Now => DateTime.Now;
}

/// <summary>
/// Test-Implementierung mit fixem Datum
/// </summary>
public class FixedDateProvider : IDateProvider
{
    private readonly DateOnly _today;
    private readonly DateTime _now;

    public FixedDateProvider(DateOnly today)
    {
        _today = today;
        _now = today.ToDateTime(TimeOnly.MinValue);
    }

    public DateOnly Today => _today;
    public DateTime Now => _now;
}
