<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('sml.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('sml.subtitle') }}</p>
        </div>
        <v-spacer />
        <v-text-field
          v-model="startOn"
          :label="t('sml.startDate')"
          type="date"
          density="comfortable"
          variant="solo-filled"
          hide-details
        />
        <v-btn color="primary" :loading="loading" @click="load">{{ t('common.refresh') }}</v-btn>
      </v-card-title>
      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">
          {{ errorMessage }}
        </v-alert>

        <div v-if="stats" class="d-flex flex-wrap ga-2 mb-4">
          <v-chip color="secondary" variant="tonal">{{ t('sml.rows', { count: stats.rowCount }) }}</v-chip>
          <v-chip color="accent" variant="tonal">{{ t('sml.total', { amount: formatMoney(stats.totalAmount) }) }}</v-chip>
        </div>

        <h4 class="text-subtitle-1 mb-2">{{ t('sml.monthlyTotals') }}</h4>
        <v-data-table :headers="monthlyHeaders" :items="stats?.monthly ?? []" :loading="loading">
          <template #item.amount="{ item }">
            {{ formatMoney(item.amount) }}
          </template>
        </v-data-table>

        <h4 class="text-subtitle-1 mt-6 mb-2">{{ t('sml.topCustomers') }}</h4>
        <v-data-table :headers="customerHeaders" :items="stats?.topCustomers ?? []" :loading="loading">
          <template #item.amount="{ item }">
            {{ formatMoney(item.amount) }}
          </template>
        </v-data-table>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { getSmlStats } from '@/services/sml'
import type { SmlStatsResponse } from '@/types/api'

const loading = ref(false)
const errorMessage = ref('')
const stats = ref<SmlStatsResponse | null>(null)
const startOn = ref(new Date().toISOString().slice(0, 10))
const { t } = useI18n({ useScope: 'global' })
const { formatCurrency } = useLocaleFormatters()

const monthlyHeaders = computed(() => [
  { title: t('sml.headers.year'), key: 'year' },
  { title: t('sml.headers.month'), key: 'month' },
  { title: t('sml.headers.count'), key: 'count' },
  { title: t('sml.headers.amount'), key: 'amount' },
])

const customerHeaders = computed(() => [
  { title: t('sml.headers.customer'), key: 'customerName' },
  { title: t('sml.headers.count'), key: 'count' },
  { title: t('sml.headers.amount'), key: 'amount' },
])

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    stats.value = await getSmlStats({
      startOn: startOn.value,
      days: 31,
      take: 500,
    })
  } catch {
    errorMessage.value = t('sml.loadFailed')
  } finally {
    loading.value = false
  }
}

function formatMoney(value: number) {
  return formatCurrency(value)
}
</script>