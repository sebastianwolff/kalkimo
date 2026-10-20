<template>
  <div class="summary-page">
    <!-- Empty state: before calculation -->
    <div v-if="!result" class="empty-result-state">
      <div class="empty-result-icon">
        <svg viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
          <rect x="10" y="6" width="28" height="36" rx="2" stroke="currentColor" stroke-width="1.5"/>
          <path d="M16 14h16" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
          <path d="M16 22h6M26 22h6M16 28h6M26 28h6M16 34h6M26 34h6" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
        </svg>
      </div>
      <h3 class="empty-result-title">Ergebnis berechnen</h3>
      <p class="empty-result-text">
        Alle Projektdaten wurden erfasst. Klicken Sie auf
        <strong>Berechnen</strong> in der unteren Leiste, um die
        Investitionsanalyse zu starten.
      </p>
      <div v-if="isCalculating" class="calculating-indicator">
        <span class="spinner"></span>
        <span>Berechnung l&auml;uft...</span>
      </div>
    </div>

    <!-- ===== RESULTS (shown after calculation) ===== -->
    <template v-if="result">

    <!-- Tab Bar -->
    <nav class="tab-bar" role="tablist">
      <button
        v-for="tab in tabs"
        :key="tab.id"
        type="button"
        role="tab"
        class="tab-btn"
        :class="{ active: activeTab === tab.id }"
        :aria-selected="activeTab === tab.id"
        @click="activeTab = tab.id"
      >
        {{ t(tab.label) }}
      </button>
    </nav>

    <!-- ===== TAB: OVERVIEW ===== -->
    <div v-show="activeTab === 'overview'" class="tab-content">

      <!-- Return Metrics -->
      <section>
        <h2 class="section-title block-title">{{ t('summary.returnMetrics') }}</h2>
        <div class="kpi-grid kpi-grid-3">
          <div class="kpi kpi-hero">
            <span class="kpi-label">{{ t('summary.metrics.irrAfterTax') }} <HelpIcon help-key="summary.irrAfterTax" /></span>
            <span class="kpi-value" :class="signClass(result.metrics.irrAfterTaxPercent)">{{ fmtPct(result.metrics.irrAfterTaxPercent) }}</span>
          </div>
          <div class="kpi">
            <span class="kpi-label">{{ t('summary.metrics.irrBeforeTax') }} <HelpIcon help-key="summary.irrBeforeTax" /></span>
            <span class="kpi-value" :class="signClass(result.metrics.irrBeforeTaxPercent)">{{ fmtPct(result.metrics.irrBeforeTaxPercent) }}</span>
          </div>
          <div class="kpi">
            <span class="kpi-label">{{ t('summary.metrics.npvAfterTax') }} <HelpIcon help-key="summary.npvAfterTax" /></span>
            <span class="kpi-value" :class="signClass(result.metrics.npvAfterTax.amount)">
              {{ fmtCur(result.metrics.npvAfterTax.amount) }}
            </span>
          </div>
          <div class="kpi">
            <span class="kpi-label">{{ t('summary.metrics.cashOnCash') }} <HelpIcon help-key="summary.cashOnCash" /></span>
            <span class="kpi-value" :class="signClass(result.metrics.cashOnCashPercent)">{{ fmtPct(result.metrics.cashOnCashPercent) }}</span>
          </div>
          <div class="kpi">
            <span class="kpi-label">{{ t('summary.metrics.equityMultiple') }} <HelpIcon help-key="summary.equityMultiple" /></span>
            <span class="kpi-value" :class="signClass(result.metrics.equityMultiple)">{{ result.metrics.equityMultiple.toFixed(2) }}x</span>
          </div>
          <div class="kpi">
            <span class="kpi-label">{{ t('summary.metrics.roi') }} <HelpIcon help-key="summary.roi" /></span>
            <span class="kpi-value" :class="signClass(result.metrics.roiPercent)">{{ fmtPct(result.metrics.roiPercent) }}</span>
          </div>
        </div>
      </section>

      <!-- Bank Metrics -->
      <section>
        <h2 class="section-title block-title">{{ t('summary.bankMetrics') }}</h2>
        <div class="kpi-grid kpi-grid-3">
          <div class="kpi" :class="{ 'kpi-warn': result.metrics.dscrMin != null && result.metrics.dscrMin < 1.2 }">
            <span class="kpi-label">{{ t('summary.metrics.dscrMin') }} <HelpIcon help-key="summary.dscrMin" /></span>
            <span class="kpi-value">{{ result.metrics.dscrMin?.toFixed(2) ?? '–' }}</span>
          </div>
          <div class="kpi">
            <span class="kpi-label">{{ t('summary.metrics.dscrAvg') }} <HelpIcon help-key="summary.dscrAvg" /></span>
            <span class="kpi-value">{{ result.metrics.dscrAvg?.toFixed(2) ?? '–' }}</span>
          </div>
          <div class="kpi" :class="{ 'kpi-warn': result.metrics.icrMin != null && result.metrics.icrMin < 1.5 }">
            <span class="kpi-label">{{ t('summary.metrics.icrMin') }} <HelpIcon help-key="summary.icrMin" /></span>
            <span class="kpi-value">{{ result.metrics.icrMin?.toFixed(2) ?? '–' }}</span>
          </div>
          <div class="kpi">
            <span class="kpi-label">{{ t('summary.metrics.ltvInitial') }} <HelpIcon help-key="summary.ltvInitial" /></span>
            <span class="kpi-value">{{ fmtPct(result.metrics.ltvInitialPercent) }}</span>
          </div>
          <div class="kpi">
            <span class="kpi-label">{{ t('summary.metrics.ltvFinal') }} <HelpIcon help-key="summary.ltvFinal" /></span>
            <span class="kpi-value">{{ fmtPct(result.metrics.ltvFinalPercent) }}</span>
          </div>
          <div class="kpi">
            <span class="kpi-label">{{ t('summary.metrics.breakEvenRent') }} <HelpIcon help-key="summary.breakEvenRent" /></span>
            <span class="kpi-value">{{ fmtCur(result.metrics.breakEvenRent.amount) }}<span class="kpi-unit">/M</span></span>
          </div>
        </div>
      </section>

      <div class="two-col">
        <!-- Project Overview -->
        <section class="panel">
          <header class="panel-header">
            <h3 class="panel-title">{{ t('summary.projectOverview') }}</h3>
          </header>
          <div class="panel-body panel-body-rows">
            <div class="data-row">
              <span class="data-label">{{ t('project.name') }}</span>
              <span class="data-value">{{ project?.name }}</span>
            </div>
            <div class="data-row">
              <span class="data-label">{{ t('property.type') }}</span>
              <span class="data-value">{{ t(`property.types.${project?.property?.type || 'SingleFamily'}`) }}</span>
            </div>
            <div class="data-row">
              <span class="data-label">{{ t('purchase.price') }}</span>
              <span class="data-value">{{ fmtCur(project?.purchase.purchasePrice.amount || 0) }}</span>
            </div>
            <div class="data-row">
              <span class="data-label">{{ t('financing.equity') }}</span>
              <span class="data-value">{{ fmtCur(project?.financing.equity.amount || 0) }}</span>
            </div>
            <div class="data-row">
              <span class="data-label">{{ t('summary.metrics.ltvInitial') }}</span>
              <span class="data-value">{{ fmtPct(ltv) }}</span>
            </div>
            <div class="data-row total">
              <span class="data-label">{{ t('purchase.totalInvestment') }}</span>
              <span class="data-value">{{ fmtCur(totalInvestment) }}</span>
            </div>
          </div>
        </section>

        <!-- Cashflow totals -->
        <section class="panel">
          <header class="panel-header">
            <h3 class="panel-title">{{ t('summary.cashflowTable') }}</h3>
          </header>
          <div class="panel-body panel-body-rows">
            <div class="data-row">
              <span class="data-label">{{ t('summary.totals.totalEquity') }}</span>
              <span class="data-value">{{ fmtCur(result.totalEquityInvested) }}</span>
            </div>
            <div class="data-row">
              <span class="data-label">{{ t('summary.totals.totalCashflowBefore') }}</span>
              <span class="data-value" :class="valClass(result.totalCashflowBeforeTax)">{{ fmtCur(result.totalCashflowBeforeTax) }}</span>
            </div>
            <div class="data-row total">
              <span class="data-label">{{ t('summary.totals.totalCashflowAfter') }}</span>
              <span class="data-value" :class="valClass(result.totalCashflowAfterTax)">{{ fmtCur(result.totalCashflowAfterTax) }}</span>
            </div>
          </div>
        </section>
      </div>

      <!-- Risk Indicators -->
      <section class="panel">
        <header class="panel-header">
          <h3 class="panel-title">{{ t('summary.riskIndicators') }}</h3>
        </header>
        <div class="panel-body risk-grid">
          <div class="risk-item">
            <div class="risk-head">
              <span class="risk-label">{{ t('summary.risk.maintenance') }} <HelpIcon help-key="summary.riskMaintenance" /></span>
              <span class="risk-score" :class="riskClass(result.metrics.maintenanceRiskScore)">
                {{ result.metrics.maintenanceRiskScore }}<span class="risk-max">/100</span>
                <span class="risk-level">{{ riskLabel(result.metrics.maintenanceRiskScore) }}</span>
              </span>
            </div>
            <div class="risk-gauge">
              <div class="risk-fill" :class="riskClass(result.metrics.maintenanceRiskScore)" :style="{ width: result.metrics.maintenanceRiskScore + '%' }"></div>
            </div>
          </div>
          <div class="risk-item">
            <div class="risk-head">
              <span class="risk-label">{{ t('summary.risk.liquidity') }} <HelpIcon help-key="summary.riskLiquidity" /></span>
              <span class="risk-score" :class="riskClass(result.metrics.liquidityRiskScore)">
                {{ result.metrics.liquidityRiskScore }}<span class="risk-max">/100</span>
                <span class="risk-level">{{ riskLabel(result.metrics.liquidityRiskScore) }}</span>
              </span>
            </div>
            <div class="risk-gauge">
              <div class="risk-fill" :class="riskClass(result.metrics.liquidityRiskScore)" :style="{ width: result.metrics.liquidityRiskScore + '%' }"></div>
            </div>
          </div>
        </div>
      </section>

      <!-- Collapsible Warnings -->
      <section v-if="warningCount > 0" class="panel warnings-card">
        <button type="button" class="warnings-card-header" :class="{ open: warningsOpen, 'has-critical': hasCriticalWarning }" :aria-expanded="warningsOpen" @click="warningsOpen = !warningsOpen">
          <span class="warnings-card-title">
            <svg class="warning-icon" viewBox="0 0 20 20" fill="currentColor">
              <path v-if="hasCriticalWarning" fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
              <path v-else fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
            </svg>
            {{ t('summary.warnings') }}
            <span class="badge" :class="hasCriticalWarning ? 'badge-error' : 'badge-neutral'">{{ warningCount }}</span>
          </span>
          <svg class="chevron-icon" :class="{ open: warningsOpen }" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd" />
          </svg>
        </button>
        <div v-show="warningsOpen" class="warnings-card-body">
          <div
            v-for="(w, i) in result.warnings"
            :key="i"
            class="callout warning-item"
            :class="w.severity === 'critical' ? 'error' : w.severity === 'warning' ? 'warning' : 'info'"
          >
            <svg class="warning-icon" viewBox="0 0 20 20" fill="currentColor">
              <path v-if="w.severity === 'critical'" fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
              <path v-else fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
            </svg>
            <span>{{ w.message }}</span>
          </div>
        </div>
      </section>

    </div><!-- /overview -->

    <!-- ===== TAB: CASHFLOW ===== -->
    <div v-show="activeTab === 'cashflow'" class="tab-content">

      <section class="panel">
        <header class="panel-header">
          <h3 class="panel-title">{{ t('summary.cashflowChart') }}</h3>
        </header>
        <div class="panel-body chart-container">
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
      </section>

      <!-- Financing Chart -->
      <section class="panel">
        <header class="panel-header">
          <h3 class="panel-title">{{ t('summary.financingChart') }}</h3>
        </header>
        <div class="panel-body chart-container">
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
      </section>

      <!-- Cashflow Table -->
      <section class="panel">
        <header class="panel-header">
          <h3 class="panel-title">{{ t('summary.cashflowTable') }}</h3>
        </header>
        <div class="table-scroll">
          <table class="data-table cashflow-table">
            <thead>
              <tr>
                <th class="sticky-col">{{ t('summary.cashflow.year') }}</th>
                <th class="num">{{ t('summary.cashflow.effectiveRent') }} <HelpIcon help-key="summary.cf.effectiveRent" /></th>
                <th class="num">{{ t('summary.cashflow.operatingCosts') }}</th>
                <th v-if="hasMaintenanceReserve" class="num">{{ t('summary.cashflow.maintenanceReserve') }}</th>
                <th class="num">{{ t('summary.cashflow.noi') }} <HelpIcon help-key="summary.cf.noi" /></th>
                <th class="num">{{ t('summary.cashflow.interest') }}</th>
                <th class="num">{{ t('summary.cashflow.principal') }}</th>
                <th class="num">{{ t('summary.cashflow.capex') }}</th>
                <th v-if="hasMaintenanceReserve" class="num">{{ t('summary.cashflow.capexNet') }}</th>
                <th v-if="hasMaintenanceReserve" class="num">{{ t('summary.cashflow.reserveBalance') }}</th>
                <th class="num">{{ t('summary.cashflow.beforeTax') }} <HelpIcon help-key="summary.cf.beforeTax" /></th>
                <th class="num">{{ t('summary.cashflow.tax') }}</th>
                <th class="num highlight-col">{{ t('summary.cashflow.afterTax') }} <HelpIcon help-key="summary.cf.afterTax" /></th>
                <th class="num">{{ t('summary.cashflow.cumulative') }} <HelpIcon help-key="summary.cf.cumulative" /></th>
                <th class="num">{{ t('summary.cashflow.debt') }}</th>
                <th class="num">{{ t('summary.cashflow.ltv') }}</th>
                <th class="num">{{ t('summary.cashflow.dscr') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in result.yearlyCashflows" :key="row.year">
                <td class="sticky-col year-col">{{ row.year }}</td>
                <td class="num">{{ fmtCompact(row.effectiveRent) }}</td>
                <td class="num" :class="valClass(-row.operatingCosts)">{{ fmtCompact(-row.operatingCosts) }}</td>
                <td v-if="hasMaintenanceReserve" class="num" :class="valClass(-row.maintenanceReserve)">{{ fmtCompact(-row.maintenanceReserve) }}</td>
                <td class="num" :class="valClass(row.netOperatingIncome)">{{ fmtCompact(row.netOperatingIncome) }}</td>
                <td class="num" :class="valClass(-row.interestPortion)">{{ fmtCompact(-row.interestPortion) }}</td>
                <td class="num" :class="valClass(-row.principalPortion)">{{ fmtCompact(-row.principalPortion) }}</td>
                <td class="num" :class="{ neg: row.capexPayments > 0 }">{{ row.capexPayments > 0 ? fmtCompact(-row.capexPayments) : '–' }}</td>
                <td v-if="hasMaintenanceReserve" class="num" :class="{ neg: row.capexFromCashflow > 0 }">{{ row.capexFromCashflow > 0 ? fmtCompact(-row.capexFromCashflow) : '–' }}</td>
                <td v-if="hasMaintenanceReserve" class="num">{{ fmtCompact(row.reserveBalanceEnd) }}</td>
                <td class="num" :class="valClass(row.cashflowBeforeTax)">{{ fmtCompact(row.cashflowBeforeTax) }}</td>
                <td class="num" :class="valClass(-row.taxPayment)">{{ fmtCompact(-row.taxPayment) }}</td>
                <td class="num highlight-col" :class="valClass(row.cashflowAfterTax)">
                  <strong>{{ fmtCompact(row.cashflowAfterTax) }}</strong>
                </td>
                <td class="num" :class="valClass(row.cumulativeCashflow)">{{ fmtCompact(row.cumulativeCashflow) }}</td>
                <td class="num">{{ fmtCompact(row.outstandingDebt) }}</td>
                <td class="num">{{ fmtPct(row.ltvPercent) }}</td>
                <td class="num" :class="{ warn: row.dscrYear < 1.2 && row.dscrYear > 0 }">{{ row.dscrYear > 0 ? row.dscrYear.toFixed(2) : '–' }}</td>
              </tr>
            </tbody>
            <tfoot>
              <tr>
                <td class="sticky-col">{{ t('common.total') }}</td>
                <td class="num">{{ fmtCompact(sumCol('effectiveRent')) }}</td>
                <td class="num" :class="valClass(-sumCol('operatingCosts'))">{{ fmtCompact(-sumCol('operatingCosts')) }}</td>
                <td v-if="hasMaintenanceReserve" class="num" :class="valClass(-sumCol('maintenanceReserve'))">{{ fmtCompact(-sumCol('maintenanceReserve')) }}</td>
                <td class="num" :class="valClass(sumCol('netOperatingIncome'))">{{ fmtCompact(sumCol('netOperatingIncome')) }}</td>
                <td class="num" :class="valClass(-sumCol('interestPortion'))">{{ fmtCompact(-sumCol('interestPortion')) }}</td>
                <td class="num" :class="valClass(-sumCol('principalPortion'))">{{ fmtCompact(-sumCol('principalPortion')) }}</td>
                <td class="num" :class="valClass(-sumCol('capexPayments'))">{{ fmtCompact(-sumCol('capexPayments')) }}</td>
                <td v-if="hasMaintenanceReserve" class="num" :class="valClass(-sumCol('capexFromCashflow'))">{{ fmtCompact(-sumCol('capexFromCashflow')) }}</td>
                <td v-if="hasMaintenanceReserve"></td>
                <td class="num" :class="valClass(result.totalCashflowBeforeTax)">{{ fmtCompact(result.totalCashflowBeforeTax) }}</td>
                <td class="num" :class="valClass(-sumCol('taxPayment'))">{{ fmtCompact(-sumCol('taxPayment')) }}</td>
                <td class="num highlight-col" :class="valClass(result.totalCashflowAfterTax)">{{ fmtCompact(result.totalCashflowAfterTax) }}</td>
                <td></td><td></td><td></td><td></td>
              </tr>
            </tfoot>
          </table>
        </div>
      </section>

    </div><!-- /cashflow -->

    <!-- ===== TAB: TAX ===== -->
    <div v-show="activeTab === 'tax'" class="tab-content">

      <!-- Tax Bridge -->
      <section class="panel">
        <header class="panel-header">
          <h3 class="panel-title">{{ t('summary.taxBridge') }}</h3>
        </header>
        <div class="panel-body">
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
        </div>
      </section>

      <!-- Tax Summary -->
      <section class="panel">
        <header class="panel-header">
          <h3 class="panel-title">{{ t('summary.taxSummary') }}</h3>
        </header>
        <div class="panel-body panel-body-rows">
          <div class="data-row">
            <span class="data-label">{{ t('summary.tax.depreciationRate') }}</span>
            <span class="data-value">{{ fmtPct(result.taxSummary.depreciationRatePercent) }}</span>
          </div>
          <div class="data-row">
            <span class="data-label">{{ t('summary.tax.depreciationBasis') }}</span>
            <span class="data-value">{{ fmtCur(result.taxSummary.depreciationBasis.amount) }}</span>
          </div>
          <div class="data-row">
            <span class="data-label">{{ t('summary.tax.annualDepreciation') }}</span>
            <span class="data-value">{{ fmtCur(result.taxSummary.annualDepreciation.amount) }}</span>
          </div>
          <div class="data-row subtotal">
            <span class="data-label">{{ t('summary.tax.totalDepreciation') }}</span>
            <span class="data-value">{{ fmtCur(result.taxSummary.totalDepreciation.amount) }}</span>
          </div>
          <div class="data-row">
            <span class="data-label">{{ t('summary.tax.totalInterest') }}</span>
            <span class="data-value">{{ fmtCur(result.taxSummary.totalInterestDeduction.amount) }}</span>
          </div>
          <div class="data-row">
            <span class="data-label">{{ t('summary.tax.totalMaintenance') }}</span>
            <span class="data-value">{{ fmtCur(result.taxSummary.totalMaintenanceDeduction.amount) }}</span>
          </div>
          <div class="data-row">
            <span class="data-label">{{ t('summary.tax.totalOperating') }}</span>
            <span class="data-value">{{ fmtCur(result.taxSummary.totalOperatingDeduction.amount) }}</span>
          </div>
          <div v-if="hasMaintenanceReserve" class="data-row muted-row">
            <span class="data-label">{{ t('summary.tax.totalMaintenanceReserve') }}</span>
            <span class="data-value value-muted">{{ fmtCur(result.taxSummary.totalMaintenanceReserve.amount) }}</span>
          </div>
          <div class="data-row subtotal">
            <span class="data-label">{{ t('summary.tax.totalSavings') }} <HelpIcon help-key="summary.tax.totalSavings" /></span>
            <span class="data-value pos">{{ fmtCur(result.taxSummary.totalTaxSavings.amount) }}</span>
          </div>
          <div class="data-row strong-row">
            <span class="data-label">{{ t('summary.tax.totalTax') }}</span>
            <span class="data-value">{{ fmtCur(result.taxSummary.totalTaxPayment.amount) }}</span>
          </div>
          <div class="data-row">
            <span class="data-label">{{ t('summary.tax.effectiveRate') }} <HelpIcon help-key="summary.tax.effectiveRate" /></span>
            <span class="data-value">{{ fmtPct(result.taxSummary.effectiveTaxRatePercent) }}</span>
          </div>
          <div class="data-row">
            <span class="data-label">{{ t('summary.tax.rule15') }} <HelpIcon help-key="summary.tax.rule15" /></span>
            <span class="data-value">
              <span class="badge" :class="result.taxSummary.acquisitionRelatedCostsTriggered ? 'badge-warning' : 'badge-neutral'">
                {{ result.taxSummary.acquisitionRelatedCostsTriggered
                  ? t('summary.tax.rule15Triggered')
                  : t('summary.tax.rule15NotTriggered')
                }}
              </span>
            </span>
          </div>
        </div>
      </section>

    </div><!-- /tax -->

    <!-- ===== TAB: PROPERTY ===== -->
    <div v-show="activeTab === 'property'" class="tab-content">

      <!-- Property Value Forecast -->
      <section class="panel">
        <header class="panel-header">
          <div>
            <h3 class="panel-title">{{ t('summary.propertyValue.title') }}</h3>
            <p class="panel-subtitle">{{ t('summary.propertyValue.subtitle') }}</p>
          </div>
        </header>
        <div class="panel-body">

          <!-- Market Comparison -->
          <div v-if="result.propertyValueForecast.marketComparison" class="kpi-grid kpi-grid-3 kpi-compact block-gap">
            <div class="kpi">
              <span class="kpi-label">{{ t('summary.propertyValue.regionalPrice') }}</span>
              <span class="kpi-value">{{ fmtCur(result.propertyValueForecast.marketComparison.regionalPricePerSqm) }}<span class="kpi-unit">/m²</span></span>
            </div>
            <div class="kpi">
              <span class="kpi-label">{{ t('summary.propertyValue.fairMarketValue') }}</span>
              <span class="kpi-value">{{ fmtCur(result.propertyValueForecast.marketComparison.fairMarketValue) }}</span>
            </div>
            <div class="kpi" :class="marketAssessmentClass">
              <span class="kpi-label">{{ t('summary.propertyValue.purchaseVsMarket') }}</span>
              <span class="kpi-value">{{ fmtPct((result.propertyValueForecast.marketComparison.purchasePriceToMarketRatio - 1) * 100) }}</span>
              <span class="kpi-sub">{{ t(`summary.propertyValue.assessment.${result.propertyValueForecast.marketComparison.assessment}`) }}</span>
            </div>
          </div>

          <div class="kpi-grid kpi-grid-2 kpi-compact block-gap">
            <div class="kpi">
              <span class="kpi-label">{{ t('summary.propertyValue.initialCondition') }} <HelpIcon help-key="summary.property.conditionFactor" /></span>
              <span class="kpi-value">{{ (result.propertyValueForecast.initialConditionFactor * 100).toFixed(0) }}%</span>
            </div>
            <div class="kpi">
              <span class="kpi-label">{{ t('summary.propertyValue.improvementFactor') }} <HelpIcon help-key="summary.property.forecast" /></span>
              <span class="kpi-value">{{ (result.propertyValueForecast.improvementValueFactor * 100).toFixed(0) }}%</span>
            </div>
          </div>

          <!-- Value chart: bars per year for base scenario -->
          <div class="chart-container">
            <div class="pv-chart">
              <div class="pv-baseline" :style="{ bottom: pvBarPct(result.propertyValueForecast.purchasePrice) }">
                <span class="pv-baseline-label">{{ fmtCompact(result.propertyValueForecast.purchasePrice) }}</span>
              </div>
              <div v-for="row in baseScenarioValues" :key="row.year" class="pv-bar-group">
                <div class="pv-bar" :style="{ height: pvBarPct(row.estimatedValue) }"
                  :title="`${row.year}: ${fmtCur(row.estimatedValue)} (Zustand: ${(row.conditionFactor * 100).toFixed(0)}%)`">
                  <span v-if="row.conditionFactor < 0.80" class="pv-condition-warn">!</span>
                </div>
                <span class="bar-label">{{ row.year.toString().slice(-2) }}</span>
              </div>
            </div>
            <div class="chart-legend">
              <span class="legend-item"><span class="swatch swatch-pv"></span> {{ t('summary.propertyValue.scenario.base') }} (1,5% p.a.)</span>
              <span class="legend-item pv-baseline-legend">{{ t('summary.propertyValue.purchasePrice') }}</span>
            </div>
          </div>
        </div>

        <!-- Scenario breakdown table -->
        <div class="table-scroll table-bordered-top">
          <table class="data-table scenario-table">
            <thead>
              <tr>
                <th class="scenario-label-col"></th>
                <th v-for="s in result.propertyValueForecast.scenarios" :key="s.label" class="num scenario-col" :class="{ 'scenario-base': s.label === 'base' }">
                  {{ t(`summary.propertyValue.scenario.${s.label}`) }}
                  <span class="scenario-rate">{{ fmtPct(s.annualAppreciationPercent) }} p.a.</span>
                </th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>{{ t('summary.propertyValue.purchasePrice') }}</td>
                <td v-for="s in result.propertyValueForecast.scenarios" :key="s.label" class="num" :class="{ 'scenario-base': s.label === 'base' }">{{ fmtCur(result.propertyValueForecast.purchasePrice) }}</td>
              </tr>
              <tr>
                <td>{{ t('summary.propertyValue.marketAppreciationRow') }}</td>
                <td v-for="(s, idx) in result.propertyValueForecast.scenarios" :key="s.label" class="num"
                  :class="[valClass(scenarioBreakdowns[idx].marketAppreciation), { 'scenario-base': s.label === 'base' }]">
                  {{ scenarioBreakdowns[idx].marketAppreciation >= 0 ? '+' : '' }}{{ fmtCur(scenarioBreakdowns[idx].marketAppreciation) }}
                </td>
              </tr>
              <tr>
                <td>{{ t('summary.propertyValue.conditionAdjustment') }}</td>
                <td v-for="(s, idx) in result.propertyValueForecast.scenarios" :key="s.label" class="num"
                  :class="[valClass(scenarioBreakdowns[idx].conditionAdjustment), { 'scenario-base': s.label === 'base' }]">
                  {{ fmtCur(scenarioBreakdowns[idx].conditionAdjustment) }}
                  <span v-if="!hasComponentDeterioration" class="scenario-rate">({{ (scenarioBreakdowns[idx].conditionFactor * 100).toFixed(0) }}%)</span>
                </td>
              </tr>
              <tr v-if="hasInvestments">
                <td>{{ t('summary.propertyValue.investmentRow') }}</td>
                <td v-for="(s, idx) in result.propertyValueForecast.scenarios" :key="s.label" class="num"
                  :class="[valClass(scenarioBreakdowns[idx].investments), { 'scenario-base': s.label === 'base' }]">
                  +{{ fmtCur(scenarioBreakdowns[idx].investments) }}
                </td>
              </tr>
              <tr v-if="hasMeanReversion">
                <td>{{ t('summary.propertyValue.meanReversionRow') }}</td>
                <td v-for="(s, idx) in result.propertyValueForecast.scenarios" :key="s.label" class="num"
                  :class="[valClass(scenarioBreakdowns[idx].meanReversion), { 'scenario-base': s.label === 'base' }]">
                  {{ scenarioBreakdowns[idx].meanReversion >= 0 ? '+' : '' }}{{ fmtCur(scenarioBreakdowns[idx].meanReversion) }}
                </td>
              </tr>
              <tr class="scenario-total">
                <td>{{ t('summary.propertyValue.finalValue') }}</td>
                <td v-for="s in result.propertyValueForecast.scenarios" :key="s.label" class="num" :class="{ 'scenario-base': s.label === 'base' }">
                  {{ fmtCur(s.finalValue) }}
                </td>
              </tr>
              <tr class="scenario-highlight">
                <td>{{ t('summary.propertyValue.vs') }}</td>
                <td v-for="s in result.propertyValueForecast.scenarios" :key="s.label" class="num"
                  :class="[valClass(s.finalValue - result.propertyValueForecast.purchasePrice), { 'scenario-base': s.label === 'base' }]">
                  {{ s.finalValue >= result.propertyValueForecast.purchasePrice ? '+' : '' }}{{ fmtCur(s.finalValue - result.propertyValueForecast.purchasePrice) }}
                  <span class="scenario-rate">{{ s.finalValue >= result.propertyValueForecast.purchasePrice ? '+' : '' }}{{ fmtPct(((s.finalValue / result.propertyValueForecast.purchasePrice) - 1) * 100) }}</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Forecast Explanation -->
        <div v-if="forecastDriverTexts.length > 0" class="panel-body forecast-explanation">
          <h4 class="section-title block-title">{{ t('summary.propertyValue.explanationTitle') }}</h4>
          <ul class="explanation-list">
            <li v-for="(driver, i) in forecastDriverTexts" :key="i" :class="driver.type">
              {{ driver.text }}
            </li>
          </ul>
        </div>
      </section>

      <!-- Component Deterioration -->
      <section v-if="hasComponentDeterioration" class="panel">
        <header class="panel-header">
          <div>
            <h3 class="panel-title">{{ t('summary.propertyValue.componentDeterioration.title') }}</h3>
            <p class="panel-subtitle">{{ t('summary.propertyValue.componentDeterioration.subtitle') }} <HelpIcon help-key="summary.property.componentDeterioration" /></p>
          </div>
        </header>
        <div class="panel-body">

          <!-- Status Summary Strip -->
          <div class="cdet-summary-strip">
            <div class="cdet-counters">
              <span v-if="detStatusCounts.ok > 0" class="badge badge-success">
                <span class="cdet-counter-num">{{ detStatusCounts.ok }}</span>
                {{ t('summary.propertyValue.componentDeterioration.statusLabels.OK') }}
              </span>
              <span v-if="detStatusCounts.renewed > 0" class="badge badge-info">
                <span class="cdet-counter-num">{{ detStatusCounts.renewed }}</span>
                {{ t('summary.propertyValue.componentDeterioration.statusLabels.Renewed') }}
              </span>
              <span v-if="detStatusCounts.overdue > 0" class="badge badge-error">
                <span class="cdet-counter-num">{{ detStatusCounts.overdue }}</span>
                {{ t('summary.propertyValue.componentDeterioration.statusLabels.Overdue') }}
              </span>
              <span v-if="detStatusCounts.overdueAtPurchase > 0" class="badge badge-neutral">
                <span class="cdet-counter-num">{{ detStatusCounts.overdueAtPurchase }}</span>
                {{ t('summary.propertyValue.componentDeterioration.statusLabels.OverdueAtPurchase') }}
              </span>
            </div>
            <div class="cdet-impact-total">
              <span class="kpi-label">{{ t('summary.propertyValue.componentDeterioration.valueImpact') }}</span>
              <strong :class="valClass(componentDeteriorationSummary?.totalValueImpact ?? 0)">{{ fmtCur(componentDeteriorationSummary?.totalValueImpact ?? 0) }}</strong>
            </div>
          </div>

          <!-- Component Cards Grid -->
          <div class="cdet-grid">
            <div v-for="row in componentDeteriorationRows" :key="row.category"
                 class="cdet-card" :class="['cdet-' + row.statusAtEnd, { expanded: expandedDetRows.has(row.category) }]"
                 role="button" tabindex="0"
                 :aria-expanded="expandedDetRows.has(row.category)"
                 @click="toggleDetRow(row.category)"
                 @keydown.enter="toggleDetRow(row.category)">
              <div class="cdet-card-header">
                <span class="cdet-card-name">{{ t(`property.components.categories.${row.category}`) }}</span>
                <span class="badge" :class="statusBadgeClass(row.statusAtEnd)">
                  {{ t(`summary.propertyValue.componentDeterioration.statusLabels.${row.statusAtEnd}`) }}
                </span>
              </div>
              <div class="cdet-card-metrics">
                <div class="cdet-metric">
                  <span class="cdet-metric-label">{{ t('summary.propertyValue.componentDeterioration.age') }} / {{ t('summary.propertyValue.componentDeterioration.cycleYears') }}</span>
                  <span class="cdet-metric-value">{{ row.ageAtEnd }} / {{ row.cycleYears }} {{ t('summary.propertyValue.componentDeterioration.yearsShort') }}</span>
                </div>
                <div class="cdet-metric">
                  <span class="cdet-metric-label">{{ t('summary.propertyValue.componentDeterioration.dueYear') }}</span>
                  <span class="cdet-metric-value">{{ row.dueYear }}</span>
                </div>
                <div class="cdet-metric">
                  <span class="cdet-metric-label">{{ t('summary.propertyValue.componentDeterioration.valueImpact') }}</span>
                  <span class="cdet-metric-value" :class="row.statusAtEnd === 'OverdueAtPurchase' ? 'value-muted' : valClass(row.valueImpact)">
                    <template v-if="row.statusAtEnd === 'OverdueAtPurchase'">0 € ({{ t('summary.propertyValue.componentDeterioration.pricedIn') }})</template>
                    <template v-else>{{ fmtCur(row.valueImpact) }}</template>
                  </span>
                </div>
              </div>
              <div class="cdet-lifecycle-bar">
                <div class="cdet-lifecycle-fill" :class="'cdet-fill-' + row.statusAtEnd"
                     :style="{ width: Math.min((row.ageAtEnd / row.cycleYears) * 100, 100) + '%' }"></div>
              </div>
              <div class="cdet-detail">
                <div v-if="row.statusAtEnd === 'OverdueAtPurchase'" class="cdet-priced-in-hint">
                  {{ t('summary.propertyValue.componentDeterioration.pricedInHint') }}
                </div>
                <div class="cdet-detail-row">
                  <span>{{ t('summary.propertyValue.componentDeterioration.renewalCost') }}</span>
                  <strong>{{ fmtCur(row.renewalCostEstimate) }}</strong>
                </div>
                <div v-if="row.recurringMaintenance" class="cdet-recurring">
                  <span class="eyebrow cdet-recurring-badge">{{ t('summary.propertyValue.componentDeterioration.recurringLabel') }}</span>
                  <div class="cdet-recurring-details">
                    <div class="cdet-detail-row">
                      <span>{{ row.recurringMaintenance.name }} ({{ row.recurringMaintenance.intervalYears }} {{ t('summary.propertyValue.componentDeterioration.yearsShort') }})</span>
                      <span class="num">{{ row.recurringMaintenance.occurrencesInPeriod }}&times; {{ fmtCur(row.recurringMaintenance.costPerOccurrence) }}</span>
                    </div>
                    <div class="cdet-detail-row">
                      <span>{{ t('summary.propertyValue.componentDeterioration.cycleYears') }} (eff.)</span>
                      <strong>{{ row.recurringMaintenance.effectiveCycleYears }} {{ t('summary.propertyValue.componentDeterioration.yearsShort') }} (+{{ getRecurringExtensionPercent(row) }}%)</strong>
                    </div>
                    <div class="cdet-detail-row">
                      <span>{{ t('summary.propertyValue.componentDeterioration.valueImpact') }}</span>
                      <strong class="pos">+{{ fmtCur(row.recurringMaintenance.valueImprovement) }}</strong>
                    </div>
                  </div>
                </div>
              </div>
              <div class="cdet-expand-hint">
                <svg :class="{ rotated: expandedDetRows.has(row.category) }" width="12" height="12" viewBox="0 0 12 12" fill="none">
                  <path d="M3 4.5L6 7.5L9 4.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </div>
            </div>
          </div>

          <!-- Bottom Totals -->
          <div v-if="componentDeteriorationSummary" class="cdet-totals">
            <div class="data-row">
              <span class="data-label">{{ t('summary.propertyValue.componentDeterioration.renewalCost') }}</span>
              <span class="data-value">{{ fmtCur(componentDeteriorationSummary.totalRenewalCostIfAllDone) }}</span>
            </div>
            <div v-if="componentDeteriorationSummary.uncoveredDeterioration > 0" class="data-row">
              <span class="data-label">{{ t('summary.propertyValue.componentDeterioration.totalUncovered') }}</span>
              <span class="data-value neg">{{ fmtCur(-componentDeteriorationSummary.uncoveredDeterioration) }}</span>
            </div>
            <div v-if="componentDeteriorationSummary.coveredByCapex > 0" class="data-row">
              <span class="data-label">{{ t('summary.propertyValue.componentDeterioration.totalCovered') }}</span>
              <span class="data-value pos">{{ fmtCur(componentDeteriorationSummary.coveredByCapex) }}</span>
            </div>
          </div>
        </div>
      </section>

      <!-- CapEx Timeline -->
      <section v-if="result.capexTimeline.length > 0" class="panel">
        <header class="panel-header">
          <h3 class="panel-title">{{ t('summary.capexTimeline') }}</h3>
        </header>
        <div class="panel-body">
          <div class="captl-axis">
            <div class="captl-track">
              <template v-for="(year, i) in timelineYears" :key="'y' + year">
                <div class="captl-year-tick" :style="{ left: yearPosition(year) }">
                  <span v-if="i % timelineYearStep === 0 || i === timelineYears.length - 1" class="captl-year-label">{{ year }}</span>
                </div>
              </template>
              <div v-for="item in result.capexTimeline" :key="item.id"
                   class="captl-dot" :class="item.taxClassification"
                   :style="{ left: yearPosition(item.year), width: dotSize(item.amount), height: dotSize(item.amount) }">
              </div>
            </div>
          </div>

          <div class="captl-groups">
            <div v-for="group in capexByYear" :key="group.year" class="captl-group">
              <div class="captl-group-year">{{ group.year }}</div>
              <div class="captl-group-items">
                <div v-for="item in group.items" :key="item.id" class="captl-group-item">
                  <span class="captl-dot-inline" :class="item.taxClassification"></span>
                  <span class="captl-item-name">{{ item.name }}</span>
                  <span class="captl-item-tax">{{ t(`capex.taxClassifications.${item.taxClassification}`) }}</span>
                  <span class="captl-item-amount">{{ fmtCur(item.amount) }}</span>
                </div>
              </div>
              <div v-if="group.items.length > 1" class="captl-group-total">{{ fmtCur(group.total) }}</div>
            </div>
          </div>

          <div class="data-row total">
            <span class="data-label">{{ t('common.total') }}</span>
            <span class="data-value">{{ fmtCur(capexTotal) }}</span>
          </div>
        </div>
      </section>

      <!-- Exit Analysis -->
      <section class="panel">
        <header class="panel-header">
          <div>
            <h3 class="panel-title">{{ t('summary.exit.title') }}</h3>
            <p class="panel-subtitle">{{ t('summary.exit.subtitle') }}</p>
          </div>
        </header>
        <div class="panel-body">
          <div class="kpi-grid kpi-grid-2 kpi-compact block-gap">
            <div class="kpi">
              <span class="kpi-label">{{ t('summary.exit.holdingPeriod') }}</span>
              <span class="kpi-value">{{ result.exitAnalysis.holdingPeriodYears }} {{ t('summary.exit.years') }}</span>
            </div>
            <div class="kpi" :class="{ 'kpi-warn': result.exitAnalysis.isWithinSpeculationPeriod }">
              <span class="kpi-label">{{ t('summary.exit.speculationPeriod') }}</span>
              <span class="kpi-value kpi-value-text">{{ result.exitAnalysis.isWithinSpeculationPeriod ? t('summary.exit.withinPeriod') : t('summary.exit.outsidePeriod') }}</span>
            </div>
          </div>

          <h4 class="section-title block-title">{{ t('summary.exit.pnlTitle') }}</h4>
          <div class="exit-pnl">
            <div class="data-row">
              <span class="data-label">{{ t('summary.exit.totalGrossIncome') }}</span>
              <span class="data-value pos">+{{ fmtCur(result.exitAnalysis.totalGrossIncome) }}</span>
            </div>
            <div class="data-row">
              <span class="data-label">{{ t('summary.exit.totalOperatingCosts') }}</span>
              <span class="data-value neg">-{{ fmtCur(result.exitAnalysis.totalOperatingCosts) }}</span>
            </div>
            <div class="data-row">
              <span class="data-label">{{ t('summary.exit.totalDebtService') }}</span>
              <span class="data-value neg">-{{ fmtCur(result.exitAnalysis.totalDebtService) }}</span>
            </div>
            <div class="data-row" v-if="result.exitAnalysis.totalCapex > 0">
              <span class="data-label">{{ t('summary.exit.totalCapex') }}</span>
              <span class="data-value neg">-{{ fmtCur(result.exitAnalysis.totalCapex) }}</span>
            </div>
            <div v-if="hasMaintenanceReserve" class="data-row">
              <span class="data-label">{{ t('summary.exit.totalMaintenanceReserve') }}</span>
              <span class="data-value neg">-{{ fmtCur(result.exitAnalysis.totalMaintenanceReserve) }}</span>
            </div>
            <div v-if="hasMaintenanceReserve && result.exitAnalysis.finalReserveBalance > 0" class="data-row">
              <span class="data-label">{{ t('summary.exit.finalReserveBalance') }}</span>
              <span class="data-value pos">+{{ fmtCur(result.exitAnalysis.finalReserveBalance) }}</span>
            </div>
            <div class="data-row">
              <span class="data-label">{{ t('summary.exit.totalTaxPaid') }}</span>
              <span class="data-value neg">-{{ fmtCur(result.exitAnalysis.totalTaxPaid) }}</span>
            </div>
            <div class="data-row total">
              <span class="data-label">{{ t('summary.exit.netCashflow') }}</span>
              <span class="data-value" :class="valClass(result.exitAnalysis.totalCashflowAfterTax)">{{ fmtCur(result.exitAnalysis.totalCashflowAfterTax) }}</span>
            </div>
          </div>

          <h4 class="section-title block-title">{{ t('summary.exit.scenarioTitle') }}</h4>
        </div>

        <div class="table-scroll table-bordered-top">
          <table class="data-table scenario-table">
            <thead>
              <tr>
                <th class="scenario-label-col"></th>
                <th v-for="s in result.exitAnalysis.scenarios" :key="s.label" class="num scenario-col" :class="{ 'scenario-base': s.label === 'base' }">
                  {{ t(`summary.exit.scenario.${s.label}`) }}
                  <span class="scenario-rate">{{ fmtPct(s.annualAppreciationPercent) }} p.a.</span>
                </th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>{{ t('summary.exit.propertyValue') }}</td>
                <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" class="num" :class="{ 'scenario-base': s.label === 'base' }">{{ fmtCur(s.propertyValueAtExit) }}</td>
              </tr>
              <tr>
                <td>{{ t('summary.exit.saleCosts') }}</td>
                <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" class="num neg" :class="{ 'scenario-base': s.label === 'base' }">-{{ fmtCur(s.saleCosts) }}</td>
              </tr>
              <tr v-if="result.exitAnalysis.isWithinSpeculationPeriod">
                <td>{{ t('summary.exit.capitalGainsTax') }}</td>
                <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" class="num" :class="{ neg: s.capitalGainsTax > 0, 'scenario-base': s.label === 'base' }">
                  {{ s.capitalGainsTax > 0 ? `-${fmtCur(s.capitalGainsTax)}` : t('summary.exit.noTax') }}
                </td>
              </tr>
              <tr v-else>
                <td>{{ t('summary.exit.capitalGainsTax') }}</td>
                <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" class="num value-muted" :class="{ 'scenario-base': s.label === 'base' }">{{ t('summary.exit.noTax') }}</td>
              </tr>
              <tr>
                <td>{{ t('summary.exit.outstandingDebt') }}</td>
                <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" class="num neg" :class="{ 'scenario-base': s.label === 'base' }">-{{ fmtCur(result.exitAnalysis.outstandingDebtAtExit) }}</td>
              </tr>
              <tr class="scenario-subtotal">
                <td>{{ t('summary.exit.netSaleProceeds') }}</td>
                <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" class="num" :class="[valClass(s.netSaleProceeds), { 'scenario-base': s.label === 'base' }]">{{ fmtCur(s.netSaleProceeds) }}</td>
              </tr>
              <tr>
                <td>{{ t('summary.exit.plusCashflow') }}</td>
                <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" class="num" :class="[valClass(result.exitAnalysis.totalCashflowAfterTax), { 'scenario-base': s.label === 'base' }]">{{ result.exitAnalysis.totalCashflowAfterTax >= 0 ? '+' : '' }}{{ fmtCur(result.exitAnalysis.totalCashflowAfterTax) }}</td>
              </tr>
              <tr>
                <td>{{ t('summary.exit.minusEquity') }}</td>
                <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" class="num neg" :class="{ 'scenario-base': s.label === 'base' }">-{{ fmtCur(result.exitAnalysis.equityInvested) }}</td>
              </tr>
              <tr class="scenario-total">
                <td>{{ t('summary.exit.totalReturn') }} <HelpIcon help-key="summary.exit.totalReturn" /></td>
                <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" class="num" :class="[valClass(s.totalReturn), { 'scenario-base': s.label === 'base' }]">
                  {{ fmtCur(s.totalReturn) }}
                </td>
              </tr>
              <tr class="scenario-highlight">
                <td>{{ t('summary.exit.totalReturnPercent') }}</td>
                <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" class="num" :class="[valClass(s.totalReturnPercent), { 'scenario-base': s.label === 'base' }]">
                  {{ fmtPct(s.totalReturnPercent) }}
                </td>
              </tr>
              <tr class="scenario-highlight">
                <td>{{ t('summary.exit.annualizedReturn') }} <HelpIcon help-key="summary.exit.annualizedReturn" /></td>
                <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" class="num" :class="[valClass(s.annualizedReturnPercent), { 'scenario-base': s.label === 'base' }]">
                  {{ fmtPct(s.annualizedReturnPercent) }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

    </div><!-- /property -->

      <!-- Actions (always visible) -->
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
import { HelpIcon } from '@/components';
import { useProjectStore } from '@/stores/projectStore';
import { useUiStore } from '@/stores/uiStore';
import { useAuthStore } from '@/stores/authStore';
import { projectsApi, calculationApi } from '@/api';
import type { CalculationResult, YearlyCashflowRow, CapExTimelineItem } from '@/stores/types';

const emit = defineEmits<{
  'validation-change': [valid: boolean];
}>();

const { t, locale } = useI18n();
const projectStore = useProjectStore();
const uiStore = useUiStore();
const authStore = useAuthStore();

const isCalculating = ref(false);
const result = ref<CalculationResult | null>(null);

type TabId = 'overview' | 'cashflow' | 'tax' | 'property';
const activeTab = ref<TabId>('overview');
const warningsOpen = ref(false);

const tabs: { id: TabId; label: string }[] = [
  { id: 'overview', label: 'summary.tabs.overview' },
  { id: 'cashflow', label: 'summary.tabs.cashflow' },
  { id: 'tax', label: 'summary.tabs.tax' },
  { id: 'property', label: 'summary.tabs.property' },
];

const hasCriticalWarning = computed(() =>
  result.value?.warnings.some(w => w.severity === 'critical') ?? false
);
const warningCount = computed(() => result.value?.warnings.length ?? 0);

const project = computed(() => projectStore.currentProject);
const currency = computed(() => project.value?.currency || 'EUR');
const totalInvestment = computed(() => projectStore.totalCapitalRequirement?.amount || 0);
const hasMaintenanceReserve = computed(() =>
  (project.value?.costs.maintenanceReserveMonthly?.amount || 0) > 0
);
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

// CapEx horizontal timeline helpers
const timelineYears = computed(() => {
  if (!result.value || !project.value) return [];
  const start = project.value.purchase.purchaseDate.year;
  const end = Math.max(start, project.value.endPeriod.year);
  const years: number[] = [];
  for (let y = start; y <= end; y++) years.push(y);
  return years;
});
function yearPosition(year: number): string {
  const years = timelineYears.value;
  if (years.length <= 1) return '0%';
  return ((year - years[0]) / (years[years.length - 1] - years[0])) * 100 + '%';
}
function dotSize(amount: number): string {
  return Math.max(8, Math.min(20, (amount / maxCapex.value) * 20)) + 'px';
}
const capexByYear = computed(() => {
  if (!result.value) return [];
  const map = new Map<number, { year: number; items: CapExTimelineItem[]; total: number }>();
  for (const item of result.value.capexTimeline) {
    if (!map.has(item.year)) map.set(item.year, { year: item.year, items: [], total: 0 });
    const g = map.get(item.year)!;
    g.items.push(item);
    g.total += item.amount;
  }
  return [...map.values()].sort((a, b) => a.year - b.year);
});
const capexTotal = computed(() =>
  result.value?.capexTimeline.reduce((s, i) => s + i.amount, 0) ?? 0
);
// Show only every Nth year label to avoid overlap on long timelines
const timelineYearStep = computed(() => {
  const len = timelineYears.value.length;
  if (len <= 12) return 1;
  if (len <= 20) return 2;
  return 5;
});

const maxPvValue = computed(() => {
  if (!result.value) return 1;
  const base = result.value.propertyValueForecast.scenarios.find(s => s.label === 'base');
  if (!base || base.yearlyValues.length === 0) return result.value.propertyValueForecast.purchasePrice || 1;
  return Math.max(...base.yearlyValues.map(r => r.estimatedValue), result.value.propertyValueForecast.purchasePrice, 1);
});
const minPvValue = computed(() => {
  if (!result.value) return 0;
  const base = result.value.propertyValueForecast.scenarios.find(s => s.label === 'base');
  if (!base || base.yearlyValues.length === 0) return result.value.propertyValueForecast.purchasePrice || 0;
  return Math.min(...base.yearlyValues.map(r => r.estimatedValue), result.value.propertyValueForecast.purchasePrice);
});
const baseScenarioValues = computed(() => {
  if (!result.value) return [];
  const base = result.value.propertyValueForecast.scenarios.find(s => s.label === 'base');
  return base?.yearlyValues ?? [];
});
const marketAssessmentClass = computed(() => {
  const mc = result.value?.propertyValueForecast.marketComparison;
  if (!mc) return '';
  return {
    'mc-below': mc.assessment === 'below',
    'mc-at': mc.assessment === 'at',
    'mc-above': mc.assessment === 'above',
  };
});
const forecastDriverTexts = computed(() => {
  if (!result.value) return [];
  return result.value.propertyValueForecast.drivers.map(d => ({
    type: d.type,
    text: t(`summary.propertyValue.driver.${d.type}`, d.params),
  }));
});
const scenarioBreakdowns = computed(() => {
  if (!result.value) return [];
  const pp = result.value.propertyValueForecast.purchasePrice;
  const initialFactor = result.value.propertyValueForecast.initialConditionFactor;
  const hasComponentDet = !!result.value.propertyValueForecast.componentDeterioration;
  return result.value.propertyValueForecast.scenarios.map(s => {
    const last = s.yearlyValues[s.yearlyValues.length - 1];
    if (!last) return { marketAppreciation: 0, conditionFactor: 1, conditionRatio: 1, conditionAdjustment: 0, investments: 0, meanReversion: 0 };
    if (hasComponentDet) {
      // Cost-based model: conditionAdjustment = cumulative EUR deterioration
      return {
        marketAppreciation: last.marketValue - pp,
        conditionFactor: last.conditionFactor,
        conditionRatio: 1,
        conditionAdjustment: last.componentDeteriorationCumulative,
        investments: last.improvementUplift,
        meanReversion: last.meanReversionAdjustment,
      };
    }
    // Abstract percentage model (fallback)
    const conditionRatio = initialFactor > 0 ? last.conditionFactor / initialFactor : 1;
    return {
      marketAppreciation: last.marketValue - pp,
      conditionFactor: last.conditionFactor,
      conditionRatio,
      conditionAdjustment: last.marketValue * (conditionRatio - 1),
      investments: last.improvementUplift,
      meanReversion: last.meanReversionAdjustment,
    };
  });
});
const hasInvestments = computed(() => scenarioBreakdowns.value.some(b => b.investments > 0));
const hasMeanReversion = computed(() => scenarioBreakdowns.value.some(b => Math.abs(b.meanReversion) > 0.5));
const hasComponentDeterioration = computed(() =>
  !!result.value?.propertyValueForecast.componentDeterioration &&
  result.value.propertyValueForecast.componentDeterioration.components.length > 0
);
const componentDeteriorationRows = computed(() =>
  result.value?.propertyValueForecast.componentDeterioration?.components ?? []
);
const componentDeteriorationSummary = computed(() =>
  result.value?.propertyValueForecast.componentDeterioration
);

// Component deterioration card expand state
const expandedDetRows = ref<Set<string>>(new Set());
function toggleDetRow(category: string) {
  const next = new Set(expandedDetRows.value);
  if (next.has(category)) { next.delete(category); } else { next.add(category); }
  expandedDetRows.value = next;
}
const detStatusCounts = computed(() => {
  const rows = componentDeteriorationRows.value;
  return {
    ok: rows.filter(r => r.statusAtEnd === 'OK').length,
    renewed: rows.filter(r => r.statusAtEnd === 'Renewed').length,
    overdue: rows.filter(r => r.statusAtEnd === 'Overdue').length,
    overdueAtPurchase: rows.filter(r => r.statusAtEnd === 'OverdueAtPurchase').length,
  };
});

// Recurring maintenance helper
function getRecurringExtensionPercent(row: { category: string; cycleYears: number; recurringMaintenance?: { effectiveCycleYears: number } }): number {
  if (!row.recurringMaintenance) return 0;
  const comp = project.value?.property.components.find(c => c.category === row.category);
  const originalCycle = comp?.expectedCycleYears || row.cycleYears;
  return originalCycle > 0 ? Math.round(((row.recurringMaintenance.effectiveCycleYears / originalCycle) - 1) * 100) : 0;
}

// Formatters
// Werte, die auf 0 runden, ohne Vorzeichen anzeigen (verhindert "-0 €")
function noNegZero(v: number): number {
  return Math.abs(v) < 0.5 ? 0 : v;
}
function fmtCur(v: number): string {
  v = noNegZero(v);
  return new Intl.NumberFormat(locale.value, { style: 'currency', currency: currency.value, maximumFractionDigits: 0 }).format(v);
}
function fmtCompact(v: number): string {
  v = noNegZero(v);
  if (Math.abs(v) >= 1_000_000) {
    return new Intl.NumberFormat(locale.value, { style: 'currency', currency: currency.value, notation: 'compact', maximumFractionDigits: 1 }).format(v);
  }
  return new Intl.NumberFormat(locale.value, { style: 'currency', currency: currency.value, maximumFractionDigits: 0 }).format(v);
}
function fmtPct(v: number): string {
  return new Intl.NumberFormat(locale.value, { style: 'percent', minimumFractionDigits: 1, maximumFractionDigits: 2 }).format(v / 100);
}

function npvClass(v: number) { return { pos: v >= 0, neg: v < 0 }; }
function valClass(v: number) { return { pos: v > 0, neg: v < 0 }; }
function signClass(v: number) { return { neg: v < 0 }; }
function statusBadgeClass(status: string): string {
  switch (status) {
    case 'OK': return 'badge-success';
    case 'Renewed': return 'badge-info';
    case 'Overdue': return 'badge-error';
    default: return 'badge-neutral';
  }
}
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
function pvBarPct(value: number): string {
  const floor = minPvValue.value * 0.90;
  const ceiling = maxPvValue.value * 1.05;
  const range = ceiling - floor;
  if (range <= 0) return '50%';
  const pct = ((value - floor) / range) * 100;
  return Math.max(Math.min(pct, 100), 2) + '%';
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
    let r: CalculationResult;
    if (authStore.isAuthenticated) {
      // Server rechnet mit den gespeicherten Projektdaten → vorher aktuellen Stand speichern
      const synced = await projectStore.syncProjectToServer();
      if (!synced) throw new Error('Sync failed');
      r = await projectsApi.calculate(project.value.id);
    } else {
      r = await calculationApi.calculateAnonymous(project.value);
    }
    result.value = r;
    projectStore.setCalculationResult(r);
  } catch {
    uiStore.showToast('Berechnung fehlgeschlagen', 'error');
  } finally {
    isCalculating.value = false;
  }
}

emit('validation-change', true);

defineExpose({ calculate });
</script>

<style scoped>
/* ============================================
 * LAYOUT
 * ============================================ */
.summary-page {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-6);
  min-width: 0;
}

.tab-content {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-6);
  min-width: 0;
}

