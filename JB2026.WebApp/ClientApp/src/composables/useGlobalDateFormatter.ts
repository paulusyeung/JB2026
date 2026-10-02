// src/composables/useGlobalDateFormatter.ts
import { computed } from 'vue'
import { useDateFormatStore } from '@/stores/dateFormat'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { DATE_FORMATS, type DateFormatType, formatDate as baseFormatDate, formatDateWithSeconds as baseFormatDateWithSeconds } from '@/utils/dateFormatter'

export function useGlobalDateFormatter() {
  const dateFormatStore = useDateFormatStore()
  const { activeLocale } = useLocaleFormatters()
  
  const currentFormat = computed({
    get: () => dateFormatStore.currentFormat,
    set: (val) => dateFormatStore.setCurrentFormat(val)
  })
  
  const format = (
    value: string | Date | null | undefined, 
    formatType?: DateFormatType, 
    locale?: string
  ) => {
    const targetLocale = locale || activeLocale.value
    
    if (formatType) {
      // Use specific format with current or provided locale
      return baseFormatDate(value, formatType, targetLocale)
    } else {
      // Use global format from store with current or provided locale
      return dateFormatStore.format(value, targetLocale)
    }
  }
  
  // Follows the global date format but always adds hours, minutes and seconds.
  const formatWithSeconds = (
    value: string | Date | null | undefined,
    formatType?: DateFormatType,
    locale?: string
  ) => {
    const targetFormat = formatType || dateFormatStore.currentFormat
    const targetLocale = locale || activeLocale.value

    return baseFormatDateWithSeconds(value, targetFormat, targetLocale)
  }

  const setFormat = (format: DateFormatType) => {
    dateFormatStore.setCurrentFormat(format)
  }
  
  return {
    currentFormat,
    format,
    formatWithSeconds,
    setFormat,
    DATE_FORMATS // Re-export for convenience in templates
  }
}
