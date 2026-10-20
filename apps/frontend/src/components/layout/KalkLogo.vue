<template>
  <div :class="['kalk-logo', `variant-${variant}`, { compact }]">
    <svg class="logo-mark" viewBox="0 0 32 32" aria-hidden="true">
      <rect x="1" y="1" width="30" height="30" rx="2" class="mark-frame" />
      <path d="M10 8v16" class="mark-stroke" />
      <path d="M22 8l-8.5 8L22 24" class="mark-stroke" />
      <path d="M17 24h5" class="mark-accent" />
    </svg>
    <div v-if="!compact" class="logo-text">
      <span class="logo-name">Kalkimo</span>
      <span class="logo-claim">Investment Planner</span>
    </div>
  </div>
</template>

<script setup lang="ts">
withDefaults(defineProps<{
  variant?: 'light' | 'dark';
  compact?: boolean;
}>(), {
  variant: 'light',
  compact: false
});
</script>

<style scoped>
.kalk-logo {
  display: inline-flex;
  align-items: center;
  gap: 10px;
}

.logo-mark {
  width: 30px;
  height: 30px;
  flex-shrink: 0;
}

.mark-frame {
  fill: var(--kalk-navy-900);
  stroke: var(--kalk-navy-900);
  stroke-width: 1.5;
}

.mark-stroke {
  fill: none;
  stroke: #ffffff;
  stroke-width: 2.4;
  stroke-linecap: square;
}

.mark-accent {
  fill: none;
  stroke: var(--kalk-brass-500);
  stroke-width: 2.4;
}

.variant-dark .mark-frame {
  fill: transparent;
  stroke: rgba(255, 255, 255, 0.35);
}

.logo-text {
  display: flex;
  flex-direction: column;
  line-height: 1.1;
}

.logo-name {
  font-family: var(--kalk-font-serif);
  font-size: 1.1875rem;
  font-weight: 600;
  color: var(--kalk-navy-900);
  letter-spacing: 0.005em;
}

.logo-claim {
  margin-top: 2px;
  font-size: 0.625rem;
  font-weight: 600;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--kalk-gray-500);
}

.variant-dark .logo-name {
  color: #ffffff;
}

.variant-dark .logo-claim {
  color: var(--kalk-brass-300);
}
</style>
