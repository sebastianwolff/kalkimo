<template>
  <KalkCard :title="t('rent.title')">
    <div class="rent-units-section">
      <div class="section-header">
        <h4 class="section-label">{{ t('rent.units') }}</h4>
        <button type="button" class="btn btn-outline btn-sm" @click="addRentUnit">
          <svg class="btn-icon" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
          </svg>
          {{ t('rent.addUnit') }}
        </button>
      </div>

      <div v-if="rentUnits.length === 0" class="empty-state">
        <p>Keine Mieteinheiten konfiguriert</p>
      </div>

      <div v-else class="units-list">
        <div
          v-for="(unit, index) in rentUnits"
          :key="unit.unitId"
          class="unit-card"
        >
          <div class="unit-header">
            <input
              v-model="unit.name"
              type="text"
              class="unit-name-input"
              :placeholder="`Einheit ${index + 1}`"
            />
            <button
              type="button"
              class="delete-btn"
              @click="removeRentUnit(unit.unitId)"
              :title="t('common.delete')"
            >
              <svg viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" />
              </svg>
            </button>
          </div>

          <div class="unit-fields">
            <KalkCurrency
              v-model="unit.monthlyRent"
              :label="t('rent.unit.monthlyRent')"
              :currency="currency"
            />
            <KalkCurrency
              v-model="unit.serviceCharge"
              :label="t('rent.unit.serviceCharge')"
              :currency="currency"
            />
          </div>

          <div class="rent-increase-section">
            <div class="increase-type-toggle">
              <button
                type="button"
                :class="['toggle-option', { active: unit.increaseType === 'percent' }]"
                @click="unit.increaseType = 'percent'"
              >
                Jährl. Steigerung
              </button>
              <button
                type="button"
                :class="['toggle-option', { active: unit.increaseType === 'schedule' }]"
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
              />
            </div>

            <div v-else class="rent-schedule">
              <div class="schedule-header">
                <span class="schedule-label">Mietstaffel</span>
                <button
                  type="button"
                  class="add-schedule-btn"
                  @click="addScheduleEntry(unit)"
                >
                  <svg class="btn-icon" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
                  </svg>
                  Stufe hinzufügen
                </button>
              </div>

              <div v-if="!unit.schedule || unit.schedule.length === 0" class="schedule-empty">
                <p>Keine Mietstaffeln definiert</p>
              </div>

              <div v-else class="schedule-list">
                <div
                  v-for="(entry, idx) in unit.schedule"
                  :key="idx"
                  class="schedule-entry"
                >
                  <div class="schedule-entry-fields">
                    <div class="schedule-field">
                      <label class="schedule-field-label">Ab Jahr</label>
                      <input
                        v-model.number="entry.fromYear"
                        type="number"
                        class="schedule-input"
                        :min="1"
                        :max="30"
                      />
                    </div>
                    <div class="schedule-field">
                      <label class="schedule-field-label">Neue Kaltmiete</label>
                      <div class="schedule-currency-input">
                        <input
                          v-model.number="entry.newRent"
                          type="number"
                          class="schedule-input"
                          :min="0"
                        />
                        <span class="schedule-currency">€</span>
                      </div>
                    </div>
                  </div>
                  <button
                    type="button"
                    class="schedule-delete-btn"
                    @click="removeScheduleEntry(unit, idx)"
                  >
                    <svg viewBox="0 0 20 20" fill="currentColor">
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
        :max="12"
        :suffix="t('common.months')"
      />
    </div>

    <div class="rent-summary">
      <div class="summary-row">
        <span>{{ t('rent.totalMonthlyRent') }}:</span>
        <strong>{{ formatCurrency(totalMonthlyRent) }}</strong>
      </div>
      <div class="summary-row">
        <span>{{ t('rent.totalAnnualRent') }}:</span>
        <strong>{{ formatCurrency(totalMonthlyRent * 12) }}</strong>
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
  margin-bottom: var(--kalk-space-8);
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--kalk-space-4);
}

.section-label {
  font-size: var(--kalk-text-base);
  font-weight: 600;
  color: var(--kalk-gray-900);
  margin: 0;
}

.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--kalk-space-2);
  font-family: var(--kalk-font-family);
  font-weight: 600;
  border-radius: var(--kalk-radius-md);
  border: none;
  cursor: pointer;
  transition: all 0.15s;
}

.btn-sm {
  height: 36px;
  padding: 0 var(--kalk-space-4);
  font-size: var(--kalk-text-xs);
}

.btn-outline {
  background: #ffffff;
  color: var(--kalk-gray-700);
  border: 1.5px solid var(--kalk-gray-300);
}

.btn-outline:hover {
  background: var(--kalk-gray-50);
  border-color: var(--kalk-gray-400);
}

.btn-icon {
  width: 16px;
  height: 16px;
}

.empty-state {
  text-align: center;
  padding: var(--kalk-space-8);
  background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-md);
  border: 2px dashed var(--kalk-gray-200);
}

.empty-state p {
  color: var(--kalk-gray-500);
  margin: 0;
}

.units-list {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-3);
}

.unit-card {
  background: #ffffff;
  border: 1px solid var(--kalk-gray-200);
  border-radius: var(--kalk-radius-md);
  padding: var(--kalk-space-4);
  transition: border-color 0.15s, box-shadow 0.15s;
}

.unit-card:hover {
  border-color: var(--kalk-gray-300);
  box-shadow: var(--kalk-shadow-sm);
}

.unit-header {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  margin-bottom: var(--kalk-space-4);
  padding-bottom: var(--kalk-space-3);
  border-bottom: 1px solid var(--kalk-gray-100);
}

