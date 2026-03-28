import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { createVuetify } from 'vuetify'
import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'
import './styles/main.scss'

const vuetify = createVuetify({
  theme: {
    defaultTheme: 'jb2026',
    themes: {
      jb2026: {
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
    },
  },
})

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.use(vuetify)
app.mount('#app')