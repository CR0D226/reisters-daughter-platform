<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'

// --------------------------------------------------
// TYPES
// --------------------------------------------------

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


// --------------------------------------------------
// STATE
// --------------------------------------------------

const inquiries = ref<Inquiry[]>([])

const loading = ref(true)

const errorMessage = ref('')

const updatingInquiryId =
  ref<number | null>(null)

const statuses = [
  'New',
  'Contacted',
  'Quoted',
  'Booked',
  'Closed',
]


// --------------------------------------------------
// LOAD INQUIRIES
// --------------------------------------------------

async function loadInquiries() {
  loading.value = true
  errorMessage.value = ''

  try {
    const response = await fetch(
      'http://localhost:5128/api/inquiries',
    )

    if (!response.ok) {
      throw new Error(
        `Server returned ${response.status}`,
      )
    }

    inquiries.value =
      await response.json()
  } catch (error) {
    console.error(
      'Could not load inquiries:',
      error,
    )

    errorMessage.value =
      'Could not load inquiries.'
  } finally {
    loading.value = false
  }
}


// --------------------------------------------------
// UPDATE STATUS
// --------------------------------------------------

async function updateStatus(
  inquiry: Inquiry,
  newStatus: string,
) {
  const previousStatus =
    inquiry.status

  updatingInquiryId.value =
    inquiry.id

  // Update the UI immediately
  inquiry.status =
    newStatus

  try {
    const response = await fetch(
      `http://localhost:5128/api/inquiries/${inquiry.id}/status`,
      {
        method: 'PATCH',

        headers: {
          'Content-Type':
            'application/json',
        },

        body: JSON.stringify({
          status: newStatus,
        }),
      },
    )

    if (!response.ok) {
      throw new Error(
        `Server returned ${response.status}`,
      )
    }

    console.log(
      `Inquiry #${inquiry.id} changed to ${newStatus}`,
    )
  } catch (error) {
    console.error(
      'Status update failed:',
      error,
    )

    // Restore the old value
    // if the database update failed.
    inquiry.status =
      previousStatus

    alert(
      'Could not update the inquiry status.',
    )
  } finally {
    updatingInquiryId.value =
      null
  }
}


// --------------------------------------------------
// FORMATTING
// --------------------------------------------------

function formatDate(
  date: string,
) {
  if (!date) {
    return '—'
  }

  return new Date(
    `${date}T00:00:00`,
  ).toLocaleDateString()
}


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


// --------------------------------------------------
// START
// --------------------------------------------------

onMounted(() => {
  loadInquiries()
})
</script>


