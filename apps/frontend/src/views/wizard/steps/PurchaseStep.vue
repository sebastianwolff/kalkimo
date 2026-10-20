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
      <div class="block-header">
        <h4 class="section-title">{{ t('purchase.costs') }} <HelpIcon help-key="purchase.costs" /></h4>
        <button type="button" class="btn btn-outline btn-sm" @click="addCostItem">
          <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
          </svg>
          {{ t('purchase.addCost') }}
        </button>
      </div>

      <div v-if="costs.length === 0" class="empty-row">
        <span>{{ t('purchase.noCosts') || 'Keine Kaufnebenkosten hinzugefügt' }}</span>
        <button type="button" class="btn btn-ghost btn-sm" @click="addDefaultCosts">
          Typische Kosten hinzufügen
        </button>
      </div>

      <div v-else class="line-list">
        <div
          v-for="cost in costs"
          :key="cost.id"
          class="line-row"
        >
          <input
            v-model="cost.name"
            type="text"
            class="form-input line-name"
            placeholder="Kostenart"
          />
          <div class="line-amount">
            <KalkAmountInput
              v-model="cost.amount"
              v-model:model-mode="cost.mode"
              :base-value="purchasePrice"
              :currency="currency"
              :show-calculated="true"
              default-mode="percent"
            />
          </div>
          <button
            type="button"
            class="icon-btn"
            @click="removeCostItem(cost.id)"
            :title="t('common.delete')"
            :aria-label="t('common.delete')"
          >
            <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
              <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>
      </div>
    </div>

    <!-- Total Investment -->
    <div class="summary-box statement">
      <div class="data-row">
        <span class="data-label">{{ t('purchase.price') }}</span>
        <span class="data-value">{{ formatCurrency(purchasePrice || 0) }}</span>
      </div>
      <div class="data-row">
        <span class="data-label">{{ t('purchase.totalCosts') }}</span>
        <span class="data-value">{{ formatCurrency(totalCosts) }}</span>
      </div>
      <div class="data-row total">
        <span class="data-label">{{ t('purchase.totalInvestment') }} <HelpIcon help-key="purchase.totalInvestment" /></span>
        <span class="data-value">{{ formatCurrency(totalInvestment) }}</span>
      </div>
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
  display: inline-flex;
  align-items: center;
  gap: 2px;
}

.empty-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: var(--kalk-space-2) var(--kalk-space-4);
  padding: var(--kalk-space-3) var(--kalk-space-4);
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-500);
  background: var(--kalk-gray-50);
  border: 1px solid var(--kalk-border);
  border-radius: var(--kalk-radius-md);
}

.empty-row .btn-ghost {
  color: var(--kalk-navy-700);
  padding: 0 var(--kalk-space-2);
}

/* Positionsliste: eine umrandete Tabelle statt einzelner Karten */
.line-list {
  border: 1px solid var(--kalk-border);
  border-radius: var(--kalk-radius-md);
  background: var(--kalk-surface);
}

.line-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1.25fr) 32px;
  gap: var(--kalk-space-3);
  align-items: start;
  padding: var(--kalk-space-3) var(--kalk-space-3) var(--kalk-space-3) var(--kalk-space-4);
  border-bottom: 1px solid var(--kalk-gray-100);
}

.line-row:last-child {
  border-bottom: none;
}

.line-name {
  height: 40px;
  font-size: var(--kalk-text-sm);
  font-weight: 500;
}

.line-amount {
  min-width: 0;
}

.line-amount :deep(.form-group) {
  margin-bottom: 0;
}

/* Kompakte Eingabe innerhalb der Tabellenzeile */
.line-amount :deep(.amount-input) {
  min-height: 0;
  height: 38px;
  padding-top: 0;
  padding-bottom: 0;
}

.line-amount :deep(.toggle-btn) {
  padding-top: 6px;
  padding-bottom: 6px;
}

.icon-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  margin-top: 4px;
  padding: 0;
  background: transparent;
  border: 1px solid transparent;
  border-radius: var(--kalk-radius-md);
  color: var(--kalk-gray-400);
  cursor: pointer;
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

@media (max-width: 600px) {
  .line-row {
    grid-template-columns: minmax(0, 1fr) 32px;
    padding: var(--kalk-space-3);
  }

  .line-amount {
    grid-column: 1 / -1;
    grid-row: 2;
  }

  .icon-btn {
    margin-top: 4px;
  }
}

.statement {
  margin-top: var(--kalk-space-6);
  background: var(--kalk-surface);
}

.statement .data-row.total .data-value {
  font-size: var(--kalk-text-lg);
  color: var(--kalk-navy-900);
}

.statement .data-label {
  display: inline-flex;
  align-items: center;
  gap: 2px;
}
</style>
