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
      class="nav-button nav-button-accent"
      :disabled="!canProceed || loading"
      @click="handleCalculate"
    >
      <svg class="button-icon" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M6 2a2 2 0 00-2 2v12a2 2 0 002 2h8a2 2 0 002-2V4a2 2 0 00-2-2H6zm1 2a1 1 0 000 2h6a1 1 0 100-2H7zm6 7a1 1 0 011 1v3a1 1 0 11-2 0v-3a1 1 0 011-1zm-3 3a1 1 0 011 1v1a1 1 0 11-2 0v-1a1 1 0 011-1zm-2-2a1 1 0 01.993.883L9 13v3a1 1 0 01-1.993.117L7 16v-3a1 1 0 011-1zm6-6a1 1 0 100 2h.01a1 1 0 100-2H13zm-3 0a1 1 0 100 2h.01a1 1 0 100-2H10zM7 8a1 1 0 100 2h.01a1 1 0 100-2H7z" clip-rule="evenodd" />
      </svg>
      {{ t('common.calculate') }}
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
  'calculate': [];
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

function handleCalculate() {
  emit('calculate');
}
</script>

<style scoped>
.wizard-navigation {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: var(--kalk-space-3);
  padding: var(--kalk-space-3) max(var(--kalk-space-8), calc((100% - var(--kalk-content-max)) / 2 + var(--kalk-space-8)));
  padding-bottom: calc(var(--kalk-space-3) + env(safe-area-inset-bottom));
  background: var(--kalk-surface);
  border-top: 1px solid var(--kalk-border);
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
  padding: 0 var(--kalk-space-5);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  border-radius: var(--kalk-radius-md);
  cursor: pointer;
  transition: background-color 0.12s, border-color 0.12s;
  min-width: 128px;
  height: 40px;
}

.nav-button:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.nav-button-outline {
  background: var(--kalk-surface);
  border: 1px solid var(--kalk-border-strong);
  color: var(--kalk-gray-800);
}

.nav-button-outline:hover:not(:disabled) {
  background: var(--kalk-gray-50);
  border-color: var(--kalk-gray-400);
}

.nav-button-primary {
  background: var(--kalk-navy-800);
  border: 1px solid var(--kalk-navy-800);
  color: #ffffff;
}

.nav-button-primary:hover:not(:disabled) {
  background: var(--kalk-navy-900);
}

.nav-button-accent {
  background: var(--kalk-navy-900);
  border: 1px solid var(--kalk-navy-900);
  color: #ffffff;
}

.nav-button-accent:hover:not(:disabled) {
  background: var(--kalk-navy-950);
}

.button-icon {
  width: 16px;
  height: 16px;
}

@media (max-width: 768px) {
  .wizard-navigation {
    padding-left: var(--kalk-space-4);
    padding-right: var(--kalk-space-4);
  }
  .nav-button {
    min-width: 0;
    flex: 1;
  }
  .spacer {
    display: none;
  }
}
</style>
