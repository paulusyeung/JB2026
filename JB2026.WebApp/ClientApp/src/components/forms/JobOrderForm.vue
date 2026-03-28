<template>
  <v-form ref="formRef" @submit.prevent="handleSubmit">
    <v-card>
      <v-card-title class="pa-6 pb-2">
        <h2 class="text-h5">{{ isNew ? 'New Job Order' : 'Edit Job Order' }}</h2>
        <p class="text-body-2 text-medium-emphasis mt-1 mb-0">
          Vuetify 3 form controls replacing the legacy DevExpress form layout.
        </p>
      </v-card-title>

      <v-card-text class="pa-6">
        <!-- Row 1: Order title + customer name -->
        <v-row dense>
          <v-col cols="12" md="8">
            <v-text-field
              v-model="draft.orderTitle"
              label="Order Title"
              variant="outlined"
              :rules="[required]"
              density="comfortable"
            />
          </v-col>
          <v-col cols="12" md="4">
            <v-select
              v-model="draft.status"
              label="Status"
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
              label="Customer Name"
              variant="outlined"
              :rules="[required]"
              density="comfortable"
            />
          </v-col>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="draft.customerRef"
              label="Customer Reference"
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
              label="Ordered By"
              variant="outlined"
              density="comfortable"
            />
          </v-col>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model.number="draft.qty"
              label="Quantity"
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
              label="Ordered On"
              type="date"
              variant="outlined"
              density="comfortable"
              :rules="[required]"
            />
          </v-col>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="draft.requiredOn"
              label="Required On"
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
              label="Payment Terms"
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
              label="Remarks"
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
        <v-btn variant="text" :disabled="saving" @click="emit('cancel')">Cancel</v-btn>
        <v-btn color="primary" type="submit" :loading="saving" min-width="120">
          {{ isNew ? 'Create' : 'Save Changes' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-form>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
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
const statusOptions = [
  { value: 0, label: 'Draft' },
  { value: 1, label: 'In Progress' },
  { value: 2, label: 'On Hold' },
  { value: 3, label: 'Completed' },
  { value: 4, label: 'Cancelled' },
]

const paymentTermsOptions = [
  'Net 7',
  'Net 14',
  'Net 30',
  'Net 60',
  'Cash on Delivery',
  'Prepaid',
]

// ---------------------------------------------------------------------------
// Validation rules
// ---------------------------------------------------------------------------
const required = (v: string | number) => (v !== '' && v !== null && v !== undefined) || 'Required'

const positiveNumber = (v: number) => v >= 0 || 'Must be 0 or greater'

const requiredAfterOrdered = (v: string) => {
  if (!v || !draft.value.orderedOn) return true
  return v >= draft.value.orderedOn || 'Required On must not be before Ordered On'
}

// ---------------------------------------------------------------------------
// Actions
// ---------------------------------------------------------------------------
function buildDraft(job: JobDetail | null): JobOrderFormData {
  if (!job) {
    const today = new Date().toISOString().slice(0, 10)
    return {
      orderId: null,
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

  return {
    orderId: job.orderId,
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

async function handleSubmit() {
  const { valid } = await formRef.value!.validate()
  if (!valid) return

  saving.value = true
  errorMessage.value = ''

  try {
    await saveJob(draft.value)
    emit('saved')
  } catch {
    errorMessage.value = 'Save failed — verify the API is reachable and try again.'
  } finally {
    saving.value = false
  }
}
</script>
