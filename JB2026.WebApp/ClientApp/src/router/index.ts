import { createRouter, createWebHistory } from 'vue-router'
import { useSessionStore } from '@/stores/session'

const router = createRouter({
  history: createWebHistory('/app/'),
  routes: [
    {
      path: '/',
      redirect: '/dashboard',
    },
    {
      path: '/dashboard',
      name: 'dashboard',
      component: () => import('@/views/DashboardView.vue'),
      meta: { requiresAuth: true, title: 'Dashboard' },
    },
    {
      path: '/jobs',
      name: 'jobs',
      component: () => import('@/views/JobsView.vue'),
      meta: { requiresAuth: true, title: 'Jobs' },
    },
    {
      path: '/quotations',
      name: 'quotations',
      component: () => import('@/views/QuotationsView.vue'),
      meta: { requiresAuth: true, title: 'Quotations' },
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue'),
      meta: { requiresAuth: false, title: 'Sign in' },
    },
    {
      path: '/editor',
      name: 'editor',
      component: () => import('@/views/EditorView.vue'),
      meta: { requiresAuth: true, title: 'Rich Text' },
    },
    {
      path: '/scheduler',
      name: 'scheduler',
      component: () => import('@/views/SchedulerView.vue'),
      meta: { requiresAuth: true, title: 'Scheduler' },
    },
    {
      path: '/job-order',
      name: 'job-order',
      component: () => import('@/views/JobOrderView.vue'),
      meta: { requiresAuth: true, title: 'Job Order' },
    },
    {
      path: '/sml',
      name: 'sml',
      component: () => import('@/views/SmlView.vue'),
      meta: { requiresAuth: true, title: 'SML' },
    },
    {
      path: '/stock',
      name: 'stock',
      component: () => import('@/views/StockView.vue'),
      meta: { requiresAuth: true, title: 'Stock' },
    },
    {
      path: '/reports',
      name: 'reports',
      component: () => import('@/views/ReportsView.vue'),
      meta: { requiresAuth: true, title: 'Reports' },
    },
    {
      path: '/admin',
      name: 'admin',
      component: () => import('@/views/AdminView.vue'),
      meta: { requiresAuth: true, title: 'Admin' },
    },
    {
      path: '/public',
      name: 'public',
      component: () => import('@/views/PublicView.vue'),
      meta: { requiresAuth: true, title: 'Public' },
    },
    {
      path: '/settings',
      name: 'settings',
      component: () => import('@/views/SettingsView.vue'),
      meta: { requiresAuth: true, title: 'Settings' },
    },
    {
      path: '/help',
      name: 'help',
      component: () => import('@/views/HelpView.vue'),
      meta: { requiresAuth: true, title: 'Help' },
    },
  ],
})

router.beforeEach(async (to) => {
  document.title = `${String(to.meta.title ?? 'JB2026')} | JB2026`

  const sessionStore = useSessionStore()
  if (sessionStore.isAuthenticated && !sessionStore.profile) {
    await sessionStore.bootstrapProfile()
  }

  if (to.meta.requiresAuth && !sessionStore.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.name === 'login' && sessionStore.isAuthenticated) {
    return { name: 'dashboard' }
  }

  return true
})

export default router