.block-title {
  margin: 0 0 var(--kalk-space-3);
}

.block-gap {
  margin-bottom: var(--kalk-space-5);
}

.two-col {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--kalk-space-6);
}

@media (max-width: 760px) {
  .two-col { grid-template-columns: 1fr; }
}

.panel { min-width: 0; }

.panel-body-rows {
  padding-top: var(--kalk-space-2);
  padding-bottom: var(--kalk-space-3);
}

.panel-subtitle :deep(.help-icon-btn) { vertical-align: middle; }

/* ============================================
 * TAB BAR (understated underline)
 * ============================================ */
.tab-bar {
  display: flex;
  gap: var(--kalk-space-6);
  border-bottom: 1px solid var(--kalk-border);
  position: sticky;
  top: 0;
  z-index: 10;
  background: var(--kalk-canvas);
  overflow-x: auto;
  -webkit-overflow-scrolling: touch;
  scrollbar-width: none;
}
.tab-bar::-webkit-scrollbar { display: none; }

.tab-btn {
  flex: 0 0 auto;
  padding: var(--kalk-space-3) 0;
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  color: var(--kalk-gray-500);
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  margin-bottom: -1px;
  cursor: pointer;
  white-space: nowrap;
  transition: color 0.12s, border-color 0.12s;
}
.tab-btn:hover { color: var(--kalk-gray-800); }
.tab-btn.active {
  color: var(--kalk-navy-900);
  font-weight: 600;
  border-bottom-color: var(--kalk-navy-900);
}

