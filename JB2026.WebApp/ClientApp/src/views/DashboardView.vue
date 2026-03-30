<template>
  <section class="page-section">
    <div class="hero-card">
      <div>
        <p class="eyebrow mb-2">{{ t('dashboard.eyebrow') }}</p>
        <h1 class="text-h3 mb-3">{{ t('dashboard.title') }}</h1>
        <p class="text-body-1 text-medium-emphasis mb-0">
          {{ t('dashboard.description') }}
        </p>
      </div>
      <div class="hero-badge">/dashboard</div>
    </div>

    <div class="grid-three">
      <KpiCard
        :label="t('dashboard.kpi.enabledSlicesLabel')"
        :value="String(featureFlags.enabledCount)"
        :helper="t('dashboard.kpi.enabledSlicesHelper')"
        icon="mdi-flag-variant-outline"
      />
      <KpiCard
        :label="t('dashboard.kpi.jobsLoadedLabel')"
        :value="String(jobs.rows.length)"
        :helper="t('dashboard.kpi.jobsLoadedHelper')"
        icon="mdi-briefcase-clock-outline"
      />
      <KpiCard
        :label="t('dashboard.kpi.quotationsLoadedLabel')"
        :value="String(quotations.rowCount)"
        :helper="t('dashboard.kpi.quotationsLoadedHelper')"
        icon="mdi-file-document-outline"
      />
    </div>

    <div class="grid-two">
      <v-card rounded="xl" elevation="0" class="panel-card">
        <v-card-title class="d-flex justify-space-between align-center">
          <div>
            <h3 class="text-h6 mb-1">{{ t('dashboard.sliceHealth.title') }}</h3>
            <p class="text-body-2 text-medium-emphasis mb-0">{{ t('dashboard.sliceHealth.subtitle') }}</p>
          </div>
          <v-btn variant="text" color="primary" @click="reload">{{ t('common.refresh') }}</v-btn>
        </v-card-title>
        <v-card-text>
          <v-list lines="two">
            <v-list-item v-for="flag in featureFlags.flags" :key="flag.key" :title="flag.displayName" :subtitle="flag.prefixes.join(', ')">
              <template #append>
                <v-chip :color="flag.enabled ? 'success' : 'warning'" variant="tonal">
                  {{ flag.enabled ? t('dashboard.sliceHealth.enabled') : t('dashboard.sliceHealth.legacy') }}
                </v-chip>
              </template>
            </v-list-item>
          </v-list>
        </v-card-text>
      </v-card>

      <v-card rounded="xl" elevation="0" class="panel-card">
        <v-card-title>
          <h3 class="text-h6 mb-1">{{ t('dashboard.volumeTrend.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('dashboard.volumeTrend.subtitle') }}</p>
        </v-card-title>
        <v-card-text>
          <Bar :data="chartData" :options="chartOptions" />
        </v-card-text>
      </v-card>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { Bar } from 'vue-chartjs'
import {
  BarElement,
  CategoryScale,
  Chart as ChartJS,
  Legend,
  LinearScale,
  Tooltip,
} from 'chart.js'
import KpiCard from '@/components/cards/KpiCard.vue'
import { useFeatureFlagsStore } from '@/stores/featureFlags'
import { useJobsStore } from '@/stores/jobs'
import { useQuotationsStore } from '@/stores/quotations'

ChartJS.register(CategoryScale, LinearScale, BarElement, Legend, Tooltip)

const featureFlags = useFeatureFlagsStore()
const jobs = useJobsStore()
const quotations = useQuotationsStore()
const { t } = useI18n({ useScope: 'global' })

onMounted(async () => {
  await reload()
})

const chartData = computed(() => ({
  labels: [
    t('dashboard.volumeTrend.labels.featureFlags'),
    t('dashboard.volumeTrend.labels.jobs'),
    t('dashboard.volumeTrend.labels.quotations'),
  ],
  datasets: [
    {
      label: t('dashboard.volumeTrend.datasetLabel'),
      backgroundColor: ['#9f4f2a', '#284b63', '#c9923d'],
      borderRadius: 12,
      data: [featureFlags.enabledCount, jobs.rows.length, quotations.rowCount],
    },
  ],
}))

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      display: false,
    },
  },
  scales: {
    y: {
      beginAtZero: true,
      ticks: {
        precision: 0,
      },
    },
  },
}

async function reload() {
  await Promise.all([featureFlags.load(), jobs.load(), quotations.load()])
}
</script>