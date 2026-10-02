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

interface StandaloneEvent {
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

interface CalendarDay {
  date: Date
  dateKey: string
  dayNumber: number
  isCurrentMonth: boolean
  isToday: boolean
  bookings: Booking[]
  standaloneEvents: StandaloneEvent[]
}

const router = useRouter()

const bookings = ref<Booking[]>([])
const events = ref<StandaloneEvent[]>([])

const loading = ref(true)
const errorMessage = ref('')

const activeView = ref<'calendar' | 'list'>('calendar')
const calendarDate = ref(new Date())

const todayKey = toDateKey(new Date())

const calendarTitle = computed(() =>
  calendarDate.value.toLocaleDateString('en-US', {
    month: 'long',
    year: 'numeric',
  }),
)

const upcomingBookings = computed(() =>
  bookings.value
    .filter(
      booking =>
        booking.status !== 'Completed' &&
        booking.status !== 'Cancelled',
    )
    .sort((a, b) => {
      const aValue = `${a.eventDate} ${a.eventTime}`
      const bValue = `${b.eventDate} ${b.eventTime}`

      return aValue.localeCompare(bValue)
    }),
)

const upcomingStandaloneEvents = computed(() =>
  events.value
    .filter(
      event =>
        event.status !== 'Completed' &&
        event.status !== 'Cancelled',
    )
    .sort((a, b) => {
      const aValue = `${a.startDate} ${a.startTime}`
      const bValue = `${b.startDate} ${b.startTime}`

      return aValue.localeCompare(bValue)
    }),
)

const calendarDays = computed<CalendarDay[]>(() => {
  const year = calendarDate.value.getFullYear()
  const month = calendarDate.value.getMonth()

  const firstDayOfMonth = new Date(year, month, 1)
  const lastDayOfMonth = new Date(year, month + 1, 0)

  const firstCalendarDay = new Date(firstDayOfMonth)

  firstCalendarDay.setDate(
    firstCalendarDay.getDate() -
      firstCalendarDay.getDay(),
  )

  const lastCalendarDay = new Date(lastDayOfMonth)

  lastCalendarDay.setDate(
    lastCalendarDay.getDate() +
      (6 - lastCalendarDay.getDay()),
  )

  const days: CalendarDay[] = []

  const cursor = new Date(firstCalendarDay)

  while (cursor <= lastCalendarDay) {
    const date = new Date(cursor)
    const dateKey = toDateKey(date)

    const dayBookings = bookings.value
      .filter(
        booking =>
          booking.eventDate === dateKey &&
          booking.status !== 'Cancelled',
      )
      .sort((a, b) =>
        (a.eventTime || '').localeCompare(
          b.eventTime || '',
        ),
      )

    const dayStandaloneEvents = events.value
      .filter(
        event =>
          event.startDate === dateKey &&
          event.status !== 'Cancelled',
      )
      .sort((a, b) =>
        (a.startTime || '').localeCompare(
          b.startTime || '',
        ),
      )

    days.push({
      date,
      dateKey,
      dayNumber: date.getDate(),
      isCurrentMonth: date.getMonth() === month,
      isToday: dateKey === todayKey,
      bookings: dayBookings,
      standaloneEvents: dayStandaloneEvents,
    })

    cursor.setDate(cursor.getDate() + 1)
  }

  return days
})

function toDateKey(date: Date): string {
  const year = date.getFullYear()

  const month = String(
    date.getMonth() + 1,
  ).padStart(2, '0')

  const day = String(
    date.getDate(),
  ).padStart(2, '0')

  return `${year}-${month}-${day}`
}

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

function openBooking(id: number) {
  router.push(`/admin/bookings/${id}`)
}

function customerName(
  customer: BookingCustomer | null,
) {
  if (!customer) {
    return 'No customer'
  }

  const fullName = [
    customer.firstName,
    customer.lastName,
  ]
    .filter(Boolean)
    .join(' ')
    .trim()

  return (
    fullName ||
    customer.company ||
    customer.email ||
    'Customer'
  )
}

function formatDate(dateValue: string) {
  if (!dateValue) {
    return 'No date'
  }

  const [year, month, day] = dateValue
    .split('-')
    .map(Number)

  if (!year || !month || !day) {
    return dateValue
  }

  return new Date(
    year,
    month - 1,
    day,
  ).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

function formatEventTime(timeValue: string) {
  if (!timeValue) {
    return ''
  }

  const [hoursString, minutesString] =
    timeValue.split(':')

  const hours = Number(hoursString)
  const minutes = Number(minutesString)

  if (
    Number.isNaN(hours) ||
    Number.isNaN(minutes)
  ) {
    return timeValue
  }

  const date = new Date()

  date.setHours(hours, minutes, 0, 0)

  return date.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
  })
}

