<template>
  <ion-page>
    <AppHeader :title="t('nav.dashboard')" />

    <ion-content>
      <div class="page-content">
        <header class="page-head">
          <div>
            <span class="eyebrow">{{ todayLabel }}</span>
            <h1 class="page-title">
              {{ isAuthenticated ? `${t('home.greeting')}, ${firstName}` : t('home.titleGuest') }}
            </h1>
            <p class="page-subtitle">{{ t('home.subtitle') }}</p>
          </div>
          <div class="page-actions">
            <button type="button" class="btn btn-primary" @click="handleNewProject">
              <ion-icon :icon="addOutline" />
              {{ t('nav.newProject') }}
            </button>
          </div>
        </header>

        <div v-if="!isAuthenticated" class="callout info guest-callout">
          <ion-icon :icon="informationCircleOutline" />
          <p>
            {{ t('home.guestNotice') }}
            <a href="/login" @click.prevent="router.push('/login')">{{ t('home.guestNoticeLink') }}</a>.
          </p>
        </div>

        <div class="dashboard-grid">
          <!-- Hauptspalte -->
          <div class="main-col">
            <!-- Angemeldet: letzte Projekte -->
            <section v-if="isAuthenticated" class="panel">
              <div class="panel-header">
                <h2 class="panel-title">{{ t('home.recentProjects') }}</h2>
                <button
                  v-if="recentProjects.length > 0"
                  type="button"
                  class="btn btn-ghost btn-sm"
                  @click="router.push('/projects')"
                >
                  {{ t('home.allProjects') }}
                  <ion-icon :icon="arrowForwardOutline" />
                </button>
              </div>

              <div v-if="loading" class="panel-body loading-row">
                <ion-spinner name="lines-small" />
              </div>

              <table v-else-if="recentProjects.length > 0" class="data-table">
                <thead>
                  <tr>
                    <th>{{ t('home.colProject') }}</th>
                    <th class="num hide-sm">{{ t('home.colVersion') }}</th>
                    <th class="num">{{ t('home.colUpdated') }}</th>
                    <th aria-hidden="true"></th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="project in recentProjects"
                    :key="project.id"
                    class="clickable-row"
                    tabindex="0"
                    @click="openProject(project.id)"
                    @keydown.enter="openProject(project.id)"
                  >
                    <td>
                      <span class="project-name">{{ project.name }}</span>
                    </td>
                    <td class="num hide-sm">v{{ project.version }}</td>
                    <td class="num">{{ formatDate(project.updatedAt) }}</td>
                    <td class="row-action">
                      <ion-icon :icon="chevronForwardOutline" />
                    </td>
                  </tr>
                </tbody>
              </table>

              <div v-else class="panel-body empty-inline">
                <p class="empty-title">{{ t('home.noProjects') }}</p>
                <p class="empty-text">{{ t('home.noProjectsHint') }}</p>
                <button type="button" class="btn btn-outline btn-sm" @click="handleNewProject">
                  <ion-icon :icon="addOutline" />
                  {{ t('nav.newProject') }}
                </button>
              </div>
            </section>

            <!-- Gast mit lokalem Projekt -->
            <section v-else-if="hasLocalProject" class="panel">
              <div class="panel-header">
                <h2 class="panel-title">{{ t('home.currentProject') }}</h2>
                <span class="badge badge-neutral">{{ t('home.currentProjectHint') }}</span>
              </div>
              <div class="panel-body current-project">
                <div class="current-project-info">
                  <ion-icon :icon="documentTextOutline" class="doc-icon" />
                  <span class="project-name">{{ localProjectName }}</span>
                </div>
                <div class="current-project-actions">
                  <button type="button" class="btn btn-danger btn-sm" @click="confirmNewProject">
                    {{ t('home.replace') }}
                  </button>
                  <button type="button" class="btn btn-primary btn-sm" @click="continueProject">
                    {{ t('home.continue') }}
                  </button>
                </div>
              </div>
            </section>

            <!-- Ablauf -->
            <section v-if="!isAuthenticated || recentProjects.length === 0" class="panel">
              <div class="panel-header">
                <h2 class="panel-title">{{ t('home.startTitle') }}</h2>
              </div>
              <ol class="process-list">
                <li v-for="(step, i) in processSteps" :key="i">
                  <span class="process-index">{{ String(i + 1).padStart(2, '0') }}</span>
                  <div>
                    <p class="process-title">{{ step.title }}</p>
                    <p class="process-text">{{ step.text }}</p>
                  </div>
                </li>
              </ol>
              <div class="panel-footer">
                <button type="button" class="btn btn-primary btn-sm" @click="handleNewProject">
                  {{ t('home.startCta') }}
                  <ion-icon :icon="arrowForwardOutline" />
                </button>
              </div>
            </section>
          </div>

          <!-- Seitenspalte -->
          <aside class="side-col">
            <section class="panel">
              <div class="panel-header">
                <h2 class="panel-title">{{ t('home.methodTitle') }}</h2>
              </div>
              <ul class="fact-list">
                <li v-for="key in ['method1', 'method2', 'method3', 'method4']" :key="key">
                  <ion-icon :icon="checkmarkOutline" />
                  <span>{{ t(`home.${key}`) }}</span>
                </li>
              </ul>
            </section>

            <section class="panel">
              <div class="panel-header">
                <h2 class="panel-title">{{ t('home.securityTitle') }}</h2>
                <ion-icon :icon="shieldCheckmarkOutline" class="panel-icon" />
              </div>
              <ul class="fact-list">
                <li v-for="key in ['security1', 'security2', 'security3']" :key="key">
                  <ion-icon :icon="checkmarkOutline" />
                  <span>{{ t(`home.${key}`) }}</span>
                </li>
              </ul>
            </section>
          </aside>
        </div>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { IonPage, IonContent, IonIcon, IonSpinner, alertController } from '@ionic/vue';
