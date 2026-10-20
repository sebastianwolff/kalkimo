<template>
  <ion-page>
    <AppHeader :title="t('help.title')" />

    <ion-content>
      <div class="page-content">
        <header class="page-head">
          <div>
            <span class="eyebrow">{{ t('helpPage.eyebrow') }}</span>
            <h1 class="page-title">{{ t('help.title') }}</h1>
            <p class="page-subtitle">{{ t('helpPage.subtitle') }}</p>
          </div>
        </header>

        <div class="help-grid">
          <div class="main-col">
            <section class="panel">
              <div class="panel-header">
                <h2 class="panel-title">{{ t('help.gettingStarted') }}</h2>
              </div>
              <div class="panel-body">
                <p class="intro">{{ t('help.welcome') }}</p>
                <ol class="step-list">
                  <li v-for="(key, i) in ['step1', 'step2', 'step3', 'step4']" :key="key">
                    <span class="step-index">{{ String(i + 1).padStart(2, '0') }}</span>
                    <span>{{ t(`help.${key}`) }}</span>
                  </li>
                </ol>
              </div>
            </section>

            <section class="panel">
              <div class="panel-header">
                <h2 class="panel-title">{{ t('help.faq') }}</h2>
              </div>
              <details v-for="entry in faqEntries" :key="entry.id" class="disclosure">
                <summary>
                  <span>{{ t(`helpTexts.${entry.id}.title`) }}</span>
                  <ion-icon :icon="chevronDownOutline" class="disclosure-icon" />
                </summary>
                <div class="disclosure-body" v-html="t(`helpTexts.${entry.id}.detail`)"></div>
              </details>
            </section>

            <section class="panel">
              <div class="panel-header">
                <h2 class="panel-title">{{ t('help.glossary') }}</h2>
              </div>
              <details v-for="entry in glossaryEntries" :key="entry.id" class="disclosure">
                <summary>
                  <span>{{ t(`helpTexts.${entry.id}.title`) }}</span>
                  <ion-icon :icon="chevronDownOutline" class="disclosure-icon" />
                </summary>
                <div class="disclosure-body">{{ t(`helpTexts.${entry.id}.summary`) }}</div>
              </details>
            </section>
          </div>

          <aside class="side-col">
            <section class="panel">
              <div class="panel-header">
                <h2 class="panel-title">{{ t('help.contact') }}</h2>
              </div>
              <div class="panel-body contact">
                <p>{{ t('help.contactText') }}</p>
                <a :href="`mailto:${t('help.contactEmail')}`" class="contact-link">
                  <ion-icon :icon="mailOutline" />
                  {{ t('help.contactEmail') }}
                </a>
              </div>
            </section>
          </aside>
        </div>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { IonPage, IonContent, IonIcon } from '@ionic/vue';
import { chevronDownOutline, mailOutline } from 'ionicons/icons';
import { AppHeader } from '@/components';
import { getEntriesByCategory } from '@/help/helpCatalog';

const { t } = useI18n();

const glossaryEntries = computed(() => getEntriesByCategory('glossary'));

const faqEntries = computed(() => [
  { id: 'tax.depreciation' },
  { id: 'summary.tax.rule15' },
  { id: 'tax.section23' },
  { id: 'tax.section82b' }
]);
</script>

<style scoped>
.help-grid {
  display: grid;
  grid-template-columns: minmax(0, 2fr) minmax(0, 1fr);
  gap: var(--kalk-space-6);
  align-items: start;
}

.main-col,
.side-col {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-6);
}

@media (max-width: 960px) {
  .help-grid {
    grid-template-columns: 1fr;
  }
}

.intro {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-700);
}

.step-list {
  list-style: none;
  margin: var(--kalk-space-4) 0 0;
  padding: 0;
}

.step-list li {
  display: flex;
  gap: var(--kalk-space-3);
  padding: var(--kalk-space-2) 0;
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-800);
  border-top: 1px solid var(--kalk-gray-100);
}

.step-index {
  flex-shrink: 0;
  width: 24px;
  font-weight: 600;
  color: var(--kalk-brass-600);
}

.disclosure {
  border-bottom: 1px solid var(--kalk-gray-100);
}

.disclosure:last-child {
  border-bottom: 0;
}

.disclosure summary {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--kalk-space-3);
  padding: var(--kalk-space-3) var(--kalk-space-5);
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  color: var(--kalk-gray-900);
  cursor: pointer;
  list-style: none;
}

.disclosure summary::-webkit-details-marker {
  display: none;
}

.disclosure summary:hover {
  background: var(--kalk-gray-50);
}

.disclosure-icon {
  flex-shrink: 0;
  font-size: 14px;
  color: var(--kalk-gray-400);
  transition: transform 0.15s;
}

.disclosure[open] .disclosure-icon {
  transform: rotate(180deg);
}

.disclosure[open] summary {
  color: var(--kalk-navy-800);
}

.disclosure-body {
  padding: 0 var(--kalk-space-5) var(--kalk-space-4);
  font-size: var(--kalk-text-sm);
  line-height: 1.65;
  color: var(--kalk-gray-700);
}

.disclosure-body :deep(p) {
  margin: 0 0 var(--kalk-space-2);
  color: inherit;
}

.disclosure-body :deep(ul),
.disclosure-body :deep(ol) {
  padding-left: var(--kalk-space-5);
  margin: var(--kalk-space-2) 0;
}

.contact p {
  font-size: var(--kalk-text-sm);
}

.contact-link {
  display: inline-flex;
  align-items: center;
  gap: var(--kalk-space-2);
  margin-top: var(--kalk-space-3);
  font-size: var(--kalk-text-sm);
  font-weight: 500;
}
</style>
