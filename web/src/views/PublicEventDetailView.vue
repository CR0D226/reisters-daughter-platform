<script setup lang="ts">
import { API_BASE } from '../config/api'
import {
  computed,
  onMounted,
  reactive,
  ref,
} from 'vue'

import {
  RouterLink,
  useRoute,
} from 'vue-router'

interface PublicEvent {
  id: number
  title: string
  description: string | null
  type: string
  startDate: string
  startTime: string
  endDate: string | null
  endTime: string | null
  location: string | null
  capacity: number | null
  coverImagePath: string | null
}

interface ReservationResponse {
  id: number
  eventId: number
  firstName: string
  lastName: string
  email: string
  phone: string
  guestCount: number
  status: string
  createdAt: string
}

interface ApiErrorResponse {
  message?: string
  availableSeats?: number
}

const route = useRoute()

const event = ref<PublicEvent | null>(null)
const loading = ref(true)
const notFound = ref(false)
const errorMessage = ref('')

const reservationSubmitting = ref(false)
const reservationError = ref('')
const reservationSuccess =
  ref<ReservationResponse | null>(null)

const reservationForm = reactive({
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  guestCount: 1,
  notes: '',
})

const eventId = computed(() => {
  const id = Number(route.params.id)

  return Number.isFinite(id)
    ? id
    : null
})

async function loadEvent() {
  loading.value = true
  errorMessage.value = ''
  notFound.value = false

  if (!eventId.value) {
    notFound.value = true
    loading.value = false
    return
  }

  try {
    const response = await fetch(
      `${API_BASE}/api/public/events/${eventId.value}`,
    )

    if (response.status === 404) {
      notFound.value = true
      return
    }

    if (!response.ok) {
      throw new Error(
        `Server returned ${response.status}`,
      )
    }

    event.value =
      (await response.json()) as PublicEvent
  } catch (error) {
    console.error(
      'Unable to load event:',
      error,
    )

    errorMessage.value =
      'We could not load this event right now.'
  } finally {
    loading.value = false
  }
}

async function submitReservation() {
  if (!eventId.value) {
    return
  }

  reservationSubmitting.value = true
  reservationError.value = ''

  try {
    const response = await fetch(
      `${API_BASE}/api/public/events/${eventId.value}/reservations`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          firstName:
            reservationForm.firstName,
          lastName:
            reservationForm.lastName,
          email:
            reservationForm.email,
          phone:
            reservationForm.phone,
          guestCount:
            reservationForm.guestCount,
          notes:
            reservationForm.notes || null,
        }),
      },
    )

    const data =
      (await response.json()) as
        | ReservationResponse
        | ApiErrorResponse

    if (!response.ok) {
      const apiError =
        data as ApiErrorResponse

      reservationError.value =
        apiError.message ??
        'We could not submit your reservation.'

      return
    }

    reservationSuccess.value =
      data as ReservationResponse
  } catch (error) {
    console.error(
      'Unable to submit reservation:',
      error,
    )

    reservationError.value =
      'We could not submit your reservation. Please try again.'
  } finally {
    reservationSubmitting.value = false
  }
}

function makeAnotherReservation() {
  reservationSuccess.value = null
  reservationError.value = ''

  reservationForm.firstName = ''
  reservationForm.lastName = ''
  reservationForm.email = ''
  reservationForm.phone = ''
  reservationForm.guestCount = 1
  reservationForm.notes = ''
}

function parseDate(value: string) {
  const [year, month, day] =
    value.split('-').map(Number)

  return new Date(
    year ?? 0,
    (month ?? 1) - 1,
    day ?? 1,
  )
}

function formatDate(value: string) {
  return parseDate(value)
    .toLocaleDateString('en-US', {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
      year: 'numeric',
    })
}

function formatTime(value: string | null) {
  if (!value) {
    return ''
  }

  const [hours, minutes] =
    value.split(':').map(Number)

  const date = new Date()

  date.setHours(
    hours ?? 0,
    minutes ?? 0,
    0,
    0,
  )

  return date.toLocaleTimeString(
    'en-US',
    {
      hour: 'numeric',
      minute: '2-digit',
    },
  )
}

