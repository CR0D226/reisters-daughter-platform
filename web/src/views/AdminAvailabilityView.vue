<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { apiFetch } from '@/services/api'

interface Entry {
  date: string
  startTime: string
  endTime: string
}

interface Employee {
  id: number
  firstName: string
  lastName: string
  email: string
  status: string
  submittedAt: string | null
  entries: Entry[]
}

interface Period {
  startDate: string
  endDate: string
  deadline: string | null
  totalEmployees: number
  submittedCount: number
  missingCount: number
  employees: Employee[]
}

const periodIndex = ref(0)
const period = ref<Period | null>(null)
const loading = ref(false)
const error = ref('')
const selectedEmployee = ref<number | null>(null)

function dateAt(offset: number) {
  return new Date(2026, 10, 2 + offset)
}

function dateKey(date: Date) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

const startDate = computed(() =>
  dateKey(dateAt(periodIndex.value * 14))
)

const dates = computed(() =>
  Array.from({ length: 14 }, (_, i) =>
    dateKey(dateAt(periodIndex.value * 14 + i))
  )
)

function formatDate(value: string) {
  const [y, m, d] = value.split('-').map(Number)
  return new Date(y!, m! - 1, d!).toLocaleDateString(
    'en-US',
    { month: 'short', day: 'numeric', weekday: 'short' }
  )
}

function formatTime(value: string) {
  if (value === '23:59:59') return 'End of day'
  const [hours, minutes] = value.split(':').map(Number)
  const hour = hours!
  return `${hour % 12 || 12}:${String(minutes).padStart(2, '0')} ${
    hour < 12 ? 'AM' : 'PM'
  }`
}

function hoursFor(employee: Employee, date: string) {
  return employee.entries.filter(e => e.date === date)
}

async function loadPeriod() {
  loading.value = true
  error.value = ''
  period.value = null
  selectedEmployee.value = null

  try {
    const response = await apiFetch(
      `/api/admin/availability/period?startDate=${startDate.value}`
    )

    if (!response.ok) {
      throw new Error(`Request failed (${response.status})`)
    }

    period.value = await response.json()
  } catch (err) {
    error.value = err instanceof Error
      ? err.message
      : 'Unable to load availability.'
  } finally {
    loading.value = false
  }
}

function changePeriod(offset: number) {
  const next = periodIndex.value + offset
  if (next < 0) return
  periodIndex.value = next
  void loadPeriod()
}

onMounted(loadPeriod)
</script>

<template>
  <main class="availability-admin">
    <header>
      <h1>Staff Availability</h1>
      <p>Review employee availability and submissions.</p>
    </header>

    <nav class="period-nav">
      <button
        :disabled="loading || periodIndex === 0"
        @click="changePeriod(-1)"
      >
        Previous
      </button>

      <strong>
        {{ formatDate(dates[0]!) }} –
        {{ formatDate(dates[13]!) }}
      </strong>

      <button :disabled="loading" @click="changePeriod(1)">
        Next
      </button>
    </nav>

    <p v-if="loading">Loading...</p>
    <p v-if="error" role="alert">{{ error }}</p>

    <template v-if="period && !loading">
      <div class="summary">
        <div>
          <strong>{{ period.totalEmployees }}</strong>
          <span>Employees</span>
        </div>

        <div>
          <strong>{{ period.submittedCount }}</strong>
          <span>Submitted</span>
        </div>

        <div>
          <strong>{{ period.missingCount }}</strong>
          <span>Not submitted</span>
        </div>
      </div>

      <p class="deadline" v-if="period.deadline">
        Recorded deadline:
        {{
          new Date(period.deadline).toLocaleString('en-US', {
            timeZone: 'America/New_York',
            dateStyle: 'medium',
            timeStyle: 'short'
          })
        }} Eastern
      </p>

      <p v-else class="deadline">
        This period has no recorded deadline yet.
      </p>

      <p class="note">
        Only saved submissions are shown. Unsubmitted weekly
        templates are not confirmed availability.
      </p>

      <section class="employee-list">
        <article
          v-for="employee in period.employees"
          :key="employee.id"
          class="employee-card"
        >
          <button
            class="employee-header"
            :aria-expanded="selectedEmployee === employee.id"
            @click="
              selectedEmployee =
                selectedEmployee === employee.id
                  ? null
                  : employee.id
            "
          >
            <span>
              <strong>
                {{ employee.firstName }}
                {{ employee.lastName }}
              </strong>
              <small>{{ employee.email }}</small>
            </span>

            <span class="status">
              {{ employee.status }}
            </span>
          </button>

          <div
            v-if="selectedEmployee === employee.id"
            class="employee-details"
          >
            <p v-if="employee.submittedAt">
              Last updated:
              {{
                new Date(employee.submittedAt)
                  .toLocaleString()
              }}
            </p>

            <p v-if="employee.status === 'NotSubmitted'">
              No submitted availability for this period.
            </p>

            <div class="days">
              <div
                v-for="date in dates"
                :key="date"
                class="day"
              >
                <strong>{{ formatDate(date) }}</strong>

                <p
                  v-for="(entry, index) in hoursFor(employee, date)"
                  :key="index"
                  class="hours"
                >
                  {{ formatTime(entry.startTime) }}
                  –
                  {{ formatTime(entry.endTime) }}
                </p>

                <small v-if="!hoursFor(employee, date).length">
                  No hours
                </small>
              </div>
            </div>
          </div>
        </article>
      </section>
    </template>
  </main>
