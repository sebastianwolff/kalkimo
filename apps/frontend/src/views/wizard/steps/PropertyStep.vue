<template>
  <KalkCard :title="t('property.title')">
    <KalkSelect
      v-model="propertyType"
      :label="t('property.type')"
      :options="propertyTypeOptions"
      required
      help-key="property.type"
    />

    <KalkInput
      v-model="constructionYear"
      :label="t('property.constructionYear')"
      type="number"
      :min="1800"
      :max="2030"
      required
      help-key="property.constructionYear"
    />

    <KalkSelect
      v-model="condition"
      :label="t('property.condition')"
      :options="conditionOptions"
      required
    />

    <div class="area-section">
      <KalkInput
        v-model="totalArea"
        :label="t('property.totalArea')"
        type="number"
        :suffix="t('common.sqm')"
        :min="1"
        required
      />

      <KalkInput
        v-model="livingArea"
        :label="t('property.livingArea')"
        type="number"
        :suffix="t('common.sqm')"
        :min="1"
        required
      />

      <KalkInput
        v-model="landArea"
        :label="t('property.landArea')"
        type="number"
        :suffix="t('common.sqm')"
        :min="0"
        :hint="propertyType === 'Condominium' ? '' : undefined"
      />
    </div>

    <KalkInput
      v-if="showUnitCount"
      v-model="unitCount"
      :label="t('property.unitCount')"
      type="number"
      :min="1"
      :max="500"
    />

    <!-- WEG Configuration for Condominiums -->
    <div v-if="propertyType === 'Condominium'" class="weg-section">
      <h4 class="section-label">{{ t('property.weg.title') }}</h4>

      <KalkInput
        v-model="meaPerMille"
        :label="t('property.weg.meaPerMille')"
        type="number"
        :min="1"
        :max="10000"
        suffix="\u2030"
        help-key="weg.mea"
      />

      <KalkInput
        v-model="totalMea"
        :label="t('property.weg.totalMea')"
        type="number"
        :min="1000"
        :max="10000"
        suffix="\u2030"
      />

      <KalkCurrency
        v-model="reserveBalance"
        :label="t('property.weg.reserveBalance')"
        :currency="currency"
      />
    </div>
  </KalkCard>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { KalkCard, KalkInput, KalkSelect, KalkCurrency } from '@/components';
import { useProjectStore } from '@/stores/projectStore';
import type { PropertyType, Condition } from '@/stores/types';

const emit = defineEmits<{
  'validation-change': [valid: boolean];
}>();

const { t } = useI18n();
const projectStore = useProjectStore();

// Form values
const propertyType = ref<PropertyType>('SingleFamily');
const constructionYear = ref<number>(1990);
const condition = ref<Condition>('Good');
const totalArea = ref<number>(150);
const livingArea = ref<number>(120);
const landArea = ref<number | undefined>();
const unitCount = ref<number>(1);

// WEG fields
const meaPerMille = ref<number>(100);
const totalMea = ref<number>(1000);
const reserveBalance = ref<number | undefined>();

const currency = computed(() => projectStore.currentProject?.currency || 'EUR');

const propertyTypeOptions = computed(() => [
  { value: 'SingleFamily', label: t('property.types.SingleFamily') },
  { value: 'MultiFamily', label: t('property.types.MultiFamily') },
  { value: 'Condominium', label: t('property.types.Condominium') },
  { value: 'Commercial', label: t('property.types.Commercial') },
  { value: 'Mixed', label: t('property.types.Mixed') }
]);

const conditionOptions = computed(() => [
  { value: 'New', label: t('property.conditions.New') },
  { value: 'Good', label: t('property.conditions.Good') },
  { value: 'Fair', label: t('property.conditions.Fair') },
  { value: 'Poor', label: t('property.conditions.Poor') },
  { value: 'NeedsRenovation', label: t('property.conditions.NeedsRenovation') }
]);

const showUnitCount = computed(() =>
  ['MultiFamily', 'Mixed'].includes(propertyType.value)
);

// Initialize from store
onMounted(() => {
  const property = projectStore.currentProject?.property;
  if (property) {
    propertyType.value = property.type;
    constructionYear.value = property.constructionYear;
    condition.value = property.overallCondition;
    totalArea.value = property.totalArea;
    livingArea.value = property.livingArea;
    landArea.value = property.landArea;
    unitCount.value = property.unitCount;

    if (property.wegConfiguration) {
      meaPerMille.value = property.wegConfiguration.meaPerMille;
      totalMea.value = property.wegConfiguration.totalMeaPerMille;
      reserveBalance.value = property.wegConfiguration.currentReserveBalance?.amount;
    }
  }
});

// Validation
const isValid = computed(() => {
  return (
    constructionYear.value >= 1800 &&
    constructionYear.value <= 2030 &&
    totalArea.value > 0 &&
    livingArea.value > 0 &&
    livingArea.value <= totalArea.value
  );
});

watch(isValid, (valid) => {
  emit('validation-change', valid);
}, { immediate: true });

// Sync to store
watch(
  [propertyType, constructionYear, condition, totalArea, livingArea, landArea, unitCount, meaPerMille, totalMea, reserveBalance],
  () => {
    if (!projectStore.currentProject) return;

    const wegConfiguration = propertyType.value === 'Condominium' ? {
      meaPerMille: meaPerMille.value,
      totalMeaPerMille: totalMea.value,
      currentReserveBalance: reserveBalance.value ? {
        amount: reserveBalance.value,
        currency: currency.value
      } : undefined,
      hausgeld: projectStore.currentProject.property.wegConfiguration?.hausgeld || {
        monthlyTotal: { amount: 0, currency: currency.value },
        monthlyReserveContribution: { amount: 0, currency: currency.value },
        annualIncreasePercent: 2
      },
      sonderumlagen: [],
      distributionKeys: []
    } : undefined;

    projectStore.updateProject({
      property: {
        ...projectStore.currentProject.property,
        type: propertyType.value,
        constructionYear: constructionYear.value,
        overallCondition: condition.value,
        totalArea: totalArea.value,
        livingArea: livingArea.value,
        landArea: landArea.value,
        unitCount: showUnitCount.value ? unitCount.value : 1,
        wegConfiguration
      }
    });
  },
  { deep: true }
);
</script>

<style scoped>
.area-section {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--kalk-space-4);
}

@media (max-width: 768px) {
  .area-section {
    grid-template-columns: 1fr;
  }
}

.weg-section {
  margin-top: var(--kalk-space-8);
  padding-top: var(--kalk-space-6);
  border-top: 1px solid var(--kalk-gray-200);
}

.section-label {
  font-size: var(--kalk-text-base);
  font-weight: 600;
  color: var(--kalk-gray-900);
  margin-bottom: var(--kalk-space-4);
}
</style>
