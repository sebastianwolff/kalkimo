using Kalkimo.Domain.Calculators;
using Kalkimo.Domain.Models;
using Kalkimo.Domain.Tests.TestHelpers;

namespace Kalkimo.Domain.Tests.GoldenTests;

/// <summary>
/// Golden Tests für die steuerliche Bemessungsgrundlage:
/// - Aufteilung der Kaufnebenkosten auf Gebäude/Boden
/// - Aktivierte Maßnahmen in der AfA ab Entstehung, AfA bis Restbuchwert 0
/// - Verlustausgleich (Privat) und Verlustvortrag (GmbH, Mindestbesteuerung)
/// - Einnahmen = vereinnahmte Miete + Umlagen
/// - Mietanpassungen aus Maßnahmen im Cashflow
/// </summary>
public class TaxBaseTests
{
    private readonly TaxCalculator _taxCalculator;
    private readonly CalculationOrchestrator _orchestrator;

    public TaxBaseTests()
    {
        _taxCalculator = new TaxCalculator(new FixedDateProvider(new DateOnly(2025, 1, 1)));
        _orchestrator = new CalculationOrchestrator(_taxCalculator);
    }

    #region Kaufnebenkosten

    [Fact]
    public void Purchase_SplitsCapitalizableCosts_AndExcludesFinancingCosts()
    {
        var purchase = new Purchase
        {
            PurchasePrice = Money.Euro(400_000m),
            LandValue = Money.Euro(100_000m),
            PurchaseDate = new DateOnly(2025, 1, 1),
            AcquisitionCosts =
            [
                new AcquisitionCost { Type = AcquisitionCostType.TransferTax, Amount = Money.Euro(20_000m) },
                new AcquisitionCost { Type = AcquisitionCostType.FinancingCosts, Amount = Money.Euro(2_000m), IsCapitalizable = false }
            ]
        };

        // Gebäudeanteil 75% → 300.000 + 75% × 20.000 = 315.000
        Assert.Equal(315_000m, purchase.DepreciationBase.Amount);
        // §23-Basis: Kaufpreis + aktivierte NK, ohne Finanzierungskosten
        Assert.Equal(420_000m, purchase.AcquisitionCostBasis.Amount);
        Assert.Equal(2_000m, purchase.ImmediatelyDeductibleAcquisitionCosts.Amount);
    }

    #endregion

    #region AfA-Zeitreihe

    [Fact]
    public void DepreciationTimeSeries_ManufacturingCosts_IncreaseBaseFromMonthIncurred()
    {
        var purchase = CreatePurchase(new DateOnly(2025, 1, 1));
        var property = CreateProperty(2000); // 2%
        var measure = TestProjectBuilder.CreateMeasure(cost: 60_000m, year: 2026, month: 7,
            taxClass: TaxClassification.ManufacturingCosts);

        var afa = _taxCalculator.CalculateDepreciationTimeSeries(
            purchase, property, CreateTaxProfile(), new YearMonth(2025, 1), new YearMonth(2027, 12), [measure]);

        // Gebäude 320.000 × 2% / 12 = 533,33
        Assert.Equal(533.33m, afa[new YearMonth(2026, 6)].Amount);
        // ab Juli 2026: 380.000 × 2% / 12 = 633,33
        Assert.Equal(633.33m, afa[new YearMonth(2026, 7)].Amount);
    }

    [Fact]
    public void DepreciationTimeSeries_AcquisitionRelatedCosts_StartWhenIncurred_NotAtPurchase()
    {
        var purchase = CreatePurchase(new DateOnly(2025, 1, 1)); // Schwelle 15% × 320.000 = 48.000
        var property = CreateProperty(2000);
        var measure = TestProjectBuilder.CreateMeasure(cost: 50_000m, year: 2027, month: 1);

        var afa = _taxCalculator.CalculateDepreciationTimeSeries(
            purchase, property, CreateTaxProfile(), new YearMonth(2025, 1), new YearMonth(2027, 12), [measure]);

        Assert.Equal(533.33m, afa[new YearMonth(2025, 1)].Amount);
        Assert.Equal(533.33m, afa[new YearMonth(2026, 12)].Amount);
        // 370.000 × 2% / 12 = 616,67
        Assert.Equal(616.67m, afa[new YearMonth(2027, 1)].Amount);
    }

    [Fact]
    public void DepreciationTimeSeries_StopsWhenBookValueExhausted()
    {
        var purchase = CreatePurchase(new DateOnly(2020, 1, 1));
        var property = CreateProperty(2000);
        var taxProfile = CreateTaxProfile() with { CustomDepreciationRatePercent = 50m }; // 2 Jahre

        var afa = _taxCalculator.CalculateDepreciationTimeSeries(
            purchase, property, taxProfile, new YearMonth(2020, 1), new YearMonth(2023, 12));

        // Exakt die Bemessungsgrundlage; ab 2022 nur noch Rundungsrest der Monatsbeträge
        Assert.Equal(purchase.DepreciationBase.Amount, afa.Sum().Amount);
        Assert.True(afa.SumForYear(2022).Amount < 1m);
        Assert.Equal(0m, afa.SumForYear(2023).Amount);
    }

