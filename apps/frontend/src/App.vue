<template>
  <ion-app>
    <AppMenu />
    <ion-router-outlet id="main-content" />
  </ion-app>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { IonApp, IonRouterOutlet } from '@ionic/vue';
import { AppMenu } from '@/components';
import { useAuthStore } from '@/stores/authStore';
import { useProjectStore } from '@/stores/projectStore';

const authStore = useAuthStore();
const projectStore = useProjectStore();

// Initialize stores from localStorage on app start
onMounted(() => {
  authStore.initFromStorage();
  projectStore.initFromStorage();
});
</script>
