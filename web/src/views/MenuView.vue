<script setup lang="ts">
import { API_BASE } from '../config/api'
import { onMounted, ref, computed } from 'vue'

interface MenuItem {
  id: number
  name: string
  description: string
  price: number
  priceVaries: boolean
  category: string
  subcategory: string
  vegan: boolean
  glutenFree: boolean
  featured: boolean
  imageUrl: string | null
}

const menuItems = ref<MenuItem[]>([])
const loading = ref(true)
const error = ref('')
const selectedSection = ref('All')

onMounted(async () => {
  try {
    const response = await fetch(`${API_BASE}/api/menu`)
    if (!response.ok) throw new Error('Unable to load menu.')
    menuItems.value = await response.json()
  } catch (err) {
    console.error(err)
    error.value = 'We could not load the menu.'
  } finally {
    loading.value = false
  }
})

const menuSections = computed(() => {
  const sections = new Map<string, MenuItem[]>()

  for (const item of menuItems.value) {
    const name = item.subcategory || item.category || 'Other'
    const existing = sections.get(name)

    if (existing) existing.push(item)
    else sections.set(name, [item])
  }

  return Array.from(sections, ([name, items]) => ({
    name,
    items,
    category: items[0]?.category || '',
  }))
})

const visibleSections = computed(() =>
  selectedSection.value === 'All'
    ? menuSections.value
    : menuSections.value.filter(
        section => section.name === selectedSection.value,
      ),
)

const money = (price: number) =>
  new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(price)
</script>

<template>
  <main class="menu-page">
    <header class="menu-header">
      <p class="eyebrow">THE REISTER'S DAUGHTER · REISTERSTOWN, MD</p>
      <h1>Made with <em>care.</em><br />Served with love.</h1>
      <p class="menu-intro">
        Discover our coffees, specialty lattes, teas, smoothies,
        and fresh bakery favorites.
      </p>
      <div class="header-rule"></div>
      <p class="menu-kicker">EXPLORE OUR MENU</p>
    </header>

    <div v-if="loading" class="menu-message" role="status">
      Loading our menu...
    </div>

    <div v-else-if="error" class="menu-message" role="alert">
      {{ error }}
    </div>

    <template v-else>
      <nav class="menu-nav" aria-label="Menu categories">
        <button
          type="button"
          :class="{ active: selectedSection === 'All' }"
          :aria-pressed="selectedSection === 'All'"
          @click="selectedSection = 'All'"
        >
          Everything
        </button>
        <button
          v-for="section in menuSections"
          :key="section.name"
          type="button"
          :class="{ active: selectedSection === section.name }"
          :aria-pressed="selectedSection === section.name"
          @click="selectedSection = section.name"
        >
          {{ section.name }}
        </button>
      </nav>

      <div class="menu-sections">
        <section
          v-for="section in visibleSections"
          :key="section.name"
          class="menu-section"
        >
          <div class="section-heading">
            <div>
              <p class="section-category">{{ section.category }}</p>
              <h2>{{ section.name }}</h2>
            </div>
            <span class="section-count">
              {{ section.items.length }}
              {{ section.items.length === 1 ? 'item' : 'items' }}
            </span>
          </div>

          <div class="items-grid">
            <article
              v-for="item in section.items"
              :key="item.id"
              class="menu-item"
            >
              <img
                v-if="item.imageUrl"
                class="item-image"
                :src="item.imageUrl"
                :alt="item.name"
                loading="lazy"
              />

              <div class="item-body">
                <div class="item-top">
                  <h3>{{ item.name }}</h3>
                  <p class="price">
                    {{ money(item.price) }}{{ item.priceVaries ? '+' : '' }}
                  </p>
                </div>

                <p v-if="item.description" class="item-description">
                  {{ item.description }}
                </p>

                <div
                  v-if="item.vegan || item.glutenFree || item.featured"
                  class="dietary"
                >
                  <span v-if="item.vegan">Vegan</span>
                  <span v-if="item.glutenFree">Gluten-free</span>
                  <span v-if="item.featured">House favorite</span>
                </div>
              </div>
            </article>
          </div>
        </section>
      </div>

      <footer class="menu-footer">
        <span class="footer-heart" aria-hidden="true">♥</span>
        <h2>A little something for everyone.</h2>
        <p>
          Prices and selections may vary. Visit our online ordering
          page for current availability.
        </p>
      </footer>
    </template>
  </main>
</template>

<style scoped>
.menu-page {
  --brand-pink: #c85f83;
  --brand-ink: #1c1c1c;
  --brand-muted: #625c52;
  --brand-line: #e8dedb;
  max-width: 1280px;
  margin: 0 auto;
  padding: 88px 5% 100px;
  color: var(--brand-ink);
}

