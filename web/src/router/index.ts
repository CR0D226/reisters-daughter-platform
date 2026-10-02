import {
  createRouter,
  createWebHistory,
} from 'vue-router'

import HomeView from '@/views/HomeView.vue'
import MenuView from '@/views/MenuView.vue'
import AboutView from '@/views/AboutView.vue'
import CateringView from '@/views/CateringView.vue'
import EventsView from '@/views/EventsView.vue'
import PublicEventDetailView from '@/views/PublicEventDetailView.vue'

import PublicQuoteView from '@/views/PublicQuoteView.vue'

import AdminLayout from '@/components/admin/AdminLayout.vue'
import AdminDashboardView from '@/views/AdminDashboardView.vue'
import AdminInquiriesView from '@/views/AdminInquiriesView.vue'
import AdminInquiryDetailView from '@/views/AdminInquiryDetailView.vue'
import AdminQuoteDetailView from '@/views/AdminQuoteDetailView.vue'
import AdminBookingsView from '@/views/AdminBookingsView.vue'
import AdminBookingDetailView from '@/views/AdminBookingDetailView.vue'
import AdminCustomersView from '@/views/AdminCustomersView.vue'
import AdminCustomerDetailView from '@/views/AdminCustomerDetailView.vue'
import AdminEventsView from '@/views/AdminEventsView.vue'
import AdminEventDetailView from '@/views/AdminEventDetailView.vue'
import AdminUsersView from '@/views/AdminUsersView.vue'

const router = createRouter({
  history: createWebHistory(
    import.meta.env.BASE_URL,
  ),

  routes: [
    // =====================================================
    // PUBLIC SITE
    // =====================================================

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
      path: '/events',
      name: 'events',
      component: EventsView,
    },
    {
      path: '/events/:id',
      name: 'event-detail',
      component: PublicEventDetailView,
    },
    {
      path: '/quote/:token',
      name: 'public-quote',
      component: PublicQuoteView,
    },

    // =====================================================
    // ADMIN
    // =====================================================

    {
      path: '/admin',
      component: AdminLayout,

      children: [
        {
          path: '',
          name: 'admin-dashboard',
          component: AdminDashboardView,
        },

        {
          path: 'inquiries',
          name: 'admin-inquiries',
          component: AdminInquiriesView,
        },
        {
          path: 'inquiries/:id',
          name: 'admin-inquiry-detail',
          component: AdminInquiryDetailView,
        },

        {
          path: 'quotes/:id',
          name: 'admin-quote-detail',
          component: AdminQuoteDetailView,
        },

        {
          path: 'bookings',
          name: 'admin-bookings',
          component: AdminBookingsView,
        },
        {
          path: 'bookings/:id',
          name: 'admin-booking-detail',
          component: AdminBookingDetailView,
        },

        {
          path: 'customers',
          name: 'admin-customers',
          component: AdminCustomersView,
        },
        {
          path: 'customers/:id',
          name: 'admin-customer-detail',
          component: AdminCustomerDetailView,
        },

        {
          path: 'events',
          name: 'admin-events',
          component: AdminEventsView,
        },
        {
          path: 'events/new',
          name: 'admin-event-new',
          component: AdminEventDetailView,
        },
        {
          path: 'events/:id',
          name: 'admin-event-detail',
          component: AdminEventDetailView,
        },
        {
  path: 'users',
  name: 'admin-users',
  component: AdminUsersView,
},
      ],
    },
  ],
})

export default router