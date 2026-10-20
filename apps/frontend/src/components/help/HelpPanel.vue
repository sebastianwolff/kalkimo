<template>
  <Teleport to="body">
    <Transition name="help-panel">
      <div v-if="uiStore.isHelpPanelOpen" class="help-panel-overlay" @click.self="uiStore.closeHelp()">
        <div class="help-panel" role="dialog" aria-modal="true" :aria-label="panelTitle">
          <!-- Header -->
          <div class="help-panel-header">
            <h3 class="help-panel-title">{{ panelTitle }}</h3>
            <button type="button" class="help-panel-close" @click="uiStore.closeHelp()" :aria-label="t('common.close')">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" aria-hidden="true">
                <line x1="18" y1="6" x2="6" y2="18" />
                <line x1="6" y1="6" x2="18" y2="18" />
              </svg>
            </button>
          </div>

          <!-- Content -->
          <div class="help-panel-body">
            <!-- Summary -->
            <p class="help-panel-summary">{{ panelSummary }}</p>

            <!-- Detail -->
            <div v-if="panelDetail" class="help-panel-detail" v-html="panelDetail"></div>

            <!-- Related entries -->
            <div v-if="relatedLinks.length > 0" class="help-panel-related">
              <h4 class="help-panel-related-title">{{ t('help.relatedTopics') }}</h4>
              <div class="help-panel-related-links">
                <button
                  v-for="link in relatedLinks"
                  :key="link.id"
                  type="button"
                  class="help-panel-related-link"
                  @click="uiStore.openHelp(link.id)"
                >
                  {{ link.title }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, watch, onMounted, onUnmounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useUiStore } from '@/stores/uiStore';
import { getHelpEntry } from '@/help/helpCatalog';

const { t, te } = useI18n();
const uiStore = useUiStore();

const currentKey = computed(() => uiStore.currentHelpKey);

const panelTitle = computed(() => {
  if (!currentKey.value) return '';
  const key = `helpTexts.${currentKey.value}.title`;
  return te(key) ? t(key) : currentKey.value;
});

const panelSummary = computed(() => {
  if (!currentKey.value) return '';
  const key = `helpTexts.${currentKey.value}.summary`;
  return te(key) ? t(key) : '';
});

const panelDetail = computed(() => {
  if (!currentKey.value) return '';
  const key = `helpTexts.${currentKey.value}.detail`;
  return te(key) ? t(key) : '';
});

const relatedLinks = computed(() => {
  if (!currentKey.value) return [];
  const entry = getHelpEntry(currentKey.value);
  if (!entry?.relatedEntries) return [];

  return entry.relatedEntries
    .map(id => {
      const titleKey = `helpTexts.${id}.title`;
      return {
        id,
        title: te(titleKey) ? t(titleKey) : id
      };
    })
    .filter(link => link.title !== link.id); // Only show links that have actual translations
});

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape' && uiStore.isHelpPanelOpen) {
    uiStore.closeHelp();
  }
}

onMounted(() => {
  document.addEventListener('keydown', handleKeydown);
});

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeydown);
});

// Prevent body scroll when panel is open
watch(() => uiStore.isHelpPanelOpen, (isOpen) => {
  if (isOpen) {
    document.body.style.overflow = 'hidden';
  } else {
    document.body.style.overflow = '';
  }
});
</script>

<style scoped>
.help-panel-overlay {
  position: fixed;
  inset: 0;
  z-index: 1000;
  background: rgba(7, 21, 38, 0.35);
  display: flex;
  justify-content: flex-end;
}

.help-panel {
  width: 400px;
  max-width: 100%;
  height: 100%;
  background: var(--kalk-surface);
  border-left: 1px solid var(--kalk-border-strong);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.help-panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--kalk-space-4);
  min-height: var(--kalk-header-height);
  padding: var(--kalk-space-3) var(--kalk-space-4) var(--kalk-space-3) var(--kalk-space-6);
  border-bottom: 1px solid var(--kalk-border);
  flex-shrink: 0;
}

