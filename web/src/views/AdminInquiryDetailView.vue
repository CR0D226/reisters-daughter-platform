<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'

interface Customer {
  id: number
  firstName: string
  lastName: string
  company: string | null
  email: string
  phone: string
  createdAt: string
}

interface InquiryNote {
  id: number
  body: string
  createdAt: string
}

interface InquiryActivity {
  id: number
  type: string
  description: string
  createdAt: string
}

interface Inquiry {
  id: number
  customerId: number | null
  createdAt: string
  status: string

  eventType: string
  services: string[]

  firstName: string
  lastName: string
  company: string | null

  email: string
  phone: string

  eventDate: string
  eventTime: string
  guestCount: number

  cateringType: string | null
  serviceType: string | null
  deliveryAddress: string | null
  packaging: string | null

  dietaryNeeds: string[]
  otherDietaryNeeds: string | null

  recurring: string | null
  details: string

  customer: Customer | null
  notes: InquiryNote[]
  activities: InquiryActivity[]
}

const route = useRoute()

const inquiry = ref<Inquiry | null>(null)

const loading = ref(true)
const errorMessage = ref('')

const newNote = ref('')
const savingNote = ref(false)
const noteError = ref('')

function formatDate(date: string) {
  if (!date) return '—'

  return new Date(
    `${date}T00:00:00`,
  ).toLocaleDateString('en-US', {
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  })
}

function formatCreatedAt(date: string) {
  if (!date) return '—'

  return new Date(date).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  })
}

function formatTime(time: string) {
  if (!time) return '—'

  const [hourString = '0', minuteString = '0'] =
    time.split(':')

  const hours = Number(hourString)
  const minutes = Number(minuteString)

  const date = new Date()

  date.setHours(
    hours,
    minutes,
    0,
    0,
  )

  return date.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  })
}

function activityLabel(type: string) {
  switch (type) {
    case 'InquiryCreated':
      return 'Inquiry'

    case 'StatusChanged':
      return 'Status'

    case 'NoteAdded':
      return 'Note'

    case 'QuoteCreated':
      return 'Quote'

    case 'QuoteSent':
      return 'Quote'

    case 'QuoteAccepted':
      return 'Quote'

    case 'MessageSent':
      return 'Message'

    case 'MessageReceived':
      return 'Message'

    case 'BookingCreated':
      return 'Booking'

    default:
      return 'Activity'
  }
}

async function loadInquiry() {
  loading.value = true
  errorMessage.value = ''

  try {
    const response = await fetch(
      `http://localhost:5128/api/inquiries/${route.params.id}`,
    )

    if (response.status === 404) {
      throw new Error('Inquiry not found.')
    }

    if (!response.ok) {
      throw new Error(
        `Server returned ${response.status}`,
      )
    }

    inquiry.value = await response.json()
  } catch (error) {
    console.error(error)

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Could not load inquiry.'
  } finally {
    loading.value = false
  }
}

async function addNote() {
  noteError.value = ''

  const body = newNote.value.trim()

  if (!body) {
    noteError.value = 'Enter a note before saving.'
    return
  }

  savingNote.value = true

  try {
    const response = await fetch(
      `http://localhost:5128/api/inquiries/${route.params.id}/notes`,
      {
        method: 'POST',

        headers: {
          'Content-Type': 'application/json',
        },

        body: JSON.stringify({
          body,
        }),
      },
    )

    if (!response.ok) {
      let message = 'Could not save note.'

      try {
        const data = await response.json()

        if (data.message) {
          message = data.message
        }
      } catch {
        // Keep the default message if the API
        // does not return JSON.
      }

      throw new Error(message)
    }

    newNote.value = ''

    // Reload so both the new note and its
    // automatic activity entry appear.
    await loadInquiry()
  } catch (error) {
    console.error(error)

    noteError.value =
      error instanceof Error
        ? error.message
        : 'Could not save note.'
  } finally {
    savingNote.value = false
  }
}

onMounted(loadInquiry)
</script>

