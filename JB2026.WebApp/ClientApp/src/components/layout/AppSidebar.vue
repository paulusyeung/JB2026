<template>
  <v-navigation-drawer
    :model-value="drawerModel"
    :permanent="!isMobile"
    :temporary="isMobile"
    :scrim="isMobile"
    :rail="!isMobile && isCollapsed"
    rail-width="82"
    width="240"
    class="app-sidebar"
    @update:model-value="handleDrawerModelUpdate"
  >
    <div class="brand-lockup">
      <div class="brand-mark">JB</div>
      <div>
        <p class="eyebrow mb-1">{{ t('sidebar.eyebrow') }}</p>
        <h1 class="brand-name">JB2026</h1>
      </div>
    </div>

    <v-list
      nav
      density="comfortable"
      prepend-gap="8"
      v-model:opened="openedGroups"
    >
      <v-tooltip
        v-for="item in items"
        :key="item.to"
        :disabled="!showCollapsedTooltips"
        location="right"
      >
        <template #activator="{ props: tooltipProps }">
          <v-list-item
            v-bind="tooltipProps"
            :prepend-icon="item.icon"
            :title="item.title"
            :to="item.to"
            rounded="xl"
          />
        </template>

        <span>{{ item.title }}</span>
      </v-tooltip>

      <v-list-subheader class="mt-2">{{ t('sidebar.legacyCoreModules') }}</v-list-subheader>
      <MenuItemRenderer :items="legacyMenuItems" :show-tooltips="showCollapsedTooltips" />
    </v-list>
  </v-navigation-drawer>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSessionStore } from '@/stores/session'
import MenuItemRenderer from './MenuItemRenderer.vue'
import { buildLegacyMenuItems } from './menuHelper'

const props = defineProps<{
  modelValue: boolean
  isMobile: boolean
  isCollapsed: boolean
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
}>()

const { t } = useI18n({ useScope: 'global' })
const sessionStore = useSessionStore()

const drawerModel = computed(() => (props.isMobile ? props.modelValue : true))
const showCollapsedTooltips = computed(() => !props.isMobile && props.isCollapsed)

const items = computed(() => [
  { title: t('routes.dashboard'), to: '/dashboard', icon: 'mdi-view-dashboard-outline' },
])

const legacyMenuItems = computed(() => {
  return buildLegacyMenuItems(t, sessionStore.profile?.role)
})

// Track which menu groups are opened - default to first group expanded
const openedGroups = ref<string[]>(['group-0'])

function handleDrawerModelUpdate(nextValue: boolean) {
  if (!props.isMobile) {
    return
  }

  emit('update:modelValue', nextValue)
}
</script>