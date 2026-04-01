<template>
  <template
    v-for="item in items"
    :key="item.to ?? item.title"
  >
    <v-list-group
      v-if="hasChildren(item)"
      :aria-label="item.title"
    >
      <template #activator="{ props: activatorProps }">
        <v-list-item
          v-bind="activatorProps"
          :prepend-icon="item.icon"
          :title="item.title"
          :aria-label="item.title"
          rounded="xl"
        />
      </template>

      <div
        class="menu-children"
        :style="{ marginInlineStart: `${props.depth === 0 ? 0 : 0}px` }"
      >
        <MenuItemRenderer
          :items="item.children ?? []"
          :depth="props.depth + 1"
        />
      </div>
    </v-list-group>

    <v-list-item
      v-else
      :prepend-icon="item.icon"
      :title="item.title"
      :to="item.to"
      :aria-label="item.title"
      rounded="xl"
    />
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
}>(), {
  depth: 0,
})
</script>

<style scoped>
.menu-children {
  border-inline-start: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  padding-inline-start: 0;
}
</style>
