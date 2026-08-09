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
            <v-col cols="12" md="4">
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
            <v-col cols="12" md="2">
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
                  @update:model-value="onInvoiceDatePicked"
                />
              </v-menu>
            </v-col>

            <!-- Due date -->
            <v-col cols="12" md="2">
              <v-menu v-model="dueDatePickerOpen" :close-on-content-click="false">
                <template #activator="{ props: menuProps }">
                  <v-text-field
                    :model-value="form.dueDate ? format(form.dueDate) : ''"
                    :label="t('billing.invoices.editor.fields.dueDate')"
                    :rules="[rules.dueDateRequired]"
                    density="compact"
                    variant="outlined"
                    readonly
                    append-inner-icon="mdi-calendar"
                    v-bind="menuProps"
                  />
                </template>
                <v-date-picker
                  :model-value="form.dueDate ? new Date(form.dueDate + 'T12:00:00') : undefined"
                  hide-header
                  @update:model-value="onDueDatePicked"
                />
              </v-menu>
            </v-col>

            <!-- Job number -->
            <v-col cols="12" md="4">
              <v-text-field
                v-model="form.jobNumber"
                :label="t('billing.invoices.editor.fields.jobNumber')"
                density="compact"
                variant="outlined"
              >
                <template v-if="autofillRefreshVisible" #append-inner>
                  <v-tooltip :text="t('billing.invoices.editor.actions.refreshFromJobNumbers')" location="top">
                    <template #activator="{ props: tooltipProps }">
                      <v-btn
                        v-bind="tooltipProps"
                        icon="mdi-refresh"
                        size="x-small"
                        variant="tonal"
                        :loading="autofillLoading"
                        @click="handleAutofillRefresh"
                      />
                    </template>
                  </v-tooltip>
                </template>
              </v-text-field>
            </v-col>
          </v-row>

          <div v-if="jobNumberValidationMessage" class="text-caption text-error mb-2">
            {{ jobNumberValidationMessage }}
          </div>

          <v-alert
            v-if="unresolvedAutofillJobs.length > 0"
            type="warning"
            variant="tonal"
            density="compact"
            class="mb-2"
          >
            {{ t('billing.invoices.editor.messages.unresolvedJobs', { jobs: unresolvedAutofillJobs.join(', ') }) }}
          </v-alert>

          <v-alert
            v-if="manualReviewAutofillJobs.length > 0"
            type="info"
            variant="tonal"
            density="compact"
            class="mb-2"
          >
            {{ t('billing.invoices.editor.messages.manualReviewJobs', { jobs: manualReviewAutofillJobs.join(', ') }) }}
          </v-alert>

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
                      class="description-textarea"
                      density="compact"
                      variant="outlined"
                      hide-details
                      auto-grow
                      rows="3"
                    />
                    <div v-if="line.autofillStatus === 'ResolvedButMissingSection1'" class="text-caption text-warning mt-1">
                      {{ t('billing.invoices.editor.messages.manualReviewRow') }}
                    </div>
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
        <div class="d-flex align-center ga-2">
          <v-btn
            variant="outlined"
            size="small"
            color="primary"
            prepend-icon="mdi-send-circle-outline"
            :disabled="!canMarkSent || isSending"
            :loading="isSending"
            @click="handleMarkSent"
          >
            {{ t('billing.invoices.actions.markSent') }}
          </v-btn>

          <v-menu location="top start">
            <template #activator="{ props: menuProps }">
              <v-btn
                v-bind="menuProps"
                variant="outlined"
                size="small"
                :disabled="!canDownload || isDownloading"
                :loading="isDownloading"
                prepend-icon="mdi-download-circle-outline"
              >
                {{ t('billing.invoices.actions.download') }}
              </v-btn>
            </template>
            <v-list density="compact">
              <v-list-item :disabled="isDownloading" @click="handleDownloadInvoicePdf">
                <v-list-item-title>{{ t('billing.invoices.actions.invoicePdf') }}</v-list-item-title>
              </v-list-item>
              <v-list-item :disabled="isDownloading" @click="handleDownloadDeliveryNote">
                <v-list-item-title>{{ t('billing.invoices.actions.deliveryNote') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>
        </div>

        <v-spacer />
        <v-btn
          variant="outlined"
          size="small"
          @click="close"
        >
          {{ isReadOnly ? t('billing.invoices.editor.actions.close') : t('billing.invoices.editor.actions.cancel') }}
        </v-btn>
        <v-btn
          v-if="!isReadOnly"
          variant="outlined"
          size="small"
          color="primary"
          :loading="isSaving"
          @click="handleSave"
        >
          {{ t('billing.invoices.editor.actions.save') }}
        </v-btn>
      </v-card-actions>

      <v-dialog v-model="showAutofillOverwriteConfirmation" max-width="420">
        <v-card>
          <v-card-title>{{ t('billing.invoices.editor.actions.confirmRefresh') }}</v-card-title>
          <v-card-text>{{ t('billing.invoices.editor.messages.refreshOverwriteConfirm') }}</v-card-text>
          <v-card-actions>
            <v-spacer />
            <v-btn variant="text" @click="showAutofillOverwriteConfirmation = false">
              {{ t('billing.invoices.editor.actions.cancel') }}
            </v-btn>
            <v-btn color="primary" variant="elevated" :loading="autofillLoading" @click="confirmAutofillOverwrite">
              {{ t('billing.invoices.editor.actions.refreshFromJobNumbers') }}
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>

      <v-dialog v-model="showMarkSentConfirmation" max-width="420">
        <v-card>
          <v-card-title>{{ t('billing.invoices.actions.confirmMarkSent') }}</v-card-title>
          <v-card-text>{{ t('billing.invoices.messages.markSentConfirm') }}</v-card-text>
          <v-card-actions>
            <v-spacer />
            <v-btn variant="text" @click="showMarkSentConfirmation = false">
              {{ t('billing.invoices.actions.cancel') }}
            </v-btn>
            <v-btn color="primary" variant="elevated" :loading="isSending" @click="performMarkSent">
              {{ t('billing.invoices.actions.markAsSent') }}
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>

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
  lookupInvoiceEditorAutofill,
  createInvoice,
  updateInvoice,
  sendInvoice,
  downloadInvoicePdf,
  downloadDeliveryNote,
  type BillingClientOption,
  type InvoiceEditorAutofillLookupItem,
  type InvoiceEditorAutofillLookupStatus,
  type InvoiceBillingSummary,
} from '@/services/billing'
import { buildJobNumberSignature, parseJobNumberExpression } from './invoiceAutofill'

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
  sourceJobNumber: string | null
  autofillStatus: InvoiceEditorAutofillLookupStatus | null
}

