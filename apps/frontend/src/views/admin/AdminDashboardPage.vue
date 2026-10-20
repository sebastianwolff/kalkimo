<template>
  <ion-page>
    <AppHeader :title="t('admin.title')" />

    <ion-content>
      <div class="page-content">
        <header class="page-head">
          <div>
            <span class="eyebrow">{{ t('admin.title') }}</span>
            <h1 class="page-title">{{ t('nav.userAdmin') }}</h1>
            <p class="page-subtitle">{{ t('admin.subtitle') }}</p>
          </div>
        </header>

        <div class="kpi-grid kpi-row">
          <div class="kpi">
            <div class="kpi-label">{{ t('admin.userCount') }}</div>
            <div class="kpi-value">{{ isLoading ? '–' : users.length }}</div>
          </div>
          <div class="kpi">
            <div class="kpi-label">{{ t('admin.projectCount') }}</div>
            <div class="kpi-value">{{ isLoading ? '–' : totalProjects }}</div>
          </div>
          <div class="kpi">
            <div class="kpi-label">{{ t('admin.adminCount') }}</div>
            <div class="kpi-value">{{ isLoading ? '–' : adminCount }}</div>
          </div>
        </div>

        <section class="panel">
          <div class="panel-header">
            <h2 class="panel-title">{{ t('admin.users') }}</h2>
          </div>

          <div v-if="isLoading" class="panel-body loading-state">
            <ion-spinner name="lines-small" />
          </div>

          <div v-else-if="error" class="panel-body">
            <div class="callout error">
              <ion-icon :icon="alertCircleOutline" />
              <div>
                <p>{{ error }}</p>
                <button type="button" class="btn btn-outline btn-sm retry-btn" @click="loadUsers">
                  {{ t('admin.retry') }}
                </button>
              </div>
            </div>
          </div>

          <div v-else class="table-scroll">
            <table class="data-table">
              <thead>
                <tr>
                  <th>{{ t('admin.colUser') }}</th>
                  <th>{{ t('admin.roles') }}</th>
                  <th class="num">{{ t('admin.colProjects') }}</th>
                  <th class="num hide-sm">{{ t('admin.registeredAt') }}</th>
                  <th aria-hidden="true"></th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="user in users"
                  :key="user.id"
                  class="clickable-row"
                  tabindex="0"
                  @click="openUserDetail(user.id)"
                  @keydown.enter="openUserDetail(user.id)"
                >
                  <td>
                    <div class="user-cell">
                      <span class="user-name">{{ user.name }}</span>
                      <span class="user-email">{{ user.email }}</span>
                    </div>
                  </td>
                  <td>
                    <span
                      v-for="role in user.roles"
                      :key="role"
                      :class="['badge', role === 'Admin' ? 'badge-info' : 'badge-neutral']"
                    >{{ role }}</span>
                  </td>
                  <td class="num">{{ user.projectCount }}</td>
                  <td class="num hide-sm">{{ formatDate(user.createdAt) }}</td>
                  <td class="row-chevron">
                    <ion-icon :icon="chevronForwardOutline" />
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { IonPage, IonContent, IonIcon, IonSpinner } from '@ionic/vue';
import { chevronForwardOutline, alertCircleOutline } from 'ionicons/icons';
import { AppHeader } from '@/components';
import { adminApi, type AdminUserSummary } from '@/api';

const { t, locale } = useI18n();
const router = useRouter();

const users = ref<AdminUserSummary[]>([]);
const isLoading = ref(false);
const error = ref<string | null>(null);

const totalProjects = computed(() =>
  users.value.reduce((sum, user) => sum + user.projectCount, 0)
);

const adminCount = computed(() =>
  users.value.filter(user => user.roles.includes('Admin')).length
);

onMounted(() => {
  loadUsers();
});

async function loadUsers() {
  isLoading.value = true;
  error.value = null;

  try {
    users.value = await adminApi.getUsers();
  } catch (e) {
    error.value = e instanceof Error ? e.message : t('errors.unknownError');
  } finally {
    isLoading.value = false;
  }
}

function openUserDetail(userId: string) {
  router.push(`/admin/users/${userId}`);
}

function formatDate(dateStr: string): string {
  return new Intl.DateTimeFormat(locale.value, {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  }).format(new Date(dateStr));
}
</script>

<style scoped>
.kpi-row {
  margin-bottom: var(--kalk-space-6);
}

.loading-state {
  display: flex;
  justify-content: center;
}

.retry-btn {
  margin-top: var(--kalk-space-2);
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

.user-cell {
  display: flex;
  flex-direction: column;
}

.user-name {
  font-weight: 600;
  color: var(--kalk-navy-900);
}

.user-email {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
}

.badge + .badge {
  margin-left: 4px;
}

.row-chevron {
  width: 36px;
  text-align: right;
  color: var(--kalk-gray-400);
}

.clickable-row:hover .row-chevron {
  color: var(--kalk-navy-700);
}

@media (max-width: 640px) {
  .hide-sm {
    display: none;
  }
}
</style>
