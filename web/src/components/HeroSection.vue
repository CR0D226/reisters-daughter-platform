<script setup lang="ts">
import heroImage from '@/assets/images/front-bar-area.jpg'
</script>
<template>
  <section class="hero">
    <div class="hero-copy">
      <p class="eyebrow">Reisterstown, Maryland</p>

      <h1>
        Coffee.<br />
        Pastries.<br />
        Community.
      </h1>

      <p class="hero-description">
        A neighborhood café and bakery serving handcrafted drinks,
        fresh pastries, and good food in the heart of Reisterstown.
      </p>

      <div class="hero-actions">
        <a class="primary-button" href="#">
          Order Online
        </a>

        <a class="secondary-button" href="/menu">
          Explore Our Menu
        </a>
      </div>
    </div>

    <div class="hero-image">
      <img
    :src="heroImage"
    alt="Interior of The Reister's Daughter café"
  />
    </div>
  </section>
</template>

<style scoped>
.hero {
  min-height: calc(100vh - 88px);
  display: grid;
  grid-template-columns: 1fr 1fr;
  background: #f7f4ee;
}

.hero-copy {
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: 80px 10%;
}

.eyebrow {
  margin: 0 0 30px;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.16em;
  text-transform: uppercase;
}

h1 {
  margin: 0;
  font-size: clamp(4rem, 7vw, 7.5rem);
  line-height: 0.86;
  letter-spacing: -0.065em;
}

.hero-description {
  max-width: 560px;
  margin: 40px 0 0;
  font-size: 1.1rem;
  line-height: 1.65;
}

.hero-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 14px;
  margin-top: 36px;
}

.primary-button,
.secondary-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 50px;
  padding: 0 24px;
  border-radius: 999px;
  font-weight: 600;
  transition:
    transform 0.2s ease,
    background 0.2s ease,
    color 0.2s ease;
}

.primary-button {
  background: #1c1c1c;
  color: #ffffff;
}

.secondary-button {
  border: 1px solid #1c1c1c;
}

.primary-button:hover,
.secondary-button:hover {
  transform: translateY(-2px);
}

.secondary-button:hover {
  background: #1c1c1c;
  color: #ffffff;
}

.hero-image img {
  width: 100%;
  height: 100%;
  min-height: 600px;
  display: block;
  object-fit: cover;
  border-radius: 4px;
}

.image-placeholder {
  width: 100%;
  height: 100%;
  min-height: 600px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  background: #d8d1c4;
  color: #625c52;
  font-size: 0.85rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

@media (max-width: 900px) {
  .hero {
    grid-template-columns: 1fr;
  }

  .hero-copy {
    padding: 70px 20px;
  }

  h1 {
    font-size: clamp(3.8rem, 16vw, 6rem);
  }

  .hero-image {
    padding: 0 20px 20px;
  }

  .hero-image img {
  min-height: 450px;
  max-height: 600px;
}
}

@media (max-width: 600px) {
  .hero-actions {
    flex-direction: column;
  }

  .primary-button,
  .secondary-button {
    width: 100%;
  }
}
</style>