// src/utils/dateFormatter.ts

// Configuration for date formats
export const DATE_FORMATS = {
  // Short formats
  SHORT_DATE: 'shortDate',
  SHORT_DATETIME: 'shortDateTime',
  SHORT_TIME: 'shortTime',
  
  // Long formats
  LONG_DATE: 'longDate',
  LONG_DATETIME: 'longDateTime',
  
  // Custom formats
  CUSTOM: 'custom',
  
  // ISO formats
  ISO_DATE: 'isoDate',
  ISO_DATETIME: 'isoDateTime'
} as const

export type DateFormatType = typeof DATE_FORMATS[keyof typeof DATE_FORMATS]

// Configuration object for different date formats
export const DATE_FORMAT_CONFIG: Record<DateFormatType, Intl.DateTimeFormatOptions> = {
  [DATE_FORMATS.SHORT_DATE]: {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  },
  [DATE_FORMATS.SHORT_DATETIME]: {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  },
  [DATE_FORMATS.SHORT_TIME]: {
    hour: '2-digit',
    minute: '2-digit'
  },
  [DATE_FORMATS.LONG_DATE]: {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  },
  [DATE_FORMATS.LONG_DATETIME]: {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  },
  [DATE_FORMATS.CUSTOM]: {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  },
  [DATE_FORMATS.ISO_DATE]: {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  },
  [DATE_FORMATS.ISO_DATETIME]: {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    timeZone: 'UTC'
  }
}

// Parse an ISO date string to a Date object in local time, avoiding UTC interpretation
function parseISODateString(value: string): Date {
  // Match ISO format like "2026-07-09" or "2026-07-09T00:00:00"
  const match = value.match(/^(\d{4})-(\d{2})-(\d{2})/)
  if (match) {
    const year = Number.parseInt(match[1], 10)
    const month = Number.parseInt(match[2], 10) - 1
    const day = Number.parseInt(match[3], 10)
    return new Date(year, month, day)
  }
  return new Date(value)
}

// Main date formatting function
export function formatDate(
  value: string | Date | null | undefined,
  format: DateFormatType = DATE_FORMATS.SHORT_DATE,
  locale: string = 'en-US'
): string {
  // Handle null/undefined values
  if (!value) return '-'
  
  // Special case for '1900-01-01' which seems to be used as a "null" value in some parts of the app
  if (typeof value === 'string' && value.startsWith('1900-01-01')) return ''

  // Convert to Date object if it's a string, avoiding UTC timezone interpretation
  const date = typeof value === 'string' ? parseISODateString(value) : value
  
  // Handle invalid dates
  if (isNaN(date.getTime())) return '-'
  
  // Special handling for ISO dates
  if (format === DATE_FORMATS.ISO_DATE) {
    const y = date.getFullYear()
    const m = String(date.getMonth() + 1).padStart(2, '0')
    const d = String(date.getDate()).padStart(2, '0')
    return `${y}-${m}-${d}`
  }
  if (format === DATE_FORMATS.ISO_DATETIME) {
    return date.toISOString()
  }
  
  // Use Intl.DateTimeFormat with the configured format
  try {
    const formatter = new Intl.DateTimeFormat(locale, DATE_FORMAT_CONFIG[format])
    return formatter.format(date)
  } catch (e) {
    console.error(`Error formatting date: ${value} with format ${format} and locale ${locale}`, e)
    return '-'
  }
}
