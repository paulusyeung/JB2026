<template>
  <v-dialog
    :model-value="modelValue"
    :width="dialogSize.width"
    scrollable
    persistent
    content-class="billing-editor-overlay"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card style="height: 100%; position: relative">
      <v-card-title class="d-flex align-center ga-3 pb-1 pt-4 px-4" style="cursor: move; user-select: none" @mousedown="startDrag">
        <span class="text-h6">{{ dialogTitle }}</span>
        <v-chip size="small" variant="tonal" :color="modeBadgeColor" class="ml-1">{{ modeBadge }}</v-chip>
        <v-spacer />
        <v-btn variant="text" icon="mdi-close" size="small" @click="close" />
      </v-card-title>

      <v-divider />

      <v-card-text class="px-4 py-4">
        <v-alert v-if="errorMessage" type="warning" variant="tonal" density="compact" class="mb-4">
          {{ errorMessage }}
        </v-alert>

        <v-progress-linear v-if="loadingDetail" indeterminate color="primary" class="mb-4" />

        <v-form ref="formRef">
          <v-row dense>
            <!-- Client selector -->
            <v-col cols="12" md="6">
              <v-autocomplete
                v-model="form.client"
                v-model:search="clientSearchText"
                :items="clientOptions"
                item-title="displayName"
                item-value="externalClientId"
                return-object
                :label="t('billing.invoices.editor.fields.client')"
                :placeholder="t('billing.invoices.editor.fields.clientPlaceholder')"
                :loading="loadingClients"
                :rules="[rules.clientRequired]"
                no-filter
                clearable
                density="compact"
                variant="outlined"
                @update:search="handleClientSearch"
              />
            </v-col>

            <!-- Invoice date -->
            <v-col cols="12" md="3">
              <v-menu v-model="datePickerOpen" :close-on-content-click="false">
                <template #activator="{ props: menuProps }">
                  <v-text-field
                    :model-value="form.invoiceDate ? format(form.invoiceDate) : ''"
                    :label="t('billing.invoices.editor.fields.invoiceDate')"
                    :rules="[rules.invoiceDateRequired]"
                    density="compact"
                    variant="outlined"
                    readonly
                    append-inner-icon="mdi-calendar"
                    v-bind="menuProps"
                  />
                </template>
                <v-date-picker
                  :model-value="form.invoiceDate ? new Date(form.invoiceDate + 'T12:00:00') : undefined"
                  hide-header
                  @update:model-value="onDatePicked"
                />
              </v-menu>
            </v-col>

            <!-- Job number -->
            <v-col cols="12" md="3">
              <v-text-field
                v-model="form.jobNumber"
                :label="t('billing.invoices.editor.fields.jobNumber')"
                density="compact"
                variant="outlined"
              />
            </v-col>
          </v-row>

          <!-- Line items table -->
          <div class="mt-2">
            <v-table density="compact" class="line-items-table">
              <thead>
                <tr>
                  <th class="text-left" style="min-width: 120px">{{ t('billing.invoices.editor.lineItems.poNumber') }}</th>
                  <th class="text-left" style="min-width: 200px">{{ t('billing.invoices.editor.lineItems.description') }}</th>
                  <th class="text-right" style="width: 90px">{{ t('billing.invoices.editor.lineItems.qty') }}</th>
                  <th class="text-center" style="width: 90px">{{ t('billing.invoices.editor.lineItems.unit') }}</th>
                  <th class="text-right" style="width: 120px">{{ t('billing.invoices.editor.lineItems.unitCost') }}</th>
                  <th class="text-right" style="width: 120px">{{ t('billing.invoices.editor.lineItems.lineTotal') }}</th>
                  <th v-if="!isReadOnly" style="width: 48px"></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(line, idx) in form.lineItems" :key="line.id">
                  <td class="py-1">
                    <v-text-field
                      v-model="line.poNumber"
                      density="compact"
                      variant="outlined"
                      hide-details
                    />
                  </td>
                  <td class="py-1">
                    <v-textarea
                      v-model="line.description"
                      density="compact"
                      variant="outlined"
                      hide-details
                      auto-grow
                      rows="2"
                    />
                  </td>
                  <td class="py-1">
                    <v-text-field
                      v-model="line.qtyStr"
                      inputmode="decimal"
                      density="compact"
                      variant="outlined"
                      hide-details
                      class="text-right"
                      @keydown="allowNumericOnly"
                      @update:model-value="recalcLine(idx)"
                    />
                  </td>
                  <td class="py-1">
                    <v-text-field
                      v-model="line.unit"
                      density="compact"
                      variant="outlined"
                      hide-details
                      class="text-center"
                    />
                  </td>
                  <td class="py-1">
                    <v-text-field
                      v-model="line.unitCostStr"
                      inputmode="decimal"
                      density="compact"
                      variant="outlined"
                      hide-details
                      class="text-right"
                      @keydown="allowNumericOnly"
                      @update:model-value="recalcLine(idx)"
                    />
                  </td>
                  <td class="text-right text-body-2 py-1 pr-2">
                    {{ formatCurrency(line.lineTotal) }}
                  </td>
                  <td v-if="!isReadOnly" class="py-1">
                    <v-btn
                      icon="mdi-delete-outline"
                      size="small"
                      variant="text"
                      color="error"
                      :disabled="form.lineItems.length <= 1"
                      @click="removeLine(idx)"
                    />
                  </td>
                </tr>
              </tbody>
            </v-table>

            <div class="d-flex align-center justify-space-between mt-2 px-1">
              <v-btn
                v-if="!isReadOnly"
                variant="text"
                size="small"
                prepend-icon="mdi-plus"
                @click="addLine"
              >
                {{ t('billing.invoices.editor.lineItems.addLine') }}
              </v-btn>
              <div v-else />
              <div class="text-body-1 font-weight-medium">
                {{ t('billing.invoices.editor.totals.invoiceTotal') }}:
                <span class="ml-2">{{ formatCurrency(invoiceTotal) }}</span>
              </div>
            </div>
          </div>
        </v-form>
      </v-card-text>

      <v-divider />

      <v-card-actions class="px-4 py-3">
        <v-spacer />
        <v-btn variant="text" @click="close">
          {{ isReadOnly ? t('billing.invoices.editor.actions.close') : t('billing.invoices.editor.actions.cancel') }}
        </v-btn>
        <v-btn
          v-if="!isReadOnly"
          color="primary"
          variant="elevated"
          :loading="isSaving"
          @click="handleSave"
        >
          {{ t('billing.invoices.editor.actions.save') }}
        </v-btn>
      </v-card-actions>
      <div class="resize-handle" @mousedown.stop.prevent="startResize" />
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import axios from 'axios'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import {
  listBillingClients,
  getInvoiceEditorDetail,
  createInvoice,
  updateInvoice,
  type BillingClientOption,
  type InvoiceBillingSummary,
} from '@/services/billing'

