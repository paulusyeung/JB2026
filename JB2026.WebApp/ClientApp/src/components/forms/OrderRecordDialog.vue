<template>
  <v-card class="order-record-dialog" :style="cardStyle">
    <v-card-title class="pb-2">
      <div class="record-title-row">
        <div class="drag-handle" @pointerdown="startDrag">
          <h2 class="text-h6 mb-1">{{ t('jobOrder.record.title') }}</h2>
          <p class="text-body-2 text-medium-emphasis mb-0">
            {{ mode === 'create' ? t('jobOrder.record.createSubtitle') : t('jobOrder.record.subtitle', { order: order?.orderNumber }) }}
          </p>
        </div>
        <v-btn icon="mdi-close" size="small" variant="tonal" @click="emit('cancel')" />
      </div>
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
          <v-menu v-model="orderedOnPickerOpen" :close-on-content-click="false">
            <template #activator="{ props: menuProps }">
              <v-text-field
                :model-value="draft.orderedOn ? globalFormat.format(draft.orderedOn) : ''"
                :label="t('jobOrder.record.fields.orderedOn')"
                variant="outlined"
                density="compact"
                readonly
                append-inner-icon="mdi-calendar"
                v-bind="menuProps"
                class="date-picker-input"
              />
            </template>
            <v-date-picker
              :model-value="draft.orderedOn ? new Date(draft.orderedOn + 'T12:00:00') : undefined"
              hide-header
              @update:model-value="onOrderedOnPicked"
            />
          </v-menu>
        </v-col>
        <v-col cols="12" md="4">
          <v-text-field
            :model-value="globalFormat.format(mode === 'edit' ? orderModifiedOn : null)"
            :label="t('jobOrder.record.fields.modifiedOn')"
            variant="outlined"
            density="compact"
            readonly
          />
        </v-col>

        <v-col cols="12" md="4">
          <v-autocomplete
            v-model="draft.customerName"
            :items="customerOptions"
            item-title="title"
            item-value="value"
            :label="t('jobOrder.record.fields.customerName')"
            variant="outlined"
            density="compact"
            :readonly="mode === 'edit'"
            clearable
            hide-no-data
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
            :label="t('jobOrder.record.fields.orderTitle')"
            variant="outlined"
            density="compact"
          />
        </v-col>
        <v-col cols="12" md="4">
          <v-menu v-model="requiredOnPickerOpen" :close-on-content-click="false">
            <template #activator="{ props: menuProps }">
              <v-text-field
                :model-value="draft.requiredOn ? globalFormat.format(draft.requiredOn) : ''"
                :label="t('jobOrder.record.fields.requiredOn')"
                variant="outlined"
                density="compact"
                readonly
                append-inner-icon="mdi-calendar"
                v-bind="menuProps"
                class="date-picker-input"
              />
            </template>
            <v-date-picker
              :model-value="draft.requiredOn ? new Date(draft.requiredOn + 'T12:00:00') : undefined"
              hide-header
              @update:model-value="onRequiredOnPicked"
            />
          </v-menu>
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
        rounded="lg" 
        border="sm opacity-25"
        class="pa-4 mb-4 d-flex flex-column gap-2"
      >
        <div class="d-flex flex-wrap ga-2 mt-2 mb-3">
          <v-btn size="small" variant="tonal" prepend-icon="mdi-plus" color="primary" @click="handleAddNewJob">
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
          <template #[`item.orderedOn`]="{ item }">{{ globalFormat.format(item.orderedOn) }}</template>
          <template #[`item.attachments`]="{ item }">
            <v-icon v-if="item.attachmentProductCount && item.attachmentProductCount > 0" color="success" size="16">mdi-paperclip</v-icon>
          </template>
          <template #[`header.customerAttachments`]>
            <v-icon size="16">mdi-paperclip</v-icon>
          </template>
          <template #[`item.customerAttachments`]="{ item }">
            <v-icon v-if="item.attachmentCustomerCount && item.attachmentCustomerCount > 0" color="error" size="16">mdi-paperclip</v-icon>
          </template>
          <template #[`item.requiredOn`]="{ item }">{{ globalFormat.format(item.requiredOn) }}</template>
          <template #[`item.modifiedOn`]="{ item }">{{ globalFormat.format(item.modifiedOn, DATE_FORMATS.SHORT_DATETIME) }}</template>
          <template #[`item.modifiedBy`]="{ item }">{{ formatUser(item.modifiedBy) }}</template>
        </v-data-table>

        <v-alert v-if="errorMessage" type="error" variant="tonal" class="mt-3">
          {{ errorMessage }}
        </v-alert>

        <!-- 2026-06-27 paulsu: 冇需要，暫時用 v-if 隱藏 -->
        <v-card-actions v-if="false" class="pa-4 d-flex ga-2 responsive-dialog-actions">
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
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSessionStore } from '@/stores/session'
import { getAdminCustomers, getAdminUsers } from '@/services/admin'
import { createJobOrder, deleteJobOrder, updateJobOrder } from '@/services/jobOrders'
import { deleteJobAttachments, getJobDetail } from '@/services/jobs'
import { getSettings, updateSettings } from '@/services/settings'
import type { JobOrderFormData, JobOrderRecord } from '@/types/api'
import { statusIcon, statusColor } from '@/composables/useJobStatus'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { DATE_FORMATS } from '@/utils/dateFormatter'

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
  (e: 'add-new-job', orderContext: { orderId: string; orderNumber: string; customerName: string; orderedBy: string; orderTitle: string; orderedOn: string; requiredOn: string; orderType: number; customerRef: string; jobCount: number }): void
}>()

