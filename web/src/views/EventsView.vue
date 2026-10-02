<script setup lang="ts">
import {
  computed,
  onMounted,
  ref,
} from 'vue'

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

const events = ref<PublicEvent[]>([])
const loading = ref(true)
const errorMessage = ref('')

const upcomingEvents = computed(() => {
  return [...events.value].sort((a, b) => {
    const aValue =
      `${a.startDate} ${a.startTime}`

    const bValue =
      `${b.startDate} ${b.startTime}`

    return aValue.localeCompare(bValue)
  })
})

async function loadEvents() {
  loading.value = true
  errorMessage.value = ''

  try {
    const response = await fetch(
      'http://localhost:5128/api/public/events',
    )

    if (!response.ok) {
      throw new Error(
        `Server returned ${response.status}`,
      )
    }

    events.value =
      (await response.json()) as PublicEvent[]
  } catch (error) {
    console.error(
      'Unable to load public events:',
      error,
    )

    errorMessage.value =
      'We could not load upcoming events right now.'
  } finally {
    loading.value = false
  }
}

function parseDate(value: string) {
  const [year, month, day] = value
    .split('-')
    .map(Number)

  return new Date(
    year ?? 0,
    (month ?? 1) - 1,
    day ?? 1,
  )
}

function formatMonth(value: string) {
  return parseDate(value)
    .toLocaleDateString('en-US', {
      month: 'short',
    })
    .toUpperCase()
}

function formatDay(value: string) {
  return parseDate(value)
    .toLocaleDateString('en-US', {
      day: 'numeric',
    })
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

  const [hours, minutes] = value
    .split(':')
    .map(Number)

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

function formatEventTime(
  event: PublicEvent,
) {
  const start =
    formatTime(event.startTime)

  const end =
    formatTime(event.endTime)

  if (!end) {
    return start
  }

  return `${start} – ${end}`
}

function isMultiDay(
  event: PublicEvent,
) {
  return Boolean(
    event.endDate &&
      event.endDate !== event.startDate,
  )
}

onMounted(loadEvents)
</script>

<template>
  <main class="events-page">

    <!-- HERO -->

    <section class="events-hero">
      <div class="hero-inner">
        <p class="eyebrow">
          Events at The Reister's Daughter
        </p>

        <h1>
          Gather. Learn.<br />
          Spend some time with us.
        </h1>

        <p class="hero-copy">
          High tea, classes, workshops,
          pop-ups and community gatherings
          happening right here in Reisterstown.
        </p>
      </div>
    </section>


    <!-- UPCOMING EVENTS -->

    <section class="events-section">
      <div class="section-heading">
        <p class="eyebrow">
          What's Happening
        </p>

        <h2>
          Upcoming Events
        </h2>

        <p>
          See what's coming up at
          The Reister's Daughter.
        </p>
      </div>


      <!-- LOADING -->

      <div
        v-if="loading"
        class="state-card"
      >
        Loading upcoming events...
      </div>


      <!-- ERROR -->

      <div
        v-else-if="errorMessage"
        class="state-card error-card"
      >
        <h3>
          Events are taking a coffee break.
        </h3>

        <p>
          {{ errorMessage }}
        </p>
      </div>


      <!-- EMPTY -->

      <div
        v-else-if="
          upcomingEvents.length === 0
        "
        class="state-card"
      >
        <h3>
          More events are coming soon.
        </h3>

        <p>
          Check back for upcoming High Teas,
          classes, workshops and other
          gatherings.
        </p>
      </div>


      <!-- EVENTS -->

      <div
        v-else
        class="event-list"
      >
        <article
          v-for="event in upcomingEvents"
          :key="event.id"
          class="event-card"
        >
          <div class="event-date">
            <span class="event-month">
              {{
                formatMonth(
                  event.startDate,
                )
              }}
            </span>

            <span class="event-day">
              {{
                formatDay(
                  event.startDate,
                )
              }}
            </span>
          </div>


          <div class="event-content">
            <p class="event-type">
              {{ event.type }}
            </p>

            <h3>
              {{ event.title }}
            </h3>

            <div class="event-details">
              <span>
                {{
                  formatDate(
                    event.startDate,
                  )
                }}
              </span>

              <span>
                {{
                  formatEventTime(
                    event,
                  )
                }}
              </span>

              <span
                v-if="event.location"
              >
                {{ event.location }}
              </span>
            </div>

            <p
              v-if="
                isMultiDay(event) &&
                event.endDate
              "
              class="multi-day"
            >
              Through
              {{
                formatDate(
                  event.endDate,
                )
              }}
            </p>

            <p
              v-if="event.description"
              class="event-description"
            >
              {{ event.description }}
            </p>

            <div class="event-footer">
              <span
                v-if="event.capacity"
                class="capacity"
              >
                Capacity:
                {{ event.capacity }}
              </span>

              <RouterLink
  class="event-link"
  :to="`/events/${event.id}`"
>
  Event Details →
</RouterLink>
            </div>
          </div>
        </article>
      </div>
    </section>


    <!-- PRIVATE EVENTS CTA -->

    <section class="private-events">
      <div class="private-inner">
        <p class="eyebrow">
          Planning Something?
        </p>

        <h2>
          Looking for catering or
          a private gathering?
        </h2>

        <p>
          Tell us what you're planning
          and we'll help you find the
          right food, coffee, and space.
        </p>

        <RouterLink
          class="inquiry-button"
          to="/catering"
        >
          Catering & Private Events
        </RouterLink>
      </div>
    </section>

  </main>
</template>

<style scoped>
.events-page {
  background: #f7f4ee;
  color: #2f2a26;
}

.events-hero {
  padding: 110px 5% 100px;
  border-bottom:
    1px solid rgba(47, 42, 38, 0.12);
}

.hero-inner {
  max-width: 900px;
}

.eyebrow {
  margin: 0 0 18px;
  color: #7b6755;
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.16em;
  text-transform: uppercase;
}

.events-hero h1 {
  max-width: 850px;
  margin: 0;
  font-size: clamp(
    3.2rem,
    7vw,
    6.8rem
  );
  line-height: 0.95;
  letter-spacing: -0.055em;
}

.hero-copy {
  max-width: 650px;
  margin: 32px 0 0;
  color: #655d56;
  font-size: 1.15rem;
  line-height: 1.7;
}

.events-section {
  max-width: 1200px;
  margin: 0 auto;
  padding: 100px 5%;
}

.section-heading {
  max-width: 700px;
  margin-bottom: 55px;
}

.section-heading h2,
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

.section-heading > p:last-child,
.private-inner > p {
  color: #6b625a;
  font-size: 1.05rem;
  line-height: 1.7;
}

.event-list {
  display: grid;
  gap: 24px;
}

.event-card {
  display: grid;
  grid-template-columns: 150px 1fr;
  overflow: hidden;
  border:
    1px solid rgba(47, 42, 38, 0.14);
  border-radius: 18px;
  background: #ffffff;
}

.event-date {
  display: flex;
  min-height: 260px;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  border-right:
    1px solid rgba(47, 42, 38, 0.12);
  background: #eee8de;
}

.event-month {
  color: #7b6755;
  font-size: 0.8rem;
  font-weight: 800;
  letter-spacing: 0.16em;
}

.event-day {
  margin-top: 4px;
  font-size: 3.8rem;
  font-weight: 700;
  line-height: 1;
  letter-spacing: -0.06em;
}

.event-content {
  padding: 38px 42px;
}

.event-type {
  margin: 0 0 8px;
  color: #8b725c;
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
}

.event-content h3 {
  margin: 0;
  font-size: 2rem;
  letter-spacing: -0.035em;
}

.event-details {
  display: flex;
  flex-wrap: wrap;
  gap: 10px 24px;
  margin-top: 18px;
  color: #5f5750;
  font-size: 0.95rem;
}

.event-description {
  max-width: 700px;
  margin: 25px 0 0;
  color: #625a53;
  line-height: 1.7;
}

.multi-day {
  margin: 12px 0 0;
  color: #625a53;
}

.event-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  margin-top: 30px;
  padding-top: 22px;
  border-top:
    1px solid rgba(47, 42, 38, 0.1);
}

