<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

interface EventRecord {
  id: number
  title: string
  description: string | null
  type: string
  status: string
  startDate: string
  startTime: string
  endDate: string | null
  endTime: string | null
  location: string | null
  isPublic: boolean
  capacity: number | null
  bookingId: number | null
  createdAt: string
  updatedAt: string
}

interface EventReservation {
  id: number
  customerId: number | null
  firstName: string
  lastName: string
  email: string
  phone: string
  guestCount: number
  status: string
  notes: string | null
  createdAt: string
  updatedAt: string
}

interface ReservationSummary {
  eventId: number
  title: string
  capacity: number | null
  reservedSeats: number
  remainingSeats: number | null
  reservationCount: number
  pendingReservations: number
  confirmedReservations: number
  reservations: EventReservation[]
}

const route = useRoute()
const router = useRouter()

const loading = ref(false)
const saving = ref(false)
const statusUpdating = ref(false)

const reservationsLoading = ref(false)
const reservationStatusUpdating =
  ref<number | null>(null)

const errorMessage = ref('')
const successMessage = ref('')

const reservationSummary =
  ref<ReservationSummary | null>(null)

const eventId = computed(() => {
  const id = Number(route.params.id)

  return Number.isFinite(id) ? id : null
})

const isNew = computed(
  () => route.name === 'admin-event-new',
)

const title = ref('')
const description = ref('')
const type = ref('High Tea')
const status = ref('Draft')

const startDate = ref('')
const startTime = ref('')
const endDate = ref('')
const endTime = ref('')

const location = ref(
  "The Reister's Daughter",
)

const isPublic = ref(false)
const capacity = ref<number | null>(null)

const eventTypes = [
  'High Tea',
  'Class',
  'Community Event',
  'Pop-Up',
  'Workshop',
  'Private Event',
  'Catering',
  'Other',
]

const pageTitle = computed(() =>
  isNew.value
    ? 'Create Event'
    : title.value || 'Event',
)

function statusClass(value: string) {
  return `status-${value
    .toLowerCase()
    .replace(/\s+/g, '-')}`
}

function clearMessages() {
  errorMessage.value = ''
  successMessage.value = ''
}

function validateForm() {
  if (!title.value.trim()) {
    errorMessage.value =
      'Event title is required.'

    return false
  }

  if (!type.value.trim()) {
    errorMessage.value =
      'Event type is required.'

    return false
  }

  if (!startDate.value) {
    errorMessage.value =
      'Start date is required.'

    return false
  }

  if (!startTime.value) {
    errorMessage.value =
      'Start time is required.'

    return false
  }

  if (
    capacity.value !== null &&
    capacity.value < 1
  ) {
    errorMessage.value =
      'Capacity must be greater than zero.'

    return false
  }

  return true
}

function buildRequest() {
  return {
    title: title.value.trim(),

    description:
      description.value.trim() || null,

    type: type.value.trim(),

    startDate: startDate.value,
    startTime: startTime.value,

    endDate:
      endDate.value || null,

    endTime:
      endTime.value || null,

    location:
      location.value.trim() || null,

    isPublic: isPublic.value,

    capacity:
      capacity.value === null ||
      capacity.value === undefined
        ? null
        : Number(capacity.value),

    bookingId: null,
  }
}

function populateForm(
  eventRecord: EventRecord,
) {
  title.value = eventRecord.title
  description.value =
    eventRecord.description ?? ''

  type.value = eventRecord.type
  status.value = eventRecord.status

  startDate.value = eventRecord.startDate
  startTime.value = eventRecord.startTime

  endDate.value =
    eventRecord.endDate ?? ''

  endTime.value =
    eventRecord.endTime ?? ''

  location.value =
    eventRecord.location ?? ''

  isPublic.value =
    eventRecord.isPublic

  capacity.value =
    eventRecord.capacity
}

function formatReservationDate(
  value: string,
) {
  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return value
  }

  return new Intl.DateTimeFormat(
    'en-US',
    {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    },
  ).format(date)
}

