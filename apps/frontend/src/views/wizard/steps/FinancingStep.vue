<template>
  <KalkCard :title="t('financing.title')">
    <KalkCurrency
      v-model="equity"
      :label="t('financing.equity')"
      :currency="currency"
      required
      help-key="financing.equity"
    />

    <!-- Loans Section -->
    <div class="loans-section">
      <div class="block-header">
        <h4 class="section-title">{{ t('financing.loans') }}</h4>
        <button type="button" class="btn btn-outline btn-sm" @click="addLoan">
          <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
          </svg>
          {{ t('financing.addLoan') }}
        </button>
      </div>

      <div v-if="loans.length === 0" class="empty-row">
        Keine Darlehen hinzugefügt
      </div>

      <div v-else class="loans-list">
        <div
          v-for="(loan, index) in loans"
          :key="loan.id"
          class="panel item-panel"
        >
          <div class="item-header">
            <span class="item-index">{{ String(index + 1).padStart(2, '0') }}</span>
            <input
              v-model="loan.name"
              type="text"
              class="item-name-input"
              placeholder="z.B. Hauptdarlehen"
            />
            <button
              type="button"
              class="icon-btn"
              @click="removeLoan(loan.id)"
              :title="t('common.delete')"
              :aria-label="t('common.delete')"
            >
              <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" />
              </svg>
            </button>
          </div>

          <div class="loan-fields">
            <KalkCurrency
              v-model="loan.principal"
              :label="t('financing.loan.principal')"
              :currency="currency"
              required
              help-key="financing.principal"
            />
            <KalkPercent
              v-model="loan.interestRate"
              :label="t('financing.loan.interestRate')"
              :min="0"
              :max="15"
              :decimals="2"
              help-key="financing.interestRate"
            />
            <KalkPercent
              v-model="loan.initialRepayment"
              :label="t('financing.loan.initialRepayment')"
              :min="0"
              :max="10"
              :decimals="2"
              help-key="financing.initialRepayment"
            />
            <KalkInput
              v-model="loan.fixedInterestYears"
              :label="t('financing.loan.fixedInterestYears')"
              type="number"
              :min="1"
              :max="30"
              :suffix="t('common.years')"
              help-key="financing.fixedInterestYears"
            />
            <KalkPercent
              v-model="loan.refinancingRate"
              :label="t('financing.loan.refinancingRate')"
              :hint="t('financing.loan.refinancingRateHint')"
              :min="0"
              :max="15"
              :decimals="2"
            />
            <KalkPercent
              v-model="loan.refinancingRepayment"
              :label="t('financing.loan.refinancingRepayment')"
              :hint="t('financing.loan.refinancingRepaymentHint')"
              :min="0"
              :max="20"
              :decimals="2"
            />
            <KalkInput
              v-model="loan.gracePeriodYears"
              :label="t('financing.loan.gracePeriodYears')"
              type="number"
              :min="0"
              :max="10"
              :suffix="t('common.years')"
            />
          </div>
        </div>
      </div>
    </div>

    <!-- Summary -->
    <div class="summary-box statement">
      <div class="data-row">
        <span class="data-label strong">Gesamtkapitalbedarf <HelpIcon help-key="financing.totalCapitalNeed" /></span>
        <span class="data-value">{{ formatCurrency(totalCapitalNeed) }}</span>
      </div>
      <div v-if="totalCapexAmount > 0" class="data-row sub">
        <span class="data-label">davon Kaufpreis + Nebenkosten</span>
        <span class="data-value">{{ formatCurrency(totalInvestment) }}</span>
      </div>
      <div v-if="totalCapexAmount > 0" class="data-row sub">
        <span class="data-label">davon Investitionsmaßnahmen</span>
        <span class="data-value">{{ formatCurrency(totalCapexAmount) }}</span>
      </div>
      <div class="data-row">
        <span class="data-label">{{ t('financing.summary.totalLoan') }}</span>
        <span class="data-value">{{ formatCurrency(totalLoans) }}</span>
      </div>
      <div class="data-row">
        <span class="data-label">{{ t('financing.summary.totalEquity') }}</span>
        <span class="data-value">{{ formatCurrency(equity || 0) }}</span>
      </div>
      <div class="data-row">
        <span class="data-label strong">Finanzierung gesamt (EK + FK)</span>
        <span class="data-value">{{ formatCurrency(totalFinancing) }}</span>
      </div>
      <div :class="['data-row', 'total', { 'is-gap': !isFinancingCovered }]">
        <span class="data-label">{{ isFinancingCovered ? 'Finanzierung gedeckt' : 'Finanzierungslücke' }} <HelpIcon help-key="financing.gap" /></span>
        <span class="data-value">{{ formatCurrency(financingGap) }}</span>
      </div>
    </div>

    <!-- Validation message -->
    <div v-if="!isFinancingCovered" class="callout error gap-callout" role="alert">
      <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
        <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
      </svg>
      <p>
        Die Finanzierung deckt den Gesamtkapitalbedarf nicht.
        Es fehlen <strong class="num">{{ formatCurrency(Math.abs(financingGap)) }}</strong>.
        Bitte erhöhen Sie das Eigenkapital oder fügen Sie weitere Darlehen hinzu.
      </p>
    </div>
  </KalkCard>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { KalkCard, KalkCurrency, KalkPercent, KalkInput, HelpIcon } from '@/components';