function formatMoney(value: number) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(value)
}

function statusClass(status: string) {
  return `status-${status
    .toLowerCase()
    .replace(/\s+/g, '-')}`
}
function openStandaloneEvent(id: number) {
  router.push(`/admin/events/${id}`)
}

function createEvent() {
  router.push('/admin/events/new')
}
async function loadEvents() {
  loading.value = true
  errorMessage.value = ''

  try {
    const [bookingsResponse, eventsResponse] =
      await Promise.all([
        fetch(
          'http://localhost:5128/api/bookings',
        ),
        fetch(
          'http://localhost:5128/api/events',
        ),
      ])

    if (!bookingsResponse.ok) {
      throw new Error(
        `Unable to load bookings (${bookingsResponse.status}).`,
      )
    }

    if (!eventsResponse.ok) {
      throw new Error(
        `Unable to load events (${eventsResponse.status}).`,
      )
    }

    bookings.value =
      await bookingsResponse.json()

    events.value =
      await eventsResponse.json()
  } catch (error) {
    console.error(error)

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Unable to load events.'
  } finally {
    loading.value = false
  }
}

onMounted(loadEvents)
</script>

<template>
  <main class="admin-events">
    <section class="events-shell">
      <header class="page-header">
        <div>
          <p class="eyebrow">
            Admin
          </p>

          <h1>Events</h1>

          <p class="page-description">
            Everything happening at The Reister's
            Daughter.
          </p>
        </div>
<div class="header-actions">
  <button
    type="button"
    class="create-event-button"
    @click="createEvent"
  >
    + Create Event
  </button>

  <div class="view-switcher">
    <button
      type="button"
      :class="{
        active: activeView === 'calendar',
      }"
      @click="activeView = 'calendar'"
    >
      Calendar
    </button>

    <button
      type="button"
      :class="{
        active: activeView === 'list',
      }"
      @click="activeView = 'list'"
    >
      List
    </button>
  </div>
