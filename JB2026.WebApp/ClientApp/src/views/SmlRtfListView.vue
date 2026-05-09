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
          <v-btn size="small" variant="outlined" color="primary" prepend-icon="mdi-printer" disabled>
            {{ t('sml.rtfList.actions.printPo') }}
          </v-btn>
          <v-btn size="small" variant="outlined" prepend-icon="mdi-paperclip" disabled>
            {{ t('sml.rtfList.actions.attachment') }}
          </v-btn>
          <v-btn size="small" variant="outlined" prepend-icon="mdi-file-document-outline" disabled>
            {{ t('sml.rtfList.actions.printInvoice') }}
          </v-btn>
          <v-btn size="small" variant="outlined" color="error" prepend-icon="mdi-delete" disabled>
            {{ t('sml.rtfList.actions.delete') }}
          </v-btn>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>



        <v-data-table
          v-model:expanded="expandedHeaderIds"
          :headers="masterHeaders"
          :items="rows"
          :loading="loading"
          :no-data-text="t('sml.rtfList.empty')"
          density="compact"
          fixed-header
          item-value="headerId"
          height="62vh"
          class="invoice-list-table sml-rtf-list-table"
          @update:expanded="onExpandedChange"
        >
          <template #[`item.expander`]="{ item }">
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

          <template #[`item.rowNumber`]="{ item }">{{ formatNumber(item.rowNumber) }}</template>
          <template #[`item.purchaseOrder`]="{ item }"><span class="font-weight-medium">{{ item.purchaseOrder || '-' }}</span></template>
          <template #[`item.customerPO`]="{ item }">{{ item.customerPO || '-' }}</template>
          <template #[`item.orderedBy`]="{ item }">{{ item.orderedBy || '-' }}</template>
          <template #[`item.orderedOn`]="{ item }">{{ format(item.orderedOn) }}</template>
          <template #[`item.originalPO`]="{ item }">{{ item.originalPO || '-' }}</template>
          <template #[`item.salesOrder`]="{ item }">{{ item.salesOrder || '-' }}</template>
          <template #[`item.originalSO`]="{ item }">{{ item.originalSO || '-' }}</template>
          <template #[`item.dnCount`]="{ item }">{{ formatNumber(item.dnCount) }}</template>
          <template #[`item.invoiceNumber`]="{ item }"><span class="font-weight-medium">{{ item.invoiceNumber || '-' }}</span></template>
          <template #[`item.createdOn`]="{ item }">{{ format(item.createdOn, DATE_FORMATS.SHORT_DATETIME) }}</template>
          <template #[`item.createdBy`]="{ item }">{{ item.createdBy || '-' }}</template>

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
                    <template #[`item.lineNumber`]="{ item: lineItem }">{{ formatNumber(lineItem.lineNumber) }}</template>
                    <template #[`item.productCode`]="{ item: lineItem }">{{ lineItem.productCode || '-' }}</template>
                    <template #[`item.productDescription`]="{ item: lineItem }">{{ lineItem.productDescription || '-' }}</template>
                    <template #[`item.price`]="{ item: lineItem }">{{ formatLineAmount(lineItem.price, 2) }}</template>
                    <template #[`item.qty`]="{ item: lineItem }">{{ formatLineAmount(lineItem.qty, 4) }}</template>
                    <template #[`item.amount`]="{ item: lineItem }">{{ formatLineAmount(lineItem.amount, 2) }}</template>
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

const commonQueryItems = computed(() => [
  { value: 1, label: t('sml.rtfList.commonQueryItems.thirty') },
  { value: 2, label: t('sml.rtfList.commonQueryItems.sixty') },
  { value: 3, label: t('sml.rtfList.commonQueryItems.ninety') },
  { value: 0, label: t('sml.rtfList.commonQueryItems.all') },
])

const masterHeaders = computed(() => [
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

const detailHeaders = computed(() => [
  { title: t('sml.rtfList.headers.lineNumber'), key: 'lineNumber', width: '70px' },
  { title: t('sml.rtfList.headers.productCode'), key: 'productCode', width: '180px' },
  { title: t('sml.rtfList.headers.productDescription'), key: 'productDescription', minWidth: '300px' },
  { title: t('sml.rtfList.headers.price'), key: 'price', width: '130px', align: 'end' as const },
  { title: t('sml.rtfList.headers.qty'), key: 'qty', width: '110px', align: 'end' as const },
  { title: t('sml.rtfList.headers.amount'), key: 'amount', width: '130px', align: 'end' as const },
])

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

function formatLineAmount(value: string | number | null | undefined, fractionDigits: number): string {
  const parsed = parseNumber(value)

  if (parsed === null) {
    return String(value ?? '-').trim() || '-'
  }

  return formatNumber(parsed, {
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  })
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
  --sml-rtf-header-bg: rgba(195, 216, 248, 0.92);
  --sml-rtf-header-fg: inherit;
}

:global(.v-theme--dark) .sml-rtf-list-page {
  --sml-rtf-header-bg: rgba(52, 74, 104, 0.95);
  --sml-rtf-header-fg: rgba(239, 246, 255, 0.98);
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
</style>
