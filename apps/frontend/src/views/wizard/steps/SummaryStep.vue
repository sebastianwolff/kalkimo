<template>
  <div class="summary-page">
    <!-- Project Overview -->
    <KalkCard :title="t('summary.projectOverview')">
      <div class="overview-grid">
        <div class="overview-item">
          <span class="label">{{ t('project.name') }}</span>
          <span class="value">{{ project?.name }}</span>
        </div>
        <div class="overview-item">
          <span class="label">{{ t('property.type') }}</span>
          <span class="value">{{ t(`property.types.${project?.property?.type || 'SingleFamily'}`) }}</span>
        </div>
        <div class="overview-item">
          <span class="label">{{ t('purchase.price') }}</span>
          <span class="value">{{ formatCurrency(project?.purchase.purchasePrice.amount || 0) }}</span>
        </div>
        <div class="overview-item">
          <span class="label">{{ t('purchase.totalInvestment') }}</span>
          <span class="value highlight">{{ formatCurrency(totalInvestment) }}</span>
        </div>
        <div class="overview-item">
          <span class="label">{{ t('financing.equity') }}</span>
          <span class="value">{{ formatCurrency(project?.financing.equity.amount || 0) }}</span>
        </div>
        <div class="overview-item">
          <span class="label">{{ t('summary.metrics.ltvInitial') }}</span>
          <span class="value">{{ formatPercent(ltv) }}</span>
        </div>
      </div>
    </KalkCard>

    <!-- Calculate Button (if no results yet) -->
    <KalkCard v-if="!calculationResult" :title="t('summary.keyMetrics')" class="metrics-card">
      <div class="calculate-prompt">
        <p>Berechnung starten um Kennzahlen zu sehen</p>
        <button type="button" class="btn btn-primary btn-lg" @click="calculate" :disabled="isCalculating">
          <svg v-if="!isCalculating" class="btn-icon" viewBox="0 0 20 20" fill="currentColor">
            <path d="M6 2a2 2 0 00-2 2v12a2 2 0 002 2h8a2 2 0 002-2V4a2 2 0 00-2-2H6zm1 2h6v1H7V4zm0 3h6v1H7V7zm0 3h3v1H7v-1z" />
          </svg>
          <span v-if="isCalculating" class="spinner"></span>
          {{ isCalculating ? 'Berechne...' : t('summary.calculate') }}
        </button>
      </div>
    </KalkCard>

    <!-- Results (after calculation) -->
    <template v-if="calculationResult">
      <!-- Warnings -->
      <div v-if="calculationResult.warnings.length > 0" class="warnings-section">
        <div
          v-for="(warning, idx) in calculationResult.warnings"
          :key="idx"
          class="warning-item"
          :class="warning.severity"
        >
          <svg class="warning-icon" viewBox="0 0 20 20" fill="currentColor">
            <path v-if="warning.severity === 'critical'" fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
            <path v-else fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
          </svg>
          <span>{{ warning.message }}</span>
        </div>
      </div>

      <!-- Key Metrics Dashboard -->
      <KalkCard :title="t('summary.returnMetrics')">
        <div class="metrics-grid">
          <div class="metric-item accent">
            <span class="metric-label">{{ t('summary.metrics.irrAfterTax') }}</span>
            <span class="metric-value">{{ formatPercent(calculationResult.metrics.irrAfterTaxPercent) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.irrBeforeTax') }}</span>
            <span class="metric-value">{{ formatPercent(calculationResult.metrics.irrBeforeTaxPercent) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.npvAfterTax') }}</span>
            <span class="metric-value" :class="{ positive: calculationResult.metrics.npvAfterTax.amount >= 0, negative: calculationResult.metrics.npvAfterTax.amount < 0 }">
              {{ formatCurrency(calculationResult.metrics.npvAfterTax.amount) }}
            </span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.cashOnCash') }}</span>
            <span class="metric-value">{{ formatPercent(calculationResult.metrics.cashOnCashPercent) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.equityMultiple') }}</span>
            <span class="metric-value">{{ calculationResult.metrics.equityMultiple.toFixed(2) }}x</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.roi') }}</span>
            <span class="metric-value">{{ formatPercent(calculationResult.metrics.roiPercent) }}</span>
          </div>
        </div>
      </KalkCard>

      <KalkCard :title="t('summary.bankMetrics')">
        <div class="metrics-grid compact">
          <div class="metric-item" :class="{ warn: calculationResult.metrics.dscrMin < 1.2 }">
            <span class="metric-label">{{ t('summary.metrics.dscrMin') }}</span>
            <span class="metric-value">{{ calculationResult.metrics.dscrMin.toFixed(2) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.dscrAvg') }}</span>
            <span class="metric-value">{{ calculationResult.metrics.dscrAvg.toFixed(2) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.ltvInitial') }}</span>
            <span class="metric-value">{{ formatPercent(calculationResult.metrics.ltvInitialPercent) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.ltvFinal') }}</span>
            <span class="metric-value">{{ formatPercent(calculationResult.metrics.ltvFinalPercent) }}</span>
          </div>
          <div class="metric-item span-2">
            <span class="metric-label">{{ t('summary.metrics.breakEvenRent') }}</span>
            <span class="metric-value">{{ formatCurrency(calculationResult.metrics.breakEvenRent.amount) }} {{ t('common.perMonth') }}</span>
          </div>
        </div>
      </KalkCard>

      <!-- Cashflow Table -->
      <KalkCard :title="t('summary.cashflowTable')">
        <div class="table-scroll">
          <table class="cashflow-table">
            <thead>
              <tr>
                <th class="sticky-col">{{ t('summary.cashflow.year') }}</th>
                <th>{{ t('summary.cashflow.effectiveRent') }}</th>
                <th>{{ t('summary.cashflow.operatingCosts') }}</th>
                <th>{{ t('summary.cashflow.noi') }}</th>
                <th>{{ t('summary.cashflow.debtService') }}</th>
                <th>{{ t('summary.cashflow.beforeTax') }}</th>
                <th>{{ t('summary.cashflow.tax') }}</th>
                <th class="highlight-col">{{ t('summary.cashflow.afterTax') }}</th>
                <th>{{ t('summary.cashflow.cumulative') }}</th>
                <th>{{ t('summary.cashflow.debt') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in calculationResult.yearlyCashflows" :key="row.year">
                <td class="sticky-col year-col">{{ row.year }}</td>
                <td>{{ formatCompact(row.effectiveRent) }}</td>
                <td class="negative">{{ formatCompact(-row.operatingCosts) }}</td>
                <td :class="{ positive: row.netOperatingIncome >= 0, negative: row.netOperatingIncome < 0 }">
                  {{ formatCompact(row.netOperatingIncome) }}
                </td>
                <td class="negative">{{ formatCompact(-row.debtService) }}</td>
                <td :class="{ positive: row.cashflowBeforeTax >= 0, negative: row.cashflowBeforeTax < 0 }">
                  {{ formatCompact(row.cashflowBeforeTax) }}
                </td>
                <td class="negative">{{ formatCompact(-row.taxPayment) }}</td>
                <td class="highlight-col" :class="{ positive: row.cashflowAfterTax >= 0, negative: row.cashflowAfterTax < 0 }">
                  <strong>{{ formatCompact(row.cashflowAfterTax) }}</strong>
                </td>
                <td :class="{ positive: row.cumulativeCashflow >= 0, negative: row.cumulativeCashflow < 0 }">
                  {{ formatCompact(row.cumulativeCashflow) }}
                </td>
                <td>{{ formatCompact(row.outstandingDebt) }}</td>
              </tr>
            </tbody>
            <tfoot>
              <tr>
                <td class="sticky-col"><strong>{{ t('common.total') }}</strong></td>
                <td>{{ formatCompact(sumColumn('effectiveRent')) }}</td>
                <td class="negative">{{ formatCompact(-sumColumn('operatingCosts')) }}</td>
                <td>{{ formatCompact(sumColumn('netOperatingIncome')) }}</td>
                <td class="negative">{{ formatCompact(-sumColumn('debtService')) }}</td>
                <td>{{ formatCompact(calculationResult.totalCashflowBeforeTax) }}</td>
                <td class="negative">{{ formatCompact(-sumColumn('taxPayment')) }}</td>
                <td class="highlight-col"><strong>{{ formatCompact(calculationResult.totalCashflowAfterTax) }}</strong></td>
                <td></td>
                <td></td>
              </tr>
            </tfoot>
          </table>
        </div>
      </KalkCard>

      <!-- Tax Summary -->
      <KalkCard :title="t('summary.taxSummary')">
        <div class="tax-grid">
          <div class="tax-row">
            <span>{{ t('summary.tax.depreciationRate') }}</span>
            <strong>{{ formatPercent(calculationResult.taxSummary.depreciationRatePercent) }}</strong>
          </div>
          <div class="tax-row">
            <span>{{ t('summary.tax.depreciationBasis') }}</span>
            <strong>{{ formatCurrency(calculationResult.taxSummary.depreciationBasis.amount) }}</strong>
          </div>
          <div class="tax-row">
            <span>{{ t('summary.tax.annualDepreciation') }}</span>
            <strong>{{ formatCurrency(calculationResult.taxSummary.annualDepreciation.amount) }}</strong>
          </div>
          <div class="tax-row separator">
            <span>{{ t('summary.tax.totalDepreciation') }}</span>
            <strong>{{ formatCurrency(calculationResult.taxSummary.totalDepreciation.amount) }}</strong>
          </div>
          <div class="tax-row">
            <span>{{ t('summary.tax.totalInterest') }}</span>
            <strong>{{ formatCurrency(calculationResult.taxSummary.totalInterestDeduction.amount) }}</strong>
          </div>
          <div class="tax-row">
            <span>{{ t('summary.tax.totalMaintenance') }}</span>
            <strong>{{ formatCurrency(calculationResult.taxSummary.totalMaintenanceDeduction.amount) }}</strong>
          </div>
          <div class="tax-row highlight">
            <span>{{ t('summary.tax.totalTax') }}</span>
            <strong>{{ formatCurrency(calculationResult.taxSummary.totalTaxPayment.amount) }}</strong>
          </div>
          <div class="tax-row" :class="{ 'rule-triggered': calculationResult.taxSummary.acquisitionRelatedCostsTriggered }">
            <span>{{ t('summary.tax.rule15') }}</span>
            <strong>
              {{ calculationResult.taxSummary.acquisitionRelatedCostsTriggered
                ? t('summary.tax.rule15Triggered')
                : t('summary.tax.rule15NotTriggered')
              }}
            </strong>
          </div>
        </div>
      </KalkCard>

      <!-- Totals -->
      <div class="totals-section">
        <div class="total-item">
          <span>{{ t('summary.totals.totalEquity') }}</span>
          <strong>{{ formatCurrency(calculationResult.totalEquityInvested) }}</strong>
        </div>
        <div class="total-item">
          <span>{{ t('summary.totals.totalCashflowBefore') }}</span>
          <strong :class="{ positive: calculationResult.totalCashflowBeforeTax >= 0, negative: calculationResult.totalCashflowBeforeTax < 0 }">
            {{ formatCurrency(calculationResult.totalCashflowBeforeTax) }}
          </strong>
        </div>
        <div class="total-item accent">
          <span>{{ t('summary.totals.totalCashflowAfter') }}</span>
          <strong :class="{ positive: calculationResult.totalCashflowAfterTax >= 0, negative: calculationResult.totalCashflowAfterTax < 0 }">
            {{ formatCurrency(calculationResult.totalCashflowAfterTax) }}
          </strong>
        </div>
      </div>

      <!-- Actions -->
      <div class="actions">
        <button type="button" class="btn btn-outline" @click="calculate">
          <svg class="btn-icon" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z" clip-rule="evenodd" />
          </svg>
          {{ t('summary.recalculate') }}
        </button>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { KalkCard } from '@/components';
import { useProjectStore } from '@/stores/projectStore';
import { useUiStore } from '@/stores/uiStore';
import { useAuthStore } from '@/stores/authStore';
import { projectsApi } from '@/api';
import { calculateProject } from '@/services/calculationService';
import type { CalculationResult, YearlyCashflowRow } from '@/stores/types';

const emit = defineEmits<{
  'validation-change': [valid: boolean];
}>();

const { t, locale } = useI18n();
const projectStore = useProjectStore();
const uiStore = useUiStore();
const authStore = useAuthStore();

const isCalculating = ref(false);
const calculationResult = ref<CalculationResult | null>(null);

const project = computed(() => projectStore.currentProject);
const currency = computed(() => project.value?.currency || 'EUR');

const totalInvestment = computed(() => {
  if (!project.value) return 0;
  return projectStore.totalCapitalRequirement?.amount || 0;
});

const ltv = computed(() => {
  if (!project.value || totalInvestment.value === 0) return 0;
  const totalLoans = project.value.financing.loans.reduce((sum, l) => sum + l.principal.amount, 0);
  return (totalLoans / totalInvestment.value) * 100;
});

function formatCurrency(value: number): string {
  return new Intl.NumberFormat(locale.value, {
    style: 'currency',
    currency: currency.value,
    maximumFractionDigits: 0
  }).format(value);
}

function formatCompact(value: number): string {
  if (Math.abs(value) >= 1000000) {
    return new Intl.NumberFormat(locale.value, {
      style: 'currency',
      currency: currency.value,
      notation: 'compact',
      maximumFractionDigits: 1
    }).format(value);
  }
  return new Intl.NumberFormat(locale.value, {
    style: 'currency',
    currency: currency.value,
    maximumFractionDigits: 0
  }).format(value);
}

function formatPercent(value: number): string {
  return new Intl.NumberFormat(locale.value, {
    style: 'percent',
    minimumFractionDigits: 1,
    maximumFractionDigits: 2
  }).format(value / 100);
}

function sumColumn(key: keyof YearlyCashflowRow): number {
  if (!calculationResult.value) return 0;
  return calculationResult.value.yearlyCashflows.reduce(
    (sum, row) => sum + (row[key] as number),
    0
  );
}

async function calculate() {
  if (!project.value) return;

  isCalculating.value = true;

  try {
    // Try backend API first (authenticated users)
    if (authStore.isAuthenticated) {
      try {
        const result = await projectsApi.calculate(project.value.id);
        calculationResult.value = result;
        projectStore.setCalculationResult(result);
        return;
      } catch {
        // Backend unavailable, fall through to local calculation
      }
    }

    // Local frontend calculation (guests or backend unavailable)
    const result = calculateProject(project.value);
    calculationResult.value = result;
    projectStore.setCalculationResult(result);
  } catch (error) {
    uiStore.showToast('Berechnung fehlgeschlagen', 'error');
  } finally {
    isCalculating.value = false;
  }
}

// Always valid on summary
emit('validation-change', true);
</script>

<style scoped>
.summary-page {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-6);
}

/* Overview Grid */
.overview-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--kalk-space-4);
}

@media (max-width: 600px) {
  .overview-grid {
    grid-template-columns: 1fr;
  }
}

.overview-item {
  display: flex;
  flex-direction: column;
  padding: var(--kalk-space-4);
  background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-md);
}

.overview-item .label {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-500);
  margin-bottom: var(--kalk-space-1);
}

.overview-item .value {
  font-size: var(--kalk-text-lg);
  font-weight: 600;
  color: var(--kalk-gray-900);
  font-variant-numeric: tabular-nums;
}

.overview-item .value.highlight {
  color: var(--kalk-accent-600);
}

/* Calculate Prompt */
.calculate-prompt {
  text-align: center;
  padding: var(--kalk-space-8);
}

.calculate-prompt p {
  color: var(--kalk-gray-500);
  margin-bottom: var(--kalk-space-6);
}

/* Buttons */
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--kalk-space-2);
  font-family: var(--kalk-font-family);
  font-weight: 600;
  border-radius: var(--kalk-radius-md);
  border: none;
  cursor: pointer;
  transition: all 0.15s;
}

.btn-lg {
  height: 48px;
  padding: 0 var(--kalk-space-8);
  font-size: var(--kalk-text-base);
}

.btn-primary {
  background: var(--kalk-accent-500);
  color: #ffffff;
}

.btn-primary:hover {
  background: var(--kalk-accent-600);
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-outline {
  background: #ffffff;
  color: var(--kalk-gray-700);
  border: 1.5px solid var(--kalk-gray-300);
  height: 40px;
  padding: 0 var(--kalk-space-5);
  font-size: var(--kalk-text-sm);
}

.btn-outline:hover {
  background: var(--kalk-gray-50);
  border-color: var(--kalk-gray-400);
}

.btn-icon {
  width: 18px;
  height: 18px;
}

.spinner {
  width: 18px;
  height: 18px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: #ffffff;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Warnings */
.warnings-section {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-2);
}

.warning-item {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  padding: var(--kalk-space-3) var(--kalk-space-4);
  border-radius: var(--kalk-radius-md);
  font-size: var(--kalk-text-sm);
}

.warning-item.info {
  background: var(--kalk-info-bg, #eff6ff);
  color: var(--kalk-info, #1d4ed8);
}

.warning-item.warning {
  background: var(--kalk-warning-bg, #fffbeb);
  color: var(--kalk-warning, #b45309);
}

.warning-item.critical {
  background: var(--kalk-error-bg, #fef2f2);
  color: var(--kalk-error, #dc2626);
}

.warning-icon {
  width: 20px;
  height: 20px;
  flex-shrink: 0;
}

/* Metrics Grid */
.metrics-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--kalk-space-4);
}

.metrics-grid.compact {
  grid-template-columns: repeat(2, 1fr);
}

@media (max-width: 600px) {
  .metrics-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .metrics-grid.compact {
    grid-template-columns: 1fr;
  }
}

.metric-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: var(--kalk-space-4) var(--kalk-space-3);
  background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-md);
  text-align: center;
}

.metric-item.accent {
  background: var(--kalk-accent-50, #f0f9ff);
  border: 1px solid var(--kalk-accent-200, #bae6fd);
}

.metric-item.warn {
  background: var(--kalk-warning-bg, #fffbeb);
  border: 1px solid var(--kalk-warning, #f59e0b);
}

.metric-item.span-2 {
  grid-column: span 2;
}

.metric-label {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
  margin-bottom: var(--kalk-space-1);
}

.metric-value {
  font-size: var(--kalk-text-xl);
  font-weight: 700;
  color: var(--kalk-gray-900);
  font-variant-numeric: tabular-nums;
}

/* Cashflow Table */
.table-scroll {
  overflow-x: auto;
  -webkit-overflow-scrolling: touch;
  margin: 0 calc(-1 * var(--kalk-space-4));
  padding: 0 var(--kalk-space-4);
}

.cashflow-table {
  width: 100%;
  min-width: 800px;
  border-collapse: collapse;
  font-size: var(--kalk-text-xs);
  font-variant-numeric: tabular-nums;
}

.cashflow-table th,
.cashflow-table td {
  padding: var(--kalk-space-2) var(--kalk-space-3);
  text-align: right;
  white-space: nowrap;
  border-bottom: 1px solid var(--kalk-gray-100);
}

.cashflow-table th {
  background: var(--kalk-gray-50);
  color: var(--kalk-gray-600);
  font-weight: 600;
  position: sticky;
  top: 0;
  z-index: 1;
}

.cashflow-table .sticky-col {
  text-align: left;
  position: sticky;
  left: 0;
  background: #ffffff;
  z-index: 2;
}

.cashflow-table th.sticky-col {
  background: var(--kalk-gray-50);
  z-index: 3;
}

.cashflow-table .year-col {
  font-weight: 600;
  color: var(--kalk-gray-900);
}

.cashflow-table .highlight-col {
  background: var(--kalk-accent-50, #f0f9ff);
}

.cashflow-table th.highlight-col {
  background: var(--kalk-accent-100, #e0f2fe);
}

.cashflow-table tbody tr:hover {
  background: var(--kalk-gray-50);
}

.cashflow-table tbody tr:hover .sticky-col {
  background: var(--kalk-gray-50);
}

.cashflow-table tfoot {
  border-top: 2px solid var(--kalk-gray-300);
}

.cashflow-table tfoot td {
  background: var(--kalk-gray-50);
  font-weight: 600;
}

.cashflow-table tfoot .sticky-col {
  background: var(--kalk-gray-50);
}

/* Color coding */
.positive {
  color: var(--kalk-success, #16a34a);
}

.negative {
  color: var(--kalk-error, #dc2626);
}

/* Tax Summary */
.tax-grid {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-2);
}

.tax-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--kalk-space-3) var(--kalk-space-4);
  border-radius: var(--kalk-radius-sm);
  font-size: var(--kalk-text-sm);
}

.tax-row span {
  color: var(--kalk-gray-600);
}

.tax-row strong {
  font-variant-numeric: tabular-nums;
  color: var(--kalk-gray-900);
}

.tax-row.separator {
  border-top: 1px solid var(--kalk-gray-200);
  margin-top: var(--kalk-space-2);
  padding-top: var(--kalk-space-4);
}

.tax-row.highlight {
  background: var(--kalk-gray-50);
  font-weight: 600;
}

.tax-row.rule-triggered {
  background: var(--kalk-warning-bg, #fffbeb);
}

.tax-row.rule-triggered strong {
  color: var(--kalk-warning, #b45309);
}

/* Totals */
.totals-section {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--kalk-space-4);
}

@media (max-width: 600px) {
  .totals-section {
    grid-template-columns: 1fr;
  }
}

.total-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: var(--kalk-space-5);
  background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-md);
  text-align: center;
}

.total-item.accent {
  background: var(--kalk-primary-900);
  color: #ffffff;
}

.total-item span {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
  margin-bottom: var(--kalk-space-2);
}

.total-item.accent span {
  color: rgba(255, 255, 255, 0.8);
}

.total-item strong {
  font-size: var(--kalk-text-lg);
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}

.total-item.accent strong {
  color: #ffffff;
}

/* Actions */
.actions {
  display: flex;
  justify-content: center;
  gap: var(--kalk-space-4);
}
</style>
