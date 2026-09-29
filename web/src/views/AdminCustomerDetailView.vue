<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

interface Quote {
  id: number
  quoteNumber: string
  status: string
  createdAt: string
  total: number
}

interface Inquiry {
  id: number
  createdAt: string
  status: string
  eventType: string
  eventDate: string
  eventTime: string
  guestCount: number
  services: string[]
  details: string
  quotes: Quote[]
}

interface Booking {
  id: number
  status: string
  eventType: string
  eventDate: string
  eventTime: string
  guestCount: number
  total: number
  quoteId: number
  quoteNumber: string
  inquiryId: number
}

interface Customer {
  id: number
  createdAt: string
  firstName: string
  lastName: string
  company: string | null
  email: string
  phone: string
  inquiries: Inquiry[]
  bookings: Booking[]
}

interface Communication {
  id: number
  inquiryId: number | null
  type: string
  direction: string
  subject: string
  body: string
  fromAddress: string
  toAddress: string
  status: string
  createdAt: string
  sentAt: string | null
}

const route = useRoute()
const router = useRouter()

const customer = ref<Customer | null>(null)
const loading = ref(true)
const error = ref('')

const communications = ref<Communication[]>([])
const communicationLoading = ref(false)
const communicationError = ref('')

const showComposer = ref(false)
const savingCommunication = ref(false)

const messageSubject = ref('')
const messageBody = ref('')
const messageInquiryId = ref<number | null>(null)

const customerId = computed(() => Number(route.params.id))

const quoteCount = computed(() =>
  customer.value?.inquiries.reduce(
    (total, inquiry) => total + inquiry.quotes.length,
    0,
  ) ?? 0,
)

function customerName() {
  if (!customer.value) return ''

  return `${customer.value.firstName} ${customer.value.lastName}`.trim()
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
  }).format(
    new Date(
      year,
      month - 1,
      day,
    ),
  )
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
  }).format(
    new Date(2000, 0, 1, hours, minutes),
  )
}

function formatDateTime(value: string) {
  if (!value) return '—'

  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  }).format(new Date(value))
}

async function loadCustomer() {
  loading.value = true
  error.value = ''

  try {
    const response = await fetch(
      `http://localhost:5128/api/customers/${customerId.value}`,
    )

    if (!response.ok) {
      throw new Error(
        `Unable to load customer (${response.status}).`,
      )
    }

    customer.value = await response.json()
  } catch (err) {
    error.value =
      err instanceof Error
        ? err.message
        : 'Unable to load customer.'
  } finally {
    loading.value = false
  }
}

async function loadCommunications() {
  communicationLoading.value = true
  communicationError.value = ''

  try {
    const response = await fetch(
      `http://localhost:5128/api/customers/${customerId.value}/communications`,
    )

    if (!response.ok) {
      throw new Error(
        `Unable to load communications (${response.status}).`,
      )
    }

    communications.value = await response.json()
  } catch (err) {
    communicationError.value =
      err instanceof Error
        ? err.message
        : 'Unable to load communications.'
  } finally {
    communicationLoading.value = false
  }
}

function openComposer() {
  if (!customer.value) return

  messageSubject.value = ''
  messageBody.value = ''
  messageInquiryId.value = null
  communicationError.value = ''
  showComposer.value = true
}

function closeComposer() {
  showComposer.value = false
  communicationError.value = ''
}

async function saveDraft() {
  if (!customer.value) return

  if (!messageBody.value.trim()) {
    communicationError.value = 'Message body is required.'
    return
  }

  savingCommunication.value = true
  communicationError.value = ''

  try {
    const response = await fetch(
      'http://localhost:5128/api/communications',
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          customerId: customer.value.id,
          inquiryId: messageInquiryId.value,
          type: 'Email',
          direction: 'Outbound',
          subject: messageSubject.value,
          body: messageBody.value,
          fromAddress: 'hello@thereistersdaughter.com',
          toAddress: customer.value.email,
        }),
      },
    )

    if (!response.ok) {
      const result = await response
        .json()
        .catch(() => null)

      throw new Error(
        result?.message ??
          `Unable to save draft (${response.status}).`,
      )
    }

    showComposer.value = false

    messageSubject.value = ''
    messageBody.value = ''
    messageInquiryId.value = null

    await loadCommunications()
  } catch (err) {
    communicationError.value =
      err instanceof Error
        ? err.message
        : 'Unable to save draft.'
  } finally {
    savingCommunication.value = false
  }
}

