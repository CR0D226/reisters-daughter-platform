<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { apiFetch } from '@/services/api'
import EmployeePicker from '@/components/admin/EmployeePicker.vue'

type Position = 'Barista' | 'Cashier' | 'Baker'
type Assignment = {
  appUserId: number
  firstName: string
  lastName: string
}
type Shift = {
  id: number
  date: string
  startTime: string
  endTime: string
  position: string
  requiredEmployees: number
  assignments: Assignment[]
}
type Employee = {
  id: number
  firstName: string
  lastName: string
  status: string
  positions: string[]
  entries: {
    date: string
    startTime: string
    endTime: string
  }[]
}

const view = ref<'shift' | 'employee'>('shift')
const periodIndex = ref(0)
const selectedWeek = ref<0 | 1>(0)
const schedule = ref<{ id: number; status: string } | null>(null)
const shifts = ref<Shift[]>([])
const employees = ref<Employee[]>([])
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const selectedEmployee = ref<number | null>(null)
const editingId = ref<number | null>(null)
const formOpen = ref(false)

const form = ref({
  date: '2026-11-02',
  startTime: '07:00',
  endTime: '14:00',
  position: 'Barista' as Position,
  requiredEmployees: 1
})

function key(date: Date) {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

const days = computed(() =>
  Array.from({ length: 14 }, (_, i) =>
    key(new Date(2026, 10, 2 + periodIndex.value * 14 + i))
  )
)
const visibleDays = computed(() => {
  const start = selectedWeek.value * 7
  return days.value.slice(start, start + 7)
})


const startDate = computed(() => days.value[0]!)


function isQualified(employee: Employee, position: string): boolean {
  return employee.positions?.includes(position) ?? false
}
function availabilityLabel(employee: Employee, shift: Shift): string {
  if (employee.status === 'NotSubmitted') {
    return '⚠ Not submitted'
  }

  if (coversShift(employee, shift)) {
    return '✓ Available'
  }

  return '✕ Unavailable'
}

function displayDate(value: string) {
  const [y, m, d] = value.split('-').map(Number)
  return new Date(y!, m! - 1, d!).toLocaleDateString(
    'en-US',
    { weekday: 'short', month: 'short', day: 'numeric' }
  )
}

function displayTime(value: string) {
  const [h, m] = value.split(':').map(Number)
  return `${h! % 12 || 12}:${String(m).padStart(2, '0')} ${
    h! < 12 ? 'AM' : 'PM'
  }`
}

function shiftsOn(date: string) {
  return shifts.value.filter(s => s.date === date)
}

function employeeShifts(id: number) {
  return shifts.value.filter(s =>
    s.assignments.some(a => a.appUserId === id)
  )
}

function conflictingShift(
  employeeId: number,
  targetShift: Shift,
): Shift | undefined {
  return shifts.value.find(shift =>
    shift.id !== targetShift.id &&
    shift.date === targetShift.date &&
    shift.startTime < targetShift.endTime &&
    shift.endTime > targetShift.startTime &&
    shift.assignments.some(a => a.appUserId === employeeId)
  )
}

function coversShift(employee: Employee, shift: Shift) {
  if (employee.status === 'NotSubmitted') return false

  const windows = employee.entries
    .filter(e => e.date === shift.date)
    .sort((a, b) => a.startTime.localeCompare(b.startTime))

  let covered = shift.startTime

  for (const window of windows) {
    if (window.startTime > covered) break
    if (window.endTime > covered) covered = window.endTime
    if (covered >= shift.endTime) return true
  }

  return false
}

async function request(url: string, options?: RequestInit) {
  const response = await apiFetch(url, options)

  if (!response.ok) {
    const text = await response.text()
    throw new Error(text || `Request failed: ${response.status}`)
  }

  if (response.status === 204) return null
  return response.json()
}

async function load() {
  loading.value = true
  error.value = ''

  try {
    const [scheduleResponse, availability] = await Promise.all([
      apiFetch(
        `/api/admin/schedules?startDate=${startDate.value}`
      ),
      request(
        `/api/admin/availability/period?startDate=${startDate.value}`
      )
    ])

    if (scheduleResponse.status === 404) {
      schedule.value = null
      shifts.value = []
    } else {
      if (!scheduleResponse.ok) {
        throw new Error(await scheduleResponse.text())
      }

      const data = await scheduleResponse.json()
      schedule.value = data.schedule
      shifts.value = data.shifts
    }

    employees.value = availability.employees
  } catch (e) {
    error.value = String(e)
  } finally {
    loading.value = false
  }
}

async function perform(action: () => Promise<unknown>) {
  busy.value = true
  error.value = ''
  notice.value = ''

  try {
    await action()
    await load()
  } catch (e) {
    error.value = String(e)
  } finally {
    busy.value = false
  }
}
async function publishSchedule() {
  if (!schedule.value || schedule.value.status !== 'Draft') return

  const confirmed = window.confirm(
    'Publish this two-week schedule?\n\n' +
    'Employees will be able to view their assigned shifts once ' +
    'the employee schedule page is enabled.\n\n' +
    'Publishing locks the schedule against further editing.'
  )

  if (!confirmed) return

  const scheduleId = schedule.value.id

  await perform(async () => {
    await request(
      `/api/admin/schedules/${scheduleId}/publish`,
      { method: 'POST' }
    )

    notice.value = 'Schedule published successfully.'
  })
}

function changePeriod(offset: number) {
  const next = periodIndex.value + offset
  if (next < 0) return

  periodIndex.value = next
  formOpen.value = false
  selectedEmployee.value = null
  void load()
}

function createSchedule() {
  perform(async () => {
    await request(
      `/api/admin/schedules?startDate=${startDate.value}`,
      { method: 'POST' }
    )
    notice.value = 'Draft schedule created.'
  })
}

function openNew(date: string) {
  editingId.value = null
  form.value = {
    date,
    startTime: '07:00',
    endTime: '14:00',
    position: 'Barista',
    requiredEmployees: 1
  }
  formOpen.value = true
}

function openEdit(shift: Shift) {
  editingId.value = shift.id
  form.value = {
    date: shift.date,
    startTime: shift.startTime.slice(0, 5),
    endTime: shift.endTime.slice(0, 5),
    position: shift.position as Position,
    requiredEmployees: shift.requiredEmployees
  }
  formOpen.value = true
}

async function saveShift() {
  if (!schedule.value) return

  const editing = editingId.value

  await perform(async () => {
    await request(
      editing === null
        ? `/api/admin/schedules/${schedule.value!.id}/shifts`
        : `/api/admin/shifts/${editing}`,
      {
        method: editing === null ? 'POST' : 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(form.value)
      }
    )

    formOpen.value = false
    notice.value = 'Shift saved.'
  })
}

function deleteShift(shift: Shift) {
  if (!confirm(`Delete the ${shift.position} shift on ${displayDate(shift.date)}?`))
    return

  perform(async () => {
    await request(`/api/admin/shifts/${shift.id}`, {
      method: 'DELETE'
    })
    notice.value = 'Shift deleted.'
  })
}

function assign(shift: Shift, employeeId: number) {
  if (!employeeId) return

  const employee = employees.value.find(e => e.id === employeeId)
  if (!employee) return



  const available = coversShift(employee, shift)

if (!available) {
  const reason = employee.status === 'NotSubmitted'
    ? 'has not submitted availability for this period'
    : 'is not available for the entire shift'

  if (!confirm(
    `${employee.firstName} ${employee.lastName} ${reason}.\n\n` +
    `Assign them anyway?`
  )) {
    return
  }
}

const override = !available

  perform(async () => {
    await request(
      `/api/admin/shifts/${shift.id}/assignments`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          appUserId: employeeId,
          overrideAvailability: override
        })
      }
    )
    notice.value = 'Employee assigned.'
  })
}

