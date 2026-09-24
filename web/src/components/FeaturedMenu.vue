<script setup lang="ts">
import pastriesImage from '@/assets/images/pastries.jpeg'
import coffeeImage from '@/assets/images/coffe-latte.jpeg'
import smoothiesImage from '@/assets/images/smoothies.jpg'
import teaImage from '@/assets/images/tea.jpg'
import veganImage from '@/assets/images/vegan-sandwich.jpeg'
import breadImage from '@/assets/images/breads.jpeg'

const menuCategories = [
  {
    name: 'Pastries',
    description: 'Fresh-baked pastries made in house.',
    image: pastriesImage,
  },
  {
    name: 'Coffee + Lattes',
    description: 'Espresso, coffee, and handcrafted lattes.',
    image: coffeeImage,
  },
  {
    name: 'Smoothies',
    description: 'Blended fruit smoothies made to order.',
    image: smoothiesImage,
  },
  {
    name: 'Tea + Matcha',
    description: 'Matcha, hojicha, chai, and specialty teas.',
    image: teaImage,
  },
  {
    name: 'Vegan',
    description: 'Plant-based pastries and café favorites.',
    image: veganImage,
  },
  {
    name: 'Bread',
    description: 'Fresh-baked loaves and artisan breads.',
    image: breadImage,
  },
]
</script>

<template>
  <section class="featured-menu">
    <div class="section-heading">
      <p class="eyebrow">Explore our menu</p>
      <h2>Something for everyone.</h2>
    </div>

    <div class="menu-grid">
      <article
        v-for="category in menuCategories"
        :key="category.name"
        class="menu-card"
      >
        <div class="image-wrapper">
          <img
            :src="category.image"
            :alt="category.name"
            class="menu-image"
          />
        </div>

        <div class="menu-content">
          <h3>{{ category.name }}</h3>

          <p class="description">
            {{ category.description }}
          </p>

          <span class="explore-link">
            Explore →
          </span>
        </div>
      </article>
    </div>
  </section>
</template>

<style scoped>
.featured-menu {
  padding: 100px 6%;
  background: #ffffff;
}

.section-heading {
  max-width: 700px;
  margin-bottom: 60px;
}

.eyebrow {
  margin: 0 0 16px;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.16em;
  text-transform: uppercase;
}

h2 {
  margin: 0;
  font-size: clamp(2.5rem, 5vw, 4.5rem);
  line-height: 1;
  letter-spacing: -0.05em;
}

.menu-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 24px;
}

.menu-card {
  display: flex;
  flex-direction: column;
}

.image-wrapper {
  width: 100%;
  aspect-ratio: 4 / 3;
  overflow: hidden;
  background: #f0ece4;
}

.menu-image {
  width: 100%;
  height: 100%;
  display: block;
  object-fit: cover;
  transition: transform 0.4s ease;
}

.menu-card:hover .menu-image {
  transform: scale(1.03);
}

.menu-content {
  display: flex;
  flex-direction: column;
  flex: 1;
  padding: 24px 0 10px;
}

h3 {
  margin: 0;
  font-size: 1.6rem;
  letter-spacing: -0.03em;
}

.description {
  max-width: 320px;
  margin: 12px 0 0;
  line-height: 1.6;
  color: #625c52;
}

.explore-link {
  margin-top: 24px;
  font-size: 0.8rem;
  font-weight: 700;
  letter-spacing: 0.1em;
  text-transform: uppercase;
}

@media (max-width: 900px) {
  .featured-menu {
    padding: 70px 20px;
  }

  .menu-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 600px) {
  .menu-grid {
    grid-template-columns: 1fr;
  }

  .section-heading {
    margin-bottom: 40px;
  }
}
</style>