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

    <!-- Building Components Section -->
    <div class="components-section">
      <h4 class="section-label">{{ t('property.components.title') }}</h4>
      <p class="components-hint">{{ t('property.components.hint') }}</p>

      <div class="components-table">
        <div class="components-table-header">
          <span class="col-category">{{ t('capex.measure.category') }}</span>
          <span class="col-condition">{{ t('property.components.condition') }}</span>
          <span class="col-year">{{ t('property.components.lastRenovation') }}</span>
          <span class="col-cycle">{{ t('property.components.expectedCycle') }}</span>
        </div>

        <div
          v-for="comp in components"
          :key="comp.category"
          class="component-row"
        >
          <span class="col-category component-label">
            {{ t(`property.components.categories.${comp.category}`) }}
          </span>

          <div class="col-condition">
            <select
              v-model="comp.condition"
              class="component-select"
            >
              <option value="New">{{ t('property.conditions.New') }}</option>
              <option value="Good">{{ t('property.conditions.Good') }}</option>
              <option value="Fair">{{ t('property.conditions.Fair') }}</option>
              <option value="Poor">{{ t('property.conditions.Poor') }}</option>
              <option value="NeedsRenovation">{{ t('property.conditions.NeedsRenovation') }}</option>
            </select>
          </div>

          <div class="col-year">
            <input
              v-model.number="comp.lastRenovationYear"
              type="number"
              class="component-input"
              :placeholder="String(constructionYear)"
              :min="1800"
              :max="2030"
            />
          </div>

          <div class="col-cycle">
            <input
              v-model.number="comp.expectedCycleYears"
              type="number"
              class="component-input"
              :min="5"
              :max="100"
            />
          </div>
        </div>
      </div>
    </div>

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
import { ref, computed, watch, onMounted, reactive } from 'vue';
import { useI18n } from 'vue-i18n';
import { KalkCard, KalkInput, KalkSelect, KalkCurrency } from '@/components';
import { useProjectStore } from '@/stores/projectStore';
import type { PropertyType, Condition, CapExCategory } from '@/stores/types';
import { getDefaultCycleYears } from '@/services/renovationForecastService';

interface ComponentRow {
  category: CapExCategory;
  condition: Condition;
  lastRenovationYear: number | undefined;
  expectedCycleYears: number;
}

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

// Components
const standardCategories: CapExCategory[] = [
  'Heating', 'Roof', 'Facade', 'Windows',
  'Electrical', 'Plumbing', 'Interior', 'Exterior'
];

const components = reactive<ComponentRow[]>(
  standardCategories.map(category => ({
    category,
    condition: 'Good' as Condition,
    lastRenovationYear: undefined,
    expectedCycleYears: getDefaultCycleYears(category as CapExCategory),
  }))
);

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

    // Initialize components from store
    if (property.components && property.components.length > 0) {
      for (const stored of property.components) {
        const row = components.find(c => c.category === stored.category);
        if (row) {
          row.condition = stored.condition;
          row.lastRenovationYear = stored.lastRenovationYear;
          row.expectedCycleYears = stored.expectedCycleYears;
        }
      }
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
  [propertyType, constructionYear, condition, totalArea, livingArea, landArea, unitCount, meaPerMille, totalMea, reserveBalance, components],
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
        wegConfiguration,
        components: components.map(c => ({
          category: c.category,
          condition: c.condition,
          lastRenovationYear: c.lastRenovationYear || undefined,
          expectedCycleYears: c.expectedCycleYears,
        }))
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

/* Building Components */
.components-section {
  margin-top: var(--kalk-space-8);
  padding-top: var(--kalk-space-6);
  border-top: 1px solid var(--kalk-gray-200);
}

.components-hint {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
  margin-top: var(--kalk-space-1);
  margin-bottom: var(--kalk-space-4);
}

.components-table {
  margin-top: var(--kalk-space-2);
}

.components-table-header {
  display: grid;
  grid-template-columns: 2fr 1.5fr 1fr 1fr;
  gap: var(--kalk-space-3);
  padding: var(--kalk-space-2) var(--kalk-space-3);
  font-size: var(--kalk-text-xs);
  font-weight: 600;
  color: var(--kalk-gray-600);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  border-bottom: 1px solid var(--kalk-gray-200);
}

.component-row {
  display: grid;
  grid-template-columns: 2fr 1.5fr 1fr 1fr;
  gap: var(--kalk-space-3);
  padding: var(--kalk-space-2) var(--kalk-space-3);
  align-items: center;
  border-bottom: 1px solid var(--kalk-gray-100);
  transition: background 0.1s;
}

.component-row:hover {
  background: var(--kalk-gray-50);
}

.component-label {
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  color: var(--kalk-gray-800);
}

.component-select {
  width: 100%;
  padding: var(--kalk-space-1) var(--kalk-space-2);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-800);
  background: #ffffff;
  border: 1px solid var(--kalk-gray-300);
  border-radius: var(--kalk-radius-sm);
  outline: none;
  transition: border-color 0.15s;
}

.component-select:focus {
  border-color: var(--kalk-accent-500);
}

.component-input {
  width: 100%;
  padding: var(--kalk-space-1) var(--kalk-space-2);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-800);
  background: #ffffff;
  border: 1px solid var(--kalk-gray-300);
  border-radius: var(--kalk-radius-sm);
  outline: none;
  transition: border-color 0.15s;
  text-align: right;
}

.component-input:focus {
  border-color: var(--kalk-accent-500);
}

.component-input::placeholder {
  color: var(--kalk-gray-400);
  text-align: right;
}

@media (max-width: 768px) {
  .components-table-header {
    display: none;
  }

  .component-row {
    grid-template-columns: 1fr 1fr;
    gap: var(--kalk-space-2);
    padding: var(--kalk-space-3);
  }

  .component-label {
    grid-column: 1 / -1;
    font-weight: 600;
  }
}
</style>