<template>
  <main class="admin-page">

    <div class="admin-container">

      <!-- HEADER -->

      <header class="admin-header">

        <div>
          <p class="eyebrow">
            The Reister's Daughter
          </p>

          <h1>
            Inquiries
          </h1>

          <p>
            Catering, corporate event
            and café rental leads.
          </p>
        </div>


        <div class="inquiry-count">
          <strong>
            {{ inquiries.length }}
          </strong>

          <span>
            Total Inquiries
          </span>
        </div>

      </header>


      <!-- LOADING -->

      <div
        v-if="loading"
        class="state-message"
      >
        Loading inquiries...
      </div>


      <!-- ERROR -->

      <div
        v-else-if="errorMessage"
        class="state-message error"
      >
        {{ errorMessage }}
      </div>


      <!-- EMPTY -->

      <div
        v-else-if="inquiries.length === 0"
        class="state-message"
      >
        No inquiries yet.
      </div>


      <!-- INQUIRIES -->

      <section
        v-else
        class="inquiry-list"
      >

        <article
          v-for="inquiry in inquiries"
          :key="inquiry.id"
          class="inquiry-card"
        >

          <!-- CARD HEADER -->

          <div class="card-header">

            <div>

              <div class="inquiry-meta">
                Inquiry #{{ inquiry.id }}

                <span>•</span>

              {{ formatTime(inquiry.eventTime) }}
              </div>


              <h2>
                {{ inquiry.firstName }}
                {{ inquiry.lastName }}
              </h2>


              <p v-if="inquiry.company">
                {{ inquiry.company }}
              </p>

            </div>


            <!-- STATUS -->

            <div class="status-control">

              <span class="status-label">
                Status
              </span>


              <select
                class="status-select"
                :class="
                  inquiry.status.toLowerCase()
                "
                :value="inquiry.status"
                :disabled="
                  updatingInquiryId ===
                  inquiry.id
                "
                @change="
                  updateStatus(
                    inquiry,
                    (
                      $event.target as
                      HTMLSelectElement
                    ).value,
                  )
                "
              >

                <option
                  v-for="status in statuses"
                  :key="status"
                  :value="status"
                >
                  {{ status }}
                </option>

              </select>

            </div>

          </div>


          <!-- EVENT SUMMARY -->

          <div class="event-summary">

            <div>
              <span>
                Event
              </span>

              <strong>
                {{
                  inquiry.eventType ||
                  'Not specified'
                }}
              </strong>
            </div>


            <div>
              <span>
                Date
              </span>

              <strong>
                {{
                  formatDate(
                    inquiry.eventDate,
                  )
                }}
              </strong>
            </div>


            <div>
              <span>
                Time
              </span>

              <strong>
                {{
                   formatTime(inquiry.eventTime) 
                }}
              </strong>
            </div>


            <div>
              <span>
                Guests
              </span>

              <strong>
                {{ inquiry.guestCount }}
              </strong>
            </div>

          </div>


          <!-- SERVICE DETAILS -->

          <div class="details-grid">

            <div class="detail-block">

              <span>
                Services
              </span>

              <strong>
                {{
                  inquiry.services.length
                    ? inquiry.services.join(
                        ', ',
                      )
                    : 'Not specified'
                }}
              </strong>

            </div>


            <div
              v-if="inquiry.cateringType"
              class="detail-block"
            >

              <span>
                Catering
              </span>

              <strong>
                {{ inquiry.cateringType }}
              </strong>

            </div>


            <div
              v-if="inquiry.serviceType"
              class="detail-block"
            >

              <span>
                Pickup / Delivery
              </span>

              <strong>
                {{ inquiry.serviceType }}
              </strong>

            </div>


            <div
              v-if="inquiry.recurring"
              class="detail-block"
            >

              <span>
                Frequency
              </span>

              <strong>
                {{ inquiry.recurring }}
              </strong>

            </div>

          </div>


          <!-- DELIVERY ADDRESS -->

          <div
            v-if="inquiry.deliveryAddress"
            class="extra-detail"
          >

            <span>
              Delivery Address
            </span>

            <strong>
              {{ inquiry.deliveryAddress }}
            </strong>

          </div>


          <!-- DIETARY -->

          <div
            v-if="
              inquiry.dietaryNeeds.length
            "
            class="extra-detail"
          >

            <span>
              Dietary Needs
            </span>

            <strong>
              {{
                inquiry.dietaryNeeds.join(
                  ', ',
                )
              }}
            </strong>

          </div>


          <!-- CONTACT -->

          <div class="contact-row">

            <a
              :href="
                `mailto:${inquiry.email}`
              "
            >
              {{ inquiry.email }}
            </a>


            <a
              :href="
                `tel:${inquiry.phone}`
              "
            >
              {{ inquiry.phone }}
            </a>

          </div>


          <!-- CUSTOMER NOTES -->

          <div
            v-if="inquiry.details"
            class="notes"
          >

            <span>
              Customer Notes
            </span>

            <p>
              {{ inquiry.details }}
            </p>

          </div>
          <RouterLink
                class="view-button"
             :to="`/admin/inquiries/${inquiry.id}`"
                >
         View Inquiry →
        </RouterLink>

        </article>

      </section>

    </div>

  </main>
</template>


<style scoped>
.admin-page {
  min-height: 100vh;

  padding:
    70px 24px
    120px;

  background: #efede7;

  color: #22221f;
}


.admin-container {
  width: min(
    1180px,
    100%
  );

  margin: 0 auto;
}


/* HEADER */

.admin-header {
  display: flex;

  justify-content:
    space-between;

  align-items:
    flex-end;

  gap: 40px;

  margin-bottom:
    50px;
}


.eyebrow {
  margin:
    0 0 10px;

  font-size:
    0.75rem;

  font-weight:
    700;

  letter-spacing:
    0.15em;

  text-transform:
    uppercase;

  color:
    #77736b;
}


.admin-header h1 {
  margin: 0;

  font-size:
    clamp(
      3rem,
      7vw,
      5.5rem
    );

  line-height: 1;

  letter-spacing:
    -0.05em;
}


.admin-header p:last-child {
  margin:
    15px 0 0;

  color:
    #6d6961;
}


/* COUNTER */

.inquiry-count {
  min-width:
    150px;

  padding:
    20px;

  border-radius:
    14px;

  background:
    #22221f;

  color:
    white;
}


.inquiry-count strong {
  display:
    block;

  font-size:
    2rem;
}


.inquiry-count span {
  font-size:
    0.8rem;

  color:
    #c9c6bf;
}


