<template>
  <div class="form-group">
    <label v-if="label" class="form-label">
      {{ label }}
      <span v-if="required" class="required-mark">*</span>
      <button
        v-if="helpKey"
        type="button"
        class="help-button"
        @click.stop="showHelp"
      >
        <svg class="help-icon" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-8-3a1 1 0 00-.867.5 1 1 0 11-1.731-1A3 3 0 0113 8a3.001 3.001 0 01-2 2.83V11a1 1 0 11-2 0v-1a1 1 0 011-1 1 1 0 100-2zm0 8a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
        </svg>
      </button>
    </label>

    <div class="date-inputs">
      <div class="select-wrapper month-select">
        <select
          v-model="selectedMonth"
          :class="['form-select', { 'has-error': !!errorMessage }]"
          @change="handleChange"
        >
          <option value="" disabled>{{ t('common.month') }}</option>
          <option
            v-for="month in months"
            :key="month.value"
            :value="month.value"
          >
            {{ month.label }}
          </option>
        </select>
        <svg class="select-chevron" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd" />
        </svg>
      </div>

      <div class="select-wrapper year-select">
        <select
          v-model="selectedYear"
          :class="['form-select', { 'has-error': !!errorMessage }]"
          @change="handleChange"
        >
          <option value="" disabled>{{ t('common.year') }}</option>
          <option
            v-for="year in years"
            :key="year"
            :value="year"
          >
            {{ year }}
          </option>
        </select>
        <svg class="select-chevron" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd" />
        </svg>
      </div>
    </div>

    <p v-if="errorMessage" class="form-error">{{ errorMessage }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useUiStore } from '@/stores/uiStore';
import type { YearMonth } from '@/stores/types';

const props = withDefaults(defineProps<{
  label?: string;
  modelValue?: YearMonth;
  minYear?: number;
  maxYear?: number;
  required?: boolean;
  errorMessage?: string;
  helpKey?: string;
}>(), {
  minYear: 2000,
  maxYear: 2050,
  required: false
});

const uiStore = useUiStore();

const emit = defineEmits<{
  'update:modelValue': [value: YearMonth | undefined];
}>();

const { t, locale } = useI18n();

const selectedMonth = ref<number | string>(props.modelValue?.month || '');
const selectedYear = ref<number | string>(props.modelValue?.year || '');

const months = computed(() => {
  const formatter = new Intl.DateTimeFormat(locale.value, { month: 'long' });
  return Array.from({ length: 12 }, (_, i) => ({
    value: i + 1,
    label: formatter.format(new Date(2000, i, 1))
  }));
});

const years = computed(() => {
  const result = [];
  for (let y = props.minYear; y <= props.maxYear; y++) {
    result.push(y);
  }
  return result;
});

watch(
  () => props.modelValue,
  (newValue) => {
    selectedMonth.value = newValue?.month || '';
    selectedYear.value = newValue?.year || '';
  }
);

function handleChange() {
  const month = typeof selectedMonth.value === 'number' ? selectedMonth.value : parseInt(selectedMonth.value as string);
  const year = typeof selectedYear.value === 'number' ? selectedYear.value : parseInt(selectedYear.value as string);

  if (month && year && !isNaN(month) && !isNaN(year)) {
    emit('update:modelValue', {
      year: year,
      month: month
    });
  } else {
    emit('update:modelValue', undefined);
  }
}

function showHelp() {
  if (props.helpKey) {
    uiStore.openHelp(props.helpKey);
  }
}
</script>

<style scoped>
.form-group {
  margin-bottom: var(--kalk-space-5);
}

.form-label {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-1);
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  color: var(--kalk-gray-700);
  margin-bottom: var(--kalk-space-2);
}

.required-mark {
  color: var(--kalk-error);
}

.help-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  background: none;
  border: none;
  cursor: pointer;
  color: var(--kalk-gray-400);
  transition: color 0.15s;
}

.help-button:hover {
  color: var(--kalk-accent-500);
}

.help-icon {
  width: 16px;
  height: 16px;
}

.date-inputs {
  display: flex;
  gap: var(--kalk-space-3);
}

.select-wrapper {
  position: relative;
}

.month-select {
  flex: 2;
}

.year-select {
  flex: 1;
}

.form-select {
  display: block;
  width: 100%;
  padding: var(--kalk-space-3) var(--kalk-space-10) var(--kalk-space-3) var(--kalk-space-4);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-base);
  color: var(--kalk-gray-900);
  background-color: #ffffff;
  border: 1.5px solid var(--kalk-gray-200);
  border-radius: var(--kalk-radius-md);
  appearance: none;
  cursor: pointer;
  min-height: 48px;
  transition: border-color 0.15s, box-shadow 0.15s;
}

.form-select:hover {
  border-color: var(--kalk-gray-300);
}

.form-select:focus {
  outline: none;
  border-color: var(--kalk-accent-500);
  box-shadow: 0 0 0 3px var(--kalk-ring-color);
}

.form-select.has-error {
  border-color: var(--kalk-error);
}

.select-chevron {
  position: absolute;
  right: var(--kalk-space-4);
  top: 50%;
  transform: translateY(-50%);
  width: 20px;
  height: 20px;
  color: var(--kalk-gray-400);
  pointer-events: none;
}

.form-error {
  margin: var(--kalk-space-2) 0 0;
  font-size: var(--kalk-text-xs);
  color: var(--kalk-error);
}
</style>