.unit-name-input {
  flex: 1;
  padding: var(--kalk-space-2) var(--kalk-space-3);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  font-weight: 600;
  color: var(--kalk-gray-900);
  background: transparent;
  border: none;
  border-bottom: 1px solid transparent;
  outline: none;
  transition: border-color 0.15s;
}

.unit-name-input:focus {
  border-bottom-color: var(--kalk-accent-500);
}

.unit-name-input::placeholder {
  color: var(--kalk-gray-400);
  font-weight: 500;
}

.delete-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  background: transparent;
  border: none;
  border-radius: var(--kalk-radius-sm);
  color: var(--kalk-gray-400);
  cursor: pointer;
  transition: all 0.15s;
  flex-shrink: 0;
}

.delete-btn:hover {
  background: var(--kalk-error-bg);
  color: var(--kalk-error);
}

.delete-btn svg {
  width: 18px;
  height: 18px;
}

.unit-fields {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--kalk-space-4);
}

@media (max-width: 600px) {
  .unit-fields {
    grid-template-columns: 1fr;
  }
}

.rent-increase-section {
  margin-top: var(--kalk-space-4);
  padding-top: var(--kalk-space-4);
  border-top: 1px solid var(--kalk-gray-100);
}

.increase-type-toggle {
  display: flex;
  background: var(--kalk-gray-100);
  border-radius: var(--kalk-radius-md);
  padding: 3px;
  margin-bottom: var(--kalk-space-4);
}

.toggle-option {
  flex: 1;
  padding: var(--kalk-space-2) var(--kalk-space-3);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-xs);
  font-weight: 500;
  color: var(--kalk-gray-600);
  background: transparent;
  border: none;
  border-radius: var(--kalk-radius-sm);
  cursor: pointer;
  transition: all 0.15s;
}

.toggle-option:hover:not(.active) {
  color: var(--kalk-gray-900);
}

.toggle-option.active {
  background: #ffffff;
  color: var(--kalk-gray-900);
  box-shadow: var(--kalk-shadow-sm);
}

.increase-percent {
  max-width: 280px;
}

.rent-schedule {
  background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-md);
  padding: var(--kalk-space-4);
}

.schedule-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--kalk-space-3);
}

.schedule-label {
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  color: var(--kalk-gray-700);
}

.add-schedule-btn {
  display: inline-flex;
  align-items: center;
  gap: var(--kalk-space-1);
  padding: var(--kalk-space-1) var(--kalk-space-2);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-xs);
  font-weight: 500;
  color: var(--kalk-accent-600);
  background: transparent;
  border: none;
  border-radius: var(--kalk-radius-sm);
  cursor: pointer;
  transition: all 0.15s;
}

.add-schedule-btn:hover {
  background: var(--kalk-accent-50);
}

.add-schedule-btn .btn-icon {
  width: 14px;
  height: 14px;
}

.schedule-empty {
  text-align: center;
  padding: var(--kalk-space-4);
  color: var(--kalk-gray-400);
  font-size: var(--kalk-text-sm);
}

.schedule-empty p {
  margin: 0;
}

.schedule-list {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-2);
}

.schedule-entry {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  padding: var(--kalk-space-2);
  background: #ffffff;
  border-radius: var(--kalk-radius-sm);
}

.schedule-entry-fields {
  display: flex;
  gap: var(--kalk-space-3);
  flex: 1;
}

.schedule-field {
  flex: 1;
}

.schedule-field-label {
  display: block;
  font-size: var(--kalk-text-xs);
  font-weight: 500;
  color: var(--kalk-gray-500);
  margin-bottom: var(--kalk-space-1);
}

.schedule-input {
  width: 100%;
  padding: var(--kalk-space-2);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-sm);
  color: var(--kalk-gray-900);
  background: var(--kalk-gray-50);
  border: 1px solid var(--kalk-gray-200);
  border-radius: var(--kalk-radius-sm);
  outline: none;
  transition: border-color 0.15s;
}

.schedule-input:focus {
  border-color: var(--kalk-accent-500);
}

.schedule-currency-input {
  display: flex;
  align-items: center;
  background: var(--kalk-gray-50);
  border: 1px solid var(--kalk-gray-200);
  border-radius: var(--kalk-radius-sm);
  transition: border-color 0.15s;
}

.schedule-currency-input:focus-within {
  border-color: var(--kalk-accent-500);
}

.schedule-currency-input .schedule-input {
  border: none;
  background: transparent;
}

.schedule-currency {
  padding: 0 var(--kalk-space-2);
  color: var(--kalk-gray-500);
  font-size: var(--kalk-text-sm);
}

.schedule-delete-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  padding: 0;
  background: transparent;
  border: none;
  border-radius: var(--kalk-radius-sm);
  color: var(--kalk-gray-400);
  cursor: pointer;
  transition: all 0.15s;
  flex-shrink: 0;
}

.schedule-delete-btn:hover {
  background: var(--kalk-error-bg);
  color: var(--kalk-error);
}

.schedule-delete-btn svg {
  width: 14px;
  height: 14px;
}

.global-settings {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--kalk-space-4);
  margin-bottom: var(--kalk-space-8);
}

@media (max-width: 600px) {
  .global-settings {
    grid-template-columns: 1fr;
  }
}

.rent-summary {
  padding: var(--kalk-space-6);
  background: var(--kalk-accent-600);
  color: #ffffff;
  border-radius: var(--kalk-radius-md);
}

.summary-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--kalk-space-1) 0;
}

.summary-row span {
  color: rgba(255, 255, 255, 0.9);
  font-size: var(--kalk-text-sm);
}

.summary-row strong {
  color: #ffffff;
  font-variant-numeric: tabular-nums;
}
</style>
