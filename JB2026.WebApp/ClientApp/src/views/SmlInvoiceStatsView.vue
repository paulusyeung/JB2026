<template>
  <section class="page-section sml-invoice-stats-page">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('sml.invoiceStats.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('sml.invoiceStats.subtitle') }}</p>
        </div>
      </v-card-title>

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('sml.invoiceStats.lookup')"
            prepend-inner-icon="mdi-magnify"
            variant="solo-filled"
            hide-details
            clearable
            @keydown.enter="refresh"
          />

          <v-text-field
            v-model="startOn"
            type="date"
            density="comfortable"
            :label="t('sml.invoiceStats.startDate')"
            variant="solo-filled"
            hide-details
          />

          <v-text-field
            v-model="endOn"
            type="date"
            density="comfortable"
            :label="t('sml.invoiceStats.endDate')"
            variant="solo-filled"
            hide-details
          />

          <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loading" @click="refresh">
            {{ t('common.search') }}
          </v-btn>

          <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="load">
            {{ t('common.refresh') }}
          </v-btn>

          <v-btn
            variant="outlined"
            prepend-icon="mdi-microsoft-excel"
            :disabled="columnKeys.length === 0"
            @click="exportToCsv"
          >
            {{ t('sml.invoiceStats.exportToExcel') }}
          </v-btn>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ errorMessage }}</v-alert>

        <div class="text-caption text-medium-emphasis mt-2 mb-2">
          {{ t('sml.invoiceStats.rows', { count: formatNumber(rows.length) }) }}
        </div>

        <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-3" />

        <div class="pivot-shell">
          <table class="pivot-table" v-if="columnKeys.length > 0">
            <thead>
              <tr>
                <th>{{ t('sml.invoiceStats.headers.customerName') }}</th>
                <th>{{ t('sml.invoiceStats.headers.invoiceNumber') }}</th>
                <th>{{ t('sml.invoiceStats.headers.purchaseOrder') }}</th>
                <th>{{ t('sml.invoiceStats.headers.productCode') }}</th>
                <th>{{ t('sml.invoiceStats.headers.qty') }}</th>
                <th>{{ t('sml.invoiceStats.headers.unit') }}</th>
                <th>{{ t('sml.invoiceStats.headers.price') }}</th>
                <th v-for="column in columnKeys" :key="column">{{ formatColumnLabel(column) }}</th>
                <th>{{ t('sml.invoiceStats.headers.grandTotal') }}</th>
              </tr>
            </thead>

            <tbody>
              <template v-for="customer in pagedCustomers" :key="customer.key">
                <tr class="group-row">
                  <th :colspan="rowHeaderCount + columnKeys.length + 1">{{ customer.customerName }}</th>
                </tr>

                <template v-for="invoice in customer.invoices" :key="invoice.key">
                  <tr v-for="group in invoice.groups" :key="group.key">
                    <td class="label-cell"></td>
                    <td class="label-cell">{{ group.invoiceNumber }}</td>
                    <td class="label-cell">{{ group.purchaseOrder }}</td>
                    <td class="label-cell">{{ group.productCode }}</td>
                    <td class="numeric-cell">{{ formatPlainNumber(group.qty) }}</td>
                    <td class="center-cell">{{ group.unit }}</td>
                    <td class="numeric-cell">{{ formatAmount(group.price) }}</td>
                    <td v-for="column in columnKeys" :key="`${group.key}-${column}`" class="numeric-cell">
                      {{ formatAmount(group.byColumn[column] ?? 0) }}
                    </td>
                    <td class="numeric-cell total-cell">{{ formatAmount(group.total) }}</td>
                  </tr>

                  <tr class="subtotal-row">
                    <td></td>
                    <th :colspan="6">{{ t('sml.invoiceStats.invoiceTotal', { invoiceNumber: invoice.invoiceNumber }) }}</th>
                    <td v-for="column in columnKeys" :key="`${invoice.key}-${column}`" class="numeric-cell">
                      {{ formatAmount(invoice.byColumn[column] ?? 0) }}
                    </td>
                    <td class="numeric-cell total-cell">{{ formatAmount(invoice.total) }}</td>
                  </tr>
                </template>
              </template>
            </tbody>

            <tfoot>
              <tr>
                <th :colspan="rowHeaderCount">{{ t('sml.invoiceStats.headers.grandTotal') }}</th>
                <th v-for="column in columnKeys" :key="`total-${column}`" class="numeric-cell">
                  {{ formatAmount(grandByColumn[column] ?? 0) }}
                </th>
                <th class="numeric-cell">{{ formatAmount(grandTotal) }}</th>
              </tr>
            </tfoot>
          </table>

          <div v-else class="text-body-2 text-medium-emphasis py-6 text-center">
            {{ t('sml.invoiceStats.empty') }}
          </div>
        </div>

        <div class="pager-row" v-if="totalPages > 1">
          <div class="text-caption text-medium-emphasis">
            {{ t('sml.invoiceStats.page', { page, pages: totalPages, count: customers.length }) }}
          </div>
          <v-pagination v-model="page" :length="totalPages" density="comfortable" rounded="circle" total-visible="7" />
        </div>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { getSmlInvoiceStats } from '@/services/sml'
