<template>
  <v-dialog v-model="attachmentModel" max-width="min(100%, 1100px)" scrollable>
    <v-card class="job-attachment-dialog">
      <v-card-title class="d-flex align-center ga-2 flex-wrap">
        <div class="text-h6">{{ t('jobForm.dialogs.attachmentsTitle') }}</div>
        <v-chip size="small" color="primary" variant="tonal">{{ job?.orderNumber || '-' }}</v-chip>
        <v-spacer />
        <v-btn size="small" icon="mdi-close" variant="tonal" @click="attachmentModel = false" />
      </v-card-title>

      <v-divider />

      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>
        <v-alert v-if="infoMessage" type="info" variant="tonal" class="mb-3">{{ infoMessage }}</v-alert>

        <div class="attachment-toolbar mb-3">
          <v-btn-toggle v-model="sizeMode" mandatory density="comfortable" divided>
            <v-btn value="small">{{ t('jobForm.dialogs.size.small') }}</v-btn>
            <v-btn value="medium">{{ t('jobForm.dialogs.size.medium') }}</v-btn>
            <v-btn value="large">{{ t('jobForm.dialogs.size.large') }}</v-btn>
            <v-btn value="x-large">{{ t('jobForm.dialogs.size.xLarge') }}</v-btn>
          </v-btn-toggle>

          <div class="attachment-toolbar__spacer" />

          <v-file-input
            v-model="selectedUpload"
            :label="t('jobForm.dialogs.uploadFile')"
            multiple
            chips
            density="comfortable"
            variant="outlined"
            hide-details
            :disabled="uploading || !job"
            class="attachment-upload"
          />

          <v-btn
            color="primary"
            prepend-icon="mdi-upload"
            :loading="uploading"
            :disabled="!job || selectedUpload.length === 0"
            @click="handleUpload"
          >
            {{ t('jobForm.actions.upload') }}
          </v-btn>
        </div>

        <div class="attachment-actions mb-3">
          <v-btn
            variant="outlined"
            prepend-icon="mdi-open-in-new"
            :disabled="selectedKeys.length === 0 || busyAction"
            :loading="openingSelection"
            @click="openSelected"
          >
            {{ t('jobForm.actions.open') }}
          </v-btn>

          <v-btn
            variant="outlined"
            prepend-icon="mdi-download-multiple"
            :disabled="selectedKeys.length === 0 || busyAction"
            :loading="downloading"
            @click="downloadSelected"
          >
            {{ t('jobForm.actions.download') }}
          </v-btn>

          <v-btn
            variant="outlined"
            color="error"
            prepend-icon="mdi-delete"
            :disabled="selectedKeys.length === 0 || busyAction"
            :loading="deleting"
            @click="deleteSelected"
          >
            {{ t('jobForm.actions.deleteSelected') }}
          </v-btn>

          <v-spacer />

          <span class="text-caption text-medium-emphasis">
            {{ t('jobForm.dialogs.selectedCount', { count: selectedKeys.length }) }}
          </span>
        </div>

        <div v-if="attachments.length === 0" class="text-body-2 text-medium-emphasis pa-4">
          {{ t('jobForm.dialogs.noAttachments') }}
        </div>

        <div v-else class="attachment-grid" :style="tileCssVariables">
          <v-card
            v-for="attachment in attachments"
            :key="attachmentKey(attachment)"
            variant="tonal"
            class="attachment-tile"
            :class="{ 'attachment-tile--selected': selectedKeys.includes(attachmentKey(attachment)) }"
            @click="toggleSelected(attachmentKey(attachment))"
          >
            <div class="attachment-tile__check">
              <v-checkbox-btn
                :model-value="selectedKeys.includes(attachmentKey(attachment))"
                density="compact"
                hide-details
                @click.stop="toggleSelected(attachmentKey(attachment))"
              />
            </div>

            <div class="attachment-tile__preview">
              <img
                v-if="isImageFile(attachment.fileName) && previewSrc(attachment)"
                :src="previewSrc(attachment)"
                :alt="attachment.fileName"
                class="attachment-preview-image"
              >
              <v-icon v-else :size="iconSize">{{ tileIcon(attachment.fileName) }}</v-icon>
            </div>

            <div class="attachment-tile__meta">
              <div class="attachment-tile__name" :title="attachment.fileName">{{ attachment.fileName }}</div>
              <div class="text-caption text-medium-emphasis">{{ attachment.attachmentType }} • {{ attachment.uploadedBy }}</div>
            </div>

            <div class="attachment-tile__actions">
              <v-btn size="small" variant="text" prepend-icon="mdi-open-in-new" @click.stop="openAttachment(attachment)">
                {{ t('jobForm.actions.open') }}
              </v-btn>
              <v-btn size="small" variant="text" prepend-icon="mdi-download" @click.stop="downloadAttachment(attachment)">
                {{ t('jobForm.actions.download') }}
              </v-btn>
            </div>
          </v-card>
        </div>
      </v-card-text>
      <v-divider />
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="attachmentModel = false">{{ t('jobForm.actions.cancel') }}</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <v-dialog v-model="productDetailsModel" max-width="min(100%, 760px)" scrollable>
    <v-card>
      <v-card-title class="d-flex align-center ga-2 flex-wrap">
        <div class="text-h6">{{ t('jobForm.dialogs.productDetailsTitle') }}</div>
        <v-spacer />
        <v-btn size="small" icon="mdi-close" variant="tonal" :disabled="savingProductDetails" @click="productDetailsModel = false" />
      </v-card-title>
      <v-card-text>
        <!-- <v-alert type="info" variant="tonal" class="mb-3">
          {{ t('jobForm.dialogs.productDetailsHint') }}
        </v-alert> -->

          <div class="product-details-editor" :class="{ 'product-details-editor--disabled': savingProductDetails || !job }">
            <Ckeditor :editor="htmlEditor" v-model="productDetails" :config="editorConfig" />
        </div>
      </v-card-text>
      <v-divider />
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" :disabled="savingProductDetails" @click="productDetailsModel = false">
          {{ t('jobForm.actions.cancel') }}
        </v-btn>
        <v-btn
          color="primary"
          :loading="savingProductDetails"
          :disabled="!job"
          @click="saveProductDetails"
        >
          {{ t('jobForm.actions.save') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <v-dialog v-model="remarksModel" max-width="min(100%, 760px)" scrollable>
    <v-card>
      <v-card-title class="d-flex align-center ga-2 flex-wrap">
        <div class="text-h6">{{ t('jobForm.dialogs.remarksTitle') }}</div>
        <v-spacer />
        <v-btn size="small" icon="mdi-close" variant="tonal" :disabled="savingRemarks" @click="remarksModel = false" />
      </v-card-title>
      <v-card-text>
        <!-- <v-alert type="info" variant="tonal" class="mb-3">
          {{ t('jobForm.dialogs.remarksHint') }}
        </v-alert> -->

        <div class="remarks-editor" :class="{ 'remarks-editor--disabled': savingRemarks || !job }">
          <Ckeditor :editor="htmlEditor" v-model="remarks" :config="remarksEditorConfig" />
        </div>
      </v-card-text>
      <v-divider />
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" :disabled="savingRemarks" @click="remarksModel = false">
          {{ t('jobForm.actions.cancel') }}
        </v-btn>
        <v-btn
          color="primary"
          :loading="savingRemarks"
          :disabled="!job"
          @click="saveRemarks"
        >
          {{ t('jobForm.actions.save') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <v-dialog v-model="showDeleteConfirm" max-width="460">
    <v-card>
      <v-card-title>{{ t('jobForm.actions.deleteSelected') }}</v-card-title>
      <v-card-text>
        {{ t('jobForm.messages.confirmDeleteAttachments', { count: selectedKeys.length }) }}
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showDeleteConfirm = false">
          {{ t('jobForm.actions.cancel') }}
        </v-btn>
        <v-btn color="error" variant="flat" :loading="deleting" @click="confirmDeleteSelected">
          {{ t('jobForm.actions.deleteSelected') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Ckeditor } from '@ckeditor/ckeditor5-vue'
import ClassicEditor from '@ckeditor/ckeditor5-build-classic'
import { getJobPreviewBlob } from '@/services/jobOrders'
import { deleteJobAttachments, saveJob, uploadJobAttachment } from '@/services/jobs'
import type { JobAttachment, JobDetail, JobOrderFormData } from '@/types/api'

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

type AttachmentSizeMode = 'small' | 'medium' | 'large' | 'x-large'

const props = defineProps<{
  job: JobDetail | null
  attachmentOpen: boolean
  productDetailsOpen: boolean
  remarksOpen: boolean
}>()

const emit = defineEmits<{
  (e: 'update:attachmentOpen', value: boolean): void
  (e: 'update:productDetailsOpen', value: boolean): void
  (e: 'update:remarksOpen', value: boolean): void
  (e: 'updated'): void
  (e: 'error', message: string): void
}>()

const { t } = useI18n({ useScope: 'global' })
const htmlEditor = ClassicEditor

const editorConfig = {
  licenseKey: 'GPL',
  toolbar: {
    items: [
      'undo',
      'redo',
      '|',
      'heading',
      '|',
      'bold',
      'italic',
      'link',
      '|',
      'bulletedList',
      'numberedList',
      '|',
      'outdent',
      'indent',
      '|',
      'blockQuote',
      'insertTable',
      'mediaEmbed',
      'imageUpload',
    ],
    shouldNotGroupWhenFull: true,
  },
}

const remarksEditorConfig = {
  licenseKey: 'GPL',
  toolbar: {
    items: [
      'undo',
      'redo',
      '|',
      'heading',
      '|',
      'bold',
      'italic',
      'link',
      '|',
      'bulletedList',
      'numberedList',
      '|',
      'outdent',
      'indent',
      '|',
      'blockQuote',
      'insertTable',
      'tableColumn',
      'tableRow',
    ],
    shouldNotGroupWhenFull: true,
  },
}

const uploading = ref(false)
const openingSelection = ref(false)
const downloading = ref(false)
const deleting = ref(false)
const savingProductDetails = ref(false)
const errorMessage = ref('')
const infoMessage = ref('')
const selectedUpload = ref<File[]>([])
const selectedKeys = ref<string[]>([])
const sizeMode = ref<AttachmentSizeMode>('medium')
const productDetails = ref('')
const previewUrls = ref<Record<string, string>>({})
const remarks = ref('')
const savingRemarks = ref(false)
const showDeleteConfirm = ref(false)

const tileSizeMap: Record<AttachmentSizeMode, number> = {
  small: 84,
  medium: 120,
  large: 168,
  'x-large': 224,
}

const tileCssVariables = computed(() => {
  const tile = tileSizeMap[sizeMode.value]
  return {
    '--tile-size': `${tile}px`,
  }
})

const iconSize = computed(() => Math.max(22, Math.floor(tileSizeMap[sizeMode.value] * 0.34)))
const attachments = computed(() => props.job?.attachments ?? [])
const busyAction = computed(() => uploading.value || openingSelection.value || downloading.value || deleting.value)

const attachmentModel = computed({
  get: () => props.attachmentOpen,
  set: (value: boolean) => emit('update:attachmentOpen', value),
})

const productDetailsModel = computed({
  get: () => props.productDetailsOpen,
  set: (value: boolean) => emit('update:productDetailsOpen', value),
})

const remarksModel = computed({
  get: () => props.remarksOpen,
  set: (value: boolean) => emit('update:remarksOpen', value),
})

watch(
  () => [props.productDetailsOpen, props.job?.orderId],
  () => {
    if (!props.productDetailsOpen) return
    productDetails.value = props.job?.productDetails ?? ''
  },
  { immediate: true },
)

watch(
  () => [props.remarksOpen, props.job?.orderId],
  () => {
    if (!props.remarksOpen) return
    remarks.value = props.job?.remarks && props.job.remarks.trim()
      ? props.job.remarks
      : defaultRemarksTable()
  },
  { immediate: true },
)

function defaultRemarksTable(): string {
  const rows = Array.from({ length: 4 })
    .map(() => '<tr><td>&nbsp;</td><td style="text-align: right;">&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td></tr>')
    .join('')
  return (
    '<table><thead><tr><th>　成本分析　</th><th style="text-align: left;">　金額　</th><th>　日期　</th><th>　供應商　</th></tr></thead>' +
    '<tbody>' + rows + '</tbody></table>'
  )
}

function sumRemarksAmount(html: string): number {
  const doc = new DOMParser().parseFromString(html, 'text/html')
  const cells = doc.querySelectorAll('table td:nth-child(2)')
  let total = 0
  cells.forEach((cell) => {
    const text = (cell.textContent ?? '').replace(/[^0-9.\-]/g, '')
    if (text.trim() === '') return
    const value = Number(text)
    if (!Number.isNaN(value)) total += value
  })
  return total
}

watch(
  () => [props.attachmentOpen, props.job?.orderId],
  async ([open]) => {
    if (!open) {
      selectedKeys.value = []
      selectedUpload.value = []
      errorMessage.value = ''
      infoMessage.value = ''
      showDeleteConfirm.value = false
      revokeAllPreviewUrls()
      return
    }

    await loadImagePreviews()
  },
  { immediate: true },
)

watch(
  () => (props.job?.attachments ?? []).map((attachment) => attachment.attachmentId).join('|'),
  async () => {
    if (!props.attachmentOpen) {
      return
    }

    await loadImagePreviews()
  },
)

function attachmentKey(attachment: JobAttachment): string {
  return `${attachment.fileName}-${attachment.uploadedOn}`
}

function previewSrc(attachment: JobAttachment): string {
  return previewUrls.value[attachmentKey(attachment)] ?? ''
}

function revokeAllPreviewUrls() {
  for (const url of Object.values(previewUrls.value)) {
    URL.revokeObjectURL(url)
  }
  previewUrls.value = {}
}

async function loadImagePreviews() {
  if (!props.job) return

  revokeAllPreviewUrls()

  const imageItems = attachments.value.filter((attachment) => isImageFile(attachment.fileName))
  if (imageItems.length === 0) {
    return
  }

  const response = await Promise.all(imageItems.map(async (attachment) => {
    try {
      const blob = await getJobPreviewBlob(props.job!.orderId, attachment.fileName, attachment.attachmentType)
      return [attachmentKey(attachment), URL.createObjectURL(blob)] as const
    } catch {
      return null
    }
  }))

  for (const item of response) {
    if (!item) continue

    const [key, objectUrl] = item
    previewUrls.value[key] = objectUrl
  }
}

function toggleSelected(key: string) {
  if (selectedKeys.value.includes(key)) {
    selectedKeys.value = selectedKeys.value.filter((item) => item !== key)
    return
  }

  selectedKeys.value = [...selectedKeys.value, key]
}

function isImageFile(fileName: string): boolean {
  return /\.(png|jpe?g|gif|webp|bmp|svg)$/i.test(fileName)
}

function isPdfFile(fileName: string): boolean {
  return /\.pdf$/i.test(fileName)
}

function tileIcon(fileName: string): string {
  if (isPdfFile(fileName)) return 'mdi-file-pdf-box'
  if (isImageFile(fileName)) return 'mdi-file-image'
  return 'mdi-file-outline'
}

function triggerBlobDownload(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  anchor.click()
  setTimeout(() => URL.revokeObjectURL(url), 30_000)
}

async function openAttachment(attachment: JobAttachment) {
  if (!props.job) return

  try {
    const blob = await getJobPreviewBlob(props.job.orderId, attachment.fileName, attachment.attachmentType)
    const url = URL.createObjectURL(blob)
    window.open(url, '_blank', 'noopener,noreferrer')
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch {
    errorMessage.value = t('jobForm.messages.attachmentOpenFailed')
  }
}

async function downloadAttachment(attachment: JobAttachment) {
  if (!props.job) return

  try {
    const blob = await getJobPreviewBlob(props.job.orderId, attachment.fileName, attachment.attachmentType)
    triggerBlobDownload(blob, attachment.fileName)
  } catch {
    errorMessage.value = t('jobForm.messages.attachmentDownloadFailed')
  }
}

async function openSelected() {
  if (!props.job || selectedKeys.value.length === 0) {
    return
  }

  openingSelection.value = true
  errorMessage.value = ''
  infoMessage.value = ''
  try {
    const targets = attachments.value.filter((attachment) => selectedKeys.value.includes(attachmentKey(attachment)))
    for (const attachment of targets) {
      const blob = await getJobPreviewBlob(props.job.orderId, attachment.fileName, attachment.attachmentType)
      const url = URL.createObjectURL(blob)
      window.open(url, '_blank', 'noopener,noreferrer')
      setTimeout(() => URL.revokeObjectURL(url), 60_000)
    }

    infoMessage.value = t('jobForm.messages.attachmentOpenStarted', { count: targets.length })
  } catch {
    errorMessage.value = t('jobForm.messages.attachmentOpenFailed')
  } finally {
    openingSelection.value = false
  }
}

async function downloadSelected() {
  if (!props.job || selectedKeys.value.length === 0) {
    return
  }

  downloading.value = true
  errorMessage.value = ''
  infoMessage.value = ''
  try {
    const targets = attachments.value.filter((attachment) => selectedKeys.value.includes(attachmentKey(attachment)))
    for (const attachment of targets) {
      const blob = await getJobPreviewBlob(props.job.orderId, attachment.fileName, attachment.attachmentType)
      triggerBlobDownload(blob, attachment.fileName)
    }

    infoMessage.value = t('jobForm.messages.attachmentDownloadStarted', { count: targets.length })
  } catch {
    errorMessage.value = t('jobForm.messages.attachmentDownloadFailed')
  } finally {
    downloading.value = false
  }
}

function deleteSelected() {
  if (selectedKeys.value.length === 0) {
    return
  }

  showDeleteConfirm.value = true
}

async function confirmDeleteSelected() {
  if (!props.job || selectedKeys.value.length === 0) {
    showDeleteConfirm.value = false
    return
  }

  deleting.value = true
  errorMessage.value = ''
  infoMessage.value = ''
  showDeleteConfirm.value = false

  try {
    const attachmentIds = attachments.value
      .filter((attachment) => selectedKeys.value.includes(attachmentKey(attachment)))
      .map((attachment) => attachment.attachmentId)

    await deleteJobAttachments(props.job.orderId, attachmentIds)
    selectedKeys.value = []
    emit('updated')
    infoMessage.value = t('jobForm.messages.attachmentDeleteSuccess')
  } catch {
    errorMessage.value = t('jobForm.messages.attachmentDeleteFailed')
  } finally {
    deleting.value = false
  }
}

async function handleUpload() {
  if (!props.job || selectedUpload.value.length === 0) return

  uploading.value = true
  errorMessage.value = ''
  infoMessage.value = ''
  try {
    for (const file of selectedUpload.value) {
      await uploadJobAttachment(props.job.orderId, file)
    }
    selectedUpload.value = []
    emit('updated')
    infoMessage.value = t('jobForm.messages.attachmentUploadSuccess')
  } catch {
    errorMessage.value = t('jobForm.messages.attachmentUploadFailed')
  } finally {
    uploading.value = false
  }
}

async function saveProductDetails() {
  if (!props.job) return

  savingProductDetails.value = true
  try {
    const parsed = parseCompositeOrderNumber(props.job.orderNumber)
    const payload: JobOrderFormData = {
      orderId: props.job.orderId,
      orderNumber: parsed.orderNumber,
      jobNumber: parsed.jobNumber || props.job.jobNumber || '',
      orderTitle: props.job.orderTitle,
      customerName: props.job.customerName,
      customerRef: props.job.customerRef,
      orderedBy: props.job.orderedBy,
      orderedOn: props.job.orderedOn?.slice(0, 10) ?? '',
      requiredOn: props.job.requiredOn?.slice(0, 10) ?? '',
      qty: props.job.qty,
      status: props.job.status ?? 1,
      orderType: props.job.orderType ?? 0,
      paymentTerms: props.job.paymentTerms ?? '',
      remarks: props.job.remarks ?? '',
      productDetails: productDetails.value,
      soNumber: props.job.soNumber ?? '',
      originalSONumber: props.job.originalSONumber ?? '',
      productStyle: props.job.productStyle ?? '',
      productCode: props.job.productCode ?? '',
      outputRef: props.job.outputRef ?? '',
      invoiceRef: props.job.invoiceRef ?? '',
      invoiceAmount: props.job.invoiceAmount ?? undefined,
      workflowAttributes: props.job.workflowAttributes ?? {},
    }

    await saveJob(payload)
    emit('updated')
    productDetailsModel.value = false
  } catch {
    emit('error', t('jobForm.messages.productDetailsSaveFailed'))
  } finally {
    savingProductDetails.value = false
  }
}

async function saveRemarks() {
  if (!props.job) return

  savingRemarks.value = true
  try {
    const parsed = parseCompositeOrderNumber(props.job.orderNumber)
    const payload: JobOrderFormData = {
      orderId: props.job.orderId,
      orderNumber: parsed.orderNumber,
      jobNumber: parsed.jobNumber || props.job.jobNumber || '',
      orderTitle: props.job.orderTitle,
      customerName: props.job.customerName,
      customerRef: props.job.customerRef,
      orderedBy: props.job.orderedBy,
      orderedOn: props.job.orderedOn?.slice(0, 10) ?? '',
      requiredOn: props.job.requiredOn?.slice(0, 10) ?? '',
      qty: props.job.qty,
      status: props.job.status ?? 1,
      orderType: props.job.orderType ?? 0,
      paymentTerms: props.job.paymentTerms ?? '',
      remarks: remarks.value,
      productDetails: props.job.productDetails ?? '',
      soNumber: props.job.soNumber ?? '',
      originalSONumber: String(sumRemarksAmount(remarks.value)),
      productStyle: props.job.productStyle ?? '',
      productCode: props.job.productCode ?? '',
      outputRef: props.job.outputRef ?? '',
      invoiceRef: props.job.invoiceRef ?? '',
      invoiceAmount: props.job.invoiceAmount ?? undefined,
      workflowAttributes: props.job.workflowAttributes ?? {},
    }

    await saveJob(payload)
    emit('updated')
    remarksModel.value = false
  } catch {
    emit('error', t('jobForm.messages.remarksSaveFailed'))
  } finally {
    savingRemarks.value = false
  }
}

onBeforeUnmount(() => {
  revokeAllPreviewUrls()
})
</script>

<style scoped>
.job-attachment-dialog {
  min-height: 50vh;
}

.attachment-toolbar {
  display: flex;
  gap: 12px;
  align-items: center;
  flex-wrap: wrap;
}

.attachment-toolbar__spacer {
  flex: 1 1 auto;
}

.attachment-upload {
  min-width: min(460px, 100%);
  flex: 1 1 320px;
}

.attachment-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.attachment-grid {
  display: grid;
  gap: 12px;
  grid-template-columns: repeat(auto-fill, minmax(var(--tile-size), 1fr));
}

.attachment-tile {
  padding: 8px;
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
  cursor: pointer;
  min-height: calc(var(--tile-size) + 110px);
  display: grid;
  align-content: start;
}

.attachment-tile--selected {
  border-color: rgba(var(--v-theme-primary), 0.7);
  box-shadow: 0 0 0 2px rgba(var(--v-theme-primary), 0.2);
}

.attachment-tile__check {
  display: flex;
  justify-content: flex-end;
}

.attachment-tile__preview {
  width: 100%;
  height: var(--tile-size);
  display: grid;
  place-items: center;
  border-radius: 8px;
  background: rgba(var(--v-theme-surface-variant), 0.3);
  overflow: hidden;
}

.attachment-preview-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.attachment-tile__meta {
  margin-top: 8px;
}

.attachment-tile__name {
  font-size: 12px;
  font-weight: 600;
  line-height: 1.35;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.attachment-tile__actions {
  margin-top: 4px;
  display: flex;
  justify-content: space-between;
  gap: 4px;
}

.product-details-editor {
  border: 1px solid rgba(var(--v-theme-outline), 0.45);
  border-radius: 8px;
  overflow: hidden;
}

.product-details-editor :deep(.ck-editor__editable_inline) {
  min-height: 320px;
  max-height: 60vh;
}

.product-details-editor--disabled {
  opacity: 0.72;
  pointer-events: none;
}

.remarks-editor {
  border: 1px solid rgba(var(--v-theme-outline), 0.45);
  border-radius: 8px;
  overflow: hidden;
}

.remarks-editor :deep(.ck-editor__editable_inline) {
  min-height: 320px;
  max-height: 60vh;
}

:deep(.ck-editor__editable) {
  color: var(--ck-color-base-text, #333);
}

.remarks-editor :deep(.ck-editor__editable_inline table td:nth-child(2)),
.remarks-editor :deep(.ck-editor__editable_inline table th:nth-child(2)) {
  text-align: right;
}

.remarks-editor--disabled {
  opacity: 0.72;
  pointer-events: none;
}

@media (max-width: 900px) {
  .attachment-toolbar {
    display: grid;
    grid-template-columns: 1fr;
  }

  .attachment-toolbar__spacer {
    display: none;
  }

  .attachment-upload {
    min-width: 0;
  }
}
</style>
