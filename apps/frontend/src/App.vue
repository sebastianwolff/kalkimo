<template>
  <ion-app>
    <ion-split-pane content-id="main-content" when="lg">
      <AppMenu />
      <ion-router-outlet id="main-content" />
    </ion-split-pane>
  </ion-app>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { IonApp, IonRouterOutlet, IonSplitPane } from '@ionic/vue';
import { AppMenu } from '@/components';
import { useAuthStore } from '@/stores/authStore';
import { useProjectStore } from '@/stores/projectStore';

const authStore = useAuthStore();
const projectStore = useProjectStore();

// Initialize stores from localStorage on app start
onMounted(async () => {
  authStore.initFromStorage();
  projectStore.initFromStorage();

  // Angemeldet, aber noch Gast-Projekte im Browser (z. B. Login vor diesem Fix):
  // ins Konto übernehmen, damit sie auf Dashboard und Projektliste erscheinen
  if (authStore.isAuthenticated && projectStore.hasGuestProjects()) {
    await projectStore.adoptGuestProjects();
  }
});
</script>

<style>
ion-split-pane {
  --side-width: var(--kalk-sidebar-width);
  --side-min-width: var(--kalk-sidebar-width);
  --side-max-width: var(--kalk-sidebar-width);
  --border: 0;
}
</style>
