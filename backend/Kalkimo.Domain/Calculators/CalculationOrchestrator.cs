using Kalkimo.Domain.Models;

namespace Kalkimo.Domain.Calculators;

/// <summary>
/// Orchestriert alle Berechnungen und erstellt ein vollständiges CalculationResult
/// </summary>
public class CalculationOrchestrator
{
    private readonly TaxCalculator _taxCalculator;

    public const string EngineVersion = "2.0.0";

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

        var currency = effectiveProject.Currency;

        // === 1. Finanzierung ===
        var loanSchedules = effectiveProject.Financing.Loans
            .Select(loan => FinancingCalculator.CalculateLoanSchedule(loan, startPeriod, endPeriod, currency))
            .ToList();

        var aggregatedFinancing = FinancingCalculator.AggregateLoans(loanSchedules, startPeriod, endPeriod, currency);

        // === 2. Mieteinnahmen ===
        var grossRent = CashflowCalculator.CalculateGrossRent(
            effectiveProject.Rent,
            startPeriod,
            endPeriod,
            currency);

        var effectiveRent = CashflowCalculator.CalculateEffectiveRent(
            grossRent,
            effectiveProject.Rent,
            startPeriod,
            endPeriod,
            currency);

        // === 3. Nebenkosten / Service Charges ===
        var serviceChargeIncome = CashflowCalculator.CalculateServiceChargeIncome(
            effectiveProject.Rent,
            startPeriod,
            endPeriod,
            currency);

        var transferableCosts = CashflowCalculator.CalculateTransferableCosts(
            effectiveProject.Costs,
            livingArea,
            startPeriod,
            endPeriod,
            currency);

        var nonTransferableCosts = CashflowCalculator.CalculateNonTransferableCosts(
            effectiveProject.Costs,
            livingArea,
            startPeriod,
            endPeriod,
            currency);

        // === 4. Betriebskosten (Gesamt) ===
        var operatingCosts = CashflowCalculator.CalculateOperatingCosts(
            effectiveProject.Costs,
            livingArea,
            startPeriod,
            endPeriod,
            currency);

        // === 5. Recurring CapEx Expansion ===
        var recurringOccurrences = RecurringCapExExpander.Expand(
            effectiveProject.CapEx,
            effectiveProject.Property,
            startPeriod.Year,
            endPeriod.Year,
            currency);

        // Merge recurring occurrences into effective CapEx configuration
        var effectiveCapEx = recurringOccurrences.Count > 0
            ? RecurringCapExExpander.ExpandIntoConfiguration(
                effectiveProject.CapEx,
                effectiveProject.Property,
                startPeriod.Year,
                endPeriod.Year,
                currency)
            : effectiveProject.CapEx;

        // === 5b. Maßnahmen-Impacts (Mietanpassungen + Kosteneinsparungen) ===
        var (rentAdjustment, costSavings) = CashflowCalculator.CalculateMeasureImpacts(
            effectiveCapEx, grossRent, startPeriod, endPeriod, currency);

        // Apply rent adjustments to gross and effective rent (adjustment bears the same vacancy share)
        effectiveRent = CashflowCalculator.ApplyRentAdjustment(
            grossRent, effectiveRent, rentAdjustment, effectiveProject.Rent.VacancyRatePercent);
        grossRent = grossRent.Add(rentAdjustment);

        // Apply cost savings to operating costs (savings cannot push costs below zero)
        var adjustedOperatingCosts = new MoneyTimeSeries(startPeriod, endPeriod, currency);
        foreach (var period in adjustedOperatingCosts.Periods)
        {
            var adjusted = operatingCosts[period] - costSavings[period];
            adjustedOperatingCosts[period] = adjusted.Amount < 0 ? Money.Zero(currency) : adjusted;
        }
        operatingCosts = adjustedOperatingCosts;

