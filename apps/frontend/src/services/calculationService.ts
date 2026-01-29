/**
 * Frontend Calculation Service
 *
 * Pure-function calculation engine that computes investment metrics
 * from project data entirely client-side. Mirrors the backend
 * Kalkimo.Domain.Calculators logic in simplified form.
 */
import type {
  Project,
  CalculationResult,
  YearlyCashflowRow,
  TaxBridgeRow,
  CapExTimelineItem,
  InvestmentMetrics,
  TaxSummary,
  CalculationWarning,
  Money,
  Loan,
  YearMonth,
  AmountMode
} from '@/stores/types';

// === Helper functions ===

function ymToMonths(ym: YearMonth): number {
  return ym.year * 12 + ym.month;
}

function monthsBetween(start: YearMonth, end: YearMonth): number {
  return ymToMonths(end) - ymToMonths(start) + 1;
}

function addMonths(ym: YearMonth, months: number): YearMonth {
  const total = ym.year * 12 + (ym.month - 1) + months;
  return { year: Math.floor(total / 12), month: (total % 12) + 1 };
}

function ymInRange(ym: YearMonth, start: YearMonth, end: YearMonth): boolean {
  const m = ymToMonths(ym);
  return m >= ymToMonths(start) && m <= ymToMonths(end);
}

function money(amount: number, currency: string): Money {
  return { amount, currency };
}

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

// === AfA (Depreciation) ===

function getAfaRate(constructionYear: number, override?: number): number {
  if (override && override > 0) return override;
  if (constructionYear >= 2023) return 3.0;   // §7 Abs. 4 Nr. 2a EStG
  if (constructionYear >= 1925) return 2.0;   // §7 Abs. 4 Nr. 2 EStG
  return 2.5;                                  // §7 Abs. 4 Nr. 1 EStG (pre-1925)
}

function getPurchaseCostAmount(purchasePrice: number, cost: { amount: { amount: number }; mode?: AmountMode }): number {
  const mode = (cost as any).mode as AmountMode | undefined;
  if (mode === 'percent') {
    return purchasePrice * cost.amount.amount / 100;
  }
  return cost.amount.amount;
}

function getDepreciationBasis(project: Project): number {
  const purchasePrice = project.purchase.purchasePrice.amount;

  const acquisitionCosts = project.purchase.costs
    .filter(c => c.taxClassification === 'AcquisitionCost')
    .reduce((sum, c) => sum + getPurchaseCostAmount(purchasePrice, c), 0);

  const totalBasis = purchasePrice + acquisitionCosts;
  const landPercent = project.purchase.landValuePercent || 0;
  return totalBasis * (1 - landPercent / 100);
}

function getTotalPurchaseCosts(project: Project): number {
  const purchasePrice = project.purchase.purchasePrice.amount;
  return project.purchase.costs.reduce(
    (sum, c) => sum + getPurchaseCostAmount(purchasePrice, c), 0
  );
}

// === Financing ===

interface MonthlyLoanPayment {
  interest: number;
  principal: number;
  totalPayment: number;
  remainingBalance: number;
}

function calculateLoanSchedule(
  loan: Loan,
  projectStart: YearMonth,
  projectEnd: YearMonth
): Map<string, MonthlyLoanPayment> {
  const schedule = new Map<string, MonthlyLoanPayment>();
  const monthlyRate = loan.interestRatePercent / 100 / 12;
  const monthlyAnnuity = (loan.interestRatePercent / 100 + loan.initialRepaymentPercent / 100)
    * loan.principal.amount / 12;

  let balance = loan.principal.amount;
  const totalMonths = monthsBetween(projectStart, projectEnd);

  for (let m = 0; m < totalMonths; m++) {
    const period = addMonths(projectStart, m);
    const periodKey = `${period.year}-${period.month}`;

    if (ymToMonths(period) < ymToMonths(loan.startDate)) {
      schedule.set(periodKey, {
        interest: 0, principal: 0, totalPayment: 0,
        remainingBalance: loan.principal.amount
      });
      continue;
    }

    if (balance <= 0) {
      schedule.set(periodKey, {
        interest: 0, principal: 0, totalPayment: 0, remainingBalance: 0
      });
      continue;
    }

    const interest = balance * monthlyRate;
    let principalPayment = monthlyAnnuity - interest;
    if (principalPayment > balance) principalPayment = balance;
    balance -= principalPayment;

    schedule.set(periodKey, {
      interest,
      principal: principalPayment,
      totalPayment: interest + principalPayment,
      remainingBalance: Math.max(0, balance)
    });
  }

  return schedule;
}

