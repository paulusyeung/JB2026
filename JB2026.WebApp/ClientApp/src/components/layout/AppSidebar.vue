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
    >
      <v-tooltip
        v-for="item in visibleItems"
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
      <MenuItemRenderer :items="visibleLegacyMenuItems" :show-tooltips="showCollapsedTooltips" />
    </v-list>
  </v-navigation-drawer>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSessionStore } from '@/stores/session'
import MenuItemRenderer from './MenuItemRenderer.vue'
import { buildLegacyMenuItems, filterMenuByAccess } from './menuHelper'

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
  { title: t('routes.dashboardOperator'), to: '/dashboard/operator', icon: 'mdi-view-dashboard-outline' },
])

const legacyMenuItems = computed(() => {
  return buildLegacyMenuItems(t, sessionStore.profile?.role)
})

const effectiveRbac = computed(() => sessionStore.rbac)

const visibleItems = computed(() => filterMenuByAccess(items.value, effectiveRbac.value))

const visibleLegacyMenuItems = computed(() =>
  filterMenuByAccess(legacyMenuItems.value, effectiveRbac.value))

function handleDrawerModelUpdate(nextValue: boolean) {
  if (!props.isMobile) {
    return
  }

  emit('update:modelValue', nextValue)
}
</script>