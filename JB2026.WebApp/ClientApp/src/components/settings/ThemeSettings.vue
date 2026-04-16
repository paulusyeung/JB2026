<template>
  <div class="theme-settings">
    <div class="d-flex align-center justify-space-between mb-4">
      <div class="text-subtitle-1 font-weight-bold">{{ t('theme.colorScheme') }}</div>
      <v-btn-toggle
        :model-value="themeStore.mode"
        mandatory
        divided
        rounded="lg"
        color="primary"
        variant="outlined"
        density="compact"
        @update:model-value="themeStore.setMode"
      >
        <v-btn value="light" size="small">
          <v-icon start icon="mdi-white-balance-sunny" />
          {{ t('topbar.lightTheme') }}
        </v-btn>
        <v-btn value="dark" size="small">
          <v-icon start icon="mdi-weather-night" />
          {{ t('topbar.darkTheme') }}
        </v-btn>
      </v-btn-toggle>
    </div>

    <v-item-group :model-value="themeStore.scheme" mandatory @update:model-value="themeStore.setScheme">
      <v-row dense>
        <v-col v-for="scheme in currentSchemes" :key="scheme.id" cols="4">
          <v-item v-slot="{ isSelected, toggle }" :value="scheme.id">
            <v-card
              :class="['scheme-card', { 'active-scheme': isSelected }]"
              flat
              Border
              v-ripple
              @click="toggle"
            >
              <div class="scheme-preview" :style="{ background: scheme.previewColor }">
                <v-icon v-if="isSelected" color="white" icon="mdi-check-circle" size="small" class="check-icon" />
              </div>
              <v-card-text class="pa-2 text-center text-caption">
                {{ t(`theme.schemes.${scheme.id}`) }}
              </v-card-text>
            </v-card>
          </v-item>
        </v-col>
      </v-row>
    </v-item-group>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useThemeStore, appSchemes } from '@/stores/theme'

const { t } = useI18n()
const themeStore = useThemeStore()

const currentSchemes = computed(() => {
  const mode = themeStore.mode
  const ids = appSchemes[mode]
  
  const palettePreviews: Record<string, string> = {
    nature: '#9f4f2a',
    indigo: '#1e40af',
    rose: '#e11d48',
    forest: '#e29a60',
    midnight: '#38bdf8',
    amethyst: '#c084fc'
  }

  return ids.map(id => ({
    id,
    previewColor: palettePreviews[id] || '#ccc'
  }))
})
</script>

<style scoped lang="scss">
.scheme-card {
  cursor: pointer;
  transition: all 0.2s ease-in-out;
  border: 2px solid transparent !important;
  
  &.active-scheme {
    border-color: rgb(var(--v-theme-primary)) !important;
    background-color: rgb(var(--v-theme-primary), 0.05);
  }
}

.scheme-preview {
  height: 48px;
  width: 100%;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
}

.check-icon {
  position: absolute;
  top: 4px;
  right: 4px;
  filter: drop-shadow(0 1px 2px rgba(0,0,0,0.5));
}
</style>
