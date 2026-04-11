<template>
  <v-card class="order-record-dialog">
    <v-card-title class="d-flex align-center ga-3 pb-2">
      <div>
        <h2 class="text-h6 mb-1">{{ t('jobOrder.record.title') }}</h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          {{ mode === 'create' ? t('jobOrder.record.createSubtitle') : t('jobOrder.record.subtitle', { order: order?.orderNumber }) }}
        </p>
      </div>
      <v-spacer />
      <v-btn variant="text" icon="mdi-close" @click="emit('cancel')" />
    </v-card-title>

    <v-card-text class="pt-2">
      <div class="d-flex flex-wrap ga-2 mb-3">
        <v-btn size="small" color="primary" prepend-icon="mdi-content-save" :loading="saving" @click="handleSave()">
          {{ t('jobOrder.record.actions.save') }}
        </v-btn>
        <v-btn size="small" variant="tonal" prepend-icon="mdi-content-save-check" :loading="saving" @click="handleSave(true)">
          {{ t('jobOrder.record.actions.saveClose') }}
        </v-btn>
        <v-btn size="small" variant="outlined" prepend-icon="mdi-delete" :loading="deleting" :disabled="mode === 'create'" @click="handleDelete">
          {{ t('jobOrder.record.actions.delete') }}
        </v-btn>
      </div>

      <v-row dense>
        <v-col cols="12" md="4">
          <v-text-field
            v-model="draft.orderNumber"
            :label="t('jobOrder.record.fields.orderNumber')"
            variant="outlined"
            density="compact"
            :readonly="mode === 'edit'"
          />
        </v-col>
        <v-col cols="12" md="4">
          <v-select
            v-model="draft.customerName"
            :items="customerOptions"
            :label="t('jobOrder.record.fields.customerName')"
            variant="outlined"
            density="compact"
            :disabled="mode === 'edit'"
            @update:model-value="handleCustomerChanged"
          />
        </v-col>
        <v-col cols="12" md="2">
          <v-text-field
            v-model="draft.orderedOn"
            type="date"
            :label="t('jobOrder.record.fields.orderedOn')"
            variant="outlined"
            density="compact"
            :readonly="mode === 'edit'"
          />
        </v-col>
        <v-col cols="12" md="2">
          <v-text-field
            :model-value="formatDate(mode === 'edit' ? orderModifiedOn : null)"
            :label="t('jobOrder.record.fields.modifiedOn')"
            variant="outlined"
            density="compact"
            readonly
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="4">
          <v-text-field
            v-model="draft.orderTitle"
            :label="t('jobOrder.record.fields.brand')"
            variant="outlined"
            density="compact"
          />
        </v-col>
        <v-col cols="12" md="2">
          <v-select
            v-model="draft.orderedBy"
            :items="orderedByOptions"
            :label="t('jobOrder.record.fields.salesRep')"
            variant="outlined"
            density="compact"
            :disabled="mode === 'edit'"
          />
        </v-col>
        <v-col cols="12" md="2">
          <v-text-field
            v-model="draft.requiredOn"
            type="date"
            :label="t('jobOrder.record.fields.requiredOn')"
            variant="outlined"
            density="compact"
            :readonly="mode === 'edit'"
          />
        </v-col>
        <v-col cols="12" md="2">
          <v-select
            v-model="draft.paymentTerms"
            :items="paymentTermsOptions"
            :label="t('jobOrder.record.fields.paymentTerms')"
            variant="outlined"
            density="compact"
            clearable
            :disabled="mode === 'edit'"
          />
        </v-col>
        <v-col cols="12" md="2">
          <v-text-field
            :model-value="order?.invoiceRef || '-'"
            :label="t('jobOrder.record.fields.invoiceNo')"
            variant="outlined"
            density="compact"
            readonly
          />
        </v-col>
        <v-col cols="12" md="2">
          <v-text-field
            :model-value="formatAmount(order?.invoiceAmount)"
            :label="t('jobOrder.record.fields.invoiceAmount')"
            variant="outlined"
            density="compact"
            readonly
          />
        </v-col>
      </v-row>

      <div class="d-flex flex-wrap ga-2 mt-2 mb-3">
        <v-btn size="small" variant="tonal" prepend-icon="mdi-plus" color="primary" @click="resetDraft">
          {{ t('jobOrder.record.actions.addNew') }}
        </v-btn>
        <v-btn size="small" variant="tonal" prepend-icon="mdi-refresh" @click="refreshDraft">
          {{ t('jobOrder.record.actions.refresh') }}
        </v-btn>
        <v-btn size="small" variant="outlined" prepend-icon="mdi-delete" :loading="deleting" :disabled="mode === 'create'" @click="handleDelete">
          {{ t('jobOrder.record.actions.delete') }}
        </v-btn>
        <v-btn size="small" variant="outlined" prepend-icon="mdi-archive-arrow-down" @click="handleImportJobs">
          {{ t('jobOrder.record.actions.importJobs') }}
        </v-btn>
      </div>

      <v-data-table
        :headers="relatedHeaders"
        :items="relatedOrders"
        item-value="orderId"
        density="compact"
        :items-per-page="10"
        class="order-record-grid"
        @click:row="onRelatedRowClick"
      >
        <template #[`item.indicator`]="{ item }">
          <v-icon :color="item.orderId === orderId ? 'primary' : statusColor(item.status)" size="16">
            {{ item.orderId === orderId ? 'mdi-circle-slice-8' : statusIcon(item.status) }}
          </v-icon>
        </template>
        <template #[`item.orderNumber`]="{ item }">
          <v-btn variant="text" density="comfortable" class="px-0 text-none" @click.stop="emit('open-order', item.orderId)">
            {{ item.orderNumber }}-{{ item.jobNumber }}
          </v-btn>
        </template>
        <template #[`item.orderedOn`]="{ item }">{{ formatDate(item.orderedOn) }}</template>
      </v-data-table>

      <v-alert v-if="errorMessage" type="error" variant="tonal" class="mt-3">
        {{ errorMessage }}
      </v-alert>
    </v-card-text>

    <v-divider />

    <v-card-actions class="pa-4 d-flex ga-2">
      <v-spacer />
      <v-btn variant="text" :disabled="saving" @click="emit('cancel')">{{ t('jobOrder.dismiss') }}</v-btn>
      <v-btn color="primary" :loading="saving" @click="handleSave">{{ t('jobOrder.record.actions.save') }}</v-btn>
      <v-btn variant="tonal" :loading="saving" @click="handleSave(true)">{{ t('jobOrder.record.actions.saveClose') }}</v-btn>
    </v-card-actions>
  </v-card>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { getAdminUsers } from '@/services/admin'