import type { SmlInvoiceStatsRow } from '@/types/api'

type PivotLeaf = {
  key: string
  customerName: string
  invoiceNumber: string
  purchaseOrder: string
  productCode: string
  qty: number
  unit: string
  price: number
  byColumn: Record<string, number>
  total: number
}

type InvoiceGroup = {
  key: string
  invoiceNumber: string
  groups: PivotLeaf[]
  byColumn: Record<string, number>
  total: number
}

type CustomerGroup = {
  key: string
  customerName: string
  invoices: InvoiceGroup[]
  total: number
}

const { t } = useI18n({ useScope: 'global' })
const { formatNumber } = useLocaleFormatters()

const rows = ref<SmlInvoiceStatsRow[]>([])
const loading = ref(false)
const errorMessage = ref('')
const lookup = ref('')
const page = ref(1)
const customersPerPage = 10
const rowHeaderCount = 7

const startOn = ref('')
const endOn = ref('')

const columnKeys = computed(() => {
  return Array.from(new Set(rows.value.map((row) => toColumnKey(row.year, row.month)))).sort()
})

const customers = computed<CustomerGroup[]>(() => {
  const customerMap = new Map<string, CustomerGroup>()

  for (const row of rows.value) {
    const customerName = row.customerName.trim() || t('sml.invoiceStats.blank')
    const invoiceNumber = row.invoiceNumber.trim() || t('sml.invoiceStats.blank')
    const purchaseOrder = row.purchaseOrder.trim() || t('sml.invoiceStats.blank')
    const productCode = row.productCode.trim() || t('sml.invoiceStats.blank')
    const unit = row.unit.trim() || '-'
    const keyParts = [
      customerName.toLowerCase(),
      invoiceNumber.toLowerCase(),
      purchaseOrder.toLowerCase(),
      productCode.toLowerCase(),
      row.qty,
      unit,
      row.price,
    ]
    const leafKey = keyParts.join('|')
    const invoiceKey = `${customerName.toLowerCase()}|${invoiceNumber.toLowerCase()}`
    const customerKey = customerName.toLowerCase()
    const columnKey = toColumnKey(row.year, row.month)

    let customer = customerMap.get(customerKey)
    if (!customer) {
      customer = {
        key: customerKey,
        customerName,
        invoices: [],
        total: 0,
      }
      customerMap.set(customerKey, customer)
    }

    let invoice = customer.invoices.find((item) => item.key === invoiceKey)
    if (!invoice) {
      invoice = {
        key: invoiceKey,
        invoiceNumber,
        groups: [],
        byColumn: {},
        total: 0,
      }
      customer.invoices.push(invoice)
    }

    let leaf = invoice.groups.find((item) => item.key === leafKey)
    if (!leaf) {
      leaf = {
        key: leafKey,
        customerName,
        invoiceNumber,
        purchaseOrder,
        productCode,
        qty: row.qty,
        unit,
        price: row.price,
        byColumn: {},
        total: 0,
      }
      invoice.groups.push(leaf)
    }

    leaf.byColumn[columnKey] = (leaf.byColumn[columnKey] ?? 0) + row.amount
    leaf.total += row.amount
    invoice.byColumn[columnKey] = (invoice.byColumn[columnKey] ?? 0) + row.amount
    invoice.total += row.amount
    customer.total += row.amount
  }

  return Array.from(customerMap.values())
    .map((customer) => ({
      ...customer,
      invoices: customer.invoices
        .map((invoice) => ({
          ...invoice,
          groups: invoice.groups.sort((left, right) => {
            const purchaseCompare = left.purchaseOrder.localeCompare(right.purchaseOrder)
            if (purchaseCompare !== 0) {
              return purchaseCompare
            }

            const productCompare = left.productCode.localeCompare(right.productCode)
            if (productCompare !== 0) {
              return productCompare
            }

            return left.key.localeCompare(right.key)
          }),
        }))
        .sort((left, right) => left.invoiceNumber.localeCompare(right.invoiceNumber)),
    }))
    .sort((left, right) => left.customerName.localeCompare(right.customerName))
})

