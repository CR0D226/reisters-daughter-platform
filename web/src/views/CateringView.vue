<script setup lang="ts">
import { computed, ref } from 'vue'

// --------------------------------------------------
// FORM STATE
// --------------------------------------------------

const eventType = ref('')
const services = ref<string[]>([])

const firstName = ref('')
const lastName = ref('')
const company = ref('')
const email = ref('')
const phone = ref('')

const eventDate = ref('')
const eventTime = ref('')
const guestCount = ref<number | null>(null)

const cateringType = ref('')
const serviceType = ref('')
const deliveryAddress = ref('')
const packaging = ref('')

const dietaryNeeds = ref<string[]>([])
const otherDietaryNeeds = ref('')

const recurring = ref('One-time')
const details = ref('')

const submitting = ref(false)
const successMessage = ref('')
const errorMessage = ref('')

// --------------------------------------------------
// CONDITIONAL FORM LOGIC
// --------------------------------------------------

const wantsCatering = computed(() => {
  return services.value.includes('Catering')
})

const wantsRental = computed(() => {
  return services.value.includes('Cafe Rental')
})

const isCorporate = computed(() => {
  return eventType.value === 'Corporate Event'
})

// --------------------------------------------------
// SUBMIT INQUIRY
// --------------------------------------------------

