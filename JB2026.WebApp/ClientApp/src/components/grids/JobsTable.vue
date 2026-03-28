<template>
  <v-card rounded="xl" elevation="0" class="panel-card">
    <v-card-title class="d-flex flex-wrap align-center ga-3">
      <div>
        <h3 class="text-h6 mb-1">Jobs</h3>
        <p class="text-body-2 text-medium-emphasis mb-0">Server-style grid with filter, pagination, and sort.</p>
      </div>
      <v-spacer />
      <v-text-field
        v-model="jobsStore.filter"
        density="comfortable"
        label="Filter rows"
        prepend-inner-icon="mdi-magnify"
        variant="solo-filled"
        flat
        hide-details
      />
    </v-card-title>
    <v-card-text>
      <v-alert v-if="virtual.prefersVirtualScroll" type="info" variant="tonal" class="mb-4">
        This slice will switch to the virtual-scroll composable once row volume exceeds 500.
      </v-alert>

      <v-data-table-server
        v-model:page="jobsStore.page"
        v-model:items-per-page="jobsStore.itemsPerPage"
        v-model:sort-by="jobsStore.sortBy"
        :headers="headers"
        :items="jobsStore.filteredRows"
        :items-length="jobsStore.filteredRows.length"
        :loading="jobsStore.loading"
        item-value="orderId"
        loading-text="Loading jobs"
        class="jobs-table"
        @click:row="handleSelect"
      >
        <template #item.requiredOn="{ item }">
          {{ formatDate(item.requiredOn) }}
        </template>
        <template #item.qty="{ item }">
          {{ formatQty(item.qty) }}
        </template>
        <template #item.status="{ item }">
          <v-chip size="small" color="secondary" variant="tonal">Status {{ item.status }}</v-chip>
        </template>
      </v-data-table-server>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { onMounted, watch } from 'vue'
import { useJobsStore } from '@/stores/jobs'
import { useVirtualScrollThreshold } from '@/composables/useVirtualScrollThreshold'
import type { JobListItem } from '@/types/api'

const jobsStore = useJobsStore()
const virtual = useVirtualScrollThreshold()

const headers = [
  { title: 'Order', key: 'orderNumber' },
  { title: 'Customer', key: 'customerName' },
  { title: 'Reference', key: 'customerRef' },
  { title: 'Title', key: 'orderTitle' },
  { title: 'Required', key: 'requiredOn' },
  { title: 'Qty', key: 'qty', align: 'end' as const },
  { title: 'Status', key: 'status' },
]

onMounted(async () => {
  if (jobsStore.rows.length === 0) {
    await jobsStore.load()
  }
})

watch(
  () => jobsStore.rows.length,
  (count) => virtual.setRowCount(count),
  { immediate: true },
)

function handleSelect(_: Event, payload: { item: JobListItem }) {
  void jobsStore.select(payload.item.orderId)
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString()
}

function formatQty(value: number) {
  return value.toLocaleString(undefined, { maximumFractionDigits: 2 })
}
</script>