<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { apiFetch } from '@/services/api'

interface Applicant {
  id: number
  createdAt: string
  status: string
  position: string
  firstName: string
  lastName: string
  email: string
  phone: string
  availableStartDate: string | null
}

const applicants = ref<Applicant[]>([])
const loading = ref(true)
const error = ref('')
const statusFilter = ref('')
const positionFilter = ref('')

const statuses = [
  'New', 'Reviewing', 'Interviewing',
  'Offered', 'Hired', 'Rejected',
]

const positions = ['Barista', 'Baker', 'Cashier']

const filtered = computed(() =>
  applicants.value.filter(a =>
    (!statusFilter.value || a.status === statusFilter.value) &&
    (!positionFilter.value || a.position === positionFilter.value),
  ),
)

async function loadApplicants() {
  loading.value = true
  error.value = ''

  try {
    const response = await apiFetch('/api/jobs/applications')
    if (!response.ok) throw new Error(`HTTP ${response.status}`)
    applicants.value = await response.json() as Applicant[]
  } catch (e) {
    console.error(e)
    error.value = 'Could not load job applications.'
  } finally {
    loading.value = false
  }
}

onMounted(() => { void loadApplicants() })
</script>

<template>
  <section class="applicants-page">
    <div class="applicants-heading">
      <div>
        <h1>Job Applicants</h1>
        <p>Review applications for The Reister's Daughter.</p>
      </div>
      <button type="button" @click="loadApplicants">Refresh</button>
    </div>

    <div class="applicants-filters">
      <label>
        Position
        <select v-model="positionFilter">
          <option value="">All positions</option>
          <option v-for="position in positions" :key="position">
            {{ position }}
          </option>
        </select>
      </label>
      <label>
        Status
        <select v-model="statusFilter">
          <option value="">All statuses</option>
          <option v-for="status in statuses" :key="status">
            {{ status }}
          </option>
        </select>
      </label>
    </div>

    <p v-if="loading">Loading applicants...</p>
    <p v-else-if="error" role="alert">{{ error }}</p>
    <p v-else-if="filtered.length === 0">No matching applicants.</p>

    <div v-else class="applicants-list">
      <RouterLink
        v-for="applicant in filtered"
        :key="applicant.id"
        :to="`/admin/applicants/${applicant.id}`"
        class="applicant-card"
      >
        <div>
          <strong>{{ applicant.firstName }} {{ applicant.lastName }}</strong>
          <p>{{ applicant.position }} · {{ applicant.email }}</p>
        </div>
        <div class="applicant-meta">
          <span class="applicant-status">{{ applicant.status }}</span>
          <small>{{ new Date(applicant.createdAt).toLocaleDateString() }}</small>
        </div>
      </RouterLink>
    </div>
  </section>
</template>

<style scoped>
.applicants-page { padding: 1.5rem; max-width: 1100px; margin: auto; }
.applicants-heading { display: flex; justify-content: space-between; gap: 1rem; align-items: center; }
.applicants-heading h1 { margin: 0; }
.applicants-heading p { color: #666; }
.applicants-heading button { padding: .65rem 1rem; cursor: pointer; }
.applicants-filters { display: flex; flex-wrap: wrap; gap: 1rem; margin: 1.5rem 0; }
.applicants-filters label { display: grid; gap: .4rem; font-weight: 600; }
.applicants-filters select { padding: .65rem; min-width: 180px; }
.applicants-list { display: grid; gap: .8rem; }
.applicant-card { display: flex; justify-content: space-between; gap: 1rem; align-items: center; padding: 1.2rem; background: #fff; border: 1px solid #e5e5e5; border-radius: 12px; text-decoration: none; color: #333; }
.applicant-card:hover { border-color: #999; }
.applicant-card p { margin: .4rem 0 0; color: #666; }
.applicant-meta { display: grid; gap: .5rem; text-align: right; }
.applicant-status { font-weight: 600; }

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
