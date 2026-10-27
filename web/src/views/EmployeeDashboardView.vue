<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { apiFetch } from '@/services/api'

interface AppUser {
  id: number
  firstName: string
  lastName: string
  email: string
  role: string
}

const user = ref<AppUser | null>(null)
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    const response = await apiFetch(
      '/api/auth/app-user',
    )

    if (!response.ok) {
      throw new Error('Unable to load employee profile.')
    }

    user.value = await response.json()
  } catch (err) {
    error.value =
      err instanceof Error
        ? err.message
        : 'Something went wrong.'
  } finally {
    loading.value = false
  }
})
</script>


<template>
  <section class="employee-dashboard">
    <header class="dashboard-heading">
      <p class="eyebrow">EMPLOYEE DASHBOARD</p>

      <h1 v-if="user">Welcome, {{ user.firstName }}</h1>
      <h1 v-else>Employee Portal</h1>

      <p v-if="loading" class="subtitle">Loading profile...</p>

      <p v-else-if="error" class="error" role="alert">
        {{ error }}
      </p>

      <p v-else-if="user" class="subtitle">
        Signed in as {{ user.email }} ({{ user.role }})
      </p>
    </header>

    <div v-if="user && !loading && !error" class="employee-grid">
      <article class="employee-card">
        <div class="card-icon">📅</div>
        <h2>My Schedule</h2>
        <p>View your assigned shifts and upcoming workdays.</p>

        <RouterLink to="/employee/schedule" class="card-link">
          View My Schedule
          <span aria-hidden="true">→</span>
        </RouterLink>
      </article>

      <article class="employee-card">
        <div class="card-icon">🕒</div>
        <h2>Availability</h2>
        <p>Submit your availability for upcoming scheduling periods.</p>

        <RouterLink
          to="/employee/biweekly-availability"
          class="card-link"
        >
          Submit Biweekly Availability
          <span aria-hidden="true">→</span>
        </RouterLink>

        <RouterLink
          to="/employee/availability"
          class="secondary-link"
        >
          Edit Weekly Template
        </RouterLink>
      </article>

      <article class="employee-card">
        <div class="card-icon">🌴</div>
        <h2>Time Off</h2>
        <p>Request time off and review upcoming absences.</p>

        <span class="coming-soon">Coming Soon</span>
      </article>
    </div>
  </section>
</template>



<style scoped>
.employee-dashboard {
  width: 100%;
  max-width: 1450px;
  margin: 0 auto;
  padding: 40px 32px 64px;
  color: #30252d;
}

.dashboard-heading {
  margin-bottom: 32px;
}

.eyebrow {
  margin: 0 0 10px;
  color: #8b526f;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.15em;
}

h1 {
  margin: 0 0 12px;
  color: #282528;
  font-size: clamp(30px, 3vw, 42px);
  font-weight: 750;
  letter-spacing: -0.04em;
}

.subtitle {
  margin: 0;
  color: #756c73;
  font-size: 14px;
}

.error {
  color: #a43c45;
}

.employee-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 20px;
}

.employee-card {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  min-width: 0;
  min-height: 250px;
  padding: 28px;
  background: #fff;
  border: 1px solid #e9dfe5;
  border-radius: 16px;
  box-shadow: 0 4px 18px rgb(90 45 70 / 5%);
}

.card-icon {
  display: grid;
  place-items: center;
  width: 44px;
  height: 44px;
  margin-bottom: 20px;
  border-radius: 12px;
  background: #f4e3ec;
  font-size: 21px;
}

.employee-card h2 {
  margin: 0 0 10px;
  color: #30252d;
  font-size: 19px;
  letter-spacing: -0.02em;
}

.employee-card p {
  margin: 0 0 24px;
  color: #756c73;
  font-size: 14px;
  line-height: 1.6;
}

.card-link {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  margin-top: auto;
  padding: 11px 15px;
  background: #8b526f;
  border-radius: 9px;
  color: #fff;
  font-size: 13px;
  font-weight: 700;
  text-decoration: none;
}

.card-link:hover {
  background: #70405a;
}

.secondary-link {
  margin-top: 14px;
  color: #8b526f;
  font-size: 13px;
  font-weight: 600;
  text-decoration: none;
}

.secondary-link:hover {
  text-decoration: underline;
}

.coming-soon {
  margin-top: auto;
  padding: 8px 12px;
  background: #f4e3ec;
  border-radius: 20px;
  color: #8b526f;
  font-size: 12px;
  font-weight: 700;
}

@media (max-width: 1050px) {
  .employee-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 700px) {
  .employee-dashboard {
    padding: 24px 16px 48px;
  }

  .employee-grid {
    grid-template-columns: 1fr;
  }

  .employee-card {
    min-height: 220px;
    padding: 22px;
  }
}
</style>
