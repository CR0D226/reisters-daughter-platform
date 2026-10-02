<script setup lang="ts">
import {
  computed,
  onMounted,
  ref,
} from 'vue'

interface AppUser {
  id: number
  email: string
  firstName: string
  lastName: string
  role: 'Manager' | 'Employee'
  isActive: boolean
  externalId: string | null
  createdAt: string
  updatedAt: string
}

const users = ref<AppUser[]>([])
const loading = ref(true)
const saving = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const showCreateForm = ref(false)

const newUser = ref({
  email: '',
  firstName: '',
  lastName: '',
  role: 'Employee',
})

const activeUsers = computed(() =>
  users.value.filter(user => user.isActive),
)

const inactiveUsers = computed(() =>
  users.value.filter(user => !user.isActive),
)

const managerCount = computed(() =>
  activeUsers.value.filter(
    user => user.role === 'Manager',
  ).length,
)

const employeeCount = computed(() =>
  activeUsers.value.filter(
    user => user.role === 'Employee',
  ).length,
)

async function loadUsers() {
  loading.value = true
  errorMessage.value = ''

  try {
    const response = await fetch(
      'http://localhost:5128/api/admin/users',
    )

    if (!response.ok) {
      throw new Error('Unable to load users.')
    }

    users.value = await response.json()
  } catch (error) {
    console.error(error)

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Unable to load users.'
  } finally {
    loading.value = false
  }
}

async function createUser() {
  saving.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const response = await fetch(
      'http://localhost:5128/api/admin/users',
      {
        method: 'POST',

        headers: {
          'Content-Type': 'application/json',
        },

        body: JSON.stringify(newUser.value),
      },
    )

    const data = await response.json()

    if (!response.ok) {
      throw new Error(
        data.message || 'Unable to create user.',
      )
    }

    successMessage.value =
      `${data.firstName} ${data.lastName} was added.`

    newUser.value = {
      email: '',
      firstName: '',
      lastName: '',
      role: 'Employee',
    }

    showCreateForm.value = false

    await loadUsers()
  } catch (error) {
    console.error(error)

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Unable to create user.'
  } finally {
    saving.value = false
  }
}

async function updateUser(
  user: AppUser,
  changes: Partial<AppUser>,
) {
  errorMessage.value = ''
  successMessage.value = ''

  const updatedUser = {
    ...user,
    ...changes,
  }

  try {
    const response = await fetch(
      `http://localhost:5128/api/admin/users/${user.id}`,
      {
        method: 'PUT',

        headers: {
          'Content-Type': 'application/json',
        },

        body: JSON.stringify({
          email: updatedUser.email,
          firstName: updatedUser.firstName,
          lastName: updatedUser.lastName,
          role: updatedUser.role,
          isActive: updatedUser.isActive,
        }),
      },
    )

    const data = await response.json()

    if (!response.ok) {
      throw new Error(
        data.message || 'Unable to update user.',
      )
    }

    successMessage.value =
      `${data.firstName} ${data.lastName} was updated.`

    await loadUsers()
  } catch (error) {
    console.error(error)

    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Unable to update user.'
  }
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(
    undefined,
    {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    },
  ).format(new Date(value))
}

onMounted(loadUsers)
</script>

