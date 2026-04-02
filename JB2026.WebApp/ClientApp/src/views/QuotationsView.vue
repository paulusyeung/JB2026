<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('quotations.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('quotations.subtitle') }}</p>
        </div>
        <v-spacer />
        <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreate">{{ t('quotations.new') }}</v-btn>
        <v-text-field
          v-model="store.keyword"
          density="comfortable"
          :label="t('quotations.search')"
          prepend-inner-icon="mdi-magnify"
          variant="solo-filled"
          hide-details
        />
        <v-btn color="primary" @click="store.search">{{ t('common.search') }}</v-btn>
      </v-card-title>
      <v-card-text>
        <v-data-table-server
          v-model:page="store.page"
          v-model:items-per-page="store.itemsPerPage"
          v-model:sort-by="store.sortBy"
          :headers="headers"
          :items="store.rows"
          :items-length="store.rowCount"
          :loading="store.loading"
          item-value="headerId"
          @click:row="onRowClick"
        >
          <template #[`item.rowNumber`]="{ index }">
            {{ rowNumber(index) }}
          </template>
          <template #[`item.createdOn`]="{ item }">
            {{ formatDate(item.createdOn) }}
          </template>
          <template #[`item.modifiedOn`]="{ item }">
            {{ formatDate(item.modifiedOn) }}
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
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import QuotationFormDialog from '@/components/forms/QuotationFormDialog.vue'
import { useQuotationsStore } from '@/stores/quotations'
import type { QuotationListItem } from '@/types/api'

const store = useQuotationsStore()
const { t } = useI18n({ useScope: 'global' })
const { formatDate: formatDateByLocale } = useLocaleFormatters()

const formOpen = ref(false)
const formQuotation = ref<QuotationListItem | null>(null)
const saveSuccess = ref(false)

const headers = computed(() => [
  { title: t('quotations.headers.quoteNumber'), key: 'quoteNumber' },
  { title: t('quotations.headers.quoteIndex'), key: 'rowNumber', sortable: false },
  { title: t('quotations.headers.customer'), key: 'customerName' },
  { title: t('quotations.headers.title'), key: 'printTitle' },
  { title: t('quotations.headers.createdOn'), key: 'createdOn' },
  { title: t('quotations.headers.createdBy'), key: 'createdBy' },
  { title: t('quotations.headers.modifiedOn'), key: 'modifiedOn' },
  { title: t('quotations.headers.modifiedBy'), key: 'modifiedBy' },
])

onMounted(async () => {
  if (store.rows.length === 0) {
    await store.load()
  }
})

function openCreate() {
  formQuotation.value = null
  formOpen.value = true
}

function onRowClick(_event: Event, payload: { item: { raw: QuotationListItem } }) {
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

function formatDate(value: string) {
  return formatDateByLocale(value)
}

function rowNumber(index: number) {
  return (store.page - 1) * store.itemsPerPage + index + 1
}
</script>