@media (max-width: 480px) {
  .tab-bar { gap: var(--kalk-space-5); }
}

/* ============================================
 * KPI STRIPS
 * ============================================ */
.kpi-grid-3 { grid-template-columns: repeat(3, minmax(0, 1fr)); }
.kpi-grid-2 { grid-template-columns: repeat(2, minmax(0, 1fr)); }

@media (max-width: 640px) {
  .kpi-grid-3 { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}

.kpi {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.kpi-label {
  display: block;
}

.kpi-label :deep(.help-icon-btn) {
  width: 14px;
  height: 14px;
  margin-left: 2px;
  vertical-align: -3px;
}

.kpi-value {
  margin-top: auto;
  padding-top: 6px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.kpi-unit {
  margin-left: 2px;
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  color: var(--kalk-gray-500);
}

.kpi-hero .kpi-value {
  font-family: var(--kalk-font-serif);
  font-size: var(--kalk-text-3xl);
  line-height: 1.1;
}

.kpi-warn {
  box-shadow: inset 3px 0 0 var(--kalk-caution);
}
.kpi-warn .kpi-value { color: var(--kalk-caution); }

.kpi-compact .kpi-value { font-size: var(--kalk-text-lg); }
.kpi-value-text { white-space: normal; }

.kpi.mc-below { box-shadow: inset 3px 0 0 var(--kalk-positive); }
.kpi.mc-below .kpi-value { color: var(--kalk-positive); }
.kpi.mc-above { box-shadow: inset 3px 0 0 var(--kalk-caution); }
.kpi.mc-above .kpi-value { color: var(--kalk-caution); }

@media (max-width: 640px) {
  .kpi { padding: var(--kalk-space-3) var(--kalk-space-4); }
  .kpi-value { font-size: var(--kalk-text-lg); }
  .kpi-hero .kpi-value { font-size: var(--kalk-text-2xl); }
}

/* Value colours */
.pos { color: var(--kalk-positive); }
.neg { color: var(--kalk-negative); }
.warn { color: var(--kalk-caution); }
.kpi-value.neg { color: var(--kalk-negative); }

/* ============================================
 * DATA ROWS (extensions)
 * ============================================ */
.data-row .data-label {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}
.data-value.pos { color: var(--kalk-positive); }
.data-value.neg { color: var(--kalk-negative); }
.data-row.subtotal {
  border-top: 1px solid var(--kalk-border-strong);
}
.data-row.subtotal .data-label { color: var(--kalk-gray-800); font-weight: 600; }
.data-row.strong-row .data-label { color: var(--kalk-gray-800); font-weight: 600; }
.data-row.muted-row .data-label { color: var(--kalk-gray-500); }
.data-row.total .data-value.pos { color: var(--kalk-positive); }
.data-row.total .data-value.neg { color: var(--kalk-negative); }

/* ============================================
 * EMPTY STATE
 * ============================================ */
.empty-result-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: var(--kalk-space-12) var(--kalk-space-6);
  min-height: 300px;
  background: var(--kalk-surface);
  border: 1px solid var(--kalk-border);
  border-radius: var(--kalk-radius-lg);
}
.empty-result-icon {
  width: 48px;
  height: 48px;
  color: var(--kalk-gray-400);
  margin-bottom: var(--kalk-space-5);
}
.empty-result-icon svg { width: 100%; height: 100%; }
.empty-result-title {
  font-size: var(--kalk-text-lg);
  font-weight: 600;
  color: var(--kalk-gray-900);
  margin: 0 0 var(--kalk-space-2);
}
.empty-result-text {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-600);
  max-width: 380px;
  line-height: 1.6;
  margin: 0;
}
.empty-result-text strong { color: var(--kalk-navy-800); font-weight: 600; }
.calculating-indicator {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  margin-top: var(--kalk-space-6);
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-700);
}
.spinner {
  width: 16px;
  height: 16px;
  border: 2px solid var(--kalk-gray-200);
  border-top-color: var(--kalk-navy-700);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

/* ============================================
 * RISK
 * ============================================ */
.risk-grid { display: flex; flex-direction: column; gap: var(--kalk-space-5); }
.risk-item { display: flex; flex-direction: column; gap: var(--kalk-space-2); }
.risk-head {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  gap: var(--kalk-space-3);
  flex-wrap: wrap;
}
.risk-label {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-700);
}
.risk-gauge { height: 4px; background: var(--kalk-gray-100); border-radius: var(--kalk-radius-sm); overflow: hidden; }
.risk-fill { height: 100%; }
.risk-fill.risk-low { background: var(--kalk-positive); }
.risk-fill.risk-medium { background: var(--kalk-caution); }
.risk-fill.risk-high,
.risk-fill.risk-critical { background: var(--kalk-negative); }
.risk-score { font-size: var(--kalk-text-sm); font-weight: 600; font-variant-numeric: tabular-nums; color: var(--kalk-gray-900); }
.risk-max { color: var(--kalk-gray-500); font-weight: 500; }
.risk-level { margin-left: var(--kalk-space-2); font-size: var(--kalk-text-xs); font-weight: 600; }
.risk-score.risk-low .risk-level { color: var(--kalk-positive); }
.risk-score.risk-medium .risk-level { color: var(--kalk-caution); }
.risk-score.risk-high .risk-level,
.risk-score.risk-critical .risk-level { color: var(--kalk-negative); }

