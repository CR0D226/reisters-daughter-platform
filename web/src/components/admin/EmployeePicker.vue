
<script setup lang="ts">
import { computed, ref } from 'vue'

type EmployeeOption = {
  id: number
  firstName: string
  lastName: string
  qualified: boolean
  availability: string
  conflict?: string
}

const props = defineProps<{
  employees: EmployeeOption[]
  disabled?: boolean
}>()

const emit = defineEmits<{
  select: [employeeId: number]
}>()

const open = ref(false)
const search = ref('')

const filtered = computed(() =>
  props.employees
    .filter(employee =>
      `${employee.firstName} ${employee.lastName}`
        .toLowerCase()
        .includes(search.value.toLowerCase())
    )
    .sort((a, b) => {
      if (!!a.conflict !== !!b.conflict)
        return Number(!!a.conflict) - Number(!!b.conflict)

      if (a.qualified !== b.qualified)
        return Number(b.qualified) - Number(a.qualified)

      return a.firstName.localeCompare(b.firstName)
    })
)

function selectEmployee(employee: EmployeeOption) {
  if (employee.conflict || props.disabled) return

  emit('select', employee.id)
  open.value = false
  search.value = ''
}
</script>

<template>
  <div class="employee-picker">
    <button
      type="button"
      class="picker-trigger"
      :disabled="disabled"
      :aria-expanded="open"
      @click="open = !open"
    >
      <span>+ Assign employee</span>
      <span>{{ open ? '−' : '⌄' }}</span>
    </button>

    <div v-if="open" class="picker-panel">
      <input
        v-model="search"
        class="picker-search"
        type="search"
        placeholder="Search employees..."
        aria-label="Search employees"
      />

      <div class="picker-results">
        <button
          v-for="employee in filtered"
          :key="employee.id"
          type="button"
          class="employee-option"
          :disabled="!!employee.conflict || disabled"
          @click="selectEmployee(employee)"
        >
          <span class="avatar">
            {{ employee.firstName.charAt(0) }}
            {{ employee.lastName.charAt(0) }}
          </span>

          <span class="employee-details">
            <strong>
              {{ employee.firstName }} {{ employee.lastName }}
            </strong>

            <span class="employee-meta">
              <span
                class="qualification"
                :class="{ qualified: employee.qualified }"
              >
                {{ employee.qualified ? '✓ Qualified' : 'Other position' }}
              </span>

              <span
                class="availability"
                :class="{ conflicting: !!employee.conflict }"
              >
                {{ employee.conflict || employee.availability }}
              </span>
            </span>
          </span>
        </button>

        <p v-if="filtered.length === 0" class="no-results">
          No matching employees.
        </p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.employee-picker {
  position: relative;
  width: 100%;
  margin: 12px 0;
}

.picker-trigger {
  width: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 11px 13px;
  background: #fff;
  color: #8b526f;
  border: 1px solid #debbcd;
  border-radius: 10px;
  font-weight: 600;
  cursor: pointer;
}

.picker-trigger:hover:not(:disabled) {
  background: #f8edf3;
}

.picker-trigger:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.picker-panel {
  position: absolute;
  top: calc(100% + 6px);
  left: 0;
  z-index: 50;
  width: min(340px, 85vw);
  padding: 12px;
  background: white;
  border: 1px solid #e9dfe5;
  border-radius: 14px;
  box-shadow: 0 14px 35px rgb(60 30 50 / 16%);
}

.picker-search {
  width: 100%;
  padding: 11px;
  border: 1px solid #e9dfe5;
  border-radius: 9px;
  font: inherit;
}

.picker-results {
  max-height: 300px;
  overflow-y: auto;
  margin-top: 10px;
}

.employee-option {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  padding: 10px 8px;
  border: none;
  border-radius: 9px;
  background: transparent;
  text-align: left;
  cursor: pointer;
}

.employee-option:hover:not(:disabled) {
  background: #faf0f5;
}

.employee-option:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.avatar {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: #f4e3ec;
  color: #8b526f;
  font-size: 12px;
  font-weight: 700;
}

.employee-details {
  display: flex;
  flex-direction: column;
  gap: 5px;
  min-width: 0;
}

.employee-details strong {
  color: #282528;
  font-size: 13px;
}

.employee-meta {
  display: flex;
  flex-direction: column;
  gap: 3px;
  font-size: 11px;
  color: #756c73;
}

.qualification.qualified {
  color: #397a55;
}

.availability.conflicting {
  color: #b42318;
}

.no-results {
  padding: 12px;
  font-size: 12px;
  color: #756c73;
}
</style>
