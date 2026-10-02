import {
  createRouter,
  createWebHistory,
} from 'vue-router'

import HomeView from '@/views/HomeView.vue'
import MenuView from '@/views/MenuView.vue'
import AboutView from '@/views/AboutView.vue'
import CateringView from '@/views/CateringView.vue'

import AdminDashboardView from '@/views/AdminDashboardView.vue'

import AdminInquiriesView from '@/views/AdminInquiriesView.vue'
import AdminInquiryDetailView from '@/views/AdminInquiryDetailView.vue'

import AdminQuoteDetailView from '@/views/AdminQuoteDetailView.vue'
import PublicQuoteView from '@/views/PublicQuoteView.vue'

import AdminBookingsView from '@/views/AdminBookingsView.vue'
import AdminBookingDetailView from '@/views/AdminBookingDetailView.vue'

import AdminCustomersView from '@/views/AdminCustomersView.vue'
import AdminCustomerDetailView from '@/views/AdminCustomerDetailView.vue'

import AdminEventsView from '@/views/AdminEventsView.vue'
import AdminEventDetailView from '@/views/AdminEventDetailView.vue'
import EventsView from '@/views/EventsView.vue'
import PublicEventDetailView from '@/views/PublicEventDetailView.vue'

const router = createRouter({
  history: createWebHistory(
    import.meta.env.BASE_URL,
  ),

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
      path: '/admin',
      name: 'admin-dashboard',
      component: AdminDashboardView,
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

    {
      path: '/admin/quotes/:id',
      name: 'admin-quote-detail',
      component: AdminQuoteDetailView,
    },
    {
      path: '/quote/:token',
      name: 'public-quote',
      component: PublicQuoteView,
    },

    {
      path: '/admin/bookings',
      name: 'admin-bookings',
      component: AdminBookingsView,
    },
    {
      path: '/admin/bookings/:id',
      name: 'admin-booking-detail',
      component: AdminBookingDetailView,
    },

    {
      path: '/admin/customers',
      name: 'admin-customers',
      component: AdminCustomersView,
    },
    {
      path: '/admin/customers/:id',
      name: 'admin-customer-detail',
      component: AdminCustomerDetailView,
    },

    {
      path: '/admin/events',
      name: 'admin-events',
      component: AdminEventsView,
    },
    {
      path: '/admin/events/new',
      name: 'admin-event-new',
      component: AdminEventDetailView,
    },
    {
      path: '/admin/events/:id',
      name: 'admin-event-detail',
      component: AdminEventDetailView,
    },
    {
       path: '/events',
       name: 'events',
       component: EventsView,
    },
    {
  path: '/events/:id',
  name: 'event-detail',
  component: PublicEventDetailView,
},
  ],
})

export default router