import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { vDraggableDialog } from '@/directives/draggableDialog'
import App from './App.vue'
import router from './router'
import { createVuetify } from 'vuetify'
import { createVueI18nAdapter } from 'vuetify/locale/adapters/vue-i18n'
import { useI18n } from 'vue-i18n'
import { i18n } from '@/i18n'
import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'
import './styles/main.scss'
import { themeRegistry } from '@/themes/registry'

const vuetifyThemes: any = {}

themeRegistry.forEach(pair => {
  // Register light version
  vuetifyThemes[`light-${pair.id}`] = pair.light
  // Register dark version
  vuetifyThemes[`dark-${pair.id}`] = pair.dark
})

const vuetify = createVuetify({
  theme: {
    defaultTheme: 'light-indigo',
    themes: vuetifyThemes,
  },
  locale: {
    adapter: createVueI18nAdapter({ i18n: i18n as never, useI18n }),
  },
})
const app = createApp(App)
app.use(createPinia())
app.use(router)
app.use(vuetify)
app.use(i18n)
app.directive('draggable-dialog', vDraggableDialog)
app.mount('#app')