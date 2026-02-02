<template>
  <button
    type="button"
    class="help-icon-btn"
    @click.stop="openHelp"
    :aria-label="`Hilfe: ${helpKey}`"
  >
    <svg width="14" height="14" viewBox="0 0 20 20" fill="currentColor">
      <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-8-3a1 1 0 00-.867.5 1 1 0 11-1.731-1A3 3 0 0113 8a3.001 3.001 0 01-2 2.83V11a1 1 0 11-2 0v-1a1 1 0 011-1 1 1 0 100-2zm0 8a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
    </svg>
  </button>
</template>

<script setup lang="ts">
import { useUiStore } from '@/stores/uiStore';

const props = defineProps<{
  helpKey: string;
}>();

const uiStore = useUiStore();

function openHelp() {
  uiStore.openHelp(props.helpKey);
}
</script>

<style scoped>
.help-icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  padding: 0;
  background: none;
  border: none;
  color: var(--kalk-gray-400);
  cursor: pointer;
  border-radius: 50%;
  transition: color 0.15s, background-color 0.15s;
  vertical-align: middle;
  flex-shrink: 0;
}

.help-icon-btn:hover {
  color: var(--kalk-accent-600);
  background: var(--kalk-accent-50);
}
</style>
