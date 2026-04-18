<template>
  <section class="page-section">
    <v-alert v-if="error" type="error" variant="tonal" class="mb-6" closable @click:close="error = null">
      {{ error }}
    </v-alert>

    <!-- Interactive Filters -->
    <DashboardFilters
      v-model:filters="dashboardFilters"
      @refresh="reload"
      class="mb-6"
    />

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
        :value="String(jobs.rows.length)"
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
  BarElement,
  CategoryScale,
  Chart as ChartJS,
  Legend,
  LinearScale,
  PointElement,
  LineElement,
  ArcElement,
  Tooltip,
} from 'chart.js'
import { useFeatureFlagsStore } from '@/stores/featureFlags'
import { useJobsStore } from '@/stores/jobs'
import { useQuotationsStore } from '@/stores/quotations'
import { useOrdersStore } from '@/stores/orders'
import { useThemeStore } from '@/stores/theme'
import KpiCard from '@/components/cards/KpiCard.vue'
import DashboardFilters from '@/components/layout/DashboardFilters.vue';
import ActivityTimeline from '@/components/layout/ActivityTimeline.vue';
import type { ActivityItem } from '@/components/layout/ActivityTimeline.vue';

const featureFlags = useFeatureFlagsStore()
const jobs = useJobsStore()
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
  Tooltip
)

const { t } = useI18n({ useScope: 'global' })

const loading = ref(false);
const error = ref<string | null>(null);

const chartType = ref<'bar' | 'line' | 'pie'>('bar');
const dashboardFilters = ref({
  dateRange: 'Last 30 Days',
  status: [],
  search: '',
});

const mockActivities = ref<ActivityItem[]>([
  { type: 'job', title: 'Job #9842 Updated', status: 'In Progress', timestamp: '2 mins ago' },
  { type: 'quote', title: 'New Quotation Drafted', status: 'Draft', timestamp: '15 mins ago' },
  { type: 'invoice', title: 'Invoice #2024-05 Paid', status: 'Paid', timestamp: '1 hour ago' },
  { type: 'job', title: 'Job #9840 Completed', status: 'Completed', timestamp: '3 hours ago' },
  { type: 'system', title: 'System Maintenance', status: 'Scheduled', timestamp: 'Today' },
]);

watch(dashboardFilters, () => {
  reload();
}, { deep: true });

onMounted(async () => {
  await reload()
})

const chartData = computed(() => ({
  labels: [
    'Orders',
    t('dashboard.volumeTrend.labels.jobs'),
    t('dashboard.volumeTrend.labels.quotations'),
  ],
  datasets: [
    {
      label: t('dashboard.volumeTrend.datasetLabel'),
      backgroundColor: chartPalette.value.bars,
      borderRadius: 12,
      data: [orders.uniqueOrderCount, jobs.filteredRows.length, quotations.rowCount],
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

async function reload() {
  loading.value = true;
  error.value = null;
  try {
    const params: any = {
      lookup: dashboardFilters.value.search,
      take: 500, // Match Order List limit
    };

    // Determine commonQuery based on dateRange (matching EfJobManagementRepository logic)
    if (dashboardFilters.value.dateRange === 'Last 7 Days') {
      params.commonQuery = 1;
    } else if (dashboardFilters.value.dateRange === 'Last 30 Days') {
      params.commonQuery = 2;
    }

    // Calculate specific date range for other stores
    const now = new Date();
    if (dashboardFilters.value.dateRange === 'Today') {
      params.startOn = now.toISOString().slice(0, 10);
      params.endOn = now.toISOString().slice(0, 10);
    } else if (dashboardFilters.value.dateRange === 'Last 7 Days') {
      const d = new Date();
      d.setDate(d.getDate() - 7);
      params.startOn = d.toISOString().slice(0, 10);
    } else if (dashboardFilters.value.dateRange === 'Last 30 Days') {
      const d = new Date();
      d.setDate(d.getDate() - 30);
      params.startOn = d.toISOString().slice(0, 10);
    } else if (dashboardFilters.value.dateRange === 'Last 90 Days') {
      const d = new Date();
      d.setDate(d.getDate() - 90);
      params.startOn = d.toISOString().slice(0, 10);
    } else if (dashboardFilters.value.dateRange === 'This Year') {
      params.startOn = `${now.getFullYear()}-01-01`;
    }
    
    // Pass the search query to stores that support it
    const dateParam = params.startOn ? new Date(params.startOn) : undefined;
    await Promise.all([
      featureFlags.load(),
      jobs.load(dateParam), 
      quotations.load(dateParam),
      orders.load(params)
    ]);

    // If search is present but store doesn't support params in load(), 
    // we use their internal filtering (search/keyword)
    if (dashboardFilters.value.search) {
      jobs.filter = dashboardFilters.value.search;
      quotations.keyword = dashboardFilters.value.search;
    } else {
      jobs.filter = '';
      quotations.keyword = '';
    }
  } catch (e) {
    console.error('Failed to load dashboard data:', e);
    error.value = 'Failed to load some dashboard components. Please try again.';
  } finally {
    loading.value = false;
  }
}
</script>