<script setup lang="ts">
import {
  computed,
  onMounted,
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
}

const route = useRoute()

const event = ref<PublicEvent | null>(null)
const loading = ref(true)
const notFound = ref(false)
const errorMessage = ref('')

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
      `http://localhost:5128/api/public/events/${eventId.value}`,
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

      <!-- HERO -->

      <section class="event-hero">
        <div class="hero-inner">

          <RouterLink
            class="back-link"
            to="/events"
          >
            ← All Events
          </RouterLink>

          <p class="eyebrow">
            {{ event.type }}
          </p>

          <h1>
            {{ event.title }}
          </h1>

          <p
            v-if="event.description"
            class="hero-description"
          >
            {{ event.description }}
          </p>

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


          <aside class="reservation-card">
            <p class="eyebrow">
              Reservations
            </p>

            <h3>
              Interested in this event?
            </h3>

            <p>
              Online reservations and
              ticketing will be available
              here soon.
            </p>

            <div class="reservation-status">
              Reservations coming soon
            </div>
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
    minmax(0, 1fr) 380px;
  gap: 80px;
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

.reservation-card > p {
  color: rgba(255, 255, 255, 0.7);
  line-height: 1.6;
}

.reservation-status {
  margin-top: 28px;
  padding: 14px 16px;
  border:
    1px solid rgba(
      255,
      255,
      255,
      0.18
    );
  border-radius: 10px;
  color: rgba(255, 255, 255, 0.75);
  font-size: 0.85rem;
  font-weight: 700;
  text-align: center;
}

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

  .private-events {
    padding: 75px 20px;
  }

  .state-section {
    padding: 80px 20px;
  }
}
</style>