        // === 6. NOI (Net Operating Income) ===
        // NOI = Effective Rent + umgelegte Nebenkosten (max. umlagefähige Kosten) - Operating Costs
        var noi = CashflowCalculator.CalculateNoi(
            effectiveRent,
            serviceChargeIncome,
            transferableCosts,
            operatingCosts,
            startPeriod,
            endPeriod,
            currency);

        // === 7. CapEx ===
        var capExPayments = CashflowCalculator.CalculateCapExPayments(
            effectiveCapEx,
            startPeriod,
            endPeriod);

        // === 8. Cashflow vor Steuern ===
        var cashflowBeforeTax = CashflowCalculator.CalculateCashflowBeforeTax(
            noi,
            aggregatedFinancing.TotalDebtService,
            capExPayments,
            startPeriod,
            endPeriod,
            currency);

        // === 9. Steuern ===
        // Steuerlich relevant sind nur durchgeführte Maßnahmen (inkl. expandierter wiederkehrender)
        var executedMeasures = effectiveCapEx?.Measures.Where(m => m.IsExecuted).ToList() ?? [];

        var maintenanceExpense = CalculateMaintenanceExpenseForTax(effectiveProject, executedMeasures, startPeriod, endPeriod);

        // Einnahmen (§21 EStG): tatsächlich vereinnahmte Kaltmiete + Umlagen
        var rentalIncome = effectiveRent.Add(serviceChargeIncome);

        // Werbungskosten: abzugsfähige Betriebskosten (umlagefähig + nicht umlagefähig)
        // + sofort abziehbare Kaufnebenkosten (z.B. Grundschuldbestellung)
        var otherDeductions = CalculateOtherTaxDeductions(
            effectiveProject.Costs, operatingCosts, effectiveProject.Purchase, livingArea, startPeriod, endPeriod, currency);

        var taxTimeSeries = _taxCalculator.CalculateTaxTimeSeries(
            effectiveProject,
            executedMeasures,
            rentalIncome,
            aggregatedFinancing.TotalInterest,
            maintenanceExpense,
            otherDeductions,
            startPeriod,
            endPeriod);

        // === 10. Cashflow nach Steuern ===
        var cashflowAfterTax = CashflowCalculator.CalculateCashflowAfterTax(
            cashflowBeforeTax,
            taxTimeSeries.TaxPayment,
            startPeriod,
            endPeriod,
            currency);

        // === 10. Kumulativer Cashflow ===
        var cumulativeCashflow = cashflowAfterTax.Cumulative();

        // === 11. Rücklagen ===
        var reserveBalance = CashflowCalculator.CalculateReserveBalance(
            effectiveProject.Costs.ReserveAccount,
            grossRent,
            livingArea,
            capExPayments,
            startPeriod,
            endPeriod,
            currency);

        // === 13. Objektwert ===
        var initialValue = effectiveProject.Purchase.PurchasePrice;
        var propertyValue = CashflowCalculator.CalculatePropertyValue(
            initialValue,
            effectiveProject.Valuation,
            effectiveCapEx,
            startPeriod,
            endPeriod);

        // === 13. Exit (geplanter Verkauf, sonst fiktiver Verkauf am Horizontende) ===
        var plannedSaleDate = effectiveProject.Valuation.PlannedSaleDate;
        var isSalePlanned = plannedSaleDate.HasValue
            && plannedSaleDate.Value >= startPeriod
            && plannedSaleDate.Value <= endPeriod;
        var exitPeriod = isSalePlanned ? plannedSaleDate!.Value : endPeriod;

        var salePrice = propertyValue[exitPeriod];
        var saleCosts = salePrice * effectiveProject.Valuation.SaleCostsPercent / 100;

        // Nur die bis zum Verkauf in Anspruch genommene AfA mindert die Anschaffungskosten
        var accumulatedDepreciation = taxTimeSeries.Depreciation
            .Where(p => p.Period <= exitPeriod)
            .Aggregate(Money.Zero(currency), (sum, p) => sum + p.Value);

