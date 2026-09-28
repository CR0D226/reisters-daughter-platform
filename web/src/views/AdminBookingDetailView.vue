<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'

interface QuoteItem {
  id: number
  description: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

interface Booking {
  id: number
  status: string
  eventType: string
  eventDate: string
  eventTime: string
  guestCount: number
  total: number
  internalNotes: string | null
  createdAt: string
  updatedAt: string

  quote: {
    quoteId: number
    quoteNumber: string
    status: string
    customerMessage: string | null
    subtotal: number
    tax: number
    total: number
    items: QuoteItem[]
  }

  customer: {
    id: number
    firstName: string
    lastName: string
    company: string | null
    email: string
    phone: string
  } | null

  inquiry: {
    inquiryId: number
    eventType: string
    services: string[]
    cateringType: string | null
    serviceType: string | null
    deliveryAddress: string | null
    packaging: string | null
    dietaryNeeds: string[]
    otherDietaryNeeds: string | null
    recurring: string | null
    details: string
  }
}

const route = useRoute()

const booking = ref<Booking | null>(null)

const loading = ref(true)
const error = ref('')

const savingStatus = ref(false)
const savingNotes = ref(false)

const statusMessage = ref('')
const notesMessage = ref('')

const internalNotes = ref('')

const bookingStatuses = [
  'Confirmed',
  'In Preparation',
  'Completed',
  'Cancelled',
]

const bookingId = computed(() => Number(route.params.id))

function formatMoney(value: number) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(value)
}

function formatEventDate(value: string) {
  if (!value) return 'Date TBD'

  const parts = value.split('-')

  if (parts.length !== 3) {
    return value
  }

  const year = Number(parts[0])
  const month = Number(parts[1])
  const day = Number(parts[2])

  if (
    !Number.isFinite(year) ||
    !Number.isFinite(month) ||
    !Number.isFinite(day)
  ) {
    return value
  }

  return new Intl.DateTimeFormat('en-US', {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  }).format(
    new Date(year, month - 1, day),
  )
}

function formatEventTime(value: string) {
  if (!value) return 'Time TBD'

  const parts = value.split(':')

  if (parts.length < 2) {
    return value
  }

  const hours = Number(parts[0])
  const minutes = Number(parts[1])

  if (
    !Number.isFinite(hours) ||
    !Number.isFinite(minutes)
  ) {
    return value
  }

  const date = new Date()
  date.setHours(hours, minutes, 0, 0)

  return new Intl.DateTimeFormat('en-US', {
    hour: 'numeric',
    minute: '2-digit',
  }).format(date)
}

function statusClass(status: string) {
  return status
    .toLowerCase()
    .replace(/\s+/g, '-')
}

async function loadBooking() {
  loading.value = true
  error.value = ''

  try {
    const response = await fetch(
      `http://localhost:5128/api/bookings/${bookingId.value}`,
    )

    if (!response.ok) {
      throw new Error(
        `Unable to load booking (${response.status}).`,
      )
    }

    booking.value = await response.json()

    internalNotes.value =
      booking.value?.internalNotes ?? ''
  } catch (err) {
    error.value =
      err instanceof Error
        ? err.message
        : 'Unable to load booking.'
  } finally {
    loading.value = false
  }
}

async function updateStatus(status: string) {
  if (!booking.value) return

  if (booking.value.status === status) {
    return
  }

  savingStatus.value = true
  statusMessage.value = ''

  try {
    const response = await fetch(
      `http://localhost:5128/api/bookings/${booking.value.id}/status`,
      {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          status,
        }),
      },
    )

    const result = await response.json()

    if (!response.ok) {
      throw new Error(
        result.message ?? 'Unable to update status.',
      )
    }

    booking.value.status = result.status
    booking.value.updatedAt = result.updatedAt

    statusMessage.value = 'Status updated.'
  } catch (err) {
    statusMessage.value =
      err instanceof Error
        ? err.message
        : 'Unable to update status.'
  } finally {
    savingStatus.value = false
  }
}

async function saveInternalNotes() {
  if (!booking.value) return

  savingNotes.value = true
  notesMessage.value = ''

  try {
    const response = await fetch(
      `http://localhost:5128/api/bookings/${booking.value.id}/notes`,
      {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          internalNotes: internalNotes.value,
        }),
      },
    )

    const result = await response.json()

    if (!response.ok) {
      throw new Error(
        result.message ?? 'Unable to save notes.',
      )
    }

    booking.value.internalNotes =
      result.internalNotes

    booking.value.updatedAt =
      result.updatedAt

    internalNotes.value =
      result.internalNotes ?? ''

    notesMessage.value = 'Notes saved.'
  } catch (err) {
    notesMessage.value =
      err instanceof Error
        ? err.message
        : 'Unable to save notes.'
  } finally {
    savingNotes.value = false
  }
}

