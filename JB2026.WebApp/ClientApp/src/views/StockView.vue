<template>
  <section class="page-section stock-page">
    <v-card rounded="xl" elevation="0" class="panel-card stock-card">


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
        <v-alert v-if="successMessage" type="success" variant="tonal" class="mt-3 mb-2">{{ successMessage }}</v-alert>

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

          <template v-if="!isPhoneLayout">
            <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
              {{ t('stock.actions.checkbox') }}
            </v-btn>

            <v-menu location="bottom">
              <template #activator="{ props }">
                <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                  {{ t('stock.actions.views') }}
                </v-btn>
              </template>
              <v-list density="compact" class="toolbar-menu-list">
                <v-list-item
                  prepend-icon="mdi-table"
                  :active="viewMode === 'detail'"
                  @click="setViewMode('detail')"
                >
                  <v-list-item-title>{{ detailViewLabel }}</v-list-item-title>
                </v-list-item>
                <v-list-item
                  prepend-icon="mdi-view-grid-outline"
                  :active="viewMode === 'card'"
                  @click="setViewMode('card')"
                >
                  <v-list-item-title>{{ cardViewLabel }}</v-list-item-title>
                </v-list-item>
              </v-list>
            </v-menu>

            <v-divider vertical class="mx-1" />

            <v-btn variant="outlined" size="small" prepend-icon="mdi-paperclip" @click="openStockAttachmentDialog">
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
              @click="openCreateDialog"
            >
              {{ t('stock.actions.newProduct') }}
            </v-btn>

            <v-btn
              variant="outlined"
              size="small"
              prepend-icon="mdi-delete"
              :disabled="selectedIds.length === 0 || deleting"
              :loading="deleting"
              @click="startDelete"
            >
              {{ t('stock.actions.delete') }}
            </v-btn>

            <v-btn variant="outlined" size="small" prepend-icon="mdi-swap-horizontal" :disabled="selectedIds.length !== 1" @click="openStockInOutDialog">
              {{ t('stock.actions.stockInOut') }}
            </v-btn>
          </template>

          <v-menu v-else location="bottom end">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                {{ t('stock.actions.more') }}
              </v-btn>
            </template>

            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
                <v-list-item-title>{{ t('stock.actions.checkbox') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>{{ detailViewLabel }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>{{ cardViewLabel }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-paperclip" @click="openStockAttachmentDialog">
                <v-list-item-title>{{ t('stock.actions.attachment') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-file-delimited-outline" :disabled="displayedRows.length === 0" @click="exportToCsv">
                <v-list-item-title>{{ t('stock.actions.export') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-file-plus" @click="openCreateDialog">
                <v-list-item-title>{{ t('stock.actions.newProduct') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-delete" :disabled="selectedIds.length === 0" @click="startDelete">
                <v-list-item-title>{{ t('stock.actions.delete') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-swap-horizontal" :disabled="selectedIds.length !== 1" @click="openStockInOutDialog">
                <v-list-item-title>{{ t('stock.actions.stockInOut') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <span class="text-caption text-medium-emphasis" v-if="checkboxMode">
            {{ t('stock.actions.selected', { count: selectedIds.length }) }}
          </span>
        </div>

        <div v-if="isCardView" class="stock-mobile-list">
          <v-card
            v-for="row in displayedRows"
            :key="row.productId"
            rounded="lg"
            elevation="0"
            class="stock-mobile-card"
            role="button"
            tabindex="0"
            @click="openEditDialog(row.productId)"
            @keyup.enter="openEditDialog(row.productId)"
          >
            <div class="stock-mobile-card__header">
              <div>
                <div class="text-subtitle-2 font-weight-bold">{{ row.productName }}</div>
                <div class="text-caption text-medium-emphasis">
                  {{ formatStockNumber(row.stockNumber) }} · {{ row.productCode }}
                </div>
              </div>

              <v-checkbox-btn
                v-if="checkboxMode"
                :model-value="selectedIds.includes(row.productId)"
                density="compact"
                hide-details
                @click.stop="toggleSelected(row.productId)"
              />
            </div>

            <div class="stock-mobile-card__metrics">
              <v-chip size="small" color="secondary" variant="tonal">
                {{ t('stock.headers.balance') }}: {{ formatQty(row.balance) }}
              </v-chip>
              <span class="text-caption">{{ t('stock.headers.sellingPrice') }}: {{ formatMoney(row.sellingPrice) }}</span>
              <span class="text-caption">{{ t('stock.headers.cogs') }}: {{ formatMoney(row.cogs) }}</span>
            </div>

            <div class="stock-mobile-card__meta text-caption text-medium-emphasis">
              <span>{{ t('stock.headers.createdOn') }}: {{ format(row.createdOn) }}</span>
              <span>{{ t('stock.headers.modifiedOn') }}: {{ format(row.modifiedOn) }}</span>
            </div>

            <div class="stock-mobile-card__footer">
              <v-chip size="x-small" :color="row.attachmentCount > 0 ? 'success' : 'error'" variant="tonal">
                {{ t('stock.headers.attachment') }}: {{ row.attachmentCount }}
              </v-chip>
              <span class="text-caption text-medium-emphasis">{{ row.createdBy || '-' }} / {{ row.modifiedBy || '-' }}</span>
            </div>
          </v-card>
        </div>

        <div v-else class="stock-table-shell">
          <v-data-table
            :headers="headers"
            :items="displayedRows"
            :loading="loading"
            item-value="productId"
            v-model="selectedIds"
            :show-select="checkboxMode"
            density="compact"
            fixed-header
            height="100%"
            class="stock-table"
            @click:row="onRowClick"
          >
            <template #[`header.attachment`]>
              <v-icon size="14" color="primary">mdi-paperclip</v-icon>
            </template>

            <template #[`item.stockNumber`]="{ item }">
              <a
                href="javascript:void(0)"
                class="stock-number-link"
                @click.stop="openEditDialog(item.productId)"
              >
                {{ formatStockNumber(item.stockNumber) }}
              </a>
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

            <template #[`item.createdOn`]="{ item }">{{ format(item.createdOn) }}</template>
            <template #[`item.modifiedOn`]="{ item }">{{ format(item.modifiedOn) }}</template>

          </v-data-table>
        </div>

        </v-card-text>
            </v-card>

            <product-record-dialog
      v-model="dialogOpen"
      :mode="dialogMode"
      :product-id="activeProductId"
      :customer-code-options="customerCodeOptions"
      :category-code-options="categoryCodeOptions"
      @saved="onDialogSaved"
      @deleted="onDialogDeleted"
    />

    <stock-in-out-dialog
      v-model="stockInOutDialogOpen"
      :product-id="stockInOutProductId"
      :stock-number="stockInOutStockNumber"
      @saved="onStockInOutSaved"
    />

    <stock-attachment-dialog
      v-model="stockAttachmentDialogOpen"
      :product-id="stockAttachmentProductId"
      :stock-number="stockAttachmentStockNumber"
      :can-delete="canDeleteAttachments"
      @changed="onAttachmentChanged"
    />
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useDisplay } from 'vuetify'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { useViewSettings } from '@/composables/useColumnPersistence'
import { getStockProducts, parseStockNumber, deleteProductRecord } from '@/services/stock'
import { useSessionStore } from '@/stores/session'
import ProductRecordDialog from '@/components/stock/ProductRecordDialog.vue'
import StockInOutDialog from '@/components/stock/StockInOutDialog.vue'
import StockAttachmentDialog from '@/components/stock/StockAttachmentDialog.vue'
import type { StockInOutTransactionResult, StockProductListItem } from '@/types/api'

type StockDisplayRow = StockProductListItem & {
  ln: number
}

type StockViewMode = 'detail' | 'card'
type ProductDialogMode = 'create' | 'edit'

const rows = ref<StockProductListItem[]>([])
const loading = ref(false)
const keyword = ref('')
const errorMessage = ref('')
const successMessage = ref('')
const deleting = ref(false)
const selectedIds = ref<string[]>([])
const { t } = useI18n({ useScope: 'global' })
const { format, DATE_FORMATS } = useGlobalDateFormatter()
const { formatCurrency, formatNumber } = useLocaleFormatters()
const sessionStore = useSessionStore()
const display = useDisplay()
const isPhoneLayout = computed(() => display.smAndDown.value)
const canDeleteAttachments = computed(() => {
  const rawRole = sessionStore.profile?.role
  const normalizedRole = String(rawRole ?? '').toLowerCase().trim()
  return normalizedRole === 'admin' || normalizedRole === '4'
})

const defaultColumnKeys = [
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
]

const viewSettings = useViewSettings('stock', {
  visibleColumns: defaultColumnKeys,
  sortKey: 'stockNumber',
  sortDirection: 'asc',
  checkboxMode: false,
  viewMode: 'detail',
})

const visibleColumnKeys = viewSettings.visibleColumns
const sortKey = viewSettings.sortKey
const sortDirection = viewSettings.sortDirection
const checkboxMode = viewSettings.checkboxMode
const viewMode = viewSettings.viewMode
const dialogOpen = ref(false)
const dialogMode = ref<ProductDialogMode>('create')
const activeProductId = ref<string | null>(null)
const stockInOutDialogOpen = ref(false)
const stockInOutProductId = ref<string | null>(null)
const stockInOutStockNumber = ref('')
const stockAttachmentDialogOpen = ref(false)
const stockAttachmentProductId = ref<string | null>(null)
const stockAttachmentStockNumber = ref('')
const detailViewLabel = computed(() => t('stock.actions.detailView'))
const cardViewLabel = computed(() => t('stock.actions.cardView'))
const isCardView = computed(() => viewMode.value === 'card')

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

function formatStockNumber(stockNumber: string): string {
  const normalized = String(stockNumber ?? '').trim()
  if (!normalized) {
    return ''
  }

  // Keep compatibility with both dashed and legacy compact formats from DB.
  const compact = normalized.replace(/[^a-zA-Z0-9]/g, '')
  if (compact.length >= 7) {
    const customerCode = compact.slice(0, 3)
    const categoryCode = compact.slice(3, 6)
    const sequenceNumber = compact.slice(6)
    return [customerCode, categoryCode, sequenceNumber].join('-')
  }

  const parsed = parseStockNumber(normalized)
  return [parsed.customerCode, parsed.categoryCode, parsed.sequenceNumber].filter((part) => part).join('-') || normalized
}

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

const customerCodeOptions = computed(() => {
  const codes = new Set(rows.value.map((r) => parseStockNumber(r.stockNumber).customerCode).filter(Boolean))
  return [...codes].sort()
})

const categoryCodeOptions = computed(() => {
  const codes = new Set(rows.value.map((r) => parseStockNumber(r.stockNumber).categoryCode).filter(Boolean))
  return [...codes].sort()
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

function toggleSelected(productId: string) {
  if (selectedIds.value.includes(productId)) {
    selectedIds.value = selectedIds.value.filter((id) => id !== productId)
    return
  }

  selectedIds.value = [...selectedIds.value, productId]
}

function setViewMode(mode: StockViewMode) {
  viewMode.value = mode
}

function openCreateDialog() {
  dialogMode.value = 'create'
  activeProductId.value = null
  dialogOpen.value = true
}

function openEditDialog(productId: string) {
  dialogMode.value = 'edit'
  activeProductId.value = productId
  dialogOpen.value = true
}

function onRowClick(_event: Event, payload: unknown) {
  const row = payload as { item?: { productId?: string; raw?: { productId?: string } } }
  const productId = row?.item?.productId ?? row?.item?.raw?.productId
  if (!productId) {
    return
  }

  toggleSelected(productId)
}

async function onDialogSaved() {
  await load()
}

async function onDialogDeleted(_productId: string, outcome: string) {
  await load()
  successMessage.value =
    outcome === 'hardDeleted'
      ? t('stock.messages.deleteHardDeletedSuccess')
      : t('stock.messages.deleteRetiredSuccess')
}

function openStockInOutDialog() {
  if (selectedIds.value.length !== 1) {
    errorMessage.value = t('stock.messages.stockInOutSelectOne')
    return
  }

  const productId = selectedIds.value[0]
  const row = rows.value.find((r) => r.productId === productId)
  if (!row) {
    return
  }

  stockInOutProductId.value = productId ?? null
  stockInOutStockNumber.value = row.stockNumber
  stockInOutDialogOpen.value = true
}

function openStockAttachmentDialog() {
  if (selectedIds.value.length !== 1) {
    errorMessage.value = t('stock.attachments.errors.selectSingleProduct')
    return
  }

  const productId = selectedIds.value[0]
  if (!productId) {
    return
  }

  const row = rows.value.find((item) => item.productId === productId)
  if (!row) {
    return
  }

  stockAttachmentProductId.value = productId
  stockAttachmentStockNumber.value = formatStockNumber(row.stockNumber)
  stockAttachmentDialogOpen.value = true
}

async function onAttachmentChanged() {
  await load()
}

async function onStockInOutSaved(_result: StockInOutTransactionResult) {
  await load()
}

async function startDelete() {
  if (selectedIds.value.length === 0) {
    errorMessage.value = t('stock.messages.deleteSelectFirst')
    return
  }

  const count = selectedIds.value.length
  const message =
    count === 1
      ? t('stock.messages.confirmDeleteSingle')
      : t('stock.messages.confirmDeleteBatch', { count })

  if (!window.confirm(message)) {
    return
  }

  const idsToDelete = [...selectedIds.value]
  let successCount = 0
  let failedCount = 0
  let lastOutcome = ''

  deleting.value = true
  errorMessage.value = ''
  successMessage.value = ''
  try {
    for (const productId of idsToDelete) {
      try {
        const result = await deleteProductRecord(productId)
        successCount++
        lastOutcome = result.outcome
      } catch {
        failedCount++
      }
    }
  } finally {
    deleting.value = false
  }

  selectedIds.value = []
  await load()

  if (count === 1 && successCount === 1) {
    successMessage.value =
      lastOutcome === 'hardDeleted'
        ? t('stock.messages.deleteHardDeletedSuccess')
        : t('stock.messages.deleteRetiredSuccess')
  } else if (count > 1) {
    if (failedCount > 0) {
      errorMessage.value = t('stock.messages.deleteBatchResult', { success: successCount, failed: failedCount })
    } else {
      successMessage.value = t('stock.messages.deleteBatchResult', { success: successCount, failed: 0 })
    }
  }
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
        if (h.key === 'createdOn' || h.key === 'modifiedOn') return `"${format(String(val), DATE_FORMATS.ISO_DATE)}"`
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
  --stock-header-bg: rgb(var(--v-theme-surface-variant));
  --stock-header-fg: rgb(var(--v-theme-on-surface-variant));
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

.stock-table-shell {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 260px);
  min-height: 400px;
  overflow-x: auto;
}

.stock-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.stock-mobile-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  align-items: start;
}

.stock-mobile-card {
  display: grid;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgb(var(--v-theme-surface));
}

.stock-mobile-card__header,
.stock-mobile-card__footer {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
}

.stock-mobile-card__metrics,
.stock-mobile-card__meta {
  display: grid;
  gap: 0.45rem;
}

/* Snake-like stagger so cards don't feel like a rigid vertical stack. */
.stock-mobile-list .stock-mobile-card:nth-child(2n) {
  transform: translateY(14px);
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

.stock-number-link {
  color: rgb(var(--v-theme-primary));
  text-decoration: none;
  cursor: pointer;
  font-weight: 600;
}

.stock-number-link:hover {
  text-decoration: underline;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }

  .toolbar-bar {
    align-items: stretch;
  }

  .stock-mobile-list {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .stock-mobile-list .stock-mobile-card:nth-child(2n) {
    transform: translateY(10px);
  }
}

@media (max-width: 600px) {
  .toolbar-bar {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .stock-mobile-card__header,
  .stock-mobile-card__footer {
    flex-direction: column;
  }

  .stock-mobile-list {
    grid-template-columns: 1fr;
  }

  .stock-mobile-list .stock-mobile-card:nth-child(2n) {
    transform: none;
  }
}
</style>