</div>
      </header>

      <div
        v-if="loading"
        class="state-card"
      >
        Loading events...
      </div>

      <div
        v-else-if="errorMessage"
        class="state-card error-card"
      >
        <p>{{ errorMessage }}</p>

        <button
          type="button"
          @click="loadEvents"
        >
          Try Again
        </button>
      </div>

      <template v-else>
        <!-- =========================================
             CALENDAR VIEW
        ========================================== -->

        <section
          v-if="activeView === 'calendar'"
          class="calendar-section"
        >
          <div class="calendar-toolbar">
            <div class="calendar-navigation">
              <button
                type="button"
                class="today-button"
                @click="goToToday"
              >
                Today
              </button>

              <button
                type="button"
                class="month-button"
                aria-label="Previous month"
                @click="previousMonth"
              >
                ‹
              </button>

              <button
                type="button"
                class="month-button"
                aria-label="Next month"
                @click="nextMonth"
              >
                ›
              </button>
            </div>

            <h2>
              {{ calendarTitle }}
            </h2>
          </div>

          <div class="calendar-scroll">
            <div class="calendar">
              <div class="weekday">
                Sun
              </div>

              <div class="weekday">
                Mon
              </div>

              <div class="weekday">
                Tue
              </div>

              <div class="weekday">
                Wed
              </div>

              <div class="weekday">
                Thu
              </div>

              <div class="weekday">
                Fri
              </div>

              <div class="weekday">
                Sat
              </div>

              <div
                v-for="day in calendarDays"
                :key="day.dateKey"
                class="calendar-day"
                :class="{
                  'outside-month':
                    !day.isCurrentMonth,
                  today: day.isToday,
                }"
              >
                <div class="day-header">
                  <span
                    class="day-number"
                    :class="{
                      'today-number':
                        day.isToday,
                    }"
                  >
                    {{ day.dayNumber }}
                  </span>
                </div>

                <!-- Customer bookings -->

                <button
                  v-for="booking in day.bookings"
                  :key="`booking-${booking.id}`"
                  type="button"
                  class="calendar-event booking-event"
                  @click="openBooking(booking.id)"
                >
                  <strong>
                    {{
                      formatEventTime(
                        booking.eventTime,
                      )
                    }}
                  </strong>

                  <span>
                    {{ booking.eventType }}
                  </span>

                  <small>
                    {{
                      customerName(
                        booking.customer,
                      )
                    }}
                    ·
                    {{ booking.guestCount }}
                    {{
                      booking.guestCount === 1
                        ? 'guest'
                        : 'guests'
                    }}
                  </small>
                </button>

                <!-- Standalone shop/public events -->

                <button
                  v-for="event in day.standaloneEvents"
                  :key="`event-${event.id}`"
                  type="button"
                  class="calendar-event standalone-event"
                  @click="openStandaloneEvent(event.id)"
                >
                  <strong>
                    {{
                      formatEventTime(
                        event.startTime,
                      )
                    }}
                  </strong>

                  <span>
                    {{ event.title }}
                  </span>

                  <small>
                    {{ event.type }}

                    <template
                      v-if="event.capacity"
                    >
                      ·
                      {{ event.capacity }}
                      capacity
                    </template>
                  </small>
                </button>
              </div>
            </div>
          </div>
        </section>

        <!-- =========================================
             LIST VIEW
        ========================================== -->

        <section
          v-else
          class="list-section"
        >
          <div class="list-group">
            <div class="section-heading">
              <div>
                <p class="section-eyebrow">
                  Customer
                </p>

                <h2>
                  Booked Events
                </h2>
              </div>

              <span class="count-badge">
                {{ upcomingBookings.length }}
              </span>
            </div>

            <div
              v-if="
                upcomingBookings.length === 0
              "
              class="state-card"
            >
              No upcoming customer bookings.
            </div>

            <div
              v-else
              class="event-list"
            >
              <article
                v-for="booking in upcomingBookings"
                :key="booking.id"
                class="event-card clickable"
                @click="openBooking(booking.id)"
              >
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
                      class="status-badge"
                      :class="
                        statusClass(
                          booking.status,
                        )
                      "
                    >
                      {{ booking.status }}
                    </span>
                  </div>

                  <div class="event-meta">
                    <span>
                      {{
                        formatDate(
                          booking.eventDate,
                        )
                      }}
                    </span>

                    <span>
                      {{
                        formatEventTime(
                          booking.eventTime,
                        )
                      }}
                    </span>

                    <span>
                      {{ booking.guestCount }}
                      {{
                        booking.guestCount === 1
                          ? 'guest'
                          : 'guests'
                      }}
                    </span>

                    <span>
                      {{
                        formatMoney(
                          booking.total,
                        )
                      }}
                    </span>
                  </div>

                  <div class="customer-block">
                    <strong>
                      {{
                        customerName(
                          booking.customer,
                        )
                      }}
                    </strong>

                    <span
                      v-if="
                        booking.customer
                          ?.company
                      "
                    >
                      {{
                        booking.customer
                          .company
                      }}
                    </span>
                  </div>

                  <div class="event-footer">
                    <span
                      v-if="
                        booking.quote
                          ?.quoteNumber
                      "
                      class="quote-number"
                    >
                      {{
                        booking.quote
                          .quoteNumber
                      }}
                    </span>

                    <div
                      v-if="
                        booking.inquiry
                          ?.services
                          ?.length
                      "
                      class="service-list"
                    >
                      <span
                        v-for="service in booking
                          .inquiry.services"
                        :key="service"
                        class="service-chip"
                      >
                        {{ service }}
                      </span>
                    </div>
                  </div>
                </div>
              </article>
            </div>
          </div>

          <div class="list-group">
            <div class="section-heading">
              <div>
                <p class="section-eyebrow">
                  Shop
                </p>

                <h2>
                  Standalone Events
                </h2>
              </div>

              <span class="count-badge">
                {{
                  upcomingStandaloneEvents.length
                }}
              </span>
            </div>

            <div
              v-if="
                upcomingStandaloneEvents.length ===
                0
              "
              class="state-card"
            >
              No standalone events.
            </div>

            <div
              v-else
              class="event-list"
            >
              <article
                v-for="event in upcomingStandaloneEvents"
                :key="event.id"
                class="event-card standalone-card clickable"
                @click="openStandaloneEvent(event.id)"
              >
                <div class="event-main">
                  <div class="event-title-row">
                    <div>
                      <p class="event-number">
                        Event #{{ event.id }}
                      </p>

                      <h3>
                        {{ event.title }}
                      </h3>
                    </div>

                    <span
                      class="status-badge"
                      :class="
                        statusClass(
                          event.status,
                        )
                      "
                    >
                      {{ event.status }}
                    </span>
                  </div>

                  <div class="event-meta">
                    <span>
                      {{
                        formatDate(
                          event.startDate,
                        )
                      }}
                    </span>

                    <span>
                      {{
                        formatEventTime(
                          event.startTime,
                        )
                      }}
                    </span>

                    <span>
                      {{ event.type }}
                    </span>

                    <span
                      v-if="event.capacity"
                    >
                      Capacity:
                      {{ event.capacity }}
                    </span>
                  </div>

                  <p
                    v-if="event.description"
                    class="event-description"
                  >
                    {{ event.description }}
                  </p>

                  <div class="event-footer">
                    <div class="service-list">
                      <span
                        v-if="event.isPublic"
                        class="service-chip public-chip"
                      >
                        Public
                      </span>

                      <span
                        v-else
                        class="service-chip"
                      >
                        Internal
                      </span>

                      <span
                        v-if="event.location"
                        class="service-chip"
                      >
                        {{ event.location }}
                      </span>
                    </div>
                  </div>
                </div>
              </article>
            </div>
          </div>
        </section>
      </template>
    </section>
  </main>