// === 15% Rule (Anschaffungsnahe Herstellungskosten) ===

function check15PercentRule(project: Project): { triggered: boolean; amount: number } {
  const purchaseDate = project.purchase.purchaseDate;
  const threeYearsLater = addMonths(purchaseDate, 36);
  const buildingValue = getDepreciationBasis(project);

  const maintenanceInPeriod = project.capex
    .filter(m =>
      m.taxClassification === 'MaintenanceExpense' &&
      ymInRange(m.scheduledDate, purchaseDate, threeYearsLater)
    )
    .reduce((sum, m) => sum + m.amount.amount, 0);

  const threshold = buildingValue * 0.15;
  return {
    triggered: maintenanceInPeriod > threshold,
    amount: maintenanceInPeriod
  };
}

// === Risk Scores ===

function calculateMaintenanceRiskScore(project: Project): number {
  if (project.property.components.length === 0) {
    // No component data: estimate from building age
    const age = new Date().getFullYear() - project.property.constructionYear;
    if (age < 10) return 10;
    if (age < 20) return 25;
    if (age < 30) return 40;
    if (age < 50) return 60;
    return 80;
  }

  let totalRisk = 0;
  for (const comp of project.property.components) {
    const lastReno = comp.lastRenovationYear || project.property.constructionYear;
    const age = new Date().getFullYear() - lastReno;
    const overdue = Math.max(0, age - comp.expectedCycleYears);
    const componentRisk = clamp((overdue / comp.expectedCycleYears) * 100, 0, 100);
    totalRisk += componentRisk;
  }
  return Math.round(clamp(totalRisk / project.property.components.length, 0, 100));
}

function calculateLiquidityRiskScore(
  yearlyCashflows: YearlyCashflowRow[],
  dscrMin: number
): number {
  let score = 0;

  // Negative cashflow years increase risk
  const negativeYears = yearlyCashflows.filter(r => r.cashflowAfterTax < 0).length;
  score += (negativeYears / yearlyCashflows.length) * 40;

  // Low DSCR increases risk
  if (dscrMin < 1.0) score += 40;
  else if (dscrMin < 1.2) score += 25;
  else if (dscrMin < 1.5) score += 10;

  // High LTV increases risk
  const initialLtv = yearlyCashflows[0]?.ltvPercent || 0;
  if (initialLtv > 90) score += 20;
  else if (initialLtv > 80) score += 10;

  return Math.round(clamp(score, 0, 100));
}

// === Main Calculation ===

