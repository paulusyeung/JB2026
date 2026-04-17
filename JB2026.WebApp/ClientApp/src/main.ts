import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { createVuetify } from 'vuetify'
import { i18n } from '@/i18n'
import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'
import './styles/main.scss'

const vuetify = createVuetify({
  theme: {
    defaultTheme: 'light-nature',
    themes: {
      'light-nature': {
        dark: false,
        colors: {
          background: '#f5f4ee',
          surface: '#fffdf8',
          surfaceVariant: '#ece4d5',
          primary: '#9f4f2a',
          secondary: '#284b63',
          accent: '#c9923d',
          success: '#487a52',
          warning: '#c4812f',
          error: '#9c2f2f',
          info: '#406882',
        },
      },
      'light-indigo': {
        dark: false,
        colors: {
          background: '#f8fafc',
          surface: '#ffffff',
          surfaceVariant: '#e2e8f0',
          primary: '#1e40af',
          secondary: '#0ea5e9',
          accent: '#f59e0b',
          success: '#10b981',
          warning: '#f59e0b',
          error: '#ef4444',
          info: '#3b82f6',
        },
      },
      'light-rose': {
        dark: false,
        colors: {
          background: '#fff5f8',
          surface: '#ffffff',
          surfaceVariant: '#ffe4e6',
          primary: '#e11d48',
          secondary: '#fb7185',
          accent: '#c026d3',
          success: '#10b981',
          warning: '#f59e0b',
          error: '#ef4444',
          info: '#3b82f6',
        },
      },
      'light-slate': {
        dark: false,
        colors: {
          background: '#f8fafc',
          surface: '#ffffff',
          surfaceVariant: '#e2e8f0',
          primary: '#475569',
          secondary: '#94a3b8',
          accent: '#0ea5e9',
          success: '#10b981',
          warning: '#f59e0b',
          error: '#ef4444',
          info: '#3b82f6',
        },
      },
      'dark-forest': {
        dark: true,
        colors: {
          background: '#161916',
          surface: '#1e241f',
          surfaceVariant: '#2a322b',
          primary: '#e29a60',
          secondary: '#8cb9d4',
          accent: '#d8ab58',
          success: '#7ec08c',
          warning: '#e0ae53',
          error: '#ef8a8a',
          info: '#7fb2cf',
        },
      },
      'dark-midnight': {
        dark: true,
        colors: {
          background: '#020617',
          surface: '#0f172a',
          surfaceVariant: '#1e293b',
          primary: '#38bdf8',
          secondary: '#7dd3fc',
          accent: '#818cf8',
          success: '#34d399',
          warning: '#fbbf24',
          error: '#fb7185',
          info: '#60a5fa',
        },
      },
      'dark-amethyst': {
        dark: true,
        colors: {
          background: '#1a1024',
          surface: '#261a35',
          surfaceVariant: '#352648',
          primary: '#c084fc',
          secondary: '#a855f7',
          accent: '#f0abfc',
          success: '#4ade80',
          warning: '#fbbf24',
          error: '#f87171',
          info: '#818cf8',
        },
      },
      'dark-obsidian': {
        dark: true,
        colors: {
          background: '#000000',
          surface: '#121212',
          surfaceVariant: '#262626',
          primary: '#fbbf24',
          secondary: '#78350f',
          accent: '#eab308',
          success: '#34d399',
          warning: '#fbbf24',
          error: '#fb7185',
          info: '#60a5fa',
        },
      },
    },
  },
})

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.use(vuetify)
app.use(i18n)
app.mount('#app')