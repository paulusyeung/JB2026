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
            :placeholder="mode === 'create' ? t('jobOrder.record.fields.orderNumberAuto') : ''"
            variant="outlined"
            density="compact"
            readonly
          />
        </v-col>
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
            :model-value="formatDate(mode === 'edit' ? orderModifiedOn : null)"
            :label="t('jobOrder.record.fields.modifiedOn')"
            variant="outlined"
            density="compact"
            readonly
          />
        </v-col>

        <v-col cols="12" md="4">
          <v-select
            v-model="draft.customerName"
            :items="customerOptions"
            :label="t('jobOrder.record.fields.customerName')"
            variant="outlined"
            density="compact"
            :readonly="mode === 'edit'"
            @update:model-value="handleCustomerChanged"
          />
        </v-col>
        <v-col cols="12" md="4">
          <v-select
            v-model="draft.orderedBy"
            :items="orderedByOptions"
            :label="t('jobOrder.record.fields.salesRep')"
            variant="outlined"
            density="compact"
            :readonly="mode === 'edit'"
          />
        </v-col>
        <v-col cols="12" md="4">
          <v-text-field
            v-model="draft.invoiceRef"
            :label="t('jobOrder.record.fields.invoiceNo')"
            variant="outlined"
            density="compact"
          />
        </v-col>

        <v-col cols="12" md="4">
          <v-text-field
            v-model="draft.orderTitle"
            :label="t('jobOrder.record.fields.brand')"
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
            v-model.number="draft.invoiceAmount"
            type="number"
            step="0.01"
            min="0"
            :label="t('jobOrder.record.fields.invoiceAmount')"
            variant="outlined"
            density="compact"
          />
        </v-col>
      </v-row>

      <!-- GROUP 1: Jobs Info -->
      <!-- elevation="1" adds a subtle shadow. rounded makes corners smooth. -->
      <v-sheet 
        v-if="mode !== 'create'"
        color="grey-lighten-4" 
        elevation="1" 
        rounded="lg" 
        class="pa-4 mb-4 d-flex flex-column gap-2"
      >
        <div class="d-flex flex-wrap ga-2 mt-2 mb-3">
          <v-btn size="small" variant="tonal" prepend-icon="mdi-plus" color="primary" @click="resetDraft">
            {{ t('jobOrder.record.actions.addNew') }}
          </v-btn>
          <v-btn v-if="false" size="small" variant="tonal" prepend-icon="mdi-refresh" @click="refreshDraft">
            {{ t('jobOrder.record.actions.refresh') }}
          </v-btn>
          <v-btn size="small" variant="outlined" prepend-icon="mdi-delete" :loading="deleting" :disabled="selectedIds.size === 0" @click="handleDeleteSelected">
            {{ t('jobOrder.record.actions.delete') }}
          </v-btn>
          <v-btn v-if="false" size="small" variant="outlined" prepend-icon="mdi-archive-arrow-down" @click="handleImportJobs">
            {{ t('jobOrder.record.actions.importJobs') }}
          </v-btn>
        </div>

        <v-data-table
          :headers="relatedHeaders"
          :items="relatedOrders"
          item-value="orderId"
          density="compact"
          :items-per-page="10"
          class="order-record-grid text-no-wrap"
          @click:row="onRelatedRowClick"
        >
          <template #[`header.select`]>
            <v-checkbox
              :model-value="allSelected"
              density="compact"
              hide-details
              @click.stop="toggleSelectAll"
            />
          </template>
          <template #[`item.select`]="{ item }">
            <v-checkbox
              :model-value="selectedIds.has(item.orderId)"
              density="compact"
              hide-details
              @click.stop="toggleSelect(item.orderId)"
            />
          </template>
          <template #[`header.attachments`]>
            <v-icon size="small">mdi-paperclip</v-icon>
          </template>
          <template #[`item.indicator`]="{ item }">
            <v-tooltip :text="statusLabel(item.status)" location="top">
              <template v-slot:activator="{ props }">
                <v-icon
                  v-bind="props"
                  :color="item.orderId === orderId ? 'primary' : statusColor(item.status)"
                  size="16"
                >
                  {{ item.orderId === orderId ? 'mdi-flag-checkered' : statusIcon(item.status) }}
                </v-icon>
              </template>
            </v-tooltip>
          </template>
          <template #[`item.orderNumber`]="{ item }">
            <v-btn
              variant="text"
              color="primary"
              density="comfortable"
              class="px-0 text-none"
              @click.stop="emit('open-job-form', item.orderId)"
            >
              {{ compositeOrderNumber(item) }}
            </v-btn>
          </template>
          <template #[`item.orderedOn`]="{ item }">{{ formatDate(item.orderedOn) }}</template>
          <template #[`item.attachments`]="{ item }">
            <v-icon v-if="item.attachmentProductCount && item.attachmentProductCount > 0" color="success" size="16">mdi-circle</v-icon>
          </template>
          <template #[`header.customerAttachments`]>
            <v-icon size="small">mdi-paperclip</v-icon>
          </template>
          <template #[`item.customerAttachments`]="{ item }">
            <v-icon v-if="item.attachmentCustomerCount && item.attachmentCustomerCount > 0" color="error" size="16">mdi-circle-outline</v-icon>
          </template>
          <template #[`item.requiredOn`]="{ item }">{{ formatDate(item.requiredOn) }}</template>
          <template #[`item.modifiedOn`]="{ item }">{{ formatDateTime(item.modifiedOn) }}</template>
          <template #[`item.modifiedBy`]="{ item }">{{ formatUser(item.modifiedBy) }}</template>
        </v-data-table>

        <v-alert v-if="errorMessage" type="error" variant="tonal" class="mt-3">
          {{ errorMessage }}
        </v-alert>

        <v-card-actions class="pa-4 d-flex ga-2 responsive-dialog-actions">
          <v-spacer />
          <v-btn variant="text" :disabled="saving" @click="emit('cancel')">{{ t('common.cancel') }}</v-btn>
          <v-btn color="primary" :loading="saving" @click="handleSave">{{ t('jobOrder.record.actions.save') }}</v-btn>
          <v-btn variant="tonal" :loading="saving" @click="handleSave(true)">{{ t('jobOrder.record.actions.saveClose') }}</v-btn>
        </v-card-actions>

      </v-sheet>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSessionStore } from '@/stores/session'
