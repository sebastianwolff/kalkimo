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
      <div class="section-header">
        <h4 class="section-label">{{ t('financing.loans') }}</h4>
        <button type="button" class="btn btn-outline btn-sm" @click="addLoan">
          <svg class="btn-icon" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
          </svg>
          {{ t('financing.addLoan') }}
        </button>
      </div>

      <div v-if="loans.length === 0" class="empty-state">
        <p>Keine Darlehen hinzugefügt</p>
      </div>

      <div v-else class="loans-list">
        <div
          v-for="loan in loans"
          :key="loan.id"
          class="loan-card"
        >
          <div class="loan-header">
            <input
              v-model="loan.name"
              type="text"
              class="loan-name-input"
              placeholder="z.B. Hauptdarlehen"
            />
            <button
              type="button"
              class="delete-btn"
              @click="removeLoan(loan.id)"
              :title="t('common.delete')"
            >
              <svg viewBox="0 0 20 20" fill="currentColor">
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
          </div>
        </div>
      </div>
    </div>

    <!-- Summary -->
    <div class="financing-summary">
      <div class="summary-row">
        <span>Gesamtkapitalbedarf: <HelpIcon help-key="financing.totalCapitalNeed" /></span>
        <strong>{{ formatCurrency(totalCapitalNeed) }}</strong>
      </div>
      <div v-if="totalCapexAmount > 0" class="summary-row sub">
        <span>davon Kaufpreis + Nebenkosten:</span>
        <span>{{ formatCurrency(totalInvestment) }}</span>
      </div>
      <div v-if="totalCapexAmount > 0" class="summary-row sub">
        <span>davon Investitionsmaßnahmen:</span>
        <span>{{ formatCurrency(totalCapexAmount) }}</span>
      </div>
      <div class="summary-row">
        <span>{{ t('financing.summary.totalLoan') }}:</span>
        <strong>{{ formatCurrency(totalLoans) }}</strong>
      </div>
      <div class="summary-row">
        <span>{{ t('financing.summary.totalEquity') }}:</span>
        <strong>{{ formatCurrency(equity || 0) }}</strong>
      </div>
      <div class="summary-row">
        <span>Finanzierung gesamt (EK + FK):</span>
        <strong>{{ formatCurrency(totalFinancing) }}</strong>
      </div>
      <div :class="['summary-row', 'highlight', { 'highlight-error': !isFinancingCovered }]">
        <span>{{ isFinancingCovered ? 'Finanzierung gedeckt' : 'Finanzierungslücke' }}: <HelpIcon help-key="financing.gap" /></span>
        <strong>{{ formatCurrency(financingGap) }}</strong>
      </div>
    </div>

    <!-- Validation message -->
    <div v-if="!isFinancingCovered" class="validation-error">
      <svg class="error-icon" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
      </svg>
      <p>
        Die Finanzierung deckt den Gesamtkapitalbedarf nicht.
        Es fehlen <strong>{{ formatCurrency(Math.abs(financingGap)) }}</strong>.
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
      fixedInterestYears: l.fixedInterestYears
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
          startDate: startPeriod
        }))
      }
    });
  },
  { deep: true }
);
</script>

<style scoped>
.loans-section {
  margin-top: var(--kalk-space-8);
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--kalk-space-4);
}

.section-label {
  font-size: var(--kalk-text-base);
  font-weight: 600;
  color: var(--kalk-gray-900);
  margin: 0;
}

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

.btn-sm {
  height: 36px;
  padding: 0 var(--kalk-space-4);
  font-size: var(--kalk-text-xs);
}

.btn-outline {
  background: #ffffff;
  color: var(--kalk-gray-700);
  border: 1.5px solid var(--kalk-gray-300);
}

.btn-outline:hover {
  background: var(--kalk-gray-50);
  border-color: var(--kalk-gray-400);
}

.btn-icon {
  width: 16px;
  height: 16px;
}

