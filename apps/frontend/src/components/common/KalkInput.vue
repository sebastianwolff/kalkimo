<template>
  <div class="form-group">
    <!-- Label -->
    <label v-if="label" class="form-label">
      {{ label }}
      <span v-if="required" class="required">*</span>
      <button
        v-if="helpKey"
        type="button"
        class="help-btn"
        @click.stop="showHelp"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <circle cx="12" cy="12" r="10"/>
          <path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"/>
          <line x1="12" y1="17" x2="12.01" y2="17"/>
        </svg>
      </button>
    </label>

    <!-- Input wrapper -->
    <div :class="['input-wrapper', { 'has-suffix': suffix }]">
      <input
        ref="inputRef"
        :value="modelValue"
        :type="type"
        :placeholder="placeholder"
        :disabled="disabled"
        :readonly="readonly"
        :inputmode="inputmode"
        :min="min"
        :max="max"
        :step="step"
        :class="['form-input', { 'has-error': !!errorMessage }]"
        @input="handleInput"
        @focus="isFocused = true"
        @blur="handleBlur"
      />
      <span v-if="suffix" class="input-suffix">{{ suffix }}</span>
    </div>

    <!-- Hint / Error -->
    <p v-if="errorMessage" class="form-error">{{ errorMessage }}</p>
    <p v-else-if="hint" class="form-hint">{{ hint }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useUiStore } from '@/stores/uiStore';

const props = withDefaults(defineProps<{
  label?: string;
  modelValue?: string | number;
  type?: 'text' | 'number' | 'email' | 'password' | 'tel' | 'url';
  placeholder?: string;
  disabled?: boolean;
  readonly?: boolean;
  required?: boolean;
  errorMessage?: string;
  hint?: string;
  suffix?: string;
  helpKey?: string;
  inputmode?: 'none' | 'text' | 'decimal' | 'numeric' | 'tel' | 'search' | 'email' | 'url';
  min?: number;
  max?: number;
  step?: string;
}>(), {
  type: 'text',
  disabled: false,
  readonly: false,
  required: false
});

const emit = defineEmits<{
  'update:modelValue': [value: string | number | undefined];
  'blur': [];
  'input': [value: string | number | undefined];
}>();

const uiStore = useUiStore();
const inputRef = ref<HTMLInputElement>();
const isFocused = ref(false);

function handleInput(event: Event) {
  const target = event.target as HTMLInputElement;
  const value = props.type === 'number' ? (target.value ? Number(target.value) : undefined) : target.value;
  emit('update:modelValue', value);
  emit('input', value);
}

function handleBlur() {
  isFocused.value = false;
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

.required {
  color: var(--kalk-error);
}

.help-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  padding: 0;
  background: none;
  border: none;
  color: var(--kalk-gray-400);
  cursor: pointer;
  border-radius: 50%;
  transition: color 0.15s, background-color 0.15s;
}

.help-btn:hover {
  color: var(--kalk-accent-600);
  background: var(--kalk-accent-50);
}

.input-wrapper {
  position: relative;
}

.input-wrapper.has-suffix .form-input {
  padding-right: var(--kalk-space-12);
}

.form-input {
  width: 100%;
  height: 48px;
  padding: 0 var(--kalk-space-4);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-base);
  color: var(--kalk-gray-900);
  background: #ffffff;
  border: 1.5px solid var(--kalk-gray-200);
  border-radius: var(--kalk-radius-md);
  transition: border-color 0.15s, box-shadow 0.15s;
  outline: none;
}

.form-input:hover:not(:disabled) {
  border-color: var(--kalk-gray-300);
}

.form-input:focus {
  border-color: var(--kalk-accent-500);
  box-shadow: 0 0 0 3px var(--kalk-ring-color);
}

.form-input::placeholder {
  color: var(--kalk-gray-400);
}

.form-input:disabled {
  background: var(--kalk-gray-50);
  color: var(--kalk-gray-400);
  cursor: not-allowed;
}

.form-input.has-error {
  border-color: var(--kalk-error);
}

.form-input.has-error:focus {
  box-shadow: 0 0 0 3px rgba(220, 38, 38, 0.15);
}

.input-suffix {
  position: absolute;
  right: var(--kalk-space-4);
  top: 50%;
  transform: translateY(-50%);
  color: var(--kalk-gray-400);
  font-size: var(--kalk-text-sm);
  pointer-events: none;
}

.form-hint {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
  margin: var(--kalk-space-2) 0 0;
}

.form-error {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-error);
  margin: var(--kalk-space-2) 0 0;
}
</style>