import { useProjectStore } from '@/stores/projectStore';

interface LoanItem {
  id: string;
  name: string;
  principal: number;
  interestRate: number;
  initialRepayment: number;
  fixedInterestYears: number;
  refinancingRate?: number;
  refinancingRepayment?: number;
  gracePeriodYears?: number;
}

const emit = defineEmits<{
  'validation-change': [valid: boolean];
}>();

const { t, locale } = useI18n();
const projectStore = useProjectStore();

const equity = ref<number>(60000);
const loans = ref<LoanItem[]>([]);

const currency = computed(() => projectStore.currentProject?.currency || 'EUR');

const totalLoans = computed(() =>
  loans.value.reduce((sum, l) => sum + (l.principal || 0), 0)
);

const totalInvestment = computed(() =>
  projectStore.totalPurchasePrice?.amount || 0
);

const totalCapexAmount = computed(() => projectStore.totalCapex);

const totalCapitalNeed = computed(() =>
  projectStore.totalCapitalRequirement?.amount || 0
);

const totalFinancing = computed(() =>
  (equity.value || 0) + totalLoans.value
);

const financingGap = computed(() =>
  totalFinancing.value - totalCapitalNeed.value
);

const isFinancingCovered = computed(() =>
  financingGap.value >= 0
);

function formatCurrency(value: number): string {
  return new Intl.NumberFormat(locale.value, {
    style: 'currency',
    currency: currency.value
  }).format(value);
}

function addLoan() {
  const remaining = totalCapitalNeed.value - (equity.value || 0) - totalLoans.value;
  loans.value.push({
    id: crypto.randomUUID(),
    name: `Darlehen ${loans.value.length + 1}`,
    principal: Math.max(0, remaining),
    interestRate: 3.5,
    initialRepayment: 2.0,
    fixedInterestYears: 10
  });
}

function removeLoan(id: string) {
  const index = loans.value.findIndex(l => l.id === id);
  if (index !== -1) {
    loans.value.splice(index, 1);
  }
}

// Initialize from store
onMounted(() => {
  const financing = projectStore.currentProject?.financing;
  if (financing) {
    equity.value = financing.equity.amount;
    loans.value = financing.loans.map(l => ({
      id: l.id,
      name: l.name,
      principal: l.principal.amount,
      interestRate: l.interestRatePercent,
      initialRepayment: l.initialRepaymentPercent,
      fixedInterestYears: l.fixedInterestYears,
      refinancingRate: l.refinancingInterestRatePercent,
      refinancingRepayment: l.refinancingRepaymentPercent,
      gracePeriodYears: l.gracePeriodYears
    }));
  }
});

// Validation: financing must cover total capital requirement
const isValid = computed(() => equity.value >= 0 && isFinancingCovered.value);

watch(isValid, (valid) => {
  emit('validation-change', valid);
}, { immediate: true });

