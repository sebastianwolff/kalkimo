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

        <div v-if="projects.length === 0" class="empty-state">
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
                {{ t(`property.types.${project.propertyType}`) }} |
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
import { computed } from 'vue';
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

interface LocalProjectSummary {
  id: string;
  name: string;
  description?: string;
  propertyType: string;
  currency: string;
  createdAt: string;
  updatedAt: string;
  isLocal?: boolean;
}

const { t, locale } = useI18n();
const router = useRouter();
const authStore = useAuthStore();
const projectStore = useProjectStore();
const uiStore = useUiStore();

const isAuthenticated = computed(() => authStore.isAuthenticated);

// Reactive project list - automatically updates when store changes
const projects = computed<LocalProjectSummary[]>(() =>
  projectStore.projects.map(p => ({
    id: p.id,
    name: p.name,
    description: p.description,
    propertyType: p.property?.type || 'SingleFamily',
    currency: p.currency,
    createdAt: p.createdAt,
    updatedAt: p.updatedAt,
    isLocal: true
  }))
);

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

function openProject(id: string) {
  // Load project from local storage
  const project = projectStore.projects.find(p => p.id === id);
  if (project) {
    projectStore.setProject(project);
    uiStore.resetWizard();
    router.push('/wizard');
  }
}

async function confirmDelete(project: LocalProjectSummary) {
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

function deleteProject(id: string) {
  // Remove from store (computed projects list will auto-update)
  const updatedProjects = projectStore.projects.filter(p => p.id !== id);
  projectStore.setProjects(updatedProjects);
  uiStore.showToast('Projekt gelöscht', 'success');
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
