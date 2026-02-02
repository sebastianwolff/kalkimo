using Kalkimo.Api.Models;
using Kalkimo.Domain.Models;

namespace Kalkimo.Api.Mapping;

/// <summary>
/// Maps Frontend-DTOs (JSON-Struktur) → Backend-Domain-Model (Project).
/// Diese zentrale Mapping-Schicht überbrückt die Strukturunterschiede:
/// - Frontend: flache Strukturen (landValuePercent, equity als Money)
/// - Backend: berechnete Werte (LandValue als Money, EquityContributions[])
/// </summary>
public static class FrontendProjectMapper
{
    /// <summary>
    /// Mappt einen anonymen Berechnungs-Request auf ein Domain-Projekt
    /// </summary>
    public static Project MapToProject(AnonymousCalculationRequest request)
    {
        var currency = request.Currency;
        var startPeriod = MapYearMonth(request.StartPeriod);
        var endPeriod = MapYearMonth(request.EndPeriod);
        var purchasePrice = MapMoney(request.Purchase.PurchasePrice, currency);
        var landValue = purchasePrice * (request.Purchase.LandValuePercent / 100);

        var property = MapProperty(request.Property, currency);
        var costs = MapCosts(request.Costs, currency);

        // Seed reserve initial balance from WEG currentReserveBalance if available
        if (property.WegConfiguration?.CurrentReserveBalance is { } wegReserve)
        {
            costs = costs with
            {
                ReserveAccount = costs.ReserveAccount with
                {
                    InitialBalance = wegReserve
                }
            };
        }

        var capEx = MapCapEx(request.Capex, currency);

        // Apply global §82b EStDV distribution setting:
        // When enabled, reclassify MaintenanceExpense measures to MaintenanceExpenseDistributed
        if (request.TaxProfile.Use82bDistribution && capEx != null)
        {
            var distributionYears = Math.Clamp(request.TaxProfile.DistributionYears82b, 2, 5);
            capEx = capEx with
            {
                Measures = capEx.Measures.Select(m =>
                    m.TaxClassification == TaxClassification.MaintenanceExpense
                        ? m with
                        {
                            TaxClassification = TaxClassification.MaintenanceExpenseDistributed,
                            DistributionYears = m.DistributionYears ?? distributionYears
                        }
                        : m
                ).ToList()
            };
        }

        return new Project
        {
            Id = $"anon-{Guid.NewGuid():N}",
            Name = "Anonyme Berechnung",
            Currency = currency,
            StartPeriod = startPeriod,
            EndPeriod = endPeriod,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Property = property,
            Purchase = MapPurchase(request.Purchase, purchasePrice, landValue, currency),
            Financing = MapFinancing(request.Financing, startPeriod, currency),
            Rent = MapRent(request.Rent, startPeriod, currency),
            Costs = costs,
            TaxProfile = MapTaxProfile(request.TaxProfile),
            CapEx = capEx,
            Valuation = new ValuationConfiguration()
        };
    }

    // === Property ===

    private static Property MapProperty(PropertyDto dto, string currency) => new()
    {
        Id = dto.Id ?? "property-1",
        Type = ParseEnum<PropertyType>(dto.Type, MapPropertyType),
        ConstructionYear = dto.ConstructionYear,
        OverallCondition = ParseEnum<Condition>(dto.OverallCondition),
        TotalArea = dto.TotalArea,
        LivingArea = dto.LivingArea,
        LandArea = dto.LandArea,
        UnitCount = dto.UnitCount,
        Units = dto.Units?.Select(u => MapUnit(u)).ToList() ?? [],
        Components = dto.Components?.Select(c => MapComponentCondition(c)).ToList() ?? [],
        RegionalPricePerSqm = dto.RegionalPricePerSqm,
        WegConfiguration = dto.WegConfiguration != null
            ? MapWegConfiguration(dto.WegConfiguration, currency)
            : null
    };

