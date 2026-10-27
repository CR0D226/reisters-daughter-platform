
<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { apiFetch } from '../services/api'

type AvailabilityWindow = {
  dayOfWeek: number
  startTime: string
  endTime: string
}

const weekdays = [
  'Sunday', 'Monday', 'Tuesday', 'Wednesday',
  'Thursday', 'Friday', 'Saturday'
]

const today = new Date()
const currentMonth = ref(
  new Date(today.getFullYear(), today.getMonth(), 1)
)
const selectedDate = ref(
  new Date(today.getFullYear(), today.getMonth(), today.getDate())
)

const windows = ref<AvailabilityWindow[]>([])
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const message = ref('')
const dirty = ref(false)

const selectedWeekday = computed(
  () => selectedDate.value.getDay()
)

const selectedWindows = computed(() =>
  windows.value.filter(
    w => w.dayOfWeek === selectedWeekday.value
  )
)

const monthLabel = computed(() =>
  currentMonth.value.toLocaleDateString('en-US', {
    month: 'long',
    year: 'numeric'
  })
)

const selectedLabel = computed(() =>
  selectedDate.value.toLocaleDateString('en-US', {
    weekday: 'long',
    month: 'long',
    day: 'numeric'
  })
)

const calendarDays = computed(() => {
  const year = currentMonth.value.getFullYear()
  const month = currentMonth.value.getMonth()

  const firstDay = new Date(year, month, 1).getDay()
  const daysInMonth = new Date(
    year, month + 1, 0
  ).getDate()

  const cells: (Date | null)[] = []

  for (let i = 0; i < firstDay; i++) {
    cells.push(null)
  }

  for (let day = 1; day <= daysInMonth; day++) {
    cells.push(new Date(year, month, day))
  }

  while (cells.length % 7 !== 0) {
    cells.push(null)
  }

  return cells
})

const timeOptions = Array.from(
  { length: 96 },
  (_, i) => {
    const hours = Math.floor(i / 4)
    const minutes = (i % 4) * 15

    const hour12 = hours % 12 || 12
    const period = hours < 12 ? 'AM' : 'PM'

    return {
      value:
        `${String(hours).padStart(2, '0')}:` +
        `${String(minutes).padStart(2, '0')}:00`,
      label:
        `${hour12}:` +
        `${String(minutes).padStart(2, '0')} ${period}`
    }
  }
)

const endTimeOptions = [
  ...timeOptions,
  {
    value: '23:59:59',
    label: 'End of day'
  }
]

function formatTime(time: string): string {
  if (time === '23:59:59') {
    return 'End of day'
  }

  const [hourPart = '0', minutePart = '00'] =
    time.split(':')

  const hour = Number(hourPart)
  const period = hour < 12 ? 'AM' : 'PM'
  const hour12 = hour % 12 || 12

  return `${hour12}:${minutePart} ${period}`
}

function getWindows(dayOfWeek: number) {
  return windows.value
    .filter(w => w.dayOfWeek === dayOfWeek)
    .sort((a, b) =>
      a.startTime.localeCompare(b.startTime)
    )
}

function isSelected(date: Date) {
  return (
    date.getFullYear() ===
      selectedDate.value.getFullYear() &&
    date.getMonth() ===
      selectedDate.value.getMonth() &&
    date.getDate() ===
      selectedDate.value.getDate()
  )
}

function isToday(date: Date) {
  return (
    date.getFullYear() === today.getFullYear() &&
    date.getMonth() === today.getMonth() &&
    date.getDate() === today.getDate()
  )
}

function changeMonth(offset: number) {
  const year = currentMonth.value.getFullYear()
  const month = currentMonth.value.getMonth()

  const next = new Date(year, month + offset, 1)

  currentMonth.value = next
  selectedDate.value = new Date(
    next.getFullYear(),
    next.getMonth(),
    1
  )
}

function goToToday() {
  currentMonth.value = new Date(
    today.getFullYear(),
    today.getMonth(),
    1
  )

  selectedDate.value = new Date(
    today.getFullYear(),
    today.getMonth(),
    today.getDate()
  )
}

function selectDate(date: Date) {
  selectedDate.value = new Date(
    date.getFullYear(),
    date.getMonth(),
    date.getDate()
  )
}

function markChanged() {
  dirty.value = true
  message.value = ''
  error.value = ''
}

