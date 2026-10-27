<script setup lang="ts">
import { apiFetch } from '@/services/api'
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
  positions: string[]
}

const users = ref<AppUser[]>([])
const loading = ref(true)
const saving = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const showCreateForm = ref(false)
const showImportForm = ref(false)

const importFileName = ref('')
const importLoading = ref(false)
const importFile = ref<File | null>(null)

function handleImportFile(event: Event) {
  const input = event.target as HTMLInputElement
  importFile.value = input.files?.[0] ?? null
  importFileName.value = importFile.value?.name ?? ''
  importPreview.value = null
  importRows.value = []
  importError.value = ''
}

interface ImportRow {
  firstName: string
  lastName: string
  email: string
  positions: string[]
}

interface ImportPreviewRow extends ImportRow {
  row: number
  status: 'Ready' | 'Existing' | 'Invalid'
  errors: string[]
}

interface ImportPreview {
  total: number
  ready: number
  existing: number
  invalid: number
  employees: ImportPreviewRow[]
}

const importPreview = ref<ImportPreview | null>(null)
const importRows = ref<ImportRow[]>([])
const importError = ref('')

function parseCsvLine(line: string): string[] {
  const values: string[] = []
  let current = ''
  let quoted = false

  for (let i = 0; i < line.length; i++) {
    const character = line[i]

    if (character === '"') {
      if (quoted && line[i + 1] === '"') {
        current += '"'
        i++
      } else {
        quoted = !quoted
      }
    } else if (character === ',' && !quoted) {
      values.push(current.trim())
      current = ''
    } else {
      current += character
    }
  }

  if (quoted) {
    throw new Error('Unclosed quotation mark in CSV.')
  }

  values.push(current.trim())
  return values
}

async function previewImport() {
  if (!importFile.value) return

  importLoading.value = true
  importError.value = ''
  importPreview.value = null
  importRows.value = []

  try {
    const text = (await importFile.value.text())
      .replace(/^\uFEFF/, '')

    // Initial format: First,Last,Email,Positions
    // Positions can contain multiple values separated by |
    const lines = text.split(/\r?\n/)
      .filter(line => line.trim())

    if (lines.length < 2) {
      throw new Error('CSV must contain a header and employee rows.')
    }

    const headers = parseCsvLine(lines[0]!)
      .map(value => value.toLowerCase().replace(/\s+/g, ''))

    const findColumn = (...names: string[]) =>
      headers.findIndex(header => names.includes(header))

    const firstIndex = findColumn('first', 'firstname')
    const lastIndex = findColumn('last', 'lastname')
    const emailIndex = findColumn('email', 'workemail')
    const positionsIndex = findColumn('positions', 'position')

    if ([firstIndex, lastIndex, emailIndex].includes(-1)) {
      throw new Error('CSV requires First, Last, and Email columns.')
    }

    const rows: ImportRow[] = lines.slice(1).map(line => {
      const columns = parseCsvLine(line)

      return {
        firstName: columns[firstIndex] ?? '',
        lastName: columns[lastIndex] ?? '',
        email: columns[emailIndex] ?? '',
        positions: positionsIndex === -1
          ? []
          : (columns[positionsIndex] ?? '')
              .split('|')
              .map(position => position.trim())
              .filter(Boolean),
      }
    })

    // Kelsey already exists; her email will be corrected separately.
    const employees = rows.filter(row =>
      !(
        row.firstName.toLowerCase() === 'kelsey' &&
        row.lastName.toLowerCase() === 'marcus'
      ),
    )

    if (employees.length === 0) {
      throw new Error('No employees to preview.')
    }

    const response = await apiFetch(
      '/api/admin/users/import/preview',
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ employees }),
      },
    )

    const data = await response.json()

    if (!response.ok) {
      throw new Error(data.message ?? 'Unable to preview import.')
    }

    importRows.value = employees
    importPreview.value = data as ImportPreview
  } catch (error) {
    importError.value = error instanceof Error
      ? error.message
      : 'Unable to read CSV.'
  } finally {
    importLoading.value = false
  }
}
const confirmingImport = ref(false)

