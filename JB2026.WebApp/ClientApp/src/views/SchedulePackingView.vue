<template>
  <section class="page-section packing-list-page">
    <v-card rounded="xl" elevation="0" class="panel-card packing-list-card">


      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('jobOrder.packing.lookup')"
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
            :label="t('jobOrder.packing.commonQuery')"
            variant="solo-filled"
            density="comfortable"
            hide-details
          />

          <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loading" @click="applyLookup">
            {{ t('jobOrder.packing.search') }}
          </v-btn>

          <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="refreshList">
            {{ t('common.refresh') }}
          </v-btn>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ errorMessage }}</v-alert>

        <div class="toolbar-bar mb-2 mt-2">
          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-view-column">
                {{ t('jobOrder.packing.actions.columns') }}
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
                {{ t('jobOrder.packing.actions.sorting') }}
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
                :label="t('jobOrder.packing.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('jobOrder.packing.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('jobOrder.packing.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
            {{ t('jobOrder.packing.actions.checkbox') }}
          </v-btn>

          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                {{ t('jobOrder.packing.actions.views') }}
              </v-btn>
            </template>
            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>{{ t('jobOrder.packing.actions.detailView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>{{ t('jobOrder.packing.actions.cardView') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <v-divider vertical class="mx-1" />

          <span class="text-medium">@1:</span>
          <v-btn v-for="c in packingLightColors" :key="`p1-${c.code}`"
            icon size="x-small" density="compact" :color="c.color" variant="tonal"
            :disabled="selectedOrderIds.length === 0 || workflowActionLoading"
            :title="c.label"
            @click="applyWorkflow(0, c.code)">
            <v-icon size="16">mdi-circle</v-icon>
          </v-btn>

          <v-divider vertical class="mx-1" />

          <v-btn variant="outlined" size="small" prepend-icon="mdi-file-delimited-outline" :disabled="rows.length === 0" @click="exportToCsv">
            {{ t('jobOrder.packing.actions.export') }}
          </v-btn>
        </div>

        <div v-if="isPhoneLayout" class="packing-mobile-list mt-2">
          <v-card
            v-for="(item, index) in displayedRows"
            :key="item.orderId"
            rounded="lg"
            elevation="0"
            class="packing-mobile-card"
            @click="openEditor(item)"
          >
            <div class="packing-mobile-card__header">
              <div>
                <div class="text-subtitle-2 font-weight-bold text-primary">{{ item.orderNumber }}</div>
                <div class="text-caption text-medium-emphasis">{{ item.customerName }}</div>
              </div>
              <v-checkbox-btn
                v-if="checkboxMode"
                :model-value="selectedOrderIds.includes(item.orderId)"
                density="compact"
                hide-details
                @click.stop="toggleSelectedOrder(item.orderId)"
              />
            </div>

            <div class="text-body-2 mt-1">{{ item.orderTitle }}</div>

            <div class="packing-mobile-card__chips mt-2">
              <v-chip size="x-small" variant="tonal">@1: {{ item.step1Status ?? '-' }}</v-chip>
              <v-chip size="x-small" variant="tonal">@2: {{ item.step2Status ?? '-' }}</v-chip>
              <v-chip size="x-small" variant="tonal">@3: {{ item.step3Status ?? '-' }}</v-chip>
              <v-chip size="x-small" variant="tonal">LN: {{ index + 1 }}</v-chip>
            </div>

            <div class="packing-mobile-card__meta text-caption text-medium-emphasis mt-2">
              <span>{{ t('jobOrder.packing.headers.orderedOn') }}: {{ format(item.orderedOn) }}</span>
              <span>{{ t('jobOrder.packing.headers.requiredOn') }}: {{ format(item.requiredOn) }}</span>
              <span>{{ t('jobOrder.packing.headers.remarks') }}: {{ item.remarks || '-' }}</span>
            </div>
          </v-card>
        </div>

        <div v-else-if="isCardView" class="packing-card-list mt-2">
          <v-card
            v-for="(item, index) in displayedRows"
            :key="item.orderId"
            rounded="lg"
            elevation="0"
            class="packing-card"
            @click="openEditor(item)"
          >
            <div class="packing-card__header">
              <div>
                <div class="text-subtitle-2 font-weight-bold text-primary">{{ item.orderNumber }}</div>
                <div class="text-caption text-medium-emphasis">{{ item.customerName }}</div>
              </div>
            </div>

            <v-checkbox-btn
              v-if="checkboxMode"
              class="packing-card-checkbox"
              :model-value="selectedOrderIds.includes(item.orderId)"
              density="compact"
              hide-details
              @click.stop="toggleSelectedOrder(item.orderId)"
            />

            <div class="text-body-2 mt-1">{{ item.orderTitle }}</div>

            <div class="packing-card__chips mt-2">
              <v-chip size="x-small" variant="tonal">@1: {{ item.step1Status ?? '-' }}</v-chip>
              <v-chip size="x-small" variant="tonal">@2: {{ item.step2Status ?? '-' }}</v-chip>
              <v-chip size="x-small" variant="tonal">@3: {{ item.step3Status ?? '-' }}</v-chip>
              <v-chip size="x-small" variant="tonal">LN: {{ index + 1 }}</v-chip>
            </div>

            <div class="packing-card__meta text-caption text-medium-emphasis mt-2">
              <span>{{ t('jobOrder.packing.headers.orderedOn') }}: {{ format(item.orderedOn) }}</span>
              <span>{{ t('jobOrder.packing.headers.requiredOn') }}: {{ format(item.requiredOn) }}</span>
            </div>
          </v-card>
        </div>

        <v-data-table
          v-else
          :headers="headers"
          :items="displayedRows"
          :loading="loading"
          v-model="selectedOrderIds"
          :show-select="checkboxMode"
          item-value="orderId"
          density="compact"
          fixed-header
          height="62vh"
          class="packing-list-table"
          @click:row="onRowClick"
        >
          <template #[`item.orderNumber`]="{ item }">
            <v-btn variant="text" color="primary" density="comfortable" class="px-0 text-none" @click.stop="openEditor(item)">
              {{ item.orderNumber }}
            </v-btn>
          </template>

          <template #[`item.orderType`]="{ item }">
            <div class="d-flex justify-center">
              <v-icon size="16" :color="getOrderTypeMeta(item.orderType).color">{{ getOrderTypeMeta(item.orderType).icon }}</v-icon>
            </div>
          </template>

          <template #[`item.step1Status`]="{ item }">
            <div class="d-flex justify-center">
              <v-icon size="16" :color="workflowColor(item.step1Status)">mdi-circle</v-icon>
            </div>
          </template>

          <template #[`item.step2Status`]="{ item }">
            <div class="d-flex justify-center">
              <v-icon size="16" :color="workflowColor(item.step2Status)">mdi-circle</v-icon>
            </div>
          </template>

          <template #[`item.step3Status`]="{ item }">
            <div class="d-flex justify-center">
              <v-icon size="16" :color="workflowColor(item.step3Status)">mdi-circle</v-icon>
            </div>
          </template>

          <template #[`item.ln`]="{ index }">{{ index + 1 }}</template>
          <template #[`item.orderedOn`]="{ item }">{{ format(item.orderedOn) }}</template>
          <template #[`item.requiredOn`]="{ item }">{{ format(item.requiredOn) }}</template>
        </v-data-table>

        
      </v-card-text>
    </v-card>

    <v-dialog v-model="formOpen" max-width="760" scrollable>
      <JobOrderForm
        v-if="formJob"
        :job="formJob"
        @saved="handleSaved"
        @cancel="formOpen = false"
        @attachment="handleAttachment"
        @print-order="handlePrintOrder"
        @workflow="handleWorkflow"
        @product-details-edit="handleProductDetailsEdit"
        @remarks-edit="handleRemarksEdit"
      />
    </v-dialog>

    <JobOrderActionDialogs
      :job="formJob"
      v-model:attachment-open="attachmentDialogOpen"
      v-model:product-details-open="productDetailsDialogOpen"
      v-model:remarks-open="remarksDialogOpen"
      @updated="handleActionUpdated"
      @error="showActionNotice"
    />

    <JobOrderPrintManagerDialog
      v-model="printManagerOpen"
      :order-id="printManagerJob?.orderId ?? null"
      :order-number="printManagerJob?.orderNumber ?? ''"
      :style-titles="printManagerJob?.styleTitles"
    />

    <v-snackbar v-model="actionNoticeOpen" color="info" timeout="3200">
      {{ actionNoticeMessage }}
    </v-snackbar>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useDisplay } from 'vuetify'
import { useViewSettings } from '@/composables/useColumnPersistence'
import JobOrderActionDialogs from '@/components/forms/JobOrderActionDialogs.vue'
import JobOrderForm from '@/components/forms/JobOrderForm.vue'
import JobOrderPrintManagerDialog from '@/components/forms/JobOrderPrintManagerDialog.vue'
import { getJobDetail } from '@/services/jobs'
import { getPackingSchedule, updatePendingWorkflow } from '@/services/scheduler'
import type { JobDetail, JobSchedulePackingItem } from '@/types/api'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { getOrderTypeMeta } from '@/utils/orderType'

const rows = ref<JobSchedulePackingItem[]>([])
const loading = ref(false)
const errorMessage = ref('')
const lookup = ref('')
const commonQuery = ref(0)
const selectedOrderIds = ref<string[]>([])
const activeOrderId = ref<string | null>(null)
const formOpen = ref(false)
const formJob = ref<JobDetail | null>(null)
const actionNoticeOpen = ref(false)
const actionNoticeMessage = ref('')
const attachmentDialogOpen = ref(false)
const productDetailsDialogOpen = ref(false)
const remarksDialogOpen = ref(false)
const printManagerOpen = ref(false)
const printManagerJob = ref<JobDetail | null>(null)
const workflowActionLoading = ref(false)

const viewSettings = useViewSettings('packing-schedule', {
  visibleColumns: [
    'orderNumber',
    'orderType',
    'step1Status',
    'step2Status',
    'step3Status',
    'ln',
    'customerName',
    'orderTitle',
    'orderedOn',
    'requiredOn',
    'remarks',
  ],
  sortKey: 'orderNumber',
  sortDirection: 'desc',
  checkboxMode: false,
  viewMode: 'detail',
})
const visibleColumnKeys = viewSettings.visibleColumns
const sortKey = viewSettings.sortKey
const sortDirection = viewSettings.sortDirection
const checkboxMode = viewSettings.checkboxMode
const viewMode = viewSettings.viewMode

const { t } = useI18n({ useScope: 'global' })
const { format, DATE_FORMATS } = useGlobalDateFormatter()
const router = useRouter()
const display = useDisplay()
const isPhoneLayout = computed(() => display.smAndDown.value)
const isCardView = computed(() => viewMode.value === 'card')

const commonQueryItems = computed(() => [
  { value: 0, label: t('jobOrder.packing.commonQueryItems.none') },
  { value: 1, label: t('jobOrder.packing.commonQueryItems.ordered30') },
  { value: 2, label: t('jobOrder.packing.commonQueryItems.ordered90') },
])

const allHeaders = computed(() => [
  { title: t('jobOrder.packing.headers.order'), key: 'orderNumber', width: '132px' },
  { title: t('jobOrder.packing.headers.orderType'), key: 'orderType', width: '58px', sortable: false },
  { title: '@1', key: 'step1Status', width: '52px', sortable: false },
  { title: '@2', key: 'step2Status', width: '52px', sortable: false },
  { title: '@3', key: 'step3Status', width: '52px', sortable: false },
  { title: t('jobOrder.packing.headers.ln'), key: 'ln', width: '52px', sortable: false },
  { title: t('jobOrder.packing.headers.customer'), key: 'customerName', minWidth: '220px' },
  { title: t('jobOrder.packing.headers.orderTitle'), key: 'orderTitle', minWidth: '220px' },
  { title: t('jobOrder.packing.headers.orderedOn'), key: 'orderedOn', width: '122px' },
  { title: t('jobOrder.packing.headers.requiredOn'), key: 'requiredOn', width: '122px' },
  { title: t('jobOrder.packing.headers.remarks'), key: 'remarks', minWidth: '260px' },
])

const headers = computed(() => allHeaders.value.filter((h) => visibleColumnKeys.value.includes(String(h.key))))
const columnOptions = computed(() => allHeaders.value.map((h) => ({ key: String(h.key), title: String(h.title) })))
const sortableColumns = computed(() =>
  allHeaders.value
    .filter((h) => h.sortable !== false)
    .map((h) => ({ key: String(h.key), title: String(h.title) })),
)

const displayedRows = computed(() => {
  const result = [...rows.value]
  const key = sortKey.value as keyof JobSchedulePackingItem

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

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''
  selectedOrderIds.value = []

  try {
    rows.value = await getPackingSchedule({
      lookup: lookup.value.trim() || undefined,
      commonQuery: commonQuery.value,
      take: 1000,
    })

    if (activeOrderId.value && !rows.value.some((row) => row.orderId === activeOrderId.value)) {
      activeOrderId.value = rows.value[0]?.orderId ?? null
    }

    if (!activeOrderId.value && rows.value.length > 0) {
      activeOrderId.value = rows.value[0]?.orderId ?? null
    }
  } catch {
    errorMessage.value = t('jobOrder.packing.loadFailed')
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

function toggleSelectedOrder(orderId: string) {
  if (selectedOrderIds.value.includes(orderId)) {
    selectedOrderIds.value = selectedOrderIds.value.filter((id) => id !== orderId)
    return
  }

  selectedOrderIds.value = [...selectedOrderIds.value, orderId]
}

function toggleColumn(columnKey: string) {
  if (visibleColumnKeys.value.includes(columnKey)) {
    if (visibleColumnKeys.value.length > 1) {
      visibleColumnKeys.value = visibleColumnKeys.value.filter((k) => k !== columnKey)
    }
    return
  }

  visibleColumnKeys.value = [...visibleColumnKeys.value, columnKey]
}

function setViewMode(mode: 'detail' | 'card') {
  viewMode.value = mode
}

function onRowClick(_event: Event, payload: { item: JobSchedulePackingItem }) {
  activeOrderId.value = payload.item.orderId
}

async function openEditor(record: JobSchedulePackingItem) {
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
  await load()
}

async function applyWorkflow(stepIndex: number, targetStatus: number) {
  if (selectedOrderIds.value.length === 0) return
  workflowActionLoading.value = true
  try {
    for (const orderId of selectedOrderIds.value) {
      const result = await updatePendingWorkflow(orderId, { stepIndex, targetStatus })
      const rowIndex = rows.value.findIndex((r) => r.orderId === orderId)
      if (rowIndex !== -1) {
        const row = rows.value[rowIndex]!
        rows.value[rowIndex] = {
          ...row,
          step1Status: result.step1Status ?? row.step1Status,
          step2Status: result.step2Status ?? row.step2Status,
          step3Status: result.step3Status ?? row.step3Status,
        }
      }
    }
  } catch {
    showActionNotice(t('jobOrder.packing.workflow.updateFailed'))
  } finally {
    workflowActionLoading.value = false
  }
}

function exportToCsv() {
  const exportCols = headers.value.filter((header) => !['orderType', 'step1Status', 'step2Status', 'step3Status', 'ln'].includes(String(header.key)))
  const headerRow = exportCols.map((header) => `"${String(header.title).replace(/"/g, '""')}"`).join(',')

  const dataRows = displayedRows.value.map((row) =>
    exportCols
      .map((header) => {
        const key = String(header.key) as keyof JobSchedulePackingItem
        const value = row[key]
        if (value == null || value === '') return '""'

        if (key === 'orderedOn' || key === 'requiredOn') {
          return `"${format(String(value), DATE_FORMATS.ISO_DATE)}"`
        }

        return `"${String(value).replace(/"/g, '""')}"`
      })
      .join(','),
  )

  const csv = '\uFEFF' + [headerRow, ...dataRows].join('\r\n')
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = `job-schedule-packing-${new Date().toISOString().slice(0, 10)}.csv`
  anchor.click()
  URL.revokeObjectURL(url)
}



const packingLightColors = computed(() => [
  { code: 0, color: 'error',   label: t('jobOrder.packing.workflow.red') },
  { code: 1, color: 'warning', label: t('jobOrder.packing.workflow.yellow') },
  { code: 2, color: 'success', label: t('jobOrder.packing.workflow.green') },
])

function workflowColor(status: number | null) {
  if (status == null) return 'grey-lighten-1'
  if (status === 0) return 'error'
  if (status === 1) return 'warning'
  if (status === 2) return 'success'
  if (status === 3) return 'blue'
  return 'grey-lighten-1'
}

function showActionNotice(message: string) {
  actionNoticeMessage.value = message
  actionNoticeOpen.value = true
}

function handleAttachment(job: JobDetail) {
  formJob.value = job
  attachmentDialogOpen.value = true
}

function handleProductDetailsEdit(job: JobDetail) {
  formJob.value = job
  productDetailsDialogOpen.value = true
}

function handleRemarksEdit(job: JobDetail) {
  formJob.value = job
  remarksDialogOpen.value = true
}

function handlePrintOrder(job: JobDetail) {
  printManagerJob.value = job
  printManagerOpen.value = true
}

function handleWorkflow(job: JobDetail) {
  void router.push({ name: 'admin-workflow', query: { orderId: job.orderId } })
}

async function handleActionUpdated() {
  if (!formJob.value) return

  try {
    formJob.value = await getJobDetail(formJob.value.orderId)
    await load()
  } catch {
    showActionNotice(t('jobOrder.packing.loadFailed'))
  }
}
</script>

<style scoped>
.packing-list-page {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  --packing-list-header-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%);
  --packing-list-header-fg: rgb(var(--v-theme-on-surface-variant));
}

.filter-bar {
  display: grid;
  grid-template-columns: minmax(220px, 1.4fr) minmax(200px, 1fr) auto auto;
  gap: 0.75rem;
  align-items: center;
}

.toolbar-bar {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  align-items: center;
}

.toolbar-menu-list {
  max-height: 340px;
  overflow: auto;
}

.packing-mobile-list {
  display: grid;
  gap: 12px;
}

.packing-mobile-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  padding: 12px;
}

.packing-mobile-card__header {
  display: flex;
  justify-content: space-between;
  gap: 8px;
}

.packing-mobile-card__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.packing-mobile-card__meta {
  display: grid;
  gap: 2px;
}

.packing-list-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.packing-list-table :deep(.v-table__wrapper > table > thead > tr > th),
.packing-list-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--packing-list-header-bg) !important;
  color: var(--packing-list-header-fg) !important;
}

.packing-list-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.packing-list-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.packing-list-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.packing-list-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

.packing-list-table :deep(tbody td) {
  font-size: 12px;
}

.packing-card-list {
  display: grid;
  gap: 12px;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .packing-card-list {
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    align-items: start;
  }
}

.packing-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  padding: 12px;
  position: relative;
}

.packing-card :deep(.packing-card-checkbox) {
  position: absolute;
  top: 6px;
  right: 6px;
}

.packing-card__header {
  display: flex;
  justify-content: space-between;
  gap: 8px;
}

.packing-card__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.packing-card__meta {
  display: grid;
  gap: 2px;
}

@media (max-width: 1200px) {
  .filter-bar {
    grid-template-columns: 1fr 1fr;
  }
}

@media (max-width: 780px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}
</style>
