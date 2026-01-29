<template>
  <KalkCard :title="t('costs.title')">
    <div class="section-header">
      <h4 class="section-label">{{ t('costs.items') }}</h4>
      <button type="button" class="btn btn-outline btn-sm" @click="addCostItem">
        <svg class="btn-icon" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
        </svg>
        {{ t('costs.addItem') }}
      </button>
    </div>

    <div v-if="costItems.length === 0" class="empty-state">
      <p>Keine laufenden Kosten konfiguriert</p>
      <button type="button" class="btn btn-accent btn-sm" @click="addDefaultCosts">
        Typische Kosten hinzufügen
      </button>
    </div>

    <div v-else class="costs-list">
      <div
        v-for="item in costItems"
        :key="item.id"
        class="cost-card"
      >
        <div class="cost-card-header">
          <input
            v-model="item.name"
            type="text"
            class="cost-name-input"
            placeholder="Kostenart"
          />
          <button
            type="button"
            class="delete-btn"
            @click="removeCostItem(item.id)"
            :title="t('common.delete')"
          >
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>

        <div class="cost-card-body">
          <div class="cost-fields">
            <KalkCurrency
              v-model="item.monthlyAmount"
              :label="t('costs.item.monthlyAmount')"
              :currency="currency"
            />
            <div class="checkbox-field">
              <label class="checkbox-label">
                <input
                  type="checkbox"
                  v-model="item.isTransferable"
                  class="checkbox-input"
                />
                <span class="checkbox-custom"></span>
                <span class="checkbox-text">{{ t('costs.item.isTransferable') }}</span>
              </label>
              <p class="checkbox-hint">Auf Mieter umlegbar</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="costs-summary">
      <div class="summary-row">
        <span>{{ t('costs.totalMonthly') }}:</span>
        <strong>{{ formatCurrency(totalMonthly) }}</strong>
      </div>
      <div class="summary-row">
        <span>{{ t('costs.transferable') }}:</span>
        <span>{{ formatCurrency(transferableTotal) }}</span>
      </div>
      <div class="summary-row">
        <span>{{ t('costs.nonTransferable') }}:</span>
        <span class="highlight">{{ formatCurrency(nonTransferableTotal) }}</span>
      </div>
    </div>
  </KalkCard>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { KalkCard, KalkCurrency } from '@/components';
import { useProjectStore } from '@/stores/projectStore';

interface CostItemData {
  id: string;
  name: string;
  monthlyAmount: number;
  isTransferable: boolean;
  annualIncreasePercent: number;
}

const emit = defineEmits<{
  'validation-change': [valid: boolean];
}>();

const { t, locale } = useI18n();
const projectStore = useProjectStore();

const costItems = ref<CostItemData[]>([]);

const currency = computed(() => projectStore.currentProject?.currency || 'EUR');

const totalMonthly = computed(() =>
  costItems.value.reduce((sum, c) => sum + (c.monthlyAmount || 0), 0)
);

const transferableTotal = computed(() =>
  costItems.value.filter(c => c.isTransferable).reduce((sum, c) => sum + (c.monthlyAmount || 0), 0)
);

const nonTransferableTotal = computed(() =>
  costItems.value.filter(c => !c.isTransferable).reduce((sum, c) => sum + (c.monthlyAmount || 0), 0)
);

function formatCurrency(value: number): string {
  return new Intl.NumberFormat(locale.value, {
    style: 'currency',
    currency: currency.value
  }).format(value);
}

function addCostItem() {
  costItems.value.push({
    id: crypto.randomUUID(),
    name: '',
    monthlyAmount: 0,
    isTransferable: false,
    annualIncreasePercent: 2
  });
}

function removeCostItem(id: string) {
  const index = costItems.value.findIndex(c => c.id === id);
  if (index !== -1) {
    costItems.value.splice(index, 1);
  }
}

