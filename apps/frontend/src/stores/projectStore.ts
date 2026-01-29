import { defineStore } from 'pinia';
import { ref, computed, watch } from 'vue';
import type { Project, CalculationResult, YearMonth, Money, ComponentCondition, CapExCategory } from './types';
import { getDefaultCycleYears } from '@/services/renovationForecastService';
import { projectsApi } from '@/api/projects';
import type { ProjectSummary } from '@/api/projects';
import { useAuthStore } from './authStore';

// LocalStorage key prefixes (suffixed with userId for authenticated users)
const LOCAL_PROJECT_KEY = 'kalkimo_local_project';
const LOCAL_PROJECTS_KEY = 'kalkimo_local_projects';

function getStorageKey(base: string): string {
  // Scope localStorage by userId when authenticated
  try {
    const authStore = useAuthStore();
    if (authStore.user?.id) {
      return `${base}_${authStore.user.id}`;
    }
  } catch {
    // authStore might not be available yet
  }
  return base;
}

// Command for undo/redo
interface Command {
  id: string;
  description: string;
  timestamp: number;
  patch: unknown; // JSON Patch
  inversePatch: unknown;
}

export const useProjectStore = defineStore('project', () => {
  // State
  const currentProject = ref<Project | null>(null);
  const projects = ref<Project[]>([]);
  const projectSummaries = ref<ProjectSummary[]>([]);
  const calculationResult = ref<CalculationResult | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);
  const isSavedToCloud = ref(false); // Track if current project is synced to backend

  // Undo/Redo stack
  const undoStack = ref<Command[]>([]);
  const redoStack = ref<Command[]>([]);
  const maxUndoStackSize = 50;

  // Auto-save current project to localStorage
  watch(currentProject, (project) => {
    if (project) {
      saveToLocalStorage();
    }
  }, { deep: true });

  // Getters
  const hasProject = computed(() => currentProject.value !== null);
  const projectName = computed(() => currentProject.value?.name ?? '');
  const canUndo = computed(() => undoStack.value.length > 0);
  const canRedo = computed(() => redoStack.value.length > 0);
  const hasUnsavedChanges = computed(() => hasProject.value && !isSavedToCloud.value);

  const totalPurchasePrice = computed(() => {
    if (!currentProject.value) return null;
    const purchase = currentProject.value.purchase;
    const basePrice = purchase.purchasePrice.amount;
    const costsTotal = purchase.costs.reduce((sum, c) => {
      const mode = (c as any).mode;
      if (mode === 'percent') {
        return sum + (basePrice * c.amount.amount / 100);
      }
      return sum + c.amount.amount;
    }, 0);
    return {
      amount: basePrice + costsTotal,
      currency: purchase.purchasePrice.currency
    } as Money;
  });

  // Total CapEx amount
  const totalCapex = computed(() => {
    if (!currentProject.value) return 0;
    return currentProject.value.capex.reduce((sum, m) => sum + m.amount.amount, 0);
  });

  // Total capital requirement = purchase (incl. costs) + CapEx
  const totalCapitalRequirement = computed(() => {
    if (!currentProject.value) return null;
    const purchaseTotal = totalPurchasePrice.value?.amount || 0;
    return {
      amount: purchaseTotal + totalCapex.value,
      currency: currentProject.value.purchase.purchasePrice.currency
    } as Money;
  });

  const totalEquityRequired = computed(() => {
    if (!currentProject.value) return null;
    const totalCapital = totalCapitalRequirement.value;
    if (!totalCapital) return null;
    const totalLoans = currentProject.value.financing.loans.reduce(
      (sum, l) => sum + l.principal.amount,
      0
    );
    return {
      amount: totalCapital.amount - totalLoans,
      currency: totalCapital.currency
    } as Money;
  });

  // LocalStorage functions
  function saveToLocalStorage() {
    try {
      if (currentProject.value) {
        localStorage.setItem(getStorageKey(LOCAL_PROJECT_KEY), JSON.stringify(currentProject.value));
      }
    } catch (e) {
      console.warn('Failed to save project to localStorage:', e);
    }
  }

  function saveProjectsToLocalStorage() {
    try {
      localStorage.setItem(getStorageKey(LOCAL_PROJECTS_KEY), JSON.stringify(projects.value));
    } catch (e) {
      console.warn('Failed to save projects to localStorage:', e);
    }
  }

  function initFromStorage() {
    try {
      // Load current project
      const stored = localStorage.getItem(getStorageKey(LOCAL_PROJECT_KEY));
      if (stored) {
        currentProject.value = JSON.parse(stored) as Project;
        isSavedToCloud.value = false;
      }

      // Load projects list
      const storedProjects = localStorage.getItem(getStorageKey(LOCAL_PROJECTS_KEY));
      if (storedProjects) {
        projects.value = JSON.parse(storedProjects) as Project[];
      }
    } catch (e) {
      console.warn('Failed to load project from localStorage:', e);
    }
  }

  function clearLocalStorage() {
    localStorage.removeItem(getStorageKey(LOCAL_PROJECT_KEY));
  }

  // === Server sync methods (for authenticated users) ===

  async function syncProjectToServer(): Promise<boolean> {
    if (!currentProject.value) return false;

    const authStore = useAuthStore();
    if (!authStore.isAuthenticated) return false;

    try {
      isLoading.value = true;
      await projectsApi.saveData(currentProject.value.id, currentProject.value);
      isSavedToCloud.value = true;
      return true;
    } catch (e) {
      console.error('Failed to sync project to server:', e);
      error.value = e instanceof Error ? e.message : 'Sync failed';
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  async function loadProjectFromServer(projectId: string): Promise<Project | null> {
    try {
      isLoading.value = true;
      const project = await projectsApi.getData(projectId);
      return project;
    } catch (e) {
      console.error('Failed to load project from server:', e);
      error.value = e instanceof Error ? e.message : 'Load failed';
      return null;
    } finally {
      isLoading.value = false;
    }
  }

  async function loadProjectListFromServer(): Promise<ProjectSummary[]> {
    const authStore = useAuthStore();
    if (!authStore.isAuthenticated) return [];

    try {
      isLoading.value = true;
      const summaries = await projectsApi.list();
      projectSummaries.value = summaries;
      return summaries;
    } catch (e) {
      console.error('Failed to load project list from server:', e);
      error.value = e instanceof Error ? e.message : 'Load failed';
      return [];
    } finally {
      isLoading.value = false;
    }
  }

  async function deleteProjectFromServer(projectId: string): Promise<boolean> {
    const authStore = useAuthStore();
    if (!authStore.isAuthenticated) return false;

    try {
      await projectsApi.delete(projectId);
      return true;
    } catch (e) {
      console.error('Failed to delete project from server:', e);
      error.value = e instanceof Error ? e.message : 'Delete failed';
      return false;
    }
  }

  // === Clear all project data (called on logout) ===

  function clearAll() {
    currentProject.value = null;
    projects.value = [];
    projectSummaries.value = [];
    calculationResult.value = null;
    isSavedToCloud.value = false;
    undoStack.value = [];
    redoStack.value = [];
    error.value = null;

    // Clear user-scoped localStorage
    localStorage.removeItem(getStorageKey(LOCAL_PROJECT_KEY));
    localStorage.removeItem(getStorageKey(LOCAL_PROJECTS_KEY));

    // Also clear non-scoped keys (legacy cleanup)
    localStorage.removeItem(LOCAL_PROJECT_KEY);
    localStorage.removeItem(LOCAL_PROJECTS_KEY);
  }

  // Actions
  function setProject(project: Project, savedToCloud = false) {
    currentProject.value = project;
    isSavedToCloud.value = savedToCloud;
    undoStack.value = [];
    redoStack.value = [];
    error.value = null;
  }

  function clearProject() {
    currentProject.value = null;
    calculationResult.value = null;
    isSavedToCloud.value = false;
    undoStack.value = [];
    redoStack.value = [];
    clearLocalStorage();
  }

  function updateProject(updates: Partial<Project>) {
    if (!currentProject.value) return;

    // Create command for undo
    const previousState = JSON.parse(JSON.stringify(currentProject.value));

    currentProject.value = {
      ...currentProject.value,
      ...updates,
      updatedAt: new Date().toISOString()
    };

    // Mark as not saved to cloud
    isSavedToCloud.value = false;

    // Push to undo stack
    const command: Command = {
      id: crypto.randomUUID(),
      description: 'Update project',
      timestamp: Date.now(),
      patch: updates,
      inversePatch: previousState
    };

    undoStack.value.push(command);
    if (undoStack.value.length > maxUndoStackSize) {
      undoStack.value.shift();
    }

    // Clear redo stack on new action
    redoStack.value = [];
  }

  function undo() {
    if (!canUndo.value || !currentProject.value) return;

    const command = undoStack.value.pop()!;
    redoStack.value.push(command);

    // Restore previous state
    currentProject.value = command.inversePatch as Project;
    isSavedToCloud.value = false;
  }

  function redo() {
    if (!canRedo.value || !currentProject.value) return;

    const command = redoStack.value.pop()!;
    undoStack.value.push(command);

    // Apply patch
    currentProject.value = {
      ...currentProject.value,
      ...(command.patch as Partial<Project>),
      updatedAt: new Date().toISOString()
    };
    isSavedToCloud.value = false;
  }

  function setCalculationResult(result: CalculationResult) {
    calculationResult.value = result;
  }

  function setLoading(loading: boolean) {
    isLoading.value = loading;
  }

  function setError(err: string | null) {
    error.value = err;
  }

  function setProjects(projectList: Project[]) {
    projects.value = projectList;
    saveProjectsToLocalStorage();
  }

  function markAsSavedToCloud() {
    isSavedToCloud.value = true;
  }

  // Save current project to local projects list
  function saveProjectLocally() {
    if (!currentProject.value) return;

    // Update or add to projects list
    const index = projects.value.findIndex(p => p.id === currentProject.value!.id);
    if (index >= 0) {
      projects.value[index] = { ...currentProject.value };
    } else {
      projects.value.push({ ...currentProject.value });
    }
    saveProjectsToLocalStorage();
  }

  // Factory for new project
  function createNewProject(name: string, currency = 'EUR'): Project {
    const now = new Date();
    const currentYear = now.getFullYear();
    const currentMonth = now.getMonth() + 1; // 1-12

    // Start period: next month
    const startPeriod: YearMonth = currentMonth === 12
      ? { year: currentYear + 1, month: 1 }
      : { year: currentYear, month: currentMonth + 1 };

    // End period: 10 years from start (ending the month before start month)
    const endYear = startPeriod.year + 10;
    const endMonth = startPeriod.month === 1 ? 12 : startPeriod.month - 1;
    const endPeriod: YearMonth = {
      year: endMonth === 12 ? endYear - 1 : endYear,
      month: endMonth
    };

    // Initialize standard building components with default cycle years
    const defaultCondition = 'Good';
    const standardCategories: CapExCategory[] = [
      'Heating', 'Roof', 'Facade', 'Windows',
      'Electrical', 'Plumbing', 'Interior', 'Exterior'
    ];
    const defaultComponents: ComponentCondition[] = standardCategories.map(category => ({
      category,
      condition: defaultCondition,
      expectedCycleYears: getDefaultCycleYears(category),
    }));

    return {
      id: crypto.randomUUID(),
      name,
      currency,
      startPeriod,
      endPeriod,
      property: {
        id: crypto.randomUUID(),
        type: 'SingleFamily',
        constructionYear: 1990,
        overallCondition: 'Good',
        totalArea: 150,
        livingArea: 120,
        unitCount: 1,
        units: [],
        components: defaultComponents
      },
      purchase: {
        purchasePrice: { amount: 300000, currency },
        purchaseDate: startPeriod,
        landValuePercent: 20,
        costs: []
      },
      financing: {
        equity: { amount: 60000, currency },
        loans: []
      },
      rent: {
        units: [],
        vacancyRatePercent: 2,
        rentLossReserveMonths: 3
      },
      costs: {
        items: []
      },
      capex: [],
      taxProfile: {
        marginalTaxRatePercent: 42,
        isCorporate: false,
        use82bDistribution: false,
        distributionYears82b: 5
      },
      createdAt: now.toISOString(),
      updatedAt: now.toISOString()
    };
  }

  return {
    // State
    currentProject,
    projects,
    projectSummaries,
    calculationResult,
    isLoading,
    error,
    isSavedToCloud,

    // Getters
    hasProject,
    projectName,
    canUndo,
    canRedo,
    totalPurchasePrice,
    totalCapex,
    totalCapitalRequirement,
    totalEquityRequired,
    hasUnsavedChanges,

    // Actions
    initFromStorage,
    setProject,
    clearProject,
    updateProject,
    undo,
    redo,
    setCalculationResult,
    setLoading,
    setError,
    setProjects,
    createNewProject,
    saveProjectLocally,
    markAsSavedToCloud,
    clearLocalStorage,

    // Server sync
    syncProjectToServer,
    loadProjectFromServer,
    loadProjectListFromServer,
    deleteProjectFromServer,
    clearAll
  };
});
