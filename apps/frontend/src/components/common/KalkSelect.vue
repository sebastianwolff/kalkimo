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

    <div class="select-wrapper">
      <select
        :value="modelValue"
        :disabled="disabled"
        :class="['form-select', { 'has-error': !!errorMessage }]"
        @change="handleChange"
      >
        <option v-if="placeholder" value="" disabled>{{ placeholder }}</option>
        <option
          v-for="option in options"
          :key="option.value"
          :value="option.value"
        >
          {{ option.label }}
        </option>
      </select>
      <svg class="select-chevron" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd" />
      </svg>
    </div>

    <p v-if="errorMessage" class="form-error">{{ errorMessage }}</p>
    <p v-else-if="hint" class="form-hint">{{ hint }}</p>
  </div>
</template>

<script setup lang="ts">
import { useUiStore } from '@/stores/uiStore';

export interface SelectOption {
  value: string | number;
  label: string;
}

const props = withDefaults(defineProps<{
  label?: string;
  modelValue?: string | number;
  options: SelectOption[];
  placeholder?: string;
  disabled?: boolean;
  required?: boolean;
  errorMessage?: string;
  hint?: string;
  helpKey?: string;
}>(), {
  disabled: false,
  required: false
});

const emit = defineEmits<{
  'update:modelValue': [value: string | number | undefined];
  'change': [value: string | number | undefined];
}>();

const uiStore = useUiStore();

function handleChange(event: Event) {
  const target = event.target as HTMLSelectElement;
  const value = target.value;
  emit('update:modelValue', value);
  emit('change', value);
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

.select-wrapper {
  position: relative;
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

.form-select:disabled {
  background-color: var(--kalk-gray-100);
  color: var(--kalk-gray-400);
  cursor: not-allowed;
}

.form-select.has-error {
  border-color: var(--kalk-error);
}

.form-select.has-error:focus {
  box-shadow: 0 0 0 3px rgba(220, 38, 38, 0.2);
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

.form-hint {
  margin: var(--kalk-space-2) 0 0;
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
}
</style>
