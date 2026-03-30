<template>
  <v-form ref="formRef" @submit.prevent="handleSubmit">
    <v-card>
      <v-card-title class="pa-6 pb-2">
        <h2 class="text-h5">{{ isNew ? t('jobForm.newTitle') : t('jobForm.editTitle') }}</h2>
        <p class="text-body-2 text-medium-emphasis mt-1 mb-0">
          {{ t('jobForm.subtitle') }}
        </p>
      </v-card-title>

      <v-card-text class="pa-6">
        <v-row dense>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="draft.orderNumber"
              :label="t('jobForm.fields.orderNumber')"
              variant="outlined"
              density="comfortable"
              :rules="isNew ? [required] : []"
              :readonly="!isNew"
            />
          </v-col>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="draft.jobNumber"
              :label="t('jobForm.fields.jobNumber')"
              variant="outlined"
              density="comfortable"
              :rules="isNew ? [required] : []"
              :readonly="!isNew"
            />
          </v-col>
        </v-row>

        <!-- Row 1: Order title + customer name -->
        <v-row dense>
          <v-col cols="12" md="8">
            <v-text-field
              v-model="draft.orderTitle"
              :label="t('jobForm.fields.orderTitle')"
              variant="outlined"
              :rules="[required]"
              density="comfortable"
            />
          </v-col>
          <v-col cols="12" md="4">
            <v-select
              v-model="draft.status"
              :label="t('jobForm.fields.status')"
              :items="statusOptions"
              item-title="label"
              item-value="value"
              variant="outlined"
              density="comfortable"
            />
          </v-col>
        </v-row>

        <!-- Row 2: Customer name + reference -->
        <v-row dense>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="draft.customerName"
              :label="t('jobForm.fields.customerName')"
              variant="outlined"
              :rules="[required]"
              density="comfortable"
            />
          </v-col>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="draft.customerRef"
              :label="t('jobForm.fields.customerReference')"
              variant="outlined"
              density="comfortable"
            />
          </v-col>
        </v-row>

        <!-- Row 3: Ordered by + qty -->
        <v-row dense>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="draft.orderedBy"
              :label="t('jobForm.fields.orderedBy')"
              variant="outlined"
              density="comfortable"
            />
          </v-col>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model.number="draft.qty"
              :label="t('jobForm.fields.quantity')"
              type="number"
              min="0"
              step="1"
              variant="outlined"
              density="comfortable"
              :rules="[positiveNumber]"
            />
          </v-col>
        </v-row>

        <!-- Row 4: Dates -->
        <v-row dense>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="draft.orderedOn"
              :label="t('jobForm.fields.orderedOn')"
              type="date"
              variant="outlined"
              density="comfortable"
              :rules="[required]"
            />
          </v-col>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="draft.requiredOn"
              :label="t('jobForm.fields.requiredOn')"
              type="date"
              variant="outlined"
              density="comfortable"
              :rules="[required, requiredAfterOrdered]"
            />
          </v-col>
        </v-row>

        <!-- Row 5: Payment terms -->
        <v-row dense>
          <v-col cols="12">
            <v-select
              v-model="draft.paymentTerms"
              :label="t('jobForm.fields.paymentTerms')"
              :items="paymentTermsOptions"
              variant="outlined"
              density="comfortable"
              clearable
            />
          </v-col>
        </v-row>

        <!-- Row 6: Remarks -->
        <v-row dense>
          <v-col cols="12">
            <v-textarea
              v-model="draft.remarks"
              :label="t('jobForm.fields.remarks')"
              variant="outlined"
              rows="4"
              auto-grow
              density="comfortable"
            />
          </v-col>
        </v-row>

        <v-alert v-if="errorMessage" type="error" variant="tonal" class="mt-2">
          {{ errorMessage }}
        </v-alert>
      </v-card-text>

      <v-divider />

      <v-card-actions class="pa-4 d-flex ga-2">
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
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { VForm } from 'vuetify/components'
import { saveJob } from '@/services/jobs'
import type { JobDetail, JobOrderFormData } from '@/types/api'

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
}>()

// ---------------------------------------------------------------------------
// Local state
// ---------------------------------------------------------------------------
const formRef = ref<InstanceType<typeof VForm> | null>(null)
const saving = ref(false)
const errorMessage = ref('')
const { t } = useI18n({ useScope: 'global' })

const isNew = computed(() => props.job === null)

const draft = ref<JobOrderFormData>(buildDraft(props.job))

watch(
  () => props.job,
  (job) => {
    draft.value = buildDraft(job)
    errorMessage.value = ''
  },
)

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

// ---------------------------------------------------------------------------
// Validation rules
// ---------------------------------------------------------------------------
const required = (v: string | number) => (v !== '' && v !== null && v !== undefined) || t('jobForm.validation.required')

const positiveNumber = (v: number) => v >= 0 || t('jobForm.validation.nonNegative')

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
</script>
