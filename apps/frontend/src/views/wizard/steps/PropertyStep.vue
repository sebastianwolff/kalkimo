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
      help-key="property.condition"
    />

    <div class="area-section">
      <KalkInput
        v-model="totalArea"
        :label="t('property.totalArea')"
        type="number"
        :suffix="t('common.sqm')"
        :min="1"
        required
        help-key="property.totalArea"
      />

      <KalkInput
        v-model="livingArea"
        :label="t('property.livingArea')"
        type="number"
        :suffix="t('common.sqm')"
        :min="1"
        required
        help-key="property.livingArea"
      />

      <KalkInput
        v-model="landArea"
        :label="t('property.landArea')"
        type="number"
        :suffix="t('common.sqm')"
        :min="0"
        :hint="propertyType === 'Condominium' ? '' : undefined"
        help-key="property.landArea"
      />
    </div>

    <KalkInput
      v-if="showUnitCount"
      v-model="unitCount"
      :label="t('property.unitCount')"
      type="number"
      :min="1"
      :max="500"
      help-key="property.unitCount"
    />

    <!-- Regional Market Price -->
    <div class="market-section">
      <h4 class="section-label">{{ t('property.marketReference.title') }}</h4>
      <p class="components-hint">{{ t('property.marketReference.hint') }}</p>
      <div class="market-fields">
        <KalkCurrency
          v-model="regionalPricePerSqm"
          :label="t('property.marketReference.pricePerSqm')"
          :currency="currency"
          :hint="fairMarketValueHint"
          help-key="property.regionalPrice"
        />
      </div>
    </div>

    <!-- Building Components Section -->
    <div class="components-section">
      <h4 class="section-label">{{ t('property.components.title') }} <HelpIcon help-key="property.components" /></h4>
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

    <!-- Unit Management Section -->
    <div v-if="showUnits" class="units-section">
      <h4 class="section-label">{{ t('property.units.title') }}</h4>
      <p class="components-hint">{{ t('property.unitComponents.hint') }}</p>

      <div v-for="(unit, index) in units" :key="unit.id" class="unit-panel">
        <div class="unit-header" @click="toggleUnit(unit.id)">
          <span class="unit-toggle">{{ expandedUnits.has(unit.id) ? '\u25BE' : '\u25B8' }}</span>
          <span class="unit-title">{{ unit.name || `WE ${index + 1}` }}</span>
          <span class="unit-meta">{{ unit.area }} {{ t('common.sqm') }}</span>
          <button
            type="button"
            class="unit-delete-btn"
            @click.stop="removeUnit(index)"
            :title="t('property.units.removeUnit')"
          >
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>

        <div v-if="expandedUnits.has(unit.id)" class="unit-body">
          <div class="unit-fields">
            <KalkInput
              v-model="unit.name"
              :label="t('property.units.name')"
            />
            <KalkInput
              v-model="unit.area"
              :label="t('property.units.area')"
              type="number"
              :suffix="t('common.sqm')"
              :min="1"
            />
            <KalkSelect
              v-model="unit.type"
              :label="t('property.units.type')"
              :options="unitTypeOptions"
            />
          </div>

          <div class="unit-components-section">
            <h5 class="unit-components-label">{{ t('property.units.components') }}</h5>
            <p class="components-hint">{{ t('property.units.componentsHint') }}</p>

            <div class="components-table">
              <div class="components-table-header">
                <span class="col-category">{{ t('capex.measure.category') }}</span>
                <span class="col-condition">{{ t('property.components.condition') }}</span>
                <span class="col-year">{{ t('property.components.lastRenovation') }}</span>
                <span class="col-cycle">{{ t('property.components.expectedCycle') }}</span>
              </div>

              <div
                v-for="comp in unit.components"
                :key="comp.category"
                class="component-row"
              >
                <span class="col-category component-label">
                  {{ t(`property.components.categories.${comp.category}`) }}
                </span>

                <div class="col-condition">
                  <select v-model="comp.condition" class="component-select">
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
        </div>
      </div>

      <button type="button" class="add-unit-btn" @click="addUnit">
        <svg class="add-unit-icon" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
        </svg>
        {{ t('property.units.addUnit') }}
      </button>
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
        help-key="weg.totalMea"
      />

      <KalkCurrency
        v-model="reserveBalance"
        :label="t('property.weg.reserveBalance')"
        :currency="currency"
        help-key="weg.reserveBalance"
      />
    </div>
  </KalkCard>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, reactive } from 'vue';
