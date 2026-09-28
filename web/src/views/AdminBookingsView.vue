<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

interface BookingCustomer {
  id: number
  firstName: string
  lastName: string
  company: string | null
  email: string
  phone: string
}

interface BookingQuote {
  quoteId: number
  quoteNumber: string
}

interface BookingInquiry {
  inquiryId: number
  services: string[]
  cateringType: string | null
  serviceType: string | null
  deliveryAddress: string | null
  packaging: string | null
  dietaryNeeds: string[]
  otherDietaryNeeds: string | null
  details: string
}

interface Booking {
  id: number
  status: string
  eventType: string
  eventDate: string
  eventTime: string
  guestCount: number
  total: number
  createdAt: string
  updatedAt: string
  quote: BookingQuote
  customer: BookingCustomer | null
  inquiry: BookingInquiry
}

const router = useRouter()

const bookings = ref<Booking[]>([])
const loading = ref(true)
const error = ref('')

const upcomingBookings = computed(() => {
  return bookings.value.filter(
    booking =>
      booking.status !== 'Completed' &&
      booking.status !== 'Cancelled',
  )
})

function formatMoney(value: number) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(value)
}

function formatEventDate(date: string) {
  if (!date) return 'Date TBD'

  const parts = date.split('-')

  if (parts.length !== 3) {
    return date
  }

  const year = Number(parts[0])
  const month = Number(parts[1])
  const day = Number(parts[2])

  if (
    !Number.isFinite(year) ||
    !Number.isFinite(month) ||
    !Number.isFinite(day)
  ) {
    return date
  }

  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(
    new Date(year, month - 1, day),
  )
}

function formatEventTime(time: string) {
  if (!time) return 'Time TBD'

  const parts = time.split(':')

  if (parts.length < 2) {
    return time
  }

  const hours = Number(parts[0])
  const minutes = Number(parts[1])

  if (
    !Number.isFinite(hours) ||
    !Number.isFinite(minutes)
  ) {
    return time
  }

  const date = new Date()
  date.setHours(hours, minutes, 0, 0)

  return new Intl.DateTimeFormat('en-US', {
    hour: 'numeric',
    minute: '2-digit',
  }).format(date)
}

function customerName(customer: BookingCustomer | null) {
  if (!customer) {
    return 'Customer unavailable'
  }

  return `${customer.firstName} ${customer.lastName}`.trim()
}

function statusClass(status: string) {
  return status
    .toLowerCase()
    .replace(/\s+/g, '-')
}

function openBooking(id: number) {
  router.push(`/admin/bookings/${id}`)
}

async function loadBookings() {
  loading.value = true
  error.value = ''

  try {
    const response = await fetch(
      'http://localhost:5128/api/bookings',
    )

    if (!response.ok) {
      throw new Error(
        `Unable to load bookings (${response.status}).`,
      )
    }

    bookings.value = await response.json()
  } catch (err) {
    error.value =
      err instanceof Error
        ? err.message
        : 'Unable to load bookings.'
  } finally {
    loading.value = false
  }
}

onMounted(loadBookings)
</script>

<template>
  <main class="admin-bookings">
    <section class="bookings-header">
      <div>
        <p class="eyebrow">
          ADMIN
        </p>

        <h1>Bookings</h1>

        <p class="subtitle">
          Upcoming events and accepted customer quotes.
        </p>
      </div>

      <div class="booking-count">
        <strong>{{ upcomingBookings.length }}</strong>
        <span>Upcoming</span>
      </div>
    </section>

    <section
      v-if="loading"
      class="state-card"
    >
      Loading bookings...
    </section>

    <section
      v-else-if="error"
      class="state-card error"
    >
      <strong>Unable to load bookings</strong>

      <p>{{ error }}</p>

      <button
        type="button"
        @click="loadBookings"
      >
        Try Again
      </button>
    </section>

    <section
      v-else-if="bookings.length === 0"
      class="state-card"
    >
      <h2>No bookings yet</h2>

      <p>
        Accepted quotes will automatically appear here.
      </p>
    </section>

    <section
      v-else
      class="booking-list"
    >
      <article
        v-for="booking in bookings"
        :key="booking.id"
        class="booking-card"
        role="link"
        tabindex="0"
        @click="openBooking(booking.id)"
        @keydown.enter="openBooking(booking.id)"
        @keydown.space.prevent="openBooking(booking.id)"
      >
        <div class="booking-date">
          <span class="date-label">
            {{ formatEventDate(booking.eventDate) }}
          </span>

          <strong>
            {{ formatEventTime(booking.eventTime) }}
          </strong>
        </div>

        <div class="booking-main">
          <div class="booking-title-row">
            <div>
              <p class="booking-number">
                Booking #{{ booking.id }}
              </p>

              <h2>
                {{ booking.eventType }}
              </h2>
            </div>

            <span
              class="status"
              :class="statusClass(booking.status)"
            >
              {{ booking.status }}
            </span>
          </div>

          <div class="customer">
            <strong>
              {{ customerName(booking.customer) }}
            </strong>

            <span v-if="booking.customer?.company">
              {{ booking.customer.company }}
            </span>
          </div>

          <div class="booking-details">
            <div>
              <span>Guests</span>

              <strong>
                {{ booking.guestCount }}
              </strong>
            </div>

            <div>
              <span>Total</span>

              <strong>
                {{ formatMoney(booking.total) }}
              </strong>
            </div>

            <div>
              <span>Quote</span>

              <RouterLink
                :to="`/admin/quotes/${booking.quote.quoteId}`"
                @click.stop
              >
                {{ booking.quote.quoteNumber }}
              </RouterLink>
            </div>

            <div>
              <span>Inquiry</span>

              <RouterLink
                :to="`/admin/inquiries/${booking.inquiry.inquiryId}`"
                @click.stop
              >
                #{{ booking.inquiry.inquiryId }}
              </RouterLink>
            </div>
          </div>

          <div
            v-if="booking.inquiry.services.length"
            class="services"
          >
            <span
              v-for="service in booking.inquiry.services"
              :key="service"
              class="service-chip"
            >
              {{ service }}
            </span>
          </div>

          <div
            v-if="booking.inquiry.details"
            class="event-notes"
          >
            <span>Customer notes</span>

            <p>
              {{ booking.inquiry.details }}
            </p>
          </div>

          <div
            v-if="booking.customer"
            class="contact"
          >
            <a
              :href="`mailto:${booking.customer.email}`"
              @click.stop
            >
              {{ booking.customer.email }}
            </a>

            <a
              v-if="booking.customer.phone"
              :href="`tel:${booking.customer.phone}`"
              @click.stop
            >
              {{ booking.customer.phone }}
            </a>
          </div>
        </div>
      </article>
    </section>
  </main>
