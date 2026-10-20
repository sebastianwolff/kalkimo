<template>
  <KalkCard :title="t('tax.title')">
    <div class="rate-fields">
      <KalkPercent
        v-model="marginalTaxRate"
        :label="t('tax.marginalRate')"
        :min="0"
        :max="50"
        help-key="tax.marginalRate"
      />

      <KalkPercent
        v-model="churchTax"
        :label="t('tax.churchTax')"
        :min="0"
        :max="9"
        :hint="'Optional - 8% oder 9% je nach Bundesland'"
        help-key="tax.churchTax"
      />
    </div>

    <label class="toggle-row">
      <span class="toggle-label">{{ t('tax.isCorporate') }} <HelpIcon help-key="tax.isCorporate" /></span>
      <span class="toggle-switch">
        <input type="checkbox" v-model="isCorporate" />
        <span class="toggle-slider"></span>
      </span>
    </label>

    <!-- Depreciation Section -->
    <div class="tax-section">
      <h4 class="section-title">{{ t('tax.depreciation.title') }} <HelpIcon help-key="tax.depreciation" /></h4>

      <div class="summary-box statement">
        <div class="data-row">
          <span class="data-label">{{ t('tax.depreciation.rate') }}</span>
          <span class="data-value">{{ depreciationRate }}%</span>
        </div>
        <div class="data-row">
          <span class="data-label">{{ t('tax.depreciation.basis') }}</span>
          <span class="data-value">{{ formatCurrency(depreciationBasis) }}</span>
        </div>
        <div class="data-row total">
          <span class="data-label">{{ t('tax.depreciation.annual') }}</span>
          <span class="data-value">{{ formatCurrency(annualDepreciation) }}</span>
        </div>
      </div>
      <p class="section-hint">{{ t('tax.depreciation.info') }}</p>
    </div>

    <!-- Section 82b -->
    <div class="tax-section">
      <h4 class="section-title">{{ t('tax.section82b.title') }} <HelpIcon help-key="tax.section82b" /></h4>

      <label class="toggle-row">
        <span class="toggle-label">{{ t('tax.section82b.enabled') }}</span>
        <span class="toggle-switch">
          <input type="checkbox" v-model="use82b" />
          <span class="toggle-slider"></span>
        </span>
      </label>

      <div v-if="use82b" class="years-field">
        <KalkInput
          v-model="distributionYears"
          :label="t('tax.section82b.years')"
          type="number"
          :min="2"
          :max="5"
          :suffix="t('common.years')"
          help-key="tax.distributionYears"
        />
      </div>

      <p class="section-hint">{{ t('tax.section82b.info') }}</p>
    </div>

    <!-- Section 23 Info -->
    <div class="tax-section">
      <h4 class="section-title">{{ t('tax.section23.title') }} <HelpIcon help-key="tax.section23" /></h4>

      <div class="summary-box statement">
        <div class="data-row">
          <span class="data-label">{{ t('tax.section23.holdingPeriod') }}</span>
          <span class="data-value">10 {{ t('common.years') }}</span>
        </div>
      </div>
      <div class="callout info section-callout">
        <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
        </svg>
        <p>{{ t('tax.section23.info') }}</p>
      </div>
    </div>
  </KalkCard>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { KalkCard, KalkPercent, KalkInput, HelpIcon } from '@/components';
import { useProjectStore } from '@/stores/projectStore';

const emit = defineEmits<{
  'validation-change': [valid: boolean];
}>();

const { t, locale } = useI18n();
const projectStore = useProjectStore();

const marginalTaxRate = ref<number>(42);
const churchTax = ref<number | undefined>();
const isCorporate = ref(false);
const use82b = ref(false);
const distributionYears = ref<number>(5);

const currency = computed(() => projectStore.currentProject?.currency || 'EUR');

// Calculate depreciation based on construction year
const depreciationRate = computed(() => {
  const year = projectStore.currentProject?.property.constructionYear || 1990;
  if (year >= 2023) return 3.0;  // New buildings from 2023
  if (year >= 1925) return 2.0;  // Standard rate
  return 2.5; // Pre-1925 buildings
});

