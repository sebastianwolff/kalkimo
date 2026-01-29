<template>
  <KalkCard :title="t('project.title')">
    <KalkInput
      v-model="projectName"
      :label="t('project.name')"
      :placeholder="t('project.namePlaceholder')"
      required
      :error-message="errors.name"
    />

    <KalkInput
      v-model="projectDescription"
      :label="t('project.description')"
      :placeholder="t('project.descriptionPlaceholder')"
      type="text"
    />

    <KalkSelect
      v-model="currency"
      :label="t('project.currency')"
      :options="currencyOptions"
      required
    />

    <div class="period-section">
      <h4 class="section-label">{{ t('project.period') }}</h4>

      <!-- Start Date -->
      <div class="start-date-row">
        <KalkDatePicker
          v-model="startPeriod"
          :label="t('project.startDate')"
          :min-year="currentYear"
          :max-year="currentYear + 10"
          required
        />
      </div>

      <!-- End Date Mode Toggle -->
      <div class="end-date-section">
        <div class="mode-toggle">
          <button
            type="button"
            :class="['toggle-btn', { active: endDateMode === 'duration' }]"
            @click="endDateMode = 'duration'"
          >
            Laufzeit in Jahren
          </button>
          <button
            type="button"
            :class="['toggle-btn', { active: endDateMode === 'date' }]"
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
          />
        </div>
      </div>
    </div>
  </KalkCard>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { KalkCard, KalkInput, KalkSelect, KalkDatePicker } from '@/components';
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
</script>

<style scoped>
.period-section {
  margin-top: var(--kalk-space-6);
}

.section-label {
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-900);
  margin-bottom: var(--kalk-space-4);
}

.start-date-row {
  margin-bottom: var(--kalk-space-6);
}

.end-date-section {
  margin-top: var(--kalk-space-4);
}

.mode-toggle {
  display: flex;
  gap: 0;
  margin-bottom: var(--kalk-space-4);
  border: 1px solid var(--kalk-gray-300);
  border-radius: var(--kalk-radius-md);
  overflow: hidden;
}

.toggle-btn {
  flex: 1;
  padding: var(--kalk-space-3) var(--kalk-space-4);
  border: none;
  background: #ffffff;
  color: var(--kalk-gray-600);
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.toggle-btn:first-child {
  border-right: 1px solid var(--kalk-gray-300);
}

.toggle-btn:hover:not(.active) {
  background: var(--kalk-gray-50);
}

.toggle-btn.active {
  background: var(--kalk-accent-500);
  color: #ffffff;
}

.duration-input {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-2);
}

.calculated-end {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-500);
  margin: 0;
  padding-left: var(--kalk-space-1);
}
</style>
