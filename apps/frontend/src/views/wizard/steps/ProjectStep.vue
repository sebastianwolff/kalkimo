<template>
  <KalkCard :title="t('project.title')">
    <KalkInput
      v-model="projectName"
      :label="t('project.name')"
      :placeholder="t('project.namePlaceholder')"
      required
      :error-message="errors.name"
      help-key="project.name"
    />

    <KalkInput
      v-model="projectDescription"
      :label="t('project.description')"
      :placeholder="t('project.descriptionPlaceholder')"
      type="text"
      help-key="project.description"
    />

    <KalkSelect
      v-model="currency"
      :label="t('project.currency')"
      :options="currencyOptions"
      required
      help-key="project.currency"
    />

    <div class="period-section">
      <h4 class="section-title period-title">{{ t('project.period') }}</h4>

      <!-- Start Date -->
      <div class="start-date-row">
        <KalkDatePicker
          v-model="startPeriod"
          :label="t('project.startDate')"
          :min-year="currentYear"
          :max-year="currentYear + 10"
          required
          help-key="project.startDate"
        />
      </div>

      <!-- End Date Mode Toggle -->
      <div class="end-date-section">
        <div class="mode-toggle" role="group">
          <button
            type="button"
            :class="['toggle-btn', { active: endDateMode === 'duration' }]"
            :aria-pressed="endDateMode === 'duration'"
            @click="endDateMode = 'duration'"
          >
            Laufzeit in Jahren
          </button>
          <button
            type="button"
            :class="['toggle-btn', { active: endDateMode === 'date' }]"
            :aria-pressed="endDateMode === 'date'"
            @click="endDateMode = 'date'"
          >
            Enddatum
          </button>
        </div>

        <!-- Duration Input -->
        <div v-if="endDateMode === 'duration'" class="duration-input">
          <KalkInput
            v-model.number="durationYears"
            label="Analysezeitraum"
            type="number"
            :min="1"
            :max="50"
            suffix="Jahre"
            required
            help-key="project.duration"
          />
          <p class="calculated-end">
            Enddatum: {{ formatYearMonth(calculatedEndPeriod) }}
          </p>
        </div>

        <!-- Specific Date Input -->
        <div v-else class="date-input">
          <KalkDatePicker
            v-model="endPeriod"
            :label="t('project.endDate')"
            :min-year="startPeriod?.year || currentYear"
            :max-year="currentYear + 50"
            required
            help-key="project.endDate"
          />
        </div>
      </div>
    </div>

    <div class="period-section">
      <h4 class="section-title period-title">{{ t('project.valuation.title') }}</h4>

      <div class="valuation-grid">
        <KalkPercent
          v-model="marketGrowthRate"
          :label="t('project.valuation.marketGrowth')"
          :hint="t('project.valuation.marketGrowthHint')"
          :min="-10"
          :max="15"
          :decimals="1"
        />
        <KalkPercent
          v-model="saleCostsPercent"
          :label="t('project.valuation.saleCosts')"
          :hint="t('project.valuation.saleCostsHint')"
          :min="0"
          :max="15"
          :decimals="1"
        />
        <KalkPercent
          v-model="discountRate"
          :label="t('project.valuation.discountRate')"
          :hint="t('project.valuation.discountRateHint')"
          :min="0"
          :max="20"
          :decimals="1"
        />
      </div>

      <label class="checkbox-row">
        <input v-model="hasPlannedSale" type="checkbox" />
        <span>{{ t('project.valuation.plannedSale') }}</span>
      </label>

      <KalkDatePicker
        v-if="hasPlannedSale"
        v-model="plannedSaleDate"
        :label="t('project.valuation.plannedSaleDate')"
        :min-year="startPeriod.year"
        :max-year="actualEndPeriod.year"
      />
      <p v-else class="calculated-end">{{ t('project.valuation.exitHint') }}</p>
    </div>
  </KalkCard>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { KalkCard, KalkInput, KalkSelect, KalkDatePicker, KalkPercent } from '@/components';
