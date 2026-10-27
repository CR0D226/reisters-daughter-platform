
<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import keycloak from '@/auth/keycloak'

async function logout() {
  await keycloak.logout({
    redirectUri: window.location.origin,
  })
}
</script>

<template>
  <div class="employee-portal">
    <aside class="employee-sidebar">
      <RouterLink to="/employee" class="brand">
        <span class="brand-logo">RD</span>
        <span class="brand-text">
          <strong>The Reister's Daughter</strong>
          <small>EMPLOYEE PORTAL</small>
        </span>
      </RouterLink>

      <nav class="sidebar-nav" aria-label="Employee navigation">
        <RouterLink to="/employee" exact-active-class="active">
          Dashboard
        </RouterLink>

        <RouterLink to="/employee/schedule">
          My Schedule
        </RouterLink>

        <RouterLink to="/employee/biweekly-availability">
          Biweekly Availability
        </RouterLink>

        <RouterLink to="/employee/availability">
          Weekly Template
        </RouterLink>
      </nav>
    </aside>

    <div class="employee-workspace">
      <header class="employee-header">
        <span class="header-label">Employee Portal</span>

        <button type="button" @click="logout">
          Logout
        </button>
      </header>

      <main class="employee-content">
        <RouterView />
      </main>
    </div>
  </div>
</template>

<style scoped>
.employee-portal {
  display: grid;
  grid-template-columns: 240px minmax(0, 1fr);
  min-height: 100vh;
  background: #faf7f9;
}

.employee-sidebar {
  min-width: 0;
  padding: 24px 14px;
  background: #30252d;
  color: white;
}

.brand {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 4px 10px 30px;
  color: white;
  text-decoration: none;
}

.brand-logo {
  display: grid;
  place-items: center;
  flex-shrink: 0;
  width: 42px;
  height: 42px;
  border-radius: 12px;
  background: #deb4c8;
  color: #30252d;
  font-weight: 800;
}

.brand-text {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.brand-text strong {
  font-size: 12px;
  line-height: 1.3;
}

.brand-text small {
  color: #deb4c8;
  font-size: 10px;
  letter-spacing: 0.12em;
}

.sidebar-nav {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.sidebar-nav a {
  display: block;
  padding: 12px 14px;
  border-radius: 10px;
  color: #e5dce2;
  text-decoration: none;
  font-size: 13px;
  font-weight: 600;
}

.sidebar-nav a:hover {
  background: #493744;
  color: white;
}

.sidebar-nav a.router-link-exact-active {
  background: #80536d;
  color: white;
}

.employee-workspace {
  min-width: 0;
}

.employee-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 68px;
  padding: 12px 24px;
  background: white;
  border-bottom: 1px solid #e9dfe5;
}

.header-label {
  color: #756c73;
  font-size: 13px;
  font-weight: 600;
}

.employee-header button {
  padding: 10px 16px;
  border: 1px solid #e9dfe5;
  border-radius: 9px;
  background: white;
  cursor: pointer;
}

.employee-content {
  min-width: 0;
}

@media (max-width: 650px) {
  .employee-portal {
    grid-template-columns: minmax(0, 1fr);
  }

  .employee-sidebar {
    padding: 14px;
  }

  .brand {
    padding-bottom: 14px;
  }

  .sidebar-nav {
    flex-direction: row;
    overflow-x: auto;
    padding-bottom: 4px;
  }

  .sidebar-nav a {
    flex-shrink: 0;
    white-space: nowrap;
  }

  .employee-header {
    padding: 12px 14px;
  }
}
</style>