</template>


<style scoped>
.availability-admin {
  width: 100%;
  max-width: 1450px;
  margin: 0 auto;
  padding: 40px 32px 64px;
  color: #30252d;
}

/* Page heading */
.availability-admin h1 {
  margin: 0 0 10px;
  color: #282528;
  font-size: clamp(30px, 3vw, 40px);
  font-weight: 750;
  letter-spacing: -0.04em;
}

.availability-admin > p {
  color: #756c73;
  font-size: 14px;
  line-height: 1.6;
}

/* Period navigation */
.period-nav {
  display: flex;
  align-items: center;
  gap: 16px;
  flex-wrap: wrap;
  margin: 28px 0 24px;
}

.period-nav button {
  padding: 10px 16px;
  background: #fff;
  color: #8b526f;
  border: 1px solid #e9dfe5;
  border-radius: 9px;
  font-size: 13px;
  font-weight: 700;
}

.period-nav button:hover:not(:disabled) {
  background: #f4e3ec;
}

.period-nav strong {
  color: #30252d;
  font-size: 15px;
}

button {
  cursor: pointer;
  font: inherit;
}

button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* Summary cards */
.summary {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
  margin-bottom: 24px;
}

.summary > div {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 0;
  padding: 24px;
  background: #fff;
  border: 1px solid #e9dfe5;
  border-radius: 14px;
  box-shadow: 0 4px 18px rgb(90 45 70 / 5%);
  color: #756c73;
  font-size: 13px;
}

.summary strong {
  color: #30252d;
  font-size: 32px;
  font-weight: 750;
  line-height: 1.1;
  letter-spacing: -0.04em;
}

/* Deadline information */
.deadline,
.note {
  margin: 10px 0;
  color: #756c73;
  font-size: 13px;
  line-height: 1.6;
}

/* Employee list */
.employee-list {
  display: grid;
  gap: 12px;
  margin-top: 26px;
}

.employee-card {
  min-width: 0;
  overflow: hidden;
  background: #fff;
  border: 1px solid #e9dfe5;
  border-radius: 13px;
  box-shadow: 0 3px 12px rgb(90 45 70 / 3%);
}

.employee-card:hover {
  border-color: #d9b4c8;
}

.employee-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
  width: 100%;
  min-height: 72px;
  padding: 16px 20px;
  border: 0;
  background: #fff;
  color: #30252d;
  text-align: left;
}

.employee-header:hover {
  background: #fcf8fa;
}

.employee-header span:first-child {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.employee-header strong {
  font-size: 14px;
  font-weight: 700;
}

.employee-header small {
  color: #928991;
  font-size: 12px;
}

.status {
  flex-shrink: 0;
  padding: 7px 11px;
  background: #f4e3ec;
  color: #80536d;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 700;
}

/* Expanded employee availability */
.employee-details {
  padding: 20px;
  border-top: 1px solid #e9dfe5;
  background: #fff;
}

.days {
  display: grid;
  grid-template-columns: repeat(7, minmax(0, 1fr));
  gap: 8px;
}

.day {
  min-width: 0;
  min-height: 100px;
  padding: 12px;
  background: #faf7f9;
  border: 1px solid #f0e7ec;
  border-radius: 9px;
}

.day strong {
  display: block;
  margin-bottom: 8px;
  color: #30252d;
  font-size: 12px;
}

.day small {
  color: #928991;
  font-size: 11px;
}

.hours {
  margin-top: 6px;
  padding: 7px;
  background: #f0dce7;
  color: #70405a;
  border-radius: 6px;
  font-size: 11px;
  font-weight: 600;
  overflow-wrap: anywhere;
}

button:focus-visible {
  outline: 2px solid #8b526f;
  outline-offset: 2px;
}

/* Tablet */
@media (max-width: 1000px) {
  .days {
    grid-template-columns: repeat(4, minmax(0, 1fr));
  }
}

/* Mobile */
@media (max-width: 700px) {
  .availability-admin {
    padding: 24px 16px 48px;
  }

  .summary {
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 8px;
  }

  .summary > div {
    padding: 14px;
  }

  .summary strong {
    font-size: 26px;
  }

  .days {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .employee-header {
    padding: 14px;
  }

  .employee-details {
    padding: 14px;
  }
}

@media (max-width: 420px) {
  .summary {
    grid-template-columns: 1fr;
  }
}
</style>
