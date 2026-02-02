<template>
  <KalkCard :title="t('purchase.title')">
    <KalkCurrency
      v-model="purchasePrice"
      :label="t('purchase.price')"
      :currency="currency"
      required
      help-key="purchase.price"
    />

    <KalkDatePicker
      v-model="purchaseDate"
      :label="t('purchase.date')"
      :min-year="2020"
      :max-year="2040"
      required
      help-key="purchase.date"
    />

    <KalkAmountInput
      v-model="landValue"
      v-model:model-mode="landValueMode"
      :label="t('purchase.landValuePercent')"
      :base-value="totalInvestment"
      :currency="currency"
      :show-calculated="true"
      default-mode="percent"
      :hint="t('purchase.landValueInfo')"
      help-key="purchase.landValue"
    />

    <!-- Purchase Costs Section -->
    <div class="costs-section">
      <div class="section-header">
        <h4 class="section-label">{{ t('purchase.costs') }} <HelpIcon help-key="purchase.costs" /></h4>
        <button type="button" class="btn btn-outline btn-sm" @click="addCostItem">
          <svg class="btn-icon" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
          </svg>
          {{ t('purchase.addCost') }}
        </button>
      </div>

      <div v-if="costs.length === 0" class="empty-state">
        <p>{{ t('purchase.noCosts') || 'Keine Kaufnebenkosten hinzugefügt' }}</p>
        <button type="button" class="btn btn-accent btn-sm" @click="addDefaultCosts">
          Typische Kosten hinzufügen
        </button>
      </div>

      <div v-else class="costs-list">
        <div
          v-for="cost in costs"
          :key="cost.id"
          class="cost-card"
        >
          <div class="cost-card-content">
            <div class="cost-name-row">
              <input
                v-model="cost.name"
                type="text"
                class="cost-name-input"
                placeholder="Kostenart"
              />
              <button
                type="button"
                class="delete-btn"
                @click="removeCostItem(cost.id)"
                :title="t('common.delete')"
              >
                <svg viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" />
                </svg>
              </button>
            </div>
            <div class="cost-amount-row">
              <KalkAmountInput
                v-model="cost.amount"
                v-model:model-mode="cost.mode"
                :base-value="purchasePrice"
                :currency="currency"
                :show-calculated="true"
                default-mode="percent"
              />
            </div>
          </div>
        </div>
      </div>

      <div v-if="costs.length > 0" class="costs-summary">
        <span>{{ t('purchase.totalCosts') }}:</span>
        <strong>{{ formatCurrency(totalCosts) }}</strong>
      </div>
    </div>

    <!-- Total Investment -->
    <div class="investment-summary">
      <span>{{ t('purchase.totalInvestment') }}: <HelpIcon help-key="purchase.totalInvestment" /></span>
      <strong class="total-amount">{{ formatCurrency(totalInvestment) }}</strong>
    </div>
  </KalkCard>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { KalkCard, KalkCurrency, KalkDatePicker, KalkAmountInput, HelpIcon } from '@/components';
import { useProjectStore } from '@/stores/projectStore';
import type { YearMonth, TaxClassification, AmountMode } from '@/stores/types';

interface CostItem {
  id: string;
  name: string;
  amount: number;
  mode: AmountMode;
  isDeductible: boolean;
  taxClassification: TaxClassification;
}

const emit = defineEmits<{
  'validation-change': [valid: boolean];
}>();

const { t, n, locale } = useI18n();
const projectStore = useProjectStore();

// Form values
const purchasePrice = ref<number>(300000);
const purchaseDate = ref<YearMonth | undefined>();
const landValue = ref<number>(20);
const landValueMode = ref<AmountMode>('percent');
const costs = ref<CostItem[]>([]);

const currency = computed(() => projectStore.currentProject?.currency || 'EUR');

const totalCosts = computed(() =>
  costs.value.reduce((sum, c) => {
    if (c.mode === 'percent') {
      return sum + ((purchasePrice.value || 0) * (c.amount || 0) / 100);
    }
    return sum + (c.amount || 0);
  }, 0)
);

const totalInvestment = computed(() =>
  (purchasePrice.value || 0) + totalCosts.value
);

// Calculate actual land value percent for storage
const landValuePercent = computed(() => {
  if (landValueMode.value === 'percent') {
    return landValue.value;
  }
  // Convert absolute to percent
  if (totalInvestment.value === 0) return 0;
  return (landValue.value / totalInvestment.value) * 100;
});

function formatCurrency(value: number): string {
  return new Intl.NumberFormat(locale.value, {
    style: 'currency',
    currency: currency.value
  }).format(value);
}

function addCostItem() {
  costs.value.push({
    id: crypto.randomUUID(),
    name: '',
    amount: 0,
    mode: 'percent',
    isDeductible: true,
    taxClassification: 'AcquisitionCost'
  });
}

function removeCostItem(id: string) {
  const index = costs.value.findIndex(c => c.id === id);
  if (index !== -1) {
    costs.value.splice(index, 1);
  }
}

