<template>
  <section class="page-section sml-invoice-list-page">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('sml.invoiceList.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('sml.invoiceList.subtitle') }}</p>
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

        <v-data-table
          :headers="headers"
          :items="rows"
          :loading="loading"
          density="compact"
          fixed-header
          item-value="headerId"
          height="62vh"
          class="invoice-list-table"
        >
          <template #[`item.invoiceNumber`]="{ item }">
            <span class="font-weight-medium">{{ item.invoiceNumber }}</span>
          </template>

          <template #[`item.invoiceDate`]="{ item }">{{ format(item.invoiceDate) }}</template>
          <template #[`item.invoiceAmount`]="{ item }">{{ formatAmount(item.invoiceAmount) }}</template>
          <template #[`item.createdOn`]="{ item }">{{ format(item.createdOn, DATE_FORMATS.SHORT_DATETIME) }}</template>
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

.sml-invoice-list-page .filter-bar > * {
  flex: 1 1 220px;
}

.invoice-list-table {
  border: 1px solid rgba(var(--v-theme-on-surface), 0.12);
  border-radius: 8px;
}
</style>