async function submitInquiry() {
  successMessage.value = ''
  errorMessage.value = ''
  submitting.value = true

  const inquiry = {
    eventType: eventType.value,
    services: services.value,

    firstName: firstName.value,
    lastName: lastName.value,
    company: company.value || null,

    email: email.value,
    phone: phone.value,

    eventDate: eventDate.value,
    eventTime: eventTime.value,
    guestCount: guestCount.value,

    cateringType: wantsCatering.value
      ? cateringType.value
      : null,

    serviceType: wantsCatering.value
      ? serviceType.value
      : null,

    deliveryAddress:
      wantsCatering.value &&
      serviceType.value === 'Delivery'
        ? deliveryAddress.value
        : null,

    packaging: wantsCatering.value
      ? packaging.value
      : null,

    dietaryNeeds: wantsCatering.value
      ? dietaryNeeds.value
      : [],

    otherDietaryNeeds: wantsCatering.value
      ? otherDietaryNeeds.value
      : null,

    recurring:
      wantsCatering.value && isCorporate.value
        ? recurring.value
        : null,

    details: details.value,
  }

  try {
    const response = await fetch(
      'http://localhost:5128/api/inquiries',
      {
        method: 'POST',

        headers: {
          'Content-Type': 'application/json',
        },

        body: JSON.stringify(inquiry),
      },
    )

    if (!response.ok) {
      throw new Error(
        `Server returned ${response.status}`,
      )
    }

    successMessage.value =
      "Thanks! We've received your inquiry and will follow up with you."

    console.log(
      'Inquiry submitted:',
      inquiry,
    )
  } catch (error) {
    console.error(
      'Inquiry submission failed:',
      error,
    )

    errorMessage.value =
      'We could not send your inquiry. Please try again.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <main class="catering-page">

    <!-- HERO -->

    <section class="catering-hero">
      <div class="hero-inner">
        <p class="eyebrow">
          Catering & Private Events
        </p>

        <h1>
          Good food. Good coffee.<br />
          Made for gathering.
        </h1>

        <p class="hero-copy">
          From office meetings and team breakfasts
          to birthdays and private gatherings,
          The Reister's Daughter can help make your
          event feel a little more special.
        </p>

        <a
          class="hero-button"
          href="#inquiry"
        >
          Start an Inquiry
        </a>
      </div>
    </section>


    <!-- SERVICES INTRO -->

    <section class="services-section">
      <div class="section-heading">
        <p class="eyebrow">
          Gather With Us
        </p>

        <h2>
          Catering for work, celebrations
          & everything in between.
        </h2>

        <p>
          Planning a meeting, office breakfast,
          birthday, shower, or private gathering?
          Tell us what you're planning and we'll
          help you figure out what works best.
        </p>
      </div>


      <!-- SERVICE ROW 1 -->

      <div class="service-row">
        <div class="service-number">
          01
        </div>

        <div class="service-content">
          <h3>
            Office & Corporate Catering
          </h3>

          <p>
            Coffee, pastries, breakfast and lunch
            options for meetings, trainings,
            team gatherings and workplace events.
          </p>
        </div>
      </div>


      <!-- SERVICE ROW 2 -->

      <div class="service-row">
        <div class="service-number">
          02
        </div>

        <div class="service-content">
          <h3>
            Celebrations & Private Gatherings
          </h3>

          <p>
            Hosting a birthday, shower,
            family gathering or special event?
            Send us the details and we'll help
            explore catering or café rental options.
          </p>
        </div>
      </div>
    </section>


    <!-- CATERING OPTIONS -->

    <section class="options-section">
      <div class="section-heading light">
        <p class="eyebrow">
          Catering Ideas
        </p>

        <h2>
          A few ways we can help.
        </h2>

        <p>
          Every event is different. These are
          examples of the kinds of catering
          requests we can discuss with you.
        </p>
      </div>

      <div class="option-grid">

        <article class="option-card">
          <span class="option-number">
            01
          </span>

          <h3>
            Coffee + Pastries
          </h3>

          <p>
            A simple option for morning meetings,
            office gatherings, presentations
            and casual events.
          </p>
        </article>


        <article class="option-card">
          <span class="option-number">
            02
          </span>

          <h3>
            Breakfast
          </h3>

          <p>
            Start the morning with café favorites
            for your team, guests or gathering.
          </p>
        </article>


        <article class="option-card">
          <span class="option-number">
            03
          </span>

          <h3>
            Lunch
          </h3>

          <p>
            Planning a midday meeting or event?
            Tell us what you need and we'll help
            explore available options.
          </p>
        </article>

      </div>
    </section>


    <!-- INQUIRY FORM -->

    <section
      id="inquiry"
      class="inquiry-section"
    >
      <div class="inquiry-layout">

        <!-- LEFT COLUMN -->

        <div class="inquiry-intro">
          <p class="eyebrow">
            Let's Plan Something
          </p>

          <h2>
            Tell us about your event.
          </h2>

          <p>
            Share a few details about what
            you're planning. We'll use this
            information to better understand
            your event and follow up with you.
          </p>
        </div>


        <!-- FORM -->

        <form
          class="inquiry-form"
          @submit.prevent="submitInquiry"
        >

          <!-- EVENT TYPE -->

          <fieldset>
            <legend>
              What are you planning?
            </legend>

            <div class="choice-grid">

              <label class="choice-card">
                <input
                  v-model="eventType"
                  type="radio"
                  name="eventType"
                  value="Corporate Event"
                  required
                />

                <span>
                  <strong>
                    Corporate Event
                  </strong>

                  <small>
                    Meetings, trainings
                    & team gatherings
                  </small>
                </span>
              </label>


              <label class="choice-card">
                <input
                  v-model="eventType"
                  type="radio"
                  name="eventType"
                  value="Birthday / Celebration"
                  required
                />

                <span>
                  <strong>
                    Birthday / Celebration
                  </strong>

                  <small>
                    Birthdays, showers
                    & special occasions
                  </small>
                </span>
              </label>


              <label class="choice-card">
                <input
                  v-model="eventType"
                  type="radio"
                  name="eventType"
                  value="Private Gathering"
                  required
                />

                <span>
                  <strong>
                    Private Gathering
                  </strong>

                  <small>
                    Family events, clubs
                    & get-togethers
                  </small>
                </span>
              </label>


              <label class="choice-card">
                <input
                  v-model="eventType"
                  type="radio"
                  name="eventType"
                  value="Cafe Rental Inquiry"
                  required
                />

                <span>
                  <strong>
                    Café Rental Inquiry
                  </strong>

                  <small>
                    Interested in hosting
                    something at our café
                  </small>
                </span>
              </label>


              <label class="choice-card">
                <input
                  v-model="eventType"
                  type="radio"
                  name="eventType"
                  value="Something Else"
                  required
                />

                <span>
                  <strong>
                    Something Else
                  </strong>

                  <small>
                    Tell us what you have
                    in mind
                  </small>
                </span>
              </label>

            </div>
          </fieldset>


          <!-- SERVICES -->

          <fieldset>
            <legend>
              How can we help?
            </legend>

            <p class="field-help">
              Select all that apply.
            </p>

            <div class="choice-grid service-choices">

              <label class="choice-card">
                <input
                  v-model="services"
                  type="checkbox"
                  value="Catering"
                />

                <span>
                  <strong>
                    Catering
                  </strong>

                  <small>
                    Food, coffee
                    & café favorites
                  </small>
                </span>
              </label>


              <label class="choice-card">
                <input
                  v-model="services"
                  type="checkbox"
                  value="Cafe Rental"
                />

                <span>
                  <strong>
                    Café Rental
                  </strong>

                  <small>
                    Ask about hosting
                    your gathering with us
                  </small>
                </span>
              </label>


              <label class="choice-card">
                <input
                  v-model="services"
                  type="checkbox"
                  value="Not Sure Yet"
                />

                <span>
                  <strong>
                    Not Sure Yet
                  </strong>

                  <small>
                    Help me figure
                    out the best option
                  </small>
                </span>
              </label>

            </div>
          </fieldset>


          <!-- CONTACT -->

          <fieldset>
            <legend>
              Your information
            </legend>

            <div class="form-grid">

              <label>
                First Name

                <input
                  v-model="firstName"
                  type="text"
                  autocomplete="given-name"
                  required
                />
              </label>


              <label>
                Last Name

                <input
                  v-model="lastName"
                  type="text"
                  autocomplete="family-name"
                  required
                />
              </label>


              <label
                v-if="isCorporate"
                class="full-width"
              >
                Company / Organization

                <input
                  v-model="company"
                  type="text"
                  autocomplete="organization"
                />
              </label>


              <label>
                Email

                <input
                  v-model="email"
                  type="email"
                  autocomplete="email"
                  required
                />
              </label>


              <label>
                Phone

                <input
                  v-model="phone"
                  type="tel"
                  autocomplete="tel"
                  required
                />
              </label>

            </div>
          </fieldset>


          <!-- EVENT DETAILS -->

          <fieldset>
            <legend>
              Event details
            </legend>

            <div class="form-grid">

              <label>
                Event Date

                <input
                  v-model="eventDate"
                  type="date"
                  required
                />
              </label>


              <label>
                Event Time

                <input
                  v-model="eventTime"
                  type="time"
                  required
                />
              </label>


              <label>
                Estimated Guest Count

                <input
                  v-model.number="guestCount"
                  type="number"
                  min="1"
                  placeholder="25"
                  required
                />
              </label>

            </div>
          </fieldset>


          <!-- CATERING DETAILS -->

          <fieldset
            v-if="wantsCatering"
            class="conditional-section"
          >
            <legend>
              Catering details
            </legend>


            <div class="form-grid">

              <label>
                What are you interested in?

                <select
                  v-model="cateringType"
                  required
                >
                  <option
                    value=""
                    disabled
                  >
                    Select an option
                  </option>

                  <option value="Coffee + Pastries">
                    Coffee + Pastries
                  </option>

                  <option value="Breakfast">
                    Breakfast
                  </option>

                  <option value="Lunch">
                    Lunch
                  </option>

                  <option value="Not Sure">
                    Not Sure Yet
                  </option>
                </select>
              </label>


              <label>
                Pickup or Delivery?

                <select
                  v-model="serviceType"
                  required
                >
                  <option
                    value=""
                    disabled
                  >
                    Select an option
                  </option>

                  <option value="Pickup">
                    Pickup
                  </option>

                  <option value="Delivery">
                    Delivery
                  </option>

                  <option value="Not Sure">
                    Not Sure Yet
                  </option>
                </select>
              </label>


              <label
                v-if="serviceType === 'Delivery'"
                class="full-width"
              >
                Delivery Address

                <input
                  v-model="deliveryAddress"
                  type="text"
                  autocomplete="street-address"
                  placeholder="Where would the order be delivered?"
                  required
                />
              </label>


              <label>
                Packaging Preference

                <select v-model="packaging">
                  <option value="">
                    No preference
                  </option>

                  <option value="Individual">
                    Individually Packaged
                  </option>

                  <option value="Group">
                    Group / Shared Setup
                  </option>

                  <option value="Not Sure">
                    Not Sure
                  </option>
                </select>
              </label>


              <label
                v-if="isCorporate"
              >
                Is this recurring?

                <select v-model="recurring">
                  <option value="One-time">
                    One-time
                  </option>

                  <option value="Weekly">
                    Weekly
                  </option>

                  <option value="Monthly">
                    Monthly
                  </option>

                  <option value="Occasionally">
                    Occasionally
                  </option>

                  <option value="Not Sure">
                    Not Sure
                  </option>
                </select>
              </label>

            </div>


            <!-- DIETARY -->

            <div class="dietary-section">
              <p class="field-label">
                Dietary needs
              </p>

              <p class="field-help">
                Select all that apply.
              </p>

              <div class="checkbox-row">

                <label>
                  <input
                    v-model="dietaryNeeds"
                    type="checkbox"
                    value="Vegetarian"
                  />

                  Vegetarian
                </label>


                <label>
                  <input
                    v-model="dietaryNeeds"
                    type="checkbox"
                    value="Vegan"
                  />

                  Vegan
                </label>


                <label>
                  <input
                    v-model="dietaryNeeds"
                    type="checkbox"
                    value="Gluten Free"
                  />

                  Gluten Free
                </label>


                <label>
                  <input
                    v-model="dietaryNeeds"
                    type="checkbox"
                    value="Other"
                  />

                  Other
                </label>

              </div>


              <label
                v-if="dietaryNeeds.includes('Other')"
                class="other-dietary"
              >
                Other dietary needs

                <input
                  v-model="otherDietaryNeeds"
                  type="text"
                  placeholder="Tell us more"
                />
              </label>

            </div>
          </fieldset>


          <!-- RENTAL -->

          <div
            v-if="wantsRental"
            class="rental-notice"
          >
            <strong>
              Interested in renting the café?
            </strong>

            <p>
              Great — include anything you already
              know about your gathering below,
              including the type of event and
              anything you'd like us to know.
            </p>
          </div>


          <!-- DETAILS -->

          <fieldset>
            <legend>
              Anything else we should know?
            </legend>

            <label>
              Event Details

              <textarea
                v-model="details"
                rows="6"
                placeholder="Tell us about your event, what you're looking for, or any questions you have."
                required
              ></textarea>
            </label>
          </fieldset>


          <!-- RESPONSE MESSAGES -->

          <div
            v-if="successMessage"
            class="form-message success-message"
            role="status"
          >
            {{ successMessage }}
          </div>

          <div
            v-if="errorMessage"
            class="form-message error-message"
            role="alert"
          >
            {{ errorMessage }}
          </div>


          <!-- SUBMIT -->

          <button
            class="submit-button"
            type="submit"
            :disabled="submitting"
          >
            {{
              submitting
                ? 'Sending...'
                : 'Send Inquiry'
            }}
          </button>

        </form>
      </div>
    </section>

  </main>
</template>

<style scoped>
.catering-page {
  background: #f7f4ee;
  color: #1d1d1b;
}


/* HERO */

.catering-hero {
  min-height: 560px;
  display: flex;
  align-items: center;
  background:
    linear-gradient(
      90deg,
      rgba(20, 20, 18, 0.88),
      rgba(20, 20, 18, 0.56)
    );
  color: white;
}

.hero-inner {
  width: min(1180px, calc(100% - 48px));
  margin: 0 auto;
  padding: 100px 0;
}

.eyebrow {
  margin: 0 0 18px;
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.16em;
  text-transform: uppercase;
}

.catering-hero .eyebrow {
  color: #e5c9a5;
}

.catering-hero h1 {
  max-width: 820px;
  margin: 0;
  font-size: clamp(3rem, 7vw, 6rem);
  line-height: 0.98;
  letter-spacing: -0.045em;
}

.hero-copy {
  max-width: 650px;
  margin: 28px 0 34px;
  font-size: 1.12rem;
  line-height: 1.7;
  color: rgba(255, 255, 255, 0.85);
}

.hero-button {
  display: inline-flex;
  padding: 15px 23px;
  border-radius: 999px;
  background: #f4eee3;
  color: #1d1d1b;
  font-weight: 700;
}


/* GENERAL */

.services-section,
.inquiry-section {
  width: min(1180px, calc(100% - 48px));
  margin: 0 auto;
  padding: 100px 0;
}

.section-heading {
  max-width: 760px;
  margin-bottom: 65px;
}

.section-heading h2,
.inquiry-intro h2 {
  margin: 0 0 20px;
  font-size: clamp(2.4rem, 5vw, 4.5rem);
  line-height: 1.02;
  letter-spacing: -0.04em;
}

.section-heading > p:last-child,
.inquiry-intro > p:last-child {
  font-size: 1.05rem;
  line-height: 1.75;
  color: #66635d;
}


/* SERVICES */

.service-row {
  display: grid;
  grid-template-columns: 100px 1fr;
  gap: 35px;
  padding: 42px 0;
  border-top: 1px solid #cec9bf;
}

.service-row:last-child {
  border-bottom: 1px solid #cec9bf;
}

.service-number {
  font-size: 0.8rem;
  font-weight: 700;
  letter-spacing: 0.12em;
  color: #817c72;
}

.service-content h3 {
  margin: 0 0 14px;
  font-size: 1.8rem;
}

.service-content p {
  max-width: 700px;
  margin: 0;
  line-height: 1.7;
  color: #66635d;
}


/* OPTIONS */

.options-section {
  padding: 100px max(24px, calc((100% - 1180px) / 2));
  background: #22221f;
  color: white;
}

.section-heading.light p {
  color: #c5c0b7;
}

.option-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 18px;
}

.option-card {
  min-height: 300px;
  padding: 32px;
  border: 1px solid #4a4944;
  border-radius: 16px;
  background: #2b2b27;
}

.option-number {
  display: block;
  margin-bottom: 80px;
  font-size: 0.75rem;
  letter-spacing: 0.14em;
  color: #a9a59d;
}

.option-card h3 {
  margin: 0 0 14px;
  font-size: 1.65rem;
}

.option-card p {
  margin: 0;
  line-height: 1.65;
  color: #c7c4bd;
}


/* INQUIRY */

.inquiry-layout {
  display: grid;
  grid-template-columns: 0.75fr 1.25fr;
  gap: 90px;
  align-items: start;
}

.inquiry-intro {
  position: sticky;
  top: 110px;
}

.inquiry-form {
  padding: 40px;
  border-radius: 18px;
  background: white;
  box-shadow:
    0 12px 40px rgba(0, 0, 0, 0.06);
}

fieldset {
  margin: 0 0 44px;
  padding: 0;
  border: 0;
}

legend {
  width: 100%;
  margin-bottom: 20px;
  padding: 0;
  font-size: 1.25rem;
  font-weight: 700;
}

.field-help {
  margin: -12px 0 18px;
  color: #77736c;
  font-size: 0.9rem;
}


/* CHOICE CARDS */

.choice-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
}

