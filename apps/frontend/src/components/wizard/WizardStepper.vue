<template>
  <div class="wizard-stepper">
    <div class="stepper-container">
      <!-- Progress line -->
      <div class="progress-line">
        <div class="progress-fill" :style="{ width: `${progressWidth}%` }" />
      </div>

      <!-- Steps -->
      <div class="steps">
        <div
          v-for="(step, index) in steps"
          :key="step.id"
          :class="[
            'step',
            {
              'is-active': step.id === currentStep,
              'is-completed': step.isCompleted,
              'is-clickable': step.isCompleted || index <= currentStepIndex + 1
            }
          ]"
          @click="handleStepClick(step, index)"
        >
          <!-- Step circle -->
          <div class="step-circle">
            <svg v-if="step.isCompleted" class="check-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3">
              <polyline points="20 6 9 17 4 12" />
            </svg>
            <span v-else class="step-number">{{ index + 1 }}</span>
          </div>

          <!-- Step label -->
          <span class="step-label">{{ t(step.labelKey) }}</span>
        </div>
      </div>
    </div>
  </div>
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

// Calculate progress width based on completed steps
const progressWidth = computed(() => {
  const totalSteps = steps.value.length;
  if (totalSteps <= 1) return 0;
  return (currentStepIndex.value / (totalSteps - 1)) * 100;
});

function handleStepClick(step: WizardStepInfo, index: number) {
  if (step.isCompleted || index <= currentStepIndex.value + 1) {
    uiStore.goToStep(step.id);
  }
}
</script>

<style scoped>
.wizard-stepper {
  background: #ffffff;
  border-bottom: 1px solid var(--kalk-gray-200);
  padding: var(--kalk-space-5) var(--kalk-space-6);
}

.stepper-container {
  max-width: 900px;
  margin: 0 auto;
  position: relative;
}

/* Progress line behind circles */
.progress-line {
  position: absolute;
  top: 16px;
  left: 24px;
  right: 24px;
  height: 2px;
  background: var(--kalk-gray-200);
  z-index: 0;
}

.progress-fill {
  height: 100%;
  background: var(--kalk-accent-500);
  transition: width 0.3s ease;
}

/* Steps container */
.steps {
  display: flex;
  justify-content: space-between;
  position: relative;
  z-index: 1;
}

/* Individual step */
.step {
  display: flex;
  flex-direction: column;
  align-items: center;
  flex: 1;
  max-width: 100px;
}

.step.is-clickable {
  cursor: pointer;
}

/* Step circle */
.step-circle {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: #ffffff;
  border: 2px solid var(--kalk-gray-300);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: var(--kalk-space-2);
  transition: all 0.2s;
}

.step-number {
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-400);
}

/* Active step */
.step.is-active .step-circle {
  border-color: var(--kalk-accent-500);
  background: var(--kalk-accent-500);
  box-shadow: 0 0 0 4px var(--kalk-accent-100);
}

.step.is-active .step-number {
  color: #ffffff;
}

.step.is-active .step-label {
  color: var(--kalk-gray-900);
  font-weight: 600;
}

/* Completed step */
.step.is-completed .step-circle {
  border-color: var(--kalk-accent-500);
  background: var(--kalk-accent-500);
}

.check-icon {
  width: 16px;
  height: 16px;
  color: #ffffff;
}

.step.is-completed .step-label {
  color: var(--kalk-gray-600);
}

/* Step label */
.step-label {
  font-size: 11px;
  font-weight: 500;
  color: var(--kalk-gray-400);
  text-align: center;
  white-space: nowrap;
  transition: color 0.2s;
}

/* Hover state */
.step.is-clickable:hover .step-label {
  color: var(--kalk-gray-700);
}

/* Mobile: Horizontal scroll */
@media (max-width: 768px) {
  .wizard-stepper {
    padding: var(--kalk-space-4);
    overflow-x: auto;
    -webkit-overflow-scrolling: touch;
  }

  .stepper-container {
    min-width: 600px;
  }

  .steps {
    gap: var(--kalk-space-2);
  }

  .step {
    min-width: 70px;
  }

  .step-label {
    font-size: 10px;
  }
}
</style>