import {
  addOutline,
  arrowForwardOutline,
  chevronForwardOutline,
  documentTextOutline,
  informationCircleOutline,
  checkmarkOutline,
  shieldCheckmarkOutline
} from 'ionicons/icons';
import { AppHeader } from '@/components';
import { useAuthStore } from '@/stores/authStore';
import { useProjectStore } from '@/stores/projectStore';
import { useUiStore } from '@/stores/uiStore';

interface ProjectSummary {
  id: string;
  name: string;
  version: number;
  updatedAt: string;
}

const { t, locale } = useI18n();
const router = useRouter();
const authStore = useAuthStore();
const projectStore = useProjectStore();
const uiStore = useUiStore();

const recentProjects = computed<ProjectSummary[]>(() =>
  [...projectStore.projectSummaries]
    .sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime())
    .slice(0, 5)
    .map(s => ({
      id: s.id,
      name: s.name,
      version: s.version,
      updatedAt: s.updatedAt
    }))
);
const loading = ref(false);

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

const firstName = computed(() => {
  const name = authStore.user?.name?.trim();
  if (name) return name.split(/\s+/)[0];
  return authStore.user?.email?.split('@')[0] ?? '';
});

const todayLabel = computed(() =>
  new Intl.DateTimeFormat(locale.value, {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric'
  }).format(new Date())
);

const processSteps = computed(() => [
  { title: t('home.start1Title'), text: t('home.start1Text') },
  { title: t('home.start2Title'), text: t('home.start2Text') },
  { title: t('home.start3Title'), text: t('home.start3Text') }
]);

onMounted(async () => {
  if (isAuthenticated.value) {
    loading.value = true;
    await projectStore.loadProjectListFromServer();
    loading.value = false;
  }
});

function formatDate(dateStr: string): string {
  return new Intl.DateTimeFormat(locale.value, {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  }).format(new Date(dateStr));
}

function handleNewProject() {
  if (!isAuthenticated.value && hasLocalProject.value) {
    confirmNewProject();
  } else {
    startNewProject();
  }
}