function addDefaultCosts() {
  costItems.value = [
    { id: crypto.randomUUID(), name: t('costs.categories.administration'), monthlyAmount: 30, isTransferable: false, annualIncreasePercent: 2 },
    { id: crypto.randomUUID(), name: t('costs.categories.maintenance'), monthlyAmount: 100, isTransferable: false, annualIncreasePercent: 2 },
    { id: crypto.randomUUID(), name: t('costs.categories.insurance'), monthlyAmount: 50, isTransferable: true, annualIncreasePercent: 2 },
    { id: crypto.randomUUID(), name: t('costs.categories.landTax'), monthlyAmount: 40, isTransferable: true, annualIncreasePercent: 1 }
  ];
}

onMounted(() => {
  const costs = projectStore.currentProject?.costs;
  if (costs) {
    costItems.value = costs.items.map(c => ({
      id: c.id,
      name: c.name,
      monthlyAmount: c.monthlyAmount.amount,
      isTransferable: c.isTransferable,
      annualIncreasePercent: c.annualIncreasePercent
    }));
  }
});

const isValid = computed(() => true);

watch(isValid, (valid) => {
  emit('validation-change', valid);
}, { immediate: true });

watch(
  costItems,
  () => {
    if (!projectStore.currentProject) return;

    projectStore.updateProject({
      costs: {
        items: costItems.value.map(c => ({
          id: c.id,
          name: c.name,
          monthlyAmount: { amount: c.monthlyAmount, currency: currency.value },
          isTransferable: c.isTransferable,
          annualIncreasePercent: c.annualIncreasePercent
        }))
      }
    });
  },
  { deep: true }
);
</script>

<style scoped>
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

.cost-card-header {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  padding: var(--kalk-space-3) var(--kalk-space-4);
  border-bottom: 1px solid var(--kalk-gray-100);
}

.cost-name-input {
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

.cost-name-input:focus {
  border-bottom-color: var(--kalk-accent-500);
}

.cost-name-input::placeholder {
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

.cost-card-body {
  padding: var(--kalk-space-4);
}

.cost-fields {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--kalk-space-4);
  align-items: start;
}

@media (max-width: 600px) {
  .cost-fields {
    grid-template-columns: 1fr;
  }
}

.checkbox-field {
  padding-top: var(--kalk-space-6);
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  cursor: pointer;
}

.checkbox-input {
  position: absolute;
  opacity: 0;
  width: 0;
  height: 0;
}

.checkbox-custom {
  width: 20px;
  height: 20px;
  border: 2px solid var(--kalk-gray-300);
  border-radius: var(--kalk-radius-sm);
  background: #ffffff;
  transition: all 0.15s;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.checkbox-input:checked + .checkbox-custom {
  background: var(--kalk-accent-500);
  border-color: var(--kalk-accent-500);
}

.checkbox-input:checked + .checkbox-custom::after {
  content: '';
  width: 6px;
  height: 10px;
  border: solid #ffffff;
  border-width: 0 2px 2px 0;
  transform: rotate(45deg);
  margin-bottom: 2px;
}

.checkbox-input:focus + .checkbox-custom {
  box-shadow: 0 0 0 3px var(--kalk-ring-color);
}

.checkbox-text {
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  color: var(--kalk-gray-700);
}

.checkbox-hint {
  margin: var(--kalk-space-1) 0 0 calc(20px + var(--kalk-space-3));
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
}

.costs-summary {
  margin-top: var(--kalk-space-8);
  padding: var(--kalk-space-6);
  background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-md);
}

.summary-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--kalk-space-1) 0;
}

.summary-row span {
  color: var(--kalk-gray-600);
  font-size: var(--kalk-text-sm);
}

.summary-row strong {
  color: var(--kalk-gray-900);
  font-variant-numeric: tabular-nums;
}

.highlight {
  color: var(--kalk-accent-600);
  font-weight: 600;
  font-variant-numeric: tabular-nums;
}
</style>
