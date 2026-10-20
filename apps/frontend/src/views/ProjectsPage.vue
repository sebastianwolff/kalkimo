<template>
  <ion-page>
    <AppHeader :title="t('nav.projects')" />

    <ion-content>
      <div class="page-content">
        <!-- Nicht angemeldet -->
        <div v-if="!isAuthenticated" class="login-required panel">
          <div class="panel-body">
            <ion-icon :icon="lockClosedOutline" class="lock-icon" />
            <h2 class="panel-title">{{ t('projectsPage.loginTitle') }}</h2>
            <p class="login-text">{{ t('projectsPage.loginText') }}</p>
            <p class="login-hint">{{ t('projectsPage.loginHint') }}</p>
            <div class="action-buttons">
              <button type="button" class="btn btn-primary" @click="goToLogin">
                <ion-icon :icon="logInOutline" />
                {{ t('auth.login') }}
              </button>
              <button type="button" class="btn btn-outline" @click="goToWizard">
                {{ t('projectsPage.continueAsGuest') }}
              </button>
            </div>
          </div>
        </div>

        <!-- Projektliste -->
        <template v-else>
          <header class="page-head">
            <div>
              <span class="eyebrow">{{ t('projectsPage.eyebrow') }}</span>
              <h1 class="page-title">{{ t('nav.projects') }}</h1>
              <p class="page-subtitle">
                {{ t('projectsPage.subtitle') }}
                <template v-if="!loadingProjects && projects.length > 0">
                  · {{ t('projectsPage.count', { n: projects.length }, projects.length) }}
                </template>
              </p>
            </div>
            <div class="page-actions">
              <button type="button" class="btn btn-primary" @click="startNewProject">
                <ion-icon :icon="addOutline" />
                {{ t('nav.newProject') }}
              </button>
            </div>
          </header>

          <div v-if="loadingProjects" class="panel loading-state">
            <ion-spinner name="lines-small" />
            <span>{{ t('projectsPage.loading') }}</span>
          </div>

          <div v-else-if="projects.length === 0" class="empty-state">
            <ion-icon :icon="folderOpenOutline" class="empty-state-icon" />
            <p class="empty-state-title">{{ t('home.noProjects') }}</p>
            <p class="empty-state-text">{{ t('home.noProjectsHint') }}</p>
            <button type="button" class="btn btn-primary" @click="startNewProject">
              <ion-icon :icon="addOutline" />
              {{ t('nav.newProject') }}
            </button>
          </div>

          <div v-else class="panel table-panel">
            <div class="table-scroll">
              <table class="data-table">
                <thead>
                  <tr>
                    <th>{{ t('home.colProject') }}</th>
                    <th class="num hide-sm">{{ t('home.colVersion') }}</th>
                    <th class="num hide-md">{{ t('projectsPage.colCreated') }}</th>
                    <th class="num">{{ t('home.colUpdated') }}</th>
                    <th class="actions-col" aria-hidden="true"></th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="project in projects"
                    :key="project.id"
                    class="clickable-row"
                    tabindex="0"
                    @click="openProject(project.id)"
                    @keydown.enter="openProject(project.id)"
                  >
                    <td>
                      <div class="project-cell">
                        <span class="project-name">{{ project.name }}</span>
                        <span v-if="project.description" class="project-desc">{{ project.description }}</span>
                      </div>
                    </td>
                    <td class="num hide-sm">{{ project.version != null ? `v${project.version}` : '–' }}</td>
                    <td class="num hide-md">{{ formatDate(project.createdAt) }}</td>
                    <td class="num">{{ formatDate(project.updatedAt) }}</td>
                    <td class="actions-col">
                      <div class="row-actions">
                      <button
                        type="button"
                        class="row-btn danger"
                        :title="t('projectsPage.delete')"
                        :aria-label="t('projectsPage.delete')"
                        @click.stop="confirmDelete(project)"
                      >
                        <ion-icon :icon="trashOutline" />
                      </button>
                      <ion-icon :icon="chevronForwardOutline" class="row-chevron" />
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </template>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import {
  IonPage,
  IonContent,
  IonIcon,
  IonSpinner,
  alertController
} from '@ionic/vue';
import {
  addOutline,
  folderOpenOutline,
  trashOutline,
  lockClosedOutline,
  logInOutline,
  chevronForwardOutline
} from 'ionicons/icons';
import { AppHeader } from '@/components';
import { useAuthStore } from '@/stores/authStore';
import { useProjectStore } from '@/stores/projectStore';
import { useUiStore } from '@/stores/uiStore';

interface DisplayProject {
  id: string;
  name: string;
  description?: string;
  version?: number;
  createdAt: string;
  updatedAt: string;
}

const { t, locale } = useI18n();
const router = useRouter();
const authStore = useAuthStore();
const projectStore = useProjectStore();
const uiStore = useUiStore();

const isAuthenticated = computed(() => authStore.isAuthenticated);
const loadingProjects = ref(false);

