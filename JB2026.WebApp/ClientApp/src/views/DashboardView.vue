<template>
  <section class="page-section">
    <v-alert v-if="error" type="error" variant="tonal" class="mb-6" closable @click:close="error = null">
      {{ error }}
    </v-alert>

    <DashboardFilters v-model:filters="dashboardFilters" @refresh="reload" class="mb-6" />

    <v-overlay :model-value="loading" persistent class="align-center justify-center">
      <v-progress-circular indeterminate color="primary" size="64" />
    </v-overlay>

    <div class="grid-three mb-6">
      <KpiCard
        :label="t('dashboard.kpi.ordersLoadedLabel')"
        :value="String(orders.uniqueOrderCount)"
        :helper="t('dashboard.kpi.ordersLoadedHelper')"
        icon="mdi-cart-outline"
        :trend="0"
      />
      <KpiCard
        :label="t('dashboard.kpi.jobsLoadedLabel')"
        :value="String(jobListRows.length)"
        :helper="t('dashboard.kpi.jobsLoadedHelper')"
        icon="mdi-briefcase-clock-outline"
        :trend="5"
      />
      <KpiCard
        :label="t('dashboard.kpi.invoicesLoadedLabel')"
        :value="String(invoiceCount)"
        :helper="t('dashboard.kpi.invoicesLoadedHelper')"
        icon="mdi-receipt-text-outline"
        :trend="-2"
      />
    </div>

    <!-- TODO: Implement a dedicated /stats/count API for the dashboard to show ACTUAL database counts
         Current cards show count of records loaded in local store session (max 100) -->

    <v-row>
      <v-col cols="12" lg="8">
        <v-card rounded="xl" elevation="0" class="panel-card mb-6 h-100">
          <v-card-title class="d-flex justify-space-between align-center">
            <div>
              <h3 class="text-h6 mb-1">{{ t('dashboard.volumeTrend.title') }}</h3>
              <p class="text-body-2 text-medium-emphasis mb-0">{{ t('dashboard.volumeTrend.subtitle') }}</p>
            </div>
            <v-btn-toggle v-model="chartType" mandatory density="compact" rounded="lg" color="primary">
              <v-btn value="bar" icon="mdi-chart-bar" />
              <v-btn value="line" icon="mdi-chart-line" />
              <v-btn value="pie" icon="mdi-chart-pie" />
            </v-btn-toggle>
          </v-card-title>
          <v-card-text style="height: 400px;">
            <Bar v-if="chartType === 'bar'" :data="chartData" :options="chartOptions" />
            <Line v-else-if="chartType === 'line'" :data="chartData" :options="chartOptions" />
            <div v-else-if="chartType === 'pie'" class="d-flex justify-center h-100">
              <Pie :data="chartData" :options="chartOptions" />
            </div>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" lg="4">
        <ActivityTimeline :items="recentActivities" @open-job="openJobFromActivity" />
      </v-col>
    </v-row>

    <v-dialog v-model="formOpen" max-width="min(100%, 760px)" scrollable>
      <JobOrderForm
        v-if="formOpen"
        :job="formJob"
        @saved="handleFormSaved"
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
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { Bar, Line, Pie } from 'vue-chartjs'
import {
  ArcElement,
  BarElement,
  CategoryScale,
  Chart as ChartJS,
  Legend,
  LineElement,
  LinearScale,
  PointElement,
  Tooltip,
} from 'chart.js'
import { getJobList } from '@/services/jobOrders'
import { getJobDetail } from '@/services/jobs'
import KpiCard from '@/components/cards/KpiCard.vue'
import ActivityTimeline from '@/components/layout/ActivityTimeline.vue'
import DashboardFilters from '@/components/layout/DashboardFilters.vue'
import JobOrderActionDialogs from '@/components/forms/JobOrderActionDialogs.vue'
import JobOrderForm from '@/components/forms/JobOrderForm.vue'
import JobOrderPrintManagerDialog from '@/components/forms/JobOrderPrintManagerDialog.vue'
import { useOrdersStore } from '@/stores/orders'
import { listInvoices, type InvoiceBillingSummary } from '@/services/billing'
import { useThemeStore } from '@/stores/theme'
import type { ActivityItem } from '@/components/layout/ActivityTimeline.vue'
import type { JobDetail, JobOrderRecord } from '@/types/api'

type DateRangeKey = 'today' | 'last7Days' | 'last30Days' | 'last90Days' | 'thisYear' | 'allTime'

const invoiceCount = ref(0)
const orders = useOrdersStore()
const themeStore = useThemeStore()
const router = useRouter()

ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  PointElement,
  LineElement,
  ArcElement,
  Legend,
  Tooltip,
)