    private static Unit MapUnit(UnitDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Type = ParseEnum<UnitType>(dto.Type),
        Area = dto.Area,
        Rooms = dto.Rooms,
        Floor = dto.Floor,
        Status = ParseEnum<UnitStatus>(dto.Status),
        Components = dto.Components?.Select(c => MapComponentCondition(c)).ToList() ?? []
    };

    private static ComponentCondition MapComponentCondition(ComponentConditionDto dto) => new()
    {
        Category = ParseEnum<CapExCategory>(dto.Category, MapCapExCategory),
        Condition = ParseEnum<Condition>(dto.Condition),
        LastRenovationYear = dto.LastRenovationYear,
        ExpectedCycleYears = dto.ExpectedCycleYears
    };

    private static WegConfiguration MapWegConfiguration(WegConfigurationDto dto, string currency) => new()
    {
        WegName = dto.WegName,
        MeaPerMille = dto.MeaPerMille,
        TotalMeaPerMille = dto.TotalMeaPerMille,
        Hausgeld = MapHausgeld(dto.Hausgeld, currency),
        CurrentReserveBalance = dto.CurrentReserveBalance != null
            ? MapMoney(dto.CurrentReserveBalance, currency) : null,
        Sonderumlagen = dto.Sonderumlagen?.Select(s => MapSonderumlage(s, currency)).ToList() ?? [],
        DistributionKeys = dto.DistributionKeys?.Select(MapDistributionKey).ToList() ?? []
    };

    private static HausgeldConfiguration MapHausgeld(HausgeldConfigurationDto dto, string currency) => new()
    {
        MonthlyTotal = MapMoney(dto.MonthlyTotal, currency),
        MonthlyReserveContribution = MapMoney(dto.MonthlyReserveContribution, currency),
        MonthlyAdministration = dto.MonthlyAdministration != null
            ? MapMoney(dto.MonthlyAdministration, currency) : null,
        MonthlyMaintenance = dto.MonthlyMaintenance != null
            ? MapMoney(dto.MonthlyMaintenance, currency) : null,
        MonthlyHeating = dto.MonthlyHeating != null
            ? MapMoney(dto.MonthlyHeating, currency) : null,
        MonthlyOperatingCosts = dto.MonthlyOperatingCosts != null
            ? MapMoney(dto.MonthlyOperatingCosts, currency) : null,
        AnnualIncreasePercent = dto.AnnualIncreasePercent
    };

    private static Sonderumlage MapSonderumlage(SonderumlageDto dto, string currency) => new()
    {
        Id = dto.Id,
        Description = dto.Description,
        Amount = MapMoney(dto.Amount, currency),
        DueDate = MapYearMonth(dto.DueDate),
        RelatedMeasure = dto.RelatedMeasure,
        IsTaxDeductible = dto.IsTaxDeductible,
        TaxClassification = ParseEnum<TaxClassification>(dto.TaxClassification, MapTaxClassification)
    };

    private static CostDistributionKey MapDistributionKey(CostDistributionKeyDto dto) => new()
    {
        Name = dto.Name,
        Type = ParseEnum<DistributionKeyType>(dto.Type),
        CustomShare = dto.CustomShare,
        Description = dto.Description,
        ApplicableCostTypes = dto.ApplicableCostTypes?.ToList() ?? []
    };

    // === Purchase ===

    private static Purchase MapPurchase(
        PurchaseDto dto,
        Money purchasePrice,
        Money landValue,
        string currency)
    {
        var purchaseDate = new DateOnly(dto.PurchaseDate.Year, dto.PurchaseDate.Month, 1);

        var acquisitionCosts = dto.Costs?.Select(c => new AcquisitionCost
        {
            Type = MapAcquisitionCostType(c.Name),
            Amount = MapMoney(c.Amount, currency),
            Description = c.Name,
            IsCapitalizable = true
        }).ToList() ?? [];

        return new Purchase
        {
            PurchasePrice = purchasePrice,
            LandValue = landValue,
            PurchaseDate = purchaseDate,
            AcquisitionCosts = acquisitionCosts
        };
    }