async function loadReservations() {
  if (!eventId.value || isNew.value) {
    return
  }

  reservationsLoading.value = true

  try {
    const response = await fetch(
      `http://localhost:5128/api/events/${eventId.value}/reservations`,
    )

    const data = await response.json()

    if (!response.ok) {
      throw new Error(
        data.message ||
          'Unable to load reservations.',
      )
    }

    reservationSummary.value = data
  } catch (error) {
    console.error(error)

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Unable to load reservations.'
  } finally {
    reservationsLoading.value = false
  }
}

async function updateReservationStatus(
  reservationId: number,
  newStatus: string,
) {
  if (!eventId.value) {
    return
  }

  clearMessages()

  reservationStatusUpdating.value =
    reservationId

  try {
    const response = await fetch(
      `http://localhost:5128/api/events/${eventId.value}/reservations/${reservationId}/status`,
      {
        method: 'PUT',

        headers: {
          'Content-Type':
            'application/json',
        },

        body: JSON.stringify({
          status: newStatus,
        }),
      },
    )

    const data = await response.json()

    if (!response.ok) {
      throw new Error(
        data.message ||
          'Unable to update reservation.',
      )
    }

    successMessage.value =
      `Reservation moved to ${data.status}.`

    await loadReservations()
  } catch (error) {
    console.error(error)

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Unable to update reservation.'
  } finally {
    reservationStatusUpdating.value = null
  }
}

async function deleteReservation(
  reservationId: number,
) {
  if (!eventId.value) {
    return
  }

  const confirmed = window.confirm(
    'Permanently delete this reservation? This cannot be undone.',
  )

  if (!confirmed) {
    return
  }

  clearMessages()

  reservationStatusUpdating.value =
    reservationId

  try {
    const response = await fetch(
      `http://localhost:5128/api/events/${eventId.value}/reservations/${reservationId}`,
      {
        method: 'DELETE',
      },
    )

    if (!response.ok) {
      const data = await response.json()

      throw new Error(
        data.message ||
          'Unable to delete reservation.',
      )
    }

    successMessage.value =
      'Reservation permanently deleted.'

    await loadReservations()
  } catch (error) {
    console.error(error)

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Unable to delete reservation.'
  } finally {
    reservationStatusUpdating.value = null
  }
}

async function loadEvent() {
  if (isNew.value) {
    return
  }

  if (!eventId.value) {
    errorMessage.value =
      'Invalid event ID.'

    return
  }

  loading.value = true
  clearMessages()

  try {
    const response = await fetch(
      `http://localhost:5128/api/events/${eventId.value}`,
    )

    if (!response.ok) {
      throw new Error(
        `Unable to load event (${response.status}).`,
      )
    }

    const eventRecord: EventRecord =
      await response.json()

    populateForm(eventRecord)

    await loadReservations()
  } catch (error) {
    console.error(error)

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Unable to load event.'
  } finally {
    loading.value = false
  }
}

async function saveEvent() {
  clearMessages()

  if (!validateForm()) {
    return
  }

  saving.value = true

  try {
    const url = isNew.value
      ? 'http://localhost:5128/api/events'
      : `http://localhost:5128/api/events/${eventId.value}`

    const response = await fetch(url, {
      method: isNew.value
        ? 'POST'
        : 'PUT',

      headers: {
        'Content-Type':
          'application/json',
      },

      body: JSON.stringify(
        buildRequest(),
      ),
    })

    const data = await response.json()

    if (!response.ok) {
      throw new Error(
        data.message ||
          'Unable to save event.',
      )
    }

    if (isNew.value) {
      await router.replace(
        `/admin/events/${data.id}`,
      )

      return
    }

    populateForm(data)

    successMessage.value =
      'Event saved.'

    await loadReservations()
  } catch (error) {
    console.error(error)

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Unable to save event.'
  } finally {
    saving.value = false
  }
}

async function updateStatus(
  newStatus: string,
) {
  if (!eventId.value || isNew.value) {
    return
  }

  clearMessages()
  statusUpdating.value = true

  try {
    const response = await fetch(
      `http://localhost:5128/api/events/${eventId.value}/status`,
      {
        method: 'PUT',

        headers: {
          'Content-Type':
            'application/json',
        },

        body: JSON.stringify({
          status: newStatus,
        }),
      },
    )

    const data = await response.json()

    if (!response.ok) {
      throw new Error(
        data.message ||
          'Unable to update status.',
      )
    }

    status.value = data.status

    successMessage.value =
      `Event moved to ${data.status}.`
  } catch (error) {
    console.error(error)

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Unable to update status.'
  } finally {
    statusUpdating.value = false
  }
}