import { createJobOrder, deleteJobOrder, updateJobOrder } from '@/services/jobOrders'
import type { JobOrderFormData, JobOrderRecord } from '@/types/api'

const props = defineProps<{
  order?: JobOrderRecord
  allOrders: JobOrderRecord[]
}>()

const emit = defineEmits<{
  (e: 'saved', orderId: string): void
  (e: 'cancel'): void
  (e: 'open-order', orderId: string): void
  (e: 'deleted'): void
}>()

const { t } = useI18n({ useScope: 'global' })
const saving = ref(false)
const deleting = ref(false)
const errorMessage = ref('')
const mode = ref<'edit' | 'create'>(props.order ? 'edit' : 'create')
const orderedByDynamicOptions = ref<string[]>([])

const draft = ref<JobOrderFormData>(props.order ? buildDraft(props.order) : buildCreateDraft())

watch(
  () => props.order,
  (order) => {
    if (!order) return
    mode.value = 'edit'
    draft.value = buildDraft(order)
    errorMessage.value = ''
  },
)

onMounted(async () => {
  await loadOrderedByOptions()
})

const orderModifiedOn = computed(() => props.order?.modifiedOn ?? null)
const orderId = computed(() => props.order?.orderId ?? null)

const relatedHeaders = computed(() => [
  { title: '', key: 'indicator', sortable: false, width: '28px' },
  { title: t('jobOrder.headers.order'), key: 'orderNumber' },
  { title: t('jobOrder.headers.jobNumber'), key: 'jobNumber', width: '80px' },
  { title: t('jobOrder.record.fields.orderedOn'), key: 'orderedOn', width: '120px' },
  { title: t('jobOrder.headers.customer'), key: 'customerName' },
  { title: t('jobOrder.record.fields.brand'), key: 'orderTitle' },
  { title: t('jobOrder.orderList.headers.productCode'), key: 'productCode' },
  { title: t('jobOrder.orderList.headers.customerRef'), key: 'customerRef' },
])

