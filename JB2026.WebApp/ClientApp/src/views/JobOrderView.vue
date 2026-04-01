<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('jobOrder.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('jobOrder.subtitle') }}</p>
        </div>
        <v-spacer />
        <v-text-field
          v-model="keyword"
          density="comfortable"
          :label="t('jobOrder.search')"
          prepend-inner-icon="mdi-magnify"
          variant="solo-filled"
          hide-details
        />
        <v-btn color="primary" :loading="loading" @click="load">{{ t('common.refresh') }}</v-btn>
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
          <template #[`item.orderNumber`]="{ item }">
            <v-btn
              variant="text"
              color="primary"
              density="comfortable"
              class="px-0 text-none"
              @click.stop="openEdit(item)"
            >
              {{ item.orderNumber }}
            </v-btn>
          </template>
          <template #[`item.orderedOn`]="{ item }">{{ formatDate(item.orderedOn) }}</template>
          <template #[`item.requiredOn`]="{ item }">{{ formatDate(item.requiredOn) }}</template>
          <template #[`item.qty`]="{ item }">{{ formatQty(item.qty) }}</template>
        </v-data-table>

        <v-divider class="my-4" />

        <div v-if="selected">
          <h4 class="text-subtitle-1 mb-2">{{ t('jobOrder.selectedOrder') }}</h4>
          <div class="text-body-2">{{ selected.orderNumber }}-{{ selected.jobNumber }} · {{ selected.customerName }}</div>
          <div class="text-body-2">{{ selected.orderTitle }}</div>
          <div class="text-body-2">{{ t('jobOrder.requiredQty', { date: formatDate(selected.requiredOn), qty: formatQty(selected.qty) }) }}</div>
        </div>
      </v-card-text>
    </v-card>

    <v-dialog v-model="formOpen" max-width="1080" scrollable>
      <OrderRecordDialog
        v-if="formJob"
        :order="formJob"
        :all-orders="rows"
        @saved="handleSaved"
        @deleted="handleDeleted"
        @open-order="handleOpenOrder"
        @cancel="formOpen = false"
      />
    </v-dialog>

    <v-snackbar v-model="saveSuccess" color="success" timeout="3000">
      {{ t('jobOrder.saved') }}
      <template #actions>
        <v-btn variant="text" @click="saveSuccess = false">{{ t('jobOrder.dismiss') }}</v-btn>
      </template>
    </v-snackbar>

    <v-snackbar v-model="deleteSuccess" color="success" timeout="3000">
      {{ t('jobOrder.deleted') }}
      <template #actions>
        <v-btn variant="text" @click="deleteSuccess = false">{{ t('jobOrder.dismiss') }}</v-btn>
      </template>
    </v-snackbar>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { getJobOrder, getJobOrders } from '@/services/jobOrders'
import OrderRecordDialog from '@/components/forms/OrderRecordDialog.vue'
import type { JobOrderRecord } from '@/types/api'

const rows = ref<JobOrderRecord[]>([])
const loading = ref(false)
const errorMessage = ref('')
const selected = ref<JobOrderRecord | null>(null)
const keyword = ref('')
const formOpen = ref(false)
const formJob = ref<JobOrderRecord | null>(null)
const saveSuccess = ref(false)
const deleteSuccess = ref(false)
const { t } = useI18n({ useScope: 'global' })
const { formatDate: formatDateByLocale, formatNumber } = useLocaleFormatters()

const headers = computed(() => [
  { title: t('jobOrder.headers.order'), key: 'orderNumber' },
  { title: t('jobOrder.headers.jobNumber'), key: 'jobNumber' },
  { title: t('jobOrder.headers.customer'), key: 'customerName' },
  { title: t('jobOrder.headers.title'), key: 'orderTitle' },
  { title: t('jobOrder.headers.ordered'), key: 'orderedOn' },
  { title: t('jobOrder.headers.required'), key: 'requiredOn' },
  { title: t('jobOrder.headers.qty'), key: 'qty' },
])

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
    selected.value = rows.value[0] ?? null
  } catch {
    errorMessage.value = t('jobOrder.loadFailed')
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

async function openEdit(record: JobOrderRecord) {
  try {
    const latest = await getJobOrder(record.orderId)
    selected.value = latest
    formJob.value = latest
    formOpen.value = true
  } catch {
    errorMessage.value = t('jobOrder.openEditFailed')
  }
}

async function handleSaved(orderId: string) {
  try {
    rows.value = await getJobOrders()
    selected.value = await getJobOrder(orderId)
    formJob.value = selected.value
    saveSuccess.value = true
    formOpen.value = false
  } catch {
    errorMessage.value = t('jobOrder.reloadAfterSaveFailed')
  }
}

async function handleDeleted() {
  try {
    rows.value = await getJobOrders()
    selected.value = rows.value[0] ?? null
    formJob.value = null
    formOpen.value = false
    deleteSuccess.value = true
  } catch {
    errorMessage.value = t('jobOrder.reloadAfterDeleteFailed')
  }
}

async function handleOpenOrder(orderId: string) {
  try {
    const latest = await getJobOrder(orderId)
    selected.value = latest
    formJob.value = latest
  } catch {
    errorMessage.value = t('jobOrder.openEditFailed')
  }
}

function formatDate(value: string) {
  return formatDateByLocale(value)
}

function formatQty(value: number) {
  return formatNumber(value, { maximumFractionDigits: 2 })
}
</script>