import { useProjectStore } from '@/stores/projectStore';
import type { YearMonth } from '@/stores/types';

const emit = defineEmits<{
  'validation-change': [valid: boolean];
}>();

const { t } = useI18n();
const projectStore = useProjectStore();

// Current date helpers
const now = new Date();
const currentYear = now.getFullYear();
const currentMonth = now.getMonth() + 1;

// Get next month
function getNextMonth(): YearMonth {
  if (currentMonth === 12) {
    return { year: currentYear + 1, month: 1 };
  }
  return { year: currentYear, month: currentMonth + 1 };
}

// Form values
const projectName = ref('');
const projectDescription = ref('');
const currency = ref('EUR');
const startPeriod = ref<YearMonth>(getNextMonth());
const endPeriod = ref<YearMonth | undefined>();
const endDateMode = ref<'duration' | 'date'>('duration');
const durationYears = ref(10);

// Wertentwicklung & Verkauf (Defaults entsprechen den Backend-Defaults)
const marketGrowthRate = ref<number | undefined>(1.5);
const saleCostsPercent = ref<number | undefined>(3);
const discountRate = ref<number | undefined>(5);
const hasPlannedSale = ref(false);
const plannedSaleDate = ref<YearMonth | undefined>();

// Validation errors
const errors = ref<{ name?: string }>({});

const currencyOptions = [
  { value: 'EUR', label: 'Euro (EUR)' },
  { value: 'CHF', label: 'Schweizer Franken (CHF)' },
  { value: 'USD', label: 'US Dollar (USD)' },
  { value: 'GBP', label: 'Britisches Pfund (GBP)' }
];

// Calculate end period from duration
const calculatedEndPeriod = computed<YearMonth>(() => {
  const start = startPeriod.value;
  const years = durationYears.value || 10;

  // End date is start + years, same month
  let endYear = start.year + years;
  let endMonth = start.month;

  // Subtract one month to get the end of the period (e.g., 10 years means month 120, not 121)
  if (endMonth === 1) {
    endYear -= 1;
    endMonth = 12;
  } else {
    endMonth -= 1;
  }

  return { year: endYear, month: endMonth };
});

// Get the actual end period (either calculated or manually set)
const actualEndPeriod = computed<YearMonth>(() => {
  if (endDateMode.value === 'duration') {
    return calculatedEndPeriod.value;
  }
  return endPeriod.value || calculatedEndPeriod.value;
});

// Format YearMonth for display
function formatYearMonth(ym: YearMonth): string {
  const monthNames = ['Jan', 'Feb', 'Mär', 'Apr', 'Mai', 'Jun', 'Jul', 'Aug', 'Sep', 'Okt', 'Nov', 'Dez'];
  return `${monthNames[ym.month - 1]} ${ym.year}`;
}

// Initialize from project store
onMounted(() => {
  if (projectStore.currentProject) {
    projectName.value = projectStore.currentProject.name;
    projectDescription.value = projectStore.currentProject.description || '';
    currency.value = projectStore.currentProject.currency;
    startPeriod.value = projectStore.currentProject.startPeriod;

    // Determine if existing project used duration or specific date
    const existingEnd = projectStore.currentProject.endPeriod;
    const start = projectStore.currentProject.startPeriod;

    // Calculate what the duration would be
    const yearsDiff = existingEnd.year - start.year;

    // Check if end date matches a clean year boundary
    if (existingEnd.month === (start.month === 1 ? 12 : start.month - 1)) {
      // Looks like it was set by duration
      endDateMode.value = 'duration';
      durationYears.value = yearsDiff + (existingEnd.month >= start.month ? 0 : 0);
    } else {
      // Was set as specific date
      endDateMode.value = 'date';
      endPeriod.value = existingEnd;
    }

    const valuation = projectStore.currentProject.valuation;
    if (valuation) {
      marketGrowthRate.value = valuation.marketGrowthRatePercent ?? 1.5;
      saleCostsPercent.value = valuation.saleCostsPercent ?? 3;
      discountRate.value = valuation.discountRatePercent ?? 5;
      hasPlannedSale.value = !!valuation.plannedSaleDate;
      plannedSaleDate.value = valuation.plannedSaleDate;
    }
  }
});

