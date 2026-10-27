<script setup lang="ts">
import { apiFetch } from '@/services/api'
import { onMounted, onUnmounted, ref } from 'vue'
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

const openStatusId = ref<number | null>(null)

function toggleStatusMenu(id: number) {
  openStatusId.value = openStatusId.value === id ? null : id
}

function closeStatusMenu() {
  openStatusId.value = null
}

function selectStatus(inquiry: Inquiry, status: string) {
  closeStatusMenu()

  if (status === inquiry.status) return

  void updateStatus(inquiry, status)
}
function handleStatusOutsideClick(event: MouseEvent) {
  const target = event.target

  if (
    target instanceof Element &&
    !target.closest('.custom-status')
  ) {
    closeStatusMenu()
  }
}

function handleStatusEscape(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    closeStatusMenu()
  }
}

onMounted(() => {
  document.addEventListener('click', handleStatusOutsideClick)
  document.addEventListener('keydown', handleStatusEscape)
})

onUnmounted(() => {
  document.removeEventListener('click', handleStatusOutsideClick)
  document.removeEventListener('keydown', handleStatusEscape)
})
// --------------------------------------------------
// LOAD INQUIRIES
// --------------------------------------------------

async function loadInquiries() {
  loading.value = true
  errorMessage.value = ''

  try {
    const response = await apiFetch(
      '/api/inquiries',
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
    const response = await apiFetch(
      `/api/inquiries/${inquiry.id}/status`,
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



<div class="custom-status" :class="{ 'is-open': openStatusId === inquiry.id }">
  <button
    type="button"
    class="status-trigger"
    :class="inquiry.status.toLowerCase()"
    :disabled="updatingInquiryId === inquiry.id"
    :aria-expanded="openStatusId === inquiry.id"
    aria-haspopup="true"
    @click="toggleStatusMenu(inquiry.id)"
  >
    <span>{{ inquiry.status }}</span>
    <span class="status-chevron" aria-hidden="true">⌄</span>
  </button>

  <div
    v-if="openStatusId === inquiry.id"
    class="status-menu"
    role="group"
    aria-label="Change inquiry status"
  >
    <button
      v-for="status in statuses"
      :key="status"
      type="button"
      class="status-option"
      :class="{ selected: inquiry.status === status }"
      :aria-pressed="inquiry.status === status"
      @click="selectStatus(inquiry, status)"
    >
      <span class="status-dot" :class="status.toLowerCase()"></span>
      <span>{{ status }}</span>
      <span v-if="inquiry.status === status" class="status-check" aria-hidden="true">✓</span>
    </button>
  </div>
</div>


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

/* ==========================================
   ADMIN PORTAL DESIGN CONSISTENCY
   ========================================== */

.admin-page {
  min-height: 100%;
  padding: 40px 32px 64px;
  background: #faf7f9;
  color: #30252d;
}

.admin-container {
  width: 100%;
  max-width: 1450px;
  margin: 0 auto;
}

/* Page header */

.admin-header {
  align-items: center;
  gap: 24px;
  margin-bottom: 30px;
}

.eyebrow {
  margin-bottom: 10px;
  color: #8b526f;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.15em;
}

.admin-header h1 {
  margin: 0;
  color: #282528;
  font-size: clamp(30px, 3vw, 40px);
  font-weight: 750;
  line-height: 1.15;
  letter-spacing: -0.04em;
}

.admin-header p:last-child {
  margin-top: 10px;
  color: #756c73;
  font-size: 14px;
  line-height: 1.6;
}

/* Inquiry count */

.inquiry-count {
  min-width: 145px;
  padding: 18px 22px;
  background: #fff;
  color: #30252d;
  border: 1px solid #e9dfe5;
  border-radius: 14px;
  box-shadow: 0 4px 18px rgb(90 45 70 / 5%);
}

.inquiry-count strong {
  font-size: 30px;
  line-height: 1.2;
  letter-spacing: -0.04em;
}

.inquiry-count span {
  color: #756c73;
  font-size: 12px;
}

/* Inquiry cards */

.inquiry-list {
  gap: 16px;
}

.inquiry-card {
  padding: 24px;
  background: #fff;
  border: 1px solid #e9dfe5;
  border-radius: 14px;
  box-shadow: 0 4px 18px rgb(90 45 70 / 5%);
}

.card-header {
  align-items: flex-start;
  gap: 20px;
  margin-bottom: 22px;
}

.inquiry-meta {
  color: #928991;
  font-size: 12px;
}

.card-header h2 {
  color: #30252d;
  font-size: 21px;
  letter-spacing: -0.02em;
}

.card-header p {
  color: #756c73;
  font-size: 13px;
}

/* Status controls */

.status-label {
  color: #928991;
  font-size: 11px;
}

.status-select {
  min-height: 40px;
  border: 1px solid #e9dfe5;
  font-size: 12px;
}

/* Event information */

.event-summary {
  gap: 16px;
  padding: 20px;
  background: #faf7f9;
  border: 1px solid #f0e7ec;
  border-radius: 10px;
}

.event-summary span,
.detail-block span,
.extra-detail span,
.notes span {
  color: #928991;
  font-size: 11px;
}

.details-grid {
  gap: 18px;
  margin-top: 22px;
}

.contact-row {
  border-top-color: #e9dfe5;
}

.contact-row a {
  color: #8b526f;
}

.notes p {
  color: #756c73;
}

/* View details button */

.view-button {
  margin-top: 22px;
  padding: 11px 17px;
  background: #8b526f;
  color: #fff;
  border-radius: 9px;
  font-size: 13px;
  font-weight: 700;
  text-decoration: none;
}

.view-button:hover {
  background: #70405a;
  opacity: 1;
}

/* Empty and error states */

.state-message {
  padding: 36px;
  border: 1px solid #e9dfe5;
  border-radius: 14px;
  background: #fff;
}

/* Responsive */

@media (max-width: 800px) {
  .admin-page {
    padding: 28px 20px 48px;
  }

  .admin-header {
    flex-direction: column;
    align-items: flex-start;
  }

  .inquiry-count {
    width: 100%;
  }

  .event-summary,
  .details-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

/* Refined inquiry status dropdown */

.status-control {
  align-items: flex-start;
  gap: 7px;
}

.status-label {
  color: #756c73;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.08em;
}

.status-select {
  appearance: none;
  -webkit-appearance: none;

  min-width: 150px;
  min-height: 42px;
  padding: 10px 38px 10px 15px;

  border: 1px solid #e9dfe5;
  border-radius: 9px;

  font-size: 13px;
  font-weight: 700;

  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='%23756c73' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpath d='m6 9 6 6 6-6'/%3E%3C/svg%3E");
  background-repeat: no-repeat;
  background-position: right 12px center;
  background-size: 16px;

  cursor: pointer;
}

.status-select:focus-visible {
  outline: 2px solid #8b526f;
  outline-offset: 2px;
}

.status-select:hover:not(:disabled) {
  border-color: #8b526f;
}

/* Custom inquiry status dropdown */

.status-control {
  align-items: flex-end;
}

.custom-status {
  position: relative;
  min-width: 150px;
}

.custom-status.is-open {
  z-index: 20;
}

.status-trigger {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  width: 100%;
  min-height: 42px;
  padding: 10px 13px;
  border: 1px solid #e9dfe5;
  border-radius: 9px;
  color: #30252d;
  background: #faf7f9;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
}

.status-trigger.new {
  background: #e5efe7;
  color: #31593a;
}

.status-trigger.contacted {
  background: #e8edf4;
  color: #334e70;
}

.status-trigger.quoted {
  background: #f5edd8;
  color: #72591c;
}

.status-trigger.booked {
  background: #e4efe9;
  color: #25583d;
}

.status-trigger.closed {
  background: #ecebea;
  color: #66615a;
}

.status-trigger:hover:not(:disabled) {
  border-color: #8b526f;
}

.status-trigger:disabled {
  opacity: 0.6;
  cursor: wait;
}

.status-chevron {
  font-size: 18px;
  line-height: 1;
}

.status-menu {
  position: absolute;
  top: calc(100% + 6px);
  right: 0;
  z-index: 30;
  width: 190px;
  padding: 6px;
  background: #fff;
  border: 1px solid #e9dfe5;
  border-radius: 12px;
  box-shadow: 0 12px 32px rgb(48 37 45 / 15%);
}

.status-option {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  padding: 11px 12px;
  border: 0;
  border-radius: 7px;
  background: transparent;
  color: #30252d;
  text-align: left;
  font-size: 13px;
  cursor: pointer;
}

.status-option:hover,
.status-option.selected {
  background: #f4e3ec;
}

.status-check {
  margin-left: auto;
  color: #8b526f;
  font-weight: 800;
}

.status-dot {
  flex-shrink: 0;
  width: 9px;
  height: 9px;
  border-radius: 50%;
  background: #999;
}

.status-dot.new {
  background: #5b9b70;
}

.status-dot.contacted {
  background: #6e94c1;
}

.status-dot.quoted {
  background: #c6a34c;
}

.status-dot.booked {
  background: #40815b;
}

.status-dot.closed {
  background: #929292;
}

.status-trigger:focus-visible,
.status-option:focus-visible {
  outline: 2px solid #8b526f;
  outline-offset: 2px;
}

@media (max-width: 500px) {
  .admin-page {
    padding: 24px 16px 48px;
  }

  .inquiry-card {
    padding: 18px;
  }

  .card-header {
    flex-direction: column;
    gap: 14px;
  }

  .status-control {
    align-items: flex-start;
  }

  .event-summary,
  .details-grid {
    grid-template-columns: 1fr;
  }
}

</style>
