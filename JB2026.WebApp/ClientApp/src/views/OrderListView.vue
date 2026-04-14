<template>
  <section class="page-section order-list-page" :class="{ 'order-list-page--dark': isDark }">
    <v-card rounded="xl" elevation="0" class="panel-card order-list-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('jobOrder.orderList.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('jobOrder.orderList.subtitle') }}</p>
        </div>
      </v-card-title>

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('jobOrder.orderList.lookup')"
            prepend-inner-icon="mdi-magnify"
            variant="solo-filled"
            hide-details
            clearable
            @keydown.enter="applyLookup"
          />

          <v-select
            v-model="commonQuery"
            :items="commonQueryItems"
            item-title="label"
            item-value="value"
            :label="t('jobOrder.orderList.commonQuery')"
            variant="solo-filled"
            density="comfortable"
            hide-details
          />

          <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loading" @click="applyLookup">
            {{ t('jobOrder.orderList.search') }}
          </v-btn>

          <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="refreshList">
            {{ t('common.refresh') }}
          </v-btn>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ errorMessage }}</v-alert>

        <div class="toolbar-bar mb-2">
          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-view-column">
                {{ t('jobOrder.orderList.actions.columns') }}
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
                {{ t('jobOrder.orderList.actions.sorting') }}
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
                :label="t('jobOrder.orderList.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('jobOrder.orderList.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('jobOrder.orderList.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
            {{ t('jobOrder.orderList.actions.checkbox') }}
          </v-btn>

          <v-divider vertical class="mx-1" />

          <v-btn variant="outlined" size="small" prepend-icon="mdi-printer" @click="printList">
            {{ t('jobOrder.orderList.actions.print') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-file-delimited-outline" :disabled="rows.length === 0" @click="exportToCsv">
            {{ t('jobOrder.orderList.actions.export') }}
          </v-btn>

          <v-btn
            variant="outlined"
            color="primary"
            size="small"
            prepend-icon="mdi-file-plus"
            class="toolbar-new-order-btn"
            @click="openCreate"
          >
            {{ t('jobOrder.orderList.actions.newOrder') }}
          </v-btn>

          <v-btn
            v-if="checkboxMode && selectedOrderIds.length > 0"
            variant="tonal"
            color="error"
            size="small"
            prepend-icon="mdi-delete"
            :loading="deleting"
            @click="confirmBatchDelete"
          >
            {{ t('jobOrder.orderList.actions.deleteSelected') }}
          </v-btn>

          <span class="text-caption text-medium-emphasis" v-if="checkboxMode">
            {{ t('jobOrder.orderList.actions.selected', { count: selectedOrderIds.length }) }}
          </span>
        </div>

        <v-data-table
          :headers="masterHeaders"
          :items="masterRows"
          :loading="loading"
          v-model:expanded="expandedMasterIds"
          v-model="selectedOrderIds"
          :show-select="checkboxMode"
          item-value="orderId"
          density="compact"
          fixed-header
          height="62vh"
          class="order-list-table"
          @click:row="onRowClick"
        >
          <template #[`item.ln`]="{ index }">{{ index + 1 }}</template>

          <template #[`item.expander`]="{ item }">
            <v-btn
              v-if="hasDetailRows(item)"
              variant="text"
              density="comfortable"
              size="x-small"
              icon
              @click.stop="toggleExpandRow(item)"
            >
              <v-icon size="16">{{ isRowExpanded(item) ? 'mdi-minus-box-outline' : 'mdi-plus-box-outline' }}</v-icon>
            </v-btn>
          </template>

          <template #[`item.orderNumber`]="{ item }">
            <v-btn variant="text" color="primary" density="comfortable" class="px-0 text-none" @click.stop="openEdit(item)">
              {{ item.orderNumber }}
            </v-btn>
          </template>

          <template #[`item.status`]="{ item }">
            <div class="d-flex justify-center">
              <v-icon size="16" :color="statusColor(item.status)">mdi-flag</v-icon>
            </div>
          </template>

          <template #[`item.attachProduct`]="{ item }">
            <div class="d-flex justify-center">
              <v-icon size="14" :color="item.attachmentProductCount > 0 ? 'success' : 'error'">
                {{ item.attachmentProductCount > 0 ? 'mdi-paperclip' : 'mdi-circle-outline' }}
              </v-icon>
            </div>
          </template>

          <template #[`item.attachCustomer`]="{ item }">
            <div class="d-flex justify-center">
              <v-icon size="14" :color="item.attachmentCustomerCount > 0 ? 'success' : 'error'">
                {{ item.attachmentCustomerCount > 0 ? 'mdi-paperclip' : 'mdi-circle-outline' }}
              </v-icon>
            </div>
          </template>

          <template #[`item.orderedOn`]="{ item }">{{ formatDateYMD(item.orderedOn) }}</template>
          <template #[`item.requiredOn`]="{ item }">{{ formatDateYMD(item.requiredOn) }}</template>
          <template #[`item.completedOn`]="{ item }">{{ formatDate(item.completedOn) }}</template>
          <template #[`item.modifiedOn`]="{ item }">{{ formatDate(item.modifiedOn) }}</template>
          <template #[`item.modifiedBy`]="{ item }">{{ item.modifiedBy || '-' }}</template>
          <template #[`item.invoiceAmount`]="{ item }">{{ item.invoiceAmount === 0 ? '' : formatQty(item.invoiceAmount) }}</template>

          <template #expanded-row="{ item }">
            <tr>
              <td :colspan="masterHeaders.length + (checkboxMode ? 1 : 0)" class="pa-0">
                <v-data-table
                  :headers="detailHeaders"
                  :items="detailRowsFor(item)"
                  density="compact"
                  hide-default-footer
                  class="detail-grid"
                  @click:row="onDetailRowClick"
                >
                  <template #[`header.status`]>
                    <v-icon size="14" color="primary">mdi-flag</v-icon>
                  </template>

                  <template #[`header.attachProduct`]>
                    <v-icon size="16" color="grey darken-3">mdi-paperclip</v-icon>
                  </template>

                  <template #[`header.attachCustomer`]>
                    <v-icon size="16" color="grey darken-3">mdi-paperclip</v-icon>
                  </template>

                  <template #[`item.orderNumber`]="{ item: detail }">
                    <v-btn variant="text" color="primary" density="comfortable" class="px-0 text-none" @click.stop="openEdit(detail)">
                      {{ detail.orderNumber }}-{{ detail.jobNumber }}
                    </v-btn>
                  </template>

                  <template #[`item.status`]="{ item: detail }">
                    <div class="d-flex justify-center">
                      <v-icon size="16" :color="statusColor(detail.status)">mdi-flag</v-icon>
                    </div>
                  </template>

                  <template #[`item.attachProduct`]="{ item: detail }">
                    <div class="d-flex justify-center">
                      <v-icon size="14" :color="detail.attachmentProductCount > 0 ? 'success' : 'error'">
                        {{ detail.attachmentProductCount > 0 ? 'mdi-paperclip' : 'mdi-circle-outline' }}
                      </v-icon>
                    </div>
                  </template>

                  <template #[`item.attachCustomer`]="{ item: detail }">
                    <div class="d-flex justify-center">
                      <v-icon size="14" :color="detail.attachmentCustomerCount > 0 ? 'success' : 'error'">
                        {{ detail.attachmentCustomerCount > 0 ? 'mdi-paperclip' : 'mdi-circle-outline' }}
                      </v-icon>
                    </div>
                  </template>

                  <template #[`item.orderedOn`]="{ item: detail }">{{ formatDateYMD(detail.orderedOn) }}</template>
                  <template #[`item.requiredOn`]="{ item: detail }">{{ formatDateYMD(detail.requiredOn) }}</template>
                  <template #[`item.completedOn`]="{ item: detail }">{{ formatDateYMD1900(detail.completedOn) }}</template>
                  <template #[`item.modifiedOn`]="{ item: detail }">{{ formatDateYMD(detail.modifiedOn) }}</template>
                  <template #[`item.modifiedBy`]="{ item: detail }">{{ detail.modifiedBy || '-' }}</template>
                  <template #[`item.invoiceAmount`]="{ item: detail }">{{ detail.invoiceAmount === 0 ? '' : formatQty(detail.invoiceAmount) }}</template>
                </v-data-table>
              </td>
            </tr>
          </template>
        </v-data-table>

        <div class="text-caption text-medium-emphasis mt-2">
          {{ t('jobOrder.orderList.rows', { count: formatNumber(masterRows.length) }) }}
        </div>
      </v-card-text>
    </v-card>

    <v-dialog v-model="formOpen" max-width="1080" scrollable>
      <OrderRecordDialog
        v-if="formOpen"
        :order="formJob ?? undefined"
        :all-orders="rows"
        @saved="handleSaved"
        @deleted="handleDeleted"
        @open-order="handleOpenOrder"
        @cancel="formOpen = false"
      />
    </v-dialog>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useTheme } from 'vuetify'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { deleteJobOrder, getJobOrder, getOrderList } from '@/services/jobOrders'
import OrderRecordDialog from '@/components/forms/OrderRecordDialog.vue'
import type { JobOrderRecord } from '@/types/api'

const rows = ref<JobOrderRecord[]>([])
const loading = ref(false)
const errorMessage = ref('')
const lookup = ref('')
const commonQuery = ref(0)
const checkboxMode = ref(false)
const selectedOrderIds = ref<string[]>([])
const expandedMasterIds = ref<string[]>([])
const sortDirection = ref<'asc' | 'desc'>('desc')
const sortKey = ref('orderNumber')
const visibleColumnKeys = ref<string[]>([
  'expander',
  'orderNumber',
  'status',
  'orderedOn',
  'customerName',
  'orderTitle',
  'attachProduct',
  'customerRef',
  'attachCustomer',
  'orderedBy',
  'invoiceAmount',
  'invoiceRef',
  'modifiedBy',
  'modifiedOn',
  'requiredOn',
  'completedOn',
])
const formOpen = ref(false)
const formJob = ref<JobOrderRecord | null>(null)
const deleting = ref(false)

const { t } = useI18n({ useScope: 'global' })
const { formatDate: formatDateByLocale, formatNumber } = useLocaleFormatters()
const theme = useTheme()
const isDark = computed(() => theme.global.current.value.dark)

const commonQueryItems = computed(() => [
  { value: 0, label: t('jobOrder.orderList.commonQueryItems.none') },
  { value: 1, label: t('jobOrder.orderList.commonQueryItems.ordered7') },
  { value: 2, label: t('jobOrder.orderList.commonQueryItems.ordered30') },
  { value: 3, label: t('jobOrder.orderList.commonQueryItems.required7') },
  { value: 4, label: t('jobOrder.orderList.commonQueryItems.required30') },
])

const masterHeaders = computed(() => [
  { title: '', key: 'expander', width: '42px', sortable: false },
  { title: '#', key: 'ln', width: '48px', sortable: false },
  { title: t('jobOrder.record.fields.orderNumber'), key: 'orderNumber', width: '130px' },
  { title: t('jobOrder.record.fields.customerName'), key: 'customerName', minWidth: '240px' },
  { title: t('jobOrder.record.fields.brand'), key: 'orderTitle', minWidth: '280px' },
  { title: t('jobOrder.record.fields.requiredOn'), key: 'requiredOn', width: '120px' },
  { title: t('jobOrder.record.fields.invoiceAmount'), key: 'invoiceAmount', align: 'end' as const, width: '120px' },
  { title: t('jobOrder.orderList.headers.salesRep'), key: 'orderedBy', width: '100px' },
  { title: t('jobOrder.record.fields.orderedOn'), key: 'orderedOn', width: '120px' },
])

const allHeaders = computed(() => [
  { title: '', key: 'expander', width: '42px', sortable: false },
  { title: t('jobOrder.record.fields.jobNumber'), key: 'orderNumber', width: '130px' },
  { title: t('jobOrder.orderList.headers.status'), key: 'status', width: '70px' },
  { title: t('jobOrder.orderList.headers.orderedOn'), key: 'orderedOn', width: '120px' },
  { title: t('jobOrder.orderList.headers.customer'), key: 'customerName', minWidth: '240px' },
  { title: t('jobOrder.orderList.headers.orderTitle'), key: 'orderTitle', minWidth: '280px' },
  { title: '', key: 'attachProduct', width: '72px', sortable: false, icon: 'mdi-paperclip' },
  { title: t('jobOrder.orderList.headers.customerRef'), key: 'customerRef', width: '160px' },
  { title: '', key: 'attachCustomer', width: '72px', sortable: false, icon: 'mdi-paperclip' },
  { title: t('jobOrder.orderList.headers.orderedBy'), key: 'orderedBy', width: '100px' },
  { title: t('jobOrder.orderList.headers.invoiceAmount'), key: 'invoiceAmount', align: 'end' as const, width: '120px' },
  { title: t('jobOrder.orderList.headers.invoiceRef'), key: 'invoiceRef', width: '120px' },
  { title: t('jobOrder.orderList.headers.modifiedBy'), key: 'modifiedBy', width: '100px' },
  { title: t('jobOrder.orderList.headers.modifiedOn'), key: 'modifiedOn', width: '120px' },
  { title: t('jobOrder.orderList.headers.requiredOn'), key: 'requiredOn', width: '120px' },
  { title: t('jobOrder.orderList.headers.completedOn'), key: 'completedOn', width: '120px' },
])

const headers = computed(() => allHeaders.value.filter((header) => visibleColumnKeys.value.includes(String(header.key))))

const sortableColumns = computed(() =>
  allHeaders.value
    .filter((header) => header.sortable !== false && header.key !== 'status' && header.key !== 'attachProduct' && header.key !== 'attachCustomer')
    .map((header) => ({ key: String(header.key), title: String(header.title) })),
)

const columnOptions = computed(() => allHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title) })))
// Patch: Show clip icon for attachProduct/attachCustomer headers in detail table
const detailHeaders = computed(() => {
  return headers.value
    .filter((h) => h.key !== 'expander')
    .map((h) => {
      if (h.key === 'attachProduct' || h.key === 'attachCustomer') {
        return { ...h, title: '', icon: 'mdi-paperclip' }
      }
      return h
    })
})