function backToEvents() {
  router.push('/admin/events')
}

onMounted(loadEvent)
</script>

<template>
  <main class="event-editor-page">
    <section class="event-editor-shell">
      <button
        type="button"
        class="back-button"
        @click="backToEvents"
      >
        ← Events
      </button>

      <div
        v-if="loading"
        class="state-card"
      >
        Loading event...
      </div>

      <template v-else>
        <header class="page-header">
          <div>
            <p class="eyebrow">
              Events
            </p>

            <div class="title-row">
              <h1>
                {{ pageTitle }}
              </h1>

              <span
                v-if="!isNew"
                class="status-badge"
                :class="
                  statusClass(status)
                "
              >
                {{ status }}
              </span>
            </div>

            <p class="page-description">
              {{
                isNew
                  ? 'Add an event to the internal calendar.'
                  : 'Manage event details, reservations, visibility, and publishing.'
              }}
            </p>
          </div>
        </header>

        <div
          v-if="errorMessage"
          class="message error-message"
        >
          {{ errorMessage }}
        </div>

        <div
          v-if="successMessage"
          class="message success-message"
        >
          {{ successMessage }}
        </div>

        <form
          class="editor-layout"
          @submit.prevent="saveEvent"
        >
          <div class="main-column">
            <section class="editor-card">
              <div class="card-heading">
                <div>
                  <p class="card-eyebrow">
                    Details
                  </p>

                  <h2>
                    Event Information
                  </h2>
                </div>
              </div>

              <div class="form-grid">
                <label class="field full-width">
                  <span>Event Title</span>

                  <input
                    v-model="title"
                    type="text"
                    placeholder="High Tea"
                    required
                  />
                </label>

                <label class="field">
                  <span>Event Type</span>

                  <select v-model="type">
                    <option
                      v-for="option in eventTypes"
                      :key="option"
                      :value="option"
                    >
                      {{ option }}
                    </option>
                  </select>
                </label>

                <label class="field">
                  <span>Capacity</span>

                  <input
                    v-model.number="capacity"
                    type="number"
                    min="1"
                    placeholder="20"
                  />
                </label>

                <label class="field full-width">
                  <span>Description</span>

                  <textarea
                    v-model="description"
                    rows="6"
                    placeholder="Describe the event..."
                  />
                </label>
              </div>
            </section>

            <section class="editor-card">
              <div class="card-heading">
                <div>
                  <p class="card-eyebrow">
                    Schedule
                  </p>

                  <h2>
                    Date & Time
                  </h2>
                </div>
              </div>

              <div class="form-grid">
                <label class="field">
                  <span>Start Date</span>

                  <input
                    v-model="startDate"
                    type="date"
                    required
                  />
                </label>

                <label class="field">
                  <span>Start Time</span>

                  <input
                    v-model="startTime"
                    type="time"
                    required
                  />
                </label>

                <label class="field">
                  <span>End Date</span>

                  <input
                    v-model="endDate"
                    type="date"
                  />
                </label>

                <label class="field">
                  <span>End Time</span>

                  <input
                    v-model="endTime"
                    type="time"
                  />
                </label>

                <label class="field full-width">
                  <span>Location</span>

                  <input
                    v-model="location"
                    type="text"
                    placeholder="The Reister's Daughter"
                  />
                </label>
              </div>
            </section>

            <section
              v-if="!isNew"
              class="editor-card"
            >
              <div class="card-heading reservations-heading">
                <div>
                  <p class="card-eyebrow">
                    Attendance
                  </p>

                  <h2>
                    Reservations
                  </h2>
                </div>

                <button
                  type="button"
                  class="refresh-button"
                  :disabled="reservationsLoading"
                  @click="loadReservations"
                >
                  {{
                    reservationsLoading
                      ? 'Loading...'
                      : 'Refresh'
                  }}
                </button>
              </div>

              <div
                v-if="reservationsLoading &&
                  !reservationSummary"
                class="reservation-empty"
              >
                Loading reservations...
              </div>

              <template
                v-else-if="reservationSummary"
              >
                <div class="reservation-stats">
                  <div class="stat-card">
                    <strong>
                      {{
                        reservationSummary.reservedSeats
                      }}
                    </strong>

                    <span>
                      Reserved Seats
                    </span>
                  </div>

                  <div class="stat-card">
                    <strong>
                      {{
                        reservationSummary.capacity ??
                          '—'
                      }}
                    </strong>

                    <span>
                      Capacity
                    </span>
                  </div>

                  <div class="stat-card">
                    <strong>
                      {{
                        reservationSummary.remainingSeats ??
                          '—'
                      }}
                    </strong>

                    <span>
                      Remaining
                    </span>
                  </div>
                </div>

                <div class="reservation-breakdown">
                  <span>
                    {{
                      reservationSummary.reservationCount
                    }}
                    {{
                      reservationSummary.reservationCount === 1
                        ? 'reservation'
                        : 'reservations'
                    }}
                  </span>

                  <span>
                    {{
                      reservationSummary.pendingReservations
                    }}
                    pending
                  </span>

                  <span>
                    {{
                      reservationSummary.confirmedReservations
                    }}
                    confirmed
                  </span>
                </div>

                <div
                  v-if="
                    reservationSummary.reservations.length === 0
                  "
                  class="reservation-empty"
                >
                  No reservations have been submitted
                  for this event yet.
                </div>

                <div
                  v-else
                  class="reservation-list"
                >
                  <article
                    v-for="
                      reservation in
                        reservationSummary.reservations
                    "
                    :key="reservation.id"
                    class="reservation-card"
                  >
                    <div class="reservation-top">
                      <div>
                        <h3>
                          {{ reservation.firstName }}
                          {{ reservation.lastName }}
                        </h3>

                        <p class="reservation-submitted">
                          Submitted
                          {{
                            formatReservationDate(
                              reservation.createdAt,
                            )
                          }}
                        </p>
                      </div>

                      <span
                        class="status-badge"
                        :class="
                          statusClass(
                            reservation.status,
                          )
                        "
                      >
                        {{ reservation.status }}
                      </span>
                    </div>

                    <div class="reservation-details">
                      <div>
                        <span>Guests</span>
                        <strong>
                          {{ reservation.guestCount }}
                        </strong>
                      </div>

                      <div>
                        <span>Email</span>

                        <a
                          :href="
                            `mailto:${reservation.email}`
                          "
                        >
                          {{ reservation.email }}
                        </a>
                      </div>

                      <div>
                        <span>Phone</span>

                        <a
                          :href="
                            `tel:${reservation.phone}`
                          "
                        >
                          {{ reservation.phone }}
                        </a>
                      </div>
                    </div>

                    <div
                      v-if="reservation.notes"
                      class="reservation-notes"
                    >
                      <span>Notes</span>

                      <p>
                        {{ reservation.notes }}
                      </p>
                    </div>

                    <div class="reservation-actions">
                      <button
                        v-if="
                          reservation.status !==
                            'Confirmed'
                        "
                        type="button"
                        class="confirm-button"
                        :disabled="
                          reservationStatusUpdating ===
                            reservation.id
                        "
                        @click="
                          updateReservationStatus(
                            reservation.id,
                            'Confirmed',
                          )
                        "
                      >
                        {{
                          reservationStatusUpdating ===
                            reservation.id
                            ? 'Updating...'
                            : 'Confirm'
                        }}
                      </button>

                      <button
                        v-if="
                          reservation.status !==
                            'Pending'
                        "
                        type="button"
                        class="secondary-small-button"
                        :disabled="
                          reservationStatusUpdating ===
                            reservation.id
                        "
                        @click="
                          updateReservationStatus(
                            reservation.id,
                            'Pending',
                          )
                        "
                      >
                        Move to Pending
                      </button>

                      <button
                        v-if="
                          reservation.status !==
                            'Cancelled'
                        "
                        type="button"
                        class="cancel-small-button"
                        :disabled="
                          reservationStatusUpdating ===
                            reservation.id
                        "
                        @click="
                          updateReservationStatus(
                            reservation.id,
                            'Cancelled',
                          )
                        "
                      >
                      
                        Cancel
                      </button>
                      <button
  v-if="
    reservation.status ===
      'Cancelled'
  "
  type="button"
  class="delete-small-button"
  :disabled="
    reservationStatusUpdating ===
      reservation.id
  "
  @click="
    deleteReservation(
      reservation.id,
    )
  "