interface FormState {
  client: BillingClientOption | null
  invoiceDate: string
  dueDate: string
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
    sourceJobNumber: null,
    autofillStatus: null,
  }
}

function resetForm(): FormState {
  return {
    client: null,
    invoiceDate: toIsoDate(new Date()),
    dueDate: toIsoDate(new Date()),
    jobNumber: '',
    lineItems: [emptyLine()],
  }
}

const form = ref<FormState>(resetForm())

// ── Computed ──────────────────────────────────────────────────────────────────

const isReadOnly = computed(() => props.mode === 'view' || markedSent.value)

const canMarkSent = computed(
  () => props.mode === 'edit' && !!props.externalInvoiceId && !markedSent.value,
)

const canDownload = computed(() => !!props.externalInvoiceId)

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

const jobNumberParseResult = computed(() => parseJobNumberExpression(form.value.jobNumber))
const currentCanonicalJobNumbers = computed(() => jobNumberParseResult.value.canonicalJobNumbers)
const currentAutofillSignature = computed(() => buildJobNumberSignature(currentCanonicalJobNumbers.value))
const jobNumberValidationMessage = computed(() =>
  jobNumberParseResult.value.error ? t('billing.invoices.editor.validation.jobNumberFormat') : '',
)

// ── Dialog Size & Position ────────────────────────────────────────────────────

const datePickerOpen = ref(false)
const dueDatePickerOpen = ref(false)