const displayedRows = computed(() => {
  const result = [...rows.value]
  const key = sortKey.value as keyof JobOrderRecord

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

  return result
})

const masterGroups = computed(() => {
  const groups = new Map<string, JobOrderRecord[]>()

  for (const row of displayedRows.value) {
    const key = getMasterKey(row)
    if (!groups.has(key)) {
      groups.set(key, [])
    }
    groups.get(key)!.push(row)
  }

  const normalized = new Map<string, { master: JobOrderRecord, details: JobOrderRecord[] }>()
  for (const [key, group] of groups.entries()) {
    const sortedGroup = [...group].sort((lhs, rhs) => {
      const leftJob = Number.parseInt(lhs.jobNumber, 10)
      const rightJob = Number.parseInt(rhs.jobNumber, 10)
      return (Number.isFinite(leftJob) ? leftJob : 0) - (Number.isFinite(rightJob) ? rightJob : 0)
    })
    const first = sortedGroup[0]
    if (!first) {
      continue
    }
    const master = sortedGroup.find((row) => row.jobNumber === '1') ?? first
    const details = sortedGroup
    normalized.set(key, { master, details })
  }

  return normalized
})

const masterRows = computed(() => [...masterGroups.value.values()].map((entry) => entry.master))

watch([commonQuery], async () => {
  await load()
})

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''
  selectedOrderIds.value = []
  expandedMasterIds.value = []
  try {
    rows.value = await getOrderList({
      lookup: lookup.value.trim() || undefined,
      commonQuery: commonQuery.value,
      take: 500,
    })
  } catch {
    errorMessage.value = t('jobOrder.orderList.loadFailed')
  } finally {
    loading.value = false
  }
}

