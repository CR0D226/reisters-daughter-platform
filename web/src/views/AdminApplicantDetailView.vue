<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { apiFetch } from '@/services/api'

interface ApplicationNote {
  id: number
  body: string
  createdAt: string
}

interface ApplicationActivity {
  id: number
  type: string
  description: string
  createdAt: string
}

interface Application {
  id: number
  createdAt: string
  status: string
  position: string
  firstName: string
  lastName: string
  email: string
  phone: string
  availableEarlyMornings: boolean
  availableWeekends: boolean
  desiredWeeklyHours: string
  availableStartDate: string | null
  experience: string
  hasFoodSafetyCertification: boolean | null
  hasReliableTransportation: boolean | null
  introduction: string
  resumeOriginalFileName: string | null
  notes: ApplicationNote[]
  activities: ApplicationActivity[]
}

const route = useRoute()

const application = ref<Application | null>(null)
const loading = ref(true)
const saving = ref(false)
const downloadingResume = ref(false)
const error = ref('')
const success = ref('')
const noteBody = ref('')
const selectedStatus = ref('')

const statuses = [
  'New',
  'Reviewing',
  'Interviewing',
  'Offered',
  'Hired',
  'Rejected',
]

function formatDate(value: string | null) {
  if (!value) return 'Not specified'
  return new Date(value).toLocaleDateString()
}

function yesNo(value: boolean | null) {
  if (value === null) return 'Not specified'
  return value ? 'Yes' : 'No'
}

async function loadApplication() {
  loading.value = true
  error.value = ''

  try {
    const id = Number(route.params.id)

    if (!Number.isInteger(id) || id <= 0) {
      throw new Error('Invalid applicant ID.')
    }

    const response = await apiFetch(
      `/api/jobs/applications/${id}`,
    )

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`)
    }

    application.value = await response.json() as Application
    selectedStatus.value = application.value.status
  } catch (e) {
    console.error(e)
    error.value = 'Could not load this application.'
  } finally {
    loading.value = false
  }
}

async function updateStatus() {
  if (!application.value || saving.value) return

  const id = application.value.id
  const previousStatus = application.value.status

  if (selectedStatus.value === previousStatus) return

  saving.value = true
  error.value = ''
  success.value = ''

  try {
    const response = await apiFetch(
      `/api/jobs/applications/${id}/status`,
      {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          status: selectedStatus.value,
        }),
      },
    )

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`)
    }

    await loadApplication()
    success.value = 'Application status updated.'
  } catch (e) {
    console.error(e)
    selectedStatus.value = previousStatus
    error.value = 'Could not update the application status.'
  } finally {
    saving.value = false
  }
}

async function downloadResume() {
  if (!application.value?.resumeOriginalFileName ||
      downloadingResume.value) return

  downloadingResume.value = true
  error.value = ''

  try {
    const response = await apiFetch(
      `/api/jobs/applications/${application.value.id}/resume`,
    )

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`)
    }

    const blob = await response.blob()
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')

    link.href = url
    link.download = application.value.resumeOriginalFileName
    document.body.appendChild(link)
    link.click()
    link.remove()

    window.setTimeout(() => URL.revokeObjectURL(url), 1000)
  } catch (e) {
    console.error(e)
    error.value = 'Could not download the résumé.'
  } finally {
    downloadingResume.value = false
  }
}

async function addNote() {
  if (!application.value || saving.value) return

  const body = noteBody.value.trim()

  if (!body || body.length > 5000) {
    error.value = 'Note must be between 1 and 5000 characters.'
    return
  }

  saving.value = true
  error.value = ''
  success.value = ''

  try {
    const response = await apiFetch(
      `/api/jobs/applications/${application.value.id}/notes`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ body }),
      },
    )

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`)
    }

    noteBody.value = ''
    await loadApplication()
    success.value = 'Private note added.'
  } catch (e) {
    console.error(e)
    error.value = 'Could not save the private note.'
  } finally {
    saving.value = false
  }
}

onMounted(() => { void loadApplication() })

watch(
  () => route.params.id,
  () => { void loadApplication() },
)
</script>

