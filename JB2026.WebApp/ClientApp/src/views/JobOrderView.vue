<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">Job order register</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">Dedicated Job Order surface backed by /api/v2/job-orders.</p>
        </div>
        <v-spacer />
        <v-text-field
          v-model="keyword"
          density="comfortable"
          label="Search order/customer"
          prepend-inner-icon="mdi-magnify"
          variant="solo-filled"
          hide-details
        />
        <v-btn color="primary" :loading="loading" @click="load">Refresh</v-btn>
      </v-card-title>
      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">
          {{ errorMessage }}
        </v-alert>

        <v-data-table
          :headers="headers"
          :items="filteredRows"
          :loading="loading"
          item-value="orderId"
          @click:row="onRowClick"
        >
          <template #item.orderedOn="{ item }">{{ formatDate(item.orderedOn) }}</template>
          <template #item.requiredOn="{ item }">{{ formatDate(item.requiredOn) }}</template>
          <template #item.qty="{ item }">{{ formatQty(item.qty) }}</template>
        </v-data-table>

        <v-divider class="my-4" />

        <div v-if="selected">
          <h4 class="text-subtitle-1 mb-2">Selected order</h4>
          <div class="text-body-2">{{ selected.orderNumber }}-{{ selected.jobNumber }} · {{ selected.customerName }}</div>
          <div class="text-body-2">{{ selected.orderTitle }}</div>
          <div class="text-body-2">Required: {{ formatDate(selected.requiredOn) }} · Qty: {{ formatQty(selected.qty) }}</div>
        </div>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { getJobOrder, getJobOrders } from '@/services/jobOrders'
import type { JobOrderRecord } from '@/types/api'

const rows = ref<JobOrderRecord[]>([])
const loading = ref(false)
const errorMessage = ref('')
const selected = ref<JobOrderRecord | null>(null)
const keyword = ref('')

const headers = [
  { title: 'Order', key: 'orderNumber' },
  { title: 'Job #', key: 'jobNumber' },
  { title: 'Customer', key: 'customerName' },
  { title: 'Title', key: 'orderTitle' },
  { title: 'Ordered', key: 'orderedOn' },
  { title: 'Required', key: 'requiredOn' },
  { title: 'Qty', key: 'qty' },
]

const filteredRows = computed(() => {
  const token = keyword.value.trim().toLowerCase()
  if (!token) return rows.value

  return rows.value.filter((row) =>
    row.orderNumber.toLowerCase().includes(token) ||
    row.customerName.toLowerCase().includes(token) ||
    row.orderTitle.toLowerCase().includes(token),
  )
})

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    rows.value = await getJobOrders()
    selected.value = rows.value.length > 0 ? rows.value[0] : null
  } catch {
    errorMessage.value = 'Unable to load job orders. Please verify API availability.'
  } finally {
    loading.value = false
  }
}

async function onRowClick(_event: Event, payload: { item: JobOrderRecord }) {
  try {
    selected.value = await getJobOrder(payload.item.orderId)
  } catch {
    selected.value = payload.item
  }
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString()
}

function formatQty(value: number) {
  return new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 }).format(value)
}
</script>