function unassign(shift: Shift, employeeId: number) {
  if (!confirm('Remove this employee from the shift?')) return

  perform(async () => {
    await request(
      `/api/admin/shifts/${shift.id}/assignments/${employeeId}`,
      { method: 'DELETE' }
    )
    notice.value = 'Assignment removed.'
  })
}

onMounted(load)
</script>

<template>
  <main class="scheduler">
    <header>
      <h1>Schedule Builder</h1>
      <p>Create and manage the bakery's two-week schedule.</p>
    </header>

    <div class="toolbar">
      <button
        :disabled="periodIndex === 0 || loading || busy"
        @click="changePeriod(-1)"
      >
        Previous
      </button>

      <strong>
        {{ displayDate(days[0]!) }} –
        {{ displayDate(days[13]!) }}
      </strong>

      <button :disabled="loading || busy" @click="changePeriod(1)">
        Next
      </button>
    </div>

    <p v-if="loading">Loading schedule...</p>
    <p v-if="error" role="alert" class="error">{{ error }}</p>
    <p v-if="notice" role="status">{{ notice }}</p>

    <template v-if="!loading">
      <div v-if="!schedule" class="empty">
        <p>No schedule exists for this period.</p>
        <button :disabled="busy" @click="createSchedule">
          Create Draft Schedule
        </button>
      </div>

      <template v-else>
        <div class="toolbar">
          <strong><button
  v-if="schedule.status === 'Draft'"
  type="button"
  class="publish-button"
  :disabled="busy || loading"
  @click="publishSchedule"
