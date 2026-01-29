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
          <span class="value">{{ fmtCur(project?.purchase.purchasePrice.amount || 0) }}</span>
        </div>
        <div class="overview-item">
          <span class="label">{{ t('purchase.totalInvestment') }}</span>
          <span class="value highlight">{{ fmtCur(totalInvestment) }}</span>
        </div>
        <div class="overview-item">
          <span class="label">{{ t('financing.equity') }}</span>
          <span class="value">{{ fmtCur(project?.financing.equity.amount || 0) }}</span>
        </div>
        <div class="overview-item">
          <span class="label">{{ t('summary.metrics.ltvInitial') }}</span>
          <span class="value">{{ fmtPct(ltv) }}</span>
        </div>
      </div>
    </KalkCard>

    <!-- Calculate Button -->
    <KalkCard v-if="!result" :title="t('summary.keyMetrics')" class="metrics-card">
      <div class="calculate-prompt">
        <p>Berechnung starten um Kennzahlen zu sehen</p>
        <button type="button" class="btn btn-primary btn-lg" @click="calculate" :disabled="isCalculating">
          <span v-if="isCalculating" class="spinner"></span>
          {{ isCalculating ? 'Berechne...' : t('summary.calculate') }}
        </button>
      </div>
    </KalkCard>

    <!-- ===== RESULTS ===== -->
    <template v-if="result">

      <!-- Warnings -->
      <div v-if="result.warnings.length > 0" class="warnings-section">
        <div
          v-for="(w, i) in result.warnings"
          :key="i"
          class="warning-item"
          :class="w.severity"
        >
          <svg class="warning-icon" viewBox="0 0 20 20" fill="currentColor">
            <path v-if="w.severity === 'critical'" fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
            <path v-else fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
          </svg>
          <span>{{ w.message }}</span>
        </div>
      </div>

      <!-- ===== RETURN METRICS ===== -->
      <KalkCard :title="t('summary.returnMetrics')">
        <div class="metrics-grid">
          <div class="metric-item accent">
            <span class="metric-label">{{ t('summary.metrics.irrAfterTax') }}</span>
            <span class="metric-value">{{ fmtPct(result.metrics.irrAfterTaxPercent) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.irrBeforeTax') }}</span>
            <span class="metric-value">{{ fmtPct(result.metrics.irrBeforeTaxPercent) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.npvAfterTax') }}</span>
            <span class="metric-value" :class="npvClass(result.metrics.npvAfterTax.amount)">
              {{ fmtCur(result.metrics.npvAfterTax.amount) }}
            </span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.cashOnCash') }}</span>
            <span class="metric-value">{{ fmtPct(result.metrics.cashOnCashPercent) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.equityMultiple') }}</span>
            <span class="metric-value">{{ result.metrics.equityMultiple.toFixed(2) }}x</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.roi') }}</span>
            <span class="metric-value">{{ fmtPct(result.metrics.roiPercent) }}</span>
          </div>
        </div>
      </KalkCard>

      <!-- ===== BANK METRICS ===== -->
      <KalkCard :title="t('summary.bankMetrics')">
        <div class="metrics-grid compact">
          <div class="metric-item" :class="{ warn: result.metrics.dscrMin < 1.2 }">
            <span class="metric-label">{{ t('summary.metrics.dscrMin') }}</span>
            <span class="metric-value">{{ result.metrics.dscrMin.toFixed(2) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.dscrAvg') }}</span>
            <span class="metric-value">{{ result.metrics.dscrAvg.toFixed(2) }}</span>
          </div>
          <div class="metric-item" :class="{ warn: result.metrics.icrMin < 1.5 }">
            <span class="metric-label">{{ t('summary.metrics.icrMin') }}</span>
            <span class="metric-value">{{ result.metrics.icrMin.toFixed(2) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.ltvInitial') }}</span>
            <span class="metric-value">{{ fmtPct(result.metrics.ltvInitialPercent) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.ltvFinal') }}</span>
            <span class="metric-value">{{ fmtPct(result.metrics.ltvFinalPercent) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.breakEvenRent') }}</span>
            <span class="metric-value">{{ fmtCur(result.metrics.breakEvenRent.amount) }}/M</span>
          </div>
        </div>
      </KalkCard>

      <!-- ===== CASHFLOW CHART ===== -->
      <KalkCard :title="t('summary.cashflowChart')">
        <div class="chart-container">
          <div class="bar-chart">
            <div v-for="row in result.yearlyCashflows" :key="row.year" class="bar-group">
              <div class="bar-stack">
                <div class="bar bar-noi" :style="{ height: barHeight(row.netOperatingIncome) }" :title="`NOI: ${fmtCur(row.netOperatingIncome)}`"></div>
                <div class="bar bar-debt" :style="{ height: barHeight(row.debtService) }" :title="`Schuldendienst: ${fmtCur(row.debtService)}`"></div>
                <div class="bar bar-tax" :style="{ height: barHeight(row.taxPayment) }" :title="`Steuern: ${fmtCur(row.taxPayment)}`"></div>
              </div>
              <div class="bar-cf-marker" :class="{ negative: row.cashflowAfterTax < 0 }" :style="{ bottom: cfMarkerPos(row.cashflowAfterTax) }" :title="`CF n.St.: ${fmtCur(row.cashflowAfterTax)}`"></div>
              <span class="bar-label">{{ row.year.toString().slice(-2) }}</span>
            </div>
          </div>
          <div class="chart-legend">
            <span class="legend-item"><span class="swatch swatch-noi"></span> NOI</span>
            <span class="legend-item"><span class="swatch swatch-debt"></span> {{ t('summary.cashflow.debtService') }}</span>
            <span class="legend-item"><span class="swatch swatch-tax"></span> {{ t('summary.cashflow.tax') }}</span>
            <span class="legend-item"><span class="swatch swatch-cf"></span> CF n. St.</span>
          </div>
        </div>
      </KalkCard>

      <!-- ===== FINANCING CHART ===== -->
      <KalkCard :title="t('summary.financingChart')">
        <div class="chart-container">
          <div class="financing-chart">
            <div v-for="row in result.yearlyCashflows" :key="row.year" class="fin-bar-group">
              <div class="fin-bar" :style="{ height: debtBarHeight(row.outstandingDebt) }" :title="`Restschuld: ${fmtCur(row.outstandingDebt)} | LTV: ${fmtPct(row.ltvPercent)}`">
                <span class="fin-ltv-label">{{ Math.round(row.ltvPercent) }}%</span>
              </div>
              <span class="bar-label">{{ row.year.toString().slice(-2) }}</span>
            </div>
          </div>
          <div class="chart-legend">
            <span class="legend-item"><span class="swatch swatch-debt-bar"></span> {{ t('summary.cashflow.debt') }}</span>
            <span class="legend-item">% = LTV</span>
          </div>
        </div>
      </KalkCard>

      <!-- ===== STEUER-BRIDGE ===== -->
      <KalkCard :title="t('summary.taxBridge')">
        <div class="tax-bridge">
          <div v-for="row in result.taxBridge" :key="row.year" class="bridge-year">
            <div class="bridge-year-label">{{ row.year }}</div>
            <div class="bridge-bars">
              <div class="bridge-bar income" :style="{ width: bridgeWidth(row.grossIncome) }" :title="fmtCur(row.grossIncome)"></div>
              <div class="bridge-bar afa" :style="{ width: bridgeWidth(row.depreciation) }" :title="`AfA: -${fmtCur(row.depreciation)}`"></div>
              <div class="bridge-bar interest" :style="{ width: bridgeWidth(row.interestExpense) }" :title="`Zinsen: -${fmtCur(row.interestExpense)}`"></div>
              <div class="bridge-bar operating" :style="{ width: bridgeWidth(row.operatingExpenses) }" :title="`BK: -${fmtCur(row.operatingExpenses)}`"></div>
              <div v-if="row.maintenanceExpense > 0" class="bridge-bar maintenance" :style="{ width: bridgeWidth(row.maintenanceExpense) }" :title="`Erhalt: -${fmtCur(row.maintenanceExpense)}`"></div>
            </div>
            <div class="bridge-result" :class="{ negative: row.taxableIncome < 0 }">
              {{ fmtCompact(row.taxableIncome) }} &rarr; {{ fmtCompact(row.taxPayment) }}
            </div>
          </div>
        </div>
        <div class="chart-legend bridge-legend">
          <span class="legend-item"><span class="swatch swatch-income"></span> {{ t('summary.tax.bridge.income') }}</span>
          <span class="legend-item"><span class="swatch swatch-afa"></span> {{ t('summary.tax.bridge.depreciation') }}</span>
          <span class="legend-item"><span class="swatch swatch-interest-b"></span> {{ t('summary.tax.bridge.interest') }}</span>
          <span class="legend-item"><span class="swatch swatch-operating"></span> {{ t('summary.tax.bridge.operating') }}</span>
          <span class="legend-item"><span class="swatch swatch-maintenance"></span> {{ t('summary.tax.bridge.maintenance') }}</span>
        </div>
      </KalkCard>

      <!-- ===== CAPEX TIMELINE ===== -->
      <KalkCard v-if="result.capexTimeline.length > 0" :title="t('summary.capexTimeline')">
        <div class="capex-timeline">
          <div v-for="item in result.capexTimeline" :key="item.id" class="capex-item" :class="item.taxClassification">
            <div class="capex-date">{{ String(item.month).padStart(2, '0') }}/{{ item.year }}</div>
            <div class="capex-bar" :style="{ width: capexBarWidth(item.amount) }">
              <span class="capex-name">{{ item.name }}</span>
              <span class="capex-amount">{{ fmtCur(item.amount) }}</span>
            </div>
            <div class="capex-tag" :class="item.taxClassification">
              {{ t(`capex.taxClassifications.${item.taxClassification}`) }}
            </div>
          </div>
        </div>
      </KalkCard>

      <!-- ===== RISK INDICATORS ===== -->
      <KalkCard :title="t('summary.riskIndicators')">
        <div class="risk-grid">
          <div class="risk-item">
            <span class="risk-label">{{ t('summary.risk.maintenance') }}</span>
            <div class="risk-gauge">
              <div class="risk-fill" :class="riskClass(result.metrics.maintenanceRiskScore)" :style="{ width: result.metrics.maintenanceRiskScore + '%' }"></div>
            </div>
            <span class="risk-score" :class="riskClass(result.metrics.maintenanceRiskScore)">
              {{ result.metrics.maintenanceRiskScore }}/100
              <span class="risk-level">{{ riskLabel(result.metrics.maintenanceRiskScore) }}</span>
            </span>
          </div>
          <div class="risk-item">
            <span class="risk-label">{{ t('summary.risk.liquidity') }}</span>
            <div class="risk-gauge">
              <div class="risk-fill" :class="riskClass(result.metrics.liquidityRiskScore)" :style="{ width: result.metrics.liquidityRiskScore + '%' }"></div>
            </div>
            <span class="risk-score" :class="riskClass(result.metrics.liquidityRiskScore)">
              {{ result.metrics.liquidityRiskScore }}/100
              <span class="risk-level">{{ riskLabel(result.metrics.liquidityRiskScore) }}</span>
            </span>
          </div>
        </div>
      </KalkCard>

      <!-- ===== CASHFLOW TABLE ===== -->
      <KalkCard :title="t('summary.cashflowTable')">
        <div class="table-scroll">
          <table class="cashflow-table">
            <thead>
              <tr>
                <th class="sticky-col">{{ t('summary.cashflow.year') }}</th>
                <th>{{ t('summary.cashflow.effectiveRent') }}</th>
                <th>{{ t('summary.cashflow.operatingCosts') }}</th>
                <th>{{ t('summary.cashflow.noi') }}</th>
                <th>{{ t('summary.cashflow.interest') }}</th>
                <th>{{ t('summary.cashflow.principal') }}</th>
                <th>{{ t('summary.cashflow.capex') }}</th>
                <th>{{ t('summary.cashflow.beforeTax') }}</th>
                <th>{{ t('summary.cashflow.tax') }}</th>
                <th class="highlight-col">{{ t('summary.cashflow.afterTax') }}</th>
                <th>{{ t('summary.cashflow.cumulative') }}</th>
                <th>{{ t('summary.cashflow.debt') }}</th>
                <th>{{ t('summary.cashflow.ltv') }}</th>
                <th>{{ t('summary.cashflow.dscr') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in result.yearlyCashflows" :key="row.year">
                <td class="sticky-col year-col">{{ row.year }}</td>
                <td>{{ fmtCompact(row.effectiveRent) }}</td>
                <td class="neg">{{ fmtCompact(-row.operatingCosts) }}</td>
                <td :class="valClass(row.netOperatingIncome)">{{ fmtCompact(row.netOperatingIncome) }}</td>
                <td class="neg">{{ fmtCompact(-row.interestPortion) }}</td>
                <td class="neg">{{ fmtCompact(-row.principalPortion) }}</td>
                <td class="neg">{{ row.capexPayments > 0 ? fmtCompact(-row.capexPayments) : '–' }}</td>
                <td :class="valClass(row.cashflowBeforeTax)">{{ fmtCompact(row.cashflowBeforeTax) }}</td>
                <td class="neg">{{ fmtCompact(-row.taxPayment) }}</td>
                <td class="highlight-col" :class="valClass(row.cashflowAfterTax)">
                  <strong>{{ fmtCompact(row.cashflowAfterTax) }}</strong>
                </td>
                <td :class="valClass(row.cumulativeCashflow)">{{ fmtCompact(row.cumulativeCashflow) }}</td>
                <td>{{ fmtCompact(row.outstandingDebt) }}</td>
                <td>{{ fmtPct(row.ltvPercent) }}</td>
                <td :class="{ warn: row.dscrYear < 1.2 && row.dscrYear > 0 }">{{ row.dscrYear > 0 ? row.dscrYear.toFixed(2) : '–' }}</td>
              </tr>
            </tbody>
            <tfoot>
              <tr>
                <td class="sticky-col"><strong>{{ t('common.total') }}</strong></td>
                <td>{{ fmtCompact(sumCol('effectiveRent')) }}</td>
                <td class="neg">{{ fmtCompact(-sumCol('operatingCosts')) }}</td>
                <td>{{ fmtCompact(sumCol('netOperatingIncome')) }}</td>
                <td class="neg">{{ fmtCompact(-sumCol('interestPortion')) }}</td>
                <td class="neg">{{ fmtCompact(-sumCol('principalPortion')) }}</td>
                <td class="neg">{{ fmtCompact(-sumCol('capexPayments')) }}</td>
                <td>{{ fmtCompact(result.totalCashflowBeforeTax) }}</td>
                <td class="neg">{{ fmtCompact(-sumCol('taxPayment')) }}</td>
                <td class="highlight-col"><strong>{{ fmtCompact(result.totalCashflowAfterTax) }}</strong></td>
                <td></td><td></td><td></td><td></td>
              </tr>
            </tfoot>
          </table>
        </div>
      </KalkCard>

      <!-- ===== TAX SUMMARY ===== -->
      <KalkCard :title="t('summary.taxSummary')">
        <div class="tax-grid">
          <div class="tax-row">
            <span>{{ t('summary.tax.depreciationRate') }}</span>
            <strong>{{ fmtPct(result.taxSummary.depreciationRatePercent) }}</strong>
          </div>
          <div class="tax-row">
            <span>{{ t('summary.tax.depreciationBasis') }}</span>
            <strong>{{ fmtCur(result.taxSummary.depreciationBasis.amount) }}</strong>
          </div>
          <div class="tax-row">
            <span>{{ t('summary.tax.annualDepreciation') }}</span>
            <strong>{{ fmtCur(result.taxSummary.annualDepreciation.amount) }}</strong>
          </div>
          <div class="tax-row separator">
            <span>{{ t('summary.tax.totalDepreciation') }}</span>
            <strong>{{ fmtCur(result.taxSummary.totalDepreciation.amount) }}</strong>
          </div>
          <div class="tax-row">
            <span>{{ t('summary.tax.totalInterest') }}</span>
            <strong>{{ fmtCur(result.taxSummary.totalInterestDeduction.amount) }}</strong>
          </div>
          <div class="tax-row">
            <span>{{ t('summary.tax.totalMaintenance') }}</span>
            <strong>{{ fmtCur(result.taxSummary.totalMaintenanceDeduction.amount) }}</strong>
          </div>
          <div class="tax-row">
            <span>{{ t('summary.tax.totalOperating') }}</span>
            <strong>{{ fmtCur(result.taxSummary.totalOperatingDeduction.amount) }}</strong>
          </div>
          <div class="tax-row highlight">
            <span>{{ t('summary.tax.totalSavings') }}</span>
            <strong class="pos">{{ fmtCur(result.taxSummary.totalTaxSavings.amount) }}</strong>
          </div>
          <div class="tax-row highlight">
            <span>{{ t('summary.tax.totalTax') }}</span>
            <strong>{{ fmtCur(result.taxSummary.totalTaxPayment.amount) }}</strong>
          </div>
          <div class="tax-row">
            <span>{{ t('summary.tax.effectiveRate') }}</span>
            <strong>{{ fmtPct(result.taxSummary.effectiveTaxRatePercent) }}</strong>
          </div>
          <div class="tax-row" :class="{ 'rule-triggered': result.taxSummary.acquisitionRelatedCostsTriggered }">
            <span>{{ t('summary.tax.rule15') }}</span>
            <strong>
              {{ result.taxSummary.acquisitionRelatedCostsTriggered
                ? t('summary.tax.rule15Triggered')
                : t('summary.tax.rule15NotTriggered')
              }}
            </strong>
          </div>
        </div>
      </KalkCard>

      <!-- ===== TOTALS ===== -->
      <div class="totals-section">
        <div class="total-item">
          <span>{{ t('summary.totals.totalEquity') }}</span>
          <strong>{{ fmtCur(result.totalEquityInvested) }}</strong>
        </div>
        <div class="total-item">
          <span>{{ t('summary.totals.totalCashflowBefore') }}</span>
          <strong :class="valClass(result.totalCashflowBeforeTax)">{{ fmtCur(result.totalCashflowBeforeTax) }}</strong>
        </div>
        <div class="total-item accent">
          <span>{{ t('summary.totals.totalCashflowAfter') }}</span>
          <strong :class="valClass(result.totalCashflowAfterTax)">{{ fmtCur(result.totalCashflowAfterTax) }}</strong>
        </div>
      </div>

      <!-- Actions -->
      <div class="actions">
        <button type="button" class="btn btn-outline" @click="calculate">
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
const result = ref<CalculationResult | null>(null);

const project = computed(() => projectStore.currentProject);
const currency = computed(() => project.value?.currency || 'EUR');
const totalInvestment = computed(() => projectStore.totalCapitalRequirement?.amount || 0);
const ltv = computed(() => {
  if (!project.value || totalInvestment.value === 0) return 0;
  const totalLoans = project.value.financing.loans.reduce((s, l) => s + l.principal.amount, 0);
  return (totalLoans / totalInvestment.value) * 100;
});

// Chart scaling
const maxNoi = computed(() => {
  if (!result.value) return 1;
  return Math.max(...result.value.yearlyCashflows.map(r => r.netOperatingIncome), 1);
});
const maxDebt = computed(() => {
  if (!result.value) return 1;
  return Math.max(...result.value.yearlyCashflows.map(r => r.outstandingDebt), 1);
});
const maxBridgeValue = computed(() => {
  if (!result.value) return 1;
  return Math.max(...result.value.taxBridge.map(r => r.grossIncome), 1);
});
const maxCapex = computed(() => {
  if (!result.value) return 1;
  return Math.max(...result.value.capexTimeline.map(r => r.amount), 1);
});

// Formatters
function fmtCur(v: number): string {
  return new Intl.NumberFormat(locale.value, { style: 'currency', currency: currency.value, maximumFractionDigits: 0 }).format(v);
}
function fmtCompact(v: number): string {
  if (Math.abs(v) >= 1_000_000) {
    return new Intl.NumberFormat(locale.value, { style: 'currency', currency: currency.value, notation: 'compact', maximumFractionDigits: 1 }).format(v);
  }
  return new Intl.NumberFormat(locale.value, { style: 'currency', currency: currency.value, maximumFractionDigits: 0 }).format(v);
}
function fmtPct(v: number): string {
  return new Intl.NumberFormat(locale.value, { style: 'percent', minimumFractionDigits: 1, maximumFractionDigits: 2 }).format(v / 100);
}

function npvClass(v: number) { return { pos: v >= 0, neg: v < 0 }; }
function valClass(v: number) { return { pos: v >= 0, neg: v < 0 }; }
function sumCol(key: keyof YearlyCashflowRow): number {
  if (!result.value) return 0;
  return result.value.yearlyCashflows.reduce((s, r) => s + (r[key] as number), 0);
}

// Chart helpers
function barHeight(value: number): string {
  return Math.max(Math.min((value / maxNoi.value) * 100, 100), 2) + '%';
}
function cfMarkerPos(value: number): string {
  const pct = (value / maxNoi.value) * 50 + 50;
  return Math.min(Math.max(pct, 5), 95) + '%';
}
function debtBarHeight(value: number): string {
  return Math.max((value / maxDebt.value) * 100, 2) + '%';
}
function bridgeWidth(value: number): string {
  return Math.max((value / maxBridgeValue.value) * 100, 1) + '%';
}
function capexBarWidth(value: number): string {
  return Math.max((value / maxCapex.value) * 80, 10) + '%';
}

// Risk helpers
function riskClass(score: number): string {
  if (score <= 25) return 'risk-low';
  if (score <= 50) return 'risk-medium';
  if (score <= 75) return 'risk-high';
  return 'risk-critical';
}
function riskLabel(score: number): string {
  if (score <= 25) return t('summary.risk.low');
  if (score <= 50) return t('summary.risk.medium');
  if (score <= 75) return t('summary.risk.high');
  return t('summary.risk.critical');
}

async function calculate() {
  if (!project.value) return;
  isCalculating.value = true;
  try {
    if (authStore.isAuthenticated) {
      try {
        const r = await projectsApi.calculate(project.value.id);
        result.value = r;
        projectStore.setCalculationResult(r);
        return;
      } catch { /* fallthrough to local */ }
    }
    const r = calculateProject(project.value);
    result.value = r;
    projectStore.setCalculationResult(r);
  } catch {
    uiStore.showToast('Berechnung fehlgeschlagen', 'error');
  } finally {
    isCalculating.value = false;
  }
}

emit('validation-change', true);
</script>

<style scoped>
.summary-page { display: flex; flex-direction: column; gap: var(--kalk-space-6); }

/* Overview */
.overview-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: var(--kalk-space-4); }
@media (max-width: 600px) { .overview-grid { grid-template-columns: 1fr; } }
.overview-item { display: flex; flex-direction: column; padding: var(--kalk-space-4); background: var(--kalk-gray-50); border-radius: var(--kalk-radius-md); }
.overview-item .label { font-size: var(--kalk-text-sm); color: var(--kalk-gray-500); margin-bottom: var(--kalk-space-1); }
.overview-item .value { font-size: var(--kalk-text-lg); font-weight: 600; color: var(--kalk-gray-900); font-variant-numeric: tabular-nums; }
.overview-item .value.highlight { color: var(--kalk-accent-600); }

/* Calculate */
.calculate-prompt { text-align: center; padding: var(--kalk-space-8); }
.calculate-prompt p { color: var(--kalk-gray-500); margin-bottom: var(--kalk-space-6); }

/* Buttons */
.btn { display: inline-flex; align-items: center; justify-content: center; gap: var(--kalk-space-2); font-family: var(--kalk-font-family); font-weight: 600; border-radius: var(--kalk-radius-md); border: none; cursor: pointer; transition: all 0.15s; }
.btn-lg { height: 48px; padding: 0 var(--kalk-space-8); font-size: var(--kalk-text-base); }
.btn-primary { background: var(--kalk-accent-500); color: #fff; }
.btn-primary:hover { background: var(--kalk-accent-600); }
.btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
.btn-outline { background: #fff; color: var(--kalk-gray-700); border: 1.5px solid var(--kalk-gray-300); height: 40px; padding: 0 var(--kalk-space-5); font-size: var(--kalk-text-sm); }
.btn-outline:hover { background: var(--kalk-gray-50); }
.spinner { width: 18px; height: 18px; border: 2px solid rgba(255,255,255,0.3); border-top-color: #fff; border-radius: 50%; animation: spin 0.6s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

/* Warnings */
.warnings-section { display: flex; flex-direction: column; gap: var(--kalk-space-2); }
.warning-item { display: flex; align-items: center; gap: var(--kalk-space-3); padding: var(--kalk-space-3) var(--kalk-space-4); border-radius: var(--kalk-radius-md); font-size: var(--kalk-text-sm); }
.warning-item.info { background: var(--kalk-gray-50); color: var(--kalk-primary-700); }
.warning-item.warning { background: #fefce8; color: #92400e; }
.warning-item.critical { background: #fef2f2; color: #991b1b; }
.warning-icon { width: 20px; height: 20px; flex-shrink: 0; }

/* Metrics */
.metrics-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: var(--kalk-space-4); }
.metrics-grid.compact { grid-template-columns: repeat(3, 1fr); }
@media (max-width: 600px) { .metrics-grid, .metrics-grid.compact { grid-template-columns: repeat(2, 1fr); } }
.metric-item { display: flex; flex-direction: column; align-items: center; padding: var(--kalk-space-4) var(--kalk-space-3); background: var(--kalk-gray-50); border-radius: var(--kalk-radius-md); text-align: center; }
.metric-item.accent { background: var(--kalk-accent-50); border: 1px solid rgba(16, 185, 129, 0.2); }
.metric-item.warn { background: #fefce8; border: 1px solid rgba(217, 119, 6, 0.2); }
.metric-label { font-size: var(--kalk-text-xs); color: var(--kalk-gray-500); margin-bottom: var(--kalk-space-1); }
.metric-value { font-size: var(--kalk-text-xl); font-weight: 700; color: var(--kalk-gray-900); font-variant-numeric: tabular-nums; }

/* === CASHFLOW CHART === */
.chart-container { padding: var(--kalk-space-2) 0; }
.bar-chart { display: flex; align-items: flex-end; gap: 2px; height: 180px; padding: 0 var(--kalk-space-2); border-bottom: 1px solid var(--kalk-gray-200); }
.bar-group { flex: 1; display: flex; flex-direction: column; align-items: center; position: relative; height: 100%; }
.bar-stack { display: flex; flex-direction: column-reverse; width: 100%; max-width: 32px; height: 100%; justify-content: flex-start; }
.bar { width: 100%; border-radius: 2px 2px 0 0; transition: height 0.3s; min-height: 1px; }
.bar-noi { background: var(--kalk-accent-500); opacity: 0.85; }
.bar-debt { background: var(--kalk-primary-700); }
.bar-tax { background: var(--kalk-gray-400); }
.bar-cf-marker { position: absolute; left: 50%; transform: translateX(-50%); width: 8px; height: 8px; border-radius: 50%; background: var(--kalk-primary-900); border: 1.5px solid #fff; box-shadow: 0 1px 2px rgba(0,0,0,0.15); z-index: 2; }
.bar-cf-marker.negative { background: #b91c1c; }
.bar-label { font-size: 10px; color: var(--kalk-gray-500); margin-top: var(--kalk-space-1); }

/* === FINANCING CHART === */
.financing-chart { display: flex; align-items: flex-end; gap: 3px; height: 160px; padding: 0 var(--kalk-space-2); border-bottom: 1px solid var(--kalk-gray-200); }
.fin-bar-group { flex: 1; display: flex; flex-direction: column; align-items: center; height: 100%; justify-content: flex-end; }
.fin-bar { width: 100%; max-width: 36px; background: var(--kalk-primary-800); border-radius: 2px 2px 0 0; transition: height 0.3s; display: flex; align-items: flex-start; justify-content: center; min-height: 2px; }
.fin-ltv-label { font-size: 9px; font-weight: 600; color: rgba(255,255,255,0.9); padding-top: 2px; }

/* Legend */
.chart-legend { display: flex; flex-wrap: wrap; gap: var(--kalk-space-4); justify-content: center; margin-top: var(--kalk-space-3); font-size: var(--kalk-text-xs); color: var(--kalk-gray-500); }
.legend-item { display: flex; align-items: center; gap: var(--kalk-space-1); }
.swatch { width: 10px; height: 10px; border-radius: 2px; display: inline-block; }
.swatch-noi { background: var(--kalk-accent-500); opacity: 0.85; }
.swatch-debt { background: var(--kalk-primary-700); }
.swatch-tax { background: var(--kalk-gray-400); }
.swatch-cf { background: var(--kalk-primary-900); border-radius: 50%; width: 8px; height: 8px; }
.swatch-debt-bar { background: var(--kalk-primary-800); }

/* === STEUER-BRIDGE === */
.tax-bridge { display: flex; flex-direction: column; gap: var(--kalk-space-2); }
.bridge-year { display: flex; align-items: center; gap: var(--kalk-space-3); }
.bridge-year-label { width: 36px; font-size: var(--kalk-text-xs); font-weight: 600; color: var(--kalk-gray-600); text-align: right; flex-shrink: 0; font-variant-numeric: tabular-nums; }
.bridge-bars { flex: 1; display: flex; height: 18px; border-radius: 2px; overflow: hidden; }
.bridge-bar { height: 100%; min-width: 2px; }
.bridge-bar.income { background: var(--kalk-accent-500); opacity: 0.8; }
.bridge-bar.afa { background: var(--kalk-primary-800); }
.bridge-bar.interest { background: var(--kalk-primary-700); opacity: 0.75; }
.bridge-bar.operating { background: var(--kalk-gray-400); }
.bridge-bar.maintenance { background: var(--kalk-gray-300); }
.bridge-result { font-size: var(--kalk-text-xs); font-weight: 600; color: var(--kalk-gray-700); white-space: nowrap; min-width: 100px; text-align: right; font-variant-numeric: tabular-nums; }
.bridge-result.negative { color: #b91c1c; }
.bridge-legend { margin-top: var(--kalk-space-4); }
.swatch-income { background: var(--kalk-accent-500); opacity: 0.8; }
.swatch-afa { background: var(--kalk-primary-800); }
.swatch-interest-b { background: var(--kalk-primary-700); opacity: 0.75; }
.swatch-operating { background: var(--kalk-gray-400); }
.swatch-maintenance { background: var(--kalk-gray-300); }

/* === CAPEX TIMELINE === */
.capex-timeline { display: flex; flex-direction: column; gap: var(--kalk-space-2); }
.capex-item { display: flex; align-items: center; gap: var(--kalk-space-3); }
.capex-date { width: 56px; font-size: var(--kalk-text-xs); font-weight: 600; color: var(--kalk-gray-500); text-align: right; flex-shrink: 0; font-variant-numeric: tabular-nums; }
.capex-bar { display: flex; justify-content: space-between; align-items: center; padding: var(--kalk-space-2) var(--kalk-space-3); border-radius: var(--kalk-radius-sm); font-size: var(--kalk-text-xs); min-width: 60px; border-left: 3px solid; }
.capex-item.MaintenanceExpense .capex-bar { background: var(--kalk-gray-50); border-color: var(--kalk-accent-500); color: var(--kalk-gray-800); }
.capex-item.AcquisitionCost .capex-bar { background: var(--kalk-gray-50); border-color: var(--kalk-primary-700); color: var(--kalk-gray-800); }
.capex-item.ImprovementCost .capex-bar { background: var(--kalk-gray-50); border-color: var(--kalk-primary-900); color: var(--kalk-gray-800); }
.capex-item.NotDeductible .capex-bar { background: var(--kalk-gray-50); border-color: var(--kalk-gray-400); color: var(--kalk-gray-600); }
.capex-name { font-weight: 500; }
.capex-amount { font-weight: 700; font-variant-numeric: tabular-nums; margin-left: var(--kalk-space-2); }
.capex-tag { font-size: 10px; font-weight: 600; padding: 2px 6px; border-radius: 3px; white-space: nowrap; flex-shrink: 0; }
.capex-tag.MaintenanceExpense { background: rgba(16, 185, 129, 0.1); color: #047857; }
.capex-tag.AcquisitionCost { background: rgba(51, 65, 85, 0.1); color: var(--kalk-primary-700); }
.capex-tag.ImprovementCost { background: rgba(15, 23, 42, 0.1); color: var(--kalk-primary-900); }
.capex-tag.NotDeductible { background: var(--kalk-gray-100); color: var(--kalk-gray-600); }

/* === RISK INDICATORS === */
.risk-grid { display: flex; flex-direction: column; gap: var(--kalk-space-6); }
.risk-item { display: flex; flex-direction: column; gap: var(--kalk-space-2); }
.risk-label { font-size: var(--kalk-text-sm); font-weight: 600; color: var(--kalk-gray-700); }
.risk-gauge { height: 6px; background: var(--kalk-gray-100); border-radius: 3px; overflow: hidden; }
.risk-fill { height: 100%; border-radius: 3px; transition: width 0.5s ease-out; }
.risk-fill.risk-low { background: var(--kalk-accent-500); }
.risk-fill.risk-medium { background: #d97706; }
.risk-fill.risk-high { background: #c2410c; }
.risk-fill.risk-critical { background: #b91c1c; }
.risk-score { font-size: var(--kalk-text-sm); font-weight: 700; font-variant-numeric: tabular-nums; }
.risk-score.risk-low { color: #047857; }
.risk-score.risk-medium { color: #92400e; }
.risk-score.risk-high { color: #9a3412; }
.risk-score.risk-critical { color: #991b1b; }
.risk-level { font-weight: 500; margin-left: var(--kalk-space-2); }

/* === CASHFLOW TABLE === */
.table-scroll { overflow-x: auto; -webkit-overflow-scrolling: touch; margin: 0 calc(-1 * var(--kalk-space-4)); padding: 0 var(--kalk-space-4); }
.cashflow-table { width: 100%; min-width: 1000px; border-collapse: collapse; font-size: var(--kalk-text-xs); font-variant-numeric: tabular-nums; }
.cashflow-table th, .cashflow-table td { padding: var(--kalk-space-2) var(--kalk-space-3); text-align: right; white-space: nowrap; border-bottom: 1px solid var(--kalk-gray-100); }
.cashflow-table th { background: var(--kalk-gray-50); color: var(--kalk-gray-600); font-weight: 600; position: sticky; top: 0; z-index: 1; }
.cashflow-table .sticky-col { text-align: left; position: sticky; left: 0; background: #fff; z-index: 2; }
.cashflow-table th.sticky-col { background: var(--kalk-gray-50); z-index: 3; }
.cashflow-table .year-col { font-weight: 600; color: var(--kalk-gray-900); }
.cashflow-table .highlight-col { background: var(--kalk-accent-50); }
.cashflow-table th.highlight-col { background: rgba(16, 185, 129, 0.08); }
.cashflow-table tbody tr:hover { background: var(--kalk-gray-50); }
.cashflow-table tbody tr:hover .sticky-col { background: var(--kalk-gray-50); }
.cashflow-table tfoot { border-top: 2px solid var(--kalk-gray-300); }
.cashflow-table tfoot td { background: var(--kalk-gray-50); font-weight: 600; }
.cashflow-table tfoot .sticky-col { background: var(--kalk-gray-50); }

/* Colors */
.pos { color: #047857; }
.neg { color: #991b1b; }
.warn { color: #92400e; }

/* === TAX SUMMARY === */
.tax-grid { display: flex; flex-direction: column; gap: var(--kalk-space-2); }
.tax-row { display: flex; justify-content: space-between; align-items: center; padding: var(--kalk-space-3) var(--kalk-space-4); border-radius: var(--kalk-radius-sm); font-size: var(--kalk-text-sm); }
.tax-row span { color: var(--kalk-gray-600); }
.tax-row strong { font-variant-numeric: tabular-nums; color: var(--kalk-gray-900); }
.tax-row.separator { border-top: 1px solid var(--kalk-gray-200); margin-top: var(--kalk-space-2); padding-top: var(--kalk-space-4); }
.tax-row.highlight { background: var(--kalk-gray-50); font-weight: 600; }
.tax-row.rule-triggered { background: #fefce8; }
.tax-row.rule-triggered strong { color: #92400e; }

/* === TOTALS === */
.totals-section { display: grid; grid-template-columns: repeat(3, 1fr); gap: var(--kalk-space-4); }
@media (max-width: 600px) { .totals-section { grid-template-columns: 1fr; } }
.total-item { display: flex; flex-direction: column; align-items: center; padding: var(--kalk-space-5); background: var(--kalk-gray-50); border-radius: var(--kalk-radius-md); text-align: center; }
.total-item.accent { background: var(--kalk-primary-900); color: #fff; }
.total-item span { font-size: var(--kalk-text-xs); color: var(--kalk-gray-500); margin-bottom: var(--kalk-space-2); }
.total-item.accent span { color: rgba(255,255,255,0.8); }
.total-item strong { font-size: var(--kalk-text-lg); font-weight: 700; font-variant-numeric: tabular-nums; }
.total-item.accent strong { color: #fff; }

.actions { display: flex; justify-content: center; gap: var(--kalk-space-4); }
</style>
