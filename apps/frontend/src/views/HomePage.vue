<template>
  <ion-page>
    <AppHeader :title="t('nav.dashboard')" />

    <ion-content :fullscreen="true" class="ion-padding">
      <div class="dashboard">
        <h1 class="welcome-title">{{ t('common.appName') }}</h1>
        <p class="welcome-subtitle">Immobilien-Investitionsrechner</p>

        <!-- Guest user with existing project -->
        <div v-if="!isAuthenticated && hasLocalProject" class="current-project-card">
          <div class="project-info">
            <ion-icon :icon="documentOutline" class="project-icon" />
            <div>
              <h3>{{ localProjectName }}</h3>
              <p>Ihr aktuelles Projekt</p>
            </div>
          </div>
          <div class="project-actions">
            <ion-button @click="continueProject">
              Fortsetzen
            </ion-button>
            <ion-button fill="outline" color="danger" @click="confirmNewProject">
              Neues Projekt
            </ion-button>
          </div>
        </div>

        <!-- Action cards -->
        <div class="action-cards">
          <!-- New project card - different behavior for guest vs logged in -->
          <div class="action-card primary" @click="handleNewProject">
            <ion-icon :icon="addCircleOutline" class="card-icon" />
            <h3>{{ t('nav.newProject') }}</h3>
            <p>{{ isAuthenticated ? 'Neues Investitionsprojekt anlegen' : 'Projekt erstellen und berechnen' }}</p>
          </div>

          <!-- Projects card - only for authenticated users -->
          <div v-if="isAuthenticated" class="action-card" @click="router.push('/projects')">
            <ion-icon :icon="folderOutline" class="card-icon" />
            <h3>{{ t('nav.projects') }}</h3>
            <p>Bestehende Projekte verwalten</p>
          </div>

          <!-- Login card - only for guest users -->
          <div v-else class="action-card" @click="router.push('/login')">
            <ion-icon :icon="logInOutline" class="card-icon" />
            <h3>Anmelden</h3>
            <p>Mehrere Projekte speichern & verwalten</p>
          </div>
        </div>

        <!-- Guest info box -->
        <div v-if="!isAuthenticated" class="guest-info">
          <ion-icon :icon="informationCircleOutline" />
          <p>
            Als Gast können Sie ein Projekt erstellen und berechnen.
            Für mehrere Projekte und Cloud-Speicherung melden Sie sich an.
          </p>
        </div>

        <!-- Recent projects - only for authenticated users -->
        <div v-if="isAuthenticated && recentProjects.length > 0" class="recent-section">
          <h2>Letzte Projekte</h2>
          <ion-list>
            <ion-item
              v-for="project in recentProjects"
              :key="project.id"
              button
              @click="openProject(project.id)"
            >
              <ion-icon slot="start" :icon="documentOutline" />
              <ion-label>
                <h3>{{ project.name }}</h3>
                <p>{{ formatDate(project.updatedAt) }}</p>
              </ion-label>
            </ion-item>
          </ion-list>
        </div>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { IonPage, IonContent, IonIcon, IonList, IonItem, IonLabel, IonButton, alertController } from '@ionic/vue';
import { addCircleOutline, folderOutline, documentOutline, logInOutline, informationCircleOutline } from 'ionicons/icons';
import { AppHeader } from '@/components';
import { useAuthStore } from '@/stores/authStore';
import { useProjectStore } from '@/stores/projectStore';
import { useUiStore } from '@/stores/uiStore';

interface ProjectSummary {
  id: string;
  name: string;
  updatedAt: string;
}

const { t, locale } = useI18n();
const router = useRouter();
const authStore = useAuthStore();
const projectStore = useProjectStore();
const uiStore = useUiStore();

const recentProjects = ref<ProjectSummary[]>([]);

const isAuthenticated = computed(() => authStore.isAuthenticated);
const hasLocalProject = computed(() => projectStore.currentProject !== null || projectStore.projects.length > 0);
const localProjectName = computed(() => {
  if (projectStore.currentProject) {
    return projectStore.currentProject.name;
  }
  if (projectStore.projects.length > 0) {
    return projectStore.projects[0].name;
  }
  return 'Unbenanntes Projekt';
});

onMounted(() => {
  // Load recent projects from store (for authenticated users)
  if (isAuthenticated.value) {
    recentProjects.value = projectStore.projects.slice(0, 5).map(p => ({
      id: p.id,
      name: p.name,
      updatedAt: p.updatedAt
    }));
  }
});

function formatDate(dateStr: string): string {
  return new Intl.DateTimeFormat(locale.value, {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  }).format(new Date(dateStr));
}

