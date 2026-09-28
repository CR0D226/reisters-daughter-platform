<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

interface CustomerSummary {
  id?: number | null
  customerId?: number | null
  firstName: string
  lastName: string
  company: string | null
}

interface RecentInquiry {
  id: number
  createdAt: string
  status: string
  eventType: string
  eventDate: string
  eventTime: string
  guestCount: number
  customer: CustomerSummary
}

interface UpcomingBooking {
  id: number
  status: string
  eventType: string
  eventDate: string
  eventTime: string
  guestCount: number
  total: number
  customer: CustomerSummary | null
  quoteId: number
  quoteNumber: string
  inquiryId: number
}

interface AttentionQuote {
  id: number
  quoteNumber: string
  status: string
  updatedAt: string
  expiresAt: string | null
  total: number
  inquiryId: number
  customer: CustomerSummary
}

interface DashboardData {
  generatedAt: string
  counts: {
    customers: number
    inquiries: number
    newInquiries: number
    quotes: {
      draft: number
      sent: number
      accepted: number
    }
    bookings: {
      confirmed: number
      inPreparation: number
    }
  }
  recentInquiries: RecentInquiry[]
  upcomingBookings: UpcomingBooking[]
  quotesNeedingAttention: AttentionQuote[]
}

const router = useRouter()

const dashboard = ref<DashboardData | null>(null)
const loading = ref(true)
const error = ref('')

const activeBookingCount = computed(() => {
  if (!dashboard.value) return 0

  return (
    dashboard.value.counts.bookings.confirmed +
    dashboard.value.counts.bookings.inPreparation
  )
})

function personName(customer: CustomerSummary | null) {
  if (!customer) return 'No customer linked'

  return `${customer.firstName} ${customer.lastName}`.trim()
}

function formatMoney(value: number) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(value)
}

function formatDate(value: string) {
  if (!value) return '—'

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
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(year, month - 1, day))
}

function formatTime(value: string) {
  if (!value) return '—'

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

  return new Intl.DateTimeFormat('en-US', {
    hour: 'numeric',
    minute: '2-digit',
  }).format(new Date(2000, 0, 1, hours, minutes))
}

function formatExpiration(value: string | null) {
  if (!value) return 'No expiration'

  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(value))
}

async function loadDashboard() {
  loading.value = true
  error.value = ''

  try {
    const response = await fetch(
      'http://localhost:5128/api/admin/dashboard',
    )

    if (!response.ok) {
      throw new Error(
        `Unable to load dashboard (${response.status}).`,
      )
    }

    dashboard.value = await response.json()
  } catch (err) {
    error.value =
      err instanceof Error
        ? err.message
        : 'Unable to load dashboard.'
  } finally {
    loading.value = false
  }
}

onMounted(loadDashboard)
</script>

