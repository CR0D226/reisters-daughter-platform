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

type ViewMode = 'calendar' | 'list'

const router = useRouter()

const bookings = ref<Booking[]>([])
const loading = ref(true)
const error = ref('')
const viewMode = ref<ViewMode>('list')
const calendarDate = ref(new Date())

const upcomingEvents = computed(() => {
  return bookings.value
    .filter(
      booking =>
        booking.status !== 'Completed' &&
        booking.status !== 'Cancelled',
    )
    .sort((a, b) => {
      const aDate = `${a.eventDate}T${a.eventTime || '00:00'}`
      const bDate = `${b.eventDate}T${b.eventTime || '00:00'}`

      return aDate.localeCompare(bDate)
    })
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
  }).format(new Date(year, month - 1, day))
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

function openEvent(id: number) {
  router.push(`/admin/bookings/${id}`)
}

async function loadEvents() {
  loading.value = true
  error.value = ''

  try {
    const response = await fetch(
      'http://localhost:5128/api/bookings',
    )

    if (!response.ok) {
      throw new Error(
        `Unable to load events (${response.status}).`,
      )
    }

    bookings.value = await response.json()
  } catch (err) {
    error.value =
      err instanceof Error
        ? err.message
        : 'Unable to load events.'
  } finally {
    loading.value = false
  }
}
const calendarTitle = computed(() => {
  return new Intl.DateTimeFormat('en-US', {
    month: 'long',
    year: 'numeric',
  }).format(calendarDate.value)
})

const calendarDays = computed(() => {
  const year = calendarDate.value.getFullYear()
  const month = calendarDate.value.getMonth()

  const firstDay = new Date(year, month, 1)
  const lastDay = new Date(year, month + 1, 0)

  const days: Array<{
    date: Date
    dateKey: string
    dayNumber: number
    currentMonth: boolean
    today: boolean
    events: Booking[]
  }> = []

  const startOffset = firstDay.getDay()
  const totalCells = Math.ceil(
    (startOffset + lastDay.getDate()) / 7,
  ) * 7

  const startDate = new Date(
    year,
    month,
    1 - startOffset,
  )

  const today = new Date()

  for (let index = 0; index < totalCells; index++) {
    const date = new Date(
      startDate.getFullYear(),
      startDate.getMonth(),
      startDate.getDate() + index,
    )

    const dateKey = [
      date.getFullYear(),
      String(date.getMonth() + 1).padStart(2, '0'),
      String(date.getDate()).padStart(2, '0'),
    ].join('-')

    days.push({
      date,
      dateKey,
      dayNumber: date.getDate(),
      currentMonth: date.getMonth() === month,
      today:
        date.getFullYear() === today.getFullYear() &&
        date.getMonth() === today.getMonth() &&
        date.getDate() === today.getDate(),
      events: bookings.value
        .filter(
          booking =>
            booking.eventDate === dateKey &&
            booking.status !== 'Cancelled',
        )
        .sort((a, b) =>
          (a.eventTime || '').localeCompare(
            b.eventTime || '',
          ),
        ),
    })
  }

  return days
})

function previousMonth() {
  calendarDate.value = new Date(
    calendarDate.value.getFullYear(),
    calendarDate.value.getMonth() - 1,
    1,
  )
}

function nextMonth() {
  calendarDate.value = new Date(
    calendarDate.value.getFullYear(),
    calendarDate.value.getMonth() + 1,
    1,
  )
}

function goToToday() {
  calendarDate.value = new Date()
}

onMounted(loadEvents)
</script>

<template>
  <main class="admin-events">
    <section class="events-header">
      <div>
        <p class="eyebrow">ADMIN</p>

        <h1>Events</h1>

        <p class="subtitle">
          Everything happening at The Reister's Daughter.
        </p>
      </div>

      <div class="event-count">
        <strong>{{ upcomingEvents.length }}</strong>
        <span>Upcoming</span>
      </div>
    </section>

    <section class="events-toolbar">
      <div class="view-tabs">
        <button
          type="button"
          :class="{ active: viewMode === 'calendar' }"
          @click="viewMode = 'calendar'"
        >
          Calendar
        </button>

        <button
          type="button"
          :class="{ active: viewMode === 'list' }"
          @click="viewMode = 'list'"
        >
          List
        </button>
      </div>
    </section>

    <section
      v-if="loading"
      class="state-card"
    >
      Loading events...
    </section>

    <section
      v-else-if="error"
      class="state-card error"
    >
      <strong>Unable to load events</strong>

      <p>{{ error }}</p>

      <button
        type="button"
        @click="loadEvents"
      >
        Try Again
      </button>
    </section>

    <section
  v-else-if="viewMode === 'calendar'"
  class="event-calendar"