.help-panel-title {
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-lg);
  font-weight: 600;
  color: var(--kalk-navy-900);
  letter-spacing: 0;
  margin: 0;
}

.help-panel-close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  background: none;
  border: 1px solid transparent;
  border-radius: var(--kalk-radius-md);
  color: var(--kalk-gray-500);
  cursor: pointer;
  flex-shrink: 0;
  transition: color 0.12s, background-color 0.12s, border-color 0.12s;
}

.help-panel-close:hover {
  color: var(--kalk-gray-900);
  background: var(--kalk-gray-50);
  border-color: var(--kalk-border);
}

.help-panel-body {
  flex: 1;
  overflow-y: auto;
  padding: var(--kalk-space-6);
}

.help-panel-summary {
  font-size: var(--kalk-text-base);
  line-height: 1.6;
  color: var(--kalk-gray-800);
  font-weight: 500;
  margin: 0 0 var(--kalk-space-5);
}

.help-panel-detail {
  font-size: var(--kalk-text-base);
  line-height: 1.6;
  color: var(--kalk-gray-700);
  border-top: 1px solid var(--kalk-border);
  padding-top: var(--kalk-space-5);
}

.help-panel-detail :deep(p) {
  color: inherit;
  margin: 0 0 var(--kalk-space-3);
}

.help-panel-detail :deep(b),
.help-panel-detail :deep(strong) {
  color: var(--kalk-gray-900);
  font-weight: 600;
}

.help-panel-detail :deep(ul),
.help-panel-detail :deep(ol) {
  padding-left: var(--kalk-space-5);
  margin: var(--kalk-space-3) 0;
}

.help-panel-detail :deep(li) {
  margin-bottom: var(--kalk-space-1);
}

.help-panel-detail :deep(code) {
  font-family: var(--kalk-font-mono);
  font-size: 0.9em;
  padding: 0 3px;
  background: var(--kalk-gray-100);
  border-radius: var(--kalk-radius-sm);
}

.help-panel-related {
  margin-top: var(--kalk-space-6);
  padding-top: var(--kalk-space-5);
  border-top: 1px solid var(--kalk-border);
}

.help-panel-related-title {
  font-size: var(--kalk-text-2xs);
  font-weight: 600;
  color: var(--kalk-gray-500);
  text-transform: uppercase;
  letter-spacing: var(--kalk-tracking-caps);
  margin: 0 0 var(--kalk-space-3);
}

.help-panel-related-links {
  display: flex;
  flex-wrap: wrap;
  gap: var(--kalk-space-2);
}

.help-panel-related-link {
  display: inline-flex;
  align-items: center;
  padding: 3px 8px;
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-xs);
  font-weight: 500;
  line-height: 1.4;
  color: var(--kalk-gray-700);
  background: var(--kalk-gray-100);
  border: 1px solid var(--kalk-border);
  border-radius: var(--kalk-radius-sm);
  cursor: pointer;
  transition: background-color 0.12s, color 0.12s, border-color 0.12s;
}

.help-panel-related-link:hover {
  background: var(--kalk-navy-50);
  border-color: var(--kalk-navy-200);
  color: var(--kalk-navy-800);
}

/* Transition: dezentes Einschieben, kein Bounce */
.help-panel-enter-active,
.help-panel-leave-active {
  transition: opacity 0.18s ease;
}

.help-panel-enter-active .help-panel,
.help-panel-leave-active .help-panel {
  transition: transform 0.2s ease-out;
}

.help-panel-enter-from,
.help-panel-leave-to {
  opacity: 0;
}

.help-panel-enter-from .help-panel,
.help-panel-leave-to .help-panel {
  transform: translateX(24px);
}

@media (max-width: 480px) {
  .help-panel {
    width: 100%;
    border-left: none;
  }

  .help-panel-header {
    padding-left: var(--kalk-space-4);
  }

  .help-panel-body {
    padding: var(--kalk-space-5) var(--kalk-space-4);
  }
}
</style>
