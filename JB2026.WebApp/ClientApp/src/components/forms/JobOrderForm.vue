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
              v-model="legacyProductCode"
              :label="t('jobForm.fields.productCode')"
              variant="outlined"
              density="compact"
              hide-details="auto"
              readonly
            />

            <v-text-field
              v-model="draft.customerRef"
              :label="t('jobForm.fields.purchaseOrder')"
              variant="outlined"
              density="compact"
              hide-details="auto"
            />
          </div>

          <div class="legacy-col">
            <v-text-field
              v-model="draft.requiredOn"
              :label="t('jobForm.fields.requiredOn')"
              placeholder="yyyy-MM-dd"
              variant="outlined"
              density="compact"
              hide-details="auto"
              :rules="[required, validIsoDate, requiredAfterOrdered]"
            />

            <v-text-field
              v-model="legacyCompletedOn"
              :label="t('jobForm.fields.completedOn')"
              variant="outlined"
              density="compact"
              hide-details="auto"
              readonly
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

            <v-text-field
              v-model="legacyType"
              :label="t('jobForm.fields.type')"
              variant="outlined"
              density="compact"
              hide-details="auto"
              readonly
            />

            <v-text-field
              v-model="legacyQuotationNumber"
              :label="t('jobForm.fields.quotationNumber')"
              variant="outlined"
              density="compact"
              hide-details="auto"
              readonly
            />
          </div>

          <div class="legacy-col">
            <v-text-field
              v-model="draft.orderedOn"
              :label="t('jobForm.fields.orderedOn')"
              placeholder="yyyy-MM-dd"
              variant="outlined"
              density="compact"
              hide-details="auto"
              :rules="[required, validIsoDate]"
            />

            <v-text-field
              v-model="legacyModifiedOn"
              :label="t('jobForm.fields.modifiedOn')"
              variant="outlined"
              density="compact"
              hide-details="auto"
              readonly
            />

            <v-text-field
              v-model="legacyOutputRef"
              :label="t('jobForm.fields.outputReference')"
              variant="outlined"
              density="compact"
              hide-details="auto"
              readonly
            />

            <v-text-field
              v-model="legacyInvoiceNo"
              :label="t('jobForm.fields.invoiceNumber')"
              variant="outlined"
              density="compact"
              hide-details="auto"
              readonly
            />

            <v-text-field
              v-model="legacyInvoiceAmount"
              :label="t('jobForm.fields.invoiceAmount')"
              variant="outlined"
              density="compact"
              hide-details="auto"
              readonly
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
              <div class="legacy-attribute-row">
                <v-select
                  v-model="legacyPrintingPaper"
                  :label="t('jobForm.fields.printingPaper')"
                  :items="legacyAttributeOptions"
                  variant="outlined"
                  density="compact"
                  hide-details
                />
                <span class="legacy-indicator legacy-indicator-blue" />
              </div>
              <div class="legacy-attribute-row">
                <v-select
                  v-model="legacyFinishingOutput"
                  :label="t('jobForm.fields.finishingOutput')"
                  :items="legacyAttributeOptions"
                  variant="outlined"
                  density="compact"
                  hide-details
                />
                <span class="legacy-indicator legacy-indicator-green" />
              </div>
              <div class="legacy-attribute-row">
                <v-select
                  v-model="legacyPackagingRequirement"
                  :label="t('jobForm.fields.packagingRequirement')"
                  :items="legacyAttributeOptions"
                  variant="outlined"
                  density="compact"
                  hide-details
                />
                <span class="legacy-indicator legacy-indicator-red" />
              </div>
            </div>
          </div>

          <div class="legacy-right-column">
            <v-textarea
              v-model="draft.remarks"
              :label="t('jobForm.fields.remarks')"
              variant="outlined"
              rows="5"
              density="compact"
              class="legacy-notes-box"
            />

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
        <v-btn variant="text" :disabled="saving" @click="emit('cancel')">{{ t('jobForm.actions.cancel') }}</v-btn>
        <v-btn color="primary" type="submit" :loading="saving" min-width="120">
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
import { saveJob } from '@/services/jobs'
import { getJobOrder, getJobPreviewBlob } from '@/services/jobOrders'
import type { JobDetail, JobOrderFormData, JobOrderRecord } from '@/types/api'

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
}>()