>
  <div class="calendar-header">
    <div>
      <p class="eyebrow">SCHEDULE</p>
      <h2>{{ calendarTitle }}</h2>
    </div>

    <div class="calendar-controls">
      <button
        type="button"
        @click="goToToday"
      >
        Today
      </button>

      <button
        type="button"
        aria-label="Previous month"
        @click="previousMonth"
      >
        ←
      </button>

      <button
        type="button"
        aria-label="Next month"
        @click="nextMonth"
      >
        →
      </button>
    </div>
  </div>

  <div class="calendar-grid calendar-weekdays">
    <div>Sun</div>
    <div>Mon</div>
    <div>Tue</div>
    <div>Wed</div>
    <div>Thu</div>
    <div>Fri</div>
    <div>Sat</div>
  </div>

  <div class="calendar-grid calendar-days">
    <div
      v-for="day in calendarDays"
      :key="day.dateKey"
      class="calendar-day"
      :class="{
        muted: !day.currentMonth,
        today: day.today,
      }"
    >
      <div class="calendar-day-header">
        <span>{{ day.dayNumber }}</span>
      </div>

      <div class="calendar-events">
        <button
          v-for="event in day.events"
          :key="event.id"
          type="button"
          class="calendar-event"
          @click="openEvent(event.id)"
        >
          <strong>
            {{ formatEventTime(event.eventTime) }}
          </strong>

          <span>
            {{ event.eventType }}
          </span>

          <small>
            {{ customerName(event.customer) }}
            · {{ event.guestCount }}
            {{ event.guestCount === 1 ? 'guest' : 'guests' }}
          </small>
        </button>
      </div>
    </div>
  </div>
</section>

    <section
      v-else-if="upcomingEvents.length === 0"
      class="state-card"
    >
      <h2>No upcoming events</h2>

      <p>
        Accepted customer quotes will appear here as events.
      </p>
    </section>

    <section
      v-else
      class="event-list"
    >
      <div class="section-heading">
        <div>
          <p class="eyebrow">SCHEDULE</p>
          <h2>Upcoming Events</h2>
        </div>

        <span>
          {{ upcomingEvents.length }}
          {{ upcomingEvents.length === 1 ? 'event' : 'events' }}
        </span>
      </div>

      <article
        v-for="booking in upcomingEvents"
        :key="booking.id"
        class="event-card"
        role="link"
        tabindex="0"
        @click="openEvent(booking.id)"
        @keydown.enter="openEvent(booking.id)"
        @keydown.space.prevent="openEvent(booking.id)"
      >
        <div class="event-date">
          <span>
            {{ formatEventDate(booking.eventDate) }}
          </span>

          <strong>
            {{ formatEventTime(booking.eventTime) }}
          </strong>
        </div>

        <div class="event-main">
          <div class="event-title-row">
            <div>
              <p class="event-number">
                Event #{{ booking.id }}
              </p>

              <h3>
                {{ booking.eventType }}
              </h3>
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

          <div class="event-meta">
            <span>
              {{ booking.guestCount }}
              {{ booking.guestCount === 1 ? 'guest' : 'guests' }}
            </span>

            <span>
              {{ formatMoney(booking.total) }}
            </span>

            <span>
              {{ booking.quote.quoteNumber }}
            </span>
          </div>

          <div
            v-if="booking.inquiry.services.length"
            class="services"
          >
            <span
              v-for="service in booking.inquiry.services"
              :key="service"
            >
              {{ service }}
            </span>
          </div>
        </div>

        <div class="event-arrow">
          →
        </div>
      </article>
    </section>
  </main>
</template>

<style scoped>
.admin-events {
  width: min(1120px, calc(100% - 40px));
  margin: 0 auto;
  padding: 48px 0 80px;
}

.events-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 32px;
  margin-bottom: 28px;
}

