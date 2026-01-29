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

function ymEquals(a: YearMonth, b: YearMonth): boolean {
  return a.year === b.year && a.month === b.month;
}

function ymInRange(ym: YearMonth, start: YearMonth, end: YearMonth): boolean {
  const m = ymToMonths(ym);
  return m >= ymToMonths(start) && m <= ymToMonths(end);
}

function money(amount: number, currency: string): Money {
  return { amount, currency };
}

// === AfA (Depreciation) ===

function getAfaRate(constructionYear: number, override?: number): number {
  if (override && override > 0) return override;
  if (constructionYear >= 2023) return 3.0;   // §7 Abs. 4 Nr. 2a EStG
  if (constructionYear >= 1925) return 2.0;   // §7 Abs. 4 Nr. 2 EStG
  return 2.5;                                  // §7 Abs. 4 Nr. 1 EStG (pre-1925)
}

function getDepreciationBasis(project: Project): number {
  const purchasePrice = project.purchase.purchasePrice.amount;

  // Add acquisition costs
  const acquisitionCosts = project.purchase.costs
    .filter(c => c.taxClassification === 'AcquisitionCost')
    .reduce((sum, c) => {
      const mode = (c as any).mode as AmountMode | undefined;
      if (mode === 'percent') {
        return sum + (purchasePrice * c.amount.amount / 100);
      }
      return sum + c.amount.amount;
    }, 0);

  const totalBasis = purchasePrice + acquisitionCosts;

  // Subtract land value (not depreciable)
  const landPercent = project.purchase.landValuePercent || 0;
  return totalBasis * (1 - landPercent / 100);
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

    // Loan hasn't started yet
    if (ymToMonths(period) < ymToMonths(loan.startDate)) {
      schedule.set(periodKey, {
        interest: 0,
        principal: 0,
        totalPayment: 0,
        remainingBalance: loan.principal.amount
      });
      continue;
    }

    if (balance <= 0) {
      schedule.set(periodKey, {
        interest: 0,
        principal: 0,
        totalPayment: 0,
        remainingBalance: 0
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

  // Sum maintenance CapEx within first 3 years
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

// === Main Calculation ===

export function calculateProject(project: Project): CalculationResult {
  const currency = project.currency;
  const start = project.startPeriod;
  const end = project.endPeriod;
  const totalMonths = monthsBetween(start, end);

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

  // Determine which years we cover
  const startYear = start.year;
  const endYear = end.year;

  // Monthly rent calculation helper
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

  // Monthly operating costs
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

  // Build yearly cashflow rows
  const yearlyCashflows: YearlyCashflowRow[] = [];
  let cumulativeCF = 0;
  let totalDebtServiceAll = 0;
  let totalInterestAll = 0;
  let totalDepreciationAll = 0;
  let totalMaintenanceDeductionAll = 0;
  let totalTaxAll = 0;
  let totalCFBeforeTax = 0;
  let totalCFAfterTax = 0;
  const dscrValues: number[] = [];

  for (let year = startYear; year <= endYear; year++) {
    const yearsFromStart = year - startYear;

    // Determine months in this year within project range
    const yearStart = year === startYear ? start.month : 1;
    const yearEnd = year === endYear ? end.month : 12;
    const monthsInYear = yearEnd - yearStart + 1;

    // Rent with annual increase
    const rentMultiplier = Math.pow(1 + avgRentIncrease / 100, yearsFromStart);
    const monthlyRent = totalMonthlyRent * rentMultiplier;
    const grossRent = monthlyRent * monthsInYear;
    const vacancyLoss = grossRent * vacancyRate;
    const effectiveRent = grossRent - vacancyLoss;

    // Operating costs with increase
    const costMultiplier = Math.pow(1 + avgCostIncrease / 100, yearsFromStart);
    const operatingCosts = totalMonthlyCosts * costMultiplier * monthsInYear;

    const noi = effectiveRent - operatingCosts;

    // Debt service for this year
    let yearDebtService = 0;
    let yearInterest = 0;
    let yearEndBalance = 0;
    for (let month = yearStart; month <= yearEnd; month++) {
      const periodKey = `${year}-${month}`;
      for (const schedule of loanSchedules) {
        const payment = schedule.get(periodKey);
        if (payment) {
          yearDebtService += payment.totalPayment;
          yearInterest += payment.interest;
          yearEndBalance += payment.remainingBalance;
        }
      }
    }
    // Average remaining balance across loans at year end
    const lastMonthKey = `${year}-${yearEnd}`;
    let outstandingDebt = 0;
    for (const schedule of loanSchedules) {
      const payment = schedule.get(lastMonthKey);
      if (payment) outstandingDebt += payment.remainingBalance;
    }

    // CapEx payments in this year
    let capexPayments = 0;
    for (let month = yearStart; month <= yearEnd; month++) {
      const key = `${year}-${month}`;
      capexPayments += capexByMonth.get(key) || 0;
    }

    const cashflowBeforeTax = noi - yearDebtService - capexPayments;

    // Tax calculation
    const depreciation = monthlyDepreciation * monthsInYear;

    // Maintenance deduction: immediate or §82b distributed
    let maintenanceDeduction = 0;
    if (project.taxProfile.use82bDistribution) {
      maintenanceDeduction = maintenanceDistributions.get(year) || 0;
    } else {
      // Immediate deduction for maintenance capex in this year
      for (const measure of project.capex) {
        if (
          measure.taxClassification === 'MaintenanceExpense' &&
          measure.scheduledDate.year === year
        ) {
          maintenanceDeduction += measure.amount.amount;
        }
      }
    }

    // If 15% rule triggered, maintenance becomes acquisition cost (not deductible as maintenance)
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
    totalTaxAll += taxPayment;
    totalCFBeforeTax += cashflowBeforeTax;
    totalCFAfterTax += cashflowAfterTax;

    // DSCR
    if (yearDebtService > 0) {
      dscrValues.push(noi / yearDebtService);
    }

    yearlyCashflows.push({
      year,
      grossRent,
      vacancyLoss,
      effectiveRent,
      operatingCosts,
      netOperatingIncome: noi,
      debtService: yearDebtService,
      capexPayments,
      cashflowBeforeTax,
      depreciation,
      interestDeduction: yearInterest,
      maintenanceDeduction,
      taxableIncome,
      taxPayment,
      cashflowAfterTax,
      cumulativeCashflow: cumulativeCF,
      outstandingDebt
    });
  }

  // === Compute Metrics ===
  const equity = project.financing.equity.amount;
  const totalLoans = project.financing.loans.reduce((s, l) => s + l.principal.amount, 0);
  const purchaseTotal = project.purchase.purchasePrice.amount
    + project.purchase.costs.reduce((sum, c) => {
      const mode = (c as any).mode as AmountMode | undefined;
      if (mode === 'percent') {
        return sum + (project.purchase.purchasePrice.amount * c.amount.amount / 100);
      }
      return sum + c.amount.amount;
    }, 0);
  const totalCapex = project.capex.reduce((s, m) => s + m.amount.amount, 0);
  const totalCapitalRequirement = purchaseTotal + totalCapex;

  // Cash-on-Cash (first full year)
  const firstFullYear = yearlyCashflows.find(r => r.year === startYear + 1) || yearlyCashflows[0];
  const cashOnCash = equity > 0 ? (firstFullYear?.cashflowAfterTax || 0) / equity * 100 : 0;

  // Equity Multiple
  const equityMultiple = equity > 0 ? (equity + totalCFAfterTax) / equity : 0;

  // DSCR
  const dscrMin = dscrValues.length > 0 ? Math.min(...dscrValues) : 0;
  const dscrAvg = dscrValues.length > 0 ? dscrValues.reduce((a, b) => a + b, 0) / dscrValues.length : 0;

  // LTV
  const ltvInitial = totalCapitalRequirement > 0 ? totalLoans / totalCapitalRequirement * 100 : 0;
  const lastRow = yearlyCashflows[yearlyCashflows.length - 1];
  const ltvFinal = totalCapitalRequirement > 0 && lastRow
    ? lastRow.outstandingDebt / totalCapitalRequirement * 100
    : 0;

  // Break-even rent (monthly): costs + debt service = rent needed
  const firstYearRow = yearlyCashflows[0];
  const monthsFirstYear = start.month <= 12 ? (startYear === endYear ? end.month - start.month + 1 : 12 - start.month + 1) : 12;
  const breakEvenMonthly = firstYearRow
    ? (firstYearRow.operatingCosts + firstYearRow.debtService) / monthsFirstYear
    : 0;

  // ROI
  const roi = equity > 0 ? totalCFAfterTax / equity / (endYear - startYear + 1) * 100 : 0;

  // IRR calculation (before and after tax)
  const annualCFBeforeTax = yearlyCashflows.map(r => r.cashflowBeforeTax);
  const annualCFAfterTax = yearlyCashflows.map(r => r.cashflowAfterTax);
  const irrBeforeTax = calculateIRR([-totalCapitalRequirement, ...annualCFBeforeTax]);
  const irrAfterTax = calculateIRR([-totalCapitalRequirement, ...annualCFAfterTax]);

  // NPV at 5% discount rate
  const discountRate = 0.05;
  const npvBeforeTax = calculateNPV(discountRate, [-totalCapitalRequirement, ...annualCFBeforeTax]);
  const npvAfterTax = calculateNPV(discountRate, [-totalCapitalRequirement, ...annualCFAfterTax]);

  const metrics: InvestmentMetrics = {
    irrBeforeTaxPercent: irrBeforeTax * 100,
    irrAfterTaxPercent: irrAfterTax * 100,
    npvBeforeTax: money(npvBeforeTax, currency),
    npvAfterTax: money(npvAfterTax, currency),
    cashOnCashPercent: cashOnCash,
    equityMultiple,
    dscrMin,
    dscrAvg,
    ltvInitialPercent: ltvInitial,
    ltvFinalPercent: ltvFinal,
    breakEvenRent: money(breakEvenMonthly, currency),
    roiPercent: roi
  };

  const taxSummary: TaxSummary = {
    totalDepreciation: money(totalDepreciationAll, currency),
    totalInterestDeduction: money(totalInterestAll, currency),
    totalMaintenanceDeduction: money(totalMaintenanceDeductionAll, currency),
    acquisitionRelatedCostsTriggered: rule15.triggered,
    acquisitionRelatedCostsAmount: money(rule15.amount, currency),
    totalTaxPayment: money(totalTaxAll, currency),
    annualDepreciation: money(annualDepreciation, currency),
    depreciationRatePercent: afaRate,
    depreciationBasis: money(depreciationBasis, currency)
  };

  // Warnings
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

  return {
    projectId: project.id,
    calculatedAt: new Date().toISOString(),
    metrics,
    yearlyCashflows,
    taxSummary,
    warnings,
    totalEquityInvested: equity,
    totalCashflowBeforeTax: totalCFBeforeTax,
    totalCashflowAfterTax: totalCFAfterTax
  };
}

// === IRR (Newton-Raphson) ===

function calculateIRR(cashflows: number[], maxIterations = 100, tolerance = 1e-7): number {
  // Initial guess
  let rate = 0.1;

  for (let i = 0; i < maxIterations; i++) {
    let npv = 0;
    let dnpv = 0; // derivative

    for (let t = 0; t < cashflows.length; t++) {
      const discountFactor = Math.pow(1 + rate, t);
      npv += cashflows[t] / discountFactor;
      if (t > 0) {
        dnpv -= t * cashflows[t] / Math.pow(1 + rate, t + 1);
      }
    }

    if (Math.abs(npv) < tolerance) return rate;
    if (Math.abs(dnpv) < 1e-10) break; // avoid division by zero

    const newRate = rate - npv / dnpv;

    // Guard against divergence
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
