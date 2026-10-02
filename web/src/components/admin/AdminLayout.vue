<script setup lang="ts">
import {
  computed,
  onBeforeUnmount,
  onMounted,
  ref,
} from 'vue'

import {
  RouterLink,
  RouterView,
} from 'vue-router'

interface PendingReservation {
  reservationId: number
  eventId: number
  eventTitle: string
  firstName: string
  lastName: string
  email: string
  phone: string
  guestCount: number
  status: string
  createdAt: string
}

interface DashboardData {
  counts: {
    newInquiries: number
    pendingReservations: number
  }

  pendingReservations: PendingReservation[]
}

const dashboard = ref<DashboardData | null>(null)
const attentionOpen = ref(false)
const attentionLoading = ref(false)

const attentionCount = computed(() => {
  if (!dashboard.value) {
    return 0
  }

  return (
    dashboard.value.counts.newInquiries +
    dashboard.value.counts.pendingReservations
  )
})

async function loadAttention() {
  attentionLoading.value = true

  try {
    const response = await fetch(
      'http://localhost:5128/api/admin/dashboard',
    )

    if (!response.ok) {
      throw new Error(
        'Unable to load notifications.',
      )
    }

    dashboard.value = await response.json()
  } catch (error) {
    console.error(
      'Unable to load admin notifications:',
      error,
    )
  } finally {
    attentionLoading.value = false
  }
}

function toggleAttention() {
  attentionOpen.value = !attentionOpen.value

  if (attentionOpen.value) {
    loadAttention()
  }
}

function closeAttention() {
  attentionOpen.value = false
}

function formatSubmittedDate(
  value: string,
) {
  return new Intl.DateTimeFormat(
    undefined,
    {
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    },
  ).format(new Date(value))
}

function handleDocumentClick(
  event: MouseEvent,
) {
  const target = event.target as HTMLElement

  if (
    !target.closest('.attention-wrapper')
  ) {
    closeAttention()
  }
}

onMounted(() => {
  loadAttention()

  document.addEventListener(
    'click',
    handleDocumentClick,
  )
})

onBeforeUnmount(() => {
  document.removeEventListener(
    'click',
    handleDocumentClick,
  )
})
</script>

<template>
  <div class="admin-layout">
    <header class="admin-header">
      <div class="admin-header-inner">
        <div class="admin-brand">
          <RouterLink to="/admin" class="admin-brand-link">
            <span class="admin-brand-name">
              The Reister's Daughter
            </span>

            <span class="admin-brand-label">
              Admin
            </span>
          </RouterLink>
        </div>

        <nav class="admin-nav">
          <RouterLink
            to="/admin"
            class="admin-nav-link"
            exact-active-class="active"
          >
            Dashboard
          </RouterLink>

          <RouterLink
            to="/admin/inquiries"
            class="admin-nav-link"
          >
            Inquiries
          </RouterLink>

          <RouterLink
            to="/admin/customers"
            class="admin-nav-link"
          >
            Customers
          </RouterLink>

          <RouterLink
            to="/admin/bookings"
            class="admin-nav-link"
          >
            Bookings
          </RouterLink>

          <RouterLink
            to="/admin/events"
            class="admin-nav-link"
          >
            Events
          </RouterLink>

          <RouterLink
  to="/admin/users"
  class="admin-nav-link"
>
  Users