const { t } = useI18n({ useScope: 'global' })

const loading = ref(false)
const error = ref<string | null>(null)
const jobListRows = ref<JobOrderRecord[]>([])
const formOpen = ref(false)
const formJob = ref<JobDetail | null>(null)
const actionNoticeOpen = ref(false)
const actionNoticeMessage = ref('')
const attachmentDialogOpen = ref(false)
const productDetailsDialogOpen = ref(false)
const remarksDialogOpen = ref(false)
const printManagerOpen = ref(false)
const printManagerJob = ref<JobDetail | null>(null)

const chartType = ref<'bar' | 'line' | 'pie'>('bar')
const dashboardFilters = ref({
  dateRange: 'last30Days' as DateRangeKey,
  search: '',
})

const recentActivities = computed<ActivityItem[]>(() =>
  [...jobListRows.value]
    .sort((a, b) => {
      const aModified = resolveModifiedTimestamp(a)
      const bModified = resolveModifiedTimestamp(b)
      if (bModified !== aModified) return bModified - aModified

      const aCreated = resolveCreatedTimestamp(a)
      const bCreated = resolveCreatedTimestamp(b)
      return bCreated - aCreated
    })
    .slice(0, 5)
    .map((row) => {
      const modifiedTimestamp = resolveModifiedTimestamp(row)
      const createdTimestamp = resolveCreatedTimestamp(row)
      const displayTimestamp = modifiedTimestamp > 0 ? modifiedTimestamp : createdTimestamp

      return {
        type: 'job',
        title: '',
        titlePrefix: t('dashboard.activity.items.jobPrefix'),
        titleSuffix: isJobUpdated(row)
          ? t('dashboard.activity.items.jobUpdatedVerb')
          : t('dashboard.activity.items.jobCreatedVerb'),
        jobOrderId: row.orderId,
        jobNumberDisplay: compositeOrderNumber(row),
        status: getJobStatusLabel(row.status),
        statusTone: getJobStatusTone(row.status),
        timestamp: displayTimestamp <= 0
          ? '-'
          : new Intl.DateTimeFormat(undefined, {
            dateStyle: 'medium',
            timeStyle: 'short',
          }).format(new Date(displayTimestamp)),
      }
    }),
)

function resolveModifiedTimestamp(row: JobOrderRecord) {
  return parseTimestamp(row.modifiedOn) ?? 0
}

function resolveCreatedTimestamp(row: JobOrderRecord) {
  return parseTimestamp(row.createdOn) ?? parseTimestamp(row.orderedOn) ?? 0
}

function isJobUpdated(row: JobOrderRecord) {
  const created = resolveCreatedTimestamp(row)
  const modified = resolveModifiedTimestamp(row)

  if (created <= 0 || modified <= 0) return false
  return modified > created
}

function parseTimestamp(value: string | null | undefined) {
  if (!value) return null
  const parsed = Date.parse(value)
  return Number.isNaN(parsed) ? null : parsed
}

function compositeOrderNumber(row: JobOrderRecord) {
  if (row.orderNumber && row.jobNumber) return `${row.orderNumber}-${row.jobNumber}`
  return row.orderNumber || row.jobNumber || row.orderId
}

function getJobStatusLabel(status: number) {
  if (status <= 0) return t('dashboard.activity.statuses.draft')
  if (status === 1) return t('dashboard.activity.statuses.inProgress')
  if (status === 2) return t('dashboard.activity.statuses.completed')
  return t('dashboard.activity.statuses.scheduled')
}

function getJobStatusTone(status: number): ActivityItem['statusTone'] {
  if (status <= 0) return 'primary'
  if (status === 1) return 'warning'
  if (status === 2) return 'success'
  return 'error'
}

async function openJobFromActivity(orderId: string) {
  try {
    formJob.value = await getJobDetail(orderId)
    formOpen.value = true
  } catch {
    error.value = t('jobOrder.openEditFailed')
  }
}

async function handleFormSaved() {
  formOpen.value = false
  await reload()
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

async function handlePrintOrder(job: JobDetail) {
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
    await reload()
  } catch {
    showActionNotice(t('jobOrder.reloadAfterSaveFailed'))
  }
}

watch(
  dashboardFilters,
  () => {
    void reload()
  },
  { deep: true },
)

onMounted(async () => {
  await reload()
})

const chartData = computed(() => ({
  labels: [
    t('dashboard.volumeTrend.labels.orders'),
    t('dashboard.volumeTrend.labels.jobs'),
    t('dashboard.volumeTrend.labels.invoices'),
  ],
  datasets: [
    {
      label: t('dashboard.volumeTrend.datasetLabel'),
      backgroundColor: chartPalette.value.bars,
      borderRadius: 12,
      data: [orders.uniqueOrderCount, jobListRows.value.length, invoiceCount.value],
    },
  ],
}))