/* STATES */

.state-message {
  padding:
    50px;

  border-radius:
    14px;

  background:
    white;

  text-align:
    center;
}


.state-message.error {
  color:
    #7b2c25;
}


/* LIST */

.inquiry-list {
  display:
    grid;

  gap:
    20px;
}


/* CARD */

.inquiry-card {
  padding:
    30px;

  border:
    1px solid
    #dedad1;

  border-radius:
    16px;

  background:
    white;
}


/* CARD HEADER */

.card-header {
  display:
    flex;

  justify-content:
    space-between;

  gap:
    25px;

  margin-bottom:
    28px;
}


.inquiry-meta {
  margin-bottom:
    8px;

  font-size:
    0.75rem;

  color:
    #858077;
}


.card-header h2 {
  margin: 0;

  font-size:
    1.8rem;
}


.card-header p {
  margin:
    6px 0 0;

  color:
    #6d6961;
}


/* STATUS */

.status-control {
  display:
    flex;

  flex-direction:
    column;

  align-items:
    flex-end;

  gap:
    6px;
}


.status-label {
  font-size:
    0.68rem;

  font-weight:
    700;

  letter-spacing:
    0.1em;

  text-transform:
    uppercase;

  color:
    #817c73;
}


.status-select {
  width:
    auto;

  min-width:
    135px;

  min-height:
    40px;

  padding:
    0 36px
    0 14px;

  border:
    1px solid
    #d4d0c8;

  border-radius:
    999px;

  font-size:
    0.78rem;

  font-weight:
    700;

  cursor:
    pointer;
}


.status-select.new {
  background:
    #e5efe7;

  color:
    #31593a;
}


.status-select.contacted {
  background:
    #e8edf4;

  color:
    #334e70;
}


.status-select.quoted {
  background:
    #f5edd8;

  color:
    #72591c;
}


.status-select.booked {
  background:
    #e4efe9;

  color:
    #25583d;
}


.status-select.closed {
  background:
    #ecebea;

  color:
    #66615a;
}


.status-select:disabled {
  cursor:
    wait;

  opacity:
    0.6;
}


/* EVENT SUMMARY */

.event-summary {
  display:
    grid;

  grid-template-columns:
    2fr 1fr 1fr 0.7fr;

  gap:
    15px;

  padding:
    22px;

  border-radius:
    12px;

  background:
    #f4f1eb;
}


.event-summary div,
.detail-block {
  display:
    flex;

  flex-direction:
    column;

  gap:
    5px;
}


.event-summary span,
.detail-block span,
.extra-detail span,
.notes span {
  font-size:
    0.72rem;

  font-weight:
    700;

  letter-spacing:
    0.08em;

  text-transform:
    uppercase;

  color:
    #817c73;
}


/* DETAILS */

.details-grid {
  display:
    grid;

  grid-template-columns:
    repeat(
      4,
      1fr
    );

  gap:
    20px;

  margin-top:
    25px;
}


.extra-detail {
  display:
    flex;

  flex-direction:
    column;

  gap:
    6px;

  margin-top:
    22px;
}


/* CONTACT */

.contact-row {
  display:
    flex;

  flex-wrap:
    wrap;

  gap:
    20px;

  margin-top:
    28px;

  padding-top:
    22px;

  border-top:
    1px solid
    #e3dfd7;
}


.contact-row a {
  font-weight:
    700;
}


.contact-row a:hover {
  text-decoration:
    underline;
}


/* NOTES */

.notes {
  margin-top:
    24px;
}


.notes p {
  margin:
    8px 0 0;

  line-height:
    1.6;

  color:
    #55524c;
}


/* RESPONSIVE */

@media (
  max-width: 800px
) {

  .admin-header {
    align-items:
      flex-start;

    flex-direction:
      column;
  }


  .event-summary,
  .details-grid {
    grid-template-columns:
      repeat(
        2,
        1fr
      );
  }


  .inquiry-count {
    width:
      100%;
  }
}


@media (
  max-width: 500px
) {

  .event-summary,
  .details-grid {
    grid-template-columns:
      1fr;
  }


  .inquiry-card {
    padding:
      20px;
  }


  .card-header {
    align-items:
      flex-start;
  }


  .status-select {
    min-width:
      115px;
  }
}
.view-button {
  display: inline-flex;
  margin-top: 26px;
  padding: 11px 17px;
  border-radius: 999px;
  background: #22221f;
  color: white;
  font-size: 0.82rem;
  font-weight: 700;
}

.view-button:hover {
  opacity: 0.85;
}
</style>