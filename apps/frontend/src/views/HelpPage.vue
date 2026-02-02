<template>
  <ion-page>
    <AppHeader :title="t('help.title')" />

    <ion-content :fullscreen="true" class="ion-padding">
      <div class="help-page">
        <KalkCard :title="t('help.gettingStarted')">
          <div class="help-content">
            <p>{{ t('help.welcome') }}</p>

            <h4>{{ t('help.howToStart') }}</h4>
            <ol>
              <li>{{ t('help.step1') }}</li>
              <li>{{ t('help.step2') }}</li>
              <li>{{ t('help.step3') }}</li>
              <li>{{ t('help.step4') }}</li>
            </ol>
          </div>
        </KalkCard>

        <KalkCard :title="t('help.glossary')">
          <ion-accordion-group>
            <ion-accordion v-for="entry in glossaryEntries" :key="entry.id">
              <ion-item slot="header">
                <ion-label>{{ t(`helpTexts.${entry.id}.title`) }}</ion-label>
              </ion-item>
              <div slot="content" class="accordion-content">
                {{ t(`helpTexts.${entry.id}.summary`) }}
              </div>
            </ion-accordion>
          </ion-accordion-group>
        </KalkCard>

        <KalkCard :title="t('help.faq')">
          <ion-accordion-group>
            <ion-accordion v-for="entry in faqEntries" :key="entry.id">
              <ion-item slot="header">
                <ion-label>{{ t(`helpTexts.${entry.id}.title`) }}</ion-label>
              </ion-item>
              <div slot="content" class="accordion-content" v-html="t(`helpTexts.${entry.id}.detail`)">
              </div>
            </ion-accordion>
          </ion-accordion-group>
        </KalkCard>

        <KalkCard :title="t('help.contact')">
          <div class="help-content">
            <p>{{ t('help.contactText') }}</p>
            <p><strong>E-Mail:</strong> {{ t('help.contactEmail') }}</p>
          </div>
        </KalkCard>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import {
  IonPage,
  IonContent,
  IonAccordionGroup,
  IonAccordion,
  IonItem,
  IonLabel
} from '@ionic/vue';
import { AppHeader, KalkCard } from '@/components';
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
.help-page {
  max-width: 800px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-6);
}

.help-content {
  line-height: 1.6;
}

.help-content h4 {
  margin-top: var(--kalk-space-6);
  margin-bottom: var(--kalk-space-2);
  color: var(--kalk-gray-900);
}

.help-content ol {
  padding-left: var(--kalk-space-6);
  margin: var(--kalk-space-4) 0;
}

.help-content li {
  margin-bottom: var(--kalk-space-2);
}

.accordion-content {
  padding: var(--kalk-space-4) var(--kalk-space-6);
  color: var(--kalk-gray-500);
  line-height: 1.6;
}
</style>
