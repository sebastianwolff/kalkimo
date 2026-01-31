using Kalkimo.Domain.Models;

namespace Kalkimo.Domain.Tests.TestHelpers;

/// <summary>
/// Builder für realistische Testprojekte.
/// Standard: MFH, Baujahr 2000, KP 400.000€, 20% Boden, Annuitätendarlehen 3,5%.
/// </summary>
public static class TestProjectBuilder
{
    public static Project CreateStandardProject(
        int startYear = 2025,
        int endYear = 2035,
        decimal purchasePrice = 400_000m,
        decimal landValuePercent = 20m,
        int constructionYear = 2000,
        Condition condition = Condition.Good)
    {
        var startPeriod = new YearMonth(startYear, 1);
        var endPeriod = new YearMonth(endYear, 12);
        var landValue = purchasePrice * landValuePercent / 100;

        return new Project
        {
            Id = $"test-{Guid.NewGuid():N}",
            Name = "Test-Projekt",
            Currency = "EUR",
            StartPeriod = startPeriod,
            EndPeriod = endPeriod,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Property = new Property
            {
                Id = "property-1",
                Type = PropertyType.MultiFamilyHome,
                ConstructionYear = constructionYear,
                OverallCondition = condition,
                TotalArea = 500m,
                LivingArea = 400m,
                LandArea = 600m,
                UnitCount = 4,
                Components = new[]
                {
                    new ComponentCondition { Category = CapExCategory.Heating, Condition = Condition.Fair, ExpectedCycleYears = 20, LastRenovationYear = 2010 },
                    new ComponentCondition { Category = CapExCategory.Roof, Condition = Condition.Good, ExpectedCycleYears = 45, LastRenovationYear = 2000 },
                    new ComponentCondition { Category = CapExCategory.Windows, Condition = Condition.Fair, ExpectedCycleYears = 30, LastRenovationYear = 2005 },
                    new ComponentCondition { Category = CapExCategory.Facade, Condition = Condition.Good, ExpectedCycleYears = 40, LastRenovationYear = 2000 },
                    new ComponentCondition { Category = CapExCategory.Electrical, Condition = Condition.Fair, ExpectedCycleYears = 40, LastRenovationYear = 2000 },
                    new ComponentCondition { Category = CapExCategory.Plumbing, Condition = Condition.Fair, ExpectedCycleYears = 40, LastRenovationYear = 2000 }
                }
            },
            Purchase = new Purchase
            {
                PurchasePrice = Money.Euro(purchasePrice),
                LandValue = Money.Euro(landValue),
                PurchaseDate = startPeriod.ToFirstDayOfMonth(),
                AcquisitionCosts = new[]
                {
                    new AcquisitionCost { Type = AcquisitionCostType.TransferTax, Amount = Money.Euro(purchasePrice * 0.05m) },
                    new AcquisitionCost { Type = AcquisitionCostType.Notary, Amount = Money.Euro(purchasePrice * 0.015m) },
                    new AcquisitionCost { Type = AcquisitionCostType.BrokerFee, Amount = Money.Euro(purchasePrice * 0.0357m) }
                }
            },
            Financing = new Financing
            {
                EquityContributions = new[]
                {
                    new EquityContribution
                    {
                        InvestorId = "investor-1",
                        Amount = Money.Euro(100_000m),
                        ContributionDate = startPeriod.ToFirstDayOfMonth()
                    }
                },
                Loans = new[]
                {
                    new Loan
                    {
                        Id = "loan-1",
                        Name = "Hauptdarlehen",
                        Type = LoanType.Annuity,
                        Principal = Money.Euro(purchasePrice - 100_000m + purchasePrice * 0.1007m),
                        DisbursementDate = startPeriod.ToFirstDayOfMonth(),
                        InterestRatePercent = 3.5m,
                        InitialRepaymentPercent = 2m,
                        FixedInterestPeriodMonths = 120
                    }
                }
            },
            Rent = new RentConfiguration
            {
                Tenancies = new[]
                {
                    new Tenancy
                    {
                        Id = "tenancy-1",
                        StartDate = startPeriod.ToFirstDayOfMonth(),
                        NetRent = Money.Euro(2_000m),
                        ServiceChargeAdvance = Money.Euro(300m),
                        DevelopmentModel = RentDevelopmentModel.Annual,
                        AnnualIncreasePercent = 2m
                    }
                },
                VacancyRatePercent = 3m
            },
            Costs = new CostConfiguration
            {
                Items = new[]
                {
                    new CostItem
                    {
                        Id = "cost-admin",
                        Name = "Verwaltung",
                        Classification = CostClassification.Administration,
                        MonthlyAmount = Money.Euro(100m),
                        IsTaxDeductible = true,
                        AnnualInflationPercent = 2m
                    },
                    new CostItem
                    {
                        Id = "cost-insurance",
                        Name = "Versicherung",
                        Classification = CostClassification.Insurance,
                        MonthlyAmount = Money.Euro(80m),
                        IsTaxDeductible = true
                    }
                },
                ReserveAccount = new ReserveAccountConfig
                {
                    InitialBalance = Money.Euro(10_000m),
                    ContributionPerSqmPerYear = 10m,
                    AnnualInflationPercent = 2m
                }
            },
            TaxProfile = new TaxProfile
            {
                OwnershipType = OwnershipType.PrivateIndividual,
                MarginalTaxRatePercent = 42m,
                SolidaritySurchargePercent = 5.5m,
                LossOffsetEnabled = true
            },
            Valuation = new ValuationConfiguration
            {
                MarketGrowthRatePercent = 1.5m,
                SaleCostsPercent = 5m,
                DiscountRatePercent = 5m
            }
        };
    }