</template>

<style scoped>
.admin-bookings {
  width: min(1180px, calc(100% - 40px));
  margin: 0 auto;
  padding: 64px 0 100px;
}

.bookings-header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 32px;
  margin-bottom: 40px;
}

.eyebrow {
  margin: 0 0 8px;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.18em;
}

h1 {
  margin: 0;
  font-size: clamp(2.5rem, 6vw, 4.5rem);
  line-height: 1;
}

.subtitle {
  margin: 14px 0 0;
  opacity: 0.7;
}

.booking-count {
  min-width: 120px;
  padding: 18px 24px;
  border: 1px solid rgba(0, 0, 0, 0.12);
  border-radius: 16px;
  text-align: center;
}

.booking-count strong {
  display: block;
  font-size: 2rem;
}

.booking-count span {
  font-size: 0.8rem;
  opacity: 0.65;
}

.booking-list {
  display: grid;
  gap: 20px;
}

.booking-card {
  display: grid;
  grid-template-columns: 190px 1fr;
  overflow: hidden;
  border: 1px solid rgba(0, 0, 0, 0.12);
  border-radius: 18px;
  background: #fff;
  cursor: pointer;
  transition:
    transform 0.15s ease,
    box-shadow 0.15s ease,
    border-color 0.15s ease;
}

.booking-card:hover {
  transform: translateY(-2px);
  border-color: rgba(0, 0, 0, 0.2);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.06);
}

.booking-card:focus-visible {
  outline: 2px solid #1d1d1d;
  outline-offset: 3px;
}

.booking-date {
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 8px;
  padding: 28px;
  border-right: 1px solid rgba(0, 0, 0, 0.1);
  background: rgba(0, 0, 0, 0.025);
}

.booking-date strong {
  font-size: 1.35rem;
}

.date-label {
  font-size: 0.85rem;
  opacity: 0.65;
}

.booking-main {
  padding: 28px 32px;
}

.booking-title-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 24px;
}

.booking-number {
  margin: 0 0 4px;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  opacity: 0.55;
}

.booking-title-row h2 {
  margin: 0;
  font-size: 1.55rem;
}

.status {
  padding: 7px 12px;
  border-radius: 999px;
  background: #ececec;
  font-size: 0.75rem;
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

.customer {
  display: flex;
  flex-wrap: wrap;
  gap: 8px 14px;
  margin-top: 14px;
}

.customer span {
  opacity: 0.6;
}

.booking-details {
  display: grid;
  grid-template-columns: repeat(
    4,
    minmax(100px, 1fr)
  );
  gap: 16px;
  margin-top: 24px;
}

.booking-details > div {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.booking-details span,
.event-notes > span {
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  opacity: 0.5;
}

.booking-details a {
  font-weight: 700;
}

.services {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 24px;
}

.service-chip {
  padding: 6px 10px;
  border-radius: 999px;
  background: rgba(0, 0, 0, 0.06);
  font-size: 0.8rem;
}

.event-notes {
  margin-top: 24px;
  padding: 16px 18px;
  border-radius: 12px;
  background: rgba(0, 0, 0, 0.035);
}

.event-notes p {
  margin: 7px 0 0;
  line-height: 1.55;
}

.contact {
  display: flex;
  flex-wrap: wrap;
  gap: 18px;
  margin-top: 20px;
  font-size: 0.9rem;
}

.state-card {
  padding: 48px;
  border: 1px solid rgba(0, 0, 0, 0.12);
  border-radius: 18px;
  text-align: center;
}

.state-card p {
  margin-bottom: 20px;
}

.error {
  border-color: rgba(160, 30, 30, 0.25);
}

@media (max-width: 760px) {
  .admin-bookings {
    width: min(100% - 28px, 1180px);
    padding-top: 40px;
  }

  .bookings-header {
    align-items: flex-start;
  }

  .booking-count {
    min-width: 90px;
  }

  .booking-card {
    grid-template-columns: 1fr;
  }

  .booking-date {
    border-right: 0;
    border-bottom: 1px solid rgba(0, 0, 0, 0.1);
  }

  .booking-details {
    grid-template-columns: repeat(2, 1fr);
  }
}
</style>