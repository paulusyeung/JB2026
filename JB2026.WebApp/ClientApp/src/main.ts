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
    defaultTheme: 'jb2026Light',
    themes: {
      jb2026Light: {
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
      jb2026Dark: {
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
    },
  },
})

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.use(vuetify)
app.use(i18n)
app.mount('#app')