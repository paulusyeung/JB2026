import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import vuetify from 'vite-plugin-vuetify'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    base: '/app/',
    plugins: [vue(), vuetify({ autoImport: true })],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    server: {
      host: '127.0.0.1',
      port: 5173,
      proxy: {
        '/api': {
          target: env.VITE_API_BASE_URL ?? 'https://localhost:7165',
          changeOrigin: true,
          secure: false,
        },
        '/ui': {
          target: env.VITE_WEBAPP_BASE_URL ?? 'https://localhost:7163',
          changeOrigin: true,
          secure: false,
        },
      },
    },
    build: {
      outDir: '../wwwroot/app',
      emptyOutDir: true,
    },
  }
})