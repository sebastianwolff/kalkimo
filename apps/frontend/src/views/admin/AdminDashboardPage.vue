<template>
  <ion-page>
    <AppHeader :title="t('admin.title')" />

    <ion-content :fullscreen="true" class="ion-padding">
      <div class="admin-dashboard">
        <!-- Summary Cards -->
        <div class="summary-cards">
          <KalkCard :title="t('admin.userCount')">
            <div class="summary-value">{{ users.length }}</div>
          </KalkCard>
          <KalkCard :title="t('admin.projectCount')">
            <div class="summary-value">{{ totalProjects }}</div>
          </KalkCard>
        </div>

        <!-- User List -->
        <KalkCard :title="t('admin.users')">
          <div v-if="isLoading" class="loading-state">
            <ion-spinner name="crescent" />
          </div>

          <div v-else-if="error" class="error-state">
            <p>{{ error }}</p>
            <ion-button fill="outline" size="small" @click="loadUsers">
              {{ t('common.error') }}
            </ion-button>
          </div>

          <ion-list v-else>
            <ion-item
              v-for="user in users"
              :key="user.id"
              button
              @click="openUserDetail(user.id)"
              detail
            >
              <ion-icon slot="start" :icon="personOutline" />
              <ion-label>
                <h2>{{ user.name }}</h2>
                <h3>{{ user.email }}</h3>
                <p>
                  <span
                    v-for="role in user.roles"
                    :key="role"
                    class="role-badge"
                    :class="{ 'role-admin': role === 'Admin' }"
                  >{{ role }}</span>
                  <span class="project-count">
                    {{ user.projectCount }} {{ user.projectCount === 1 ? 'Projekt' : 'Projekte' }}
                  </span>
                </p>
              </ion-label>
              <ion-note slot="end">{{ formatDate(user.createdAt) }}</ion-note>
            </ion-item>
          </ion-list>
        </KalkCard>
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
  IonList,
  IonItem,
  IonIcon,
  IonLabel,
  IonNote,
  IonButton,
  IonSpinner
} from '@ionic/vue';
import { personOutline } from 'ionicons/icons';
import { AppHeader, KalkCard } from '@/components';
import { adminApi, type AdminUserSummary } from '@/api';

const { t, locale } = useI18n();
const router = useRouter();

const users = ref<AdminUserSummary[]>([]);
const isLoading = ref(false);
const error = ref<string | null>(null);

const totalProjects = computed(() =>
  users.value.reduce((sum, user) => sum + user.projectCount, 0)
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
    month: 'short',
    day: 'numeric'
  }).format(new Date(dateStr));
}
</script>

<style scoped>
.admin-dashboard {
  max-width: 900px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-6);
}

.summary-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: var(--kalk-space-4);
}

.summary-value {
  font-size: var(--kalk-text-3xl);
  font-weight: 700;
  color: var(--kalk-primary-900);
  text-align: center;
  padding: var(--kalk-space-4) 0;
}

.loading-state {
  display: flex;
  justify-content: center;
  padding: var(--kalk-space-8);
}

.error-state {
  text-align: center;
  padding: var(--kalk-space-8);
  color: var(--ion-color-danger);
}

ion-list {
  background: transparent;
  padding: 0;
}

ion-item {
  --background: transparent;
  --padding-start: 0;
  --inner-padding-end: 0;
}

ion-item h2 {
  font-weight: 600;
  font-size: var(--kalk-text-base);
}

ion-item h3 {
  color: var(--kalk-gray-500);
  font-size: var(--kalk-text-sm);
}

.role-badge {
  display: inline-block;
  padding: 1px 8px;
  border-radius: var(--kalk-radius-sm);
  font-size: var(--kalk-text-xs);
  background: var(--kalk-gray-100);
  color: var(--kalk-gray-700);
  margin-right: var(--kalk-space-1);
}

.role-admin {
  background: var(--kalk-accent-100, #e0e7ff);
  color: var(--kalk-accent-700, #3730a3);
}

.project-count {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-400);
  margin-left: var(--kalk-space-2);
}

ion-note {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-400);
}
</style>
