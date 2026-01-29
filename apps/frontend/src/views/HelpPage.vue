<template>
  <ion-page>
    <AppHeader :title="t('help.title')" />

    <ion-content :fullscreen="true" class="ion-padding">
      <div class="help-page">
        <KalkCard title="Erste Schritte">
          <div class="help-content">
            <p>
              Willkommen bei Kalkimo Planner, dem professionellen Immobilien-Investitionsrechner.
            </p>

            <h4>So starten Sie:</h4>
            <ol>
              <li>Klicken Sie auf "Neues Projekt" um ein Investitionsprojekt anzulegen</li>
              <li>Folgen Sie dem Assistenten durch alle Eingabeschritte</li>
              <li>Sehen Sie Ihre Kennzahlen und Cashflow-Prognosen</li>
              <li>Exportieren Sie Berichte für Ihre Bank oder Steuerberater</li>
            </ol>
          </div>
        </KalkCard>

        <KalkCard :title="t('help.glossary')">
          <ion-accordion-group>
            <ion-accordion v-for="term in glossaryTerms" :key="term.key">
              <ion-item slot="header">
                <ion-label>{{ term.title }}</ion-label>
              </ion-item>
              <div slot="content" class="accordion-content">
                {{ term.description }}
              </div>
            </ion-accordion>
          </ion-accordion-group>
        </KalkCard>

        <KalkCard :title="t('help.faq')">
          <ion-accordion-group>
            <ion-accordion v-for="faq in faqs" :key="faq.question">
              <ion-item slot="header">
                <ion-label>{{ faq.question }}</ion-label>
              </ion-item>
              <div slot="content" class="accordion-content">
                {{ faq.answer }}
              </div>
            </ion-accordion>
          </ion-accordion-group>
        </KalkCard>

        <KalkCard :title="t('help.contact')">
          <div class="help-content">
            <p>Bei Fragen oder Problemen kontaktieren Sie uns:</p>
            <p><strong>E-Mail:</strong> support@kalkimo.de</p>
          </div>
        </KalkCard>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
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

const { t } = useI18n();

const glossaryTerms = [
  {
    key: 'irr',
    title: 'IRR (Interner Zinsfuß)',
    description: 'Der interne Zinsfuß ist die Rendite, bei der der Kapitalwert aller Cashflows gleich Null ist. Er zeigt die effektive Verzinsung Ihres eingesetzten Kapitals über die Haltedauer.'
  },
  {
    key: 'npv',
    title: 'NPV (Kapitalwert)',
    description: 'Der Kapitalwert ist die Summe aller abgezinsten zukünftigen Cashflows minus der Anfangsinvestition. Ein positiver NPV bedeutet, dass die Investition rentabel ist.'
  },
  {
    key: 'dscr',
    title: 'DSCR (Schuldendienstdeckung)',
    description: 'Debt Service Coverage Ratio - Verhältnis zwischen verfügbarem Cashflow und Schuldendienst (Zins + Tilgung). Banken fordern oft einen DSCR von mindestens 1,2.'
  },
  {
    key: 'ltv',
    title: 'LTV (Beleihungsauslauf)',
    description: 'Loan-to-Value - Verhältnis zwischen Darlehenssumme und Immobilienwert. Je niedriger, desto sicherer für die Bank.'
  },
  {
    key: 'afa',
    title: 'AfA (Absetzung für Abnutzung)',
    description: 'Steuerliche Abschreibung des Gebäudewerts über die Nutzungsdauer. Der Satz hängt vom Baujahr ab: 2% (ab 1925), 2,5% (vor 1925), 3% (Neubau ab 2023).'
  },
  {
    key: 'mea',
    title: 'MEA (Miteigentumsanteil)',
    description: 'Bei Eigentumswohnungen der Anteil am Gemeinschaftseigentum, meist in Tausendstel angegeben. Bestimmt den Kostenanteil an Gemeinschaftsausgaben.'
  }
];

const faqs = [
  {
    question: 'Wie wird die AfA berechnet?',
    answer: 'Die AfA-Bemessungsgrundlage ist der Gebäudeanteil des Kaufpreises (ohne Grundstück). Der Grundstückswert wird automatisch abgezogen. Bei Neubauten ab 2023 gilt ein Satz von 3%, für ältere Gebäude 2% bzw. 2,5% vor 1925.'
  },
  {
    question: 'Was ist die 15%-Regel?',
    answer: 'Wenn Modernisierungskosten in den ersten 3 Jahren nach Kauf 15% des Gebäudewerts überschreiten, gelten sie als anschaffungsnahe Herstellungskosten und müssen abgeschrieben werden, statt sofort als Erhaltungsaufwand abzugsfähig zu sein.'
  },
  {
    question: 'Wann ist ein Verkauf steuerfrei?',
    answer: 'Bei Privatpersonen ist der Verkaufsgewinn nach 10 Jahren Haltedauer steuerfrei (§23 EStG). Bei Verkauf innerhalb dieser Frist wird der Gewinn mit dem persönlichen Steuersatz versteuert. Es gibt eine Freigrenze von 1.000€ pro Jahr.'
  },
  {
    question: 'Was ist die §82b-Verteilung?',
    answer: 'Größere Erhaltungsaufwendungen können nach §82b EStDV auf 2-5 Jahre verteilt werden, um Steuerspitzen zu glätten. Dies ist besonders bei größeren Sanierungen sinnvoll.'
  }
];
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