import { getAdminUsers } from '@/services/admin'
import { createJobOrder, deleteJobOrder, updateJobOrder } from '@/services/jobOrders'
import { getSettings, updateSettings } from '@/services/settings'
import type { JobOrderFormData, JobOrderRecord } from '@/types/api'

const props = defineProps<{
  order?: JobOrderRecord
  allOrders: JobOrderRecord[]
}>()

const emit = defineEmits<{
  (e: 'saved', orderId: string): void
  (e: 'cancel'): void
  (e: 'open-order', orderId: string): void
  (e: 'open-job-form', orderId: string): void
  (e: 'deleted'): void
}>()

const { t } = useI18n({ useScope: 'global' })
const saving = ref(false)
const deleting = ref(false)
const errorMessage = ref('')
const mode = ref<'edit' | 'create'>(props.order ? 'edit' : 'create')
const orderedByDynamicOptions = ref<string[]>([])
const userMap = ref<Record<string, string>>({})
const nextOrderNumber = ref('')
const selectedIds = ref(new Set<string>())
const session = useSessionStore()

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
  await Promise.all([
    loadOrderedByOptions(),
    loadNextOrderNumber(),
  ])
})

const orderModifiedOn = computed(() => props.order?.modifiedOn ?? null)
const orderId = computed(() => props.order?.orderId ?? null)

function compositeOrderNumber(record: JobOrderRecord): string {
  return record.jobNumber ? `${record.orderNumber}-${record.jobNumber}` : record.orderNumber
}

function toggleSelect(orderId: string) {
  const s = new Set(selectedIds.value)
  if (s.has(orderId)) s.delete(orderId)
  else s.add(orderId)
  selectedIds.value = s
}

function toggleSelectAll() {
  if (selectedIds.value.size === relatedOrders.value.length) {
    selectedIds.value = new Set()
  } else {
    selectedIds.value = new Set(relatedOrders.value.map((r) => r.orderId))
  }
}

const allSelected = computed(() =>
  relatedOrders.value.length > 0 && selectedIds.value.size === relatedOrders.value.length,
)

