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
    <section class="step-section">
      <div class="step-section-head">
        <h4 class="step-section-title">{{ t('property.marketReference.title') }}</h4>
        <p class="step-section-desc">{{ t('property.marketReference.hint') }}</p>
      </div>
      <div class="market-fields">
        <KalkCurrency
          v-model="regionalPricePerSqm"
          :label="t('property.marketReference.pricePerSqm')"
          :currency="currency"
          :hint="fairMarketValueHint"
          help-key="property.regionalPrice"
        />
      </div>
    </section>

    <!-- Building Components Section -->
    <section class="step-section">
      <div class="step-section-head">
        <h4 class="step-section-title">{{ t('property.components.title') }} <HelpIcon help-key="property.components" /></h4>
        <p class="step-section-desc">{{ t('property.components.hint') }}</p>
      </div>

      <div class="table-scroll">
        <table class="data-table components-table">
          <thead>
            <tr>
              <th class="col-category">{{ t('capex.measure.category') }}</th>
              <th class="col-condition">{{ t('property.components.condition') }}</th>
              <th class="col-year num">{{ t('property.components.lastRenovation') }}</th>
              <th class="col-cycle num">{{ t('property.components.expectedCycle') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="comp in components" :key="comp.category">
              <td class="col-category component-label">
                {{ t(`property.components.categories.${comp.category}`) }}
              </td>
              <td class="col-condition">
                <div class="condition-cell">
                  <span class="condition-marker" :class="`cond-${comp.condition}`" aria-hidden="true"></span>
                  <select v-model="comp.condition" class="table-control">
                    <option value="New">{{ t('property.conditions.New') }}</option>
                    <option value="Good">{{ t('property.conditions.Good') }}</option>
                    <option value="Fair">{{ t('property.conditions.Fair') }}</option>
                    <option value="Poor">{{ t('property.conditions.Poor') }}</option>
                    <option value="NeedsRenovation">{{ t('property.conditions.NeedsRenovation') }}</option>
                  </select>
                </div>
              </td>
              <td class="col-year num">
                <input
                  v-model.number="comp.lastRenovationYear"
                  type="number"
                  class="table-control table-control--num"
                  :placeholder="String(constructionYear)"
                  :min="1800"
                  :max="2030"
                />
              </td>
              <td class="col-cycle num">
                <input
                  v-model.number="comp.expectedCycleYears"
                  type="number"
                  class="table-control table-control--num"
                  :min="5"
                  :max="100"
                />
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <!-- Unit Management Section -->
    <section v-if="showUnits" class="step-section">
      <div class="step-section-head">
        <h4 class="step-section-title">{{ t('property.units.title') }}</h4>
        <p class="step-section-desc">{{ t('property.unitComponents.hint') }}</p>
      </div>

      <div v-for="(unit, index) in units" :key="unit.id" class="unit-panel">
        <div class="unit-header" @click="toggleUnit(unit.id)">
          <svg
            class="unit-chevron"
            :class="{ 'is-expanded': expandedUnits.has(unit.id) }"
            viewBox="0 0 20 20"
            fill="currentColor"
            aria-hidden="true"
          >
            <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
          </svg>
          <span class="unit-title">{{ unit.name || `WE ${index + 1}` }}</span>
          <span class="unit-meta">{{ unit.area }} {{ t('common.sqm') }}</span>
          <button
            type="button"
            class="unit-delete-btn"
            @click.stop="removeUnit(index)"
            :title="t('property.units.removeUnit')"
          >
            <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
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
            <p class="step-section-desc">{{ t('property.units.componentsHint') }}</p>

            <div class="table-scroll">
              <table class="data-table components-table">
                <thead>
                  <tr>
                    <th class="col-category">{{ t('capex.measure.category') }}</th>
                    <th class="col-condition">{{ t('property.components.condition') }}</th>
                    <th class="col-year num">{{ t('property.components.lastRenovation') }}</th>
                    <th class="col-cycle num">{{ t('property.components.expectedCycle') }}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="comp in unit.components" :key="comp.category">
                    <td class="col-category component-label">
                      {{ t(`property.components.categories.${comp.category}`) }}
                    </td>
                    <td class="col-condition">
                      <div class="condition-cell">
                        <span class="condition-marker" :class="`cond-${comp.condition}`" aria-hidden="true"></span>
                        <select v-model="comp.condition" class="table-control">
                          <option value="New">{{ t('property.conditions.New') }}</option>
                          <option value="Good">{{ t('property.conditions.Good') }}</option>
                          <option value="Fair">{{ t('property.conditions.Fair') }}</option>
                          <option value="Poor">{{ t('property.conditions.Poor') }}</option>
                          <option value="NeedsRenovation">{{ t('property.conditions.NeedsRenovation') }}</option>
                        </select>
                      </div>
                    </td>
                    <td class="col-year num">
                      <input
                        v-model.number="comp.lastRenovationYear"
                        type="number"
                        class="table-control table-control--num"
                        :placeholder="String(constructionYear)"
                        :min="1800"
                        :max="2030"
                      />
                    </td>
                    <td class="col-cycle num">
                      <input
                        v-model.number="comp.expectedCycleYears"
                        type="number"
                        class="table-control table-control--num"
                        :min="5"
                        :max="100"
                      />
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>

      <button type="button" class="btn btn-outline btn-sm add-unit-btn" @click="addUnit">
        <svg class="add-unit-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
          <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
        </svg>
        {{ t('property.units.addUnit') }}
      </button>
    </section>

    <!-- WEG Configuration for Condominiums -->
    <section v-if="propertyType === 'Condominium'" class="step-section">
      <div class="step-section-head">
        <h4 class="step-section-title">{{ t('property.weg.title') }}</h4>
      </div>

      <KalkInput
        v-model="meaPerMille"
        :label="t('property.weg.meaPerMille')"
        type="number"
        :min="1"
        :max="10000"
        suffix="‰"
        help-key="weg.mea"
      />

      <KalkInput
        v-model="totalMea"
        :label="t('property.weg.totalMea')"
        type="number"
        :min="1000"
        :max="10000"
        suffix="‰"
        help-key="weg.totalMea"
      />

      <KalkCurrency
        v-model="reserveBalance"
        :label="t('property.weg.reserveBalance')"
        :currency="currency"
        help-key="weg.reserveBalance"
      />
    </section>
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
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: var(--kalk-space-4);
}

/* ------------------------------------------------------------
 * Sections within the step
 * ------------------------------------------------------------ */
.step-section {
  margin-top: var(--kalk-space-6);
  padding-top: var(--kalk-space-6);
  border-top: 1px solid var(--kalk-border);
}

.step-section-head {
  margin-bottom: var(--kalk-space-4);
}

.step-section-title {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-1);
  font-size: var(--kalk-text-base);
  font-weight: 600;
  color: var(--kalk-gray-900);
  margin: 0;
}

