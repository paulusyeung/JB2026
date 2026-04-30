<template>
  <section class="page-section completed-list-page">
    <v-card rounded="xl" elevation="0" class="panel-card completed-list-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('jobOrder.completed.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('jobOrder.completed.subtitle') }}</p>
        </div>
      </v-card-title>

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('jobOrder.completed.lookup')"
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
            :label="t('jobOrder.completed.commonQuery')"
            variant="solo-filled"
            density="comfortable"
            hide-details
          />

          <v-btn-toggle v-model="machineFilter" mandatory density="compact" variant="outlined" class="machine-toggle">
            <v-btn value="0" size="small">{{ t('jobOrder.completed.machine.all') }}</v-btn>
            <v-btn value="1" size="small">M1</v-btn>
            <v-btn value="2" size="small">M2</v-btn>
            <v-btn value="3" size="small">M3</v-btn>
            <v-btn value="4" size="small">M4</v-btn>
            <v-btn value="5" size="small">M5</v-btn>
          </v-btn-toggle>

          <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loading" @click="applyLookup">
            {{ t('jobOrder.completed.search') }}
          </v-btn>

          <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="refreshList">
            {{ t('common.refresh') }}
          </v-btn>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ errorMessage }}</v-alert>

        <div class="toolbar-bar mb-2 mt-2">
          <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
            {{ t('jobOrder.completed.actions.checkbox') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-open-in-app" :disabled="!activeRow" @click="openPopup">
            {{ t('jobOrder.completed.actions.popup') }}
          </v-btn>

          <v-divider vertical class="mx-1" />

          <v-btn
            color="warning"
            variant="outlined"
            size="small"
            prepend-icon="mdi-calendar-refresh"
            :disabled="!canReschedule"
            :loading="rescheduling"
            @click="rescheduleSelected"
          >
            {{ t('jobOrder.completed.actions.reschedule') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-printer" @click="printList">
            {{ t('jobOrder.completed.actions.print') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-file-delimited-outline" :disabled="rows.length === 0" @click="exportToCsv">
            {{ t('jobOrder.completed.actions.export') }}
          </v-btn>
        </div>

        <div v-if="isPhoneLayout" class="completed-mobile-list mt-2">
          <v-card
            v-for="item in displayedRows"
            :key="item.orderId"
            rounded="lg"
            elevation="0"
            class="completed-mobile-card"
            @click="openEditor(item)"
          >
            <div class="completed-mobile-card__header">
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

            <div class="completed-mobile-card__chips mt-2">
              <v-chip size="x-small" variant="tonal">M{{ item.machineNumber || '-' }}</v-chip>
              <v-chip size="x-small" variant="tonal">LN: {{ item.ln }}</v-chip>
            </div>

            <div class="completed-mobile-card__meta text-caption text-medium-emphasis mt-2">
              <span>{{ t('jobOrder.completed.headers.orderedOn') }}: {{ format(item.orderedOn) }}</span>
              <span>{{ t('jobOrder.completed.headers.requiredOn') }}: {{ format(item.requiredOn) }}</span>
              <span>{{ t('jobOrder.completed.headers.scheduledOn') }}: {{ format(item.scheduledOn) }}</span>
              <span>{{ t('jobOrder.completed.headers.completedOn') }}: {{ format(item.completedOn, DATE_FORMATS.SHORT_DATETIME) }}</span>
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
          class="completed-list-table"
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

          <template #[`item.machineNumber`]="{ item }">
            <div class="d-flex justify-center">
              <v-chip size="x-small" variant="tonal">{{ item.machineNumber || '-' }}</v-chip>
            </div>
          </template>

          <template #[`item.ln`]="{ index }">{{ index + 1 }}</template>
          <template #[`item.orderedOn`]="{ item }">{{ format(item.orderedOn) }}</template>
          <template #[`item.requiredOn`]="{ item }">{{ format(item.requiredOn) }}</template>
          <template #[`item.scheduledOn`]="{ item }">{{ format(item.scheduledOn) }}</template>
          <template #[`item.completedOn`]="{ item }">{{ format(item.completedOn, DATE_FORMATS.SHORT_DATETIME) }}</template>
        </v-data-table>

        <div class="text-caption text-medium-emphasis mt-2">
          {{ t('jobOrder.completed.rows', { count: formatNumber(displayedRows.length) }) }}
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
import { useDisplay } from 'vuetify'
import JobOrderActionDialogs from '@/components/forms/JobOrderActionDialogs.vue'
import JobOrderForm from '@/components/forms/JobOrderForm.vue'
import JobOrderPrintManagerDialog from '@/components/forms/JobOrderPrintManagerDialog.vue'
import { getJobDetail } from '@/services/jobs'
import { getCompletedSchedule, rescheduleCompletedOrders } from '@/services/scheduler'
import type { JobDetail, JobScheduleCompletedItem } from '@/types/api'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'

const rows = ref<JobScheduleCompletedItem[]>([])
const loading = ref(false)
const rescheduling = ref(false)
const errorMessage = ref('')
const lookup = ref('')
const commonQuery = ref(1)
const machineFilter = ref('0')
const checkboxMode = ref(false)
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

const { t } = useI18n({ useScope: 'global' })
const { format, DATE_FORMATS } = useGlobalDateFormatter()
const { formatNumber } = useLocaleFormatters()
const router = useRouter()
const display = useDisplay()
const isPhoneLayout = computed(() => display.smAndDown.value)

const commonQueryItems = computed(() => [
  { value: 0, label: t('jobOrder.completed.commonQueryItems.none') },
  { value: 1, label: t('jobOrder.completed.commonQueryItems.completed7') },
  { value: 2, label: t('jobOrder.completed.commonQueryItems.completed30') },
])

const headers = computed(() => [
  { title: t('jobOrder.completed.headers.order'), key: 'orderNumber', width: '130px' },
  { title: t('jobOrder.completed.headers.orderType'), key: 'orderType', width: '58px', sortable: false },
  { title: t('jobOrder.completed.headers.machine'), key: 'machineNumber', width: '72px', sortable: false },
  { title: t('jobOrder.completed.headers.ln'), key: 'ln', width: '48px', sortable: false },
  { title: t('jobOrder.completed.headers.customer'), key: 'customerName', minWidth: '220px' },
  { title: t('jobOrder.completed.headers.orderTitle'), key: 'orderTitle', minWidth: '220px' },
  { title: t('jobOrder.completed.headers.orderedOn'), key: 'orderedOn', width: '122px' },
  { title: t('jobOrder.completed.headers.requiredOn'), key: 'requiredOn', width: '122px' },
  { title: t('jobOrder.completed.headers.scheduledOn'), key: 'scheduledOn', width: '122px' },
  { title: t('jobOrder.completed.headers.completedOn'), key: 'completedOn', width: '146px' },
])

const displayedRows = computed(() => rows.value)
const activeRow = computed(() => rows.value.find((row) => row.orderId === activeOrderId.value) ?? null)
const canReschedule = computed(() => selectedOrderIds.value.length > 0 || !!activeRow.value)

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''
  selectedOrderIds.value = []

  try {
    rows.value = await getCompletedSchedule({
      lookup: lookup.value.trim() || undefined,
      commonQuery: commonQuery.value,
      machine: machineFilter.value,
      take: 1000,
    })

    if (activeOrderId.value && !rows.value.some((row) => row.orderId === activeOrderId.value)) {
      activeOrderId.value = rows.value[0]?.orderId ?? null
    }

    if (!activeOrderId.value && rows.value.length > 0) {
      activeOrderId.value = rows.value[0]?.orderId ?? null
    }
  } catch {
    errorMessage.value = t('jobOrder.completed.loadFailed')
  } finally {
    loading.value = false
  }
}

async function applyLookup() {
  await load()
}

async function refreshList() {
  lookup.value = ''
  commonQuery.value = 1
  machineFilter.value = '0'
  await load()
}

function toggleSelectedOrder(orderId: string) {
  if (selectedOrderIds.value.includes(orderId)) {
    selectedOrderIds.value = selectedOrderIds.value.filter((id) => id !== orderId)
    return
  }

  selectedOrderIds.value = [...selectedOrderIds.value, orderId]
}

function onRowClick(_event: Event, payload: { item: JobScheduleCompletedItem }) {
  activeOrderId.value = payload.item.orderId
}

async function openPopup() {
  if (!activeRow.value) {
    return
  }

  await openEditor(activeRow.value)
}

async function openEditor(record: JobScheduleCompletedItem) {
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

async function rescheduleSelected() {
  const targetIds = selectedOrderIds.value.length > 0
    ? [...selectedOrderIds.value]
    : activeRow.value
      ? [activeRow.value.orderId]
      : []

  if (targetIds.length === 0) {
    return
  }

  if (!window.confirm(t('jobOrder.completed.rescheduleConfirm', { count: targetIds.length }))) {
    return
  }

  rescheduling.value = true
  errorMessage.value = ''

  try {
    await rescheduleCompletedOrders({ orderIds: targetIds })
    await load()
  } catch {
    errorMessage.value = t('jobOrder.completed.rescheduleFailed')
  } finally {
    rescheduling.value = false
  }
}

function printList() {
  window.print()
}

function exportToCsv() {
  const exportCols = headers.value.filter((header) => !['orderType', 'machineNumber', 'ln'].includes(String(header.key)))
  const headerRow = exportCols.map((header) => `"${String(header.title).replace(/"/g, '""')}"`).join(',')
  const dataRows = displayedRows.value.map((item) => {
    const values = exportCols.map((header) => {
      const key = String(header.key) as keyof JobScheduleCompletedItem
      const raw = item[key]
      const formatted =
        key === 'orderedOn' || key === 'requiredOn' || key === 'scheduledOn' || key === 'completedOn'
          ? format(raw as string | null, key === 'completedOn' ? DATE_FORMATS.SHORT_DATETIME : DATE_FORMATS.ISO_DATE)
          : String(raw ?? '')
      return `"${formatted.replace(/"/g, '""')}"`
    })
    return values.join(',')
  })

  const csv = [headerRow, ...dataRows].join('\n')
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
  const link = document.createElement('a')
  link.href = URL.createObjectURL(blob)
  link.setAttribute('download', 'job-schedule-completed.csv')
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
}

function orderTypeMeta(orderType: number) {
  switch (orderType) {
    case 0:
      return { icon: 'mdi-tag-outline', color: 'success' }
    case 1:
      return { icon: 'mdi-label-outline', color: 'error' }
    case 2:
      return { icon: 'mdi-ribbon', color: 'warning' }
    default:
      return { icon: 'mdi-shape-outline', color: 'info' }
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
    showActionNotice(t('jobOrder.completed.loadFailed'))
  }
}
</script>

<style scoped>
.completed-list-page {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.filter-bar {
  display: grid;
  grid-template-columns: minmax(220px, 1.4fr) minmax(200px, 1fr) auto auto auto;
  gap: 0.75rem;
  align-items: center;
}

.toolbar-bar {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  align-items: center;
}

.machine-toggle :deep(.v-btn) {
  min-width: 40px;
}

.completed-mobile-list {
  display: grid;
  gap: 12px;
}

.completed-mobile-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  padding: 12px;
}

.completed-mobile-card__header {
  display: flex;
  justify-content: space-between;
  gap: 8px;
}

.completed-mobile-card__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.completed-mobile-card__meta {
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
