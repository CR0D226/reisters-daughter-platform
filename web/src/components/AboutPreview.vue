<template>
  <section class="about-preview">
    <div class="about-label">
      <p>Our Story</p>
    </div>

    <div class="about-content">
      <h2>
        Locally Owned.<br />
        Community-Focused.<br />
        Rooted in Craft.
      </h2>

      <p class="intro">
        The Reister’s Daughter is a family-owned coffee and tea shop in the
        historic Beckley House, proudly operated by local Reisterstown
        residents. We’re rooted in quality, hospitality, and a connection
        to the community we call home.
      </p>

      <p>
        From Maryland-roasted Ceremony Coffee and house-made pastries to
        our own loose-leaf tea blends, everything we serve reflects our
        love of craft and the neighborhood around us.
      </p>

      <a href="/about" class="about-link">
        Our Story →
      </a>
    </div>
  </section>
</template>

<style scoped>
.about-preview {
  display: grid;
  grid-template-columns: 1fr 2fr;
  gap: 60px;
  padding: 120px 6%;
  background: #f7f4ee;
}

.about-label p {
  margin: 0;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.16em;
  text-transform: uppercase;
}

.about-content {
  max-width: 950px;
}

h2 {
  margin: 0;
  font-size: clamp(2.7rem, 5vw, 5rem);
  line-height: 0.98;
  letter-spacing: -0.055em;
}

.about-content > p {
  max-width: 680px;
  margin: 24px 0 0;
  font-size: 1.05rem;
  line-height: 1.75;
  color: #4f4a43;
}

.about-content .intro {
  margin-top: 40px;
  font-size: 1.15rem;
  color: #1c1c1c;
}

.about-link {
  display: inline-block;
  margin-top: 36px;
  padding-bottom: 4px;
  border-bottom: 1px solid currentColor;
  font-size: 0.85rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

@media (max-width: 700px) {
  .about-preview {
    grid-template-columns: 1fr;
    gap: 30px;
    padding: 80px 20px;
  }
}
</style>