/* ============================================
 * WARNINGS
 * ============================================ */
.warnings-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: var(--kalk-space-3) var(--kalk-space-5);
  background: var(--kalk-surface);
  border: none;
  cursor: pointer;
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-800);
  text-align: left;
}
.warnings-card-header:hover { background: var(--kalk-gray-50); }
.warnings-card-header.open { border-bottom: 1px solid var(--kalk-border); }
.warnings-card-header .warning-icon { color: var(--kalk-caution); }
.warnings-card-header.has-critical .warning-icon { color: var(--kalk-negative); }
.warnings-card-title { display: flex; align-items: center; gap: var(--kalk-space-2); }
.chevron-icon {
  width: 18px;
  height: 18px;
  flex-shrink: 0;
  color: var(--kalk-gray-400);
  transition: transform 0.15s;
}
.chevron-icon.open { transform: rotate(180deg); }
.warnings-card-body {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-2);
  padding: var(--kalk-space-4) var(--kalk-space-5);
  max-height: 480px;
  overflow-y: auto;
}
.warning-icon { width: 16px; height: 16px; flex-shrink: 0; }
.warning-item { align-items: flex-start; }
.warning-item .warning-icon { margin-top: 2px; }

/* ============================================
 * CHARTS
 * ============================================ */
.chart-container { min-width: 0; }
.bar-chart,
.financing-chart,
.pv-chart {
  display: flex;
  align-items: flex-end;
  gap: 2px;
  height: 180px;
  margin-bottom: 20px;
  padding: 0 var(--kalk-space-1);
  border-bottom: 1px solid var(--kalk-border-strong);
}
.financing-chart { height: 160px; gap: 3px; }
.pv-chart { position: relative; }

