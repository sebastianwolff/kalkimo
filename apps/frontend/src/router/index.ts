import { createRouter, createWebHistory } from '@ionic/vue-router';
import { RouteRecordRaw } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    redirect: '/home'
  },
  {
    path: '/home',
    name: 'Home',
    component: () => import('@/views/HomePage.vue')
  },
  {
    path: '/wizard',
    name: 'Wizard',
    component: () => import('@/views/wizard/WizardPage.vue')
  },
  {
    path: '/projects',
    name: 'Projects',
    component: () => import('@/views/ProjectsPage.vue')
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/LoginPage.vue')
  },
  {
    path: '/settings',
    name: 'Settings',
    component: () => import('@/views/SettingsPage.vue')
  },
  {
    path: '/help',
    name: 'Help',
    component: () => import('@/views/HelpPage.vue')
  },
  {
    path: '/admin',
    name: 'Admin',
    component: () => import('@/views/admin/AdminDashboardPage.vue'),
    meta: { requiresAdmin: true }
  },
  {
    path: '/admin/users/:userId',
    name: 'AdminUserDetail',
    component: () => import('@/views/admin/AdminUserDetailPage.vue'),
    meta: { requiresAdmin: true }
  }
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes
});

// Navigation Guard: Admin-Routen nur für Admins zugänglich
router.beforeEach((to) => {
  if (to.meta.requiresAdmin) {
    const authStore = useAuthStore();
    if (!authStore.isAuthenticated || !authStore.isAdmin) {
      return { name: 'Home' };
    }
  }
});

export default router;
