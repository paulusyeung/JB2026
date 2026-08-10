<template>
  <v-form ref="formRef" @submit.prevent="handleSubmit">
    <v-card class="legacy-draggable-card" :style="cardStyle">
      <v-card-title class="pa-6 pb-2 legacy-header">
        <div class="legacy-title-row">
          <div class="legacy-drag-handle" @pointerdown="startDrag">
            <h2 class="text-h5">{{ isNew ? t('jobForm.newTitle') : t('jobForm.editTitle') }}</h2>
            <p class="text-body-2 text-medium-emphasis mt-1 mb-0">
              {{ t('jobForm.subtitle') }}
            </p>
          </div>

          <v-btn
            icon="mdi-close"
            size="small"
            variant="text"
            class="legacy-close-btn"
            @click="emit('cancel')"
          />
        </div>

        <div class="legacy-toolbar mt-4">
          <v-btn variant="tonal" color="primary" :disabled="isNew" @click="handleAttachmentClick">
            {{ t('jobForm.actions.attachment') }}
          </v-btn>
          <v-btn variant="tonal" color="primary" :disabled="isNew" @click="handlePrintClick">
            {{ t('jobForm.actions.printOrder') }}
          </v-btn>
          <v-btn variant="tonal" color="primary" :disabled="isNew" @click="handleWorkflowClick">
            {{ t('jobForm.actions.workflow') }}
          </v-btn>
        </div>
      </v-card-title>

      <v-card-text class="pa-6 legacy-form-surface">
        <div class="legacy-top-grid">
          <div class="legacy-col">
            <div class="legacy-inline-pair">
              <v-text-field
                v-model="draft.orderNumber"
                :label="t('jobForm.fields.orderNumber')"
                variant="outlined"
                density="compact"
                hide-details="auto"
                :rules="isNew ? [required] : []"
                :readonly="!isNew"
                class="legacy-main-number"
              />
              <v-text-field
                v-model="draft.jobNumber"
                :label="t('jobForm.fields.jobNumber')"
                variant="outlined"
                density="compact"
                hide-details="auto"
                :rules="isNew ? [required] : []"
                :readonly="!isNew"
                class="legacy-sub-number"
              />
            </div>

            <v-text-field
              v-model="draft.customerName"
              :label="t('jobForm.fields.customerName')"
              variant="outlined"
              density="compact"
              hide-details="auto"
              :rules="[required]"
              :readonly="!isNew"
            />

            <v-text-field
              v-model="legacyBrand"
              :label="t('jobForm.fields.brand')"
              variant="outlined"
              density="compact"
              hide-details="auto"
              readonly
            />

            <v-text-field
              v-model="draft.productCode"
              :label="t('jobForm.fields.productCode')"
              variant="outlined"
              density="compact"
              hide-details="auto"
            />

            <v-text-field
              v-model="draft.customerRef"
              :label="t('jobForm.fields.purchaseOrder')"
              variant="outlined"
              density="compact"
              hide-details="auto"
            />

            <v-text-field
              v-model="draft.soNumber"
              :label="t('jobForm.fields.salesOrderNumber')"
              type="number"
              step="0.1"
              min="0"
              variant="outlined"
              density="compact"
              hide-details="auto"
              :rules="[maxOneDecimal]"
            />
          </div>

          <div class="legacy-col">
            <v-menu v-model="requiredOnPickerOpen" :close-on-content-click="false">
              <template #activator="{ props: menuProps }">
                <v-text-field
                  v-model="draft.requiredOn"
                  :label="t('jobForm.fields.requiredOn')"
                  variant="outlined"
                  density="compact"
                  hide-details="auto"
                  readonly
                  append-inner-icon="mdi-calendar"
                  v-bind="menuProps"
                  class="date-picker-input"
                  :rules="[required, requiredAfterOrdered]"
                />
              </template>
              <v-date-picker
                :model-value="draft.requiredOn ? new Date(draft.requiredOn + 'T12:00:00') : undefined"
                hide-header
                @update:model-value="onRequiredOnPicked"
              />
            </v-menu>

            <v-text-field
              v-model="legacyCompletedOn"
              :label="t('jobForm.fields.completedOn')"
              placeholder="yyyy-MM-dd"
              variant="outlined"
              density="compact"
              hide-details="auto"
            />

            <v-text-field
              v-model.number="draft.qty"
              :label="t('jobForm.fields.quantity')"
              type="number"
              min="0"
              step="1"
              variant="outlined"
              density="compact"
              hide-details="auto"
              :rules="[positiveNumber]"
            />

            <v-select
              v-model="draft.orderType"
              :label="t('jobForm.fields.type')"
              :items="orderTypeOptions"
              item-title="label"
              item-value="value"
              variant="outlined"
              density="compact"
              hide-details="auto"
            >
              <template #item="{ props: itemProps, item }">
                <v-list-item v-bind="itemProps" :title="item.raw.label">
                  <template #prepend>
                    <v-icon :color="item.raw.color">{{ item.raw.icon }}</v-icon>
                  </template>
                </v-list-item>
              </template>
              <template #selection="{ item }">
                <div class="d-flex align-center ga-2">
                  <v-icon :color="item.raw.color" size="small">{{ item.raw.icon }}</v-icon>
                  <span>{{ item.raw.label }}</span>
                </div>
              </template>
            </v-select>

            <v-text-field
              v-model="draft.productStyle"
              :label="t('jobForm.fields.quotationNumber')"
              variant="outlined"
              density="compact"
              hide-details="auto"
            />

            <v-text-field
              v-model="draft.originalSONumber"
              :label="t('jobForm.fields.originalSalesOrderNumber')"
              type="number"
              step="0.01"
              min="0"
              variant="outlined"
              density="compact"
              hide-details="auto"
              :rules="[maxTwoDecimals]"
            />
          </div>

          <div class="legacy-col">
            <v-menu v-model="orderedOnPickerOpen" :close-on-content-click="false">
              <template #activator="{ props: menuProps }">
                <v-text-field
                  v-model="draft.orderedOn"
                  :label="t('jobForm.fields.orderedOn')"
                  variant="outlined"
                  density="compact"
                  hide-details="auto"
                  readonly
                  append-inner-icon="mdi-calendar"
                  v-bind="menuProps"
                  class="date-picker-input"
                  :rules="[required]"
                />
              </template>
              <v-date-picker
                :model-value="draft.orderedOn ? new Date(draft.orderedOn + 'T12:00:00') : undefined"
                hide-header
                @update:model-value="onOrderedOnPicked"
              />
            </v-menu>

            <v-text-field
              v-model="legacyModifiedOn"
              :label="t('jobForm.fields.modifiedOn')"
              variant="outlined"
              density="compact"
              hide-details="auto"
              readonly
            />

            <v-text-field
              v-model="draft.outputRef"
              :label="t('jobForm.fields.outputReference')"
              variant="outlined"
              density="compact"
              hide-details="auto"
            />

            <v-text-field
              v-model="draft.invoiceRef"
              :label="t('jobForm.fields.invoiceNumber')"
              variant="outlined"
              density="compact"
              hide-details="auto"
            />

            <v-text-field
              v-model.number="draft.invoiceAmount"
              :label="t('jobForm.fields.invoiceAmount')"
              variant="outlined"
              density="compact"
              hide-details="auto"
            />
          </div>
        </div>

        <div class="legacy-lower-grid mt-4">
          <div>
            <div class="legacy-product-details-wrap">
              <div class="legacy-product-details-actions">
                <v-tooltip location="left">
                  <template #activator="{ props: tooltipProps }">
                    <v-btn
                      v-bind="tooltipProps"
                      icon="mdi-pencil-outline"
                      size="small"
                      variant="tonal"
                      color="primary"
                      :disabled="isNew"
                      @click="handleProductDetailsEdit"
                    />
                  </template>
                  <span>{{ t('jobForm.actions.editProductDetails') }}</span>
                </v-tooltip>
              </div>
              <div class="legacy-product-details-html" v-html="legacyProductDetails" />
            </div>

            <div class="legacy-attribute-grid mt-3">
              <div v-for="(attr, index) in workflowAttributeDefs" :key="attr.workflowName" class="legacy-attribute-row">
                <v-select
                  v-model="workflowAttributeValues[attr.workflowName]"
                  :label="attr.workflowName"
                  :items="attr.options"
                  variant="outlined"
                  density="compact"
                  hide-details
                />
                <span :class="['legacy-indicator', indicatorColor(index)]" />
              </div>
            </div>
          </div>

          <div class="legacy-right-column">
            <div class="legacy-remarks-wrap">
              <div class="legacy-remarks-actions">
                <v-tooltip location="left">
                  <template #activator="{ props: tooltipProps }">
                    <v-btn
                      v-bind="tooltipProps"
                      icon="mdi-pencil-outline"
                      size="small"
                      variant="tonal"
                      color="primary"
                      :disabled="isNew"
                      @click="handleRemarksEdit"
                    />
                  </template>
                  <span>{{ t('jobForm.actions.editRemarks') }}</span>
                </v-tooltip>
              </div>
              <div class="legacy-remarks-html" v-html="legacyRemarks" />
            </div>

            <div class="legacy-preview mt-3">
              <div class="legacy-preview-header">{{ t('jobForm.fields.preview') }}</div>
              <div class="legacy-preview-body">
                <img
                  v-if="previewImageUrl"
                  :src="previewImageUrl"
                  :alt="t('jobForm.fields.preview')"
                  class="legacy-preview-image"
                />
                <v-icon v-else size="34" color="grey-darken-1">mdi-file-document-outline</v-icon>
              </div>
            </div>
          </div>
        </div>

        <v-alert v-if="errorMessage" type="error" variant="tonal" class="mt-2">
          {{ errorMessage }}
        </v-alert>
      </v-card-text>

      <v-divider />

      <v-card-actions class="pa-4 d-flex ga-2 responsive-dialog-actions">
        <v-spacer />
        <v-btn variant="tonal" color="primary" :disabled="saving" @click="emit('cancel')">{{ t('jobForm.actions.cancel') }}</v-btn>
        <v-btn variant="tonal" color="primary" type="submit" :loading="saving" min-width="120">
          {{ isNew ? t('jobForm.actions.create') : t('jobForm.actions.saveChanges') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-form>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { VForm } from 'vuetify/components'
import { useOrderTypeOptions } from '@/composables/useOrderTypeOptions'
import { saveJob } from '@/services/jobs'
import { getJobOrder, getJobPreviewBlob, getOrderTypeWorkflowAttributes } from '@/services/jobOrders'
import type { JobDetail, JobOrderFormData, JobOrderRecord, OrderTypeWorkflowAttribute } from '@/types/api'

// ---------------------------------------------------------------------------
// Props / emits
// ---------------------------------------------------------------------------
const props = defineProps<{
  /** Pass null to open in create mode. Pass a JobDetail to open in edit mode. */
  job: JobDetail | null
}>()

const emit = defineEmits<{
  (e: 'saved'): void
  (e: 'cancel'): void
  (e: 'attachment', job: JobDetail): void
  (e: 'print-order', job: JobDetail): void
  (e: 'workflow', job: JobDetail): void
  (e: 'product-details-edit', job: JobDetail): void
  (e: 'remarks-edit', job: JobDetail): void
}>()

// ---------------------------------------------------------------------------
// Local state
// ---------------------------------------------------------------------------
const formRef = ref<InstanceType<typeof VForm> | null>(null)
const saving = ref(false)
const errorMessage = ref('')
const { t } = useI18n({ useScope: 'global' })
const { orderTypeOptions } = useOrderTypeOptions()
const legacyRecord = ref<JobOrderRecord | null>(null)
const legacyBrand = computed(() => draft.value.orderTitle ?? '')
const legacyCompletedOn = ref('')
const workflowAttributeDefs = ref<OrderTypeWorkflowAttribute[]>([])
const workflowAttributeValues = ref<Record<string, string>>({})
const previewImageUrl = ref<string | null>(null)
const dragOffset = ref({ x: 0, y: 0 })
const dragPointer = ref<{ id: number; startX: number; startY: number; originX: number; originY: number } | null>(null)

const isNew = computed(() => props.job === null || !props.job.orderId)
const cardStyle = computed(() => ({
  transform: `translate(${dragOffset.value.x}px, ${dragOffset.value.y}px)`,
}))

const draft = ref<JobOrderFormData>(buildDraft(props.job))

watch(
  () => props.job,
  async (job) => {
    draft.value = buildDraft(job)
    legacyRecord.value = null
    syncLegacyFields(null)
    errorMessage.value = ''
    clearPreviewImage()
    fetchWorkflowAttributes(draft.value.orderType)

    if (!job?.orderId) return

    try {
      legacyRecord.value = await getJobOrder(job.orderId)
      syncLegacyFields(legacyRecord.value)
      await loadPreviewImage(job)
    } catch {
      // Keep the form usable even if legacy detail endpoint is unavailable.
      legacyRecord.value = null
      await loadPreviewImage(job)
    }
  },
  { immediate: true },
)

onBeforeUnmount(() => {
  stopDrag()
  clearPreviewImage()
})

const legacyModifiedOn = computed(() => formatLegacyDate(legacyRecord.value?.modifiedOn ?? null))
const legacyProductDetails = computed(() => {
  if (legacyRecord.value?.productDetails?.trim()) return legacyRecord.value.productDetails
  if (legacyRecord.value?.productStyle?.trim()) return legacyRecord.value.productStyle
  if (props.job?.styleTitles?.length) return props.job.styleTitles.join('<br>')
  return draft.value.orderTitle
})

const legacyRemarks = computed(() => {
  if (draft.value.remarks?.trim()) return draft.value.remarks
  return '<em class="legacy-empty-hint">' + t('jobForm.fields.emptyRemarks') + '</em>'
})

// ---------------------------------------------------------------------------
// Validation rules
// ---------------------------------------------------------------------------
const required = (v: string | number) => (v !== '' && v !== null && v !== undefined) || t('jobForm.validation.required')

const positiveNumber = (v: number) => v >= 0 || t('jobForm.validation.nonNegative')

const maxOneDecimal = (v: string) => {
  if (!v) return true
  return /^\d+(\.\d)?$/.test(v) || t('jobForm.validation.maxOneDecimal')
}

const maxTwoDecimals = (v: string) => {
  if (!v) return true
  return /^\d+(\.\d{1,2})?$/.test(v) || t('jobForm.validation.maxTwoDecimals')
}

const requiredAfterOrdered = (v: string) => {
  if (!v || !draft.value.orderedOn) return true
  return v >= draft.value.orderedOn || t('jobForm.validation.requiredAfterOrdered')
}

const orderedOnPickerOpen = ref(false)
const requiredOnPickerOpen = ref(false)

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

// ---------------------------------------------------------------------------
// Actions
// ---------------------------------------------------------------------------
function buildDraft(job: JobDetail | null): JobOrderFormData {
  if (!job || !job.orderId) {
    const today = new Date().toISOString().slice(0, 10)
    const partial = job as JobDetail & { jobNumber?: string; orderType?: number }
    return {
      orderId: null,
      orderNumber: job?.orderNumber ?? '',
      jobNumber: partial?.jobNumber ?? '',
      orderTitle: job?.orderTitle ?? '',
      customerName: job?.customerName ?? '',
      customerRef: job?.customerRef ?? '',
      orderedBy: job?.orderedBy ?? '',
      orderedOn: job?.orderedOn?.slice(0, 10) ?? today,
      requiredOn: job?.requiredOn?.slice(0, 10) ?? today,
      qty: job?.qty ?? 1,
      status: 1,
      orderType: partial?.orderType ?? 0,
      paymentTerms: job?.paymentTerms ?? '',
      remarks: '',
      soNumber: '',
      originalSONumber: '',
      productStyle: '',
      productCode: '',
      outputRef: '',
      invoiceRef: '',
      invoiceAmount: undefined,
      productDetails: '',
      workflowAttributes: {},
    }
  }

  const parsedNumbers = parseCompositeOrderNumber(job.orderNumber)

  return {
    orderId: job.orderId,
    orderNumber: parsedNumbers.orderNumber,
    jobNumber: parsedNumbers.jobNumber || job.jobNumber || '',
    orderTitle: job.orderTitle,
    customerName: job.customerName,
    customerRef: job.customerRef,
    orderedBy: job.orderedBy,
    orderedOn: job.orderedOn?.slice(0, 10) ?? '',
    requiredOn: job.requiredOn?.slice(0, 10) ?? '',
    qty: job.qty,
    status: job.status,
    orderType: 0,
    paymentTerms: job.paymentTerms ?? '',
    remarks: job.remarks ?? '',
    soNumber: job.soNumber ?? '',
    originalSONumber: job.originalSONumber ?? '',
    productStyle: job.productStyle ?? '',
    productCode: job.productCode ?? '',
    outputRef: job.outputRef ?? '',
    invoiceRef: job.invoiceRef ?? '',
    invoiceAmount: job.invoiceAmount ?? undefined,
    productDetails: job.productDetails ?? '',
    workflowAttributes: job.workflowAttributes ?? {},
  }
}

function parseCompositeOrderNumber(orderNumber: string) {
  const trimmed = orderNumber.trim()
  const match = trimmed.match(/^(.*?)-(\d+)$/)

  if (!match) {
    return {
      orderNumber: trimmed,
      jobNumber: '',
    }
  }

  return {
    orderNumber: match[1] ?? trimmed,
    jobNumber: match[2] ?? '',
  }
}

function formatLegacyDate(value: string | null): string {
  if (!value) return ''
  // Treat '1900-01-01' as a legacy empty/sentinel date
  if (value.startsWith('1900-01-01')) return ''
  return value.slice(0, 10)
}

function formatAmount(value: number): string {
  return new Intl.NumberFormat(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value)
}

function syncLegacyFields(record: JobOrderRecord | null) {
  draft.value.productCode = record?.productCode ?? ''
  draft.value.outputRef = record?.outputRef ?? ''
  draft.value.invoiceRef = record?.invoiceRef ?? ''
  draft.value.invoiceAmount = record?.invoiceAmount ?? undefined
  legacyCompletedOn.value = formatLegacyDate(record?.completedOn ?? null)
  draft.value.productStyle = record?.productStyle ?? ''
  draft.value.productDetails = record?.productDetails ?? ''
  if (record) {
    draft.value.orderType = record.orderType
  }
}

async function handleSubmit() {
  const { valid } = await formRef.value!.validate()
  if (!valid) return

  draft.value.workflowAttributes = { ...workflowAttributeValues.value }

  saving.value = true
  errorMessage.value = ''

  try {
      await saveJob(draft.value)
      emit('saved')
    } catch (err: unknown) {
      if (import.meta.env.DEV) {
        console.error('Job save failed:', err)
      }

      const axiosErr = err as { isAxiosError?: boolean; response?: { status?: number; data?: { title?: string; detail?: string; errors?: Record<string, string[]> } } }

      if (axiosErr?.response) {
        const status = axiosErr.response.status
        const data = axiosErr.response.data

        // 400/422: Extract validation error details from ProblemDetails / ValidationProblemDetails
        if ((status === 400 || status === 422) && data?.errors) {
          const messages = Object.values(data.errors).flat().filter(Boolean)
          errorMessage.value = messages.length > 0
            ? messages.join('. ')
            : t('jobForm.saveFailed')
        } else if (data?.title || data?.detail) {
          errorMessage.value = data.title || data.detail || t('jobForm.saveFailed')
        } else if (status === 404) {
          errorMessage.value = t('jobForm.notFound')
        } else {
          errorMessage.value = t('jobForm.saveFailed')
        }
      } else {
        errorMessage.value = t('jobForm.saveFailed')
      }
    } finally {
      saving.value = false
    }
  }

const legacyIndicatorColors = [
  'legacy-indicator-blue',
  'legacy-indicator-green',
  'legacy-indicator-red',
  'legacy-indicator-orange',
  'legacy-indicator-purple',
  'legacy-indicator-teal',
]

function indicatorColor(index: number): string {
  return legacyIndicatorColors[index % legacyIndicatorColors.length]
}

async function fetchWorkflowAttributes(orderType: number) {
  workflowAttributeDefs.value = []
  workflowAttributeValues.value = {}
  try {
    workflowAttributeDefs.value = await getOrderTypeWorkflowAttributes(orderType)
    // Restore any saved values for the fetched definitions
    const saved = draft.value.workflowAttributes ?? {}
    for (const attr of workflowAttributeDefs.value) {
      if (saved[attr.workflowName] !== undefined) {
        workflowAttributeValues.value[attr.workflowName] = saved[attr.workflowName]
      }
    }
  } catch {
    console.warn('Failed to fetch workflow attributes for order type', orderType)
  }
}

watch(() => draft.value.orderType, (orderType) => {
  fetchWorkflowAttributes(orderType)
})

function handleAttachmentClick() {
  if (!props.job) return
  emit('attachment', props.job)
}

function handlePrintClick() {
  if (!props.job) return
  emit('print-order', props.job)
}

function handleWorkflowClick() {
  if (!props.job) return
  emit('workflow', props.job)
}

function handleProductDetailsEdit() {
  if (!props.job) return
  emit('product-details-edit', props.job)
}

function handleRemarksEdit() {
  if (!props.job) return
  emit('remarks-edit', props.job)
}

function clearPreviewImage() {
  if (previewImageUrl.value) {
    URL.revokeObjectURL(previewImageUrl.value)
  }
  previewImageUrl.value = null
}

function startDrag(event: PointerEvent) {
  if (event.button !== 0) {
    return
  }

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
  if (!dragPointer.value || event.pointerId !== dragPointer.value.id) {
    return
  }

  dragOffset.value = {
    x: dragPointer.value.originX + (event.clientX - dragPointer.value.startX),
    y: dragPointer.value.originY + (event.clientY - dragPointer.value.startY),
  }
}

function stopDrag(event?: PointerEvent) {
  if (event && dragPointer.value && event.pointerId !== dragPointer.value.id) {
    return
  }

  dragPointer.value = null
  window.removeEventListener('pointermove', handleDrag)
  window.removeEventListener('pointerup', stopDrag)
}

async function loadPreviewImage(job: JobDetail) {
  clearPreviewImage()

  const firstAttachment = job.attachments?.[0]
  if (!firstAttachment?.fileName) {
    return
  }

  const fileName = firstAttachment.fileName
  const normalizedFileName = fileName.toLowerCase()
  const isPdf = normalizedFileName.endsWith('.pdf')

  const candidates = isPdf
    ? [
        `${fileName}.jpg`,
        `${fileName.slice(0, -4)}.jpg`,
        fileName,
      ]
    : [fileName]

  const attachmentType =
    (firstAttachment as unknown as { attachmentType?: string }).attachmentType
    ?? (firstAttachment as unknown as { contentType?: string }).contentType

  for (const candidate of candidates) {
    try {
      const blob = await getJobPreviewBlob(job.orderId, candidate, attachmentType)
      previewImageUrl.value = URL.createObjectURL(blob)
      return
    } catch {
      // Try next candidate.
    }
  }

  previewImageUrl.value = null
}
</script>

<style scoped>
.legacy-draggable-card {
  transition: box-shadow 0.18s ease;
  will-change: transform;
}

.legacy-header {
  user-select: none;
}

.legacy-title-row {
  display: flex;
  gap: 12px;
  align-items: flex-start;
}

.legacy-drag-handle {
  flex: 1;
  min-width: 0;
  cursor: move;
  touch-action: none;
}

.legacy-close-btn {
  flex-shrink: 0;
}

.legacy-form-surface {
  --legacy-notes-height: 188px;
  color: rgba(var(--v-theme-on-surface), 0.95);
}

.legacy-form-surface :deep(.v-field) {
  background: rgb(var(--v-theme-surface));
}

.legacy-form-surface :deep(.v-input:has(input[readonly]) .v-field),
.legacy-form-surface :deep(.v-input:has(textarea[readonly]) .v-field) {
  background: rgba(var(--v-theme-on-surface), 0.06);
}

.legacy-form-surface :deep(.v-input:has(input[readonly]) .v-field__input),
.legacy-form-surface :deep(.v-input:has(textarea[readonly]) .v-field__input) {
  color: rgba(var(--v-theme-on-surface), 0.95);
}

.legacy-form-surface :deep(.v-input.date-picker-input:has(input[readonly]) .v-field) {
  background: transparent;
}

.legacy-toolbar {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.legacy-top-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
}

.legacy-col {
  display: grid;
  gap: 10px;
  align-content: start;
}

.legacy-inline-pair {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 86px;
  gap: 8px;
}

.legacy-main-number,
.legacy-sub-number {
  min-width: 0;
}

.legacy-lower-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.legacy-right-column {
  display: flex;
  flex-direction: column;
}

.legacy-notes-box {
  min-height: var(--legacy-notes-height);
}

:deep(.legacy-notes-box .v-input__control),
:deep(.legacy-notes-box .v-field),
:deep(.legacy-notes-box .v-field__input) {
  min-height: var(--legacy-notes-height);
  height: var(--legacy-notes-height);
}

:deep(.legacy-notes-box textarea) {
  height: 100%;
  overflow-y: auto;
}

.legacy-remarks-wrap {
  width: calc(100% - 26px);
  position: relative;
}

.legacy-remarks-html {
  min-height: var(--legacy-notes-height);
  max-height: var(--legacy-notes-height);
  overflow-y: auto;
  border: 1px solid rgba(var(--v-theme-on-surface), 0.28);
  border-radius: 4px;
  background: rgba(var(--v-theme-on-surface), 0.05);
  padding: 12px 14px;
  line-height: 1.35;
  white-space: normal;
}

.legacy-remarks-html :deep(table td:nth-child(2)),
.legacy-remarks-html :deep(table th:nth-child(2)) {
  text-align: right;
}

.legacy-remarks-actions {
  position: absolute;
  top: 12px;
  right: 12px;
  z-index: 1;
  display: flex;
  justify-content: flex-end;
}

.legacy-empty-hint {
  color: rgba(var(--v-theme-on-surface), 0.5);
  font-style: italic;
}

.legacy-product-details-wrap {
  width: calc(100% - 26px);
  position: relative;
}

.legacy-product-details-html {
  min-height: var(--legacy-notes-height);
  max-height: var(--legacy-notes-height);
  overflow-y: auto;
  border: 1px solid rgba(var(--v-theme-on-surface), 0.28);
  border-radius: 4px;
  background: rgba(var(--v-theme-on-surface), 0.05);
  padding: 12px 14px;
  line-height: 1.35;
  white-space: normal;
}

.legacy-product-details-actions {
  position: absolute;
  top: 12px;
  right: 12px;
  z-index: 1;
  display: flex;
  justify-content: flex-end;
}

.legacy-attribute-grid {
  display: grid;
  gap: 8px;
}

.legacy-attribute-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 18px;
  gap: 8px;
  align-items: center;
}

.legacy-indicator {
  width: 14px;
  height: 14px;
  border-radius: 999px;
  border: 1px solid rgba(var(--v-theme-on-surface), 0.4);
  display: inline-block;
}

.legacy-indicator-blue {
  background: #56a8e3;
}

.legacy-indicator-green {
  background: #61c06a;
}

.legacy-indicator-red {
  background: #e76464;
}

.legacy-indicator-orange {
  background: #f4a261;
}

.legacy-indicator-purple {
  background: #9b59b6;
}

.legacy-indicator-teal {
  background: #1abc9c;
}

.legacy-preview {
  border: 1px solid rgba(var(--v-theme-on-surface), 0.28);
  background: rgba(var(--v-theme-on-surface), 0.04);
  min-height: 182px;
}

.legacy-preview-header {
  padding: 6px 10px;
  font-size: 12px;
  color: rgba(var(--v-theme-on-surface), 0.6);
  border-bottom: 1px solid rgba(var(--v-theme-on-surface), 0.2);
}

.legacy-preview-body {
  min-height: 140px;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.legacy-preview-image {
  width: 100%;
  height: 140px;
  object-fit: contain;
}

.legacy-form-surface :deep(.v-field-label),
.legacy-form-surface :deep(.v-label) {
  color: rgba(var(--v-theme-on-surface), 0.55);
  opacity: 1;
}

.legacy-form-surface :deep(.v-field--focused .v-field-label) {
  color: rgba(var(--v-theme-on-surface), 0.9);
  opacity: 1;
}

@media (max-width: 960px) {
  .legacy-top-grid {
    grid-template-columns: 1fr;
  }

  .legacy-lower-grid {
    grid-template-columns: 1fr;
  }
}
</style>