.bar-group { flex: 1; min-width: 0; display: flex; flex-direction: column; align-items: center; position: relative; height: 100%; }
.bar-stack { display: flex; flex-direction: column-reverse; width: 100%; max-width: 28px; height: 100%; justify-content: flex-start; }
.bar { width: 100%; min-height: 1px; }
.bar-noi { background: var(--kalk-navy-700); }
.bar-debt { background: var(--kalk-navy-200); }
.bar-tax { background: var(--kalk-gray-400); }
.bar-cf-marker {
  position: absolute;
  left: 50%;
  transform: translateX(-50%);
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--kalk-navy-950);
  border: 1.5px solid var(--kalk-surface);
  z-index: 2;
}
.bar-cf-marker.negative { background: var(--kalk-negative); }
.bar-label {
  position: absolute;
  bottom: -18px;
  font-size: var(--kalk-text-2xs);
  color: var(--kalk-gray-500);
  font-variant-numeric: tabular-nums;
}

.fin-bar-group,
.pv-bar-group { flex: 1; min-width: 0; display: flex; flex-direction: column; align-items: center; height: 100%; justify-content: flex-end; position: relative; }
.fin-bar {
  width: 100%;
  max-width: 32px;
  background: var(--kalk-navy-600);
  display: flex;
  align-items: flex-start;
  justify-content: center;
  min-height: 2px;
  overflow: hidden;
}
.fin-ltv-label { font-size: 9px; font-weight: 600; color: var(--kalk-surface); padding-top: 3px; font-variant-numeric: tabular-nums; }