const timeDisplay = computed(() => {
  if (!event.value) {
    return ''
  }

  const start =
    formatTime(event.value.startTime)

  const end =
    formatTime(event.value.endTime)

  return end
    ? `${start} – ${end}`
    : start
})

const multiDay = computed(() => {
  if (!event.value?.endDate) {
    return false
  }

  return (
    event.value.endDate !==
    event.value.startDate
  )
})

onMounted(loadEvent)
</script>

<template>
  <main class="event-detail-page">

    <section
      v-if="loading"
      class="state-section"
    >
      <p>Loading event...</p>
    </section>

    <section
      v-else-if="notFound"
      class="state-section"
    >
      <p class="eyebrow">
        Event Not Found
      </p>

      <h1>
        This event isn't available.
      </h1>

      <p>
        It may no longer be published,
        or the event may have ended.
      </p>

      <RouterLink
        class="back-button"
        to="/events"
      >
        View Upcoming Events
      </RouterLink>
    </section>

    <section
      v-else-if="errorMessage"
      class="state-section"
    >
      <p class="eyebrow">
        Something Went Wrong
      </p>

      <h1>
        We couldn't load this event.
      </h1>

      <p>
        {{ errorMessage }}
      </p>

      <RouterLink
        class="back-button"
        to="/events"
      >
        Back to Events
      </RouterLink>
    </section>

    <template v-else-if="event">

      <!-- EVENT SPLIT HERO V1 -->
      <section
        class="event-hero split-event-hero"
        :class="{ 'without-cover': !event.coverImagePath }"
      >
        <div class="split-hero-content">
          <RouterLink
            class="back-link"
            to="/events"
          >
            ← All Events
          </RouterLink>

          <div class="split-hero-copy">
            <p class="eyebrow">
              {{ event.type }}
            </p>

            <h1>{{ event.title }}</h1>

            <p
              v-if="event.description"
              class="hero-description"
            >
              {{ event.description }}
            </p>

            <div class="hero-event-facts">
              <div>
                <span class="hero-fact-label">Date</span>
                <strong>{{ formatDate(event.startDate) }}</strong>
                <span v-if="multiDay && event.endDate">
                  Through {{ formatDate(event.endDate) }}
                </span>
              </div>

              <div>
                <span class="hero-fact-label">Time</span>
                <strong>{{ timeDisplay }}</strong>
              </div>

              <div v-if="event.location">
                <span class="hero-fact-label">Location</span>
                <strong>{{ event.location }}</strong>
              </div>
            </div>

            <a
              class="hero-reserve-button"
              href="#event-reservations"
            >
              Reserve Your Spot
              <span aria-hidden="true">→</span>
            </a>
          </div>
        </div>

        <div
          v-if="event.coverImagePath"
          class="split-hero-image"
        >
          <img
            :src="`${API_BASE}/api/public/events/${event.id}/cover-image`"
            :alt="`${event.title} cover image`"
          />
        </div>
      </section>

      <!-- EVENT INFORMATION -->

      <section class="event-information">

        <div class="details-grid">

          <div class="detail">
            <p class="detail-label">
              Date
            </p>

            <p class="detail-value">
              {{
                formatDate(
                  event.startDate,
                )
              }}
            </p>

            <p
              v-if="
                multiDay &&
                event.endDate
              "
              class="detail-secondary"
            >
              through
              {{
                formatDate(
                  event.endDate,
                )
              }}
            </p>
          </div>

          <div class="detail">
            <p class="detail-label">
              Time
            </p>

            <p class="detail-value">
              {{ timeDisplay }}
            </p>
          </div>

          <div
            v-if="event.location"
            class="detail"
          >
            <p class="detail-label">
              Location
            </p>

            <p class="detail-value">
              {{ event.location }}
            </p>
          </div>

          <div
            v-if="event.capacity"
            class="detail"
          >
            <p class="detail-label">
              Capacity
            </p>

            <p class="detail-value">
              {{ event.capacity }} guests
            </p>
          </div>

        </div>

        <div class="event-body">

          <div class="event-copy">
            <p class="eyebrow">
              About The Event
            </p>

            <h2>
              Join us at
              The Reister's Daughter.
            </h2>

            <p v-if="event.description">
              {{ event.description }}
            </p>

            <p v-else>
              More details about this event
              will be available soon.
            </p>
          </div>

          <!-- RESERVATION -->

          <aside id="event-reservations" class="reservation-card">
            <p class="eyebrow">
              Reservations
            </p>

            <template
              v-if="reservationSuccess"
            >
              <h3>
                Reservation received!
              </h3>

              <p>
                Thanks,
                {{ reservationSuccess.firstName }}.
                Your reservation for
                {{
                  reservationSuccess.guestCount
                }}
                {{
                  reservationSuccess.guestCount === 1
                    ? 'guest'
                    : 'guests'
                }}
                has been received.
              </p>

              <div class="success-box">
                <span class="success-label">
                  Status
                </span>

                <strong>
                  {{
                    reservationSuccess.status
                  }}
                </strong>
              </div>

              <p class="confirmation-note">
                We'll follow up with you
                about confirmation and
                event details.
              </p>

              <button
                class="secondary-button"
                type="button"
                @click="makeAnotherReservation"
              >
                Make Another Reservation
              </button>
            </template>

            <template v-else>
              <h3>
                Interested in this event?
              </h3>

              <p>
                Reserve your spot below.
                Reservations are pending
                until confirmed by our team.
              </p>

              <form
                class="reservation-form"
                @submit.prevent="
                  submitReservation
                "
              >
                <div class="form-row">
                  <label>
                    <span>First Name</span>

                    <input
                      v-model="
                        reservationForm.firstName
                      "
                      type="text"
                      autocomplete="given-name"
                      required
                    />
                  </label>

                  <label>
                    <span>Last Name</span>

                    <input
                      v-model="
                        reservationForm.lastName
                      "
                      type="text"
                      autocomplete="family-name"
                      required
                    />
                  </label>
                </div>

                <label>
                  <span>Email</span>

                  <input
                    v-model="
                      reservationForm.email
                    "
                    type="email"
                    autocomplete="email"
                    required
                  />
                </label>

                <label>
                  <span>Phone</span>

                  <input
                    v-model="
                      reservationForm.phone
                    "
                    type="tel"
                    autocomplete="tel"
                    required
                  />
                </label>

                <label>
                  <span>Number of Guests</span>

                  <input
                    v-model.number="
                      reservationForm.guestCount
                    "
                    type="number"
                    min="1"
                    :max="
                      event.capacity ?? undefined
                    "
                    required
                  />
                </label>

                <label>
                  <span>
                    Notes
                    <small>Optional</small>
                  </span>

                  <textarea
                    v-model="
                      reservationForm.notes
                    "
                    rows="3"
                    placeholder="Anything we should know?"
                  />
                </label>

                <div
                  v-if="reservationError"
                  class="reservation-error"
                  role="alert"
                >
                  {{ reservationError }}
                </div>

                <button
                  class="reserve-button"
                  type="submit"
                  :disabled="
                    reservationSubmitting
                  "
                >
                  {{
                    reservationSubmitting
                      ? 'Submitting...'
                      : 'Reserve Your Spot'
                  }}
                </button>
              </form>
            </template>
          </aside>

        </div>

      </section>

      <!-- CATERING CTA -->

      <section class="private-events">
        <div class="private-inner">

          <p class="eyebrow">
            Host Your Own
          </p>

          <h2>
            Planning a private event?
          </h2>

          <p>
            From catering to private
            gatherings, tell us what
            you're planning.
          </p>

          <RouterLink
            class="inquiry-button"
            to="/catering"
          >
            Catering & Private Events
          </RouterLink>

        </div>
      </section>

    </template>

  </main>