<template>
  <section class="applicant-detail">
    <RouterLink to="/admin/applicants" class="back-link">
      ← All applicants
    </RouterLink>

    <p v-if="loading">Loading application...</p>
    <p v-else-if="error && !application" role="alert">{{ error }}</p>

    <template v-else-if="application">
      <header class="detail-header">
        <div>
          <h1>
            {{ application.firstName }}
            {{ application.lastName }}
          </h1>
          <p>
            {{ application.position }} ·
            Applied {{ formatDate(application.createdAt) }}
          </p>
        </div>
        <span class="status-badge">{{ application.status }}</span>
      </header>

      <p v-if="error" class="message error" role="alert">{{ error }}</p>
      <p v-if="success" class="message success" role="status">{{ success }}</p>

      <div class="detail-grid">
        <div class="detail-main">
          <section class="detail-card">
            <h2>Applicant information</h2>
            <dl>
              <dt>Email</dt>
              <dd><a :href="`mailto:${application.email}`">{{ application.email }}</a></dd>
              <dt>Phone</dt>
              <dd>{{ application.phone }}</dd>
              <dt>Position</dt>
              <dd>{{ application.position }}</dd>
              <dt>Available start date</dt>
              <dd>{{ application.availableStartDate || 'Not specified' }}</dd>
              <dt>Desired weekly hours</dt>
              <dd>{{ application.desiredWeeklyHours || 'Not specified' }}</dd>
              <dt>Early mornings</dt>
              <dd>{{ yesNo(application.availableEarlyMornings) }}</dd>
              <dt>Weekends</dt>
              <dd>{{ yesNo(application.availableWeekends) }}</dd>
              <dt>Food safety certification</dt>
              <dd>{{ yesNo(application.hasFoodSafetyCertification) }}</dd>
              <dt>Reliable transportation</dt>
              <dd>{{ yesNo(application.hasReliableTransportation) }}</dd>
            </dl>
          </section>

          <section class="detail-card">
            <h2>Résumé</h2>

            <template v-if="application.resumeOriginalFileName">
              <p>{{ application.resumeOriginalFileName }}</p>
              <button
                type="button"
                :disabled="downloadingResume"
                @click="downloadResume"
              >
                {{ downloadingResume ? 'Downloading...' : 'Download résumé' }}
              </button>
            </template>

            <p v-else>No résumé uploaded.</p>
          </section>

          <section class="detail-card">
            <h2>Experience</h2>
            <p class="multiline">{{ application.experience || 'Not provided' }}</p>
            <h2>About the applicant</h2>
            <p class="multiline">{{ application.introduction || 'Not provided' }}</p>
          </section>

          <section class="detail-card">
            <h2>Private manager notes</h2>
            <form @submit.prevent="addNote">
              <label for="applicant-note">Add a note</label>
              <textarea
                id="applicant-note"
                v-model="noteBody"
                rows="4"
                maxlength="5000"
                placeholder="Interview feedback, follow-up details, etc."
              />
              <button type="submit" :disabled="saving || !noteBody.trim()">
                {{ saving ? 'Saving...' : 'Save private note' }}
              </button>
            </form>

            <p v-if="application.notes.length === 0">
              No notes yet.
            </p>
            <article
              v-for="note in application.notes"
              :key="note.id"
              class="history-item"
            >
              <small>{{ new Date(note.createdAt).toLocaleString() }}</small>
              <p class="multiline">{{ note.body }}</p>
            </article>
          </section>
        </div>

        <div class="detail-side">
          <section class="detail-card">
            <h2>Hiring status</h2>
            <label for="applicant-status">Application status</label>
            <select id="applicant-status" v-model="selectedStatus">
              <option v-for="status in statuses" :key="status">
                {{ status }}
              </option>
            </select>
            <button
              type="button"
              :disabled="saving || selectedStatus === application.status"
              @click="updateStatus"
            >
              {{ saving ? 'Saving...' : 'Update status' }}
            </button>
          </section>

          <section class="detail-card">
            <h2>Activity history</h2>
            <p v-if="application.activities.length === 0">
              No activity recorded.
            </p>
            <article
              v-for="activity in application.activities"
              :key="activity.id"
              class="history-item"
            >
              <small>{{ new Date(activity.createdAt).toLocaleString() }}</small>
              <p>{{ activity.description }}</p>
            </article>
          </section>
        </div>
      </div>
    </template>
  </section>
</template>