@media (max-width: 480px) {
  .fin-ltv-label { display: none; }
}

.pv-bar { width: 100%; max-width: 28px; background: var(--kalk-navy-600); min-height: 2px; position: relative; }
.pv-bar:hover { background: var(--kalk-navy-800); }
.pv-condition-warn { position: absolute; top: -14px; left: 50%; transform: translateX(-50%); font-size: 10px; font-weight: 700; color: var(--kalk-caution); }
.pv-baseline { position: absolute; left: 0; right: 0; border-top: 1px dashed var(--kalk-gray-500); z-index: 1; pointer-events: none; }
.pv-baseline-label {
  position: absolute;
  right: 0;
  top: -16px;
  font-size: var(--kalk-text-2xs);
  font-weight: 600;
  color: var(--kalk-gray-600);
  background: var(--kalk-surface);
  padding: 0 var(--kalk-space-1);
  font-variant-numeric: tabular-nums;
}
.pv-baseline-legend { position: relative; padding-left: 16px; }
.pv-baseline-legend::before { content: ''; position: absolute; left: 0; top: 50%; width: 10px; border-top: 1px dashed var(--kalk-gray-500); }

.chart-legend {
  display: flex;
  flex-wrap: wrap;
  gap: var(--kalk-space-2) var(--kalk-space-5);
  margin-top: var(--kalk-space-4);
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-600);
}
.legend-item { display: flex; align-items: center; gap: 6px; }
.swatch { width: 10px; height: 10px; border-radius: 1px; display: inline-block; }
.swatch-noi { background: var(--kalk-navy-700); }
.swatch-debt { background: var(--kalk-navy-200); }
.swatch-tax { background: var(--kalk-gray-400); }
.swatch-cf { background: var(--kalk-navy-950); border-radius: 50%; width: 8px; height: 8px; }
.swatch-debt-bar { background: var(--kalk-navy-600); }
.swatch-pv { background: var(--kalk-navy-600); }