.step-section-desc {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
  line-height: 1.5;
  margin: 2px 0 0;
}

.market-fields {
  max-width: 300px;
}

/* ------------------------------------------------------------
 * Components table
 * ------------------------------------------------------------ */
.table-scroll {
  width: 100%;
  overflow-x: auto;
  -webkit-overflow-scrolling: touch;
  border: 1px solid var(--kalk-border);
  border-radius: var(--kalk-radius-lg);
  background: var(--kalk-surface);
}

.components-table {
  min-width: 560px;
}

.components-table select.table-control {
  min-width: 150px;
}

.components-table .table-control--num {
  min-width: 80px;
}

.components-table th,
.components-table td {
  vertical-align: middle;
}

.components-table td {
  padding-top: 6px;
  padding-bottom: 6px;
}

.components-table .col-category {
  width: 34%;
}

.components-table .col-condition {
  width: 30%;
}

.components-table .col-year,
.components-table .col-cycle {
  width: 18%;
}

.component-label {
  font-weight: 500;
  color: var(--kalk-gray-800);
  white-space: nowrap;
}

.condition-cell {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-2);
}

/* Small rectangular status marker, muted semantic colors */
.condition-marker {
  flex-shrink: 0;
  width: 8px;
  height: 8px;
  border-radius: 1px;
  border: 1px solid var(--kalk-border-strong);
  background: var(--kalk-gray-100);
}

