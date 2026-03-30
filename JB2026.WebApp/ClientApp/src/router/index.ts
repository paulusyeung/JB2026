import { createRouter, createWebHistory } from 'vue-router'
import { watch } from 'vue'
import { useSessionStore } from '@/stores/session'
import { i18n } from '@/i18n'

function resolveTitle(titleKey?: string): string {
  if (!titleKey) {
    return i18n.global.t('common.appName')
  }

  return i18n.global.t(titleKey)
}

function applyDocumentTitle(titleKey?: string) {
  const title = resolveTitle(titleKey)
  const suffix = i18n.global.t('app.titleSuffix')
  document.title = `${title} | ${suffix}`
}

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
      meta: { requiresAuth: true, titleKey: 'routes.dashboard' },
    },
    {
      path: '/jobs',
      name: 'jobs',
      component: () => import('@/views/JobsView.vue'),
      meta: { requiresAuth: true, titleKey: 'routes.jobs' },
    },
    {
      path: '/quotations',
      name: 'quotations',
      component: () => import('@/views/QuotationsView.vue'),
      meta: { requiresAuth: true, titleKey: 'routes.quotations' },
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue'),
      meta: { requiresAuth: false, titleKey: 'routes.login' },
    },
    {
      path: '/editor',
      name: 'editor',
      component: () => import('@/views/EditorView.vue'),
      meta: { requiresAuth: true, titleKey: 'routes.editor' },
    },
    {
      path: '/scheduler',
      name: 'scheduler',
      component: () => import('@/views/SchedulerView.vue'),
      meta: { requiresAuth: true, titleKey: 'routes.scheduler' },
    },
    {
      path: '/job-order',
      name: 'job-order',
      component: () => import('@/views/JobOrderView.vue'),
      meta: { requiresAuth: true, titleKey: 'routes.jobOrder' },
    },
    {
      path: '/sml',
      name: 'sml',
      component: () => import('@/views/SmlView.vue'),
      meta: { requiresAuth: true, titleKey: 'routes.sml' },
    },
    {
      path: '/stock',
      name: 'stock',
      component: () => import('@/views/StockView.vue'),
      meta: { requiresAuth: true, titleKey: 'routes.stock' },
    },
    {
      path: '/reports',
      name: 'reports',
      component: () => import('@/views/ReportsView.vue'),
      meta: { requiresAuth: true, titleKey: 'routes.reports' },
    },
    {
      path: '/admin',
      name: 'admin',
      component: () => import('@/views/AdminView.vue'),
      meta: { requiresAuth: true, titleKey: 'routes.admin' },
    },
    {
      path: '/public',
      name: 'public',
      component: () => import('@/views/PublicView.vue'),
      meta: { requiresAuth: true, titleKey: 'routes.public' },
    },
    {
      path: '/settings',
      name: 'settings',
      component: () => import('@/views/SettingsView.vue'),
      meta: { requiresAuth: true, titleKey: 'routes.settings' },
    },
    {
      path: '/help',
      name: 'help',
      component: () => import('@/views/HelpView.vue'),
      meta: { requiresAuth: true, titleKey: 'routes.help' },
    },
  ],
})

router.beforeEach(async (to) => {
  applyDocumentTitle(typeof to.meta.titleKey === 'string' ? to.meta.titleKey : undefined)

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

// Keep title in sync when locale changes after the user has already navigated.
router.afterEach((to) => {
  applyDocumentTitle(typeof to.meta.titleKey === 'string' ? to.meta.titleKey : undefined)
})

watch(
  () => i18n.global.locale.value,
  () => {
    const currentRoute = router.currentRoute.value
    const titleKey = typeof currentRoute.meta.titleKey === 'string' ? currentRoute.meta.titleKey : undefined
    applyDocumentTitle(titleKey)
  },
)

export default router