>
  Publish Schedule
</button>Status: {{ schedule.status }}</strong>

          <div class="view-switch">
            <button
              :class="{ active: view === 'shift' }"
              @click="view = 'shift'"
            >
              By Shift
            </button>
            <button
              :class="{ active: view === 'employee' }"
              @click="view = 'employee'"
            >
              By Employee
            </button>
          </div>

          <button
            v-if="schedule.status === 'Draft'"
            :disabled="busy"
            @click="openNew(days[0]!)"
          >
            + Add Shift
          </button>
        </div>

        <form
          v-if="formOpen && schedule.status === 'Draft'"
          class="shift-form"
          @submit.prevent="saveShift"
        >
          <h2>{{ editingId === null ? 'New Shift' : 'Edit Shift' }}</h2>

          <label>
            Date
            <input
              v-model="form.date"
              type="date"
              :min="days[0]"
              :max="days[13]"
              required
            />
          </label>

          <label>
            Start
            <input v-model="form.startTime" type="time" required />
          </label>

          <label>
            End
            <input v-model="form.endTime" type="time" required />
          </label>

          <label>
            Position
            <select v-model="form.position">
              <option>Barista</option>
              <option>Cashier</option>
              <option>Baker</option>
            </select>
          </label>

          <label>
            Staff needed
            <input
              v-model.number="form.requiredEmployees"
              type="number"
              min="1"
              required
            />
          </label>

          <button type="submit" :disabled="busy">Save Shift</button>
          <button type="button" @click="formOpen = false">
            Cancel
          </button>
        </form>

        <template v-if="view === 'shift'">
         <div class="week-switcher">
  <button
    type="button"
    :class="{ selected: selectedWeek === 0 }"
    @click="selectedWeek = 0"
  >
    Week 1
  </button>

  <button
    type="button"
    :class="{ selected: selectedWeek === 1 }"
    @click="selectedWeek = 1"
  >
    Week 2
  </button>
</div>
            <div class="calendar">
            <section v-for="day in visibleDays" :key="day" class="day">              <div class="day-heading">
                <strong>{{ displayDate(day) }}</strong>
                <button
                  v-if="schedule.status === 'Draft'"
                  @click="openNew(day)"
                >
                  +
                </button>
              </div>

              <article
  v-for="shift in shiftsOn(day)"
  :key="shift.id"
  class="shift"
  :class="{
    'shift-barista': shift.position === 'Barista',
    'shift-baker': shift.position === 'Baker',
    'shift-cashier': shift.position === 'Cashier'
  }"