.eyebrow {
  margin: 0 0 8px;
  font-size: 0.75rem;
  font-weight: 800;
  letter-spacing: 0.14em;
  color: #7a6758;
}

h1 {
  margin: 0;
  font-size: clamp(2.25rem, 5vw, 4rem);
  line-height: 1;
}

.subtitle {
  max-width: 620px;
  margin: 14px 0 0;
  color: #6c625a;
  font-size: 1.05rem;
}

.event-count {
  min-width: 120px;
  padding: 18px 22px;
  border: 1px solid #ded7d1;
  border-radius: 16px;
  background: #fff;
  text-align: center;
}

.event-count strong {
  display: block;
  font-size: 1.75rem;
}

.event-count span {
  display: block;
  margin-top: 2px;
  color: #746a62;
  font-size: 0.82rem;
}

.events-toolbar {
  display: flex;
  justify-content: flex-start;
  margin-bottom: 28px;
  padding-bottom: 18px;
  border-bottom: 1px solid #e4ded9;
}

.view-tabs {
  display: inline-flex;
  gap: 6px;
  padding: 5px;
  border-radius: 12px;
  background: #eee9e5;
}

.view-tabs button {
  border: 0;
  border-radius: 9px;
  padding: 10px 18px;
  background: transparent;
  color: #5f564f;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}

.view-tabs button.active {
  background: #fff;
  color: #211d1a;
  box-shadow: 0 1px 5px rgba(0, 0, 0, 0.08);
}

.state-card,
.calendar-placeholder {
  padding: 48px 32px;
  border: 1px solid #e1dad4;
  border-radius: 18px;
  background: #fff;
  text-align: center;
}

.state-card.error {
  border-color: #d7a6a6;
}

.state-card button,
.calendar-placeholder button {
  margin-top: 12px;
  border: 0;
  border-radius: 9px;
  padding: 11px 17px;
  background: #29231f;
  color: white;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}

.calendar-placeholder {
  padding-top: 64px;
  padding-bottom: 64px;
}

.calendar-icon {
  display: grid;
  place-items: center;
  width: 58px;
  height: 58px;
  margin: 0 auto 18px;
  border-radius: 13px;
  background: #29231f;
  color: white;
  font-size: 1.25rem;
  font-weight: 800;
}

.calendar-placeholder h2 {
  margin: 0;
}

.calendar-placeholder p {
  max-width: 460px;
  margin: 10px auto 0;
  color: #746a62;
  line-height: 1.6;
}

.section-heading {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 20px;
  margin-bottom: 16px;
}

.section-heading h2 {
  margin: 0;
  font-size: 1.45rem;
}

.section-heading span {
  color: #776d65;
  font-size: 0.9rem;
}

.event-list {
  display: grid;
  gap: 14px;
}

.event-card {
  display: grid;
  grid-template-columns: 190px minmax(0, 1fr) 32px;
  gap: 24px;
  align-items: stretch;
  overflow: hidden;
  border: 1px solid #e0d9d3;
  border-radius: 17px;
  background: #fff;
  cursor: pointer;
  transition:
    transform 0.15s ease,
    box-shadow 0.15s ease,
    border-color 0.15s ease;
}

.event-card:hover {
  transform: translateY(-2px);
  border-color: #c8bcb2;
  box-shadow: 0 8px 24px rgba(35, 29, 25, 0.08);
}

.event-card:focus-visible {
  outline: 3px solid #b8a89b;
  outline-offset: 2px;
}

.event-date {
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: 24px;
  background: #f5f1ed;
}

.event-date span {
  color: #6f655d;
  font-size: 0.9rem;
  font-weight: 700;
}

.event-date strong {
  margin-top: 5px;
  font-size: 1.15rem;
}

.event-main {
  min-width: 0;
  padding: 22px 0;
}

.event-title-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}

.event-number {
  margin: 0 0 4px;
  color: #81756c;
  font-size: 0.78rem;
  font-weight: 800;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.event-title-row h3 {
  margin: 0;
  font-size: 1.3rem;
}

.status {
  flex: 0 0 auto;
  border-radius: 999px;
  padding: 6px 10px;
  background: #ece8e4;
  font-size: 0.75rem;
  font-weight: 800;
}

.status.confirmed {
  background: #e3efe5;
  color: #315f3b;
}

.status.in-preparation {
  background: #fff0cd;
  color: #735814;
}

.status.completed {
  background: #e4e9f0;
  color: #455467;
}

.status.cancelled {
  background: #f4dddd;
  color: #7c3636;
}

.customer {
  display: flex;
  flex-wrap: wrap;
  gap: 8px 14px;
  margin-top: 13px;
}

.customer span {
  color: #766c64;
}

.event-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 10px 20px;
  margin-top: 14px;
  color: #6e655e;
  font-size: 0.9rem;
}

