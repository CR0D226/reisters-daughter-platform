<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { apiFetch } from '../services/api'

type Entry = { date: string; startTime: string; endTime: string }
type Period = {
  startDate: string
  endDate: string
  deadline: string
  isLocked: boolean
  status: string
  source: string
  entries: Entry[]
}

const FIRST = '2026-11-02'
const startDate = ref(FIRST)
const period = ref<Period | null>(null)
const entries = ref<Entry[]>([])
const loading = ref(false)
const saving = ref(false)
const dirty = ref(false)
const error = ref('')
const message = ref('')

function parseDate(value: string): Date {
  const [y, m, d] = value.split('-').map(Number)
  return new Date(y!, m! - 1, d!)
}
function isoDate(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}
function addDays(value: string, amount: number): string {
  const date = parseDate(value)
  date.setDate(date.getDate() + amount)
  return isoDate(date)
}
function dateLabel(value: string): string {
  return parseDate(value).toLocaleDateString('en-US', {
    weekday: 'short', month: 'short', day: 'numeric', year: 'numeric'
  })
}
function formatTime(value: string): string {
  if (value === '23:59:59') return 'End of day'
  const [hours, minutes] = value.split(':').map(Number)
  return `${hours! % 12 || 12}:${String(minutes).padStart(2, '0')} ${hours! < 12 ? 'AM' : 'PM'}`
}
const timeOptions = Array.from({ length: 96 }, (_, index) => {
  const hour = Math.floor(index / 4)
  const minute = (index % 4) * 15
  const value = `${String(hour).padStart(2, '0')}:${String(minute).padStart(2, '0')}:00`
  return { value, label: formatTime(value) }
})
const endTimeOptions = [...timeOptions, { value: '23:59:59', label: 'End of day' }]
const days = computed(() => Array.from({ length: 14 }, (_, index) => addDays(startDate.value, index)))
const locked = computed(() => period.value?.isLocked || (period.value ? Date.now() >= new Date(period.value.deadline).getTime() : true))
const deadlineLabel = computed(() => period.value
  ? new Date(period.value.deadline).toLocaleString('en-US', {
    timeZone: 'America/New_York', month: 'short', day: 'numeric', year: 'numeric',
    hour: 'numeric', minute: '2-digit', timeZoneName: 'short'
  }) : '')
const sourceLabel = computed(() => {
  switch (period.value?.source) {
    case 'WeeklyTemplate': return 'Loaded from your weekly template'
    case 'PreviousPeriod': return 'Copied from your previous two-week period'
    case 'Saved': return 'Loaded from saved availability'
    default: return ''
  }
})
function windowsFor(date: string): Entry[] {
  return entries.value.filter(entry => entry.date === date)
}
function changed() {
  dirty.value = true
  message.value = ''
  error.value = ''
}
function addWindow(date: string) {
  if (locked.value || saving.value || entries.value.length >= 56) return
  entries.value.push({ date, startTime: '08:00:00', endTime: '16:00:00' })
  changed()
}
function removeWindow(entry: Entry) {
  if (locked.value || saving.value) return
  entries.value = entries.value.filter(item => item !== entry)
  changed()
}
function validate(): string {
  if (entries.value.length > 56) return 'Maximum 56 windows per period.'
  for (const date of days.value) {
    const sorted = windowsFor(date).slice().sort((a, b) => a.startTime.localeCompare(b.startTime))
    for (let index = 0; index < sorted.length; index++) {
      const current = sorted[index]!
      if (current.endTime <= current.startTime) return `Invalid time range on ${dateLabel(date)}.`
      if (index && current.startTime < sorted[index - 1]!.endTime)
        return `Overlapping hours on ${dateLabel(date)}.`
    }
  }
  return ''
}
async function loadPeriod() {
  loading.value = true
  error.value = ''
  message.value = ''
  period.value = null
  try {
    const response = await apiFetch(`/api/employee/availability/period?startDate=${startDate.value}`)
    if (!response.ok) throw new Error(`Unable to load period (${response.status}): ${await response.text()}`)
    const result: Period = await response.json()
    period.value = result
    entries.value = result.entries.map(entry => ({ ...entry }))
    dirty.value = false
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to load availability.'
  } finally {
    loading.value = false
  }
}
async function movePeriod(offset: number) {
  if (dirty.value && !window.confirm('Discard your unsaved changes?')) return
  const next = addDays(startDate.value, offset * 14)
  if (next < FIRST) return
  startDate.value = next
  await loadPeriod()
}
async function savePeriod() {
  if (!period.value || locked.value || saving.value || loading.value) return
  error.value = ''
  message.value = ''
  const issue = validate()
  if (issue) { error.value = issue; return }
  if (!window.confirm(`Submit availability for ${dateLabel(startDate.value)} through ${dateLabel(period.value.endDate)}?`)) return
  saving.value = true
  try {
    const response = await apiFetch('/api/employee/availability/period', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ startDate: startDate.value, entries: entries.value })
    })
    if (!response.ok) throw new Error(`Submission failed (${response.status}): ${await response.text()}`)
    await loadPeriod()
    message.value = 'Availability submitted successfully.'
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to submit availability.'
  } finally {
    saving.value = false
  }
}
onMounted(loadPeriod)
</script>

