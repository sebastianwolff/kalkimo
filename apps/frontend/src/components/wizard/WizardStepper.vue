<template>
  <nav class="wizard-stepper" :aria-label="t('wizard.progress')">
    <div class="stepper-container">
      <div class="stepper-meta">
        <span class="meta-step">
          {{ t('wizard.step') }} {{ currentStepIndex + 1 }} {{ t('wizard.of') }} {{ steps.length }}
        </span>
        <span class="meta-title">{{ currentLabel }}</span>
      </div>

      <ol class="steps">
        <li
          v-for="(step, index) in steps"
          :key="step.id"
          :class="[
            'step',
            {
              'is-active': step.id === currentStep,
              'is-completed': step.isCompleted && step.id !== currentStep,
              'is-clickable': step.isCompleted || index <= currentStepIndex + 1
            }
          ]"
        >
          <button
            type="button"
            class="step-button"
            :disabled="!(step.isCompleted || index <= currentStepIndex + 1)"
            :aria-current="step.id === currentStep ? 'step' : undefined"
            @click="handleStepClick(step, index)"
          >
            <span class="step-index">
              <svg v-if="step.isCompleted && step.id !== currentStep" class="check-icon" viewBox="0 0 16 16" aria-hidden="true">
                <path d="M3.5 8.5l3 3 6-7" />
              </svg>
              <template v-else>{{ String(index + 1).padStart(2, '0') }}</template>
            </span>
            <span class="step-label">{{ t(step.labelKey) }}</span>
          </button>
        </li>
      </ol>
    </div>
  </nav>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useUiStore, type WizardStepInfo } from '@/stores/uiStore';

const { t } = useI18n();
const uiStore = useUiStore();

const steps = computed(() => uiStore.wizardSteps);
const currentStep = computed(() => uiStore.currentWizardStep);
const currentStepIndex = computed(() => uiStore.currentStepIndex);
const currentLabel = computed(() => {
  const step = steps.value[currentStepIndex.value];
  return step ? t(step.labelKey) : '';
});

function handleStepClick(step: WizardStepInfo, index: number) {
  if (step.isCompleted || index <= currentStepIndex.value + 1) {
    uiStore.goToStep(step.id);
  }
}
</script>

<style scoped>
.wizard-stepper {
  background: var(--kalk-surface);
  border-bottom: 1px solid var(--kalk-border);
}

.stepper-container {
  max-width: var(--kalk-content-max);
  margin: 0 auto;
  padding: var(--kalk-space-4) var(--kalk-space-8) 0;
}

.stepper-meta {
  display: none;
}

.steps {
  display: grid;
  grid-auto-flow: column;
  grid-auto-columns: 1fr;
  list-style: none;
  margin: 0;
  padding: 0;
}

.step-button {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 2px;
  width: 100%;
  padding: 0 var(--kalk-space-2) var(--kalk-space-3) 0;
  font-family: var(--kalk-font-family);
  text-align: left;
  background: transparent;
  border: 0;
  border-bottom: 2px solid var(--kalk-gray-200);
  cursor: default;
  transition: border-color 0.15s;
}

.step.is-clickable .step-button {
  cursor: pointer;
}

.step-index {
  display: flex;
  align-items: center;
  height: 16px;
  font-size: var(--kalk-text-2xs);
  font-weight: 600;
  letter-spacing: 0.04em;
  color: var(--kalk-gray-400);
  font-variant-numeric: tabular-nums;
}

.check-icon {
  width: 14px;
  height: 14px;
  fill: none;
  stroke: var(--kalk-navy-600);
  stroke-width: 2;
  stroke-linecap: square;
}

.step-label {
  font-size: var(--kalk-text-xs);
  font-weight: 500;
  color: var(--kalk-gray-500);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 100%;
}

/* Erledigt */
.step.is-completed .step-button {
  border-bottom-color: var(--kalk-navy-400);
}

.step.is-completed .step-label {
  color: var(--kalk-gray-700);
}

/* Aktiv */
.step.is-active .step-button {
  border-bottom-color: var(--kalk-navy-900);
  border-bottom-width: 3px;
  padding-bottom: calc(var(--kalk-space-3) - 1px);
}

.step.is-active .step-index {
  color: var(--kalk-brass-600);
}

.step.is-active .step-label {
  color: var(--kalk-navy-900);
  font-weight: 600;
}

.step.is-clickable:not(.is-active) .step-button:hover .step-label {
  color: var(--kalk-navy-700);
}

.step.is-clickable:not(.is-active) .step-button:hover {
  border-bottom-color: var(--kalk-navy-500);
}

.step-button:disabled .step-label {
  color: var(--kalk-gray-400);
}

/* Mobil: kompakte Kopfzeile + schmale Segmentleiste */
@media (max-width: 860px) {
  .stepper-container {
    padding: var(--kalk-space-3) var(--kalk-space-4) var(--kalk-space-3);
  }

  .stepper-meta {
    display: flex;
    align-items: baseline;
    justify-content: space-between;
    margin-bottom: var(--kalk-space-2);
  }

  .meta-step {
    font-size: var(--kalk-text-2xs);
    font-weight: 600;
    letter-spacing: var(--kalk-tracking-caps);
    text-transform: uppercase;
    color: var(--kalk-gray-500);
  }

  .meta-title {
    font-size: var(--kalk-text-sm);
    font-weight: 600;
    color: var(--kalk-navy-900);
  }

  .steps {
    gap: 3px;
  }

  .step-button {
    padding: 0;
    height: 10px;
    border-bottom-width: 3px;
  }

  .step.is-active .step-button {
    padding: 0;
    border-bottom-width: 3px;
  }

  .step-index,
  .step-label {
    display: none;
  }
}
</style>
