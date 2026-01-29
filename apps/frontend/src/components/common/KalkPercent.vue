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

    <div :class="['percent-input-wrapper', { 'has-error': !!errorMessage, 'is-focused': isFocused }]">
      <input
        v-model="displayValue"
        type="text"
        inputmode="decimal"
        :placeholder="placeholder"
        :disabled="disabled"
        :readonly="readonly"
        class="percent-input"
        @focus="handleFocus"
        @blur="handleBlur"
      />
      <span class="percent-symbol">%</span>
    </div>

    <p v-if="errorMessage" class="form-error">{{ errorMessage }}</p>
    <p v-else-if="hint" class="form-hint">{{ hint }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useUiStore } from '@/stores/uiStore';

const props = withDefaults(defineProps<{
  label?: string;
  modelValue?: number;
  placeholder?: string;
  disabled?: boolean;
  readonly?: boolean;
  required?: boolean;
  errorMessage?: string;
  hint?: string;
  helpKey?: string;
  min?: number;
  max?: number;
  decimals?: number;
}>(), {
  disabled: false,
  readonly: false,
  required: false,
  min: 0,
  max: 100,
  decimals: 2
});

const emit = defineEmits<{
  'update:modelValue': [value: number | undefined];
  'blur': [];
}>();

const { locale } = useI18n();
const uiStore = useUiStore();
const isFocused = ref(false);
const displayValue = ref('');

function formatNumber(value: number | undefined): string {
  if (value === undefined || value === null) return '';

  return new Intl.NumberFormat(locale.value, {
    minimumFractionDigits: props.decimals,
    maximumFractionDigits: props.decimals
  }).format(value);
}

function parseNumber(value: string): number | undefined {
  if (!value) return undefined;

  const normalized = value
    .replace(/\s/g, '')
    .replace(/,/g, '.');

  const parsed = parseFloat(normalized);
  if (isNaN(parsed)) return undefined;

  // Clamp to min/max
  if (props.min !== undefined && parsed < props.min) return props.min;
  if (props.max !== undefined && parsed > props.max) return props.max;

  return parsed;
}

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

.percent-input-wrapper {
  display: flex;
  align-items: center;
  background-color: #ffffff;
  border: 1.5px solid var(--kalk-gray-200);
  border-radius: var(--kalk-radius-md);
  transition: border-color 0.15s, box-shadow 0.15s;
  padding-right: var(--kalk-space-4);
  overflow: hidden;
}

.percent-input-wrapper:hover {
  border-color: var(--kalk-gray-300);
}

.percent-input-wrapper.is-focused {
  border-color: var(--kalk-accent-500);
  box-shadow: 0 0 0 3px var(--kalk-ring-color);
}

.percent-input-wrapper.has-error {
  border-color: var(--kalk-error);
}

.percent-input-wrapper.has-error.is-focused {
  box-shadow: 0 0 0 3px rgba(220, 38, 38, 0.2);
}

.percent-input {
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

.percent-input::placeholder {
  color: var(--kalk-gray-400);
}

.percent-input:disabled {
  background-color: var(--kalk-gray-100);
  color: var(--kalk-gray-400);
  cursor: not-allowed;
}

.percent-symbol {
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