    /// <summary>
    /// Projekt mit CapEx-Maßnahmen
    /// </summary>
    public static Project WithCapEx(this Project project, params CapExMeasure[] measures)
    {
        return project with
        {
            CapEx = new CapExConfiguration { Measures = measures.ToList() }
        };
    }

    /// <summary>
    /// Projekt mit regionaler Preisinformation für Mean-Reversion
    /// </summary>
    public static Project WithRegionalPrice(this Project project, decimal pricePerSqm)
    {
        return project with
        {
            Property = project.Property with { RegionalPricePerSqm = pricePerSqm }
        };
    }

    /// <summary>
    /// Standard-CapEx-Maßnahme
    /// </summary>
    public static CapExMeasure CreateMeasure(
        string id = "measure-1",
        CapExCategory category = CapExCategory.Heating,
        decimal cost = 25_000m,
        int year = 2027,
        int month = 6,
        TaxClassification taxClass = TaxClassification.MaintenanceExpense,
        bool isRecurring = false,
        RecurringMeasureConfig? recurringConfig = null)
    {
        return new CapExMeasure
        {
            Id = id,
            Name = $"Test-{category}",
            Category = category,
            PlannedPeriod = new YearMonth(year, month),
            EstimatedCost = Money.Euro(cost),
            TaxClassification = taxClass,
            IsExecuted = true,
            IsNecessary = false,
            Priority = MeasurePriority.Medium,
            IsRecurring = isRecurring,
            RecurringConfig = recurringConfig
        };
    }

    /// <summary>
    /// Wiederkehrende CapEx-Maßnahme
    /// </summary>
    public static CapExMeasure CreateRecurringMeasure(
        string id = "recurring-1",
        CapExCategory category = CapExCategory.Heating,
        decimal intervalPercent = 40m,
        decimal costPercent = 25m,
        decimal cycleExtensionPercent = 30m)
    {
        return CreateMeasure(
            id: id,
            category: category,
            cost: 0, // Cost calculated from renewal cost
            isRecurring: true,
            recurringConfig: new RecurringMeasureConfig
            {
                IntervalPercent = intervalPercent,
                CostPercent = costPercent,
                CycleExtensionPercent = cycleExtensionPercent
            });
    }
}
