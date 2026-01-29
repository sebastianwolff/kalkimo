<template>
  <KalkCard :title="t('tax.title')">
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
    />

    <div class="toggle-row">
      <span class="toggle-label">{{ t('tax.isCorporate') }}</span>
      <label class="toggle-switch">
        <input type="checkbox" v-model="isCorporate" />
        <span class="toggle-slider"></span>
      </label>
    </div>

    <!-- Depreciation Section -->
    <div class="section">
      <h4 class="section-label">{{ t('tax.depreciation.title') }}</h4>

      <div class="info-box">
        <div class="info-row">
          <span>{{ t('tax.depreciation.rate') }}:</span>
          <strong>{{ depreciationRate }}%</strong>
        </div>
        <div class="info-row">
          <span>{{ t('tax.depreciation.basis') }}:</span>
          <strong>{{ formatCurrency(depreciationBasis) }}</strong>
        </div>
        <div class="info-row highlight">
          <span>{{ t('tax.depreciation.annual') }}:</span>
          <strong>{{ formatCurrency(annualDepreciation) }}</strong>
        </div>
        <p class="info-hint">{{ t('tax.depreciation.info') }}</p>
      </div>
    </div>

    <!-- Section 82b -->
    <div class="section">
      <h4 class="section-label">{{ t('tax.section82b.title') }}</h4>

      <div class="toggle-row">
        <span class="toggle-label">{{ t('tax.section82b.enabled') }}</span>
        <label class="toggle-switch">
          <input type="checkbox" v-model="use82b" />
          <span class="toggle-slider"></span>
        </label>
      </div>

      <KalkInput
        v-if="use82b"
        v-model="distributionYears"
        :label="t('tax.section82b.years')"
        type="number"
        :min="2"
        :max="5"
        :suffix="t('common.years')"
      />

      <p class="section-hint">{{ t('tax.section82b.info') }}</p>
    </div>

    <!-- Section 23 Info -->
    <div class="section">
      <h4 class="section-label">{{ t('tax.section23.title') }}</h4>

      <div class="info-box">
        <div class="info-row">
          <span>{{ t('tax.section23.holdingPeriod') }}:</span>
          <strong>10 {{ t('common.years') }}</strong>
        </div>
        <p class="info-hint">{{ t('tax.section23.info') }}</p>
      </div>
    </div>
  </KalkCard>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { KalkCard, KalkPercent, KalkInput } from '@/components';
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
.section {
  margin-top: var(--kalk-space-8);
  padding-top: var(--kalk-space-6);
  border-top: 1px solid var(--kalk-gray-200);
}

.section-label {
  font-size: var(--kalk-text-base);
  font-weight: 600;
  color: var(--kalk-gray-900);
  margin: 0 0 var(--kalk-space-4);
}

.toggle-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--kalk-space-3) 0;
  margin-bottom: var(--kalk-space-4);
}

.toggle-label {
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  color: var(--kalk-gray-700);
}

.toggle-switch {
  position: relative;
  display: inline-block;
  width: 44px;
  height: 24px;
  flex-shrink: 0;
}

.toggle-switch input {
  opacity: 0;
  width: 0;
  height: 0;
}

.toggle-slider {
  position: absolute;
  cursor: pointer;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: var(--kalk-gray-300);
  transition: 0.2s;
  border-radius: 24px;
}

.toggle-slider:before {
  position: absolute;
  content: "";
  height: 18px;
  width: 18px;
  left: 3px;
  bottom: 3px;
  background-color: white;
  transition: 0.2s;
  border-radius: 50%;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.toggle-switch input:checked + .toggle-slider {
  background-color: var(--kalk-accent-500);
}

.toggle-switch input:checked + .toggle-slider:before {
  transform: translateX(20px);
}

.toggle-switch input:focus + .toggle-slider {
  box-shadow: 0 0 0 2px var(--kalk-accent-100);
}

.info-box {
  padding: var(--kalk-space-4);
  background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-md);
}

.info-row {
  display: flex;
  justify-content: space-between;
  padding: var(--kalk-space-1) 0;
  font-size: var(--kalk-text-sm);
}

.info-row span {
  color: var(--kalk-gray-600);
}

.info-row strong {
  color: var(--kalk-gray-900);
  font-variant-numeric: tabular-nums;
}

.info-row.highlight {
  margin-top: var(--kalk-space-2);
  padding-top: var(--kalk-space-2);
  border-top: 1px solid var(--kalk-gray-200);
  font-size: var(--kalk-text-lg);
}

.info-row.highlight span {
  color: var(--kalk-accent-600);
}

.info-row.highlight strong {
  color: var(--kalk-accent-600);
}

.info-hint,
.section-hint {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-500);
  margin-top: var(--kalk-space-2);
}
</style>
