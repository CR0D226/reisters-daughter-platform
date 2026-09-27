<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'

interface QuoteItem {
  id: number
  description: string
  quantity: number
  unitPrice: number
  lineTotal: number
  sortOrder: number
}

interface Quote {
  id: number
  inquiryId: number
  quoteNumber: string
  status: string
  createdAt: string
  updatedAt: string
  expiresAt: string | null
  customerMessage: string | null
  subtotal: number
  tax: number
  total: number
  items: QuoteItem[]
}

const route = useRoute()

const quote = ref<Quote | null>(null)
const loading = ref(true)
const saving = ref(false)
const sending = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const editableItems = ref<QuoteItem[]>([])
const customerMessage = ref('')
const expiresAt = ref('')
const tax = ref(0)

const subtotal = computed(() =>
  editableItems.value.reduce(
    (sum, item) =>
      sum + item.quantity * item.unitPrice,
    0,
  ),
)

const total = computed(
  () => subtotal.value + tax.value,
)

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(amount)
}

function formatDateTime(date: string) {
  return new Date(date).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  })
}

function toDateInput(date: string | null) {
  if (!date) return ''

  return new Date(date)
    .toISOString()
    .slice(0, 10)
}

async function loadQuote() {
  loading.value = true
  errorMessage.value = ''

  try {
    const response = await fetch(
      `http://localhost:5128/api/quotes/${route.params.id}`,
    )

    if (response.status === 404) {
      throw new Error('Quote not found.')
    }

    if (!response.ok) {
      throw new Error(
        `Server returned ${response.status}`,
      )
    }

    const data: Quote = await response.json()

    quote.value = data

    editableItems.value = data.items.map(
      item => ({
        ...item,
      }),
    )

    customerMessage.value =
      data.customerMessage ?? ''

    expiresAt.value =
      toDateInput(data.expiresAt)

    tax.value = data.tax
  } catch (error) {
    console.error(error)

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Could not load quote.'
  } finally {
    loading.value = false
  }
}

function addItem() {
  editableItems.value.push({
    id: 0,
    description: '',
    quantity: 1,
    unitPrice: 0,
    lineTotal: 0,
    sortOrder: editableItems.value.length,
  })
}

function removeItem(index: number) {
  if (editableItems.value.length === 1) {
    return
  }

  editableItems.value.splice(index, 1)
}

async function saveQuote() {
  if (!quote.value) return

  errorMessage.value = ''
  successMessage.value = ''

  const cleanedItems =
    editableItems.value.map(item => ({
      description: item.description.trim(),
      quantity: item.quantity,
      unitPrice: item.unitPrice,
    }))

  if (
    cleanedItems.some(
      item => !item.description,
    )
  ) {
    errorMessage.value =
      'Every item needs a description.'
    return
  }

  if (
    cleanedItems.some(
      item =>
        !Number.isInteger(item.quantity) ||
        item.quantity <= 0,
    )
  ) {
    errorMessage.value =
      'Quantities must be whole numbers greater than zero.'
    return
  }

  if (
    cleanedItems.some(
      item => item.unitPrice < 0,
    )
  ) {
    errorMessage.value =
      'Item prices cannot be negative.'
    return
  }

  if (tax.value < 0) {
    errorMessage.value =
      'Tax cannot be negative.'
    return
  }

  saving.value = true

  try {
    const response = await fetch(
      `http://localhost:5128/api/quotes/${quote.value.id}`,
      {
        method: 'PUT',

        headers: {
          'Content-Type': 'application/json',
        },

        body: JSON.stringify({
          expiresAt:
            expiresAt.value
              ? `${expiresAt.value}T23:59:59`
              : null,

          customerMessage:
            customerMessage.value.trim() || null,

          tax: tax.value,

          items: cleanedItems,
        }),
      },
    )

    if (!response.ok) {
      let message =
        'Could not save quote.'

      try {
        const data = await response.json()

        if (data.message) {
          message = data.message
        }
      } catch {
        // Keep default message.
      }

      throw new Error(message)
    }

    successMessage.value =
      'Quote saved successfully.'

    await loadQuote()

    successMessage.value =
      'Quote saved successfully.'
  } catch (error) {
    console.error(error)

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Could not save quote.'
  } finally {
    saving.value = false
  }
}
async function sendQuote() {
  if (!quote.value) return

  const confirmed = window.confirm(
    `Send ${quote.value.quoteNumber} to the customer? Once sent, this quote will be locked from editing.`,
  )

  if (!confirmed) {
    return
  }

  errorMessage.value = ''
  successMessage.value = ''
  sending.value = true

  try {
    const response = await fetch(
      `http://localhost:5128/api/quotes/${quote.value.id}/send`,
      {
        method: 'POST',
      },
    )

    if (!response.ok) {
      let message = 'Could not send quote.'

      try {
        const data = await response.json()

        if (data.message) {
          message = data.message
        }
      } catch {
        // Keep default message.
      }

      throw new Error(message)
    }

    await loadQuote()

    successMessage.value =
      'Quote marked as sent successfully.'
  } catch (error) {
    console.error(error)

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Could not send quote.'
  } finally {
    sending.value = false
  }
}
onMounted(loadQuote)
</script>