.capacity {
  color: #625a53;
  font-size: 0.9rem;
}

.event-link {
  color: #8b725c;
  font-size: 0.82rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  transition: opacity 0.2s ease;
}

.event-link:hover {
  opacity: 0.55;
}

.state-card {
  padding: 45px;
  border:
    1px solid rgba(47, 42, 38, 0.14);
  border-radius: 18px;
  background: #ffffff;
}

.state-card h3 {
  margin: 0 0 10px;
  font-size: 1.5rem;
}

.state-card p {
  margin: 0;
  color: #655d56;
  line-height: 1.6;
}

.error-card {
  border-color:
    rgba(143, 72, 62, 0.3);
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

.private-inner > p {
  color: rgba(255, 255, 255, 0.72);
}

.inquiry-button {
  display: inline-flex;
  margin-top: 20px;
  padding: 15px 24px;
  border-radius: 999px;
  background: #ffffff;
  color: #302a25;
  font-weight: 700;
}

@media (max-width: 700px) {
  .events-hero {
    padding: 75px 20px;
  }

  .events-section {
    padding: 75px 20px;
  }

  .event-card {
    grid-template-columns: 1fr;
  }

  .event-date {
    min-height: auto;
    padding: 24px;
    flex-direction: row;
    gap: 8px;
    justify-content: flex-start;
    border-right: 0;
    border-bottom:
      1px solid rgba(
        47,
        42,
        38,
        0.12
      );
  }

  .event-day {
    margin: 0;
    font-size: 1.3rem;
  }

  .event-content {
    padding: 28px 24px;
  }

  .event-content h3 {
    font-size: 1.7rem;
  }

  .event-details {
    flex-direction: column;
    gap: 7px;
  }

  .event-footer {
    align-items: flex-start;
    flex-direction: column;
  }

  .private-events {
    padding: 75px 20px;
  }
}
</style>