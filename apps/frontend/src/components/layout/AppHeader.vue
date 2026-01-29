<template>
  <ion-header class="kalk-header">
    <ion-toolbar>
      <ion-buttons slot="start">
        <ion-menu-button v-if="showMenuButton" />
        <ion-back-button v-if="showBackButton" :default-href="backHref" />
      </ion-buttons>

      <ion-title>
        <slot name="title">{{ title }}</slot>
      </ion-title>

      <ion-buttons slot="end">
        <slot name="actions" />

        <!-- Undo/Redo buttons when in wizard -->
        <template v-if="showUndoRedo">
          <ion-button
            :disabled="!canUndo"
            @click="handleUndo"
            :title="t('common.undo')"
          >
            <ion-icon slot="icon-only" :icon="arrowUndoOutline" />
          </ion-button>
          <ion-button
            :disabled="!canRedo"
            @click="handleRedo"
            :title="t('common.redo')"
          >
            <ion-icon slot="icon-only" :icon="arrowRedoOutline" />
          </ion-button>
        </template>

        <!-- Language switcher -->
        <ion-button @click="toggleLanguage">
          <ion-icon slot="icon-only" :icon="languageOutline" />
        </ion-button>
      </ion-buttons>
    </ion-toolbar>

    <!-- Progress bar for wizard -->
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
  IonTitle,
  IonButtons,
  IonButton,
  IonIcon,
  IonMenuButton,
  IonBackButton,
  IonProgressBar
} from '@ionic/vue';
import { arrowUndoOutline, arrowRedoOutline, languageOutline } from 'ionicons/icons';
import { useProjectStore } from '@/stores/projectStore';
import { useUiStore } from '@/stores/uiStore';
import { setLocale, getLocale } from '@/i18n';

const props = withDefaults(defineProps<{
  title?: string;
  showMenuButton?: boolean;
  showBackButton?: boolean;
  backHref?: string;
  showUndoRedo?: boolean;
  showProgress?: boolean;
}>(), {
  title: 'Kalkimo',
  showMenuButton: true,
  showBackButton: false,
  backHref: '/',
  showUndoRedo: false,
  showProgress: false
});

const { t } = useI18n();
const projectStore = useProjectStore();
const uiStore = useUiStore();

const canUndo = computed(() => projectStore.canUndo);
const canRedo = computed(() => projectStore.canRedo);
const progressValue = computed(() => uiStore.progressPercent);

function handleUndo() {
  projectStore.undo();
}

function handleRedo() {
  projectStore.redo();
}

function toggleLanguage() {
  const current = getLocale();
  setLocale(current === 'de' ? 'en' : 'de');
}
</script>

<style scoped>
.kalk-header {
  --background: var(--kalk-primary-900);
  --color: #ffffff;
}

ion-toolbar {
  --background: var(--kalk-primary-900);
  --color: #ffffff;
}

ion-title {
  font-family: var(--kalk-font-family);
  font-weight: 600;
}

.kalk-progress {
  --background: var(--kalk-primary-700);
  --progress-background: var(--kalk-accent-500);
}
</style>
