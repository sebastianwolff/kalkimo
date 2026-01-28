using Kalkimo.Domain.Models;

namespace Kalkimo.Domain.Calculators;

/// <summary>
/// Orchestriert alle Berechnungen und erstellt ein vollständiges CalculationResult
/// </summary>
public class CalculationOrchestrator
{
    private readonly TaxCalculator _taxCalculator;

    public const string EngineVersion = "1.0.0";

    public CalculationOrchestrator(TaxCalculator taxCalculator)
    {
        _taxCalculator = taxCalculator;
    }

    /// <summary>
    /// Führt die vollständige Berechnung für ein Projekt durch
    /// </summary>
    public CalculationResult Calculate(Project project, string? scenarioId = null)
    {
        // Apply scenario overrides if specified
        var effectiveProject = ApplyScenarioOverrides(project, scenarioId);
        var startPeriod = effectiveProject.StartPeriod;
        var endPeriod = effectiveProject.EndPeriod;
        var livingArea = effectiveProject.Property.LivingArea;

        // === 1. Finanzierung ===
        var loanSchedules = effectiveProject.Financing.Loans
            .Select(loan => FinancingCalculator.CalculateLoanSchedule(loan, startPeriod, endPeriod))
            .ToList();

        var aggregatedFinancing = FinancingCalculator.AggregateLoans(loanSchedules, startPeriod, endPeriod);

        // === 2. Mieteinnahmen ===
        var grossRent = CashflowCalculator.CalculateGrossRent(
            effectiveProject.Rent,
            startPeriod,
            endPeriod);

        var effectiveRent = CashflowCalculator.CalculateEffectiveRent(
            grossRent,
            effectiveProject.Rent,
            startPeriod,
            endPeriod);

        // === 3. Nebenkosten / Service Charges ===
        var serviceChargeIncome = CashflowCalculator.CalculateServiceChargeIncome(
            effectiveProject.Rent,
            startPeriod,
            endPeriod,
            effectiveProject.Currency);

        var transferableCosts = CashflowCalculator.CalculateTransferableCosts(
            effectiveProject.Costs,
            livingArea,
            startPeriod,
            endPeriod,
            effectiveProject.Currency);

        var nonTransferableCosts = CashflowCalculator.CalculateNonTransferableCosts(
            effectiveProject.Costs,
            livingArea,
            startPeriod,
            endPeriod,
            effectiveProject.Currency);

        // === 4. Betriebskosten (Gesamt) ===
        var operatingCosts = CashflowCalculator.CalculateOperatingCosts(
            effectiveProject.Costs,
            livingArea,
            startPeriod,
            endPeriod);

        // === 5. NOI (Net Operating Income) ===
        // NOI = Effective Rent + Service Charge Income - Operating Costs
        // (Service charges are income for cashflow, transferable costs offset by service charge advances)
        var noi = CashflowCalculator.CalculateNoi(
            effectiveRent,
            operatingCosts,
            startPeriod,
            endPeriod);

        // === 6. CapEx ===
        var capExPayments = CashflowCalculator.CalculateCapExPayments(
            effectiveProject.CapEx,
            startPeriod,
            endPeriod);

        // === 7. Cashflow vor Steuern ===
        var cashflowBeforeTax = CashflowCalculator.CalculateCashflowBeforeTax(
            noi,
            aggregatedFinancing.TotalDebtService,
            capExPayments,
            startPeriod,
            endPeriod);

        // === 8. Steuern ===
        // Calculate maintenance expenses for tax purposes
        var maintenanceExpense = CalculateMaintenanceExpenseForTax(effectiveProject, startPeriod, endPeriod);

        // Other deductions (non-transferable costs that are tax deductible)
        var otherDeductions = CalculateOtherTaxDeductions(effectiveProject.Costs, livingArea, startPeriod, endPeriod);

        var taxTimeSeries = _taxCalculator.CalculateTaxTimeSeries(
            effectiveProject,
            grossRent,
            aggregatedFinancing.TotalInterest,
            maintenanceExpense,
            otherDeductions,
            startPeriod,
            endPeriod);

        // === 9. Cashflow nach Steuern ===
        var cashflowAfterTax = CashflowCalculator.CalculateCashflowAfterTax(
            cashflowBeforeTax,
            taxTimeSeries.TaxPayment,
            startPeriod,
            endPeriod);

        // === 10. Kumulativer Cashflow ===
        var cumulativeCashflow = cashflowAfterTax.Cumulative();

        // === 11. Rücklagen ===
        var reserveBalance = CashflowCalculator.CalculateReserveBalance(
            effectiveProject.Costs.ReserveAccount,
            grossRent,
            livingArea,
            capExPayments,
            startPeriod,
            endPeriod);

        // === 12. Objektwert ===
        var initialValue = effectiveProject.Purchase.PurchasePrice;
        var propertyValue = CashflowCalculator.CalculatePropertyValue(
            initialValue,
            effectiveProject.Valuation,
            effectiveProject.CapEx,
            startPeriod,
            endPeriod);

        // === 13. Verkaufserlös (falls geplant) ===
        Money? saleNetProceeds = null;
        TaxSummary taxSummary;

        if (effectiveProject.Valuation.PlannedSaleDate.HasValue)
        {
            var saleDate = effectiveProject.Valuation.PlannedSaleDate.Value;
            if (saleDate <= endPeriod)
            {
                var salePrice = propertyValue[saleDate];
                var saleCosts = salePrice * effectiveProject.Valuation.SaleCostsPercent / 100;

                // Calculate capital gains tax
                var accumulatedDepreciation = taxTimeSeries.Depreciation.Sum();
                var capitalGainsResult = _taxCalculator.CalculateCapitalGainsTax(
                    effectiveProject.Purchase,
                    salePrice,
                    saleCosts,
                    accumulatedDepreciation,
                    saleDate.ToFirstDayOfMonth(),
                    effectiveProject.TaxProfile,
                    effectiveProject.Valuation.CapitalGainsTax);

                saleNetProceeds = salePrice - saleCosts - capitalGainsResult.TaxAmount;

                taxSummary = BuildTaxSummary(
                    effectiveProject, taxTimeSeries, aggregatedFinancing.TotalInterest, maintenanceExpense, capitalGainsResult);
            }
            else
            {
                taxSummary = BuildTaxSummary(
                    effectiveProject, taxTimeSeries, aggregatedFinancing.TotalInterest, maintenanceExpense, null);
            }
        }
        else
        {
            taxSummary = BuildTaxSummary(
                effectiveProject, taxTimeSeries, aggregatedFinancing.TotalInterest, maintenanceExpense, null);
        }

        // === 14. Kennzahlen ===
        var equity = effectiveProject.Financing.TotalEquity;
        var initialInvestment = effectiveProject.Purchase.TotalInvestment;

        var metrics = MetricsCalculator.CalculateAllMetrics(
            initialInvestment,
            equity,
            noi,
            cashflowBeforeTax,
            cashflowAfterTax,
            aggregatedFinancing.TotalDebtService,
            aggregatedFinancing.TotalInterest,
            aggregatedFinancing.TotalOutstandingDebt,
            propertyValue,
            effectiveProject.Property,
            effectiveProject.CapEx,
            reserveBalance,
            saleNetProceeds,
            discountRate: 5m, // Default discount rate for NPV
            startPeriod,
            endPeriod);

        // === 15. Warnungen ===
        var warnings = GenerateWarnings(
            effectiveProject,
            cashflowAfterTax,
            reserveBalance,
            noi,
            aggregatedFinancing.TotalDebtService,
            taxSummary,
            startPeriod,
            endPeriod);

        return new CalculationResult
        {
            ProjectId = effectiveProject.Id,
            ScenarioId = scenarioId ?? "base",
            CalculatedAt = YearMonth.FromDate(DateOnly.FromDateTime(DateTime.Today)),
            EngineVersion = EngineVersion,

            // Zeitreihen
            GrossRent = grossRent,
            EffectiveRent = effectiveRent,
            ServiceChargeIncome = serviceChargeIncome,
            TransferableCosts = transferableCosts,
            NonTransferableCosts = nonTransferableCosts,
            OperatingCosts = operatingCosts,
            NetOperatingIncome = noi,
            DebtService = aggregatedFinancing.TotalDebtService,
            InterestExpense = aggregatedFinancing.TotalInterest,
            PrincipalRepayment = aggregatedFinancing.TotalPrincipal,
            CashflowBeforeTax = cashflowBeforeTax,
            TaxPayment = taxTimeSeries.TaxPayment,
            CashflowAfterTax = cashflowAfterTax,
            CumulativeCashflow = cumulativeCashflow,
            ReserveBalance = reserveBalance,
            OutstandingDebt = aggregatedFinancing.TotalOutstandingDebt,
            PropertyValue = propertyValue,

            // Steuerzeitreihen
            DepreciationDeduction = taxTimeSeries.Depreciation,
            TaxableIncome = taxTimeSeries.TaxableIncome,

            // Kennzahlen und Zusammenfassungen
            Metrics = metrics,
            TaxSummary = taxSummary,
            Warnings = warnings
        };
    }