</template>

<style scoped>
.event-detail-page {
  min-height: 100vh;
  background: #f7f4ee;
  color: #2f2a26;
}

.event-hero {
  padding: 90px 5% 95px;
  border-bottom:
    1px solid rgba(47, 42, 38, 0.12);
}

.hero-inner {
  max-width: 1000px;
}

.back-link {
  display: inline-block;
  margin-bottom: 60px;
  color: #655d56;
  font-size: 0.9rem;
  font-weight: 600;
}

.back-link:hover {
  opacity: 0.6;
}

.eyebrow {
  margin: 0 0 16px;
  color: #8b725c;
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.16em;
  text-transform: uppercase;
}

.event-hero h1 {
  max-width: 900px;
  margin: 0;
  font-size: clamp(
    3.4rem,
    8vw,
    7.5rem
  );
  line-height: 0.92;
  letter-spacing: -0.06em;
}

.hero-description {
  max-width: 720px;
  margin: 35px 0 0;
  color: #625a53;
  font-size: 1.2rem;
  line-height: 1.7;
}

.event-information {
  max-width: 1200px;
  margin: 0 auto;
  padding: 80px 5% 110px;
}

.details-grid {
  display: grid;
  grid-template-columns:
    repeat(4, 1fr);
  margin-bottom: 90px;
  border-top:
    1px solid rgba(47, 42, 38, 0.15);
  border-bottom:
    1px solid rgba(47, 42, 38, 0.15);
}

