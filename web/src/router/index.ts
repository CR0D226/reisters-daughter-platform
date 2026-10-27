
import {
  createRouter,
  createWebHistory,
} from 'vue-router'

import HomeView from '@/views/HomeView.vue'
import MenuView from '@/views/MenuView.vue'
import AboutView from '@/views/AboutView.vue'
import CateringView from '@/views/CateringView.vue'
import JobsView from '@/views/JobsView.vue'
import EventsView from '@/views/EventsView.vue'
import PublicEventDetailView from '@/views/PublicEventDetailView.vue'
import PublicQuoteView from '@/views/PublicQuoteView.vue'

import AdminLayout from '@/components/admin/AdminLayout.vue'
import AdminDashboardView from '@/views/AdminDashboardView.vue'
import AdminInquiriesView from '@/views/AdminInquiriesView.vue'
import AdminApplicantsView from '@/views/AdminApplicantsView.vue'
import AdminApplicantDetailView from '@/views/AdminApplicantDetailView.vue'
import AdminInquiryDetailView from '@/views/AdminInquiryDetailView.vue'
import AdminQuoteDetailView from '@/views/AdminQuoteDetailView.vue'
import AdminBookingsView from '@/views/AdminBookingsView.vue'
import AdminBookingDetailView from '@/views/AdminBookingDetailView.vue'
import AdminCustomersView from '@/views/AdminCustomersView.vue'
import AdminCustomerDetailView from '@/views/AdminCustomerDetailView.vue'
import AdminEventsView from '@/views/AdminEventsView.vue'
import AdminEventDetailView from '@/views/AdminEventDetailView.vue'
import AdminUsersView from '@/views/AdminUsersView.vue'

import EmployeeLayout from '@/components/employee/EmployeeLayout.vue'
import EmployeeDashboardView from '@/views/EmployeeDashboardView.vue'

import keycloak from '@/auth/keycloak'
import { apiFetch } from '@/services/api'

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
      path: '/jobs',
      name: 'jobs',
      component: JobsView,
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
    // ADMIN PORTAL
    // =====================================================

    {
      path: '/admin',
      component: AdminLayout,
      meta: {
        requiresAuth: true,
        requiredRole: 'Manager',
      },

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
          path: 'applicants',
          name: 'admin-applicants',
          component: AdminApplicantsView,
        },
        {
          path: 'applicants/:id',
          name: 'admin-applicant-detail',
          component: AdminApplicantDetailView,
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
        {
  path: 'availability',
  name: 'admin-availability',
  component: () =>
    import('../views/AdminAvailabilityView.vue'),
},
{
  path: 'schedule',
  name: 'admin-schedule',
  component: () =>
    import('../views/AdminScheduleView.vue'),
},
      ],
    },

    // =====================================================
    // EMPLOYEE PORTAL
    // =====================================================

  {
  path: '/employee',
  component: EmployeeLayout,
  meta: {
    requiresAuth: true,
    requiredRole: 'Employee',
  },
  children: [
    {
      path: '',
      name: 'employee-dashboard',
      component: EmployeeDashboardView,
    },
    {
  path: 'schedule',
  name: 'employee-schedule',
  component: () =>
    import('../views/EmployeeScheduleView.vue'),
},
    {
      path: 'availability',
      name: 'employee-availability',
      component: () =>
        import('../views/EmployeeAvailabilityView.vue'),
    },
    {
      path: 'biweekly-availability',
      name: 'employee-biweekly-availability',
      component: () =>
        import('../views/EmployeeBiweeklyAvailabilityView.vue'),
    },
  ],
},
  ],
})

// =====================================================
// AUTHENTICATION + ROLE AUTHORIZATION
// =====================================================

router.beforeEach(async (to) => {
  const requiresAuth = to.matched.some(
    (record) => record.meta.requiresAuth === true,
  )

  // Public pages are accessible without authentication.
  if (!requiresAuth) {
    return true
  }

  // Redirect unauthenticated users to Keycloak.
  if (!keycloak.authenticated) {
    await keycloak.login({
      redirectUri: `${window.location.origin}${to.path}`,
    })

    return false
  }

  try {
    // Verify the logged-in user's identity and role
    // against our backend, not just the JWT contents.
    const response = await apiFetch(
      '/api/auth/app-user',
    )

    if (!response.ok) {
      console.error(
        'Unable to authorize user:',
        response.status,
      )

      return '/'
    }

    const user: {
      role: string
    } = await response.json()

    const requiredRole = to.matched
      .map((record) => record.meta.requiredRole)
      .find((role) => typeof role === 'string')

    // Only Managers can access /admin.
    if (
      requiredRole === 'Manager' &&
      user.role !== 'Manager'
    ) {
      return '/employee'
    }

    // Employees and Managers can access /employee.
    if (
      requiredRole === 'Employee' &&
      user.role !== 'Employee' &&
      user.role !== 'Manager'
    ) {
      return '/'
    }

    return true
  } catch (error) {
    console.error(
      'Authorization failed:',
      error,
    )

    return '/'
  }
})

export default router