.choice-card {
  position: relative;
  cursor: pointer;
}

.choice-card input {
  position: absolute;
  opacity: 0;
  pointer-events: none;
}

.choice-card span {
  min-height: 100px;
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 6px;
  padding: 18px;
  border: 1px solid #d6d1c8;
  border-radius: 12px;
  transition:
    border-color 0.15s ease,
    background 0.15s ease,
    transform 0.15s ease;
}

.choice-card span:hover {
  border-color: #918b80;
  transform: translateY(-1px);
}

.choice-card input:checked + span {
  border-color: #24241f;
  background: #24241f;
  color: white;
}

.choice-card small {
  line-height: 1.4;
  color: #77736c;
}

.choice-card input:checked + span small {
  color: #d2cfc7;
}


/* INPUTS */

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 20px;
}

.form-grid label,
.inquiry-form > fieldset > label,
.other-dietary {
  display: flex;
  flex-direction: column;
  gap: 9px;
  font-size: 0.9rem;
  font-weight: 700;
}

.full-width {
  grid-column: 1 / -1;
}

input,
select,
textarea {
  width: 100%;
  border: 1px solid #cec9bf;
  border-radius: 9px;
  background: white;
  color: #1d1d1b;
  font: inherit;
}

.form-grid input,
.form-grid select,
.other-dietary input {
  min-height: 48px;
  padding: 0 13px;
}

