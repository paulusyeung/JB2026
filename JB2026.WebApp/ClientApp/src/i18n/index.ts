import { createI18n } from 'vue-i18n'
import { localeStorageKey, messages, supportedLocales, type AppLocale } from './messages'

function normalizeLocale(input: string | null | undefined): AppLocale {
  if (!input) {
    return 'en'
  }

  const lowered = input.trim().toLowerCase()
  if (lowered.startsWith('zh-hans') || lowered.startsWith('zh-cn') || lowered.startsWith('zh-sg') || lowered.startsWith('zh-chs')) {
    return 'zh-Hans'
  }

  if (lowered.startsWith('zh-hant') || lowered.startsWith('zh-tw') || lowered.startsWith('zh-hk') || lowered.startsWith('zh-mo') || lowered.startsWith('zh-cht')) {
    return 'zh-Hant'
  }

  if (lowered.startsWith('zh')) {
    return 'zh-Hans'
  }

  return 'en'
}

export function getInitialLocale(): AppLocale {
  const stored = localStorage.getItem(localeStorageKey)
  const normalized = normalizeLocale(stored ?? navigator.language)
  return supportedLocales.includes(normalized) ? normalized : 'en'
}

function toHtmlLangTag(locale: AppLocale): string {
  switch (locale) {
    case 'zh-Hans':
      return 'zh-CN'
    case 'zh-Hant':
      return 'zh-TW'
    default:
      return 'en'
  }
}

export function setLocale(locale: AppLocale) {
  i18n.global.locale.value = locale
  localStorage.setItem(localeStorageKey, locale)
  document.documentElement.lang = toHtmlLangTag(locale)
}

const initialLocale = getInitialLocale()

export const i18n = createI18n({
  legacy: false,
  locale: initialLocale,
  fallbackLocale: 'en',
  messages,
})

document.documentElement.lang = toHtmlLangTag(initialLocale)
