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
          <v-btn size="small" variant="outlined" prepend-icon="mdi-paperclip" disabled>
            {{ t('sml.rtfList.actions.attachment') }}
          </v-btn>
          <v-btn size="small" variant="outlined" prepend-icon="mdi-printer" disabled>
            {{ t('sml.rtfList.actions.printInvoice') }}
          </v-btn>
          <v-btn size="small" variant="outlined" prepend-icon="mdi-tag-text-outline" disabled>
            {{ t('sml.rtfList.actions.printLabels') }}
          </v-btn>
          <v-btn size="small" variant="outlined" prepend-icon="mdi-file-document-outline" disabled>
            {{ t('sml.rtfList.actions.printPo') }}
          </v-btn>
          <v-btn size="small" variant="outlined" color="error" prepend-icon="mdi-delete" disabled>
            {{ t('sml.rtfList.actions.delete') }}
          </v-btn>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>

        <v-data-table
          :headers="headers"
          :items="rows"
          :loading="loading"
          density="compact"
          fixed-header
          item-value="headerId"
          v-model:expanded="expanded"
          height="62vh"
          class="rtf-list-table"
          @click:row="onRowClick"
        >
          <template #[`item.expander`]="{ item }">
            <v-btn
              v-if="item.items.length > 0"
              variant="text"
              density="comfortable"
              size="x-small"
              icon
              @click.stop="toggleExpandRow(item)"
            >
              <v-icon size="16">{{ isRowExpanded(item) ? 'mdi-minus-box-outline' : 'mdi-plus-box-outline' }}</v-icon>
            </v-btn>
          </template>

          <template #[`item.fileType`]="{ item }">
            <v-icon size="16" :color="item.rtfFileName.toLowerCase().endsWith('.xls') ? 'success' : 'primary'">
              {{ item.rtfFileName.toLowerCase().endsWith('.xls') ? 'mdi-file-excel' : 'mdi-file-document-outline' }}
            </v-icon>
          </template>

          <template #[`item.purchaseOrder`]="{ item }">
            <span class="font-weight-medium">{{ item.purchaseOrder }}</span>
          </template>

          <template #[`item.orderedOn`]="{ item }">{{ formatDate(item.orderedOn) }}</template>
          <template #[`item.createdOn`]="{ item }">{{ formatDateTime(item.createdOn) }}</template>

          <template #[`item.isLabelPrinted`]="{ item }">
            <div class="d-flex justify-center">
              <v-icon size="14" :color="item.isLabelPrinted ? 'success' : 'error'">
                {{ item.isLabelPrinted ? 'mdi-circle' : 'mdi-circle-outline' }}
              </v-icon>
            </div>
          </template>

          <template #expanded-row="{ item }">
            <tr>
              <td :colspan="headers.length" class="pa-0">
                <v-data-table
                  :headers="detailHeaders"
                  :items="detailRowsFor(item)"
                  density="compact"
                  hide-default-footer
                  class="detail-grid"
                />
              </td>
            </tr>
          </template>
        </v-data-table>

        <div class="text-caption text-medium-emphasis mt-2">
          {{ t('sml.rtfList.rows', { count: rows.length }) }}
        </div>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { getSmlRtfList } from '@/services/sml'
import type { SmlRtfListHeader } from '@/types/api'

const { t } = useI18n({ useScope: 'global' })
const { activeLocale, formatDate: formatDateByLocale } = useLocaleFormatters()

const lookup = ref('')
const commonQuery = ref(1)
const loading = ref(false)
const errorMessage = ref('')
const rows = ref<SmlRtfListHeader[]>([])
const expanded = ref<string[]>([])

const commonQueryItems = computed(() => [
  { value: 1, label: t('sml.rtfList.commonQueryItems.thirty') },
  { value: 2, label: t('sml.rtfList.commonQueryItems.sixty') },
  { value: 3, label: t('sml.rtfList.commonQueryItems.ninety') },
  { value: 0, label: t('sml.rtfList.commonQueryItems.all') },
])

