<template>
  <KalkCard :title="t('costs.title')">
    <!-- Maintenance Reserve Section -->
    <div class="reserve-section">
      <h4 class="section-title">{{ t('costs.maintenanceReserve.title') }}</h4>
      <p class="reserve-hint">{{ t('costs.maintenanceReserve.hint') }}</p>
      <div class="reserve-fields">
        <KalkCurrency
          v-model="maintenanceReserveMonthly"
          :label="t('costs.maintenanceReserve.monthlyAmount')"
          :currency="currency"
          help-key="costs.maintenanceReserve"
        />
        <KalkCurrency
          :model-value="maintenanceReserveMonthly * 12"
          :label="t('costs.maintenanceReserve.yearly')"
          :currency="currency"
          disabled
        />
      </div>
    </div>

    <div class="items-section">
      <div class="block-header">
        <h4 class="section-title">{{ t('costs.items') }}</h4>
        <button type="button" class="btn btn-outline btn-sm" @click="addCostItem">
          <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
          </svg>
          {{ t('costs.addItem') }}
        </button>
      </div>

      <div v-if="costItems.length === 0" class="empty-row">
        <span>Keine laufenden Kosten konfiguriert</span>
        <button type="button" class="btn btn-ghost btn-sm" @click="addDefaultCosts">
          Typische Kosten hinzufügen
        </button>
      </div>

      <div v-else class="line-table">
        <div class="line-row line-head">
          <span>{{ t('costs.item.name') }}</span>
          <span class="num">{{ t('costs.item.monthlyAmount') }} <HelpIcon help-key="costs.monthlyAmount" /></span>
          <span>{{ t('costs.item.isTransferable') }} <HelpIcon help-key="costs.isTransferable" /></span>
          <span></span>
        </div>
        <div
          v-for="item in costItems"
          :key="item.id"
          class="line-row"
        >
          <input
            v-model="item.name"
            type="text"
            class="form-input line-name"
            placeholder="Kostenart"
            :aria-label="t('costs.item.name')"
          />
          <div class="line-amount">
            <KalkCurrency
              v-model="item.monthlyAmount"
              :currency="currency"
            />
          </div>
          <label class="checkbox-label" :title="'Auf Mieter umlegbar'">
            <input
              type="checkbox"
              v-model="item.isTransferable"
              class="checkbox-input"
            />
            <span class="checkbox-custom"></span>
            <span class="checkbox-text">{{ t('costs.item.isTransferable') }}</span>
          </label>
          <button
            type="button"
            class="icon-btn"
            @click="removeCostItem(item.id)"
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

    <div class="summary-box statement">
      <div class="data-row">
        <span class="data-label strong">{{ t('costs.totalMonthly') }}</span>
        <span class="data-value">{{ formatCurrency(totalMonthly) }}</span>
      </div>
      <div class="data-row sub">
        <span class="data-label">{{ t('costs.transferable') }}</span>
        <span class="data-value">{{ formatCurrency(transferableTotal) }}</span>
      </div>
      <div class="data-row sub">
        <span class="data-label">{{ t('costs.nonTransferable') }}</span>
        <span class="data-value">{{ formatCurrency(nonTransferableTotal) }}</span>
      </div>
      <div class="data-row">
        <span class="data-label">{{ t('costs.maintenanceReserve.title') }}</span>
        <span class="data-value">{{ formatCurrency(maintenanceReserveMonthly) }}</span>
      </div>
      <div class="data-row total">
        <span class="data-label">{{ t('costs.totalWithReserve') }}</span>
        <span class="data-value">{{ formatCurrency(totalMonthly + maintenanceReserveMonthly) }}</span>
      </div>
    </div>
  </KalkCard>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { KalkCard, KalkCurrency, HelpIcon } from '@/components';
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
const maintenanceReserveMonthly = ref<number>(0);
const maintenanceReserveIncrease = ref<number>(2);

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
    maintenanceReserveMonthly.value = costs.maintenanceReserveMonthly?.amount || 0;
    maintenanceReserveIncrease.value = costs.maintenanceReserveAnnualIncreasePercent ?? 2;
  }
});

const isValid = computed(() => true);

watch(isValid, (valid) => {
  emit('validation-change', valid);
}, { immediate: true });