function handleNewProject() {
  if (!isAuthenticated.value && hasLocalProject.value) {
    // Guest with existing project - ask to confirm replacement
    confirmNewProject();
  } else {
    // Authenticated user or no existing project - just create new
    startNewProject();
  }
}

async function confirmNewProject() {
  const alert = await alertController.create({
    header: 'Neues Projekt erstellen?',
    message: 'Als Gast können Sie nur ein Projekt speichern. Das bestehende Projekt wird ersetzt. Möchten Sie fortfahren?',
    buttons: [
      { text: 'Abbrechen', role: 'cancel' },
      {
        text: 'Neues Projekt',
        role: 'destructive',
        handler: () => startNewProject()
      },
      {
        text: 'Anmelden',
        handler: () => router.push('/login')
      }
    ]
  });

  await alert.present();
}

function startNewProject() {
  projectStore.clearProject();
  // Also clear local projects list for guests
  if (!isAuthenticated.value) {
    projectStore.setProjects([]);
  }
  uiStore.resetWizard();
  router.push('/wizard');
}

function continueProject() {
  // Load the local project if not already loaded
  if (!projectStore.currentProject && projectStore.projects.length > 0) {
    projectStore.setProject(projectStore.projects[0]);
  }
  uiStore.resetWizard();
  router.push('/wizard');
}

function openProject(id: string) {
  const project = projectStore.projects.find(p => p.id === id);
  if (project) {
    projectStore.setProject(project);
    uiStore.resetWizard();
    router.push('/wizard');
  }
}
</script>

<style scoped>
.dashboard {
  max-width: 800px;
  margin: 0 auto;
  padding: var(--kalk-space-6);
}

.welcome-title {
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-3xl);
  font-weight: 700;
  color: var(--kalk-gray-900);
  margin: 0;
}

.welcome-subtitle {
  color: var(--kalk-gray-500);
  font-size: var(--kalk-text-lg);
  margin-top: var(--kalk-space-1);
}

/* Current project card for guests */
.current-project-card {
  background: var(--kalk-primary-50);
  border: 1px solid var(--kalk-primary-200);
  border-radius: var(--kalk-radius-lg);
  padding: var(--kalk-space-6);
  margin-top: var(--kalk-space-6);
}

.project-info {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-4);
  margin-bottom: var(--kalk-space-4);
}

.project-icon {
  font-size: 40px;
  color: var(--kalk-primary-500);
}

.project-info h3 {
  font-size: var(--kalk-text-lg);
  font-weight: 600;
  color: var(--kalk-gray-900);
  margin: 0;
}

.project-info p {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-500);
  margin: 0;
}

.project-actions {
  display: flex;
  gap: var(--kalk-space-3);
}

.action-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: var(--kalk-space-6);
  margin-top: var(--kalk-space-8);
}

.action-card {
  background: #ffffff;
  border: 1px solid var(--kalk-gray-200);
  border-radius: var(--kalk-radius-lg);
  padding: var(--kalk-space-8);
  cursor: pointer;
  transition: transform 0.2s, box-shadow 0.2s;
}

.action-card:hover {
  transform: translateY(-2px);
  box-shadow: var(--kalk-shadow-md);
}

.action-card.primary {
  background: var(--kalk-accent-500);
  border-color: var(--kalk-accent-500);
  color: #ffffff;
}

.action-card.primary .card-icon,
.action-card.primary h3,
.action-card.primary p {
  color: #ffffff;
}

.card-icon {
  font-size: 48px;
  color: var(--kalk-accent-500);
  margin-bottom: var(--kalk-space-4);
}

.action-card h3 {
  font-size: var(--kalk-text-lg);
  font-weight: 600;
  margin: 0 0 var(--kalk-space-1);
  color: var(--kalk-gray-900);
}

.action-card p {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-500);
  margin: 0;
}

/* Guest info box */
.guest-info {
  display: flex;
  align-items: flex-start;
  gap: var(--kalk-space-3);
  margin-top: var(--kalk-space-6);
  padding: var(--kalk-space-4);
  background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-md);
}

.guest-info ion-icon {
  font-size: 20px;
  color: var(--kalk-gray-400);
  flex-shrink: 0;
  margin-top: 2px;
}

.guest-info p {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-600);
  margin: 0;
  line-height: 1.5;
}

.recent-section {
  margin-top: var(--kalk-space-12);
}

.recent-section h2 {
  font-size: var(--kalk-text-lg);
  font-weight: 600;
  color: var(--kalk-gray-900);
  margin-bottom: var(--kalk-space-4);
}

ion-list {
  background: transparent;
}

ion-item {
  --background: #ffffff;
  --border-radius: var(--kalk-radius-md);
  margin-bottom: var(--kalk-space-2);
}
</style>
