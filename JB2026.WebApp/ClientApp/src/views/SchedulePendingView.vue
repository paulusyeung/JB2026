<template>
  <section class="page-section pending-list-page" :class="{ 'pending-list-page--dark': isDark }">
    <v-card rounded="xl" elevation="0" class="panel-card pending-list-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('jobOrder.pending.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('jobOrder.pending.subtitle') }}</p>
        </div>
      </v-card-title>

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('jobOrder.pending.lookup')"
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
            :label="t('jobOrder.pending.commonQuery')"
            variant="solo-filled"
            density="comfortable"
            hide-details
          />

          <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loading" @click="applyLookup">
            {{ t('jobOrder.pending.search') }}
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
                {{ t('jobOrder.pending.actions.columns') }}
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
                {{ t('jobOrder.pending.actions.sorting') }}
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
                :label="t('jobOrder.pending.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('jobOrder.pending.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('jobOrder.pending.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
            {{ t('jobOrder.pending.actions.checkbox') }}
          </v-btn>

          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                {{ t('jobOrder.pending.actions.views') }}
              </v-btn>
            </template>
            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>{{ t('jobOrder.pending.actions.detailView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>{{ t('jobOrder.pending.actions.cardView') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <v-divider vertical class="mx-1" />

          <!-- Workflow light actions (selection-gated, inline @1 / @2 circles + bells) -->
          <span class="text-caption text-medium-emphasis">@1:</span>
          <v-btn v-for="c in pendingLightColors" :key="`p1-${c.code}`"
            icon size="x-small" density="compact" :color="c.color" variant="tonal"
            :disabled="selectedOrderIds.length === 0 || workflowActionLoading"
            :title="c.label"
            @click="applyWorkflow(0, c.code)">
            <v-icon size="12">mdi-circle</v-icon>
          </v-btn>
          <v-divider vertical class="mx-1" />
          <span class="text-caption text-medium-emphasis">@2:</span>
          <v-btn v-for="c in pendingLightColors" :key="`p2-${c.code}`"
            icon size="x-small" density="compact" :color="c.color" variant="tonal"
            :disabled="selectedOrderIds.length === 0 || workflowActionLoading"
            :title="c.label"
            @click="applyWorkflow(1, c.code)">
            <v-icon size="12">mdi-circle</v-icon>
          </v-btn>
          <v-divider vertical class="mx-1" />

          <!-- Urgency bells (selection-gated, toggles off if active) -->
          <v-btn
            icon size="x-small" density="compact" color="error" variant="tonal"
            :disabled="selectedOrderIds.length === 0 || urgencyActionLoading"
            :title="t('jobOrder.pending.actions.bellRed')"
            @click="applyUrgency('red')"
          >
            <v-icon size="12">mdi-bell-alert</v-icon>
          </v-btn>
          <v-btn
            icon size="x-small" density="compact" color="warning" variant="tonal"
            :disabled="selectedOrderIds.length === 0 || urgencyActionLoading"
            :title="t('jobOrder.pending.actions.bellYellow')"
            @click="applyUrgency('yellow')"
          >
            <v-icon size="12">mdi-bell</v-icon>
          </v-btn>

          <v-divider vertical class="mx-1" />

          <v-btn variant="outlined" size="small" prepend-icon="mdi-printer" @click="printList">
            {{ t('jobOrder.pending.actions.print') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-file-delimited-outline" :disabled="rows.length === 0" @click="exportToCsv">
            {{ t('jobOrder.pending.actions.export') }}
          </v-btn>
        </div>

        <div v-if="isPhoneLayout" class="pending-mobile-list mt-2">
          <v-card
            v-for="item in displayedRows"
            :key="item.orderId"
            rounded="lg"
            elevation="0"
            class="pending-mobile-card"
            @click="openEditor(item)"
          >
            <div class="pending-mobile-card__header">
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

            <div class="pending-mobile-card__chips mt-2">
              <v-chip size="x-small" variant="tonal">{{ t('jobOrder.pending.headers.orderType') }}: {{ item.orderType }}</v-chip>
              <v-chip size="x-small" variant="tonal" :color="statusColor(item.status)">{{ t('jobOrder.pending.headers.status') }}</v-chip>
              <v-chip size="x-small" variant="tonal">@1: {{ item.step1Status ?? '-' }}</v-chip>
              <v-chip size="x-small" variant="tonal">@2: {{ item.step2Status ?? '-' }}</v-chip>
              <v-chip size="x-small" variant="tonal">@3: {{ item.step3Status ?? '-' }}</v-chip>
            </div>

            <div class="pending-mobile-card__meta text-caption text-medium-emphasis mt-2">
              <span>{{ t('jobOrder.pending.headers.orderedOn') }}: {{ format(item.orderedOn) }}</span>
              <span>{{ t('jobOrder.pending.headers.requiredOn') }}: {{ format(item.requiredOn) }}</span>
            </div>

            <div class="pending-mobile-card__actions mt-2">
              <v-btn size="x-small" variant="text" color="primary" prepend-icon="mdi-open-in-app" @click.stop="openEditor(item)">
                {{ t('jobOrder.pending.actions.popup') }}
              </v-btn>
            </div>
          </v-card>
        </div>

        <div v-else-if="isCardView" class="pending-card-list mt-2">
          <v-card
            v-for="item in displayedRows"
            :key="item.orderId"
            rounded="lg"
            elevation="0"
            class="pending-card"
            @click="openEditor(item)"
          >
            <div class="pending-card__header">
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

            <div class="pending-card__chips mt-2">
              <v-chip size="x-small" variant="tonal">{{ t('jobOrder.pending.headers.orderType') }}: {{ item.orderType }}</v-chip>
              <v-chip size="x-small" variant="tonal" :color="statusColor(item.status)">{{ t('jobOrder.pending.headers.status') }}</v-chip>
              <v-chip size="x-small" variant="tonal">@1: {{ item.step1Status ?? '-' }}</v-chip>
              <v-chip size="x-small" variant="tonal">@2: {{ item.step2Status ?? '-' }}</v-chip>
              <v-chip size="x-small" variant="tonal">@3: {{ item.step3Status ?? '-' }}</v-chip>
            </div>

            <div class="pending-card__meta text-caption text-medium-emphasis mt-2">
              <span>{{ t('jobOrder.pending.headers.orderedOn') }}: {{ format(item.orderedOn) }}</span>
              <span>{{ t('jobOrder.pending.headers.requiredOn') }}: {{ format(item.requiredOn) }}</span>
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
          class="pending-list-table"
          @click:row="onRowClick"
        >
          <template #[`item.orderNumber`]="{ item }">
            <v-btn variant="text" color="primary" density="comfortable" class="px-0 text-none" @click.stop="openEditor(item)">
              {{ item.orderNumber }}
            </v-btn>
          </template>

          <template #[`item.orderType`]="{ item }">
            <div class="d-flex justify-center">
              <v-icon size="16" :color="orderTypeMeta(item.orderType).color">{{ orderTypeMeta(item.orderType).icon }}</v-icon>
            </div>
          </template>

          <template #[`item.status`]="{ item }">
            <div class="d-flex justify-center">
              <v-icon size="16" :color="statusColor(item.status)">mdi-flag</v-icon>
            </div>
          </template>

          <template #[`item.step1Status`]="{ item }">
            <div class="d-flex justify-center">
              <v-icon size="14" :color="workflowColor(item.step1Status)">mdi-circle</v-icon>
            </div>
          </template>

          <template #[`item.step2Status`]="{ item }">
            <div class="d-flex justify-center">
              <v-icon size="14" :color="workflowColor(item.step2Status)">mdi-circle</v-icon>
            </div>
          </template>

          <template #[`item.step3Status`]="{ item }">
            <div class="d-flex justify-center">
              <v-icon size="14" :color="workflowColor(item.step3Status)">mdi-circle</v-icon>
            </div>
          </template>

          <template #[`item.urgencyLevel`]="{ item }">
            <div class="d-flex justify-center">
              <v-icon v-if="urgencyIcon(item.urgencyLevel)" size="14" :color="urgencyColor(item.urgencyLevel)">{{ urgencyIcon(item.urgencyLevel) }}</v-icon>
            </div>
          </template>

          <template #[`header.urgencyLevel`]>
            <div class="d-flex justify-center">
              <v-icon size="14" color="grey-darken-2">mdi-bell</v-icon>
            </div>
          </template>

          <template #[`item.orderedOn`]="{ item }">{{ format(item.orderedOn) }}</template>
          <template #[`item.requiredOn`]="{ item }">{{ format(item.requiredOn) }}</template>
        </v-data-table>

        <div class="text-caption text-medium-emphasis mt-2">
          {{ t('jobOrder.pending.rows', { count: formatNumber(displayedRows.length) }) }}
        </div>
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
      />
    </v-dialog>

    <JobOrderActionDialogs
      :job="formJob"
      v-model:attachment-open="attachmentDialogOpen"
      v-model:product-details-open="productDetailsDialogOpen"
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
import { useDisplay, useTheme } from 'vuetify'
import { useViewSettings } from '@/composables/useColumnPersistence'
import JobOrderActionDialogs from '@/components/forms/JobOrderActionDialogs.vue'
import JobOrderForm from '@/components/forms/JobOrderForm.vue'
import JobOrderPrintManagerDialog from '@/components/forms/JobOrderPrintManagerDialog.vue'
import { getJobDetail } from '@/services/jobs'
import { getPendingSchedule, updatePendingUrgency, updatePendingWorkflow } from '@/services/scheduler'
import type { JobDetail, JobSchedulePendingItem } from '@/types/api'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'

const rows = ref<JobSchedulePendingItem[]>([])
const loading = ref(false)
const errorMessage = ref('')
const lookup = ref('')
// commonQuery mapping: 0=None, 1=Ordered in last 30 days, 2=Ordered in last 90 days.
const commonQuery = ref(0)
const selectedOrderIds = ref<string[]>([])
const activeOrderId = ref<string | null>(null)
const formOpen = ref(false)
const formJob = ref<JobDetail | null>(null)
const actionNoticeOpen = ref(false)
const actionNoticeMessage = ref('')
const attachmentDialogOpen = ref(false)
const productDetailsDialogOpen = ref(false)
const printManagerOpen = ref(false)
const printManagerJob = ref<JobDetail | null>(null)
const workflowActionLoading = ref(false)
const urgencyActionLoading = ref(false)

const viewSettings = useViewSettings('pending-schedule', {
  visibleColumns: [
    'orderNumber',
    'orderType',
    'status',
    'step1Status',
    'step2Status',
    'step3Status',
    'urgencyLevel',
    'customerName',
    'orderTitle',
    'orderedOn',
    'requiredOn',
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
const { formatNumber } = useLocaleFormatters()
const router = useRouter()
const display = useDisplay()
const theme = useTheme()
const isDark = computed(() => theme.global.current.value.dark)
const isPhoneLayout = computed(() => display.smAndDown.value)
const isCardView = computed(() => viewMode.value === 'card')

const commonQueryItems = computed(() => [
  { value: 0, label: t('jobOrder.pending.commonQueryItems.none') },
  { value: 1, label: t('jobOrder.pending.commonQueryItems.ordered30') },
  { value: 2, label: t('jobOrder.pending.commonQueryItems.ordered90') },
])

const allHeaders = computed(() => [
  { title: t('jobOrder.pending.headers.order'), key: 'orderNumber', width: '132px' },
  { title: t('jobOrder.pending.headers.orderType'), key: 'orderType', width: '58px', sortable: false },
  { title: t('jobOrder.pending.headers.status'), key: 'status', width: '58px', sortable: false },
  { title: '@1', key: 'step1Status', width: '52px', sortable: false },
  { title: '@2', key: 'step2Status', width: '52px', sortable: false },
  { title: '@3', key: 'step3Status', width: '52px', sortable: false },
  { title: t('jobOrder.pending.headers.urgency'), key: 'urgencyLevel', width: '70px', sortable: false },
  { title: t('jobOrder.pending.headers.customer'), key: 'customerName', minWidth: '220px' },
  { title: t('jobOrder.pending.headers.orderTitle'), key: 'orderTitle', minWidth: '240px' },
  { title: t('jobOrder.pending.headers.orderedOn'), key: 'orderedOn', width: '122px' },
  { title: t('jobOrder.pending.headers.requiredOn'), key: 'requiredOn', width: '122px' },
])

const headers = computed(() => allHeaders.value.filter((header) => visibleColumnKeys.value.includes(String(header.key))))
const columnOptions = computed(() => allHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title) })))
const sortableColumns = computed(() =>
  allHeaders.value
    .filter((header) => header.sortable !== false)
    .map((header) => ({ key: String(header.key), title: String(header.title) })),
)

const displayedRows = computed(() => {
  const result = [...rows.value]
  const key = sortKey.value as keyof JobSchedulePendingItem

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

const activeRow = computed(() => rows.value.find((row) => row.orderId === activeOrderId.value) ?? null)

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''
  selectedOrderIds.value = []

  try {
    rows.value = await getPendingSchedule({
      lookup: lookup.value.trim() || undefined,
      commonQuery: commonQuery.value,
      take: 1000,
    })

    if (activeOrderId.value && !rows.value.some((row) => row.orderId === activeOrderId.value)) {
      activeOrderId.value = null
    }
  } catch {
    errorMessage.value = t('jobOrder.pending.loadFailed')
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
      visibleColumnKeys.value = visibleColumnKeys.value.filter((key) => key !== columnKey)
    }
    return
  }

  visibleColumnKeys.value = [...visibleColumnKeys.value, columnKey]
}

function onRowClick(_event: Event, payload: { item: JobSchedulePendingItem }) {
  activeOrderId.value = payload.item.orderId
}

function setViewMode(mode: 'detail' | 'card') {
  viewMode.value = mode
}

async function openPopup() {
  if (!activeRow.value) {
    return
  }

  await openEditor(activeRow.value)
}

async function openEditor(record: JobSchedulePendingItem) {
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

function printList() {
  window.print()
}

function exportToCsv() {
  const exportCols = headers.value.filter((header) => !['orderType', 'status', 'step1Status', 'step2Status', 'step3Status', 'urgencyLevel'].includes(String(header.key)))
  const headerRow = exportCols.map((header) => `"${String(header.title).replace(/"/g, '""')}"`).join(',')

  const dataRows = displayedRows.value.map((row) =>
    exportCols
      .map((header) => {
        const key = String(header.key) as keyof JobSchedulePendingItem
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
  anchor.download = `job-schedule-pending-${new Date().toISOString().slice(0, 10)}.csv`
  anchor.click()
  URL.revokeObjectURL(url)
}



function statusColor(status: number) {
  if (status <= 0) return 'grey'
  if (status === 1) return 'success'
  if (status === 2) return 'warning'
  return 'error'
}

function workflowColor(status: number | null) {
  if (status == null) return 'grey-lighten-1'
  if (status === 0) return 'error'
  if (status === 1) return 'warning'
  if (status === 2) return 'success'
  if (status === 3) return 'info'
  return 'grey'
}

const pendingLightColors = computed(() => [
  { code: 0, color: 'error',   label: t('jobOrder.pending.workflow.red') },
  { code: 1, color: 'warning', label: t('jobOrder.pending.workflow.yellow') },
  { code: 2, color: 'success', label: t('jobOrder.pending.workflow.green') },
  { code: 3, color: 'info',    label: t('jobOrder.pending.workflow.blue') },
])

async function applyWorkflow(stepIndex: number, targetStatus: number) {
  if (selectedOrderIds.value.length === 0) return
  workflowActionLoading.value = true
  try {
    for (const orderId of selectedOrderIds.value) {
      const result = await updatePendingWorkflow(orderId, { stepIndex, targetStatus })
      const rowIndex = rows.value.findIndex((r) => r.orderId === orderId)
      if (rowIndex !== -1) {
        rows.value[rowIndex] = {
          ...rows.value[rowIndex],
          step1Status: result.step1Status ?? rows.value[rowIndex].step1Status,
          step2Status: result.step2Status ?? rows.value[rowIndex].step2Status,
          step3Status: result.step3Status ?? rows.value[rowIndex].step3Status,
        }
      }
    }
  } catch {
    showActionNotice(t('jobOrder.pending.workflow.updateFailed'))
  } finally {
    workflowActionLoading.value = false
  }
}

async function applyUrgency(targetColor: 'red' | 'yellow') {
  if (selectedOrderIds.value.length === 0) return
  urgencyActionLoading.value = true
  try {
    for (const orderId of selectedOrderIds.value) {
      const result = await updatePendingUrgency(orderId, { targetColor })
      const rowIndex = rows.value.findIndex((r) => r.orderId === orderId)
      if (rowIndex !== -1) {
        rows.value[rowIndex] = {
          ...rows.value[rowIndex],
          urgencyLevel: result.urgencyLevel,
        }
      }
    }
  } catch {
    showActionNotice(t('jobOrder.pending.workflow.urgencyFailed'))
  } finally {
    urgencyActionLoading.value = false
  }
}

function urgencyIcon(level: number) {
  // Be tolerant to payload shape; some responses can serialize numbers as strings/null.
  const urgency = Number(level)
  if (!Number.isFinite(urgency) || urgency === -1) return 'mdi-stop-circle-outline'
  if (urgency === 4) return 'mdi-bell'
  if (urgency === 2) return 'mdi-bell'
  return ''
}

function urgencyColor(level: number) {
  const urgency = Number(level)
  if (urgency === 4) return 'error'
  if (urgency === 2) return 'warning'
  if (!Number.isFinite(urgency) || urgency === -1) return 'error'
  return 'grey'
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
    showActionNotice(t('jobOrder.pending.loadFailed'))
  }
}
</script>

<style scoped>
.pending-list-page {
  min-height: 0;
  --pending-list-header-bg: rgba(195, 216, 248, 0.92);
  --pending-list-header-fg: inherit;
}

.pending-list-page--dark {
  --pending-list-header-bg: rgba(52, 74, 104, 0.95);
  --pending-list-header-fg: rgba(239, 246, 255, 0.98);
}

.pending-list-card {
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

.pending-list-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.pending-list-table :deep(.v-table__wrapper > table > thead > tr > th),
.pending-list-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--pending-list-header-bg) !important;
  color: var(--pending-list-header-fg) !important;
}

.pending-list-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.pending-list-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.pending-list-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.pending-list-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

.pending-list-table :deep(tbody td) {
  font-size: 12px;
}

.pending-mobile-list {
  display: grid;
  gap: 12px;
}

.pending-card-list {
  display: grid;
  gap: 12px;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .pending-card-list {
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    align-items: start;
  }
}

.pending-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  padding: 12px;
  cursor: pointer;
}

.pending-card__header {
  display: flex;
  justify-content: space-between;
  gap: 8px;
}

.pending-card__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.pending-card__meta {
  display: grid;
  gap: 2px;
}

.pending-mobile-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  padding: 12px;
}

.pending-mobile-card__header {
  display: flex;
  justify-content: space-between;
  gap: 8px;
}

.pending-mobile-card__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.pending-mobile-card__meta {
  display: grid;
  gap: 2px;
}

.pending-mobile-card__actions {
  display: flex;
  justify-content: flex-end;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}
</style>
