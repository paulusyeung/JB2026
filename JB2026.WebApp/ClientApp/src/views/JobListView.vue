<template>
  <section class="page-section job-list-page">
    <v-card rounded="xl" elevation="0" class="panel-card job-list-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('jobOrder.jobList.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('jobOrder.jobList.subtitle') }}</p>
        </div>
      </v-card-title>

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('jobOrder.jobList.lookup')"
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
            :label="t('jobOrder.jobList.commonQuery')"
            variant="solo-filled"
            density="comfortable"
            hide-details
          />

          <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loading" @click="applyLookup">
            {{ t('jobOrder.jobList.search') }}
          </v-btn>

          <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="refreshList">
            {{ t('jobOrder.jobList.actions.refresh') }}
          </v-btn>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ errorMessage }}</v-alert>
        <v-alert
          v-if="showInitialWindowNotice"
          type="info"
          variant="tonal"
          class="mt-3 mb-2"
        >
          {{ t('jobOrder.jobList.initialWindowNotice') }}
        </v-alert>

        <div class="toolbar-bar mb-2">
          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-view-column">
                {{ t('jobOrder.jobList.actions.columns') }}
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
                {{ t('jobOrder.jobList.actions.sorting') }}
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
                :label="t('jobOrder.jobList.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('jobOrder.jobList.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('jobOrder.jobList.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
            {{ t('jobOrder.jobList.actions.checkbox') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-open-in-app" :disabled="!activeRow" @click="openPopup">
            {{ t('jobOrder.jobList.actions.popup') }}
          </v-btn>

          <v-divider vertical class="mx-1" />

          <v-btn variant="outlined" size="small" prepend-icon="mdi-printer" @click="printList">
            {{ t('jobOrder.jobList.actions.print') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-file-delimited-outline" :disabled="rows.length === 0" @click="exportToCsv">
            {{ t('jobOrder.jobList.actions.export') }}
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
            {{ t('jobOrder.jobList.actions.deleteSelected') }}
          </v-btn>

          <span class="text-caption text-medium-emphasis" v-if="checkboxMode">
            {{ t('jobOrder.jobList.actions.selected', { count: selectedOrderIds.length }) }}
          </span>

          <span class="text-caption text-medium-emphasis" v-else-if="activeRow">
            {{ t('jobOrder.jobList.selectedOrder', { order: compositeOrderNumber(activeRow) }) }}
          </span>

          <span class="text-caption text-medium-emphasis" v-else>
            {{ t('jobOrder.jobList.noSelection') }}
          </span>
        </div>

        <v-data-table
          :headers="headers"
          :items="displayedRows"
          :loading="loading"
          v-model="selectedOrderIds"
          :show-select="checkboxMode"
          item-value="orderId"
          density="compact"
          fixed-header
          height="62vh"
          class="job-list-table"
          @click:row="onRowClick"
        >
          <template #[`item.ln`]="{ index }">{{ index + 1 }}</template>

          <template #[`header.orderType`]>
            <span class="sr-only">{{ t('jobOrder.jobList.headers.orderType') }}</span>
            <v-icon size="14" color="primary">mdi-tag-outline</v-icon>
          </template>

          <template #[`header.status`]>
            <span class="sr-only">{{ t('jobOrder.jobList.headers.status') }}</span>
            <v-icon size="14" color="primary">mdi-flag</v-icon>
          </template>

          <template #[`header.attachProduct`]>
            <span class="sr-only">{{ t('jobOrder.jobList.headers.attachProduct') }}</span>
            <v-icon size="14" color="primary">mdi-paperclip</v-icon>
          </template>

          <template #[`header.attachCustomer`]>
            <span class="sr-only">{{ t('jobOrder.jobList.headers.attachCustomer') }}</span>
            <v-icon size="14" color="primary">mdi-paperclip</v-icon>
          </template>

          <template #[`item.orderType`]="{ item }">
            <div class="d-flex justify-center">
              <v-icon size="16" :color="orderTypeMeta(item.orderType).color">{{ orderTypeMeta(item.orderType).icon }}</v-icon>
            </div>
          </template>

          <template #[`item.orderNumber`]="{ item }">
            <v-btn variant="text" color="primary" density="comfortable" class="px-0 text-none" @click.stop="openEditor(item)">
              {{ compositeOrderNumber(item) }}
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

          <template #[`item.orderedOn`]="{ item }">{{ formatDate(item.orderedOn) }}</template>
          <template #[`item.requiredOn`]="{ item }">{{ formatDate(item.requiredOn) }}</template>
          <template #[`item.completedOn`]="{ item }">{{ formatDate(item.completedOn) }}</template>
          <template #[`item.modifiedOn`]="{ item }">{{ formatDate(item.modifiedOn) }}</template>
          <template #[`item.modifiedBy`]="{ item }">{{ item.modifiedBy || '-' }}</template>
          <template #[`item.invoiceRef`]="{ item }">{{ item.invoiceRef || '-' }}</template>
          <template #[`item.invoiceAmount`]="{ item }">{{ formatCurrency(item.invoiceAmount) }}</template>
          <template #[`item.productStyle`]="{ item }">{{ item.productStyle || '-' }}</template>
        </v-data-table>

        <div class="text-caption text-medium-emphasis mt-2">
          {{ t('jobOrder.jobList.rows', { count: formatNumber(displayedRows.length) }) }}
        </div>
      </v-card-text>
    </v-card>

    <v-dialog v-model="formOpen" max-width="760" scrollable>
      <JobOrderForm
        v-if="formJob"
        :job="formJob"
        @saved="handleSaved"
        @cancel="formOpen = false"
      />
    </v-dialog>

    <v-snackbar v-model="saveSuccess" color="success" timeout="3000">
      {{ t('jobOrder.saved') }}
      <template #actions>
        <v-btn variant="text" @click="saveSuccess = false">{{ t('jobOrder.dismiss') }}</v-btn>
      </template>
    </v-snackbar>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import JobOrderForm from '@/components/forms/JobOrderForm.vue'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { getJobDetail } from '@/services/jobs'
import { deleteJobOrder, getJobList } from '@/services/jobOrders'
import type { JobDetail, JobOrderRecord } from '@/types/api'

const rows = ref<JobOrderRecord[]>([])
const loading = ref(false)
const deleting = ref(false)
const errorMessage = ref('')
const lookup = ref('')
const commonQuery = ref(0)
const checkboxMode = ref(false)
const selectedOrderIds = ref<string[]>([])
const activeOrderId = ref<string | null>(null)
const sortDirection = ref<'asc' | 'desc'>('desc')
const sortKey = ref('orderNumber')
const visibleColumnKeys = ref<string[]>([
  'orderType',
  'ln',
  'orderNumber',
  'status',
  'orderedOn',
  'customerName',
  'orderTitle',
  'attachProduct',
  'customerRef',
  'attachCustomer',
  'orderedBy',
  'productStyle',
  'invoiceAmount',
  'invoiceRef',
  'requiredOn',
  'modifiedOn',
  'modifiedBy',
  'completedOn',
])
const formOpen = ref(false)
const formJob = ref<JobDetail | null>(null)
const saveSuccess = ref(false)

const { t } = useI18n({ useScope: 'global' })
const { formatCurrency: formatCurrencyByLocale, formatDate: formatDateByLocale, formatNumber } = useLocaleFormatters()

const commonQueryItems = computed(() => [
  { value: 0, label: t('jobOrder.jobList.commonQueryItems.none') },
  { value: 1, label: t('jobOrder.jobList.commonQueryItems.ordered30') },
  { value: 2, label: t('jobOrder.jobList.commonQueryItems.ordered90') },
])

const allHeaders = computed(() => [
  { title: t('jobOrder.jobList.headers.orderType'), key: 'orderType', width: '52px', sortable: false },
  { title: t('jobOrder.jobList.headers.ln'), key: 'ln', width: '52px', sortable: false },
  { title: t('jobOrder.jobList.headers.order'), key: 'orderNumber', width: '132px' },
  { title: t('jobOrder.jobList.headers.status'), key: 'status', width: '72px', sortable: false },
  { title: t('jobOrder.jobList.headers.orderedOn'), key: 'orderedOn', width: '122px' },
  { title: t('jobOrder.jobList.headers.customer'), key: 'customerName', minWidth: '220px' },
  { title: t('jobOrder.jobList.headers.orderTitle'), key: 'orderTitle', minWidth: '240px' },
  { title: t('jobOrder.jobList.headers.attachProduct'), key: 'attachProduct', width: '72px', sortable: false },
  { title: t('jobOrder.jobList.headers.customerRef'), key: 'customerRef', width: '160px' },
  { title: t('jobOrder.jobList.headers.attachCustomer'), key: 'attachCustomer', width: '72px', sortable: false },
  { title: t('jobOrder.jobList.headers.orderedBy'), key: 'orderedBy', width: '100px' },
  { title: t('jobOrder.jobList.headers.quotation'), key: 'productStyle', width: '120px' },
  { title: t('jobOrder.jobList.headers.invoiceAmount'), key: 'invoiceAmount', width: '132px', align: 'end' as const },
  { title: t('jobOrder.jobList.headers.invoiceRef'), key: 'invoiceRef', width: '110px' },
  { title: t('jobOrder.jobList.headers.requiredOn'), key: 'requiredOn', width: '122px' },
  { title: t('jobOrder.jobList.headers.modifiedOn'), key: 'modifiedOn', width: '122px' },
  { title: t('jobOrder.jobList.headers.modifiedBy'), key: 'modifiedBy', width: '100px' },
  { title: t('jobOrder.jobList.headers.completedOn'), key: 'completedOn', width: '122px' },
])

const headers = computed(() => allHeaders.value.filter((header) => visibleColumnKeys.value.includes(String(header.key))))

const sortableColumns = computed(() =>
  allHeaders.value
    .filter((header) => header.sortable !== false && header.key !== 'status' && header.key !== 'attachProduct' && header.key !== 'attachCustomer')
    .map((header) => ({ key: String(header.key), title: String(header.title) })),
)

const columnOptions = computed(() => allHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title) })))