function addWindow() {
  if (windows.value.length >= 28) {
    error.value = 'Maximum 28 availability windows.'
    return
  }

  windows.value.push({
    dayOfWeek: selectedWeekday.value,
    startTime: '08:00:00',
    endTime: '16:00:00'
  })

  markChanged()
}

function removeWindow(window: AvailabilityWindow) {
  windows.value = windows.value.filter(
    item => item !== window
  )

  markChanged()
}

function validate(): string {
  if (windows.value.length > 28) {
    return 'Maximum 28 availability windows.'
  }

  for (const window of windows.value) {
    if (
      !Number.isInteger(window.dayOfWeek) ||
      window.dayOfWeek < 0 ||
      window.dayOfWeek > 6
    ) {
      return 'Invalid weekday.'
    }

    if (window.endTime <= window.startTime) {
      return `Invalid hours on ${
        weekdays[window.dayOfWeek]
      }.`
    }
  }

  for (let day = 0; day < 7; day++) {
    const sorted = getWindows(day)

    for (let i = 1; i < sorted.length; i++) {
      const current = sorted[i]
      const previous = sorted[i - 1]

      if (
        current &&
        previous &&
        current.startTime < previous.endTime
      ) {
        return `Overlapping hours on ${weekdays[day]}.`
      }
    }
  }

  return ''
}

async function loadAvailability() {
  loading.value = true
  error.value = ''

  try {
    const response = await apiFetch(
      '/api/employee/availability'
    )

    if (!response.ok) {
      throw new Error(
        `Load failed (${response.status}).`
      )
    }

    windows.value = await response.json()
    dirty.value = false
  } catch (err) {
    error.value =
      err instanceof Error
        ? err.message
        : 'Could not load availability.'
  } finally {
    loading.value = false
  }
}

async function saveAvailability() {
  error.value = ''
  message.value = ''

  const validation = validate()

  if (validation) {
    error.value = validation
    return
  }

  saving.value = true

  try {
    const response = await apiFetch(
      '/api/employee/availability',
      {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(windows.value)
      }
    )

    if (!response.ok) {
      throw new Error(
        `Save failed (${response.status}): ` +
        await response.text()
      )
    }

    dirty.value = false
    message.value = 'Weekly availability saved.'
  } catch (err) {
    error.value =
      err instanceof Error
        ? err.message
        : 'Could not save availability.'
  } finally {
    saving.value = false
  }
}

onMounted(loadAvailability)
</script>