const chartPalette = computed(() =>
  themeStore.mode === 'dark'
    ? {
        bars: ['#e29a60', '#8cb9d4', '#d8ab58'],
        axis: '#d7ddd3',
        grid: 'rgba(237, 241, 235, 0.12)',
        tooltipBackground: '#1e241f',
      }
    : {
        bars: ['#9f4f2a', '#284b63', '#c9923d'],
        axis: '#49514d',
        grid: 'rgba(31, 36, 33, 0.12)',
        tooltipBackground: '#fffdf8',
      },
)

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      display: false,
    },
    tooltip: {
      backgroundColor: chartPalette.value.tooltipBackground,
      titleColor: chartPalette.value.axis,
      bodyColor: chartPalette.value.axis,
      borderColor: chartPalette.value.grid,
      borderWidth: 1,
    },
  },
  scales: {
    x: {
      ticks: {
        color: chartPalette.value.axis,
      },
      grid: {
        color: chartPalette.value.grid,
      },
    },
    y: {
      beginAtZero: true,
      ticks: {
        precision: 0,
        color: chartPalette.value.axis,
      },
      grid: {
        color: chartPalette.value.grid,
      },
    },
  },
}))

function formatDateOnly(value: Date) {
  return value.toISOString().slice(0, 10)
}

function buildDateRangeParams() {
  const now = new Date()
  const today = formatDateOnly(now)
  const params: { startOn?: string; endOn?: string } = {}

  switch (dashboardFilters.value.dateRange) {
    case 'today':
      params.startOn = today
      params.endOn = today
      break
    case 'last7Days': {
      const start = new Date(now)
      start.setDate(start.getDate() - 6)
      params.startOn = formatDateOnly(start)
      params.endOn = today
      break
    }
    case 'last30Days': {
      const start = new Date(now)
      start.setDate(start.getDate() - 29)
      params.startOn = formatDateOnly(start)
      params.endOn = today
      break
    }
    case 'last90Days': {
      const start = new Date(now)
      start.setDate(start.getDate() - 89)
      params.startOn = formatDateOnly(start)
      params.endOn = today
      break
    }
    case 'thisYear':
      params.startOn = `${now.getFullYear()}-01-01`
      params.endOn = today
      break
    default:
      break
  }

  return params
}

function buildDateBoundary(value: string, endOfDay = false) {
  return Date.parse(`${value}T${endOfDay ? '23:59:59.999' : '00:00:00.000'}`)
}

function matchesInvoiceFilters(
  invoice: InvoiceBillingSummary,
  filters: { search: string },
  dateRangeParams: { startOn?: string; endOn?: string },
) {
  const search = filters.search.trim().toLowerCase()
  if (search) {
    const matchesSearch = [invoice.invoiceNumber, invoice.clientName, invoice.status]
      .some((value) => value?.toLowerCase().includes(search))

    if (!matchesSearch) {
      return false
    }
  }

  if (!dateRangeParams.startOn && !dateRangeParams.endOn) {
    return true
  }

  const invoiceTimestamp = parseTimestamp(invoice.invoiceDate) ?? parseTimestamp(invoice.lastSyncedAt)
  if (invoiceTimestamp == null) {
    return false
  }

  const startTimestamp = dateRangeParams.startOn ? buildDateBoundary(dateRangeParams.startOn) : null
  const endTimestamp = dateRangeParams.endOn ? buildDateBoundary(dateRangeParams.endOn, true) : null

  if (startTimestamp != null && invoiceTimestamp < startTimestamp) {
    return false
  }

  if (endTimestamp != null && invoiceTimestamp > endTimestamp) {
    return false
  }

  return true
}

async function reload() {
  loading.value = true
  error.value = null

  try {
    const dateRangeParams = buildDateRangeParams()
    const orderParams = {
      lookup: dashboardFilters.value.search || undefined,
      take: 500,
      startOn: dateRangeParams.startOn,
      endOn: dateRangeParams.endOn,
    }

    await Promise.all([
      getJobList(orderParams).then((rows) => {
        jobListRows.value = rows
      }),
      listInvoices().then((invoices) => {
        invoiceCount.value = invoices.filter((invoice) => matchesInvoiceFilters(invoice, dashboardFilters.value, dateRangeParams)).length
      }),
      orders.load(orderParams),
    ])
  } catch (e) {
    console.error('Failed to load dashboard data:', e)
    error.value = t('dashboard.loadFailed')
  } finally {
    loading.value = false
  }
}
</script>