<template>
  <main class="detail-page">
    <div class="detail-container">

      <RouterLink
        class="back-link"
        to="/admin/inquiries"
      >
        ← Back to Inquiries
      </RouterLink>

      <div
        v-if="loading"
        class="state-message"
      >
        Loading inquiry...
      </div>

      <div
        v-else-if="errorMessage"
        class="state-message error"
      >
        {{ errorMessage }}
      </div>

      <template v-else-if="inquiry">

        <!-- HEADER -->

        <header class="detail-header">
          <div>
            <p class="eyebrow">
              Inquiry #{{ inquiry.id }}
            </p>

            <h1>
              {{ inquiry.firstName }}
              {{ inquiry.lastName }}
            </h1>

            <p class="submitted">
              Submitted
              {{ formatCreatedAt(inquiry.createdAt) }}
            </p>

            <p
              v-if="inquiry.customerId"
              class="customer-reference"
            >
              Customer #{{ inquiry.customerId }}
            </p>
          </div>

          <span
            class="status"
            :class="inquiry.status.toLowerCase()"
          >
            {{ inquiry.status }}
          </span>
        </header>


        <!-- EVENT -->

        <section class="panel">
          <p class="section-label">
            Event
          </p>

          <div class="info-grid">

            <div>
              <span>Event Type</span>

              <strong>
                {{ inquiry.eventType || 'Not specified' }}
              </strong>
            </div>

            <div>
              <span>Date</span>

              <strong>
                {{ formatDate(inquiry.eventDate) }}
              </strong>
            </div>

            <div>
              <span>Time</span>

              <strong>
                {{ formatTime(inquiry.eventTime) }}
              </strong>
            </div>

            <div>
              <span>Guests</span>

              <strong>
                {{ inquiry.guestCount }}
              </strong>
            </div>

          </div>
        </section>


        <!-- CUSTOMER -->

        <section class="panel">
          <p class="section-label">
            Customer
          </p>

          <div class="info-grid">

            <div>
              <span>Name</span>

              <strong>
                {{ inquiry.firstName }}
                {{ inquiry.lastName }}
              </strong>
            </div>

            <div v-if="inquiry.company">
              <span>Company</span>

              <strong>
                {{ inquiry.company }}
              </strong>
            </div>

            <div>
              <span>Email</span>

              <a :href="`mailto:${inquiry.email}`">
                {{ inquiry.email }}
              </a>
            </div>

            <div>
              <span>Phone</span>

              <a :href="`tel:${inquiry.phone}`">
                {{ inquiry.phone }}
              </a>
            </div>

          </div>
        </section>


        <!-- SERVICES -->

        <section class="panel">
          <p class="section-label">
            Services
          </p>

          <div class="info-grid">

            <div>
              <span>Requested Services</span>

              <strong>
                {{
                  inquiry.services.length
                    ? inquiry.services.join(', ')
                    : 'Not specified'
                }}
              </strong>
            </div>

            <div v-if="inquiry.cateringType">
              <span>Catering</span>

              <strong>
                {{ inquiry.cateringType }}
              </strong>
            </div>

            <div v-if="inquiry.serviceType">
              <span>Pickup / Delivery</span>

              <strong>
                {{ inquiry.serviceType }}
              </strong>
            </div>

            <div v-if="inquiry.packaging">
              <span>Packaging</span>

              <strong>
                {{ inquiry.packaging }}
              </strong>
            </div>

            <div v-if="inquiry.recurring">
              <span>Frequency</span>

              <strong>
                {{ inquiry.recurring }}
              </strong>
            </div>

            <div v-if="inquiry.deliveryAddress">
              <span>Delivery Address</span>

              <strong>
                {{ inquiry.deliveryAddress }}
              </strong>
            </div>

          </div>
        </section>


        <!-- DIETARY -->

        <section
          v-if="
            inquiry.dietaryNeeds.length ||
            inquiry.otherDietaryNeeds
          "
          class="panel"
        >
          <p class="section-label">
            Dietary Needs
          </p>

          <p>
            {{
              inquiry.dietaryNeeds.length
                ? inquiry.dietaryNeeds.join(', ')
                : 'None specified'
            }}
          </p>

          <p v-if="inquiry.otherDietaryNeeds">
            {{ inquiry.otherDietaryNeeds }}
          </p>
        </section>


        <!-- CUSTOMER MESSAGE -->

        <section class="panel message-panel">
          <p class="section-label">
            Customer Message
          </p>

          <p>
            {{ inquiry.details || 'No message provided.' }}
          </p>
        </section>


        <!-- CRM AREA -->

        <div class="crm-grid">

          <!-- INTERNAL NOTES -->

          <section class="panel crm-panel">

            <div class="panel-heading">
              <div>
                <p class="section-label">
                  Internal Notes
                </p>

                <h2>Staff Notes</h2>
              </div>

              <span class="count">
                {{ inquiry.notes.length }}
              </span>
            </div>

            <p class="helper-text">
              These notes are internal and are never shown
              to the customer.
            </p>

            <form
              class="note-form"
              @submit.prevent="addNote"
            >
              <textarea
                v-model="newNote"
                rows="4"
                placeholder="Add an internal note..."
                :disabled="savingNote"
              ></textarea>

              <div class="note-actions">

                <p
                  v-if="noteError"
                  class="note-error"
                >
                  {{ noteError }}
                </p>

                <button
                  type="submit"
                  class="save-note-button"
                  :disabled="savingNote"
                >
                  {{
                    savingNote
                      ? 'Saving...'
                      : 'Add Note'
                  }}
                </button>

              </div>
            </form>

            <div
              v-if="inquiry.notes.length"
              class="notes-list"
            >
              <article
                v-for="note in inquiry.notes"
                :key="note.id"
                class="note"
              >
                <p class="note-body">
                  {{ note.body }}
                </p>

                <p class="note-date">
                  {{ formatCreatedAt(note.createdAt) }}
                </p>
              </article>
            </div>

            <div
              v-else
              class="empty-state"
            >
              No internal notes yet.
            </div>

          </section>


          <!-- ACTIVITY -->

          <section class="panel crm-panel">

            <div class="panel-heading">
              <div>
                <p class="section-label">
                  Activity
                </p>

                <h2>History</h2>
              </div>

              <span class="count">
                {{ inquiry.activities.length }}
              </span>
            </div>

            <div
              v-if="inquiry.activities.length"
              class="timeline"
            >
              <article
                v-for="activity in inquiry.activities"
                :key="activity.id"
                class="timeline-item"
              >
                <div class="timeline-marker"></div>

                <div class="timeline-content">

                  <span class="activity-type">
                    {{ activityLabel(activity.type) }}
                  </span>

                  <p>
                    {{ activity.description }}
                  </p>

                  <time>
                    {{ formatCreatedAt(activity.createdAt) }}
                  </time>

                </div>
              </article>
            </div>

            <div
              v-else
              class="empty-state"
            >
              No activity recorded yet.
            </div>

          </section>

        </div>

      </template>

    </div>
  </main>
