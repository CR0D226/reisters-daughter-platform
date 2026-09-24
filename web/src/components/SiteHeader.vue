<script setup lang="ts">
import { RouterLink } from 'vue-router'

const navigation = [
  { name: 'Home', path: '/' },
  { name: 'Menu', path: '/menu' },
  { name: 'About', path: '/about' },
  { name: 'Catering & Rentals', path: '/catering' },
  { name: 'Jobs', path: '/jobs' },
]
</script>

<template>
  <header class="site-header">
    <RouterLink class="brand" to="/">
      The Reister's Daughter
    </RouterLink>

    <nav aria-label="Main navigation">
      <RouterLink
        v-for="item in navigation"
        :key="item.name"
        :to="item.path"
      >
        {{ item.name }}
      </RouterLink>
    </nav>

    <a class="order-button" href="#">
      Order Online
    </a>
  </header>
</template>

<style scoped>
.site-header {
  width: 100%;
  height: 88px;
  padding: 0 5%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 40px;
  border-bottom: 1px solid rgba(28, 28, 28, 0.12);
  background: #f7f4ee;
}

.brand {
  font-size: 1.35rem;
  font-weight: 700;
  letter-spacing: -0.03em;
  white-space: nowrap;
}

nav {
  display: flex;
  align-items: center;
  gap: 32px;
  margin-left: auto;
}

nav a {
  font-size: 0.95rem;
  font-weight: 500;
  transition: opacity 0.2s ease;
}

nav a:hover {
  opacity: 0.55;
}

nav a.router-link-active {
  font-weight: 700;
}

.order-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 13px 22px;
  border-radius: 999px;
  background: #1c1c1c;
  color: #ffffff;
  font-weight: 600;
  white-space: nowrap;
}

@media (max-width: 800px) {
  .site-header {
    height: 72px;
    padding: 0 20px;
  }

  nav {
    display: none;
  }

  .brand {
    font-size: 1.1rem;
  }

  .order-button {
    padding: 10px 16px;
    font-size: 0.85rem;
  }
}
</style>