onMounted(loadBooking)
</script>

<template>
  <main class="booking-page">
    <RouterLink
      to="/admin/bookings"
      class="back-link"
    >
      ← Back to Bookings
    </RouterLink>

    <section
      v-if="loading"
      class="state-card"
    >
      Loading booking...
    </section>

    <section
      v-else-if="error"
      class="state-card error-state"
    >
      <strong>Unable to load booking</strong>

      <p>{{ error }}</p>

      <button
        type="button"
        class="primary-button"
        @click="loadBooking"
      >
        Try Again
      </button>
    </section>

    <template v-else-if="booking">
      <header class="page-header">
        <div>
          <p class="eyebrow">
            BOOKING #{{ booking.id }}
          </p>

          <h1>{{ booking.eventType }}</h1>

          <p class="event-date">
            {{ formatEventDate(booking.eventDate) }}
            ·
            {{ formatEventTime(booking.eventTime) }}
          </p>
        </div>

        <span
          class="status"
          :class="statusClass(booking.status)"
        >
          {{ booking.status }}
        </span>
      </header>

      <section class="summary-grid">
        <article>
          <span>Guests</span>
          <strong>{{ booking.guestCount }}</strong>
        </article>

        <article>
          <span>Total</span>
          <strong>{{ formatMoney(booking.total) }}</strong>
        </article>

        <article>
          <span>Quote</span>

          <RouterLink
            :to="`/admin/quotes/${booking.quote.quoteId}`"
          >
            {{ booking.quote.quoteNumber }}
          </RouterLink>
        </article>

        <article>
          <span>Inquiry</span>

          <RouterLink
            :to="`/admin/inquiries/${booking.inquiry.inquiryId}`"
          >
            #{{ booking.inquiry.inquiryId }}
          </RouterLink>
        </article>
      </section>

      <div class="content-grid">
        <div class="main-column">
          <!-- ORDER -->
          <section class="panel">
            <h2>Order</h2>

            <div class="quote-items">
              <div
                v-for="item in booking.quote.items"
                :key="item.id"
                class="quote-item"
              >
                <div>
                  <strong>{{ item.description }}</strong>

                  <span>
                    {{ item.quantity }} ×
                    {{ formatMoney(item.unitPrice) }}
                  </span>
                </div>

                <strong>
                  {{ formatMoney(item.lineTotal) }}
                </strong>
              </div>
            </div>

            <div class="totals">
              <div>
                <span>Subtotal</span>

                <span>
                  {{ formatMoney(booking.quote.subtotal) }}
                </span>
              </div>

              <div>
                <span>Tax</span>

                <span>
                  {{ formatMoney(booking.quote.tax) }}
                </span>
              </div>

              <div class="grand-total">
                <strong>Total</strong>

                <strong>
                  {{ formatMoney(booking.quote.total) }}
                </strong>
              </div>
            </div>
          </section>

          <!-- EVENT DETAILS -->
          <section class="panel">
            <h2>Event Details</h2>

            <dl class="details">
              <div>
                <dt>Event Type</dt>
                <dd>{{ booking.eventType }}</dd>
              </div>

              <div>
                <dt>Date</dt>

                <dd>
                  {{ formatEventDate(booking.eventDate) }}
                </dd>
              </div>

              <div>
                <dt>Time</dt>

                <dd>
                  {{ formatEventTime(booking.eventTime) }}
                </dd>
              </div>

              <div>
                <dt>Guest Count</dt>
                <dd>{{ booking.guestCount }}</dd>
              </div>

              <div
                v-if="booking.inquiry.services.length"
              >
                <dt>Services</dt>

                <dd>
                  {{ booking.inquiry.services.join(', ') }}
                </dd>
              </div>

              <div v-if="booking.inquiry.cateringType">
                <dt>Catering Type</dt>

                <dd>
                  {{ booking.inquiry.cateringType }}
                </dd>
              </div>

              <div v-if="booking.inquiry.serviceType">
                <dt>Service Type</dt>

                <dd>
                  {{ booking.inquiry.serviceType }}
                </dd>
              </div>

              <div v-if="booking.inquiry.deliveryAddress">
                <dt>Delivery Address</dt>

                <dd>
                  {{ booking.inquiry.deliveryAddress }}
                </dd>
              </div>

              <div v-if="booking.inquiry.packaging">
                <dt>Packaging</dt>

                <dd>
                  {{ booking.inquiry.packaging }}
                </dd>
              </div>

              <div v-if="booking.inquiry.recurring">
                <dt>Recurring</dt>

                <dd>
                  {{ booking.inquiry.recurring }}
                </dd>
              </div>

              <div
                v-if="booking.inquiry.dietaryNeeds.length"
              >
                <dt>Dietary Needs</dt>

                <dd>
                  {{ booking.inquiry.dietaryNeeds.join(', ') }}
                </dd>
              </div>

              <div
                v-if="booking.inquiry.otherDietaryNeeds"
              >
                <dt>Other Dietary Needs</dt>

                <dd>
                  {{ booking.inquiry.otherDietaryNeeds }}
                </dd>
              </div>
            </dl>
          </section>

          <!-- CUSTOMER NOTES -->
          <section
            v-if="
              booking.inquiry.details ||
              booking.quote.customerMessage
            "
            class="panel"
          >
            <h2>Customer Notes</h2>

            <div
              v-if="booking.inquiry.details"
              class="note"
            >
              <span>Inquiry</span>

              <p>
                {{ booking.inquiry.details }}
              </p>
            </div>

            <div
              v-if="booking.quote.customerMessage"
              class="note"
            >
              <span>Quote Message</span>

              <p>
                {{ booking.quote.customerMessage }}
              </p>
            </div>
          </section>
        </div>

        <aside class="side-column">
          <!-- CUSTOMER -->
          <section class="panel">
            <h2>Customer</h2>

            <template v-if="booking.customer">
              <h3>
                {{ booking.customer.firstName }}
                {{ booking.customer.lastName }}
              </h3>

              <p v-if="booking.customer.company">
                {{ booking.customer.company }}
              </p>

              <div class="contact-links">
                <a
                  :href="`mailto:${booking.customer.email}`"
                >
                  {{ booking.customer.email }}
                </a>

                <a
                  v-if="booking.customer.phone"
                  :href="`tel:${booking.customer.phone}`"
                >
                  {{ booking.customer.phone }}
                </a>
              </div>
            </template>

            <p v-else class="muted">
              No linked customer record.
            </p>
          </section>

          <!-- INTERNAL NOTES -->
          <section class="panel">
            <h2>Internal Notes</h2>

            <textarea
              v-model="internalNotes"
              class="notes-input"
              rows="6"
              placeholder="Add preparation, fulfillment, delivery, or event notes..."
            />

            <div class="notes-actions">
              <button
                type="button"
                class="primary-button"
                :disabled="savingNotes"
                @click="saveInternalNotes"
              >
                {{ savingNotes ? 'Saving...' : 'Save Notes' }}
              </button>

              <span
                v-if="notesMessage"
                class="save-message"
              >
                {{ notesMessage }}
              </span>
            </div>
          </section>

          <!-- WORKFLOW -->
          <section class="panel workflow-panel">
            <h2>Workflow</h2>

            <p class="current-status">
              Current status:
              <strong>{{ booking.status }}</strong>
            </p>

            <div class="status-controls">
              <button
                v-for="statusOption in bookingStatuses"
                :key="statusOption"
                type="button"
                class="status-button"
                :class="{
                  active:
                    booking.status === statusOption,
                  cancelled:
                    statusOption === 'Cancelled',
                }"
                :disabled="
                  savingStatus ||
                  booking.status === statusOption
                "
                @click="updateStatus(statusOption)"
              >
                {{ statusOption }}
              </button>
            </div>

            <p
              v-if="statusMessage"
              class="save-message"
            >
              {{ statusMessage }}
            </p>
          </section>
        </aside>
      </div>
    </template>
  </main>
