import { useI18n } from 'vue-i18n'

export function useJobStatus() {
  const { t } = useI18n({ useScope: 'global' })

  function statusIcon(status: number): string {
    if (status >= 2) return 'mdi-flag-checkered'
    if (status === 1) return 'mdi-flag-variant-outline'
    return 'mdi-flag-minus-outline'
  }

  function statusColor(status: number): string {
    if (status >= 2) return 'success'
    if (status === 1) return 'info'
    return 'secondary'
  }

  function statusLabel(status: number): string {
    if (status >= 2) return t('jobOrder.status.completed')
    if (status === 1) return t('jobOrder.status.inProgress')
    return t('jobOrder.status.notStarted')
  }

  return { statusIcon, statusColor, statusLabel }
}