<template>
  <main class="quote-page">
    <div class="quote-container">

      <RouterLink
        v-if="quote"
        class="back-link"
        :to="`/admin/inquiries/${quote.inquiryId}`"
      >
        ← Back to Inquiry #{{ quote.inquiryId }}
      </RouterLink>

      <div
        v-if="loading"
        class="state-message"
      >
        Loading quote...
      </div>

      <div
        v-else-if="errorMessage && !quote"
        class="state-message error"
      >
        {{ errorMessage }}
      </div>

      <template v-else-if="quote">

        <header class="quote-header">
          <div>
            <p class="eyebrow">
              Quote
            </p>

            <h1>
              {{ quote.quoteNumber }}
            </h1>

            <p class="created">
              Created
              {{ formatDateTime(quote.createdAt) }}
            </p>
          </div>

          <span
            class="status"
            :class="quote.status.toLowerCase()"
          >
            {{ quote.status }}
          </span>
        </header>

        <section class="panel">
          <div class="panel-heading">
            <div>
              <p class="section-label">
                Line Items
              </p>

              <h2>Quote Items</h2>
            </div>

            <button
              type="button"
              class="secondary-button"
              @click="addItem"
            >
              + Add Item
            </button>
          </div>

          <div class="items">

            <div class="item-headings">
              <span>Description</span>
              <span>Qty</span>
              <span>Unit Price</span>
              <span>Total</span>
              <span></span>
            </div>

            <div
              v-for="(item, index) in editableItems"
              :key="`${item.id}-${index}`"
              class="item-row"
            >
              <input
                v-model="item.description"
                type="text"
                placeholder="Item description"
              />

              <input
                v-model.number="item.quantity"
                type="number"
                min="1"
                step="1"
                aria-label="Quantity"
              />

              <input
                v-model.number="item.unitPrice"
                type="number"
                min="0"
                step="0.01"
                aria-label="Unit price"
              />

              <strong>
                {{
                  formatCurrency(
                    item.quantity *
                    item.unitPrice,
                  )
                }}
              </strong>

              <button
                type="button"
                class="remove-button"
                :disabled="
                  editableItems.length === 1
                "
                @click="removeItem(index)"
              >
                Remove
              </button>
            </div>

          </div>
        </section>

        <div class="quote-grid">

          <section class="panel">
            <p class="section-label">
              Customer
            </p>

            <h2>Customer Message</h2>

            <textarea
              v-model="customerMessage"
              rows="7"
              placeholder="Message shown to the customer..."
            ></textarea>

            <label class="field">
              <span>Expiration Date</span>

              <input
                v-model="expiresAt"
                type="date"
              />
            </label>
          </section>

          <section class="panel totals-panel">
            <p class="section-label">
              Summary
            </p>

            <h2>Quote Total</h2>

            <div class="total-row">
              <span>Subtotal</span>

              <strong>
                {{ formatCurrency(subtotal) }}
              </strong>
            </div>

            <label class="total-row tax-row">
              <span>Tax</span>

              <input
                v-model.number="tax"
                type="number"
                min="0"
                step="0.01"
              />
            </label>

            <div class="grand-total">
              <span>Total</span>

              <strong>
                {{ formatCurrency(total) }}
              </strong>
            </div>
          </section>

        </div>

        <div
          v-if="errorMessage"
          class="feedback error"
        >
          {{ errorMessage }}
        </div>

        <div
          v-if="successMessage"
          class="feedback success"
        >
          {{ successMessage }}
        </div>

        <div class="actions">
          <button
            type="button"
            class="save-button"
            :disabled="
              saving ||
              quote.status !== 'Draft'
            "
            @click="saveQuote"
          >
            {{
              saving
                ? 'Saving...'
                : 'Save Changes'
            }}
          </button>

         <button
  type="button"
  class="send-button"
  :disabled="
    sending ||
    saving ||
    quote.status !== 'Draft'
  "
  @click="sendQuote"
>
  {{
    sending
      ? 'Sending...'
      : quote.status === 'Sent'
        ? 'Quote Sent'
        : 'Send Quote'
  }}
