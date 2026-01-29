<template>
  <KalkCard :title="t('capex.title')">
    <div class="section-header">
      <h4 class="section-label">{{ t('capex.measures') }}</h4>
      <button type="button" class="btn btn-outline btn-sm" @click="addMeasure">
        <svg class="btn-icon" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
        </svg>
        {{ t('capex.addMeasure') }}
      </button>
    </div>

    <div v-if="measures.length === 0" class="empty-state">
      <p>Keine Investitionsmaßnahmen geplant</p>
    </div>

    <div v-else class="measures-list">
      <div
        v-for="measure in measures"
        :key="measure.id"
        class="measure-card"
      >
        <div class="measure-header">
          <input
            v-model="measure.name"
            type="text"
            class="measure-name-input"
            placeholder="z.B. Dachsanierung"
          />
          <button
            type="button"
            class="delete-btn"
            @click="removeMeasure(measure.id)"
            :title="t('common.delete')"
          >
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>

        <div class="measure-fields">
          <KalkSelect
            v-model="measure.category"
            :label="t('capex.measure.category')"
            :options="categoryOptions"
          />
          <KalkCurrency
            v-model="measure.amount"
            :label="t('capex.measure.amount')"
            :currency="currency"
          />
          <KalkDatePicker
            v-model="measure.scheduledDate"
            :label="t('capex.measure.scheduledDate')"
            :min-year="startYear"
            :max-year="endYear"
          />
          <KalkSelect
            v-model="measure.taxClassification"
            :label="t('capex.measure.taxClassification')"
            :options="taxClassificationOptions"
            help-key="capex.taxClassification"
          />
        </div>
      </div>
    </div>

    <div v-if="measures.length > 0" class="capex-summary">
      <span>{{ t('capex.totalCapex') }}:</span>
      <strong>{{ formatCurrency(totalCapex) }}</strong>
    </div>
  </KalkCard>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { KalkCard, KalkSelect, KalkCurrency, KalkDatePicker } from '@/components';
import { useProjectStore } from '@/stores/projectStore';
import type { CapExCategory, TaxClassification, YearMonth } from '@/stores/types';

interface MeasureItem {
  id: string;
  name: string;
  category: CapExCategory;
  amount: number;
  scheduledDate?: YearMonth;
  taxClassification: TaxClassification;
}

const emit = defineEmits<{
  'validation-change': [valid: boolean];
}>();

const { t, locale } = useI18n();
const projectStore = useProjectStore();

const measures = ref<MeasureItem[]>([]);

const currency = computed(() => projectStore.currentProject?.currency || 'EUR');
const startYear = computed(() => projectStore.currentProject?.startPeriod.year || 2024);
const endYear = computed(() => projectStore.currentProject?.endPeriod.year || 2034);

const totalCapex = computed(() =>
  measures.value.reduce((sum, m) => sum + (m.amount || 0), 0)
);

const categoryOptions = computed(() => [
  { value: 'Roof', label: t('capex.categories.Roof') },
  { value: 'Facade', label: t('capex.categories.Facade') },
  { value: 'Windows', label: t('capex.categories.Windows') },
  { value: 'Heating', label: t('capex.categories.Heating') },
  { value: 'Electrical', label: t('capex.categories.Electrical') },
  { value: 'Plumbing', label: t('capex.categories.Plumbing') },
  { value: 'Interior', label: t('capex.categories.Interior') },
  { value: 'Exterior', label: t('capex.categories.Exterior') },
  { value: 'Other', label: t('capex.categories.Other') }
]);

const taxClassificationOptions = computed(() => [
  { value: 'MaintenanceExpense', label: t('capex.taxClassifications.MaintenanceExpense') },
  { value: 'AcquisitionCost', label: t('capex.taxClassifications.AcquisitionCost') },
  { value: 'ImprovementCost', label: t('capex.taxClassifications.ImprovementCost') },
  { value: 'NotDeductible', label: t('capex.taxClassifications.NotDeductible') }
]);

function formatCurrency(value: number): string {
  return new Intl.NumberFormat(locale.value, {
    style: 'currency',
    currency: currency.value
  }).format(value);
}

function addMeasure() {
  measures.value.push({
    id: crypto.randomUUID(),
    name: '',
    category: 'Other',
    amount: 0,
    taxClassification: 'MaintenanceExpense'
  });
}

function removeMeasure(id: string) {
  const index = measures.value.findIndex(m => m.id === id);
  if (index !== -1) {
    measures.value.splice(index, 1);
  }
}

onMounted(() => {
  const capex = projectStore.currentProject?.capex;
  if (capex) {
    measures.value = capex.map(m => ({
      id: m.id,
      name: m.name,
      category: m.category,
      amount: m.amount.amount,
      scheduledDate: m.scheduledDate,
      taxClassification: m.taxClassification
    }));
  }
});

const isValid = computed(() => true);

watch(isValid, (valid) => {
  emit('validation-change', valid);
}, { immediate: true });

watch(
  measures,
  () => {
    if (!projectStore.currentProject) return;

    projectStore.updateProject({
      capex: measures.value.map(m => ({
        id: m.id,
        name: m.name,
        category: m.category,
        amount: { amount: m.amount, currency: currency.value },
        scheduledDate: m.scheduledDate || projectStore.currentProject!.startPeriod,
        taxClassification: m.taxClassification
      }))
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

.measures-list {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-3);
}

.measure-card {
  background: #ffffff;
  border: 1px solid var(--kalk-gray-200);
  border-radius: var(--kalk-radius-md);
  padding: var(--kalk-space-4);
  transition: border-color 0.15s, box-shadow 0.15s;
}

.measure-card:hover {
  border-color: var(--kalk-gray-300);
  box-shadow: var(--kalk-shadow-sm);
}

.measure-header {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  margin-bottom: var(--kalk-space-4);
  padding-bottom: var(--kalk-space-3);
  border-bottom: 1px solid var(--kalk-gray-100);
}

.measure-name-input {
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

.measure-name-input:focus {
  border-bottom-color: var(--kalk-accent-500);
}

.measure-name-input::placeholder {
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

.measure-fields {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--kalk-space-4);
}

@media (max-width: 768px) {
  .measure-fields {
    grid-template-columns: 1fr;
  }
}

.capex-summary {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--kalk-space-6);
  background: var(--kalk-primary-900);
  color: #ffffff;
  border-radius: var(--kalk-radius-md);
  margin-top: var(--kalk-space-8);
  font-size: var(--kalk-text-lg);
}

.capex-summary span {
  color: rgba(255, 255, 255, 0.9);
}

.capex-summary strong {
  color: #ffffff;
  font-variant-numeric: tabular-nums;
}
</style>
