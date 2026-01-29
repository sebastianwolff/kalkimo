<template>
  <ion-page>
    <AppHeader :title="t('nav.settings')" />

    <ion-content :fullscreen="true" class="ion-padding">
      <div class="settings-page">
        <KalkCard title="Sprache / Language">
          <ion-list>
            <ion-item>
              <ion-label>{{ t('common.appName') }}</ion-label>
              <ion-select
                v-model="selectedLocale"
                interface="popover"
                @ionChange="changeLocale"
              >
                <ion-select-option value="de">Deutsch</ion-select-option>
                <ion-select-option value="en">English</ion-select-option>
              </ion-select>
            </ion-item>
          </ion-list>
        </KalkCard>

        <KalkCard title="Konto" v-if="isAuthenticated">
          <ion-list>
            <ion-item>
              <ion-label>
                <h3>E-Mail</h3>
                <p>{{ userEmail }}</p>
              </ion-label>
            </ion-item>
            <ion-item button @click="handleLogout">
              <ion-icon slot="start" :icon="logOutOutline" color="danger" />
              <ion-label color="danger">{{ t('auth.logout') }}</ion-label>
            </ion-item>
          </ion-list>
        </KalkCard>

        <KalkCard title="Über die App">
          <ion-list>
            <ion-item>
              <ion-label>
                <h3>Version</h3>
                <p>1.0.0-dev</p>
              </ion-label>
            </ion-item>
            <ion-item>
              <ion-label>
                <h3>Entwickelt von</h3>
                <p>Kalkimo Team</p>
              </ion-label>
            </ion-item>
          </ion-list>
        </KalkCard>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import {
  IonPage,
  IonContent,
  IonList,
  IonItem,
  IonLabel,
  IonSelect,
  IonSelectOption,
  IonIcon
} from '@ionic/vue';
import { logOutOutline } from 'ionicons/icons';
import { AppHeader, KalkCard } from '@/components';
import { useAuthStore } from '@/stores/authStore';
import { useProjectStore } from '@/stores/projectStore';
import { setLocale, getLocale } from '@/i18n';
import { authApi } from '@/api';

const { t } = useI18n();
const router = useRouter();
const authStore = useAuthStore();
const projectStore = useProjectStore();

const selectedLocale = ref(getLocale());
const isAuthenticated = computed(() => authStore.isAuthenticated);
const userEmail = computed(() => authStore.user?.email || '');

function changeLocale(event: CustomEvent) {
  setLocale(event.detail.value);
}

async function handleLogout() {
  projectStore.clearAll();
  await authApi.logout();
  authStore.logout();
  router.push('/login');
}
</script>

<style scoped>
.settings-page {
  max-width: 600px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-6);
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
</style>
