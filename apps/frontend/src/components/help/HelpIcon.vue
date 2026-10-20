<template>
  <button
    type="button"
    class="help-icon-btn"
    @click.stop="openHelp"
    :aria-label="`Hilfe: ${helpKey}`"
  >
    <svg width="16" height="16" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
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
  width: 16px;
  height: 16px;
  padding: 0;
  margin-left: 2px;
  background: none;
  border: none;
  border-radius: var(--kalk-radius-sm);
  color: var(--kalk-gray-400);
  cursor: pointer;
  transition: color 0.12s;
  vertical-align: -3px;
  flex-shrink: 0;
}

.help-icon-btn svg {
  width: 16px;
  height: 16px;
  display: block;
}

.help-icon-btn:hover {
  color: var(--kalk-navy-600);
}

.help-icon-btn:focus-visible {
  outline: 2px solid var(--kalk-navy-500);
  outline-offset: 1px;
}
</style>
