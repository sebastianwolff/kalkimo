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
    /// Berechnet die effektive AfA-Bemessungsgrundlage am Ende der Investitionsphase:
    /// Gebäude-AK + Herstellungskosten + anschaffungsnahe HK (wenn 15%-Regel greift)
    /// </summary>
    /// <param name="executedMeasures">Nur durchgeführte Maßnahmen (inkl. expandierter wiederkehrender)</param>
    public Money CalculateEffectiveDepreciationBase(
        Purchase purchase,
        IEnumerable<CapExMeasure>? executedMeasures)
    {
        return GetCapitalizedMeasureCosts(purchase, executedMeasures)
            .Aggregate(purchase.DepreciationBase, (sum, c) => sum + c.Amount);
    }

    /// <summary>
    /// Ermittelt alle zu aktivierenden Maßnahmenkosten (Herstellungskosten und
    /// durch die 15%-Regel umqualifizierte Erhaltungsaufwendungen) mit Zeitpunkt.
    /// </summary>
    public IReadOnlyList<CapitalizedCost> GetCapitalizedMeasureCosts(
        Purchase purchase,
        IEnumerable<CapExMeasure>? executedMeasures)
    {
        if (executedMeasures == null)
            return [];

        var measures = executedMeasures.ToList();
        var acquisitionRelatedResult = CheckAcquisitionRelatedCosts(purchase, measures);

        return measures
            .Select(m => (Measure: m, Classification: ClassifyMeasure(m, acquisitionRelatedResult, purchase.PurchaseDate)))
            .Where(x => x.Classification == TaxClassification.ManufacturingCosts ||
                        x.Classification == TaxClassification.AcquisitionRelatedCosts)
            .Select(x => new CapitalizedCost
            {
                MeasureId = x.Measure.Id,
                Period = x.Measure.PlannedPeriod,
                Amount = x.Measure.CostWithBuffer,
                Classification = x.Classification
            })
            .ToList();
    }

    /// <summary>
    /// Ermittelt den AfA-Satz (Gutachten-Satz überschreibt den gesetzlichen Satz)
    /// </summary>
    public static decimal GetDepreciationRate(Property property, TaxProfile taxProfile) =>
        taxProfile.CustomDepreciationRatePercent
            ?? DepreciationRates.GetAnnualRate(property.ConstructionYear);

    /// <summary>
    /// Berechnet die jährliche AfA (volles Jahr, auf die effektive Bemessungsgrundlage)
    /// </summary>
    public Money CalculateAnnualDepreciation(
        Purchase purchase,
        Property property,
        TaxProfile taxProfile,
        IEnumerable<CapExMeasure>? executedMeasures = null)
    {
        var rate = GetDepreciationRate(property, taxProfile);
        var depreciationBase = CalculateEffectiveDepreciationBase(purchase, executedMeasures);

        return (depreciationBase * rate / 100).Round();
    }

    /// <summary>
    /// Erstellt die AfA-Zeitreihe (§7 Abs. 4 EStG, zeitanteilig ab Monat der Anschaffung/Herstellung).
    /// Nachträgliche und anschaffungsnahe HK erhöhen die Bemessungsgrundlage ab dem Monat,
    /// in dem sie anfallen; der AfA-Satz bleibt unverändert und es wird abgeschrieben,
    /// bis der Restbuchwert aufgebraucht ist (R 7.4 Abs. 9 EStR).
    /// </summary>
    public MoneyTimeSeries CalculateDepreciationTimeSeries(
        Purchase purchase,
        Property property,
        TaxProfile taxProfile,
        YearMonth startPeriod,
        YearMonth endPeriod,
        IEnumerable<CapExMeasure>? executedMeasures = null)
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod);
        var purchasePeriod = YearMonth.FromDate(purchase.PurchaseDate);
        var rate = GetDepreciationRate(property, taxProfile);

        // Zugänge zur Bemessungsgrundlage: Gebäude-AK im Anschaffungsmonat + aktivierte Maßnahmen
        var additions = new Dictionary<YearMonth, Money> { [purchasePeriod] = purchase.DepreciationBase };
        foreach (var cost in GetCapitalizedMeasureCosts(purchase, executedMeasures))
        {
            var period = cost.Period < purchasePeriod ? purchasePeriod : cost.Period;
            additions[period] = additions.TryGetValue(period, out var existing) ? existing + cost.Amount : cost.Amount;
        }

        var depreciationBase = Money.Zero();
        var bookValue = Money.Zero();
        var firstPeriod = purchasePeriod < startPeriod ? purchasePeriod : startPeriod;

        for (var period = firstPeriod; period <= endPeriod; period = period.AddMonths(1))
        {
            if (additions.TryGetValue(period, out var addition))
            {
                depreciationBase += addition;
                bookValue += addition;
            }

            var monthly = Money.Zero();
            if (period >= purchasePeriod && bookValue.Amount > 0)
            {
                monthly = (depreciationBase * rate / 100 / 12).Round();
                if (monthly > bookValue)
                    monthly = bookValue;
                bookValue -= monthly;
            }

            if (period >= startPeriod)
                result[period] = monthly;
        }

        return result;
    }

    /// <summary>
    /// Prüft und berechnet die 15%-Regel (anschaffungsnahe Herstellungskosten, §6 Abs. 1 Nr. 1a EStG).
    /// Schwelle: 15% der Gebäude-Anschaffungskosten (inkl. anteiliger Nebenkosten).
    /// Der Aufrufer übergibt nur tatsächlich durchgeführte Maßnahmen.
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
            .Aggregate(Money.Zero(), (sum, m) => sum + m.CostWithBuffer);

        var threshold = AcquisitionRelatedCostsRule.CalculateThreshold(purchase.DepreciationBase);
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

        var annualAmount = measure.CostWithBuffer / distributionYears;
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
    /// Bei Kapitalgesellschaften gilt §23 EStG NICHT - Veräußerungsgewinne sind laufende gewerbliche Einkünfte
    /// </summary>
    public CapitalGainsTaxResult CalculateCapitalGainsTax(
        Purchase purchase,
        Money salePrice,
        Money saleCosts,
        Money accumulatedDepreciation,
        DateOnly saleDate,
        TaxProfile taxProfile,
        CapitalGainsTaxParameters parameters,
        Money? capitalizedImprovements = null)
    {
        // Anschaffungskosten (Kaufpreis + aktivierungspflichtige NK) + aktivierte Maßnahmen
        // (Herstellungskosten, anschaffungsnahe HK); sofort abziehbare NK gehören nicht dazu
        var costBasis = purchase.AcquisitionCostBasis + (capitalizedImprovements ?? Money.Zero());

        // Bei Kapitalgesellschaft: KEINE Haltefrist, volle Besteuerung als gewerbliche Einkünfte
        if (taxProfile.OwnershipType == OwnershipType.Corporation)
        {
            var corpAdjustedBasis = costBasis - accumulatedDepreciation;
            var corpGain = salePrice - saleCosts - corpAdjustedBasis;

            // Veräußerungsgewinn ist laufender Gewinn, besteuert mit KSt + GewSt
            var corpTax = corpGain > Money.Zero() ? CalculateCorporateTax(corpGain, taxProfile) : Money.Zero();

            return new CapitalGainsTaxResult
            {
                IsTaxExempt = false,
                HoldingPeriodYears = 0, // Irrelevant bei GmbH
                SalePrice = salePrice,
                SaleCosts = saleCosts,
                AdjustedBasis = corpAdjustedBasis,
                Gain = corpGain,
                TaxAmount = corpTax,
                Reason = "Gewerbliche Einkünfte (§8 KStG)"
            };
        }

        // Exakte Tagesberechnung für §23 EStG (Haltedauer)
        // Die 10-Jahres-Frist endet am Tag NACH dem 10. Jahrestag des Kaufs
        var holdingDays = saleDate.DayNumber - purchase.PurchaseDate.DayNumber;
        // Berücksichtigt Schaltjahre (365.25 Tage pro Jahr)
        var holdingPeriodYears = (int)(holdingDays / 365.25);

        // Fortgeschriebene Anschaffungskosten = AK + aktivierte Maßnahmen - AfA (§23 Abs. 3 S. 4 EStG)
        var adjustedBasis = costBasis - accumulatedDepreciation;

        // Veräußerungsgewinn = Verkaufspreis - Verkaufskosten - fortgeschriebene AK
        var gain = salePrice - saleCosts - adjustedBasis;

        // Präzise Prüfung: Verkauf muss NACH dem Jahrestag erfolgen
        var tenYearDeadline = purchase.PurchaseDate.AddYears(parameters.HoldingPeriodYears);
        var holdingPeriodExpired = saleDate > tenYearDeadline;

        string? exemptReason = null;
        if (holdingPeriodExpired)
            exemptReason = "Spekulationsfrist abgelaufen";
        else if (parameters.OwnerOccupiedExemption)
            exemptReason = "Eigennutzung";
        // Freigrenze prüfen (1.000€ seit 2024); Verluste lösen keine Steuer aus
        else if (gain <= parameters.ExemptionThreshold)
            exemptReason = "Unter Freigrenze";

        // Steuer = Gewinn * persönlicher Steuersatz
        // WICHTIG: Bei Überschreiten der Freigrenze wird der GESAMTE Gewinn besteuert!
        var tax = exemptReason == null
            ? (gain * taxProfile.EffectiveTaxRatePercent / 100).Round()
            : Money.Zero(salePrice.Currency);

        return new CapitalGainsTaxResult
        {
            IsTaxExempt = exemptReason != null,
            HoldingPeriodYears = holdingPeriodYears,
            Reason = exemptReason,
            SalePrice = salePrice,
            SaleCosts = saleCosts,
            AdjustedBasis = adjustedBasis,
            Gain = gain,
            TaxAmount = tax
        };
    }

    /// <summary>
    /// Berechnet die jährliche Steuerlast
    /// Unterscheidet zwischen Privatperson, Personengesellschaft und Kapitalgesellschaft
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

        return taxProfile.OwnershipType switch
        {
            OwnershipType.Corporation => CalculateCorporateTax(taxableIncome, taxProfile),
            OwnershipType.Partnership => CalculatePartnershipTax(taxableIncome, taxProfile),
            _ => (taxableIncome * taxProfile.EffectiveTaxRatePercent / 100).Round()
        };
    }

    /// <summary>
    /// Berechnet die Körperschaftsteuer für Kapitalgesellschaften
    /// KSt 15% + Soli 5,5% + Gewerbesteuer
    /// </summary>
    private Money CalculateCorporateTax(Money taxableIncome, TaxProfile taxProfile)
    {
        const decimal CORP_TAX_RATE = 15m;
        const decimal SOLI_ON_CORP = 5.5m;

        var corpTax = taxableIncome * CORP_TAX_RATE / 100;
        var soliOnCorp = corpTax * SOLI_ON_CORP / 100;

        // Gewerbesteuer: Messbetrag * Hebesatz
        // Messbetrag = 3,5% vom Gewerbeertrag
        // Hebesatz ist kommunal unterschiedlich (min 200%, oft 400-500%)
        var tradeTaxRate = taxProfile.TradeTaxMultiplier ?? 400m; // Default 400%
        var tradeTaxBase = taxableIncome * 3.5m / 100;
        var tradeTax = tradeTaxBase * tradeTaxRate / 100;

        return (corpTax + soliOnCorp + tradeTax).Round();
    }

    /// <summary>
    /// Berechnet die Steuer für Personengesellschaften
    /// Transparenzprinzip: Besteuerung auf Gesellschafterebene
    /// </summary>
    private Money CalculatePartnershipTax(Money taxableIncome, TaxProfile taxProfile)
    {
        // Bei Personengesellschaften: Besteuerung nach persönlichem Steuersatz
        // (Transparenzprinzip - Einkünfte werden den Gesellschaftern zugerechnet)
        return (taxableIncome * taxProfile.EffectiveTaxRatePercent / 100).Round();
    }

    /// <summary>
    /// Erstellt die vollständige Steuer-Zeitreihe
    /// </summary>
    /// <param name="rentalIncome">Einnahmen: vereinnahmte Kaltmiete + Umlagen (Nebenkostenvorauszahlungen)</param>
    /// <param name="executedMeasures">Durchgeführte Maßnahmen (für aktivierte HK in der AfA)</param>
    public TaxTimeSeries CalculateTaxTimeSeries(
        Project project,
        IEnumerable<CapExMeasure>? executedMeasures,
        MoneyTimeSeries rentalIncome,
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
            endPeriod,
            executedMeasures);

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

            yearlyData[year].GrossIncome += rentalIncome[period];
            yearlyData[year].Depreciation += depreciation[period];
            yearlyData[year].Interest += interestExpense[period];
            yearlyData[year].Maintenance += maintenanceExpense[period];
            yearlyData[year].OtherDeductions += otherDeductions[period];
        }

        var taxProfile = project.TaxProfile;
        var lossCarryforward = taxProfile.LossCarryforward;
        var lossCarryforwardByYear = new Dictionary<int, Money>();

        // Berechne Steuer pro Jahr und verteile auf Monate
        foreach (var (year, data) in yearlyData.OrderBy(kv => kv.Key))
        {
            var yearlyTaxableIncome = data.GrossIncome
                - data.Depreciation
                - data.Interest
                - data.Maintenance
                - data.OtherDeductions;

            Money yearlyTax;
            if (yearlyTaxableIncome.Amount < 0)
            {
                if (UsesImmediateLossOffset(taxProfile))
                {
                    // Verlustausgleich mit anderen Einkünften: Steuerersparnis zum Grenzsteuersatz
                    yearlyTax = (yearlyTaxableIncome * taxProfile.EffectiveTaxRatePercent / 100).Round();
                }
                else
                {
                    // Kein Ausgleich möglich → Verlustvortrag (§10d Abs. 2 EStG / §8 Abs. 1 KStG)
                    lossCarryforward += -yearlyTaxableIncome;
                    yearlyTax = Money.Zero();
                }
            }
            else
            {
                var usableLoss = CalculateUsableLossCarryforward(
                    yearlyTaxableIncome, lossCarryforward, year, taxProfile.JointAssessment);
                lossCarryforward -= usableLoss;
                yearlyTax = CalculateAnnualTax(yearlyTaxableIncome - usableLoss, taxProfile);
            }

            lossCarryforwardByYear[year] = lossCarryforward;

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
            TaxPayment = taxPayment,
            LossCarryforwardByYear = lossCarryforwardByYear
        };
    }

    /// <summary>
    /// Verluste werden sofort mit anderen Einkünften verrechnet (Privatperson/Personengesellschaft
    /// mit ausreichenden anderen Einkünften). Kapitalgesellschaften tragen Verluste vor.
    /// </summary>
    private static bool UsesImmediateLossOffset(TaxProfile taxProfile) =>
        taxProfile.LossOffsetEnabled && taxProfile.OwnershipType != OwnershipType.Corporation;

    /// <summary>
    /// Nutzbarer Verlustvortrag unter Mindestbesteuerung (§10d Abs. 2 EStG, §8 Abs. 1 KStG):
    /// bis 1 Mio. € (2 Mio. € bei Zusammenveranlagung) unbeschränkt, darüber hinaus
    /// 70% (VZ 2024–2027, Wachstumschancengesetz) bzw. 60% des übersteigenden Einkommens.
    /// </summary>
    private static Money CalculateUsableLossCarryforward(
        Money taxableIncome,
        Money lossCarryforward,
        int year,
        bool jointAssessment)
    {
        if (lossCarryforward.Amount <= 0 || taxableIncome.Amount <= 0)
            return Money.Zero();

        var baseAmount = jointAssessment ? 2_000_000m : 1_000_000m;
        var excessRate = year is >= 2024 and <= 2027 ? 0.70m : 0.60m;
        var income = taxableIncome.Amount;
        var deductionCap = Math.Min(income, baseAmount) + Math.Max(0, income - baseAmount) * excessRate;

        return new Money(Math.Min(lossCarryforward.Amount, deductionCap), taxableIncome.Currency);
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

    /// <summary>Verbleibender Verlustvortrag am Jahresende</summary>
    public IReadOnlyDictionary<int, Money> LossCarryforwardByYear { get; init; } = new Dictionary<int, Money>();
}

/// <summary>
/// Aktivierte Maßnahmenkosten (erhöhen die AfA-Bemessungsgrundlage)
/// </summary>
public record CapitalizedCost
{
    public required string MeasureId { get; init; }
    public required YearMonth Period { get; init; }
    public required Money Amount { get; init; }
    public required TaxClassification Classification { get; init; }
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