export function calculateProject(project: Project): CalculationResult {
  const currency = project.currency;
  const start = project.startPeriod;
  const end = project.endPeriod;

  // Pre-compute loan schedules
  const loanSchedules = project.financing.loans.map(loan =>
    calculateLoanSchedule(loan, start, end)
  );

  // Pre-compute AfA
  const afaRate = getAfaRate(
    project.property.constructionYear,
    project.taxProfile.depreciationRatePercent
  );
  const depreciationBasis = getDepreciationBasis(project);
  const annualDepreciation = depreciationBasis * afaRate / 100;
  const monthlyDepreciation = annualDepreciation / 12;

  // 15% rule check
  const rule15 = check15PercentRule(project);

  // Determine year range
  const startYear = start.year;
  const endYear = end.year;

  // Rent base values
  const totalMonthlyRent = project.rent.units.reduce(
    (sum, u) => sum + u.monthlyRent.amount, 0
  );
  const totalMonthlyServiceCharge = project.rent.units.reduce(
    (sum, u) => sum + u.monthlyServiceCharge.amount, 0
  );
  const avgRentIncrease = project.rent.units.length > 0
    ? project.rent.units.reduce((sum, u) => sum + u.annualRentIncreasePercent, 0) / project.rent.units.length
    : 0;
  const vacancyRate = project.rent.vacancyRatePercent / 100;

  // Cost base values
  const totalMonthlyCosts = project.costs.items.reduce(
    (sum, c) => sum + c.monthlyAmount.amount, 0
  );
  const avgCostIncrease = project.costs.items.length > 0
    ? project.costs.items.reduce((sum, c) => sum + c.annualIncreasePercent, 0) / project.costs.items.length
    : 0;

  // CapEx by year-month
  const capexByMonth = new Map<string, number>();
  for (const measure of project.capex) {
    const key = `${measure.scheduledDate.year}-${measure.scheduledDate.month}`;
    capexByMonth.set(key, (capexByMonth.get(key) || 0) + measure.amount.amount);
  }

  // Pre-compute measure impacts: for each measure with impact, determine the
  // effective start month (scheduledDate + delayMonths) and monthly savings/increases
  interface ActiveImpact {
    effectiveStart: YearMonth;
    costSavingsMonthly: number;
    rentIncreaseMonthly: number;
    rentIncreasePercent: number;
  }
  const activeImpacts: ActiveImpact[] = [];
  for (const measure of project.capex) {
    if (!measure.impact) continue;
    const imp = measure.impact;
    const hasSavings = (imp.costSavingsMonthly?.amount ?? 0) > 0;
    const hasRentAbs = (imp.rentIncreaseMonthly?.amount ?? 0) > 0;
    const hasRentPct = (imp.rentIncreasePercent ?? 0) > 0;
    if (!hasSavings && !hasRentAbs && !hasRentPct) continue;

    const delay = imp.delayMonths ?? 0;
    const effectiveStart = addMonths(measure.scheduledDate, delay);
    activeImpacts.push({
      effectiveStart,
      costSavingsMonthly: imp.costSavingsMonthly?.amount ?? 0,
      rentIncreaseMonthly: imp.rentIncreaseMonthly?.amount ?? 0,
      rentIncreasePercent: imp.rentIncreasePercent ?? 0,
    });
  }

  // §82b distributions by year
  const maintenanceDistributions = new Map<number, number>();
  if (project.taxProfile.use82bDistribution) {
    for (const measure of project.capex) {
      if (measure.taxClassification === 'MaintenanceExpense' && measure.distributionYears) {
        const annualDeduction = measure.amount.amount / measure.distributionYears;
        for (let y = 0; y < measure.distributionYears; y++) {
          const year = measure.scheduledDate.year + y;
          maintenanceDistributions.set(
            year,
            (maintenanceDistributions.get(year) || 0) + annualDeduction
          );
        }
      }
    }
  }

  // === Build yearly rows ===
  const yearlyCashflows: YearlyCashflowRow[] = [];
  const taxBridge: TaxBridgeRow[] = [];
  let cumulativeCF = 0;
  let totalDebtServiceAll = 0;
  let totalInterestAll = 0;
  let totalDepreciationAll = 0;
  let totalMaintenanceDeductionAll = 0;
  let totalOperatingDeductionAll = 0;
  let totalTaxAll = 0;
  let totalCFBeforeTax = 0;
  let totalCFAfterTax = 0;
  let totalEffectiveRent = 0;
  const dscrValues: number[] = [];
  const icrValues: number[] = [];

  // Capital requirement
  const equity = project.financing.equity.amount;
  const totalLoans = project.financing.loans.reduce((s, l) => s + l.principal.amount, 0);
  const purchaseCosts = getTotalPurchaseCosts(project);
  const purchaseTotal = project.purchase.purchasePrice.amount + purchaseCosts;
  const totalCapexAmount = project.capex.reduce((s, m) => s + m.amount.amount, 0);
  const totalCapitalRequirement = purchaseTotal + totalCapexAmount;

  for (let year = startYear; year <= endYear; year++) {
    const yearsFromStart = year - startYear;
    const yearStart = year === startYear ? start.month : 1;
    const yearEnd = year === endYear ? end.month : 12;
    const monthsInYear = yearEnd - yearStart + 1;

    // Rent with annual increase
    const rentMultiplier = Math.pow(1 + avgRentIncrease / 100, yearsFromStart);
    const monthlyRent = totalMonthlyRent * rentMultiplier;
    const grossRent = monthlyRent * monthsInYear;
    const vacancyLoss = grossRent * vacancyRate;
    let effectiveRent = grossRent - vacancyLoss;
    const serviceChargeIncome = totalMonthlyServiceCharge * rentMultiplier * monthsInYear * (1 - vacancyRate);

    // Operating costs with increase
    const costMultiplier = Math.pow(1 + avgCostIncrease / 100, yearsFromStart);
    let operatingCosts = totalMonthlyCosts * costMultiplier * monthsInYear;

    // Apply measure impacts: cost savings reduce operating costs,
    // rent increases boost effective rent (per active month in this year)
    for (const impact of activeImpacts) {
      // Count how many months this impact is active within this year
      let activeMonths = 0;
      for (let month = yearStart; month <= yearEnd; month++) {
        const periodM = ymToMonths({ year, month });
        const effectiveM = ymToMonths(impact.effectiveStart);
        if (periodM >= effectiveM) activeMonths++;
      }
      if (activeMonths === 0) continue;

      // Cost savings reduce operating costs
      if (impact.costSavingsMonthly > 0) {
        operatingCosts -= impact.costSavingsMonthly * activeMonths;
      }

      // Rent increase (absolute) boosts effective rent
      if (impact.rentIncreaseMonthly > 0) {
        effectiveRent += impact.rentIncreaseMonthly * activeMonths * (1 - vacancyRate);
      }

      // Rent increase (relative) boosts effective rent based on base monthly rent
      if (impact.rentIncreasePercent > 0) {
        effectiveRent += monthlyRent * (impact.rentIncreasePercent / 100) * activeMonths * (1 - vacancyRate);
      }
    }

    // Ensure operating costs don't go negative
    operatingCosts = Math.max(0, operatingCosts);

    const noi = effectiveRent - operatingCosts;

    // Debt service for this year
    let yearDebtService = 0;
    let yearInterest = 0;
    let yearPrincipal = 0;
    for (let month = yearStart; month <= yearEnd; month++) {
      const periodKey = `${year}-${month}`;
      for (const schedule of loanSchedules) {
        const payment = schedule.get(periodKey);
        if (payment) {
          yearDebtService += payment.totalPayment;
          yearInterest += payment.interest;
          yearPrincipal += payment.principal;
        }
      }
    }

    // Outstanding debt at year end
    const lastMonthKey = `${year}-${yearEnd}`;
    let outstandingDebt = 0;
    for (const schedule of loanSchedules) {
      const payment = schedule.get(lastMonthKey);
      if (payment) outstandingDebt += payment.remainingBalance;
    }

    // LTV for this year
    const ltvPercent = totalCapitalRequirement > 0
      ? (outstandingDebt / totalCapitalRequirement) * 100
      : 0;

    // DSCR and ICR for this year
    const dscrYear = yearDebtService > 0 ? noi / yearDebtService : 0;
    const icrYear = yearInterest > 0 ? noi / yearInterest : 0;

    // CapEx payments in this year
    let capexPayments = 0;
    for (let month = yearStart; month <= yearEnd; month++) {
      const key = `${year}-${month}`;
      capexPayments += capexByMonth.get(key) || 0;
    }

    const cashflowBeforeTax = noi - yearDebtService - capexPayments;

    // Tax calculation
    const depreciation = monthlyDepreciation * monthsInYear;

    // Maintenance deduction
    let maintenanceDeduction = 0;
    if (project.taxProfile.use82bDistribution) {
      maintenanceDeduction = maintenanceDistributions.get(year) || 0;
    } else {
      for (const measure of project.capex) {
        if (
          measure.taxClassification === 'MaintenanceExpense' &&
          measure.scheduledDate.year === year
        ) {
          maintenanceDeduction += measure.amount.amount;
        }
      }
    }

    // 15% rule: maintenance reclassified as acquisition cost
    if (rule15.triggered) {
      maintenanceDeduction = 0;
    }

    const taxableIncome = effectiveRent - operatingCosts - depreciation - yearInterest - maintenanceDeduction;
    const taxPayment = taxableIncome > 0
      ? taxableIncome * project.taxProfile.marginalTaxRatePercent / 100
      : 0;

    const cashflowAfterTax = cashflowBeforeTax - taxPayment;
    cumulativeCF += cashflowAfterTax;

    // Track totals
    totalDebtServiceAll += yearDebtService;
    totalInterestAll += yearInterest;
    totalDepreciationAll += depreciation;
    totalMaintenanceDeductionAll += maintenanceDeduction;
    totalOperatingDeductionAll += operatingCosts;
    totalTaxAll += taxPayment;
    totalCFBeforeTax += cashflowBeforeTax;
    totalCFAfterTax += cashflowAfterTax;
    totalEffectiveRent += effectiveRent;

    if (yearDebtService > 0) dscrValues.push(dscrYear);
    if (yearInterest > 0) icrValues.push(icrYear);

    yearlyCashflows.push({
      year,
      grossRent,
      vacancyLoss,
      effectiveRent,
      serviceChargeIncome,
      operatingCosts,
      netOperatingIncome: noi,
      debtService: yearDebtService,
      interestPortion: yearInterest,
      principalPortion: yearPrincipal,
      capexPayments,
      cashflowBeforeTax,
      depreciation,
      interestDeduction: yearInterest,
      maintenanceDeduction,
      taxableIncome,
      taxPayment,
      cashflowAfterTax,
      cumulativeCashflow: cumulativeCF,
      outstandingDebt,
      ltvPercent,
      dscrYear,
      icrYear
    });

    // Tax bridge row
    taxBridge.push({
      year,
      grossIncome: effectiveRent,
      depreciation,
      interestExpense: yearInterest,
      maintenanceExpense: maintenanceDeduction,
      operatingExpenses: operatingCosts,
      taxableIncome,
      taxPayment
    });
  }

  // === CapEx Timeline ===
  const capexTimeline: CapExTimelineItem[] = project.capex.map(m => ({
    id: m.id,
    name: m.name,
    category: m.category,
    year: m.scheduledDate.year,
    month: m.scheduledDate.month,
    amount: m.amount.amount,
    taxClassification: m.taxClassification,
    distributionYears: m.distributionYears
  }));

  // === Compute Metrics ===
  const dscrMin = dscrValues.length > 0 ? Math.min(...dscrValues) : 0;
  const dscrAvg = dscrValues.length > 0 ? dscrValues.reduce((a, b) => a + b, 0) / dscrValues.length : 0;
  const icrMin = icrValues.length > 0 ? Math.min(...icrValues) : 0;

  const firstFullYear = yearlyCashflows.find(r => r.year === startYear + 1) || yearlyCashflows[0];
  const cashOnCash = equity > 0 ? (firstFullYear?.cashflowAfterTax || 0) / equity * 100 : 0;
  const equityMultiple = equity > 0 ? (equity + totalCFAfterTax) / equity : 0;

  const ltvInitial = totalCapitalRequirement > 0 ? totalLoans / totalCapitalRequirement * 100 : 0;
  const lastRow = yearlyCashflows[yearlyCashflows.length - 1];
  const ltvFinal = totalCapitalRequirement > 0 && lastRow
    ? lastRow.outstandingDebt / totalCapitalRequirement * 100
    : 0;

  const monthsFirstYear = startYear === endYear
    ? end.month - start.month + 1
    : 12 - start.month + 1;
  const firstYearRow = yearlyCashflows[0];
  const breakEvenMonthly = firstYearRow
    ? (firstYearRow.operatingCosts + firstYearRow.debtService) / monthsFirstYear
    : 0;

  const totalYears = endYear - startYear + 1;
  const roi = equity > 0 ? totalCFAfterTax / equity / totalYears * 100 : 0;

  const annualCFBeforeTax = yearlyCashflows.map(r => r.cashflowBeforeTax);
  const annualCFAfterTax = yearlyCashflows.map(r => r.cashflowAfterTax);
  const irrBeforeTax = calculateIRR([-totalCapitalRequirement, ...annualCFBeforeTax]);
  const irrAfterTax = calculateIRR([-totalCapitalRequirement, ...annualCFAfterTax]);

  const discountRate = 0.05;
  const npvBeforeTax = calculateNPV(discountRate, [-totalCapitalRequirement, ...annualCFBeforeTax]);
  const npvAfterTax = calculateNPV(discountRate, [-totalCapitalRequirement, ...annualCFAfterTax]);

  // Risk scores
  const maintenanceRiskScore = calculateMaintenanceRiskScore(project);

  const metrics: InvestmentMetrics = {
    irrBeforeTaxPercent: irrBeforeTax * 100,
    irrAfterTaxPercent: irrAfterTax * 100,
    npvBeforeTax: money(npvBeforeTax, currency),
    npvAfterTax: money(npvAfterTax, currency),
    cashOnCashPercent: cashOnCash,
    equityMultiple,
    dscrMin,
    dscrAvg,
    icrMin,
    ltvInitialPercent: ltvInitial,
    ltvFinalPercent: ltvFinal,
    breakEvenRent: money(breakEvenMonthly, currency),
    roiPercent: roi,
    maintenanceRiskScore,
    liquidityRiskScore: 0 // calculated after cashflows
  };

  // Liquidity risk needs cashflow data
  metrics.liquidityRiskScore = calculateLiquidityRiskScore(yearlyCashflows, dscrMin);

  // Tax summary
  const totalTaxSavings = totalDepreciationAll + totalInterestAll + totalMaintenanceDeductionAll + totalOperatingDeductionAll;
  const effectiveTaxRate = totalEffectiveRent > 0
    ? (totalTaxAll / totalEffectiveRent) * 100
    : 0;

  const taxSummary: TaxSummary = {
    totalDepreciation: money(totalDepreciationAll, currency),
    totalInterestDeduction: money(totalInterestAll, currency),
    totalMaintenanceDeduction: money(totalMaintenanceDeductionAll, currency),
    totalOperatingDeduction: money(totalOperatingDeductionAll, currency),
    acquisitionRelatedCostsTriggered: rule15.triggered,
    acquisitionRelatedCostsAmount: money(rule15.amount, currency),
    totalTaxPayment: money(totalTaxAll, currency),
    totalTaxSavings: money(totalTaxSavings * project.taxProfile.marginalTaxRatePercent / 100, currency),
    annualDepreciation: money(annualDepreciation, currency),
    depreciationRatePercent: afaRate,
    depreciationBasis: money(depreciationBasis, currency),
    effectiveTaxRatePercent: effectiveTaxRate
  };

  // === Warnings ===
  const warnings: CalculationWarning[] = [];

  if (dscrMin > 0 && dscrMin < 1.0) {
    warnings.push({
      type: 'DscrBelowOne',
      message: 'DSCR unter 1,0 – Mieteinnahmen decken den Schuldendienst nicht vollständig',
      severity: 'critical'
    });
  }

  if (ltvInitial > 80) {
    warnings.push({
      type: 'HighLtv',
      message: `Hoher Beleihungsauslauf (${ltvInitial.toFixed(0)}%) – erhöhtes Risiko`,
      severity: 'warning'
    });
  }

  if (rule15.triggered) {
    warnings.push({
      type: 'AcquisitionRelatedCostsTriggered',
      message: '15%-Regel ausgelöst – Erhaltungsaufwand wird als Anschaffungskosten behandelt',
      severity: 'warning'
    });
  }

  const negativeYears = yearlyCashflows.filter(r => r.cashflowAfterTax < 0);
  if (negativeYears.length > 0) {
    warnings.push({
      type: 'NegativeCashflow',
      message: `Negativer Cashflow in ${negativeYears.length} Jahr(en)`,
      severity: negativeYears.length > 3 ? 'critical' : 'warning'
    });
  }

  if (equity < 0) {
    warnings.push({
      type: 'LiquidityShortfall',
      message: 'Eigenkapital ist negativ – Finanzierung reicht nicht aus',
      severity: 'critical'
    });
  }

  if (icrMin > 0 && icrMin < 1.5) {
    warnings.push({
      type: 'LowIcr',
      message: `Niedrige Zinsdeckung (ICR min. ${icrMin.toFixed(2)})`,
      severity: icrMin < 1.0 ? 'critical' : 'warning'
    });
  }

  if (maintenanceRiskScore > 60) {
    warnings.push({
      type: 'DeferredMaintenance',
      message: `Erhöhtes Sanierungsrisiko (Score: ${maintenanceRiskScore}/100)`,
      severity: maintenanceRiskScore > 80 ? 'critical' : 'warning'
    });
  }

  return {
    projectId: project.id,
    calculatedAt: new Date().toISOString(),
    metrics,
    yearlyCashflows,
    taxBridge,
    capexTimeline,
    taxSummary,
    warnings,
    totalEquityInvested: equity,
    totalCashflowBeforeTax: totalCFBeforeTax,
    totalCashflowAfterTax: totalCFAfterTax
  };
}

// === IRR (Newton-Raphson) ===

function calculateIRR(cashflows: number[], maxIterations = 100, tolerance = 1e-7): number {
  let rate = 0.1;

  for (let i = 0; i < maxIterations; i++) {
    let npv = 0;
    let dnpv = 0;

    for (let t = 0; t < cashflows.length; t++) {
      const discountFactor = Math.pow(1 + rate, t);
      npv += cashflows[t] / discountFactor;
      if (t > 0) {
        dnpv -= t * cashflows[t] / Math.pow(1 + rate, t + 1);
      }
    }

    if (Math.abs(npv) < tolerance) return rate;
    if (Math.abs(dnpv) < 1e-10) break;

    const newRate = rate - npv / dnpv;

    if (newRate < -0.99) rate = -0.5;
    else if (newRate > 10) rate = 5;
    else rate = newRate;
  }

  return rate;
}

// === NPV ===

function calculateNPV(discountRate: number, cashflows: number[]): number {
  return cashflows.reduce((npv, cf, t) => {
    return npv + cf / Math.pow(1 + discountRate, t);
  }, 0);
}
