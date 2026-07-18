<template>
  <div class="list-mobile-card-stack">
    <v-card
      v-for="item in items"
      :key="getItemKey(item)"
      rounded="lg"
      elevation="0"
      class="list-mobile-card"
      @click="handleCardClick(item)"
    >
      <div class="list-mobile-card__header">
        <div class="list-mobile-card__header-content">
          <div
            v-for="column in headerColumns"
            :key="column.key"
            :class="['list-mobile-card__field', column.emphasis ? 'list-mobile-card__field--emphasis' : '']"
          >
            <span v-if="column.label" class="list-mobile-card__label text-caption text-medium-emphasis">{{ column.label }}</span>
            <span>{{ formatValue(item, column) }}</span>
          </div>
        </div>

        <div class="d-flex align-center">
          <v-icon
            v-if="syncedKey && asRecord(item)[syncedKey]"
            size="16"
            color="success"
            :title="t('crm.companies.messages.syncedTooltip')"
          >mdi-link-variant</v-icon>
          <v-checkbox-btn
            v-if="checkboxMode"
            :model-value="isSelected(item)"
            density="compact"
            hide-details
            @click.stop="toggleSelection(item)"
          />
        </div>
      </div>

      <div v-if="bodyColumns.length > 0" class="list-mobile-card__body">
        <div v-for="column in bodyColumns" :key="column.key" class="list-mobile-card__row">
          <span class="list-mobile-card__label text-caption text-medium-emphasis">{{ column.label }}</span>
          <span class="text-body-2">{{ formatValue(item, column) }}</span>
        </div>
      </div>

      <div v-if="footerColumns.length > 0 || !!$slots.actions" class="list-mobile-card__footer">
        <div v-if="footerColumns.length > 0" class="list-mobile-card__footer-meta text-caption text-medium-emphasis">
          <span v-for="column in footerColumns" :key="column.key">{{ column.label }}: {{ formatValue(item, column) }}</span>
        </div>

        <slot name="actions" :item="item" />
      </div>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

const { t } = useI18n({ useScope: 'global' })

type ListMobileCardItem = any

export type ListMobileCardColumn<T = ListMobileCardItem> = {
  key: string
  label?: string
  section?: 'header' | 'body' | 'footer'
  emphasis?: boolean
  formatter?: (item: T) => string
}

const props = withDefaults(
  defineProps<{
    items: ListMobileCardItem[]
    columns: ListMobileCardColumn<any>[]
    itemKey?: string
    checkboxMode?: boolean
    selectedIds?: string[]
    onSelect?: (item: any, selected: boolean) => void
    onCardClick?: (item: any) => void
    syncedKey?: string
  }>(),
  {
    itemKey: 'id',
    checkboxMode: false,
    selectedIds: () => [],
    onSelect: undefined,
    onCardClick: undefined,
  },
)

const headerColumns = computed(() => props.columns.filter((column) => (column.section ?? 'body') === 'header'))
const bodyColumns = computed(() => props.columns.filter((column) => (column.section ?? 'body') === 'body'))
const footerColumns = computed(() => props.columns.filter((column) => column.section === 'footer'))

function asRecord(item: ListMobileCardItem): Record<string, unknown> {
  return item as Record<string, unknown>
}

function getItemKey(item: ListMobileCardItem) {
  return String(asRecord(item)[props.itemKey])
}

function formatValue(item: ListMobileCardItem, column: ListMobileCardColumn<any>) {
  if (column.formatter) {
    return column.formatter(item)
  }

  const value = asRecord(item)[column.key]
  if (value === null || value === undefined || value === '') {
    return '-'
  }

  return String(value)
}

function isSelected(item: ListMobileCardItem) {
  return props.selectedIds.includes(String(asRecord(item)[props.itemKey]))
}

function toggleSelection(item: ListMobileCardItem) {
  const selected = !isSelected(item)
  props.onSelect?.(asRecord(item), selected)
}

function handleCardClick(item: ListMobileCardItem) {
  props.onCardClick?.(asRecord(item))
}
</script>

<style scoped>
.list-mobile-card-stack {
  display: grid;
  gap: 12px;
}

.list-mobile-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  padding: 12px;
}

.list-mobile-card__header {
  display: flex;
  justify-content: space-between;
  gap: 8px;
}

.list-mobile-card__header-content {
  display: grid;
  gap: 4px;
}

.list-mobile-card__field {
  display: grid;
  gap: 2px;
}

.list-mobile-card__field--emphasis {
  font-weight: 600;
}

.list-mobile-card__body {
  display: grid;
  gap: 6px;
  margin-top: 10px;
}

.list-mobile-card__row {
  display: flex;
  justify-content: space-between;
  gap: 12px;
}

.list-mobile-card__footer {
  margin-top: 10px;
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: center;
}

.list-mobile-card__footer-meta {
  display: grid;
  gap: 2px;
}

.list-mobile-card__label {
  line-height: 1.1;
}
</style>
