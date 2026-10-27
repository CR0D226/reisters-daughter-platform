<script setup lang="ts">
import { computed } from 'vue'
import {
  RouterView,
  useRoute,
} from 'vue-router'

import SiteHeader from '@/components/SiteHeader.vue'
import SiteFooter from '@/components/SiteFooter.vue'
import FloatingOrderButton from '@/components/FloatingOrderButton.vue'

const route = useRoute()

const isPortal = computed(() =>
  route.path.startsWith('/admin') ||
  route.path.startsWith('/employee')
)
</script>

<template>
  <template v-if="isPortal">
  <RouterView />
</template>

  <template v-else>
    <SiteHeader />

    <RouterView />

    <SiteFooter />

    <FloatingOrderButton />
  </template>
</template>