.detail {
  min-height: 150px;
  padding: 28px 24px;
  border-right:
    1px solid rgba(47, 42, 38, 0.15);
}

.detail:last-child {
  border-right: 0;
}

.detail-label {
  margin: 0 0 15px;
  color: #8b725c;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.detail-value {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 650;
  line-height: 1.5;
}

.detail-secondary {
  margin: 8px 0 0;
  color: #6b625a;
  font-size: 0.9rem;
}

.event-body {
  display: grid;
  grid-template-columns:
    minmax(0, 1fr) 420px;
  gap: 70px;
  align-items: start;
}

.event-copy {
  max-width: 650px;
}

.event-copy h2 {
  margin: 0 0 25px;
  font-size: clamp(
    2.3rem,
    5vw,
    4rem
  );
  line-height: 1;
  letter-spacing: -0.045em;
}

.event-copy > p:last-child {
  color: #625a53;
  font-size: 1.05rem;
  line-height: 1.8;
}

/* RESERVATION CARD */

.reservation-card {
  padding: 35px;
  border-radius: 18px;
  background: #302a25;
  color: #ffffff;
}

.reservation-card .eyebrow {
  color: #c7b29f;
}

.reservation-card h3 {
  margin: 0 0 15px;
  font-size: 1.65rem;
  letter-spacing: -0.03em;
}

.reservation-card > p,
.reservation-card
  > template
  > p {
  color: rgba(255, 255, 255, 0.7);
  line-height: 1.6;
}

.reservation-form {
  display: grid;
  gap: 17px;
  margin-top: 28px;
}

