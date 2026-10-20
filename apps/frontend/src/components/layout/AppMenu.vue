<template>
  <ion-menu content-id="main-content" type="overlay" class="kalk-sidebar">
    <div class="sidebar">
      <div class="sidebar-brand">
        <KalkLogo variant="dark" />
      </div>

      <nav class="sidebar-nav" aria-label="Hauptnavigation">
        <span class="nav-group-label">{{ t('nav.workspace') }}</span>
        <button
          v-for="item in menuItems"
          :key="item.path"
          type="button"
          :class="['nav-item', { active: isActive(item) }]"
          :aria-current="isActive(item) ? 'page' : undefined"
          @click="navigate(item.path)"
        >
          <ion-icon :icon="item.icon" class="nav-icon" />
          <span>{{ t(item.labelKey) }}</span>
        </button>

        <template v-if="isAdmin">
          <span class="nav-group-label">{{ t('nav.admin') }}</span>
          <button
            v-for="item in adminMenuItems"
            :key="item.path"
            type="button"
            :class="['nav-item', { active: isActive(item) }]"
            :aria-current="isActive(item) ? 'page' : undefined"
            @click="navigate(item.path)"
          >
            <ion-icon :icon="item.icon" class="nav-icon" />
            <span>{{ t(item.labelKey) }}</span>
          </button>
        </template>
      </nav>

      <div class="sidebar-footer">
        <div v-if="isAuthenticated" class="user-block">
          <div class="user-avatar" aria-hidden="true">{{ initials }}</div>
          <div class="user-meta">
            <span class="user-name">{{ displayName }}</span>
            <span class="user-role">{{ roleLabel }}</span>
          </div>
          <button type="button" class="icon-btn" :title="t('auth.logout')" @click="handleLogout">
            <ion-icon :icon="logOutOutline" />
          </button>
        </div>
        <button v-else type="button" class="nav-item" @click="navigate('/login')">
          <ion-icon :icon="logInOutline" class="nav-icon" />
          <span>{{ t('auth.login') }}</span>
        </button>

        <div class="security-note">
          <ion-icon :icon="lockClosedOutline" />
          <span>{{ t('nav.securityNote') }}</span>
        </div>
      </div>
    </div>
  </ion-menu>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { IonMenu, IonIcon, menuController } from '@ionic/vue';
import {
  gridOutline,
  folderOutline,
  addOutline,
  settingsOutline,
  helpCircleOutline,
  logOutOutline,
  logInOutline,
  shieldOutline,
  lockClosedOutline
} from 'ionicons/icons';
import KalkLogo from './KalkLogo.vue';
import { useAuthStore } from '@/stores/authStore';
import { useProjectStore } from '@/stores/projectStore';
import { authApi } from '@/api';

interface MenuItem {
  path: string;
  icon: string;
  labelKey: string;
  requiresAuth?: boolean;
  match?: string[];
}

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const projectStore = useProjectStore();

const isAuthenticated = computed(() => authStore.isAuthenticated);
const isAdmin = computed(() => authStore.isAdmin);
const displayName = computed(() => authStore.displayName);

const initials = computed(() => {
  const name = displayName.value || '';
  const parts = name.split(/[\s@.]+/).filter(Boolean);
  return ((parts[0]?.[0] ?? '') + (parts[1]?.[0] ?? '')).toUpperCase() || '·';
});

const roleLabel = computed(() => (isAdmin.value ? t('nav.roleAdmin') : t('nav.roleUser')));

const allMenuItems: MenuItem[] = [
  { path: '/home', icon: gridOutline, labelKey: 'nav.dashboard', match: ['/', '/home'] },
  { path: '/projects', icon: folderOutline, labelKey: 'nav.projects', requiresAuth: true },
  { path: '/wizard', icon: addOutline, labelKey: 'nav.newProject' },
  { path: '/settings', icon: settingsOutline, labelKey: 'nav.settings' },
  { path: '/help', icon: helpCircleOutline, labelKey: 'nav.help' }
];

