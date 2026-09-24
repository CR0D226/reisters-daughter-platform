import { createRouter, createWebHistory } from 'vue-router'

import HomeView from '@/views/HomeView.vue'
import MenuView from '@/views/MenuView.vue'
import AboutView from '@/views/AboutView.vue'
import CateringView from '@/views/CateringView.vue'
import AdminInquiriesView from '@/views/AdminInquiriesView.vue'
import AdminInquiryDetailView from '@/views/AdminInquiryDetailView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),

  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/menu',
      name: 'menu',
      component: MenuView,
    },
    {
      path: '/about',
      name: 'about',
      component: AboutView,
    },
    {
      path: '/catering',
      name: 'catering',
      component: CateringView,
    },
    {
  path: '/admin/inquiries',
  name: 'admin-inquiries',
  component: AdminInquiriesView,
    },
    {
  path: '/admin/inquiries/:id',
  name: 'admin-inquiry-detail',
  component: AdminInquiryDetailView,
},
  ],
})

export default router