const hasActiveFilters = computed(() => lookup.value.trim().length > 0 || commonQuery.value > 0)
const showInitialWindowNotice = computed(() => !hasActiveFilters.value && rows.value.length > 0)

const displayedRows = computed(() => {
  const result = [...rows.value]
  const key = sortKey.value as keyof JobOrderRecord

  result.sort((lhs, rhs) => {
    const leftValue = valueForSort(lhs, key)
    const rightValue = valueForSort(rhs, key)

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

const activeRow = computed(() => rows.value.find((row) => row.orderId === activeOrderId.value) ?? null)

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''
  selectedOrderIds.value = []
  try {
    rows.value = await getJobList({
      lookup: lookup.value.trim() || undefined,
      commonQuery: commonQuery.value,
    })

    if (activeOrderId.value && !rows.value.some((row) => row.orderId === activeOrderId.value)) {
      activeOrderId.value = rows.value[0]?.orderId ?? null
    }

    if (!activeOrderId.value && rows.value.length > 0) {
      activeOrderId.value = rows.value[0]?.orderId ?? null
    }
  } catch {
    errorMessage.value = t('jobOrder.jobList.loadFailed')
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

function toggleColumn(columnKey: string) {
  if (visibleColumnKeys.value.includes(columnKey)) {
    if (visibleColumnKeys.value.length > 1) {
      visibleColumnKeys.value = visibleColumnKeys.value.filter((key) => key !== columnKey)
    }
    return
  }

  visibleColumnKeys.value = [...visibleColumnKeys.value, columnKey]
}

function onRowClick(_event: Event, payload: { item: JobOrderRecord }) {
  activeOrderId.value = payload.item.orderId
}

async function openPopup() {
  if (!activeRow.value) {
    return
  }

  await openEditor(activeRow.value)
}

async function openEditor(record: JobOrderRecord) {
  activeOrderId.value = record.orderId
  try {
    formJob.value = await getJobDetail(record.orderId)
    formOpen.value = true
  } catch {
    errorMessage.value = t('jobOrder.openEditFailed')
  }
}

async function handleSaved() {
  formOpen.value = false
  saveSuccess.value = true
  await load()
}

function printList() {
  window.print()
}

function exportToCsv() {
  const exportCols = headers.value.filter((header) => !['orderType', 'status', 'attachProduct', 'attachCustomer'].includes(String(header.key)))
  const headerRow = exportCols.map((header) => `"${String(header.title).replace(/"/g, '""')}"`).join(',')
  const dateKeys = new Set(['orderedOn', 'requiredOn', 'completedOn', 'modifiedOn'])

  const dataRows = displayedRows.value.map((row) =>
    exportCols
      .map((header) => {
        const key = String(header.key)

        if (key === 'ln') {
          return '""'
        }

        if (key === 'orderNumber') {
          return `"${compositeOrderNumber(row).replace(/"/g, '""')}"`
        }

        const value = row[key as keyof JobOrderRecord]
        if (value == null || value === '') return '""'
        if (dateKeys.has(key)) return `"${formatDate(value as string)}"`
        if (typeof value === 'number' && key === 'invoiceAmount') return `"${formatCurrency(value)}"`
        return `"${String(value).replace(/"/g, '""')}"`
      })
      .join(','),
  )

  const csv = '\uFEFF' + [headerRow, ...dataRows].join('\r\n')
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = `job-list-${new Date().toISOString().slice(0, 10)}.csv`
  anchor.click()
  URL.revokeObjectURL(url)
}

async function confirmBatchDelete() {
  const message = t('jobOrder.jobList.batchDeleteConfirm', { count: selectedOrderIds.value.length })
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
    errorMessage.value = t('jobOrder.jobList.batchDeleteFailed')
  }
}

function valueForSort(row: JobOrderRecord, key: keyof JobOrderRecord) {
  if (key === 'orderNumber') {
    return compositeOrderNumber(row)
  }

  if (key === 'jobNumber') {
    const numeric = Number.parseInt(row.jobNumber, 10)
    return Number.isFinite(numeric) ? numeric : row.jobNumber
  }

  return row[key]
}

function compositeOrderNumber(row: JobOrderRecord) {
  return row.jobNumber ? `${row.orderNumber}-${row.jobNumber}` : row.orderNumber
}

function formatDate(value: string | null | undefined) {
  if (!value) return '-'
  return formatDateByLocale(value)
}

function formatCurrency(value: number) {
  return formatCurrencyByLocale(value)
}

function statusColor(status: number) {
  if (status <= 0) return 'grey'
  if (status === 1) return 'amber'
  if (status === 2) return 'success'
  return 'error'
}

function orderTypeMeta(orderType: number) {
  switch (orderType) {
    case 1:
      return { icon: 'mdi-tag-text-outline', color: 'error' }
    case 2:
      return { icon: 'mdi-label-outline', color: 'warning' }
    case 3:
      return { icon: 'mdi-shape-outline', color: 'secondary' }
    default:
      return { icon: 'mdi-tag-outline', color: 'success' }
  }
}
</script>

<style scoped>
.job-list-page {
  min-height: 0;
}

.job-list-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
  background: linear-gradient(180deg, rgba(224, 237, 255, 0.92), rgba(241, 247, 255, 0.96));
}

.filter-bar {
  display: grid;
  gap: 12px;
  grid-template-columns: minmax(240px, 1fr) minmax(180px, 260px) auto auto;
  align-items: center;
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

.job-list-table :deep(thead th) {
  white-space: nowrap;
  background: rgba(195, 216, 248, 0.72);
}

.job-list-table :deep(tbody td) {
  font-size: 12px;
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}
</style>