        // Bis zum Verkauf aktivierte Maßnahmen erhöhen die Anschaffungskosten
        var capitalizedImprovements = _taxCalculator
            .GetCapitalizedMeasureCosts(effectiveProject.Purchase, executedMeasures)
            .Where(c => c.Period <= exitPeriod)
            .Aggregate(Money.Zero(currency), (sum, c) => sum + c.Amount);

        var capitalGainsResult = _taxCalculator.CalculateCapitalGainsTax(
            effectiveProject.Purchase,
            salePrice,
            saleCosts,
            accumulatedDepreciation,
            exitPeriod.ToFirstDayOfMonth(),
            effectiveProject.TaxProfile,
            effectiveProject.Valuation.CapitalGainsTax,
            capitalizedImprovements);

        // Dem Eigenkapital fließt der Erlös erst nach Ablösung der Restschuld zu
        var outstandingDebtAtExit = aggregatedFinancing.TotalOutstandingDebt[exitPeriod];
        var exitEquityProceeds = salePrice - saleCosts - capitalGainsResult.TaxAmount - outstandingDebtAtExit;

        // Steuerübersicht weist Veräußerungssteuer nur bei tatsächlich geplantem Verkauf aus
        var taxSummary = BuildTaxSummary(
            effectiveProject, executedMeasures, taxTimeSeries, rentalIncome, aggregatedFinancing.TotalInterest,
            maintenanceExpense, otherDeductions, isSalePlanned ? capitalGainsResult : null);

        // === 14. Kennzahlen ===
        var equity = effectiveProject.Financing.TotalEquity;

        // Verwende konfigurierbaren Diskontierungszins aus Valuation
        var discountRate = effectiveProject.Valuation.EffectiveDiscountRate;

        var metrics = MetricsCalculator.CalculateAllMetrics(
            equity,
            noi,
            effectiveRent,
            cashflowBeforeTax,
            cashflowAfterTax,
            aggregatedFinancing.TotalDebtService,
            aggregatedFinancing.TotalInterest,
            aggregatedFinancing.TotalOutstandingDebt,
            propertyValue,
            effectiveProject.Property,
            effectiveProject.CapEx,
            reserveBalance,
            exitEquityProceeds,
            exitPeriod,
            discountRate,
            startPeriod,
            endPeriod) with
        {
            ExitPeriod = exitPeriod,
            IsExitPlanned = isSalePlanned,
            ExitEquityProceeds = exitEquityProceeds
        };

        // === 16. Warnungen ===
        var warnings = GenerateWarnings(
            effectiveProject,
            effectiveCapEx,
            cashflowAfterTax,
            reserveBalance,
            noi,
            aggregatedFinancing.TotalDebtService,
            taxSummary,
            startPeriod,
            endPeriod);

        // === 16. Investor-Ergebnisse ===
        var investorResults = InvestorCalculator.CalculateInvestorResults(
            effectiveProject.Investors,
            effectiveProject.Financing,
            cashflowBeforeTax,
            exitEquityProceeds,
            exitPeriod,
            discountRate,
            startPeriod,
            endPeriod,
            currency);

        // === 17. Immobilienwert-Prognose (Multi-Szenario) ===
        var propertyValueForecast = PropertyValueForecastCalculator.Calculate(
            effectiveProject, startPeriod.Year, endPeriod.Year);

        // === 18. Exit-Analyse ===
        var exitAnalysis = ExitAnalysisCalculator.Calculate(
            effectiveProject,
            propertyValueForecast,
            cashflowAfterTax,
            grossRent,
            operatingCosts,
            aggregatedFinancing.TotalDebtService,
            capExPayments,
            taxTimeSeries.TaxPayment,
            aggregatedFinancing.TotalOutstandingDebt,
            reserveBalance,
            taxTimeSeries.Depreciation,
            _taxCalculator,
            exitPeriod,
            executedMeasures);

        // === 19. Jahresaggregation ===
        var result = new CalculationResult
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
            Warnings = warnings,
            InvestorResults = investorResults,

