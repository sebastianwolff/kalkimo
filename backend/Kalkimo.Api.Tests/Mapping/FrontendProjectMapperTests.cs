using FluentAssertions;
using Kalkimo.Api.Mapping;
using Kalkimo.Api.Models;
using Kalkimo.Domain.Models;

namespace Kalkimo.Api.Tests.Mapping;

/// <summary>
/// Tests für FrontendProjectMapper.
/// Verifiziert die korrekte Transformation von Frontend-DTOs
/// (JSON-Struktur) in Backend-Domain-Modelle.
/// </summary>
public class FrontendProjectMapperTests
{
    /// <summary>
    /// Erzeugt einen vollständigen, gültigen AnonymousCalculationRequest
    /// der die typische Frontend-JSON-Struktur widerspiegelt.
    /// </summary>
    private static AnonymousCalculationRequest CreateValidRequest(
        decimal purchasePrice = 400_000m,
        decimal landValuePercent = 20m,
        decimal equity = 100_000m,
        int startYear = 2025,
        int endYear = 2035)
    {
        return new AnonymousCalculationRequest
        {
            Currency = "EUR",
            StartPeriod = new YearMonthDto { Year = startYear, Month = 1 },
            EndPeriod = new YearMonthDto { Year = endYear, Month = 12 },
            Property = new PropertyDto
            {
                Id = "prop-1",
                Type = "MultiFamily",
                ConstructionYear = 2000,
                OverallCondition = "Good",
                TotalArea = 500m,
                LivingArea = 400m,
                LandArea = 600m,
                UnitCount = 4,
                Components = new[]
                {
                    new ComponentConditionDto { Category = "Heating", Condition = "Fair", LastRenovationYear = 2010, ExpectedCycleYears = 20 },
                    new ComponentConditionDto { Category = "Roof", Condition = "Good", LastRenovationYear = 2000, ExpectedCycleYears = 45 }
                }
            },
            Purchase = new PurchaseDto
            {
                PurchasePrice = new MoneyInputDto { Amount = purchasePrice },
                PurchaseDate = new YearMonthDto { Year = startYear, Month = 1 },
                LandValuePercent = landValuePercent,
                Costs = new[]
                {
                    new PurchaseCostItemDto { Id = "cost-1", Name = "Grunderwerbsteuer", Amount = new MoneyInputDto { Amount = purchasePrice * 0.05m } },
                    new PurchaseCostItemDto { Id = "cost-2", Name = "Notar und Grundbuch", Amount = new MoneyInputDto { Amount = purchasePrice * 0.015m } },
                    new PurchaseCostItemDto { Id = "cost-3", Name = "Maklergebühr", Amount = new MoneyInputDto { Amount = purchasePrice * 0.0357m } }
                }
            },
            Financing = new FinancingDto
            {
                Equity = new MoneyInputDto { Amount = equity },
                Loans = new[]
                {
                    new LoanDto
                    {
                        Id = "loan-1",
                        Name = "Hauptdarlehen",
                        Principal = new MoneyInputDto { Amount = 340_280m },
                        InterestRatePercent = 3.5m,
                        InitialRepaymentPercent = 2m,
                        FixedInterestYears = 10,
                        StartDate = new YearMonthDto { Year = startYear, Month = 1 }
                    }
                }
            },
            Rent = new RentConfigurationDto
            {
                Units = new[]
                {
                    new RentUnitDto
                    {
                        UnitId = "unit-1",
                        MonthlyRent = new MoneyInputDto { Amount = 2_000m },
                        MonthlyServiceCharge = new MoneyInputDto { Amount = 300m },
                        AnnualRentIncreasePercent = 2m
                    }
                },
                VacancyRatePercent = 3m
            },
            Costs = new CostConfigurationDto
            {
                Items = new[]
                {
                    new CostItemDto { Id = "cost-admin", Name = "Verwaltung", MonthlyAmount = new MoneyInputDto { Amount = 100m }, IsTransferable = false, AnnualIncreasePercent = 2m },
                    new CostItemDto { Id = "cost-insurance", Name = "Versicherung", MonthlyAmount = new MoneyInputDto { Amount = 80m }, IsTransferable = true }
                },
                MaintenanceReserveMonthly = new MoneyInputDto { Amount = 333m },
                MaintenanceReserveAnnualIncreasePercent = 2m
            },
            TaxProfile = new TaxProfileDto
            {
                MarginalTaxRatePercent = 42m,
                IsCorporate = false
            }
        };
    }

