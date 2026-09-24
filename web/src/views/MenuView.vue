<script setup lang="ts">
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

onMounted(async () => {
  try {
    const response = await fetch('http://localhost:5128/api/menu')

    if (!response.ok) {
      throw new Error('Unable to load menu.')
    }

    menuItems.value = await response.json()
  } catch (err) {
    console.error(err)
    error.value = 'We could not load the menu.'
  } finally {
    loading.value = false
  }
})

const menuSections = computed(() => {
  const sections: Record<string, MenuItem[]> = {}

  for (const item of menuItems.value) {
    const section = sections[item.subcategory]

    if (section) {
      section.push(item)
    } else {
      sections[item.subcategory] = [item]
    }
  }

  return sections
})
</script>

<template>
  <main class="menu-page">
    <header class="menu-header">
      <p class="eyebrow">The Reister's Daughter</p>
      <h1>Our Menu</h1>

      <p>
        Coffee, tea, pastries and café favorites made with care
        in Reisterstown.
      </p>
    </header>

    <p v-if="loading">Loading menu...</p>

    <p v-else-if="error">
      {{ error }}
    </p>

    <div v-else class="menu-sections">
      <section
        v-for="(items, section) in menuSections"
        :key="section"
        class="menu-section"
      >
        <h2>{{ section }}</h2>

        <article
          v-for="item in items"
          :key="item.id"
          class="menu-item"
        >
          <div class="item-info">
            <h3>{{ item.name }}</h3>

            <p v-if="item.description">
              {{ item.description }}
            </p>

            <div class="dietary">
              <span v-if="item.vegan">Vegan</span>
              <span v-if="item.glutenFree">GF</span>
            </div>
          </div>

          <p class="price">
            ${{ item.price.toFixed(2) }}{{ item.priceVaries ? '+' : '' }}
          </p>
        </article>
      </section>
    </div>

    <p class="menu-note">
      Prices and selections may vary. Visit our online ordering page
      for current availability.
    </p>
  </main>
</template>

<style scoped>
.menu-page {
  max-width: 1100px;
  margin: 0 auto;
  padding: 100px 6%;
}

.menu-header {
  max-width: 700px;
  margin-bottom: 80px;
}

.eyebrow {
  margin: 0 0 16px;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.16em;
  text-transform: uppercase;
}

h1 {
  margin: 0;
  font-size: clamp(3.5rem, 8vw, 7rem);
  line-height: 0.9;
  letter-spacing: -0.06em;
}

.menu-header > p:last-child {
  max-width: 550px;
  margin-top: 30px;
  font-size: 1.1rem;
  line-height: 1.7;
  color: #625c52;
}

.menu-section {
  margin-bottom: 70px;
}

.menu-section h2 {
  margin: 0 0 25px;
  padding-bottom: 15px;
  border-bottom: 2px solid #1c1c1c;
  font-size: 2rem;
  letter-spacing: -0.04em;
}

.menu-item {
  display: flex;
  justify-content: space-between;
  gap: 40px;
  padding: 22px 0;
  border-bottom: 1px solid #d9d3c8;
}

.item-info {
  max-width: 700px;
}

h3 {
  margin: 0;
  font-size: 1.15rem;
}

.item-info > p {
  margin: 8px 0 0;
  line-height: 1.5;
  color: #625c52;
}

.price {
  margin: 0;
  white-space: nowrap;
  font-weight: 700;
}

.dietary {
  display: flex;
  gap: 8px;
  margin-top: 10px;
}

.dietary span {
  font-size: 0.7rem;
  font-weight: 700;
  text-transform: uppercase;
}

.menu-note {
  margin-top: 80px;
  padding-top: 30px;
  border-top: 1px solid #d9d3c8;
  color: #625c52;
  font-size: 0.9rem;
}

@media (max-width: 600px) {
  .menu-page {
    padding: 70px 20px;
  }

  .menu-header {
    margin-bottom: 55px;
  }

  .menu-item {
    gap: 20px;
  }
}
</style>