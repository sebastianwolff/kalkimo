<template>
  <KalkCard :title="t('capex.title')">
    <!-- Initial prompt when entering with no measures -->
    <div v-if="showInitialPrompt" class="initial-prompt">
      <div class="initial-prompt-icon" aria-hidden="true">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 18v-5.25m0 0a6.01 6.01 0 001.5-.189m-1.5.189a6.01 6.01 0 01-1.5-.189m3.75 7.478a12.06 12.06 0 01-4.5 0m3.75 2.383a14.406 14.406 0 01-3 0M14.25 18v-.192c0-.983.658-1.823 1.508-2.316a7.5 7.5 0 10-7.517 0c.85.493 1.509 1.333 1.509 2.316V18" />
        </svg>
      </div>
      <div class="initial-prompt-body">
        <h4 class="initial-prompt-title">{{ t('capex.initialPrompt.title') }}</h4>
        <p class="initial-prompt-desc">{{ t('capex.initialPrompt.description') }}</p>
        <div class="initial-prompt-actions">
          <button type="button" class="btn btn-primary" @click="handleShowSuggestions">
            {{ t('capex.initialPrompt.showSuggestions') }}
          </button>
          <button type="button" class="btn btn-outline" @click="showInitialPrompt = false">
            {{ t('capex.initialPrompt.manualOnly') }}
          </button>
        </div>
      </div>
    </div>

    <template v-else>
    <div class="step-section-head">
      <h4 class="step-section-title">{{ t('capex.measures') }}</h4>
      <div class="header-actions">
        <button type="button" class="btn btn-outline btn-sm" @click="runForecast">
          <svg class="btn-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd" d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z" clip-rule="evenodd" />
          </svg>
          {{ t('capex.suggestMeasures') }}
        </button>
        <button type="button" class="btn btn-primary btn-sm" @click="addMeasure">
          <svg class="btn-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
          </svg>
          {{ t('capex.addMeasure') }}
        </button>
      </div>
    </div>

    <!-- Suggestions -->
    <div v-if="suggestions.length > 0" class="suggestions-section">
      <div class="suggestions-header">
        <span class="eyebrow">{{ t('capex.suggestions.title') }} ({{ suggestions.length }})</span>
        <button type="button" class="btn btn-outline btn-sm" @click="acceptAllSuggestions">
          {{ t('capex.suggestions.acceptAll') }}
        </button>
      </div>

      <div class="suggestions-list">
        <div
          v-for="(suggestion, index) in suggestions"
          :key="`suggestion-${index}`"
          class="suggestion-row"
        >
          <div class="suggestion-content">
            <div class="suggestion-top">
              <span class="suggestion-name">{{ suggestion.name }}</span>
              <span
                class="badge"
                :class="{
                  'badge-error': suggestion.priority === 'Critical',
                  'badge-warning': suggestion.priority === 'High',
                  'badge-info': suggestion.priority === 'Medium',
                  'badge-neutral': suggestion.priority === 'Low'
                }"
              >
                {{ t(`capex.suggestions.priorities.${suggestion.priority}`) }}
              </span>
            </div>
            <div class="suggestion-reasoning">
              {{ suggestion.reasoning }}
            </div>
          </div>
          <div class="suggestion-figures">
            <span class="suggestion-year">{{ suggestion.plannedYear }}</span>
            <span class="suggestion-cost">{{ formatCurrency(suggestion.estimatedCost) }}</span>
          </div>
          <div class="suggestion-actions">
            <button
              type="button"
              class="btn btn-outline btn-sm"
              @click="acceptSuggestion(index)"
              :title="t('capex.suggestions.accept')"
            >
              {{ t('capex.suggestions.accept') }}
            </button>
            <button
              type="button"
              class="btn btn-ghost btn-sm"
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
    <div v-if="measures.length === 0 && suggestions.length === 0" class="empty-state capex-empty">
      <p class="empty-state-text">{{ t('capex.noMeasures') }}</p>
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
            <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
              <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>

        <div class="measure-body">
          <div class="measure-fields">
            <KalkSelect
              v-model="measure.category"
              :label="t('capex.measure.category')"
              :options="categoryOptions"
              help-key="capex.category"
            />
          </div>

          <!-- One-time investment toggle -->
          <div class="measure-subsection">
            <label class="check-label">
              <input
                type="checkbox"
                v-model="measure.isOneTime"
                class="check-input"
              />
              <span class="check-box"></span>
              <span class="check-text">{{ t('capex.oneTime.toggle') }}</span>
            </label>

            <div v-if="measure.isOneTime" class="sub-fields onetime-fields">
              <KalkCurrency
                v-model="measure.amount"
                :label="t('capex.measure.amount')"
                :currency="currency"
                help-key="capex.amount"
              />
              <KalkDatePicker
                v-model="measure.scheduledDate"
                :label="t('capex.measure.scheduledDate')"
                :min-year="startYear"
                :max-year="endYear"
                help-key="capex.scheduledDate"
                @update:modelValue="applyTaxSuggestion(measure)"
              />
              <KalkSelect
                v-model="measure.taxClassification"
                :label="t('capex.measure.taxClassification')"
                :options="taxClassificationOptions"
                help-key="capex.taxClassification"
              />
              <KalkPercent
                v-model="measure.valueFactorPercent"
                :label="t('capex.measure.valueFactor')"
                help-key="capex.valueFactor"
              />
            </div>

            <div
              v-if="measure.isOneTime && measure.amount > 0 && !isComponentCategory(measure)"
              class="callout info tax-hint"
            >
              <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
              </svg>
              <span>{{ t('capex.valueHint.noComponent') }}</span>
            </div>

            <div
              v-if="measure.isOneTime && measure.scheduledDate"
              class="callout tax-hint"
              :class="getTaxSuggestion(measure).isWarning ? 'warning' : 'info'"
            >
              <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
              </svg>
              <span>{{ getTaxSuggestion(measure).reason }}</span>
            </div>
          </div>

          <!-- Recurring measure toggle -->
          <div class="measure-subsection">
            <label class="check-label">
              <input
                type="checkbox"
                v-model="measure.isRecurring"
                class="check-input"
              />
              <span class="check-box"></span>
              <span class="check-text">{{ t('capex.recurring.toggle') }}</span>
            </label>

            <div v-if="measure.isRecurring" class="sub-fields recurring-fields">
              <KalkPercent
                v-model="measure.recurringIntervalPercent"
                :label="t('capex.recurring.intervalPercent')"
                help-key="capex.recurringInterval"
              />
              <KalkPercent
                v-model="measure.recurringCostPercent"
                :label="t('capex.recurring.costPercent')"
                help-key="capex.recurringCost"
              />
              <KalkPercent
                v-model="measure.recurringCycleExtensionPercent"
                :label="t('capex.recurring.cycleExtension')"
                help-key="capex.cycleExtension"
              />
              <ul class="recurring-hints">
                <li class="recurring-hint">{{ t('capex.recurring.calculatedInterval', { years: getRecurringIntervalYears(measure) }) }}</li>
                <li class="recurring-hint">{{ t('capex.recurring.calculatedCost', { cost: formatCurrency(getRecurringCostPerEvent(measure)) }) }}</li>
                <li v-if="getRecurringOccurrenceYears(measure).length > 0" class="recurring-hint">
                  {{ t('capex.recurring.occurrences', { count: getRecurringOccurrenceYears(measure).length, years: getRecurringOccurrenceYears(measure).join(', ') }) }}
                </li>
                <li v-else class="recurring-hint recurring-hint--warn">
                  {{ t('capex.recurring.noOccurrences') }}
                </li>
              </ul>
            </div>
          </div>

          <!-- Impact / Wirtschaftliche Auswirkung -->
          <div class="measure-subsection impact-section">
            <button
              type="button"
              class="impact-toggle"
              :aria-expanded="measure.impactExpanded"
              @click="measure.impactExpanded = !measure.impactExpanded"
            >
              <svg
                class="impact-chevron"
                :class="{ 'is-expanded': measure.impactExpanded }"
                viewBox="0 0 20 20"
                fill="currentColor"
                aria-hidden="true"
              >
                <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
              </svg>
              {{ t('capex.measure.impact') }}
              <span v-if="hasImpactData(measure)" class="impact-indicator" aria-hidden="true"></span>
            </button>

            <div v-if="measure.impactExpanded" class="sub-fields impact-fields">
              <KalkCurrency
                v-model="measure.impact.costSavingsMonthly"
                :label="t('capex.measure.costSavings')"
                :currency="currency"
                :placeholder="t('capex.measure.costSavingsHint')"
                help-key="capex.costSavings"
              />
              <KalkCurrency
                v-model="measure.impact.rentIncreaseMonthly"
                :label="t('capex.measure.rentIncrease')"
                :currency="currency"
                :placeholder="t('capex.measure.rentIncreaseHint')"
                help-key="capex.rentIncrease"
              />
              <KalkPercent
                v-model="measure.impact.rentIncreasePercent"
                :label="t('capex.measure.rentIncreasePercent')"
                help-key="capex.rentIncreasePercent"
              />
              <KalkInput
                v-model="measure.impact.delayMonths"
                type="number"
                :label="t('capex.measure.delayMonths')"
                :min="0"
                :max="36"
                :placeholder="t('capex.measure.delayMonthsHint')"
                help-key="capex.delayMonths"
              />
            </div>
          </div>
        </div>
      </div>
    </div>

    <div v-if="measures.length > 0" class="capex-summary">
      <span class="capex-summary-label">{{ t('capex.totalCapex') }}</span>
      <strong class="capex-summary-value">{{ formatCurrency(totalCapex) }}</strong>
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
import { generateForecast, DEFAULT_COMPONENT_CYCLES, type ForecastedMeasure } from '@/services/renovationForecastService';

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
  valueFactorPercent: number;
  impactExpanded: boolean;
  impact: MeasureImpactLocal;
  isOneTime: boolean;
  isRecurring: boolean;
  recurringIntervalPercent: number;
  recurringCostPercent: number;
  recurringCycleExtensionPercent: number;
  unitId?: string;
}

