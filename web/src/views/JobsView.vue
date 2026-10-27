<script setup lang="ts">
import { computed, ref } from 'vue'
import { API_BASE } from '@/config/api'

const positions = [
  {
    name: 'Barista',
    description: 'Craft great coffee, connect with guests, and help create a welcoming café experience.',
  },
  {
    name: 'Baker',
    description: 'Help prepare fresh baked goods and bring care, consistency, and creativity to our kitchen.',
  },
  {
    name: 'Cashier',
    description: 'Welcome our guests, take orders, and keep the café experience running smoothly.',
  },
]

const position = ref('')
const firstName = ref('')
const lastName = ref('')
const email = ref('')
const phone = ref('')
const availableEarlyMornings = ref(false)
const availableWeekends = ref(false)
const desiredWeeklyHours = ref('')
const availableStartDate = ref('')
const experience = ref('')
const hasFoodSafetyCertification = ref('')
const hasReliableTransportation = ref('')
const introduction = ref('')
const resumeFile = ref<File | null>(null)
const resumeError = ref('')
const maxResumeSize = 5 * 1024 * 1024

const submitting = ref(false)
const submitted = ref(false)
const errorMessage = ref('')

const canSubmit = computed(() =>
  position.value &&
  firstName.value.trim() &&
  lastName.value.trim() &&
  email.value.trim() &&
  phone.value.trim() &&
  desiredWeeklyHours.value &&
  experience.value.trim() &&
  introduction.value.trim(),
)

function handleResumeChange(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0] ?? null

  resumeFile.value = null
  resumeError.value = ''

  if (!file) return

  if (!/\.(pdf|doc|docx)$/i.test(file.name)) {
    resumeError.value = 'Please choose a PDF, DOC, or DOCX file.'
    input.value = ''
    return
  }

  if (file.size === 0 || file.size > maxResumeSize) {
    resumeError.value = 'Résumé must be smaller than 5 MB.'
    input.value = ''
    return
  }

  resumeFile.value = file
}

function optionalBoolean(value: string): boolean | null {
  if (value === 'yes') return true
  if (value === 'no') return false
  return null
}

function choosePosition(name: string) {
  position.value = name
  document.getElementById('job-application')?.scrollIntoView({
    behavior: 'smooth',
  })
}