.empty-state {
  text-align: center;
  padding: var(--kalk-space-8);
  background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-md);
  border: 2px dashed var(--kalk-gray-200);
}

.empty-state p {
  color: var(--kalk-gray-500);
  margin: 0;
}

.loans-list {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-3);
}

.loan-card {
  background: #ffffff;
  border: 1px solid var(--kalk-gray-200);
  border-radius: var(--kalk-radius-md);
  padding: var(--kalk-space-4);
  transition: border-color 0.15s, box-shadow 0.15s;
}

.loan-card:hover {
  border-color: var(--kalk-gray-300);
  box-shadow: var(--kalk-shadow-sm);
}

.loan-header {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  margin-bottom: var(--kalk-space-4);
  padding-bottom: var(--kalk-space-3);
  border-bottom: 1px solid var(--kalk-gray-100);
}

.loan-name-input {
  flex: 1;
  padding: var(--kalk-space-2) var(--kalk-space-3);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-900);
  background: transparent;
  border: none;
  border-bottom: 1px solid transparent;
  outline: none;
  transition: border-color 0.15s;
}

.loan-name-input:focus {
  border-bottom-color: var(--kalk-accent-500);
}

.loan-name-input::placeholder {
  color: var(--kalk-gray-400);
  font-weight: 500;
}

.delete-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  background: transparent;
  border: none;
  border-radius: var(--kalk-radius-sm);
  color: var(--kalk-gray-400);
  cursor: pointer;
  transition: all 0.15s;
  flex-shrink: 0;
}

.delete-btn:hover {
  background: var(--kalk-error-bg);
  color: var(--kalk-error);
}

.delete-btn svg {
  width: 18px;
  height: 18px;
}

.loan-fields {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--kalk-space-4);
}

@media (max-width: 600px) {
  .loan-fields {
    grid-template-columns: 1fr;
  }
}

.financing-summary {
  margin-top: var(--kalk-space-8);
  padding: var(--kalk-space-6);
  background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-md);
}

.summary-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--kalk-space-2) 0;
  border-bottom: 1px solid var(--kalk-gray-200);
}

.summary-row:last-child {
  border-bottom: none;
}

.summary-row span {
  color: var(--kalk-gray-600);
  font-size: var(--kalk-text-sm);
}

.summary-row strong {
  color: var(--kalk-gray-900);
  font-variant-numeric: tabular-nums;
}

.summary-row.highlight {
  margin-top: var(--kalk-space-2);
  padding-top: var(--kalk-space-4);
  border-top: 2px solid var(--kalk-accent-500);
  border-bottom: none;
  font-size: var(--kalk-text-lg);
}

.summary-row.highlight span {
  color: var(--kalk-accent-600);
}

.summary-row.highlight strong {
  color: var(--kalk-accent-600);
}

.summary-row.highlight-error {
  border-top-color: var(--kalk-error);
}

.summary-row.highlight-error span,
.summary-row.highlight-error strong {
  color: var(--kalk-error);
}

.summary-row.sub {
  border-bottom: none;
  padding: var(--kalk-space-1) 0 var(--kalk-space-1) var(--kalk-space-4);
}

.summary-row.sub span {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-400);
}

.validation-error {
  display: flex;
  align-items: flex-start;
  gap: var(--kalk-space-3);
  margin-top: var(--kalk-space-4);
  padding: var(--kalk-space-4);
  background: rgba(220, 38, 38, 0.05);
  border: 1px solid rgba(220, 38, 38, 0.2);
  border-radius: var(--kalk-radius-md);
}

.error-icon {
  width: 20px;
  height: 20px;
  color: var(--kalk-error);
  flex-shrink: 0;
  margin-top: 2px;
}

.validation-error p {
  margin: 0;
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-700);
  line-height: 1.5;
}

.validation-error strong {
  color: var(--kalk-error);
}
</style>
