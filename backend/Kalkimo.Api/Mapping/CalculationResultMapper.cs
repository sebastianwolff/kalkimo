using Kalkimo.Api.Models;
using Kalkimo.Domain.Models;

namespace Kalkimo.Api.Mapping;

/// <summary>
/// Maps Backend CalculationResult → Frontend CalculationResponseDto.
/// MoneyTimeSeries werden NICHT exponiert — nur die Jahresaggregate
/// und berechneten Kennzahlen.
/// </summary>
public static class CalculationResultMapper
{
    public static CalculationResponseDto MapToDto(CalculationResult result)
    {
        return new CalculationResponseDto
        {
            ProjectId = result.ProjectId,
            CalculatedAt = DateTimeOffset.UtcNow.ToString("o"),
            Metrics = MapMetrics(result.Metrics),
            YearlyCashflows = result.YearlyCashflows.Select(MapCashflowRow).ToList(),
            TaxBridge = result.TaxBridge.Select(MapTaxBridgeRow).ToList(),
            CapexTimeline = result.CapExTimeline.Select(MapCapExTimelineItem).ToList(),
            TaxSummary = MapTaxSummary(result.TaxSummary, result),
            Warnings = result.Warnings.Select(MapWarning).ToList(),
            TotalEquityInvested = result.TotalEquityInvested,
            TotalCashflowBeforeTax = result.TotalCashflowBeforeTax,
            TotalCashflowAfterTax = result.TotalCashflowAfterTax,
            ExitAnalysis = MapExitAnalysis(result.ExitAnalysis, result),
            PropertyValueForecast = MapPropertyValueForecast(result.PropertyValueForecast, result)
        };
    }

    // === Metrics ===

    private static InvestmentMetricsDto MapMetrics(InvestmentMetrics m) => new()
    {
        IrrBeforeTaxPercent = m.IrrBeforeTaxPercent,
        IrrAfterTaxPercent = m.IrrAfterTaxPercent,
        NpvBeforeTax = MapMoney(m.NpvBeforeTax),
        NpvAfterTax = MapMoney(m.NpvAfterTax),
        CashOnCashPercent = m.CashOnCashPercent,
        EquityMultiple = m.EquityMultiple,
        DscrMin = m.DscrMin,
        DscrAvg = m.DscrAvg,
        IcrMin = m.IcrMin,
        LtvInitialPercent = m.LtvInitialPercent,
        LtvFinalPercent = m.LtvFinalPercent,
        BreakEvenRent = MapMoney(m.BreakEvenRent),
        RoiPercent = m.RoiPercent,
        MaintenanceRiskScore = m.MaintenanceRiskScore,
        LiquidityRiskScore = m.LiquidityRiskScore
    };

    // === Yearly Cashflow ===

    private static YearlyCashflowRowDto MapCashflowRow(YearlyCashflowRow r) => new()
    {
        Year = r.Year,
        GrossRent = r.GrossRent,
        VacancyLoss = r.VacancyLoss,
        EffectiveRent = r.EffectiveRent,
        ServiceChargeIncome = r.ServiceChargeIncome,
        OperatingCosts = r.OperatingCosts,
        MaintenanceReserve = r.MaintenanceReserve,
        NetOperatingIncome = r.NetOperatingIncome,
        DebtService = r.DebtService,
        InterestPortion = r.InterestPortion,
        PrincipalPortion = r.PrincipalPortion,
        CapexPayments = r.CapexPayments,
        ReserveBalanceStart = r.ReserveBalanceStart,
        CapexFromReserve = r.CapexFromReserve,
        CapexFromCashflow = r.CapexFromCashflow,
        ReserveBalanceEnd = r.ReserveBalanceEnd,
        CashflowBeforeTax = r.CashflowBeforeTax,
        Depreciation = r.Depreciation,
        InterestDeduction = r.InterestDeduction,
        MaintenanceDeduction = r.MaintenanceDeduction,
        TaxableIncome = r.TaxableIncome,
        TaxPayment = r.TaxPayment,
        CashflowAfterTax = r.CashflowAfterTax,
        CumulativeCashflow = r.CumulativeCashflow,
        OutstandingDebt = r.OutstandingDebt,
        LtvPercent = r.LtvPercent,
        DscrYear = r.DscrYear,
        IcrYear = r.IcrYear
    };

    // === Tax Bridge ===