    /// <summary>
    /// Wendet Szenario-Parameter auf das Projekt an
    /// </summary>
    private static Project ApplyScenarioOverrides(Project project, string? scenarioId)
    {
        if (string.IsNullOrEmpty(scenarioId) || scenarioId == "base")
            return project;

        var scenario = project.Scenarios.FirstOrDefault(s => s.Id == scenarioId);
        if (scenario == null)
            return project;

        var parameters = scenario.Parameters;

        // Create modified copies with overrides
        var modifiedValuation = project.Valuation;
        if (parameters.MarketGrowthRatePercent.HasValue)
        {
            modifiedValuation = modifiedValuation with
            {
                MarketGrowthRatePercent = parameters.MarketGrowthRatePercent.Value
            };
        }

        var modifiedRent = project.Rent;
        if (parameters.VacancyRatePercent.HasValue)
        {
            modifiedRent = modifiedRent with
            {
                VacancyRatePercent = parameters.VacancyRatePercent.Value
            };
        }

        // Add additional vacancy events from scenario
        if (parameters.AdditionalVacancyEvents != null && parameters.AdditionalVacancyEvents.Any())
        {
            modifiedRent = modifiedRent with
            {
                VacancyEvents = modifiedRent.VacancyEvents
                    .Concat(parameters.AdditionalVacancyEvents)
                    .ToList()
            };
        }

        // Apply refinancing rate override to loans
        var modifiedFinancing = project.Financing;
        if (parameters.RefinancingInterestRatePercent.HasValue)
        {
            var modifiedLoans = project.Financing.Loans.Select(loan =>
            {
                if (loan.Refinancing != null)
                {
                    return loan with
                    {
                        Refinancing = loan.Refinancing with
                        {
                            InterestRatePercent = parameters.RefinancingInterestRatePercent.Value
                        }
                    };
                }
                return loan;
            }).ToArray();

            modifiedFinancing = modifiedFinancing with { Loans = modifiedLoans };
        }

        // Apply cost inflation override
        var modifiedCosts = project.Costs;
        if (parameters.CostInflationPercent.HasValue)
        {
            var modifiedItems = project.Costs.Items.Select(item =>
                item with { AnnualInflationPercent = parameters.CostInflationPercent.Value }
            ).ToArray();

            modifiedCosts = modifiedCosts with { Items = modifiedItems };
        }

        // Add additional CapEx from scenario
        var modifiedCapEx = project.CapEx;
        if (parameters.AdditionalCapEx != null && parameters.AdditionalCapEx.Any() && modifiedCapEx != null)
        {
            modifiedCapEx = modifiedCapEx with
            {
                Measures = modifiedCapEx.Measures
                    .Concat(parameters.AdditionalCapEx)
                    .ToList()
            };
        }

        return project with
        {
            Valuation = modifiedValuation,
            Rent = modifiedRent,
            Financing = modifiedFinancing,
            Costs = modifiedCosts,
            CapEx = modifiedCapEx
        };
    }