onMounted(async () => {
  await Promise.all([
    loadCustomer(),
    loadCommunications(),
  ])
})
</script>

<template>
  <main class="customer-detail">
    <button
      class="back-button"
      type="button"
      @click="router.push('/admin/customers')"
    >
      ← Customers
    </button>

    <section
      v-if="loading"
      class="state-card"
    >
      Loading customer...
    </section>

    <section
      v-else-if="error"
      class="state-card"
    >
      <strong>Unable to load customer</strong>
      <p>{{ error }}</p>
    </section>

    <template v-else-if="customer">
      <!-- ===================================================
           CUSTOMER HEADER
           =================================================== -->

      <section class="customer-header">
        <div>
          <p class="eyebrow">
            CUSTOMER #{{ customer.id }}
          </p>

          <h1>{{ customerName() }}</h1>

          <p
            v-if="customer.company"
            class="company"
          >
            {{ customer.company }}
          </p>
        </div>

        <div class="summary-stats">
          <div>
            <strong>
              {{ customer.inquiries.length }}
            </strong>
            <span>Inquiries</span>
          </div>

          <div>
            <strong>{{ quoteCount }}</strong>
            <span>Quotes</span>
          </div>

          <div>
            <strong>
              {{ customer.bookings.length }}
            </strong>
            <span>Bookings</span>
          </div>
        </div>
      </section>

      <!-- ===================================================
           CONTACT
           =================================================== -->

      <section class="contact-card">
        <div>
          <span>Email</span>

          <a :href="`mailto:${customer.email}`">
            {{ customer.email }}
          </a>
        </div>

        <div>
          <span>Phone</span>

          <a :href="`tel:${customer.phone}`">
            {{ customer.phone || '—' }}
          </a>
        </div>
      </section>

      <!-- ===================================================
           CUSTOMER HISTORY
           =================================================== -->

      <section class="section">
        <div class="section-heading">
          <div>
            <p class="eyebrow">CRM</p>
            <h2>Customer History</h2>
          </div>
        </div>

        <div
          v-if="
            customer.inquiries.length === 0 &&
            customer.bookings.length === 0
          "
          class="state-card"
        >
          No customer history yet.
        </div>

        <div
          v-for="inquiry in customer.inquiries"
          :key="inquiry.id"
          class="history-group"
        >
          <article
            class="history-card inquiry-card"
            role="link"
            tabindex="0"
            @click="
              router.push(
                `/admin/inquiries/${inquiry.id}`,
              )
            "
            @keydown.enter="
              router.push(
                `/admin/inquiries/${inquiry.id}`,
              )
            "
          >
            <div class="history-top">
              <div>
                <p class="history-label">
                  Inquiry #{{ inquiry.id }}
                </p>

                <h3>{{ inquiry.eventType }}</h3>
              </div>

              <span class="status">
                {{ inquiry.status }}
              </span>
            </div>

            <div class="history-meta">
              <span>
                {{ formatDate(inquiry.eventDate) }}
              </span>

              <span>
                {{ formatTime(inquiry.eventTime) }}
              </span>

              <span>
                {{ inquiry.guestCount }} guests
              </span>
            </div>

            <div
              v-if="inquiry.services.length"
              class="tags"
            >
              <span
                v-for="service in inquiry.services"
                :key="service"
              >
                {{ service }}
              </span>
            </div>

            <p
              v-if="inquiry.details"
              class="details"
            >
              {{ inquiry.details }}
            </p>
          </article>

          <div
            v-for="quote in inquiry.quotes"
            :key="quote.id"
            class="history-child"
          >
            <div class="connector" />

            <article
              class="history-card quote-card"
              role="link"
              tabindex="0"
              @click="
                router.push(
                  `/admin/quotes/${quote.id}`,
                )
              "
              @keydown.enter="
                router.push(
                  `/admin/quotes/${quote.id}`,
                )
              "
            >
              <div>
                <p class="history-label">
                  Quote
                </p>

                <h3>{{ quote.quoteNumber }}</h3>
              </div>

              <div class="quote-summary">
                <span
                  :class="[
                    'status',
                    quote.status.toLowerCase(),
                  ]"
                >
                  {{ quote.status }}
                </span>

                <strong>
                  {{ formatMoney(quote.total) }}
                </strong>
              </div>
            </article>

            <template
              v-for="booking in customer.bookings.filter(
                item => item.quoteId === quote.id,
              )"
              :key="booking.id"
            >
              <div class="booking-child">
                <div class="connector" />

                <article
                  class="history-card booking-card"
                  role="link"
                  tabindex="0"
                  @click="
                    router.push(
                      `/admin/bookings/${booking.id}`,
                    )
                  "
                  @keydown.enter="
                    router.push(
                      `/admin/bookings/${booking.id}`,
                    )
                  "
                >
                  <div>
                    <p class="history-label">
                      Booking #{{ booking.id }}
                    </p>

                    <h3>
                      {{ booking.eventType }}
                    </h3>

                    <p class="booking-date">
                      {{
                        formatDate(
                          booking.eventDate,
                        )
                      }}
                      ·
                      {{
                        formatTime(
                          booking.eventTime,
                        )
                      }}
                    </p>
                  </div>

                  <div class="quote-summary">
                    <span
                      :class="[
                        'status',
                        booking.status
                          .toLowerCase()
                          .replaceAll(' ', '-'),
                      ]"
                    >
                      {{ booking.status }}
                    </span>

                    <strong>
                      {{
                        formatMoney(
                          booking.total,
                        )
                      }}
                    </strong>
                  </div>
                </article>
              </div>
            </template>
          </div>
        </div>
      </section>

      <!-- ===================================================
           COMMUNICATIONS
           =================================================== -->

      <section class="section">
        <div
          class="section-heading communication-heading"
        >
          <div>
            <p class="eyebrow">CRM</p>
            <h2>Communications</h2>
          </div>

          <button
            class="primary-button"
            type="button"
            @click="openComposer"
          >
            Compose Message
          </button>
        </div>

        <!-- COMPOSER -->

        <div
          v-if="showComposer"
          class="composer-card"
        >
          <div class="composer-header">
            <div>
              <p class="history-label">
                New Message
              </p>

              <h3>
                Email {{ customerName() }}
              </h3>
            </div>

            <button
              class="close-button"
              type="button"
              @click="closeComposer"
            >
              Cancel
            </button>
          </div>

          <div class="form-grid">
            <label>
              <span>To</span>

              <input
                :value="customer.email"
                type="email"
                disabled
              />
            </label>

            <label>
              <span>Related Inquiry</span>

              <select
                v-model="messageInquiryId"
              >
                <option :value="null">
                  General customer message
                </option>

                <option
                  v-for="inquiry in customer.inquiries"
                  :key="inquiry.id"
                  :value="inquiry.id"
                >
                  Inquiry #{{ inquiry.id }} —
                  {{ inquiry.eventType }}
                </option>
              </select>
            </label>
          </div>

          <label class="composer-field">
            <span>Subject</span>

            <input
              v-model="messageSubject"
              type="text"
              placeholder="Email subject"
            />
          </label>

          <label class="composer-field">
            <span>Message</span>

            <textarea
              v-model="messageBody"
              rows="7"
              placeholder="Write your message..."
            />
          </label>

          <div class="composer-actions">
            <span>
              This saves to the CRM. Email
              delivery will be added next.
            </span>

            <button
              class="primary-button"
              type="button"
              :disabled="savingCommunication"
              @click="saveDraft"
            >
              {{
                savingCommunication
                  ? 'Saving...'
                  : 'Save Draft'
              }}
            </button>
          </div>
        </div>

        <!-- ERROR -->

        <div
          v-if="communicationError"
          class="communication-error"
        >
          {{ communicationError }}
        </div>

        <!-- LOADING -->

        <div
          v-if="communicationLoading"
          class="state-card"
        >
          Loading communications...
        </div>

        <!-- EMPTY -->

        <div
          v-else-if="
            communications.length === 0
          "
          class="state-card"
        >
          No communications yet.
        </div>

        <!-- COMMUNICATION HISTORY -->

        <div
          v-else
          class="communication-list"
        >
          <article
            v-for="communication in communications"
            :key="communication.id"
            class="communication-card"
          >
            <div class="communication-top">
              <div>
                <div
                  class="communication-labels"
                >
                  <span class="status">
                    {{ communication.type }}
                  </span>

                  <span class="status">
                    {{
                      communication.direction
                    }}
                  </span>

                  <span
                    :class="[
                      'status',
                      communication.status.toLowerCase(),
                    ]"
                  >
                    {{
                      communication.status
                    }}
                  </span>
                </div>

                <h3>
                  {{
                    communication.subject ||
                    '(No subject)'
                  }}
                </h3>
              </div>

              <time>
                {{
                  formatDateTime(
                    communication.sentAt ??
                      communication.createdAt,
                  )
                }}
              </time>
            </div>

            <p class="communication-body">
              {{ communication.body }}
            </p>

            <div class="communication-footer">
              <span>
                {{
                  communication.fromAddress
                }}
                →
                {{
                  communication.toAddress
                }}
              </span>

              <button
                v-if="
                  communication.inquiryId
                "
                type="button"
                class="text-button"
                @click="
                  router.push(
                    `/admin/inquiries/${communication.inquiryId}`,
                  )
                "
              >
                Inquiry #{{
                  communication.inquiryId
                }}
              </button>
            </div>
          </article>
        </div>
      </section>
    </template>
  </main>