import { useI18n } from 'vue-i18n';
import { KalkCard, KalkInput, KalkSelect, KalkCurrency, HelpIcon } from '@/components';
import { useProjectStore } from '@/stores/projectStore';
import type { PropertyType, Condition, CapExCategory, UnitType } from '@/stores/types';
import { UNIT_LEVEL_CATEGORIES } from '@/stores/types';
import { getDefaultCycleYears } from '@/services/renovationForecastService';

interface ComponentRow {
  category: CapExCategory;
  condition: Condition;
  lastRenovationYear: number | undefined;
  expectedCycleYears: number;
}

interface UnitItem {
  id: string;
  name: string;
  type: UnitType;
  area: number;
  components: ComponentRow[];
}

const emit = defineEmits<{
  'validation-change': [valid: boolean];
}>();

const { t, locale } = useI18n();
const projectStore = useProjectStore();

// Form values
const propertyType = ref<PropertyType>('SingleFamily');
const constructionYear = ref<number>(1990);
const condition = ref<Condition>('Good');
const totalArea = ref<number>(150);
const livingArea = ref<number>(120);
const landArea = ref<number | undefined>();
const unitCount = ref<number>(1);

// Regional market price
const regionalPricePerSqm = ref<number | undefined>();

// WEG fields
const meaPerMille = ref<number>(100);
const totalMea = ref<number>(1000);
const reserveBalance = ref<number | undefined>();

// Building components
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

// Unit management
const units = reactive<UnitItem[]>([]);
const expandedUnits = ref(new Set<string>());

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

const unitTypeOptions = computed(() => [
  { value: 'Residential', label: t('property.units.types.Residential') },
  { value: 'Commercial', label: t('property.units.types.Commercial') },
  { value: 'Parking', label: t('property.units.types.Parking') },
  { value: 'Storage', label: t('property.units.types.Storage') }
]);

const showUnitCount = computed(() =>
  ['MultiFamily', 'Mixed'].includes(propertyType.value)
);

const showUnits = computed(() =>
  ['MultiFamily', 'Mixed'].includes(propertyType.value)
);

const fairMarketValueHint = computed(() => {
  if (!regionalPricePerSqm.value || !livingArea.value) return t('property.marketReference.hintEmpty');
  const fmv = regionalPricePerSqm.value * livingArea.value;
  const formatted = new Intl.NumberFormat(locale.value, {
    style: 'currency',
    currency: currency.value,
    maximumFractionDigits: 0,
  }).format(fmv);
  return `${t('property.marketReference.fairValue')}: ${formatted}`;
});

// Unit management functions
function createDefaultUnitComponents(): ComponentRow[] {
  return UNIT_LEVEL_CATEGORIES.map(category => ({
    category,
    condition: 'Good' as Condition,
    lastRenovationYear: undefined,
    expectedCycleYears: getDefaultCycleYears(category),
  }));
}

function addUnit() {
  const id = crypto.randomUUID();
  const index = units.length + 1;
  units.push({
    id,
    name: `WE ${index}`,
    type: 'Residential',
    area: livingArea.value > 0 ? Math.round(livingArea.value / Math.max(unitCount.value, 1)) : 60,
    components: createDefaultUnitComponents()
  });
  expandedUnits.value = new Set([...expandedUnits.value, id]);
}

function removeUnit(index: number) {
  const id = units[index].id;
  units.splice(index, 1);
  const next = new Set(expandedUnits.value);
  next.delete(id);
  expandedUnits.value = next;
}