/** Anteil der Investitionskosten, der standardmäßig in den Immobilienwert eingeht */
const DEFAULT_VALUE_FACTOR_PERCENT = 70;

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
  measures.value.reduce((sum, m) => {
    let total = m.isOneTime ? (m.amount || 0) : 0;
    if (m.isRecurring) total += getRecurringTotalCost(m);
    return sum + total;
  }, 0)
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
  { value: 'Other', label: t('capex.categories.Other') },
  { value: 'Kitchen', label: t('capex.categories.Kitchen') },
  { value: 'Bathroom', label: t('capex.categories.Bathroom') },
  { value: 'UnitRenovation', label: t('capex.categories.UnitRenovation') },
  { value: 'UnitOther', label: t('capex.categories.UnitOther') }
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

/** Gibt es ein erfasstes Bauteil, dessen Alterung diese Maßnahme ausgleicht? (Abgleich wie im Backend: Kategorie + Einheit) */
function isComponentCategory(measure: MeasureItem): boolean {
  const property = projectStore.currentProject?.property;
  if (!property) return false;
  const components = measure.unitId
    ? property.units.find(u => u.id === measure.unitId)?.components ?? []
    : property.components;
  return components.some(c => c.category === measure.category);
}

function getComponentCycle(category: CapExCategory): number {
  const comp = projectStore.currentProject?.property.components.find(c => c.category === category);
  if (comp) return comp.expectedCycleYears;
  const cycleData = DEFAULT_COMPONENT_CYCLES[category];
  return cycleData ? Math.round((cycleData.minYears + cycleData.maxYears) / 2) : 20;
}

