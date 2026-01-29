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

    <div :class="['currency-input-wrapper', { 'has-error': !!errorMessage, 'is-focused': isFocused }]">
      <input
        v-model="displayValue"
        type="text"
        inputmode="decimal"
        :placeholder="placeholder"
        :disabled="disabled"
        :readonly="readonly"
        class="currency-input"
        @focus="handleFocus"
        @blur="handleBlur"
        @input="handleInput"
      />
      <span class="currency-symbol">{{ currencySymbol }}</span>
    </div>

    <p v-if="errorMessage" class="form-error">{{ errorMessage }}</p>
    <p v-else-if="hint" class="form-hint">{{ hint }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useUiStore } from '@/stores/uiStore';

const props = withDefaults(defineProps<{
  label?: string;
  modelValue?: number;
  currency?: string;
  placeholder?: string;
  disabled?: boolean;
  readonly?: boolean;
  required?: boolean;
  errorMessage?: string;
  hint?: string;
  helpKey?: string;
  min?: number;
  max?: number;
}>(), {
  currency: 'EUR',
  disabled: false,
  readonly: false,
  required: false
});

const emit = defineEmits<{
  'update:modelValue': [value: number | undefined];
  'blur': [];
}>();

const { locale } = useI18n();
const uiStore = useUiStore();
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

// Format number for display
function formatNumber(value: number | undefined): string {
  if (value === undefined || value === null) return '';

  return new Intl.NumberFormat(locale.value, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(value);
}

// Parse display string to number
function parseNumber(value: string): number | undefined {
  if (!value) return undefined;

  // Remove thousands separators and normalize decimal separator
  const normalized = value
    .replace(/\s/g, '')
    .replace(/\./g, '')
    .replace(/,/g, '.');

  const parsed = parseFloat(normalized);
  return isNaN(parsed) ? undefined : parsed;
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

function handleFocus() {
  isFocused.value = true;
  // Show raw number when editing
  if (props.modelValue !== undefined) {
    displayValue.value = props.modelValue.toString().replace('.', ',');
  }
}

function handleBlur() {
  isFocused.value = false;
  const parsed = parseNumber(displayValue.value);
  emit('update:modelValue', parsed);
  displayValue.value = formatNumber(parsed);
  emit('blur');
}

function handleInput(event: Event) {
  const target = event.target as HTMLInputElement;
  const value = target.value;

  // Allow only numbers, comma, dot, and minus
  const sanitized = value.replace(/[^0-9,.\-]/g, '');
  if (sanitized !== value) {
    displayValue.value = sanitized;
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

.currency-input-wrapper {
  display: flex;
  align-items: center;
  background-color: #ffffff;
  border: 1.5px solid var(--kalk-gray-200);
  border-radius: var(--kalk-radius-md);
  transition: border-color 0.15s, box-shadow 0.15s;
  padding-right: var(--kalk-space-4);
}

.currency-input-wrapper:hover {
  border-color: var(--kalk-gray-300);
}

.currency-input-wrapper.is-focused {
  border-color: var(--kalk-accent-500);
  box-shadow: 0 0 0 3px var(--kalk-ring-color);
}

.currency-input-wrapper.has-error {
  border-color: var(--kalk-error);
}

.currency-input-wrapper.has-error.is-focused {
  box-shadow: 0 0 0 3px rgba(220, 38, 38, 0.2);
}

.currency-input {
  flex: 1;
  padding: var(--kalk-space-3) var(--kalk-space-2) var(--kalk-space-3) var(--kalk-space-4);
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

.currency-input::placeholder {
  color: var(--kalk-gray-400);
}

.currency-input:disabled {
  background-color: var(--kalk-gray-100);
  color: var(--kalk-gray-400);
  cursor: not-allowed;
}

.currency-symbol {
  color: var(--kalk-gray-500);
  font-size: var(--kalk-text-base);
  font-weight: 500;
  flex-shrink: 0;
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