// Sync to store
watch(
  [equity, loans],
  () => {
    if (!projectStore.currentProject) return;

    const startPeriod = projectStore.currentProject.startPeriod;

    projectStore.updateProject({
      financing: {
        equity: { amount: equity.value, currency: currency.value },
        loans: loans.value.map(l => ({
          id: l.id,
          name: l.name,
          principal: { amount: l.principal, currency: currency.value },
          interestRatePercent: l.interestRate,
          initialRepaymentPercent: l.initialRepayment,
          fixedInterestYears: l.fixedInterestYears,
          startDate: startPeriod,
          refinancingInterestRatePercent: l.refinancingRate,
          refinancingRepaymentPercent: l.refinancingRepayment,
          gracePeriodYears: l.gracePeriodYears ? Number(l.gracePeriodYears) : undefined
        }))
      }
    });
  },
  { deep: true }
);
</script>

<style scoped>
.loans-section {
  margin-top: var(--kalk-space-6);
  padding-top: var(--kalk-space-5);
  border-top: 1px solid var(--kalk-border);
}

.block-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: var(--kalk-space-3);
  margin-bottom: var(--kalk-space-3);
}

.block-header .section-title {
  margin: 0;
}

.empty-row {
  padding: var(--kalk-space-3) var(--kalk-space-4);
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-500);
  background: var(--kalk-gray-50);
  border: 1px solid var(--kalk-border);
  border-radius: var(--kalk-radius-md);
}

.loans-list {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-3);
}

.item-panel {
  border-radius: var(--kalk-radius-md);
}

.item-header {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  padding: var(--kalk-space-1) var(--kalk-space-2) var(--kalk-space-1) var(--kalk-space-4);
  background: var(--kalk-gray-50);
  border-bottom: 1px solid var(--kalk-border);
}

.item-index {
  font-size: var(--kalk-text-xs);
  font-weight: 600;
  color: var(--kalk-gray-500);
  font-variant-numeric: tabular-nums;
}

.item-name-input {
  flex: 1;
  min-width: 0;
  height: 32px;
  padding: 0 var(--kalk-space-2);
  margin-left: calc(-1 * var(--kalk-space-2));
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-900);
  background: transparent;
  border: 1px solid transparent;
  border-radius: var(--kalk-radius-sm);
  outline: none;
  transition: border-color 0.12s, background-color 0.12s;
}

.item-name-input:hover {
  border-color: var(--kalk-border);
}

.item-name-input:focus {
  background: var(--kalk-surface);
  border-color: var(--kalk-navy-600);
}

.item-name-input::placeholder {
  color: var(--kalk-gray-400);
  font-weight: 500;
}

.icon-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  background: transparent;
  border: 1px solid transparent;
  border-radius: var(--kalk-radius-md);
  color: var(--kalk-gray-400);
  cursor: pointer;
  flex-shrink: 0;
  transition: color 0.12s, background-color 0.12s, border-color 0.12s;
}

.icon-btn:hover {
  color: var(--kalk-negative);
  background: var(--kalk-negative-bg);
  border-color: var(--kalk-negative-border);
}

.icon-btn svg {
  width: 16px;
  height: 16px;
}

.loan-fields {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  column-gap: var(--kalk-space-4);
  padding: var(--kalk-space-4) var(--kalk-space-4) 0;
}

@media (max-width: 600px) {
  .loan-fields {
    grid-template-columns: minmax(0, 1fr);
  }
}

/* Finanzierungsuebersicht im Stil einer Aufstellung */
.statement {
  margin-top: var(--kalk-space-6);
  background: var(--kalk-surface);
}

.statement .data-label {
  display: inline-flex;
  align-items: center;
  gap: 2px;
}

.statement .data-label.strong {
  color: var(--kalk-gray-900);
  font-weight: 600;
}

.statement .data-row.sub {
  padding: 6px 0 6px var(--kalk-space-4);
}

.statement .data-row.sub .data-label,
.statement .data-row.sub .data-value {
  font-size: var(--kalk-text-sm);
  font-weight: 400;
  color: var(--kalk-gray-500);
}

.statement .data-row.total .data-value {
  font-size: var(--kalk-text-lg);
}

.statement .data-row.total.is-gap .data-label,
.statement .data-row.total.is-gap .data-value {
  color: var(--kalk-negative);
}

.gap-callout {
  margin-top: var(--kalk-space-4);
}

.gap-callout strong {
  color: var(--kalk-negative);
  font-weight: 600;
}
</style>