const totalPages = computed(() => Math.max(1, Math.ceil(customers.value.length / customersPerPage)))

const pagedCustomers = computed(() => {
  const offset = (page.value - 1) * customersPerPage
  return customers.value.slice(offset, offset + customersPerPage)
})

const grandByColumn = computed<Record<string, number>>(() => {
  const totals: Record<string, number> = {}

  for (const columnKey of columnKeys.value) {
    totals[columnKey] = 0
  }

  for (const customer of customers.value) {
    for (const invoice of customer.invoices) {
      for (const columnKey of columnKeys.value) {
        totals[columnKey] = (totals[columnKey] ?? 0) + (invoice.byColumn[columnKey] ?? 0)
      }
    }
  }

  return totals
})

const grandTotal = computed(() => {
  let total = 0
  for (const customer of customers.value) {
    total += customer.total
  }
  return total
})

watch([lookup, startOn, endOn], () => {
  page.value = 1
})

watch(totalPages, (value) => {
  if (page.value > value) {
    page.value = value
  }
})

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    const response = await getSmlInvoiceStats({
      startOn: startOn.value || undefined,
      endOn: endOn.value || undefined,
      lookup: lookup.value.trim() || undefined,
      take: 20000,
    })

    rows.value = response.rows
  } catch {
    errorMessage.value = t('sml.invoiceStats.loadFailed')
  } finally {
    loading.value = false
  }
}

async function refresh() {
  page.value = 1
  await load()
}

function toColumnKey(year: number, month: number): string {
  if (year <= 0 || month <= 0) {
    return 'unknown'
  }

  return `${year}-${month.toString().padStart(2, '0')}`
}

function formatColumnLabel(columnKey: string): string {
  if (columnKey === 'unknown') {
    return t('sml.invoiceStats.headers.unknownPeriod')
  }

  const [yearText, monthText] = columnKey.split('-')
  const year = Number(yearText)
  const month = Number(monthText)

  if (!Number.isFinite(year) || !Number.isFinite(month)) {
    return columnKey
  }

  return `${year}/${month.toString().padStart(2, '0')}`
}

function formatAmount(value: number): string {
  return formatNumber(value, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })
}

function formatPlainNumber(value: number): string {
  return formatNumber(value, {
    minimumFractionDigits: 0,
    maximumFractionDigits: 4,
  })
}