>
  Delete Permanently
</button>
                    </div>
                  </article>
                </div>
              </template>
            </section>
          </div>

          <aside class="side-column">
            <section class="editor-card">
              <div class="card-heading">
                <div>
                  <p class="card-eyebrow">
                    Visibility
                  </p>

                  <h2>
                    Public Event
                  </h2>
                </div>
              </div>

              <label class="visibility-option">
                <div>
                  <strong>
                    Show publicly
                  </strong>

                  <p>
                    Allows this event to
                    appear on the public
                    website once it is
                    published.
                  </p>
                </div>

                <input
                  v-model="isPublic"
                  type="checkbox"
                />
              </label>

              <div class="visibility-note">
                <strong>
                  {{
                    isPublic
                      ? 'Public'
                      : 'Internal'
                  }}
                </strong>

                <span v-if="isPublic">
                  This event is eligible
                  for the public calendar.
                </span>

                <span v-else>
                  This event stays inside
                  the admin system.
                </span>
              </div>
            </section>

            <section class="editor-card">
              <div class="card-heading">
                <div>
                  <p class="card-eyebrow">
                    Actions
                  </p>

                  <h2>
                    Save
                  </h2>
                </div>
              </div>

              <button
                type="submit"
                class="primary-button"
                :disabled="
                  saving ||
                  statusUpdating
                "
              >
                {{
                  saving
                    ? 'Saving...'
                    : isNew
                      ? 'Create Event'
                      : 'Save Changes'
                }}
              </button>
            </section>

            <section
              v-if="!isNew"
              class="editor-card"
            >
              <div class="card-heading">
                <div>
                  <p class="card-eyebrow">
                    Status
                  </p>

                  <h2>
                    Publishing
                  </h2>
                </div>
              </div>

              <p class="status-help">
                Current status:
                <strong>
                  {{ status }}
                </strong>
              </p>

              <div class="status-actions">
                <button
                  v-if="
                    status !== 'Published'
                  "
                  type="button"
                  class="primary-button"
                  :disabled="
                    statusUpdating
                  "
                  @click="
                    updateStatus(
                      'Published',
                    )
                  "
                >
                  Publish Event
                </button>

                <button
                  v-if="
                    status !== 'Draft'
                  "
                  type="button"
                  class="secondary-button"
                  :disabled="
                    statusUpdating
                  "
                  @click="
                    updateStatus('Draft')
                  "
                >
                  Move to Draft
                </button>

                <button
                  v-if="
                    status !== 'Completed'
                  "
                  type="button"
                  class="secondary-button"
                  :disabled="
                    statusUpdating
                  "
                  @click="
                    updateStatus(
                      'Completed',
                    )
                  "
                >
                  Mark Completed
                </button>

                <button
                  v-if="
                    status !== 'Cancelled'
                  "
                  type="button"
                  class="danger-button"
                  :disabled="
                    statusUpdating
                  "
                  @click="
                    updateStatus(
                      'Cancelled',
                    )
                  "
                >
                  Cancel Event
                </button>
              </div>
            </section>
          </aside>
        </form>
      </template>
    </section>
  </main>
