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