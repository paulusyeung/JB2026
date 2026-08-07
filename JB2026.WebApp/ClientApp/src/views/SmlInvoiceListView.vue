<template>
  <section class="page-section sml-invoice-list-page">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="load">
          {{ t('common.refresh') }}
        </v-btn>
      </v-card-title>

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('sml.invoiceList.lookup')"
            prepend-inner-icon="mdi-magnify"
            variant="solo-filled"
            hide-details
            clearable
            @keydown.enter="applyLookup"
          />

          <v-select
            v-model="commonQuery"
            :items="commonQueryItems"
            item-title="label"
            item-value="value"
            :label="t('sml.invoiceList.commonQuery')"
            variant="solo-filled"
            density="comfortable"
            hide-details
          />

          <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loading" @click="applyLookup">
            {{ t('common.search') }}
          </v-btn>
        </div>

        <div class="toolbar-bar mb-3">
          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-view-column">
                {{ t('sml.invoiceList.actions.columns') }}
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
                {{ t('sml.invoiceList.actions.sorting') }}
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
                :label="t('sml.invoiceList.actions.sortBy')"
                hide-details
              />
              <div class="sort-direction-controls mt-3">
                <v-btn
                  size="small"
                  variant="outlined"
                  :color="sortDirection === 'asc' ? 'primary' : undefined"
                  @click="sortDirection = 'asc'"
                >
                  {{ t('sml.invoiceList.actions.asc') }}
                </v-btn>
                <v-btn
                  size="small"
                  variant="outlined"
                  :color="sortDirection === 'desc' ? 'primary' : undefined"
                  @click="sortDirection = 'desc'"
                >
                  {{ t('sml.invoiceList.actions.desc') }}
                </v-btn>
              </div>
            </v-card>
          </v-menu>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
            {{ t('sml.invoiceList.actions.checkbox') }}
          </v-btn>

          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                {{ t('sml.invoiceList.actions.views') }}
              </v-btn>
            </template>
            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>{{ t('sml.invoiceList.actions.detailView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>{{ t('sml.invoiceList.actions.cardView') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <v-divider vertical class="toolbar-separator" />

          <v-btn size="small" variant="outlined" color="primary" prepend-icon="mdi-plus" disabled>
            {{ t('sml.invoiceList.actions.newSupplier') }}
          </v-btn>
          <v-btn size="small" variant="outlined" prepend-icon="mdi-paperclip" disabled>
            {{ t('sml.invoiceList.actions.attachment') }}
          </v-btn>
          <v-btn size="small" variant="outlined" prepend-icon="mdi-file-excel" disabled>
            {{ t('sml.invoiceList.actions.export') }}
          </v-btn>
          <v-btn size="small" variant="outlined" color="error" prepend-icon="mdi-delete" disabled>
            {{ t('sml.invoiceList.actions.delete') }}
          </v-btn>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>

        <div v-if="isCardView" class="invoice-mobile-list">
          <v-card v-for="row in displayedRows" :key="row.headerId" rounded="lg" elevation="0" class="invoice-mobile-card">
            <div class="invoice-mobile-card__header">
              <div>
                <div class="text-subtitle-2 font-weight-bold">{{ row.invoiceNumber || '-' }}</div>
                <div class="text-caption text-medium-emphasis">{{ row.customerName || '-' }}</div>
              </div>

              <v-checkbox-btn
                v-if="checkboxMode"
                :model-value="selectedHeaderIds.includes(row.headerId)"
                density="compact"
                hide-details
                @click.stop="toggleSelected(row.headerId)"
              />
            </div>

            <div class="invoice-mobile-card__body">
              <div class="invoice-mobile-card__metrics">
                <span class="text-caption">{{ t('sml.invoiceList.headers.rowNumber') }}: {{ row.rowNumber ?? '-' }}</span>
                <span class="text-caption">{{ t('sml.invoiceList.headers.invoiceDate') }}: {{ format(row.invoiceDate) }}</span>
                <span class="text-caption">{{ t('sml.invoiceList.headers.invoiceAmount') }}: {{ formatAmount(row.invoiceAmount) }}</span>
              </div>
            </div>

            <div class="invoice-mobile-card__meta text-caption text-medium-emphasis">
              <span>{{ t('sml.invoiceList.headers.icNumber') }}: {{ row.icNumber || '-' }}</span>
              <span>{{ t('sml.invoiceList.headers.createdBy') }}: {{ row.createdBy || '-' }}</span>
              <span>{{ t('sml.invoiceList.headers.createdOn') }}: {{ format(row.createdOn, DATE_FORMATS.SHORT_DATETIME) }}</span>
            </div>
          </v-card>
        </div>

        <v-data-table
          v-else
          :headers="masterHeaders"
          :items="displayedRows"
          :loading="loading"
          v-model="selectedHeaderIds"
          :show-select="checkboxMode"
          density="compact"
          fixed-header
          item-value="headerId"
          height="62vh"
          class="invoice-list-table sml-invoice-list-table"
        >
          <template #item.rowNumber="{ item }">{{ item.rowNumber ?? '-' }}</template>
          <template #item.invoiceNumber="{ item }">
            <span class="font-weight-medium">{{ item.invoiceNumber || '-' }}</span>
          </template>
          <template #item.customerName="{ item }">{{ item.customerName || '-' }}</template>
          <template #item.invoiceDate="{ item }">{{ format(item.invoiceDate) }}</template>
          <template #item.invoiceAmount="{ item }">{{ formatAmount(item.invoiceAmount) }}</template>
          <template #item.icNumber="{ item }">{{ item.icNumber || '-' }}</template>
          <template #item.createdOn="{ item }">{{ format(item.createdOn, DATE_FORMATS.SHORT_DATETIME) }}</template>
          <template #item.createdBy="{ item }">{{ item.createdBy || '-' }}</template>
        </v-data-table>

      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { useViewSettings } from '@/composables/useColumnPersistence'
import { getSmlInvoiceList } from '@/services/sml'
import type { SmlInvoiceListRow } from '@/types/api'

const { t } = useI18n({ useScope: 'global' })
const { format, DATE_FORMATS } = useGlobalDateFormatter()
const { activeLocale } = useLocaleFormatters()

const lookup = ref('')
const commonQuery = ref(1)
const loading = ref(false)
const errorMessage = ref('')
const rows = ref<SmlInvoiceListRow[]>([])
const selectedHeaderIds = ref<string[]>([])

const {
  visibleColumns: visibleColumnKeys,
  sortKey,
  sortDirection,
  checkboxMode,
  viewMode,
} = useViewSettings('smlinvoicelist', {
  visibleColumns: [
    'invoiceNumber',
    'rowNumber',
    'customerName',
    'invoiceDate',
    'invoiceAmount',
    'icNumber',
    'createdOn',
    'createdBy',
  ],
  sortKey: 'invoiceNumber',
  sortDirection: 'asc',
  checkboxMode: false,
  viewMode: 'detail',
})

const commonQueryItems = computed(() => [
  { value: 1, label: t('sml.invoiceList.commonQueryItems.thirty') },
  { value: 2, label: t('sml.invoiceList.commonQueryItems.sixty') },
  { value: 3, label: t('sml.invoiceList.commonQueryItems.ninety') },
  { value: 0, label: t('sml.invoiceList.commonQueryItems.all') },
])

const headers = computed(() => [
  { title: t('sml.invoiceList.headers.invoiceNumber'), key: 'invoiceNumber', width: '140px' },
  { title: t('sml.invoiceList.headers.rowNumber'), key: 'rowNumber', width: '52px' },
  { title: t('sml.invoiceList.headers.customerName'), key: 'customerName', minWidth: '240px' },
  { title: t('sml.invoiceList.headers.invoiceDate'), key: 'invoiceDate', width: '130px' },
  { title: t('sml.invoiceList.headers.invoiceAmount'), key: 'invoiceAmount', width: '140px', align: 'end' as const },
  { title: t('sml.invoiceList.headers.icNumber'), key: 'icNumber', width: '120px' },
  { title: t('sml.invoiceList.headers.createdOn'), key: 'createdOn', width: '160px' },
  { title: t('sml.invoiceList.headers.createdBy'), key: 'createdBy', width: '120px' },
])

const masterHeaders = computed(() => headers.value.filter((header) => visibleColumnKeys.value.includes(String(header.key))))

const sortableColumns = computed(() =>
  headers.value.map((header) => ({ key: String(header.key), title: String(header.title) })),
)

const columnOptions = computed(() => headers.value.map((header) => ({ key: String(header.key), title: String(header.title) })))

const displayedRows = computed(() => {
  const result = [...rows.value]
  const key = sortKey.value as keyof SmlInvoiceListRow

  result.sort((lhs, rhs) => {
    const leftValue = lhs[key]
    const rightValue = rhs[key]

    if (leftValue == null && rightValue == null) return 0
    if (leftValue == null) return sortDirection.value === 'asc' ? -1 : 1
    if (rightValue == null) return sortDirection.value === 'asc' ? 1 : -1

    if (typeof leftValue === 'number' && typeof rightValue === 'number') {
      return sortDirection.value === 'asc' ? leftValue - rightValue : rightValue - leftValue
    }

    const left = String(leftValue)
    const right = String(rightValue)
    return sortDirection.value === 'asc' ? left.localeCompare(right) : right.localeCompare(left)
  })

  return result
})

const isCardView = computed(() => viewMode.value === 'card')

onMounted(async () => {
  await load()
})

watch(commonQuery, async (value) => {
  if (!lookup.value.trim() && value >= 0) {
    await load()
  }
})

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    const response = await getSmlInvoiceList({
      lookup: lookup.value.trim() || undefined,
      commonQuery: lookup.value.trim() ? undefined : commonQuery.value,
      take: 500,
    })

    rows.value = response.rows
  } catch {
    errorMessage.value = t('sml.invoiceList.loadFailed')
  } finally {
    loading.value = false
  }
}

async function applyLookup() {
  await load()
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

function setViewMode(mode: 'detail' | 'card') {
  viewMode.value = mode
}

function toggleSelected(headerId: string) {
  if (selectedHeaderIds.value.includes(headerId)) {
    selectedHeaderIds.value = selectedHeaderIds.value.filter((id) => id !== headerId)
    return
  }

  selectedHeaderIds.value = [...selectedHeaderIds.value, headerId]
}

function formatAmount(value: number) {
  return new Intl.NumberFormat(activeLocale.value, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value)
}

</script>

<style scoped>
.sml-invoice-list-page .filter-bar,
.sml-invoice-list-page .toolbar-bar {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.sml-invoice-list-page .toolbar-bar {
  margin-top: 12px;
}

.sml-invoice-list-page {
  --sml-invoice-header-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%);
  --sml-invoice-header-fg: rgb(var(--v-theme-on-surface-variant));
}

.toolbar-separator {
  margin: 0 4px;
}

.sort-direction-controls {
  display: flex;
  gap: 8px;
}

.sml-invoice-list-page .filter-bar > * {
  flex: 1 1 220px;
}

.invoice-list-table {
  border: 1px solid rgba(var(--v-theme-on-surface), 0.12);
  border-radius: 8px;
  overflow: hidden;
}

.sml-invoice-list-table :deep(.v-table__wrapper > table > thead > tr > th),
.sml-invoice-list-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--sml-invoice-header-bg) !important;
  color: var(--sml-invoice-header-fg) !important;
  vertical-align: middle !important;
  text-align: center !important;
}

.sml-invoice-list-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.sml-invoice-list-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.sml-invoice-list-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.sml-invoice-list-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

.invoice-mobile-list {
  display: grid;
  gap: 12px;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  align-items: start;
}

.invoice-mobile-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.18);
  background: rgb(var(--v-theme-surface));
  color: rgba(var(--v-theme-on-surface), 0.92);
  padding: 12px;
}

.invoice-mobile-card :deep(.text-medium-emphasis) {
  color: rgba(var(--v-theme-on-surface), 0.72) !important;
  opacity: 1;
}

.invoice-mobile-card :deep(.text-caption) {
  color: rgba(var(--v-theme-on-surface), 0.86);
}

.invoice-mobile-card__header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 12px;
}

.invoice-mobile-card__header .text-subtitle-2 {
  color: rgba(var(--v-theme-on-surface), 0.95);
}

.invoice-mobile-card__body {
  margin-top: 8px;
}

.invoice-mobile-card__metrics {
  display: grid;
  gap: 4px;
}

.invoice-mobile-card__meta {
  display: flex;
  flex-wrap: wrap;
  gap: 8px 16px;
  margin-top: 8px;
}
</style>