function getRenewalCost(category: CapExCategory): number {
  const property = projectStore.currentProject?.property;
  if (!property) return 0;
  const cycleData = DEFAULT_COMPONENT_CYCLES[category];
  if (!cycleData) return 0;
  let unitCount: number;
  switch (cycleData.areaMode) {
    case 'total': unitCount = property.totalArea; break;
    case 'perUnit': unitCount = Math.ceil(property.livingArea * (cycleData.unitsPerSqm || (1 / 8))); break;
    default: unitCount = property.livingArea; break;
  }
  return Math.round((cycleData.costPerSqmMax * unitCount) / 100) * 100;
}

function getRecurringIntervalYears(measure: MeasureItem): number {
  return Math.round(getComponentCycle(measure.category) * measure.recurringIntervalPercent / 100);
}

function getRecurringCostPerEvent(measure: MeasureItem): number {
  return Math.round(getRenewalCost(measure.category) * measure.recurringCostPercent / 100 / 100) * 100;
}

function getRecurringOccurrenceYears(measure: MeasureItem): number[] {
  const cycle = getComponentCycle(measure.category);
  const intervalYears = Math.round(cycle * measure.recurringIntervalPercent / 100);
  if (intervalYears <= 0) return [];

  const comp = projectStore.currentProject?.property.components.find(c => c.category === measure.category);
  const lastReno = comp?.lastRenovationYear || projectStore.currentProject?.property.constructionYear || startYear.value;

  const years: number[] = [];
  for (let age = intervalYears; lastReno + age <= endYear.value; age += intervalYears) {
    if (lastReno + age >= startYear.value) {
      years.push(lastReno + age);
    }
  }
  return years;
}

