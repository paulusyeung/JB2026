<template>
  <section class="page-section billing-invoices-page">
    <v-card rounded="xl" elevation="0" class="panel-card billing-invoices-card">
      <v-card-text>
        <div class="filter-bar">
          <div class="view-heading">
            <div class="text-h6">Billing Invoices</div>
            <div class="text-caption text-medium-emphasis">Invoice Ninja synced invoice summaries</div>
          </div>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>

        <div class="toolbar-bar mb-2">
          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-view-column">
                Columns
              </v-btn>
            </template>
            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item v-for="column in columnOptions" :key="column.key" @click="toggleColumn(column.key)">
                <template #prepend>
                  <v-checkbox-btn :model-value="visibleColumnKeys.includes(column.key)" />
                </template>
                <v-list-item-title>{{ column.title }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-sort">
                Sorting
              </v-btn>
            </template>
            <v-card min-width="280" class="pa-3">
              <v-select
                v-model="sortKey"
                :items="sortableColumns"
                item-title="title"
                item-value="key"
                density="compact"
                variant="outlined"
                label="Sort by"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">Asc</v-btn>
                <v-btn value="desc">Desc</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
            Check Box
          </v-btn>

          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                Views
              </v-btn>
            </template>
            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>Detail View</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>Card View</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <v-divider vertical class="mx-1" />

          <v-btn variant="outlined" size="small" color="primary" prepend-icon="mdi-plus-circle-outline" @click="openNewInvoice">
            New Invoice
          </v-btn>

          <v-btn 
            variant="outlined" 
            size="small" 
            :disabled="!isMarkSentEnabled || isSendingInvoice"
            :loading="isSendingInvoice"
            prepend-icon="mdi-send-circle-outline" 
            @click="handleMarkSent"
          >
            Mark Sent
          </v-btn>

          <span v-if="checkboxMode" class="text-caption text-medium-emphasis">
            {{ selectedInvoiceIds.length }} selected
          </span>
        </div>

        <div v-if="isCardView" class="invoice-card-list">
          <v-card
            v-for="invoice in displayedInvoices"
            :key="invoice.externalInvoiceId"
            rounded="lg"
            elevation="0"
            class="invoice-card"
            @click="openInvoice(invoice)"
          >
            <div v-if="checkboxMode" class="invoice-card__checkbox-anchor" @click.stop>
              <v-checkbox-btn
                class="invoice-card__checkbox"
                :model-value="selectedInvoiceIds.includes(invoice.externalInvoiceId)"
                density="compact"
                hide-details
                @click.stop="handleCardCheckbox(invoice.externalInvoiceId)"
              />
            </div>

            <div class="invoice-card__header">
              <div>
                <div class="text-subtitle-2 font-weight-bold">{{ invoice.invoiceNumber || invoice.externalInvoiceId }}</div>
                <div class="text-caption text-medium-emphasis">{{ invoice.clientName || '-' }}</div>
              </div>
            </div>

            <div class="invoice-card__body">
              <span>{{ invoice.invoiceDate ? format(invoice.invoiceDate) : '-' }}</span>
              <v-chip size="small" :color="statusColor(invoice.status)" variant="tonal">
                {{ invoice.status || 'Unknown' }}
              </v-chip>
            </div>

            <div class="invoice-card__footer text-caption text-medium-emphasis">
              <span>Amount: {{ formatCurrency(invoice.amount) }}</span>
              <span>Due: {{ invoice.dueDate ? format(invoice.dueDate) : '-' }}</span>
              <span>Last Synced: {{ invoice.lastSyncedAt ? format(invoice.lastSyncedAt) : '-' }}</span>
            </div>
          </v-card>
        </div>

        <v-data-table
          v-else
          v-model="selectedInvoiceIds"
          :headers="headers"
          :items="displayedInvoices"
          :loading="loading"
          item-value="externalInvoiceId"
          :show-select="checkboxMode"
          density="compact"
          fixed-header
          height="62vh"
          class="billing-invoices-table"
          @click:row="onRowClick"
        >
          <template #[`item.invoiceNumber`]="{ item }">
            <v-btn variant="text" color="primary" class="px-0 text-none" @click="openInvoice(item)">
              {{ item.invoiceNumber || item.externalInvoiceId }}
            </v-btn>
          </template>

          <template #[`item.clientName`]="{ item }">
            {{ item.clientName || '-' }}
          </template>

          <template #[`item.invoiceDate`]="{ item }">
            {{ item.invoiceDate ? format(item.invoiceDate) : '-' }}
          </template>

          <template #[`item.status`]="{ item }">
            <v-chip size="small" :color="statusColor(item.status)" variant="tonal">
              {{ item.status || 'Unknown' }}
            </v-chip>
          </template>

          <template #[`item.amount`]="{ item }">
            {{ formatCurrency(item.amount) }}
          </template>

          <template #[`item.dueDate`]="{ item }">
            {{ item.dueDate ? format(item.dueDate) : '-' }}
          </template>

          <template #[`item.lastSyncedAt`]="{ item }">
              {{ item.lastSyncedAt ? format(item.lastSyncedAt) : '-' }}
          </template>
        </v-data-table>
      </v-card-text>
    </v-card>

    <!-- Confirmation Dialog for Mark Sent -->
    <v-dialog v-model="showMarkSentConfirmation" max-width="400">
      <v-card>
        <v-card-title>Confirm Mark as Sent</v-card-title>
        <v-card-text>
          Are you sure you want to mark this invoice as sent to Invoice Ninja? This action cannot be undone.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showMarkSentConfirmation = false">Cancel</v-btn>
          <v-btn color="primary" variant="elevated" :loading="isSendingInvoice" @click="performMarkSent">
            Mark as Sent
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </section>
</template>

<script setup lang="ts">
import axios from 'axios'
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useViewSettings } from '@/composables/useColumnPersistence'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { listInvoices, sendInvoice, type InvoiceBillingSummary } from '@/services/billing'

