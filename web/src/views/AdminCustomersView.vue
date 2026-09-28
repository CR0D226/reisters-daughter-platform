<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

interface Customer {
  id: number
  createdAt: string
  firstName: string
  lastName: string
  company: string | null
  email: string
  phone: string
  inquiryCount: number
  quoteCount: number
  bookingCount: number
}

const router = useRouter()

const customers = ref<Customer[]>([])
const loading = ref(true)
const error = ref('')
const search = ref('')

const filteredCustomers = computed(() => {
  const query = search.value.trim().toLowerCase()

  if (!query) {
    return customers.value
  }

  return customers.value.filter(customer => {
    const searchable = [
      customer.firstName,
      customer.lastName,
      customer.company ?? '',
      customer.email,
      customer.phone,
    ]
      .join(' ')
      .toLowerCase()

    return searchable.includes(query)
  })
})

function customerName(customer: Customer) {
  return `${customer.firstName} ${customer.lastName}`.trim()
}

function openCustomer(id: number) {
  router.push(`/admin/customers/${id}`)
}

async function loadCustomers() {
  loading.value = true
  error.value = ''

  try {
    const response = await fetch(
      'http://localhost:5128/api/customers',
    )

    if (!response.ok) {
      throw new Error(
        `Unable to load customers (${response.status}).`,
      )
    }

    customers.value = await response.json()
  } catch (err) {
    error.value =
      err instanceof Error
        ? err.message
        : 'Unable to load customers.'
  } finally {
    loading.value = false
  }
}

onMounted(loadCustomers)
</script>

<template>
  <main class="admin-customers">
    <section class="customers-header">
      <div>
        <p class="eyebrow">
          ADMIN
        </p>

        <h1>Customers</h1>

        <p class="subtitle">
          Customer relationships, inquiries, quotes and bookings.
        </p>
      </div>

      <div class="customer-count">
        <strong>{{ customers.length }}</strong>
        <span>Customers</span>
      </div>
    </section>

    <section class="search-section">
      <input
        v-model="search"
        type="search"
        placeholder="Search name, company, email or phone..."
        aria-label="Search customers"
      >
    </section>

    <section
      v-if="loading"
      class="state-card"
    >
      Loading customers...
    </section>

    <section
      v-else-if="error"
      class="state-card error"
    >
      <strong>Unable to load customers</strong>
      <p>{{ error }}</p>

      <button
        type="button"
        @click="loadCustomers"
      >
        Try Again
      </button>
    </section>

    <section
      v-else-if="customers.length === 0"
      class="state-card"
    >
      <h2>No customers yet</h2>
      <p>
        Customers will appear here when inquiries are submitted.
      </p>
    </section>

    <section
      v-else-if="filteredCustomers.length === 0"
      class="state-card"
    >
      <h2>No matching customers</h2>
      <p>
        Try a different name, company, email or phone number.
      </p>
    </section>

    <section
      v-else
      class="customer-list"
    >
      <article
        v-for="customer in filteredCustomers"
        :key="customer.id"
        class="customer-card"
        role="link"
        tabindex="0"
        @click="openCustomer(customer.id)"
        @keydown.enter="openCustomer(customer.id)"
        @keydown.space.prevent="openCustomer(customer.id)"
      >
        <div class="customer-main">
          <p class="customer-number">
            Customer #{{ customer.id }}
          </p>

          <h2>
            {{ customerName(customer) }}
          </h2>

          <p
            v-if="customer.company"
            class="company"
          >
            {{ customer.company }}
          </p>

          <div class="contact">
            <a
              :href="`mailto:${customer.email}`"
              @click.stop
            >
              {{ customer.email }}
            </a>

            <a
              v-if="customer.phone"
              :href="`tel:${customer.phone}`"
              @click.stop
            >
              {{ customer.phone }}
            </a>
          </div>
        </div>

        <div class="customer-stats">
          <div>
            <strong>{{ customer.inquiryCount }}</strong>
            <span>
              {{ customer.inquiryCount === 1 ? 'Inquiry' : 'Inquiries' }}
            </span>
          </div>

          <div>
            <strong>{{ customer.quoteCount }}</strong>
            <span>
              {{ customer.quoteCount === 1 ? 'Quote' : 'Quotes' }}
            </span>
          </div>

          <div>
            <strong>{{ customer.bookingCount }}</strong>
            <span>
              {{ customer.bookingCount === 1 ? 'Booking' : 'Bookings' }}
            </span>
          </div>
        </div>
      </article>
    </section>
  </main>
</template>

<style scoped>
.admin-customers {
  width: min(1180px, calc(100% - 40px));
  margin: 0 auto;
  padding: 64px 0 100px;
}

.customers-header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 32px;
  margin-bottom: 32px;
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

.customer-count {
  min-width: 120px;
  padding: 18px 24px;
  border: 1px solid rgba(0, 0, 0, 0.12);
  border-radius: 16px;
  text-align: center;
}

.customer-count strong {
  display: block;
  font-size: 2rem;
}

.customer-count span {
  font-size: 0.8rem;
  opacity: 0.65;
}

.search-section {
  margin-bottom: 24px;
}

.search-section input {
  width: 100%;
  padding: 16px 18px;
  border: 1px solid rgba(0, 0, 0, 0.14);
  border-radius: 12px;
  background: #fff;
  font: inherit;
  font-size: 1rem;
}

.search-section input:focus {
  outline: 2px solid #1d1d1d;
  outline-offset: 2px;
}

.customer-list {
  display: grid;
  gap: 16px;
}

.customer-card {
  display: grid;
  grid-template-columns: 1fr auto;
  align-items: center;
  gap: 32px;
  padding: 28px 32px;
  border: 1px solid rgba(0, 0, 0, 0.12);
  border-radius: 18px;
  background: #fff;
  cursor: pointer;
  transition:
    transform 0.15s ease,
    box-shadow 0.15s ease,
    border-color 0.15s ease;
}

.customer-card:hover {
  transform: translateY(-2px);
  border-color: rgba(0, 0, 0, 0.2);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.06);
}

.customer-card:focus-visible {
  outline: 2px solid #1d1d1d;
  outline-offset: 3px;
}

.customer-number {
  margin: 0 0 5px;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  opacity: 0.5;
}

.customer-main h2 {
  margin: 0;
  font-size: 1.5rem;
}

.company {
  margin: 6px 0 0;
  opacity: 0.65;
}

.contact {
  display: flex;
  flex-wrap: wrap;
  gap: 18px;
  margin-top: 16px;
  font-size: 0.9rem;
}

.customer-stats {
  display: grid;
  grid-template-columns: repeat(3, 100px);
  gap: 10px;
}

.customer-stats > div {
  padding: 14px 12px;
  border-radius: 12px;
  background: rgba(0, 0, 0, 0.035);
  text-align: center;
}

.customer-stats strong {
  display: block;
  font-size: 1.35rem;
}

.customer-stats span {
  display: block;
  margin-top: 3px;
  font-size: 0.72rem;
  opacity: 0.6;
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
  .admin-customers {
    width: min(100% - 28px, 1180px);
    padding-top: 40px;
  }

  .customers-header {
    align-items: flex-start;
  }

  .customer-count {
    min-width: 90px;
  }

  .customer-card {
    grid-template-columns: 1fr;
  }

  .customer-stats {
    grid-template-columns: repeat(3, 1fr);
  }
}
</style>