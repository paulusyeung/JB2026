<template>
  <section class="page-section quotations-page">
    <v-card rounded="xl" elevation="0" class="panel-card quotations-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('quotations.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('quotations.subtitle') }}</p>
        </div>
      </v-card-title>

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

          <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
            {{ t('quotations.actions.checkbox') }}
          </v-btn>

          <v-divider vertical class="mx-1" />

          <v-btn variant="outlined" size="small" prepend-icon="mdi-printer" @click="printList">
            {{ t('quotations.actions.print') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-file-delimited-outline" :disabled="store.rows.length === 0" @click="exportToCsv">
            {{ t('quotations.actions.export') }}
          </v-btn>

          <span class="text-caption text-medium-emphasis" v-if="checkboxMode">
            {{ t('quotations.actions.selected', { count: selectedHeaderIds.length }) }}
          </span>
        </div>

        <v-data-table-server
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
            {{ formatDateYMD(item.createdOn) }}
          </template>
          <template #[`item.modifiedOn`]="{ item }">
            {{ formatDateYMD(item.modifiedOn) }}
          </template>
        </v-data-table-server>
      </v-card-text>
    </v-card>
  </section>

  <v-dialog v-model="formOpen" max-width="860" scrollable>
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
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import QuotationFormDialog from '@/components/forms/QuotationFormDialog.vue'
import { useQuotationsStore } from '@/stores/quotations'
import type { QuotationListItem } from '@/types/api'

type SortItem = { key: string, order: 'asc' | 'desc' }

const store = useQuotationsStore()
const { t } = useI18n({ useScope: 'global' })
const { formatDate: formatDateByLocale } = useLocaleFormatters()

const formOpen = ref(false)
const formQuotation = ref<QuotationListItem | null>(null)
const saveSuccess = ref(false)
const checkboxMode = ref(false)
const selectedHeaderIds = ref<Array<string | number>>([])
const visibleColumnKeys = ref<string[]>([
  'quoteNumber',
  'rowNumber',
  'customerName',
  'printTitle',
  'createdOn',
  'createdBy',
  'modifiedOn',
  'modifiedBy',
])
const sortDirection = ref<'asc' | 'desc'>('desc')
const sortKey = ref('modifiedOn')

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

watch([sortKey, sortDirection], () => {
  store.sortBy = [{ key: sortKey.value, order: sortDirection.value }] as SortItem[]
})

onMounted(async () => {
  const activeSort = store.sortBy[0] as SortItem | undefined
  if (activeSort) {
    sortKey.value = activeSort.key
    sortDirection.value = activeSort.order === 'asc' ? 'asc' : 'desc'
  }

  if (store.rows.length === 0) {
    await store.load()
  }
})

async function applySearch() {
  await store.search()
}

async function refreshList() {
  store.keyword = ''
  await store.load()
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

function openCreate() {
  formQuotation.value = null
  formOpen.value = true
}

function onRowClick(_event: Event, payload: { item: { raw: QuotationListItem } }) {
  if (checkboxMode.value) {
    return
  }

  formQuotation.value = payload.item.raw
  formOpen.value = true
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
        if (dateKeys.has(key)) return `"${formatDateYMD(String(val))}"`
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

function formatDateYMD(value: string | null | undefined) {
  if (!value) return '-'
  const date = new Date(value)
  if (isNaN(date.getTime())) return '-'
  const yyyy = date.getFullYear()
  const mm = String(date.getMonth() + 1).padStart(2, '0')
  const dd = String(date.getDate()).padStart(2, '0')
  return `${yyyy}-${mm}-${dd}`
}

function rowNumber(index: number) {
  return (store.page - 1) * store.itemsPerPage + index + 1
}
</script>

<style scoped>
.quotations-page {
  min-height: 0;
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

.quotations-table :deep(thead th) {
  white-space: nowrap;
  background: rgba(195, 216, 248, 0.7);
}

.quotations-table {
  border-radius: 8px;
  overflow: hidden;
}

.quotations-table :deep(thead th:first-child) {
  border-top-left-radius: 8px;
}

.quotations-table :deep(thead th:last-child) {
  border-top-right-radius: 8px;
}

.quotations-table :deep(tbody td) {
  font-size: 12px;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}
</style>