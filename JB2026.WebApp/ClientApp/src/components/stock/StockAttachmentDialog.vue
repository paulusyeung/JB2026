<template>
  <v-dialog
    :model-value="modelValue"
    max-width="min(100%, 1100px)"
    scrollable
    @update:model-value="onDialogVisibilityChanged"
  >
    <v-card class="stock-attachment-dialog">
      <v-card-title class="d-flex align-center ga-2 flex-wrap">
        <div class="text-h6">{{ t('stock.attachments.title') }}</div>
        <v-chip size="small" color="primary" variant="tonal">{{ stockNumber || '-' }}</v-chip>
        <v-spacer />
        <v-btn icon="mdi-close" variant="tonal" @click="closeDialog" />
      </v-card-title>

      <v-divider />

      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>
        <v-alert v-if="infoMessage" type="info" variant="tonal" class="mb-3">{{ infoMessage }}</v-alert>

        <div class="attachment-toolbar mb-3">
          <v-btn-toggle v-model="sizeMode" mandatory density="comfortable" divided>
            <v-btn value="small">{{ t('stock.attachments.size.small') }}</v-btn>
            <v-btn value="medium">{{ t('stock.attachments.size.medium') }}</v-btn>
            <v-btn value="large">{{ t('stock.attachments.size.large') }}</v-btn>
            <v-btn value="x-large">{{ t('stock.attachments.size.xLarge') }}</v-btn>
          </v-btn-toggle>

          <div class="attachment-toolbar__spacer" />

          <v-file-input
            v-model="selectedUpload"
            :label="t('stock.attachments.uploadLabel')"
            multiple
            chips
            density="comfortable"
            variant="outlined"
            hide-details
            :disabled="!productId || uploading"
            class="attachment-upload"
          />

          <v-btn
            color="primary"
            prepend-icon="mdi-upload"
            :loading="uploading"
            :disabled="!productId || selectedUpload.length === 0"
            @click="uploadSelected"
          >
            {{ t('stock.attachments.actions.upload') }}
          </v-btn>
        </div>

        <div class="attachment-actions mb-3">
          <v-btn
            variant="outlined"
            prepend-icon="mdi-download-multiple"
            :disabled="selectedIds.length === 0 || busyAction"
            :loading="downloading"
            @click="downloadSelected"
          >
            {{ t('stock.attachments.actions.downloadSelected') }}
          </v-btn>

          <v-btn
            variant="outlined"
            color="error"
            prepend-icon="mdi-delete"
            :disabled="!canDelete || selectedIds.length === 0 || busyAction"
            :loading="deleting"
            @click="deleteSelected"
          >
            {{ t('stock.attachments.actions.deleteSelected') }}
          </v-btn>

          <v-spacer />

          <span class="text-caption text-medium-emphasis">
            {{ t('stock.attachments.selectedCount', { count: selectedIds.length }) }}
          </span>
        </div>

        <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-3" />

        <div v-if="attachments.length === 0 && !loading" class="text-body-2 text-medium-emphasis pa-4">
          {{ t('stock.attachments.empty') }}
        </div>

        <div v-else class="attachment-grid" :style="tileCssVariables">
          <v-card
            v-for="item in attachments"
            :key="item.attachmentId"
            variant="tonal"
            class="attachment-tile"
            :class="{ 'attachment-tile--selected': selectedIds.includes(item.attachmentId) }"
            @click="toggleSelected(item.attachmentId)"
          >
            <div class="attachment-tile__check">
              <v-checkbox-btn
                :model-value="selectedIds.includes(item.attachmentId)"
                density="compact"
                hide-details
                @click.stop="toggleSelected(item.attachmentId)"
              />
            </div>

            <div class="attachment-tile__preview">
              <img
                v-if="isImageFile(item.fileName) && previewSrc(item)"
                :src="previewSrc(item)"
                :alt="item.fileName"
                class="attachment-preview-image"
              >
              <v-icon v-else :size="iconSize">{{ tileIcon(item.fileName) }}</v-icon>
            </div>

            <div class="attachment-tile__meta">
              <div class="attachment-tile__name" :title="item.fileName">{{ item.fileName }}</div>
              <div class="text-caption text-medium-emphasis">{{ formatBytes(item.fileSizeBytes) }}</div>
            </div>

            <div class="attachment-tile__actions">
              <v-btn size="small" variant="text" prepend-icon="mdi-open-in-new" @click.stop="openPreview(item)">
                {{ t('stock.attachments.actions.preview') }}
              </v-btn>
              <v-btn size="small" variant="text" prepend-icon="mdi-download" @click.stop="downloadAttachment(item)">
                {{ t('stock.attachments.actions.download') }}
              </v-btn>
            </div>
          </v-card>
        </div>
      </v-card-text>
    </v-card>
  </v-dialog>

  <v-dialog v-model="showDeleteConfirm" max-width="460">
    <v-card>
      <v-card-title>{{ t('stock.attachments.actions.deleteSelected') }}</v-card-title>
      <v-card-text>
        {{ t('stock.attachments.messages.confirmDelete', { count: selectedIds.length }) }}
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showDeleteConfirm = false">
          {{ t('common.cancel') }}
        </v-btn>
        <v-btn color="error" variant="flat" :loading="deleting" @click="confirmDeleteSelected">
          {{ t('stock.actions.delete') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  deleteProductAttachments,
  getProductAttachmentBlob,
  getProductAttachments,
  mapStockAttachmentError,
  uploadProductAttachments,
} from '@/services/stock'
import type { StockProductAttachment } from '@/types/api'

type AttachmentSizeMode = 'small' | 'medium' | 'large' | 'x-large'

const props = withDefaults(defineProps<{
  modelValue: boolean
  productId: string | null
  stockNumber?: string
  canDelete?: boolean
}>(), {
  stockNumber: '',
  canDelete: true,
})

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  changed: []
}>()