// ── Props & Emits ─────────────────────────────────────────────────────────────

interface Props {
  modelValue: boolean
  mode: 'create' | 'edit' | 'view'
  externalInvoiceId?: string
}

const props = withDefaults(defineProps<Props>(), {
  externalInvoiceId: undefined,
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'saved', summary: InvoiceBillingSummary): void
}>()

// ── i18n & Formatters ─────────────────────────────────────────────────────────

const { t } = useI18n()
const { formatCurrency } = useLocaleFormatters()
const { format } = useGlobalDateFormatter()

// ── Form State ────────────────────────────────────────────────────────────────

interface FormLineItem {
  id: string
  poNumber: string
  description: string
  qtyStr: string
  unit: string
  unitCostStr: string
  lineTotal: number
}

interface FormState {
  client: BillingClientOption | null
  invoiceDate: string
  jobNumber: string
  lineItems: FormLineItem[]
}

const formRef = ref<{ validate: () => Promise<{ valid: boolean }> } | null>(null)

function emptyLine(): FormLineItem {
  return {
    id: `line-${Date.now()}-${Math.random()}`,
    poNumber: '',
    description: '',
    qtyStr: '1',
    unit: '',
    unitCostStr: '0',
    lineTotal: 0,
  }
}

function resetForm(): FormState {
  return {
    client: null,
    invoiceDate: '',
    jobNumber: '',
    lineItems: [emptyLine()],
  }
}