    [Fact]
    public void DepreciationTimeSeries_PurchaseBeforeHorizon_ContinuesWithReducedBookValue()
    {
        var purchase = CreatePurchase(new DateOnly(2020, 1, 1));
        var property = CreateProperty(2000);
        var taxProfile = CreateTaxProfile() with { CustomDepreciationRatePercent = 50m };

        // Horizont beginnt 2021: nur noch 1 Jahr Restbuchwert
        var afa = _taxCalculator.CalculateDepreciationTimeSeries(
            purchase, property, taxProfile, new YearMonth(2021, 1), new YearMonth(2023, 12));

        Assert.Equal(purchase.DepreciationBase.Amount / 2, afa.Sum().Amount, 0);
    }

    #endregion

    #region Verluste

    [Fact]
    public void TaxTimeSeries_PrivateWithLossOffset_ProducesNegativeTax()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2026);
        var (income, zero, maintenance) = CreateSeries(project, monthlyIncome: 1_000m, maintenance2025: 60_000m);

        var tax = _taxCalculator.CalculateTaxTimeSeries(
            project, null, income, zero, maintenance, zero, project.StartPeriod, project.EndPeriod);

        var taxable2025 = 12_000m - tax.Depreciation.SumForYear(2025).Amount - 60_000m;
        var expected = taxable2025 * project.TaxProfile.EffectiveTaxRatePercent / 100;

        Assert.True(tax.TaxPayment.SumForYear(2025).Amount < 0, "Verlust sollte Steuerersparnis erzeugen");
        Assert.Equal(expected, tax.TaxPayment.SumForYear(2025).Amount, 0);
        Assert.Equal(0m, tax.LossCarryforwardByYear[2025].Amount);
    }

    [Fact]
    public void TaxTimeSeries_Corporation_CarriesLossForward()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2026);
        project = project with
        {
            TaxProfile = project.TaxProfile with { OwnershipType = OwnershipType.Corporation }
        };
        var (income, zero, maintenance) = CreateSeries(project, monthlyIncome: 3_000m, maintenance2025: 40_000m);

        var tax = _taxCalculator.CalculateTaxTimeSeries(
            project, null, income, zero, maintenance, zero, project.StartPeriod, project.EndPeriod);

        var afaPerYear = tax.Depreciation.SumForYear(2025).Amount;
        var loss2025 = 36_000m - afaPerYear - 40_000m;
        var profit2026 = 36_000m - tax.Depreciation.SumForYear(2026).Amount;

        Assert.True(loss2025 < 0);
        Assert.Equal(0m, tax.TaxPayment.SumForYear(2025).Amount);
        Assert.Equal(-loss2025, tax.LossCarryforwardByYear[2025].Amount, 2);

        // 2026: Verlustvortrag mindert das zu versteuernde Einkommen
        var expectedTaxable2026 = profit2026 + loss2025;
        var expectedTax = _taxCalculator.CalculateAnnualTax(Money.Euro(expectedTaxable2026), project.TaxProfile).Amount;
        Assert.Equal(expectedTax, tax.TaxPayment.SumForYear(2026).Amount, 0);
        Assert.Equal(0m, tax.LossCarryforwardByYear[2026].Amount);
    }

    [Fact]
    public void TaxTimeSeries_PrivateWithoutOffset_UsesInitialCarryforward()
    {
        var project = TestProjectBuilder.CreateStandardProject(startYear: 2025, endYear: 2025);
        project = project with
        {
            TaxProfile = project.TaxProfile with
            {
                LossOffsetEnabled = false,
                LossCarryforward = Money.Euro(5_000m)
            }
        };
        var (income, zero, maintenance) = CreateSeries(project, monthlyIncome: 3_000m, maintenance2025: 0m);

        var tax = _taxCalculator.CalculateTaxTimeSeries(
            project, null, income, zero, maintenance, zero, project.StartPeriod, project.EndPeriod);

        var taxable = 36_000m - tax.Depreciation.SumForYear(2025).Amount - 5_000m;
        var expected = taxable * project.TaxProfile.EffectiveTaxRatePercent / 100;
        Assert.Equal(expected, tax.TaxPayment.SumForYear(2025).Amount, 0);
    }

    #endregion

    #region Orchestrator

    [Fact]
    public void Calculate_TaxableIncome_UsesCollectedRentPlusServiceCharges()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        var row = result.TaxBridge.Single(r => r.Year == 2026);
        var expectedIncome = result.EffectiveRent.SumForYear(2026) + result.ServiceChargeIncome.SumForYear(2026);
        Assert.Equal(expectedIncome.Amount, row.GrossIncome);

        // Steuerbrücke geht auf
        var bridged = row.GrossIncome - row.Depreciation - row.InterestExpense - row.MaintenanceExpense - row.OperatingExpenses;
        Assert.Equal(bridged, row.TaxableIncome, 0);
    }

    [Fact]
    public void Calculate_YearlyTaxSummary_HasNoDoubleDeduction()
    {
        var project = TestProjectBuilder.CreateStandardProject();

        var result = _orchestrator.Calculate(project);

        var year = result.TaxSummary.YearlyTax[2026];
        var bridged = year.GrossIncome - year.Depreciation - year.InterestExpense - year.MaintenanceExpense - year.OtherDeductions;
        Assert.Equal(bridged.Amount, year.TaxableIncome.Amount, 0);
    }

    [Fact]
    public void Calculate_RentIncreaseFromMeasure_ReachesEffectiveRent()
    {
        var baseProject = TestProjectBuilder.CreateStandardProject();
        var measure = TestProjectBuilder.CreateMeasure(cost: 10_000m, year: 2027, month: 1) with
        {
            Impact = new MeasureImpact { RentIncreaseMonthly = Money.Euro(100m) }
        };
        var withMeasure = baseProject.WithCapEx(measure);

        var baseResult = _orchestrator.Calculate(baseProject);
        var result = _orchestrator.Calculate(withMeasure);

        var period = new YearMonth(2027, 6);
        var delta = result.EffectiveRent[period] - baseResult.EffectiveRent[period];
        // 100 € Erhöhung bei 3% Leerstand → 97 € vereinnahmt
        Assert.Equal(97m, delta.Amount, 1);
    }

    [Fact]
    public void Calculate_ManufacturingCosts_IncreaseDepreciation()
    {
        var baseProject = TestProjectBuilder.CreateStandardProject();
        var withHk = baseProject.WithCapEx(
            TestProjectBuilder.CreateMeasure(cost: 60_000m, year: 2027, month: 1, taxClass: TaxClassification.ManufacturingCosts));

        var baseResult = _orchestrator.Calculate(baseProject);
        var result = _orchestrator.Calculate(withHk);

        // Baujahr 2000 → 2%: 60.000 × 2% = 1.200 € mehr AfA pro Jahr
        var delta = result.DepreciationDeduction.SumForYear(2028) - baseResult.DepreciationDeduction.SumForYear(2028);
        Assert.Equal(1_200m, delta.Amount, 0);
        Assert.Equal(0m, (result.DepreciationDeduction.SumForYear(2026) - baseResult.DepreciationDeduction.SumForYear(2026)).Amount);
    }

    #endregion

    #region §23 EStG

    [Fact]
    public void CapitalGainsTax_CapitalizedImprovements_IncreaseBasis()
    {
        var purchase = CreatePurchase(new DateOnly(2025, 1, 1));

        var result = _taxCalculator.CalculateCapitalGainsTax(
            purchase,
            salePrice: Money.Euro(500_000m),
            saleCosts: Money.Euro(0m),
            accumulatedDepreciation: Money.Euro(10_000m),
            saleDate: new DateOnly(2028, 1, 1),
            CreateTaxProfile(),
            new CapitalGainsTaxParameters(),
            capitalizedImprovements: Money.Euro(60_000m));

        // 400.000 + 60.000 − 10.000 = 450.000
        Assert.Equal(450_000m, result.AdjustedBasis.Amount);
        Assert.Equal(50_000m, result.Gain.Amount);
    }

    #endregion

    #region Helpers

    private static (MoneyTimeSeries income, MoneyTimeSeries zero, MoneyTimeSeries maintenance) CreateSeries(
        Project project, decimal monthlyIncome, decimal maintenance2025)
    {
        var income = new MoneyTimeSeries(project.StartPeriod, project.EndPeriod);
        var zero = new MoneyTimeSeries(project.StartPeriod, project.EndPeriod);
        var maintenance = new MoneyTimeSeries(project.StartPeriod, project.EndPeriod);

        foreach (var period in income.Periods)
        {
            income[period] = Money.Euro(monthlyIncome);
            if (period.Year == 2025)
                maintenance[period] = Money.Euro(maintenance2025 / 12);
        }

        return (income, zero, maintenance);
    }

    private static Purchase CreatePurchase(DateOnly purchaseDate) => new()
    {
        PurchasePrice = Money.Euro(400_000m),
        LandValue = Money.Euro(80_000m),
        PurchaseDate = purchaseDate,
        AcquisitionCosts = []
    };

    private static Property CreateProperty(int constructionYear) => new()
    {
        Id = "test-property",
        Type = PropertyType.MultiFamilyHome,
        ConstructionYear = constructionYear,
        OverallCondition = Condition.Good,
        TotalArea = 500m,
        LivingArea = 400m
    };

    private static TaxProfile CreateTaxProfile() => new()
    {
        OwnershipType = OwnershipType.PrivateIndividual,
        MarginalTaxRatePercent = 42m
    };

    #endregion
}