async function submitApplication() {
  if (submitting.value || !canSubmit.value || resumeError.value) return

  submitting.value = true
  errorMessage.value = ''

  const application = {
    position: position.value,
    firstName: firstName.value.trim(),
    lastName: lastName.value.trim(),
    email: email.value.trim(),
    phone: phone.value.trim(),
    availableEarlyMornings: availableEarlyMornings.value,
    availableWeekends: availableWeekends.value,
    desiredWeeklyHours: desiredWeeklyHours.value,
    availableStartDate: availableStartDate.value || null,
    experience: experience.value.trim(),
    hasFoodSafetyCertification: optionalBoolean(
      hasFoodSafetyCertification.value,
    ),
    hasReliableTransportation: optionalBoolean(
      hasReliableTransportation.value,
    ),
    introduction: introduction.value.trim(),
  }

  try {
    const formData = new FormData()
    formData.append('application', JSON.stringify(application))

    if (resumeFile.value) {
      formData.append('resume', resumeFile.value)
    }

    const response = await fetch(
      `${API_BASE}/api/jobs/applications/upload`,
      {
        method: 'POST',
        body: formData,
      },
    )

    if (!response.ok) {
      throw new Error(`Application submission failed: ${response.status}`)
    }

    submitted.value = true
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } catch (error) {
    console.error('Application submission failed:', error)
    errorMessage.value =
      'We could not submit your application. Please try again.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <main class="jobs-page">
    <section class="jobs-hero">
      <div class="hero-content">
        <p class="eyebrow">Careers at The Reister's Daughter</p>
        <h1>Good people.<br /><em>Great coffee.</em></h1>
        <p class="hero-description">
          We're a locally owned café and bakery rooted in craft,
          community, and the simple joy of making someone's day.
          Interested in being part of the team?
        </p>
        <a class="primary-button" href="#open-roles">
          Explore opportunities
        </a>
      </div>
      <div class="hero-art" aria-hidden="true">
        <div class="art-circle">
          <span>Made with<br />care & coffee</span>
          <span class="art-heart">♥</span>
        </div>
      </div>
    </section>

    <section v-if="submitted" class="success-section" role="status">
      <div class="success-card">
        <span class="success-icon" aria-hidden="true">♥</span>
        <h2>Thank you for applying!</h2>
        <p>
          We've received your application. Our team will review it
          and reach out if there's a good match.
        </p>
      </div>
    </section>

    <template v-else>
      <section id="open-roles" class="roles-section">
        <div class="section-heading">
          <p class="eyebrow">Find your place</p>
          <h2>Join our team</h2>
          <p>
            We welcome applications from people who care about
            quality, teamwork, and creating a warm experience for guests.
          </p>
        </div>

        <div class="role-grid">
          <article
            v-for="role in positions"
            :key="role.name"
            class="role-card"
          >
            <span class="role-decoration" aria-hidden="true">✳</span>
            <h3>{{ role.name }}</h3>
            <p>{{ role.description }}</p>
            <button type="button" @click="choosePosition(role.name)">
              Apply for {{ role.name }} <span aria-hidden="true">→</span>
            </button>
          </article>
        </div>

        <p class="role-disclaimer">
          These are positions we accept applications for;
          current openings and hiring timelines may vary.
        </p>
      </section>

      <section class="culture-section">
        <div>
          <p class="eyebrow">Our little corner of Reisterstown</p>
          <h2>Locally owned.<br />Community focused.<br /><em>Rooted in craft.</em></h2>
        </div>
        <p>
          Every cup, pastry, and conversation matters.
          We value reliability, kindness, curiosity, and the
          willingness to learn. Whether you're experienced or
          starting your journey, we'd love to hear from you.
        </p>
      </section>

      <section id="job-application" class="application-section">
        <div class="application-intro">
          <p class="eyebrow">Take the next step</p>
          <h2>Tell us about yourself.</h2>
          <p>
            Complete the form below to share your interest
            in joining The Reister's Daughter.
          </p>
          <p class="privacy-note">
            Your information is used to review your application
            and contact you about employment opportunities.
          </p>
        </div>

        <form class="application-form" @submit.prevent="submitApplication">
          <h3>Application form</h3>
          <p class="required-note">Fields marked * are required.</p>

          <div class="form-field">
            <label for="job-position">Position of interest *</label>
            <select id="job-position" v-model="position" required>
              <option value="" disabled>Select a position</option>
              <option v-for="role in positions" :key="role.name">
                {{ role.name }}
              </option>
            </select>
          </div>

          <div class="form-row">
            <div class="form-field">
              <label for="job-first-name">First name *</label>
              <input id="job-first-name" v-model.trim="firstName"
                autocomplete="given-name" maxlength="100" required />
            </div>
            <div class="form-field">
              <label for="job-last-name">Last name *</label>
              <input id="job-last-name" v-model.trim="lastName"
                autocomplete="family-name" maxlength="100" required />
            </div>
          </div>

          <div class="form-row">
            <div class="form-field">
              <label for="job-email">Email *</label>
              <input id="job-email" v-model.trim="email" type="email"
                autocomplete="email" maxlength="254" required />
            </div>
            <div class="form-field">
              <label for="job-phone">Phone *</label>
              <input id="job-phone" v-model.trim="phone" type="tel"
                autocomplete="tel" maxlength="40" required />
            </div>
          </div>

          <h4>Availability</h4>
          <div class="form-row">
            <div class="form-field">
              <label for="job-hours">Desired weekly hours *</label>
              <select id="job-hours" v-model="desiredWeeklyHours" required>
                <option value="" disabled>Select hours</option>
                <option>Under 15 hours</option>
                <option>15–24 hours</option>
                <option>25–34 hours</option>
                <option>35+ hours</option>
                <option>Flexible</option>
              </select>
            </div>
            <div class="form-field">
              <label for="job-start">Available start date</label>
              <input id="job-start" v-model="availableStartDate" type="date" />
            </div>
          </div>

          <div class="checkbox-group">
            <label>
              <input v-model="availableEarlyMornings" type="checkbox" />
              Available for early mornings
            </label>
            <label>
              <input v-model="availableWeekends" type="checkbox" />
              Available on weekends
            </label>
          </div>

          <h4>Experience & background</h4>
          <div class="form-field">
            <label for="job-experience">Relevant experience *</label>
            <textarea id="job-experience" v-model.trim="experience"
              rows="5" maxlength="5000" required
              placeholder="Tell us about your previous work, customer service, baking, coffee, or other relevant experience." />
          </div>

          <div class="form-row">
            <div class="form-field">
              <label for="job-food-safety">Food safety certification</label>
              <select id="job-food-safety" v-model="hasFoodSafetyCertification">
                <option value="">Prefer not to say</option>
                <option value="yes">Yes</option>
                <option value="no">No</option>
              </select>
            </div>
            <div class="form-field">
              <label for="job-transport">Reliable transportation</label>
              <select id="job-transport" v-model="hasReliableTransportation">
                <option value="">Prefer not to say</option>
                <option value="yes">Yes</option>
                <option value="no">No</option>
              </select>
            </div>
          </div>

          <div class="form-field">
            <label for="job-introduction">Why would you like to join us? *</label>
            <textarea id="job-introduction" v-model.trim="introduction"
              rows="5" maxlength="5000" required
              placeholder="Introduce yourself and tell us what interests you about our café." />
          </div>

          <div class="form-field">
            <label for="job-resume">Upload résumé (optional)</label>
            <input
              id="job-resume"
              type="file"
              accept=".pdf,.doc,.docx"
              aria-describedby="job-resume-help"
              @change="handleResumeChange"
            />
            <small id="job-resume-help">
              PDF, DOC, or DOCX — maximum 5 MB.
            </small>
            <small v-if="resumeFile">
              Selected: {{ resumeFile.name }}
            </small>
            <p v-if="resumeError" class="form-error" role="alert">
              {{ resumeError }}
            </p>
          </div>

          <p v-if="errorMessage" class="form-error" role="alert">
            {{ errorMessage }}
          </p>

          <button class="submit-button" type="submit"
            :disabled="submitting || !canSubmit || !!resumeError">
            {{ submitting ? 'Sending application...' : 'Submit application' }}
          </button>
          <p class="form-footnote">
            Please do not include sensitive personal information,
            such as Social Security numbers, in your responses.
          </p>
        </form>
      </section>
    </template>
  </main>
</template>

<style scoped>
.jobs-page {
  --cream: #FFF8F1;
  --blush: #F6D5DF;
  --rose: #C85F83;
  --espresso: #392D2A;
  --border: #E9D9D9;
  color: var(--espresso);
  background: var(--cream);
}
.jobs-hero {
  min-height: 570px;
  display: grid;
  grid-template-columns: 1.15fr .85fr;
  align-items: center;
  gap: 3rem;
  padding: 6rem max(6%, calc((100vw - 1250px)/2));
  background: linear-gradient(115deg, #FFF8F1 5%, #FCE9EB 100%);
}
.hero-content { max-width: 650px; }
.eyebrow {
  color: var(--rose);
  text-transform: uppercase;
  font-size: .8rem;
  font-weight: 800;
  letter-spacing: .16em;
}
.jobs-hero h1 {
  font-size: clamp(3.2rem, 6.2vw, 6.5rem);
  letter-spacing: -.055em;
  line-height: 1.05;
  margin: 1.2rem 0;
}
.jobs-hero h1 em, .culture-section h2 em {
  font-family: Georgia, serif;
  font-weight: 400;
  color: var(--rose);
}
.hero-description {
  max-width: 520px;
  line-height: 1.85;
  font-size: 1.1rem;
  margin-bottom: 2rem;
}
.primary-button, .submit-button {
  display: inline-flex;
  justify-content: center;
  align-items: center;
  border: 0;
  border-radius: 999px;
  padding: 1rem 2rem;
  background: var(--rose);
  color: white;
  font-weight: 700;
  text-decoration: none;
  cursor: pointer;
}
.primary-button:hover, .submit-button:hover:not(:disabled) {
  background: #A84668;
}
.hero-art { display: flex; justify-content: center; align-items: center; }
.art-circle {
  width: min(34vw, 380px);
  aspect-ratio: 1;
  border-radius: 50%;
  background: var(--blush);
  border: 12px solid rgba(255,255,255,.7);
  box-shadow: 25px 30px 0 rgba(200,95,131,.10);
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  transform: rotate(9deg);
  text-align: center;
}
.art-circle span:first-child {
  font-family: Georgia, serif;
  font-style: italic;
  font-size: clamp(2rem, 3.8vw, 3.8rem);
  line-height: 1.15;
}
.art-heart { font-size: 3.5rem; color: var(--rose); margin-top: .8rem; }
.roles-section { padding: 6rem 6%; max-width: 1350px; margin: auto; }
.section-heading { max-width: 650px; margin-bottom: 3rem; }
.section-heading h2, .application-intro h2 {
  font-size: clamp(2.5rem, 4vw, 4.5rem);
  line-height: 1.1;
  letter-spacing: -.045em;
  margin: .7rem 0 1rem;
}
.section-heading > p:last-child { line-height: 1.8; }
.role-grid { display: grid; grid-template-columns: repeat(3, minmax(0,1fr)); gap: 1.4rem; }
.role-card {
  position: relative;
  display: flex;
  flex-direction: column;
  padding: 2.2rem;
  background: white;
  border: 1px solid var(--border);
  border-radius: 20px;
  min-height: 295px;
}
.role-decoration { color: var(--rose); font-size: 2.3rem; }
.role-card h3 { font-size: 1.8rem; margin: 1rem 0 .4rem; }
.role-card p { line-height: 1.7; flex: 1; }
.role-card button {
  background: none;
  border: 0;
  color: var(--rose);
  font: inherit;
  font-weight: 800;
  padding: 1rem 0 0;
  text-align: left;
  cursor: pointer;
}
.role-card button:hover { text-decoration: underline; }
.role-disclaimer { color: #685b59; margin-top: 1.5rem; font-size: .9rem; }
.culture-section {
  display: grid;
  grid-template-columns: 1fr 1fr;
  align-items: center;
  gap: 4rem;
  background: var(--blush);
  padding: 5.5rem 8%;
}
.culture-section h2 {
  font-size: clamp(2.2rem, 3.7vw, 4rem);
  letter-spacing: -.04em;
  line-height: 1.17;
}
.culture-section > p { font-size: 1.15rem; line-height: 2; max-width: 550px; }
.application-section {
  max-width: 1250px;
  margin: auto;
  padding: 6rem 6%;
  display: grid;
  grid-template-columns: .8fr 1.2fr;
  align-items: start;
  gap: 4rem;
}
.application-intro { position: sticky; top: 2rem; }
.application-intro > p { line-height: 1.8; }
.privacy-note { font-size: .9rem; color: #756767; }
.application-form {
  background: white;
  border: 1px solid var(--border);
  border-radius: 22px;
  padding: clamp(1.5rem, 3vw, 3rem);
  box-shadow: 0 12px 35px rgba(57,45,42,.05);
}
.application-form h3 { font-size: 1.7rem; margin-top: 0; }
.application-form h4 {
  font-size: 1.15rem;
  margin: 2rem 0 1rem;
  padding-bottom: .7rem;
  border-bottom: 1px solid var(--border);
}
.required-note, .form-footnote { color: #756767; font-size: .85rem; }
.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
.form-field { display: flex; flex-direction: column; gap: .5rem; margin-bottom: 1.3rem; min-width: 0; }
.form-field label { font-weight: 650; font-size: .93rem; }
.form-field input, .form-field select, .form-field textarea {
  box-sizing: border-box;
  width: 100%;
  padding: .9rem 1rem;
  border: 1px solid #DCCFCF;
  border-radius: 9px;
  background: #FFFEFC;
  color: var(--espresso);
  font: inherit;
}
.form-field textarea { resize: vertical; }
.form-field input:focus-visible, .form-field select:focus-visible,
.form-field textarea:focus-visible, .submit-button:focus-visible {
  outline: 2px solid var(--rose);
  outline-offset: 2px;
}
.checkbox-group { display: flex; flex-wrap: wrap; gap: 1.3rem; }
.checkbox-group label { display: flex; align-items: center; gap: .5rem; cursor: pointer; }
.checkbox-group input { accent-color: var(--rose); width: 18px; height: 18px; }
.submit-button { width: 100%; margin-top: 1.5rem; }
.submit-button:disabled { opacity: .55; cursor: not-allowed; }
.form-error { background: #FFF0F0; color: #9B2333; padding: 1rem; border-radius: 9px; }
.success-section { padding: 6rem 1.5rem; }
.success-card { max-width: 650px; margin: auto; text-align: center; background: white; padding: 4rem 2rem; border-radius: 22px; }
.success-card h2 { font-size: 2.5rem; }
.success-icon { color: var(--rose); font-size: 3rem; }
@media (max-width: 900px) {
  .jobs-hero { grid-template-columns: 1fr; padding: 4rem 6%; }
  .hero-art { display: none; }
  .role-grid { grid-template-columns: 1fr; }
  .culture-section { grid-template-columns: 1fr; gap: 1rem; }
  .application-section { grid-template-columns: 1fr; gap: 2rem; }
  .application-intro { position: static; }
}
@media (max-width: 550px) {
  .jobs-hero h1 { font-size: 3.2rem; }
  .form-row { grid-template-columns: 1fr; gap: 0; }
  .roles-section, .application-section { padding: 4rem 5%; }
  .culture-section { padding: 4rem 6%; }
}
@media (prefers-reduced-motion: reduce) {
  .jobs-page * { scroll-behavior: auto; }
}
</style>