</template>

<style scoped>
.event-editor-page {
  min-height: 100vh;
  background: #f7f5f1;
  padding: 2rem 1rem 5rem;
}

.event-editor-shell {
  width: min(1200px, 100%);
  margin: 0 auto;
}

.back-button {
  border: 0;
  background: transparent;
  padding: 0;
  margin-bottom: 1.5rem;
  color: #675c53;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}

.page-header {
  margin-bottom: 2rem;
}

.eyebrow,
.card-eyebrow {
  margin: 0 0 0.35rem;
  color: #806b58;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
}

.title-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
}

.title-row h1 {
  margin: 0;
  color: #28231f;
  font-size: clamp(
    2rem,
    4vw,
    3rem
  );
}

.page-description {
  margin: 0.5rem 0 0;
  color: #6b625b;
}

.editor-layout {
  display: grid;
  grid-template-columns:
    minmax(0, 2fr)
    minmax(280px, 0.8fr);
  gap: 1.5rem;
  align-items: start;
}

.main-column,
.side-column {
  display: grid;
  gap: 1.5rem;
}

.editor-card {
  border: 1px solid #e0d9d2;
  border-radius: 1rem;
  background: #ffffff;
  padding: 1.4rem;
}

.card-heading {
  margin-bottom: 1.25rem;
}