</template>

<style scoped>
.booking-page {
  width: min(1180px, calc(100% - 40px));
  margin: 0 auto;
  padding: 48px 0 100px;
}

.back-link {
  display: inline-block;
  margin-bottom: 34px;
  font-weight: 600;
  text-decoration: none;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 32px;
  margin-bottom: 36px;
}

.eyebrow {
  margin: 0 0 8px;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.16em;
}

h1 {
  margin: 0;
  font-size: clamp(2.5rem, 6vw, 4.5rem);
  line-height: 1;
}

.event-date {
  margin: 14px 0 0;
  font-size: 1.1rem;
  opacity: 0.65;
}

.status {
  padding: 8px 14px;
  border-radius: 999px;
  background: #ececec;
  font-size: 0.8rem;
  font-weight: 700;
}

.status.confirmed {
  background: #e7f5ea;
}

.status.in-preparation {
  background: #fff1cf;
}

.status.completed {
  background: #e7eef8;
}

.status.cancelled {
  background: #f8e5e5;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 14px;
  margin-bottom: 24px;
}

.summary-grid article {
  display: flex;
  flex-direction: column;
  gap: 7px;
  padding: 20px;
  border: 1px solid rgba(0, 0, 0, 0.1);
  border-radius: 14px;
}

.summary-grid span {
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  opacity: 0.5;
}

