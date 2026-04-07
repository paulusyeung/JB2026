/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string
  readonly VITE_WEBAPP_BASE_URL?: string
  readonly VITE_DEV_USERNAME?: string
  readonly VITE_DEV_PASSWORD?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

declare module 'vuetify/styles'
declare module 'webpivottable-wpt'
declare module 'webpivottable'
declare module 'webpivottable/dist/wpt.js'
declare module 'webpivottable-dist'