            // Neue Felder
            PropertyValueForecast = propertyValueForecast,
            ExitAnalysis = exitAnalysis,
            TotalEquityInvested = equity.Amount,
            TotalCashflowBeforeTax = cashflowBeforeTax.Sum().Amount,
            TotalCashflowAfterTax = cashflowAfterTax.Sum().Amount,
            CapExTimeline = YearlyAggregationService.BuildCapExTimeline(
                effectiveProject.CapEx, recurringOccurrences)
        };

        // Jahresaggregate als letzter Schritt (braucht das CalculationResult)
        result = result with
        {
            YearlyCashflows = YearlyAggregationService.AggregateToYearlyCashflowRows(
                result, grossRent, capExPayments, maintenanceExpense, propertyValue),
            TaxBridge = YearlyAggregationService.AggregateToTaxBridgeRows(
                result, rentalIncome, maintenanceExpense, otherDeductions)
        };

        return result;
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

        // Apply rent growth rate override to all tenancies
        if (parameters.RentGrowthRatePercent.HasValue)
        {
            var modifiedTenancies = project.Rent.Tenancies.Select(tenancy =>
            {
                // Only override for tenancies using Annual development model
                if (tenancy.DevelopmentModel == RentDevelopmentModel.Annual)
                {
                    return tenancy with
                    {
                        AnnualIncreasePercent = parameters.RentGrowthRatePercent.Value
                    };
                }
                return tenancy;
            }).ToArray();

            modifiedRent = modifiedRent with { Tenancies = modifiedTenancies };
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
    /// Berechnet steuerlich absetzbare Instandhaltungskosten (sofort oder nach §82b EStDV verteilt).
    /// Aktivierte Maßnahmen (Herstellungskosten, anschaffungsnahe HK) wirken über die AfA.
    /// </summary>
    private MoneyTimeSeries CalculateMaintenanceExpenseForTax(
        Project project,
        IReadOnlyList<CapExMeasure> executedMeasures,
        YearMonth startPeriod,
        YearMonth endPeriod)
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, project.Currency);

        if (executedMeasures.Count == 0)
            return result;

        // 15%-Regel auf Basis der durchgeführten Maßnahmen
        var acquisitionRelatedResult = _taxCalculator.CheckAcquisitionRelatedCosts(
            project.Purchase,
            executedMeasures);

        foreach (var measure in executedMeasures)
        {
            var classification = _taxCalculator.ClassifyMeasure(
                measure,
                acquisitionRelatedResult,
                project.Purchase.PurchaseDate);

            switch (classification)
            {
                case TaxClassification.MaintenanceExpense:
                    // Sofort absetzbar im Jahr der Durchführung (gezahlter Betrag inkl. Puffer)
                    DistributeOverYear(result, measure.PlannedPeriod.Year, measure.CostWithBuffer, startPeriod, endPeriod);
                    break;

                case TaxClassification.MaintenanceExpenseDistributed:
                    // §82b EStDV - Verteilung über 2-5 Jahre
                    var distributionYears = measure.DistributionYears ?? 5;
                    foreach (var deduction in _taxCalculator.CalculateMaintenanceDistribution(measure, distributionYears))
                    {
                        DistributeOverYear(result, deduction.Year, deduction.Amount, startPeriod, endPeriod);
                    }
                    break;

                // ManufacturingCosts und AcquisitionRelatedCosts werden aktiviert (AfA-Zeitreihe)
            }
        }

        return result;
    }

    private static void DistributeOverYear(MoneyTimeSeries series, int year, Money amount, YearMonth startPeriod, YearMonth endPeriod)
    {
        var months = Enumerable.Range(1, 12)
            .Select(m => new YearMonth(year, m))
            .Where(p => p >= startPeriod && p <= endPeriod)
            .ToList();

        if (months.Count == 0)
            return;

        var monthly = (amount / months.Count).Round();
        foreach (var period in months)
        {
            series[period] += monthly;
        }
    }

    /// <summary>
    /// Berechnet sonstige Werbungskosten:
    /// abzugsfähige Betriebskosten (umlagefähige und nicht umlagefähige, nach Maßnahmen-Einsparungen)
    /// + sofort abziehbare Kaufnebenkosten im Zahlungsmonat.
    /// </summary>
    private static MoneyTimeSeries CalculateOtherTaxDeductions(
        CostConfiguration costs,
        MoneyTimeSeries operatingCosts,
        Purchase purchase,
        decimal livingAreaSqm,
        YearMonth startPeriod,
        YearMonth endPeriod,
        string currency = "EUR")
    {
        var result = new MoneyTimeSeries(startPeriod, endPeriod, currency);

        foreach (var period in result.Periods)
        {
            var nonDeductible = Money.Zero(currency);
            var periodDate = period.ToFirstDayOfMonth();
            var yearsElapsed = period.Year - startPeriod.Year;

            foreach (var item in costs.Items.Where(i => !i.IsTaxDeductible))
            {
                if (item.StartDate.HasValue && periodDate < item.StartDate.Value)
                    continue;
                if (item.EndDate.HasValue && periodDate > item.EndDate.Value)
                    continue;

                var baseAmount = item.AmountPerSqmPerYear.HasValue
                    ? new Money(item.AmountPerSqmPerYear.Value * livingAreaSqm / 12, currency)
                    : item.MonthlyAmount;

                nonDeductible += baseAmount *
                    (decimal)Math.Pow(1 + (double)item.AnnualInflationPercent / 100, yearsElapsed);
            }

            result[period] = (operatingCosts[period] - nonDeductible).Round();
        }

        // Nicht aktivierungspflichtige Kaufnebenkosten (z.B. Grundschuld) sofort abziehbar
        var purchasePeriod = YearMonth.FromDate(purchase.PurchaseDate);
        foreach (var cost in purchase.AcquisitionCosts.Where(c => !c.IsCapitalizable))
        {
            var period = cost.PaymentDate.HasValue ? YearMonth.FromDate(cost.PaymentDate.Value) : purchasePeriod;
            if (period >= startPeriod && period <= endPeriod)
                result[period] += cost.Amount;
        }

        return result;
    }

    /// <summary>
    /// Erstellt die Steuer-Zusammenfassung
    /// </summary>
    private TaxSummary BuildTaxSummary(
        Project project,
        IReadOnlyList<CapExMeasure> executedMeasures,
        TaxTimeSeries taxTimeSeries,
        MoneyTimeSeries rentalIncome,
        MoneyTimeSeries interestExpense,
        MoneyTimeSeries maintenanceExpense,
        MoneyTimeSeries otherDeductions,
        CapitalGainsTaxResult? capitalGainsResult)
    {
        var acquisitionRelatedResult = _taxCalculator.CheckAcquisitionRelatedCosts(project.Purchase, executedMeasures);

        // Build maintenance distributions (nur Maßnahmen, die nicht durch die 15%-Regel aktiviert werden)
        var distributions = executedMeasures
            .Where(m => _taxCalculator.ClassifyMeasure(m, acquisitionRelatedResult, project.Purchase.PurchaseDate)
                        == TaxClassification.MaintenanceExpenseDistributed)
            .Select(m => new MaintenanceDistribution
            {
                MeasureId = m.Id,
                TotalAmount = m.CostWithBuffer,
                DistributionYears = m.DistributionYears ?? 5,
                StartYear = m.PlannedPeriod.Year
            })
            .ToList();

        // Build yearly summaries
        var yearlySummaries = new Dictionary<int, TaxYearSummary>();
        var startYear = project.StartPeriod.Year;
        var endYear = project.EndPeriod.Year;

        for (var year = startYear; year <= endYear; year++)
        {
            if (!rentalIncome.Periods.Any(p => p.Year == year))
                continue;

            yearlySummaries[year] = new TaxYearSummary
            {
                Year = year,
                GrossIncome = rentalIncome.SumForYear(year),
                Depreciation = taxTimeSeries.Depreciation.SumForYear(year),
                InterestExpense = interestExpense.SumForYear(year),
                MaintenanceExpense = maintenanceExpense.SumForYear(year),
                OtherDeductions = otherDeductions.SumForYear(year),
                TaxableIncome = taxTimeSeries.TaxableIncome.SumForYear(year),
                TaxPayment = taxTimeSeries.TaxPayment.SumForYear(year)
            };
        }

        var depreciationBasis = _taxCalculator.CalculateEffectiveDepreciationBase(project.Purchase, executedMeasures);
        var depreciationRate = TaxCalculator.GetDepreciationRate(project.Property, project.TaxProfile);
        var annualDepreciation = _taxCalculator.CalculateAnnualDepreciation(
            project.Purchase, project.Property, project.TaxProfile, executedMeasures);
        var effectiveTaxRate = project.TaxProfile.EffectiveTaxRatePercent;

        // Total deductions and tax savings
        var totalOperatingDeduction = otherDeductions.Sum();
        var totalDeductions = taxTimeSeries.Depreciation.Sum() + interestExpense.Sum() +
                              maintenanceExpense.Sum() + totalOperatingDeduction;
        var totalTaxSavings = (totalDeductions * effectiveTaxRate / 100).Round();

        return new TaxSummary
        {
            TotalDepreciation = taxTimeSeries.Depreciation.Sum(),
            TotalInterestDeduction = interestExpense.Sum(),
            TotalMaintenanceDeduction = maintenanceExpense.Sum(),
            TotalOperatingDeduction = totalOperatingDeduction,
            AcquisitionRelatedCostsTriggered = acquisitionRelatedResult.IsTriggered,
            AcquisitionRelatedCostsAmount = acquisitionRelatedResult.ActualCosts,
            MaintenanceDistributions = distributions,
            CapitalGainsTax = capitalGainsResult?.TaxAmount,
            SaleTaxExempt = capitalGainsResult?.IsTaxExempt ?? true,
            TotalTaxPayment = taxTimeSeries.TaxPayment.Sum() + (capitalGainsResult?.TaxAmount ?? Money.Zero()),
            YearlyTax = yearlySummaries,
            DepreciationBasis = depreciationBasis,
            AnnualDepreciation = annualDepreciation,
            DepreciationRatePercent = depreciationRate,
            EffectiveTaxRatePercent = effectiveTaxRate,
            TotalTaxSavings = totalTaxSavings
        };
    }

    /// <summary>
    /// Generiert Warnungen basierend auf den Berechnungsergebnissen
    /// </summary>
    private static IReadOnlyList<CalculationWarning> GenerateWarnings(
        Project project,
        CapExConfiguration? effectiveCapEx,
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

        // Zinsbindung endet im Analysezeitraum ohne Anschlusskondition → Sollzins wird fortgeschrieben
        foreach (var loan in project.Financing.Loans.Where(l => l.Refinancing == null && l.FixedInterestPeriodMonths > 0))
        {
            var fixedInterestEnd = YearMonth.FromDate(loan.DisbursementDate).AddMonths(loan.FixedInterestPeriodMonths);
            if (fixedInterestEnd < endPeriod)
            {
                warnings.Add(new CalculationWarning
                {
                    Type = WarningType.RefinancingAssumed,
                    Message = $"Zinsbindung von '{loan.Name}' endet {fixedInterestEnd}; ohne Anschlusszins wird mit {loan.InterestRatePercent:0.##} % weitergerechnet",
                    Severity = WarningSeverity.Warning,
                    Period = fixedInterestEnd,
                    RelatedField = "Financing"
                });
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

        // Check for deferred maintenance (use effectiveCapEx to include recurring expansions)
        var capExForWarnings = effectiveCapEx ?? project.CapEx;
        if (capExForWarnings != null)
        {
            var deferredMeasures = capExForWarnings.Measures
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
