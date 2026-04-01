import type { ComposerTranslation } from 'vue-i18n'

export type MenuItem = {
  title: string
  icon?: string
  to?: string
  children?: MenuItem[]
}

export function buildLegacyMenuItems(t: ComposerTranslation): MenuItem[] {
  return [
    {
      title: t('routes.jobOrder'),
      icon: 'mdi-clipboard-text-outline',
      children: [
        { title: t('routes.jobOrderQuotationList'), to: '/job-order/quotation-list', icon: 'mdi-file-document-multiple-outline' },
        { title: t('routes.jobOrderOrderList'), to: '/job-order/order-list', icon: 'mdi-format-list-bulleted' },
        { title: t('routes.jobOrderJobList'), to: '/job-order/job-list', icon: 'mdi-briefcase-outline' },
        { title: t('routes.jobOrderJobStats'), to: '/job-order/job-stats', icon: 'mdi-chart-line' },
        {
          title: t('routes.jobOrderSchedule'),
          icon: 'mdi-calendar-clock-outline',
          children: [
            { title: t('routes.jobOrderSchedulePending'), to: '/job-order/schedule/pending', icon: 'mdi-timer-sand' },
            { title: t('routes.jobOrderScheduleScheduled'), to: '/job-order/schedule/scheduled', icon: 'mdi-calendar-check-outline' },
            { title: t('routes.jobOrderScheduleCompleted'), to: '/job-order/schedule/completed', icon: 'mdi-check-circle-outline' },
            { title: t('routes.jobOrderSchedulePackingOnAir'), to: '/job-order/schedule/packing', icon: 'mdi-package-variant-closed' },
          ],
        },
        {
          title: t('routes.sml'),
          icon: 'mdi-folder-multiple-outline',
          children: [
            { title: t('routes.smlRtfList'), to: '/job-order/sml/rtf-list', icon: 'mdi-file-document-outline' },
            { title: t('routes.smlInvoiceList'), to: '/job-order/sml/invoice-list', icon: 'mdi-receipt-text-outline' },
            { title: t('routes.smlRtfStats'), to: '/job-order/sml/rtf-stats', icon: 'mdi-chart-bar' },
            { title: t('routes.smlInvoiceStats'), to: '/job-order/sml/invoice-stats', icon: 'mdi-chart-areaspline' },
          ],
        },
        {
          title: t('routes.reports'),
          icon: 'mdi-chart-box-outline',
          children: [
            { title: t('routes.reportsExceptionalReport'), to: '/job-order/reports/exceptional', icon: 'mdi-alert-circle-outline' },
          ],
        },
      ],
    },
    {
      title: t('routes.stock'),
      icon: 'mdi-package-variant-closed',
      children: [
        { title: t('routes.stockProduct'), to: '/stock/product', icon: 'mdi-cube-outline' },
      ],
    },
    {
      title: t('routes.admin'),
      icon: 'mdi-shield-account-outline',
      children: [
        { title: t('routes.adminWorkflow'), to: '/admin/workflow', icon: 'mdi-source-branch' },
        { title: t('routes.adminWorkflowForms'), to: '/admin/workflow-forms', icon: 'mdi-file-tree-outline' },
        { title: t('routes.adminOrderType'), to: '/admin/order-type', icon: 'mdi-shape-outline' },
        { title: t('routes.adminUser'), to: '/admin/user', icon: 'mdi-account-outline' },
        { title: t('routes.adminCustomer'), to: '/admin/customer', icon: 'mdi-account-group-outline' },
        { title: t('routes.adminSupplier'), to: '/admin/supplier', icon: 'mdi-truck-outline' },
        {
          title: t('routes.quotations'),
          icon: 'mdi-file-document-multiple-outline',
          children: [
            { title: t('routes.adminQuotationItemGroup'), to: '/admin/quotation/item-group', icon: 'mdi-shape-plus-outline' },
            { title: t('routes.adminQuotationItem'), to: '/admin/quotation/item', icon: 'mdi-tag-outline' },
          ],
        },
        { title: t('routes.adminFcmConsole'), to: '/admin/fcm-console', icon: 'mdi-bell-badge-outline' },
      ],
    },
    {
      title: t('routes.settings'),
      icon: 'mdi-cog-outline',
      children: [
        { title: t('routes.settingsSystemParameters'), to: '/settings/system-parameters', icon: 'mdi-tune-vertical-variant' },
      ],
    },
  ]
}

export function hasChildren(item: MenuItem): boolean {
  return Array.isArray(item.children) && item.children.length > 0
}