function getRecurringTotalCost(measure: MeasureItem): number {
  const costPerEvent = getRecurringCostPerEvent(measure);
  return getRecurringOccurrenceYears(measure).length * costPerEvent;
}

function getTaxSuggestion(measure: MeasureItem): { classification: TaxClassification; reason: string; isWarning: boolean } {
  const project = projectStore.currentProject;
  if (!project?.purchase?.purchaseDate || !measure.scheduledDate) {
    return { classification: 'MaintenanceExpense', reason: t('capex.taxHint.afterThreeYears'), isWarning: false };
  }

  const pd = project.purchase.purchaseDate;
  const md = measure.scheduledDate;
  const purchaseMonths = pd.year * 12 + (pd.month || 1);
  const measureMonths = md.year * 12 + (md.month || 1);
  const monthsSince = measureMonths - purchaseMonths;

  // Before purchase → likely acquisition cost
  if (monthsSince < 0) {
    return {
      classification: 'AcquisitionCost',
      reason: t('capex.taxHint.beforePurchase'),
      isWarning: false,
    };
  }

  // Within 3 years of purchase → §6 Abs. 1 Nr. 1a EStG (15% rule)
  // Bemessungsgrundlage: Anschaffungskosten des Gebäudes inkl. anteiliger Nebenkosten (ohne Grundstück)
  // Ref: BMF-Schreiben 26.01.2026, Haufe: https://www.haufe.de/steuern/steuerwissen-tipps/anschaffungsnahe-herstellungskosten-bei-gebaeuden_170_275760.html
  if (monthsSince <= 36) {
    const landPercent = project.purchase.landValuePercent || 20;
    const purchasePrice = project.purchase.purchasePrice.amount;
    // Anteilige Anschaffungsnebenkosten (Grunderwerbsteuer, Notar, Makler etc.)
    const acquisitionCosts = (project.purchase.costs || [])
      .filter(c => c.taxClassification === 'AcquisitionCost')
      .reduce((sum, c) => {
        const mode = (c as any).mode;
        if (mode === 'percent') return sum + purchasePrice * c.amount.amount / 100;
        return sum + c.amount.amount;
      }, 0);
    const buildingValue = (purchasePrice + acquisitionCosts) * (1 - landPercent / 100);
    const threshold = buildingValue * 0.15;

    // Sum all measure amounts within 3 years (excluding current measure)
    let totalIn3Y = 0;
    for (const m of measures.value) {
      if (m.id === measure.id) continue;
      if (!m.scheduledDate) continue;
      const mMonths = m.scheduledDate.year * 12 + (m.scheduledDate.month || 1);
      const mSince = mMonths - purchaseMonths;
      if (mSince >= 0 && mSince <= 36) {
        totalIn3Y += m.amount || 0;
      }
    }

    const totalWithCurrent = totalIn3Y + (measure.amount || 0);

    if (totalWithCurrent > threshold) {
      return {
        classification: 'AcquisitionCost',
        reason: t('capex.taxHint.rule15Exceeded', { threshold: formatCurrency(threshold), total: formatCurrency(totalWithCurrent) }),
        isWarning: true,
      };
    }

    return {
      classification: 'MaintenanceExpense',
      reason: t('capex.taxHint.within3Years', { threshold: formatCurrency(threshold), remaining: formatCurrency(threshold - totalIn3Y) }),
      isWarning: false,
    };
  }

  // After 3 years
  return { classification: 'MaintenanceExpense', reason: t('capex.taxHint.afterThreeYears'), isWarning: false };
}

