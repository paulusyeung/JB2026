import pluginVue from 'eslint-plugin-vue'
import vueTsConfig from '@vue/eslint-config-typescript'

export default [
  {
    ignores: ['dist/**', 'node_modules/**', 'playwright-report/**', 'test-results/**'],
  },
  ...pluginVue.configs['flat/recommended'],
  ...vueTsConfig(),
  {
    files: ['src/**/*.vue'],
    rules: {
      'vue/component-api-style': ['error', ['script-setup']],
    },
  },
]