function toggleUnit(id: string) {
  const next = new Set(expandedUnits.value);
  if (next.has(id)) {
    next.delete(id);
  } else {
    next.add(id);
  }
  expandedUnits.value = next;
}

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
    regionalPricePerSqm.value = property.regionalPricePerSqm;

    if (property.wegConfiguration) {
      meaPerMille.value = property.wegConfiguration.meaPerMille;
      totalMea.value = property.wegConfiguration.totalMeaPerMille;
      reserveBalance.value = property.wegConfiguration.currentReserveBalance?.amount;
    }

    // Initialize building components from store
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

    // Initialize units from store
    if (property.units && property.units.length > 0) {
      for (const stored of property.units) {
        units.push({
          id: stored.id,
          name: stored.name,
          type: stored.type,
          area: stored.area,
          components: UNIT_LEVEL_CATEGORIES.map(category => {
            const sc = stored.components?.find((c: any) => c.category === category);
            return {
              category,
              condition: (sc?.condition ?? 'Good') as Condition,
              lastRenovationYear: sc?.lastRenovationYear,
              expectedCycleYears: sc?.expectedCycleYears ?? getDefaultCycleYears(category),
            };
          })
        });
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
  [propertyType, constructionYear, condition, totalArea, livingArea, landArea, unitCount, regionalPricePerSqm, meaPerMille, totalMea, reserveBalance, components, units],
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
        regionalPricePerSqm: regionalPricePerSqm.value || undefined,
        wegConfiguration,
        components: components.map(c => ({
          category: c.category,
          condition: c.condition,
          lastRenovationYear: c.lastRenovationYear || undefined,
          expectedCycleYears: c.expectedCycleYears,
        })),
        units: showUnits.value ? units.map(u => ({
          id: u.id,
          name: u.name,
          type: u.type,
          area: u.area,
          status: 'Rented' as const,
          components: u.components.map(c => ({
            category: c.category,
            condition: c.condition,
            lastRenovationYear: c.lastRenovationYear || undefined,
            expectedCycleYears: c.expectedCycleYears,
          }))
        })) : projectStore.currentProject.property.units
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

.market-section {
  margin-top: var(--kalk-space-8);
  padding-top: var(--kalk-space-6);
  border-top: 1px solid var(--kalk-gray-200);
}
.market-fields {
  max-width: 300px;
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

/* Unit Management */
.units-section {
  margin-top: var(--kalk-space-8);
  padding-top: var(--kalk-space-6);
  border-top: 1px solid var(--kalk-gray-200);
}

.unit-panel {
  background: var(--kalk-gray-50);
  border: 1px solid var(--kalk-gray-200);
  border-radius: var(--kalk-radius-md);
  margin-bottom: var(--kalk-space-3);
  overflow: hidden;
}

.unit-header {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  padding: var(--kalk-space-3) var(--kalk-space-4);
  cursor: pointer;
  transition: background 0.15s;
}

.unit-header:hover {
  background: var(--kalk-gray-100);
}

.unit-toggle {
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-500);
  width: 16px;
  text-align: center;
  flex-shrink: 0;
}

.unit-title {
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-900);
  flex: 1;
}

.unit-meta {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
  flex-shrink: 0;
}

.unit-delete-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  padding: 0;
  background: transparent;
  border: none;
  border-radius: var(--kalk-radius-sm);
  color: var(--kalk-gray-400);
  cursor: pointer;
  transition: all 0.15s;
  flex-shrink: 0;
}

.unit-delete-btn:hover {
  background: var(--kalk-error-bg, #fef2f2);
  color: var(--kalk-error, #ef4444);
}

.unit-delete-btn svg {
  width: 16px;
  height: 16px;
}

.unit-body {
  padding: 0 var(--kalk-space-4) var(--kalk-space-4);
  border-top: 1px solid var(--kalk-gray-200);
  background: #ffffff;
}

.unit-fields {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--kalk-space-4);
  padding-top: var(--kalk-space-4);
}

.unit-components-section {
  margin-top: var(--kalk-space-4);
  padding-top: var(--kalk-space-4);
  border-top: 1px solid var(--kalk-gray-100);
}

.unit-components-label {
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-800);
  margin-bottom: var(--kalk-space-2);
}

.add-unit-btn {
  display: inline-flex;
  align-items: center;
  gap: var(--kalk-space-2);
  padding: var(--kalk-space-2) var(--kalk-space-4);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  color: var(--kalk-accent-600);
  background: transparent;
  border: 1.5px dashed var(--kalk-accent-300);
  border-radius: var(--kalk-radius-md);
  cursor: pointer;
  transition: all 0.15s;
  width: 100%;
  justify-content: center;
}

.add-unit-btn:hover {
  background: var(--kalk-accent-50);
  border-color: var(--kalk-accent-500);
  color: var(--kalk-accent-700);
}

.add-unit-icon {
  width: 16px;
  height: 16px;
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

  .unit-fields {
    grid-template-columns: 1fr;
  }
}
</style>