.card-heading h2 {
  margin: 0;
  color: #302a25;
  font-size: 1.2rem;
}

.form-grid {
  display: grid;
  grid-template-columns:
    repeat(2, minmax(0, 1fr));
  gap: 1rem;
}

.full-width {
  grid-column: 1 / -1;
}

.field {
  display: grid;
  gap: 0.45rem;
}

.field > span {
  color: #514840;
  font-size: 0.8rem;
  font-weight: 700;
}

.field input,
.field select,
.field textarea {
  width: 100%;
  box-sizing: border-box;
  border: 1px solid #d8d0c8;
  border-radius: 0.65rem;
  background: #ffffff;
  padding: 0.75rem 0.85rem;
  color: #302a25;
  font: inherit;
}

.field textarea {
  resize: vertical;
  line-height: 1.5;
}

.field input:focus,
.field select:focus,
.field textarea:focus {
  outline: 2px solid
    rgba(139, 104, 69, 0.18);
  border-color: #8b6845;
}

.visibility-option {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  cursor: pointer;
}

.visibility-option strong {
  color: #302a25;
}

.visibility-option p {
  margin: 0.35rem 0 0;
  color: #746a62;
  font-size: 0.85rem;
  line-height: 1.5;
}

.visibility-option input {
  width: 1.15rem;
  height: 1.15rem;
  margin-top: 0.15rem;
}

.visibility-note {
  display: grid;
  gap: 0.2rem;
  margin-top: 1rem;
  border-radius: 0.7rem;
  background: #f5f1eb;
  padding: 0.8rem;
}

.visibility-note strong {
  color: #443b34;
}

.visibility-note span {
  color: #746a62;
  font-size: 0.8rem;
}

.primary-button,
.secondary-button,
.danger-button {
  width: 100%;
  border-radius: 0.65rem;
  padding: 0.75rem 1rem;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}

.primary-button {
  border: 1px solid #302a25;
  background: #302a25;
  color: #ffffff;
}

.secondary-button {
  border: 1px solid #d6cec6;
  background: #ffffff;
  color: #4e453e;
}

.danger-button {
  border: 1px solid #d9b9b5;
  background: #fff8f7;
  color: #8b4741;
}

.primary-button:disabled,
.secondary-button:disabled,
.danger-button:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.status-actions {
  display: grid;
  gap: 0.65rem;
}

.status-help {
  margin: 0 0 1rem;
  color: #6b625b;
  font-size: 0.85rem;
}

.status-badge {
  display: inline-flex;
  border-radius: 999px;
  background: #ece8e3;
  padding: 0.35rem 0.7rem;
  color: #5d554e;
  font-size: 0.75rem;
  font-weight: 700;
}

.status-published,
.status-confirmed {
  background: #e3eee5;
  color: #3d6344;
}

.status-draft,
.status-pending {
  background: #f3ead7;
  color: #775e2f;
}

.status-completed {
  background: #e4ebf2;
  color: #445c73;
}

.status-cancelled {
  background: #f5e3e1;
  color: #864943;
}

.message {
  margin-bottom: 1.25rem;
  border-radius: 0.75rem;
  padding: 0.85rem 1rem;
  font-size: 0.9rem;
}

.error-message {
  border: 1px solid #e1c2be;
  background: #fff5f4;
  color: #874b45;
}

.success-message {
  border: 1px solid #cbdccb;
  background: #f3f9f3;
  color: #426148;
}

.state-card {
  border: 1px solid #e0d9d2;
  border-radius: 1rem;
  background: #ffffff;
  padding: 2rem;
  color: #655d56;
}