function exportToCsv() {
  const header = [
    t('sml.invoiceStats.headers.customerName'),
    t('sml.invoiceStats.headers.invoiceNumber'),
    t('sml.invoiceStats.headers.purchaseOrder'),
    t('sml.invoiceStats.headers.productCode'),
    t('sml.invoiceStats.headers.qty'),
    t('sml.invoiceStats.headers.unit'),
    t('sml.invoiceStats.headers.price'),
    ...columnKeys.value.map((column) => formatColumnLabel(column)),
    t('sml.invoiceStats.headers.grandTotal'),
  ]

  const lines = [header.map(csvEscape).join(',')]

  for (const customer of customers.value) {
    lines.push([csvEscape(customer.customerName)].join(','))

    for (const invoice of customer.invoices) {
      for (const group of invoice.groups) {
        const row = [
          csvEscape(''),
          csvEscape(group.invoiceNumber),
          csvEscape(group.purchaseOrder),
          csvEscape(group.productCode),
          csvEscape(formatPlainNumber(group.qty)),
          csvEscape(group.unit),
          csvEscape(formatAmount(group.price)),
          ...columnKeys.value.map((column) => csvEscape(formatAmount(group.byColumn[column] ?? 0))),
          csvEscape(formatAmount(group.total)),
        ]

        lines.push(row.join(','))
      }

      const subtotalRow = [
        csvEscape(''),
        csvEscape(t('sml.invoiceStats.invoiceTotal', { invoiceNumber: invoice.invoiceNumber })),
        csvEscape(''),
        csvEscape(''),
        csvEscape(''),
        csvEscape(''),
        csvEscape(''),
        ...columnKeys.value.map((column) => csvEscape(formatAmount(invoice.byColumn[column] ?? 0))),
        csvEscape(formatAmount(invoice.total)),
      ]

      lines.push(subtotalRow.join(','))
    }
  }

  const totalRow = [
    csvEscape(t('sml.invoiceStats.headers.grandTotal')),
    csvEscape(''),
    csvEscape(''),
    csvEscape(''),
    csvEscape(''),
    csvEscape(''),
    csvEscape(''),
    ...columnKeys.value.map((column) => csvEscape(formatAmount(grandByColumn.value[column] ?? 0))),
    csvEscape(formatAmount(grandTotal.value)),
  ]

  lines.push(totalRow.join(','))

  const blob = new Blob([`\uFEFF${lines.join('\n')}`], { type: 'text/csv;charset=utf-8;' })
  const link = document.createElement('a')
  const timestamp = new Date().toISOString().replace(/[-:T]/g, '').slice(0, 12)

  link.href = URL.createObjectURL(blob)
  link.download = `InvoiceStats_${timestamp}.csv`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(link.href)
}

function csvEscape(value: string): string {
  const escaped = value.replace(/"/g, '""')
  return `"${escaped}"`
}
</script>

<style scoped>
.sml-invoice-stats-page {
  --pivot-shell-border: rgba(var(--v-theme-on-surface), 0.2);
  --pivot-shell-bg: rgb(var(--v-theme-surface));
  --pivot-cell-border: rgba(var(--v-theme-on-surface), 0.15);
  --pivot-head-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 86%, rgb(var(--v-theme-surface)) 14%);
  --pivot-body-bg: color-mix(in srgb, rgb(var(--v-theme-surface)) 95%, rgb(var(--v-theme-on-surface)) 5%);
  --pivot-group-bg: color-mix(in srgb, rgb(var(--v-theme-primary)) 10%, rgb(var(--v-theme-surface)) 90%);
  --pivot-subtotal-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 65%, rgb(var(--v-theme-surface)) 35%);
}

.filter-bar {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(190px, 1fr));
  gap: 12px;
  align-items: center;
}

.pivot-shell {
  overflow: auto;
  border: 1px solid var(--pivot-shell-border);
  border-radius: 10px;
  background: var(--pivot-shell-bg);
}

.pivot-table {
  width: 100%;
  border-collapse: collapse;
  min-width: 1500px;
}

.pivot-table th,
.pivot-table td {
  border: 1px solid var(--pivot-cell-border);
  padding: 7px 10px;
  white-space: nowrap;
  font-size: 0.84rem;
}

.pivot-table thead th,
.pivot-table tfoot th {
  background: var(--pivot-head-bg);
  font-weight: 600;
}

.pivot-table tbody td {
  background: var(--pivot-body-bg);
}

.pivot-table .group-row th {
  background: var(--pivot-group-bg);
  text-align: left;
  font-weight: 700;
}

.pivot-table .subtotal-row th,
.pivot-table .subtotal-row td {
  background: var(--pivot-subtotal-bg);
  font-weight: 600;
}

.pivot-table .label-cell {
  font-weight: 500;
}

.pivot-table .numeric-cell {
  text-align: right;
}

.pivot-table .center-cell {
  text-align: center;
}

.pivot-table .total-cell {
  font-weight: 700;
}

.pager-row {
  margin-top: 12px;
  display: flex;
  justify-content: space-between;
  gap: 10px;
  align-items: center;
  flex-wrap: wrap;
}

@supports not (background: color-mix(in srgb, black, white)) {
  .sml-invoice-stats-page {
    --pivot-head-bg: rgba(var(--v-theme-surface-variant), 0.9);
    --pivot-body-bg: rgba(var(--v-theme-surface), 0.95);
    --pivot-group-bg: rgba(var(--v-theme-primary), 0.08);
    --pivot-subtotal-bg: rgba(var(--v-theme-surface-variant), 0.8);
  }
}
</style>