const relatedHeaders = computed(() => [
  { title: '', key: 'select', sortable: false, width: '48px' },
  { title: t('jobOrder.record.fields.orderNumber'), key: 'orderNumber', width: '150px' },
  { title: '', key: 'indicator', sortable: false, width: '36px' },
  { title: t('jobOrder.record.fields.orderedOn'), key: 'orderedOn', width: '110px' },
  { title: t('jobOrder.headers.customer'), key: 'customerName', width: '160px' },
  { title: t('jobOrder.record.fields.brand'), key: 'orderTitle', width: '200px' },
  { title: t('jobOrder.orderList.headers.productCode'), key: 'productCode', width: '120px' },
  { title: '', key: 'attachments', sortable: false, width: '40px' },
  { title: 'Purchase Order', key: 'customerRef', width: '140px' },
  { title: '', key: 'customerAttachments', sortable: false, width: '40px' },
  { title: 'Sales Rep.', key: 'orderedBy', width: '120px' },
  { title: 'Output Ref.', key: 'outputRef', width: '120px' },
  { title: 'Required On', key: 'requiredOn', width: '110px' },
  { title: 'Invoice No.', key: 'invoiceRef', width: '120px' },
  { title: 'Modified On', key: 'modifiedOn', width: '140px' },
  { title: 'Modified By', key: 'modifiedBy', width: '120px' },
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
  
  // 1. Add all names from the server list (non-Guest users)
  for (const value of orderedByDynamicOptions.value) {
    if (value) values.add(value)
  }
  
  // 2. Include names from historical orders to ensure previously used sales reps are available
  for (const order of props.allOrders) {
    if (order.orderedBy) values.add(order.orderedBy)
  }
  
  // 3. Ensure the currently selected draft value is included
  if (draft.value.orderedBy) values.add(draft.value.orderedBy)
  
  return [...values].sort((a, b) => a.localeCompare(b))
})

const relatedOrders = computed(() => {
  if (!props.order) return []
  return props.allOrders
    .filter((row) => row.orderNumber === props.order!.orderNumber && Number(row.jobNumber) !== 0)
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
    orderType: order.orderType,
    paymentTerms: order.paymentTerms ?? '',
    invoiceRef: order.invoiceRef ?? '',
    invoiceAmount: order.invoiceAmount,
    remarks: order.remarks ?? '',
    workflowAttributes: {},
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
    const staff = users.filter((u) => u.role !== 'Guest')
    orderedByDynamicOptions.value = staff
      .map((user) => user.displayName || user.username)
      .filter((value): value is string => Boolean(value && value.trim()))

    const map: Record<string, string> = {}
    for (const u of users) {
      map[u.userId] = u.displayName || u.username
    }
    userMap.value = map
  } catch {
    // Keep fallback options from existing order rows when admin lookup fails.
    orderedByDynamicOptions.value = []
  }
}

async function loadNextOrderNumber() {
  try {
    const settings = await getSettings()
    // nextOrderNumber.value = settings.nextOrderNumber
    // Convert the number to a string and pad it to a length of 6 with '0'
    nextOrderNumber.value = String(settings.nextOrderNumber).padStart(6, '0');

  } catch {
    // Non-critical; save will fail validation if nextOrderNumber is unavailable.
  }
}

function buildCreateDraft(): JobOrderFormData {
  const today = new Date().toISOString().slice(0, 10)

  return {
    orderId: null,
    orderNumber: '',
    jobNumber: '0',
    orderTitle: '',
    customerName: props.order?.customerName ?? '',
    customerRef: props.order?.customerRef ?? '',
    orderedBy: session.profile?.displayName ?? props.order?.orderedBy ?? '',
    orderedOn: today,
    requiredOn: today,
    qty: 1,
    status: 1,
    orderType: 0,
    paymentTerms: props.order?.paymentTerms || 'Net 30',
    invoiceRef: '',
    invoiceAmount: 0,
    remarks: '',
    workflowAttributes: {},
  }
}