    #region Grundlegendes Mapping

    [Fact]
    public void MapToProject_ValidRequest_ReturnsProject()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        project.Should().NotBeNull();
        project.Currency.Should().Be("EUR");
    }

    [Fact]
    public void MapToProject_SetsAnonymousId()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        project.Id.Should().StartWith("anon-");
    }

    [Fact]
    public void MapToProject_MapsStartAndEndPeriod()
    {
        var request = CreateValidRequest(startYear: 2025, endYear: 2035);

        var project = FrontendProjectMapper.MapToProject(request);

        project.StartPeriod.Year.Should().Be(2025);
        project.StartPeriod.Month.Should().Be(1);
        project.EndPeriod.Year.Should().Be(2035);
        project.EndPeriod.Month.Should().Be(12);
    }

    #endregion

    #region Property Mapping

    [Fact]
    public void MapToProject_MapsPropertyType_MultiFamily()
    {
        var request = CreateValidRequest();
        request = request with
        {
            Property = request.Property with { Type = "MultiFamily" }
        };

        var project = FrontendProjectMapper.MapToProject(request);

        project.Property.Type.Should().Be(PropertyType.MultiFamilyHome);
    }

    [Fact]
    public void MapToProject_MapsPropertyType_SingleFamily()
    {
        var request = CreateValidRequest();
        request = request with
        {
            Property = request.Property with { Type = "SingleFamily" }
        };

        var project = FrontendProjectMapper.MapToProject(request);

        project.Property.Type.Should().Be(PropertyType.SingleFamilyHome);
    }

    [Fact]
    public void MapToProject_MapsPropertyType_Condominium()
    {
        var request = CreateValidRequest();
        request = request with
        {
            Property = request.Property with { Type = "Condominium" }
        };

        var project = FrontendProjectMapper.MapToProject(request);

        project.Property.Type.Should().Be(PropertyType.Condominium);
    }

    [Fact]
    public void MapToProject_MapsPropertyAreas()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        project.Property.TotalArea.Should().Be(500m);
        project.Property.LivingArea.Should().Be(400m);
        project.Property.LandArea.Should().Be(600m);
        project.Property.UnitCount.Should().Be(4);
    }

    [Fact]
    public void MapToProject_MapsOverallCondition()
    {
        var request = CreateValidRequest();
        request = request with
        {
            Property = request.Property with { OverallCondition = "Fair" }
        };

        var project = FrontendProjectMapper.MapToProject(request);

        project.Property.OverallCondition.Should().Be(Condition.Fair);
    }

    [Fact]
    public void MapToProject_MapsComponents()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        project.Property.Components.Should().HaveCount(2);
        project.Property.Components[0].Category.Should().Be(CapExCategory.Heating);
        project.Property.Components[0].Condition.Should().Be(Condition.Fair);
        project.Property.Components[0].LastRenovationYear.Should().Be(2010);
        project.Property.Components[0].ExpectedCycleYears.Should().Be(20);
        project.Property.Components[1].Category.Should().Be(CapExCategory.Roof);
    }

    [Fact]
    public void MapToProject_MapsRegionalPrice()
    {
        var request = CreateValidRequest();
        request = request with
        {
            Property = request.Property with { RegionalPricePerSqm = 3_000m }
        };

        var project = FrontendProjectMapper.MapToProject(request);

        project.Property.RegionalPricePerSqm.Should().Be(3_000m);
    }

    [Fact]
    public void MapToProject_NullComponents_ReturnsEmptyList()
    {
        var request = CreateValidRequest();
        request = request with
        {
            Property = request.Property with { Components = null }
        };

        var project = FrontendProjectMapper.MapToProject(request);

        project.Property.Components.Should().BeEmpty();
    }

    #endregion

    #region Purchase Mapping

    [Fact]
    public void MapToProject_MapsPurchasePrice()
    {
        var request = CreateValidRequest(purchasePrice: 500_000m);

        var project = FrontendProjectMapper.MapToProject(request);

        project.Purchase.PurchasePrice.Amount.Should().Be(500_000m);
        project.Purchase.PurchasePrice.Currency.Should().Be("EUR");
    }

    [Fact]
    public void MapToProject_CalculatesLandValue()
    {
        var request = CreateValidRequest(purchasePrice: 400_000m, landValuePercent: 20m);

        var project = FrontendProjectMapper.MapToProject(request);

        project.Purchase.LandValue.Amount.Should().Be(80_000m); // 400_000 * 20%
    }

    [Fact]
    public void MapToProject_CalculatesLandValue_DifferentPercentage()
    {
        var request = CreateValidRequest(purchasePrice: 500_000m, landValuePercent: 30m);

        var project = FrontendProjectMapper.MapToProject(request);

        project.Purchase.LandValue.Amount.Should().Be(150_000m); // 500_000 * 30%
    }

    [Fact]
    public void MapToProject_MapsPurchaseDate()
    {
        var request = CreateValidRequest(startYear: 2025);

        var project = FrontendProjectMapper.MapToProject(request);

        project.Purchase.PurchaseDate.Year.Should().Be(2025);
        project.Purchase.PurchaseDate.Month.Should().Be(1);
    }

    [Fact]
    public void MapToProject_MapsAcquisitionCosts_TransferTax()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        var transferTax = project.Purchase.AcquisitionCosts
            .FirstOrDefault(c => c.Type == AcquisitionCostType.TransferTax);
        transferTax.Should().NotBeNull();
        transferTax!.Amount.Amount.Should().Be(20_000m); // 400_000 * 5%
    }

    [Fact]
    public void MapToProject_MapsAcquisitionCosts_Notary()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        var notary = project.Purchase.AcquisitionCosts
            .FirstOrDefault(c => c.Type == AcquisitionCostType.Notary);
        notary.Should().NotBeNull();
    }

    [Fact]
    public void MapToProject_MapsAcquisitionCosts_BrokerFee()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        var broker = project.Purchase.AcquisitionCosts
            .FirstOrDefault(c => c.Type == AcquisitionCostType.BrokerFee);
        broker.Should().NotBeNull();
    }

    #endregion

    #region Financing Mapping

    [Fact]
    public void MapToProject_MapsEquity()
    {
        var request = CreateValidRequest(equity: 100_000m);

        var project = FrontendProjectMapper.MapToProject(request);

        project.Financing.EquityContributions.Should().HaveCount(1);
        project.Financing.EquityContributions[0].Amount.Amount.Should().Be(100_000m);
        project.Financing.EquityContributions[0].InvestorId.Should().Be("anonymous");
    }

    [Fact]
    public void MapToProject_MapsLoan()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        project.Financing.Loans.Should().HaveCount(1);
        var loan = project.Financing.Loans[0];
        loan.Id.Should().Be("loan-1");
        loan.Name.Should().Be("Hauptdarlehen");
        loan.Type.Should().Be(LoanType.Annuity);
        loan.InterestRatePercent.Should().Be(3.5m);
        loan.InitialRepaymentPercent.Should().Be(2m);
    }

    [Fact]
    public void MapToProject_MapsFixedInterestPeriod_YearsToMonths()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        // Frontend sends years, backend expects months
        project.Financing.Loans[0].FixedInterestPeriodMonths.Should().Be(120); // 10 years * 12
    }

    [Fact]
    public void MapToProject_NullLoans_ReturnsEmptyList()
    {
        var request = CreateValidRequest();
        request = request with
        {
            Financing = request.Financing with { Loans = null }
        };

        var project = FrontendProjectMapper.MapToProject(request);

        project.Financing.Loans.Should().BeEmpty();
    }

    #endregion

    #region Rent Mapping

    [Fact]
    public void MapToProject_MapsTenancies()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        project.Rent.Tenancies.Should().HaveCount(1);
        var tenancy = project.Rent.Tenancies[0];
        tenancy.NetRent.Amount.Should().Be(2_000m);
        tenancy.ServiceChargeAdvance.Amount.Should().Be(300m);
        tenancy.AnnualIncreasePercent.Should().Be(2m);
    }

    [Fact]
    public void MapToProject_TenancyDevelopmentModel_IsAnnual()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        project.Rent.Tenancies[0].DevelopmentModel.Should().Be(RentDevelopmentModel.Annual);
    }

    [Fact]
    public void MapToProject_MapsVacancyRate()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        project.Rent.VacancyRatePercent.Should().Be(3m);
    }

    [Fact]
    public void MapToProject_MultipleRentUnits()
    {
        var request = CreateValidRequest();
        request = request with
        {
            Rent = new RentConfigurationDto
            {
                Units = new[]
                {
                    new RentUnitDto { UnitId = "u1", MonthlyRent = new MoneyInputDto { Amount = 1_000m }, MonthlyServiceCharge = new MoneyInputDto { Amount = 200m }, AnnualRentIncreasePercent = 2m },
                    new RentUnitDto { UnitId = "u2", MonthlyRent = new MoneyInputDto { Amount = 1_500m }, MonthlyServiceCharge = new MoneyInputDto { Amount = 250m }, AnnualRentIncreasePercent = 1.5m }
                },
                VacancyRatePercent = 3m
            }
        };

        var project = FrontendProjectMapper.MapToProject(request);

        project.Rent.Tenancies.Should().HaveCount(2);
        project.Rent.Tenancies[0].NetRent.Amount.Should().Be(1_000m);
        project.Rent.Tenancies[1].NetRent.Amount.Should().Be(1_500m);
    }

    #endregion

    #region Cost Mapping

    [Fact]
    public void MapToProject_MapsCostItems()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        project.Costs.Items.Should().HaveCount(2);
    }

    [Fact]
    public void MapToProject_MapsCostClassification_NonTransferable()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        var admin = project.Costs.Items.First(i => i.Id == "cost-admin");
        admin.Classification.Should().Be(CostClassification.NonTransferable);
    }

    [Fact]
    public void MapToProject_MapsCostClassification_Transferable()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        var insurance = project.Costs.Items.First(i => i.Id == "cost-insurance");
        insurance.Classification.Should().Be(CostClassification.Transferable);
    }

    [Fact]
    public void MapToProject_MapsMaintenanceReserve()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        project.Costs.ReserveAccount.Should().NotBeNull();
        project.Costs.ReserveAccount.MonthlyContribution.Should().NotBeNull();
        project.Costs.ReserveAccount.MonthlyContribution!.Value.Amount.Should().Be(333m);
        project.Costs.ReserveAccount.AnnualInflationPercent.Should().Be(2m);
    }

    #endregion

    #region CapEx Mapping

    [Fact]
    public void MapToProject_NullCapex_ReturnsNull()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        project.CapEx.Should().BeNull();
    }

    [Fact]
    public void MapToProject_EmptyCapex_ReturnsNull()
    {
        var request = CreateValidRequest();
        request = request with { Capex = Array.Empty<CapExMeasureDto>() };

        var project = FrontendProjectMapper.MapToProject(request);

        project.CapEx.Should().BeNull();
    }

    [Fact]
    public void MapToProject_MapsCapExMeasure()
    {
        var request = CreateValidRequest();
        request = request with
        {
            Capex = new[]
            {
                new CapExMeasureDto
                {
                    Id = "capex-1",
                    Name = "Heizungserneuerung",
                    Category = "Heating",
                    Amount = new MoneyInputDto { Amount = 25_000m },
                    ScheduledDate = new YearMonthDto { Year = 2027, Month = 6 },
                    TaxClassification = "MaintenanceExpense"
                }
            }
        };

        var project = FrontendProjectMapper.MapToProject(request);

        project.CapEx.Should().NotBeNull();
        project.CapEx!.Measures.Should().HaveCount(1);
        var measure = project.CapEx.Measures[0];
        measure.Id.Should().Be("capex-1");
        measure.Category.Should().Be(CapExCategory.Heating);
        measure.EstimatedCost.Amount.Should().Be(25_000m);
        measure.PlannedPeriod.Year.Should().Be(2027);
        measure.PlannedPeriod.Month.Should().Be(6);
        measure.TaxClassification.Should().Be(TaxClassification.MaintenanceExpense);
        measure.IsExecuted.Should().BeTrue();
    }

    [Fact]
    public void MapToProject_MapsRecurringCapEx()
    {
        var request = CreateValidRequest();
        request = request with
        {
            Capex = new[]
            {
                new CapExMeasureDto
                {
                    Id = "r1",
                    Name = "Wartung Heizung",
                    Category = "Heating",
                    Amount = new MoneyInputDto { Amount = 0m },
                    ScheduledDate = new YearMonthDto { Year = 2027, Month = 1 },
                    TaxClassification = "MaintenanceExpense",
                    IsRecurring = true,
                    RecurringConfig = new RecurringMeasureConfigDto
                    {
                        IntervalPercent = 40m,
                        CostPercent = 25m,
                        CycleExtensionPercent = 30m
                    }
                }
            }
        };

        var project = FrontendProjectMapper.MapToProject(request);

        var measure = project.CapEx!.Measures[0];
        measure.IsRecurring.Should().BeTrue();
        measure.RecurringConfig.Should().NotBeNull();
        measure.RecurringConfig!.IntervalPercent.Should().Be(40m);
        measure.RecurringConfig.CostPercent.Should().Be(25m);
        measure.RecurringConfig.CycleExtensionPercent.Should().Be(30m);
    }

    [Fact]
    public void MapToProject_MapsCapExImpact()
    {
        var request = CreateValidRequest();
        request = request with
        {
            Capex = new[]
            {
                new CapExMeasureDto
                {
                    Id = "m1",
                    Name = "Heizung",
                    Category = "Heating",
                    Amount = new MoneyInputDto { Amount = 25_000m },
                    ScheduledDate = new YearMonthDto { Year = 2027, Month = 6 },
                    TaxClassification = "MaintenanceExpense",
                    Impact = new MeasureImpactDto
                    {
                        CostSavingsMonthly = new MoneyInputDto { Amount = 100m },
                        RentIncreasePercent = 3m,
                        DelayMonths = 2
                    }
                }
            }
        };

        var project = FrontendProjectMapper.MapToProject(request);

        var impact = project.CapEx!.Measures[0].Impact;
        impact.Should().NotBeNull();
        impact!.CostSavingsMonthly.Should().NotBeNull();
        impact.CostSavingsMonthly!.Value.Amount.Should().Be(100m);
        impact.RentIncreasePercent.Should().Be(3m);
        impact.DelayMonths.Should().Be(2);
    }

    #endregion

    #region TaxProfile Mapping

    [Fact]
    public void MapToProject_MapsPrivateIndividual()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        project.TaxProfile.OwnershipType.Should().Be(OwnershipType.PrivateIndividual);
        project.TaxProfile.MarginalTaxRatePercent.Should().Be(42m);
    }

    [Fact]
    public void MapToProject_MapsCorporate()
    {
        var request = CreateValidRequest();
        request = request with
        {
            TaxProfile = new TaxProfileDto { MarginalTaxRatePercent = 15m, IsCorporate = true }
        };

        var project = FrontendProjectMapper.MapToProject(request);

        project.TaxProfile.OwnershipType.Should().Be(OwnershipType.Corporation);
    }

    [Fact]
    public void MapToProject_MapsChurchTax()
    {
        var request = CreateValidRequest();
        request = request with
        {
            TaxProfile = new TaxProfileDto { MarginalTaxRatePercent = 42m, ChurchTaxPercent = 8m }
        };

        var project = FrontendProjectMapper.MapToProject(request);

        project.TaxProfile.ChurchTaxPercent.Should().Be(8m);
    }

    [Fact]
    public void MapToProject_MapsCustomDepreciationRate()
    {
        var request = CreateValidRequest();
        request = request with
        {
            TaxProfile = new TaxProfileDto { MarginalTaxRatePercent = 42m, DepreciationRatePercent = 3m }
        };

        var project = FrontendProjectMapper.MapToProject(request);

        project.TaxProfile.CustomDepreciationRatePercent.Should().Be(3m);
    }

    #endregion

    #region AcquisitionCostType Heuristic Mapping

    [Fact]
    public void MapToProject_AcquisitionCost_Grunderwerbsteuer_IsTransferTax()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        project.Purchase.AcquisitionCosts
            .Should().Contain(c => c.Type == AcquisitionCostType.TransferTax);
    }

    [Fact]
    public void MapToProject_AcquisitionCost_Notar_IsNotary()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        project.Purchase.AcquisitionCosts
            .Should().Contain(c => c.Type == AcquisitionCostType.Notary);
    }

    [Fact]
    public void MapToProject_AcquisitionCost_Makler_IsBrokerFee()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        project.Purchase.AcquisitionCosts
            .Should().Contain(c => c.Type == AcquisitionCostType.BrokerFee);
    }

    [Fact]
    public void MapToProject_AcquisitionCost_UnknownName_IsOther()
    {
        var request = CreateValidRequest();
        request = request with
        {
            Purchase = request.Purchase with
            {
                Costs = new[]
                {
                    new PurchaseCostItemDto { Id = "c1", Name = "Sonstiges", Amount = new MoneyInputDto { Amount = 1_000m } }
                }
            }
        };

        var project = FrontendProjectMapper.MapToProject(request);

        project.Purchase.AcquisitionCosts[0].Type.Should().Be(AcquisitionCostType.Other);
    }

    #endregion

    #region Currency Handling

    [Fact]
    public void MapToProject_UsesCurrencyFromRequest()
    {
        var request = CreateValidRequest();
        request = request with { Currency = "EUR" };

        var project = FrontendProjectMapper.MapToProject(request);

        project.Currency.Should().Be("EUR");
        project.Purchase.PurchasePrice.Currency.Should().Be("EUR");
    }

    [Fact]
    public void MapToProject_MoneyWithoutCurrency_UsesFallback()
    {
        var request = CreateValidRequest();

        var project = FrontendProjectMapper.MapToProject(request);

        // All Money values should use EUR as fallback
        project.Financing.EquityContributions[0].Amount.Currency.Should().Be("EUR");
        project.Financing.Loans[0].Principal.Currency.Should().Be("EUR");
        project.Rent.Tenancies[0].NetRent.Currency.Should().Be("EUR");
    }

    #endregion

    #region Invalid Enum Values

    [Fact]
    public void MapToProject_InvalidPropertyType_Throws()
    {
        var request = CreateValidRequest();
        request = request with
        {
            Property = request.Property with { Type = "InvalidType" }
        };

        var act = () => FrontendProjectMapper.MapToProject(request);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*PropertyType*InvalidType*");
    }

    [Fact]
    public void MapToProject_InvalidCondition_Throws()
    {
        var request = CreateValidRequest();
        request = request with
        {
            Property = request.Property with { OverallCondition = "Terrible" }
        };

        var act = () => FrontendProjectMapper.MapToProject(request);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Condition*Terrible*");
    }

    [Fact]
    public void MapToProject_InvalidCapExCategory_Throws()
    {
        var request = CreateValidRequest();
        request = request with
        {
            Capex = new[]
            {
                new CapExMeasureDto
                {
                    Id = "m1",
                    Name = "Test",
                    Category = "NonExistentCategory",
                    Amount = new MoneyInputDto { Amount = 1_000m },
                    ScheduledDate = new YearMonthDto { Year = 2027, Month = 1 },
                    TaxClassification = "MaintenanceExpense"
                }
            }
        };

        var act = () => FrontendProjectMapper.MapToProject(request);

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region End-to-End: Mapped Project kann berechnet werden

    [Fact]
    public void MapToProject_ResultCanBeCalculated()
    {
        // The mapped project should be valid enough to run through the orchestrator
        var request = CreateValidRequest();
        var project = FrontendProjectMapper.MapToProject(request);

        var dateProvider = new Kalkimo.Domain.Calculators.FixedDateProvider(new DateOnly(2025, 1, 1));
        var taxCalculator = new Kalkimo.Domain.Calculators.TaxCalculator(dateProvider);
        var orchestrator = new Kalkimo.Domain.Calculators.CalculationOrchestrator(taxCalculator);

        var result = orchestrator.Calculate(project);

        result.Should().NotBeNull();
        result.GrossRent.Sum().Amount.Should().BeGreaterThan(0);
        result.YearlyCashflows.Should().NotBeEmpty();
        result.PropertyValueForecast.Should().NotBeNull();
        result.ExitAnalysis.Should().NotBeNull();
    }

    #endregion
}
