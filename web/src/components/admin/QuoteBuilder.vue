<script setup lang="ts">
import { computed, ref } from 'vue'

interface QuoteItemForm {
  description: string
  quantity: number
  unitPrice: number
}

interface CreatedQuote {
  id: number
  inquiryId: number
  quoteNumber: string
  status: string
  subtotal: number
  tax: number
  total: number
}

const props = defineProps<{
  inquiryId: number
}>()

const emit = defineEmits<{
  saved: []
}>()

const items = ref<QuoteItemForm[]>([
  {
    description: '',
    quantity: 1,
    unitPrice: 0,
  },
])

const customerMessage = ref('')
const expiresAt = ref('')
const tax = ref(0)

const saving = ref(false)
const errorMessage = ref('')
const createdQuote = ref<CreatedQuote | null>(null)

const subtotal = computed(() => {
  return items.value.reduce((total, item) => {
    const quantity = Number(item.quantity) || 0
    const unitPrice = Number(item.unitPrice) || 0

    return total + quantity * unitPrice
  }, 0)
})

const total = computed(() => {
  return subtotal.value + (Number(tax.value) || 0)
})

function formatMoney(value: number) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(value)
}

function lineTotal(item: QuoteItemForm) {
  return (
    (Number(item.quantity) || 0) *
    (Number(item.unitPrice) || 0)
  )
}

function addItem() {
  items.value.push({
    description: '',
    quantity: 1,
    unitPrice: 0,
  })
}

function removeItem(index: number) {
  if (items.value.length === 1) {
    items.value[0] = {
      description: '',
      quantity: 1,
      unitPrice: 0,
    }

    return
  }

  items.value.splice(index, 1)
}

function resetForm() {
  items.value = [
    {
      description: '',
      quantity: 1,
      unitPrice: 0,
    },
  ]

  customerMessage.value = ''
  expiresAt.value = ''
  tax.value = 0
}

