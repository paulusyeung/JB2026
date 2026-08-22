import { enMessages } from './locales/en'
import { zhHansMessages } from './locales/zhHans'
import { zhHantMessages } from './locales/zhHant'
import { en as enVuetify, zhHans as zhHansVuetify, zhHant as zhHantVuetify } from 'vuetify/locale'

export const localeStorageKey = 'jb2026.locale'

export const supportedLocales = ['en', 'zh-Hans', 'zh-Hant'] as const

export type AppLocale = (typeof supportedLocales)[number]

export type LocaleOption = {
  value: AppLocale
  label: string
}

export const localeOptions: LocaleOption[] = [
  { value: 'en', label: 'English' },
  { value: 'zh-Hans', label: '简体中文' },
  { value: 'zh-Hant', label: '繁體中文' },
]

export const messages = {
  en: { ...enMessages, $vuetify: enVuetify },
  'zh-Hans': { ...zhHansMessages, $vuetify: zhHansVuetify },
  'zh-Hant': { ...zhHantMessages, $vuetify: zhHantVuetify },
}