    private static TaxBridgeRowDto MapTaxBridgeRow(TaxBridgeRow r) => new()
    {
        Year = r.Year,
        GrossIncome = r.GrossIncome,
        Depreciation = r.Depreciation,
        InterestExpense = r.InterestExpense,
        MaintenanceExpense = r.MaintenanceExpense,
        OperatingExpenses = r.OperatingExpenses,
        MaintenanceReserve = r.MaintenanceReserve,
        TaxableIncome = r.TaxableIncome,
        TaxPayment = r.TaxPayment
    };

    // === CapEx Timeline ===

    private static CapExTimelineItemDto MapCapExTimelineItem(CapExTimelineItem i) => new()
    {
        Id = i.Id,
        Name = i.Name,
        Category = i.Category.ToString(),
        Year = i.Year,
        Month = i.Month,
        Amount = i.Amount,
        TaxClassification = i.TaxClassification.ToString(),
        DistributionYears = i.DistributionYears
    };

    // === Tax Summary ===

    private static TaxSummaryDto MapTaxSummary(TaxSummary ts, CalculationResult result)
    {
        // Reserve balance at end
        var endPeriod = result.ReserveBalance.EndPeriod;
        var totalReserve = result.ReserveBalance.HasValue(endPeriod)
            ? result.ReserveBalance[endPeriod]
            : Money.Zero(ts.TotalDepreciation.Currency);

        return new TaxSummaryDto
        {
            TotalDepreciation = MapMoney(ts.TotalDepreciation),
            TotalInterestDeduction = MapMoney(ts.TotalInterestDeduction),
            TotalMaintenanceDeduction = MapMoney(ts.TotalMaintenanceDeduction),
            TotalOperatingDeduction = MapMoney(ts.TotalOperatingDeduction),
            AcquisitionRelatedCostsTriggered = ts.AcquisitionRelatedCostsTriggered,
            AcquisitionRelatedCostsAmount = MapMoney(ts.AcquisitionRelatedCostsAmount),
            TotalTaxPayment = MapMoney(ts.TotalTaxPayment),
            TotalTaxSavings = MapMoney(ts.TotalTaxSavings),
            AnnualDepreciation = MapMoney(ts.AnnualDepreciation),
            DepreciationRatePercent = ts.DepreciationRatePercent,
            DepreciationBasis = MapMoney(ts.DepreciationBasis),
            EffectiveTaxRatePercent = ts.EffectiveTaxRatePercent,
            TotalMaintenanceReserve = MapMoney(totalReserve)
        };
    }

    // === Warnings ===

    private static CalculationWarningDto MapWarning(CalculationWarning w) => new()
    {
        Type = w.Type.ToString(),
        Message = w.Message,
        Severity = w.Severity.ToString().ToLowerInvariant()
    };

    // === Exit Analysis ===

    private static ExitAnalysisDto MapExitAnalysis(ExitAnalysisResult? ea, CalculationResult result)
    {
        if (ea == null)
        {
            return new ExitAnalysisDto
            {
                Scenarios = []
            };
        }

        // Final reserve balance for DTO
        var endPeriod = result.ReserveBalance.EndPeriod;
        var finalReserveBalance = result.ReserveBalance.HasValue(endPeriod)
            ? result.ReserveBalance[endPeriod].Amount
            : 0m;

        return new ExitAnalysisDto
        {
            HoldingPeriodYears = ea.HoldingPeriodYears,
            IsWithinSpeculationPeriod = ea.IsWithinSpeculationPeriod,
            PurchasePrice = ea.PurchasePrice,
            TotalPurchaseCosts = ea.TotalPurchaseCosts,
            EquityInvested = ea.EquityInvested,
            TotalCashflowAfterTax = ea.TotalCashflowAfterTax,
            OutstandingDebtAtExit = ea.OutstandingDebtAtExit,
            TotalGrossIncome = ea.TotalGrossIncome,
            TotalOperatingCosts = ea.TotalOperatingCosts,
            TotalDebtService = ea.TotalDebtService,
            TotalCapex = ea.TotalCapex,
            TotalTaxPaid = ea.TotalTaxPaid,
            TotalMaintenanceReserve = ea.TotalMaintenanceReserve,
            FinalReserveBalance = finalReserveBalance,
            SaleCostsPercent = ea.SaleCostsPercent,
            Scenarios = ea.Scenarios.Select(s => new ExitScenarioDto
            {
                Label = s.Label,
                AnnualAppreciationPercent = s.AnnualAppreciationPercent,
                PropertyValueAtExit = s.PropertyValueAtExit,
                SaleCosts = s.SaleCosts,
                CapitalGain = s.CapitalGain,
                CapitalGainsTax = s.CapitalGainsTax,
                NetSaleProceeds = s.NetSaleProceeds,
                TotalReturn = s.TotalReturn,
                TotalReturnPercent = s.TotalReturnPercent,
                AnnualizedReturnPercent = s.AnnualizedReturnPercent
            }).ToList()
        };
    }

