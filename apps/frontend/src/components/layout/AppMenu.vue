<template>
  <ion-menu content-id="main-content" type="overlay">
    <ion-header>
      <ion-toolbar>
        <ion-title>{{ t('common.appName') }}</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content>
      <ion-list>
        <ion-item
          v-for="item in menuItems"
          :key="item.path"
          :router-link="item.path"
          router-direction="root"
          lines="none"
          detail
          :class="{ 'menu-item-active': isActive(item.path) }"
        >
          <ion-icon slot="start" :icon="item.icon" />
          <ion-label>{{ t(item.labelKey) }}</ion-label>
        </ion-item>
      </ion-list>

      <ion-list v-if="isAuthenticated">
        <ion-list-header>
          <ion-label>{{ displayName }}</ion-label>
        </ion-list-header>

        <ion-item button @click="handleLogout" lines="none">
          <ion-icon slot="start" :icon="logOutOutline" />
          <ion-label>{{ t('auth.logout') }}</ion-label>
        </ion-item>
      </ion-list>
    </ion-content>
  </ion-menu>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import {
  IonMenu,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonContent,
  IonList,
  IonListHeader,
  IonItem,
  IonIcon,
  IonLabel,
  menuController
} from '@ionic/vue';
import {
  homeOutline,
  folderOutline,
  addCircleOutline,
  settingsOutline,
  helpCircleOutline,
  logOutOutline
} from 'ionicons/icons';
import { useAuthStore } from '@/stores/authStore';
import { authApi } from '@/api';

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const isAuthenticated = computed(() => authStore.isAuthenticated);
const displayName = computed(() => authStore.displayName);

const allMenuItems = [
  { path: '/', icon: homeOutline, labelKey: 'nav.dashboard', requiresAuth: false },
  { path: '/projects', icon: folderOutline, labelKey: 'nav.projects', requiresAuth: true },
  { path: '/wizard', icon: addCircleOutline, labelKey: 'nav.newProject', requiresAuth: false },
  { path: '/settings', icon: settingsOutline, labelKey: 'nav.settings', requiresAuth: false },
  { path: '/help', icon: helpCircleOutline, labelKey: 'nav.help', requiresAuth: false }
];

// Filter menu items based on auth status
const menuItems = computed(() =>
  allMenuItems.filter(item => !item.requiresAuth || isAuthenticated.value)
);

function isActive(path: string): boolean {
  return route.path === path;
}

async function handleLogout() {
  await authApi.logout();
  authStore.logout();
  await menuController.close();
  router.push('/login');
}
</script>

<style scoped>
ion-menu {
  --background: #ffffff;
}

ion-toolbar {
  --background: var(--kalk-primary-900);
  --color: #ffffff;
}

ion-list {
  background: transparent;
  padding: var(--kalk-space-2) 0;
}

ion-item {
  --background: transparent;
  --color: var(--kalk-gray-900);
  --padding-start: var(--kalk-space-4);
  margin: var(--kalk-space-1) var(--kalk-space-2);
  border-radius: var(--kalk-radius-md);
  transition: background-color 0.2s;
}

ion-item:hover {
  --background: var(--kalk-gray-50);
}

.menu-item-active {
  --background: var(--kalk-accent-500);
  --color: #ffffff;
}

.menu-item-active ion-icon {
  color: #ffffff;
}

ion-icon {
  color: var(--kalk-gray-500);
}

ion-list-header {
  --color: var(--kalk-gray-500);
  font-size: var(--kalk-text-sm);
  padding: var(--kalk-space-4);
}
</style>