.summary-grid strong,
.summary-grid a {
  font-size: 1.15rem;
  font-weight: 700;
}

.content-grid {
  display: grid;
  grid-template-columns:
    minmax(0, 2fr)
    minmax(280px, 1fr);
  gap: 24px;
  align-items: start;
}

.main-column,
.side-column {
  display: grid;
  gap: 24px;
}

.panel {
  padding: 28px;
  border: 1px solid rgba(0, 0, 0, 0.1);
  border-radius: 16px;
  background: #fff;
}

.panel h2 {
  margin: 0 0 22px;
  font-size: 1.25rem;
}

.panel h3 {
  margin: 0 0 5px;
}

.quote-items {
  display: grid;
}

.quote-item {
  display: flex;
  justify-content: space-between;
  gap: 24px;
  padding: 16px 0;
  border-bottom: 1px solid rgba(0, 0, 0, 0.08);
}

.quote-item > div {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.quote-item span {
  font-size: 0.85rem;
  opacity: 0.55;
}

.totals {
  width: min(100%, 340px);
  margin: 22px 0 0 auto;
}

.totals > div {
  display: flex;
  justify-content: space-between;
  padding: 7px 0;
}

.grand-total {
  margin-top: 7px;
  padding-top: 14px !important;
  border-top: 1px solid rgba(0, 0, 0, 0.12);
  font-size: 1.1rem;
}

.details {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 22px;
  margin: 0;
}

.details div {
  display: grid;
  gap: 5px;
}

.details dt,
.note span {
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.07em;
  text-transform: uppercase;
  opacity: 0.5;
}

.details dd {
  margin: 0;
  font-weight: 600;
}

.note + .note {
  margin-top: 20px;
}

.note p {
  margin: 6px 0 0;
  line-height: 1.55;
}

.contact-links {
  display: grid;
  gap: 9px;
  margin-top: 20px;
}

.muted {
  opacity: 0.55;
}

/* INTERNAL NOTES */

.notes-input {
  width: 100%;
  min-height: 130px;
  box-sizing: border-box;
  padding: 14px;
  border: 1px solid rgba(0, 0, 0, 0.14);
  border-radius: 10px;
  background: #fff;
  font: inherit;
  line-height: 1.5;
  resize: vertical;
}

.notes-input:focus {
  outline: 2px solid rgba(0, 0, 0, 0.15);
  outline-offset: 2px;
}

.notes-actions {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
  margin-top: 12px;
}

.primary-button {
  padding: 10px 16px;
  border: 0;
  border-radius: 9px;
  background: #1d1d1d;
  color: #fff;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}

.primary-button:hover:not(:disabled) {
  opacity: 0.85;
}

.primary-button:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

/* WORKFLOW */

.workflow-panel p {
  margin-bottom: 8px;
}

.current-status {
  margin: 0 0 16px !important;
}

.status-controls {
  display: grid;
  gap: 8px;
}

.status-button {
  width: 100%;
  padding: 11px 13px;
  border: 1px solid rgba(0, 0, 0, 0.13);
  border-radius: 9px;
  background: #fff;
  font: inherit;
  font-weight: 600;
  text-align: left;
  cursor: pointer;
}

.status-button:hover:not(:disabled) {
  background: rgba(0, 0, 0, 0.04);
}

.status-button.active {
  background: #e7f5ea;
  border-color: #bdddc5;
}

.status-button.cancelled:not(.active) {
  color: #9c3030;
}

.status-button.active.cancelled {
  background: #f8e5e5;
  border-color: #e1bcbc;
  color: #8b2929;
}

.status-button:disabled {
  cursor: default;
  opacity: 0.65;
}

.save-message {
  display: inline-block;
  margin-top: 12px;
  font-size: 0.85rem;
  font-weight: 600;
  opacity: 0.65;
}

.state-card {
  padding: 48px;
  border: 1px solid rgba(0, 0, 0, 0.1);
  border-radius: 16px;
  text-align: center;
}

.error-state p {
  margin: 10px 0 20px;
}

@media (max-width: 800px) {
  .summary-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .content-grid {
    grid-template-columns: 1fr;
  }

  .details {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 520px) {
  .booking-page {
    width: min(100% - 28px, 1180px);
  }

  .page-header {
    flex-direction: column;
  }

  .summary-grid {
    grid-template-columns: 1fr 1fr;
  }
}
</style>