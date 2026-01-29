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

    <div class="amount-input-container">
      <!-- Mode Toggle -->
      <div class="mode-toggle">
        <button
          type="button"
          :class="['toggle-btn', { active: mode === 'percent' }]"
          @click="setMode('percent')"
        >
          %
        </button>
        <button
          type="button"
          :class="['toggle-btn', { active: mode === 'fixed' }]"
          @click="setMode('fixed')"
        >
          {{ currencySymbol }}
        </button>
      </div>

      <!-- Input Field -->
      <div :class="['input-wrapper', { 'has-error': !!errorMessage, 'is-focused': isFocused }]">
        <input
          v-model="displayValue"
          type="text"
          inputmode="decimal"
          :placeholder="placeholder"
          :disabled="disabled"
          class="amount-input"
          @focus="handleFocus"
          @blur="handleBlur"
        />
      </div>
    </div>

    <!-- Calculated Display -->
    <p v-if="showCalculated && mode === 'percent' && baseValue" class="calculated-value">
      = {{ formatCurrency(calculatedAmount) }}
    </p>
    <p v-else-if="showCalculated && mode === 'fixed' && baseValue" class="calculated-value">
      = {{ formatPercent(calculatedPercent) }} von {{ formatCurrency(baseValue) }}
    </p>

    <p v-if="errorMessage" class="form-error">{{ errorMessage }}</p>
    <p v-else-if="hint" class="form-hint">{{ hint }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useUiStore } from '@/stores/uiStore';

type InputMode = 'percent' | 'fixed';

const props = withDefaults(defineProps<{
  label?: string;
  modelValue?: number;
  modelMode?: InputMode;
  baseValue?: number;
  currency?: string;
  placeholder?: string;
  disabled?: boolean;
  required?: boolean;
  errorMessage?: string;
  hint?: string;
  helpKey?: string;
  showCalculated?: boolean;
  defaultMode?: InputMode;
}>(), {
  currency: 'EUR',
  disabled: false,
  required: false,
  showCalculated: true,
  defaultMode: 'percent'
});

const emit = defineEmits<{
  'update:modelValue': [value: number | undefined];
  'update:modelMode': [mode: InputMode];
  'change': [value: number | undefined, mode: InputMode];
}>();

const { locale } = useI18n();
const uiStore = useUiStore();

const mode = ref<InputMode>(props.modelMode || props.defaultMode);
const isFocused = ref(false);
const displayValue = ref('');

const currencySymbol = computed(() => {
  const symbols: Record<string, string> = {
    EUR: '\u20AC',
    USD: '$',
    GBP: '\u00A3',
    CHF: 'CHF'
  };
  return symbols[props.currency] || props.currency;
});

// Calculate amount from percent or vice versa
const calculatedAmount = computed(() => {
  if (!props.baseValue || props.modelValue === undefined) return 0;
  if (mode.value === 'percent') {
    return props.baseValue * (props.modelValue / 100);
  }
  return props.modelValue;
});

const calculatedPercent = computed(() => {
  if (!props.baseValue || props.modelValue === undefined || props.baseValue === 0) return 0;
  if (mode.value === 'fixed') {
    return (props.modelValue / props.baseValue) * 100;
  }
  return props.modelValue;
});

function formatCurrency(value: number): string {
  return new Intl.NumberFormat(locale.value, {
    style: 'currency',
    currency: props.currency
  }).format(value);
}

