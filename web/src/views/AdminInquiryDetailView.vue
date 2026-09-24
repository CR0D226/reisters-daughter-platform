<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'

interface Inquiry {
  id: number
  createdAt: string
  status: string

  eventType: string
  services: string[]

  firstName: string
  lastName: string
  company: string | null

  email: string
  phone: string

  eventDate: string
  eventTime: string
  guestCount: number

  cateringType: string | null
  serviceType: string | null
  deliveryAddress: string | null
  packaging: string | null

  dietaryNeeds: string[]
  otherDietaryNeeds: string | null

  recurring: string | null
  details: string
}

const route = useRoute()

const inquiry = ref<Inquiry | null>(null)
const loading = ref(true)
const errorMessage = ref('')

function formatDate(date: string) {
  if (!date) return '—'

  return new Date(
    `${date}T00:00:00`,
  ).toLocaleDateString()
}

function formatCreatedAt(date: string) {
  return new Date(date).toLocaleString()
}

async function loadInquiry() {
  loading.value = true
  errorMessage.value = ''

  try {
    const response = await fetch(
      `http://localhost:5128/api/inquiries/${route.params.id}`,
    )

    if (response.status === 404) {
      throw new Error('Inquiry not found.')
    }

    if (!response.ok) {
      throw new Error(
        `Server returned ${response.status}`,
      )
    }

    inquiry.value = await response.json()
  } catch (error) {
    console.error(error)

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Could not load inquiry.'
  } finally {
    loading.value = false
  }
}

onMounted(loadInquiry)

function formatTime(time: string) {
  if (!time) return '—'

  const [hourString = '0', minuteString = '0'] =
    time.split(':')

  const hours = Number(hourString)
  const minutes = Number(minuteString)

  const date = new Date()

  date.setHours(
    hours,
    minutes,
    0,
    0,
  )

  return date.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  })
}
</script>