    /// <summary>
    /// Berechnet steuerlich absetzbare Instandhaltungskosten
    /// </summary>
    private MoneyTimeSeries CalculateMaintenanceExpenseForTax(Project project, YearMonth startPeriod, YearMonth endPeriod)
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod);

        if (project.CapEx == null)
            return result;

        // Check for 15% rule
        var acquisitionRelatedResult = _taxCalculator.CheckAcquisitionRelatedCosts(
            project.Purchase,
            project.CapEx.Measures);

        foreach (var measure in project.CapEx.Measures.Where(m => m.IsExecuted))
        {
            // Classify the measure (considering 15% rule)
            var classification = _taxCalculator.ClassifyMeasure(
                measure,
                acquisitionRelatedResult,
                project.Purchase.PurchaseDate);

            switch (classification)
            {
                case TaxClassification.MaintenanceExpense:
                    // Sofort absetzbar im Jahr der Durchführung
                    var monthsInYear = Enumerable.Range(1, 12)
                        .Select(m => new YearMonth(measure.PlannedPeriod.Year, m))
                        .Where(p => p >= startPeriod && p <= endPeriod)
                        .ToList();

                    if (monthsInYear.Any())
                    {
                        var monthlyDeduction = measure.EstimatedCost / monthsInYear.Count;
                        foreach (var period in monthsInYear)
                        {
                            result[period] += monthlyDeduction.Round();
                        }
                    }
                    break;

                case TaxClassification.MaintenanceExpenseDistributed:
                    // §82b EStDV - Verteilung über 2-5 Jahre
                    var distributionYears = measure.DistributionYears ?? 5;
                    var deductions = _taxCalculator.CalculateMaintenanceDistribution(measure, distributionYears);

                    foreach (var deduction in deductions)
                    {
                        var yearPeriods = Enumerable.Range(1, 12)
                            .Select(m => new YearMonth(deduction.Year, m))
                            .Where(p => p >= startPeriod && p <= endPeriod)
                            .ToList();

                        if (yearPeriods.Any())
                        {
                            var monthlyDeduction = deduction.Amount / yearPeriods.Count;
                            foreach (var period in yearPeriods)
                            {
                                result[period] += monthlyDeduction.Round();
                            }
                        }
                    }
                    break;

                // ManufacturingCosts and AcquisitionRelatedCosts are capitalized (added to AfA basis)
                // and not deducted as maintenance expense
            }
        }

        return result;
    }

    /// <summary>
    /// Berechnet sonstige steuerliche Abzüge (nicht-umlagefähige Kosten)
    /// </summary>
    private static MoneyTimeSeries CalculateOtherTaxDeductions(
        CostConfiguration costs,
        decimal livingAreaSqm,
        YearMonth startPeriod,
        YearMonth endPeriod)
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod);

        foreach (var period in result.Periods)
        {
            var totalDeductions = Money.Zero();
            var periodDate = period.ToFirstDayOfMonth();
            var yearsElapsed = period.Year - startPeriod.Year;

            foreach (var item in costs.Items)
            {
                // Only tax-deductible, non-transferable costs
                if (!item.IsTaxDeductible || item.Classification == CostClassification.Transferable)
                    continue;

                // Check time range
                if (item.StartDate.HasValue && periodDate < item.StartDate.Value)
                    continue;
                if (item.EndDate.HasValue && periodDate > item.EndDate.Value)
                    continue;

                var baseAmount = item.AmountPerSqmPerYear.HasValue
                    ? Money.Euro(item.AmountPerSqmPerYear.Value * livingAreaSqm / 12)
                    : item.MonthlyAmount;

                var inflatedAmount = baseAmount *
                    (decimal)Math.Pow(1 + (double)item.AnnualInflationPercent / 100, yearsElapsed);

                totalDeductions += inflatedAmount;
            }

            result[period] = totalDeductions.Round();
        }

        return result;
    }

    /// <summary>
    /// Erstellt die Steuer-Zusammenfassung
    /// </summary>
    private TaxSummary BuildTaxSummary(
        Project project,
        TaxTimeSeries taxTimeSeries,
        MoneyTimeSeries interestExpense,
        MoneyTimeSeries maintenanceExpense,
        CapitalGainsTaxResult? capitalGainsResult)
    {
        var acquisitionRelatedResult = project.CapEx != null
            ? _taxCalculator.CheckAcquisitionRelatedCosts(project.Purchase, project.CapEx.Measures)
            : new AcquisitionRelatedCostsResult { IsTriggered = false };

        // Build maintenance distributions
        var distributions = new List<MaintenanceDistribution>();
        if (project.CapEx != null)
        {
            foreach (var measure in project.CapEx.Measures.Where(m =>
                m.IsExecuted && m.TaxClassification == TaxClassification.MaintenanceExpenseDistributed))
            {
                var years = measure.DistributionYears ?? 5;
                distributions.Add(new MaintenanceDistribution
                {
                    MeasureId = measure.Id,
                    TotalAmount = measure.EstimatedCost,
                    DistributionYears = years,
                    StartYear = measure.PlannedPeriod.Year
                });
            }
        }

        // Build yearly summaries
        var yearlySummaries = new Dictionary<int, TaxYearSummary>();
        var startYear = project.StartPeriod.Year;
        var endYear = project.EndPeriod.Year;

        for (var year = startYear; year <= endYear; year++)
        {
            var yearPeriods = Enumerable.Range(1, 12)
                .Select(m => new YearMonth(year, m))
                .Where(p => p >= project.StartPeriod && p <= project.EndPeriod)
                .ToList();

            if (!yearPeriods.Any())
                continue;

            var grossIncome = yearPeriods.Aggregate(Money.Zero(), (sum, p) => sum + taxTimeSeries.TaxableIncome[p]);
            var depreciation = yearPeriods.Aggregate(Money.Zero(), (sum, p) => sum + taxTimeSeries.Depreciation[p]);
            var interest = yearPeriods.Aggregate(Money.Zero(), (sum, p) => sum + interestExpense[p]);
            var maintenance = yearPeriods.Aggregate(Money.Zero(), (sum, p) => sum + maintenanceExpense[p]);
            var taxPayment = yearPeriods.Aggregate(Money.Zero(), (sum, p) => sum + taxTimeSeries.TaxPayment[p]);

            yearlySummaries[year] = new TaxYearSummary
            {
                Year = year,
                GrossIncome = grossIncome,
                Depreciation = depreciation,
                InterestExpense = interest,
                MaintenanceExpense = maintenance,
                OtherDeductions = Money.Zero(), // Could be calculated separately
                TaxableIncome = grossIncome - depreciation - interest - maintenance,
                TaxPayment = taxPayment
            };
        }

        return new TaxSummary
        {
            TotalDepreciation = taxTimeSeries.Depreciation.Sum(),
            TotalInterestDeduction = interestExpense.Sum(),
            TotalMaintenanceDeduction = maintenanceExpense.Sum(),
            AcquisitionRelatedCostsTriggered = acquisitionRelatedResult.IsTriggered,
            AcquisitionRelatedCostsAmount = acquisitionRelatedResult.ActualCosts,
            MaintenanceDistributions = distributions,
            CapitalGainsTax = capitalGainsResult?.TaxAmount,
            SaleTaxExempt = capitalGainsResult?.IsTaxExempt ?? true,
            TotalTaxPayment = taxTimeSeries.TaxPayment.Sum() + (capitalGainsResult?.TaxAmount ?? Money.Zero()),
            YearlyTax = yearlySummaries
        };
    }

    /// <summary>
    /// Generiert Warnungen basierend auf den Berechnungsergebnissen
    /// </summary>
    private static IReadOnlyList<CalculationWarning> GenerateWarnings(
        Project project,
        MoneyTimeSeries cashflowAfterTax,
        MoneyTimeSeries reserveBalance,
        MoneyTimeSeries noi,
        MoneyTimeSeries debtService,
        TaxSummary taxSummary,
        YearMonth startPeriod,
        YearMonth endPeriod)
    {
        var warnings = new List<CalculationWarning>();

        // Check for negative cashflow periods
        foreach (var (period, value) in cashflowAfterTax)
        {
            if (value.Amount < 0)
            {
                warnings.Add(new CalculationWarning
                {
                    Type = WarningType.NegativeCashflow,
                    Message = $"Negativer Cashflow von {value} im {period}",
                    Severity = WarningSeverity.Warning,
                    Period = period,
                    RelatedField = "CashflowAfterTax"
                });
            }
        }

        // Check for reserve below threshold
        foreach (var (period, balance) in reserveBalance)
        {
            if (balance.Amount < 0)
            {
                warnings.Add(new CalculationWarning
                {
                    Type = WarningType.ReserveBelowThreshold,
                    Message = $"Rücklagenkonto negativ ({balance}) im {period}",
                    Severity = WarningSeverity.Critical,
                    Period = period,
                    RelatedField = "ReserveBalance"
                });
                break; // Only warn once for negative reserves
            }
            else if (balance.Amount < 5000)
            {
                warnings.Add(new CalculationWarning
                {
                    Type = WarningType.ReserveBelowThreshold,
                    Message = $"Rücklagenkonto unter 5.000€ ({balance}) im {period}",
                    Severity = WarningSeverity.Warning,
                    Period = period,
                    RelatedField = "ReserveBalance"
                });
            }
        }

        // Check DSCR
        foreach (var period in noi.Periods)
        {
            var ds = debtService[period].Amount;
            if (ds > 0)
            {
                var dscr = noi[period].Amount / ds;
                if (dscr < 1.0m)
                {
                    warnings.Add(new CalculationWarning
                    {
                        Type = WarningType.DscrBelowOne,
                        Message = $"DSCR unter 1.0 ({dscr:F2}) im {period}",
                        Severity = WarningSeverity.Critical,
                        Period = period,
                        RelatedField = "DSCR"
                    });
                }
            }
        }

        // Check for 15% rule trigger
        if (taxSummary.AcquisitionRelatedCostsTriggered)
        {
            warnings.Add(new CalculationWarning
            {
                Type = WarningType.AcquisitionRelatedCostsTriggered,
                Message = $"15%-Regel wurde ausgelöst: Erhaltungsaufwendungen von {taxSummary.AcquisitionRelatedCostsAmount} werden zu Herstellungskosten",
                Severity = WarningSeverity.Info,
                RelatedField = "TaxClassification"
            });
        }

        // Check for deferred maintenance
        if (project.CapEx != null)
        {
            var deferredMeasures = project.CapEx.Measures
                .Where(m => m.IsNecessary && !m.IsExecuted && m.PlannedPeriod <= endPeriod)
                .ToList();

            foreach (var measure in deferredMeasures)
            {
                warnings.Add(new CalculationWarning
                {
                    Type = WarningType.DeferredMaintenance,
                    Message = $"Notwendige Maßnahme '{measure.Name}' nicht durchgeführt (geplant: {measure.PlannedPeriod})",
                    Severity = measure.Priority == MeasurePriority.Critical ? WarningSeverity.Critical : WarningSeverity.Warning,
                    Period = measure.PlannedPeriod,
                    RelatedField = "CapEx"
                });
            }
        }

        // Deduplicate similar warnings
        return warnings
            .GroupBy(w => new { w.Type, w.Period })
            .Select(g => g.First())
            .OrderBy(w => w.Period ?? YearMonth.MinValue)
            .ThenBy(w => w.Severity)
            .ToList();
    }
}