</template>

<style scoped>
.detail-page {
  min-height: 100vh;
  padding: 70px 24px 120px;
  background: #efede7;
  color: #22221f;
}

.detail-container {
  width: min(1050px, 100%);
  margin: 0 auto;
}

.back-link {
  display: inline-block;
  margin-bottom: 35px;
  font-weight: 700;
  color: #55524c;
}

.back-link:hover {
  text-decoration: underline;
}

.detail-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 30px;
  margin-bottom: 40px;
}

.eyebrow,
.section-label {
  margin: 0 0 10px;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: #817c73;
}

.detail-header h1 {
  margin: 0;
  font-size: clamp(3rem, 7vw, 5rem);
  line-height: 1;
  letter-spacing: -0.045em;
}

.submitted {
  margin: 14px 0 0;
  color: #6d6961;
}

.customer-reference {
  margin: 6px 0 0;
  font-size: 0.82rem;
  font-weight: 700;
  color: #817c73;
}

.status {
  padding: 10px 16px;
  border-radius: 999px;
  background: #ece9e2;
  font-size: 0.8rem;
  font-weight: 700;
}

.status.new {
  background: #e5efe7;
  color: #31593a;
}

.status.contacted {
  background: #e8edf4;
  color: #334e70;
}

.status.quoted {
  background: #f5edd8;
  color: #72591c;
}

.status.booked {
  background: #e4efe9;
  color: #25583d;
}

.status.closed {
  background: #ecebea;
  color: #66615a;
}

.panel {
  margin-bottom: 18px;
  padding: 30px;
  border: 1px solid #dedad1;
  border-radius: 16px;
  background: white;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 28px;
}

