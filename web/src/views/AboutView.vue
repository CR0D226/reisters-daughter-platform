<script setup lang="ts">
import cafeImage from '@/assets/images/front-bar-area.jpg'
import coffeeImage from '@/assets/images/coffe-latte.jpeg'
import teaImage from '@/assets/images/tea.jpg'
</script>

<template>
  <main class="about-page">

    <!-- INTRO -->
    <section class="about-hero">
      <div class="hero-copy">
        <p class="eyebrow">Our Story</p>

        <h1>
          Locally Owned.<br />
          Community-Focused.<br />
          Rooted in Craft.
        </h1>

        <p class="lead">
          Welcome to The Reister’s Daughter, a family-owned coffee and tea
          shop proudly operated by local Reisterstown residents.
        </p>
      </div>

      <img
        :src="cafeImage"
        alt="Inside The Reister's Daughter café"
      />
    </section>

    <!-- INTRO STORY -->
    <section class="story-section">
      <div class="section-label">
        <p>The Reister's Daughter</p>
      </div>

      <div class="story-copy">
        <p>
          Located in the historic Beckley House, our café is a reflection
          of everything we value: quality, hospitality, and a connection
          to this community we call home.
        </p>

        <p>
          We opened with the intention of creating a warm and welcoming
          space; one that honors local history while offering exceptional
          coffee, tea and house-made baked goods.
        </p>

        <p>
          Whether you’re popping in for a morning pick-me-up or sitting in
          with friends for a cozy afternoon, we hope you feel part of
          something familiar and meaningful.
        </p>
      </div>
    </section>

    <!-- COFFEE -->
    <section class="feature-section">
      <img
        :src="coffeeImage"
        alt="Coffee at The Reister's Daughter"
      />

      <div class="feature-copy">
        <p class="eyebrow">Coffee</p>
        <h2>Thoughtfully Sourced Coffee</h2>

        <p>
          We proudly brew Ceremony Coffee Roasters, a fellow Maryland-based
          company known for ethically sourced, small-batch beans.
        </p>

        <p>
          Every drink — from drip coffee to latte — is made to bring out
          the best in the coffee and to savor the moment. Our baristas are
          trained to approach every cup with the same care and detail
          Ceremony puts into their roasting.
        </p>
      </div>
    </section>

    <!-- TEA -->
    <section class="feature-section reverse">
      <img
        :src="teaImage"
        alt="Tea at The Reister's Daughter"
      />

      <div class="feature-copy">
        <p class="eyebrow">Tea</p>
        <h2>A Passion for Tea</h2>

        <p>
          Tea is more than an offering here — it’s part of our foundation.
          Co-owner Kelsey, a trained tea specialist, studied under a Chinese
          tea master in New York and worked in a traditional Japanese tea
          shop before bringing her passion to Reisterstown.
        </p>

        <p>
          We create our own house-made loose leaf tea blends, available by
          the cup in the shop or packaged for you to enjoy at home.
        </p>

        <p>
          Our curated tea menu includes herbal, green, white, black, and
          seasonal blends — all crafted with an emphasis on quality and
          flavor.
        </p>
      </div>
    </section>

    <!-- HISTORY -->
    <section class="text-section">
      <p class="eyebrow">Since 1779</p>
      <h2>Our Home in History</h2>

      <div class="text-columns">
        <p>
          The Beckley House, built in 1779, is one of Reisterstown’s oldest
          surviving structures in the town.
        </p>

        <p>
          Inside, you’ll find a blend of past and present: exposed wooden
          beams, original brick walls, and a front bar repurposed from the
          wood paneling once used when the building was The Ski Shop.
        </p>

        <p>
          It’s a cozy, character-filled space that honors the past while
          welcoming today’s conversations and connections.
        </p>
      </div>
    </section>

    <!-- COMMUNITY -->
    <section class="text-section">
      <p class="eyebrow">Reisterstown</p>
      <h2>Part of the Neighborhood</h2>

      <div class="text-columns">
        <p>
          We love being located in the heart of Reisterstown’s Main Street —
          and we’re proud to support and highlight other local favorites.
        </p>

        <p>
          In the summer, grab your drink and head across the street to The
          Cow for some ice cream, or relax in Reister’s Desire Park just
          behind our shop, with peaceful seating connected to our parking lot.
        </p>

        <p>
          Our café is a place to unwind, catch up, or simply enjoy the
          rhythm of your day.
        </p>
      </div>
    </section>

    <!-- CLOSING -->
    <section class="closing-section">
      <p class="eyebrow">Always Something Brewing</p>

      <h2>We’re Glad You’re Here.</h2>

      <p>
        From high tea events and specialty lattes to limited-release tea
        blends and locally curated goods, we’re always looking for ways to
        connect and create.
      </p>

      <p>
        Whether you’re joining us for your first visit or your hundredth,
        thank you for supporting a truly local, family-run shop. We love
        this town and we’re honored to be part of its story, one cup at a
        time.
      </p>

      <RouterLink to="/menu" class="menu-button">
        Explore Our Menu →
      </RouterLink>
    </section>

  </main>