.form-row {
  display: grid;
  grid-template-columns:
    repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.reservation-form label {
  display: grid;
  gap: 8px;
}

.reservation-form label > span {
  color: rgba(255, 255, 255, 0.82);
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.05em;
  text-transform: uppercase;
}

.reservation-form small {
  margin-left: 5px;
  color: rgba(255, 255, 255, 0.45);
  font-size: 0.65rem;
  font-weight: 500;
}

.reservation-form input,
.reservation-form textarea {
  width: 100%;
  box-sizing: border-box;
  border:
    1px solid rgba(255, 255, 255, 0.18);
  border-radius: 9px;
  outline: none;
  background:
    rgba(255, 255, 255, 0.06);
  color: #ffffff;
  font: inherit;
}

.reservation-form input {
  height: 46px;
  padding: 0 13px;
}

.reservation-form textarea {
  min-height: 90px;
  padding: 12px 13px;
  resize: vertical;
}

.reservation-form input:focus,
.reservation-form textarea:focus {
  border-color:
    rgba(255, 255, 255, 0.55);
}

.reservation-form textarea::placeholder {
  color: rgba(255, 255, 255, 0.35);
}

.reserve-button,
.secondary-button {
  width: 100%;
  min-height: 50px;
  border: 0;
  border-radius: 9px;
  cursor: pointer;
  background: #ffffff;
  color: #302a25;
  font: inherit;
  font-weight: 750;
}

.reserve-button {
  margin-top: 4px;
}

.reserve-button:hover,
.secondary-button:hover {
  opacity: 0.9;
}

.reserve-button:disabled {
  cursor: wait;
  opacity: 0.55;
}

.reservation-error {
  padding: 12px 14px;
  border:
    1px solid rgba(255, 180, 180, 0.35);
  border-radius: 9px;
  background:
    rgba(140, 30, 30, 0.2);
  color: #ffd7d7;
  font-size: 0.85rem;
  line-height: 1.5;
}

.success-box {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 28px 0 18px;
  padding: 16px;
  border:
    1px solid rgba(255, 255, 255, 0.18);
  border-radius: 10px;
}

.success-label {
  color: rgba(255, 255, 255, 0.55);
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.1em;
  text-transform: uppercase;
}

.confirmation-note {
  color: rgba(255, 255, 255, 0.65);
  font-size: 0.9rem;
}

.secondary-button {
  margin-top: 12px;
}

/* PRIVATE EVENTS */

.private-events {
  padding: 100px 5%;
  background: #302a25;
  color: #ffffff;
}

.private-inner {
  max-width: 750px;
  margin: 0 auto;
  text-align: center;
}

.private-inner .eyebrow {
  color: #c7b29f;
}

.private-inner h2 {
  margin: 0 0 18px;
  font-size: clamp(
    2.3rem,
    5vw,
    4rem
  );
  line-height: 1;
  letter-spacing: -0.045em;
}

.private-inner > p {
  color: rgba(255, 255, 255, 0.72);
  line-height: 1.7;
}

.inquiry-button,
.back-button {
  display: inline-flex;
  margin-top: 20px;
  padding: 15px 24px;
  border-radius: 999px;
  background: #ffffff;
  color: #302a25;
  font-weight: 700;
}

.state-section {
  max-width: 900px;
  min-height: 600px;
  margin: 0 auto;
  padding: 120px 5%;
}

.state-section h1 {
  max-width: 700px;
  margin: 0 0 25px;
  font-size: clamp(
    2.8rem,
    6vw,
    5rem
  );
  line-height: 0.95;
  letter-spacing: -0.05em;
}

.state-section > p:not(.eyebrow) {
  max-width: 600px;
  color: #655d56;
  line-height: 1.7;
}

@media (max-width: 850px) {
  .details-grid {
    grid-template-columns:
      repeat(2, 1fr);
  }

  .detail:nth-child(2) {
    border-right: 0;
  }

  .event-body {
    grid-template-columns: 1fr;
    gap: 50px;
  }

  .reservation-card {
    max-width: 500px;
  }
}

@media (max-width: 600px) {
  .event-hero {
    padding: 65px 20px 70px;
  }

  .back-link {
    margin-bottom: 40px;
  }

  .event-information {
    padding: 55px 20px 75px;
  }

  .details-grid {
    grid-template-columns: 1fr;
    margin-bottom: 60px;
  }

  .detail {
    min-height: auto;
    border-right: 0;
    border-bottom:
      1px solid rgba(
        47,
        42,
        38,
        0.15
      );
  }

  .detail:last-child {
    border-bottom: 0;
  }

  .form-row {
    grid-template-columns: 1fr;
  }

  .private-events {
    padding: 75px 20px;
  }

  .state-section {
    padding: 80px 20px;
  }
}


/* THE REISTER'S DAUGHTER — BRANDING */

.event-detail-page {
  background: #FFF8F1;
  color: #1C1C1C;
}

.event-hero {
  background: #F6D5DF;
}

.eyebrow,
.detail-label,
.back-link {
  color: #A34366;
}

.details-grid {
  border-top-color: #E8B9C8;
  border-bottom-color: #E8B9C8;
}

.detail {
  border-right-color: #E8B9C8;
}

.reservation-card {
  background: #FFFFFF;
  color: #1C1C1C;
  border: 1px solid #E8B9C8;
  box-shadow: 0 12px 35px rgba(100, 40, 60, 0.07);
}

.reservation-card .eyebrow {
  color: #A34366;
}

.reservation-card > p {
  color: #625C52;
}

.reservation-form label > span {
  color: #453D39;
}

.reservation-form small {
  color: #766C66;
}

.reservation-form input,
.reservation-form textarea {
  background: #FFFFFF;
  border: 1px solid #DCC9CC;
  color: #1C1C1C;
}

.reservation-form input:focus,
.reservation-form textarea:focus {
  border-color: #C85F83;
  outline: 2px solid #C85F83;
  outline-offset: 1px;
}

.reservation-form textarea::placeholder {
  color: #817771;
}

.reserve-button,
.secondary-button {
  background: #C85F83;
  color: #FFFFFF;
  border-radius: 999px;
}

.reservation-error {
  background: #FBE9EC;
  border-color: #E8B9C8;
  color: #8B283F;
}

.success-box {
  background: #FFF8F1;
  border-color: #E8B9C8;
}

.success-label,
.confirmation-note {
  color: #625C52;
}

.private-events {
  background: #F6D5DF;
  color: #1C1C1C;
}

.private-inner .eyebrow {
  color: #A34366;
}

.private-inner > p {
  color: #625C52;
}

.inquiry-button,
.back-button {
  background: #C85F83;
  color: #FFFFFF;
}

@media (max-width: 600px) {
  .detail {
    border-right: 0;
    border-bottom-color: #E8B9C8;
  }
}
</style>


<style scoped>
/* EVENT SPLIT HERO V1 */

.split-event-hero {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  min-height: 610px;
  padding: 0;
  background: #F6D5DF;
  border-bottom: 1px solid rgba(47, 42, 38, 0.1);
}

.split-hero-content {
  display: flex;
  flex-direction: column;
  min-width: 0;
  padding: 52px clamp(28px, 5vw, 100px) 72px;
}

.split-hero-content .back-link {
  align-self: flex-start;
  margin-bottom: 65px;
  color: #A34366;
}

.split-hero-copy {
  max-width: 680px;
  margin: auto 0;
}

.split-hero-copy .eyebrow {
  color: #A34366;
}

.split-hero-copy h1 {
  overflow-wrap: anywhere;
  font-size: clamp(3.2rem, 6vw, 6.7rem);
  line-height: 0.98;
}

.split-hero-copy .hero-description {
  margin-top: 24px;
  font-size: 1.05rem;
  line-height: 1.65;
}

.hero-event-facts {
  display: grid;
  gap: 16px;
  margin-top: 32px;
  padding-top: 24px;
  border-top: 1px solid rgba(47, 42, 38, 0.18);
}

.hero-event-facts > div {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.hero-fact-label {
  color: #A34366;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
}

.hero-event-facts strong {
  font-size: 0.98rem;
  line-height: 1.45;
}

.hero-reserve-button {
  display: inline-flex;
  align-items: center;
  justify-content: space-between;
  gap: 30px;
  width: fit-content;
  margin-top: 30px;
  padding: 15px 24px;
  border-radius: 999px;
  background: #A34366;
  color: white;
  font-weight: 700;
  text-decoration: none;
  transition: background 0.2s ease;
}

.hero-reserve-button:hover {
  background: #82334f;
}

.split-hero-image {
  min-height: 100%;
  overflow: hidden;
}

.split-hero-image img {
  display: block;
  width: 100%;
  height: 100%;
  min-height: 610px;
  object-fit: cover;
}

.split-event-hero.without-cover {
  grid-template-columns: 1fr;
  min-height: 440px;
}

.without-cover .split-hero-content {
  max-width: 1100px;
  width: 100%;
  margin: 0 auto;
  box-sizing: border-box;
}

#event-reservations {
  scroll-margin-top: 110px;
}

@media (max-width: 850px) {
  .split-event-hero {
    grid-template-columns: 1fr;
  }

  .split-hero-content {
    padding: 36px 28px 48px;
  }

  .split-hero-content .back-link {
    margin-bottom: 38px;
  }

  .split-hero-image {
    height: 360px;
  }

  .split-hero-image img {
    min-height: 0;
    height: 100%;
  }
}

@media (max-width: 600px) {
  .split-hero-content {
    padding: 30px 20px 42px;
  }

  .split-hero-copy h1 {
    font-size: clamp(3rem, 12vw, 4.5rem);
  }

  .split-hero-image {
    height: 290px;
  }

  .hero-reserve-button {
    width: 100%;
    box-sizing: border-box;
  }
}

@media (prefers-reduced-motion: no-preference) {
  html {
    scroll-behavior: smooth;
  }
}
</style>