const customerOptions = computed(() => {
  const values = new Set<string>()
  for (const row of props.allOrders) {
    if (row.customerName) values.add(row.customerName)
  }
  if (draft.value.customerName) values.add(draft.value.customerName)
  return [...values].sort((a, b) => a.localeCompare(b))
})

const customerProfiles = computed(() => {
  const profiles = new Map<string, { customerRef: string, paymentTerms: string, orderedBy: string }>()

  const sorted = [...props.allOrders].sort((a, b) => {
    const left = a.orderedOn || ''
    const right = b.orderedOn || ''
    return right.localeCompare(left)
  })

  for (const row of sorted) {
    if (!row.customerName || profiles.has(row.customerName)) {
      continue
    }

    profiles.set(row.customerName, {
      customerRef: row.customerRef || '',
      paymentTerms: row.paymentTerms || '',
      orderedBy: row.orderedBy || '',
    })
  }

  return profiles
})

const orderedByOptions = computed(() => {
  const values = new Set<string>()
  for (const value of orderedByDynamicOptions.value) {
    if (value) values.add(value)
  }
  for (const row of props.allOrders) {
    if (row.orderedBy) {
      values.add(row.orderedBy)
    }
  }
  if (draft.value.orderedBy) values.add(draft.value.orderedBy)
  return [...values].sort((a, b) => a.localeCompare(b))
})

const paymentTermsOptions = computed(() => {
  const values = new Set<string>()
  for (const row of props.allOrders) {
    if (row.paymentTerms) {
      values.add(row.paymentTerms)
    }
  }
  values.add('Net 7')
  values.add('Net 14')
  values.add('Net 30')
  values.add('Net 60')
  values.add('COD')
  values.add('Prepaid')
  if (draft.value.paymentTerms) {
    values.add(draft.value.paymentTerms)
  }

  return [...values]
})

const relatedOrders = computed(() => {
  if (!props.order) return []
  return props.allOrders
    .filter((row) => row.orderNumber === props.order!.orderNumber)
    .sort((a, b) => {
      const left = Number.parseInt(a.jobNumber, 10)
      const right = Number.parseInt(b.jobNumber, 10)
      return (Number.isFinite(left) ? left : 0) - (Number.isFinite(right) ? right : 0)
    })
})

function buildDraft(order: JobOrderRecord): JobOrderFormData {
  return {
    orderId: order.orderId,
    orderNumber: order.orderNumber,
    jobNumber: order.jobNumber,
    orderTitle: order.orderTitle,
    customerName: order.customerName,
    customerRef: order.customerRef,
    orderedBy: order.orderedBy,
    orderedOn: order.orderedOn?.slice(0, 10) ?? '',
    requiredOn: order.requiredOn?.slice(0, 10) ?? '',
    qty: order.qty,
    status: order.status,
    paymentTerms: order.paymentTerms ?? '',
    remarks: order.remarks ?? '',
  }
}

function resetDraft() {
  mode.value = 'create'
  draft.value = buildCreateDraft()
  handleCustomerChanged(draft.value.customerName)
  errorMessage.value = ''
}

function refreshDraft() {
  if (mode.value === 'create') {
    draft.value = buildCreateDraft()
    handleCustomerChanged(draft.value.customerName)
  } else if (props.order) {
    draft.value = buildDraft(props.order)
  }
  errorMessage.value = ''
}

function handleCustomerChanged(customerName: string | null) {
  if (mode.value === 'edit') {
    return
  }

  if (!customerName) {
    return
  }

  const profile = customerProfiles.value.get(customerName)
  if (!profile) {
    return
  }

  draft.value.customerRef = profile.customerRef

  if (!draft.value.paymentTerms) {
    draft.value.paymentTerms = profile.paymentTerms || 'Net 30'
  }

  if (!draft.value.orderedBy && profile.orderedBy) {
    draft.value.orderedBy = profile.orderedBy
  }
}

async function loadOrderedByOptions() {
  try {
    const users = await getAdminUsers()
    orderedByDynamicOptions.value = users
      .map((user) => user.displayName || user.username)
      .filter((value): value is string => Boolean(value && value.trim()))
  } catch {
    // Keep fallback options from existing order rows when admin lookup fails.
    orderedByDynamicOptions.value = []
  }
}