.condition-marker.cond-New,
.condition-marker.cond-Good {
  background: var(--kalk-positive-bg);
  border-color: var(--kalk-positive);
}

.condition-marker.cond-Fair {
  background: var(--kalk-caution-bg);
  border-color: var(--kalk-caution);
}

.condition-marker.cond-Poor,
.condition-marker.cond-NeedsRenovation {
  background: var(--kalk-negative-bg);
  border-color: var(--kalk-negative);
}

.table-control {
  width: 100%;
  height: 32px;
  padding: 0 var(--kalk-space-2);
  font: inherit;
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-900);
  background-color: var(--kalk-surface);
  border: 1px solid var(--kalk-border-strong);
  border-radius: var(--kalk-radius-md);
  outline: none;
  transition: border-color 0.12s, box-shadow 0.12s;
}

.table-control:hover {
  border-color: var(--kalk-gray-400);
}

.table-control:focus {
  border-color: var(--kalk-navy-600);
  box-shadow: 0 0 0 3px var(--kalk-ring-color);
}

select.table-control {
  cursor: pointer;
}

.table-control--num {
  text-align: right;
  font-variant-numeric: tabular-nums;
}

.table-control::placeholder {
  color: var(--kalk-gray-400);
}

/* ------------------------------------------------------------
 * Unit management
 * ------------------------------------------------------------ */
.unit-panel {
  background: var(--kalk-surface);
  border: 1px solid var(--kalk-border);
  border-radius: var(--kalk-radius-lg);
  margin-bottom: var(--kalk-space-2);
  overflow: hidden;
}

.unit-header {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-2);
  padding: var(--kalk-space-2) var(--kalk-space-3);
  background: var(--kalk-gray-50);
  cursor: pointer;
  transition: background-color 0.12s;
}

.unit-header:hover {
  background: var(--kalk-gray-100);
}

.unit-chevron {
  width: 16px;
  height: 16px;
  color: var(--kalk-gray-500);
  flex-shrink: 0;
  transition: transform 0.15s ease;
}

.unit-chevron.is-expanded {
  transform: rotate(90deg);
}

.unit-title {
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-900);
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.unit-meta {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
  font-variant-numeric: tabular-nums;
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
  border: 1px solid transparent;
  border-radius: var(--kalk-radius-md);
  color: var(--kalk-gray-500);
  cursor: pointer;
  transition: background-color 0.12s, color 0.12s, border-color 0.12s;
  flex-shrink: 0;
}

.unit-delete-btn:hover {
  background: var(--kalk-negative-bg);
  border-color: var(--kalk-negative-border);
  color: var(--kalk-negative);
}

.unit-delete-btn svg {
  width: 16px;
  height: 16px;
}

.unit-body {
  padding: 0 var(--kalk-space-4) var(--kalk-space-4);
  border-top: 1px solid var(--kalk-border);
}

.unit-fields {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: var(--kalk-space-4);
  padding-top: var(--kalk-space-4);
}

.unit-components-section {
  margin-top: var(--kalk-space-2);
  padding-top: var(--kalk-space-4);
  border-top: 1px solid var(--kalk-gray-100);
}

.unit-components-section .step-section-desc {
  margin-bottom: var(--kalk-space-3);
}

.unit-components-label {
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-800);
  margin: 0;
}

.add-unit-btn {
  margin-top: var(--kalk-space-1);
}

.add-unit-icon {
  width: 14px;
  height: 14px;
}

@media (max-width: 768px) {
  .area-section,
  .unit-fields {
    grid-template-columns: minmax(0, 1fr);
  }

  .market-fields {
    max-width: none;
  }

  .unit-body {
    padding: 0 var(--kalk-space-3) var(--kalk-space-3);
  }
}
</style>