function validateDraft() {
  if (mode.value !== 'create' && !draft.value.orderNumber.trim()) return t('jobOrder.record.validation.orderNumber')
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
      if (!nextOrderNumber.value) {
        errorMessage.value = t('jobOrder.record.saveFailed')
        return
      }

      draft.value.orderNumber = nextOrderNumber.value

      const created = await createJobOrder({
        orderNumber: draft.value.orderNumber,
        jobNumber: draft.value.jobNumber,
        customerName: draft.value.customerName,
        customerRef: draft.value.customerRef,
        orderTitle: draft.value.orderTitle,
        orderedBy: draft.value.orderedBy,
        orderedOn: draft.value.orderedOn,
        requiredOn: draft.value.requiredOn,
        qty: draft.value.qty,
        paymentTerms: draft.value.paymentTerms || 'Net 30',
        remarks: draft.value.remarks,
        status: draft.value.status,
        invoiceRef: draft.value.invoiceRef || '',
        invoiceAmount: draft.value.invoiceAmount,
      })

      const incremented = String(Number(nextOrderNumber.value) + 1)
      nextOrderNumber.value = incremented

      try {
        const current = await getSettings()
        await updateSettings({ ...current, nextOrderNumber: incremented })
      } catch {
        // Non-critical: local nextOrderNumber is already incremented
      }

      emit('saved', created.orderId)
      if (closeAfterSave) {
        emit('cancel')
      }
    } else {
      const updated = await updateJobOrder(props.order!.orderId, {
        customerName: draft.value.customerName,
        customerRef: draft.value.customerRef,
        orderTitle: draft.value.orderTitle,
        orderedOn: draft.value.orderedOn,
        requiredOn: draft.value.requiredOn,
        qty: draft.value.qty,
        paymentTerms: draft.value.paymentTerms || 'Net 30',
        remarks: draft.value.remarks,
        status: draft.value.status,
        invoiceRef: draft.value.invoiceRef || '',
        invoiceAmount: draft.value.invoiceAmount,
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
  // const confirmed = window.confirm(t('jobOrder.record.deleteConfirm', { order: props.order.orderNumber }))
  const confirmed = window.confirm(
    `Are you sure you want to delete Order #${props.order.orderNumber} (ID: ${props.order.orderId})?`
  )
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

async function handleDeleteSelected() {
  if (selectedIds.value.size === 0) return

  // Get the specific items selected from the table
  const selectedItems = relatedOrders.value.filter((r) => selectedIds.value.has(r.orderId))
  
  // Create a list string that includes both Order Number and ID
  // Using \n for line breaks so it's readable in the confirm box
  const itemsList = selectedItems.map(item => `Order #${item.orderNumber} (ID: ${item.orderId})`).join('\n')

  const confirmed = window.confirm(
    `Are you sure you want to delete these ${selectedIds.value.size} item(s)?\n\n${itemsList}`
  )

  if (!confirmed) return

  deleting.value = true
  errorMessage.value = ''

  try {
    const items = relatedOrders.value.filter((r) => selectedIds.value.has(r.orderId))
    for (const item of items) {
      await deleteJobOrder(item.orderId)
    }
    selectedIds.value = new Set()
    emit('saved', props.order!.orderId)
  } catch (err: any) {
    const data = err?.response?.data
    errorMessage.value = data?.detail || data?.title || data?.message || err?.message || t('jobOrder.record.deleteFailed')
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
  if (status >= 3) return 'mdi-flag-check'
  if (status === 2) return 'mdi-flag-outline'
  if (status === 1) return 'mdi-flag-variant-outline'
  return 'mdi-flag-minus-outline'
}

function statusLabel(status: number): string {
  if (status >= 3) return t('jobOrder.status.completed')
  if (status === 2) return t('jobOrder.status.paused')
  if (status === 1) return t('jobOrder.status.inProgress')
  return t('jobOrder.status.notStarted')
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

function formatDateTime(value: string | null | undefined) {
  if (!value) return '-'
  return value.slice(0, 16).replace('T', ' ')
}

function formatUser(userId: string | null | undefined): string {
  if (!userId) return '-'
  return userMap.value[userId] || userId
}

</script>

<style scoped>
.order-record-dialog :deep(.v-data-table) {
  border: 1px solid rgba(var(--v-theme-on-surface), 0.12);
  border-radius: 6px;
}

.order-record-grid :deep(thead th) {
  white-space: nowrap;
  padding-left: 8px;
  padding-right: 8px;
}

.order-record-grid :deep(tbody td) {
  padding-left: 8px;
  padding-right: 8px;
}

.order-record-dialog :deep(.v-input:has(input[readonly]) .v-field),
.order-record-dialog :deep(.v-input:has(textarea[readonly]) .v-field) {
  background: #e8e8e8;
}

:deep(.v-theme--dark) .order-record-dialog .v-input:has(input[readonly]) .v-field,
:deep(.v-theme--dark) .order-record-dialog .v-input:has(textarea[readonly]) .v-field {
  background: #2f3841;
}
</style>