</template>

<style scoped>
.customer-detail {
  width: min(1180px, calc(100% - 40px));
  margin: 0 auto;
  padding: 48px 0 100px;
}

.back-button {
  margin-bottom: 36px;
  padding: 0;
  border: 0;
  background: transparent;
  font: inherit;
  cursor: pointer;
  opacity: 0.65;
}

.back-button:hover {
  opacity: 1;
}

.customer-header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 40px;
  padding-bottom: 32px;
  border-bottom: 1px solid
    rgba(0, 0, 0, 0.12);
}

.eyebrow,
.history-label {
  margin: 0 0 8px;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  opacity: 0.55;
}

h1 {
  margin: 0;
  font-size: clamp(2.7rem, 6vw, 4.7rem);
  line-height: 1;
}

.company {
  margin: 12px 0 0;
  font-size: 1.15rem;
  opacity: 0.65;
}

.summary-stats {
  display: grid;
  grid-template-columns: repeat(3, 100px);
  gap: 10px;
}

.summary-stats > div {
  padding: 16px 10px;
  border-radius: 14px;
  background: rgba(0, 0, 0, 0.04);
  text-align: center;
}

.summary-stats strong {
  display: block;
  font-size: 1.5rem;
}

.summary-stats span {
  font-size: 0.75rem;
  opacity: 0.6;
}