</RouterLink>
        </nav>

        <div class="admin-actions">
          <!-- Attention notification goes here next -->
          <div class="attention-wrapper">
  <button
    class="attention-button"
    type="button"
    aria-label="Needs attention"
    :aria-expanded="attentionOpen"
    @click.stop="toggleAttention"
  >
    <span class="attention-icon">
      🔔
    </span>

    <span
      v-if="attentionCount > 0"
      class="attention-badge"
    >
      {{ attentionCount > 99 ? '99+' : attentionCount }}
    </span>
  </button>

  <div
    v-if="attentionOpen"
    class="attention-dropdown"
  >
    <div class="attention-heading">
      <div>
        <span class="attention-eyebrow">
          Admin
        </span>

        <h2>Needs Attention</h2>
      </div>

      <span
        v-if="attentionCount > 0"
        class="attention-total"
      >
        {{ attentionCount }}
      </span>
    </div>

    <div
      v-if="attentionLoading && !dashboard"
      class="attention-empty"
    >
      Loading…
    </div>

    <template v-else-if="dashboard">
      <RouterLink
        v-if="
          dashboard.counts.newInquiries > 0
        "
        to="/admin/inquiries"
        class="attention-item"
        @click="closeAttention"
      >
        <span class="attention-dot"></span>

        <span class="attention-item-content">
          <strong>
            {{
              dashboard.counts.newInquiries
            }}
            new
            {{
              dashboard.counts.newInquiries === 1
                ? 'inquiry'
                : 'inquiries'
            }}
          </strong>

          <span>
            Waiting for review
          </span>
        </span>

        <span class="attention-arrow">
          →
        </span>
      </RouterLink>

      <RouterLink
        v-for="
          reservation in
            dashboard.pendingReservations
        "
        :key="
          `reservation-${reservation.reservationId}`
        "
        :to="
          `/admin/events/${reservation.eventId}`
        "
        class="attention-item"
        @click="closeAttention"
      >
        <span class="attention-dot"></span>

        <span class="attention-item-content">
          <strong>
            Pending reservation
          </strong>

          <span>
            {{ reservation.eventTitle }}
            ·
            {{ reservation.guestCount }}
            {{
              reservation.guestCount === 1
                ? 'guest'
                : 'guests'
            }}
          </span>

          <small>
            {{ reservation.firstName }}
            {{ reservation.lastName }}
            ·
            {{
              formatSubmittedDate(
                reservation.createdAt,
              )
            }}
          </small>
        </span>

        <span class="attention-arrow">
          →
        </span>
      </RouterLink>

      <div
        v-if="attentionCount === 0"
        class="attention-empty"
      >
        <strong>You're all caught up.</strong>
        <span>
          Nothing currently needs attention.
        </span>
      </div>
    </template>
  </div>
</div>

          <RouterLink
            to="/"
            class="view-site-link"
          >
            View Site
          </RouterLink>
        </div>
      </div>
    </header>

    <main class="admin-content">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.admin-layout {
  min-height: 100vh;
  background: #f7f4ef;
}

.admin-header {
  position: sticky;
  top: 0;
  z-index: 100;

  background: #ffffff;
  border-bottom: 1px solid #e6e0d8;
}

.admin-header-inner {
  width: min(1400px, calc(100% - 3rem));
  min-height: 72px;
  margin: 0 auto;

  display: flex;
  align-items: center;
  gap: 2rem;
}

.admin-brand {
  flex-shrink: 0;
}

.admin-brand-link {
  display: flex;
  flex-direction: column;

  color: #25221f;
  text-decoration: none;
}

.admin-brand-name {
  font-size: 0.95rem;
  font-weight: 700;
}

.admin-brand-label {
  margin-top: 0.1rem;

  color: #8a8178;

  font-size: 0.7rem;
  font-weight: 700;

  letter-spacing: 0.12em;
  text-transform: uppercase;
}

.admin-nav {
  display: flex;
  align-items: center;
  gap: 0.25rem;

  flex: 1;
}

.admin-nav-link {
  padding: 0.6rem 0.8rem;

  border-radius: 0.5rem;

  color: #625b54;
  text-decoration: none;

  font-size: 0.85rem;
  font-weight: 600;

  transition:
    background 0.15s ease,
    color 0.15s ease;
}

.admin-nav-link:hover {
  background: #f4f0eb;
  color: #25221f;
}

.admin-nav-link.router-link-active,
.admin-nav-link.active {
  background: #eee8e1;
  color: #25221f;
}