async function saveQuote() {
  errorMessage.value = ''
  createdQuote.value = null

  const cleanedItems = items.value.map((item) => ({
    description: item.description.trim(),
    quantity: Number(item.quantity),
    unitPrice: Number(item.unitPrice),
  }))

  if (
    cleanedItems.some(
      (item) => !item.description,
    )
  ) {
    errorMessage.value =
      'Every quote item needs a description.'

    return
  }

  if (
    cleanedItems.some(
      (item) => item.quantity <= 0,
    )
  ) {
    errorMessage.value =
      'Quantities must be greater than zero.'

    return
  }

  if (
    cleanedItems.some(
      (item) => item.unitPrice < 0,
    )
  ) {
    errorMessage.value =
      'Prices cannot be negative.'

    return
  }

  if (Number(tax.value) < 0) {
    errorMessage.value =
      'Tax cannot be negative.'

    return
  }

  saving.value = true

  try {
    const response = await fetch(
      `http://localhost:5128/api/inquiries/${props.inquiryId}/quotes`,
      {
        method: 'POST',

        headers: {
          'Content-Type': 'application/json',
        },

        body: JSON.stringify({
          expiresAt: expiresAt.value
            ? new Date(
                `${expiresAt.value}T23:59:59`,
              ).toISOString()
            : null,

          customerMessage:
            customerMessage.value.trim() || null,

          tax: Number(tax.value) || 0,

          items: cleanedItems,
        }),
      },
    )

    const data = await response.json()

    if (!response.ok) {
      throw new Error(
        data.message || 'Could not save quote.',
      )
    }

    createdQuote.value = data

    resetForm()

    emit('saved')
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
</script>

<template>
  <section class="quote-builder">

    <div class="quote-heading">
      <div>
        <p class="section-label">
          Quote Builder
        </p>

        <h2>Create Draft Quote</h2>

        <p class="helper-text">
          Build a quote for this inquiry.
          Totals are verified by the server when saved.
        </p>
      </div>

      <span class="draft-badge">
        Draft
      </span>
    </div>


    <!-- ITEMS -->

    <div class="items">

      <div class="item-header">
        <span>Description</span>
        <span>Qty</span>
        <span>Unit Price</span>
        <span>Total</span>
        <span></span>
      </div>

      <div
        v-for="(item, index) in items"
        :key="index"
        class="item-row"
      >
        <input
          v-model="item.description"
          type="text"
          placeholder="Coffee Traveler"
          aria-label="Item description"
        />

        <input
  v-model.number="item.quantity"
  type="number"
  min="1"
  step="1"
  aria-label="Quantity"
/>

        <div class="money-input">
          <span>$</span>

          <input
            v-model.number="item.unitPrice"
            type="number"
            min="0"
            step="0.01"
            aria-label="Unit price"
          />
        </div>

        <strong class="line-total">
          {{ formatMoney(lineTotal(item)) }}
        </strong>

        <button
          class="remove-button"
          type="button"
          aria-label="Remove item"
          @click="removeItem(index)"
        >
          ×
        </button>
      </div>

    </div>


    <button
      type="button"
      class="add-item-button"
      @click="addItem"
    >
      + Add Item
    </button>


    <!-- OPTIONS -->

    <div class="quote-options">

      <div class="message-field">
        <label for="quote-message">
          Customer Message
        </label>

        <textarea
          id="quote-message"
          v-model="customerMessage"
          rows="5"
          placeholder="Thanks for considering The Reister's Daughter for your event..."
        ></textarea>
      </div>

      <div class="quote-settings">

        <label>
          Quote Expires

          <input
            v-model="expiresAt"
            type="date"
          />
        </label>

        <label>
          Tax

          <div class="money-input tax-input">
            <span>$</span>

            <input
              v-model.number="tax"
              type="number"
              min="0"
              step="0.01"
            />
          </div>
        </label>

      </div>

    </div>


    <!-- TOTALS -->

    <div class="totals">

      <div>
        <span>Subtotal</span>

        <strong>
          {{ formatMoney(subtotal) }}
        </strong>
      </div>

      <div>
        <span>Tax</span>

        <strong>
          {{ formatMoney(Number(tax) || 0) }}
        </strong>
      </div>

      <div class="grand-total">
        <span>Total</span>

        <strong>
          {{ formatMoney(total) }}
        </strong>
      </div>

    </div>


    <!-- FEEDBACK -->

    <p
      v-if="errorMessage"
      class="error-message"
    >
      {{ errorMessage }}
    </p>

    <div
      v-if="createdQuote"
      class="success-message"
    >
      <strong>
        {{ createdQuote.quoteNumber }}
        saved successfully.
      </strong>

      <span>
        {{ formatMoney(createdQuote.total) }}
        · {{ createdQuote.status }}
      </span>
    </div>


    <!-- SAVE -->

    <div class="save-area">

      <button
        type="button"
        class="save-button"
        :disabled="saving"
        @click="saveQuote"
      >
        {{
          saving
            ? 'Saving Quote...'
            : 'Save Draft Quote'
        }}
      </button>

    </div>

  </section>
</template>

<style scoped>
.quote-builder {
  margin-top: 32px;
  padding: 34px;
  border: 1px solid #d8d3c9;
  border-radius: 18px;
  background: #fff;
}

.quote-heading {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 24px;
  margin-bottom: 30px;
}

.section-label {
  margin: 0 0 8px;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: #817c73;
}

.quote-heading h2 {
  margin: 0;
  font-size: 2rem;
  letter-spacing: -0.035em;
}

.helper-text {
  margin: 10px 0 0;
  color: #777269;
  line-height: 1.5;
}

.draft-badge {
  padding: 8px 13px;
  border-radius: 999px;
  background: #f5edd8;
  color: #72591c;
  font-size: 0.75rem;
  font-weight: 800;
  text-transform: uppercase;
}


/* ITEMS */

.items {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.item-header,
.item-row {
  display: grid;
  grid-template-columns:
    minmax(220px, 1fr)
    90px
    130px
    110px
    38px;
  gap: 12px;
  align-items: center;
}

.item-header {
  padding: 0 3px;
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: #817c73;
}

.item-row input,
.quote-options input,
.quote-options textarea {
  width: 100%;
  border: 1px solid #d8d3c9;
  border-radius: 9px;
  background: #faf9f6;
  color: #22221f;
  font: inherit;
}

.item-row > input {
  height: 44px;
  padding: 0 12px;
}

.money-input {
  display: flex;
  align-items: center;
  height: 44px;
  border: 1px solid #d8d3c9;
  border-radius: 9px;
  background: #faf9f6;
  overflow: hidden;
}

.money-input span {
  padding-left: 12px;
  color: #817c73;
}

.money-input input {
  min-width: 0;
  height: 42px;
  padding: 0 10px 0 5px;
  border: 0;
  background: transparent;
}

.item-row input:focus,
.quote-options input:focus,
.quote-options textarea:focus,
.money-input:focus-within {
  outline: 2px solid #22221f;
  outline-offset: 2px;
}

.money-input input:focus {
  outline: none;
}

.line-total {
  text-align: right;
  font-size: 0.9rem;
}

.remove-button {
  width: 34px;
  height: 34px;
  border: 0;
  border-radius: 50%;
  background: #efede7;
  color: #6d6961;
  font-size: 1.2rem;
  cursor: pointer;
}

.remove-button:hover {
  background: #e2ded6;
}

.add-item-button {
  margin-top: 14px;
  padding: 9px 0;
  border: 0;
  background: transparent;
  color: #44413c;
  font: inherit;
  font-size: 0.86rem;
  font-weight: 800;
  cursor: pointer;
}

.add-item-button:hover {
  text-decoration: underline;
}


/* OPTIONS */

.quote-options {
  display: grid;
  grid-template-columns: 1fr 260px;
  gap: 30px;
  margin-top: 34px;
  padding-top: 30px;
  border-top: 1px solid #e5e1d9;
}

.message-field label,
.quote-settings label {
  display: block;
  margin-bottom: 8px;
  font-size: 0.75rem;
  font-weight: 800;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: #6d6961;
}

.message-field textarea {
  resize: vertical;
  padding: 13px 14px;
  line-height: 1.5;
}

.quote-settings {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.quote-settings > label > input {
  height: 44px;
  margin-top: 8px;
  padding: 0 12px;
}

.tax-input {
  margin-top: 8px;
}


/* TOTALS */

.totals {
  width: min(340px, 100%);
  margin: 32px 0 0 auto;
}

.totals > div {
  display: flex;
  justify-content: space-between;
  gap: 30px;
  padding: 7px 0;
}

.totals span {
  color: #777269;
}

.grand-total {
  margin-top: 7px;
  padding-top: 15px !important;
  border-top: 1px solid #d8d3c9;
  font-size: 1.25rem;
}

.grand-total span {
  color: #22221f;
}


/* FEEDBACK */

.error-message {
  margin: 25px 0 0;
  padding: 13px 16px;
  border-radius: 9px;
  background: #f8e8e6;
  color: #7b2c25;
}

.success-message {
  display: flex;
  justify-content: space-between;
  gap: 20px;
  margin-top: 25px;
  padding: 16px;
  border-radius: 10px;
  background: #e8f1e9;
  color: #31593a;
}

.save-area {
  display: flex;
  justify-content: flex-end;
  margin-top: 25px;
}

.save-button {
  padding: 13px 23px;
  border: 0;
  border-radius: 999px;
  background: #22221f;
  color: white;
  font: inherit;
  font-weight: 800;
  cursor: pointer;
}

.save-button:hover:not(:disabled) {
  opacity: 0.86;
}

.save-button:disabled {
  cursor: wait;
  opacity: 0.55;
}


/* RESPONSIVE */

@media (max-width: 850px) {
  .item-header {
    display: none;
  }

  .item-row {
    grid-template-columns: 1fr 80px;
    padding: 16px;
    border-radius: 12px;
    background: #f5f3ee;
  }

  .item-row > input:first-child {
    grid-column: 1 / -1;
  }

  .money-input {
    grid-column: 1;
  }

  .line-total {
    grid-column: 2;
  }

  .remove-button {
    grid-column: 2;
    grid-row: 1;
    justify-self: end;
  }

  .quote-options {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 550px) {
  .quote-builder {
    padding: 22px;
  }

  .quote-heading {
    flex-direction: column;
  }

  .success-message {
    flex-direction: column;
  }
}
</style>