function formatPercent(value: number): string {
  return new Intl.NumberFormat(locale.value, {
    style: 'percent',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(value / 100);
}

function formatNumber(value: number | undefined): string {
  if (value === undefined || value === null) return '';
  return new Intl.NumberFormat(locale.value, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(value);
}

function parseNumber(value: string): number | undefined {
  if (!value) return undefined;
  const normalized = value
    .replace(/\s/g, '')
    .replace(/\./g, '')
    .replace(/,/g, '.');
  const parsed = parseFloat(normalized);
  return isNaN(parsed) ? undefined : parsed;
}

function setMode(newMode: InputMode) {
  if (newMode === mode.value) return;

  // Convert value when switching modes
  if (props.baseValue && props.modelValue !== undefined) {
    let newValue: number | undefined;

    if (newMode === 'fixed' && mode.value === 'percent') {
      // Convert percent to fixed
      newValue = props.baseValue * (props.modelValue / 100);
    } else if (newMode === 'percent' && mode.value === 'fixed') {
      // Convert fixed to percent
      newValue = (props.modelValue / props.baseValue) * 100;
    }

    if (newValue !== undefined) {
      newValue = Math.round(newValue * 100) / 100;
      emit('update:modelValue', newValue);
      displayValue.value = formatNumber(newValue);
    }
  }

  mode.value = newMode;
  emit('update:modelMode', newMode);
}

// Initialize display value
watch(
  () => props.modelValue,
  (newValue) => {
    if (!isFocused.value) {
      displayValue.value = formatNumber(newValue);
    }
  },
  { immediate: true }
);

watch(
  () => props.modelMode,
  (newMode) => {
    if (newMode && newMode !== mode.value) {
      mode.value = newMode;
    }
  }
);

function handleFocus() {
  isFocused.value = true;
  if (props.modelValue !== undefined) {
    displayValue.value = props.modelValue.toString().replace('.', ',');
  }
}

function handleBlur() {
  isFocused.value = false;
  const parsed = parseNumber(displayValue.value);
  emit('update:modelValue', parsed);
  emit('change', parsed, mode.value);
  displayValue.value = formatNumber(parsed);
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

.amount-input-container {
  display: flex;
  gap: var(--kalk-space-2);
  min-width: 0;
  max-width: 100%;
}

.mode-toggle {
  display: flex;
  background: var(--kalk-gray-100);
  border-radius: var(--kalk-radius-md);
  padding: 2px;
  flex-shrink: 0;
}

.toggle-btn {
  padding: var(--kalk-space-2) var(--kalk-space-3);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-500);
  background: transparent;
  border: none;
  border-radius: var(--kalk-radius-sm);
  cursor: pointer;
  transition: all 0.15s;
  min-width: 40px;
}

.toggle-btn:hover:not(.active) {
  color: var(--kalk-gray-700);
}

.toggle-btn.active {
  background: #ffffff;
  color: var(--kalk-gray-900);
  box-shadow: var(--kalk-shadow-sm);
}

.input-wrapper {
  flex: 1;
  min-width: 0;
  display: flex;
  align-items: center;
  background-color: #ffffff;
  border: 1.5px solid var(--kalk-gray-200);
  border-radius: var(--kalk-radius-md);
  transition: border-color 0.15s, box-shadow 0.15s;
  overflow: hidden;
}

.input-wrapper:hover {
  border-color: var(--kalk-gray-300);
}

.input-wrapper.is-focused {
  border-color: var(--kalk-accent-500);
  box-shadow: 0 0 0 3px var(--kalk-ring-color);
}

.input-wrapper.has-error {
  border-color: var(--kalk-error);
}

.amount-input {
  flex: 1;
  padding: var(--kalk-space-3) var(--kalk-space-4) var(--kalk-space-3) var(--kalk-space-3);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-base);
  font-variant-numeric: tabular-nums;
  color: var(--kalk-gray-900);
  background: transparent;
  border: none;
  outline: none;
  text-align: right;
  min-height: 48px;
}

.amount-input::placeholder {
  color: var(--kalk-gray-400);
}

.calculated-value {
  margin: var(--kalk-space-1) 0 0;
  font-size: var(--kalk-text-xs);
  color: var(--kalk-accent-600);
  text-align: right;
}

.form-error {
  margin: var(--kalk-space-2) 0 0;
  font-size: var(--kalk-text-xs);
  color: var(--kalk-error);
}

.form-hint {
  margin: var(--kalk-space-2) 0 0;
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
}
</style>