<template>
  <main class="availability-page">
    <header class="page-header">
      <h1>Biweekly Availability</h1>
      <p>Set your available hours for each date in the two-week scheduling period.</p>
      <RouterLink to="/employee/availability">Edit recurring weekly template</RouterLink>
    </header>

    <section class="calendar-card">
      <div class="period-navigation">
        <button type="button" :disabled="loading || saving || startDate === FIRST" @click="movePeriod(-1)">← Previous</button>
        <h2>{{ dateLabel(startDate) }} – {{ dateLabel(addDays(startDate, 13)) }}</h2>
        <button type="button" :disabled="loading || saving" @click="movePeriod(1)">Next →</button>
      </div>
      <p v-if="loading">Loading availability…</p>
      <template v-if="period && !loading">
        <div class="period-details">
          <p><strong>Status:</strong> {{ period.status }}</p>
          <p><strong>Deadline:</strong> {{ deadlineLabel }}</p>
          <p>{{ sourceLabel }}</p>
          <p v-if="locked" class="locked">This period is locked. Contact a manager if changes are needed.</p>
          <p v-else-if="period.status === 'Submitted'">You can update this submission until the deadline.</p>
        </div>
      </template>
      <p v-if="error" role="alert" class="notice error">{{ error }}</p>
      <p v-if="message" role="status" class="notice success">{{ message }}</p>
      <p v-if="dirty" role="status" class="notice unsaved">You have unsaved changes.</p>
    </section>

    <template v-if="period && !loading">
      <div class="days-grid">
        <section v-for="date in days" :key="date" class="day-card">
          <h3>{{ dateLabel(date) }}</h3>
          <p v-if="!windowsFor(date).length" class="empty-message">Unavailable</p>
          <div v-for="(entry, index) in windowsFor(date)" :key="index" class="time-row">
            <label>From
              <select v-model="entry.startTime" :disabled="locked || saving" @change="changed">
                <option v-for="time in timeOptions" :key="time.value" :value="time.value">{{ time.label }}</option>
              </select>
            </label>
            <label>To
              <select v-model="entry.endTime" :disabled="locked || saving" @change="changed">
                <option v-for="time in endTimeOptions" :key="time.value" :value="time.value">{{ time.label }}</option>
              </select>
            </label>
            <button type="button" class="remove-button" :disabled="locked || saving" @click="removeWindow(entry)">Remove</button>
          </div>
          <button type="button" class="add-button" :disabled="locked || saving || entries.length >= 56" @click="addWindow(date)">+ Add hours</button>
        </section>
      </div>
      <div class="editor-footer">
        <span>{{ entries.length }} availability windows across 14 days</span>
        <button type="button" class="save-button" :disabled="locked || saving || loading || !dirty" @click="savePeriod">
          {{ saving ? 'Submitting…' : period.status === 'Submitted' ? 'Update Submission' : 'Submit Availability' }}
        </button>
      </div>
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
  margin: 0 0 12px;
  color: #756c73;
  font-size: 14px;
  line-height: 1.6;
}

.page-header a {
  color: #8b526f;
  font-size: 13px;
  font-weight: 700;
  text-decoration: none;
}

.page-header a:hover {
  text-decoration: underline;
}

.calendar-card,
.day-card {
  min-width: 0;
  padding: 22px;
  background: #fff;
  border: 1px solid #e9dfe5;
  border-radius: 14px;
  box-shadow: 0 4px 18px rgb(90 45 70 / 5%);
}

.calendar-card {
  margin-bottom: 22px;
}

.period-navigation,
.editor-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
}

.period-navigation h2 {
  margin: 0;
  color: #30252d;
  font-size: 17px;
  text-align: center;
}

button {
  padding: 10px 14px;
  border-radius: 9px;
  font: inherit;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
}

.period-navigation button,
.add-button {
  border: 1px solid #e9dfe5;
  background: #faf7f9;
  color: #8b526f;
}

.period-navigation button:hover:not(:disabled),
.add-button:hover:not(:disabled) {
  background: #f4e3ec;
}

.period-details {
  display: flex;
  flex-wrap: wrap;
  column-gap: 28px;
  row-gap: 4px;
  margin-top: 22px;
  padding-top: 16px;
  border-top: 1px solid #e9dfe5;
  color: #756c73;
  font-size: 13px;
}

.period-details p {
  margin: 6px 0;
}

.period-details strong {
  color: #30252d;
}

.period-details .locked {
  width: 100%;
  color: #a43c45;
  font-weight: 700;
}

.days-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}

.day-card h3 {
  margin: 0 0 16px;
  padding-bottom: 12px;
  border-bottom: 1px solid #f0e7ec;
  color: #30252d;
  font-size: 16px;
}

.time-row {
  display: flex;
  align-items: end;
  flex-wrap: wrap;
  gap: 10px;
  margin-bottom: 14px;
}

.time-row label {
  display: flex;
  flex: 1;
  min-width: 115px;
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
  border-radius: 8px;
  background: #fff;
  color: #30252d;
  font: inherit;
  font-size: 13px;
}

select:focus-visible,
button:focus-visible,
a:focus-visible {
  outline: 2px solid #8b526f;
  outline-offset: 2px;
}

.remove-button {
  border: 1px solid #e9c4ca;
  background: #fff;
  color: #a43c45;
}

.add-button {
  margin-top: 8px;
}

.empty-message {
  margin: 0 0 14px;
  color: #928991;
  font-size: 13px;
}

.editor-footer {
  margin-top: 22px;
  padding: 20px 24px;
  border: 1px solid #e9dfe5;
  border-radius: 14px;
  background: #fff;
  color: #756c73;
  font-size: 13px;
}

.save-button {
  border: 0;
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
  margin-top: 14px;
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

@media (max-width: 1000px) {
  .days-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 700px) {
  .availability-page {
    padding: 24px 16px 48px;
  }

  .calendar-card,
  .day-card {
    padding: 16px;
  }

  .period-navigation {
    flex-wrap: wrap;
    justify-content: center;
  }

  .period-navigation h2 {
    width: 100%;
    order: -1;
  }

  .editor-footer {
    flex-direction: column;
    align-items: stretch;
  }

  .save-button {
    width: 100%;
  }
}
</style>