<template>
  <div class="users-page">
    <div class="page-heading">
      <div>
        <span class="eyebrow">
          Administration
        </span>

        <h1>Users</h1>

        <p>
          Manage who can access the business
          platform and what role they have.
        </p>
      </div>

      <button
        type="button"
        class="primary-button"
        @click="showCreateForm = !showCreateForm"
      >
        {{
          showCreateForm
            ? 'Cancel'
            : '+ Add User'
        }}
      </button>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <span>Active Users</span>
        <strong>{{ activeUsers.length }}</strong>
      </div>

      <div class="stat-card">
        <span>Managers</span>
        <strong>{{ managerCount }}</strong>
      </div>

      <div class="stat-card">
        <span>Employees</span>
        <strong>{{ employeeCount }}</strong>
      </div>

      <div class="stat-card">
        <span>Inactive</span>
        <strong>{{ inactiveUsers.length }}</strong>
      </div>
    </div>

    <div
      v-if="errorMessage"
      class="message error-message"
    >
      {{ errorMessage }}
    </div>

    <div
      v-if="successMessage"
      class="message success-message"
    >
      {{ successMessage }}
    </div>

    <form
      v-if="showCreateForm"
      class="create-card"
      @submit.prevent="createUser"
    >
      <div class="section-heading">
        <div>
          <span class="eyebrow">
            New Account
          </span>

          <h2>Add User</h2>
        </div>
      </div>

      <div class="form-grid">
        <label>
          <span>First Name</span>

          <input
            v-model="newUser.firstName"
            type="text"
            required
          />
        </label>

        <label>
          <span>Last Name</span>

          <input
            v-model="newUser.lastName"
            type="text"
            required
          />
        </label>

        <label class="wide-field">
          <span>Email</span>

          <input
            v-model="newUser.email"
            type="email"
            required
          />
        </label>

        <label>
          <span>Role</span>

          <select v-model="newUser.role">
            <option value="Employee">
              Employee
            </option>

            <option value="Manager">
              Manager
            </option>
          </select>
        </label>
      </div>

      <div class="form-actions">
        <button
          type="submit"
          class="primary-button"
          :disabled="saving"
        >
          {{
            saving
              ? 'Adding…'
              : 'Add User'
          }}
        </button>
      </div>
    </form>

    <section class="users-card">
      <div class="section-heading">
        <div>
          <span class="eyebrow">
            Access
          </span>

          <h2>Active Users</h2>
        </div>

        <span class="record-count">
          {{ activeUsers.length }}
        </span>
      </div>

      <div
        v-if="loading"
        class="empty-state"
      >
        Loading users…
      </div>

      <div
        v-else-if="activeUsers.length === 0"
        class="empty-state"
      >
        No active users.
      </div>

      <div
        v-else
        class="user-list"
      >
        <article
          v-for="user in activeUsers"
          :key="user.id"
          class="user-row"
        >
          <div class="avatar">
            {{ user.firstName.charAt(0) }}
            {{ user.lastName.charAt(0) }}
          </div>

          <div class="user-identity">
            <strong>
              {{ user.firstName }}
              {{ user.lastName }}
            </strong>

            <a :href="`mailto:${user.email}`">
              {{ user.email }}
            </a>

            <small>
              Added {{ formatDate(user.createdAt) }}
            </small>
          </div>

          <div class="identity-status">
            <span
              v-if="user.externalId"
              class="connected-badge"
            >
              Identity Connected
            </span>

            <span
              v-else
              class="pending-badge"
            >
              Not Connected
            </span>
          </div>

          <select
            class="role-select"
            :value="user.role"
            @change="
              updateUser(
                user,
                {
                  role:
                    (
                      $event.target as
                        HTMLSelectElement
                    ).value as
                      'Manager' |
                      'Employee',
                },
              )
            "
          >
            <option value="Manager">
              Manager
            </option>

            <option value="Employee">
              Employee
            </option>
          </select>

          <button
            type="button"
            class="deactivate-button"
            @click="
              updateUser(
                user,
                {
                  isActive: false,
                },
              )
            "
          >
            Deactivate
          </button>
        </article>
      </div>
    </section>

    <section
      v-if="inactiveUsers.length > 0"
      class="users-card"
    >
      <div class="section-heading">
        <div>
          <span class="eyebrow">
            Disabled Access
          </span>

          <h2>Inactive Users</h2>
        </div>

        <span class="record-count">
          {{ inactiveUsers.length }}
        </span>
      </div>

      <div class="user-list">
        <article
          v-for="user in inactiveUsers"
          :key="user.id"
          class="user-row inactive"
        >
          <div class="avatar">
            {{ user.firstName.charAt(0) }}
            {{ user.lastName.charAt(0) }}
          </div>

          <div class="user-identity">
            <strong>
              {{ user.firstName }}
              {{ user.lastName }}
            </strong>

            <span>{{ user.email }}</span>
          </div>

          <span class="role-label">
            {{ user.role }}
          </span>

          <button
            type="button"
            class="reactivate-button"
            @click="
              updateUser(
                user,
                {
                  isActive: true,
                },
              )
            "
          >
            Reactivate
          </button>
        </article>
      </div>
    </section>
  </div>
</template>

<style scoped>
.users-page {
  width: min(1180px, calc(100% - 3rem));
  margin: 0 auto;
  padding: 3rem 0 5rem;
}

.page-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 2rem;
  margin-bottom: 2rem;
}

.page-heading h1,
.section-heading h2 {
  margin: 0;
  color: #292521;
}

.page-heading h1 {
  margin-top: 0.3rem;
  font-size: clamp(2rem, 4vw, 3rem);
}

.page-heading p {
  max-width: 600px;
  margin: 0.65rem 0 0;
  color: #766e67;
  line-height: 1.6;
}