</template>
<style scoped>
.about-page {
  background: #f7f4ee;
  color: #1c1c1c;
}

/* HERO */

.about-hero {
  display: grid;
  grid-template-columns: 1fr 1fr;
  min-height: 720px;
}

.hero-copy {
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: 90px 8%;
}

.eyebrow {
  margin: 0 0 20px;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.16em;
  text-transform: uppercase;
}

.about-hero h1 {
  margin: 0;
  font-size: clamp(3.5rem, 6vw, 6.5rem);
  line-height: 0.92;
  letter-spacing: -0.055em;
}

.lead {
  max-width: 550px;
  margin-top: 35px;
  font-size: 1.15rem;
  line-height: 1.7;
  color: #625c52;
}

.about-hero img {
  width: 100%;
  height: 100%;
  min-height: 650px;
  object-fit: cover;
}


/* INTRO STORY */

.story-section {
  display: grid;
  grid-template-columns: 1fr 2fr;
  gap: 80px;

  max-width: 1200px;
  margin: 0 auto;
  padding: 120px 6%;
}

.section-label p {
  margin: 0;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.15em;
  text-transform: uppercase;
}

.story-copy {
  max-width: 760px;
}

.story-copy p {
  margin: 0 0 25px;
  font-size: 1.35rem;
  line-height: 1.65;
}


/* COFFEE + TEA */

.feature-section {
  display: grid;
  grid-template-columns: 1fr 1fr;
  align-items: stretch;
  min-height: 650px;
}

.feature-section.reverse img {
  order: 2;
}

.feature-section.reverse .feature-copy {
  order: 1;
}

.feature-section img {
  width: 100%;
  height: 100%;
  min-height: 600px;
  object-fit: cover;
}

.feature-copy {
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: 80px 10%;
}

.feature-copy h2 {
  max-width: 600px;
  margin: 0 0 30px;
  font-size: clamp(2.7rem, 4vw, 4.5rem);
  line-height: 1;
  letter-spacing: -0.045em;
}

.feature-copy p:not(.eyebrow) {
  max-width: 600px;
  margin: 0 0 20px;
  line-height: 1.7;
  color: #625c52;
}


/* HISTORY + COMMUNITY */

.text-section {
  max-width: 1200px;
  margin: 0 auto;
  padding: 120px 6%;
}

.text-section + .text-section {
  padding-top: 20px;
}

.text-section h2 {
  max-width: 800px;
  margin: 0 0 50px;
  font-size: clamp(3rem, 5vw, 5rem);
  line-height: 1;
  letter-spacing: -0.05em;
}

.text-columns {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 50px;
}

.text-columns p {
  margin: 0;
  line-height: 1.7;
  color: #625c52;
}


/* CLOSING */

.closing-section {
  max-width: 900px;
  margin: 80px auto 0;
  padding: 120px 6% 140px;
  text-align: center;
}

.closing-section h2 {
  margin: 0 0 35px;
  font-size: clamp(3.5rem, 6vw, 6rem);
  line-height: 0.95;
  letter-spacing: -0.05em;
}

.closing-section > p:not(.eyebrow) {
  max-width: 700px;
  margin: 0 auto 22px;
  line-height: 1.7;
  color: #625c52;
}

.menu-button {
  display: inline-block;
  margin-top: 25px;
  padding: 15px 24px;

  background: #1c1c1c;
  color: white;

  font-weight: 700;
  border-radius: 999px;

  transition: transform 0.2s ease;
}

.menu-button:hover {
  transform: translateY(-2px);
}


/* TABLET */

@media (max-width: 900px) {
  .about-hero {
    grid-template-columns: 1fr;
  }

  .about-hero img {
    min-height: 500px;
  }

  .story-section {
    grid-template-columns: 1fr;
    gap: 35px;
  }

  .feature-section,
  .feature-section.reverse {
    grid-template-columns: 1fr;
  }

  .feature-section.reverse img,
  .feature-section.reverse .feature-copy {
    order: initial;
  }

  .feature-section img {
    min-height: 500px;
  }

  .text-columns {
    grid-template-columns: 1fr;
    gap: 25px;
  }
}


/* MOBILE */

@media (max-width: 600px) {
  .hero-copy {
    padding: 80px 20px 60px;
  }

  .about-hero h1 {
    font-size: clamp(3rem, 14vw, 4.5rem);
  }

  .about-hero img {
    min-height: 400px;
  }

  .story-section {
    padding: 80px 20px;
  }

  .story-copy p {
    font-size: 1.15rem;
  }

  .feature-copy {
    padding: 70px 20px;
  }

  .feature-section img {
    min-height: 400px;
  }

  .text-section {
    padding: 80px 20px;
  }

  .text-section + .text-section {
    padding-top: 10px;
  }

  .closing-section {
    margin-top: 20px;
    padding: 90px 20px 110px;
  }
}
</style>