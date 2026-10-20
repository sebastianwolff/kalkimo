<template>
  <KalkCard :title="t('rent.title')">
    <div class="rent-units-section">
      <div class="block-header">
        <h4 class="section-title">{{ t('rent.units') }}</h4>
        <button type="button" class="btn btn-outline btn-sm" @click="addRentUnit">
          <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
          </svg>
          {{ t('rent.addUnit') }}
        </button>
      </div>

      <div v-if="rentUnits.length === 0" class="empty-row">
        Keine Mieteinheiten konfiguriert
      </div>

      <div v-else class="units-list">
        <div
          v-for="(unit, index) in rentUnits"
          :key="unit.unitId"
          class="panel item-panel"
        >
          <div class="item-header">
            <span class="item-index">{{ String(index + 1).padStart(2, '0') }}</span>
            <template v-if="propertyUnits.length > 0">
              <select
                :value="unit.unitId"
                @change="assignPropertyUnit(unit, ($event.target as HTMLSelectElement).value)"
                class="item-name-input item-select"
              >
                <option value="" disabled>{{ t('rent.selectUnit') }}</option>
                <option
                  v-for="pu in availablePropertyUnits(unit.unitId)"
                  :key="pu.id"
                  :value="pu.id"
                >
                  {{ pu.name }} ({{ pu.area }} {{ t('common.sqm') }})
                </option>
              </select>
            </template>
            <template v-else>
              <input
                v-model="unit.name"
                type="text"
                class="item-name-input"
                :placeholder="`Einheit ${index + 1}`"
              />
            </template>
            <button
              type="button"
              class="icon-btn"
              @click="removeRentUnit(unit.unitId)"
              :title="t('common.delete')"
              :aria-label="t('common.delete')"
            >
              <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" />
              </svg>
            </button>
          </div>

          <div class="item-body">
            <div class="unit-fields">
              <KalkCurrency
                v-model="unit.monthlyRent"
                :label="t('rent.unit.monthlyRent')"
                :currency="currency"
                help-key="rent.monthlyRent"
              />
              <KalkCurrency
                v-model="unit.serviceCharge"
                :label="t('rent.unit.serviceCharge')"
                :currency="currency"
                help-key="rent.serviceCharge"
              />
            </div>

            <div class="rent-increase-section">
              <div class="segmented" role="group">
                <button
                  type="button"
                  :class="['segment', { active: unit.increaseType === 'percent' }]"
                  :aria-pressed="unit.increaseType === 'percent'"
                  @click="unit.increaseType = 'percent'"
                >
                  Jährl. Steigerung
                </button>
                <button
                  type="button"
                  :class="['segment', { active: unit.increaseType === 'schedule' }]"
                  :aria-pressed="unit.increaseType === 'schedule'"
                  @click="unit.increaseType = 'schedule'"
                >
                  Mietstaffel
                </button>
              </div>

              <div v-if="unit.increaseType === 'percent'" class="increase-percent">
                <KalkPercent
                  v-model="unit.annualIncrease"
                  :label="'Jährliche Mietsteigerung'"
                  :min="0"
                  :max="10"
                  help-key="rent.annualIncrease"
                />
              </div>

              <div v-else class="rent-schedule">
                <div class="schedule-header">
                  <span class="schedule-label">Mietstaffel</span>
                  <button
                    type="button"
                    class="btn btn-ghost btn-sm"
                    @click="addScheduleEntry(unit)"
                  >
                    <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                      <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
                    </svg>
                    Stufe hinzufügen
                  </button>
                </div>

                <div v-if="!unit.schedule || unit.schedule.length === 0" class="schedule-empty">
                  Keine Mietstaffeln definiert
                </div>

                <div v-else class="schedule-table">
                  <div class="schedule-row schedule-head">
                    <span>Ab Jahr</span>
                    <span class="num">Neue Kaltmiete</span>
                    <span></span>
                  </div>
                  <div
                    v-for="(entry, idx) in unit.schedule"
                    :key="idx"
                    class="schedule-row"
                  >
                    <input
                      v-model.number="entry.fromYear"
                      type="number"
                      class="schedule-input"
                      aria-label="Ab Jahr"
                      :min="1"
                      :max="30"
                    />
                    <div class="schedule-currency-input">
                      <input
                        v-model.number="entry.newRent"
                        type="number"
                        class="schedule-input"
                        aria-label="Neue Kaltmiete"
                        :min="0"
                      />
                      <span class="schedule-currency">€</span>
                    </div>
                    <button
                      type="button"
                      class="icon-btn icon-btn-sm"
                      :title="t('common.delete')"
                      :aria-label="t('common.delete')"
                      @click="removeScheduleEntry(unit, idx)"
                    >
                      <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                        <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
                      </svg>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="global-settings">
      <KalkPercent
        v-model="vacancyRate"
        :label="t('rent.vacancyRate')"
        :min="0"
        :max="30"
        help-key="rent.vacancy"
      />

      <KalkInput
        v-model="rentLossReserve"
        :label="t('rent.rentLossReserve')"
        type="number"
        :min="0"
        help-key="rent.rentLossReserve"
        :max="12"
        :suffix="t('common.months')"
      />
    </div>

    <div class="summary-box statement">
      <div class="data-row">
        <span class="data-label">{{ t('rent.totalMonthlyRent') }}</span>
        <span class="data-value">{{ formatCurrency(totalMonthlyRent) }}</span>
      </div>
      <div class="data-row total">
        <span class="data-label">{{ t('rent.totalAnnualRent') }}</span>
        <span class="data-value">{{ formatCurrency(totalMonthlyRent * 12) }}</span>
      </div>
    </div>
  </KalkCard>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { KalkCard, KalkCurrency, KalkPercent, KalkInput } from '@/components';
