<template>
  <section class="page-section sml-rtf-list-page">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('sml.rtfList.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('sml.rtfList.subtitle') }}</p>
        </div>
        <v-spacer />
        <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="load">
          {{ t('common.refresh') }}
        </v-btn>
      </v-card-title>

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('sml.rtfList.lookup')"
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
            :label="t('sml.rtfList.commonQuery')"
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
                {{ t('sml.rtfList.actions.columns') }}
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
                {{ t('sml.rtfList.actions.sorting') }}
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
                :label="t('sml.rtfList.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('sml.rtfList.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('sml.rtfList.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
            {{ t('sml.rtfList.actions.checkbox') }}
          </v-btn>

          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                {{ t('sml.rtfList.actions.views') }}
              </v-btn>
            </template>
            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>{{ t('sml.rtfList.actions.detailView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>{{ t('sml.rtfList.actions.cardView') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>

        <div v-if="isCardView" class="rtf-mobile-list">
          <v-card v-for="header in displayedRows" :key="header.headerId" rounded="lg" elevation="0" class="rtf-mobile-card">
            <div class="rtf-mobile-card__header">
              <div>
                <div class="text-subtitle-2 font-weight-bold">{{ header.purchaseOrder }}</div>
                <div class="text-caption text-medium-emphasis">{{ header.customerPO || '-' }}</div>
              </div>

              <div class="d-flex align-center ga-2">
                <v-checkbox-btn
                  v-if="checkboxMode"
                  :model-value="selectedHeaderIds.includes(header.headerId)"
                  density="compact"
                  hide-details
                  @click.stop="toggleSelected(header.headerId)"
                />
                <v-btn v-if="getLineItems(header).length > 0" icon variant="text" size="small" @click.stop="toggleExpandedRow(header.headerId)">
                  <v-icon size="18">{{ isExpanded(header.headerId) ? 'mdi-chevron-up' : 'mdi-chevron-down' }}</v-icon>
                </v-btn>
              </div>
            </div>

            <div class="rtf-mobile-card__body">
              <div class="rtf-mobile-card__metrics">
                <span class="text-caption">{{ t('sml.rtfList.headers.orderedBy') }}: {{ header.orderedBy || '-' }}</span>
                <span class="text-caption">{{ t('sml.rtfList.headers.orderedOn') }}: {{ format(header.orderedOn) }}</span>
                <span class="text-caption">{{ t('sml.rtfList.headers.invoiceNumber') }}: {{ header.invoiceNumber || '-' }}</span>
                <span v-if="visibleColumnKeys.includes('dnCount')" class="text-caption">{{ t('sml.rtfList.headers.dnCount') }}: {{ formatNumber(header.dnCount) }}</span>
              </div>
            </div>

            <div class="rtf-mobile-card__meta text-caption text-medium-emphasis">
              <span>{{ t('sml.rtfList.headers.originalPO') }}: {{ header.originalPO || '-' }}</span>
              <span>{{ t('sml.rtfList.headers.salesOrder') }}: {{ header.salesOrder || '-' }}</span>
            </div>

            <v-expand-transition>
              <div v-if="getLineItems(header).length > 0 && isExpanded(header.headerId)" class="rtf-mobile-card__details">
                <div class="rtf-detail-panel">
                  <v-data-table
                    :headers="detailHeaders"
                    :items="getLineItems(header)"
                    :no-data-text="t('sml.rtfList.noLineItems')"
                    density="compact"
                    hide-default-footer
                    class="rtf-detail-table"
                  >
                    <template #item.lineNumber="{ item: lineItem }">{{ formatNumber(lineItem.lineNumber) }}</template>
                    <template #item.productCode="{ item: lineItem }">{{ lineItem.productCode || '-' }}</template>
                    <template #item.productDescription="{ item: lineItem }">{{ lineItem.productDescription || '-' }}</template>
                    <template #item.price="{ item: lineItem }">{{ formatLineAmount(lineItem.price, 4) }}</template>
                    <template #item.qty="{ item: lineItem }">{{ formatLineAmount(lineItem.qty, 0) }}</template>
                    <template #item.amount="{ item: lineItem }">{{ formatLineAmount(lineItem.amount, 2) }}</template>
                  </v-data-table>
                </div>
              </div>
            </v-expand-transition>
          </v-card>
        </div>

        <v-data-table
          v-else
          v-model:expanded="expandedHeaderIds"
          :headers="masterHeaders"
          :items="displayedRows"
          :loading="loading"
          :no-data-text="t('sml.rtfList.empty')"
          v-model="selectedHeaderIds"
          :show-select="checkboxMode"
          item-value="headerId"
          density="compact"
          fixed-header
          height="62vh"
          class="invoice-list-table sml-rtf-list-table"
          @update:expanded="onExpandedChange"
        >
          <template #item.expander="{ item }">
            <v-btn
              variant="text"
              density="comfortable"
              size="x-small"
              icon
              :aria-label="isExpanded(item.headerId) ? t('sml.rtfList.collapseRow') : t('sml.rtfList.expandRow')"
              @click.stop="toggleExpandedRow(item.headerId)"
            >
              <v-icon size="16">{{ isExpanded(item.headerId) ? 'mdi-minus-box-outline' : 'mdi-plus-box-outline' }}</v-icon>
            </v-btn>
          </template>

          <template #item.rowNumber="{ item }">{{ formatNumber(item.rowNumber) }}</template>
          <template #item.purchaseOrder="{ item }"><span class="font-weight-medium">{{ item.purchaseOrder || '-' }}</span></template>
          <template #item.customerPO="{ item }">{{ item.customerPO || '-' }}</template>
          <template #item.orderedBy="{ item }">{{ item.orderedBy || '-' }}</template>
          <template #item.orderedOn="{ item }">{{ format(item.orderedOn) }}</template>
          <template #item.originalPO="{ item }">{{ item.originalPO || '-' }}</template>
          <template #item.salesOrder="{ item }">{{ item.salesOrder || '-' }}</template>
          <template #item.originalSO="{ item }">{{ item.originalSO || '-' }}</template>
          <template #item.dnCount="{ item }">{{ formatNumber(item.dnCount) }}</template>
          <template #item.invoiceNumber="{ item }"><span class="font-weight-medium">{{ item.invoiceNumber || '-' }}</span></template>
          <template #item.createdOn="{ item }">{{ format(item.createdOn, DATE_FORMATS.SHORT_DATETIME) }}</template>
          <template #item.createdBy="{ item }">{{ item.createdBy || '-' }}</template>

          <template #expanded-row="{ item }">
            <tr>
              <td :colspan="masterHeaders.length" class="pa-0">
                <div class="rtf-detail-panel">
                  <v-data-table
                    :headers="detailHeaders"
                    :items="getLineItems(item)"
                    :no-data-text="t('sml.rtfList.noLineItems')"
                    density="compact"
                    hide-default-footer
                    class="rtf-detail-table"
                  >
                    <template #item.lineNumber="{ item: lineItem }">{{ formatNumber(lineItem.lineNumber) }}</template>
                    <template #item.productCode="{ item: lineItem }">{{ lineItem.productCode || '-' }}</template>
                    <template #item.productDescription="{ item: lineItem }">{{ lineItem.productDescription || '-' }}</template>
                    <template #item.price="{ item: lineItem }">{{ formatLineAmount(lineItem.price, 4) }}</template>
                    <template #item.qty="{ item: lineItem }">{{ formatLineAmount(lineItem.qty, 0) }}</template>
                    <template #item.amount="{ item: lineItem }">{{ formatLineAmount(lineItem.amount, 2) }}</template>
                  </v-data-table>
                </div>
              </td>
            </tr>
          </template>
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
import { getSmlRtfList } from '@/services/sml'
import type { SmlRtfListHeader, SmlRtfListItem } from '@/types/api'

const { t } = useI18n({ useScope: 'global' })
const { format, DATE_FORMATS } = useGlobalDateFormatter()
const { formatNumber } = useLocaleFormatters()

const lookup = ref('')
const commonQuery = ref(1)
const loading = ref(false)
const errorMessage = ref('')
const rows = ref<SmlRtfListHeader[]>([])
const expandedHeaderIds = ref<string[]>([])
const selectedHeaderIds = ref<string[]>([])

const {
  visibleColumns: visibleColumnKeys,
  sortKey,
  sortDirection,
  checkboxMode,
  viewMode,
} = useViewSettings('smlrtflist', {
  visibleColumns: [
    'expander',
    'purchaseOrder',
    'rowNumber',
    'customerPO',
    'orderedBy',
    'orderedOn',
    'originalPO',
    'salesOrder',
    'originalSO',
    'dnCount',
    'invoiceNumber',
    'createdOn',
    'createdBy',
  ],
  sortKey: 'purchaseOrder',
  sortDirection: 'asc',
  checkboxMode: false,
  viewMode: 'detail',
})

const commonQueryItems = computed(() => [
  { value: 1, label: t('sml.rtfList.commonQueryItems.thirty') },
  { value: 2, label: t('sml.rtfList.commonQueryItems.sixty') },
  { value: 3, label: t('sml.rtfList.commonQueryItems.ninety') },
  { value: 0, label: t('sml.rtfList.commonQueryItems.all') },
])

const allHeaders = computed(() => [
  { title: '', key: 'expander', width: '42px', sortable: false },
  { title: t('sml.rtfList.headers.purchaseOrder'), key: 'purchaseOrder', width: '140px' },
  { title: t('sml.rtfList.headers.rowNumber'), key: 'rowNumber', width: '56px' },
  { title: t('sml.rtfList.headers.customerPO'), key: 'customerPO', minWidth: '160px' },
  { title: t('sml.rtfList.headers.orderedBy'), key: 'orderedBy', width: '120px' },
  { title: t('sml.rtfList.headers.orderedOn'), key: 'orderedOn', width: '130px' },
  { title: t('sml.rtfList.headers.originalPO'), key: 'originalPO', width: '140px' },
  { title: t('sml.rtfList.headers.salesOrder'), key: 'salesOrder', width: '140px' },
  { title: t('sml.rtfList.headers.originalSO'), key: 'originalSO', width: '140px' },
  { title: t('sml.rtfList.headers.dnCount'), key: 'dnCount', width: '88px', align: 'end' as const },
  { title: t('sml.rtfList.headers.invoiceNumber'), key: 'invoiceNumber', width: '140px' },
  { title: t('sml.rtfList.headers.createdOn'), key: 'createdOn', width: '160px' },
  { title: t('sml.rtfList.headers.createdBy'), key: 'createdBy', width: '120px' },
])

const masterHeaders = computed(() => allHeaders.value.filter((header) => visibleColumnKeys.value.includes(String(header.key))))

const detailHeaders = computed(() => [
  { title: t('sml.rtfList.headers.lineNumber'), key: 'lineNumber', width: '70px' },
  { title: t('sml.rtfList.headers.productCode'), key: 'productCode', width: '180px' },
  { title: t('sml.rtfList.headers.productDescription'), key: 'productDescription', minWidth: '300px' },
  { title: t('sml.rtfList.headers.price'), key: 'price', width: '130px', align: 'end' as const },
  { title: t('sml.rtfList.headers.qty'), key: 'qty', width: '110px', align: 'end' as const },
  { title: t('sml.rtfList.headers.amount'), key: 'amount', width: '130px', align: 'end' as const },
])

const sortableColumns = computed(() =>
  allHeaders.value
    .filter((header) => header.sortable !== false && header.key !== 'expander')
    .map((header) => ({ key: String(header.key), title: String(header.title) })),
)

const columnOptions = computed(() => allHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title) })))

