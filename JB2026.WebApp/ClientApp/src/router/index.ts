import { createRouter, createWebHistory } from 'vue-router'
import { watch } from 'vue'
import { useSessionStore } from '@/stores/session'
import { i18n } from '@/i18n'

const legacyLeafRoutes = [
  { path: '/job-order/order-list', name: 'job-order-order-list', titleKey: 'routes.jobOrderOrderList' },
  { path: '/job-order/job-list', name: 'job-order-job-list', titleKey: 'routes.jobOrderJobList' },
  { path: '/job-order/job-stats', name: 'job-order-job-stats', titleKey: 'routes.jobOrderJobStats' },
  { path: '/job-order/schedule/pending', name: 'job-order-schedule-pending', titleKey: 'routes.jobOrderSchedulePending' },
  { path: '/job-order/schedule/scheduled', name: 'job-order-schedule-scheduled', titleKey: 'routes.jobOrderScheduleScheduled' },
  { path: '/job-order/schedule/completed', name: 'job-order-schedule-completed', titleKey: 'routes.jobOrderScheduleCompleted' },
  { path: '/job-order/schedule/packing', name: 'job-order-schedule-packing', titleKey: 'routes.jobOrderSchedulePackingOnAir' },
  { path: '/job-order/sml/rtf-list', name: 'job-order-sml-rtf-list', titleKey: 'routes.smlRtfList' },
  { path: '/job-order/sml/invoice-list', name: 'job-order-sml-invoice-list', titleKey: 'routes.smlInvoiceList' },
  { path: '/job-order/sml/rtf-stats', name: 'job-order-sml-rtf-stats', titleKey: 'routes.smlRtfStats' },
  { path: '/job-order/sml/invoice-stats', name: 'job-order-sml-invoice-stats', titleKey: 'routes.smlInvoiceStats' },
  { path: '/job-order/reports/exceptional', name: 'job-order-reports-exceptional', titleKey: 'routes.reportsExceptionalReport' },
  { path: '/stock/product', name: 'stock-product', titleKey: 'routes.stockProduct' },
  { path: '/admin/workflow', name: 'admin-workflow', titleKey: 'routes.adminWorkflow' },
  { path: '/admin/workflow-forms', name: 'admin-workflow-forms', titleKey: 'routes.adminWorkflowForms' },
  { path: '/admin/order-type', name: 'admin-order-type', titleKey: 'routes.adminOrderType' },
  { path: '/admin/user', name: 'admin-user', titleKey: 'routes.adminUser' },
  { path: '/admin/customer', name: 'admin-customer', titleKey: 'routes.adminCustomer' },
  { path: '/admin/supplier', name: 'admin-supplier', titleKey: 'routes.adminSupplier' },
  { path: '/admin/quotation/item-group', name: 'admin-quotation-item-group', titleKey: 'routes.adminQuotationItemGroup' },
  { path: '/admin/quotation/item', name: 'admin-quotation-item', titleKey: 'routes.adminQuotationItem' },
  { path: '/admin/fcm-console', name: 'admin-fcm-console', titleKey: 'routes.adminFcmConsole' },
  { path: '/settings/system-parameters', name: 'settings-system-parameters', titleKey: 'routes.settingsSystemParameters' },
] as const

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
      path: '/job-order/quotation-list',
      name: 'job-order-quotation-list',
      component: () => import('@/views/QuotationsView.vue'),
      meta: { requiresAuth: true, titleKey: 'routes.jobOrderQuotationList' },
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
    ...legacyLeafRoutes.map((route) => ({
      path: route.path,
      name: route.name,
      component: route.path === '/job-order/order-list'
        ? () => import('@/views/OrderListView.vue')
        : route.path === '/job-order/job-list'
          ? () => import('@/views/JobListView.vue')
        : route.path === '/job-order/job-stats'
          ? () => import('@/views/JobStatsView.vue')
        : route.path === '/job-order/schedule/pending'
          ? () => import('@/views/SchedulePendingView.vue')
        : route.path === '/job-order/schedule/scheduled'
          ? () => import('@/views/ScheduleView.vue')
        : () => import('@/views/LegacyMenuPlaceholderView.vue'),
      meta: { requiresAuth: true, titleKey: route.titleKey },
    })),
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