const { t } = useI18n({ useScope: 'global' })

const loading = ref(false)
const uploading = ref(false)
const downloading = ref(false)
const deleting = ref(false)
const errorMessage = ref('')
const infoMessage = ref('')
const sizeMode = ref<AttachmentSizeMode>('medium')
const attachments = ref<StockProductAttachment[]>([])
const selectedIds = ref<string[]>([])
const selectedUpload = ref<File[]>([])
const previewUrls = ref<Record<string, string>>({})
const showDeleteConfirm = ref(false)

const busyAction = computed(() => uploading.value || downloading.value || deleting.value)

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

watch(
  () => [props.modelValue, props.productId],
  async ([open]) => {
    if (!open || !props.productId) {
      return
    }

    await loadAttachments()
  },
  { immediate: true },
)

function onDialogVisibilityChanged(value: boolean) {
  emit('update:modelValue', value)
  if (!value) {
    clearState()
  }
}

function closeDialog() {
  emit('update:modelValue', false)
  clearState()
}

function clearState() {
  selectedIds.value = []
  selectedUpload.value = []
  errorMessage.value = ''
  infoMessage.value = ''
  showDeleteConfirm.value = false
  revokeAllPreviewUrls()
}

function revokeAllPreviewUrls() {
  for (const url of Object.values(previewUrls.value)) {
    URL.revokeObjectURL(url)
  }
  previewUrls.value = {}
}

async function loadImagePreviews(items: StockProductAttachment[]) {
  if (!props.productId) {
    return
  }

  const imageItems = items.filter((item) => isImageFile(item.fileName))
  if (imageItems.length === 0) {
    return
  }

  const response = await Promise.all(imageItems.map(async (item) => {
    try {
      const blob = await getProductAttachmentBlob(props.productId as string, item.attachmentId, true)
      return [item.attachmentId, URL.createObjectURL(blob)] as const
    } catch {
      return null
    }
  }))

  for (const item of response) {
    if (!item) {
      continue
    }

    const [attachmentId, objectUrl] = item
    previewUrls.value[attachmentId] = objectUrl
  }
}

async function loadAttachments() {
  if (!props.productId) {
    return
  }

  loading.value = true
  errorMessage.value = ''
  try {
    const items = await getProductAttachments(props.productId)
    attachments.value = items

    revokeAllPreviewUrls()
    await loadImagePreviews(items)
  } catch (error) {
    errorMessage.value = t(mapStockAttachmentError(error))
  } finally {
    loading.value = false
  }
}

function toggleSelected(attachmentId: string) {
  if (selectedIds.value.includes(attachmentId)) {
    selectedIds.value = selectedIds.value.filter((id) => id !== attachmentId)
    return
  }

  selectedIds.value = [...selectedIds.value, attachmentId]
}

function isImageFile(fileName: string): boolean {
  return /\.(png|jpe?g|gif|webp|bmp|svg)$/i.test(fileName)
}

