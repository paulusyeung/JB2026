<template>
  <v-card rounded="xl" elevation="0" class="panel-card">
    <v-card-title class="d-flex flex-wrap align-center ga-3">
      <div>
        <h3 class="text-h6 mb-1">{{ t('jobs.table.title') }}</h3>
        <p class="text-body-2 text-medium-emphasis mb-0">{{ t('jobs.table.subtitle') }}</p>
      </div>
      <v-spacer />
      <v-text-field
        v-model="jobsStore.filter"
        density="comfortable"
        :label="t('jobs.table.filterRows')"
        prepend-inner-icon="mdi-magnify"
        variant="solo-filled"
        flat
        hide-details
      />
    </v-card-title>
    <v-card-text>
      <v-data-table-server
        v-if="!virtual.prefersVirtualScroll"
        v-model:page="jobsStore.page"
        v-model:items-per-page="jobsStore.itemsPerPage"
        v-model:sort-by="jobsStore.sortBy"
        :headers="headers"
        :items="jobsStore.filteredRows"
        :items-length="jobsStore.filteredRows.length"
        :loading="jobsStore.loading"
        item-value="orderId"
        :loading-text="t('jobs.table.loading')"
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
          <v-chip size="small" color="secondary" variant="tonal">{{ t('jobs.status', { value: item.status }) }}</v-chip>
        </template>
      </v-data-table-server>

      <template v-else>
        <div class="vt-header" role="row">
          <span v-for="h in headers" :key="h.key" class="vt-cell" :class="h.align === 'end' ? 'text-end' : ''" role="columnheader">
            {{ h.title }}
          </span>
        </div>
        <v-virtual-scroll
          :items="jobsStore.filteredRows"
          height="480"
          item-height="52"
          class="vt-scroll-body"
        >
          <template #default="{ item }">
            <div
              class="vt-row"
              role="row"
              tabindex="0"
              @click="handleSelectVirtual(item)"
              @keyup.enter="handleSelectVirtual(item)"
            >
              <span class="vt-cell">{{ item.orderNumber }}</span>
              <span class="vt-cell">{{ item.customerName }}</span>
              <span class="vt-cell">{{ item.customerRef }}</span>
              <span class="vt-cell">{{ item.orderTitle }}</span>
              <span class="vt-cell">{{ formatDate(item.requiredOn) }}</span>
              <span class="vt-cell text-end">{{ formatQty(item.qty) }}</span>
              <span class="vt-cell">
                <v-chip size="x-small" color="secondary" variant="tonal">{{ t('jobs.status', { value: item.status }) }}</v-chip>
              </span>
            </div>
          </template>
        </v-virtual-scroll>
        <p class="text-caption text-medium-emphasis mt-2 text-right">
          {{ t('jobs.table.rowsVirtual', { count: formatNumber(jobsStore.filteredRows.length) }) }}
        </p>
      </template>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { useVirtualScrollThreshold } from '@/composables/useVirtualScrollThreshold'
import { useJobsStore } from '@/stores/jobs'
import type { JobListItem } from '@/types/api'

const jobsStore = useJobsStore()
const virtual = useVirtualScrollThreshold()
const { t } = useI18n({ useScope: 'global' })
const { formatDate: formatDateByLocale, formatNumber } = useLocaleFormatters()

const headers = computed(() => [
  { title: t('jobs.table.headers.order'), key: 'orderNumber' },
  { title: t('jobs.table.headers.customer'), key: 'customerName' },
  { title: t('jobs.table.headers.reference'), key: 'customerRef' },
  { title: t('jobs.table.headers.title'), key: 'orderTitle' },
  { title: t('jobs.table.headers.required'), key: 'requiredOn' },
  { title: t('jobs.table.headers.qty'), key: 'qty', align: 'end' as const },
  { title: t('jobs.table.headers.status'), key: 'status' },
])

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

function handleSelectVirtual(item: JobListItem) {
  void jobsStore.select(item.orderId)
}

function formatDate(value: string) {
  return formatDateByLocale(value)
}

function formatQty(value: number) {
  return formatNumber(value, { maximumFractionDigits: 2 })
}
</script>

<style scoped>
.vt-header,
.vt-row {
  display: grid;
  grid-template-columns: 1fr 1.5fr 1fr 2fr 1fr 0.6fr 1fr;
  align-items: center;
  gap: 0 8px;
  padding: 0 12px;
}

.vt-header {
  height: 40px;
  font-size: 0.75rem;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: rgba(var(--v-theme-on-surface), 0.6);
  border-bottom: 1px solid rgba(var(--v-theme-on-surface), 0.12);
}

.vt-row {
  height: 52px;
  border-bottom: 1px solid rgba(var(--v-theme-on-surface), 0.08);
  cursor: pointer;
  transition: background-color 0.12s;
}

.vt-row:hover {
  background-color: rgba(var(--v-theme-primary), 0.06);
}

.vt-row:focus-visible {
  outline: 2px solid rgb(var(--v-theme-primary));
  outline-offset: -2px;
}

.vt-cell {
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
  font-size: 0.875rem;
}

.vt-scroll-body {
  border: 1px solid rgba(var(--v-theme-on-surface), 0.12);
  border-radius: 4px;
}
</style>
