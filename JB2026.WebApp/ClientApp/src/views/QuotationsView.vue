<template>
  <section class="page-section quotations-page" :class="{ 'quotations-page--dark': isDark }">
    <v-card rounded="xl" elevation="0" class="panel-card quotations-card">


      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="store.keyword"
            density="comfortable"
            :label="t('quotations.search')"
            prepend-inner-icon="mdi-magnify"
            variant="solo-filled"
            hide-details
            clearable
            @keydown.enter="applySearch"
          />

          <v-btn color="primary" prepend-icon="mdi-magnify" :loading="store.loading" @click="applySearch">
            {{ t('common.search') }}
          </v-btn>

          <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="store.loading" @click="refreshList">
            {{ t('common.refresh') }}
          </v-btn>

          <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreate">{{ t('quotations.new') }}</v-btn>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ errorMessage }}</v-alert>

        <div class="toolbar-bar mb-2 mt-3">
          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-view-column">
                {{ t('quotations.actions.columns') }}
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
                {{ t('quotations.actions.sorting') }}
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
                :label="t('quotations.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('quotations.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('quotations.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <template v-if="!isPhoneLayout">
            <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
              {{ t('quotations.actions.checkbox') }}
            </v-btn>

            <v-menu location="bottom">
              <template #activator="{ props }">
                <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                  {{ viewLabel }}
                </v-btn>
              </template>
              <v-list density="compact" class="toolbar-menu-list">
                <v-list-item prepend-icon="mdi-table" :active="viewMode === 'table'" @click="setViewMode('table')">
                  <v-list-item-title>{{ tableViewLabel }}</v-list-item-title>
                </v-list-item>
                <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                  <v-list-item-title>{{ cardViewLabel }}</v-list-item-title>
                </v-list-item>
              </v-list>
            </v-menu>

            <v-divider vertical class="mx-1" />

            <v-btn variant="outlined" size="small" prepend-icon="mdi-printer" @click="printList">
              {{ t('quotations.actions.print') }}
            </v-btn>

            <v-btn variant="outlined" size="small" prepend-icon="mdi-file-delimited-outline" :disabled="store.rows.length === 0" @click="exportToCsv">
              {{ t('quotations.actions.export') }}
            </v-btn>
          </template>

          <v-menu v-else location="bottom end">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                {{ t('common.action') }}
              </v-btn>
            </template>

            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
                <v-list-item-title>{{ t('quotations.actions.checkbox') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-refresh" :disabled="store.loading" @click="refreshList">
                <v-list-item-title>{{ t('common.refresh') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-printer" @click="printList">
                <v-list-item-title>{{ t('quotations.actions.print') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-file-delimited-outline" :disabled="store.rows.length === 0" @click="exportToCsv">
                <v-list-item-title>{{ t('quotations.actions.export') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-plus" @click="openCreate">
                <v-list-item-title>{{ t('quotations.new') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <span class="text-caption text-medium-emphasis" v-if="checkboxMode">
            {{ t('quotations.actions.selected', { count: selectedHeaderIds.length }) }}
          </span>
        </div>

        <ListMobileCard
          v-if="isPhoneLayout"
          :items="store.rows"
          :columns="mobileColumns"
          item-key="headerId"
          :checkbox-mode="checkboxMode"
          :selected-ids="selectedHeaderIdsAsString"
          :on-select="handleMobileSelect"
          :on-card-click="(item) => openQuotation(item as QuotationListItem)"
        >
          <template #actions="{ item }">
            <v-menu location="bottom end">
              <template #activator="{ props }">
                <v-btn v-bind="props" variant="text" size="small" class="text-none">
                  {{ t('common.action') }}
                  <v-icon end size="16">mdi-chevron-down</v-icon>
                </v-btn>
              </template>

              <v-list density="compact" class="toolbar-menu-list">
                <v-list-item prepend-icon="mdi-open-in-app" @click.stop="openQuotation(item as QuotationListItem)">
                  <v-list-item-title>{{ t('common.open') }}</v-list-item-title>
                </v-list-item>
                <v-list-item prepend-icon="mdi-printer" @click.stop="printList">
                  <v-list-item-title>{{ t('quotations.actions.print') }}</v-list-item-title>
                </v-list-item>
                <v-list-item prepend-icon="mdi-file-delimited-outline" :disabled="store.rows.length === 0" @click.stop="exportToCsv">
                  <v-list-item-title>{{ t('quotations.actions.export') }}</v-list-item-title>
                </v-list-item>
              </v-list>
            </v-menu>
          </template>
        </ListMobileCard>

        <div v-else-if="isCardView" class="quotations-card-list">
          <v-card
            v-for="row in store.rows"
            :key="row.headerId"
            rounded="lg"
            elevation="0"
            class="quotations-item-card"
            @click="openQuotation(row)"
          >
            <div class="card-header">
              <span class="font-weight-bold card-quote-number">{{ row.quoteNumber }}</span>
              <v-checkbox-btn
                v-if="checkboxMode"
                :model-value="selectedHeaderIds.includes(row.headerId)"
                @click.stop="toggleSelectedQuotation(row.headerId)"
                class="card-checkbox"
              />
            </div>

            <div class="card-body">
              <div class="d-flex align-center ga-2 mb-2">
                <span class="text-caption text-medium-emphasis">{{ row.customerName }}</span>
              </div>
              <div class="card-title">{{ row.printTitle }}</div>
            </div>

            <div class="card-footer text-caption text-medium-emphasis">
              <span>{{ t('quotations.headers.createdOn') }}: {{ format(row.createdOn) }}</span>
              <span>{{ t('quotations.headers.modifiedOn') }}: {{ format(row.modifiedOn) }}</span>
            </div>
          </v-card>
        </div>

        <v-data-table-server
          v-else
          v-model:page="store.page"
          v-model:items-per-page="store.itemsPerPage"
          v-model:sort-by="store.sortBy"
          v-model="selectedHeaderIds"
          :headers="headers"
          :items="store.rows"
          :items-length="store.rowCount"
          :loading="store.loading"
          :show-select="checkboxMode"
          item-value="headerId"
          class="quotations-table"
          @click:row="onRowClick"
        >
          <template #[`item.rowNumber`]="{ index }">
            {{ rowNumber(index) }}
          </template>
          <template #[`item.createdOn`]="{ item }">
            {{ format(item.createdOn) }}
          </template>
          <template #[`item.modifiedOn`]="{ item }">
            {{ format(item.modifiedOn) }}
          </template>
        </v-data-table-server>
      </v-card-text>
    </v-card>
  </section>

  <v-dialog v-model="formOpen" max-width="min(100%, 860px)" scrollable>
    <QuotationFormDialog
      :quotation="formQuotation"
      @saved="handleSave"
      @cancel="formOpen = false"
    />
  </v-dialog>

  <v-snackbar v-model="saveSuccess" color="success" timeout="3000">
    {{ t('quotations.saved') }}
    <template #actions>
      <v-btn variant="text" @click="saveSuccess = false">{{ t('quotations.dismiss') }}</v-btn>
    </template>
  </v-snackbar>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useTheme } from 'vuetify'
import ListMobileCard, { type ListMobileCardColumn } from '@/components/grids/ListMobileCard.vue'
import { useResponsiveList } from '@/composables/useResponsiveList'
import { useViewSettings } from '@/composables/useColumnPersistence'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import QuotationFormDialog from '@/components/forms/QuotationFormDialog.vue'
import { useQuotationsStore } from '@/stores/quotations'
import type { QuotationListItem } from '@/types/api'

type SortItem = { key: string, order: 'asc' | 'desc' }

const store = useQuotationsStore()
const { t } = useI18n({ useScope: 'global' })
const { format, DATE_FORMATS } = useGlobalDateFormatter()
const { formatNumber } = useLocaleFormatters()
const theme = useTheme()
const isDark = computed(() => theme.global.current.value.dark)

const formOpen = ref(false)
const formQuotation = ref<QuotationListItem | null>(null)
const saveSuccess = ref(false)
const errorMessage = ref('')
const selectedHeaderIds = ref<Array<string | number>>([])
const defaultColumnKeys = [
  'quoteNumber',
  'rowNumber',
  'customerName',
  'printTitle',
  'createdOn',
  'createdBy',
  'modifiedOn',
  'modifiedBy',
]
const viewSettings = useViewSettings('quotations', {
  visibleColumns: defaultColumnKeys,
  sortKey: 'modifiedOn',
  sortDirection: 'desc',
  checkboxMode: false,
  viewMode: 'table' as 'table' | 'card',
})
const visibleColumnKeys = viewSettings.visibleColumns
const sortKey = viewSettings.sortKey
const sortDirection = viewSettings.sortDirection
const checkboxMode = viewSettings.checkboxMode
const viewMode = viewSettings.viewMode
const { isPhoneLayout } = useResponsiveList()

const allHeaders = computed(() => [
  { title: t('quotations.headers.quoteNumber'), key: 'quoteNumber' },
  { title: t('quotations.headers.quoteIndex'), key: 'rowNumber', sortable: false },
  { title: t('quotations.headers.customer'), key: 'customerName' },
  { title: t('quotations.headers.title'), key: 'printTitle' },
  { title: t('quotations.headers.createdOn'), key: 'createdOn' },
  { title: t('quotations.headers.createdBy'), key: 'createdBy' },
  { title: t('quotations.headers.modifiedOn'), key: 'modifiedOn' },
  { title: t('quotations.headers.modifiedBy'), key: 'modifiedBy' },
])

const headers = computed(() => allHeaders.value.filter((header) => visibleColumnKeys.value.includes(String(header.key))))

const sortableColumns = computed(() =>
  allHeaders.value
    .filter((header) => header.sortable !== false)
    .map((header) => ({ key: String(header.key), title: String(header.title) })),
)

const columnOptions = computed(() => allHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title) })))
const selectedHeaderIdsAsString = computed(() => selectedHeaderIds.value.map((id) => String(id)))
const isCardView = computed(() => viewMode.value === 'card')
const viewLabel = computed(() => t('quotations.actions.view'))
const tableViewLabel = computed(() => t('quotations.actions.tableView'))
const cardViewLabel = computed(() => t('quotations.actions.cardView'))

const mobileColumns = computed<ListMobileCardColumn<QuotationListItem>[]>(() => [
  { key: 'quoteNumberIndexPair', label: t('quotations.headers.quoteNumber'), section: 'header', emphasis: true },
  { key: 'customerName', label: t('quotations.headers.customer'), section: 'header' },
  { key: 'printTitle', label: t('quotations.headers.title'), section: 'body' },
  {
    key: 'createdOn',
    label: t('quotations.headers.createdOn'),
    section: 'footer',
    formatter: (item) => format(item.createdOn),
  },
  {
    key: 'modifiedOn',
    label: t('quotations.headers.modifiedOn'),
    section: 'footer',
    formatter: (item) => format(item.modifiedOn),
  },
])

watch([sortKey, sortDirection], () => {
  store.sortBy = [{ key: sortKey.value as string, order: sortDirection.value as 'asc' | 'desc' }] as SortItem[]
})

onMounted(async () => {
  const activeSort = store.sortBy[0] as SortItem | undefined
  if (activeSort) {
    sortKey.value = activeSort.key
    sortDirection.value = activeSort.order === 'asc' ? 'asc' : 'desc'
  }

  if (store.rows.length === 0) {
    try {
      await store.load()
    } catch {
      errorMessage.value = t('quotations.loadFailed')
    }
  }
})

async function applySearch() {
  try {
    await store.search()
    errorMessage.value = ''
  } catch {
    errorMessage.value = t('quotations.searchFailed')
  }
}

async function refreshList() {
  store.keyword = ''
  try {
    await store.load()
    errorMessage.value = ''
  } catch {
    errorMessage.value = t('quotations.loadFailed')
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

function setViewMode(mode: 'table' | 'card') {
  viewMode.value = mode
}

function toggleSelectedQuotation(headerId: string | number) {
  if (selectedHeaderIds.value.includes(headerId)) {
    selectedHeaderIds.value = selectedHeaderIds.value.filter((id) => id !== headerId)
    return
  }

  selectedHeaderIds.value = [...selectedHeaderIds.value, headerId]
}

function openCreate() {
  formQuotation.value = null
  formOpen.value = true
}

function openQuotation(quotation: QuotationListItem) {
  formQuotation.value = quotation
  formOpen.value = true
}

function handleMobileSelect(item: Record<string, unknown>, selected: boolean) {
  const headerId = String(item.headerId ?? '')
  if (!headerId) {
    return
  }

  if (selected && !selectedHeaderIdsAsString.value.includes(headerId)) {
    selectedHeaderIds.value = [...selectedHeaderIdsAsString.value, headerId]
    return
  }

  if (!selected) {
    selectedHeaderIds.value = selectedHeaderIdsAsString.value.filter((id) => id !== headerId)
  }
}

function onRowClick(_event: Event, payload: { item: { raw: QuotationListItem } }) {
  if (checkboxMode.value) {
    return
  }

  openQuotation(payload.item.raw)
}

function handleSave(quotation: QuotationListItem) {
  const index = store.rows.findIndex((row) => row.headerId === quotation.headerId)

  if (index === -1) {
    store.rows.unshift(quotation)
  } else {
    store.rows.splice(index, 1, quotation)
  }

  formOpen.value = false
  saveSuccess.value = true
}

function printList() {
  window.print()
}

function exportToCsv() {
  const exportCols = headers.value
  const headerRow = exportCols.map((h) => `"${String(h.title).replace(/"/g, '""')}"`).join(',')
  const dateKeys = new Set(['createdOn', 'modifiedOn'])

  const dataRows = store.rows.map((row, index) =>
    exportCols
      .map((h) => {
        const key = String(h.key)
        if (key === 'rowNumber') {
          return `"${rowNumber(index)}"`
        }

        const val = row[key as keyof QuotationListItem]
        if (val == null) return '""'
        if (dateKeys.has(key)) return `"${format(String(val), DATE_FORMATS.ISO_DATE)}"`
        return `"${String(val).replace(/"/g, '""')}"`
      })
      .join(','),
  )

  const csv = '\uFEFF' + [headerRow, ...dataRows].join('\r\n')
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = `quotations-${new Date().toISOString().slice(0, 10)}.csv`
  anchor.click()
  URL.revokeObjectURL(url)
}



function rowNumber(index: number) {
  return (store.page - 1) * store.itemsPerPage + index + 1
}
</script>

<style scoped>
.quotations-page {
  min-height: 0;
  --quotations-header-bg: rgba(195, 216, 248, 0.92);
  --quotations-header-fg: inherit;
}

.quotations-page--dark {
  --quotations-header-bg: rgba(52, 74, 104, 0.95);
  --quotations-header-fg: rgba(239, 246, 255, 0.98);
}

.quotations-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
  background: linear-gradient(180deg, rgba(224, 237, 255, 0.9), rgba(240, 247, 255, 0.95));
}

.filter-bar {
  display: grid;
  gap: 12px;
  grid-template-columns: minmax(280px, 1fr) auto auto auto;
  align-items: center;
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

.quotations-table :deep(.v-table__wrapper > table > thead > tr > th),
.quotations-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--quotations-header-bg) !important;
  color: var(--quotations-header-fg) !important;
}

.quotations-table {
  border-radius: 8px;
  overflow: hidden;
}

.quotations-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.quotations-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.quotations-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.quotations-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

.quotations-table :deep(tbody td) {
  font-size: 12px;
}

.quotations-card-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .quotations-card-list {
    grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
    align-items: start;
  }
}

.quotations-item-card {
  display: grid;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgba(255, 255, 255, 0.72);
  cursor: pointer;
  position: relative;
}

.quotations-item-card:active {
  background: rgba(255, 255, 255, 0.92);
}

.card-header,
.card-footer {
  display: flex;
  align-items: flex-start;
  width: 100%;
}

.card-header {
  position: relative;
  justify-content: flex-start;
}

.card-footer {
  justify-content: space-between;
}

.card-body {
  display: grid;
  gap: 0.45rem;
}

.card-title {
  font-weight: 500;
  white-space: normal;
  word-break: break-word;
}

.card-quote-number {
  flex: 1;
  min-width: 0;
}

.card-checkbox {
  flex-shrink: 0;
  position: absolute;
  top: 0.5rem;
  right: 0.5rem;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }

  .card-footer {
    flex-direction: column;
  }
}
</style>