// Validation
const isValid = computed(() => {
  const nameValid = projectName.value.trim().length >= 3;
  const periodValid = !!startPeriod.value && !!actualEndPeriod.value;
  const durationValid = endDateMode.value === 'date' || (durationYears.value >= 1 && durationYears.value <= 50);

  errors.value.name = nameValid ? undefined : t('errors.minLength', { min: 3 });

  return nameValid && periodValid && durationValid;
});

// Emit validation state
watch(isValid, (valid) => {
  emit('validation-change', valid);
}, { immediate: true });

// Sync changes to store
watch([projectName, projectDescription, currency, startPeriod, actualEndPeriod], () => {
  if (projectStore.currentProject) {
    projectStore.updateProject({
      name: projectName.value,
      description: projectDescription.value || undefined,
      currency: currency.value,
      startPeriod: startPeriod.value,
      endPeriod: actualEndPeriod.value
    });
  }
}, { deep: true });

// Vorbelegung des Verkaufsdatums beim Aktivieren: Ende des Analysezeitraums
watch(hasPlannedSale, (planned) => {
  if (planned && !plannedSaleDate.value) {
    plannedSaleDate.value = { ...actualEndPeriod.value };
  }
});

watch([marketGrowthRate, saleCostsPercent, discountRate, hasPlannedSale, plannedSaleDate], () => {
  if (projectStore.currentProject) {
    projectStore.updateProject({
      valuation: {
        marketGrowthRatePercent: marketGrowthRate.value,
        saleCostsPercent: saleCostsPercent.value,
        discountRatePercent: discountRate.value,
        plannedSaleDate: hasPlannedSale.value ? plannedSaleDate.value : undefined
      }
    });
  }
}, { deep: true });
</script>

<style scoped>
.period-section {
  margin-top: var(--kalk-space-6);
  padding-top: var(--kalk-space-5);
  border-top: 1px solid var(--kalk-border);
}

.period-title {
  margin: 0 0 var(--kalk-space-4);
}

.start-date-row {
  margin-bottom: var(--kalk-space-2);
}

.end-date-section {
  margin-top: 0;
}

.mode-toggle {
  display: inline-flex;
  width: 100%;
  max-width: 360px;
  margin-bottom: var(--kalk-space-4);
  border: 1px solid var(--kalk-border-strong);
  border-radius: var(--kalk-radius-md);
  overflow: hidden;
  background: var(--kalk-surface);
}

.toggle-btn {
  flex: 1;
  height: 34px;
  padding: 0 var(--kalk-space-3);
  border: none;
  background: var(--kalk-surface);
  color: var(--kalk-gray-600);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  cursor: pointer;
  white-space: nowrap;
  transition: background-color 0.12s, color 0.12s;
}

.toggle-btn + .toggle-btn {
  border-left: 1px solid var(--kalk-border-strong);
}

.toggle-btn:hover:not(.active) {
  background: var(--kalk-gray-50);
  color: var(--kalk-gray-900);
}

.toggle-btn.active {
  background: var(--kalk-navy-800);
  color: var(--kalk-surface);
}

.toggle-btn:focus-visible {
  outline: 2px solid var(--kalk-navy-500);
  outline-offset: -2px;
}

.duration-input {
  display: flex;
  flex-direction: column;
}

.duration-input :deep(.form-group) {
  margin-bottom: var(--kalk-space-2);
}

.calculated-end {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-600);
  font-variant-numeric: tabular-nums;
  margin: 0;
}

.valuation-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  column-gap: var(--kalk-space-4);
}

@media (max-width: 600px) {
  .valuation-grid {
    grid-template-columns: minmax(0, 1fr);
  }
}

.checkbox-row {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-2);
  margin-bottom: var(--kalk-space-3);
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-700);
  cursor: pointer;
}

.checkbox-row input {
  width: 16px;
  height: 16px;
  accent-color: var(--kalk-navy-800);
}
</style>