function applyTaxSuggestion(measure: MeasureItem) {
  // Only auto-manage between MaintenanceExpense and AcquisitionCost.
  // Don't override manual ImprovementCost or NotDeductible.
  if (measure.taxClassification === 'ImprovementCost' || measure.taxClassification === 'NotDeductible') {
    return;
  }
  const suggestion = getTaxSuggestion(measure);
  measure.taxClassification = suggestion.classification;
}

function addMeasure() {
  measures.value.push({
    id: crypto.randomUUID(),
    name: '',
    category: 'Other',
    amount: 0,
    taxClassification: 'MaintenanceExpense',
    valueFactorPercent: DEFAULT_VALUE_FACTOR_PERCENT,
    impactExpanded: false,
    impact: emptyImpact(),
    isOneTime: true,
    isRecurring: false,
    recurringIntervalPercent: 40,
    recurringCostPercent: 25,
    recurringCycleExtensionPercent: 40,
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

  // Filter out suggestions for (category, unitId) tuples that already have a measure
  const existingKeys = new Set(measures.value.map(m => `${m.category}:${m.unitId || ''}`));
  suggestions.value = forecasted.filter(f => !existingKeys.has(`${f.category}:${f.unitId || ''}`));
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

  const measure: MeasureItem = {
    id: crypto.randomUUID(),
    name: suggestion.name,
    category: suggestion.category,
    amount: suggestion.estimatedCost,
    scheduledDate: { year: suggestion.plannedYear, month: suggestion.plannedMonth },
    taxClassification: 'MaintenanceExpense',
    valueFactorPercent: DEFAULT_VALUE_FACTOR_PERCENT,
    impactExpanded: false,
    impact: suggestionToImpact(suggestion),
    isOneTime: true,
    isRecurring: false,
    recurringIntervalPercent: 40,
    recurringCostPercent: 25,
    recurringCycleExtensionPercent: 40,
    unitId: suggestion.unitId,
  };
  measures.value.push(measure);
  applyTaxSuggestion(measure);

  suggestions.value.splice(index, 1);
}

function dismissSuggestion(index: number) {
  suggestions.value.splice(index, 1);
}

function acceptAllSuggestions() {
  for (const suggestion of suggestions.value) {
    const measure: MeasureItem = {
      id: crypto.randomUUID(),
      name: suggestion.name,
      category: suggestion.category,
      amount: suggestion.estimatedCost,
      scheduledDate: { year: suggestion.plannedYear, month: suggestion.plannedMonth },
      taxClassification: 'MaintenanceExpense',
      valueFactorPercent: DEFAULT_VALUE_FACTOR_PERCENT,
      impactExpanded: false,
      impact: suggestionToImpact(suggestion),
      isOneTime: true,
      isRecurring: false,
      recurringIntervalPercent: 40,
      recurringCostPercent: 25,
      recurringCycleExtensionPercent: 40,
      unitId: suggestion.unitId,
    };
    measures.value.push(measure);
    applyTaxSuggestion(measure);
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
      valueFactorPercent: m.valueFactorPercent ?? DEFAULT_VALUE_FACTOR_PERCENT,
      impactExpanded: false,
      impact: {
        costSavingsMonthly: m.impact?.costSavingsMonthly?.amount ?? 0,
        rentIncreaseMonthly: m.impact?.rentIncreaseMonthly?.amount ?? 0,
        rentIncreasePercent: m.impact?.rentIncreasePercent ?? 0,
        delayMonths: m.impact?.delayMonths ?? 0,
      },
      isOneTime: m.amount.amount > 0 || !m.isRecurring,
      isRecurring: m.isRecurring || false,
      recurringIntervalPercent: m.recurringConfig?.intervalPercent ?? 40,
      recurringCostPercent: m.recurringConfig?.costPercent ?? 25,
      recurringCycleExtensionPercent: m.recurringConfig?.cycleExtensionPercent ?? 40,
      unitId: m.unitId,
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
          amount: { amount: m.isOneTime ? m.amount : 0, currency: currency.value },
          scheduledDate: (m.isOneTime && m.scheduledDate) ? m.scheduledDate : projectStore.currentProject!.startPeriod,
          taxClassification: m.taxClassification,
          valueFactorPercent: m.valueFactorPercent,
          impact: hasImpact ? {
            costSavingsMonthly: m.impact.costSavingsMonthly > 0
              ? { amount: m.impact.costSavingsMonthly, currency: currency.value } : undefined,
            rentIncreaseMonthly: m.impact.rentIncreaseMonthly > 0
              ? { amount: m.impact.rentIncreaseMonthly, currency: currency.value } : undefined,
            rentIncreasePercent: m.impact.rentIncreasePercent > 0
              ? m.impact.rentIncreasePercent : undefined,
            delayMonths: m.impact.delayMonths > 0 ? m.impact.delayMonths : undefined,
          } : undefined,
          isRecurring: m.isRecurring || undefined,
          recurringConfig: m.isRecurring ? {
            intervalPercent: m.recurringIntervalPercent,
            costPercent: m.recurringCostPercent,
            cycleExtensionPercent: m.recurringCycleExtensionPercent,
          } : undefined,
          unitId: m.unitId,
        };
      })
    });
  },
  { deep: true }
);
</script>


