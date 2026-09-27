<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'

interface QuoteItem {
  description: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

interface PublicQuote {
  quoteNumber: string
  status: string
  createdAt: string
  expiresAt: string | null
  customerMessage: string | null
  subtotal: number
  tax: number
  total: number

  customer: {
    firstName: string
    lastName: string
    company: string | null
  }

  event: {
    eventType: string
    eventDate: string
    eventTime: string
    guestCount: number
  }

  items: QuoteItem[]
}

const route = useRoute()

const quote = ref<PublicQuote | null>(null)
const loading = ref(true)
const accepting = ref(false)

const errorMessage = ref('')
const successMessage = ref('')

const token = computed(() =>
  String(route.params.token ?? ''),
)

const isAccepted = computed(() =>
  quote.value?.status === 'Accepted',
)

function formatCurrency(value: number) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(value)
}

function formatExpirationDate(value: string | null) {
  if (!value) return '—'

  /*
   * The API stores the expiration as UTC.
   *
   * We intentionally display the UTC calendar date here
   * because this represents a business expiration DATE,
   * not a local appointment time.
   *
   * This prevents:
   * September 30 UTC -> September 29 Eastern
   */
  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return value
  }

  return new Intl.DateTimeFormat('en-US', {
    month: 'long',
    day: 'numeric',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(date)
}

function formatEventDate(value: string) {
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
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  }).format(
    new Date(year, month - 1, day),
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

  const date = new Date()

  date.setHours(
    hours,
    minutes,
    0,
    0,
  )

  return new Intl.DateTimeFormat('en-US', {
    hour: 'numeric',
    minute: '2-digit',
  }).format(date)
}

async function loadQuote() {
  loading.value = true
  errorMessage.value = ''

  try {
    const response = await fetch(
      `http://localhost:5128/api/public/quotes/${encodeURIComponent(token.value)}`,
    )

    if (!response.ok) {
      if (response.status === 404) {
        throw new Error(
          'This quote could not be found or is no longer available.',
        )
      }

      throw new Error(
        'We could not load this quote.',
      )
    }

    quote.value = await response.json()
  } catch (error) {
    console.error(error)

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'We could not load this quote.'
  } finally {
    loading.value = false
  }
}

async function acceptQuote() {
  if (!quote.value) {
    return
  }

  if (quote.value.status === 'Accepted') {
    return
  }

  const confirmed = window.confirm(
    `Accept ${quote.value.quoteNumber} for ${formatCurrency(quote.value.total)}?`,
  )

  if (!confirmed) {
    return
  }

  accepting.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const response = await fetch(
      `http://localhost:5128/api/public/quotes/${encodeURIComponent(token.value)}/accept`,
      {
        method: 'POST',
      },
    )

    if (!response.ok) {
      let message =
        'We could not accept this quote.'

      try {
        const data = await response.json()

        if (data.message) {
          message = data.message
        }
      } catch {
        // Keep the default message.
      }

      throw new Error(message)
    }

    await loadQuote()

    successMessage.value =
      'Your quote has been accepted. Thank you!'
  } catch (error) {
    console.error(error)

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'We could not accept this quote.'
  } finally {
    accepting.value = false
  }
}

onMounted(loadQuote)
</script>