// ---------------------------------------------------------------------------
// Local state
// ---------------------------------------------------------------------------
const formRef = ref<InstanceType<typeof VForm> | null>(null)
const saving = ref(false)
const errorMessage = ref('')
const { t } = useI18n({ useScope: 'global' })
const legacyRecord = ref<JobOrderRecord | null>(null)
const legacyBrand = computed(() => draft.value.orderTitle ?? '')
const legacyPrintingPaper = ref('')
const legacyFinishingOutput = ref('')
const legacyPackagingRequirement = ref('')
const previewImageUrl = ref<string | null>(null)
const dragOffset = ref({ x: 0, y: 0 })
const dragPointer = ref<{ id: number; startX: number; startY: number; originX: number; originY: number } | null>(null)

const isNew = computed(() => props.job === null)
const cardStyle = computed(() => ({
  transform: `translate(${dragOffset.value.x}px, ${dragOffset.value.y}px)`,
}))

const draft = ref<JobOrderFormData>(buildDraft(props.job))

watch(
  () => props.job,
  async (job) => {
    draft.value = buildDraft(job)
    legacyRecord.value = null
    errorMessage.value = ''
    clearPreviewImage()

    if (!job?.orderId) return

    try {
      legacyRecord.value = await getJobOrder(job.orderId)
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

// ---------------------------------------------------------------------------
// Static option lists (Phase 6: these will be loaded from /api/v2/lookups in a future sprint)
// ---------------------------------------------------------------------------
const statusOptions = computed(() => [
  { value: 0, label: t('jobForm.statuses.draft') },
  { value: 1, label: t('jobForm.statuses.inProgress') },
  { value: 2, label: t('jobForm.statuses.onHold') },
  { value: 3, label: t('jobForm.statuses.completed') },
  { value: 4, label: t('jobForm.statuses.cancelled') },
])

const paymentTermsOptions = computed(() => [
  t('jobForm.paymentTerms.net7'),
  t('jobForm.paymentTerms.net14'),
  t('jobForm.paymentTerms.net30'),
  t('jobForm.paymentTerms.net60'),
  t('jobForm.paymentTerms.cod'),
  t('jobForm.paymentTerms.prepaid'),
])

const legacyAttributeOptions = computed(() => ['', ...paymentTermsOptions.value])

const legacyProductCode = computed(() => legacyRecord.value?.productCode ?? '')
const legacyOutputRef = computed(() => legacyRecord.value?.outputRef ?? '')
const legacyInvoiceNo = computed(() => legacyRecord.value?.invoiceRef ?? '')
const legacyInvoiceAmount = computed(() => {
  const value = legacyRecord.value?.invoiceAmount
  return typeof value === 'number' ? formatAmount(value) : ''
})
const legacyCompletedOn = computed(() => formatLegacyDate(legacyRecord.value?.completedOn ?? null))
const legacyModifiedOn = computed(() => formatLegacyDate(legacyRecord.value?.modifiedOn ?? null))
const legacyQuotationNumber = computed(() => '')
const legacyType = computed(() => {
  const match = statusOptions.value.find((item) => item.value === draft.value.status)
  return match?.label ?? ''
})
const legacyProductDetails = computed(() => {
  if (legacyRecord.value?.productDetails?.trim()) return legacyRecord.value.productDetails
  if (legacyRecord.value?.productStyle?.trim()) return legacyRecord.value.productStyle
  if (props.job?.styleTitles?.length) return props.job.styleTitles.join('<br>')
  return draft.value.orderTitle
})

// ---------------------------------------------------------------------------
// Validation rules
// ---------------------------------------------------------------------------
const required = (v: string | number) => (v !== '' && v !== null && v !== undefined) || t('jobForm.validation.required')

const positiveNumber = (v: number) => v >= 0 || t('jobForm.validation.nonNegative')

const validIsoDate = (v: string) => {
  const normalized = (v ?? '').trim()
  if (!/^\d{4}-\d{2}-\d{2}$/.test(normalized)) {
    return t('jobForm.validation.dateFormat')
  }

  const parsed = new Date(`${normalized}T00:00:00`)
  return Number.isNaN(parsed.getTime()) ? t('jobForm.validation.dateFormat') : true
}

const requiredAfterOrdered = (v: string) => {
  if (!v || !draft.value.orderedOn) return true
  return v >= draft.value.orderedOn || t('jobForm.validation.requiredAfterOrdered')
}

// ---------------------------------------------------------------------------
// Actions
// ---------------------------------------------------------------------------
function buildDraft(job: JobDetail | null): JobOrderFormData {
  if (!job) {
    const today = new Date().toISOString().slice(0, 10)
    return {
      orderId: null,
      orderNumber: '',
      jobNumber: '',
      orderTitle: '',
      customerName: '',
      customerRef: '',
      orderedBy: '',
      orderedOn: today,
      requiredOn: today,
      qty: 1,
      status: 0,
      paymentTerms: '',
      remarks: '',
    }
  }

  const parsedNumbers = parseCompositeOrderNumber(job.orderNumber)

  return {
    orderId: job.orderId,
    orderNumber: parsedNumbers.orderNumber,
    jobNumber: parsedNumbers.jobNumber,
    orderTitle: job.orderTitle,
    customerName: job.customerName,
    customerRef: job.customerRef,
    orderedBy: job.orderedBy,
    orderedOn: job.orderedOn?.slice(0, 10) ?? '',
    requiredOn: job.requiredOn?.slice(0, 10) ?? '',
    qty: job.qty,
    status: job.status,
    paymentTerms: job.paymentTerms ?? '',
    remarks: job.remarks ?? '',
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

async function handleSubmit() {
  const { valid } = await formRef.value!.validate()
  if (!valid) return

  saving.value = true
  errorMessage.value = ''

  try {
    await saveJob(draft.value)
    emit('saved')
  } catch {
    errorMessage.value = t('jobForm.saveFailed')
  } finally {
    saving.value = false
  }
}

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
  background: #d9d9d9;
  color: #1f2328;
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

.legacy-product-details-wrap {
  width: calc(100% - 26px);
  position: relative;
}

.legacy-product-details-html {
  min-height: var(--legacy-notes-height);
  max-height: var(--legacy-notes-height);
  overflow-y: auto;
  border: 1px solid #9a9a9a;
  border-radius: 4px;
  background: #f3f3f3;
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
  border: 1px solid #8e8e8e;
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

.legacy-preview {
  border: 1px solid #9a9a9a;
  background: #f0f0f0;
  min-height: 182px;
}

.legacy-preview-header {
  padding: 6px 10px;
  font-size: 12px;
  color: #4f4f4f;
  border-bottom: 1px solid #b4b4b4;
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

:deep(.v-theme--dark) .legacy-form-surface {
  background: #242a30;
  color: #e7ebf0;
}

:deep(.v-theme--dark) .legacy-form-surface .v-btn {
  letter-spacing: 0.01em;
}

:deep(.v-theme--dark) .legacy-form-surface .v-field {
  background: #2f3841;
  color: #edf2f7;
}

:deep(.v-theme--dark) .legacy-form-surface .v-field__input {
  color: #edf2f7;
}

:deep(.v-theme--dark) .legacy-form-surface .v-field-label,
:deep(.v-theme--dark) .legacy-form-surface .v-label {
  color: #cfd6de;
  opacity: 1;
}

:deep(.v-theme--dark) .legacy-form-surface .v-field--variant-outlined .v-field__outline {
  --v-field-border-opacity: 0.75;
}

:deep(.v-theme--dark) .legacy-preview {
  border-color: #555f6b;
  background: #1f252b;
}

:deep(.v-theme--dark) .legacy-product-details-html {
  border-color: #5d6672;
  background: #2b333c;
  color: #edf2f7;
}

:deep(.v-theme--dark) .legacy-preview-header {
  color: #c5c9cf;
  border-bottom-color: #555f6b;
}

:deep(.v-theme--dark) .legacy-indicator {
  border-color: #8b96a3;
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