async function confirmNewProject() {
  const alert = await alertController.create({
    header: t('home.newProjectConfirmTitle'),
    message: t('home.newProjectConfirmText'),
    buttons: [
      { text: t('home.cancel'), role: 'cancel' },
      { text: t('home.login'), handler: () => router.push('/login') },
      {
        text: t('nav.newProject'),
        role: 'destructive',
        handler: () => startNewProject()
      }
    ]
  });

  await alert.present();
}

function startNewProject() {
  projectStore.clearProject();
  if (!isAuthenticated.value) {
    projectStore.setProjects([]);
  }
  uiStore.resetWizard();
  router.push('/wizard');
}

function continueProject() {
  if (!projectStore.currentProject && projectStore.projects.length > 0) {
    projectStore.setProject(projectStore.projects[0]);
  }
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
  const project = projectStore.projects.find(p => p.id === id);
  if (project) {
    projectStore.setProject(project);
    uiStore.resetWizard();
    router.push('/wizard');
  }
}
</script>

<style scoped>
.eyebrow::first-letter {
  text-transform: uppercase;
}

.guest-callout {
  margin-bottom: var(--kalk-space-6);
}

.guest-callout a {
  font-weight: 500;
}

.dashboard-grid {
  display: grid;
  grid-template-columns: minmax(0, 2fr) minmax(0, 1fr);
  gap: var(--kalk-space-6);
  align-items: start;
}

.main-col,
.side-col {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-6);
}

@media (max-width: 960px) {
  .dashboard-grid {
    grid-template-columns: 1fr;
  }
}

.loading-row {
  display: flex;
  justify-content: center;
}

.clickable-row {
  cursor: pointer;
}

.clickable-row:focus-visible {
  outline: 2px solid var(--kalk-navy-500);
  outline-offset: -2px;
}

.project-name {
  font-weight: 600;
  color: var(--kalk-navy-900);
}

.row-action {
  width: 36px;
  text-align: right;
  color: var(--kalk-gray-400);
}

.clickable-row:hover .row-action {
  color: var(--kalk-navy-700);
}

@media (max-width: 640px) {
  .hide-sm {
    display: none;
  }
}

.empty-inline {
  text-align: left;
}

.empty-title {
  font-weight: 600;
  color: var(--kalk-gray-800);
}

.empty-text {
  margin: 4px 0 var(--kalk-space-4);
  font-size: var(--kalk-text-sm);
}

.current-project {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--kalk-space-4);
  flex-wrap: wrap;
}

.current-project-info {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
}

.doc-icon {
  font-size: 22px;
  color: var(--kalk-navy-600);
}

.current-project-actions {
  display: flex;
  gap: var(--kalk-space-2);
}

.process-list {
  list-style: none;
  margin: 0;
  padding: 0;
}

.process-list li {
  display: flex;
  gap: var(--kalk-space-4);
  padding: var(--kalk-space-4) var(--kalk-space-5);
  border-bottom: 1px solid var(--kalk-gray-100);
}

.process-list li:last-child {
  border-bottom: 0;
}

.process-index {
  flex-shrink: 0;
  width: 28px;
  font-family: var(--kalk-font-serif);
  font-size: var(--kalk-text-lg);
  font-weight: 600;
  color: var(--kalk-brass-600);
  line-height: 1.3;
}

.process-title {
  font-weight: 600;
  color: var(--kalk-gray-900);
}

.process-text {
  margin-top: 2px;
  font-size: var(--kalk-text-sm);
}

.panel-icon {
  font-size: 18px;
  color: var(--kalk-navy-600);
}

.fact-list {
  list-style: none;
  margin: 0;
  padding: var(--kalk-space-3) var(--kalk-space-5) var(--kalk-space-4);
}

.fact-list li {
  display: flex;
  align-items: flex-start;
  gap: var(--kalk-space-2);
  padding: 6px 0;
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-700);
  line-height: 1.45;
}

.fact-list ion-icon {
  flex-shrink: 0;
  margin-top: 2px;
  font-size: 14px;
  color: var(--kalk-positive);
}
</style>
