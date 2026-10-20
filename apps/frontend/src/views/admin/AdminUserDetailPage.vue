<template>
  <ion-page>
    <AppHeader
      :title="userDetail?.name ?? t('admin.userDetail')"
      :eyebrow="t('nav.userAdmin')"
      back-href="/admin"
    />

    <ion-content>
      <div class="page-content">
        <div v-if="isLoading" class="panel loading-state">
          <ion-spinner name="lines-small" />
        </div>

        <div v-else-if="error" class="callout error">
          <ion-icon :icon="alertCircleOutline" />
          <div>
            <p>{{ error }}</p>
            <button type="button" class="btn btn-outline btn-sm retry-btn" @click="loadUserDetail">
              {{ t('admin.retry') }}
            </button>
          </div>
        </div>

        <template v-else-if="userDetail">
          <header class="page-head">
            <div>
              <span class="eyebrow">{{ t('admin.userDetail') }}</span>
              <h1 class="page-title">{{ userDetail.name }}</h1>
              <p class="page-subtitle">{{ userDetail.email }}</p>
            </div>
          </header>

          <div class="detail-grid">
            <section class="panel">
              <div class="panel-header">
                <h2 class="panel-title">{{ t('admin.userInfo') }}</h2>
              </div>
              <dl class="definition-list">
                <div class="definition-row">
                  <dt>{{ t('auth.name') }}</dt>
                  <dd>{{ userDetail.name }}</dd>
                </div>
                <div class="definition-row">
                  <dt>{{ t('auth.email') }}</dt>
                  <dd>{{ userDetail.email }}</dd>
                </div>
                <div class="definition-row">
                  <dt>{{ t('admin.roles') }}</dt>
                  <dd>
                    <span
                      v-for="role in userDetail.roles"
                      :key="role"
                      :class="['badge', role === 'Admin' ? 'badge-info' : 'badge-neutral']"
                    >{{ role }}</span>
                  </dd>
                </div>
                <div class="definition-row">
                  <dt>{{ t('admin.registeredAt') }}</dt>
                  <dd class="num">{{ formatDate(userDetail.createdAt) }}</dd>
                </div>
              </dl>
            </section>

            <section class="panel">
              <div class="panel-header">
                <h2 class="panel-title">{{ t('admin.projects') }}</h2>
                <span class="badge badge-neutral">{{ userDetail.projects.length }}</span>
              </div>
              <div v-if="userDetail.projects.length > 0" class="table-scroll">
                <table class="data-table">
                  <thead>
                    <tr>
                      <th>{{ t('home.colProject') }}</th>
                      <th class="num">{{ t('home.colVersion') }}</th>
                      <th class="num">{{ t('home.colUpdated') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="project in userDetail.projects" :key="project.id">
                      <td>
                        <div class="project-cell">
                          <span class="project-name">{{ project.name }}</span>
                          <span v-if="project.description" class="project-desc">{{ project.description }}</span>
                        </div>
                      </td>
                      <td class="num">v{{ project.version }}</td>
                      <td class="num">{{ formatDate(project.updatedAt) }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
              <p v-else class="panel-body empty-text">{{ t('admin.noProjects') }}</p>
            </section>
          </div>
        </template>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { IonPage, IonContent, IonIcon, IonSpinner } from '@ionic/vue';
import { alertCircleOutline } from 'ionicons/icons';
import { AppHeader } from '@/components';
import { adminApi, type AdminUserDetail } from '@/api';

const { t, locale } = useI18n();
const route = useRoute();

const userDetail = ref<AdminUserDetail | null>(null);
const isLoading = ref(false);
const error = ref<string | null>(null);

onMounted(() => {
  loadUserDetail();
});

async function loadUserDetail() {
  const userId = route.params.userId as string;
  if (!userId) return;

  isLoading.value = true;
  error.value = null;

  try {
    userDetail.value = await adminApi.getUserDetail(userId);
  } catch (e) {
    error.value = e instanceof Error ? e.message : t('errors.unknownError');
  } finally {
    isLoading.value = false;
  }
}

function formatDate(dateStr: string): string {
  return new Intl.DateTimeFormat(locale.value, {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(dateStr));
}
</script>

<style scoped>
.loading-state {
  display: flex;
  justify-content: center;
  padding: var(--kalk-space-8);
}

.retry-btn {
  margin-top: var(--kalk-space-2);
}

.detail-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1.4fr);
  gap: var(--kalk-space-6);
  align-items: start;
}

@media (max-width: 960px) {
  .detail-grid {
    grid-template-columns: 1fr;
  }
}

.definition-list {
  margin: 0;
  padding: var(--kalk-space-1) var(--kalk-space-5);
}

.definition-row {
  display: grid;
  grid-template-columns: 130px 1fr;
  gap: var(--kalk-space-3);
  padding: var(--kalk-space-3) 0;
  border-bottom: 1px solid var(--kalk-gray-100);
  font-size: var(--kalk-text-sm);
}

.definition-row:last-child {
  border-bottom: 0;
}

.definition-row dt {
  color: var(--kalk-gray-500);
}

.definition-row dd {
  margin: 0;
  color: var(--kalk-gray-900);
  font-weight: 500;
  word-break: break-word;
}

.badge + .badge {
  margin-left: 4px;
}

.table-scroll {
  overflow-x: auto;
}

.project-cell {
  display: flex;
  flex-direction: column;
}

.project-name {
  font-weight: 600;
  color: var(--kalk-navy-900);
}

.project-desc {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
}

.empty-text {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-500);
}
</style>