>
                <strong>{{ shift.position }}</strong>
                <p>
                  {{ displayTime(shift.startTime) }} –
                  {{ displayTime(shift.endTime) }}
                </p>
                <div
  class="staffing-status"
  :class="{
    'fully-staffed':
      shift.assignments.length >= shift.requiredEmployees
  }"
>
  <span
    class="staffing-dot"
  ></span>

  <span>
    {{ shift.assignments.length }} /
    {{ shift.requiredEmployees }} staffed
  </span>
</div>

                <div
                  v-for="assignment in shift.assignments"
                  :key="assignment.appUserId"
                  class="assignment"
                >
                  {{ assignment.firstName }}
                  {{ assignment.lastName }}

                  <button
                    v-if="schedule.status === 'Draft'"
                    :disabled="busy"
                    @click="unassign(shift, assignment.appUserId)"
                  >
                    Remove
                  </button>
                </div>

                <template v-if="schedule.status === 'Draft'">

<EmployeePicker
  :disabled="busy || shift.assignments.length >= shift.requiredEmployees"
  :employees="
    employees
      .filter(e =>
        !shift.assignments.some(a => a.appUserId === e.id)
      )
      .map(e => {
        const conflict = conflictingShift(e.id, shift)

        return {
          id: e.id,
          firstName: e.firstName,
          lastName: e.lastName,
          qualified: isQualified(e, shift.position),
          availability: availabilityLabel(e, shift),
          conflict: conflict
            ? `Already scheduled ${displayTime(conflict.startTime)}–${displayTime(conflict.endTime)}`
            : undefined
        }
      })
  "
  @select="employeeId => assign(shift, employeeId)"
/>


                  <div class="actions">
                    <button :disabled="busy" @click="openEdit(shift)">
                      Edit
                    </button>
                    <button :disabled="busy" @click="deleteShift(shift)">
                      Delete
                    </button>
                  </div>
                </template>
              </article>

              <small v-if="!shiftsOn(day).length">No shifts</small>
            </section>
          </div>
        </template>

        <template v-else>
          <section class="employee-view">
            <article
              v-for="employee in employees"
              :key="employee.id"
              class="employee-card"
            >
              <button
                class="employee-heading"
                @click="
                  selectedEmployee =
                    selectedEmployee === employee.id
                      ? null
                      : employee.id
                "
              >
                {{ employee.firstName }} {{ employee.lastName }}
                — {{ employeeShifts(employee.id).length }} shifts
              </button>

              <div v-if="selectedEmployee === employee.id">
                <p v-if="!employeeShifts(employee.id).length">
                  No assigned shifts.
                </p>

                <p
                  v-for="shift in employeeShifts(employee.id)"
                  :key="shift.id"
                >
                  {{ displayDate(shift.date) }} —
                  {{ shift.position }} —
                  {{ displayTime(shift.startTime) }} to
                  {{ displayTime(shift.endTime) }}

                  <button
                    v-if="schedule.status === 'Draft'"
                    :disabled="busy"
                    @click="unassign(shift, employee.id)"
                  >
                    Remove
                  </button>
                </p>

                <p>
                  Total:
                  {{
                    employeeShifts(employee.id).reduce(
                      (total, shift) => {
                        const [sh, sm] = shift.startTime.split(':').map(Number)
                        const [eh, em] = shift.endTime.split(':').map(Number)
                        return total +
                          ((eh! * 60 + em!) - (sh! * 60 + sm!)) / 60
                      },
                      0
                    ).toFixed(1)
                  }}
                  hours / 2 weeks
                </p>

                <p class="hint">
                  To add an assignment, switch to By Shift and
                  choose the employee from a shift.
                </p>
              </div>
            </article>
          </section>
        </template>
      </template>
    </template>
  </main>
</template>


<style scoped>