const headers = computed(() => [
  { title: '', key: 'expander', sortable: false, width: '42px' },
  { title: '', key: 'fileType', sortable: false, width: '34px' },
  { title: t('sml.rtfList.headers.purchaseOrder'), key: 'purchaseOrder', width: '170px' },
  { title: t('sml.rtfList.headers.rowNumber'), key: 'rowNumber', width: '48px' },
  { title: t('sml.rtfList.headers.customerPO'), key: 'customerPO', width: '160px' },
  { title: t('sml.rtfList.headers.orderedBy'), key: 'orderedBy', width: '140px' },
  { title: t('sml.rtfList.headers.orderedOn'), key: 'orderedOn', width: '120px' },
  { title: t('sml.rtfList.headers.originalPO'), key: 'originalPO', width: '160px' },
  { title: t('sml.rtfList.headers.salesOrder'), key: 'salesOrder', width: '160px' },
  { title: t('sml.rtfList.headers.originalSO'), key: 'originalSO', width: '160px' },
  { title: '', key: 'isLabelPrinted', sortable: false, width: '48px' },
  { title: t('sml.rtfList.headers.invoiceNumber'), key: 'invoiceNumber', width: '120px' },
  { title: t('sml.rtfList.headers.createdOn'), key: 'createdOn', width: '160px' },
  { title: t('sml.rtfList.headers.createdBy'), key: 'createdBy', width: '120px' },
])

const detailHeaders = computed(() => [
  { title: t('sml.rtfList.headers.lineNumber'), key: 'lineNumber', width: '70px' },
  { title: t('sml.rtfList.headers.productCode'), key: 'productCode', width: '180px' },
  { title: t('sml.rtfList.headers.productDescription'), key: 'productDescription', minWidth: '300px' },
  { title: t('sml.rtfList.headers.price'), key: 'price', width: '130px', align: 'end' as const },
  { title: t('sml.rtfList.headers.qty'), key: 'qty', width: '120px', align: 'end' as const },
  { title: t('sml.rtfList.headers.amount'), key: 'amount', width: '130px', align: 'end' as const },
])

onMounted(async () => {
  await load()
})

watch(commonQuery, async (value) => {
  if (!lookup.value.trim() && value > 0) {
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
    expanded.value = []
  } catch {
    errorMessage.value = t('sml.rtfList.loadFailed')
  } finally {
    loading.value = false
  }
}

async function applyLookup() {
  if (!lookup.value.trim()) {
    await load()
    return
  }

  await load()
}

function detailRowsFor(row: SmlRtfListHeader) {
  return row.items
}

function isRowExpanded(row: SmlRtfListHeader) {
  return expanded.value.includes(row.headerId)
}

function toggleExpandRow(row: SmlRtfListHeader) {
  if (isRowExpanded(row)) {
    expanded.value = []
    return
  }

  expanded.value = [row.headerId]
}

function onRowClick(_event: Event, payload: { item: SmlRtfListHeader }) {
  if (payload.item.items.length === 0) {
    expanded.value = []
    return
  }

  toggleExpandRow(payload.item)
}

function formatDate(value: string) {
  return formatDateByLocale(value)
}

function formatDateTime(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return ''
  }

  return new Intl.DateTimeFormat(activeLocale.value, {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date)
}
</script>

<style scoped>
.sml-rtf-list-page .filter-bar,
.sml-rtf-list-page .toolbar-bar {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.sml-rtf-list-page .filter-bar > * {
  flex: 1 1 220px;
}

.rtf-list-table,
.detail-grid {
  border: 1px solid rgba(var(--v-theme-on-surface), 0.12);
  border-radius: 8px;
}

.detail-grid :deep(tbody tr) {
  height: 32px;
}

.detail-grid :deep(tbody tr:nth-child(odd)) {
  background: rgba(var(--v-theme-surface-variant), 0.25);
}

.detail-grid :deep(tbody td) {
  vertical-align: top;
}
</style>
