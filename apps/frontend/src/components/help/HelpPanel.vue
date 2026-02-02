<template>
  <Teleport to="body">
    <Transition name="help-panel">
      <div v-if="uiStore.isHelpPanelOpen" class="help-panel-overlay" @click.self="uiStore.closeHelp()">
        <div class="help-panel" role="dialog" aria-modal="true" :aria-label="panelTitle">
          <!-- Header -->
          <div class="help-panel-header">
            <h3 class="help-panel-title">{{ panelTitle }}</h3>
            <button type="button" class="help-panel-close" @click="uiStore.closeHelp()" :aria-label="t('common.close')">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
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
  background: rgba(0, 0, 0, 0.3);
  display: flex;
  justify-content: flex-end;
}

.help-panel {
  width: 380px;
  max-width: 100%;
  height: 100%;
  background: #ffffff;
  box-shadow: -4px 0 24px rgba(0, 0, 0, 0.12);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.help-panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--kalk-space-5) var(--kalk-space-6);
  border-bottom: 1px solid var(--kalk-gray-100);
  flex-shrink: 0;
}

.help-panel-title {
  font-size: var(--kalk-text-lg);
  font-weight: 600;
  color: var(--kalk-gray-900);
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
  border: none;
  border-radius: var(--kalk-radius-md);
  color: var(--kalk-gray-400);
  cursor: pointer;
  transition: color 0.15s, background-color 0.15s;
}

.help-panel-close:hover {
  color: var(--kalk-gray-600);
  background: var(--kalk-gray-50);
}

.help-panel-body {
  flex: 1;
  overflow-y: auto;
  padding: var(--kalk-space-6);
}

.help-panel-summary {
  font-size: var(--kalk-text-base);
  color: var(--kalk-gray-700);
  line-height: 1.6;
  margin: 0 0 var(--kalk-space-6);
  font-weight: 500;
}

.help-panel-detail {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-600);
  line-height: 1.7;
  border-top: 1px solid var(--kalk-gray-100);
  padding-top: var(--kalk-space-5);
}

.help-panel-detail :deep(b),
.help-panel-detail :deep(strong) {
  color: var(--kalk-gray-800);
  font-weight: 600;
}

.help-panel-detail :deep(ul) {
  padding-left: var(--kalk-space-5);
  margin: var(--kalk-space-3) 0;
}

.help-panel-detail :deep(li) {
  margin-bottom: var(--kalk-space-2);
}

.help-panel-related {
  margin-top: var(--kalk-space-6);
  padding-top: var(--kalk-space-5);
  border-top: 1px solid var(--kalk-gray-100);
}

.help-panel-related-title {
  font-size: var(--kalk-text-xs);
  font-weight: 600;
  color: var(--kalk-gray-500);
  text-transform: uppercase;
  letter-spacing: 0.05em;
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
  padding: var(--kalk-space-1) var(--kalk-space-3);
  font-size: var(--kalk-text-xs);
  font-weight: 500;
  color: var(--kalk-accent-600);
  background: var(--kalk-accent-50);
  border: 1px solid var(--kalk-accent-200);
  border-radius: var(--kalk-radius-full);
  cursor: pointer;
  transition: background-color 0.15s, color 0.15s;
}

.help-panel-related-link:hover {
  background: var(--kalk-accent-100);
  color: var(--kalk-accent-700);
}

/* Transition */
.help-panel-enter-active,
.help-panel-leave-active {
  transition: opacity 0.2s ease;
}

.help-panel-enter-active .help-panel,
.help-panel-leave-active .help-panel {
  transition: transform 0.25s ease;
}

.help-panel-enter-from,
.help-panel-leave-to {
  opacity: 0;
}

.help-panel-enter-from .help-panel,
.help-panel-leave-to .help-panel {
  transform: translateX(100%);
}

/* Mobile */
@media (max-width: 480px) {
  .help-panel {
    width: 100%;
  }
}
</style>
