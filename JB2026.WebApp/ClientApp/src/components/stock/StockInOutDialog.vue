<template>
  <v-dialog
    :model-value="modelValue"
    max-width="480"
    persistent
    @update:model-value="onDialogVisibilityChanged"
  >
    <v-card v-draggable-dialog class="stock-in-out-dialog">
      <v-card-title class="d-flex align-center ga-2">
        <div class="text-h6">{{ t('stock.stockInOut.title') }}</div>
        <v-spacer />
        <v-btn icon="mdi-close" variant="text" @click="closeDialog" />
      </v-card-title>

      <v-divider />

      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>

        <v-form @submit.prevent>
          <v-row dense>
            <v-col cols="12">
              <v-text-field
                :model-value="props.stockNumber"
                :label="t('stock.stockInOut.stockNumber')"
                density="comfortable"
                variant="solo-filled"
                readonly
                hide-details
              />
            </v-col>

            <v-col cols="12">
              <v-text-field
                v-model="form.inOutDate"
                :label="t('stock.stockInOut.date')"
                type="date"
                density="comfortable"
                variant="outlined"
                hide-details="auto"
                :error-messages="errors.inOutDate"
              />
            </v-col>

            <v-col cols="12">
              <v-text-field
                v-model="form.reference"
                :label="t('stock.stockInOut.reference')"
                density="comfortable"
                variant="outlined"
                hide-details
                maxlength="100"
              />
            </v-col>

            <v-col cols="12">
              <v-text-field
                ref="qtyField"
                v-model="form.qty"
                :label="t('stock.stockInOut.qty')"
                :hint="t('stock.stockInOut.qtyHint')"
                density="comfortable"
                variant="outlined"
                hide-details="auto"
                :error-messages="errors.qty"
                @keydown.enter="save(true)"
              />
            </v-col>
          </v-row>
        </v-form>
      </v-card-text>

      <v-divider />

      <v-card-actions class="px-4 py-3 ga-2">
        <v-spacer />
        <v-btn variant="outlined" :loading="saving" @click="save(false)">
          {{ t('stock.stockInOut.save') }}
        </v-btn>
        <v-btn color="primary" :loading="saving" @click="save(true)">
          {{ t('stock.stockInOut.saveClose') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { nextTick, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { createStockInOutTransaction } from '@/services/stock'
import type { StockInOutTransactionResult } from '@/types/api'

const props = defineProps<{
  modelValue: boolean
  productId: string | null
  stockNumber: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  saved: [result: StockInOutTransactionResult]
  close: []
}>()

const { t } = useI18n({ useScope: 'global' })

const qtyField = ref()
const saving = ref(false)
const errorMessage = ref('')

const form = reactive({
  inOutDate: '',
  reference: '',
  qty: '',
})

const errors = reactive<Record<string, string[]>>({
  inOutDate: [],
  qty: [],
})

watch(
  () => props.modelValue,
  (open) => {
    if (open) {
      initializeForm()
    }
  },
  { immediate: true },
)

function todayIso(): string {
  const now = new Date()
  const year = now.getFullYear()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function initializeForm() {
  form.inOutDate = todayIso()
  form.reference = ''
  form.qty = ''
  errors.inOutDate = []
  errors.qty = []
  errorMessage.value = ''

  nextTick(() => {
    qtyField.value?.focus?.()
  })
}

function validate(): boolean {
  errors.inOutDate = []
  errors.qty = []
  let valid = true

  if (!form.inOutDate) {
    errors.inOutDate = [t('stock.record.required')]
    valid = false
  }

  const qtyTrimmed = form.qty.trim()
  if (!qtyTrimmed) {
    errors.qty = [t('stock.stockInOut.errors.qtyRequired')]
    valid = false
  } else {
    const parsed = parseInt(qtyTrimmed, 10)
    if (!Number.isInteger(parsed) || String(parsed) !== qtyTrimmed || parsed === 0) {
      errors.qty = [t('stock.stockInOut.errors.qtyInvalid')]
      valid = false
    }
  }

  return valid
}

async function save(closeAfterSave: boolean) {
  if (!validate()) {
    return
  }

  if (!props.productId) {
    errorMessage.value = t('stock.stockInOut.errors.productNotFound')
    return
  }

  const confirmed = window.confirm(
    closeAfterSave ? t('stock.stockInOut.confirmSaveClose') : t('stock.stockInOut.confirmSave'),
  )
  if (!confirmed) {
    return
  }

  saving.value = true
  errorMessage.value = ''
  try {
    const result = await createStockInOutTransaction(props.productId, {
      inOutDate: form.inOutDate,
      reference: form.reference.trim() || undefined,
      qty: parseInt(form.qty.trim(), 10),
    })

    emit('saved', result)

    if (closeAfterSave) {
      closeDialog()
    } else {
      form.qty = ''
      errors.qty = []
      nextTick(() => qtyField.value?.focus?.())
    }
  } catch (error) {
    const apiError = error as { response?: { status?: number; data?: { errors?: Record<string, string[]> } } }
    if (apiError?.response?.status === 400 && apiError.response.data?.errors) {
      const messages = Object.values(apiError.response.data.errors).flat()
      errorMessage.value = messages[0] ?? t('stock.stockInOut.errors.saveFailed')
    } else if (apiError?.response?.status === 404) {
      errorMessage.value = t('stock.stockInOut.errors.productNotFound')
    } else {
      errorMessage.value = t('stock.stockInOut.errors.saveFailed')
    }
  } finally {
    saving.value = false
  }
}

function closeDialog() {
  emit('update:modelValue', false)
  emit('close')
}

function onDialogVisibilityChanged(value: boolean) {
  emit('update:modelValue', value)
  if (!value) {
    emit('close')
  }
}
</script>

<style scoped>
.stock-in-out-dialog {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
}
</style>