<template>
  <main class="dashboard">
    <section class="dashboard-header">
      <div>
        <p class="eyebrow">ADMIN</p>
        <h1>Dashboard</h1>
        <p class="subtitle">
          A quick view of customers, inquiries, quotes and upcoming work.
        </p>
      </div>

      <button
        class="refresh-button"
        type="button"
        :disabled="loading"
        @click="loadDashboard"
      >
        Refresh
      </button>
    </section>

    <section
      v-if="loading && !dashboard"
      class="state-card"
    >
      Loading dashboard...
    </section>

    <section
      v-else-if="error && !dashboard"
      class="state-card error"
    >
      <strong>Unable to load dashboard</strong>
      <p>{{ error }}</p>

      <button
        type="button"
        @click="loadDashboard"
      >
        Try Again
      </button>
    </section>

    <template v-else-if="dashboard">
      <section class="metrics">
        <button
          class="metric-card"
          type="button"
          @click="router.push('/admin/customers')"
        >
          <span>Customers</span>
          <strong>{{ dashboard.counts.customers }}</strong>
          <small>CRM records</small>
        </button>

        <button
          class="metric-card"
          type="button"
          @click="router.push('/admin/inquiries')"
        >
          <span>Inquiries</span>
          <strong>{{ dashboard.counts.inquiries }}</strong>
          <small>
            {{ dashboard.counts.newInquiries }} new
          </small>
        </button>

        <div class="metric-card">
          <span>Quotes</span>
          <strong>
            {{
              dashboard.counts.quotes.draft +
              dashboard.counts.quotes.sent +
              dashboard.counts.quotes.accepted
            }}
          </strong>
          <small>
            {{ dashboard.counts.quotes.sent }} sent ·
            {{ dashboard.counts.quotes.accepted }} accepted
          </small>
        </div>

        <button
          class="metric-card"
          type="button"
          @click="router.push('/admin/bookings')"
        >
          <span>Active Bookings</span>
          <strong>{{ activeBookingCount }}</strong>
          <small>
            {{ dashboard.counts.bookings.inPreparation }}
            in preparation
          </small>
        </button>
      </section>

      <section class="dashboard-grid">
        <div class="dashboard-column">
          <section class="panel">
            <div class="panel-heading">
              <div>
                <p class="eyebrow">ATTENTION</p>
                <h2>Open Quotes</h2>
              </div>

              <span class="panel-count">
                {{ dashboard.quotesNeedingAttention.length }}
              </span>
            </div>

            <div
              v-if="dashboard.quotesNeedingAttention.length === 0"
              class="empty"
            >
              No open quotes need attention.
            </div>

            <article
              v-for="quote in dashboard.quotesNeedingAttention"
              :key="quote.id"
              class="list-card"
              role="link"
              tabindex="0"
              @click="router.push(`/admin/quotes/${quote.id}`)"
              @keydown.enter="
                router.push(`/admin/quotes/${quote.id}`)
              "
            >
              <div>
                <div class="list-title">
                  <strong>{{ quote.quoteNumber }}</strong>

                  <span
                    :class="[
                      'status',
                      quote.status.toLowerCase(),
                    ]"
                  >
                    {{ quote.status }}
                  </span>
                </div>

                <p>
                  {{ personName(quote.customer) }}
                  <template v-if="quote.customer.company">
                    · {{ quote.customer.company }}
                  </template>
                </p>

                <small>
                  Expires {{ formatExpiration(quote.expiresAt) }}
                </small>
              </div>

              <strong class="amount">
                {{ formatMoney(quote.total) }}
              </strong>
            </article>
          </section>

          <section class="panel">
            <div class="panel-heading">
              <div>
                <p class="eyebrow">RECENT</p>
                <h2>Inquiries</h2>
              </div>

              <button
                class="text-button"
                type="button"
                @click="router.push('/admin/inquiries')"
              >
                View all
              </button>
            </div>

            <article
              v-for="inquiry in dashboard.recentInquiries"
              :key="inquiry.id"
              class="list-card"
              role="link"
              tabindex="0"
              @click="
                router.push(`/admin/inquiries/${inquiry.id}`)
              "
              @keydown.enter="
                router.push(`/admin/inquiries/${inquiry.id}`)
              "
            >
              <div>
                <div class="list-title">
                  <strong>
                    Inquiry #{{ inquiry.id }}
                  </strong>

                  <span class="status">
                    {{ inquiry.status }}
                  </span>
                </div>

                <p>
                  {{ personName(inquiry.customer) }}
                  · {{ inquiry.eventType }}
                </p>

                <small>
                  {{ formatDate(inquiry.eventDate) }}
                  ·
                  {{ formatTime(inquiry.eventTime) }}
                  ·
                  {{ inquiry.guestCount }} guests
                </small>
              </div>
            </article>
          </section>
        </div>

        <div class="dashboard-column">
          <section class="panel">
            <div class="panel-heading">
              <div>
                <p class="eyebrow">UPCOMING</p>
                <h2>Bookings</h2>
              </div>

              <button
                class="text-button"
                type="button"
                @click="router.push('/admin/bookings')"
              >
                View all
              </button>
            </div>

            <div
              v-if="dashboard.upcomingBookings.length === 0"
              class="empty"
            >
              No upcoming bookings.
            </div>

            <article
              v-for="booking in dashboard.upcomingBookings"
              :key="booking.id"
              class="booking-card"
              role="link"
              tabindex="0"
              @click="
                router.push(`/admin/bookings/${booking.id}`)
              "
              @keydown.enter="
                router.push(`/admin/bookings/${booking.id}`)
              "
            >
              <div class="booking-date">
                <strong>
                  {{ formatDate(booking.eventDate) }}
                </strong>

                <span>
                  {{ formatTime(booking.eventTime) }}
                </span>
              </div>

              <div class="booking-info">
                <div class="list-title">
                  <strong>
                    {{ booking.eventType }}
                  </strong>

                  <span class="status confirmed">
                    {{ booking.status }}
                  </span>
                </div>

                <p>
                  {{ personName(booking.customer) }}
                </p>

                <small>
                  {{ booking.guestCount }} guests ·
                  {{ booking.quoteNumber }}
                </small>
              </div>

              <strong class="amount">
                {{ formatMoney(booking.total) }}
              </strong>
            </article>
          </section>

          <section
            v-if="dashboard.counts.newInquiries > 0"
            class="attention-panel"
          >
            <div>
              <p class="eyebrow">NEEDS ATTENTION</p>

              <h2>
                {{ dashboard.counts.newInquiries }}
                {{
                  dashboard.counts.newInquiries === 1
                    ? 'new inquiry'
                    : 'new inquiries'
                }}
              </h2>

              <p>
                New inquiries are waiting to be reviewed.
              </p>
            </div>

            <button
              type="button"
              @click="router.push('/admin/inquiries')"
            >
              Review Inquiries
            </button>
          </section>
        </div>
      </section>
    </template>
  </main>
