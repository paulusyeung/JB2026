<template>
  <v-dialog
    :model-value="modelValue"
    max-width="480"
    persistent
    @update:model-value="onDialogVisibilityChanged"
  >
    <v-card v-draggable-dialog class="job-order-print-manager-dialog">
      <v-card-title class="d-flex align-center ga-2">
        <div class="text-h6">{{ t('jobForm.dialogs.printManager.title') }}</div>
        <v-spacer />
        <v-btn icon="mdi-close" variant="tonal" :disabled="printing" @click="closeDialog" />
      </v-card-title>

      <v-divider />

      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>
        <v-alert v-if="dmsSuccessMessage" type="success" variant="tonal" class="mb-3">{{ dmsSuccessMessage }}</v-alert>

        <v-form @submit.prevent>
          <v-row dense>
            <v-col cols="12">
              <v-text-field
                :model-value="props.orderNumber"
                :label="t('jobForm.dialogs.printManager.orderNumber')"
                density="comfortable"
                variant="solo-filled"
                readonly
                hide-details
              />
            </v-col>

            <v-col cols="12">
              <v-select
                v-model="form.layout"
                :label="t('jobForm.dialogs.printManager.layout')"
                :items="layoutOptions"
                density="comfortable"
                variant="outlined"
                hide-details
              />
            </v-col>

            <v-col cols="12" sm="6">
              <v-checkbox
                v-model="form.noPicture"
                :label="t('jobForm.dialogs.printManager.noPicture')"
                density="comfortable"
                hide-details
              />
            </v-col>

            <v-col cols="12" sm="6">
              <v-checkbox
                v-model="form.noProductDetails"
                :label="t('jobForm.dialogs.printManager.noProductDetails')"
                density="comfortable"
                hide-details
              />
            </v-col>

            <v-col cols="12" sm="6">
              <v-checkbox
                v-model="form.noRemarks"
                :label="t('jobForm.dialogs.printManager.noRemarks')"
                density="comfortable"
                hide-details
              />
            </v-col>

            <v-col cols="12">
              <div class="text-subtitle-2 mb-1">{{ t('jobForm.dialogs.printManager.workflows') }}</div>
              <div v-if="workflowItems.length === 0" class="text-body-2 text-medium-emphasis py-2">
                {{ t('jobForm.dialogs.printManager.noWorkflows') }}
              </div>
              <template v-else>
                <v-checkbox
                  v-model="allWorkflowsSelected"
                  :label="t('jobForm.dialogs.printManager.selectAllWorkflows')"
                  density="comfortable"
                  hide-details
                  class="mb-1"
                />
                <v-divider class="mb-1" />
                <v-checkbox
                  v-for="(item, index) in workflowItems"
                  :key="index"
                  v-model="form.selectedWorkflowIndices"
                  :label="item.label"
                  :value="index"
                  density="compact"
                  hide-details
                />
              </template>
            </v-col>
          </v-row>
        </v-form>
      </v-card-text>

      <v-divider />

      <v-card-actions class="px-4 py-3 ga-2">
        <v-btn v-if="paperlessConfigured" variant="outlined" :loading="uploading" :disabled="printing" @click="submitUploadToDms">
          {{ t('jobForm.dialogs.printManager.uploadToDms') }}
        </v-btn>
        <v-spacer />
        <v-btn variant="outlined" color="primary" :loading="printing" autofocus @click="submitPrint">
          {{ t('jobForm.dialogs.printManager.print') }}
        </v-btn>
        <v-btn variant="outlined" :disabled="printing" @click="closeDialog">
          {{ t('jobForm.dialogs.printManager.cancel') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { printJobOrder, uploadJobOrderToDms } from '@/services/jobs'
import { getPaperlessNgxConfigStatus } from '@/services/config'
import type { JobOrderPrintRequest } from '@/types/api'

const props = defineProps<{
  modelValue: boolean
  orderId: string | null
  orderNumber: string
  styleTitles?: string[]
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  printed: []
}>()

const { t } = useI18n({ useScope: 'global' })

const printing = ref(false)
const uploading = ref(false)
const errorMessage = ref('')
const dmsSuccessMessage = ref('')
const paperlessConfigured = ref(false)

const form = reactive({
  layout: 'default',
  noPicture: false,
  noProductDetails: false,
  noRemarks: false,
  selectedWorkflowIndices: [] as number[],
})

const layoutOptions = computed(() => [
  { title: t('jobForm.dialogs.printManager.layoutDefault'), value: 'default' },
])

const workflowItems = computed(() =>
  (props.styleTitles ?? []).map((title, index) => ({
    label: title || t('jobForm.dialogs.printManager.workflowFallback', { n: index + 1 }),
    index,
  })),
)

const allWorkflowsSelected = computed({
  get() {
    return workflowItems.value.length > 0 && form.selectedWorkflowIndices.length === workflowItems.value.length
  },
  set(value: boolean) {
    if (value) {
      form.selectedWorkflowIndices = workflowItems.value.map((_, i) => i)
    } else {
      form.selectedWorkflowIndices = []
    }
  },
})

watch(
  () => props.modelValue,
  (open) => {
    if (open) {
      resetForm()
      void refreshPaperlessConfig()
    }
  },
)

async function refreshPaperlessConfig() {
  try {
    const status = await getPaperlessNgxConfigStatus()
    paperlessConfigured.value = status.configured
  } catch {
    paperlessConfigured.value = false
  }
}

function resetForm() {
  errorMessage.value = ''
  dmsSuccessMessage.value = ''
  form.layout = 'default'
  form.noPicture = false
  form.noProductDetails = false
  form.noRemarks = false
  form.selectedWorkflowIndices = workflowItems.value.map((_, i) => i)
}

async function submitPrint() {
  if (!props.orderId) {
    return
  }

  errorMessage.value = ''
  printing.value = true

  try {
    const request: JobOrderPrintRequest = {
      layout: form.layout,
      noPicture: form.noPicture,
      noProductDetails: form.noProductDetails,
      noRemarks: form.noRemarks,
      selectedWorkflowIndices: form.selectedWorkflowIndices,
    }

    const blob = await printJobOrder(props.orderId, request)
    const objectUrl = URL.createObjectURL(blob)
    const anchor = document.createElement('a')
    anchor.href = objectUrl
    anchor.target = '_blank'
    anchor.rel = 'noopener noreferrer'
    document.body.appendChild(anchor)
    anchor.click()
    document.body.removeChild(anchor)
    window.setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000)
    emit('printed')
    closeDialog()
  } catch {
    errorMessage.value = t('jobForm.messages.printFailed')
  } finally {
    printing.value = false
  }
}

async function submitUploadToDms() {
  if (!props.orderId) {
    return
  }

  errorMessage.value = ''
  dmsSuccessMessage.value = ''
  uploading.value = true

  try {
    const request: JobOrderPrintRequest = {
      layout: form.layout,
      noPicture: form.noPicture,
      noProductDetails: form.noProductDetails,
      noRemarks: form.noRemarks,
      selectedWorkflowIndices: form.selectedWorkflowIndices,
    }

    const result = await uploadJobOrderToDms(props.orderId, request)
    if (result.alreadyExists) {
      errorMessage.value = t('jobForm.messages.dmsAlreadyExists', { title: result.title })
    } else {
      dmsSuccessMessage.value = t('jobForm.messages.dmsUploadSuccess', { title: result.title })
    }
  } catch {
    errorMessage.value = t('jobForm.messages.dmsUploadFailed')
  } finally {
    uploading.value = false
  }
}

function closeDialog() {
  emit('update:modelValue', false)
}

function onDialogVisibilityChanged(value: boolean) {
  emit('update:modelValue', value)
}
</script>

<style scoped>
.job-order-print-manager-dialog {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
}
</style>
