<template>
  <KalkCard :title="t('capex.title')">
    <!-- Initial prompt overlay when entering with no measures -->
    <div v-if="showInitialPrompt" class="initial-prompt">
      <div class="initial-prompt-icon">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 18v-5.25m0 0a6.01 6.01 0 001.5-.189m-1.5.189a6.01 6.01 0 01-1.5-.189m3.75 7.478a12.06 12.06 0 01-4.5 0m3.75 2.383a14.406 14.406 0 01-3 0M14.25 18v-.192c0-.983.658-1.823 1.508-2.316a7.5 7.5 0 10-7.517 0c.85.493 1.509 1.333 1.509 2.316V18" />
        </svg>
      </div>
      <h4 class="initial-prompt-title">{{ t('capex.initialPrompt.title') }}</h4>
      <p class="initial-prompt-desc">{{ t('capex.initialPrompt.description') }}</p>
      <div class="initial-prompt-actions">
        <button type="button" class="btn btn-accent btn-md" @click="handleShowSuggestions">
          <svg class="btn-icon" viewBox="0 0 20 20" fill="currentColor">
            <path d="M11 3a1 1 0 10-2 0v1a1 1 0 102 0V3zM15.657 5.757a1 1 0 00-1.414-1.414l-.707.707a1 1 0 001.414 1.414l.707-.707zM18 10a1 1 0 01-1 1h-1a1 1 0 110-2h1a1 1 0 011 1zM5.05 6.464A1 1 0 106.464 5.05l-.707-.707a1 1 0 00-1.414 1.414l.707.707zM4 11a1 1 0 100-2H3a1 1 0 000 2h1zM10 18a1 1 0 001-1v-1a1 1 0 10-2 0v1a1 1 0 001 1z" />
            <path fill-rule="evenodd" d="M10 2a8 8 0 100 16 8 8 0 000-16zm0 2a6 6 0 100 12 6 6 0 000-12z" clip-rule="evenodd" />
          </svg>
          {{ t('capex.initialPrompt.showSuggestions') }}
        </button>
        <button type="button" class="btn btn-outline btn-md" @click="showInitialPrompt = false">
          {{ t('capex.initialPrompt.manualOnly') }}
        </button>
      </div>
    </div>

    <template v-else>
    <div class="section-header">
      <h4 class="section-label">{{ t('capex.measures') }}</h4>
      <div class="header-actions">
        <button type="button" class="btn btn-outline btn-sm" @click="runForecast">
          <svg class="btn-icon" viewBox="0 0 20 20" fill="currentColor">
            <path d="M11 3a1 1 0 10-2 0v1a1 1 0 102 0V3zM15.657 5.757a1 1 0 00-1.414-1.414l-.707.707a1 1 0 001.414 1.414l.707-.707zM18 10a1 1 0 01-1 1h-1a1 1 0 110-2h1a1 1 0 011 1zM5.05 6.464A1 1 0 106.464 5.05l-.707-.707a1 1 0 00-1.414 1.414l.707.707zM4 11a1 1 0 100-2H3a1 1 0 000 2h1zM10 18a1 1 0 001-1v-1a1 1 0 10-2 0v1a1 1 0 001 1z" />
            <path fill-rule="evenodd" d="M10 2a8 8 0 100 16 8 8 0 000-16zm0 2a6 6 0 100 12 6 6 0 000-12z" clip-rule="evenodd" />
          </svg>
          {{ t('capex.suggestMeasures') }}
        </button>
        <button type="button" class="btn btn-outline btn-sm" @click="addMeasure">
          <svg class="btn-icon" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
          </svg>
          {{ t('capex.addMeasure') }}
        </button>
      </div>
    </div>

    <!-- Suggestions -->
    <div v-if="suggestions.length > 0" class="suggestions-section">
      <div class="suggestions-header">
        <h5 class="suggestions-title">{{ t('capex.suggestions.title') }}</h5>
        <button type="button" class="btn btn-accent-outline btn-xs" @click="acceptAllSuggestions">
          {{ t('capex.suggestions.acceptAll') }}
        </button>
      </div>

      <div class="suggestions-list">
        <div
          v-for="(suggestion, index) in suggestions"
          :key="`suggestion-${index}`"
          class="suggestion-card"
        >
          <div class="suggestion-content">
            <div class="suggestion-top">
              <span class="suggestion-name">{{ suggestion.name }}</span>
              <span class="priority-badge" :class="`priority-${suggestion.priority.toLowerCase()}`">
                {{ t(`capex.suggestions.priorities.${suggestion.priority}`) }}
              </span>
            </div>
            <div class="suggestion-details">
              <span>{{ suggestion.plannedYear }} &middot; {{ formatCurrency(suggestion.estimatedCost) }}</span>
            </div>
            <div class="suggestion-reasoning">
              {{ suggestion.reasoning }}
            </div>
          </div>
          <div class="suggestion-actions">
            <button
              type="button"
              class="btn btn-accent-outline btn-xs"
              @click="acceptSuggestion(index)"
              :title="t('capex.suggestions.accept')"
            >
              {{ t('capex.suggestions.accept') }}
            </button>
            <button
              type="button"
              class="btn btn-ghost btn-xs"
              @click="dismissSuggestion(index)"
              :title="t('capex.suggestions.dismiss')"
            >
              {{ t('capex.suggestions.dismiss') }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Existing / accepted measures -->
    <div v-if="measures.length === 0 && suggestions.length === 0" class="empty-state">
      <p>Keine Investitionsmaßnahmen geplant</p>
    </div>

    <div v-if="measures.length > 0" class="measures-list">
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

        <!-- Impact / Wirtschaftliche Auswirkung -->
        <div class="impact-section">
          <button
            type="button"
            class="impact-toggle"
            @click="measure.impactExpanded = !measure.impactExpanded"
          >
            <svg
              class="impact-chevron"
              :class="{ 'is-expanded': measure.impactExpanded }"
              viewBox="0 0 20 20"
              fill="currentColor"
            >
              <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
            {{ t('capex.measure.impact') }}
            <span v-if="hasImpactData(measure)" class="impact-indicator"></span>
          </button>

          <div v-if="measure.impactExpanded" class="impact-fields">
            <KalkCurrency
              v-model="measure.impact.costSavingsMonthly"
              :label="t('capex.measure.costSavings')"
              :currency="currency"
              :placeholder="t('capex.measure.costSavingsHint')"
            />
            <KalkCurrency
              v-model="measure.impact.rentIncreaseMonthly"
              :label="t('capex.measure.rentIncrease')"
              :currency="currency"
              :placeholder="t('capex.measure.rentIncreaseHint')"
            />
            <KalkPercent
              v-model="measure.impact.rentIncreasePercent"
              :label="t('capex.measure.rentIncreasePercent')"
            />
            <KalkInput
              v-model="measure.impact.delayMonths"
              type="number"
              :label="t('capex.measure.delayMonths')"
              :min="0"
              :max="36"
              :placeholder="t('capex.measure.delayMonthsHint')"
            />
          </div>
        </div>
      </div>
    </div>

    <div v-if="measures.length > 0" class="capex-summary">
      <span>{{ t('capex.totalCapex') }}:</span>
      <strong>{{ formatCurrency(totalCapex) }}</strong>
    </div>
    </template>
  </KalkCard>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { KalkCard, KalkSelect, KalkCurrency, KalkDatePicker, KalkPercent, KalkInput } from '@/components';
import { useProjectStore } from '@/stores/projectStore';
import type { CapExCategory, TaxClassification, YearMonth } from '@/stores/types';
import { generateForecast, type ForecastedMeasure } from '@/services/renovationForecastService';

interface MeasureImpactLocal {
  costSavingsMonthly: number;
  rentIncreaseMonthly: number;
  rentIncreasePercent: number;
  delayMonths: number;
}

interface MeasureItem {
  id: string;
  name: string;
  category: CapExCategory;
  amount: number;
  scheduledDate?: YearMonth;
  taxClassification: TaxClassification;
  impactExpanded: boolean;
  impact: MeasureImpactLocal;
}

const emit = defineEmits<{
  'validation-change': [valid: boolean];
}>();

const { t, locale } = useI18n();
const projectStore = useProjectStore();

const measures = ref<MeasureItem[]>([]);
const suggestions = ref<ForecastedMeasure[]>([]);
const showInitialPrompt = ref(false);

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

function emptyImpact(): MeasureImpactLocal {
  return { costSavingsMonthly: 0, rentIncreaseMonthly: 0, rentIncreasePercent: 0, delayMonths: 0 };
}

function hasImpactData(measure: MeasureItem): boolean {
  const i = measure.impact;
  return (i.costSavingsMonthly > 0 || i.rentIncreaseMonthly > 0 || i.rentIncreasePercent > 0);
}

function addMeasure() {
  measures.value.push({
    id: crypto.randomUUID(),
    name: '',
    category: 'Other',
    amount: 0,
    taxClassification: 'MaintenanceExpense',
    impactExpanded: false,
    impact: emptyImpact()
  });
}

function removeMeasure(id: string) {
  const index = measures.value.findIndex(m => m.id === id);
  if (index !== -1) {
    measures.value.splice(index, 1);
  }
}

function handleShowSuggestions() {
  showInitialPrompt.value = false;
  runForecast();
}

function runForecast() {
  const project = projectStore.currentProject;
  if (!project) return;

  const forecasted = generateForecast(
    project.property,
    startYear.value,
    endYear.value,
    currency.value
  );

  // Filter out suggestions for categories that already have a measure
  const existingCategories = new Set(measures.value.map(m => m.category));
  suggestions.value = forecasted.filter(f => !existingCategories.has(f.category));
}

function suggestionToImpact(suggestion: ForecastedMeasure): MeasureImpactLocal {
  if (!suggestion.suggestedImpact) return emptyImpact();
  return {
    costSavingsMonthly: suggestion.suggestedImpact.costSavingsMonthly,
    rentIncreaseMonthly: 0,
    rentIncreasePercent: suggestion.suggestedImpact.rentIncreasePercent,
    delayMonths: suggestion.suggestedImpact.delayMonths,
  };
}

function acceptSuggestion(index: number) {
  const suggestion = suggestions.value[index];
  if (!suggestion) return;

  measures.value.push({
    id: crypto.randomUUID(),
    name: suggestion.name,
    category: suggestion.category,
    amount: suggestion.estimatedCost,
    scheduledDate: { year: suggestion.plannedYear, month: suggestion.plannedMonth },
    taxClassification: 'MaintenanceExpense',
    impactExpanded: false,
    impact: suggestionToImpact(suggestion)
  });

  suggestions.value.splice(index, 1);
}

function dismissSuggestion(index: number) {
  suggestions.value.splice(index, 1);
}

function acceptAllSuggestions() {
  for (const suggestion of suggestions.value) {
    measures.value.push({
      id: crypto.randomUUID(),
      name: suggestion.name,
      category: suggestion.category,
      amount: suggestion.estimatedCost,
      scheduledDate: { year: suggestion.plannedYear, month: suggestion.plannedMonth },
      taxClassification: 'MaintenanceExpense',
      impactExpanded: false,
      impact: suggestionToImpact(suggestion)
    });
  }
  suggestions.value = [];
}

onMounted(() => {
  const capex = projectStore.currentProject?.capex;
  if (capex && capex.length > 0) {
    measures.value = capex.map(m => ({
      id: m.id,
      name: m.name,
      category: m.category,
      amount: m.amount.amount,
      scheduledDate: m.scheduledDate,
      taxClassification: m.taxClassification,
      impactExpanded: false,
      impact: {
        costSavingsMonthly: m.impact?.costSavingsMonthly?.amount ?? 0,
        rentIncreaseMonthly: m.impact?.rentIncreaseMonthly?.amount ?? 0,
        rentIncreasePercent: m.impact?.rentIncreasePercent ?? 0,
        delayMonths: m.impact?.delayMonths ?? 0,
      }
    }));
  } else {
    // No existing measures → show the initial prompt
    showInitialPrompt.value = true;
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
      capex: measures.value.map(m => {
        const hasImpact = m.impact.costSavingsMonthly > 0 ||
          m.impact.rentIncreaseMonthly > 0 ||
          m.impact.rentIncreasePercent > 0;

        return {
          id: m.id,
          name: m.name,
          category: m.category,
          amount: { amount: m.amount, currency: currency.value },
          scheduledDate: m.scheduledDate || projectStore.currentProject!.startPeriod,
          taxClassification: m.taxClassification,
          impact: hasImpact ? {
            costSavingsMonthly: m.impact.costSavingsMonthly > 0
              ? { amount: m.impact.costSavingsMonthly, currency: currency.value } : undefined,
            rentIncreaseMonthly: m.impact.rentIncreaseMonthly > 0
              ? { amount: m.impact.rentIncreaseMonthly, currency: currency.value } : undefined,
            rentIncreasePercent: m.impact.rentIncreasePercent > 0
              ? m.impact.rentIncreasePercent : undefined,
            delayMonths: m.impact.delayMonths > 0 ? m.impact.delayMonths : undefined,
          } : undefined,
        };
      })
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

.header-actions {
  display: flex;
  gap: var(--kalk-space-2);
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

.btn-md {
  height: 44px;
  padding: 0 var(--kalk-space-6);
  font-size: var(--kalk-text-sm);
}

.btn-sm {
  height: 36px;
  padding: 0 var(--kalk-space-4);
  font-size: var(--kalk-text-xs);
}

.btn-xs {
  height: 28px;
  padding: 0 var(--kalk-space-3);
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
  background: var(--kalk-accent-500, #3b82f6);
  color: #ffffff;
  border: 1.5px solid var(--kalk-accent-500, #3b82f6);
}

.btn-accent:hover {
  background: var(--kalk-accent-600, #2563eb);
  border-color: var(--kalk-accent-600, #2563eb);
}

.btn-accent-outline {
  background: #ffffff;
  color: var(--kalk-accent-600, #2563eb);
  border: 1.5px solid var(--kalk-accent-400, #60a5fa);
}

.btn-accent-outline:hover {
  background: var(--kalk-accent-50, #eff6ff);
  border-color: var(--kalk-accent-500, #3b82f6);
}

.btn-ghost {
  background: transparent;
  color: var(--kalk-gray-500);
  border: 1.5px solid transparent;
}

.btn-ghost:hover {
  background: var(--kalk-gray-100);
  color: var(--kalk-gray-700);
}

.btn-icon {
  width: 16px;
  height: 16px;
}

/* Initial prompt */
.initial-prompt {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding: var(--kalk-space-10) var(--kalk-space-6);
  background: linear-gradient(135deg, var(--kalk-accent-50, #eff6ff) 0%, #f0f9ff 100%);
  border: 2px solid var(--kalk-accent-200, #bfdbfe);
  border-radius: var(--kalk-radius-lg, 12px);
}

.initial-prompt-icon {
  width: 48px;
  height: 48px;
  color: var(--kalk-accent-500, #3b82f6);
  margin-bottom: var(--kalk-space-4);
}

.initial-prompt-icon svg {
  width: 100%;
  height: 100%;
}

.initial-prompt-title {
  font-size: var(--kalk-text-lg);
  font-weight: 700;
  color: var(--kalk-gray-900);
  margin: 0 0 var(--kalk-space-2) 0;
}

.initial-prompt-desc {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-600);
  max-width: 480px;
  line-height: 1.5;
  margin: 0 0 var(--kalk-space-6) 0;
}

.initial-prompt-actions {
  display: flex;
  gap: var(--kalk-space-3);
}

@media (max-width: 768px) {
  .initial-prompt-actions {
    flex-direction: column;
    width: 100%;
  }

  .initial-prompt-actions .btn {
    width: 100%;
  }
}

/* Suggestions */
.suggestions-section {
  margin-bottom: var(--kalk-space-6);
}

.suggestions-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--kalk-space-3);
}

.suggestions-title {
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-accent-700, #1d4ed8);
  margin: 0;
}

.suggestions-list {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-2);
}

.suggestion-card {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: var(--kalk-space-4);
  padding: var(--kalk-space-3) var(--kalk-space-4);
  background: var(--kalk-accent-50, #eff6ff);
  border: 1px solid var(--kalk-accent-200, #bfdbfe);
  border-radius: var(--kalk-radius-md);
}

.suggestion-content {
  flex: 1;
  min-width: 0;
}

.suggestion-top {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-2);
  margin-bottom: var(--kalk-space-1);
}

.suggestion-name {
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-900);
}

.priority-badge {
  display: inline-block;
  padding: 1px var(--kalk-space-2);
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  border-radius: 9999px;
}

.priority-critical {
  background: #fef2f2;
  color: #991b1b;
}

.priority-high {
  background: #fff7ed;
  color: #9a3412;
}

.priority-medium {
  background: #fefce8;
  color: #854d0e;
}

.priority-low {
  background: #f0fdf4;
  color: #166534;
}

.suggestion-details {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-600);
  font-variant-numeric: tabular-nums;
  margin-bottom: var(--kalk-space-1);
}

.suggestion-reasoning {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
  font-style: italic;
}

.suggestion-actions {
  display: flex;
  gap: var(--kalk-space-1);
  flex-shrink: 0;
}

/* Measures */
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

/* Impact section */
.impact-section {
  margin-top: var(--kalk-space-3);
  border-top: 1px solid var(--kalk-gray-100);
  padding-top: var(--kalk-space-2);
}

.impact-toggle {
  display: inline-flex;
  align-items: center;
  gap: var(--kalk-space-2);
  background: transparent;
  border: none;
  padding: var(--kalk-space-1) var(--kalk-space-2);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-xs);
  font-weight: 600;
  color: var(--kalk-gray-500);
  cursor: pointer;
  border-radius: var(--kalk-radius-sm);
  transition: all 0.15s;
}

.impact-toggle:hover {
  background: var(--kalk-gray-50);
  color: var(--kalk-gray-700);
}

.impact-chevron {
  width: 14px;
  height: 14px;
  transition: transform 0.2s;
}

.impact-chevron.is-expanded {
  transform: rotate(180deg);
}

.impact-indicator {
  display: inline-block;
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--kalk-accent-500, #3b82f6);
}

.impact-fields {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--kalk-space-4);
  margin-top: var(--kalk-space-3);
  padding: var(--kalk-space-3);
  background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-md);
}

@media (max-width: 768px) {
  .impact-fields {
    grid-template-columns: 1fr;
  }

  .measure-fields {
    grid-template-columns: 1fr;
  }

  .section-header {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--kalk-space-2);
  }

  .header-actions {
    width: 100%;
  }

  .header-actions .btn {
    flex: 1;
  }

  .suggestion-card {
    flex-direction: column;
  }

  .suggestion-actions {
    width: 100%;
  }

  .suggestion-actions .btn {
    flex: 1;
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
