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
        label="Orders Loaded"
        :value="String(orders.uniqueOrderCount)"
        helper="Total number of active orders"
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
        :label="t('dashboard.kpi.quotationsLoadedLabel')"
        :value="String(quotations.rowCount)"
        :helper="t('dashboard.kpi.quotationsLoadedHelper')"
        icon="mdi-file-document-outline"
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
        <ActivityTimeline :items="mockActivities" />
      </v-col>
    </v-row>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
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
import KpiCard from '@/components/cards/KpiCard.vue'
import ActivityTimeline from '@/components/layout/ActivityTimeline.vue'
import DashboardFilters from '@/components/layout/DashboardFilters.vue'
import { useOrdersStore } from '@/stores/orders'
import { useQuotationsStore } from '@/stores/quotations'
import { useThemeStore } from '@/stores/theme'
import type { ActivityItem } from '@/components/layout/ActivityTimeline.vue'
import type { JobOrderRecord } from '@/types/api'

const quotations = useQuotationsStore()
const orders = useOrdersStore()
const themeStore = useThemeStore()

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

const chartType = ref<'bar' | 'line' | 'pie'>('bar')
const dashboardFilters = ref({
  dateRange: 'Last 30 Days',
  search: '',
})

const mockActivities = ref<ActivityItem[]>([
  { type: 'job', title: 'Job #9842 Updated', status: 'In Progress', timestamp: '2 mins ago' },
  { type: 'quote', title: 'New Quotation Drafted', status: 'Draft', timestamp: '15 mins ago' },
  { type: 'invoice', title: 'Invoice #2024-05 Paid', status: 'Paid', timestamp: '1 hour ago' },
  { type: 'job', title: 'Job #9840 Completed', status: 'Completed', timestamp: '3 hours ago' },
  { type: 'system', title: 'System Maintenance', status: 'Scheduled', timestamp: 'Today' },
])

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
  labels: ['Orders', t('dashboard.volumeTrend.labels.jobs'), t('dashboard.volumeTrend.labels.quotations')],
  datasets: [
    {
      label: t('dashboard.volumeTrend.datasetLabel'),
      backgroundColor: chartPalette.value.bars,
      borderRadius: 12,
      data: [orders.uniqueOrderCount, jobListRows.value.length, quotations.rowCount],
    },
  ],
}))

const chartPalette = computed(() =>
  themeStore.mode === 'dark'
    ? {
        bars: ['#e29a60', '#8cb9d4', '#d8ab58'],
        axis: '#d7ddd3',
        grid: 'rgba(215, 221, 211, 0.16)',
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
    case 'Today':
      params.startOn = today
      params.endOn = today
      break
    case 'Last 7 Days': {
      const start = new Date(now)
      start.setDate(start.getDate() - 6)
      params.startOn = formatDateOnly(start)
      params.endOn = today
      break
    }
    case 'Last 30 Days': {
      const start = new Date(now)
      start.setDate(start.getDate() - 29)
      params.startOn = formatDateOnly(start)
      params.endOn = today
      break
    }
    case 'Last 90 Days': {
      const start = new Date(now)
      start.setDate(start.getDate() - 89)
      params.startOn = formatDateOnly(start)
      params.endOn = today
      break
    }
    case 'This Year':
      params.startOn = `${now.getFullYear()}-01-01`
      params.endOn = today
      break
    default:
      break
  }

  return params
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
    const dateParam = dateRangeParams.startOn ? new Date(dateRangeParams.startOn) : undefined

    await Promise.all([
      getJobList(orderParams).then((rows) => {
        jobListRows.value = rows
      }),
      quotations.load(dateParam),
      orders.load(orderParams),
    ])

    quotations.keyword = dashboardFilters.value.search || ''
  } catch (e) {
    console.error('Failed to load dashboard data:', e)
    error.value = 'Failed to load some dashboard components. Please try again.'
  } finally {
    loading.value = false
  }
}
</script>
