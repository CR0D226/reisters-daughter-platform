
<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { apiFetch } from '@/services/api'

type Shift = {
  id: number
  date: string
  startTime: string
  endTime: string
  position: string
}

const periodIndex = ref(0)
const selectedWeek = ref<0 | 1>(0)
const shifts = ref<Shift[]>([])
const published = ref(false)
const loading = ref(false)
const error = ref('')

function dateKey(date: Date): string {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

const days = computed(() =>
  Array.from({ length: 14 }, (_, i) =>
    dateKey(new Date(2026, 10, 2 + periodIndex.value * 14 + i))
  )
)

const visibleDays = computed(() =>
  days.value.slice(selectedWeek.value * 7, selectedWeek.value * 7 + 7)
)

const startDate = computed(() => days.value[0]!)

function displayDate(value: string): string {
  const [year, month, day] = value.split('-').map(Number)
  return new Date(year!, month! - 1, day!).toLocaleDateString(
    'en-US',
    { weekday: 'short', month: 'short', day: 'numeric' }
  )
}

function displayTime(value: string): string {
  const [hour, minute] = value.split(':').map(Number)
  return `${hour! % 12 || 12}:${String(minute).padStart(2, '0')} ${
    hour! < 12 ? 'AM' : 'PM'
  }`
}

function shiftsOn(date: string): Shift[] {
  return shifts.value.filter(shift => shift.date === date)
}

async function load() {
  loading.value = true
  error.value = ''
  published.value = false
  shifts.value = []

  try {
    const response = await apiFetch(
      `/api/employee/schedule?startDate=${startDate.value}`
    )

    if (response.status === 404) return

    if (!response.ok) {
      throw new Error(await response.text())
    }

    const data = await response.json()
    published.value = true
    shifts.value = data.shifts
  } catch (e) {
    error.value = String(e)
  } finally {
    loading.value = false
  }
}

async function changePeriod(offset: number) {
  if (periodIndex.value + offset < 0) return
  periodIndex.value += offset
  selectedWeek.value = 0
  await load()
}

onMounted(load)
</script>

<template>
  <main class="my-schedule">
    <h1>My Schedule</h1>
    <p>View your published shifts for each two-week period.</p>

    <div class="toolbar">
      <button
        :disabled="periodIndex === 0 || loading"
        @click="changePeriod(-1)"
      >
        Previous
      </button>

      <strong>
        {{ displayDate(days[0]!) }} –
        {{ displayDate(days[13]!) }}
      </strong>

      <button :disabled="loading" @click="changePeriod(1)">
        Next
      </button>
    </div>

    <p v-if="loading">Loading your schedule...</p>
    <p v-if="error" role="alert">{{ error }}</p>

    <template v-if="!loading && !error">
      <p v-if="!published" class="empty">
        Your manager hasn't published the schedule for this period yet.
      </p>

      <template v-else>
        <div class="week-switcher">
          <button
            :class="{ selected: selectedWeek === 0 }"
            @click="selectedWeek = 0"
          >
            Week 1
          </button>
          <button
            :class="{ selected: selectedWeek === 1 }"
            @click="selectedWeek = 1"
          >
            Week 2
          </button>
        </div>

        <div class="calendar">
          <section
            v-for="day in visibleDays"
            :key="day"
            class="day"
          >
            <strong>{{ displayDate(day) }}</strong>

            <p v-if="shiftsOn(day).length === 0" class="no-shift">
              No shift
            </p>

            <article
              v-for="shift in shiftsOn(day)"
              :key="shift.id"
              class="shift"
              :class="`shift-${shift.position.toLowerCase()}`"
            >
              <strong>{{ shift.position }}</strong>
              <p>
                {{ displayTime(shift.startTime) }} –
                {{ displayTime(shift.endTime) }}
              </p>
            </article>
          </section>
        </div>
      </template>
    </template>
  </main>
</template>

<style scoped>
.my-schedule {
  max-width: 1450px;
  margin: 0 auto;
  padding: 28px;
  color: #30252d;
}

.toolbar,
.week-switcher {
  display: flex;
  align-items: center;
  gap: 12px;
  margin: 22px 0;
}

.toolbar {
  justify-content: space-between;
}

button {
  padding: 10px 16px;
  border: 1px solid #e9dfe5;
  border-radius: 9px;
  background: white;
  cursor: pointer;
}

button.selected {
  background: #8b526f;
  color: white;
}

button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.calendar {
  display: grid;
  grid-template-columns: repeat(7, minmax(0, 1fr));
  gap: 10px;
}

.day {
  min-height: 180px;
  padding: 14px;
  background: white;
  border: 1px solid #e9dfe5;
  border-radius: 12px;
}

.shift {
  margin-top: 12px;
  padding: 12px;
  border-radius: 9px;
  border-left: 4px solid #c979a2;
  background: #fdf0f6;
}

.shift-baker {
  border-left-color: #c49a64;
  background: #fbf3e7;
}

.shift-cashier {
  border-left-color: #9b87c9;
  background: #f3effc;
}

.shift p,
.no-shift {
  font-size: 13px;
}

.no-shift {
  color: #8c858b;
}

.empty {
  padding: 24px;
  background: #f4e3ec;
  border-radius: 12px;
}

@media (max-width: 900px) {
  .calendar {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 550px) {
  .calendar {
    grid-template-columns: 1fr;
  }
}
</style>