const form = ref<FormState>(resetForm())

// ── Computed ──────────────────────────────────────────────────────────────────

const isReadOnly = computed(() => props.mode === 'view')

const dialogTitle = computed(() => {
  if (props.mode === 'create') return t('billing.invoices.editor.titleCreate')
  if (props.mode === 'edit') return t('billing.invoices.editor.titleEdit')
  return t('billing.invoices.editor.titleView')
})

const modeBadge = computed(() => {
  if (props.mode === 'create') return t('billing.invoices.editor.badge.create')
  if (props.mode === 'edit') return t('billing.invoices.editor.badge.edit')
  return t('billing.invoices.editor.badge.view')
})

const modeBadgeColor = computed(() => {
  if (props.mode === 'view') return 'secondary'
  return 'primary'
})

// ── Dialog Size & Position ────────────────────────────────────────────────────

const datePickerOpen = ref(false)

function onDatePicked(date: Date | null) {
  if (date) {
    const y = date.getFullYear()
    const m = String(date.getMonth() + 1).padStart(2, '0')
    const d = String(date.getDate()).padStart(2, '0')
    form.value.invoiceDate = `${y}-${m}-${d}`
  }
  datePickerOpen.value = false
}

const DIALOG_SIZE_KEY = 'billing-invoice-editor-size'

interface DialogSize { width: number; height: number }

function loadSavedSize(): DialogSize {
  try {
    const s = localStorage.getItem(DIALOG_SIZE_KEY)
    if (s) return JSON.parse(s) as DialogSize
  } catch {}
  return { width: 980, height: 680 }
}

const dialogSize = ref<DialogSize>(loadSavedSize())
const dialogPos = ref({ x: 0, y: 0 })
const overlayEl = ref<HTMLElement | null>(null)

function initDialogPosition() {
  const el = document.querySelector<HTMLElement>('.billing-editor-overlay')
  if (!el) return
  overlayEl.value = el
  const rect = el.getBoundingClientRect()
  dialogPos.value = { x: rect.left, y: rect.top }
  el.style.position = 'fixed'
  el.style.inset = 'auto'
  el.style.top = '0'
  el.style.left = '0'
  el.style.transform = `translate(${dialogPos.value.x}px, ${dialogPos.value.y}px)`
  el.style.width = `${dialogSize.value.width}px`
  el.style.height = `${dialogSize.value.height}px`
  el.style.maxWidth = '9999px'
  el.style.maxHeight = '9999px'
  el.style.margin = '0'
}

function startDrag(e: MouseEvent) {
  if ((e.target as HTMLElement).closest('button, .v-btn')) return
  e.preventDefault()
  const startX = e.clientX
  const startY = e.clientY
  const startPosX = dialogPos.value.x
  const startPosY = dialogPos.value.y
  const onMove = (ev: MouseEvent) => {
    dialogPos.value = {
      x: Math.max(0, startPosX + ev.clientX - startX),
      y: Math.max(0, startPosY + ev.clientY - startY),
    }
    if (overlayEl.value) {
      overlayEl.value.style.transform = `translate(${dialogPos.value.x}px, ${dialogPos.value.y}px)`
    }
  }
  const onUp = () => {
    document.removeEventListener('mousemove', onMove)
    document.removeEventListener('mouseup', onUp)
  }
  document.addEventListener('mousemove', onMove)
  document.addEventListener('mouseup', onUp)
}

function startResize(e: MouseEvent) {
  e.preventDefault()
  const startX = e.clientX
  const startY = e.clientY
  const startW = dialogSize.value.width
  const startH = dialogSize.value.height
  const onMove = (ev: MouseEvent) => {
    dialogSize.value = {
      width: Math.max(600, startW + ev.clientX - startX),
      height: Math.max(400, startH + ev.clientY - startY),
    }
    if (overlayEl.value) {
      overlayEl.value.style.width = `${dialogSize.value.width}px`
      overlayEl.value.style.height = `${dialogSize.value.height}px`
    }
  }
  const onUp = () => {
    document.removeEventListener('mousemove', onMove)
    document.removeEventListener('mouseup', onUp)
    localStorage.setItem(DIALOG_SIZE_KEY, JSON.stringify(dialogSize.value))
  }
  document.addEventListener('mousemove', onMove)
  document.addEventListener('mouseup', onUp)
}