</template>

<style scoped>
.admin-events {
  min-height: 100vh;
  background: #f7f5f1;
  padding: 2rem 1rem 4rem;
}

.events-shell {
  width: min(1400px, 100%);
  margin: 0 auto;
}

.page-header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 2rem;
  margin-bottom: 2rem;
}

.eyebrow,
.section-eyebrow {
  margin: 0 0 0.35rem;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: #806b58;
}

.page-header h1 {
  margin: 0;
  font-size: clamp(2rem, 4vw, 3rem);
  color: #28231f;
}

.page-description {
  margin: 0.5rem 0 0;
  color: #6b625b;
}

.view-switcher {
  display: inline-flex;
  padding: 0.25rem;
  border: 1px solid #ddd5cd;
  border-radius: 0.75rem;
  background: #ffffff;
}

.view-switcher button {
  border: 0;
  border-radius: 0.55rem;
  background: transparent;
  padding: 0.65rem 1rem;
  color: #665e57;
  font: inherit;
  font-weight: 600;
  cursor: pointer;
}

.view-switcher button.active {
  background: #302a25;
  color: #ffffff;
}

.state-card {
  padding: 2rem;
  border: 1px solid #e2dcd5;
  border-radius: 1rem;
  background: #ffffff;
  color: #655d56;
}

.error-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.error-card p {
  margin: 0;
}

.error-card button {
  border: 0;
  border-radius: 0.6rem;
  padding: 0.65rem 1rem;
  background: #302a25;
  color: #ffffff;
  cursor: pointer;
}

/* =========================================
   CALENDAR
========================================= */

.calendar-section {
  border: 1px solid #e0d9d2;
  border-radius: 1rem;
  background: #ffffff;
  overflow: hidden;
}

.calendar-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1rem 1.25rem;
  border-bottom: 1px solid #e5dfd8;
}

.calendar-toolbar h2 {
  margin: 0;
  font-size: 1.25rem;
  color: #302a25;
}