type BillingInvoicesViewMode = 'detail' | 'card'

const { formatCurrency } = useLocaleFormatters()
const { format } = useGlobalDateFormatter()
const router = useRouter()

const loading = ref(false)
const errorMessage = ref('')
const invoices = ref<InvoiceBillingSummary[]>([])
const selectedInvoiceIds = ref<string[]>([])
const isSendingInvoice = ref(false)
const showMarkSentConfirmation = ref(false)
const viewSettings = useViewSettings('billing-invoices', {
  visibleColumns: ['invoiceNumber', 'clientName', 'invoiceDate', 'status', 'amount', 'dueDate', 'lastSyncedAt'],
  sortKey: 'invoiceDate',
  sortDirection: 'desc',
  checkboxMode: false,
  viewMode: 'detail',
})
const visibleColumnKeys = viewSettings.visibleColumns
const sortKey = viewSettings.sortKey
const sortDirection = viewSettings.sortDirection
const checkboxMode = viewSettings.checkboxMode
const viewMode = viewSettings.viewMode

const isCardView = computed(() => viewMode.value === 'card')

const allHeaders = computed(() => [
  { title: 'Invoice', key: 'invoiceNumber', minWidth: '180px' },
  { title: 'Client', key: 'clientName', minWidth: '220px' },
  { title: 'Invoice Date', key: 'invoiceDate', width: '130px' },
  { title: 'Status', key: 'status', width: '140px' },
  { title: 'Amount', key: 'amount', width: '140px', align: 'end' as const },
  { title: 'Due Date', key: 'dueDate', width: '130px' },
  { title: 'Last Synced', key: 'lastSyncedAt', width: '180px' },
])

const headers = computed(() =>
  allHeaders.value.filter((header) => visibleColumnKeys.value.includes(String(header.key))),
)

const columnOptions = computed(() =>
  allHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title || header.key) })),
)

const sortableColumns = computed(() =>
  allHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title || header.key) })),
)

/**
 * Determines if the Mark Sent button should be enabled.
 * Enabled only when:
 * - checkbox mode is active
 * - exactly one invoice is selected
 * - that invoice has Draft status
 */
const isMarkSentEnabled = computed(() => {
  if (!checkboxMode.value || selectedInvoiceIds.value.length !== 1) {
    return false
  }
  const selectedId = selectedInvoiceIds.value[0]
  const selectedInvoice = invoices.value.find((inv) => inv.externalInvoiceId === selectedId)
  return selectedInvoice?.status === 'Draft'
})

const displayedInvoices = computed(() => {
  const result = [...invoices.value]
  const activeSortKey = sortKey.value || 'invoiceDate'
  const direction = sortDirection.value === 'asc' ? 1 : -1

  result.sort((left, right) => compareInvoices(left, right, activeSortKey) * direction)
  return result
})

onMounted(async () => {
  await loadInvoices()
})

async function loadInvoices() {
  loading.value = true
  errorMessage.value = ''
  try {
    invoices.value = await listInvoices()
  } catch (e) {
    console.error('Failed to load billing invoices', e)
    if (axios.isAxiosError<{ message?: string }>(e)) {
      errorMessage.value = e.response?.data?.message || e.message || 'Unable to load billing invoices.'
    } else if (e instanceof Error) {
      errorMessage.value = e.message || 'Unable to load billing invoices.'
    } else {
      errorMessage.value = 'Unable to load billing invoices.'
    }
  } finally {
    loading.value = false
  }
}

function openInvoice(invoice: InvoiceBillingSummary) {
  void router.push({
    name: 'billing-invoice-detail',
    params: { externalInvoiceId: invoice.externalInvoiceId },
  })
}

function openNewInvoice() {
  void router.push({ name: 'job-order-job-list' })
}

/**
 * Handles the Mark Sent button click.
 * Shows a confirmation dialog before performing the send action.
 */
function handleMarkSent() {
  showMarkSentConfirmation.value = true
}

/**
 * Performs the actual send operation after user confirmation.
 * Sends the selected draft invoice to Invoice Ninja, updates the local list,
 * clears selection, and displays any errors.
 */