import { useProjectStore } from '@/stores/projectStore';

interface RentScheduleEntry {
  fromYear: number;
  newRent: number;
}

interface RentUnitItem {
  unitId: string;
  name: string;
  monthlyRent: number;
  serviceCharge: number;
  annualIncrease: number;
  increaseType: 'percent' | 'schedule';
  schedule: RentScheduleEntry[];
}

const emit = defineEmits<{
  'validation-change': [valid: boolean];
}>();

const { t, locale } = useI18n();
const projectStore = useProjectStore();

const rentUnits = ref<RentUnitItem[]>([]);
const vacancyRate = ref<number>(2);
const rentLossReserve = ref<number>(3);

const currency = computed(() => projectStore.currentProject?.currency || 'EUR');

const propertyUnits = computed(() =>
  projectStore.currentProject?.property.units ?? []
);

function availablePropertyUnits(currentUnitId: string) {
  const assignedIds = new Set(rentUnits.value.map(u => u.unitId).filter(id => id !== currentUnitId));
  return propertyUnits.value.filter(pu => !assignedIds.has(pu.id));
}

function assignPropertyUnit(unit: RentUnitItem, propertyUnitId: string) {
  const pu = propertyUnits.value.find(u => u.id === propertyUnitId);
  if (pu) {
    unit.unitId = pu.id;
    unit.name = pu.name;
  }
}

const totalMonthlyRent = computed(() =>
  rentUnits.value.reduce((sum, u) => sum + (u.monthlyRent || 0), 0)
);

function formatCurrency(value: number): string {
  return new Intl.NumberFormat(locale.value, {
    style: 'currency',
    currency: currency.value
  }).format(value);
}

function addRentUnit() {
  rentUnits.value.push({
    unitId: crypto.randomUUID(),
    name: `Einheit ${rentUnits.value.length + 1}`,
    monthlyRent: 800,
    serviceCharge: 150,
    annualIncrease: 2,
    increaseType: 'percent',
    schedule: []
  });
}

function addScheduleEntry(unit: RentUnitItem) {
  if (!unit.schedule) {
    unit.schedule = [];
  }
  const lastYear = unit.schedule.length > 0
    ? Math.max(...unit.schedule.map(s => s.fromYear)) + 1
    : 2;
  unit.schedule.push({
    fromYear: lastYear,
    newRent: unit.monthlyRent
  });
}

function removeScheduleEntry(unit: RentUnitItem, index: number) {
  unit.schedule.splice(index, 1);
}

function removeRentUnit(unitId: string) {
  const index = rentUnits.value.findIndex(u => u.unitId === unitId);
  if (index !== -1) {
    rentUnits.value.splice(index, 1);
  }
}