.contact-card {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 24px;
  margin-top: 24px;
  padding: 24px 28px;
  border: 1px solid
    rgba(0, 0, 0, 0.12);
  border-radius: 16px;
  background: #fff;
}

.contact-card div {
  display: grid;
  gap: 5px;
}

.contact-card span {
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  opacity: 0.5;
}

.section {
  margin-top: 64px;
}

.section-heading {
  margin-bottom: 22px;
}

.section-heading h2 {
  margin: 0;
  font-size: 2rem;
}

.history-group {
  margin-bottom: 30px;
}

.history-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
  padding: 24px 28px;
  border: 1px solid
    rgba(0, 0, 0, 0.12);
  border-radius: 16px;
  background: #fff;
  cursor: pointer;
  transition:
    transform 0.15s ease,
    box-shadow 0.15s ease;
}

.history-card:hover {
  transform: translateY(-2px);
  box-shadow:
    0 8px 24px rgba(0, 0, 0, 0.06);
}

.history-card h3 {
  margin: 0;
  font-size: 1.25rem;
}

.history-top {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  width: 100%;
  gap: 24px;
}

.inquiry-card {
  display: block;
}

.history-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 18px;
  margin-top: 18px;
  font-size: 0.9rem;
  opacity: 0.7;
}

.tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 16px;
}

.tags span {
  padding: 7px 10px;
  border-radius: 999px;
  background: rgba(0, 0, 0, 0.05);
  font-size: 0.8rem;
}

.details {
  margin: 18px 0 0;
  line-height: 1.6;
  opacity: 0.75;
}

.history-child,
.booking-child {
  position: relative;
  margin-left: 42px;
  padding-top: 18px;
}

.booking-child {
  margin-left: 42px;
}

.connector {
  position: absolute;
  top: 0;
  left: 24px;
  width: 1px;
  height: 18px;
  background: rgba(0, 0, 0, 0.2);
}

.quote-card,
.booking-card {
  padding: 20px 24px;
}

.quote-summary {
  display: flex;
  align-items: center;
  gap: 20px;
}

.status {
  display: inline-flex;
  padding: 6px 10px;
  border-radius: 999px;
  background: rgba(0, 0, 0, 0.06);
  font-size: 0.75rem;
  font-weight: 700;
}

.status.accepted,
.status.confirmed,
.status.completed,
.status.sent {
  background: rgba(40, 130, 70, 0.12);
}

