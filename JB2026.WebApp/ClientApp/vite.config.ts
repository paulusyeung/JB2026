import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import vuetify from 'vite-plugin-vuetify'
import { fileURLToPath, URL } from 'node:url'
import { readdirSync, existsSync } from 'node:fs'

// CKEditor 5 ships inline SVG icons as XML strings. Vite's esbuild pre-bundler
// splits the packages across chunks, which breaks IconView's XML parsing
// (runtime "getAttribute of null" errors) and can emit empty modules. Serving
// every @ckeditor package as native ESM avoids the problem. We enumerate all
// installed @ckeditor packages dynamically so the exclusion stays correct
// regardless of which subset is actually imported.
const ckeditorExclusions: string[] = []
const scopeDir = fileURLToPath(new URL('./node_modules/@ckeditor', import.meta.url))
if (existsSync(scopeDir)) {
  for (const pkg of readdirSync(scopeDir)) {
    ckeditorExclusions.push(`@ckeditor/${pkg}`)
  }
}

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    base: '/app/',
    plugins: [
      vue({
        template: {
          compilerOptions: {
            isCustomElement: (tag) => tag === 'web-pivot-table',
          },
        },
      }),
      vuetify({ autoImport: true }),
    ],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
        'webpivottable-dist': fileURLToPath(new URL('./node_modules/webpivottable/dist/wpt.js', import.meta.url)),
      },
    },
    server: {
      host: '0.0.0.0',
      port: 5173,
      allowedHosts: ['jb2026.local'],
      proxy: {
                '/api': {
          target: env.VITE_API_BASE_URL || 'http://localhost:5225',
          changeOrigin: true,
          secure: false,
        },
        '/ui': {
          target: env.VITE_WEBAPP_BASE_URL || 'http://localhost:5113',
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