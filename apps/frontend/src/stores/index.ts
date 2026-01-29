import { createPinia } from 'pinia';

export const pinia = createPinia();

export * from './projectStore';
export * from './uiStore';
export * from './authStore';
