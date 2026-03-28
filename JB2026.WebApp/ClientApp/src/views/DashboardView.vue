<template>
  <section class="page-section">
    <div class="hero-card">
      <div>
        <p class="eyebrow mb-2">Slice A</p>
        <h1 class="text-h3 mb-3">Read-only lists and dashboards are live behind the SPA host.</h1>
        <p class="text-body-1 text-medium-emphasis mb-0">
          This shell exercises Vuetify layout, feature flags, Chart.js reporting, and the jobs/quotations API surfaces.
        </p>
      </div>
      <div class="hero-badge">/dashboard</div>
    </div>

    <div class="grid-three">
      <KpiCard label="Enabled slices" :value="String(featureFlags.enabledCount)" helper="Server-side flags currently serving SPA routes" icon="mdi-flag-variant-outline" />
      <KpiCard label="Jobs loaded" :value="String(jobs.rows.length)" helper="Active job list response from /api/v2/jobs/range" icon="mdi-briefcase-clock-outline" />
      <KpiCard label="Quotations loaded" :value="String(quotations.rowCount)" helper="Quotation list response from /api/v2/quotations" icon="mdi-file-document-outline" />
    </div>

    <div class="grid-two">
      <v-card rounded="xl" elevation="0" class="panel-card">
        <v-card-title class="d-flex justify-space-between align-center">
          <div>
            <h3 class="text-h6 mb-1">Slice health</h3>
            <p class="text-body-2 text-medium-emphasis mb-0">Flags pulled from /ui/feature-flags.</p>
          </div>
          <v-btn variant="text" color="primary" @click="reload">Refresh</v-btn>
        </v-card-title>
        <v-card-text>
          <v-list lines="two">
            <v-list-item v-for="flag in featureFlags.flags" :key="flag.key" :title="flag.displayName" :subtitle="flag.prefixes.join(', ')">
              <template #append>
                <v-chip :color="flag.enabled ? 'success' : 'warning'" variant="tonal">
                  {{ flag.enabled ? 'Enabled' : 'Legacy' }}
                </v-chip>
              </template>
            </v-list-item>
          </v-list>
        </v-card-text>
      </v-card>

      <v-card rounded="xl" elevation="0" class="panel-card">
        <v-card-title>
          <h3 class="text-h6 mb-1">Volume trend</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">Chart.js replacement for the legacy dashboard chart block.</p>
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

onMounted(async () => {
  await reload()
})

const chartData = computed(() => ({
  labels: ['Feature Flags', 'Jobs', 'Quotations'],
  datasets: [
    {
      label: 'Current volume',
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