/* Tax bridge */
.tax-bridge { display: flex; flex-direction: column; gap: 6px; }
.bridge-year { display: flex; align-items: center; gap: var(--kalk-space-3); }
.bridge-year-label { width: 36px; font-size: var(--kalk-text-xs); font-weight: 600; color: var(--kalk-gray-600); text-align: right; flex-shrink: 0; font-variant-numeric: tabular-nums; }
.bridge-bars { flex: 1; min-width: 0; display: flex; height: 14px; overflow: hidden; background: var(--kalk-gray-50); }
.bridge-bar { height: 100%; min-width: 1px; }
.bridge-bar.income { background: var(--kalk-navy-700); }
.bridge-bar.afa { background: var(--kalk-navy-400); }
.bridge-bar.interest { background: var(--kalk-navy-200); }
.bridge-bar.operating { background: var(--kalk-gray-400); }
.bridge-bar.maintenance { background: var(--kalk-gray-300); }
.bridge-result { font-size: var(--kalk-text-xs); font-weight: 600; color: var(--kalk-gray-800); white-space: nowrap; min-width: 110px; text-align: right; font-variant-numeric: tabular-nums; }
.bridge-result.negative { color: var(--kalk-negative); }
.swatch-income { background: var(--kalk-navy-700); }
.swatch-afa { background: var(--kalk-navy-400); }
.swatch-interest-b { background: var(--kalk-navy-200); }
.swatch-operating { background: var(--kalk-gray-400); }
.swatch-maintenance { background: var(--kalk-gray-300); }

@media (max-width: 480px) {
  .bridge-year { gap: var(--kalk-space-2); }
  .bridge-result { min-width: 0; font-size: var(--kalk-text-2xs); }
}

/* ============================================
 * TABLES
 * ============================================ */
.table-scroll {
  overflow-x: auto;
  -webkit-overflow-scrolling: touch;
  max-width: 100%;
}
.table-bordered-top { border-top: 1px solid var(--kalk-border); }

.data-table th :deep(.help-icon-btn) { vertical-align: middle; width: 14px; height: 14px; }
.data-table td.neg { color: var(--kalk-negative); }
.data-table td.pos { color: var(--kalk-positive); }
.data-table td.warn { color: var(--kalk-caution); font-weight: 600; }

.cashflow-table { min-width: 1000px; font-size: var(--kalk-text-xs); }
.cashflow-table th,
.cashflow-table td { white-space: nowrap; padding: 8px 10px; }
.cashflow-table th:first-child,
.cashflow-table td:first-child { padding-left: var(--kalk-space-5); }
.cashflow-table th:last-child,
.cashflow-table td:last-child { padding-right: var(--kalk-space-5); }
.cashflow-table .sticky-col { text-align: left; position: sticky; left: 0; background: var(--kalk-surface); z-index: 2; border-right: 1px solid var(--kalk-border); }
.cashflow-table th.sticky-col { background: var(--kalk-gray-50); z-index: 3; }
.cashflow-table .year-col { font-weight: 600; color: var(--kalk-gray-900); }
.cashflow-table .highlight-col { background: var(--kalk-navy-50); font-weight: 600; }
.cashflow-table th.highlight-col { background: var(--kalk-navy-100); color: var(--kalk-navy-900); }
.cashflow-table tbody tr:hover td.highlight-col { background: var(--kalk-navy-100); }
.cashflow-table tfoot td {
  background: var(--kalk-gray-50);
  font-weight: 700;
  color: var(--kalk-gray-900);
  border-top: 1px solid var(--kalk-gray-900);
  border-bottom: none;
}
.cashflow-table tfoot td.neg { color: var(--kalk-negative); }
.cashflow-table tfoot td.pos { color: var(--kalk-positive); }
.cashflow-table tfoot .sticky-col { background: var(--kalk-gray-50); }
.cashflow-table tfoot td.highlight-col { background: var(--kalk-navy-100); }