function addDefaultCosts() {
  costs.value = [
    {
      id: crypto.randomUUID(),
      name: t('purchase.costTypes.landTransferTax'),
      amount: 6, // 6% Grunderwerbsteuer
      mode: 'percent' as AmountMode,
      isDeductible: true,
      taxClassification: 'AcquisitionCost'
    },
    {
      id: crypto.randomUUID(),
      name: t('purchase.costTypes.notaryFee'),
      amount: 1.5, // 1.5% Notar
      mode: 'percent' as AmountMode,
      isDeductible: true,
      taxClassification: 'AcquisitionCost'
    },
    {
      id: crypto.randomUUID(),
      name: t('purchase.costTypes.landRegistration'),
      amount: 0.5, // 0.5% Grundbuch
      mode: 'percent' as AmountMode,
      isDeductible: true,
      taxClassification: 'AcquisitionCost'
    },
    {
      id: crypto.randomUUID(),
      name: t('purchase.costTypes.brokerFee'),
      amount: 3.57, // 3.57% Makler
      mode: 'percent' as AmountMode,
      isDeductible: true,
      taxClassification: 'AcquisitionCost'
    }
  ];
}

// Initialize from store
onMounted(() => {
  const purchase = projectStore.currentProject?.purchase;
  if (purchase) {
    purchasePrice.value = purchase.purchasePrice.amount;
    purchaseDate.value = purchase.purchaseDate;
    landValue.value = purchase.landValuePercent;
    landValueMode.value = (purchase as any).landValueMode || 'percent';
    costs.value = purchase.costs.map(c => ({
      id: c.id,
      name: c.name,
      amount: c.amount.amount,
      mode: (c as any).mode || 'fixed' as AmountMode,
      isDeductible: c.isDeductible,
      taxClassification: c.taxClassification
    }));
  }
});

// Validation
const isValid = computed(() => {
  return purchasePrice.value > 0 && purchaseDate.value !== undefined;
});

watch(isValid, (valid) => {
  emit('validation-change', valid);
}, { immediate: true });

// Sync to store
watch(
  [purchasePrice, purchaseDate, landValue, landValueMode, costs],
  () => {
    if (!projectStore.currentProject) return;

    // Store the raw input value (percent or absolute) along with the mode
    const costsForStore = costs.value.map(c => ({
      id: c.id,
      name: c.name,
      amount: { amount: c.amount, currency: currency.value },
      mode: c.mode,
      isDeductible: c.isDeductible,
      taxClassification: c.taxClassification
    }));

    projectStore.updateProject({
      purchase: {
        purchasePrice: { amount: purchasePrice.value, currency: currency.value },
        purchaseDate: purchaseDate.value || projectStore.currentProject.purchase.purchaseDate,
        landValuePercent: landValuePercent.value,
        landValueMode: landValueMode.value,
        costs: costsForStore
      }
    });
  },
  { deep: true }
);
</script>

<style scoped>
.costs-section {
  margin-top: var(--kalk-space-8);
  padding-top: var(--kalk-space-6);
  border-top: 1px solid var(--kalk-gray-200);
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

.btn-accent {
  background: var(--kalk-accent-500);
  color: #ffffff;
}

.btn-accent:hover {
  background: var(--kalk-accent-600);
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
  margin: 0 0 var(--kalk-space-4);
}

.costs-list {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-3);
}

.cost-card {
  background: #ffffff;
  border: 1px solid var(--kalk-gray-200);
  border-radius: var(--kalk-radius-md);
  transition: border-color 0.15s, box-shadow 0.15s;
}

.cost-card:hover {
  border-color: var(--kalk-gray-300);
  box-shadow: var(--kalk-shadow-sm);
}

.cost-card-content {
  padding: var(--kalk-space-4);
}

.cost-name-row {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  margin-bottom: var(--kalk-space-3);
}

.cost-name-input {
  flex: 1;
  padding: var(--kalk-space-2) var(--kalk-space-3);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  color: var(--kalk-gray-900);
  background: transparent;
  border: none;
  border-bottom: 1px solid var(--kalk-gray-200);
  outline: none;
  transition: border-color 0.15s;
}

.cost-name-input:focus {
  border-bottom-color: var(--kalk-accent-500);
}

.cost-name-input::placeholder {
  color: var(--kalk-gray-400);
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

.cost-amount-row {
  /* KalkAmountInput has its own margin-bottom, remove it here */
}

.cost-amount-row :deep(.form-group) {
  margin-bottom: 0;
}

.costs-summary {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--kalk-space-4);
  background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-md);
  margin-top: var(--kalk-space-4);
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-700);
}

.costs-summary strong {
  font-variant-numeric: tabular-nums;
  color: var(--kalk-gray-900);
}

.investment-summary {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--kalk-space-6);
  background: var(--kalk-primary-900);
  color: #ffffff;
  border-radius: var(--kalk-radius-md);
  margin-top: var(--kalk-space-8);
}

.investment-summary span {
  color: rgba(255, 255, 255, 0.9);
}

.total-amount {
  color: #ffffff;
  font-size: var(--kalk-text-xl);
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}
</style>