<style scoped>
/* ------------------------------------------------------------
 * Section head
 * ------------------------------------------------------------ */
.step-section-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: var(--kalk-space-4);
  margin-bottom: var(--kalk-space-4);
}

.step-section-title {
  font-size: var(--kalk-text-base);
  font-weight: 600;
  color: var(--kalk-gray-900);
  margin: 0;
}

.header-actions {
  display: flex;
  flex-wrap: wrap;
  gap: var(--kalk-space-2);
}

.btn-icon {
  width: 14px;
  height: 14px;
}

/* ------------------------------------------------------------
 * Initial prompt
 * ------------------------------------------------------------ */
.initial-prompt {
  display: flex;
  gap: var(--kalk-space-4);
  align-items: flex-start;
  padding: var(--kalk-space-6);
  background: var(--kalk-gray-50);
  border: 1px solid var(--kalk-border);
  border-left: 3px solid var(--kalk-navy-700);
  border-radius: var(--kalk-radius-md);
}

.initial-prompt-icon {
  width: 28px;
  height: 28px;
  flex-shrink: 0;
  color: var(--kalk-navy-600);
}

.initial-prompt-icon svg {
  width: 100%;
  height: 100%;
}

.initial-prompt-body {
  flex: 1;
  min-width: 0;
}

.initial-prompt-title {
  font-size: var(--kalk-text-base);
  font-weight: 600;
  color: var(--kalk-gray-900);
  margin: 0 0 var(--kalk-space-1) 0;
}

.initial-prompt-desc {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-600);
  max-width: 60ch;
  margin: 0 0 var(--kalk-space-5) 0;
}

.initial-prompt-actions {
  display: flex;
  flex-wrap: wrap;
  gap: var(--kalk-space-2);
}

/* ------------------------------------------------------------
 * Suggestions
 * ------------------------------------------------------------ */
.suggestions-section {
  margin-bottom: var(--kalk-space-6);
}

.suggestions-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: var(--kalk-space-3);
  margin-bottom: var(--kalk-space-2);
}

.suggestions-list {
  border: 1px solid var(--kalk-border);
  border-radius: var(--kalk-radius-lg);
  background: var(--kalk-surface);
  overflow: hidden;
}

.suggestion-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto auto;
  align-items: center;
  gap: var(--kalk-space-5);
  padding: var(--kalk-space-3) var(--kalk-space-4);
  border-bottom: 1px solid var(--kalk-gray-100);
}