.admin-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.attention-button {
  position: relative;

  width: 40px;
  height: 40px;

  display: grid;
  place-items: center;

  border: 1px solid #ddd5cc;
  border-radius: 50%;

  background: #ffffff;

  cursor: pointer;
}

.attention-button:hover {
  background: #f7f4ef;
}

.attention-icon {
  font-size: 1rem;
}

.view-site-link {
  padding: 0.6rem 0.9rem;

  border: 1px solid #d8d0c7;
  border-radius: 0.5rem;

  color: #4f4943;
  text-decoration: none;

  font-size: 0.8rem;
  font-weight: 700;
}

.view-site-link:hover {
  background: #f7f4ef;
}

.admin-content {
  min-height: calc(100vh - 72px);
}
.attention-wrapper {
  position: relative;
}

.attention-badge {
  position: absolute;
  top: -5px;
  right: -5px;

  min-width: 19px;
  height: 19px;
  padding: 0 5px;

  display: flex;
  align-items: center;
  justify-content: center;

  border: 2px solid #ffffff;
  border-radius: 999px;

  background: #8b4741;
  color: #ffffff;

  font-size: 0.65rem;
  font-weight: 800;
  line-height: 1;
}

.attention-dropdown {
  position: absolute;
  top: calc(100% + 0.75rem);
  right: 0;

  width: min(390px, calc(100vw - 2rem));
  max-height: 520px;
  overflow-y: auto;

  background: #ffffff;
  border: 1px solid #e1dad2;
  border-radius: 0.8rem;

  box-shadow:
    0 18px 45px
    rgba(48, 41, 35, 0.16);

  z-index: 200;
}

.attention-heading {
  padding: 1rem 1.1rem;

  display: flex;
  align-items: center;
  justify-content: space-between;

  border-bottom: 1px solid #eee8e1;
}

.attention-heading h2 {
  margin: 0.1rem 0 0;

  color: #292521;

  font-size: 1rem;
}

.attention-eyebrow {
  color: #958a80;

  font-size: 0.65rem;
  font-weight: 800;

  letter-spacing: 0.12em;
  text-transform: uppercase;
}

.attention-total {
  min-width: 28px;
  height: 28px;

  display: grid;
  place-items: center;

  border-radius: 999px;

  background: #f1e7e4;
  color: #8b4741;

  font-size: 0.75rem;
  font-weight: 800;
}

.attention-item {
  padding: 0.95rem 1.1rem;

  display: grid;
  grid-template-columns:
    auto 1fr auto;
  gap: 0.75rem;
  align-items: start;

  border-bottom: 1px solid #f0ebe5;

  color: inherit;
  text-decoration: none;
}

.attention-item:hover {
  background: #faf8f5;
}

.attention-dot {
  width: 8px;
  height: 8px;
  margin-top: 0.35rem;

  border-radius: 50%;

  background: #b4744f;
}

.attention-item-content {
  min-width: 0;

  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.attention-item-content strong {
  color: #302b27;

  font-size: 0.82rem;
}

.attention-item-content span {
  color: #6e665f;

  font-size: 0.78rem;
}

.attention-item-content small {
  color: #9a9189;

  font-size: 0.7rem;
}

.attention-arrow {
  color: #978b81;

  font-size: 1rem;
}

.attention-empty {
  padding: 1.4rem 1.1rem;

  display: flex;
  flex-direction: column;
  gap: 0.25rem;

  color: #777069;

  font-size: 0.8rem;
}

.attention-empty strong {
  color: #39342f;
}

@media (max-width: 900px) {
  .admin-header-inner {
    width: min(100% - 2rem, 1400px);
    padding: 0.75rem 0;

    flex-wrap: wrap;
    gap: 0.75rem;
  }

  .admin-nav {
    order: 3;

    width: 100%;

    overflow-x: auto;
    
  }

  .admin-actions {
    margin-left: auto;
  }
}
</style>