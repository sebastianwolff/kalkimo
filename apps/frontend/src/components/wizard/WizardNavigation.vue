<template>
  <div class="wizard-navigation">
    <button
      v-if="!isFirstStep"
      type="button"
      class="nav-button nav-button-outline"
      :disabled="loading"
      @click="handleBack"
    >
      <svg class="button-icon" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd" />
      </svg>
      {{ t('common.back') }}
    </button>

    <div v-else class="spacer" />

    <button
      v-if="!isLastStep"
      type="button"
      class="nav-button nav-button-primary"
      :disabled="!canProceed || loading"
      @click="handleNext"
    >
      {{ t('common.next') }}
      <svg class="button-icon" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
      </svg>
    </button>

    <button
      v-else
      type="button"
      class="nav-button nav-button-success"
      :disabled="!canProceed || loading"
      @click="handleFinish"
    >
      <svg class="button-icon" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
      </svg>
      {{ t('common.finish') }}
    </button>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useUiStore } from '@/stores/uiStore';

const props = withDefaults(defineProps<{
  canProceed?: boolean;
  loading?: boolean;
}>(), {
  canProceed: true,
  loading: false
});

const emit = defineEmits<{
  'back': [];
  'next': [];
  'finish': [];
}>();

const { t } = useI18n();
const uiStore = useUiStore();

const isFirstStep = computed(() => uiStore.isFirstStep);
const isLastStep = computed(() => uiStore.isLastStep);

function handleBack() {
  uiStore.previousStep();
  emit('back');
}

function handleNext() {
  uiStore.nextStep();
  emit('next');
}

function handleFinish() {
  emit('finish');
}
</script>

<style scoped>
.wizard-navigation {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--kalk-space-4) var(--kalk-space-6);
  background: #ffffff;
  border-top: 1px solid var(--kalk-gray-200);
  position: sticky;
  bottom: 0;
}

.spacer {
  flex: 1;
}

.nav-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--kalk-space-2);
  padding: var(--kalk-space-3) var(--kalk-space-5);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  border-radius: var(--kalk-radius-md);
  cursor: pointer;
  transition: all 0.15s;
  min-width: 120px;
  height: 44px;
}

.nav-button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.nav-button-outline {
  background: transparent;
  border: 1.5px solid var(--kalk-gray-300);
  color: var(--kalk-gray-700);
}

.nav-button-outline:hover:not(:disabled) {
  background: var(--kalk-gray-50);
  border-color: var(--kalk-gray-400);
}

.nav-button-primary {
  background: var(--kalk-primary-900);
  border: none;
  color: #ffffff;
  box-shadow: var(--kalk-shadow-sm);
}

.nav-button-primary:hover:not(:disabled) {
  background: var(--kalk-primary-800);
}

.nav-button-success {
  background: var(--kalk-accent-600);
  border: none;
  color: #ffffff;
  box-shadow: var(--kalk-shadow-sm);
}

.nav-button-success:hover:not(:disabled) {
  background: var(--kalk-accent-700);
}

.button-icon {
  width: 18px;
  height: 18px;
}
</style>