const { t } = useI18n({ useScope: 'global' })
const saving = ref(false)
const deleting = ref(false)
const errorMessage = ref('')
const mode = ref<'edit' | 'create'>(props.order ? 'edit' : 'create')
const orderedByDynamicOptions = ref<string[]>([])
const adminCustomerNames = ref<{ name: string; code: string }[]>([])
const userMap = ref<Record<string, string>>({})
const nextOrderNumber = ref('')
const selectedIds = ref(new Set<string>())
const session = useSessionStore()
const globalFormat = useGlobalDateFormatter()
const orderedOnPickerOpen = ref(false)
const requiredOnPickerOpen = ref(false)
const dragOffset = ref({ x: 0, y: 0 })
const dragPointer = ref<{ id: number; startX: number; startY: number; originX: number; originY: number } | null>(null)
const cardStyle = computed(() => ({
  transform: `translate(${dragOffset.value.x}px, ${dragOffset.value.y}px)`,
}))

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
    loadAdminCustomers(),
  ])
})

onBeforeUnmount(() => {
  stopDrag()
})

function startDrag(event: PointerEvent) {
  if (event.button !== 0) return

  dragPointer.value = {
    id: event.pointerId,
    startX: event.clientX,
    startY: event.clientY,
    originX: dragOffset.value.x,
    originY: dragOffset.value.y,
  }

  window.addEventListener('pointermove', handleDrag)
  window.addEventListener('pointerup', stopDrag)
}

function handleDrag(event: PointerEvent) {
  if (!dragPointer.value || event.pointerId !== dragPointer.value.id) return

  dragOffset.value = {
    x: dragPointer.value.originX + (event.clientX - dragPointer.value.startX),
    y: dragPointer.value.originY + (event.clientY - dragPointer.value.startY),
  }
}

function stopDrag(event?: PointerEvent) {
  if (event && dragPointer.value && event.pointerId !== dragPointer.value.id) return

  dragPointer.value = null
  window.removeEventListener('pointermove', handleDrag)
  window.removeEventListener('pointerup', stopDrag)
}

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
  { title: t('jobOrder.record.fields.jobNumber'), key: 'orderNumber', width: '150px' },
  { title: '', key: 'indicator', sortable: false, width: '36px' },
  { title: t('jobOrder.record.fields.orderedOn'), key: 'orderedOn', width: '110px' },
  { title: t('jobOrder.headers.customer'), key: 'customerName', width: '160px' },
  { title: t('jobOrder.record.fields.orderTitle'), key: 'orderTitle', width: '200px' },
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
  const seen = new Set<string>()
  const items: { title: string; value: string }[] = []

  for (const c of adminCustomerNames.value) {
    if (!c.name || seen.has(c.name)) continue
    seen.add(c.name)
    items.push({ title: c.name, value: c.name })
  }

  for (const row of props.allOrders) {
    if (!row.customerName || seen.has(row.customerName)) continue
    seen.add(row.customerName)
    items.push({ title: row.customerName, value: row.customerName })
  }

  if (draft.value.customerName && !seen.has(draft.value.customerName)) {
    items.push({ title: draft.value.customerName, value: draft.value.customerName })
  }

  return items.sort((a, b) => a.title.localeCompare(b.title))
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

function refreshDraft() {
  if (mode.value === 'create') {
    draft.value = buildCreateDraft()
    handleCustomerChanged(draft.value.customerName)
  } else if (props.order) {
    draft.value = buildDraft(props.order)
  }
  errorMessage.value = ''
}

