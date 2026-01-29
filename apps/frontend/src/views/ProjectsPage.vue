<template>
  <ion-page>
    <AppHeader :title="t('nav.projects')" />

    <ion-content :fullscreen="true" class="ion-padding">
      <!-- Login required message for unauthenticated users -->
      <div v-if="!isAuthenticated" class="login-required">
        <ion-icon :icon="lockClosedOutline" class="lock-icon" />
        <h3>Anmeldung erforderlich</h3>
        <p>
          Um mehrere Projekte zu verwalten, melden Sie sich bitte an oder erstellen Sie ein Konto.
        </p>
        <p class="hint">
          Als Gast können Sie ein einzelnes Projekt erstellen und berechnen.
        </p>
        <div class="action-buttons">
          <ion-button @click="goToLogin">
            <ion-icon slot="start" :icon="logInOutline" />
            Anmelden
          </ion-button>
          <ion-button fill="outline" @click="goToWizard">
            Als Gast fortfahren
          </ion-button>
        </div>
      </div>

      <!-- Full project list for authenticated users -->
      <div v-else class="projects-page">
        <div class="header-actions">
          <ion-button @click="startNewProject">
            <ion-icon slot="start" :icon="addOutline" />
            {{ t('nav.newProject') }}
          </ion-button>
        </div>

        <!-- Loading state -->
        <div v-if="loadingProjects" class="loading-state">
          <ion-spinner name="crescent" />
          <p>Projekte werden geladen...</p>
        </div>

        <div v-else-if="projects.length === 0" class="empty-state">
          <ion-icon :icon="folderOpenOutline" class="empty-icon" />
          <h3>Keine Projekte vorhanden</h3>
          <p>Erstellen Sie Ihr erstes Investitionsprojekt</p>
          <ion-button @click="startNewProject">
            {{ t('nav.newProject') }}
          </ion-button>
        </div>

        <ion-list v-else>
          <ion-item
            v-for="project in projects"
            :key="project.id"
            button
            @click="openProject(project.id)"
          >
            <ion-icon slot="start" :icon="documentOutline" />
            <ion-label>
              <h2>{{ project.name }}</h2>
              <p>{{ project.description || 'Keine Beschreibung' }}</p>
              <p class="meta">
                Aktualisiert: {{ formatDate(project.updatedAt) }}
              </p>
            </ion-label>
            <ion-button
              slot="end"
              fill="clear"
              color="danger"
              @click.stop="confirmDelete(project)"
            >
              <ion-icon slot="icon-only" :icon="trashOutline" />
            </ion-button>
          </ion-item>
        </ion-list>
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
  IonButton,
  IonIcon,
  IonList,
  IonItem,
  IonLabel,
  IonSpinner,
  alertController
} from '@ionic/vue';
import {
  addOutline,
  folderOpenOutline,
  documentOutline,
  trashOutline,
  lockClosedOutline,
  logInOutline
} from 'ionicons/icons';
import { AppHeader } from '@/components';
import { useAuthStore } from '@/stores/authStore';
import { useProjectStore } from '@/stores/projectStore';
import { useUiStore } from '@/stores/uiStore';

interface DisplayProject {
  id: string;
  name: string;
  description?: string;
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

// Reactive project list - from server summaries when authenticated, local otherwise
const projects = computed<DisplayProject[]>(() => {
  if (isAuthenticated.value && projectStore.projectSummaries.length > 0) {
    return projectStore.projectSummaries.map(s => ({
      id: s.id,
      name: s.name,
      description: s.description,
      createdAt: s.createdAt,
      updatedAt: s.updatedAt
    }));
  }
  // Fallback to local projects
  return projectStore.projects.map(p => ({
    id: p.id,
    name: p.name,
    description: p.description,
    createdAt: p.createdAt,
    updatedAt: p.updatedAt
  }));
});

onMounted(async () => {
  if (isAuthenticated.value) {
    loadingProjects.value = true;
    await projectStore.loadProjectListFromServer();
    loadingProjects.value = false;
  }
});

function formatDate(dateStr: string): string {
  return new Intl.DateTimeFormat(locale.value, {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
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
    // Load from server for authenticated users
    const project = await projectStore.loadProjectFromServer(id);
    if (project) {
      projectStore.setProject(project, true);
      uiStore.resetWizard();
      router.push('/wizard');
    } else {
      uiStore.showToast('Projekt konnte nicht geladen werden', 'error');
    }
  } else {
    // Load from local storage for guests
    const project = projectStore.projects.find(p => p.id === id);
    if (project) {
      projectStore.setProject(project);
      uiStore.resetWizard();
      router.push('/wizard');
    }
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
    // Delete from server
    const deleted = await projectStore.deleteProjectFromServer(id);
    if (deleted) {
      // Refresh list from server
      await projectStore.loadProjectListFromServer();
      uiStore.showToast('Projekt gelöscht', 'success');
    } else {
      uiStore.showToast('Löschen fehlgeschlagen', 'error');
    }
  } else {
    // Delete locally
    const updatedProjects = projectStore.projects.filter(p => p.id !== id);
    projectStore.setProjects(updatedProjects);
    uiStore.showToast('Projekt gelöscht', 'success');
  }
}
</script>

<style scoped>
.projects-page {
  max-width: 800px;
  margin: 0 auto;
}

.header-actions {
  display: flex;
  justify-content: flex-end;
  margin-bottom: var(--kalk-space-6);
}

.loading-state {
  text-align: center;
  padding: var(--kalk-space-12);
}

.loading-state ion-spinner {
  margin-bottom: var(--kalk-space-4);
}

.loading-state p {
  color: var(--kalk-gray-500);
}

.empty-state {
  text-align: center;
  padding: var(--kalk-space-12);
}

.empty-icon {
  font-size: 64px;
  color: var(--kalk-gray-400);
  margin-bottom: var(--kalk-space-4);
}

.empty-state h3 {
  color: var(--kalk-gray-900);
  margin-bottom: var(--kalk-space-1);
}

.empty-state p {
  color: var(--kalk-gray-500);
  margin-bottom: var(--kalk-space-6);
}

/* Login required state */
.login-required {
  max-width: 500px;
  margin: var(--kalk-space-12) auto;
  text-align: center;
  padding: var(--kalk-space-8);
  background: #ffffff;
  border-radius: var(--kalk-radius-lg);
  box-shadow: var(--kalk-shadow-md);
}

.lock-icon {
  font-size: 64px;
  color: var(--kalk-gray-400);
  margin-bottom: var(--kalk-space-4);
}

.login-required h3 {
  color: var(--kalk-gray-900);
  font-size: var(--kalk-text-xl);
  margin-bottom: var(--kalk-space-2);
}

.login-required p {
  color: var(--kalk-gray-600);
  margin-bottom: var(--kalk-space-2);
}

.login-required .hint {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-500);
  margin-bottom: var(--kalk-space-6);
}

.action-buttons {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-3);
}

@media (min-width: 400px) {
  .action-buttons {
    flex-direction: row;
    justify-content: center;
  }
}

ion-list {
  background: transparent;
}

ion-item {
  --background: #ffffff;
  --border-radius: var(--kalk-radius-md);
  margin-bottom: var(--kalk-space-2);
}

ion-item h2 {
  font-weight: 600;
  color: var(--kalk-gray-900);
}

ion-item p {
  color: var(--kalk-gray-500);
}

ion-item .meta {
  font-size: var(--kalk-text-xs);
  margin-top: var(--kalk-space-1);
}
</style>