async function applyLookup() {
  await load()
}

async function refreshList() {
  lookup.value = ''
  commonQuery.value = 0
  await load()
}

async function onRowClick(_event: Event, payload: { item: JobOrderRecord }) {
  if (checkboxMode.value) {
    return
  }

  await openEdit(payload.item)
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

function getMasterKey(row: JobOrderRecord) {
  return row.orderNumber
}

function detailRowsFor(row: JobOrderRecord) {
  const details = masterGroups.value.get(getMasterKey(row))?.details
  if (details && details.length > 0) {
    return details
  }
  return [row]
}

function hasDetailRows(row: JobOrderRecord) {
  return detailRowsFor(row).length >= 1
}

function isRowExpanded(row: JobOrderRecord) {
  return expandedMasterIds.value.includes(row.orderId)
}

function toggleExpandRow(row: JobOrderRecord) {
  if (isRowExpanded(row)) {
    expandedMasterIds.value = expandedMasterIds.value.filter((id) => id !== row.orderId)
    return
  }
  expandedMasterIds.value = [...expandedMasterIds.value, row.orderId]
}

async function onDetailRowClick(_event: Event, payload: { item: JobOrderRecord }) {
  await openEdit(payload.item)
}

async function openEdit(record: JobOrderRecord) {
  try {
    const latest = await getJobOrder(record.orderId)
    formJob.value = latest
    formOpen.value = true
  } catch {
    errorMessage.value = t('jobOrder.openEditFailed')
  }
}

function openCreate() {
  formJob.value = null
  formOpen.value = true
}

async function handleSaved(orderId: string) {
  await load()
  await handleOpenOrder(orderId)
}

async function handleDeleted() {
  formOpen.value = false
  formJob.value = null
  await load()
}

async function handleOpenOrder(orderId: string) {
  const latest = await getJobOrder(orderId)
  formJob.value = latest
}

function printList() {
  window.print()
}

function exportToCsv() {
  const exportCols = headers.value.filter((h) => h.key !== 'status' && h.key !== 'attachProduct' && h.key !== 'attachCustomer')
  const headerRow = exportCols.map((h) => `"${String(h.title).replace(/"/g, '""')}"`).join(',')
  const dateKeys = new Set(['orderedOn', 'requiredOn', 'completedOn', 'modifiedOn'])

  const dataRows = displayedRows.value.map((row) =>
    exportCols
      .map((h) => {
        const key = h.key as keyof JobOrderRecord
        const val = row[key]
        if (val == null) return '""'
        if (dateKeys.has(String(key))) return `"${formatDate(val as string)}"`
        return `"${String(val).replace(/"/g, '""')}"`
      })
      .join(','),
  )

  const csv = '\uFEFF' + [headerRow, ...dataRows].join('\r\n')
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = `order-list-${new Date().toISOString().slice(0, 10)}.csv`
  anchor.click()
  URL.revokeObjectURL(url)
}

async function confirmBatchDelete() {
  const message = t('jobOrder.orderList.batchDeleteConfirm', { count: selectedOrderIds.value.length })
  if (!window.confirm(message)) return

  deleting.value = true
  let failed = 0
  for (const id of selectedOrderIds.value) {
    try {
      await deleteJobOrder(id)
    } catch {
      failed++
    }
  }
  deleting.value = false
  selectedOrderIds.value = []
  await load()
  if (failed > 0) {
    errorMessage.value = t('jobOrder.orderList.batchDeleteFailed')
  }
}

function formatDate(value: string | null | undefined) {
  if (!value) return '-'
  return formatDateByLocale(value)
}


function formatDateYMD(value: string | null | undefined) {
  if (!value) return '-'
  const date = new Date(value)
  if (isNaN(date.getTime())) return '-'
  return date.toISOString().slice(0, 10)
}

// Format date as yyyy-MM-dd, show empty if value is '1900-01-01'
function formatDateYMD1900(value: string | null | undefined) {
  if (!value) return '-'
  // Compare raw string to avoid timezone issues
  if (typeof value === 'string' && value.slice(0, 10) === '1900-01-01') return ''
  const date = new Date(value)
  if (isNaN(date.getTime())) return '-'
  return date.toISOString().slice(0, 10)
}

function formatQty(value: number) {
  if (value === 0) return ''
  return '$' + value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function statusColor(status: number) {
  if (status <= 0) return 'grey'
  if (status === 1) return 'amber'
  if (status === 2) return 'success'
  return 'error'
}
</script>

<style scoped>
.order-list-page {
  min-height: 0;
  --order-list-header-bg: rgba(195, 216, 248, 0.92);
  --order-list-header-fg: inherit;
}

.order-list-page--dark {
  --order-list-header-bg: rgba(52, 74, 104, 0.95);
  --order-list-header-fg: rgba(239, 246, 255, 0.98);
}

.toolbar-new-order-btn {
  min-width: 168px;
}

.order-list-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
  background: linear-gradient(180deg, rgba(224, 237, 255, 0.9), rgba(240, 247, 255, 0.95));
}

.filter-bar {
  display: grid;
  gap: 12px;
  grid-template-columns: minmax(240px, 1fr) minmax(180px, 260px) auto auto;
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

.order-list-table :deep(.v-table__wrapper > table > thead > tr > th),
.order-list-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--order-list-header-bg) !important;
  color: var(--order-list-header-fg) !important;
}

.order-list-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.order-list-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.order-list-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.order-list-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.order-list-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

.order-list-table :deep(tbody td) {
  font-size: 12px;
}

.detail-grid {
  border-top: 1px solid rgba(var(--v-theme-primary), 0.2);
  background: rgba(220, 232, 247, 0.55);
}

.detail-grid :deep(tbody tr:nth-child(odd)) {
  background: rgba(227, 236, 248, 0.7);
}

.detail-grid :deep(tbody td) {
  font-size: 12px;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}
</style>
