import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

function toBcp47Locale(locale: string): string {
  switch (locale) {
    case 'zh-Hans':
      return 'zh-CN'
    case 'zh-Hant':
      return 'zh-TW'
    default:
      return 'en'
  }
}

export function useLocaleFormatters() {
  const { locale } = useI18n({ useScope: 'global' })

  const activeLocale = computed(() => toBcp47Locale(locale.value))

  function formatDate(value: string | Date): string {
    const date = typeof value === 'string' ? new Date(value) : value
    if (Number.isNaN(date.getTime())) {
      return ''
    }

    return new Intl.DateTimeFormat(activeLocale.value).format(date)
  }

  function formatNumber(value: number, options: Intl.NumberFormatOptions = {}): string {
    return new Intl.NumberFormat(activeLocale.value, options).format(value)
  }

  function formatCurrency(value: number, currency = 'USD'): string {
    return formatNumber(value, { style: 'currency', currency })
  }

  return {
    activeLocale,
    formatDate,
    formatNumber,
    formatCurrency,
  }
}
