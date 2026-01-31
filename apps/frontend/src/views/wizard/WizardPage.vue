<template>
  <ion-page>
    <AppHeader
      :title="t('wizard.title')"
      :show-menu-button="false"
      :show-back-button="true"
      :back-href="backHref"
      :show-undo-redo="true"
      :show-progress="true"
    >
      <template #actions>
        <ion-button
          fill="clear"
          :disabled="isLoading"
          @click="handleCloseProject"
          class="close-project-btn"
        >
          <ion-icon slot="start" :icon="saveOutline" />
          {{ t('common.closeProject') }}
        </ion-button>
      </template>
    </AppHeader>

    <ion-content>
      <WizardStepper />

      <div class="wizard-content">
        <component
          :is="currentStepComponent"
          ref="stepRef"
          @validation-change="handleValidationChange"
        />
      </div>
    </ion-content>

    <WizardNavigation
      :can-proceed="isStepValid"
      :loading="isLoading"
      @calculate="handleCalculate"
    />
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, markRaw, type Component } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { IonPage, IonContent, IonButton, IonIcon } from '@ionic/vue';
import { saveOutline } from 'ionicons/icons';
import { AppHeader, WizardStepper, WizardNavigation } from '@/components';
import { useAuthStore } from '@/stores/authStore';
import { useProjectStore } from '@/stores/projectStore';
import { useUiStore } from '@/stores/uiStore';

// Import wizard step components
import ProjectStep from './steps/ProjectStep.vue';
import PropertyStep from './steps/PropertyStep.vue';
import PurchaseStep from './steps/PurchaseStep.vue';
import FinancingStep from './steps/FinancingStep.vue';
import RentStep from './steps/RentStep.vue';
import CostsStep from './steps/CostsStep.vue';
import TaxStep from './steps/TaxStep.vue';
import CapexStep from './steps/CapExStep.vue';
import SummaryStep from './steps/SummaryStep.vue';

const { t } = useI18n();
const router = useRouter();
const authStore = useAuthStore();
const projectStore = useProjectStore();
const uiStore = useUiStore();

const stepRef = ref<any>(null);
const isStepValid = ref(true);
const isLoading = ref(false);

const isAuthenticated = computed(() => authStore.isAuthenticated);
const backHref = computed(() => isAuthenticated.value ? '/projects' : '/home');

// Map step IDs to components
const stepComponents: Record<string, Component> = {
  project: markRaw(ProjectStep),
  property: markRaw(PropertyStep),
  purchase: markRaw(PurchaseStep),
  financing: markRaw(FinancingStep),
  rent: markRaw(RentStep),
  costs: markRaw(CostsStep),
  tax: markRaw(TaxStep),
  capex: markRaw(CapexStep),
  summary: markRaw(SummaryStep)
};

const currentStepComponent = computed(() =>
  stepComponents[uiStore.currentWizardStep]
);

onMounted(() => {
  // Create new project if none exists
  if (!projectStore.hasProject) {
    const newProject = projectStore.createNewProject(t('project.namePlaceholder'));
    projectStore.setProject(newProject);
  }

  // Reset wizard to first step
  uiStore.resetWizard();
});

function handleValidationChange(valid: boolean) {
  isStepValid.value = valid;
}

function handleCalculate() {
  if (!projectStore.currentProject) return;
  // Delegate to SummaryStep's exposed calculate function
  if (stepRef.value?.calculate) {
    stepRef.value.calculate();
  }
}

async function handleCloseProject() {
  if (!projectStore.currentProject) return;

  // Save project locally (to localStorage)
  projectStore.saveProjectLocally();

  // Sync to server for authenticated users
  if (isAuthenticated.value) {
    isLoading.value = true;
    const synced = await projectStore.syncProjectToServer();
    isLoading.value = false;

    if (synced) {
      uiStore.showToast('Projekt gespeichert', 'success');
    } else {
      uiStore.showToast('Lokal gespeichert (Server-Sync fehlgeschlagen)', 'warning');
    }

    router.push('/projects');
  } else {
    uiStore.showToast('Projekt gespeichert', 'success');
    router.push('/home');
  }
}
</script>

<style scoped>
.wizard-content {
  padding: var(--kalk-space-lg);
  padding-bottom: 100px; /* Space for navigation */
}

@media (min-width: 768px) {
  .wizard-content {
    max-width: 1024px;
    margin: 0 auto;
  }
}

.close-project-btn {
  --color: rgba(255, 255, 255, 0.85);
  font-size: var(--kalk-text-xs);
  font-weight: 500;
  text-transform: none;
  letter-spacing: normal;
}
</style>