const adminMenuItems: MenuItem[] = [
  { path: '/admin', icon: shieldOutline, labelKey: 'nav.userAdmin', match: ['/admin'] }
];

const menuItems = computed(() =>
  allMenuItems.filter(item => !item.requiresAuth || isAuthenticated.value)
);

function isActive(item: MenuItem): boolean {
  if (item.match) {
    return item.match.some(p => (p === '/' ? route.path === '/' : route.path.startsWith(p)));
  }
  return route.path === item.path;
}

async function navigate(path: string) {
  await menuController.close();
  router.push(path);
}

async function handleLogout() {
  projectStore.clearAll();
  await authApi.logout();
  authStore.logout();
  await menuController.close();
  router.push('/login');
}
</script>

<style scoped>
.kalk-sidebar {
  --background: var(--kalk-navy-900);
  --width: var(--kalk-sidebar-width);
}

.sidebar {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: var(--kalk-navy-900);
  color: #ffffff;
  border-right: 1px solid var(--kalk-navy-950);
}

.sidebar-brand {
  display: flex;
  align-items: center;
  height: var(--kalk-header-height);
  padding: 0 var(--kalk-space-5);
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  padding-top: env(safe-area-inset-top);
  box-sizing: content-box;
}

.sidebar-nav {
  flex: 1;
  overflow-y: auto;
  padding: var(--kalk-space-4) var(--kalk-space-3);
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.nav-group-label {
  display: block;
  padding: var(--kalk-space-4) var(--kalk-space-3) var(--kalk-space-2);
  font-size: var(--kalk-text-2xs);
  font-weight: 600;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: rgba(255, 255, 255, 0.45);
}

.nav-group-label:first-child {
  padding-top: 0;
}

.nav-item {
  position: relative;
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  width: 100%;
  height: 38px;
  padding: 0 var(--kalk-space-3);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  text-align: left;
  color: rgba(255, 255, 255, 0.78);
  background: transparent;
  border: 0;
  border-radius: var(--kalk-radius-md);
  cursor: pointer;
  transition: background-color 0.12s, color 0.12s;
}

.nav-item:hover {
  background: rgba(255, 255, 255, 0.06);
  color: #ffffff;
}

.nav-item.active {
  background: rgba(255, 255, 255, 0.1);
  color: #ffffff;
}

.nav-item.active::before {
  content: '';
  position: absolute;
  left: -12px;
  top: 8px;
  bottom: 8px;
  width: 3px;
  background: var(--kalk-brass-500);
  border-radius: 0 2px 2px 0;
}

.nav-icon {
  font-size: 18px;
  flex-shrink: 0;
  opacity: 0.85;
}

.sidebar-footer {
  padding: var(--kalk-space-3);
  border-top: 1px solid rgba(255, 255, 255, 0.08);
  padding-bottom: calc(var(--kalk-space-3) + env(safe-area-inset-bottom));
}

.user-block {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  padding: var(--kalk-space-2);
}

.user-avatar {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  flex-shrink: 0;
  font-size: var(--kalk-text-xs);
  font-weight: 600;
  color: var(--kalk-navy-900);
  background: var(--kalk-brass-300);
  border-radius: var(--kalk-radius-md);
}

.user-meta {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  line-height: 1.25;
}

.user-name {
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  color: #ffffff;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.user-role {
  font-size: var(--kalk-text-2xs);
  color: rgba(255, 255, 255, 0.5);
}

.icon-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  color: rgba(255, 255, 255, 0.65);
  background: transparent;
  border: 0;
  border-radius: var(--kalk-radius-md);
  cursor: pointer;
  font-size: 18px;
}

.icon-btn:hover {
  background: rgba(255, 255, 255, 0.08);
  color: #ffffff;
}

.security-note {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-top: var(--kalk-space-2);
  padding: var(--kalk-space-2);
  font-size: var(--kalk-text-2xs);
  color: rgba(255, 255, 255, 0.42);
}

.security-note ion-icon {
  font-size: 12px;
  flex-shrink: 0;
}
</style>