.scenario-table th,
.scenario-table td { white-space: nowrap; }
.scenario-table th:first-child,
.scenario-table td:first-child { padding-left: var(--kalk-space-5); }
.scenario-table th:last-child,
.scenario-table td:last-child { padding-right: var(--kalk-space-5); }
.scenario-table .scenario-label-col { min-width: 160px; }
.scenario-table td:first-child { text-align: left; color: var(--kalk-gray-700); }
.scenario-table th.scenario-col { min-width: 120px; vertical-align: bottom; }
.scenario-rate { display: block; font-weight: 400; font-size: var(--kalk-text-2xs); color: var(--kalk-gray-500); text-transform: none; letter-spacing: 0; }
.scenario-table .scenario-base { background: var(--kalk-navy-50); }
.scenario-table th.scenario-base { color: var(--kalk-navy-900); background: var(--kalk-navy-100); }
.scenario-subtotal td { border-top: 1px solid var(--kalk-border-strong); font-weight: 600; }
.scenario-total td { border-top: 1px solid var(--kalk-gray-900); font-weight: 700; color: var(--kalk-gray-900); }
.scenario-total td.neg { color: var(--kalk-negative); }
.scenario-total td.pos { color: var(--kalk-positive); }
.scenario-highlight td { font-weight: 600; }
.scenario-table td :deep(.help-icon-btn) { vertical-align: middle; }

/* ============================================
 * PROPERTY: EXPLANATION
 * ============================================ */
.forecast-explanation { border-top: 1px solid var(--kalk-border); }
.explanation-list { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: var(--kalk-space-2); }
.explanation-list li {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-700);
  padding: 2px 0 2px var(--kalk-space-3);
  border-left: 2px solid var(--kalk-gray-200);
  line-height: 1.55;
}
.explanation-list li.summary { border-left-color: var(--kalk-navy-800); font-weight: 600; color: var(--kalk-gray-900); }
.explanation-list li.overdueComponents { border-left-color: var(--kalk-caution); }
.explanation-list li.investments { border-left-color: var(--kalk-navy-400); }
.explanation-list li.meanReversion { border-left-color: var(--kalk-navy-400); }
.explanation-list li.componentDeterioration { border-left-color: var(--kalk-negative); }

/* ============================================
 * COMPONENT DETERIORATION
 * ============================================ */
.cdet-summary-strip {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--kalk-space-3);
  flex-wrap: wrap;
  margin-bottom: var(--kalk-space-4);
  padding-bottom: var(--kalk-space-4);
  border-bottom: 1px solid var(--kalk-border);
}
.cdet-counters { display: flex; flex-wrap: wrap; gap: var(--kalk-space-2); }
.cdet-counter-num { font-variant-numeric: tabular-nums; font-weight: 700; }
.cdet-impact-total { display: flex; flex-direction: column; align-items: flex-end; gap: 2px; }
.cdet-impact-total strong { font-size: var(--kalk-text-lg); font-weight: 600; font-variant-numeric: tabular-nums; color: var(--kalk-navy-900); }
.cdet-impact-total strong.neg { color: var(--kalk-negative); }
.cdet-impact-total strong.pos { color: var(--kalk-positive); }

.cdet-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: var(--kalk-space-3); align-items: start; }
@media (max-width: 600px) { .cdet-grid { grid-template-columns: 1fr; } }
@media (min-width: 900px) { .cdet-grid { grid-template-columns: repeat(3, minmax(0, 1fr)); } }

.cdet-card {
  position: relative;
  display: flex;
  flex-direction: column;
  padding: var(--kalk-space-3) var(--kalk-space-4);
  background: var(--kalk-surface);
  border: 1px solid var(--kalk-border);
  border-radius: var(--kalk-radius-md);
  cursor: pointer;
  transition: border-color 0.12s;
  min-width: 0;
}
.cdet-card:hover { border-color: var(--kalk-border-strong); }
.cdet-card:focus-visible { outline: 2px solid var(--kalk-navy-500); outline-offset: 2px; }

.cdet-card-header { display: flex; justify-content: space-between; align-items: center; gap: var(--kalk-space-2); margin-bottom: var(--kalk-space-3); }
.cdet-card-name { font-size: var(--kalk-text-sm); font-weight: 600; color: var(--kalk-gray-900); min-width: 0; }

.cdet-card-metrics { display: flex; gap: var(--kalk-space-4); flex-wrap: wrap; }
.cdet-metric { display: flex; flex-direction: column; min-width: 0; gap: 2px; }
.cdet-metric-label { font-size: 10px; font-weight: 600; color: var(--kalk-gray-500); text-transform: uppercase; letter-spacing: var(--kalk-tracking-caps); }
.cdet-metric-value { font-size: var(--kalk-text-sm); font-weight: 600; color: var(--kalk-gray-900); font-variant-numeric: tabular-nums; }
.cdet-metric-value.pos { color: var(--kalk-positive); }
.cdet-metric-value.neg { color: var(--kalk-negative); }

.cdet-lifecycle-bar { height: 3px; background: var(--kalk-gray-100); margin-top: var(--kalk-space-3); overflow: hidden; }
.cdet-lifecycle-fill { height: 100%; }
.cdet-fill-OK { background: var(--kalk-navy-600); }
.cdet-fill-Renewed { background: var(--kalk-navy-400); }
.cdet-fill-Overdue { background: var(--kalk-negative); }
.cdet-fill-OverdueAtPurchase { background: var(--kalk-gray-400); }

.cdet-expand-hint { display: flex; justify-content: center; margin-top: var(--kalk-space-1); color: var(--kalk-gray-400); }
.cdet-expand-hint svg { transition: transform 0.15s; }
.cdet-expand-hint svg.rotated { transform: rotate(180deg); }

.cdet-detail {
  margin-top: var(--kalk-space-3);
  padding-top: var(--kalk-space-3);
  border-top: 1px solid var(--kalk-gray-100);
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-2);
}
@media (max-width: 899px) {
  .cdet-detail { display: none; }
  .cdet-card.expanded .cdet-detail { display: flex; }
}
@media (min-width: 900px) {
  .cdet-card { cursor: default; }
  .cdet-card:hover { border-color: var(--kalk-border); }
  .cdet-expand-hint { display: none; }
}
.cdet-detail-row {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  gap: var(--kalk-space-3);
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-600);
}
.cdet-detail-row strong,
.cdet-detail-row .num { color: var(--kalk-gray-900); font-weight: 600; font-variant-numeric: tabular-nums; white-space: nowrap; }
.cdet-detail-row strong.pos { color: var(--kalk-positive); }
.cdet-priced-in-hint {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-600);
  line-height: 1.5;
  padding: var(--kalk-space-2) var(--kalk-space-3);
  background: var(--kalk-gray-50);
  border-left: 2px solid var(--kalk-gray-300);
}
.cdet-recurring {
  padding: var(--kalk-space-2) var(--kalk-space-3);
  background: var(--kalk-gray-50);
  border-left: 2px solid var(--kalk-navy-400);
}
.cdet-recurring-badge { margin-bottom: var(--kalk-space-2); }
.cdet-recurring-details { display: flex; flex-direction: column; gap: var(--kalk-space-1); }

.cdet-totals { margin-top: var(--kalk-space-4); padding-top: var(--kalk-space-2); border-top: 1px solid var(--kalk-border); }

@media (max-width: 400px) {
  .cdet-impact-total { align-items: flex-start; width: 100%; }
}

/* ============================================
 * CAPEX TIMELINE
 * ============================================ */
.captl-axis { position: relative; margin-bottom: var(--kalk-space-4); padding: var(--kalk-space-4) var(--kalk-space-3) var(--kalk-space-8); }
.captl-track { position: relative; height: 1px; background: var(--kalk-border-strong); }
.captl-year-tick { position: absolute; top: -4px; transform: translateX(-50%); width: 1px; height: 9px; background: var(--kalk-gray-300); }
.captl-year-label {
  position: absolute;
  top: 14px;
  left: 50%;
  transform: translateX(-50%);
  font-size: var(--kalk-text-2xs);
  color: var(--kalk-gray-500);
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}
.captl-dot {
  position: absolute;
  top: 50%;
  transform: translate(-50%, -50%);
  border-radius: 50%;
  min-width: 8px;
  min-height: 8px;
  border: 1.5px solid var(--kalk-surface);
  z-index: 1;
}
.captl-dot.MaintenanceExpense, .captl-dot-inline.MaintenanceExpense { background: var(--kalk-navy-400); }
.captl-dot.AcquisitionCost, .captl-dot-inline.AcquisitionCost { background: var(--kalk-navy-700); }
.captl-dot.ImprovementCost, .captl-dot-inline.ImprovementCost { background: var(--kalk-navy-950); }
.captl-dot.NotDeductible, .captl-dot-inline.NotDeductible { background: var(--kalk-gray-400); }

.captl-groups { display: flex; flex-direction: column; border-top: 1px solid var(--kalk-border); }
.captl-group {
  display: flex;
  align-items: baseline;
  gap: var(--kalk-space-3);
  padding: var(--kalk-space-2) 0;
  border-bottom: 1px solid var(--kalk-gray-100);
}
.captl-group-year { width: 40px; flex-shrink: 0; font-size: var(--kalk-text-xs); font-weight: 600; color: var(--kalk-gray-700); font-variant-numeric: tabular-nums; }
.captl-group-items { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: var(--kalk-space-1); }
.captl-group-item { display: flex; align-items: center; gap: var(--kalk-space-2); font-size: var(--kalk-text-sm); color: var(--kalk-gray-800); }
.captl-dot-inline { width: 6px; height: 6px; border-radius: 50%; flex-shrink: 0; }
.captl-item-name { flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.captl-item-amount { font-weight: 600; font-variant-numeric: tabular-nums; white-space: nowrap; min-width: 90px; text-align: right; }
.captl-item-tax { font-size: var(--kalk-text-xs); color: var(--kalk-gray-500); white-space: nowrap; flex-shrink: 0; }
.captl-group-total {
  flex-shrink: 0;
  font-size: var(--kalk-text-xs);
  font-weight: 600;
  color: var(--kalk-gray-600);
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
  padding-left: var(--kalk-space-3);
  border-left: 1px solid var(--kalk-border);
}
@media (max-width: 600px) {
  .captl-axis { display: none; }
  .captl-item-tax { display: none; }
  .captl-group { flex-wrap: wrap; }
  .captl-item-amount { min-width: 0; }
}

/* ============================================
 * EXIT
 * ============================================ */
.exit-pnl { margin-bottom: var(--kalk-space-6); }

/* ============================================
 * ACTIONS
 * ============================================ */
.actions { display: flex; justify-content: flex-end; gap: var(--kalk-space-3); }
</style>