.scheduler {
  --primary: #DEB4C8;
  --primary-dark: #8B526F;
  --blush: #F4E3EC;
  --background: #FAF7F9;
  --surface: #FFFFFF;
  --text: #282528;
  --muted: #756C73;
  --border: #E9DFE5;

  width: 100%;
  max-width: 1550px;
  margin: 0 auto;
  padding: 32px 28px 64px;
  color: var(--text);
}

.scheduler h1,
.scheduler h2,
.scheduler h3 {
  color: #263029;
  letter-spacing: -0.035em;
}

.scheduler h1 {
  font-size: clamp(26px, 3vw, 36px);
  font-weight: 700;
}

.scheduler h2 {
  font-size: 20px;
}

.toolbar,
.view-switch,
.actions,
.day-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 12px;
}

.toolbar {
  margin: 20px 0;
}

button,
select,
input {
  font: inherit;
  font-size: 13px;
  padding: 10px 12px;
  border: 1px solid #dce2d9;
  border-radius: 9px;
  background: white;
  color: #303a32;
  transition:
    background 0.15s ease,
    border-color 0.15s ease,
    box-shadow 0.15s ease;
}

button {
  cursor: pointer;
  font-weight: 600;
}

button:hover:not(:disabled) {
  background: #edf3e9;
  border-color: #a8bba4;
}

button:focus-visible,
select:focus-visible,
input:focus-visible {
  outline: 3px solid #b9d4af;
  outline-offset: 2px;
}

button:disabled,
select:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.active {
  background: var(--sage-dark);
  border-color: var(--sage-dark);
  color: white;
}

.active:hover:not(:disabled) {
  background: #263d2c;
  color: white;
}

.view-switch {
  justify-content: flex-start;
  gap: 5px;
  padding: 5px;
  border-radius: 12px;
  background: #e9eee5;
}

.view-switch button {
  border-color: transparent;
}

.calendar {
  display: grid;
  grid-template-columns: repeat(7, minmax(0, 1fr));
  gap: 12px;
  align-items: start;
}

.day,
.employee-card,
.shift-form {
  border: 1px solid var(--border);
  border-radius: 14px;
  padding: 16px;
  background: #fff;
  box-shadow: 0 3px 14px rgb(40 55 35 / 4%);
}

.day {
  min-width: 0;
  min-height: 210px;
}

.day-heading {
  padding-bottom: 12px;
  margin-bottom: 12px;
  border-bottom: 1px solid #edf0ea;
}

.day-heading h3 {
  margin: 0;
  font-size: 14px;
  font-weight: 700;
}

.shift {
  position: relative;
  background: #edf5ef;
  border: 1px solid #d5e9db;
  border-left: 4px solid #4c9b6b;
  border-radius: 9px;
  padding: 12px 10px;
  margin-top: 10px;
  overflow-wrap: anywhere;
}

.shift:has(select) {
  background: #f1f6f0;
}

.shift p,
.shift small {
  font-size: 12px;
  line-height: 1.5;
}

.shift select {
  width: 100%;
  max-width: 100%;
  margin: 10px 0;
}

.assignment {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 8px;
  padding: 8px 0;
  font-size: 12px;
  border-bottom: 1px solid rgb(55 100 65 / 10%);
}

.assignment:last-child {
  border-bottom: none;
}

.shift-form {
  display: flex;
  gap: 14px;
  flex-wrap: wrap;
  align-items: end;
  margin-bottom: 24px;
  padding: 20px;
}

.shift-form label {
  display: flex;
  flex-direction: column;
  gap: 7px;
  font-size: 12px;
  font-weight: 600;
  color: #526051;
}

.shift-form input,
.shift-form select {
  min-height: 40px;
}

.employee-view {
  display: grid;
  gap: 14px;
}

.employee-card {
  padding: 20px;
}

.employee-heading {
  width: 100%;
  text-align: left;
  font-size: 15px;
  font-weight: 700;
}

.empty {
  padding: 28px;
  background: #fff;
  border: 1px dashed #bac9b5;
  border-radius: 14px;
  color: #61705f;
}