.info-grid div {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.info-grid span {
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: #817c73;
}

.info-grid a {
  font-weight: 700;
}

.info-grid a:hover {
  text-decoration: underline;
}

.message-panel > p:last-child {
  margin: 0;
  line-height: 1.7;
  white-space: pre-wrap;
}


/* CRM */

.crm-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 18px;
  margin-top: 32px;
}

.crm-panel {
  margin-bottom: 0;
}

.panel-heading {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 20px;
}

.panel-heading h2 {
  margin: 0;
  font-size: 1.65rem;
  letter-spacing: -0.03em;
}

.count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 32px;
  height: 32px;
  padding: 0 10px;
  border-radius: 999px;
  background: #efede7;
  font-size: 0.8rem;
  font-weight: 700;
}

.helper-text {
  margin: 12px 0 22px;
  color: #777269;
  font-size: 0.88rem;
  line-height: 1.5;
}


/* NOTE FORM */

.note-form {
  margin-bottom: 28px;
}

.note-form textarea {
  width: 100%;
  resize: vertical;
  min-height: 110px;
  padding: 14px 16px;
  border: 1px solid #d8d3c9;
  border-radius: 10px;
  background: #faf9f6;
  color: #22221f;
  font: inherit;
  line-height: 1.5;
}

.note-form textarea:focus {
  outline: 2px solid #22221f;
  outline-offset: 2px;
}

.note-form textarea:disabled {
  opacity: 0.65;
}

.note-actions {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 15px;
  margin-top: 12px;
}

.save-note-button {
  border: 0;
  border-radius: 999px;
  padding: 11px 20px;
  background: #22221f;
  color: white;
  font: inherit;
  font-size: 0.86rem;
  font-weight: 700;
  cursor: pointer;
}

.save-note-button:hover:not(:disabled) {
  opacity: 0.85;
}

.save-note-button:disabled {
  cursor: wait;
  opacity: 0.55;
}

.note-error {
  margin: 0 auto 0 0;
  color: #8a3029;
  font-size: 0.82rem;
}


/* NOTES */

.notes-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.note {
  padding: 16px;
  border-radius: 10px;
  background: #f5f3ee;
}

.note-body {
  margin: 0;
  line-height: 1.6;
  white-space: pre-wrap;
}

.note-date {
  margin: 10px 0 0;
  font-size: 0.76rem;
  color: #817c73;
}


/* ACTIVITY */

.timeline {
  margin-top: 25px;
}

.timeline-item {
  position: relative;
  display: grid;
  grid-template-columns: 18px 1fr;
  gap: 12px;
  padding-bottom: 26px;
}

.timeline-item:not(:last-child)::before {
  content: '';
  position: absolute;
  top: 14px;
  bottom: 0;
  left: 5px;
  width: 1px;
  background: #dedad1;
}

.timeline-marker {
  position: relative;
  z-index: 1;
  width: 11px;
  height: 11px;
  margin-top: 5px;
  border: 2px solid white;
  border-radius: 50%;
  background: #55524c;
  box-shadow: 0 0 0 1px #aaa49a;
}

.timeline-content {
  min-width: 0;
}

.activity-type {
  display: inline-block;
  margin-bottom: 5px;
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.09em;
  text-transform: uppercase;
  color: #817c73;
}

.timeline-content p {
  margin: 0 0 7px;
  line-height: 1.5;
}

.timeline-content time {
  font-size: 0.76rem;
  color: #817c73;
}

.empty-state {
  margin-top: 20px;
  padding: 24px;
  border: 1px dashed #d8d3c9;
  border-radius: 10px;
  text-align: center;
  color: #817c73;
}


/* STATES */

.state-message {
  padding: 50px;
  border-radius: 14px;
  background: white;
  text-align: center;
}

.state-message.error {
  color: #7b2c25;
}


/* MOBILE */

@media (max-width: 800px) {
  .crm-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 650px) {
  .detail-header {
    flex-direction: column;
  }

  .info-grid {
    grid-template-columns: 1fr;
  }

  .panel {
    padding: 22px;
  }

  .detail-page {
    padding-left: 16px;
    padding-right: 16px;
  }
}
</style>