onMounted(() => {
  const rent = projectStore.currentProject?.rent;
  if (rent) {
    rentUnits.value = rent.units.map(u => ({
      unitId: u.unitId,
      name: u.unitId,
      monthlyRent: u.monthlyRent.amount,
      serviceCharge: u.monthlyServiceCharge.amount,
      annualIncrease: u.annualRentIncreasePercent,
      increaseType: (u as any).increaseType || 'percent',
      schedule: (u as any).schedule || []
    }));
    vacancyRate.value = rent.vacancyRatePercent;
    rentLossReserve.value = rent.rentLossReserveMonths;
  }
});

const isValid = computed(() => true);

watch(isValid, (valid) => {
  emit('validation-change', valid);
}, { immediate: true });

watch(
  [rentUnits, vacancyRate, rentLossReserve],
  () => {
    if (!projectStore.currentProject) return;

    projectStore.updateProject({
      rent: {
        units: rentUnits.value.map(u => ({
          unitId: u.unitId,
          monthlyRent: { amount: u.monthlyRent, currency: currency.value },
          monthlyServiceCharge: { amount: u.serviceCharge, currency: currency.value },
          annualRentIncreasePercent: u.increaseType === 'percent' ? u.annualIncrease : 0,
          increaseType: u.increaseType,
          schedule: u.schedule
        })),
        vacancyRatePercent: vacancyRate.value,
        rentLossReserveMonths: rentLossReserve.value
      }
    });
  },
  { deep: true }
);
</script>

<style scoped>
.rent-units-section {
  margin-bottom: var(--kalk-space-6);
}

.block-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: var(--kalk-space-3);
  margin-bottom: var(--kalk-space-3);
}

.block-header .section-title {
  margin: 0;
}

.empty-row {
  padding: var(--kalk-space-3) var(--kalk-space-4);
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-500);
  background: var(--kalk-gray-50);
  border: 1px solid var(--kalk-border);
  border-radius: var(--kalk-radius-md);
}

.units-list {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-3);
}

.item-panel {
  border-radius: var(--kalk-radius-md);
}

.item-header {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  padding: var(--kalk-space-1) var(--kalk-space-2) var(--kalk-space-1) var(--kalk-space-4);
  background: var(--kalk-gray-50);
  border-bottom: 1px solid var(--kalk-border);
}

.item-index {
  font-size: var(--kalk-text-xs);
  font-weight: 600;
  color: var(--kalk-gray-500);
  font-variant-numeric: tabular-nums;
}

.item-name-input {
  flex: 1;
  min-width: 0;
  height: 32px;
  padding: 0 var(--kalk-space-2);
  margin-left: calc(-1 * var(--kalk-space-2));
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-900);
  background: transparent;
  border: 1px solid transparent;
  border-radius: var(--kalk-radius-sm);
  outline: none;
  transition: border-color 0.12s, background-color 0.12s;
}

.item-name-input:hover {
  border-color: var(--kalk-border);
}

.item-name-input:focus {
  background: var(--kalk-surface);
  border-color: var(--kalk-navy-600);
}

.item-name-input::placeholder {
  color: var(--kalk-gray-400);
  font-weight: 500;
}

.item-select {
  margin-left: 0;
  cursor: pointer;
  background: var(--kalk-surface);
  border-color: var(--kalk-border-strong);
}

.icon-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  background: transparent;
  border: 1px solid transparent;
  border-radius: var(--kalk-radius-md);
  color: var(--kalk-gray-400);
  cursor: pointer;
  flex-shrink: 0;
  transition: color 0.12s, background-color 0.12s, border-color 0.12s;
}

.icon-btn:hover {
  color: var(--kalk-negative);
  background: var(--kalk-negative-bg);
  border-color: var(--kalk-negative-border);
}

.icon-btn svg {
  width: 16px;
  height: 16px;
}

.icon-btn-sm svg {
  width: 14px;
  height: 14px;
}

.item-body {
  padding: var(--kalk-space-4) var(--kalk-space-4) 0;
}

.unit-fields {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  column-gap: var(--kalk-space-4);
}

.rent-increase-section {
  padding: var(--kalk-space-4) 0;
  border-top: 1px solid var(--kalk-gray-100);
}

.segmented {
  display: inline-flex;
  max-width: 100%;
  margin-bottom: var(--kalk-space-4);
  border: 1px solid var(--kalk-border-strong);
  border-radius: var(--kalk-radius-md);
  overflow: hidden;
}

