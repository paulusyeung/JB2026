<template>
  <v-dialog v-model="attachmentModel" max-width="760" scrollable>
    <v-card>
      <v-card-title>{{ t('jobForm.dialogs.attachmentsTitle') }}</v-card-title>
      <v-card-text>
        <v-file-input
          v-model="selectedUpload"
          :label="t('jobForm.dialogs.uploadFile')"
          density="comfortable"
          variant="outlined"
          hide-details
          :disabled="uploading || !job"
        />

        <div class="d-flex justify-end mt-3 mb-3">
          <v-btn
            color="primary"
            :loading="uploading"
            :disabled="!job || !selectedUpload"
            @click="handleUpload"
          >
            {{ t('jobForm.actions.upload') }}
          </v-btn>
        </div>

        <v-list v-if="(job?.attachments.length ?? 0) > 0" lines="two" density="compact">
          <v-list-item
            v-for="attachment in job?.attachments ?? []"
            :key="`${attachment.fileName}-${attachment.uploadedOn}`"
            :title="attachment.fileName"
            :subtitle="`${attachment.attachmentType} • ${attachment.uploadedBy}`"
          >
            <template #append>
              <v-btn
                size="small"
                variant="outlined"
                :disabled="openingFile"
                @click="openAttachment(attachment.fileName, attachment.attachmentType)"
              >
                {{ t('jobForm.actions.open') }}
              </v-btn>
            </template>
          </v-list-item>
        </v-list>

        <v-alert v-else type="info" variant="tonal">
          {{ t('jobForm.dialogs.noAttachments') }}
        </v-alert>
      </v-card-text>
      <v-divider />
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="attachmentModel = false">{{ t('jobForm.actions.cancel') }}</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <v-dialog v-model="productDetailsModel" max-width="760" scrollable>
    <v-card>
      <v-card-title>{{ t('jobForm.dialogs.productDetailsTitle') }}</v-card-title>
      <v-card-text>
        <v-alert type="info" variant="tonal" class="mb-3">
          {{ t('jobForm.dialogs.productDetailsHint') }}
        </v-alert>

        <v-textarea
          v-model="productDetails"
          :label="t('jobForm.fields.productDetails')"
          rows="8"
          density="comfortable"
          variant="outlined"
          :disabled="savingProductDetails || !job"
        />
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
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { getJobPreviewBlob } from '@/services/jobOrders'
import { saveJob, uploadJobAttachment } from '@/services/jobs'
import type { JobDetail, JobOrderFormData } from '@/types/api'

const props = defineProps<{
  job: JobDetail | null
  attachmentOpen: boolean
  productDetailsOpen: boolean
}>()

const emit = defineEmits<{
  (e: 'update:attachmentOpen', value: boolean): void
  (e: 'update:productDetailsOpen', value: boolean): void
  (e: 'updated'): void
  (e: 'error', message: string): void
}>()

const { t } = useI18n({ useScope: 'global' })

const uploading = ref(false)
const openingFile = ref(false)
const savingProductDetails = ref(false)
const selectedUpload = ref<File | null>(null)
const productDetails = ref('')

const attachmentModel = computed({
  get: () => props.attachmentOpen,
  set: (value: boolean) => emit('update:attachmentOpen', value),
})

const productDetailsModel = computed({
  get: () => props.productDetailsOpen,
  set: (value: boolean) => emit('update:productDetailsOpen', value),
})

watch(
  () => [props.productDetailsOpen, props.job?.orderId],
  () => {
    if (!props.productDetailsOpen) return
    productDetails.value = props.job?.orderTitle ?? ''
  },
  { immediate: true },
)

async function openAttachment(fileName: string, attachmentType: string) {
  if (!props.job) return

  openingFile.value = true
  try {
    const blob = await getJobPreviewBlob(props.job.orderId, fileName, attachmentType)
    const url = URL.createObjectURL(blob)
    window.open(url, '_blank', 'noopener,noreferrer')
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch {
    emit('error', t('jobForm.messages.attachmentUploadFailed'))
  } finally {
    openingFile.value = false
  }
}

async function handleUpload() {
  if (!props.job || !selectedUpload.value) return

  uploading.value = true
  try {
    await uploadJobAttachment(props.job.orderId, selectedUpload.value)
    selectedUpload.value = null
    emit('updated')
  } catch {
    emit('error', t('jobForm.messages.attachmentUploadFailed'))
  } finally {
    uploading.value = false
  }
}

async function saveProductDetails() {
  if (!props.job) return

  savingProductDetails.value = true
  try {
    const payload: JobOrderFormData = {
      orderId: props.job.orderId,
      orderNumber: props.job.orderNumber,
      jobNumber: '',
      orderTitle: productDetails.value,
      customerName: props.job.customerName,
      customerRef: props.job.customerRef,
      orderedBy: props.job.orderedBy,
      orderedOn: props.job.orderedOn?.slice(0, 10) ?? '',
      requiredOn: props.job.requiredOn?.slice(0, 10) ?? '',
      qty: props.job.qty,
      status: props.job.status,
      paymentTerms: props.job.paymentTerms ?? '',
      remarks: props.job.remarks ?? '',
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
</script>
