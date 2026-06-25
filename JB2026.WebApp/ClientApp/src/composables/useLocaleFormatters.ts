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

function parseISODateString(value: string): Date {
  const match = value.match(/^(\d{4})-(\d{2})-(\d{2})/)
  if (match) {
    const year = Number.parseInt(match[1], 10)
    const month = Number.parseInt(match[2], 10) - 1
    const day = Number.parseInt(match[3], 10)
    return new Date(year, month, day)
  }
  return new Date(value)
}

export function useLocaleFormatters() {
  const { locale } = useI18n({ useScope: 'global' })

  const activeLocale = computed(() => toBcp47Locale(locale.value))

  function formatDate(value: string | Date): string {
    const date = typeof value === 'string' ? parseISODateString(value) : value
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
