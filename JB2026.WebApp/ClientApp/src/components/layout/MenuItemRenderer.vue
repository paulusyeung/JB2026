<template>
  <template
    v-for="item in items"
    :key="item.to ?? item.title"
  >
    <v-list-group
      v-if="hasChildren(item)"
      :aria-label="item.title"
      :class="{ 'menu-group--collapsed': props.showTooltips }"
    >
      <template #activator="{ props: activatorProps }">
        <v-tooltip :disabled="!props.showTooltips" location="right">
          <template #activator="{ props: tooltipProps }">
            <v-list-item
              v-bind="mergeActivatorProps(activatorProps, tooltipProps)"
              :class="{ 'menu-item--collapsed': props.showTooltips }"
              :prepend-icon="item.icon"
              :title="item.title"
              :aria-label="item.title"
              rounded="xl"
            />
          </template>

          <span>{{ item.title }}</span>
        </v-tooltip>
      </template>

      <div
        class="menu-children"
        :class="{ 'menu-children--collapsed': props.showTooltips }"
        :style="{ marginInlineStart: `${props.depth === 0 ? 0 : 0}px` }"
      >
        <MenuItemRenderer
          :items="item.children ?? []"
          :depth="props.depth + 1"
          :show-tooltips="props.showTooltips"
        />
      </div>
    </v-list-group>

    <v-tooltip v-else :disabled="!props.showTooltips" location="right">
      <template #activator="{ props: tooltipProps }">
        <v-list-item
          v-bind="tooltipProps"
          :class="{ 'menu-item--collapsed': props.showTooltips }"
          :prepend-icon="item.icon"
          :title="item.title"
          :to="item.to"
          :aria-label="item.title"
          rounded="xl"
        />
      </template>

      <span>{{ item.title }}</span>
    </v-tooltip>
  </template>
</template>

<script setup lang="ts">
import { hasChildren, type MenuItem } from './menuHelper'

defineOptions({
  name: 'MenuItemRenderer',
})

const props = withDefaults(defineProps<{
  items: MenuItem[]
  depth?: number
  showTooltips?: boolean
}>(), {
  depth: 0,
  showTooltips: false,
})

function mergeActivatorProps(
  activatorProps: Record<string, unknown>,
  tooltipProps: Record<string, unknown>,
) {
  return {
    ...activatorProps,
    ...tooltipProps,
  }
}
</script>

<style scoped>
.menu-children {
  border-inline-start: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  padding-inline-start: 0;
}

.menu-children--collapsed {
  border-inline-start: 0;
}

.menu-group--collapsed :deep(.v-list-group__items .v-list-item),
.menu-item--collapsed {
  padding-inline-start: 12px !important;
  padding-inline-end: 12px !important;
}

.menu-group--collapsed :deep(.v-list-group__items .v-list-item__prepend),
.menu-item--collapsed :deep(.v-list-item__prepend) {
  width: 24px;
}
</style>