function isPdfFile(fileName: string): boolean {
  return /\.pdf$/i.test(fileName)
}

function previewSrc(item: StockProductAttachment): string {
  return previewUrls.value[item.attachmentId] ?? ''
}

function tileIcon(fileName: string): string {
  if (isPdfFile(fileName)) return 'mdi-file-pdf-box'
  if (isImageFile(fileName)) return 'mdi-file-image'
  return 'mdi-file-outline'
}

function formatBytes(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes <= 0) {
    return '0 B'
  }

  const units = ['B', 'KB', 'MB', 'GB']
  let size = bytes
  let unitIndex = 0
  while (size >= 1024 && unitIndex < units.length - 1) {
    size /= 1024
    unitIndex++
  }

  const precision = unitIndex === 0 ? 0 : 1
  return `${size.toFixed(precision)} ${units[unitIndex]}`
}

function triggerBlobDownload(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  anchor.click()
  setTimeout(() => URL.revokeObjectURL(url), 30_000)
}

async function uploadSelected() {
  if (!props.productId || selectedUpload.value.length === 0) {
    return
  }

  uploading.value = true
  errorMessage.value = ''
  infoMessage.value = ''
  try {
    await uploadProductAttachments(props.productId, selectedUpload.value)
    selectedUpload.value = []
    await loadAttachments()
    emit('changed')
    infoMessage.value = t('stock.attachments.messages.uploadSuccess')
  } catch (error) {
    errorMessage.value = t(mapStockAttachmentError(error))
  } finally {
    uploading.value = false
  }
}

async function openPreview(item: StockProductAttachment) {
  if (!props.productId) {
    return
  }

  try {
    const inline = isImageFile(item.fileName) || isPdfFile(item.fileName)
    const blob = await getProductAttachmentBlob(props.productId, item.attachmentId, inline)

    if (inline) {
      const url = URL.createObjectURL(blob)
      window.open(url, '_blank', 'noopener,noreferrer')
      setTimeout(() => URL.revokeObjectURL(url), 60_000)
      return
    }

    triggerBlobDownload(blob, item.fileName)
  } catch (error) {
    errorMessage.value = t(mapStockAttachmentError(error))
  }
}

async function downloadAttachment(item: StockProductAttachment) {
  if (!props.productId) {
    return
  }

  try {
    const blob = await getProductAttachmentBlob(props.productId, item.attachmentId, false)
    triggerBlobDownload(blob, item.fileName)
  } catch (error) {
    errorMessage.value = t(mapStockAttachmentError(error))
  }
}

async function downloadSelected() {
  if (!props.productId) {
    return
  }

  if (selectedIds.value.length === 0) {
    errorMessage.value = t('stock.attachments.errors.selectFirst')
    return
  }

  downloading.value = true
  errorMessage.value = ''
  infoMessage.value = ''
  try {
    const targets = attachments.value.filter((item) => selectedIds.value.includes(item.attachmentId))
    for (const item of targets) {
      const blob = await getProductAttachmentBlob(props.productId, item.attachmentId, false)
      triggerBlobDownload(blob, item.fileName)
    }

    infoMessage.value = t('stock.attachments.messages.downloadStarted', { count: targets.length })
  } catch (error) {
    errorMessage.value = t(mapStockAttachmentError(error))
  } finally {
    downloading.value = false
  }
}

async function deleteSelected() {
  if (!props.productId) {
    return
  }

  if (!props.canDelete) {
    errorMessage.value = t('stock.attachments.errors.deleteNotAllowed')
    return
  }

  if (selectedIds.value.length === 0) {
    errorMessage.value = t('stock.attachments.errors.selectFirst')
    return
  }

  showDeleteConfirm.value = true
}

async function confirmDeleteSelected() {
  if (!props.productId || selectedIds.value.length === 0) {
    showDeleteConfirm.value = false
    return
  }

  showDeleteConfirm.value = false

  deleting.value = true
  errorMessage.value = ''
  infoMessage.value = ''
  try {
    await deleteProductAttachments(props.productId, selectedIds.value)
    selectedIds.value = []
    await loadAttachments()
    emit('changed')
    infoMessage.value = t('stock.attachments.messages.deleteSuccess')
  } catch (error) {
    errorMessage.value = t(mapStockAttachmentError(error))
  } finally {
    deleting.value = false
  }
}

onBeforeUnmount(() => {
  revokeAllPreviewUrls()
})
</script>

<style scoped>
.stock-attachment-dialog {
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
