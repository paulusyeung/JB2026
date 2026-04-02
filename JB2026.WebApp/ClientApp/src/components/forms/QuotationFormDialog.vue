<template>
  <v-form ref="formRef" @submit.prevent="handleSubmit">
    <v-card>
      <v-card-title class="pa-6 pb-2">
        <h2 class="text-h5">{{ isNew ? t('quotations.form.newTitle') : t('quotations.form.editTitle') }}</h2>
        <p class="text-body-2 text-medium-emphasis mt-1 mb-0">
          {{ t('quotations.form.subtitle') }}
        </p>
      </v-card-title>

      <v-card-text class="pa-6">
        <v-row dense>
          <v-col cols="12" sm="4">
            <v-text-field
              v-model.number="draft.quoteNumber"
              :label="t('quotations.form.fields.quoteNumber')"
              type="number"
              min="1"
              variant="outlined"
              density="comfortable"
              :rules="[required, positiveNumber]"
            />
          </v-col>
          <v-col cols="12" sm="4">
            <v-text-field
              v-model.number="draft.quoteNumberIndex"
              :label="t('quotations.form.fields.quoteNumberIndex')"
              type="number"
              min="1"
              variant="outlined"
              density="comfortable"
              :rules="[required, positiveNumber]"
            />
          </v-col>
          <v-col cols="12" sm="4">
            <v-text-field
              :model-value="computedQuotePair"
              :label="t('quotations.form.fields.quote')"
              variant="outlined"
              density="comfortable"
              readonly
            />
          </v-col>
        </v-row>

        <v-row dense>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="draft.customerName"
              :label="t('quotations.form.fields.customerName')"
              variant="outlined"
              density="comfortable"
              :rules="[required]"
            />
          </v-col>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="draft.printTitle"
              :label="t('quotations.form.fields.printTitle')"
              variant="outlined"
              density="comfortable"
            />
          </v-col>
        </v-row>

        <v-row dense>
          <v-col cols="12" sm="4">
            <v-text-field
              v-model="draft.quotedOn"
              :label="t('quotations.form.fields.quotedOn')"
              type="date"
              variant="outlined"
              density="comfortable"
              :rules="[required]"
            />
          </v-col>
          <v-col cols="12" sm="4">
            <v-text-field
              v-model="draft.quotedBy"
              :label="t('quotations.form.fields.quotedBy')"
              variant="outlined"
              density="comfortable"
              :rules="[required]"
            />
          </v-col>
          <v-col cols="12" sm="4">
            <v-text-field
              v-model.number="draft.status"
              :label="t('quotations.form.fields.status')"
              type="number"
              min="0"
              variant="outlined"
              density="comfortable"
              :rules="[required]"
            />
          </v-col>
        </v-row>

        <v-row dense>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model.number="draft.totalCostA"
              :label="t('quotations.form.fields.total')"
              type="number"
              min="0"
              step="0.01"
              variant="outlined"
              density="comfortable"
              :rules="[nonNegativeNumber]"
            />
          </v-col>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model.number="draft.unitCostA"
              :label="t('quotations.form.fields.unit')"
              type="number"
              min="0"
              step="0.0001"
              variant="outlined"
              density="comfortable"
              :rules="[nonNegativeNumber]"
            />
          </v-col>
        </v-row>
      </v-card-text>

      <v-divider />

      <v-alert v-if="errorMessage" type="error" variant="tonal" class="mx-6 mb-2">
        {{ errorMessage }}
      </v-alert>

      <v-card-actions class="pa-4 d-flex ga-2">
        <v-spacer />
        <v-btn variant="text" :disabled="saving" @click="emit('cancel')">{{ t('quotations.form.actions.cancel') }}</v-btn>
        <v-btn color="primary" type="submit" :loading="saving" min-width="120">
          {{ isNew ? t('quotations.form.actions.create') : t('quotations.form.actions.save') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-form>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { VForm } from 'vuetify/components'
import { createQuotation, updateQuotation } from '@/services/quotations'
import type { QuotationListItem } from '@/types/api'

type QuotationFormData = {
  headerId: string | null
  quoteNumber: number
  quoteNumberIndex: number
  customerName: string
  printTitle: string
  quotedOn: string
  quotedBy: string
  totalCostA: number
  unitCostA: number
  status: number
  createdOn: string | null
  createdBy: string | null
  modifiedOn: string | null
  modifiedBy: string | null
}

const props = defineProps<{
  quotation: QuotationListItem | null
}>()

const emit = defineEmits<{
  (e: 'saved', quotation: QuotationListItem): void
  (e: 'cancel'): void
}>()

const { t } = useI18n({ useScope: 'global' })
const formRef = ref<InstanceType<typeof VForm> | null>(null)
const saving = ref(false)
const errorMessage = ref('')

const isNew = computed(() => props.quotation === null)
const draft = ref<QuotationFormData>(buildDraft(props.quotation))

const computedQuotePair = computed(() => `${draft.value.quoteNumber}-${draft.value.quoteNumberIndex}`)

watch(
  () => props.quotation,
  (quotation) => {
    draft.value = buildDraft(quotation)
    errorMessage.value = ''
  },
)

function buildDraft(quotation: QuotationListItem | null): QuotationFormData {
  if (!quotation) {
    return {
      headerId: null,
      quoteNumber: 0,
      quoteNumberIndex: 1,
      customerName: '',
      printTitle: '',
      quotedOn: new Date().toISOString().slice(0, 10),
      quotedBy: '',
      totalCostA: 0,
      unitCostA: 0,
      status: 0,
      createdOn: null,
      createdBy: null,
      modifiedOn: null,
      modifiedBy: null,
    }
  }

  return {
    headerId: quotation.headerId,
    quoteNumber: quotation.quoteNumber,
    quoteNumberIndex: quotation.quoteNumberIndex,
    customerName: quotation.customerName,
    printTitle: quotation.printTitle,
    quotedOn: quotation.quotedOn?.slice(0, 10) ?? new Date().toISOString().slice(0, 10),
    quotedBy: quotation.quotedBy,
    totalCostA: quotation.totalCostA,
    unitCostA: quotation.unitCostA,
    status: quotation.status,
    createdOn: quotation.createdOn,
    createdBy: quotation.createdBy,
    modifiedOn: quotation.modifiedOn,
    modifiedBy: quotation.modifiedBy,
  }
}

const required = (v: string | number) => (v !== '' && v !== null && v !== undefined) || t('jobForm.validation.required')
const positiveNumber = (v: number) => v > 0 || t('jobForm.validation.nonNegative')
const nonNegativeNumber = (v: number) => v >= 0 || t('jobForm.validation.nonNegative')

async function handleSubmit() {
  const { valid } = await formRef.value!.validate()
  if (!valid) {
    return
  }

  saving.value = true
  errorMessage.value = ''

  const request = {
    quoteNumber: draft.value.quoteNumber,
    quoteNumberIndex: draft.value.quoteNumberIndex,
    customerName: draft.value.customerName,
    printTitle: draft.value.printTitle,
    quotedOn: draft.value.quotedOn,
    quotedBy: draft.value.quotedBy,
    totalCostA: draft.value.totalCostA,
    unitCostA: draft.value.unitCostA,
    status: draft.value.status,
  }

  try {
    const result = draft.value.headerId === null
      ? await createQuotation(request)
      : await updateQuotation(draft.value.headerId, request)

    emit('saved', result)
  } catch {
    errorMessage.value = t('quotations.form.saveFailed')
  } finally {
    saving.value = false
  }
}
</script>