const invoiceTotal = computed(() =>
  form.value.lineItems.reduce((sum, item) => sum + item.lineTotal, 0),
)

// ── Client Autocomplete ───────────────────────────────────────────────────────

const clientOptions = ref<BillingClientOption[]>([])
const loadingClients = ref(false)
const clientSearchText = ref('')
let clientSearchTimer: ReturnType<typeof setTimeout> | null = null

async function loadClients(query?: string) {
  loadingClients.value = true
  try {
    clientOptions.value = await listBillingClients(query)
    // Ensure selected client is always in the options list
    if (form.value.client) {
      const exists = clientOptions.value.some(
        (c) => c.externalClientId === form.value.client!.externalClientId,
      )
      if (!exists) {
        clientOptions.value = [form.value.client, ...clientOptions.value]
      }
    }
  } catch {
    // Silently ignore client load errors; user can retry by typing
  } finally {
    loadingClients.value = false
  }
}

function handleClientSearch(search: string) {
  // Skip search when text matches the currently selected client (item just selected)
  if (form.value.client && search === form.value.client.displayName) return
  if (clientSearchTimer) clearTimeout(clientSearchTimer)
  clientSearchTimer = setTimeout(() => {
    void loadClients(search || undefined)
  }, 300)
}

// ── Detail Loading ────────────────────────────────────────────────────────────

const loadingDetail = ref(false)

async function loadDetail() {
  if (!props.externalInvoiceId) return
  loadingDetail.value = true
  errorMessage.value = ''
  try {
    const dto = await getInvoiceEditorDetail(props.externalInvoiceId)
    form.value.client = dto.client ?? null
    form.value.invoiceDate = dto.invoiceDate ?? ''
    form.value.jobNumber = dto.jobNumber
    form.value.lineItems =
      dto.lineItems.length > 0
        ? dto.lineItems.map((li) => ({
            id: li.id ?? `line-${Math.random()}`,
            poNumber: li.poNumber,
            description: li.description,
            qtyStr: String(li.qty),
            unit: li.unit,
            unitCostStr: String(li.unitCost),
            lineTotal: li.lineTotal,
          }))
        : [emptyLine()]

    // Seed client options with the loaded client so it shows immediately
    if (dto.client) {
      clientOptions.value = [dto.client]
    }
  } catch (e) {
    if (axios.isAxiosError<{ message?: string }>(e)) {
      errorMessage.value =
        e.response?.data?.message ?? e.message ?? t('billing.invoices.editor.messages.loadFailed')
    } else if (e instanceof Error) {
      errorMessage.value = e.message
    } else {
      errorMessage.value = t('billing.invoices.editor.messages.loadFailed')
    }
  } finally {
    loadingDetail.value = false
  }
}

// ── Dialog Open/Close ─────────────────────────────────────────────────────────

watch(
  () => props.modelValue,
  async (open) => {
    if (open) {
      errorMessage.value = ''
      isSaving.value = false
      if (props.mode === 'create') {
        form.value = resetForm()
        clientOptions.value = []
        void loadClients()
      } else {
        form.value = resetForm()
        await loadDetail()
        if (!loadingDetail.value) {
          void loadClients()
        }
      }
      await nextTick()
      requestAnimationFrame(() => requestAnimationFrame(initDialogPosition))
    } else {
      overlayEl.value = null
    }
  },
)

function close() {
  emit('update:modelValue', false)
}

// ── Line Item Management ──────────────────────────────────────────────────────

