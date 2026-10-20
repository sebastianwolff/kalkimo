<template>
  <ion-page>
    <AppHeader :title="t('nav.settings')" />

    <ion-content>
      <div class="page-content narrow">
        <header class="page-head">
          <div>
            <span class="eyebrow">{{ t('settingsPage.eyebrow') }}</span>
            <h1 class="page-title">{{ t('nav.settings') }}</h1>
            <p class="page-subtitle">{{ t('settingsPage.subtitle') }}</p>
          </div>
        </header>

        <section class="panel">
          <div class="panel-header">
            <h2 class="panel-title">{{ t('settingsPage.general') }}</h2>
          </div>
          <div class="setting-row">
            <div class="setting-text">
              <span class="setting-label">{{ t('settingsPage.language') }}</span>
              <span class="setting-hint">{{ t('settingsPage.languageHint') }}</span>
            </div>
            <div class="segmented" role="radiogroup" :aria-label="t('settingsPage.language')">
              <button
                v-for="opt in languages"
                :key="opt.value"
                type="button"
                role="radio"
                :aria-checked="locale === opt.value"
                :class="{ active: locale === opt.value }"
                @click="changeLocale(opt.value)"
              >
                {{ opt.label }}
              </button>
            </div>
          </div>
        </section>

        <section v-if="isAuthenticated" class="panel">
          <div class="panel-header">
            <h2 class="panel-title">{{ t('settingsPage.account') }}</h2>
          </div>
          <dl class="definition-list">
            <div v-if="userName" class="definition-row">
              <dt>{{ t('settingsPage.name') }}</dt>
              <dd>{{ userName }}</dd>
            </div>
            <div class="definition-row">
              <dt>{{ t('settingsPage.email') }}</dt>
              <dd>{{ userEmail }}</dd>
            </div>
            <div class="definition-row">
              <dt>{{ t('settingsPage.role') }}</dt>
              <dd>{{ isAdmin ? t('nav.roleAdmin') : t('nav.roleUser') }}</dd>
            </div>
          </dl>
          <div class="panel-footer footer-row">
            <span class="setting-hint">{{ t('settingsPage.logoutHint') }}</span>
            <button type="button" class="btn btn-danger btn-sm" @click="handleLogout">
              <ion-icon :icon="logOutOutline" />
              {{ t('auth.logout') }}
            </button>
          </div>
        </section>

        <section class="panel">
          <div class="panel-header">
            <h2 class="panel-title">{{ t('settingsPage.about') }}</h2>
          </div>
          <dl class="definition-list">
            <div class="definition-row">
              <dt>{{ t('settingsPage.version') }}</dt>
              <dd class="mono">1.0.0-dev</dd>
            </div>
            <div class="definition-row">
              <dt>{{ t('settingsPage.storage') }}</dt>
              <dd>{{ t('settingsPage.storageValue') }}</dd>
            </div>
            <div class="definition-row">
              <dt>{{ t('settingsPage.publisher') }}</dt>
              <dd>Kalkimo</dd>
            </div>
          </dl>
        </section>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { IonPage, IonContent, IonIcon } from '@ionic/vue';
import { logOutOutline } from 'ionicons/icons';
import { AppHeader } from '@/components';
import { useAuthStore } from '@/stores/authStore';
import { useProjectStore } from '@/stores/projectStore';
import { setLocale } from '@/i18n';
import { authApi } from '@/api';

const { t, locale } = useI18n();
const router = useRouter();
const authStore = useAuthStore();
const projectStore = useProjectStore();

const languages: { value: 'de' | 'en'; label: string }[] = [
  { value: 'de', label: 'Deutsch' },
  { value: 'en', label: 'English' }
];

const isAuthenticated = computed(() => authStore.isAuthenticated);
const isAdmin = computed(() => authStore.isAdmin);
const userEmail = computed(() => authStore.user?.email || '');
const userName = computed(() => authStore.user?.name || '');

function changeLocale(value: 'de' | 'en') {
  setLocale(value);
}

async function handleLogout() {
  projectStore.clearAll();
  await authApi.logout();
  authStore.logout();
  router.push('/login');
}
</script>

<style scoped>
.panel + .panel {
  margin-top: var(--kalk-space-5);
}

.setting-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--kalk-space-4);
  padding: var(--kalk-space-4) var(--kalk-space-5);
  flex-wrap: wrap;
}

.setting-text {
  display: flex;
  flex-direction: column;
}

.setting-label {
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-900);
}

.setting-hint {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
}

.segmented {
  display: inline-flex;
  padding: 2px;
  background: var(--kalk-gray-100);
  border-radius: var(--kalk-radius-md);
}

.segmented button {
  height: 30px;
  padding: 0 var(--kalk-space-4);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  color: var(--kalk-gray-600);
  background: transparent;
  border: 0;
  border-radius: 2px;
  cursor: pointer;
}

.segmented button.active {
  background: var(--kalk-surface);
  color: var(--kalk-navy-900);
  box-shadow: 0 1px 2px rgba(11, 31, 54, 0.12);
}

.definition-list {
  margin: 0;
  padding: var(--kalk-space-1) var(--kalk-space-5);
}

.definition-row {
  display: grid;
  grid-template-columns: 180px 1fr;
  gap: var(--kalk-space-4);
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

.mono {
  font-family: var(--kalk-font-mono);
  font-size: var(--kalk-text-xs);
}

.footer-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--kalk-space-4);
}

@media (max-width: 520px) {
  .definition-row {
    grid-template-columns: 1fr;
    gap: 2px;
  }
}
</style>