.suggestion-row:last-child {
  border-bottom: none;
}

.suggestion-row:hover {
  background: var(--kalk-gray-50);
}

.suggestion-content {
  min-width: 0;
}

.suggestion-top {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: var(--kalk-space-2);
  margin-bottom: 2px;
}

.suggestion-name {
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-900);
}

.suggestion-reasoning {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
}

.suggestion-figures {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.suggestion-year {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
}

.suggestion-cost {
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-900);
}

.suggestion-actions {
  display: flex;
  gap: var(--kalk-space-1);
  flex-shrink: 0;
}

/* ------------------------------------------------------------
 * Measures
 * ------------------------------------------------------------ */
.capex-empty {
  padding: var(--kalk-space-8) var(--kalk-space-6);
}

.capex-empty .empty-state-text {
  margin: 0;
}

.measures-list {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-3);
}

.measure-card {
  background: var(--kalk-surface);
  border: 1px solid var(--kalk-border);
  border-radius: var(--kalk-radius-lg);
  transition: border-color 0.12s;
}

.measure-card:hover,
.measure-card:focus-within {
  border-color: var(--kalk-border-strong);
}

.measure-header {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-2);
  padding: var(--kalk-space-2) var(--kalk-space-3) var(--kalk-space-2) var(--kalk-space-2);
  background: var(--kalk-gray-50);
  border-bottom: 1px solid var(--kalk-border);
  border-radius: var(--kalk-radius-lg) var(--kalk-radius-lg) 0 0;
}

.measure-name-input {
  flex: 1;
  min-width: 0;
  height: 32px;
  padding: 0 var(--kalk-space-2);
  font-family: inherit;
  font-size: var(--kalk-text-base);
  font-weight: 600;
  color: var(--kalk-gray-900);
  background: transparent;
  border: 1px solid transparent;
  border-radius: var(--kalk-radius-md);
  outline: none;
  transition: border-color 0.12s, box-shadow 0.12s, background-color 0.12s;
}

.measure-name-input:hover {
  border-color: var(--kalk-border-strong);
  background: var(--kalk-surface);
}

.measure-name-input:focus {
  background: var(--kalk-surface);
  border-color: var(--kalk-navy-600);
  box-shadow: 0 0 0 3px var(--kalk-ring-color);
}

.measure-name-input::placeholder {
  color: var(--kalk-gray-400);
  font-weight: 400;
}

.delete-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  background: transparent;
  border: 1px solid transparent;
  border-radius: var(--kalk-radius-md);
  color: var(--kalk-gray-500);
  cursor: pointer;
  transition: background-color 0.12s, color 0.12s, border-color 0.12s;
  flex-shrink: 0;
}

.delete-btn:hover {
  background: var(--kalk-negative-bg);
  border-color: var(--kalk-negative-border);
  color: var(--kalk-negative);
}

.delete-btn svg {
  width: 16px;
  height: 16px;
}

.measure-body {
  padding: var(--kalk-space-4) var(--kalk-space-5) var(--kalk-space-2);
}

.measure-fields {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--kalk-space-4);
}

.measure-subsection {
  border-top: 1px solid var(--kalk-gray-100);
  padding: var(--kalk-space-3) 0;
}

/* Checkbox */
.check-label {
  display: inline-flex;
  align-items: center;
  gap: var(--kalk-space-2);
  cursor: pointer;
  user-select: none;
  padding: var(--kalk-space-1) 0;
  position: relative;
}

.check-input {
  position: absolute;
  opacity: 0;
  width: 0;
  height: 0;
}

.check-box {
  display: inline-block;
  width: 16px;
  height: 16px;
  border: 1px solid var(--kalk-gray-400);
  border-radius: var(--kalk-radius-sm);
  background: var(--kalk-surface);
  transition: background-color 0.12s, border-color 0.12s;
  flex-shrink: 0;
  position: relative;
}

.check-label:hover .check-box {
  border-color: var(--kalk-gray-500);
}

.check-input:checked + .check-box {
  background: var(--kalk-navy-700);
  border-color: var(--kalk-navy-700);
}