.segment {
  height: 32px;
  padding: 0 var(--kalk-space-4);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-xs);
  font-weight: 500;
  color: var(--kalk-gray-600);
  background: var(--kalk-surface);
  border: none;
  cursor: pointer;
  white-space: nowrap;
  transition: background-color 0.12s, color 0.12s;
}

.segment + .segment {
  border-left: 1px solid var(--kalk-border-strong);
}

.segment:hover:not(.active) {
  background: var(--kalk-gray-50);
  color: var(--kalk-gray-900);
}

.segment.active {
  background: var(--kalk-navy-800);
  color: var(--kalk-surface);
}

.segment:focus-visible {
  outline: 2px solid var(--kalk-navy-500);
  outline-offset: -2px;
}

.increase-percent {
  max-width: 280px;
}

.increase-percent :deep(.form-group) {
  margin-bottom: 0;
}

.rent-schedule {
  border: 1px solid var(--kalk-border);
  border-radius: var(--kalk-radius-md);
}

.schedule-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--kalk-space-1) var(--kalk-space-1) var(--kalk-space-1) var(--kalk-space-3);
  border-bottom: 1px solid var(--kalk-border);
}

.schedule-label {
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-800);
}

.schedule-header .btn-ghost {
  color: var(--kalk-navy-700);
}

.schedule-empty {
  padding: var(--kalk-space-3);
  color: var(--kalk-gray-500);
  font-size: var(--kalk-text-sm);
}

.schedule-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1.4fr) 28px;
  gap: var(--kalk-space-3);
  align-items: center;
  padding: var(--kalk-space-2) var(--kalk-space-2) var(--kalk-space-2) var(--kalk-space-3);
  border-bottom: 1px solid var(--kalk-gray-100);
}

.schedule-row:last-child {
  border-bottom: none;
}

.schedule-head {
  padding-top: 6px;
  padding-bottom: 6px;
  background: var(--kalk-gray-50);
  border-bottom: 1px solid var(--kalk-border);
}

.schedule-head span {
  font-size: var(--kalk-text-2xs);
  font-weight: 600;
  letter-spacing: var(--kalk-tracking-caps);
  text-transform: uppercase;
  color: var(--kalk-gray-500);
}

.schedule-head .num {
  text-align: right;
}

.schedule-row .icon-btn {
  width: 28px;
  height: 28px;
}

.schedule-input {
  width: 100%;
  min-width: 0;
  height: 34px;
  padding: 0 var(--kalk-space-2);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  font-variant-numeric: tabular-nums;
  color: var(--kalk-gray-900);
  background: var(--kalk-surface);
  border: 1px solid var(--kalk-border-strong);
  border-radius: var(--kalk-radius-md);
  outline: none;
  transition: border-color 0.12s, box-shadow 0.12s;
}

.schedule-input:focus {
  border-color: var(--kalk-navy-600);
  box-shadow: 0 0 0 3px var(--kalk-ring-color);
}

.schedule-currency-input {
  display: flex;
  align-items: center;
  min-width: 0;
  background: var(--kalk-surface);
  border: 1px solid var(--kalk-border-strong);
  border-radius: var(--kalk-radius-md);
  transition: border-color 0.12s, box-shadow 0.12s;
}

.schedule-currency-input:focus-within {
  border-color: var(--kalk-navy-600);
  box-shadow: 0 0 0 3px var(--kalk-ring-color);
}

.schedule-currency-input .schedule-input {
  border: none;
  text-align: right;
}

.schedule-currency-input .schedule-input:focus {
  box-shadow: none;
}

.schedule-currency {
  padding: 0 var(--kalk-space-2) 0 0;
  color: var(--kalk-gray-500);
  font-size: var(--kalk-text-sm);
}

.global-settings {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  column-gap: var(--kalk-space-4);
}

@media (max-width: 600px) {
  .unit-fields,
  .global-settings {
    grid-template-columns: minmax(0, 1fr);
  }

  .segmented {
    display: flex;
  }

  .segment {
    flex: 1;
    padding: 0 var(--kalk-space-2);
  }
}

.statement {
  margin-top: var(--kalk-space-2);
  background: var(--kalk-surface);
}

.statement .data-row.total .data-value {
  font-size: var(--kalk-text-lg);
}
</style>
