import { defineStore } from 'pinia';
import { ref, computed } from 'vue';

export type WizardStep =
  | 'project'       // Projekt-Grunddaten
  | 'property'      // Objektdaten
  | 'purchase'      // Kaufdaten
  | 'capex'         // Maßnahmen (vor Finanzierung, damit in Kapitalbedarf einbezogen)
  | 'financing'     // Finanzierung
  | 'rent'          // Mieten
  | 'costs'         // Kosten
  | 'tax'           // Steuern
  | 'summary';      // Zusammenfassung

export interface WizardStepInfo {
  id: WizardStep;
  labelKey: string; // i18n key
  icon: string;
  isCompleted: boolean;
}

const WIZARD_STEPS: WizardStepInfo[] = [
  { id: 'project', labelKey: 'wizard.steps.project', icon: 'folder-outline', isCompleted: false },
  { id: 'property', labelKey: 'wizard.steps.property', icon: 'home-outline', isCompleted: false },
  { id: 'purchase', labelKey: 'wizard.steps.purchase', icon: 'cart-outline', isCompleted: false },
  { id: 'capex', labelKey: 'wizard.steps.capex', icon: 'construct-outline', isCompleted: false },
  { id: 'financing', labelKey: 'wizard.steps.financing', icon: 'cash-outline', isCompleted: false },
  { id: 'rent', labelKey: 'wizard.steps.rent', icon: 'people-outline', isCompleted: false },
  { id: 'costs', labelKey: 'wizard.steps.costs', icon: 'receipt-outline', isCompleted: false },
  { id: 'tax', labelKey: 'wizard.steps.tax', icon: 'calculator-outline', isCompleted: false },
  { id: 'summary', labelKey: 'wizard.steps.summary', icon: 'checkmark-done-outline', isCompleted: false }
];

export const useUiStore = defineStore('ui', () => {
  // State
  const currentWizardStep = ref<WizardStep>('project');
  const wizardSteps = ref<WizardStepInfo[]>(JSON.parse(JSON.stringify(WIZARD_STEPS)));
  const isSideMenuOpen = ref(false);
  const isHelpPanelOpen = ref(false);
  const currentHelpKey = ref<string | null>(null);
  const toastMessage = ref<string | null>(null);
  const toastType = ref<'success' | 'error' | 'warning' | 'info'>('info');
  const isModalOpen = ref(false);
  const modalComponent = ref<string | null>(null);
  const modalProps = ref<Record<string, unknown>>({});

  // Getters
  const currentStepIndex = computed(() =>
    wizardSteps.value.findIndex(s => s.id === currentWizardStep.value)
  );

  const currentStepInfo = computed(() =>
    wizardSteps.value.find(s => s.id === currentWizardStep.value)
  );

  const canGoBack = computed(() => currentStepIndex.value > 0);
  const canGoForward = computed(() => currentStepIndex.value < wizardSteps.value.length - 1);

  const isFirstStep = computed(() => currentStepIndex.value === 0);
  const isLastStep = computed(() => currentStepIndex.value === wizardSteps.value.length - 1);

  const progressPercent = computed(() =>
    Math.round(((currentStepIndex.value + 1) / wizardSteps.value.length) * 100)
  );

  const completedStepsCount = computed(() =>
    wizardSteps.value.filter(s => s.isCompleted).length
  );

  // Actions
  function goToStep(step: WizardStep) {
    currentWizardStep.value = step;
  }

  function nextStep() {
    if (!canGoForward.value) return;

    // Mark current step as completed
    const currentIndex = currentStepIndex.value;
    wizardSteps.value[currentIndex].isCompleted = true;

    currentWizardStep.value = wizardSteps.value[currentIndex + 1].id;
  }

  function previousStep() {
    if (!canGoBack.value) return;
    currentWizardStep.value = wizardSteps.value[currentStepIndex.value - 1].id;
  }

  function markStepCompleted(step: WizardStep, completed = true) {
    const stepInfo = wizardSteps.value.find(s => s.id === step);
    if (stepInfo) {
      stepInfo.isCompleted = completed;
    }
  }

  function resetWizard() {
    currentWizardStep.value = 'project';
    wizardSteps.value = JSON.parse(JSON.stringify(WIZARD_STEPS));
  }

  function toggleSideMenu() {
    isSideMenuOpen.value = !isSideMenuOpen.value;
  }

  function openHelp(helpKey: string) {
    currentHelpKey.value = helpKey;
    isHelpPanelOpen.value = true;
  }

  function closeHelp() {
    isHelpPanelOpen.value = false;
  }

  function showToast(message: string, type: 'success' | 'error' | 'warning' | 'info' = 'info') {
    toastMessage.value = message;
    toastType.value = type;

    // Auto-clear after 3 seconds
    setTimeout(() => {
      toastMessage.value = null;
    }, 3000);
  }

  function openModal(component: string, props: Record<string, unknown> = {}) {
    modalComponent.value = component;
    modalProps.value = props;
    isModalOpen.value = true;
  }

  function closeModal() {
    isModalOpen.value = false;
    modalComponent.value = null;
    modalProps.value = {};
  }

  return {
    // State
    currentWizardStep,
    wizardSteps,
    isSideMenuOpen,
    isHelpPanelOpen,
    currentHelpKey,
    toastMessage,
    toastType,
    isModalOpen,
    modalComponent,
    modalProps,

    // Getters
    currentStepIndex,
    currentStepInfo,
    canGoBack,
    canGoForward,
    isFirstStep,
    isLastStep,
    progressPercent,
    completedStepsCount,

    // Actions
    goToStep,
    nextStep,
    previousStep,
    markStepCompleted,
    resetWizard,
    toggleSideMenu,
    openHelp,
    closeHelp,
    showToast,
    openModal,
    closeModal
  };
});