/* Reservations */

.reservations-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
}

.refresh-button {
  border: 1px solid #d6cec6;
  border-radius: 0.55rem;
  background: #ffffff;
  padding: 0.5rem 0.75rem;
  color: #514840;
  font: inherit;
  font-size: 0.8rem;
  font-weight: 700;
  cursor: pointer;
}

.refresh-button:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.reservation-stats {
  display: grid;
  grid-template-columns:
    repeat(3, minmax(0, 1fr));
  gap: 0.75rem;
  margin-bottom: 0.9rem;
}

.stat-card {
  display: grid;
  gap: 0.2rem;
  border-radius: 0.75rem;
  background: #f7f5f1;
  padding: 1rem;
}

.stat-card strong {
  color: #302a25;
  font-size: 1.5rem;
}

.stat-card span {
  color: #746a62;
  font-size: 0.75rem;
  font-weight: 700;
}

.reservation-breakdown {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem 1rem;
  border-bottom: 1px solid #eee8e2;
  padding-bottom: 1rem;
  color: #746a62;
  font-size: 0.8rem;
}

.reservation-list {
  display: grid;
  gap: 1rem;
  margin-top: 1rem;
}

.reservation-card {
  border: 1px solid #e3ddd7;
  border-radius: 0.8rem;
  padding: 1rem;
}

.reservation-top {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1rem;
}

.reservation-top h3 {
  margin: 0;
  color: #302a25;
  font-size: 1rem;
}

.reservation-submitted {
  margin: 0.25rem 0 0;
  color: #8a817a;
  font-size: 0.75rem;
}

.reservation-details {
  display: grid;
  grid-template-columns:
    0.5fr 1.5fr 1fr;
  gap: 1rem;
}

.reservation-details > div {
  display: grid;
  align-content: start;
  gap: 0.25rem;
}

.reservation-details span,
.reservation-notes > span {
  color: #8a817a;
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.reservation-details strong {
  color: #302a25;
}

.reservation-details a {
  color: #5e5147;
  overflow-wrap: anywhere;
}

.reservation-notes {
  margin-top: 1rem;
  border-radius: 0.6rem;
  background: #f7f5f1;
  padding: 0.8rem;
}

.reservation-notes p {
  margin: 0.3rem 0 0;
  color: #5f5650;
  line-height: 1.5;
}

.reservation-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.6rem;
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid #eee8e2;
}

.confirm-button,
.secondary-small-button,
.cancel-small-button {
  border-radius: 0.55rem;
  padding: 0.55rem 0.85rem;
  font: inherit;
  font-size: 0.8rem;
  font-weight: 700;
  cursor: pointer;
}

.confirm-button {
  border: 1px solid #426148;
  background: #426148;
  color: #ffffff;
}

.secondary-small-button {
  border: 1px solid #d6cec6;
  background: #ffffff;
  color: #514840;
}

.cancel-small-button {
  border: 1px solid #d9b9b5;
  background: #fff8f7;
  color: #8b4741;
}

.confirm-button:disabled,
.secondary-small-button:disabled,
.cancel-small-button:disabled,
.delete-small-button:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.reservation-empty {
  border-radius: 0.75rem;
  background: #f7f5f1;
  padding: 1.25rem;
  color: #746a62;
  text-align: center;
}
.confirm-button,
.secondary-small-button,
.cancel-small-button,
.delete-small-button {
  border-radius: 0.55rem;
  padding: 0.55rem 0.85rem;
  font: inherit;
  font-size: 0.8rem;
  font-weight: 700;
  cursor: pointer;
}
.delete-small-button {
  border: 1px solid #8b4741;
  background: #8b4741;
  color: #ffffff;
}
@media (max-width: 850px) {
  .editor-layout {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 600px) {
  .event-editor-page {
    padding:
      1.25rem
      0.75rem
      4rem;
  }

  .form-grid,
  .reservation-stats,
  .reservation-details {
    grid-template-columns: 1fr;
  }

  .full-width {
    grid-column: auto;
  }

  .reservations-heading,
  .reservation-top {
    flex-direction: column;
  }
}
</style>