.status.in-preparation {
  background: rgba(180, 130, 30, 0.12);
}

.status.cancelled {
  background: rgba(170, 40, 40, 0.1);
}

.booking-date {
  margin: 7px 0 0;
  font-size: 0.85rem;
  opacity: 0.65;
}

.state-card {
  padding: 40px;
  border: 1px solid
    rgba(0, 0, 0, 0.12);
  border-radius: 16px;
  background: #fff;
  text-align: center;
}

/* =========================================================
   COMMUNICATIONS
   ========================================================= */

.communication-heading {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 24px;
}

.primary-button {
  padding: 11px 17px;
  border: 0;
  border-radius: 10px;
  background: #111;
  color: #fff;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}

.primary-button:hover {
  opacity: 0.85;
}

.primary-button:disabled {
  cursor: default;
  opacity: 0.5;
}

.composer-card {
  margin-bottom: 24px;
  padding: 28px;
  border: 1px solid
    rgba(0, 0, 0, 0.12);
  border-radius: 16px;
  background: #fff;
}

.composer-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 24px;
  margin-bottom: 24px;
}

.composer-header h3 {
  margin: 0;
  font-size: 1.4rem;
}

.close-button,
.text-button {
  padding: 0;
  border: 0;
  background: transparent;
  color: inherit;
  font: inherit;
  cursor: pointer;
  text-decoration: underline;
}

.form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 18px;
}

.form-grid label,
.composer-field {
  display: grid;
  gap: 7px;
}

.form-grid label > span,
.composer-field > span {
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  opacity: 0.55;
}

.form-grid input,
.form-grid select,
.composer-field input,
.composer-field textarea {
  width: 100%;
  box-sizing: border-box;
  padding: 12px 14px;
  border: 1px solid
    rgba(0, 0, 0, 0.16);
  border-radius: 10px;
  background: #fff;
  color: inherit;
  font: inherit;
}

.form-grid input:disabled {
  background: rgba(0, 0, 0, 0.035);
  opacity: 0.75;
}

.composer-field {
  margin-top: 18px;
}

.composer-field textarea {
  resize: vertical;
  line-height: 1.5;
}

.composer-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
  margin-top: 20px;
}

.composer-actions > span {
  font-size: 0.8rem;
  opacity: 0.55;
}

.communication-error {
  margin-bottom: 18px;
  padding: 14px 16px;
  border-radius: 10px;
  background: rgba(170, 40, 40, 0.08);
}

.communication-list {
  display: grid;
  gap: 16px;
}

.communication-card {
  padding: 24px 28px;
  border: 1px solid
    rgba(0, 0, 0, 0.12);
  border-radius: 16px;
  background: #fff;
}

.communication-top {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 24px;
}

.communication-top h3 {
  margin: 12px 0 0;
  font-size: 1.15rem;
}

.communication-top time {
  white-space: nowrap;
  font-size: 0.8rem;
  opacity: 0.55;
}

.communication-labels {
  display: flex;
  flex-wrap: wrap;
  gap: 7px;
}

.status.draft {
  background: rgba(180, 130, 30, 0.12);
}

.status.failed {
  background: rgba(170, 40, 40, 0.1);
}

.communication-body {
  margin: 18px 0;
  line-height: 1.6;
  white-space: pre-wrap;
}

.communication-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  padding-top: 16px;
  border-top: 1px solid
    rgba(0, 0, 0, 0.08);
  font-size: 0.82rem;
  opacity: 0.7;
}

@media (max-width: 760px) {
  .customer-detail {
    width: min(100% - 28px, 1180px);
  }

  .customer-header {
    align-items: flex-start;
    flex-direction: column;
  }

  .summary-stats {
    width: 100%;
    grid-template-columns:
      repeat(3, 1fr);
  }

  .contact-card {
    grid-template-columns: 1fr;
  }

  .history-card {
    align-items: flex-start;
    flex-direction: column;
  }

  .history-top {
    align-items: flex-start;
  }

  .quote-summary {
    width: 100%;
    justify-content: space-between;
  }

  .history-child,
  .booking-child {
    margin-left: 18px;
  }

  .communication-heading,
  .communication-top,
  .composer-header,
  .composer-actions,
  .communication-footer {
    align-items: flex-start;
    flex-direction: column;
  }

  .form-grid {
    grid-template-columns: 1fr;
  }

  .communication-top time {
    white-space: normal;
  }
}
</style>