function handleAddNewJob() {
  if (!props.order) return
  emit('add-new-job', {
    orderId: props.order.orderId,
    orderNumber: props.order.orderNumber,
    customerName: props.order.customerName,
    orderedBy: props.order.orderedBy ?? session.profile?.displayName ?? '',
    orderTitle: props.order.orderTitle,
    orderedOn: props.order.orderedOn,
    requiredOn: props.order.requiredOn,
    orderType: props.order.orderType,
    customerRef: props.order.customerRef ?? '',
    jobCount: relatedOrders.value.length,
  })
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

async function loadAdminCustomers() {
  try {
    const customers = await getAdminCustomers()
    adminCustomerNames.value = customers
      .filter((c) => c.customerName?.trim())
      .map((c) => ({ name: c.customerName.trim(), code: c.customerCode?.trim() ?? '' }))
  } catch {
    // Admin customer list is optional; fall back to order-based names only.
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

function toIsoDate(date: Date): string {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

function onOrderedOnPicked(date: Date | null) {
  if (date) {
    draft.value.orderedOn = toIsoDate(date)
  }
  orderedOnPickerOpen.value = false
}

function onRequiredOnPicked(date: Date | null) {
  if (date) {
    draft.value.requiredOn = toIsoDate(date)
  }
  requiredOnPickerOpen.value = false
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
        orderNumber: draft.value.orderNumber,
        customerName: draft.value.customerName,
        customerRef: draft.value.customerRef,
        orderTitle: draft.value.orderTitle,
        orderedOn: draft.value.orderedOn,
        requiredOn: draft.value.requiredOn,
        qty: draft.value.qty,
        paymentTerms: draft.value.paymentTerms || 'Net 30',
        remarks: draft.value.remarks,
        status: draft.value.status,
        orderType: draft.value.orderType,
        jobNumber: draft.value.jobNumber,
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
  const items = relatedOrders.value.filter((r) => selectedIds.value.has(r.orderId))

  // Create a list string that includes both Order Number and ID
  // Using \n for line breaks so it's readable in the confirm box
  const itemsList = items.map(item => `Order #${item.orderNumber} (ID: ${item.orderId})`).join('\n')

  const remainingCount = relatedOrders.value.length - items.length

  // When the selected items are the only remaining jobs for the order, deleting them
  // would also remove the order itself (the [Order Number]-1 and [Order Number]-0 records
  // share the same data). Instead of deleting, reset the first selected record to the
  // order record (jobNumber 0) with job-specific fields cleared, then delete the rest.
  const isLastJobScenario = remainingCount === 0

  const confirmed = window.confirm(
    isLastJobScenario
      ? `This is the last job for Order #${props.order!.orderNumber}. Deleting it would remove the entire order.\n\nInstead, it will be reset to the order record (${props.order!.orderNumber}-0) with job-specific fields cleared. Continue?`
      : `Are you sure you want to delete these ${selectedIds.value.size} item(s)?\n\n${itemsList}`,
  )

  if (!confirmed) return

  deleting.value = true
  errorMessage.value = ''

  try {
    if (isLastJobScenario) {
      const resetItem = items[0]
      if (!resetItem) return
      await resetJobToOrder(resetItem)
      for (const item of items.slice(1)) {
        await deleteJobOrder(item.orderId)
      }
    } else {
      for (const item of items) {
        await deleteJobOrder(item.orderId)
      }
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

// Reset a job record to the order record (jobNumber 0) by keeping the core order
// fields and clearing the job-specific values so the order survives without jobs.
async function resetJobToOrder(item: JobOrderRecord) {
  // Clear any job/product attachments.
  try {
    const detail = await getJobDetail(item.orderId)
    const attachmentIds = (detail.attachments ?? []).map((a) => a.attachmentId)
    if (attachmentIds.length > 0) {
      await deleteJobAttachments(item.orderId, attachmentIds)
    }
  } catch {
    // Attachment cleanup is best-effort; continue resetting the record regardless.
  }

  await updateJobOrder(item.orderId, {
    orderNumber: item.orderNumber,
    customerName: item.customerName,
    customerRef: '',
    orderTitle: item.orderTitle,
    orderedOn: item.orderedOn?.slice(0, 10) ?? '',
    requiredOn: item.requiredOn?.slice(0, 10) ?? '',
    qty: 1,
    paymentTerms: item.paymentTerms || 'Net 30',
    remarks: '',
    status: item.status,
    orderType: 0,
    jobNumber: '0',
    invoiceRef: '',
    invoiceAmount: 0,
    soNumber: '',
    originalSONumber: '',
    outputRef: '',
    productDetails: '',
    productCode: '',
    productStyle: '',
  })
}

function handleImportJobs() {
  errorMessage.value = t('jobOrder.record.importJobsUnavailable')
}

function statusLabel(status: number): string {
  if (status >= 3) return t('jobOrder.status.completed')
  if (status === 2) return t('jobOrder.status.paused')
  if (status === 1) return t('jobOrder.status.inProgress')
  return t('jobOrder.status.notStarted')
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
  background: rgba(var(--v-theme-on-surface), 0.06);
}

.order-record-dialog :deep(.v-input.date-picker-input:has(input[readonly]) .v-field) {
  background: transparent;
}

.order-record-dialog {
  transition: box-shadow 0.18s ease;
  will-change: transform;
}

.record-title-row {
  display: flex;
  gap: 12px;
  align-items: flex-start;
}

.drag-handle {
  flex: 1;
  min-width: 0;
  cursor: move;
  touch-action: none;
  user-select: none;
}
</style>