.error {
  padding: 14px 16px;
  color: #a52c24;
  background: #fff0ed;
  border: 1px solid #f1ccc5;
  border-radius: 10px;
}

.hint {
  color: var(--muted);
  font-size: 12px;
}

@media (max-width: 1200px) {
  .calendar {
    grid-template-columns: repeat(4, minmax(0, 1fr));
  }
}

@media (max-width: 750px) {
  .calendar {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .scheduler {
    padding: 20px 14px 40px;
  }
}
.week-switcher {
  display: flex;
  gap: 8px;
  margin: 18px 0;
}

.week-switcher button {
  padding: 10px 20px;
  border: 1px solid #e9dfe5;
  border-radius: 9px;
  background: #fff;
  color: #756c73;
  font-weight: 600;
  cursor: pointer;
}

.week-switcher button.selected {
  background: #8b526f;
  border-color: #8b526f;
  color: #fff;
}

@media (max-width: 450px) {
  .calendar {
    grid-template-columns: 1fr;
  }

  .shift-form {
    flex-direction: column;
    align-items: stretch;
  }

  .shift-form label {
    width: 100%;
  }
}

@media (prefers-reduced-motion: reduce) {
  button,
  select,
  input {
    transition: none;
  }
}

/* Reister's Daughter brand styling */

.scheduler {
  color: var(--text);
}

.scheduler h1,
.scheduler h2,
.scheduler h3 {
  color: var(--text);
}

.day,
.employee-card,
.shift-form {
  background: var(--surface);
  border-color: var(--border);
  box-shadow: 0 4px 18px rgb(90 45 70 / 5%);
}

.day-heading {
  border-bottom-color: var(--border);
}

button,
select,
input {
  border-color: var(--border);
  color: var(--text);
}

button:hover:not(:disabled) {
  background: var(--blush);
  border-color: var(--primary);
}

button:focus-visible,
select:focus-visible,
input:focus-visible {
  outline: 3px solid var(--primary);
  outline-offset: 2px;
}

.view-switch {
  background: var(--blush);
}

.view-switch button.active,
button.active {
  background: var(--primary-dark);
  border-color: var(--primary-dark);
  color: white;
}

.view-switch button.active:hover:not(:disabled),
button.active:hover:not(:disabled) {
  background: #70405a;
  color: white;
}

/* Default shift styling: Barista pink */
.shift {
  background: #f8eaf1;
  border-color: #efd4e2;
  border-left-color: var(--primary-dark);
}

.shift:has(select) {
  background: #f8eaf1;
}

.assignment {
  border-bottom-color: #eadce4;
}

.hint {
  color: var(--muted);
}

.empty {
  background: var(--surface);
  border-color: var(--primary);
  color: var(--muted);
}
/* Position-specific shift cards */

.shift {
  border-radius: 12px;
  padding: 14px;
  margin-top: 10px;
  border: 1px solid #e9dfe5;
  border-left: 4px solid #8b526f;
  background: #fff;
}

.shift-barista {
  background: #fdf0f6;
  border-left-color: #c979a2;
}

.shift-baker {
  background: #fbf3e7;
  border-left-color: #c49a64;
}

.shift-cashier {
  background: #f3effc;
  border-left-color: #9b87c9;
}

.shift > strong {
  display: block;
  font-size: 14px;
  margin-bottom: 5px;
}

.shift > p {
  font-size: 13px;
  margin: 4px 0 10px;
}

.staffing-status {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  font-size: 12px;
  font-weight: 600;
  color: #9b5b48;
  margin-bottom: 10px;
}

.staffing-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: #d28a62;
}

.staffing-status.fully-staffed {
  color: #3e8062;
}

.fully-staffed .staffing-dot {
  background: #4a9a72;
}
.publish-button {
  padding: 10px 18px;
  border: none;
  border-radius: 9px;
  background: #8b526f;
  color: white;
  font-weight: 700;
  cursor: pointer;
}

.publish-button:hover:not(:disabled) {
  background: #70405a;
}

.publish-button:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

</style>