async function confirmImport() {
  if (
    !importPreview.value ||
    importPreview.value.invalid > 0 ||
    importPreview.value.ready === 0 ||
    confirmingImport.value
  ) return

  const confirmed = window.confirm(
    `Import ${importPreview.value.ready} employees into the database?\n\n` +
    'Existing users will be skipped. No Keycloak accounts will be created.'
  )

  if (!confirmed) return

  confirmingImport.value = true
  importError.value = ''
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const response = await apiFetch(
      '/api/admin/users/import/confirm',
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          employees: importRows.value,
        }),
      },
    )

    const data = await response.json()

    if (!response.ok) {
      throw new Error(
        data.message ?? 'Unable to import employees.'
      )
    }

    successMessage.value =
      `${data.imported} employees imported successfully. ` +
      `${data.skippedExisting} existing accounts skipped.`

    importPreview.value = null
    importRows.value = []
    importFile.value = null
    importFileName.value = ''
    showImportForm.value = false

    await loadUsers()
  } catch (error) {
    importError.value =
      error instanceof Error
        ? error.message
        : 'Unable to import employees.'
  } finally {
    confirmingImport.value = false
  }
}

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
    const response = await apiFetch(
      '/api/admin/users',
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
    const response = await apiFetch(
      '/api/admin/users',
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
    const response = await apiFetch(
      `/api/admin/users/${user.id}`,
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
          positions: updatedUser.positions,
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
async function toggleQualification(
  user: AppUser,
  position: string,
  checked: boolean,
) {
  const positions = checked
    ? [...new Set([...(user.positions ?? []), position])]
    : (user.positions ?? []).filter(p => p !== position)

  await updateUser(user, { positions })
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

      <div class="page-actions">
  <button
    type="button"
    class="primary-button"
    @click="showImportForm = !showImportForm"
  >
    {{ showImportForm ? 'Cancel Import' : 'Import Employees' }}
  </button>

  <button
    type="button"
    class="primary-button"
    @click="showCreateForm = !showCreateForm"
  >
    {{ showCreateForm ? 'Cancel' : '+ Add User' }}
   </button>
    </div> <!-- Closes page-actions -->
  </div>
  <section
  v-if="showImportForm"
  class="create-card"
>
  <div class="section-heading">
    <div>
      <span class="eyebrow">Staff Management</span>
      <h2>Import Employees</h2>
    </div>
  </div>

  <p>
    Upload a CSV file containing employee names,
    emails, and positions. You'll review the
    records before importing them.
  </p>

  <div class="form-grid">
    <label class="wide-field">
      <span>Employee CSV File</span>

      <input
        type="file"
        accept=".csv,text/csv"
        @change="handleImportFile"
      />
    </label>
  </div>

  <div v-if="importFileName" class="form-actions">
    <p>Selected: {{ importFileName }}</p>

    <button
      type="button"
      class="primary-button"
      :disabled="importLoading"
      @click="previewImport"
    >
      {{ importLoading ? 'Checking…' : 'Preview Import' }}
    </button>
  </div>


<p v-if="importError" class="message error-message">
  {{ importError }}
</p>

<div v-if="importPreview" class="import-results">
  <h3>Import Preview</h3>

  <p>
    {{ importPreview.total }} checked ·
    {{ importPreview.ready }} ready ·
    {{ importPreview.existing }} existing ·
    {{ importPreview.invalid }} invalid
  </p>

  <div class="import-table-wrap">
    <table class="import-table">
      <thead>
        <tr>
          <th>Name</th>
          <th>Email</th>
          <th>Positions</th>
          <th>Status</th>
        </tr>
      </thead>

      <tbody>
        <tr
          v-for="employee in importPreview.employees"
          :key="employee.row"
        >
          <td>{{ employee.firstName }} {{ employee.lastName }}</td>
          <td>{{ employee.email }}</td>
          <td>{{ employee.positions.join(', ') || 'Not assigned' }}</td>
          <td>
            {{ employee.status }}
            <small v-if="employee.errors.length">
              {{ employee.errors.join('; ') }}
            </small>
          </td>
        </tr>
      </tbody>
        </table>
  </div>

  <div class="form-actions">
    <button
      type="button"
      class="primary-button"
      :disabled="
        confirmingImport ||
        importLoading ||
        importPreview.invalid > 0 ||
        importPreview.ready === 0
      "
      @click="confirmImport"
    >
      {{
        confirmingImport
          ? 'Importing Employees…'
          : `Confirm Import (${importPreview.ready})`
      }}
    </button>
  </div>

  <p>
    Existing accounts will be skipped. Imported employees
    will not receive login access until their Keycloak
    identities are connected.
  </p>
</div>
</section>

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
          <div
  v-if="user.role === 'Employee'"
  class="employee-qualifications"
>
  <span class="qualification-heading">
    Qualified Positions
  </span>

  <label
    v-for="position in ['Barista', 'Cashier', 'Baker']"
    :key="position"
    class="qualification-option"
  >
    <input
      type="checkbox"
      :checked="user.positions?.includes(position) ?? false"
      @change="
        toggleQualification(
          user,
          position,
          ($event.target as HTMLInputElement).checked
        )
      "
    />

    {{ position }}
  </label>
</div>

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

.page-actions {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
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
.import-results {
  margin-top: 1.5rem;
}

.import-table-wrap {
  overflow-x: auto;
}

.import-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.85rem;
}

.import-table th,
.import-table td {
  padding: 0.8rem;
  text-align: left;
  border-bottom: 1px solid #e5ded6;
}

.import-table small {
  display: block;
  color: #8b4741;
}

.employee-qualifications {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.6rem 1rem;
}

.qualification-heading {
  width: 100%;
  font-size: 0.75rem;
  font-weight: 600;
  opacity: 0.7;
}

.qualification-option {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.85rem;
  cursor: pointer;
}

.qualification-option input {
  accent-color: #7b5b42;
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