<template>
  <main class="availability-page">
    <header class="page-header">
      <h1>My Availability</h1>
      <p>
        Select a date to edit your regular weekly
        availability. Changes to a weekday apply
        to every week.
      </p>
    </header>

    <p v-if="loading">Loading availability...</p>

    <template v-else>
      <div
        v-if="error"
        class="notice error"
        role="alert"
      >
        {{ error }}
      </div>

      <div
        v-if="message"
        class="notice success"
        role="status"
      >
        {{ message }}
      </div>

      <div
        v-if="dirty"
        class="notice unsaved"
        role="status"
      >
        You have unsaved changes.
      </div>

      <section class="calendar-card">
        <div class="calendar-header">
          <div class="month-navigation">
            <button
              type="button"
              class="nav-button"
              aria-label="Previous month"
              @click="changeMonth(-1)"
            >
              ‹
            </button>

            <h2>{{ monthLabel }}</h2>

            <button
              type="button"
              class="nav-button"
              aria-label="Next month"
              @click="changeMonth(1)"
            >
              ›
            </button>
          </div>

          <button
            type="button"
            class="today-button"
            @click="goToToday"
          >
            Today
          </button>
        </div>

        <div class="calendar-grid">
          <div
            v-for="weekday in weekdays"
            :key="weekday"
            class="weekday-heading"
          >
            {{ weekday.slice(0, 3) }}
          </div>

          <template
            v-for="(date, index) in calendarDays"
            :key="index"
          >
            <div
              v-if="date === null"
              class="empty-day"
            />

            <button
              v-else
              type="button"
              class="calendar-day"
              :class="{
                selected: isSelected(date),
                today: isToday(date)
              }"
              :aria-pressed="isSelected(date)"
              @click="selectDate(date)"
            >
              <span class="day-number">
                {{ date.getDate() }}
              </span>

              <div class="day-windows">
                <span
                  v-for="(window, i) in
                    getWindows(date.getDay())"
                  :key="i"
                  class="availability-chip"
                >
                  {{ formatTime(window.startTime) }}
                  –
                  {{ formatTime(window.endTime) }}
                </span>

                <span
                  v-if="
                    getWindows(date.getDay()).length === 0
                  "
                  class="unavailable-label"
                >
                  Unavailable
                </span>
              </div>
            </button>
          </template>
        </div>
      </section>

      <section class="editor-card">
        <div class="editor-header">
          <div>
            <h2>{{ selectedLabel }}</h2>
            <p>
              Editing every
              <strong>
                {{ weekdays[selectedWeekday] }}
              </strong>
            </p>
          </div>

          <button
            type="button"
            class="add-button"
            :disabled="
              saving ||
              windows.length >= 28
            "
            @click="addWindow"
          >
            + Add hours
          </button>
        </div>

        <p
          v-if="selectedWindows.length === 0"
          class="empty-message"
        >
          Unavailable on this weekday.
          Add hours to change this.
        </p>

        <div
          v-for="(window, index) in selectedWindows"
          :key="index"
          class="time-row"
        >
          <label>
            From
            <select
              v-model="window.startTime"
              :disabled="saving"
              @change="markChanged"
            >
              <option
                v-for="time in timeOptions"
                :key="time.value"
                :value="time.value"
              >
                {{ time.label }}
              </option>
            </select>
          </label>

          <label>
            To
            <select
              v-model="window.endTime"
              :disabled="saving"
              @change="markChanged"
            >
              <option
                v-for="time in endTimeOptions"
                :key="time.value"
                :value="time.value"
              >
                {{ time.label }}
              </option>
            </select>
          </label>

          <button
            type="button"
            class="remove-button"
            :disabled="saving"
            @click="removeWindow(window)"
          >
            Remove
          </button>
        </div>

        <div class="editor-footer">
          <span class="recurring-note">
            Changes repeat weekly.
          </span>

          <button
            type="button"
            class="save-button"
            :disabled="saving || !dirty"
            @click="saveAvailability"
          >
            {{
              saving
                ? 'Saving...'
                : 'Save Availability'
            }}
          </button>
        </div>
      </section>
    </template>
  </main>
</template>


<style scoped>
.availability-page {
  width: 100%;
  max-width: 1450px;
  margin: 0 auto;
  padding: 40px 32px 64px;
  color: #30252d;
}

.page-header {
  margin-bottom: 28px;
}

.page-header h1 {
  margin: 0 0 10px;
  color: #282528;
  font-size: clamp(30px, 3vw, 40px);
  letter-spacing: -0.04em;
}

.page-header p {
  margin: 0;
  color: #756c73;
  font-size: 14px;
  line-height: 1.6;
}

.calendar-card,
.editor-card {
  min-width: 0;
  margin-bottom: 22px;
  padding: 24px;
  background: #fff;
  color: #30252d;
  border: 1px solid #e9dfe5;
  border-radius: 16px;
  box-shadow: 0 4px 18px rgb(90 45 70 / 5%);
}

.calendar-header,
.month-navigation,
.editor-header,
.editor-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
}

.month-navigation {
  justify-content: flex-start;
}

.month-navigation h2 {
  min-width: 180px;
  margin: 0;
  color: #30252d;
  text-align: center;
  font-size: 19px;
  font-weight: 700;
}

button {
  cursor: pointer;
  font: inherit;
  font-size: 13px;
  font-weight: 600;
}

.nav-button,
.today-button,
.add-button {
  padding: 10px 14px;
  background: #faf7f9;
  color: #8b526f;
  border: 1px solid #e9dfe5;
  border-radius: 9px;
}

.nav-button:hover:not(:disabled),
.today-button:hover:not(:disabled),
.add-button:hover:not(:disabled) {
  background: #f4e3ec;
}

.calendar-grid {
  display: grid;
  grid-template-columns: repeat(7, minmax(0, 1fr));
  gap: 7px;
  margin-top: 20px;
}

.weekday-heading {
  padding: 10px 2px;
  color: #8b526f;
  text-align: center;
  font-size: 12px;
  font-weight: 700;
}

.calendar-day,
.empty-day {
  min-width: 0;
  min-height: 112px;
  border-radius: 10px;
}