    // === Financing ===

    private static Financing MapFinancing(FinancingDto dto, YearMonth startPeriod, string currency) => new()
    {
        EquityContributions = new[]
        {
            new EquityContribution
            {
                InvestorId = "anonymous",
                Amount = MapMoney(dto.Equity, currency),
                ContributionDate = startPeriod.ToFirstDayOfMonth()
            }
        },
        Loans = dto.Loans?.Select(l => MapLoan(l, currency)).ToList() ?? []
    };

    private static Loan MapLoan(LoanDto dto, string currency) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Type = LoanType.Annuity,
        Principal = MapMoney(dto.Principal, currency),
        DisbursementDate = new DateOnly(dto.StartDate.Year, dto.StartDate.Month, 1),
        InterestRatePercent = dto.InterestRatePercent,
        InitialRepaymentPercent = dto.InitialRepaymentPercent,
        FixedInterestPeriodMonths = dto.FixedInterestYears * 12,
        AnnualSpecialRepaymentAllowed = dto.SpecialRepaymentPercentPerYear.HasValue
            ? MapMoney(dto.Principal, currency) * (dto.SpecialRepaymentPercentPerYear.Value / 100)
            : null
    };

    // === Rent ===

    private static RentConfiguration MapRent(RentConfigurationDto dto, YearMonth startPeriod, string currency) => new()
    {
        Tenancies = dto.Units.Select(u => new Tenancy
        {
            Id = u.UnitId,
            UnitId = u.UnitId,
            StartDate = startPeriod.ToFirstDayOfMonth(),
            NetRent = MapMoney(u.MonthlyRent, currency),
            ServiceChargeAdvance = MapMoney(u.MonthlyServiceCharge, currency),
            DevelopmentModel = RentDevelopmentModel.Annual,
            AnnualIncreasePercent = u.AnnualRentIncreasePercent
        }).ToList(),
        VacancyRatePercent = dto.VacancyRatePercent
    };

    // === Costs ===

    private static CostConfiguration MapCosts(CostConfigurationDto dto, string currency) => new()
    {
        Items = dto.Items.Select(i => new CostItem
        {
            Id = i.Id,
            Name = i.Name,
            Classification = i.IsTransferable
                ? CostClassification.Transferable
                : CostClassification.NonTransferable,
            MonthlyAmount = MapMoney(i.MonthlyAmount, currency),
            AnnualInflationPercent = i.AnnualIncreasePercent
        }).ToList(),
        ReserveAccount = new ReserveAccountConfig
        {
            MonthlyContribution = MapMoney(dto.MaintenanceReserveMonthly, currency),
            AnnualInflationPercent = dto.MaintenanceReserveAnnualIncreasePercent
        }
    };

    // === CapEx ===

    private static CapExConfiguration? MapCapEx(IReadOnlyList<CapExMeasureDto>? capex, string currency)
    {
        if (capex == null || capex.Count == 0) return null;

        return new CapExConfiguration
        {
            Measures = capex.Select(m => new CapExMeasure
            {
                Id = m.Id,
                Name = m.Name,
                Category = ParseEnum<CapExCategory>(m.Category, MapCapExCategory),
                PlannedPeriod = MapYearMonth(m.ScheduledDate),
                EstimatedCost = MapMoney(m.Amount, currency),
                TaxClassification = ParseEnum<TaxClassification>(m.TaxClassification, MapTaxClassification),
                DistributionYears = m.DistributionYears,
                IsExecuted = true,
                IsNecessary = false,
                Priority = MeasurePriority.Medium,
                IsRecurring = m.IsRecurring,
                UnitId = m.UnitId,
                RecurringConfig = m.RecurringConfig != null
                    ? new RecurringMeasureConfig
                    {
                        IntervalPercent = m.RecurringConfig.IntervalPercent,
                        CostPercent = m.RecurringConfig.CostPercent,
                        CycleExtensionPercent = m.RecurringConfig.CycleExtensionPercent
                    }
                    : null,
                Impact = m.Impact != null
                    ? new MeasureImpact
                    {
                        CostSavingsMonthly = m.Impact.CostSavingsMonthly != null
                            ? MapMoney(m.Impact.CostSavingsMonthly, currency) : null,
                        RentIncreaseMonthly = m.Impact.RentIncreaseMonthly != null
                            ? MapMoney(m.Impact.RentIncreaseMonthly, currency) : null,
                        RentIncreasePercent = m.Impact.RentIncreasePercent,
                        DelayMonths = m.Impact.DelayMonths ?? 0
                    }
                    : null
            }).ToList()
        };
    }

    // === Tax Profile ===

    private static TaxProfile MapTaxProfile(TaxProfileDto dto) => new()
    {
        OwnershipType = dto.IsCorporate
            ? OwnershipType.Corporation
            : OwnershipType.PrivateIndividual,
        MarginalTaxRatePercent = dto.MarginalTaxRatePercent,
        ChurchTaxPercent = dto.ChurchTaxPercent,
        CustomDepreciationRatePercent = dto.DepreciationRatePercent,
        LossOffsetEnabled = true
    };

    // === Helpers ===

    private static YearMonth MapYearMonth(YearMonthDto dto) => new(dto.Year, dto.Month);

    private static Money MapMoney(MoneyInputDto dto, string fallbackCurrency) =>
        new(dto.Amount, dto.Currency ?? fallbackCurrency);

    private static T ParseEnum<T>(string value) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var result)
            ? result
            : throw new ArgumentException($"Unknown {typeof(T).Name} value: '{value}'");

    private static T ParseEnum<T>(string value, Func<string, T?> customMapper) where T : struct, Enum
    {
        var custom = customMapper(value);
        if (custom.HasValue) return custom.Value;
        return ParseEnum<T>(value);
    }

    /// <summary>
    /// Maps frontend property type strings to backend enum.
    /// Frontend uses different casing/naming for some types.
    /// </summary>
    private static PropertyType? MapPropertyType(string value) => value switch
    {
        "SingleFamily" => PropertyType.SingleFamilyHome,
        "MultiFamily" => PropertyType.MultiFamilyHome,
        "Condominium" => PropertyType.Condominium,
        "Commercial" => PropertyType.Commercial,
        "Mixed" => PropertyType.Mixed,
        _ => null
    };

    /// <summary>
    /// Maps frontend TaxClassification strings to backend enum.
    /// Frontend uses different names: AcquisitionCost, ImprovementCost, NotDeductible.
    /// </summary>
    private static TaxClassification? MapTaxClassification(string value) => value switch
    {
        "AcquisitionCost" => TaxClassification.AcquisitionRelatedCosts,
        "ImprovementCost" => TaxClassification.ManufacturingCosts,
        "NotDeductible" => TaxClassification.NotDeductible,
        _ => null
    };

    /// <summary>
    /// Maps frontend CapExCategory strings to backend enum.
    /// Frontend uses 'Exterior' where backend uses separate Exterior enum value.
    /// </summary>
    private static CapExCategory? MapCapExCategory(string value) => value switch
    {
        "Exterior" => CapExCategory.Exterior,
        _ => null
    };

    /// <summary>
    /// Maps purchase cost name to AcquisitionCostType enum
    /// </summary>
    private static AcquisitionCostType MapAcquisitionCostType(string name)
    {
        var lower = name.ToLowerInvariant();
        if (lower.Contains("notar") || lower.Contains("grundbuch")) return AcquisitionCostType.Notary;
        if (lower.Contains("grunderwerbsteuer") || lower.Contains("transfer")) return AcquisitionCostType.TransferTax;
        if (lower.Contains("makler") || lower.Contains("broker")) return AcquisitionCostType.BrokerFee;
        if (lower.Contains("gutachten") || lower.Contains("appraisal")) return AcquisitionCostType.Appraisal;
        return AcquisitionCostType.Other;
    }
}
