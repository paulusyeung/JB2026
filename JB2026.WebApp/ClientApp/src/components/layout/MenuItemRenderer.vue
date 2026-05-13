<template>
  <v-list
    v-if="props.depth === 0"
    nav
    v-model:opened="localOpenedGroups"
  >
    <template
      v-for="(item, index) in items"
      :key="item.to ?? item.title"
    >
      <v-list-group
        v-if="hasChildren(item)"
        :value="groupValue(index)"
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
        >
          <MenuItemRenderer
            :items="item.children ?? []"
            :depth="props.depth + 1"
            :show-tooltips="props.showTooltips"
            :group-path="groupValue(index)"
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
  </v-list>

  <template
    v-else
    v-for="(item, index) in items"
    :key="item.to ?? item.title"
  >
    <v-list-group
      v-if="hasChildren(item)"
      :value="groupValue(index)"
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
      >
        <MenuItemRenderer
          :items="item.children ?? []"
          :depth="props.depth + 1"
          :show-tooltips="props.showTooltips"
          :group-path="groupValue(index)"
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
import { computed, getCurrentInstance, ref } from 'vue'
import { hasChildren, type MenuItem } from './menuHelper'

defineOptions({
  name: 'MenuItemRenderer',
})

const props = withDefaults(defineProps<{
  items: MenuItem[]
  depth?: number
  showTooltips?: boolean
  groupPath?: string
}>(), {
  depth: 0,
  showTooltips: false,
  groupPath: undefined,
})

const instanceUid = getCurrentInstance()?.uid ?? 0
const resolvedGroupPath = computed(() => props.groupPath ?? `menu-${instanceUid}`)

function groupValue(index: number): string {
  return `${resolvedGroupPath.value}-${index}`
}

// Expand the first top-level menu group by default
const localOpenedGroups = ref<string[]>([groupValue(0)])

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