.menu-header {
  max-width: 900px;
  margin-bottom: 42px;
}

.eyebrow,
.menu-kicker,
.section-category {
  color: var(--brand-pink);
  font-size: 0.72rem;
  font-weight: 800;
  letter-spacing: 0.16em;
  text-transform: uppercase;
}

.eyebrow {
  margin-bottom: 25px;
}

.menu-header h1 {
  margin: 0;
  font-size: clamp(3rem, 7.2vw, 6.5rem);
  font-weight: 750;
  line-height: 1.05;
  letter-spacing: -0.055em;
}

.menu-header h1 em {
  color: var(--brand-pink);
  font-style: normal;
}

.menu-intro {
  max-width: 570px;
  margin: 25px 0 0;
  color: var(--brand-muted);
  font-size: 1.15rem;
  line-height: 1.7;
}

.header-rule {
  width: 65px;
  height: 3px;
  margin-top: 40px;
  background: var(--brand-pink);
}

.menu-kicker {
  margin-top: 18px;
}

.menu-nav {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin: 0 0 65px;
}

.menu-nav button {
  padding: 11px 19px;
  border: 1px solid #e3d7d6;
  border-radius: 100px;
  background: #fff8f1;
  color: var(--brand-ink);
  font: inherit;
  font-size: 0.88rem;
  font-weight: 650;
  cursor: pointer;
  transition: background 0.2s, color 0.2s, border-color 0.2s;
}

.menu-nav button:hover,
.menu-nav button:focus-visible {
  border-color: var(--brand-pink);
}

.menu-nav button.active {
  border-color: var(--brand-pink);
  background: var(--brand-pink);
  color: white;
}

.menu-section {
  margin-bottom: 72px;
}

.section-heading {
  display: flex;
  justify-content: space-between;
  align-items: end;
  gap: 16px;
  padding-bottom: 18px;
  margin-bottom: 20px;
  border-bottom: 2px solid var(--brand-pink);
}

.section-category {
  margin: 0 0 8px;
}

.section-heading h2 {
  margin: 0;
  font-size: clamp(1.8rem, 3.5vw, 2.65rem);
  letter-spacing: -0.045em;
}

.section-count {
  color: var(--brand-muted);
  font-size: 0.85rem;
  white-space: nowrap;
}

.items-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0 40px;
}

.menu-item {
  display: flex;
  align-items: flex-start;
  gap: 16px;
  padding: 22px 0;
  border-bottom: 1px solid var(--brand-line);
  min-width: 0;
}

.item-image {
  width: 82px;
  height: 82px;
  flex-shrink: 0;
  object-fit: cover;
  border-radius: 12px;
}

.item-body {
  flex: 1;
  min-width: 0;
}

.item-top {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  gap: 14px;
}

.item-top h3 {
  margin: 0;
  font-size: 1.08rem;
  line-height: 1.35;
}

.price {
  margin: 0;
  font-weight: 750;
  white-space: nowrap;
}

.item-description {
  margin: 9px 0 0;
  color: var(--brand-muted);
  font-size: 0.9rem;
  line-height: 1.6;
}

.dietary {
  display: flex;
  flex-wrap: wrap;
  gap: 7px;
  margin-top: 12px;
}

.dietary span {
  padding: 4px 9px;
  border-radius: 5px;
  background: #f9e7ed;
  color: #94405d;
  font-size: 0.68rem;
  font-weight: 750;
}

.menu-message {
  padding: 30px 0;
  color: var(--brand-muted);
}

.menu-footer {
  padding: 65px 20px;
  border-top: 1px solid var(--brand-line);
  text-align: center;
}

.footer-heart {
  color: var(--brand-pink);
  font-size: 2rem;
}

.menu-footer h2 {
  margin: 12px 0;
  font-size: clamp(1.7rem, 3vw, 2.4rem);
  letter-spacing: -0.04em;
}

.menu-footer p {
  color: var(--brand-muted);
  line-height: 1.7;
}

@media (max-width: 750px) {
  .menu-page {
    padding: 55px 20px 65px;
  }

  .menu-nav {
    flex-wrap: nowrap;
    overflow-x: auto;
    padding-bottom: 12px;
    margin-bottom: 42px;
    -webkit-overflow-scrolling: touch;
  }

  .menu-nav button {
    flex-shrink: 0;
  }

  .items-grid {
    grid-template-columns: 1fr;
  }

  .menu-section {
    margin-bottom: 52px;
  }
}

@media (max-width: 420px) {
  .item-image {
    width: 64px;
    height: 64px;
  }

  .item-top {
    gap: 10px;
  }

  .item-top h3 {
    font-size: 1rem;
  }
}
</style>