const depreciationBasis = computed(() => {
  const purchase = projectStore.currentProject?.purchase;
  if (!purchase) return 0;

  const totalPrice = purchase.purchasePrice.amount +
    purchase.costs.reduce((sum, c) => sum + c.amount.amount, 0);

  // Subtract land value (not depreciable)
  const landPercent = purchase.landValuePercent / 100;
  return totalPrice * (1 - landPercent);
});

const annualDepreciation = computed(() =>
  depreciationBasis.value * (depreciationRate.value / 100)
);

function formatCurrency(value: number): string {
  return new Intl.NumberFormat(locale.value, {
    style: 'currency',
    currency: currency.value
  }).format(value);
}

onMounted(() => {
  const tax = projectStore.currentProject?.taxProfile;
  if (tax) {
    marginalTaxRate.value = tax.marginalTaxRatePercent;
    churchTax.value = tax.churchTaxPercent;
    isCorporate.value = tax.isCorporate;
    use82b.value = tax.use82bDistribution;
    distributionYears.value = tax.distributionYears82b;
  }
});

const isValid = computed(() => marginalTaxRate.value >= 0 && marginalTaxRate.value <= 50);

watch(isValid, (valid) => {
  emit('validation-change', valid);
}, { immediate: true });

watch(
  [marginalTaxRate, churchTax, isCorporate, use82b, distributionYears],
  () => {
    if (!projectStore.currentProject) return;

    projectStore.updateProject({
      taxProfile: {
        marginalTaxRatePercent: marginalTaxRate.value,
        churchTaxPercent: churchTax.value,
        isCorporate: isCorporate.value,
        use82bDistribution: use82b.value,
        distributionYears82b: distributionYears.value
      }
    });
  }
);
</script>

<style scoped>
.rate-fields {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  column-gap: var(--kalk-space-4);
}

.tax-section {
  margin-top: var(--kalk-space-6);
  padding-top: var(--kalk-space-5);
  border-top: 1px solid var(--kalk-border);
}

.tax-section > .section-title {
  display: flex;
  align-items: center;
  gap: 2px;
  /* Gesetzesverweise (z.B. "§82b EStDV") nicht in Versalien setzen */
  text-transform: none;
  letter-spacing: 0;
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-900);
  margin: 0 0 var(--kalk-space-3);
}

.toggle-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: var(--kalk-space-4);
  padding: var(--kalk-space-3) var(--kalk-space-4);
  margin-bottom: var(--kalk-space-4);
  border: 1px solid var(--kalk-border);
  border-radius: var(--kalk-radius-md);
  background: var(--kalk-surface);
  cursor: pointer;
}

.toggle-row:hover {
  border-color: var(--kalk-border-strong);
}

.toggle-label {
  display: inline-flex;
  align-items: center;
  gap: 2px;
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  color: var(--kalk-gray-800);
}

/* Schalter: rechteckig, zurueckhaltend */
.toggle-switch {
  position: relative;
  display: inline-block;
  width: 36px;
  height: 20px;
  flex-shrink: 0;
}

.toggle-switch input {
  position: absolute;
  opacity: 0;
  width: 0;
  height: 0;
}

.toggle-slider {
  position: absolute;
  inset: 0;
  background-color: var(--kalk-gray-300);
  border-radius: var(--kalk-radius-sm);
  transition: background-color 0.12s;
}

.toggle-slider::before {
  position: absolute;
  content: "";
  width: 14px;
  height: 14px;
  left: 3px;
  top: 3px;
  background-color: var(--kalk-surface);
  border-radius: 1px;
  transition: transform 0.12s;
}

.toggle-switch input:checked + .toggle-slider {
  background-color: var(--kalk-navy-800);
}

.toggle-switch input:checked + .toggle-slider::before {
  transform: translateX(16px);
}

.toggle-switch input:focus-visible + .toggle-slider {
  box-shadow: 0 0 0 3px var(--kalk-ring-color);
}

.statement {
  margin-top: 0;
  background: var(--kalk-surface);
}

.statement .data-row.total .data-value {
  font-size: var(--kalk-text-lg);
}

.years-field {
  max-width: 280px;
}

.section-hint {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-500);
  margin-top: var(--kalk-space-2);
}

.section-callout {
  margin-top: var(--kalk-space-3);
}

@media (max-width: 600px) {
  .rate-fields {
    grid-template-columns: minmax(0, 1fr);
  }

  .years-field {
    max-width: none;
  }
}
</style>