.check-input:checked + .check-box::after {
  content: '';
  position: absolute;
  left: 4.5px;
  top: 1.5px;
  width: 5px;
  height: 9px;
  border: solid var(--kalk-surface);
  border-width: 0 2px 2px 0;
  transform: rotate(45deg);
}

.check-input:focus-visible + .check-box {
  box-shadow: 0 0 0 3px var(--kalk-ring-color);
  border-color: var(--kalk-navy-600);
}

.check-text {
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  color: var(--kalk-gray-800);
}

/* Sub-field grids: indented under their toggle, no colored boxes */
.sub-fields {
  display: grid;
  gap: 0 var(--kalk-space-4);
  margin-top: var(--kalk-space-3);
  padding-left: var(--kalk-space-6);
}

.onetime-fields {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.recurring-fields {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.impact-fields {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.recurring-hints {
  grid-column: 1 / -1;
  list-style: none;
  margin: 0;
  padding: var(--kalk-space-2) 0 0;
  border-top: 1px dashed var(--kalk-border);
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.recurring-hint {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-600);
  font-variant-numeric: tabular-nums;
}

.recurring-hint--warn {
  color: var(--kalk-caution);
}

/* Tax classification hint */
.tax-hint {
  margin: var(--kalk-space-1) 0 var(--kalk-space-2) var(--kalk-space-6);
  padding: var(--kalk-space-2) var(--kalk-space-3);
  font-size: var(--kalk-text-xs);
}

.tax-hint svg {
  width: 14px;
  height: 14px;
}

/* Impact section */
.impact-toggle {
  display: inline-flex;
  align-items: center;
  gap: var(--kalk-space-2);
  background: transparent;
  border: none;
  padding: var(--kalk-space-1) 0;
  font-family: inherit;
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  color: var(--kalk-gray-800);
  cursor: pointer;
}

.impact-toggle:hover {
  color: var(--kalk-navy-700);
}

.impact-chevron {
  width: 16px;
  height: 16px;
  color: var(--kalk-gray-500);
  transition: transform 0.15s ease;
}

.impact-chevron.is-expanded {
  transform: rotate(90deg);
}

.impact-indicator {
  display: inline-block;
  width: 6px;
  height: 6px;
  border-radius: 1px;
  background: var(--kalk-navy-600);
}

/* ------------------------------------------------------------
 * Total
 * ------------------------------------------------------------ */
.capex-summary {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  gap: var(--kalk-space-4);
  margin-top: var(--kalk-space-6);
  padding: var(--kalk-space-3) var(--kalk-space-1) 0;
  border-top: 2px solid var(--kalk-navy-900);
}

.capex-summary-label {
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-800);
}

.capex-summary-value {
  font-size: var(--kalk-text-xl);
  font-weight: 600;
  color: var(--kalk-navy-900);
  font-variant-numeric: tabular-nums;
}

/* ------------------------------------------------------------
 * Responsive
 * ------------------------------------------------------------ */
@media (max-width: 768px) {
  .step-section-head {
    flex-direction: column;
    align-items: stretch;
    gap: var(--kalk-space-2);
  }

  .header-actions .btn {
    flex: 1 1 auto;
  }

  .initial-prompt {
    flex-direction: column;
    padding: var(--kalk-space-5) var(--kalk-space-4);
  }

  .initial-prompt-actions .btn {
    flex: 1 1 100%;
  }

  .suggestion-row {
    grid-template-columns: minmax(0, 1fr) auto;
    gap: var(--kalk-space-2) var(--kalk-space-3);
  }

  .suggestion-actions {
    grid-column: 1 / -1;
  }

  .suggestion-actions .btn {
    flex: 1;
  }

  .measure-body {
    padding: var(--kalk-space-3) var(--kalk-space-4) var(--kalk-space-1);
  }

  .measure-fields,
  .onetime-fields,
  .recurring-fields,
  .impact-fields {
    grid-template-columns: minmax(0, 1fr);
  }

  .sub-fields,
  .tax-hint {
    padding-left: 0;
    margin-left: 0;
  }
}
</style>