async function performMarkSent() {
  const selectedId = selectedInvoiceIds.value[0]
  if (!selectedId) return

  isSendingInvoice.value = true
  errorMessage.value = ''

  try {
    const updatedSummary = await sendInvoice(selectedId)
    
    // Find the invoice in the list and replace it with the updated summary
    const invoiceIndex = invoices.value.findIndex((inv) => inv.externalInvoiceId === selectedId)
    if (invoiceIndex !== -1) {
      invoices.value[invoiceIndex] = updatedSummary
    }
    
    // Clear selection and close dialog
    selectedInvoiceIds.value = []
    showMarkSentConfirmation.value = false
  } catch (e) {
    console.error('Failed to send invoice', e)
    if (axios.isAxiosError<{ message?: string }>(e)) {
      errorMessage.value = e.response?.data?.message || e.message || 'Failed to send invoice.'
    } else if (e instanceof Error) {
      errorMessage.value = e.message || 'Failed to send invoice.'
    } else {
      errorMessage.value = 'An unexpected error occurred while sending the invoice.'
    }
  } finally {
    isSendingInvoice.value = false
  }
}

function toggleColumn(columnKey: string) {
  if (visibleColumnKeys.value.includes(columnKey)) {
    if (visibleColumnKeys.value.length > 1) {
      visibleColumnKeys.value = visibleColumnKeys.value.filter((key) => key !== columnKey)
    }
    return
  }

  visibleColumnKeys.value = [...visibleColumnKeys.value, columnKey]
}

function setViewMode(mode: BillingInvoicesViewMode) {
  viewMode.value = mode
}

function handleCardCheckbox(externalInvoiceId: string) {
  if (selectedInvoiceIds.value.includes(externalInvoiceId)) {
    selectedInvoiceIds.value = selectedInvoiceIds.value.filter((id) => id !== externalInvoiceId)
    return
  }

  selectedInvoiceIds.value = [...selectedInvoiceIds.value, externalInvoiceId]
}

function onRowClick(_event: Event, payload: { item: InvoiceBillingSummary }) {
  if (checkboxMode.value) {
    return
  }

  openInvoice(payload.item)
}

function compareInvoices(left: InvoiceBillingSummary, right: InvoiceBillingSummary, key: string) {
  switch (key) {
    case 'amount':
      return left.amount - right.amount
    case 'invoiceDate':
      return compareDateValues(left.invoiceDate, right.invoiceDate)
    case 'dueDate':
      return compareDateValues(left.dueDate, right.dueDate)
    case 'lastSyncedAt':
      return compareDateValues(left.lastSyncedAt, right.lastSyncedAt)
    case 'invoiceNumber':
      return left.invoiceNumber.localeCompare(right.invoiceNumber)
    case 'clientName':
      return left.clientName.localeCompare(right.clientName)
    case 'status':
      return left.status.localeCompare(right.status)
    default:
      return 0
  }
}

function compareDateValues(left?: string, right?: string) {
  const leftValue = left ? new Date(left).getTime() : Number.NEGATIVE_INFINITY
  const rightValue = right ? new Date(right).getTime() : Number.NEGATIVE_INFINITY
  return leftValue - rightValue
}

function statusColor(status: string) {
  const normalized = status.toLowerCase()
  if (normalized.includes('paid')) return 'success'
  if (normalized.includes('overdue')) return 'error'
  if (normalized.includes('sent') || normalized.includes('view')) return 'info'
  return 'warning'
}
</script>

<style scoped>
.billing-invoices-page {
  min-height: 0;
  --billing-invoices-header-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%);
  --billing-invoices-header-fg: rgb(var(--v-theme-on-surface-variant));
}

.billing-invoices-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
  background: linear-gradient(180deg, rgba(224, 237, 255, 0.92), rgba(241, 247, 255, 0.96));
}

.filter-bar {
  display: grid;
  gap: 12px;
  grid-template-columns: minmax(260px, 1fr) auto;
  align-items: center;
  margin-bottom: 16px;
}

.view-heading {
  display: grid;
  gap: 4px;
}

.toolbar-bar {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
}

.toolbar-menu-list {
  max-height: 340px;
  overflow: auto;
}

.invoice-card-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: 1fr;
}

.invoice-card {
  position: relative;
  display: grid;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgba(255, 255, 255, 0.72);
  cursor: pointer;
  overflow: hidden;
}

.invoice-card__checkbox-anchor {
  position: absolute;
  top: 0.35rem;
  right: 0.35rem;
  z-index: 1;
  display: flex;
  align-items: flex-start;
  justify-content: flex-end;
}

.invoice-card__checkbox {
  margin: 0;
}

.invoice-card__header,
.invoice-card__body,
.invoice-card__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.invoice-card__body,
.invoice-card__footer {
  flex-wrap: wrap;
}

.billing-invoices-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.billing-invoices-table :deep(.v-table__wrapper > table > thead > tr > th),
.billing-invoices-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--billing-invoices-header-bg) !important;
  color: var(--billing-invoices-header-fg) !important;
}

.billing-invoices-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.billing-invoices-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.billing-invoices-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.billing-invoices-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}

@media (min-width: 960px) {
  .invoice-card-list {
    grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
    align-items: start;
  }
}
</style>