const displayedRows = computed(() => {
  const result = [...rows.value]
  const key = sortKey.value as keyof SmlRtfListHeader

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
    const response = await getSmlRtfList({
      lookup: lookup.value.trim() || undefined,
      commonQuery: lookup.value.trim() ? undefined : commonQuery.value,
      take: 500,
    })

    rows.value = response.headers
    expandedHeaderIds.value = []
  } catch {
    errorMessage.value = t('sml.rtfList.loadFailed')
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

function onExpandedChange(value: unknown) {
  expandedHeaderIds.value = normalizeExpandedIds(value)
}

function toggleExpandedRow(headerId: string) {
  if (expandedHeaderIds.value.includes(headerId)) {
    expandedHeaderIds.value = expandedHeaderIds.value.filter((expandedId) => expandedId !== headerId)
    return
  }

  expandedHeaderIds.value = [...expandedHeaderIds.value, headerId]
}

function isExpanded(headerId: string) {
  return expandedHeaderIds.value.includes(headerId)
}

function getLineItems(item: SmlRtfListHeader): SmlRtfListItem[] {
  return Array.isArray(item.items) ? item.items : []
}

function normalizeExpandedIds(value: unknown): string[] {
  if (!Array.isArray(value)) {
    return []
  }

  return value.map((entry) => String(entry))
}

function parseNumber(value: string | number | null | undefined): number | null {
  if (value === null || value === undefined || value === '') {
    return null
  }

  if (typeof value === 'number') {
    return Number.isFinite(value) ? value : null
  }

  const parsed = Number(String(value).replace(/,/g, '').trim())
  return Number.isFinite(parsed) ? parsed : null
}

function splitNumericSuffix(value: string | number | null | undefined): { numericValue: string; suffix: string } | null {
  if (value === null || value === undefined) {
    return null
  }

  const text = String(value).trim()
  if (!text) {
    return null
  }

  const match = text.match(/^(-?[\d,]+(?:\.\d+)?)(.*)$/)
  if (!match) {
    return null
  }

  const numericValue = match[1]
  const suffix = match[2]

  if (numericValue === undefined || suffix === undefined) {
    return null
  }

  return {
    numericValue,
    suffix,
  }
}

function formatLineAmount(value: string | number | null | undefined, fractionDigits: number): string {
  const split = splitNumericSuffix(value)

  if (!split) {
    const parsed = parseNumber(value)
    if (parsed === null) {
      return String(value ?? '-').trim() || '-'
    }

    return formatNumber(parsed, {
      minimumFractionDigits: fractionDigits,
      maximumFractionDigits: fractionDigits,
    })
  }

  const parsed = Number(split.numericValue.replace(/,/g, ''))
  if (!Number.isFinite(parsed)) {
    return String(value ?? '-').trim() || '-'
  }

  const formatted = formatNumber(parsed, {
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  })

  return `${formatted}${split.suffix}`
}
</script>

<style scoped>
.sml-rtf-list-page .filter-bar,
.sml-rtf-list-page .toolbar-bar {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.sml-rtf-list-page .toolbar-bar {
  margin-top: 16px;
}

.sml-rtf-list-page {
  --sml-rtf-header-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%);
  --sml-rtf-header-fg: rgb(var(--v-theme-on-surface-variant));
}

.sml-rtf-list-page .filter-bar > * {
  flex: 1 1 220px;
}

.invoice-list-table {
  border: 1px solid rgba(var(--v-theme-on-surface), 0.12);
  border-radius: 8px;
  overflow: hidden;
}

.sml-rtf-list-table :deep(.v-table__wrapper > table > thead > tr > th),
.sml-rtf-list-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--sml-rtf-header-bg) !important;
  color: var(--sml-rtf-header-fg) !important;
  vertical-align: middle !important;
  text-align: center !important;
}