function toIsoDate(date: Date): string {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

function onInvoiceDatePicked(date: Date | null) {
  if (date) {
    const iso = toIsoDate(date)
    form.value.invoiceDate = iso
    form.value.dueDate = iso
  }
  datePickerOpen.value = false
}

function onDueDatePicked(date: Date | null) {
  if (date) {
    form.value.dueDate = toIsoDate(date)
  }
  dueDatePickerOpen.value = false
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

const DIALOG_VIEWPORT_MARGIN = 24

function getClampedSize(): { width: number; height: number } {
  const vw = window.innerWidth
  const vh = window.innerHeight
  return {
    width: Math.min(dialogSize.value.width, vw - DIALOG_VIEWPORT_MARGIN),
    height: Math.min(dialogSize.value.height, vh - DIALOG_VIEWPORT_MARGIN),
  }
}

function applyDialogLayout(recenter: boolean) {
  const el = overlayEl.value
  if (!el) return
  const { width, height } = getClampedSize()
  let { x, y } = dialogPos.value
  if (recenter) {
    x = Math.max(0, Math.round((window.innerWidth - width) / 2))
    y = Math.max(0, Math.round((window.innerHeight - height) / 2))
  } else {
    x = Math.min(Math.max(0, x), Math.max(0, window.innerWidth - width))
    y = Math.min(Math.max(0, y), Math.max(0, window.innerHeight - height))
  }
  dialogPos.value = { x, y }
  el.style.position = 'fixed'
  el.style.inset = 'auto'
  el.style.top = '0'
  el.style.left = '0'
  el.style.transform = `translate(${x}px, ${y}px)`
  el.style.width = `${width}px`
  el.style.height = `${height}px`
  el.style.maxWidth = `${width}px`
  el.style.maxHeight = `${height}px`
  el.style.margin = '0'
}

function initDialogPosition() {
  const el = document.querySelector<HTMLElement>('.billing-editor-overlay')
  if (!el) return
  overlayEl.value = el
  applyDialogLayout(true)
}

function startDrag(e: MouseEvent) {
  if ((e.target as HTMLElement).closest('button, .v-btn')) return
  e.preventDefault()
  const startX = e.clientX
  const startY = e.clientY
  const startPosX = dialogPos.value.x
  const startPosY = dialogPos.value.y
  const onMove = (ev: MouseEvent) => {
    const { width, height } = getClampedSize()
    dialogPos.value = {
      x: Math.min(Math.max(0, startPosX + ev.clientX - startX), Math.max(0, window.innerWidth - width)),
      y: Math.min(Math.max(0, startPosY + ev.clientY - startY), Math.max(0, window.innerHeight - height)),
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
    const vw = window.innerWidth
    const vh = window.innerHeight
    dialogSize.value = {
      width: Math.min(Math.max(600, startW + ev.clientX - startX), vw - DIALOG_VIEWPORT_MARGIN),
      height: Math.min(Math.max(400, startH + ev.clientY - startY), vh - DIALOG_VIEWPORT_MARGIN),
    }
    if (overlayEl.value) {
      overlayEl.value.style.width = `${dialogSize.value.width}px`
      overlayEl.value.style.height = `${dialogSize.value.height}px`
      overlayEl.value.style.maxWidth = `${dialogSize.value.width}px`
      overlayEl.value.style.maxHeight = `${dialogSize.value.height}px`
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

let onWindowResize: (() => void) | null = null

function addResizeListener() {
  removeResizeListener()
  onWindowResize = () => applyDialogLayout(false)
  window.addEventListener('resize', onWindowResize)
}

function removeResizeListener() {
  if (onWindowResize) {
    window.removeEventListener('resize', onWindowResize)
    onWindowResize = null
  }
}

const invoiceTotal = computed(() =>
  form.value.lineItems.reduce((sum, item) => sum + item.lineTotal, 0),
)

const autofillLoading = ref(false)
const unresolvedAutofillJobs = ref<string[]>([])
const manualReviewAutofillJobs = ref<string[]>([])
const lastGeneratedAutofillSignature = ref('')
const lastGeneratedLineItemsSnapshot = ref('')
const showAutofillOverwriteConfirmation = ref(false)

const autofillDirty = computed(() =>
  lastGeneratedLineItemsSnapshot.value.length > 0
  && serializeLineItems(form.value.lineItems) !== lastGeneratedLineItemsSnapshot.value,
)

const autofillRefreshVisible = computed(() =>
  !isReadOnly.value
  && form.value.jobNumber.trim().length > 0
  && !jobNumberParseResult.value.error
  && currentAutofillSignature.value !== lastGeneratedAutofillSignature.value,
)

// ── Client Autocomplete ───────────────────────────────────────────────────────

const clientOptions = ref<BillingClientOption[]>([])
const loadingClients = ref(false)
const clientSearchText = ref('')
let clientSearchTimer: ReturnType<typeof setTimeout> | null = null

async function loadClients(query?: string) {
  loadingClients.value = true
  try {
    clientOptions.value = [...await listBillingClients(query)]
      .sort((left, right) => left.displayName.localeCompare(right.displayName))
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
    form.value.dueDate = dto.dueDate ?? toIsoDate(new Date())
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
            sourceJobNumber: null,
            autofillStatus: null,
          }))
        : [emptyLine()]

    lastGeneratedAutofillSignature.value = buildJobNumberSignature(parseJobNumberExpression(dto.jobNumber).canonicalJobNumbers)
    lastGeneratedLineItemsSnapshot.value = ''
    unresolvedAutofillJobs.value = []
    manualReviewAutofillJobs.value = []

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
      markedSent.value = false
      showMarkSentConfirmation.value = false
      isSending.value = false
      isDownloading.value = false
      if (props.mode === 'create') {
        form.value = resetForm()
        clientOptions.value = []
        lastGeneratedAutofillSignature.value = ''
        lastGeneratedLineItemsSnapshot.value = ''
        unresolvedAutofillJobs.value = []
        manualReviewAutofillJobs.value = []
        void loadClients()
      } else {
        form.value = resetForm()
        await loadDetail()
        if (!loadingDetail.value) {
          void loadClients()
        }
      }
      await nextTick()
      requestAnimationFrame(() => requestAnimationFrame(() => {
        initDialogPosition()
        addResizeListener()
      }))
    } else {
      overlayEl.value = null
      removeResizeListener()
    }
  },
)

watch(
  () => form.value.jobNumber,
  () => {
    unresolvedAutofillJobs.value = []
    manualReviewAutofillJobs.value = []
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
  if (!item) {
    return
  }

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

function serializeLineItems(lineItems: FormLineItem[]): string {
  return JSON.stringify(lineItems.map((item) => ({
    poNumber: item.poNumber,
    description: item.description,
    qtyStr: item.qtyStr,
    unit: item.unit,
    unitCostStr: item.unitCostStr,
  })))
}

function createAutofillLineItem(item: InvoiceEditorAutofillLookupItem): FormLineItem {
  return {
    id: `autofill-${item.canonicalJobNumber}`,
    poNumber: item.purchaseOrder,
    description: item.description,
    qtyStr: '1',
    unit: '',
    unitCostStr: '0',
    lineTotal: 0,
    sourceJobNumber: item.canonicalJobNumber,
    autofillStatus: item.status,
  }
}

async function handleAutofillRefresh() {
  if (jobNumberParseResult.value.error) {
    return
  }

  if (currentCanonicalJobNumbers.value.length === 0) {
    return
  }

  if (autofillDirty.value) {
    showAutofillOverwriteConfirmation.value = true
    return
  }

  await runAutofillRefresh()
}

async function confirmAutofillOverwrite() {
  showAutofillOverwriteConfirmation.value = false
  await runAutofillRefresh()
}

async function runAutofillRefresh() {
  autofillLoading.value = true
  errorMessage.value = ''
  unresolvedAutofillJobs.value = []
  manualReviewAutofillJobs.value = []

  try {
    const results = await lookupInvoiceEditorAutofill(currentCanonicalJobNumbers.value)
    const nextLineItems = results
      .filter((item) => item.status !== 'Unresolved')
      .map(createAutofillLineItem)

    unresolvedAutofillJobs.value = results
      .filter((item) => item.status === 'Unresolved')
      .map((item) => item.canonicalJobNumber)

    manualReviewAutofillJobs.value = results
      .filter((item) => item.status === 'ResolvedButMissingSection1')
      .map((item) => item.canonicalJobNumber)

    if (nextLineItems.length > 0) {
      form.value.lineItems = nextLineItems
      lastGeneratedAutofillSignature.value = currentAutofillSignature.value
      lastGeneratedLineItemsSnapshot.value = serializeLineItems(nextLineItems)
    }
  } catch (e) {
    if (axios.isAxiosError<{ message?: string }>(e)) {
      errorMessage.value =
        e.response?.data?.message ?? e.message ?? t('billing.invoices.editor.messages.refreshFailed')
    } else if (e instanceof Error) {
      errorMessage.value = e.message
    } else {
      errorMessage.value = t('billing.invoices.editor.messages.refreshFailed')
    }
  } finally {
    autofillLoading.value = false
  }
}

// ── Save ──────────────────────────────────────────────────────────────────────

const isSaving = ref(false)
const errorMessage = ref('')
const markedSent = ref(false)
const showMarkSentConfirmation = ref(false)
const isSending = ref(false)
const isDownloading = ref(false)

const rules = {
  clientRequired: (v: BillingClientOption | null) =>
    !!v || t('billing.invoices.editor.validation.clientRequired'),
  invoiceDateRequired: (v: string) =>
    !!v || t('billing.invoices.editor.validation.invoiceDateRequired'),
  dueDateRequired: (v: string) =>
    !!v || t('billing.invoices.editor.validation.dueDateRequired'),
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
        dueDate: form.value.dueDate || undefined,
        jobNumber: form.value.jobNumber,
        lineItems,
      })
    } else {
      summary = await updateInvoice(props.externalInvoiceId!, {
        externalClientId: form.value.client!.externalClientId,
        invoiceDate: form.value.invoiceDate || undefined,
        dueDate: form.value.dueDate || undefined,
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

// ── Mark Sent ─────────────────────────────────────────────────────────────────

function handleMarkSent() {
  showMarkSentConfirmation.value = true
}

async function performMarkSent() {
  if (!props.externalInvoiceId) return
  isSending.value = true
  errorMessage.value = ''
  try {
    const updatedSummary = await sendInvoice(props.externalInvoiceId)
    markedSent.value = true
    emit('saved', updatedSummary)
    showMarkSentConfirmation.value = false
  } catch (e) {
    if (axios.isAxiosError<{ message?: string }>(e)) {
      errorMessage.value = e.response?.data?.message ?? e.message ?? t('billing.invoices.messages.sendFailed')
    } else if (e instanceof Error) {
      errorMessage.value = e.message
    } else {
      errorMessage.value = t('billing.invoices.messages.sendUnexpected')
    }
  } finally {
    isSending.value = false
  }
}

// ── Download ──────────────────────────────────────────────────────────────────

function openPdfPreviewWindow(): Window | null {
  const previewWindow = window.open('', '_blank')
  if (!previewWindow) return null
  previewWindow.document.title = t('billing.invoices.messages.previewTitle')
  previewWindow.document.body.innerHTML = `<p style="font-family: sans-serif; padding: 16px;">${t('billing.invoices.messages.previewLoading')}</p>`
  return previewWindow
}

function showPdfPreview(previewWindow: Window, blob: Blob) {
  const previewUrl = URL.createObjectURL(blob)
  previewWindow.location.href = previewUrl
  window.setTimeout(() => URL.revokeObjectURL(previewUrl), 60_000)
}

async function handleDownloadInvoicePdf() {
  if (!props.externalInvoiceId || isDownloading.value) return
  const previewWindow = openPdfPreviewWindow()
  if (!previewWindow) {
    errorMessage.value = t('billing.invoices.messages.previewBlocked')
    return
  }
  isDownloading.value = true
  errorMessage.value = ''
  try {
    const blob = await downloadInvoicePdf(props.externalInvoiceId)
    showPdfPreview(previewWindow, blob)
  } catch (e) {
    previewWindow.close()
    if (axios.isAxiosError<{ message?: string }>(e)) {
      errorMessage.value = e.response?.data?.message ?? e.message ?? t('billing.invoices.messages.downloadInvoicePdfFailed')
    } else if (e instanceof Error) {
      errorMessage.value = e.message
    } else {
      errorMessage.value = t('billing.invoices.messages.downloadInvoicePdfUnexpected')
    }
  } finally {
    isDownloading.value = false
  }
}

async function handleDownloadDeliveryNote() {
  if (!props.externalInvoiceId || isDownloading.value) return
  const previewWindow = openPdfPreviewWindow()
  if (!previewWindow) {
    errorMessage.value = t('billing.invoices.messages.previewBlocked')
    return
  }
  isDownloading.value = true
  errorMessage.value = ''
  try {
    const blob = await downloadDeliveryNote(props.externalInvoiceId)
    showPdfPreview(previewWindow, blob)
  } catch (e) {
    previewWindow.close()
    if (axios.isAxiosError<{ message?: string }>(e)) {
      errorMessage.value = e.response?.data?.message ?? e.message ?? t('billing.invoices.messages.downloadDeliveryNoteFailed')
    } else if (e instanceof Error) {
      errorMessage.value = e.message
    } else {
      errorMessage.value = t('billing.invoices.messages.downloadDeliveryNoteUnexpected')
    }
  } finally {
    isDownloading.value = false
  }
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

.line-items-table :deep(.description-textarea textarea) {
  resize: vertical;
  overflow: auto;
  min-height: 88px;
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
