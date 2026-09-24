<script setup lang="ts">
import { RouterView } from 'vue-router'
import SiteHeader from '@/components/SiteHeader.vue'
import SiteFooter from '@/components/SiteFooter.vue'
import FloatingOrderButton from '@/components/FloatingOrderButton.vue'
</script>

<template>
  <SiteHeader />

  <RouterView />

  <SiteFooter />

  <FloatingOrderButton />
</template>