function syncCostsToStore() {
  if (!projectStore.currentProject) return;

  projectStore.updateProject({
    costs: {
      items: costItems.value.map(c => ({
        id: c.id,
        name: c.name,
        monthlyAmount: { amount: c.monthlyAmount, currency: currency.value },
        isTransferable: c.isTransferable,
        annualIncreasePercent: c.annualIncreasePercent
      })),
      maintenanceReserveMonthly: { amount: maintenanceReserveMonthly.value, currency: currency.value },
      maintenanceReserveAnnualIncreasePercent: maintenanceReserveIncrease.value
    }
  });
}

watch(costItems, syncCostsToStore, { deep: true });
watch([maintenanceReserveMonthly, maintenanceReserveIncrease], syncCostsToStore);
</script>

<style scoped>
.reserve-section .section-title {
  margin: 0;
}

.reserve-hint {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-500);
  margin: var(--kalk-space-1) 0 var(--kalk-space-4);
}

.reserve-fields {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  column-gap: var(--kalk-space-4);
}

.items-section {
  margin-top: var(--kalk-space-1);
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

/* Kostenpositionen als Tabelle */
.line-table {
  border: 1px solid var(--kalk-border);
  border-radius: var(--kalk-radius-md);
  background: var(--kalk-surface);
}

.line-row {
  display: grid;
  grid-template-columns: minmax(0, 1.3fr) minmax(0, 1fr) 112px 32px;
  gap: var(--kalk-space-3);
  align-items: center;
  padding: var(--kalk-space-2) var(--kalk-space-2) var(--kalk-space-2) var(--kalk-space-3);
  border-bottom: 1px solid var(--kalk-gray-100);
}

.line-row:last-child {
  border-bottom: none;
}

.line-head {
  padding-top: 6px;
  padding-bottom: 6px;
  background: var(--kalk-gray-50);
  border-bottom: 1px solid var(--kalk-border);
}

.line-head span {
  display: inline-flex;
  align-items: center;
  gap: 2px;
  font-size: var(--kalk-text-2xs);
  font-weight: 600;
  letter-spacing: var(--kalk-tracking-caps);
  text-transform: uppercase;
  color: var(--kalk-gray-500);
}

.line-head .num {
  justify-content: flex-end;
}

.line-name {
  height: 40px;
  font-size: var(--kalk-text-sm);
}

.line-amount {
  min-width: 0;
}

.line-amount :deep(.form-group) {
  margin-bottom: 0;
}

/* Kompakte Eingabe innerhalb der Tabellenzeile */
.line-amount :deep(.currency-input) {
  min-height: 0;
  height: 38px;
  padding-top: 0;
  padding-bottom: 0;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-2);
  cursor: pointer;
}

.checkbox-input {
  position: absolute;
  opacity: 0;
  width: 0;
  height: 0;
}

.checkbox-custom {
  width: 16px;
  height: 16px;
  border: 1px solid var(--kalk-gray-400);
  border-radius: var(--kalk-radius-sm);
  background: var(--kalk-surface);
  transition: background-color 0.12s, border-color 0.12s;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.checkbox-label:hover .checkbox-custom {
  border-color: var(--kalk-gray-600);
}

.checkbox-input:checked + .checkbox-custom {
  background: var(--kalk-navy-800);
  border-color: var(--kalk-navy-800);
}

.checkbox-input:checked + .checkbox-custom::after {
  content: '';
  width: 4px;
  height: 8px;
  border: solid var(--kalk-surface);
  border-width: 0 1.5px 1.5px 0;
  transform: rotate(45deg);
  margin-bottom: 2px;
}

.checkbox-input:focus-visible + .checkbox-custom {
  box-shadow: 0 0 0 3px var(--kalk-ring-color);
}

.checkbox-text {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-700);
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

@media (min-width: 641px) {
  /* In der Tabellenansicht steht die Beschriftung im Spaltenkopf */
  .checkbox-text {
    position: absolute;
    width: 1px;
    height: 1px;
    overflow: hidden;
    clip: rect(0 0 0 0);
    white-space: nowrap;
  }
}

@media (max-width: 640px) {
  .reserve-fields {
    grid-template-columns: minmax(0, 1fr);
  }

  .line-head {
    display: none;
  }

  .line-row {
    grid-template-columns: minmax(0, 1fr) 32px;
    row-gap: var(--kalk-space-2);
    padding: var(--kalk-space-3);
  }

  .line-amount {
    grid-column: 1;
    grid-row: 2;
  }

  .checkbox-label {
    grid-column: 1 / -1;
    grid-row: 3;
  }
}

.statement {
  margin-top: var(--kalk-space-6);
  background: var(--kalk-surface);
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
</style>
