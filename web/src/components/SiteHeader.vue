
<script setup lang="ts">
import { ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'

const route = useRoute()
const menuOpen = ref(false)

const navigation = [
  { name: 'Home', path: '/' },
  { name: 'Menu', path: '/menu' },
  { name: 'About', path: '/about' },
  { name: 'Catering & Rentals', path: '/catering' },
  { name: 'Events', path: '/events' },
  { name: 'Jobs', path: '/jobs' },
]

const orderUrl =
  'https://order.toasttab.com/online/the-reister-s-daughter-202-main-street'

watch(
  () => route.fullPath,
  () => {
    menuOpen.value = false
  }
)
</script>

<template>
  <header class="site-header">
    <RouterLink
      class="brand"
      to="/"
      @click="menuOpen = false"
    >
      The Reister's Daughter
    </RouterLink>

    <nav
      id="main-navigation"
      :class="{ open: menuOpen }"
      aria-label="Main navigation"
    >
      <RouterLink
        v-for="item in navigation"
        :key="item.name"
        :to="item.path"
        @click="menuOpen = false"
      >
        {{ item.name }}
      </RouterLink>

      <a
        class="mobile-order"
        :href="orderUrl"
        target="_blank"
        rel="noopener noreferrer"
        @click="menuOpen = false"
      >
        Order Online
      </a>
    </nav>

    <a
      class="order-button"
      :href="orderUrl"
      target="_blank"
      rel="noopener noreferrer"
    >
      Order Online
    </a>

    <button
      class="menu-toggle"
      type="button"
      aria-label="Toggle navigation"
      aria-controls="main-navigation"
      :aria-expanded="menuOpen"
      @click="menuOpen = !menuOpen"
    >
      <span></span>
      <span></span>
      <span></span>
    </button>
  </header>
</template>

<style scoped>
.site-header {
  position: relative;
  z-index: 100;
  width: 100%;
  min-height: 88px;
  padding: 0 5%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 40px;
  border-bottom: 1px solid rgba(28, 28, 28, 0.12);
  background: #FFF8F1;
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

.order-button,
.mobile-order {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 13px 22px;
  border-radius: 999px;
  background: #C85F83;
  color: #ffffff;
  font-weight: 600;
  white-space: nowrap;
}

.mobile-order,
.menu-toggle {
  display: none;
}

@media (max-width: 1100px) {
  .site-header {
    min-height: 72px;
    padding: 0 20px;
    gap: 16px;
  }

  .brand {
    font-size: 1.1rem;
    white-space: normal;
  }

  .menu-toggle {
    display: flex;
    flex-direction: column;
    justify-content: center;
    gap: 5px;
    width: 44px;
    height: 44px;
    padding: 10px;
    flex-shrink: 0;
    border: 0;
    background: transparent;
    cursor: pointer;
  }

  .menu-toggle span {
    display: block;
    width: 100%;
    height: 2px;
    background: #C85F83;
  }

  nav {
    display: none;
    position: absolute;
    top: 100%;
    left: 0;
    right: 0;
    margin: 0;
    padding: 20px;
    background: #FFF8F1;
    border-bottom: 1px solid rgba(28, 28, 28, 0.12);
    box-shadow: 0 12px 20px rgba(0, 0, 0, 0.08);
  }

  nav.open {
    display: flex;
    flex-direction: column;
    align-items: stretch;
    gap: 0;
  }

  nav a {
    padding: 14px 4px;
    font-size: 1rem;
  }

  .mobile-order {
    display: flex;
    margin-top: 12px;
    padding: 14px;
    color: #ffffff;
  }

  .order-button {
    display: none;
  }
}
</style>