.eyebrow {
  color: #998d82;
  font-size: 0.7rem;
  font-weight: 800;
  letter-spacing: 0.13em;
  text-transform: uppercase;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.stat-card,
.users-card,
.create-card {
  background: #ffffff;
  border: 1px solid #e5ded6;
  border-radius: 0.75rem;
}

.stat-card {
  padding: 1.25rem;
}

.stat-card span {
  display: block;
  margin-bottom: 0.35rem;
  color: #8a8178;
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
}

.stat-card strong {
  color: #302b27;
  font-size: 1.8rem;
}

.users-card,
.create-card {
  margin-top: 1.5rem;
  padding: 1.5rem;
}

.section-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.25rem;
}

.section-heading h2 {
  margin-top: 0.2rem;
  font-size: 1.25rem;
}

.record-count {
  min-width: 30px;
  height: 30px;
  display: grid;
  place-items: center;
  background: #f0ebe5;
  border-radius: 999px;
  color: #645b53;
  font-size: 0.75rem;
  font-weight: 800;
}

.primary-button {
  border: 0;
  border-radius: 0.55rem;
  padding: 0.75rem 1rem;
  background: #36312d;
  color: #ffffff;
  font: inherit;
  font-size: 0.82rem;
  font-weight: 700;
  cursor: pointer;
}

.primary-button:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1rem;
}

.form-grid label {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.form-grid label span {
  color: #625b54;
  font-size: 0.75rem;
  font-weight: 700;
}

.form-grid input,
.form-grid select,
.role-select {
  border: 1px solid #dcd4cb;
  border-radius: 0.5rem;
  padding: 0.7rem 0.75rem;
  background: #ffffff;
  color: #312c28;
  font: inherit;
}

.wide-field {
  grid-column: span 2;
}

.form-actions {
  margin-top: 1.25rem;
}

.message {
  margin-bottom: 1rem;
  padding: 0.8rem 1rem;
  border-radius: 0.55rem;
  font-size: 0.8rem;
}

.error-message {
  background: #f8e9e7;
  color: #8b4741;
}

.success-message {
  background: #edf5ee;
  color: #527057;
}

.user-list {
  display: flex;
  flex-direction: column;
}

.user-row {
  display: grid;
  grid-template-columns:
    auto minmax(220px, 1fr)
    auto auto auto;
  gap: 1rem;
  align-items: center;
  padding: 1rem 0;
  border-top: 1px solid #eee9e3;
}

.user-row:first-child {
  border-top: 0;
}

.user-row.inactive {
  opacity: 0.68;
}

.avatar {
  width: 42px;
  height: 42px;
  display: grid;
  place-items: center;
  border-radius: 50%;
  background: #eee7df;
  color: #544b44;
  font-size: 0.75rem;
  font-weight: 800;
}

.user-identity {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.user-identity strong {
  color: #302b27;
  font-size: 0.9rem;
}

.user-identity a,
.user-identity span {
  color: #756c64;
  font-size: 0.78rem;
}

.user-identity small {
  color: #a0978f;
  font-size: 0.68rem;
}

.connected-badge,
.pending-badge,
.role-label {
  display: inline-flex;
  padding: 0.35rem 0.55rem;
  border-radius: 999px;
  font-size: 0.68rem;
  font-weight: 800;
}

.connected-badge {
  background: #e8f2e9;
  color: #527057;
}

.pending-badge {
  background: #f4eee4;
  color: #8b704d;
}

.role-label {
  background: #efebe7;
  color: #625a53;
}

.role-select {
  min-width: 115px;
  padding: 0.5rem 0.6rem;
  font-size: 0.75rem;
}

.deactivate-button,
.reactivate-button {
  border-radius: 0.5rem;
  padding: 0.55rem 0.7rem;
  background: transparent;
  font: inherit;
  font-size: 0.72rem;
  font-weight: 700;
  cursor: pointer;
}

.deactivate-button {
  border: 1px solid #d9b9b5;
  color: #8b4741;
}

.reactivate-button {
  border: 1px solid #b8ccb9;
  color: #527057;
}

.empty-state {
  padding: 2rem 0;
  color: #8c837b;
  font-size: 0.85rem;
}

@media (max-width: 850px) {
  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .user-row {
    grid-template-columns: auto 1fr;
  }

  .identity-status,
  .role-select,
  .deactivate-button,
  .reactivate-button,
  .role-label {
    grid-column: 2;
    justify-self: start;
  }
}

@media (max-width: 600px) {
  .users-page {
    width: min(100% - 2rem, 1180px);
    padding-top: 2rem;
  }

  .page-heading {
    flex-direction: column;
  }

  .stats-grid,
  .form-grid {
    grid-template-columns: 1fr;
  }

  .wide-field {
    grid-column: auto;
  }
}
</style>