<style scoped>
.applicant-detail { max-width: 1200px; margin: auto; padding: 1.5rem; color: #333; }
.back-link { display: inline-block; margin-bottom: 1.25rem; }
.detail-header { display: flex; justify-content: space-between; align-items: center; gap: 1rem; margin-bottom: 1.5rem; }
.detail-header h1 { margin: 0; }
.detail-header p { color: #666; }
.status-badge { background: #f0f0f0; padding: .5rem .9rem; border-radius: 999px; font-weight: 600; }
.detail-grid { display: grid; grid-template-columns: minmax(0, 2fr) minmax(260px, 1fr); gap: 1.25rem; }
.detail-main, .detail-side { display: grid; align-content: start; gap: 1.25rem; }
.detail-card { padding: 1.5rem; border: 1px solid #e5e5e5; background: #fff; border-radius: 12px; min-width: 0; }
.detail-card h2 { margin-top: 0; font-size: 1.2rem; }
.detail-card dl { display: grid; grid-template-columns: 180px 1fr; gap: .8rem; }
.detail-card dt { font-weight: 600; }
.detail-card dd { margin: 0; overflow-wrap: anywhere; }
.detail-card label { display: block; margin-bottom: .5rem; font-weight: 600; }
.detail-card textarea, .detail-card select { width: 100%; padding: .75rem; border: 1px solid #ccc; border-radius: 7px; margin-bottom: .75rem; box-sizing: border-box; }
.detail-card button { padding: .7rem 1rem; cursor: pointer; }
.detail-card button:disabled { opacity: .5; cursor: not-allowed; }
.history-item { padding: .85rem 0; border-top: 1px solid #eee; }
.history-item small { color: #777; }
.multiline { white-space: pre-wrap; overflow-wrap: anywhere; }
.message { padding: .8rem; border-radius: 7px; }
.error { background: #fff0f0; }
.success { background: #effaf0; }
@media (max-width: 800px) {
  .detail-grid { grid-template-columns: 1fr; }
  .detail-card dl { grid-template-columns: 1fr; gap: .3rem; }
  .detail-card dd { margin-bottom: .7rem; }
}

/* The Reister's Daughter — Recruitment Branding */
.applicants-page,
.applicant-detail {
  --rd-blush: #F6D5DF;
  --rd-rose: #C85F83;
  --rd-espresso: #392D2A;
  --rd-cream: #FFF8F1;
  --rd-border: #EADDE0;
  color: var(--rd-espresso);
}

.applicants-page h1,
.applicant-detail h1,
.applicant-detail h2 {
  color: var(--rd-espresso);
  letter-spacing: -0.025em;
}

.applicants-page,
.applicant-detail {
  background: var(--rd-cream);
  border-radius: 18px;
  min-height: 70vh;
}

.applicants-heading {
  padding-bottom: 1.2rem;
  border-bottom: 1px solid var(--rd-border);
}

.applicants-heading button,
.detail-card button {
  background: var(--rd-rose);
  color: white;
  border: 1px solid var(--rd-rose);
  border-radius: 8px;
  font-weight: 600;
  transition: background 150ms ease, transform 150ms ease;
}

.applicants-heading button:hover,
.detail-card button:not(:disabled):hover {
  background: #A84668;
}

.applicants-filters select,
.detail-card select,
.detail-card textarea {
  border: 1px solid var(--rd-border);
  background: #fff;
  color: var(--rd-espresso);
}

.applicants-filters select:focus,
.detail-card select:focus,
.detail-card textarea:focus {
  outline: 2px solid var(--rd-rose);
  outline-offset: 2px;
}

.applicant-card,
.detail-card {
  border-color: var(--rd-border);
  box-shadow: 0 3px 12px rgba(57,45,42,.04);
}

.applicant-card {
  transition: border-color 150ms ease, box-shadow 150ms ease;
}

.applicant-card:hover {
  border-color: var(--rd-rose);
  box-shadow: 0 6px 20px rgba(200,95,131,.12);
}

.applicant-card strong {
  color: var(--rd-espresso);
}

.applicant-status,
.status-badge {
  background: var(--rd-blush);
  color: #843650;
  border-radius: 999px;
  padding: .4rem .8rem;
  font-weight: 700;
}

.back-link {
  color: var(--rd-rose);
  font-weight: 600;
  text-decoration: none;
}

.back-link:hover {
  text-decoration: underline;
}

.history-item {
  border-top-color: var(--rd-border);
}

.detail-card button:disabled {
  opacity: .55;
}

@media (prefers-reduced-motion: reduce) {
  .applicant-card,
  .applicants-heading button,
  .detail-card button {
    transition: none;
  }
}

</style>