.sml-rtf-list-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.sml-rtf-list-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.sml-rtf-list-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.sml-rtf-list-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

.rtf-detail-panel {
  overflow-x: auto;
  background: rgba(var(--v-theme-on-surface), 0.035);
  border-top: 1px solid rgba(var(--v-theme-on-surface), 0.08);
}

.rtf-detail-table {
  background: #f5f5f5;
}

:deep(.rtf-detail-table .v-data-table__th),
:deep(.rtf-detail-table .v-data-table__td) {
  min-height: 32px;
  height: 32px;
  padding-top: 4px;
  padding-bottom: 4px;
}

:deep(.invoice-list-table .v-data-table__th) {
  vertical-align: middle;
}

:deep(.invoice-list-table .v-data-table__td) {
  vertical-align: top;
}

.rtf-mobile-list {
  display: grid;
  gap: 12px;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  align-items: start;
}

.rtf-mobile-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.16);
  background: rgba(246, 250, 255, 0.95);
  padding: 12px;
}

.rtf-mobile-card__header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 12px;
}

.rtf-mobile-card__body {
  margin-top: 8px;
}

.rtf-mobile-card__metrics {
  display: grid;
  gap: 4px;
}

.rtf-mobile-card__meta {
  display: flex;
  flex-wrap: wrap;
  gap: 8px 16px;
  margin-top: 8px;
}

.rtf-mobile-card__details {
  margin-top: 10px;
  border-top: 1px solid rgba(var(--v-theme-primary), 0.2);
  padding-top: 10px;
}

:global(.v-theme--dark) .rtf-mobile-card {
  background: rgba(32, 46, 66, 0.9);
}
</style>