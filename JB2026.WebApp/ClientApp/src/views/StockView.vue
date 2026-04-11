<template>
  <section class="page-section stock-page" :class="{ 'stock-page--dark': isDark }">
    <v-card rounded="xl" elevation="0" class="panel-card stock-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3 pb-2">
        <div>
          <h3 class="text-h6 mb-1">{{ t('stock.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('stock.subtitle') }}</p>
        </div>
      </v-card-title>

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="keyword"
            density="comfortable"
            :label="t('stock.searchProducts')"
            prepend-inner-icon="mdi-magnify"
            variant="solo-filled"
            hide-details
            clearable
            @keydown.enter="applyLookup"
          />

          <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loading" @click="applyLookup">
            {{ t('stock.search') }}
          </v-btn>

          <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="refreshList">
            {{ t('stock.actions.refresh') }}
          </v-btn>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ errorMessage }}</v-alert>

        <div class="toolbar-bar mb-2">
          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-view-column">
                {{ t('stock.actions.columns') }}
              </v-btn>
            </template>
            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item v-for="column in columnOptions" :key="column.key" @click="toggleColumn(column.key)">
                <template #prepend>
                  <v-checkbox-btn :model-value="visibleColumnKeys.includes(column.key)" />
                </template>
                <v-list-item-title>{{ column.title }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-sort">
                {{ t('stock.actions.sorting') }}
              </v-btn>
            </template>
            <v-card min-width="280" class="pa-3">
              <v-select
                v-model="sortKey"
                :items="sortableColumns"
                item-title="title"
                item-value="key"
                density="compact"
                variant="outlined"
                :label="t('stock.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('stock.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('stock.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
            {{ t('stock.actions.checkbox') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-eye-outline" @click="showUnavailable('stock.actions.views')">
            {{ t('stock.actions.views') }}
          </v-btn>

          <v-divider vertical class="mx-1" />

          <v-btn variant="outlined" size="small" prepend-icon="mdi-paperclip" @click="showUnavailable('stock.actions.attachment')">
            {{ t('stock.actions.attachment') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-file-delimited-outline" :disabled="displayedRows.length === 0" @click="exportToCsv">
            {{ t('stock.actions.export') }}
          </v-btn>

          <v-btn
            variant="outlined"
            size="small"
            color="primary"
            prepend-icon="mdi-file-plus"
            class="toolbar-new-product-btn"
            @click="showUnavailable('stock.actions.newProduct')"
          >
            {{ t('stock.actions.newProduct') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-delete" @click="showUnavailable('stock.actions.delete')">
            {{ t('stock.actions.delete') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-swap-horizontal" @click="showUnavailable('stock.actions.stockInOut')">
            {{ t('stock.actions.stockInOut') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-open-in-new" @click="showUnavailable('stock.actions.popup')">
            {{ t('stock.actions.popup') }}
          </v-btn>

          <span class="text-caption text-medium-emphasis" v-if="checkboxMode">
            {{ t('stock.actions.selected', { count: selectedIds.length }) }}
          </span>
        </div>

        <v-data-table
          :headers="headers"
          :items="displayedRows"
          :loading="loading"
          item-value="productId"
          v-model="selectedIds"
          :show-select="checkboxMode"
          density="compact"
          fixed-header
          height="62vh"
          class="stock-table"
        >
          <template #[`header.attachment`]>
            <v-icon size="14" color="primary">mdi-paperclip</v-icon>
          </template>

          <template #[`item.attachment`]="{ item }">
            <v-icon size="14" :color="item.attachmentCount > 0 ? 'success' : 'error'">
              {{ item.attachmentCount > 0 ? 'mdi-paperclip' : 'mdi-circle-outline' }}
            </v-icon>
          </template>

          <template #[`item.sellingPrice`]="{ item }">
            {{ formatMoney(item.sellingPrice) }}
          </template>

          <template #[`item.cogs`]="{ item }">
            {{ formatMoney(item.cogs) }}
          </template>

          <template #[`item.balance`]="{ item }">
            <v-chip size="small" color="secondary" variant="tonal">{{ formatQty(item.balance) }}</v-chip>
          </template>

          <template #[`item.createdOn`]="{ item }">{{ formatDateCell(item.createdOn) }}</template>
          <template #[`item.modifiedOn`]="{ item }">{{ formatDateCell(item.modifiedOn) }}</template>

        </v-data-table>

        <div class="text-caption text-medium-emphasis mt-2">
          {{ t('stock.rows', { count: displayedRows.length }) }}
        </div>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useTheme } from 'vuetify'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { getStockProducts } from '@/services/stock'
import type { StockProductListItem } from '@/types/api'

type StockDisplayRow = StockProductListItem & {
  ln: number
}

const rows = ref<StockProductListItem[]>([])
const loading = ref(false)
const keyword = ref('')
const errorMessage = ref('')
const checkboxMode = ref(false)
const selectedIds = ref<string[]>([])
const sortDirection = ref<'asc' | 'desc'>('asc')
const sortKey = ref('stockNumber')
const visibleColumnKeys = ref<string[]>([
  'ln',
  'stockNumber',
  'productCode',
  'productName',
  'attachment',
  'sellingPrice',
  'cogs',
  'balance',
  'createdOn',
  'createdBy',
  'modifiedOn',
  'modifiedBy',
])

const { t } = useI18n({ useScope: 'global' })
const { formatCurrency, formatDate: formatDateByLocale, formatNumber } = useLocaleFormatters()
const theme = useTheme()
const isDark = computed(() => theme.global.current.value.dark)

const allHeaders = computed(() => [
  { title: '#', key: 'ln', width: '48px', sortable: false },
  { title: t('stock.headers.stockNumber'), key: 'stockNumber', minWidth: '160px' },
  { title: t('stock.headers.code'), key: 'productCode', minWidth: '140px' },
  { title: t('stock.headers.product'), key: 'productName', minWidth: '260px' },
  { title: t('stock.headers.attachment'), key: 'attachment', width: '72px', sortable: false },
  { title: t('stock.headers.sellingPrice'), key: 'sellingPrice', align: 'end' as const, width: '130px' },
  { title: t('stock.headers.cogs'), key: 'cogs', align: 'end' as const, width: '110px' },
  { title: t('stock.headers.balance'), key: 'balance', align: 'end' as const, width: '100px' },
  { title: t('stock.headers.createdOn'), key: 'createdOn', width: '150px' },
  { title: t('stock.headers.createdBy'), key: 'createdBy', width: '100px' },
  { title: t('stock.headers.modifiedOn'), key: 'modifiedOn', width: '150px' },
  { title: t('stock.headers.modifiedBy'), key: 'modifiedBy', width: '100px' },
])

const headers = computed(() => allHeaders.value.filter((header) => visibleColumnKeys.value.includes(String(header.key))))

const sortableColumns = computed(() =>
  allHeaders.value
    .filter((header) => header.sortable !== false)
    .map((header) => ({ key: String(header.key), title: String(header.title) })),
)

const columnOptions = computed(() => allHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title) })))

const displayedRows = computed<StockDisplayRow[]>(() => {
  const key = sortKey.value as keyof StockProductListItem
  const result = [...rows.value]

  result.sort((lhs, rhs) => {
    const leftValue = lhs[key]
    const rightValue = rhs[key]

    if (leftValue == null && rightValue == null) return 0
    if (leftValue == null) return sortDirection.value === 'asc' ? -1 : 1
    if (rightValue == null) return sortDirection.value === 'asc' ? 1 : -1

    if (typeof leftValue === 'number' && typeof rightValue === 'number') {
      return sortDirection.value === 'asc' ? leftValue - rightValue : rightValue - leftValue
    }

    const left = String(leftValue)
    const right = String(rightValue)
    return sortDirection.value === 'asc' ? left.localeCompare(right) : right.localeCompare(left)
  })

  return result.map((row, index) => ({ ...row, ln: index + 1 }))
})

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    rows.value = await getStockProducts({ keyword: keyword.value.trim(), take: 500 })
  } catch {
    errorMessage.value = t('stock.messages.loadFailed')
  } finally {
    loading.value = false
  }
}

async function applyLookup() {
  await load()
}

async function refreshList() {
  keyword.value = ''
  await load()
}

function toggleColumn(columnKey: string) {
  if (visibleColumnKeys.value.includes(columnKey)) {
    if (visibleColumnKeys.value.length > 1) {
      visibleColumnKeys.value = visibleColumnKeys.value.filter((key) => key !== columnKey)
    }
    return
  }

  visibleColumnKeys.value = [...visibleColumnKeys.value, columnKey]
}

function formatMoney(value: number) {
  return formatCurrency(value)
}

function formatQty(value: number) {
  return formatNumber(value)
}

function formatDateCell(value: string | null | undefined) {
  if (!value) return '-'
  return formatDateByLocale(value)
}

function showUnavailable(actionKey: string) {
  errorMessage.value = t('stock.messages.actionUnavailable', { action: t(actionKey) })
}

function exportToCsv() {
  const exportCols = headers.value.filter((h) => h.key !== 'attachment')
  const headerRow = exportCols.map((h) => `"${String(h.title).replace(/"/g, '""')}"`).join(',')

  const dataRows = displayedRows.value.map((row) =>
    exportCols
      .map((h) => {
        if (h.key === 'ln') return '""'
        const val = row[h.key as keyof StockDisplayRow]
        if (val == null || val === '') return '""'
        if (h.key === 'createdOn' || h.key === 'modifiedOn') return `"${formatDateCell(String(val))}"`
        return `"${String(val).replace(/"/g, '""')}"`
      })
      .join(','),
  )

  const csv = '\uFEFF' + [headerRow, ...dataRows].join('\r\n')
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = `stock-product-${new Date().toISOString().slice(0, 10)}.csv`
  anchor.click()
  URL.revokeObjectURL(url)
}
</script>

<style scoped>
.stock-page {
  min-height: 0;
  --stock-header-bg: rgba(195, 216, 248, 0.92);
  --stock-header-fg: inherit;
}

.stock-page--dark {
  --stock-header-bg: rgba(52, 74, 104, 0.95);
  --stock-header-fg: rgba(239, 246, 255, 0.98);
}

.stock-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
  background: linear-gradient(180deg, rgba(224, 237, 255, 0.92), rgba(241, 247, 255, 0.96));
}

.filter-bar {
  display: grid;
  gap: 12px;
  grid-template-columns: minmax(300px, 1fr) auto auto;
  align-items: center;
  margin-bottom: 16px;
}

.toolbar-bar {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
}

.toolbar-menu-list {
  max-height: 340px;
  overflow: auto;
}

.toolbar-new-product-btn {
  min-width: 156px;
}

.stock-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.stock-table :deep(.v-table__wrapper > table > thead > tr > th),
.stock-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--stock-header-bg) !important;
  color: var(--stock-header-fg) !important;
}

.stock-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.stock-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.stock-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.stock-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

.stock-table :deep(tbody td) {
  font-size: 12px;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}
</style>