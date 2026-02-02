<template>
  <div class="summary-page">
    <!-- Empty state: before calculation -->
    <div v-if="!result" class="empty-result-state">
      <div class="empty-result-icon">
        <svg viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
          <rect x="8" y="4" width="32" height="40" rx="4" stroke="currentColor" stroke-width="2.5"/>
          <path d="M16 12h16M16 12h16" stroke="currentColor" stroke-width="2.5" stroke-linecap="round"/>
          <rect x="14" y="20" width="6" height="6" rx="1" stroke="currentColor" stroke-width="1.5"/>
          <rect x="14" y="30" width="6" height="6" rx="1" stroke="currentColor" stroke-width="1.5"/>
          <rect x="24" y="20" width="6" height="6" rx="1" stroke="currentColor" stroke-width="1.5"/>
          <rect x="24" y="30" width="6" height="6" rx="1" stroke="currentColor" stroke-width="1.5"/>
          <rect x="34" y="20" width="0" height="16" rx="0" stroke="currentColor" stroke-width="1.5"/>
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
    <nav class="tab-bar">
      <button
        v-for="tab in tabs"
        :key="tab.id"
        class="tab-btn"
        :class="{ active: activeTab === tab.id }"
        @click="activeTab = tab.id"
      >
        {{ t(tab.label) }}
      </button>
    </nav>

    <!-- ===== TAB: OVERVIEW ===== -->
    <div v-show="activeTab === 'overview'" class="tab-content">

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

      <!-- Return Metrics -->
      <KalkCard :title="t('summary.returnMetrics')">
        <div class="metrics-grid">
          <div class="metric-item accent">
            <span class="metric-label">{{ t('summary.metrics.irrAfterTax') }} <HelpIcon help-key="summary.irrAfterTax" /></span>
            <span class="metric-value">{{ fmtPct(result.metrics.irrAfterTaxPercent) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.irrBeforeTax') }} <HelpIcon help-key="summary.irrBeforeTax" /></span>
            <span class="metric-value">{{ fmtPct(result.metrics.irrBeforeTaxPercent) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.npvAfterTax') }} <HelpIcon help-key="summary.npvAfterTax" /></span>
            <span class="metric-value" :class="npvClass(result.metrics.npvAfterTax.amount)">
              {{ fmtCur(result.metrics.npvAfterTax.amount) }}
            </span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.cashOnCash') }} <HelpIcon help-key="summary.cashOnCash" /></span>
            <span class="metric-value">{{ fmtPct(result.metrics.cashOnCashPercent) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.equityMultiple') }} <HelpIcon help-key="summary.equityMultiple" /></span>
            <span class="metric-value">{{ result.metrics.equityMultiple.toFixed(2) }}x</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.roi') }} <HelpIcon help-key="summary.roi" /></span>
            <span class="metric-value">{{ fmtPct(result.metrics.roiPercent) }}</span>
          </div>
        </div>
      </KalkCard>

      <!-- Bank Metrics -->
      <KalkCard :title="t('summary.bankMetrics')">
        <div class="metrics-grid compact">
          <div class="metric-item" :class="{ warn: result.metrics.dscrMin < 1.2 }">
            <span class="metric-label">{{ t('summary.metrics.dscrMin') }} <HelpIcon help-key="summary.dscrMin" /></span>
            <span class="metric-value">{{ result.metrics.dscrMin.toFixed(2) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.dscrAvg') }} <HelpIcon help-key="summary.dscrAvg" /></span>
            <span class="metric-value">{{ result.metrics.dscrAvg.toFixed(2) }}</span>
          </div>
          <div class="metric-item" :class="{ warn: result.metrics.icrMin < 1.5 }">
            <span class="metric-label">{{ t('summary.metrics.icrMin') }} <HelpIcon help-key="summary.icrMin" /></span>
            <span class="metric-value">{{ result.metrics.icrMin.toFixed(2) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.ltvInitial') }} <HelpIcon help-key="summary.ltvInitial" /></span>
            <span class="metric-value">{{ fmtPct(result.metrics.ltvInitialPercent) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.ltvFinal') }} <HelpIcon help-key="summary.ltvFinal" /></span>
            <span class="metric-value">{{ fmtPct(result.metrics.ltvFinalPercent) }}</span>
          </div>
          <div class="metric-item">
            <span class="metric-label">{{ t('summary.metrics.breakEvenRent') }} <HelpIcon help-key="summary.breakEvenRent" /></span>
            <span class="metric-value">{{ fmtCur(result.metrics.breakEvenRent.amount) }}/M</span>
          </div>
        </div>
      </KalkCard>

      <!-- Risk Indicators -->
      <KalkCard :title="t('summary.riskIndicators')">
        <div class="risk-grid">
          <div class="risk-item">
            <span class="risk-label">{{ t('summary.risk.maintenance') }} <HelpIcon help-key="summary.riskMaintenance" /></span>
            <div class="risk-gauge">
              <div class="risk-fill" :class="riskClass(result.metrics.maintenanceRiskScore)" :style="{ width: result.metrics.maintenanceRiskScore + '%' }"></div>
            </div>
            <span class="risk-score" :class="riskClass(result.metrics.maintenanceRiskScore)">
              {{ result.metrics.maintenanceRiskScore }}/100
              <span class="risk-level">{{ riskLabel(result.metrics.maintenanceRiskScore) }}</span>
            </span>
          </div>
          <div class="risk-item">
            <span class="risk-label">{{ t('summary.risk.liquidity') }} <HelpIcon help-key="summary.riskLiquidity" /></span>
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

      <!-- Totals -->
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

      <!-- Collapsible Warnings -->
      <div v-if="warningCount > 0" class="warnings-card">
        <button class="warnings-card-header" :class="{ open: warningsOpen, 'has-critical': hasCriticalWarning }" @click="warningsOpen = !warningsOpen">
          <span class="warnings-card-title">
            <svg class="warning-icon" viewBox="0 0 20 20" fill="currentColor">
              <path v-if="hasCriticalWarning" fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
              <path v-else fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
            </svg>
            {{ t('summary.warnings') }}
            <span class="warnings-badge" :class="{ critical: hasCriticalWarning }">{{ warningCount }}</span>
          </span>
          <svg class="chevron-icon" :class="{ open: warningsOpen }" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd" />
          </svg>
        </button>
        <div v-show="warningsOpen" class="warnings-card-body">
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
      </div>

    </div><!-- /overview -->

    <!-- ===== TAB: CASHFLOW ===== -->
    <div v-show="activeTab === 'cashflow'" class="tab-content">

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

      <!-- Financing Chart -->
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

      <!-- Cashflow Table -->
      <KalkCard :title="t('summary.cashflowTable')">
        <div class="table-scroll">
          <table class="cashflow-table">
            <thead>
              <tr>
                <th class="sticky-col">{{ t('summary.cashflow.year') }}</th>
                <th>{{ t('summary.cashflow.effectiveRent') }} <HelpIcon help-key="summary.cf.effectiveRent" /></th>
                <th>{{ t('summary.cashflow.operatingCosts') }}</th>
                <th v-if="hasMaintenanceReserve">{{ t('summary.cashflow.maintenanceReserve') }}</th>
                <th>{{ t('summary.cashflow.noi') }} <HelpIcon help-key="summary.cf.noi" /></th>
                <th>{{ t('summary.cashflow.interest') }}</th>
                <th>{{ t('summary.cashflow.principal') }}</th>
                <th>{{ t('summary.cashflow.capex') }}</th>
                <th v-if="hasMaintenanceReserve">{{ t('summary.cashflow.capexNet') }}</th>
                <th v-if="hasMaintenanceReserve">{{ t('summary.cashflow.reserveBalance') }}</th>
                <th>{{ t('summary.cashflow.beforeTax') }} <HelpIcon help-key="summary.cf.beforeTax" /></th>
                <th>{{ t('summary.cashflow.tax') }}</th>
                <th class="highlight-col">{{ t('summary.cashflow.afterTax') }} <HelpIcon help-key="summary.cf.afterTax" /></th>
                <th>{{ t('summary.cashflow.cumulative') }} <HelpIcon help-key="summary.cf.cumulative" /></th>
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
                <td v-if="hasMaintenanceReserve" class="neg">{{ fmtCompact(-row.maintenanceReserve) }}</td>
                <td :class="valClass(row.netOperatingIncome)">{{ fmtCompact(row.netOperatingIncome) }}</td>
                <td class="neg">{{ fmtCompact(-row.interestPortion) }}</td>
                <td class="neg">{{ fmtCompact(-row.principalPortion) }}</td>
                <td class="neg">{{ row.capexPayments > 0 ? fmtCompact(-row.capexPayments) : '–' }}</td>
                <td v-if="hasMaintenanceReserve" class="neg">{{ row.capexFromCashflow > 0 ? fmtCompact(-row.capexFromCashflow) : '–' }}</td>
                <td v-if="hasMaintenanceReserve">{{ fmtCompact(row.reserveBalanceEnd) }}</td>
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
                <td v-if="hasMaintenanceReserve" class="neg">{{ fmtCompact(-sumCol('maintenanceReserve')) }}</td>
                <td>{{ fmtCompact(sumCol('netOperatingIncome')) }}</td>
                <td class="neg">{{ fmtCompact(-sumCol('interestPortion')) }}</td>
                <td class="neg">{{ fmtCompact(-sumCol('principalPortion')) }}</td>
                <td class="neg">{{ fmtCompact(-sumCol('capexPayments')) }}</td>
                <td v-if="hasMaintenanceReserve" class="neg">{{ fmtCompact(-sumCol('capexFromCashflow')) }}</td>
                <td v-if="hasMaintenanceReserve"></td>
                <td>{{ fmtCompact(result.totalCashflowBeforeTax) }}</td>
                <td class="neg">{{ fmtCompact(-sumCol('taxPayment')) }}</td>
                <td class="highlight-col"><strong>{{ fmtCompact(result.totalCashflowAfterTax) }}</strong></td>
                <td></td><td></td><td></td><td></td>
              </tr>
            </tfoot>
          </table>
        </div>
      </KalkCard>

    </div><!-- /cashflow -->

    <!-- ===== TAB: TAX ===== -->
    <div v-show="activeTab === 'tax'" class="tab-content">

      <!-- Tax Bridge -->
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

      <!-- Tax Summary -->
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
          <div v-if="hasMaintenanceReserve" class="tax-row tax-row-info">
            <span>{{ t('summary.tax.totalMaintenanceReserve') }}</span>
            <strong class="hint-text">{{ fmtCur(result.taxSummary.totalMaintenanceReserve.amount) }}</strong>
          </div>
          <div class="tax-row highlight">
            <span>{{ t('summary.tax.totalSavings') }} <HelpIcon help-key="summary.tax.totalSavings" /></span>
            <strong class="pos">{{ fmtCur(result.taxSummary.totalTaxSavings.amount) }}</strong>
          </div>
          <div class="tax-row highlight">
            <span>{{ t('summary.tax.totalTax') }}</span>
            <strong>{{ fmtCur(result.taxSummary.totalTaxPayment.amount) }}</strong>
          </div>
          <div class="tax-row">
            <span>{{ t('summary.tax.effectiveRate') }} <HelpIcon help-key="summary.tax.effectiveRate" /></span>
            <strong>{{ fmtPct(result.taxSummary.effectiveTaxRatePercent) }}</strong>
          </div>
          <div class="tax-row" :class="{ 'rule-triggered': result.taxSummary.acquisitionRelatedCostsTriggered }">
            <span>{{ t('summary.tax.rule15') }} <HelpIcon help-key="summary.tax.rule15" /></span>
            <strong>
              {{ result.taxSummary.acquisitionRelatedCostsTriggered
                ? t('summary.tax.rule15Triggered')
                : t('summary.tax.rule15NotTriggered')
              }}
            </strong>
          </div>
        </div>
      </KalkCard>

    </div><!-- /tax -->

    <!-- ===== TAB: PROPERTY ===== -->
    <div v-show="activeTab === 'property'" class="tab-content">

      <!-- Property Value Forecast -->
      <KalkCard :title="t('summary.propertyValue.title')">
        <p class="exit-subtitle">{{ t('summary.propertyValue.subtitle') }}</p>

        <!-- Market Comparison Banner -->
        <div v-if="result.propertyValueForecast.marketComparison" class="market-comparison">
          <div class="mc-item">
            <span>{{ t('summary.propertyValue.regionalPrice') }}</span>
            <strong>{{ fmtCur(result.propertyValueForecast.marketComparison.regionalPricePerSqm) }}/m²</strong>
          </div>
          <div class="mc-item">
            <span>{{ t('summary.propertyValue.fairMarketValue') }}</span>
            <strong>{{ fmtCur(result.propertyValueForecast.marketComparison.fairMarketValue) }}</strong>
          </div>
          <div class="mc-item" :class="marketAssessmentClass">
            <span>{{ t('summary.propertyValue.purchaseVsMarket') }}</span>
            <strong>
              {{ fmtPct((result.propertyValueForecast.marketComparison.purchasePriceToMarketRatio - 1) * 100) }}
              ({{ t(`summary.propertyValue.assessment.${result.propertyValueForecast.marketComparison.assessment}`) }})
            </strong>
          </div>
        </div>

        <div class="exit-meta" style="margin-bottom: var(--kalk-space-4);">
          <div class="exit-meta-item">
            <span>{{ t('summary.propertyValue.initialCondition') }} <HelpIcon help-key="summary.property.conditionFactor" /></span>
            <strong>{{ (result.propertyValueForecast.initialConditionFactor * 100).toFixed(0) }}%</strong>
          </div>
          <div class="exit-meta-item">
            <span>{{ t('summary.propertyValue.improvementFactor') }} <HelpIcon help-key="summary.property.forecast" /></span>
            <strong>{{ (result.propertyValueForecast.improvementValueFactor * 100).toFixed(0) }}%</strong>
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

        <!-- Scenario breakdown table -->
        <div class="scenario-table-scroll" style="margin-top: var(--kalk-space-4);">
          <table class="scenario-table">
            <thead>
              <tr>
                <th class="scenario-label-col"></th>
                <th v-for="s in result.propertyValueForecast.scenarios" :key="s.label" class="scenario-col" :class="{ 'scenario-base': s.label === 'base' }">
                  {{ t(`summary.propertyValue.scenario.${s.label}`) }}
                  <span class="scenario-rate">{{ fmtPct(s.annualAppreciationPercent) }} p.a.</span>
                </th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>{{ t('summary.propertyValue.purchasePrice') }}</td>
                <td v-for="s in result.propertyValueForecast.scenarios" :key="s.label" :class="{ 'scenario-base': s.label === 'base' }">{{ fmtCur(result.propertyValueForecast.purchasePrice) }}</td>
              </tr>
              <tr>
                <td>{{ t('summary.propertyValue.marketAppreciationRow') }}</td>
                <td v-for="(s, idx) in result.propertyValueForecast.scenarios" :key="s.label"
                  :class="[valClass(scenarioBreakdowns[idx].marketAppreciation), { 'scenario-base': s.label === 'base' }]">
                  {{ scenarioBreakdowns[idx].marketAppreciation >= 0 ? '+' : '' }}{{ fmtCur(scenarioBreakdowns[idx].marketAppreciation) }}
                </td>
              </tr>
              <tr>
                <td>{{ t('summary.propertyValue.conditionAdjustment') }}</td>
                <td v-for="(s, idx) in result.propertyValueForecast.scenarios" :key="s.label"
                  :class="[valClass(scenarioBreakdowns[idx].conditionAdjustment), { 'scenario-base': s.label === 'base' }]">
                  {{ fmtCur(scenarioBreakdowns[idx].conditionAdjustment) }}
                  <span v-if="!hasComponentDeterioration" class="scenario-rate">({{ (scenarioBreakdowns[idx].conditionFactor * 100).toFixed(0) }}%)</span>
                </td>
              </tr>
              <tr v-if="hasInvestments">
                <td>{{ t('summary.propertyValue.investmentRow') }}</td>
                <td v-for="(s, idx) in result.propertyValueForecast.scenarios" :key="s.label"
                  :class="[valClass(scenarioBreakdowns[idx].investments), { 'scenario-base': s.label === 'base' }]">
                  +{{ fmtCur(scenarioBreakdowns[idx].investments) }}
                </td>
              </tr>
              <tr v-if="hasMeanReversion">
                <td>{{ t('summary.propertyValue.meanReversionRow') }}</td>
                <td v-for="(s, idx) in result.propertyValueForecast.scenarios" :key="s.label"
                  :class="[valClass(scenarioBreakdowns[idx].meanReversion), { 'scenario-base': s.label === 'base' }]">
                  {{ scenarioBreakdowns[idx].meanReversion >= 0 ? '+' : '' }}{{ fmtCur(scenarioBreakdowns[idx].meanReversion) }}
                </td>
              </tr>
              <tr class="scenario-highlight">
                <td>{{ t('summary.propertyValue.finalValue') }}</td>
                <td v-for="s in result.propertyValueForecast.scenarios" :key="s.label" :class="{ 'scenario-base': s.label === 'base' }">
                  <strong>{{ fmtCur(s.finalValue) }}</strong>
                </td>
              </tr>
              <tr class="scenario-highlight">
                <td>{{ t('summary.propertyValue.vs') }}</td>
                <td v-for="s in result.propertyValueForecast.scenarios" :key="s.label"
                  :class="[valClass(s.finalValue - result.propertyValueForecast.purchasePrice), { 'scenario-base': s.label === 'base' }]">
                  <strong>{{ s.finalValue >= result.propertyValueForecast.purchasePrice ? '+' : '' }}{{ fmtCur(s.finalValue - result.propertyValueForecast.purchasePrice) }}</strong>
                  <span class="scenario-rate">{{ s.finalValue >= result.propertyValueForecast.purchasePrice ? '+' : '' }}{{ fmtPct(((s.finalValue / result.propertyValueForecast.purchasePrice) - 1) * 100) }}</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Forecast Explanation -->
        <div v-if="forecastDriverTexts.length > 0" class="forecast-explanation">
          <h4 class="exit-section-title">{{ t('summary.propertyValue.explanationTitle') }}</h4>
          <ul class="explanation-list">
            <li v-for="(driver, i) in forecastDriverTexts" :key="i" :class="driver.type">
              {{ driver.text }}
            </li>
          </ul>
        </div>
      </KalkCard>

      <!-- Component Deterioration (Card Layout) -->
      <KalkCard v-if="hasComponentDeterioration" :title="t('summary.propertyValue.componentDeterioration.title')">
        <p class="exit-subtitle">{{ t('summary.propertyValue.componentDeterioration.subtitle') }} <HelpIcon help-key="summary.property.componentDeterioration" /></p>

        <!-- Status Summary Strip -->
        <div class="cdet-summary-strip">
          <div v-if="detStatusCounts.ok > 0" class="cdet-counter status-OK">
            <span class="cdet-counter-num">{{ detStatusCounts.ok }}</span>
            <span class="cdet-counter-label">{{ t('summary.propertyValue.componentDeterioration.statusLabels.OK') }}</span>
          </div>
          <div v-if="detStatusCounts.renewed > 0" class="cdet-counter status-Renewed">
            <span class="cdet-counter-num">{{ detStatusCounts.renewed }}</span>
            <span class="cdet-counter-label">{{ t('summary.propertyValue.componentDeterioration.statusLabels.Renewed') }}</span>
          </div>
          <div v-if="detStatusCounts.overdue > 0" class="cdet-counter status-Overdue">
            <span class="cdet-counter-num">{{ detStatusCounts.overdue }}</span>
            <span class="cdet-counter-label">{{ t('summary.propertyValue.componentDeterioration.statusLabels.Overdue') }}</span>
          </div>
          <div v-if="detStatusCounts.overdueAtPurchase > 0" class="cdet-counter status-OverdueAtPurchase">
            <span class="cdet-counter-num">{{ detStatusCounts.overdueAtPurchase }}</span>
            <span class="cdet-counter-label">{{ t('summary.propertyValue.componentDeterioration.statusLabels.OverdueAtPurchase') }}</span>
          </div>
          <div class="cdet-impact-total" :class="valClass(componentDeteriorationSummary?.totalValueImpact ?? 0)">
            <span class="cdet-impact-label">{{ t('summary.propertyValue.componentDeterioration.valueImpact') }}</span>
            <strong>{{ fmtCur(componentDeteriorationSummary?.totalValueImpact ?? 0) }}</strong>
          </div>
        </div>

        <!-- Component Cards Grid -->
        <div class="cdet-grid">
          <div v-for="row in componentDeteriorationRows" :key="row.category"
               class="cdet-card" :class="['cdet-' + row.statusAtEnd, { expanded: expandedDetRows.has(row.category) }]"
               role="button" tabindex="0"
               @click="toggleDetRow(row.category)"
               @keydown.enter="toggleDetRow(row.category)">
            <!-- Header: Name + Status Badge -->
            <div class="cdet-card-header">
              <span class="cdet-card-name">{{ t(`property.components.categories.${row.category}`) }}</span>
              <span class="comp-det-status" :class="'status-' + row.statusAtEnd">
                {{ t(`summary.propertyValue.componentDeterioration.statusLabels.${row.statusAtEnd}`) }}
              </span>
            </div>
            <!-- Key Metrics -->
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
                <span class="cdet-metric-value" :class="valClass(row.valueImpact)">
                  <template v-if="row.statusAtEnd === 'OverdueAtPurchase'">0 € ({{ t('summary.propertyValue.componentDeterioration.pricedIn') }})</template>
                  <template v-else>{{ fmtCur(row.valueImpact) }}</template>
                </span>
              </div>
            </div>
            <!-- Lifecycle Progress Bar -->
            <div class="cdet-lifecycle-bar">
              <div class="cdet-lifecycle-fill" :class="'cdet-fill-' + row.statusAtEnd"
                   :style="{ width: Math.min((row.ageAtEnd / row.cycleYears) * 100, 100) + '%' }"></div>
            </div>
            <!-- Detail (always visible on desktop, toggle on mobile) -->
            <div class="cdet-detail">
              <div v-if="row.statusAtEnd === 'OverdueAtPurchase'" class="cdet-priced-in-hint">
                {{ t('summary.propertyValue.componentDeterioration.pricedInHint') }}
              </div>
              <div class="cdet-detail-row">
                <span>{{ t('summary.propertyValue.componentDeterioration.renewalCost') }}</span>
                <strong>{{ fmtCur(row.renewalCostEstimate) }}</strong>
              </div>
              <div v-if="row.recurringMaintenance" class="cdet-recurring">
                <span class="cdet-recurring-badge">{{ t('summary.propertyValue.componentDeterioration.recurringLabel') }}</span>
                <div class="cdet-recurring-details">
                  <div class="cdet-detail-row">
                    <span>{{ row.recurringMaintenance.name }} ({{ row.recurringMaintenance.intervalYears }} {{ t('summary.propertyValue.componentDeterioration.yearsShort') }})</span>
                    <span>{{ row.recurringMaintenance.occurrencesInPeriod }}&times; {{ fmtCur(row.recurringMaintenance.costPerOccurrence) }}</span>
                  </div>
                  <div class="cdet-detail-row">
                    <span>{{ t('summary.propertyValue.componentDeterioration.cycleYears') }} (eff.)</span>
                    <strong>{{ row.recurringMaintenance.effectiveCycleYears }} {{ t('summary.propertyValue.componentDeterioration.yearsShort') }} (+{{ getRecurringExtensionPercent(row) }}%)</strong>
                  </div>
                  <div class="cdet-detail-row">
                    <span>{{ t('summary.propertyValue.componentDeterioration.valueImpact') }}</span>
                    <strong class="positive">+{{ fmtCur(row.recurringMaintenance.valueImprovement) }}</strong>
                  </div>
                </div>
              </div>
            </div>
            <!-- Expand chevron -->
            <div class="cdet-expand-hint">
              <svg :class="{ rotated: expandedDetRows.has(row.category) }" width="12" height="12" viewBox="0 0 12 12" fill="none">
                <path d="M3 4.5L6 7.5L9 4.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            </div>
          </div>
        </div>

        <!-- Bottom Totals -->
        <div v-if="componentDeteriorationSummary" class="cdet-totals">
          <div class="cdet-total-row">
            <span>{{ t('summary.propertyValue.componentDeterioration.renewalCost') }}</span>
            <strong>{{ fmtCur(componentDeteriorationSummary.totalRenewalCostIfAllDone) }}</strong>
          </div>
          <div v-if="componentDeteriorationSummary.uncoveredDeterioration > 0" class="cdet-total-row negative">
            <span>{{ t('summary.propertyValue.componentDeterioration.totalUncovered') }}</span>
            <strong>{{ fmtCur(-componentDeteriorationSummary.uncoveredDeterioration) }}</strong>
          </div>
          <div v-if="componentDeteriorationSummary.coveredByCapex > 0" class="cdet-total-row positive">
            <span>{{ t('summary.propertyValue.componentDeterioration.totalCovered') }}</span>
            <strong>{{ fmtCur(componentDeteriorationSummary.coveredByCapex) }}</strong>
          </div>
        </div>
      </KalkCard>

      <!-- CapEx Timeline (Horizontal) -->
      <KalkCard v-if="result.capexTimeline.length > 0" :title="t('summary.capexTimeline')">
        <!-- Horizontal axis -->
        <div class="captl-axis">
          <div class="captl-track">
            <!-- Year ticks -->
            <template v-for="(year, i) in timelineYears" :key="'y' + year">
              <div class="captl-year-tick" :style="{ left: yearPosition(year) }">
                <span v-if="i % timelineYearStep === 0 || i === timelineYears.length - 1" class="captl-year-label">{{ year }}</span>
              </div>
            </template>
            <!-- Measure dots -->
            <div v-for="item in result.capexTimeline" :key="item.id"
                 class="captl-dot" :class="item.taxClassification"
                 :style="{ left: yearPosition(item.year), width: dotSize(item.amount), height: dotSize(item.amount) }">
            </div>
          </div>
        </div>

        <!-- Grouped list by year -->
        <div class="captl-groups">
          <div v-for="group in capexByYear" :key="group.year" class="captl-group">
            <div class="captl-group-year">{{ group.year }}</div>
            <div class="captl-group-items">
              <div v-for="item in group.items" :key="item.id" class="captl-group-item">
                <span class="captl-dot-inline" :class="item.taxClassification"></span>
                <span class="captl-item-name">{{ item.name }}</span>
                <span class="captl-item-amount">{{ fmtCur(item.amount) }}</span>
                <span class="captl-item-tax">{{ t(`capex.taxClassifications.${item.taxClassification}`) }}</span>
              </div>
            </div>
            <div v-if="group.items.length > 1" class="captl-group-total">{{ fmtCur(group.total) }}</div>
          </div>
        </div>

        <!-- Total -->
        <div class="captl-total">
          <span>{{ t('common.total') }}</span>
          <strong>{{ fmtCur(capexTotal) }}</strong>
        </div>
      </KalkCard>

      <!-- Exit Analysis -->
      <KalkCard :title="t('summary.exit.title')">
        <p class="exit-subtitle">{{ t('summary.exit.subtitle') }}</p>

        <!-- Holding period & speculation status -->
        <div class="exit-meta">
          <div class="exit-meta-item">
            <span>{{ t('summary.exit.holdingPeriod') }}</span>
            <strong>{{ result.exitAnalysis.holdingPeriodYears }} {{ t('summary.exit.years') }}</strong>
          </div>
          <div class="exit-meta-item" :class="{ 'speculation-active': result.exitAnalysis.isWithinSpeculationPeriod }">
            <span>{{ t('summary.exit.speculationPeriod') }}</span>
            <strong>{{ result.exitAnalysis.isWithinSpeculationPeriod ? t('summary.exit.withinPeriod') : t('summary.exit.outsidePeriod') }}</strong>
          </div>
        </div>

        <!-- P&L over holding period -->
        <div class="exit-pnl">
          <h4 class="exit-section-title">{{ t('summary.exit.pnlTitle') }}</h4>
          <div class="exit-pnl-row">
            <span>{{ t('summary.exit.totalGrossIncome') }}</span>
            <strong class="pos">+{{ fmtCur(result.exitAnalysis.totalGrossIncome) }}</strong>
          </div>
          <div class="exit-pnl-row">
            <span>{{ t('summary.exit.totalOperatingCosts') }}</span>
            <strong class="neg">-{{ fmtCur(result.exitAnalysis.totalOperatingCosts) }}</strong>
          </div>
          <div class="exit-pnl-row">
            <span>{{ t('summary.exit.totalDebtService') }}</span>
            <strong class="neg">-{{ fmtCur(result.exitAnalysis.totalDebtService) }}</strong>
          </div>
          <div class="exit-pnl-row" v-if="result.exitAnalysis.totalCapex > 0">
            <span>{{ t('summary.exit.totalCapex') }}</span>
            <strong class="neg">-{{ fmtCur(result.exitAnalysis.totalCapex) }}</strong>
          </div>
          <div v-if="hasMaintenanceReserve" class="exit-pnl-row">
            <span>{{ t('summary.exit.totalMaintenanceReserve') }}</span>
            <strong class="neg">-{{ fmtCur(result.exitAnalysis.totalMaintenanceReserve) }}</strong>
          </div>
          <div v-if="hasMaintenanceReserve && result.exitAnalysis.finalReserveBalance > 0" class="exit-pnl-row">
            <span>{{ t('summary.exit.finalReserveBalance') }}</span>
            <strong class="pos">+{{ fmtCur(result.exitAnalysis.finalReserveBalance) }}</strong>
          </div>
          <div class="exit-pnl-row">
            <span>{{ t('summary.exit.totalTaxPaid') }}</span>
            <strong class="neg">-{{ fmtCur(result.exitAnalysis.totalTaxPaid) }}</strong>
          </div>
          <div class="exit-pnl-row exit-pnl-total">
            <span>{{ t('summary.exit.netCashflow') }}</span>
            <strong :class="valClass(result.exitAnalysis.totalCashflowAfterTax)">{{ fmtCur(result.exitAnalysis.totalCashflowAfterTax) }}</strong>
          </div>
        </div>

        <!-- Scenario comparison -->
        <div class="exit-scenarios">
          <h4 class="exit-section-title">{{ t('summary.exit.scenarioTitle') }}</h4>
          <div class="scenario-table-scroll">
            <table class="scenario-table">
              <thead>
                <tr>
                  <th class="scenario-label-col"></th>
                  <th v-for="s in result.exitAnalysis.scenarios" :key="s.label" class="scenario-col" :class="{ 'scenario-base': s.label === 'base' }">
                    {{ t(`summary.exit.scenario.${s.label}`) }}
                    <span class="scenario-rate">{{ fmtPct(s.annualAppreciationPercent) }} p.a.</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td>{{ t('summary.exit.propertyValue') }}</td>
                  <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" :class="{ 'scenario-base': s.label === 'base' }">{{ fmtCur(s.propertyValueAtExit) }}</td>
                </tr>
                <tr>
                  <td>{{ t('summary.exit.saleCosts') }}</td>
                  <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" class="neg" :class="{ 'scenario-base': s.label === 'base' }">-{{ fmtCur(s.saleCosts) }}</td>
                </tr>
                <tr v-if="result.exitAnalysis.isWithinSpeculationPeriod">
                  <td>{{ t('summary.exit.capitalGainsTax') }}</td>
                  <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" class="neg" :class="{ 'scenario-base': s.label === 'base' }">
                    {{ s.capitalGainsTax > 0 ? `-${fmtCur(s.capitalGainsTax)}` : t('summary.exit.noTax') }}
                  </td>
                </tr>
                <tr v-else>
                  <td>{{ t('summary.exit.capitalGainsTax') }}</td>
                  <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" :class="{ 'scenario-base': s.label === 'base' }">{{ t('summary.exit.noTax') }}</td>
                </tr>
                <tr>
                  <td>{{ t('summary.exit.outstandingDebt') }}</td>
                  <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" class="neg" :class="{ 'scenario-base': s.label === 'base' }">-{{ fmtCur(result.exitAnalysis.outstandingDebtAtExit) }}</td>
                </tr>
                <tr class="scenario-subtotal">
                  <td>{{ t('summary.exit.netSaleProceeds') }}</td>
                  <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" :class="[valClass(s.netSaleProceeds), { 'scenario-base': s.label === 'base' }]">{{ fmtCur(s.netSaleProceeds) }}</td>
                </tr>
                <tr>
                  <td>{{ t('summary.exit.plusCashflow') }}</td>
                  <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" :class="[valClass(result.exitAnalysis.totalCashflowAfterTax), { 'scenario-base': s.label === 'base' }]">+{{ fmtCur(result.exitAnalysis.totalCashflowAfterTax) }}</td>
                </tr>
                <tr>
                  <td>{{ t('summary.exit.minusEquity') }}</td>
                  <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" class="neg" :class="{ 'scenario-base': s.label === 'base' }">-{{ fmtCur(result.exitAnalysis.equityInvested) }}</td>
                </tr>
                <tr class="scenario-total">
                  <td>{{ t('summary.exit.totalReturn') }} <HelpIcon help-key="summary.exit.totalReturn" /></td>
                  <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" :class="[valClass(s.totalReturn), { 'scenario-base': s.label === 'base' }]">
                    <strong>{{ fmtCur(s.totalReturn) }}</strong>
                  </td>
                </tr>
                <tr class="scenario-highlight">
                  <td>{{ t('summary.exit.totalReturnPercent') }}</td>
                  <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" :class="[valClass(s.totalReturnPercent), { 'scenario-base': s.label === 'base' }]">
                    <strong>{{ fmtPct(s.totalReturnPercent) }}</strong>
                  </td>
                </tr>
                <tr class="scenario-highlight">
                  <td>{{ t('summary.exit.annualizedReturn') }} <HelpIcon help-key="summary.exit.annualizedReturn" /></td>
                  <td v-for="s in result.exitAnalysis.scenarios" :key="s.label" :class="[valClass(s.annualizedReturnPercent), { 'scenario-base': s.label === 'base' }]">
                    <strong>{{ fmtPct(s.annualizedReturnPercent) }}</strong>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </KalkCard>

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
import { KalkCard, HelpIcon } from '@/components';
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
  const end = start + result.value.exitAnalysis.holdingPeriodYears;
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
.summary-page { display: flex; flex-direction: column; gap: var(--kalk-space-6); }

/* Tab Bar */
.tab-bar {
  display: flex;
  gap: var(--kalk-space-1);
  border-bottom: 2px solid var(--kalk-gray-200);
  position: sticky;
  top: 0;
  z-index: 10;
  background: var(--kalk-gray-50, #f9fafb);
  padding: 0;
  overflow-x: auto;
  -webkit-overflow-scrolling: touch;
  scrollbar-width: none;
}
.tab-bar::-webkit-scrollbar { display: none; }
.tab-btn {
  flex: 1;
  min-width: 0;
  padding: var(--kalk-space-3) var(--kalk-space-4);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-500);
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  margin-bottom: -2px;
  cursor: pointer;
  white-space: nowrap;
  transition: color 0.15s, border-color 0.15s;
}
.tab-btn:hover { color: var(--kalk-gray-700); }
.tab-btn.active {
  color: var(--kalk-accent-600);
  border-bottom-color: var(--kalk-accent-500);
}

/* Tab Content */
.tab-content { display: flex; flex-direction: column; gap: var(--kalk-space-6); }

/* Collapsible Warnings Card */
.warnings-card {
  border: 1px solid var(--kalk-gray-200);
  border-radius: var(--kalk-radius-md);
  overflow: hidden;
  background: #fff;
}
.warnings-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: var(--kalk-space-3) var(--kalk-space-4);
  background: var(--kalk-gray-50);
  border: none;
  cursor: pointer;
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-700);
  transition: background 0.15s;
}
.warnings-card-header:hover { background: var(--kalk-gray-100); }
.warnings-card-header.has-critical { background: #fef2f2; color: #991b1b; }
.warnings-card-header.has-critical:hover { background: #fee2e2; }
.warnings-card-title {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-2);
}
.warnings-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 20px;
  height: 20px;
  padding: 0 6px;
  border-radius: 10px;
  font-size: 11px;
  font-weight: 700;
  background: var(--kalk-gray-200);
  color: var(--kalk-gray-700);
}
.warnings-badge.critical { background: #fee2e2; color: #991b1b; }
.chevron-icon {
  width: 20px;
  height: 20px;
  flex-shrink: 0;
  color: var(--kalk-gray-400);
  transition: transform 0.2s;
}
.chevron-icon.open { transform: rotate(180deg); }
.warnings-card-body {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-2);
  padding: var(--kalk-space-3) var(--kalk-space-4);
}

/* Empty state */
.empty-result-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: var(--kalk-space-12) var(--kalk-space-6);
  min-height: 300px;
}

.empty-result-icon {
  width: 64px;
  height: 64px;
  color: var(--kalk-gray-300);
  margin-bottom: var(--kalk-space-6);
}

.empty-result-icon svg {
  width: 100%;
  height: 100%;
}

.empty-result-title {
  font-size: var(--kalk-text-lg);
  font-weight: 600;
  color: var(--kalk-gray-700);
  margin: 0 0 var(--kalk-space-3);
}

.empty-result-text {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-500);
  max-width: 360px;
  line-height: 1.6;
  margin: 0;
}

.empty-result-text strong {
  color: var(--kalk-accent-600);
  font-weight: 600;
}

.calculating-indicator {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  margin-top: var(--kalk-space-6);
  font-size: var(--kalk-text-sm);
  color: var(--kalk-accent-600);
  font-weight: 500;
}

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

/* === PROPERTY VALUE FORECAST === */
.pv-chart { display: flex; align-items: flex-end; gap: 2px; height: 180px; padding: 0 var(--kalk-space-2); border-bottom: 1px solid var(--kalk-gray-200); position: relative; }
.pv-bar-group { flex: 1; display: flex; flex-direction: column; align-items: center; height: 100%; justify-content: flex-end; }
.pv-bar { width: 100%; max-width: 32px; background: var(--kalk-primary-700); opacity: 0.7; border-radius: 2px 2px 0 0; transition: height 0.3s; min-height: 2px; position: relative; }
.pv-bar:hover { opacity: 1; }
.pv-condition-warn { position: absolute; top: -14px; left: 50%; transform: translateX(-50%); font-size: 10px; font-weight: 700; color: #92400e; }
.pv-baseline { position: absolute; left: 0; right: 0; border-top: 1.5px dashed var(--kalk-gray-400); z-index: 1; pointer-events: none; }
.pv-baseline-label { position: absolute; right: 0; top: -14px; font-size: 9px; font-weight: 600; color: var(--kalk-gray-500); background: #fff; padding: 0 var(--kalk-space-1); }
.pv-baseline-legend { position: relative; padding-left: 16px; }
.pv-baseline-legend::before { content: ''; position: absolute; left: 0; top: 50%; width: 10px; border-top: 1.5px dashed var(--kalk-gray-400); }
.swatch-pv { background: var(--kalk-primary-700); opacity: 0.7; }

/* Market Comparison Banner */
.market-comparison { display: grid; grid-template-columns: repeat(3, 1fr); gap: var(--kalk-space-4); margin-bottom: var(--kalk-space-6); }
@media (max-width: 600px) { .market-comparison { grid-template-columns: 1fr; } }
.mc-item { display: flex; flex-direction: column; padding: var(--kalk-space-3) var(--kalk-space-4); background: var(--kalk-gray-50); border-radius: var(--kalk-radius-md); }
.mc-item span { font-size: var(--kalk-text-xs); color: var(--kalk-gray-500); margin-bottom: var(--kalk-space-1); }
.mc-item strong { font-size: var(--kalk-text-sm); color: var(--kalk-gray-900); font-variant-numeric: tabular-nums; }
.mc-item.mc-below { border-left: 3px solid var(--kalk-accent-500); }
.mc-item.mc-below strong { color: #047857; }
.mc-item.mc-above { border-left: 3px solid #d97706; }
.mc-item.mc-above strong { color: #92400e; }
.mc-item.mc-at { border-left: 3px solid var(--kalk-gray-400); }

/* Forecast Explanation */
.forecast-explanation { margin-top: var(--kalk-space-6); }
.explanation-list { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: var(--kalk-space-2); }
.explanation-list li { font-size: var(--kalk-text-sm); color: var(--kalk-gray-600); padding: var(--kalk-space-2) var(--kalk-space-3); border-left: 2px solid var(--kalk-gray-200); line-height: 1.5; }
.explanation-list li.summary { border-left-color: var(--kalk-primary-700); font-weight: 500; color: var(--kalk-gray-800); }
.explanation-list li.overdueComponents { border-left-color: #d97706; }
.explanation-list li.investments { border-left-color: var(--kalk-accent-500); }
.explanation-list li.meanReversion { border-left-color: var(--kalk-primary-500); }
.explanation-list li.componentDeterioration { border-left-color: #dc2626; }

/* === COMPONENT DETERIORATION (CARD LAYOUT) === */
.comp-det-status { display: inline-block; padding: 2px 8px; border-radius: var(--kalk-radius-sm); font-size: var(--kalk-text-xs); font-weight: 500; }
.status-OK { background: #dcfce7; color: #166534; }
.status-Renewed { background: #dbeafe; color: #1e40af; }
.status-Overdue { background: #fee2e2; color: #991b1b; }
.status-OverdueAtPurchase { background: var(--kalk-gray-100); color: var(--kalk-gray-500); }

/* Summary Strip */
.cdet-summary-strip {
  display: flex; align-items: center; gap: var(--kalk-space-3); flex-wrap: wrap;
  margin-bottom: var(--kalk-space-4); padding: var(--kalk-space-3) var(--kalk-space-4);
  background: var(--kalk-gray-50); border-radius: var(--kalk-radius-md);
}
.cdet-counter {
  display: flex; align-items: center; gap: var(--kalk-space-1);
  padding: var(--kalk-space-1) var(--kalk-space-3); border-radius: 999px;
  font-size: var(--kalk-text-xs); font-weight: 500;
}
.cdet-counter.status-OK { background: #dcfce7; color: #166534; }
.cdet-counter.status-Renewed { background: #dbeafe; color: #1e40af; }
.cdet-counter.status-Overdue { background: #fee2e2; color: #991b1b; }
.cdet-counter.status-OverdueAtPurchase { background: var(--kalk-gray-100); color: var(--kalk-gray-500); }
.cdet-counter-num { font-weight: 700; font-size: var(--kalk-text-sm); font-variant-numeric: tabular-nums; }
.cdet-impact-total {
  margin-left: auto; display: flex; flex-direction: column; align-items: flex-end;
  font-variant-numeric: tabular-nums;
}
.cdet-impact-label { font-size: var(--kalk-text-xs); color: var(--kalk-gray-500); }
.cdet-impact-total strong { font-size: var(--kalk-text-sm); font-weight: 700; }
@media (max-width: 400px) {
  .cdet-impact-total {
    margin-left: 0; width: 100%; flex-direction: row; justify-content: space-between;
    align-items: center; padding-top: var(--kalk-space-2); border-top: 1px solid var(--kalk-gray-200);
  }
}

/* Card Grid */
.cdet-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: var(--kalk-space-3); align-items: start; }
@media (max-width: 600px) { .cdet-grid { grid-template-columns: 1fr; } }
@media (min-width: 900px) { .cdet-grid { grid-template-columns: repeat(3, 1fr); } }

.cdet-card {
  position: relative; display: flex; flex-direction: column;
  padding: var(--kalk-space-3) var(--kalk-space-4); background: #fff;
  border: 1px solid var(--kalk-gray-200); border-radius: var(--kalk-radius-md);
  cursor: pointer; transition: box-shadow 0.15s, border-color 0.15s;
  border-left: 3px solid var(--kalk-gray-300);
}
.cdet-card:hover { box-shadow: var(--kalk-shadow-sm); border-color: var(--kalk-gray-300); }
.cdet-card:focus-visible { outline: 2px solid var(--kalk-accent-500); outline-offset: 2px; }
.cdet-card.cdet-OK { border-left-color: #16a34a; }
.cdet-card.cdet-Renewed { border-left-color: #2563eb; }
.cdet-card.cdet-Overdue { border-left-color: #dc2626; background: #fef2f2; }
.cdet-card.cdet-OverdueAtPurchase { border-left-color: var(--kalk-gray-400); }

.cdet-card-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--kalk-space-2); }
.cdet-card-name { font-size: var(--kalk-text-sm); font-weight: 600; color: var(--kalk-gray-900); }

.cdet-card-metrics { display: flex; gap: var(--kalk-space-3); flex-wrap: wrap; }
.cdet-metric { display: flex; flex-direction: column; min-width: 0; }
.cdet-metric-label { font-size: 10px; color: var(--kalk-gray-500); text-transform: uppercase; letter-spacing: 0.04em; }
.cdet-metric-value { font-size: var(--kalk-text-sm); font-weight: 600; color: var(--kalk-gray-800); font-variant-numeric: tabular-nums; }

/* Lifecycle Bar */
.cdet-lifecycle-bar { height: 4px; background: var(--kalk-gray-100); border-radius: 2px; margin-top: var(--kalk-space-2); overflow: hidden; }
.cdet-lifecycle-fill { height: 100%; border-radius: 2px; transition: width 0.5s ease-out; }
.cdet-fill-OK { background: #16a34a; }
.cdet-fill-Renewed { background: #2563eb; }
.cdet-fill-Overdue { background: #dc2626; }
.cdet-fill-OverdueAtPurchase { background: var(--kalk-gray-400); }

/* Expand Hint (mobile only) */
.cdet-expand-hint { display: flex; justify-content: center; margin-top: var(--kalk-space-1); color: var(--kalk-gray-400); }
.cdet-expand-hint svg { transition: transform 0.2s; }
.cdet-expand-hint svg.rotated { transform: rotate(180deg); }

/* Detail Section */
.cdet-detail {
  margin-top: var(--kalk-space-3); padding-top: var(--kalk-space-3);
  border-top: 1px solid var(--kalk-gray-100); display: flex; flex-direction: column; gap: var(--kalk-space-2);
}

/* Mobile: collapse/expand behavior */
@media (max-width: 899px) {
  .cdet-detail { display: none; }
  .cdet-card.expanded .cdet-detail { display: flex; }
}

/* Desktop: always show detail, hide chevron, no click affordance */
@media (min-width: 900px) {
  .cdet-card { cursor: default; }
  .cdet-card:hover { box-shadow: none; }
  .cdet-expand-hint { display: none; }
}
.cdet-detail-row {
  display: flex; justify-content: space-between; align-items: center;
  font-size: var(--kalk-text-xs); color: var(--kalk-gray-600);
}
.cdet-detail-row strong { color: var(--kalk-gray-800); font-variant-numeric: tabular-nums; }
.cdet-priced-in-hint {
  font-size: var(--kalk-text-xs); color: var(--kalk-gray-500); line-height: 1.5;
  padding: var(--kalk-space-2) var(--kalk-space-3); background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-sm); margin-bottom: var(--kalk-space-1);
}
.cdet-detail-row strong.positive { color: #16a34a; }

.cdet-recurring {
  padding: var(--kalk-space-2) var(--kalk-space-3); background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-sm); margin-top: var(--kalk-space-1);
}
.cdet-recurring-badge {
  display: inline-block; font-size: 10px; font-weight: 600; text-transform: uppercase;
  letter-spacing: 0.05em; color: var(--kalk-accent-700); margin-bottom: var(--kalk-space-2);
}
.cdet-recurring-details { display: flex; flex-direction: column; gap: var(--kalk-space-1); }

/* Bottom Totals */
.cdet-totals {
  margin-top: var(--kalk-space-4); padding-top: var(--kalk-space-3);
  border-top: 2px solid var(--kalk-gray-200); display: flex; flex-direction: column; gap: var(--kalk-space-2);
}
.cdet-total-row {
  display: flex; justify-content: space-between; align-items: center;
  font-size: var(--kalk-text-sm); padding: var(--kalk-space-2) var(--kalk-space-3); border-radius: var(--kalk-radius-sm);
}
.cdet-total-row span { color: var(--kalk-gray-600); }
.cdet-total-row strong { font-weight: 600; font-variant-numeric: tabular-nums; }
.cdet-total-row.negative { background: #fef2f2; }
.cdet-total-row.negative strong { color: #dc2626; }
.cdet-total-row.positive { background: #ecfdf5; }
.cdet-total-row.positive strong { color: #16a34a; }

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

/* === CAPEX TIMELINE (HORIZONTAL) === */

/* Axis */
.captl-axis {
  position: relative; margin-bottom: var(--kalk-space-6);
  padding: var(--kalk-space-4) var(--kalk-space-2) var(--kalk-space-8);
}
.captl-track {
  position: relative; height: 2px; background: var(--kalk-gray-200); border-radius: 1px;
}
.captl-year-tick {
  position: absolute; top: -4px; transform: translateX(-50%);
  width: 1px; height: 10px; background: var(--kalk-gray-300);
}
.captl-year-label {
  position: absolute; top: 14px; left: 50%; transform: translateX(-50%);
  font-size: 10px; font-weight: 600; color: var(--kalk-gray-500);
  font-variant-numeric: tabular-nums; white-space: nowrap;
}
.captl-dot {
  position: absolute; top: 50%; transform: translate(-50%, -50%);
  border-radius: 50%; min-width: 8px; min-height: 8px;
  border: 2px solid #fff; box-shadow: 0 0 0 1px rgba(0,0,0,0.1);
  z-index: 1;
}
.captl-dot.MaintenanceExpense { background: var(--kalk-accent-500); }
.captl-dot.AcquisitionCost { background: var(--kalk-primary-700); }
.captl-dot.ImprovementCost { background: var(--kalk-primary-900); }
.captl-dot.NotDeductible { background: var(--kalk-gray-400); }

/* Grouped list */
.captl-groups {
  display: flex; flex-direction: column; gap: var(--kalk-space-1);
}
.captl-group {
  display: flex; align-items: baseline; gap: var(--kalk-space-3);
  padding: var(--kalk-space-2) 0;
  border-bottom: 1px solid var(--kalk-gray-50);
}
.captl-group:last-child { border-bottom: none; }
.captl-group-year {
  width: 40px; flex-shrink: 0; font-size: var(--kalk-text-xs); font-weight: 700;
  color: var(--kalk-gray-600); font-variant-numeric: tabular-nums;
}
.captl-group-items { flex: 1; display: flex; flex-direction: column; gap: var(--kalk-space-1); }
.captl-group-item {
  display: flex; align-items: center; gap: var(--kalk-space-2);
  font-size: var(--kalk-text-xs); color: var(--kalk-gray-700);
}
.captl-dot-inline {
  width: 6px; height: 6px; border-radius: 50%; flex-shrink: 0;
}
.captl-dot-inline.MaintenanceExpense { background: var(--kalk-accent-500); }
.captl-dot-inline.AcquisitionCost { background: var(--kalk-primary-700); }
.captl-dot-inline.ImprovementCost { background: var(--kalk-primary-900); }
.captl-dot-inline.NotDeductible { background: var(--kalk-gray-400); }
.captl-item-name { flex: 1; font-weight: 500; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.captl-item-amount { font-weight: 700; font-variant-numeric: tabular-nums; white-space: nowrap; }
.captl-item-tax {
  font-size: 10px; color: var(--kalk-gray-400); white-space: nowrap; flex-shrink: 0;
}
.captl-group-total {
  flex-shrink: 0; font-size: var(--kalk-text-xs); font-weight: 700;
  color: var(--kalk-gray-500); font-variant-numeric: tabular-nums; white-space: nowrap;
  padding-left: var(--kalk-space-2); border-left: 1px solid var(--kalk-gray-200);
}

/* Total row */
.captl-total {
  display: flex; justify-content: space-between; align-items: center;
  margin-top: var(--kalk-space-3); padding-top: var(--kalk-space-3);
  border-top: 2px solid var(--kalk-gray-200); font-size: var(--kalk-text-sm);
}
.captl-total span { color: var(--kalk-gray-600); }
.captl-total strong { font-weight: 700; font-variant-numeric: tabular-nums; }

/* Mobile: hide axis, show only list */
@media (max-width: 600px) {
  .captl-axis { display: none; }
  .captl-item-tax { display: none; }
  .captl-group { flex-wrap: wrap; }
}

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
.tax-row.tax-row-info { background: #fffbeb; }
.tax-row.tax-row-info span { color: var(--kalk-gray-500); font-style: italic; }
.hint-text { color: var(--kalk-gray-500) !important; font-style: italic; }

/* === EXIT ANALYSIS === */
.exit-subtitle { font-size: var(--kalk-text-sm); color: var(--kalk-gray-500); margin-bottom: var(--kalk-space-4); }

.exit-meta { display: flex; gap: var(--kalk-space-4); margin-bottom: var(--kalk-space-6); }
@media (max-width: 600px) { .exit-meta { flex-direction: column; } }
.exit-meta-item { flex: 1; display: flex; flex-direction: column; padding: var(--kalk-space-3) var(--kalk-space-4); background: var(--kalk-gray-50); border-radius: var(--kalk-radius-md); }
.exit-meta-item span { font-size: var(--kalk-text-xs); color: var(--kalk-gray-500); margin-bottom: var(--kalk-space-1); }
.exit-meta-item strong { font-size: var(--kalk-text-sm); color: var(--kalk-gray-900); }
.exit-meta-item.speculation-active { border-left: 3px solid #92400e; }
.exit-meta-item.speculation-active strong { color: #92400e; }

.exit-section-title { font-size: var(--kalk-text-sm); font-weight: 600; color: var(--kalk-gray-700); margin-bottom: var(--kalk-space-3); padding-bottom: var(--kalk-space-2); border-bottom: 1px solid var(--kalk-gray-200); }

.exit-pnl { margin-bottom: var(--kalk-space-6); }
.exit-pnl-row { display: flex; justify-content: space-between; align-items: center; padding: var(--kalk-space-2) var(--kalk-space-3); font-size: var(--kalk-text-sm); }
.exit-pnl-row span { color: var(--kalk-gray-600); }
.exit-pnl-row strong { font-variant-numeric: tabular-nums; }
.exit-pnl-total { border-top: 2px solid var(--kalk-gray-300); margin-top: var(--kalk-space-2); padding-top: var(--kalk-space-3); background: var(--kalk-gray-50); border-radius: var(--kalk-radius-sm); }
.exit-pnl-total span { font-weight: 600; color: var(--kalk-gray-900); }

.exit-scenarios { margin-top: var(--kalk-space-2); }
.scenario-table-scroll { overflow-x: auto; -webkit-overflow-scrolling: touch; }
.scenario-table { width: 100%; border-collapse: collapse; font-size: var(--kalk-text-xs); font-variant-numeric: tabular-nums; }
.scenario-table th, .scenario-table td { padding: var(--kalk-space-2) var(--kalk-space-3); text-align: right; white-space: nowrap; border-bottom: 1px solid var(--kalk-gray-100); }
.scenario-table th { background: var(--kalk-gray-50); color: var(--kalk-gray-600); font-weight: 600; font-size: var(--kalk-text-xs); }
.scenario-table .scenario-label-col { text-align: left; min-width: 140px; }
.scenario-table td:first-child { text-align: left; color: var(--kalk-gray-600); font-weight: 500; }
.scenario-rate { display: block; font-weight: 400; font-size: 10px; color: var(--kalk-gray-400); }
.scenario-col { min-width: 120px; }
.scenario-base { background: var(--kalk-gray-50); }
.scenario-subtotal td { border-top: 1.5px solid var(--kalk-gray-300); font-weight: 600; }
.scenario-total td { border-top: 2px solid var(--kalk-gray-400); background: var(--kalk-gray-50); font-weight: 700; }
.scenario-highlight td { background: var(--kalk-gray-50); font-weight: 600; }

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
