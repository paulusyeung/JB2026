<template>
  <v-dialog
    :model-value="modelValue"
    max-width="1100"
    persistent
    scrollable
    @update:model-value="onDialogVisibilityChanged"
  >
    <v-card v-draggable-dialog class="product-record-dialog">
      <v-card-title class="d-flex flex-wrap align-center ga-2">
        <div class="text-h6">
          {{ isEditMode ? t('stock.record.titleEdit') : t('stock.record.titleCreate') }}
        </div>
        <v-chip size="small" color="primary" variant="tonal">{{ isEditMode ? t('stock.record.modeEdit') : t('stock.record.modeCreate') }}</v-chip>
        <v-spacer />
        <v-btn icon="mdi-close" variant="text" @click="closeDialog" />
      </v-card-title>

      <v-divider />

      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>
        <v-alert v-if="infoMessage" type="info" variant="tonal" class="mb-3">{{ infoMessage }}</v-alert>

        <v-form @submit.prevent>
          <v-row class="mb-3" dense>
            <v-col cols="12" lg="6">
              <v-card variant="tonal" class="record-section-card h-100">
                <v-card-title class="text-subtitle-1">{{ t('stock.record.identity') }}</v-card-title>
                <v-card-text>
                  <v-row dense align="end">
                    <v-col cols="12" class="text-caption text-medium-emphasis pb-0">
                      {{ t('stock.record.stockNumber') }}
                    </v-col>
                    <v-col cols="3" sm="3">
                      <v-combobox
                        ref="customerCodeField"
                        v-model="form.customerCode"
                        :label="t('stock.record.customerCode')"
                        :items="customerCodeOptions"
                        maxlength="3"
                        density="comfortable"
                        variant="outlined"
                        hide-details="auto"
                        :disabled="isEditMode"
                        :error-messages="errors.customerCode"
                        @update:model-value="val => form.customerCode = String(val ?? '').toUpperCase()"
                      />
                    </v-col>
                    <v-col cols="3" sm="3">
                      <v-combobox
                        v-model="form.categoryCode"
                        :label="t('stock.record.categoryCode')"
                        :items="categoryCodeOptions"
                        maxlength="3"
                        density="comfortable"
                        variant="outlined"
                        hide-details="auto"
                        :error-messages="errors.categoryCode"
                        @update:model-value="val => form.categoryCode = String(val ?? '').toUpperCase()"
                      />
                    </v-col>
                    <v-col cols="6" sm="6">
                      <v-text-field
                        v-model="form.sequenceNumber"
                        :label="t('stock.record.sequenceNumber')"
                        maxlength="8"
                        density="comfortable"
                        variant="outlined"
                        :disabled="isEditMode"
                        hide-details="auto"
                        :error-messages="errors.sequenceNumber"
                      >
                        <template #append-inner>
                          <v-tooltip :text="t('stock.record.nextNumber')" location="top">
                            <template #activator="{ props: tooltipProps }">
                              <v-btn
                                v-bind="tooltipProps"
                                icon
                                size="x-small"
                                variant="tonal"
                                :loading="generatingNumber"
                                :disabled="isEditMode"
                                @click="requestNextNumber"
                              >
                                <v-icon>mdi-counter</v-icon>
                              </v-btn>
                            </template>
                          </v-tooltip>
                        </template>
                      </v-text-field>
                    </v-col>
                    <v-col cols="12" class="py-3">
                      <v-text-field
                        :model-value="composedStockNumber"
                        :label="t('stock.record.stockNumberComposed')"
                        density="comfortable"
                        variant="solo-filled"
                        readonly
                        hide-details
                      />
                    </v-col>
                    <v-col cols="12">
                      <v-text-field
                        v-model="form.productCode"
                        :label="t('stock.record.productCode')"
                        density="comfortable"
                        variant="outlined"
                        :error-messages="errors.productCode"
                      />
                    </v-col>
                  </v-row>
                </v-card-text>
              </v-card>
            </v-col>

            <v-col cols="12" lg="6">
              <v-card variant="tonal" class="record-section-card h-100">
                <v-card-title class="text-subtitle-1">{{ t('stock.record.details') }}</v-card-title>
                <v-card-text>
                  <v-row>
                    <v-col cols="12">
                      <v-text-field
                        v-model="form.productName"
                        :label="t('stock.record.productName')"
                        density="comfortable"
                        variant="outlined"
                        :error-messages="errors.productName"
                      />
                    </v-col>
                    <v-col cols="12" md="6">
                      <v-textarea
                        v-model="form.productionInfo"
                        :label="t('stock.record.productionInfo')"
                        density="comfortable"
                        variant="outlined"
                        rows="2"
                        max-rows="4"
                        auto-grow
                      />
                    </v-col>
                    <v-col cols="12" md="6">
                      <v-textarea
                        v-model="form.remarks"
                        :label="t('stock.record.remarks')"
                        density="comfortable"
                        variant="outlined"
                        rows="2"
                        max-rows="4"
                        auto-grow
                      />
                    </v-col>
                    <v-col cols="12" md="4">
                      <v-text-field
                        v-model.number="form.sellingPrice"
                        :label="t('stock.record.sellingPrice')"
                        type="number"
                        density="comfortable"
                        variant="outlined"
                      />
                    </v-col>
                    <v-col cols="12" md="4">
                      <v-text-field
                        v-model.number="form.cogs"
                        :label="t('stock.record.cogs')"
                        type="number"
                        density="comfortable"
                        variant="outlined"
                      />
                    </v-col>
                    <v-col cols="12" md="4">
                      <v-text-field
                        :model-value="String(form.balance)"
                        :label="t('stock.record.balance')"
                        density="comfortable"
                        variant="solo-filled"
                        readonly
                      />
                    </v-col>
                  </v-row>
                </v-card-text>
              </v-card>
            </v-col>
          </v-row>

          <v-card v-if="isEditMode" variant="tonal">
            <v-card-title class="text-subtitle-1">{{ t('stock.record.movementHistory') }}</v-card-title>
            <v-card-text>
              <v-data-table
                :headers="movementHeaders"
                :items="movementRows"
                :sort-by="movementSortBy"
                density="compact"
                :items-per-page="10"
                :loading="loadingMovements"
                fixed-header
                height="360"
                class="movement-table"
              >
                <template #[`item.inOutDate`]="{ item }">{{ format(item.inOutDate) }}</template>
                <template #[`item.qty`]="{ item }">{{ formatNumber(item.qty) }}</template>
                <template #[`item.runningBalance`]="{ item }">{{ formatNumber(item.runningBalance) }}</template>
              </v-data-table>
            </v-card-text>
          </v-card>
        </v-form>
      </v-card-text>

      <v-divider />

      <v-card-actions class="toolbar-actions">
        <v-btn variant="outlined" prepend-icon="mdi-paperclip" :disabled="!isEditMode" @click="openAttachmentDialog">
          {{ t('stock.actions.attachment') }}
        </v-btn>
        <v-btn variant="outlined" prepend-icon="mdi-swap-horizontal" :disabled="!isEditMode" @click="openStockInOutDialog">
          {{ t('stock.actions.stockInOut') }}
        </v-btn>
        <v-btn
          variant="outlined"
          prepend-icon="mdi-printer"
          :disabled="!isEditMode"
          :loading="printing"
          @click="printRecord"
        >
          {{ t('stock.record.print') }}
        </v-btn>
        <v-btn variant="outlined" prepend-icon="mdi-file-delimited-outline" @click="showGatedAction('stock.actions.export')">
          {{ t('stock.actions.export') }}
        </v-btn>

        <v-spacer />

        <v-btn color="primary" :loading="saving" @click="save(false)">{{ t('stock.record.save') }}</v-btn>
        <v-btn color="primary" variant="tonal" :loading="saving" @click="save(true)">{{ t('stock.record.saveClose') }}</v-btn>
        <v-btn
          v-if="isEditMode"
          color="error"
          variant="outlined"
          :loading="deleting"
          @click="deleteRecord"
        >
          {{ t('stock.record.delete') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <stock-in-out-dialog
    v-model="stockInOutDialogOpen"
    :product-id="currentProductId"
    :stock-number="composedStockNumber"
    @saved="onStockInOutSaved"
  />

  <stock-attachment-dialog
    v-model="stockAttachmentDialogOpen"
    :product-id="currentProductId"
    :stock-number="composedStockNumber"
    :can-delete="canDeleteAttachments"
    @changed="onAttachmentChanged"
  />
</template>

<script setup lang="ts">
import { computed, nextTick, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { useSessionStore } from '@/stores/session'
import {
  createProductRecord,
  deleteProductRecord,
  getNextProductNumber,
  getProductRecord,
  getProductStockMovements,
  printProductRecord,
  updateProductRecord,
  validateProductCodeUniqueness,
} from '@/services/stock'
import StockInOutDialog from '@/components/stock/StockInOutDialog.vue'
import StockAttachmentDialog from '@/components/stock/StockAttachmentDialog.vue'
import type { StockInOutTransactionResult, StockProductMovementHistoryItem, StockProductRecordUpsertRequest } from '@/types/api'

type ProductRecordMode = 'create' | 'edit'
type MovementHistoryRow = StockProductMovementHistoryItem & { rowNumber: number }

const props = defineProps<{
  modelValue: boolean
  mode: ProductRecordMode
  productId: string | null
  customerCodeOptions?: string[]
  categoryCodeOptions?: string[]
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  saved: [productId: string]
  deleted: [productId: string, outcome: string]
  close: []
}>()

const { t } = useI18n({ useScope: 'global' })
const { format } = useGlobalDateFormatter()
const sessionStore = useSessionStore()

const customerCodeField = ref()
const loading = ref(false)
const loadingMovements = ref(false)
const generatingNumber = ref(false)
const saving = ref(false)
const deleting = ref(false)
const printing = ref(false)
const errorMessage = ref('')
const infoMessage = ref('')
const movementRows = ref<MovementHistoryRow[]>([])
const movementSortBy = ref([{ key: 'inOutDate', order: 'desc' as const }])
const stockInOutDialogOpen = ref(false)
const stockAttachmentDialogOpen = ref(false)

const currentMode = ref<ProductRecordMode>('create')
const currentProductId = ref<string | null>(null)
const originalProductCode = ref('')

const form = reactive({
  customerCode: '',
  categoryCode: '',
  sequenceNumber: '',
  productCode: '',
  productName: '',
  productionInfo: '',
  remarks: '',
  sellingPrice: 0,
  cogs: 0,
  balance: 0,
})

const errors = reactive<Record<string, string[]>>({
  customerCode: [],
  categoryCode: [],
  sequenceNumber: [],
  productCode: [],
  productName: [],
})

const isEditMode = computed(() => currentMode.value === 'edit' && !!currentProductId.value)
const canDeleteAttachments = computed(() => {
  const rawRole = sessionStore.profile?.role
  const normalizedRole = String(rawRole ?? '').toLowerCase().trim()
  return normalizedRole === 'admin' || normalizedRole === '4'
})

const composedStockNumber = computed(() => {
  const customer = form.customerCode.trim()
  const category = form.categoryCode.trim()
  const sequence = form.sequenceNumber.trim()

  return [customer, category, sequence].filter((segment) => segment.length > 0).join('-')
})

const movementHeaders = computed(() => [
  { title: '#', key: 'rowNumber', sortable: false, align: 'end' as const, width: 64 },
  { title: t('stock.record.movementDate'), key: 'inOutDate' },
  { title: t('stock.record.reference'), key: 'reference' },
  { title: t('stock.record.quantity'), key: 'qty', align: 'end' as const },
  { title: t('stock.record.runningBalance'), key: 'runningBalance', align: 'end' as const },
  { title: t('stock.record.modifiedOn'), key: 'modifiedOn' },
  { title: t('stock.record.modifiedBy'), key: 'modifiedBy' },
])

function formatNumber(value: number | undefined | null): string {
  if (value == null) return ''
  return value.toLocaleString()
}

watch(
  () => [props.modelValue, props.mode, props.productId],
  async ([open]) => {
    if (!open) {
      return
    }

    await initializeDialog()
  },
  { immediate: true },
)

async function initializeDialog() {
  clearMessages()
  clearErrors()
  currentMode.value = props.mode
  currentProductId.value = props.productId

  if (currentMode.value === 'edit' && currentProductId.value) {
    await loadExistingRecord(currentProductId.value)
    await loadMovements(currentProductId.value)
  } else {
    resetForm()
    movementRows.value = []
  }

  await nextTick()
  customerCodeField.value?.focus?.()
}

function resetForm() {
  form.customerCode = ''
  form.categoryCode = ''
  form.sequenceNumber = ''
  form.productCode = ''
  form.productName = ''
  form.productionInfo = ''
  form.remarks = ''
  form.sellingPrice = 0
  form.cogs = 0
  form.balance = 0
  originalProductCode.value = ''
}

function clearMessages() {
  errorMessage.value = ''
  infoMessage.value = ''
}

function clearErrors() {
  errors.customerCode = []
  errors.categoryCode = []
  errors.sequenceNumber = []
  errors.productCode = []
  errors.productName = []
}

function splitStockNumber(stockNumber: string) {
  const normalized = (stockNumber || '').trim().replace(/-/g, '')
  return {
    customerCode: normalized.slice(0, 3),
    categoryCode: normalized.slice(3, 6),
    sequenceNumber: normalized.slice(6),
  }
}

async function loadExistingRecord(productId: string) {
  loading.value = true
  try {
    const record = await getProductRecord(productId)
    const parsed = splitStockNumber(record.stockNumber)

    form.customerCode = parsed.customerCode || record.customerCode
    form.categoryCode = parsed.categoryCode || record.categoryCode
    form.sequenceNumber = parsed.sequenceNumber || record.sequenceNumber
    form.productCode = record.productCode
    form.productName = record.productName
    form.productionInfo = record.productionInfo
    form.remarks = record.remarks
    form.sellingPrice = record.sellingPrice
    form.cogs = record.cogs
    form.balance = record.balance

    originalProductCode.value = record.productCode
  } catch {
    errorMessage.value = t('stock.record.loadFailed')
  } finally {
    loading.value = false
  }
}

async function loadMovements(productId: string) {
  loadingMovements.value = true
  try {
    errorMessage.value = ''
    const rows = await getProductStockMovements(productId)
    const sortedRows = [...rows].sort((a, b) => {
      const inOutDateDelta = new Date(b.inOutDate).getTime() - new Date(a.inOutDate).getTime()
      if (inOutDateDelta !== 0) {
        return inOutDateDelta
      }

      return new Date(b.modifiedOn).getTime() - new Date(a.modifiedOn).getTime()
    })

    movementRows.value = sortedRows.map((row, index) => ({
      ...row,
      rowNumber: index + 1,
    }))
  } catch {
    movementRows.value = []
    errorMessage.value = t('stock.record.loadFailed')
  } finally {
    loadingMovements.value = false
  }
}

async function requestNextNumber() {
  if (!form.customerCode.trim() || !form.categoryCode.trim()) {
    errors.customerCode = form.customerCode.trim() ? [] : [t('stock.record.required')]
    errors.categoryCode = form.categoryCode.trim() ? [] : [t('stock.record.required')]
    return
  }

  generatingNumber.value = true
  try {
    const next = await getNextProductNumber(form.customerCode, form.categoryCode)
    form.sequenceNumber = next.sequenceNumber
  } catch {
    errorMessage.value = t('stock.record.nextNumberFailed')
  } finally {
    generatingNumber.value = false
  }
}

function validateRequiredFields() {
  clearErrors()

  errors.customerCode = form.customerCode.trim() ? [] : [t('stock.record.required')]
  errors.categoryCode = form.categoryCode.trim() ? [] : [t('stock.record.required')]
  errors.sequenceNumber = form.sequenceNumber.trim() ? [] : [t('stock.record.required')]
  errors.productCode = form.productCode.trim() ? [] : [t('stock.record.required')]
  errors.productName = form.productName.trim() ? [] : [t('stock.record.required')]

  return Object.values(errors).every((bucket) => bucket.length === 0)
}

async function validateUniqueness() {
  const nextCode = form.productCode.trim()
  if (!nextCode) {
    return false
  }

  const shouldCheck = currentMode.value === 'create' || nextCode !== originalProductCode.value
  if (!shouldCheck) {
    return true
  }

  try {
    const isUnique = await validateProductCodeUniqueness(nextCode, currentProductId.value ?? undefined)
    if (!isUnique) {
      errors.productCode = [t('stock.record.uniqueCode')]
      return false
    }

    return true
  } catch {
    errorMessage.value = t('stock.record.validationFailed')
    return false
  }
}

function buildPayload(): StockProductRecordUpsertRequest {
  return {
    customerCode: form.customerCode.trim(),
    categoryCode: form.categoryCode.trim(),
    sequenceNumber: form.sequenceNumber.trim(),
    productCode: form.productCode.trim(),
    productName: form.productName.trim(),
    productionInfo: form.productionInfo.trim(),
    remarks: form.remarks.trim(),
    sellingPrice: Number(form.sellingPrice) || 0,
    cogs: Number(form.cogs) || 0,
  }
}

async function save(closeAfterSave: boolean) {
  clearMessages()

  if (!validateRequiredFields()) {
    return
  }

  const isUnique = await validateUniqueness()
  if (!isUnique) {
    return
  }

  const confirmed = window.confirm(closeAfterSave ? t('stock.record.confirmSaveClose') : t('stock.record.confirmSave'))
  if (!confirmed) {
    return
  }

  saving.value = true
  try {
    const payload = buildPayload()

    if (isEditMode.value && currentProductId.value) {
      const updated = await updateProductRecord(currentProductId.value, payload)
      originalProductCode.value = updated.productCode
      await loadMovements(updated.productId)
      emit('saved', updated.productId)
      if (closeAfterSave) {
        closeDialog()
      }
      return
    }

    const created = await createProductRecord(payload)
    currentMode.value = 'edit'
    currentProductId.value = created.productId
    originalProductCode.value = created.productCode
    form.balance = created.balance
    emit('saved', created.productId)

    await loadMovements(created.productId)

    if (closeAfterSave) {
      closeDialog()
    }
  } catch {
    errorMessage.value = t('stock.record.saveFailed')
  } finally {
    saving.value = false
  }
}

async function deleteRecord() {
  if (!currentProductId.value) {
    return
  }

  const confirmed = window.confirm(t('stock.record.confirmDelete'))
  if (!confirmed) {
    return
  }

  deleting.value = true
  try {
    const result = await deleteProductRecord(currentProductId.value)
    emit('deleted', currentProductId.value, result.outcome)
    closeDialog()
  } catch {
    errorMessage.value = t('stock.record.deleteFailed')
  } finally {
    deleting.value = false
  }
}

function showGatedAction(actionKey: string) {
  infoMessage.value = t('stock.messages.actionUnavailable', { action: t(actionKey) })
}

function openStockInOutDialog() {
  if (!isEditMode.value) {
    return
  }
  stockInOutDialogOpen.value = true
}

function openAttachmentDialog() {
  if (!isEditMode.value || !currentProductId.value) {
    errorMessage.value = t('stock.attachments.errors.selectSingleProduct')
    return
  }

  stockAttachmentDialogOpen.value = true
}

async function onAttachmentChanged() {
  if (!currentProductId.value) {
    return
  }

  await loadExistingRecord(currentProductId.value)
  emit('saved', currentProductId.value)
}

async function printRecord() {
  if (!currentProductId.value || printing.value) {
    return
  }

  clearMessages()
  printing.value = true

  try {
    const blob = await printProductRecord(currentProductId.value)
    const objectUrl = URL.createObjectURL(blob)
    const popup = window.open(objectUrl, '_blank', 'noopener,noreferrer')

    if (!popup) {
      infoMessage.value = t('stock.record.printDownloaded')
    } else {
      infoMessage.value = t('stock.record.printOpened')
    }

    window.setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000)
  } catch {
    errorMessage.value = t('stock.record.printFailed')
  } finally {
    printing.value = false
  }
}

async function onStockInOutSaved(_result: StockInOutTransactionResult) {
  if (currentProductId.value) {
    await loadMovements(currentProductId.value)
    const record = await getProductRecord(currentProductId.value)
    form.balance = record.balance
  }
  emit('saved', currentProductId.value!)
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
.product-record-dialog {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
}

.toolbar-actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.movement-table :deep(.v-data-table__td) {
  white-space: nowrap;
}

.record-section-card {
  min-height: 100%;
}

@media (max-width: 800px) {
  .toolbar-actions {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}
</style>