.services {
  display: flex;
  flex-wrap: wrap;
  gap: 7px;
  margin-top: 14px;
}

.services span {
  border-radius: 999px;
  padding: 5px 9px;
  background: #f2eeeb;
  color: #655b54;
  font-size: 0.78rem;
  font-weight: 700;
}

.event-arrow {
  display: flex;
  align-items: center;
  justify-content: center;
  padding-right: 20px;
  color: #857970;
  font-size: 1.35rem;
}
.event-calendar {
  overflow: hidden;
  border: 1px solid #e0d9d3;
  border-radius: 18px;
  background: #fff;
}

.calendar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  padding: 22px 24px;
  border-bottom: 1px solid #e5ded8;
}

.calendar-header h2 {
  margin: 0;
  font-size: 1.5rem;
}

.calendar-controls {
  display: flex;
  gap: 7px;
}

.calendar-controls button {
  min-height: 38px;
  border: 1px solid #d9d1ca;
  border-radius: 9px;
  padding: 8px 13px;
  background: #fff;
  color: #332d29;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}

.calendar-controls button:hover {
  background: #f5f1ed;
}

.calendar-grid {
  display: grid;
  grid-template-columns: repeat(7, minmax(0, 1fr));
}

.calendar-weekdays {
  border-bottom: 1px solid #e5ded8;
  background: #f6f2ef;
}

.calendar-weekdays div {
  padding: 10px;
  color: #746960;
  font-size: 0.76rem;
  font-weight: 800;
  text-align: center;
  text-transform: uppercase;
}

.calendar-day {
  min-width: 0;
  min-height: 145px;
  padding: 9px;
  border-right: 1px solid #ebe5e0;
  border-bottom: 1px solid #ebe5e0;
  background: #fff;
}

.calendar-day:nth-child(7n) {
  border-right: 0;
}

.calendar-day.muted {
  background: #faf8f6;
}

.calendar-day.muted .calendar-day-header {
  opacity: 0.35;
}

.calendar-day-header {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 7px;
}

.calendar-day-header span {
  display: grid;
  place-items: center;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  font-size: 0.82rem;
  font-weight: 800;
}

.calendar-day.today .calendar-day-header span {
  background: #29231f;
  color: #fff;
}

.calendar-events {
  display: grid;
  gap: 6px;
}

.calendar-event {
  width: 100%;
  overflow: hidden;
  border: 0;
  border-left: 3px solid #55775c;
  border-radius: 7px;
  padding: 7px 8px;
  background: #e8f0e9;
  color: #273e2c;
  text-align: left;
  cursor: pointer;
}

.calendar-event:hover {
  background: #dce9de;
}

.calendar-event strong,
.calendar-event span,
.calendar-event small {
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.calendar-event strong {
  font-size: 0.72rem;
}

.calendar-event span {
  margin-top: 2px;
  font-size: 0.78rem;
  font-weight: 800;
}

.calendar-event small {
  margin-top: 3px;
  opacity: 0.75;
  font-size: 0.67rem;
}

@media (max-width: 760px) {
  .admin-events {
    width: min(100% - 28px, 1120px);
    padding-top: 30px;
  }

  .events-header {
    flex-direction: column;
  }

  .event-count {
    min-width: 0;
    width: 100%;
  }

  .event-card {
    grid-template-columns: 1fr;
    gap: 0;
  }

  .event-date {
    padding: 16px 20px;
  }

  .event-main {
    padding: 20px;
  }

  .event-arrow {
    display: none;
  }

  .event-title-row {
    gap: 10px;
  }
}
.calendar-header {
  align-items: flex-start;
  flex-direction: column;
}

.calendar-controls {
  width: 100%;
}

.calendar-grid {
  min-width: 760px;
}

.event-calendar {
  overflow-x: auto;
}
</style>