</button>
        </div>

      </template>

    </div>
  </main>
</template>

<style scoped>
.quote-page {
  min-height: 100vh;
  padding: 70px 24px 120px;
  background: #efede7;
  color: #22221f;
}

.quote-container {
  width: min(1100px, 100%);
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

.quote-header {
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

.quote-header h1 {
  margin: 0;
  font-size: clamp(3rem, 7vw, 5rem);
  line-height: 1;
  letter-spacing: -0.045em;
}

.created {
  margin: 14px 0 0;
  color: #6d6961;
}

.status {
  padding: 10px 16px;
  border-radius: 999px;
  background: #ece9e2;
  font-size: 0.8rem;
  font-weight: 800;
  text-transform: uppercase;
}

.status.draft {
  color: #625e57;
}

.status.sent {
  background: #e8edf4;
  color: #334e70;
}

.status.accepted {
  background: #e5efe7;
  color: #31593a;
}

.panel {
  margin-bottom: 18px;
  padding: 30px;
  border: 1px solid #dedad1;
  border-radius: 16px;
  background: white;
}

.panel h2 {
  margin: 0;
  font-size: 1.65rem;
}

.panel-heading {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 20px;
  margin-bottom: 26px;
}

.secondary-button,
.save-button,
.send-button {
  border: 0;
  border-radius: 999px;
  padding: 11px 20px;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}

.secondary-button {
  background: #efede7;
  color: #22221f;
}

.items {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.item-headings,
.item-row {
  display: grid;
  grid-template-columns:
    minmax(240px, 1fr)
    90px
    130px
    120px
    80px;
  gap: 12px;
  align-items: center;
}

.item-headings {
  padding: 0 8px;
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: #817c73;
}

.item-row {
  padding: 10px;
  border-radius: 10px;
  background: #f5f3ee;
}

input,
textarea {
  width: 100%;
  padding: 12px 13px;
  border: 1px solid #d8d3c9;
  border-radius: 8px;
  background: white;
  color: #22221f;
  font: inherit;
}

textarea {
  resize: vertical;
  margin-top: 18px;
  line-height: 1.5;
}

input:focus,
textarea:focus {
  outline: 2px solid #22221f;
  outline-offset: 2px;
}

.remove-button {
  border: 0;
  background: transparent;
  color: #8a3029;
  font: inherit;
  font-size: 0.76rem;
  font-weight: 700;
  cursor: pointer;
}

.remove-button:disabled {
  opacity: 0.3;
  cursor: default;
}

.quote-grid {
  display: grid;
  grid-template-columns: 1.4fr 0.8fr;
  gap: 18px;
}

.field {
  display: block;
  margin-top: 24px;
}

.field span {
  display: block;
  margin-bottom: 8px;
  font-size: 0.72rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: #817c73;
}

.totals-panel {
  align-self: start;
}

.total-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 25px;
  margin-top: 24px;
}

.tax-row input {
  width: 130px;
  text-align: right;
}

.grand-total {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 25px;
  margin-top: 20px;
  padding-top: 20px;
  border-top: 1px solid #dedad1;
}

.grand-total strong {
  font-size: 1.5rem;
}

.feedback {
  margin-top: 18px;
  padding: 15px 18px;
  border-radius: 10px;
  font-weight: 700;
}

.feedback.error {
  background: #f6e9e7;
  color: #8a3029;
}

.feedback.success {
  background: #e5efe7;
  color: #31593a;
}

.actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 30px;
}

.save-button {
  background: #22221f;
  color: white;
}

.send-button {
  background: #31593a;
  color: white;
}

.send-button:hover:not(:disabled) {
  opacity: 0.85;
}

.save-button:hover:not(:disabled) {
  opacity: 0.85;
}

.save-button:disabled,
.send-button:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.state-message {
  padding: 50px;
  border-radius: 14px;
  background: white;
  text-align: center;
}

.state-message.error {
  color: #7b2c25;
}

@media (max-width: 850px) {
  .quote-grid {
    grid-template-columns: 1fr;
  }

  .item-headings {
    display: none;
  }

  .item-row {
    grid-template-columns: 1fr 1fr;
  }

  .item-row input:first-child {
    grid-column: 1 / -1;
  }
}

@media (max-width: 600px) {
  .quote-page {
    padding-left: 16px;
    padding-right: 16px;
  }

  .quote-header {
    flex-direction: column;
  }

  .panel {
    padding: 22px;
  }

  .item-row {
    grid-template-columns: 1fr;
  }

  .item-row input:first-child {
    grid-column: auto;
  }

  .actions {
    flex-direction: column;
  }

  .save-button,
  .send-button {
    width: 100%;
  }
}
</style>