.calendar-navigation {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.calendar-navigation button {
  font: inherit;
  cursor: pointer;
}

.today-button {
  border: 1px solid #d9d1c8;
  border-radius: 0.55rem;
  background: #ffffff;
  padding: 0.5rem 0.8rem;
  color: #433b35;
}

.month-button {
  width: 2.25rem;
  height: 2.25rem;
  border: 1px solid #d9d1c8;
  border-radius: 0.55rem;
  background: #ffffff;
  color: #433b35;
  font-size: 1.35rem !important;
}

.calendar-scroll {
  overflow-x: auto;
}

.calendar {
  display: grid;
  grid-template-columns: repeat(7, minmax(145px, 1fr));
  min-width: 1015px;
}

.weekday {
  padding: 0.7rem;
  border-right: 1px solid #e8e2dc;
  border-bottom: 1px solid #e8e2dc;
  background: #faf8f5;
  text-align: center;
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: #7c7269;
}

.calendar-day {
  min-height: 145px;
  padding: 0.5rem;
  border-right: 1px solid #ebe5df;
  border-bottom: 1px solid #ebe5df;
  background: #ffffff;
}

.calendar-day.outside-month {
  background: #faf9f7;
}

.calendar-day.outside-month
  .day-number {
  color: #b5ada6;
}

.calendar-day.today {
  background: #fffdf8;
}

.day-header {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 0.45rem;
}

.day-number {
  display: inline-flex;
  width: 1.8rem;
  height: 1.8rem;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  font-size: 0.8rem;
  font-weight: 700;
  color: #625950;
}

.today-number {
  background: #302a25;
  color: #ffffff;
}

.calendar-event {
  display: flex;
  width: 100%;
  flex-direction: column;
  gap: 0.1rem;
  margin-bottom: 0.4rem;
  border: 0;
  border-left: 3px solid #5f7566;
  border-radius: 0.4rem;
  background: #edf2ee;
  padding: 0.45rem 0.5rem;
  text-align: left;
  color: #354238;
  font: inherit;
  cursor: pointer;
}

.calendar-event:hover {
  background: #e4ebe6;
}

.calendar-event strong {
  font-size: 0.7rem;
}

.calendar-event span {
  overflow: hidden;
  font-size: 0.78rem;
  font-weight: 700;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.calendar-event small {
  overflow: hidden;
  font-size: 0.68rem;
  opacity: 0.8;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.calendar-event.standalone-event {
  border-left-color: #8b6845;
  background: #f4ece2;
  color: #5a4029;
}

.calendar-event.standalone-event:hover {
  background: #ecdfd0;
}

/* =========================================
   LIST VIEW
========================================= */

.list-section {
  display: grid;
  gap: 2rem;
}

.list-group {
  display: grid;
  gap: 1rem;
}

.section-heading {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 1rem;
}

.section-heading h2 {
  margin: 0;
  font-size: 1.4rem;
  color: #302a25;
}

.count-badge {
  display: inline-flex;
  min-width: 2rem;
  height: 2rem;
  align-items: center;
  justify-content: center;
  border-radius: 999px;
  background: #e9e4dd;
  padding: 0 0.7rem;
  font-size: 0.8rem;
  font-weight: 700;
  color: #544b43;
}

.event-list {
  display: grid;
  gap: 1rem;
}

.event-card {
  border: 1px solid #e0d9d2;
  border-radius: 1rem;
  background: #ffffff;
  padding: 1.25rem;
}

.event-card.clickable {
  cursor: pointer;
  transition:
    transform 0.15s ease,
    box-shadow 0.15s ease;
}

.event-card.clickable:hover {
  transform: translateY(-1px);
  box-shadow:
    0 8px 22px
    rgba(46, 38, 31, 0.08);
}

.standalone-card {
  border-left: 4px solid #8b6845;
}

.event-main {
  display: grid;
  gap: 1rem;
}

.event-title-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
}

.event-number {
  margin: 0 0 0.25rem;
  font-size: 0.75rem;
  font-weight: 700;
  color: #8a8179;
}

.event-title-row h3 {
  margin: 0;
  color: #302a25;
}

.status-badge {
  display: inline-flex;
  border-radius: 999px;
  padding: 0.35rem 0.65rem;
  background: #ece8e3;
  font-size: 0.72rem;
  font-weight: 700;
  color: #5d554e;
}

.status-confirmed,
.status-published,
.status-accepted {
  background: #e3eee5;
  color: #3d6344;
}

.status-in-preparation {
  background: #fff0d7;
  color: #805b20;
}

.status-draft {
  background: #ece8e3;
  color: #655c54;
}

.status-completed {
  background: #e4ebf2;
  color: #445c73;
}

.status-cancelled {
  background: #f5e3e1;
  color: #864943;
}

.event-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 0.6rem 1.25rem;
  color: #6a6159;
  font-size: 0.9rem;
}

.customer-block {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.customer-block strong {
  color: #302a25;
}

.customer-block span {
  color: #756c64;
  font-size: 0.88rem;
}

.event-description {
  margin: 0;
  color: #655d56;
  line-height: 1.6;
}

.event-footer {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding-top: 0.9rem;
  border-top: 1px solid #eee9e4;
}

.quote-number {
  font-size: 0.8rem;
  font-weight: 700;
  color: #756b62;
}

.service-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}

.service-chip {
  display: inline-flex;
  align-items: center;
  border-radius: 999px;
  background: #f0ece7;
  padding: 0.35rem 0.65rem;
  font-size: 0.72rem;
  font-weight: 600;
  color: #655b52;
}

.public-chip {
  background: #e7efe7;
  color: #426148;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.create-event-button {
  border: 1px solid #302a25;
  border-radius: 0.65rem;
  background: #302a25;
  padding: 0.7rem 1rem;
  color: #ffffff;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}

.create-event-button:hover {
  opacity: 0.9;
}

@media (max-width: 760px) {
  .admin-events {
    padding: 1.25rem 0.75rem 3rem;
  }

  .page-header {
    align-items: flex-start;
    flex-direction: column;
  }

  .view-switcher {
    width: 100%;
  }

  .view-switcher button {
    flex: 1;
  }

  .calendar-toolbar {
    align-items: flex-start;
    flex-direction: column-reverse;
  }

  .event-title-row,
  .event-footer {
    align-items: flex-start;
    flex-direction: column;
  }

  .error-card {
    align-items: flex-start;
    flex-direction: column;
  }
  .header-actions {
  width: 100%;
  align-items: stretch;
  flex-direction: column;
}

.create-event-button {
  width: 100%;
}
}
</style>