<template>
  <main class="quote-page">
    <div class="quote-shell">
      <div
        v-if="loading"
        class="state-card"
      >
        <p>Loading your quote...</p>
      </div>

      <div
        v-else-if="errorMessage && !quote"
        class="state-card error-card"
      >
        <h1>Quote unavailable</h1>

        <p>
          {{ errorMessage }}
        </p>
      </div>

      <template v-else-if="quote">
        <header class="quote-header">
          <div>
            <p class="eyebrow">
              The Reister's Daughter
            </p>

            <h1>
              Your Quote
            </h1>

            <p class="quote-number">
              {{ quote.quoteNumber }}
            </p>
          </div>

          <div
            class="status-badge"
            :class="{
              accepted: isAccepted,
            }"
          >
            {{ quote.status }}
          </div>
        </header>

        <div
          v-if="successMessage"
          class="message success-message"
        >
          {{ successMessage }}
        </div>

        <div
          v-if="errorMessage"
          class="message error-message"
        >
          {{ errorMessage }}
        </div>

        <section class="intro-card">
          <h2>
            Hi {{ quote.customer.firstName }},
          </h2>

          <p v-if="quote.customerMessage">
            {{ quote.customerMessage }}
          </p>

          <p v-else>
            Thank you for considering
            The Reister's Daughter for your event.
            Please review your quote below.
          </p>
        </section>

        <section class="details-grid">
          <div class="detail-card">
            <span>
              Event
            </span>

            <strong>
              {{ quote.event.eventType }}
            </strong>
          </div>

          <div class="detail-card">
            <span>
              Date
            </span>

            <strong>
              {{
                formatEventDate(
                  quote.event.eventDate,
                )
              }}
            </strong>
          </div>

          <div class="detail-card">
            <span>
              Time
            </span>

            <strong>
              {{
                formatTime(
                  quote.event.eventTime,
                )
              }}
            </strong>
          </div>

          <div class="detail-card">
            <span>
              Guests
            </span>

            <strong>
              {{ quote.event.guestCount }}
            </strong>
          </div>
        </section>

        <section class="quote-card">
          <div class="section-heading">
            <div>
              <p class="eyebrow">
                Estimate
              </p>

              <h2>
                Quote Details
              </h2>
            </div>

            <p
              v-if="quote.expiresAt"
              class="expiration"
            >
              Valid through
              {{
                formatExpirationDate(
                  quote.expiresAt,
                )
              }}
            </p>
          </div>

          <div class="items">
            <div
              v-for="item in quote.items"
              :key="`${item.description}-${item.unitPrice}`"
              class="item-row"
            >
              <div class="item-description">
                <strong>
                  {{ item.description }}
                </strong>

                <span>
                  {{ item.quantity }}
                  ×
                  {{
                    formatCurrency(
                      item.unitPrice,
                    )
                  }}
                </span>
              </div>

              <strong>
                {{
                  formatCurrency(
                    item.lineTotal,
                  )
                }}
              </strong>
            </div>
          </div>

          <div class="totals">
            <div>
              <span>
                Subtotal
              </span>

              <span>
                {{
                  formatCurrency(
                    quote.subtotal,
                  )
                }}
              </span>
            </div>

            <div>
              <span>
                Tax
              </span>

              <span>
                {{
                  formatCurrency(
                    quote.tax,
                  )
                }}
              </span>
            </div>

            <div class="grand-total">
              <span>
                Total
              </span>

              <span>
                {{
                  formatCurrency(
                    quote.total,
                  )
                }}
              </span>
            </div>
          </div>
        </section>

        <section
          class="accept-card"
          :class="{
            'accepted-card': isAccepted,
          }"
        >
          <div v-if="!isAccepted">
            <p class="eyebrow">
              Ready to move forward?
            </p>

            <h2>
              Reserve your event
            </h2>

            <p>
              Review the quote above and
              accept it when you're ready
              to continue.
            </p>
          </div>

          <div v-else>
            <p class="eyebrow">
              Quote accepted
            </p>

            <h2>
              Thank you!
            </h2>

            <p>
              Your quote has been accepted.
              We'll continue with the next
              steps for your event.
            </p>
          </div>

          <button
            v-if="!isAccepted"
            type="button"
            class="accept-button"
            :disabled="accepting"
            @click="acceptQuote"
          >
            {{
              accepting
                ? 'Accepting...'
                : 'Accept Quote'
            }}
          </button>

          <div
            v-else
            class="accepted-badge"
          >
            ✓ Accepted
          </div>
        </section>

        <footer class="quote-footer">
          <strong>
            The Reister's Daughter
          </strong>

          <p>
            Locally Owned. Community-Focused.
            Rooted in Craft.
          </p>
        </footer>
      </template>
    </div>
  </main>
</template>

<style scoped>
.quote-page {
  min-height: 100vh;
  background: #f5f1e9;
  padding: 48px 20px;
  color: #24231f;
}

.quote-shell {
  width: min(960px, 100%);
  margin: 0 auto;
}

.quote-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 24px;
  margin-bottom: 28px;
}