function buildCreateDraft(): JobOrderFormData {
  const today = new Date().toISOString().slice(0, 10)

  return {
    orderId: null,
    orderNumber: '',
    jobNumber: '',
    orderTitle: '',
    customerName: props.order?.customerName ?? '',
    customerRef: props.order?.customerRef ?? '',
    orderedBy: props.order?.orderedBy ?? '',
    orderedOn: today,
    requiredOn: today,
    qty: 1,
    status: 0,
    paymentTerms: props.order?.paymentTerms || 'Net 30',
    remarks: '',
  }
}

function validateDraft() {
  if (!draft.value.orderNumber.trim()) return t('jobOrder.record.validation.orderNumber')
  if (!draft.value.jobNumber.trim()) return t('jobOrder.record.validation.jobNumber')
  if (!draft.value.customerName.trim()) return t('jobOrder.record.validation.customerName')
  if (!draft.value.orderTitle.trim()) return t('jobOrder.record.validation.orderTitle')
  if (!draft.value.requiredOn) return t('jobOrder.record.validation.requiredOn')
  if (!draft.value.orderedOn) return t('jobOrder.record.validation.orderedOn')
  if (draft.value.requiredOn < draft.value.orderedOn) return t('jobOrder.record.validation.requiredAfterOrdered')
  if (draft.value.qty <= 0) return t('jobOrder.record.validation.qty')
  return ''
}

async function handleSave(closeAfterSave = false) {
  const validationError = validateDraft()
  if (validationError) {
    errorMessage.value = validationError
    return
  }

  saving.value = true
  errorMessage.value = ''

  try {
    if (mode.value === 'create') {
      const created = await createJobOrder({
        orderNumber: draft.value.orderNumber,
        jobNumber: draft.value.jobNumber,
        customerName: draft.value.customerName,
        customerRef: draft.value.customerRef,
        orderTitle: draft.value.orderTitle,
        orderedOn: draft.value.orderedOn,
        requiredOn: draft.value.requiredOn,
        qty: draft.value.qty,
        paymentTerms: draft.value.paymentTerms || 'Net 30',
        remarks: draft.value.remarks,
        status: draft.value.status,
      })

      emit('saved', created.orderId)
      if (closeAfterSave) {
        emit('cancel')
      }
    } else {
      const updated = await updateJobOrder(props.order!.orderId, {
        customerName: draft.value.customerName,
        customerRef: draft.value.customerRef,
        orderTitle: draft.value.orderTitle,
        requiredOn: draft.value.requiredOn,
        qty: draft.value.qty,
        paymentTerms: draft.value.paymentTerms || 'Net 30',
        remarks: draft.value.remarks,
        status: draft.value.status,
      })

      emit('saved', updated.orderId)
      if (closeAfterSave) {
        emit('cancel')
      }
    }
  } catch {
    errorMessage.value = t('jobOrder.record.saveFailed')
  } finally {
    saving.value = false
  }
}

async function handleDelete() {
  if (!props.order) return
  const confirmed = window.confirm(t('jobOrder.record.deleteConfirm', { order: props.order.orderNumber }))
  if (!confirmed) return

  deleting.value = true
  errorMessage.value = ''

  try {
    await deleteJobOrder(props.order.orderId)
    emit('deleted')
  } catch {
    errorMessage.value = t('jobOrder.record.deleteFailed')
  } finally {
    deleting.value = false
  }
}

function onRelatedRowClick(_event: Event, payload: { item: JobOrderRecord }) {
  emit('open-order', payload.item.orderId)
}

function handleImportJobs() {
  errorMessage.value = t('jobOrder.record.importJobsUnavailable')
}

function statusIcon(status: number) {
  if (status >= 3) return 'mdi-check-circle-outline'
  if (status === 2) return 'mdi-pause-circle-outline'
  if (status === 1) return 'mdi-progress-clock'
  return 'mdi-circle-outline'
}

function statusColor(status: number) {
  if (status >= 3) return 'success'
  if (status === 2) return 'warning'
  if (status === 1) return 'info'
  return 'secondary'
}

function formatDate(value: string | null | undefined) {
  if (!value) return '-'

  const normalized = value.slice(0, 10)
  if (!normalized) return '-'
  return normalized
}

function formatAmount(value: number | null | undefined) {
  if (!value) return ''
  return new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value)
}
</script>

<style scoped>
.order-record-dialog :deep(.v-data-table) {
  border: 1px solid rgba(var(--v-theme-on-surface), 0.12);
  border-radius: 6px;
}

.order-record-grid :deep(thead th) {
  white-space: nowrap;
}
</style>
