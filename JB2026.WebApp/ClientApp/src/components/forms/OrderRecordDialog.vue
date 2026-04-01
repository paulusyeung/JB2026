<template>
  <v-card class="order-record-dialog">
    <v-card-title class="d-flex align-center ga-3 pb-2">
      <div>
        <h2 class="text-h6 mb-1">{{ t('jobOrder.record.title') }}</h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          {{ mode === 'create' ? t('jobOrder.record.createSubtitle') : t('jobOrder.record.subtitle', { order: order.orderNumber }) }}
        </p>
      </div>
      <v-spacer />
      <v-btn variant="text" icon="mdi-close" @click="emit('cancel')" />
    </v-card-title>

    <v-card-text class="pt-2">
      <div class="d-flex flex-wrap ga-2 mb-4">
        <v-btn size="small" variant="tonal" prepend-icon="mdi-plus" color="primary" @click="resetDraft">
          {{ t('jobOrder.record.actions.addNew') }}
        </v-btn>
        <v-btn size="small" variant="tonal" prepend-icon="mdi-refresh" @click="refreshDraft">
          {{ t('jobOrder.record.actions.refresh') }}
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
          <v-text-field
            v-model="draft.jobNumber"
            :label="t('jobOrder.record.fields.jobNumber')"
            variant="outlined"
            density="compact"
            :readonly="mode === 'edit'"
          />
        </v-col>
        <v-col cols="12" md="4">
          <v-text-field
            v-model="draft.customerName"
            :label="t('jobOrder.record.fields.customerName')"
            variant="outlined"
            density="compact"
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="4">
          <v-text-field
            v-model="draft.customerRef"
            :label="t('jobOrder.record.fields.brand')"
            variant="outlined"
            density="compact"
          />
        </v-col>
        <v-col cols="12" md="4">
          <v-text-field
            :model-value="draft.orderedBy"
            :label="t('jobOrder.record.fields.salesRep')"
            variant="outlined"
            density="compact"
            readonly
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="4">
          <v-text-field
            v-model="draft.orderedOn"
            type="date"
            :label="t('jobOrder.record.fields.orderedOn')"
            variant="outlined"
            density="compact"
          />
        </v-col>
        <v-col cols="12" md="4">
          <v-text-field
            v-model="draft.requiredOn"
            type="date"
            :label="t('jobOrder.record.fields.requiredOn')"
            variant="outlined"
            density="compact"
          />
        </v-col>
        <v-col cols="12" md="4">
          <v-text-field
            :model-value="formatDate(mode === 'edit' ? order.modifiedOn : null)"
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
            :model-value="mode === 'edit' ? order.orderedBy : '-'"
            :label="t('jobOrder.record.fields.createdBy')"
            variant="outlined"
            density="compact"
            readonly
          />
        </v-col>
        <v-col cols="12" md="4">
          <v-text-field
            :model-value="mode === 'edit' ? order.modifiedBy ?? '-' : '-'"
            :label="t('jobOrder.record.fields.invoiceNo')"
            variant="outlined"
            density="compact"
            readonly
          />
        </v-col>
        <v-col cols="12" md="4">
          <v-text-field
            :model-value="mode === 'edit' ? formatDate(order.createdOn) : '-'"
            :label="t('jobOrder.record.fields.createdOn')"
            variant="outlined"
            density="compact"
            readonly
          />
        </v-col>
      </v-row>

      <v-tabs v-model="activeTab" density="compact" class="mt-2">
        <v-tab value="general">{{ t('jobOrder.record.tabs.general') }}</v-tab>
        <v-tab value="orders">{{ t('jobOrder.record.tabs.orders') }}</v-tab>
      </v-tabs>

      <v-window v-model="activeTab" class="mt-3">
        <v-window-item value="general">
          <v-row dense>
            <v-col cols="12" md="8">
              <v-text-field
                v-model="draft.orderTitle"
                :label="t('jobOrder.record.fields.orderTitle')"
                variant="outlined"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field
                v-model="draft.paymentTerms"
                :label="t('jobOrder.record.fields.paymentTerms')"
                variant="outlined"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field
                v-model.number="draft.status"
                type="number"
                :label="t('jobOrder.record.fields.status')"
                variant="outlined"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field
                v-model.number="draft.qty"
                type="number"
                min="0.01"
                step="0.01"
                :label="t('jobOrder.record.fields.qty')"
                variant="outlined"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12">
              <v-textarea
                v-model="draft.remarks"
                :label="t('jobOrder.record.fields.remarks')"
                variant="outlined"
                rows="4"
                auto-grow
                density="comfortable"
              />
            </v-col>
          </v-row>
        </v-window-item>

        <v-window-item value="orders">
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
              <v-icon :color="item.orderId === order.orderId ? 'primary' : statusColor(item.status)" size="16">
                {{ item.orderId === order.orderId ? 'mdi-circle-slice-8' : statusIcon(item.status) }}
              </v-icon>
            </template>
            <template #[`item.orderNumber`]="{ item }">
              <v-btn variant="text" density="comfortable" class="px-0 text-none" @click.stop="emit('open-order', item.orderId)">
                {{ item.orderNumber }}
              </v-btn>
            </template>
            <template #[`item.status`]="{ item }">
              <v-chip size="x-small" :color="statusColor(item.status)" variant="tonal">
                {{ item.status }}
              </v-chip>
            </template>
            <template #[`item.orderedOn`]="{ item }">{{ formatDate(item.orderedOn) }}</template>
            <template #[`item.requiredOn`]="{ item }">{{ formatDate(item.requiredOn) }}</template>
          </v-data-table>
        </v-window-item>
      </v-window>

      <v-alert v-if="errorMessage" type="error" variant="tonal" class="mt-3">
        {{ errorMessage }}
      </v-alert>
    </v-card-text>

    <v-divider />

    <v-card-actions class="pa-4 d-flex ga-2">
      <v-spacer />
      <v-btn variant="text" :disabled="saving" @click="emit('cancel')">{{ t('jobOrder.dismiss') }}</v-btn>
      <v-btn color="primary" :loading="saving" @click="handleSave">{{ t('jobOrder.record.actions.save') }}</v-btn>
    </v-card-actions>
  </v-card>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { createJobOrder, deleteJobOrder, updateJobOrder } from '@/services/jobOrders'