.quote-header h1 {
  margin: 4px 0;
  font-size: clamp(
    2.4rem,
    6vw,
    4.5rem
  );
  line-height: 1;
}

.eyebrow {
  margin: 0;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.quote-number {
  margin: 8px 0 0;
  color: #6c695f;
}

.status-badge {
  border-radius: 999px;
  background: #31593a;
  color: white;
  padding: 9px 16px;
  font-size: 0.8rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.status-badge.accepted {
  background: #23472d;
}

.message {
  margin-bottom: 20px;
  border-radius: 14px;
  padding: 16px 20px;
  font-weight: 600;
}

.success-message {
  background: #e1eee3;
  color: #23472d;
}

.error-message {
  background: #f6e3e0;
  color: #8a332a;
}

.intro-card,
.quote-card,
.accept-card,
.state-card {
  background: white;
  border-radius: 20px;
  padding: 32px;
  box-shadow:
    0 12px 35px
    rgba(0, 0, 0, 0.06);
}

.intro-card {
  margin-bottom: 20px;
}

.intro-card h2 {
  margin-top: 0;
}

.intro-card p {
  margin-bottom: 0;
  line-height: 1.7;
  color: #5f5b52;
}

.details-grid {
  display: grid;
  grid-template-columns:
    repeat(4, 1fr);
  gap: 14px;
  margin-bottom: 20px;
}

.detail-card {
  background: #e8dfd0;
  border-radius: 16px;
  padding: 20px;
}

.detail-card span {
  display: block;
  margin-bottom: 8px;
  color: #6c695f;
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.quote-card {
  margin-bottom: 20px;
}

.section-heading {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  gap: 20px;
  padding-bottom: 24px;
  border-bottom:
    1px solid #e7e2d9;
}

.section-heading h2 {
  margin: 5px 0 0;
}

.expiration {
  margin: 0;
  color: #6c695f;
  font-size: 0.9rem;
}

.items {
  padding: 10px 0;
}

.item-row {
  display: flex;
  justify-content: space-between;
  gap: 20px;
  padding: 20px 0;
  border-bottom:
    1px solid #eeeae3;
}

.item-description {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.item-description span {
  color: #777267;
  font-size: 0.9rem;
}

.totals {
  width: min(360px, 100%);
  margin: 22px 0 0 auto;
}

.totals > div {
  display: flex;
  justify-content: space-between;
  gap: 20px;
  padding: 8px 0;
}

.grand-total {
  margin-top: 8px;
  padding-top: 16px !important;
  border-top:
    2px solid #24231f;
  font-size: 1.35rem;
  font-weight: 700;
}

.accept-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 30px;
  background: #31593a;
  color: white;
}

.accepted-card {
  background: #23472d;
}

.accept-card h2 {
  margin: 5px 0 8px;
}

.accept-card p {
  margin-bottom: 0;
}

.accept-button {
  flex-shrink: 0;
  border: 0;
  border-radius: 999px;
  background: white;
  color: #31593a;
  padding: 14px 24px;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}

.accept-button:hover:not(:disabled) {
  opacity: 0.9;
}

.accept-button:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.accepted-badge {
  flex-shrink: 0;
  border-radius: 999px;
  background: white;
  color: #23472d;
  padding: 14px 24px;
  font-weight: 700;
}

.quote-footer {
  padding: 32px 10px 0;
  text-align: center;
  color: #6c695f;
}

.quote-footer p {
  margin-top: 6px;
}

.error-card {
  text-align: center;
}

@media (max-width: 720px) {
  .quote-page {
    padding: 28px 14px;
  }

  .details-grid {
    grid-template-columns:
      repeat(2, 1fr);
  }

  .quote-header,
  .section-heading,
  .accept-card {
    align-items: flex-start;
    flex-direction: column;
  }

  .intro-card,
  .quote-card,
  .accept-card,
  .state-card {
    padding: 24px;
  }

  .accept-button,
  .accepted-badge {
    width: 100%;
    box-sizing: border-box;
    text-align: center;
  }
}

@media (max-width: 420px) {
  .details-grid {
    grid-template-columns: 1fr;
  }
}
</style>