textarea {
  padding: 14px;
  resize: vertical;
  line-height: 1.55;
}

input:focus,
select:focus,
textarea:focus {
  outline: 2px solid #24241f;
  outline-offset: 2px;
}


/* CONDITIONAL */

.conditional-section {
  padding: 28px;
  border-radius: 14px;
  background: #f4f0e8;
}

.dietary-section {
  margin-top: 28px;
}

.field-label {
  margin: 0 0 8px;
  font-size: 0.9rem;
  font-weight: 700;
}

.checkbox-row {
  display: flex;
  flex-wrap: wrap;
  gap: 12px 22px;
}

.checkbox-row label {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.9rem;
}

.checkbox-row input {
  width: auto;
}

.other-dietary {
  margin-top: 20px;
}


/* RENTAL */

.rental-notice {
  margin: 0 0 40px;
  padding: 22px;
  border-left: 4px solid #252520;
  background: #efebe3;
}

.rental-notice p {
  margin: 8px 0 0;
  line-height: 1.6;
  color: #66635d;
}


/* MESSAGES */

.form-message {
  margin-bottom: 20px;
  padding: 16px 18px;
  border-radius: 10px;
  line-height: 1.5;
}

.success-message {
  background: #e7f2e8;
  color: #25472c;
  border: 1px solid #bcd5c0;
}