import type { JobOrderFormData, JobOrderRecord } from '@/types/api'

const props = defineProps<{
  order: JobOrderRecord
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
const activeTab = ref('general')
const mode = ref<'edit' | 'create'>('edit')

const draft = ref<JobOrderFormData>(buildDraft(props.order))

watch(
  () => props.order,
  (order) => {
    mode.value = 'edit'
    draft.value = buildDraft(order)
    errorMessage.value = ''
    activeTab.value = 'general'
  },
)

const relatedHeaders = computed(() => [
  { title: '', key: 'indicator', sortable: false, width: '28px' },
  { title: t('jobOrder.headers.order'), key: 'orderNumber' },
  { title: t('jobOrder.headers.customer'), key: 'customerName' },
  { title: t('jobOrder.headers.title'), key: 'orderTitle' },
  { title: t('jobOrder.record.fields.status'), key: 'status' },
  { title: t('jobOrder.headers.ordered'), key: 'orderedOn' },
  { title: t('jobOrder.headers.required'), key: 'requiredOn' },
])

const relatedOrders = computed(() => {
  return props.allOrders
    .filter((row) => row.customerName === props.order.customerName)
    .sort((a, b) => b.orderedOn.localeCompare(a.orderedOn))
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
  errorMessage.value = ''
  activeTab.value = 'general'
}

function refreshDraft() {
  if (mode.value === 'create') {
    draft.value = buildCreateDraft()
  } else {
    draft.value = buildDraft(props.order)
  }
  errorMessage.value = ''
}

function buildCreateDraft(): JobOrderFormData {
  const today = new Date().toISOString().slice(0, 10)

  return {
    orderId: null,
    orderNumber: '',
    jobNumber: '',
    orderTitle: '',
    customerName: props.order.customerName,
    customerRef: props.order.customerRef,
    orderedBy: props.order.orderedBy,
    orderedOn: today,
    requiredOn: today,
    qty: 1,
    status: 0,
    paymentTerms: props.order.paymentTerms || 'Net 30',
    remarks: '',
  }
}

function validateDraft() {
  if (!draft.value.orderNumber.trim()) return t('jobOrder.record.validation.orderNumber')
  if (!draft.value.jobNumber.trim()) return t('jobOrder.record.validation.jobNumber')
  if (!draft.value.customerName.trim()) return t('jobOrder.record.validation.customerName')
  if (!draft.value.orderTitle.trim()) return t('jobOrder.record.validation.orderTitle')
  if (!draft.value.paymentTerms.trim()) return t('jobOrder.record.validation.paymentTerms')
  if (!draft.value.requiredOn) return t('jobOrder.record.validation.requiredOn')
  if (!draft.value.orderedOn) return t('jobOrder.record.validation.orderedOn')
  if (draft.value.requiredOn < draft.value.orderedOn) return t('jobOrder.record.validation.requiredAfterOrdered')
  if (draft.value.qty <= 0) return t('jobOrder.record.validation.qty')
  return ''
}

async function handleSave() {
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
        paymentTerms: draft.value.paymentTerms,
        remarks: draft.value.remarks,
        status: draft.value.status,
      })

      emit('saved', created.orderId)
    } else {
      const updated = await updateJobOrder(props.order.orderId, {
        customerName: draft.value.customerName,
        customerRef: draft.value.customerRef,
        orderTitle: draft.value.orderTitle,
        requiredOn: draft.value.requiredOn,
        qty: draft.value.qty,
        paymentTerms: draft.value.paymentTerms,
        remarks: draft.value.remarks,
        status: draft.value.status,
      })

      emit('saved', updated.orderId)
    }
  } catch {
    errorMessage.value = t('jobOrder.record.saveFailed')
  } finally {
    saving.value = false
  }
}

async function handleDelete() {
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

function formatDate(value: string | null) {
  if (!value) return '-'

  const normalized = value.slice(0, 10)
  if (!normalized) return '-'
  return normalized
}
</script>

<style scoped>
.order-record-dialog :deep(.v-data-table) {
  border: 1px solid rgba(var(--v-theme-on-surface), 0.12);
  border-radius: 6px;
}
</style>
