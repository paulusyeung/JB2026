<script setup lang="ts">
import { computed, ref } from 'vue'

const SELECT_COLUMN_KEY = 'data-table-select'

export interface ReorderableHeaderColumn {
  key: string | null
  title?: string
  align?: 'start' | 'center' | 'end'
  sortable?: boolean
  width?: string | number
  minWidth?: string | number
  maxWidth?: string | number
}

export interface ReorderableHeaderExtra {
  icon: string
  size?: number | string
  color?: string
  title?: string
  align?: 'start' | 'center' | 'end'
}

const props = withDefaults(
  defineProps<{
    /**
     * The `#headers` slot props of the parent `v-data-table`, passed through as-is: Vuetify
     * types these callbacks against its own internal header objects, so they are narrowed
     * once, in `headerRow` and the helpers below, rather than declared here.
     */
    table: Record<string, unknown>
    /** Icon header cells, keyed by column key. Rendered as an icon plus an `sr-only` title. */
    headerExtras?: Record<string, ReorderableHeaderExtra>
    /** Allow-list of draggable column keys. Defaults to every column except the selection column. */
    reorderableKeys?: string[]
    /** Set to false to render the row without drag affordances. */
    reorderable?: boolean
  }>(),
  {
    headerExtras: () => ({}),
    reorderable: true,
  },
)

const emit = defineEmits<{
  move: [sourceKey: string, targetKey: string]
}>()

type TableCallbacks = {
  toggleSort?: (column: ReorderableHeaderColumn, event: Event) => unknown
  isSorted?: (column: ReorderableHeaderColumn) => unknown
  getSortIcon?: (column: ReorderableHeaderColumn) => unknown
  allSelected?: boolean
  someSelected?: boolean
  selectAll?: (value: boolean) => unknown
}

const callbacks = computed(() => props.table as TableCallbacks)

const headerRow = computed<ReorderableHeaderColumn[]>(() => {
  const rows = props.table.headers
  return (Array.isArray(rows) ? (rows[0] as ReorderableHeaderColumn[] | undefined) : undefined) ?? []
})

const draggingColumnKey = ref<string | null>(null)
const dropTargetColumnKey = ref<string | null>(null)

function extraFor(columnKey: string | null) {
  return props.headerExtras[String(columnKey)]
}

function sortIconFor(column: ReorderableHeaderColumn) {
  return callbacks.value.getSortIcon?.(column) as string | undefined
}

function onHeaderActivate(column: ReorderableHeaderColumn, event: Event) {
  if (!column.sortable) return
  callbacks.value.toggleSort?.(column, event)
}

function isSortedFor(column: ReorderableHeaderColumn) {
  return Boolean(callbacks.value.isSorted?.(column))
}

function isReorderableColumn(columnKey: string | null) {
  if (!props.reorderable) return false

  const key = String(columnKey)
  if (props.reorderableKeys) return props.reorderableKeys.includes(key)
  return key !== SELECT_COLUMN_KEY
}

function toCssUnit(value: unknown) {
  if (typeof value === 'number') return `${value}px`
  return typeof value === 'string' && value ? value : undefined
}

function onColumnDragStart(event: DragEvent, columnKey: string) {
  draggingColumnKey.value = columnKey
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'move'
    event.dataTransfer.setData('text/plain', columnKey)
  }
}

function onColumnDragOver(event: DragEvent, columnKey: string) {
  if (!draggingColumnKey.value || draggingColumnKey.value === columnKey) return
  event.preventDefault()
  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = 'move'
  }
  dropTargetColumnKey.value = columnKey
}

function onColumnDrop(event: DragEvent, columnKey: string) {
  event.preventDefault()
  const sourceKey = draggingColumnKey.value || event.dataTransfer?.getData('text/plain') || ''
  onColumnDragEnd()
  if (sourceKey && sourceKey !== columnKey) {
    emit('move', sourceKey, columnKey)
  }
}

function onColumnDragEnd() {
  draggingColumnKey.value = null
  dropTargetColumnKey.value = null
}
</script>

<template>
  <tr>
    <th
      v-for="column in headerRow"
      :key="String(column.key)"
      :data-column-key="String(column.key)"
      class="v-data-table__td v-data-table__th v-data-table__th--sticky reorderable-th"
      :class="[
        `v-data-table-column--align-${column.align ?? 'start'}`,
        {
          'v-data-table-column--no-padding': column.key === SELECT_COLUMN_KEY,
          'v-data-table__th--sortable': column.sortable,
          'v-data-table__th--sorted': isSortedFor(column),
          'reorderable-th--dragging': draggingColumnKey === String(column.key),
          'reorderable-th--drop-target': dropTargetColumnKey === String(column.key),
        },
      ]"
      :style="{
        position: 'sticky',
        top: 0,
        width: toCssUnit(column.width),
        minWidth: toCssUnit(column.minWidth),
        maxWidth: toCssUnit(column.maxWidth),
      }"
      :draggable="isReorderableColumn(column.key)"
      :tabindex="column.sortable ? 0 : undefined"
      @click="onHeaderActivate(column, $event)"
      @keydown.enter="onHeaderActivate(column, $event)"
      @dragstart="onColumnDragStart($event, String(column.key))"
      @dragover="onColumnDragOver($event, String(column.key))"
      @drop="onColumnDrop($event, String(column.key))"
      @dragend="onColumnDragEnd"
    >
      <v-checkbox-btn
        v-if="column.key === SELECT_COLUMN_KEY"
        color="primary"
        :model-value="callbacks.allSelected"
        :indeterminate="Boolean(callbacks.someSelected && !callbacks.allSelected)"
        density="compact"
        @update:model-value="callbacks.selectAll?.(Boolean($event))"
      />
      <div
        v-else
        class="v-data-table-header__content"
        :class="extraFor(column.key)?.align === 'center' ? 'd-flex justify-center' : undefined"
      >
        <span v-if="extraFor(column.key)" class="sr-only">{{ column.title }}</span>
        <v-icon
          v-if="extraFor(column.key)"
          :size="extraFor(column.key)?.size ?? 14"
          :color="extraFor(column.key)?.color"
          :title="extraFor(column.key)?.title"
        >{{ extraFor(column.key)?.icon }}</v-icon>
        <span v-else>{{ column.title }}</span>
        <v-icon v-if="column.sortable" class="v-data-table-header__sort-icon" :icon="sortIconFor(column)" />
      </div>
    </th>
  </tr>
</template>

<style scoped>
.reorderable-th {
  cursor: grab;
}

.reorderable-th--dragging {
  opacity: 0.4;
}

.reorderable-th--drop-target {
  box-shadow: inset 3px 0 0 0 rgb(var(--v-theme-primary));
}
</style>
