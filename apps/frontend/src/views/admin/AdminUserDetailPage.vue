<template>
  <ion-page>
    <AppHeader :title="userDetail?.name ?? t('admin.userDetail')" backHref="/admin" />

    <ion-content :fullscreen="true" class="ion-padding">
      <div class="user-detail-page">
        <div v-if="isLoading" class="loading-state">
          <ion-spinner name="crescent" />
        </div>

        <div v-else-if="error" class="error-state">
          <p>{{ error }}</p>
          <ion-button fill="outline" size="small" @click="loadUserDetail">
            {{ t('common.error') }}
          </ion-button>
        </div>

        <template v-else-if="userDetail">
          <!-- User Info -->
          <KalkCard :title="t('admin.userInfo')">
            <ion-list>
              <ion-item>
                <ion-label>
                  <h3>{{ t('auth.name') }}</h3>
                  <p>{{ userDetail.name }}</p>
                </ion-label>
              </ion-item>
              <ion-item>
                <ion-label>
                  <h3>{{ t('auth.email') }}</h3>
                  <p>{{ userDetail.email }}</p>
                </ion-label>
              </ion-item>
              <ion-item>
                <ion-label>
                  <h3>{{ t('admin.roles') }}</h3>
                  <p>
                    <span
                      v-for="role in userDetail.roles"
                      :key="role"
                      class="role-badge"
                      :class="{ 'role-admin': role === 'Admin' }"
                    >{{ role }}</span>
                  </p>
                </ion-label>
              </ion-item>
              <ion-item>
                <ion-label>
                  <h3>{{ t('admin.registeredAt') }}</h3>
                  <p>{{ formatDate(userDetail.createdAt) }}</p>
                </ion-label>
              </ion-item>
            </ion-list>
          </KalkCard>

          <!-- Projects -->
          <KalkCard :title="t('admin.projects')">
            <ion-list v-if="userDetail.projects.length > 0">
              <ion-item
                v-for="project in userDetail.projects"
                :key="project.id"
              >
                <ion-icon slot="start" :icon="documentOutline" />
                <ion-label>
                  <h2>{{ project.name }}</h2>
                  <p v-if="project.description">{{ project.description }}</p>
                  <p>
                    Version {{ project.version }}
                    &middot;
                    {{ formatDate(project.updatedAt) }}
                  </p>
                </ion-label>
              </ion-item>
            </ion-list>
            <div v-else class="empty-state">
              <p>{{ t('admin.noProjects') }}</p>
            </div>
          </KalkCard>
        </template>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import {
  IonPage,
  IonContent,
  IonList,
  IonItem,
  IonIcon,
  IonLabel,
  IonButton,
  IonSpinner
} from '@ionic/vue';
import { documentOutline } from 'ionicons/icons';
import { AppHeader, KalkCard } from '@/components';
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
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(dateStr));
}
</script>

<style scoped>
.user-detail-page {
  max-width: 700px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-6);
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

.empty-state {
  text-align: center;
  padding: var(--kalk-space-6);
  color: var(--kalk-gray-400);
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
  font-weight: 600;
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-500);
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
</style>