function allowNumericOnly(e: KeyboardEvent) {
  const allowed = ['Backspace', 'Delete', 'Tab', 'ArrowLeft', 'ArrowRight', 'Home', 'End', '.']
  if (allowed.includes(e.key)) return
  // Allow Ctrl/Cmd shortcuts (copy, paste, select all, etc.)
  if (e.ctrlKey || e.metaKey) return
  // Block the decimal point if one already exists
  if (e.key === '.' && (e.target as HTMLInputElement).value.includes('.')) {
    e.preventDefault()
    return
  }
  if (!/^\d$/.test(e.key)) e.preventDefault()
}

function recalcLine(idx: number) {
  const item = form.value.lineItems[idx]
  const qty = parseFloat(item.qtyStr) || 0
  const cost = parseFloat(item.unitCostStr) || 0
  item.lineTotal = Math.round(qty * cost * 100) / 100
}

function addLine() {
  form.value.lineItems.push(emptyLine())
}

function removeLine(idx: number) {
  form.value.lineItems.splice(idx, 1)
}

// ── Save ──────────────────────────────────────────────────────────────────────

const isSaving = ref(false)
const errorMessage = ref('')

const rules = {
  clientRequired: (v: BillingClientOption | null) =>
    !!v || t('billing.invoices.editor.validation.clientRequired'),
  invoiceDateRequired: (v: string) =>
    !!v || t('billing.invoices.editor.validation.invoiceDateRequired'),
}

async function handleSave() {
  errorMessage.value = ''

  const { valid } = await (formRef.value?.validate() ?? Promise.resolve({ valid: false }))
  if (!valid) return

  // Manual line item validation
  const lineItemErrors = validateLineItems()
  if (lineItemErrors) {
    errorMessage.value = lineItemErrors
    return
  }

  isSaving.value = true
  try {
    const lineItems = form.value.lineItems.map((li) => ({
      poNumber: li.poNumber,
      description: li.description,
      qty: parseFloat(li.qtyStr) || 0,
      unit: li.unit,
      unitCost: parseFloat(li.unitCostStr) || 0,
    }))

    let summary: InvoiceBillingSummary

    if (props.mode === 'create') {
      summary = await createInvoice({
        externalClientId: form.value.client!.externalClientId,
        invoiceDate: form.value.invoiceDate || undefined,
        jobNumber: form.value.jobNumber,
        lineItems,
      })
    } else {
      summary = await updateInvoice(props.externalInvoiceId!, {
        externalClientId: form.value.client!.externalClientId,
        invoiceDate: form.value.invoiceDate || undefined,
        jobNumber: form.value.jobNumber,
        lineItems,
      })
    }

    emit('saved', summary)
    emit('update:modelValue', false)
  } catch (e) {
    if (axios.isAxiosError<{ message?: string }>(e)) {
      errorMessage.value =
        e.response?.data?.message ?? e.message ?? t('billing.invoices.editor.messages.saveFailed')
    } else if (e instanceof Error) {
      errorMessage.value = e.message
    } else {
      errorMessage.value = t('billing.invoices.editor.messages.saveFailed')
    }
  } finally {
    isSaving.value = false
  }
}

function validateLineItems(): string | null {
  if (form.value.lineItems.length === 0) {
    return t('billing.invoices.editor.validation.atLeastOneLine')
  }
  for (const item of form.value.lineItems) {
    const qty = parseFloat(item.qtyStr) || 0
    if (qty < 0) return t('billing.invoices.editor.validation.qtyPositive')
    const cost = parseFloat(item.unitCostStr)
    if (isNaN(cost) || cost < 0) return t('billing.invoices.editor.validation.unitCostNonNegative')
  }
  return null
}
</script>

<style scoped>
.line-items-table :deep(th) {
  white-space: nowrap;
}

.line-items-table :deep(.v-text-field .v-field__input) {
  min-height: 32px;
  padding-top: 4px;
  padding-bottom: 4px;
  padding-left: 4px;
  padding-right: 4px;
}

.line-items-table :deep(.text-right .v-field__input) {
  text-align: right;
}

.line-items-table :deep(.text-center .v-field__input) {
  text-align: center;
}

.resize-handle {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 16px;
  height: 16px;
  cursor: se-resize;
  background: linear-gradient(135deg, transparent 50%, rgba(128, 128, 128, 0.5) 50%);
  border-radius: 0 0 4px 0;
}
</style>