const projects = computed<DisplayProject[]>(() => {
  const list: DisplayProject[] = isAuthenticated.value && projectStore.projectSummaries.length > 0
    ? projectStore.projectSummaries.map(s => ({
        id: s.id,
        name: s.name,
        description: s.description,
        version: s.version,
        createdAt: s.createdAt,
        updatedAt: s.updatedAt
      }))
    : projectStore.projects.map(p => ({
        id: p.id,
        name: p.name,
        description: p.description,
        createdAt: p.createdAt,
        updatedAt: p.updatedAt
      }));
  return [...list].sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime());
});

onMounted(async () => {
  if (isAuthenticated.value) {
    loadingProjects.value = true;
    await projectStore.loadProjectListFromServer();
    loadingProjects.value = false;
  }
});

function formatDate(dateStr: string): string {
  if (!dateStr) return '–';
  return new Intl.DateTimeFormat(locale.value, {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  }).format(new Date(dateStr));
}

function goToLogin() {
  router.push('/login');
}

function goToWizard() {
  router.push('/wizard');
}

function startNewProject() {
  projectStore.clearProject();
  uiStore.resetWizard();
  router.push('/wizard');
}

async function openProject(id: string) {
  if (isAuthenticated.value) {
    const project = await projectStore.loadProjectFromServer(id);
    if (project) {
      projectStore.setProject(project, true);
      uiStore.resetWizard();
      router.push('/wizard');
      return;
    }
  }

  // Fallback: lokal gespeichertes Projekt (Gast oder noch nicht synchronisiert)
  const project = projectStore.projects.find(p => p.id === id);
  if (project) {
    projectStore.setProject(project);
    uiStore.resetWizard();
    router.push('/wizard');
  } else {
    uiStore.showToast('Projekt konnte nicht geladen werden', 'error');
  }
}

async function confirmDelete(project: DisplayProject) {
  const alert = await alertController.create({
    header: 'Projekt löschen',
    message: `Möchten Sie "${project.name}" wirklich löschen?`,
    buttons: [
      { text: t('common.cancel'), role: 'cancel' },
      {
        text: t('common.delete'),
        role: 'destructive',
        handler: () => deleteProject(project.id)
      }
    ]
  });

  await alert.present();
}

async function deleteProject(id: string) {
  if (isAuthenticated.value) {
    const deleted = await projectStore.deleteProjectFromServer(id);
    if (deleted) {
      await projectStore.loadProjectListFromServer();
      uiStore.showToast('Projekt gelöscht', 'success');
    } else {
      uiStore.showToast('Löschen fehlgeschlagen', 'error');
    }
  } else {
    const updatedProjects = projectStore.projects.filter(p => p.id !== id);
    projectStore.setProjects(updatedProjects);
    uiStore.showToast('Projekt gelöscht', 'success');
  }
}
</script>

<style scoped>
.login-required {
  max-width: 520px;
  margin: var(--kalk-space-8) auto 0;
}

.login-required .panel-body {
  padding: var(--kalk-space-8);
}

.lock-icon {
  font-size: 24px;
  color: var(--kalk-navy-600);
  margin-bottom: var(--kalk-space-3);
}

.login-text {
  margin-top: var(--kalk-space-2);
  font-size: var(--kalk-text-sm);
}

.login-hint {
  margin-top: var(--kalk-space-2);
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
}

.action-buttons {
  display: flex;
  flex-wrap: wrap;
  gap: var(--kalk-space-2);
  margin-top: var(--kalk-space-6);
}

.loading-state {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  padding: var(--kalk-space-5);
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-600);
}

.table-scroll {
  overflow-x: auto;
}

.clickable-row {
  cursor: pointer;
}

.clickable-row:focus-visible {
  outline: 2px solid var(--kalk-navy-500);
  outline-offset: -2px;
}

.project-cell {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.project-name {
  font-weight: 600;
  color: var(--kalk-navy-900);
}

.project-desc {
  margin-top: 2px;
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
  max-width: 48ch;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.actions-col {
  width: 72px;
  text-align: right;
  white-space: nowrap;
}

.row-actions {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: var(--kalk-space-1);
}

.row-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  font-size: 15px;
  color: var(--kalk-gray-400);
  background: transparent;
  border: 1px solid transparent;
  border-radius: var(--kalk-radius-md);
  cursor: pointer;
  opacity: 0;
  transition: opacity 0.12s;
}

.clickable-row:hover .row-btn,
.clickable-row:focus-within .row-btn {
  opacity: 1;
}

.row-btn.danger:hover {
  color: var(--kalk-negative);
  background: var(--kalk-negative-bg);
  border-color: var(--kalk-negative-border);
}

.row-chevron {
  font-size: 16px;
  color: var(--kalk-gray-400);
}

.clickable-row:hover .row-chevron {
  color: var(--kalk-navy-700);
}

@media (hover: none) {
  .row-btn {
    opacity: 1;
  }
}

@media (max-width: 900px) {
  .hide-md {
    display: none;
  }
}

@media (max-width: 640px) {
  .hide-sm {
    display: none;
  }
}
</style>