.calendar-day {
  display: flex;
  flex-direction: column;
  align-items: stretch;
  gap: 8px;
  padding: 10px;
  background: #faf7f9;
  border: 1px solid #eee5eb;
  color: #30252d;
  text-align: left;
  transition: background 0.15s, border-color 0.15s;
}

.calendar-day:hover:not(:disabled) {
  background: #f4e3ec;
  border-color: #d9b4c8;
}

.calendar-day.selected {
  border: 2px solid #8b526f;
  background: #f7eaf1;
}

.calendar-day.today .day-number {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 27px;
  height: 27px;
  border-radius: 50%;
  background: #8b526f;
  color: #fff;
}

.day-number {
  font-size: 14px;
  font-weight: 700;
}

.day-windows {
  display: flex;
  flex-direction: column;
  gap: 5px;
  min-width: 0;
}

.availability-chip {
  padding: 6px;
  background: #f0dce7;
  color: #70405a;
  border-radius: 6px;
  font-size: 11px;
  font-weight: 600;
  overflow-wrap: anywhere;
}

.unavailable-label {
  color: #a39aa1;
  font-size: 11px;
}

.editor-header {
  align-items: flex-start;
}

.editor-header h2 {
  margin: 0 0 8px;
  color: #30252d;
  font-size: 20px;
}

.editor-header p,
.recurring-note {
  color: #756c73;
  font-size: 13px;
  line-height: 1.6;
}

.time-row {
  display: flex;
  align-items: end;
  flex-wrap: wrap;
  gap: 14px;
  margin-top: 18px;
}

.time-row label {
  display: flex;
  flex: 1;
  min-width: 140px;
  flex-direction: column;
  gap: 7px;
  color: #756c73;
  font-size: 12px;
  font-weight: 600;
}

select {
  width: 100%;
  min-width: 0;
  padding: 11px;
  border: 1px solid #e9dfe5;
  border-radius: 9px;
  background: #fff;
  color: #30252d;
  font: inherit;
  font-size: 13px;
}

select:focus-visible,
button:focus-visible {
  outline: 2px solid #8b526f;
  outline-offset: 2px;
}

.remove-button {
  padding: 11px 14px;
  background: #fff;
  color: #a43c45;
  border: 1px solid #e9c4ca;
  border-radius: 9px;
}

.remove-button:hover:not(:disabled) {
  background: #fce8e8;
}

.empty-message {
  padding: 22px 0;
  color: #928991;
  font-size: 13px;
}

.editor-footer {
  margin-top: 22px;
  padding-top: 18px;
  border-top: 1px solid #e9dfe5;
}

.save-button {
  padding: 13px 20px;
  border: none;
  border-radius: 9px;
  background: #8b526f;
  color: #fff;
  font-weight: 700;
}

.save-button:hover:not(:disabled) {
  background: #70405a;
}

button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.notice {
  margin-bottom: 14px;
  padding: 12px 14px;
  border-radius: 9px;
  font-size: 13px;
}

.error {
  background: #fce8e8;
  color: #922;
}

.success {
  background: #e5f3e9;
  color: #215c32;
}

.unsaved {
  background: #fff3d8;
  color: #805600;
}

@media (max-width: 900px) {
  .availability-page {
    padding: 28px 20px;
  }

  .calendar-card,
  .editor-card {
    padding: 18px;
  }

  .calendar-grid {
    gap: 4px;
  }

  .calendar-day,
  .empty-day {
    min-height: 95px;
  }

  .calendar-day {
    padding: 6px;
  }
}

@media (max-width: 700px) {
  .availability-page {
    padding: 24px 12px 48px;
  }

  .calendar-card,
  .editor-card {
    padding: 12px;
  }

  .calendar-header,
  .month-navigation {
    flex-wrap: wrap;
  }

  .month-navigation h2 {
    min-width: 130px;
    font-size: 16px;
  }

  .calendar-day,
  .empty-day {
    min-height: 75px;
  }

  .calendar-day {
    padding: 4px;
    gap: 5px;
  }

  .day-number {
    font-size: 12px;
  }

  .availability-chip {
    padding: 3px;
    font-size: 9px;
  }

  .unavailable-label {
    display: none;
  }

  .editor-header,
  .editor-footer {
    align-items: flex-start;
    flex-wrap: wrap;
  }

  .time-row {
    flex-direction: column;
    align-items: stretch;
  }

  .time-row label {
    width: 100%;
  }
}
</style>
