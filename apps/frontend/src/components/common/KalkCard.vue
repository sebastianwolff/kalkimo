<template>
  <div :class="['kalk-card', { 'has-shadow': shadow }]">
    <div v-if="title || $slots.header" class="card-header">
      <slot name="header">
        <h3 class="card-title">{{ title }}</h3>
        <p v-if="subtitle" class="card-subtitle">{{ subtitle }}</p>
      </slot>
      <div v-if="$slots.actions" class="card-actions">
        <slot name="actions" />
      </div>
    </div>

    <div class="card-content">
      <slot />
    </div>

    <div v-if="$slots.footer" class="card-footer">
      <slot name="footer" />
    </div>
  </div>
</template>

<script setup lang="ts">
withDefaults(defineProps<{
  title?: string;
  subtitle?: string;
  shadow?: boolean;
}>(), {
  shadow: true
});
</script>

<style scoped>
.kalk-card {
  background: var(--kalk-surface);
  border-radius: var(--kalk-radius-lg);
  border: 1px solid var(--kalk-border);
  overflow: hidden;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: var(--kalk-space-4);
  padding: var(--kalk-space-4) var(--kalk-space-5);
  border-bottom: 1px solid var(--kalk-border);
}

.card-title {
  margin: 0;
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-base);
  font-weight: 600;
  color: var(--kalk-gray-900);
  letter-spacing: 0;
}

.card-subtitle {
  margin: 2px 0 0;
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
}

.card-actions {
  display: flex;
  gap: var(--kalk-space-2);
}

.card-content {
  padding: var(--kalk-space-5);
}

.card-footer {
  padding: var(--kalk-space-3) var(--kalk-space-5);
  border-top: 1px solid var(--kalk-border);
  background: var(--kalk-gray-50);
}

@media (max-width: 640px) {
  .card-header { padding: var(--kalk-space-3) var(--kalk-space-4); }
  .card-content { padding: var(--kalk-space-4); }
}
</style>
