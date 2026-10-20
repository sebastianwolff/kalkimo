<template>
  <ion-header class="kalk-header">
    <ion-toolbar>
      <ion-buttons slot="start">
        <ion-menu-button v-if="showMenuButton" />
        <ion-back-button
          v-if="displayBackButton"
          :default-href="backHref"
          :text="''"
          :icon="arrowBackOutline"
        />
      </ion-buttons>

      <div class="header-title">
        <span v-if="eyebrow" class="header-eyebrow">{{ eyebrow }}</span>
        <span class="header-text">
          <slot name="title">{{ title }}</slot>
        </span>
      </div>

      <ion-buttons slot="end" class="header-actions">
        <slot name="actions" />

        <div v-if="showUndoRedo" class="tool-group" role="group" :aria-label="t('common.undo') + ' / ' + t('common.redo')">
          <button
            type="button"
            class="tool-btn"
            :disabled="!canUndo"
            :title="t('common.undo')"
            @click="handleUndo"
          >
            <ion-icon :icon="arrowUndoOutline" />
          </button>
          <button
            type="button"
            class="tool-btn"
            :disabled="!canRedo"
            :title="t('common.redo')"
            @click="handleRedo"
          >
            <ion-icon :icon="arrowRedoOutline" />
          </button>
        </div>

        <div class="lang-switch" role="group" aria-label="Sprache / Language">
          <button
            type="button"
            :class="{ active: currentLocale === 'de' }"
            @click="changeLanguage('de')"
          >DE</button>
          <button
            type="button"
            :class="{ active: currentLocale === 'en' }"
            @click="changeLanguage('en')"
          >EN</button>
        </div>
      </ion-buttons>
    </ion-toolbar>

    <ion-progress-bar
      v-if="showProgress"
      :value="progressValue / 100"
      class="kalk-progress"
    />
  </ion-header>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import {
  IonHeader,
  IonToolbar,
  IonButtons,
  IonIcon,
  IonMenuButton,
  IonBackButton,
  IonProgressBar
} from '@ionic/vue';
import { arrowUndoOutline, arrowRedoOutline, arrowBackOutline } from 'ionicons/icons';
import { useProjectStore } from '@/stores/projectStore';
import { useUiStore } from '@/stores/uiStore';
import { setLocale } from '@/i18n';

const props = withDefaults(defineProps<{
  title?: string;
  eyebrow?: string;
  showMenuButton?: boolean;
  showBackButton?: boolean;
  backHref?: string;
  showUndoRedo?: boolean;
  showProgress?: boolean;
}>(), {
  title: 'Kalkimo',
  eyebrow: '',
  showMenuButton: true,
  showBackButton: false,
  backHref: '/',
  showUndoRedo: false,
  showProgress: false
});

const { t, locale } = useI18n();
const projectStore = useProjectStore();
const uiStore = useUiStore();

const canUndo = computed(() => projectStore.canUndo);
const canRedo = computed(() => projectStore.canRedo);
const progressValue = computed(() => uiStore.progressPercent);
const currentLocale = computed(() => locale.value);
const displayBackButton = computed(() => props.showBackButton || props.backHref !== '/');

function handleUndo() {
  projectStore.undo();
}

function handleRedo() {
  projectStore.redo();
}

function changeLanguage(lang: 'de' | 'en') {
  if (lang !== locale.value) {
    setLocale(lang);
  }
}
</script>

<style scoped>
ion-toolbar {
  --background: var(--kalk-surface);
  --color: var(--kalk-gray-900);
  --border-color: var(--kalk-border);
  --border-width: 0 0 1px 0;
}

.header-title {
  display: flex;
  flex-direction: column;
  justify-content: center;
  min-width: 0;
  padding: 0 var(--kalk-space-2);
  line-height: 1.2;
}

.header-eyebrow {
  font-size: var(--kalk-text-2xs);
  font-weight: 600;
  letter-spacing: var(--kalk-tracking-caps);
  text-transform: uppercase;
  color: var(--kalk-gray-500);
}

.header-text {
  font-size: var(--kalk-text-base);
  font-weight: 600;
  color: var(--kalk-gray-900);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

ion-back-button {
  --color: var(--kalk-gray-700);
  --icon-font-size: 20px;
  --border-radius: var(--kalk-radius-md);
}

ion-menu-button {
  --color: var(--kalk-gray-700);
}

.header-actions {
  gap: var(--kalk-space-2);
}

.tool-group {
  display: inline-flex;
  border: 1px solid var(--kalk-border);
  border-radius: var(--kalk-radius-md);
  overflow: hidden;
}

.tool-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 34px;
  height: 32px;
  color: var(--kalk-gray-700);
  background: var(--kalk-surface);
  border: 0;
  cursor: pointer;
  font-size: 16px;
}

.tool-btn + .tool-btn {
  border-left: 1px solid var(--kalk-border);
}

.tool-btn:hover:not(:disabled) {
  background: var(--kalk-gray-50);
  color: var(--kalk-gray-900);
}

.tool-btn:disabled {
  color: var(--kalk-gray-300);
  cursor: default;
}

.lang-switch {
  display: inline-flex;
  padding: 2px;
  background: var(--kalk-gray-100);
  border-radius: var(--kalk-radius-md);
}

.lang-switch button {
  height: 28px;
  min-width: 32px;
  padding: 0 8px;
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-2xs);
  font-weight: 600;
  letter-spacing: 0.06em;
  color: var(--kalk-gray-500);
  background: transparent;
  border: 0;
  border-radius: 2px;
  cursor: pointer;
}

.lang-switch button.active {
  background: var(--kalk-surface);
  color: var(--kalk-navy-900);
  box-shadow: 0 1px 2px rgba(11, 31, 54, 0.12);
}

.kalk-progress {
  --background: var(--kalk-gray-100);
  --progress-background: var(--kalk-navy-600);
  height: 2px;
}
</style>