    // === Property Value Forecast ===

    private static PropertyValueForecastDto MapPropertyValueForecast(
        PropertyValueForecastResult? pvf,
        CalculationResult result)
    {
        if (pvf == null)
        {
            return new PropertyValueForecastDto
            {
                Drivers = [],
                Scenarios = []
            };
        }

        return new PropertyValueForecastDto
        {
            PurchasePrice = pvf.PurchasePrice,
            ImprovementValueFactor = pvf.ImprovementValueFactor,
            InitialConditionFactor = pvf.InitialConditionFactor,
            MarketComparison = pvf.MarketComparison != null
                ? new MarketComparisonDto
                {
                    RegionalPricePerSqm = pvf.MarketComparison.RegionalPricePerSqm,
                    LivingArea = pvf.MarketComparison.LivingArea,
                    FairMarketValue = pvf.MarketComparison.FairMarketValue,
                    PurchasePriceToMarketRatio = pvf.MarketComparison.PurchasePriceToMarketRatio,
                    Assessment = pvf.MarketComparison.Assessment
                }
                : null,
            ComponentDeterioration = pvf.ComponentDeterioration != null
                ? MapComponentDeterioration(pvf.ComponentDeterioration)
                : null,
            Drivers = pvf.Drivers.Select(d => new ForecastDriverDto
            {
                Type = d.Type,
                Params = d.Params
            }).ToList(),
            Scenarios = pvf.Scenarios.Select(s => new PropertyValueScenarioDto
            {
                Label = s.Label,
                AnnualAppreciationPercent = s.AnnualAppreciationPercent,
                FinalValue = s.FinalValue,
                YearlyValues = s.YearlyValues.Select(v => new PropertyValueRowDto
                {
                    Year = v.Year,
                    MarketValue = v.MarketValue,
                    ImprovementUplift = v.ImprovementUplift,
                    ConditionFactor = v.ConditionFactor,
                    ConditionDelta = v.ConditionDelta,
                    MeanReversionAdjustment = v.MeanReversionAdjustment,
                    ComponentDeteriorationCumulative = v.ComponentDeteriorationCumulative,
                    EstimatedValue = v.EstimatedValue
                }).ToList()
            }).ToList()
        };
    }

    private static ComponentDeteriorationSummaryDto MapComponentDeterioration(
        ComponentDeteriorationSummary cd) => new()
    {
        TotalValueImpact = cd.TotalValueImpact,
        TotalRenewalCostIfAllDone = cd.TotalRenewalCostIfAllDone,
        CoveredByCapex = cd.CoveredByCapex,
        UncoveredDeterioration = cd.UncoveredDeterioration,
        Components = cd.Components.Select(c => new ComponentDeteriorationRowDto
        {
            Category = c.Category.ToString(),
            AgeAtStart = c.AgeAtStart,
            AgeAtEnd = c.AgeAtEnd,
            CycleYears = c.CycleYears,
            DueYear = c.DueYear,
            RenewalCostEstimate = c.RenewalCostEstimate,
            CapexAddressedYear = c.CapexAddressedYear,
            ValueImpact = c.ValueImpact,
            StatusAtEnd = c.StatusAtEnd,
            RecurringMaintenance = c.RecurringMaintenance != null
                ? new RecurringMaintenanceInfoDto
                {
                    Name = c.RecurringMaintenance.Name,
                    IntervalYears = c.RecurringMaintenance.IntervalYears,
                    CostPerOccurrence = c.RecurringMaintenance.CostPerOccurrence,
                    OccurrencesInPeriod = c.RecurringMaintenance.OccurrencesInPeriod,
                    TotalCostInPeriod = c.RecurringMaintenance.TotalCostInPeriod,
                    EffectiveCycleYears = c.RecurringMaintenance.EffectiveCycleYears,
                    ValueImprovement = c.RecurringMaintenance.ValueImprovement
                }
                : null
        }).ToList()
    };

    // === Money Helper ===

    private static MoneyDto MapMoney(Money m) => new()
    {
        Amount = m.Amount,
        Currency = m.Currency
    };
}
