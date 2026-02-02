import type { WizardStep } from '@/stores/uiStore';

export interface HelpEntry {
  id: string;
  step: WizardStep | 'summary' | 'glossary';
  category: 'input' | 'output' | 'glossary';
  i18nKey: string;
  relatedEntries?: string[];
}

export const helpCatalog: HelpEntry[] = [
  // ── ProjectStep ──────────────────────────────────────────
  { id: 'project.name', step: 'project', category: 'input', i18nKey: 'helpTexts.project.name' },
  { id: 'project.description', step: 'project', category: 'input', i18nKey: 'helpTexts.project.description' },
  { id: 'project.currency', step: 'project', category: 'input', i18nKey: 'helpTexts.project.currency' },
  { id: 'project.startDate', step: 'project', category: 'input', i18nKey: 'helpTexts.project.startDate' },
  { id: 'project.duration', step: 'project', category: 'input', i18nKey: 'helpTexts.project.duration', relatedEntries: ['summary.irrAfterTax', 'summary.npvAfterTax'] },
  { id: 'project.endDate', step: 'project', category: 'input', i18nKey: 'helpTexts.project.endDate' },

  // ── PropertyStep ─────────────────────────────────────────
  { id: 'property.type', step: 'property', category: 'input', i18nKey: 'helpTexts.property.type', relatedEntries: ['glossary.afa', 'tax.depreciation'] },
  { id: 'property.constructionYear', step: 'property', category: 'input', i18nKey: 'helpTexts.property.constructionYear', relatedEntries: ['glossary.afa', 'tax.depreciation'] },
  { id: 'property.condition', step: 'property', category: 'input', i18nKey: 'helpTexts.property.condition', relatedEntries: ['summary.property.conditionFactor'] },
  { id: 'property.totalArea', step: 'property', category: 'input', i18nKey: 'helpTexts.property.totalArea' },
  { id: 'property.livingArea', step: 'property', category: 'input', i18nKey: 'helpTexts.property.livingArea', relatedEntries: ['property.regionalPrice'] },
  { id: 'property.landArea', step: 'property', category: 'input', i18nKey: 'helpTexts.property.landArea', relatedEntries: ['purchase.landValue'] },
  { id: 'property.unitCount', step: 'property', category: 'input', i18nKey: 'helpTexts.property.unitCount' },
  { id: 'property.regionalPrice', step: 'property', category: 'input', i18nKey: 'helpTexts.property.regionalPrice', relatedEntries: ['summary.property.forecast'] },
  { id: 'weg.mea', step: 'property', category: 'input', i18nKey: 'helpTexts.weg.mea', relatedEntries: ['glossary.mea'] },
  { id: 'weg.totalMea', step: 'property', category: 'input', i18nKey: 'helpTexts.weg.totalMea' },
  { id: 'weg.reserveBalance', step: 'property', category: 'input', i18nKey: 'helpTexts.weg.reserveBalance' },
  { id: 'property.components', step: 'property', category: 'input', i18nKey: 'helpTexts.property.components', relatedEntries: ['summary.property.componentDeterioration'] },

  // ── PurchaseStep ─────────────────────────────────────────
  { id: 'purchase.price', step: 'purchase', category: 'input', i18nKey: 'helpTexts.purchase.price', relatedEntries: ['purchase.landValue', 'summary.irrAfterTax'] },
  { id: 'purchase.date', step: 'purchase', category: 'input', i18nKey: 'helpTexts.purchase.date', relatedEntries: ['summary.tax.rule15', 'tax.section23'] },
  { id: 'purchase.landValue', step: 'purchase', category: 'input', i18nKey: 'helpTexts.purchase.landValue', relatedEntries: ['glossary.afa', 'tax.depreciation'] },
  { id: 'purchase.costs', step: 'purchase', category: 'input', i18nKey: 'helpTexts.purchase.costs' },
  { id: 'purchase.totalInvestment', step: 'purchase', category: 'output', i18nKey: 'helpTexts.purchase.totalInvestment', relatedEntries: ['financing.totalCapitalNeed'] },

  // ── CapExStep ────────────────────────────────────────────
  { id: 'capex.category', step: 'capex', category: 'input', i18nKey: 'helpTexts.capex.category' },
  { id: 'capex.amount', step: 'capex', category: 'input', i18nKey: 'helpTexts.capex.amount', relatedEntries: ['financing.totalCapitalNeed'] },
  { id: 'capex.scheduledDate', step: 'capex', category: 'input', i18nKey: 'helpTexts.capex.scheduledDate', relatedEntries: ['summary.tax.rule15'] },
  { id: 'capex.taxClassification', step: 'capex', category: 'input', i18nKey: 'helpTexts.capex.taxClassification', relatedEntries: ['glossary.afa', 'tax.section82b'] },
  { id: 'capex.costSavings', step: 'capex', category: 'input', i18nKey: 'helpTexts.capex.costSavings', relatedEntries: ['summary.cf.noi'] },
  { id: 'capex.rentIncrease', step: 'capex', category: 'input', i18nKey: 'helpTexts.capex.rentIncrease' },
  { id: 'capex.rentIncreasePercent', step: 'capex', category: 'input', i18nKey: 'helpTexts.capex.rentIncreasePercent' },
  { id: 'capex.delayMonths', step: 'capex', category: 'input', i18nKey: 'helpTexts.capex.delayMonths' },
  { id: 'capex.recurringInterval', step: 'capex', category: 'input', i18nKey: 'helpTexts.capex.recurringInterval' },
  { id: 'capex.recurringCost', step: 'capex', category: 'input', i18nKey: 'helpTexts.capex.recurringCost' },
  { id: 'capex.cycleExtension', step: 'capex', category: 'input', i18nKey: 'helpTexts.capex.cycleExtension' },

  // ── FinancingStep ────────────────────────────────────────
  { id: 'financing.equity', step: 'financing', category: 'input', i18nKey: 'helpTexts.financing.equity', relatedEntries: ['summary.ltvInitial', 'summary.cashOnCash'] },
  { id: 'financing.principal', step: 'financing', category: 'input', i18nKey: 'helpTexts.financing.principal' },
  { id: 'financing.interestRate', step: 'financing', category: 'input', i18nKey: 'helpTexts.financing.interestRate', relatedEntries: ['summary.cf.beforeTax'] },
  { id: 'financing.initialRepayment', step: 'financing', category: 'input', i18nKey: 'helpTexts.financing.initialRepayment' },
  { id: 'financing.fixedInterestYears', step: 'financing', category: 'input', i18nKey: 'helpTexts.financing.fixedInterestYears' },
  { id: 'financing.totalCapitalNeed', step: 'financing', category: 'output', i18nKey: 'helpTexts.financing.totalCapitalNeed' },
  { id: 'financing.gap', step: 'financing', category: 'output', i18nKey: 'helpTexts.financing.gap' },

  // ── RentStep ─────────────────────────────────────────────
  { id: 'rent.monthlyRent', step: 'rent', category: 'input', i18nKey: 'helpTexts.rent.monthlyRent', relatedEntries: ['summary.cf.effectiveRent'] },
  { id: 'rent.serviceCharge', step: 'rent', category: 'input', i18nKey: 'helpTexts.rent.serviceCharge' },
  { id: 'rent.annualIncrease', step: 'rent', category: 'input', i18nKey: 'helpTexts.rent.annualIncrease' },
  { id: 'rent.vacancy', step: 'rent', category: 'input', i18nKey: 'helpTexts.rent.vacancy', relatedEntries: ['summary.cf.effectiveRent'] },
  { id: 'rent.rentLossReserve', step: 'rent', category: 'input', i18nKey: 'helpTexts.rent.rentLossReserve', relatedEntries: ['summary.riskLiquidity'] },

  // ── CostsStep ────────────────────────────────────────────
  { id: 'costs.maintenanceReserve', step: 'costs', category: 'input', i18nKey: 'helpTexts.costs.maintenanceReserve' },
  { id: 'costs.monthlyAmount', step: 'costs', category: 'input', i18nKey: 'helpTexts.costs.monthlyAmount', relatedEntries: ['summary.cf.noi'] },
  { id: 'costs.isTransferable', step: 'costs', category: 'input', i18nKey: 'helpTexts.costs.isTransferable' },

  // ── TaxStep ──────────────────────────────────────────────
  { id: 'tax.marginalRate', step: 'tax', category: 'input', i18nKey: 'helpTexts.tax.marginalRate', relatedEntries: ['summary.tax.effectiveRate'] },
  { id: 'tax.churchTax', step: 'tax', category: 'input', i18nKey: 'helpTexts.tax.churchTax' },
  { id: 'tax.isCorporate', step: 'tax', category: 'input', i18nKey: 'helpTexts.tax.isCorporate' },
  { id: 'tax.depreciation', step: 'tax', category: 'input', i18nKey: 'helpTexts.tax.depreciation', relatedEntries: ['glossary.afa', 'property.constructionYear'] },
  { id: 'tax.section82b', step: 'tax', category: 'input', i18nKey: 'helpTexts.tax.section82b' },
  { id: 'tax.distributionYears', step: 'tax', category: 'input', i18nKey: 'helpTexts.tax.distributionYears' },
  { id: 'tax.section23', step: 'tax', category: 'input', i18nKey: 'helpTexts.tax.section23' },

  // ── SummaryStep: Overview ────────────────────────────────
  { id: 'summary.irrAfterTax', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.irrAfterTax', relatedEntries: ['glossary.irr'] },
  { id: 'summary.irrBeforeTax', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.irrBeforeTax', relatedEntries: ['glossary.irr'] },
  { id: 'summary.npvAfterTax', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.npvAfterTax', relatedEntries: ['glossary.npv'] },
  { id: 'summary.cashOnCash', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.cashOnCash', relatedEntries: ['financing.equity'] },
  { id: 'summary.equityMultiple', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.equityMultiple' },
  { id: 'summary.roi', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.roi' },
  { id: 'summary.dscrMin', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.dscrMin', relatedEntries: ['glossary.dscr'] },
  { id: 'summary.dscrAvg', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.dscrAvg', relatedEntries: ['glossary.dscr'] },
  { id: 'summary.icrMin', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.icrMin' },
  { id: 'summary.ltvInitial', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.ltvInitial', relatedEntries: ['glossary.ltv'] },
  { id: 'summary.ltvFinal', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.ltvFinal', relatedEntries: ['glossary.ltv'] },
  { id: 'summary.breakEvenRent', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.breakEvenRent' },
  { id: 'summary.riskMaintenance', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.riskMaintenance' },
  { id: 'summary.riskLiquidity', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.riskLiquidity' },

  // ── SummaryStep: Cashflow ────────────────────────────────
  { id: 'summary.cf.effectiveRent', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.cf.effectiveRent' },
  { id: 'summary.cf.noi', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.cf.noi' },
  { id: 'summary.cf.beforeTax', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.cf.beforeTax' },
  { id: 'summary.cf.afterTax', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.cf.afterTax' },
  { id: 'summary.cf.cumulative', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.cf.cumulative' },

  // ── SummaryStep: Tax ─────────────────────────────────────
  { id: 'summary.tax.bridge', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.tax.bridge' },
  { id: 'summary.tax.effectiveRate', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.tax.effectiveRate', relatedEntries: ['tax.marginalRate'] },
  { id: 'summary.tax.rule15', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.tax.rule15' },
  { id: 'summary.tax.totalSavings', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.tax.totalSavings' },

  // ── SummaryStep: Property ────────────────────────────────
  { id: 'summary.property.forecast', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.property.forecast' },
  { id: 'summary.property.conditionFactor', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.property.conditionFactor', relatedEntries: ['property.condition'] },
  { id: 'summary.property.componentDeterioration', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.property.componentDeterioration', relatedEntries: ['property.components'] },
  { id: 'summary.exit.totalReturn', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.exit.totalReturn' },
  { id: 'summary.exit.annualizedReturn', step: 'summary', category: 'output', i18nKey: 'helpTexts.summary.exit.annualizedReturn' },

  // ── Glossar ──────────────────────────────────────────────
  { id: 'glossary.irr', step: 'glossary', category: 'glossary', i18nKey: 'helpTexts.glossary.irr', relatedEntries: ['summary.irrAfterTax', 'summary.irrBeforeTax'] },
  { id: 'glossary.npv', step: 'glossary', category: 'glossary', i18nKey: 'helpTexts.glossary.npv', relatedEntries: ['summary.npvAfterTax'] },
  { id: 'glossary.dscr', step: 'glossary', category: 'glossary', i18nKey: 'helpTexts.glossary.dscr', relatedEntries: ['summary.dscrMin', 'summary.dscrAvg'] },
  { id: 'glossary.ltv', step: 'glossary', category: 'glossary', i18nKey: 'helpTexts.glossary.ltv', relatedEntries: ['summary.ltvInitial', 'summary.ltvFinal'] },
  { id: 'glossary.afa', step: 'glossary', category: 'glossary', i18nKey: 'helpTexts.glossary.afa', relatedEntries: ['tax.depreciation', 'property.constructionYear'] },
  { id: 'glossary.mea', step: 'glossary', category: 'glossary', i18nKey: 'helpTexts.glossary.mea', relatedEntries: ['weg.mea'] },
];

export function getHelpEntry(helpKey: string): HelpEntry | undefined {
  return helpCatalog.find(e => e.id === helpKey);
}

export function getEntriesByStep(step: string): HelpEntry[] {
  return helpCatalog.filter(e => e.step === step);
}

export function getEntriesByCategory(category: HelpEntry['category']): HelpEntry[] {
  return helpCatalog.filter(e => e.category === category);
}

export function getAllEntries(): HelpEntry[] {
  return [...helpCatalog];
}