<template>
  <main class="detail-page">
    <div class="detail-container">

      <RouterLink
        class="back-link"
        to="/admin/inquiries"
      >
        ← Back to Inquiries
      </RouterLink>

      <div
        v-if="loading"
        class="state-message"
      >
        Loading inquiry...
      </div>

      <div
        v-else-if="errorMessage"
        class="state-message error"
      >
        {{ errorMessage }}
      </div>

      <template v-else-if="inquiry">

        <header class="detail-header">

          <div>
            <p class="eyebrow">
              Inquiry #{{ inquiry.id }}
            </p>

            <h1>
              {{ inquiry.firstName }}
              {{ inquiry.lastName }}
            </h1>

            <p class="submitted">
              Submitted
              {{ formatCreatedAt(inquiry.createdAt) }}
            </p>
          </div>

          <span
            class="status"
            :class="inquiry.status.toLowerCase()"
          >
            {{ inquiry.status }}
          </span>

        </header>


        <!-- EVENT -->

        <section class="panel">
          <p class="section-label">
            Event
          </p>

          <div class="info-grid">

            <div>
              <span>Event Type</span>
              <strong>
                {{ inquiry.eventType || 'Not specified' }}
              </strong>
            </div>

            <div>
              <span>Date</span>
              <strong>
                {{ formatDate(inquiry.eventDate) }}
              </strong>
            </div>

            <div>
              <span>Time</span>
              <strong>
                {{ formatTime(inquiry.eventTime) }}
              </strong>
            </div>

            <div>
              <span>Guests</span>
              <strong>
                {{ inquiry.guestCount }}
              </strong>
            </div>

          </div>
        </section>


        <!-- CUSTOMER -->

        <section class="panel">
          <p class="section-label">
            Customer
          </p>

          <div class="info-grid">

            <div>
              <span>Name</span>
              <strong>
                {{ inquiry.firstName }}
                {{ inquiry.lastName }}
              </strong>
            </div>

            <div v-if="inquiry.company">
              <span>Company</span>
              <strong>
                {{ inquiry.company }}
              </strong>
            </div>

            <div>
              <span>Email</span>

              <a :href="`mailto:${inquiry.email}`">
                {{ inquiry.email }}
              </a>
            </div>

            <div>
              <span>Phone</span>

              <a :href="`tel:${inquiry.phone}`">
                {{ inquiry.phone }}
              </a>
            </div>

          </div>
        </section>


        <!-- SERVICES -->

        <section class="panel">
          <p class="section-label">
            Services
          </p>

          <div class="info-grid">

            <div>
              <span>Requested Services</span>

              <strong>
                {{
                  inquiry.services.length
                    ? inquiry.services.join(', ')
                    : 'Not specified'
                }}
              </strong>
            </div>

            <div v-if="inquiry.cateringType">
              <span>Catering</span>
              <strong>
                {{ inquiry.cateringType }}
              </strong>
            </div>

            <div v-if="inquiry.serviceType">
              <span>Pickup / Delivery</span>
              <strong>
                {{ inquiry.serviceType }}
              </strong>
            </div>

            <div v-if="inquiry.packaging">
              <span>Packaging</span>
              <strong>
                {{ inquiry.packaging }}
              </strong>
            </div>

            <div v-if="inquiry.recurring">
              <span>Frequency</span>
              <strong>
                {{ inquiry.recurring }}
              </strong>
            </div>

            <div v-if="inquiry.deliveryAddress">
              <span>Delivery Address</span>
              <strong>
                {{ inquiry.deliveryAddress }}
              </strong>
            </div>

          </div>
        </section>


        <!-- DIETARY -->

        <section
          v-if="
            inquiry.dietaryNeeds.length ||
            inquiry.otherDietaryNeeds
          "
          class="panel"
        >
          <p class="section-label">
            Dietary Needs
          </p>

          <p>
            {{
              inquiry.dietaryNeeds.length
                ? inquiry.dietaryNeeds.join(', ')
                : 'None specified'
            }}
          </p>

          <p v-if="inquiry.otherDietaryNeeds">
            {{ inquiry.otherDietaryNeeds }}
          </p>
        </section>


        <!-- CUSTOMER MESSAGE -->

        <section class="panel message-panel">
          <p class="section-label">
            Customer Message
          </p>

          <p>
            {{ inquiry.details || 'No message provided.' }}
          </p>
        </section>

      </template>

    </div>
  </main>
</template>

<style scoped>
.detail-page {
  min-height: 100vh;
  padding: 70px 24px 120px;
  background: #efede7;
  color: #22221f;
}

.detail-container {
  width: min(1050px, 100%);
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

.detail-header {
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

.detail-header h1 {
  margin: 0;
  font-size: clamp(3rem, 7vw, 5rem);
  line-height: 1;
  letter-spacing: -0.045em;
}

.submitted {
  margin: 14px 0 0;
  color: #6d6961;
}

.status {
  padding: 10px 16px;
  border-radius: 999px;
  background: #ece9e2;
  font-size: 0.8rem;
  font-weight: 700;
}

.status.new {
  background: #e5efe7;
  color: #31593a;
}

.status.contacted {
  background: #e8edf4;
  color: #334e70;
}

.status.quoted {
  background: #f5edd8;
  color: #72591c;
}

.status.booked {
  background: #e4efe9;
  color: #25583d;
}

.status.closed {
  background: #ecebea;
  color: #66615a;
}

.panel {
  margin-bottom: 18px;
  padding: 30px;
  border: 1px solid #dedad1;
  border-radius: 16px;
  background: white;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 28px;
}

.info-grid div {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.info-grid span {
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: #817c73;
}

.info-grid a {
  font-weight: 700;
}

.info-grid a:hover {
  text-decoration: underline;
}

.message-panel > p:last-child {
  margin: 0;
  line-height: 1.7;
  white-space: pre-wrap;
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

@media (max-width: 650px) {
  .detail-header {
    flex-direction: column;
  }

  .info-grid {
    grid-template-columns: 1fr;
  }

  .panel {
    padding: 22px;
  }
}
</style>