</template>

<style scoped>
.dashboard {
  width: min(1240px, calc(100% - 40px));
  margin: 0 auto;
  padding: 64px 0 100px;
}

.dashboard-header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 32px;
  margin-bottom: 32px;
}

.eyebrow {
  margin: 0 0 8px;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.15em;
  text-transform: uppercase;
  opacity: 0.55;
}

h1 {
  margin: 0;
  font-size: clamp(2.7rem, 6vw, 4.7rem);
  line-height: 1;
}

.subtitle {
  margin: 14px 0 0;
  opacity: 0.65;
}

.refresh-button,
.attention-panel button {
  padding: 11px 18px;
  border: 1px solid #1d1d1d;
  border-radius: 999px;
  background: #1d1d1d;
  color: #fff;
  font: inherit;
  font-weight: 600;
  cursor: pointer;
}

.metrics {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 14px;
  margin-bottom: 28px;
}

.metric-card {
  display: block;
  padding: 22px;
  border: 1px solid rgba(0, 0, 0, 0.12);
  border-radius: 16px;
  background: #fff;
  color: inherit;
  font: inherit;
  text-align: left;
}

button.metric-card {
  cursor: pointer;
}

button.metric-card:hover {
  border-color: rgba(0, 0, 0, 0.3);
}

.metric-card span {
  display: block;
  font-size: 0.8rem;
  font-weight: 700;
  opacity: 0.55;
}

.metric-card strong {
  display: block;
  margin: 8px 0;
  font-size: 2rem;
}

.metric-card small {
  opacity: 0.6;
}

.dashboard-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 24px;
  align-items: start;
}

.dashboard-column {
  display: grid;
  gap: 24px;
}

.panel {
  padding: 26px;
  border: 1px solid rgba(0, 0, 0, 0.12);
  border-radius: 18px;
  background: #fff;
}

.panel-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  margin-bottom: 20px;
}

.panel-heading h2,
.attention-panel h2 {
  margin: 0;
  font-size: 1.55rem;
}

.panel-count {
  display: grid;
  width: 36px;
  height: 36px;
  place-items: center;
  border-radius: 50%;
  background: rgba(0, 0, 0, 0.05);
  font-weight: 700;
}

.text-button {
  padding: 0;
  border: 0;
  background: transparent;
  font: inherit;
  font-size: 0.85rem;
  cursor: pointer;
  opacity: 0.6;
}

.text-button:hover {
  opacity: 1;
}

.list-card,
.booking-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  padding: 18px 0;
  border-top: 1px solid rgba(0, 0, 0, 0.08);
  cursor: pointer;
}

.list-card:first-of-type {
  border-top: 0;
}

.list-card p,
.booking-card p {
  margin: 6px 0;
  font-size: 0.9rem;
}

.list-card small,
.booking-card small {
  opacity: 0.55;
}

.list-title {
  display: flex;
  align-items: center;
  gap: 10px;
}

.status {
  display: inline-flex;
  padding: 5px 9px;
  border-radius: 999px;
  background: rgba(0, 0, 0, 0.06);
  font-size: 0.7rem;
  font-weight: 700;
}

.status.sent {
  background: rgba(40, 90, 160, 0.12);
}

.status.accepted,
.status.confirmed {
  background: rgba(40, 130, 70, 0.12);
}

.amount {
  white-space: nowrap;
}

.booking-card {
  display: grid;
  grid-template-columns: 110px 1fr auto;
}

.booking-date {
  display: grid;
  gap: 4px;
}

.booking-date span {
  font-size: 0.85rem;
  opacity: 0.6;
}

.attention-panel {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
  padding: 28px;
  border: 1px solid rgba(150, 100, 20, 0.2);
  border-radius: 18px;
  background: rgba(180, 130, 40, 0.06);
}

.attention-panel p:not(.eyebrow) {
  margin: 8px 0 0;
  opacity: 0.65;
}

.empty,
.state-card {
  padding: 30px;
  border-radius: 12px;
  background: rgba(0, 0, 0, 0.025);
  text-align: center;
  opacity: 0.65;
}

.state-card {
  border: 1px solid rgba(0, 0, 0, 0.12);
  background: #fff;
}

@media (max-width: 900px) {
  .metrics {
    grid-template-columns: repeat(2, 1fr);
  }

  .dashboard-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 600px) {
  .dashboard {
    width: min(100% - 28px, 1240px);
    padding-top: 40px;
  }

  .dashboard-header {
    align-items: flex-start;
    flex-direction: column;
  }

  .metrics {
    grid-template-columns: 1fr 1fr;
  }

  .booking-card {
    grid-template-columns: 1fr;
  }

  .attention-panel {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>