.error-message {
  background: #f8e9e7;
  color: #6b2b24;
  border: 1px solid #e2bdb8;
}


/* SUBMIT */

.submit-button {
  width: 100%;
  min-height: 54px;
  border: 0;
  border-radius: 999px;
  background: #22221f;
  color: white;
  font-size: 1rem;
  font-weight: 700;
  cursor: pointer;
}

.submit-button:hover:not(:disabled) {
  opacity: 0.88;
}

.submit-button:disabled {
  cursor: wait;
  opacity: 0.6;
}


/* RESPONSIVE */

@media (max-width: 900px) {
  .option-grid {
    grid-template-columns: 1fr;
  }

  .inquiry-layout {
    grid-template-columns: 1fr;
    gap: 45px;
  }

  .inquiry-intro {
    position: static;
  }
}

@media (max-width: 650px) {
  .hero-inner,
  .services-section,
  .inquiry-section {
    width: min(100% - 30px, 1180px);
  }

  .catering-hero {
    min-height: 500px;
  }

  .hero-inner {
    padding: 75px 0;
  }

  .services-section,
  .inquiry-section,
  .options-section {
    padding-top: 70px;
    padding-bottom: 70px;
  }

  .service-row {
    grid-template-columns: 1fr;
    gap: 12px;
  }

  .choice-grid,
  .form-grid {
    grid-template-columns: 1fr;
  }

  .full-width {
    grid-column: auto;
  }

  .inquiry-form {
    padding: 24px 18px;
  }

  .conditional-section {
    padding: 20px 16px;
  }
}
</style>