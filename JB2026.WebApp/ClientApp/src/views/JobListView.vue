<template>
  <section class="page-section job-list-page" :class="{ 'job-list-page--dark': isDark }">
    <v-card rounded="xl" elevation="0" class="panel-card job-list-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('jobOrder.jobList.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('jobOrder.jobList.subtitle') }}</p>
        </div>
      </v-card-title>

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('jobOrder.jobList.lookup')"
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
            :label="t('jobOrder.jobList.commonQuery')"
            variant="solo-filled"
            density="comfortable"
            hide-details
          />

          <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loading" @click="applyLookup">
            {{ t('jobOrder.jobList.search') }}
          </v-btn>

          <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="refreshList">
            {{ t('jobOrder.jobList.actions.refresh') }}
          </v-btn>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ errorMessage }}</v-alert>
        <v-alert
          v-if="showInitialWindowNotice"
          type="info"
          variant="tonal"
          class="mt-3 mb-2"
        >
          {{ t('jobOrder.jobList.initialWindowNotice') }}
        </v-alert>

        <div class="toolbar-bar mb-2">
          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-view-column">
                {{ t('jobOrder.jobList.actions.columns') }}
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
                {{ t('jobOrder.jobList.actions.sorting') }}
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
                :label="t('jobOrder.jobList.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('jobOrder.jobList.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('jobOrder.jobList.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <template v-if="!isPhoneLayout">
            <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
              {{ t('jobOrder.jobList.actions.checkbox') }}
            </v-btn>

            <v-menu location="bottom">
              <template #activator="{ props }">
                <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                  {{ t('jobOrder.jobList.actions.views') }}
                </v-btn>
              </template>
              <v-list density="compact" class="toolbar-menu-list">
                <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                  <v-list-item-title>{{ detailViewLabel }}</v-list-item-title>
                </v-list-item>
                <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                  <v-list-item-title>{{ cardViewLabel }}</v-list-item-title>
                </v-list-item>
              </v-list>
            </v-menu>

            <v-divider vertical class="mx-1" />

            <v-btn variant="outlined" size="small" prepend-icon="mdi-paperclip" :disabled="attachmentAndPrintDisabled" @click="openAttachmentDialog">
              {{ t('jobForm.actions.attachment') }}
            </v-btn>

            <v-btn variant="outlined" size="small" prepend-icon="mdi-printer" :disabled="attachmentAndPrintDisabled" @click="printList">
              {{ t('jobOrder.jobList.actions.print') }}
            </v-btn>

            <v-btn variant="outlined" size="small" prepend-icon="mdi-file-delimited-outline" :disabled="rows.length === 0" @click="exportToCsv">
              {{ t('jobOrder.jobList.actions.export') }}
            </v-btn>

            <v-btn
              variant="outlined"
              size="small"
              color="primary"
              prepend-icon="mdi-file-plus"
              class="toolbar-new-order-btn"
              @click="openNew"
            >
              {{ t('jobOrder.jobList.actions.newOrder') }}
            </v-btn>

            <v-btn
              variant="tonal"
              color="error"
              size="small"
              prepend-icon="mdi-delete"
              :disabled="selectedOrderIds.length === 0 || deleting"
              :loading="deleting"
              @click="confirmBatchDelete"
            >
              {{ t('jobOrder.jobList.actions.deleteSelected') }}
            </v-btn>

            <span class="text-caption text-medium-emphasis" v-if="checkboxMode">
              {{ t('jobOrder.jobList.actions.selected', { count: selectedOrderIds.length }) }}
            </span>

            <span class="text-caption text-medium-emphasis" v-else-if="activeRow">
              {{ t('jobOrder.jobList.selectedOrder', { order: compositeOrderNumber(activeRow) }) }}
            </span>

            <span class="text-caption text-medium-emphasis" v-else>
              {{ t('jobOrder.jobList.noSelection') }}
            </span>
          </template>

          <v-menu v-else location="bottom end">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                {{ t('jobOrder.jobList.actions.more') }}
              </v-btn>
            </template>

            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
                <v-list-item-title>{{ t('jobOrder.jobList.actions.checkbox') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>{{ detailViewLabel }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>{{ cardViewLabel }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-paperclip" :disabled="attachmentAndPrintDisabled" @click="openAttachmentDialog">
                <v-list-item-title>{{ t('jobForm.actions.attachment') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-printer" :disabled="attachmentAndPrintDisabled" @click="printList">
                <v-list-item-title>{{ t('jobOrder.jobList.actions.print') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-file-delimited-outline" :disabled="rows.length === 0" @click="exportToCsv">
                <v-list-item-title>{{ t('jobOrder.jobList.actions.export') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-file-plus" @click="openNew">
                <v-list-item-title>{{ t('jobOrder.jobList.actions.newOrder') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-delete" :disabled="selectedOrderIds.length === 0 || deleting" @click="confirmBatchDelete">
                <v-list-item-title>{{ t('jobOrder.jobList.actions.deleteSelected') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>
        </div>

        <div v-if="isCardView" class="job-mobile-list">
          <v-card
            v-for="row in displayedRows"
            :key="row.orderId"
            rounded="lg"
            elevation="0"
            class="job-mobile-card"
            @click="openEditor(row)"
          >
            <div class="job-mobile-card__header">
              <div class="d-flex align-center ga-2">
                <v-icon size="18" :color="orderTypeMeta(row.orderType).color">
                  {{ orderTypeMeta(row.orderType).icon }}
                </v-icon>
                <div>
                  <div class="text-subtitle-2 font-weight-bold">{{ compositeOrderNumber(row) }}</div>
                  <div class="text-caption text-medium-emphasis">{{ row.customerName || '-' }}</div>
                </div>
              </div>

              <v-checkbox-btn
                v-if="checkboxMode"
                :model-value="selectedOrderIds.includes(row.orderId)"
                density="compact"
                hide-details
                @click.stop="toggleSelected(row.orderId)"
              />
            </div>

            <div class="job-mobile-card__body">
              <div class="d-flex align-center ga-2 mb-2">
                <v-chip size="small" :color="statusColor(row.status)" variant="tonal">
                  <v-icon start size="12" :color="statusColor(row.status)">mdi-flag</v-icon>
                  {{ row.status }}
                </v-chip>
                <span class="text-caption">{{ row.orderTitle || '-' }}</span>
              </div>

              <div class="job-mobile-card__metrics">
                <span class="text-caption">{{ t('jobOrder.jobList.headers.quotation') }}: {{ row.productStyle || '-' }}</span>
                <span class="text-caption font-weight-medium">{{ t('jobOrder.jobList.headers.invoiceAmount') }}: {{ formatCurrency(row.invoiceAmount) }}</span>
              </div>
            </div>

            <div class="job-mobile-card__footer text-caption text-medium-emphasis">
              <span>{{ t('jobOrder.jobList.headers.orderedOn') }}: {{ format(row.orderedOn) }}</span>
              <span>{{ t('jobOrder.jobList.headers.requiredOn') }}: {{ format(row.requiredOn) }}</span>
            </div>

            <div class="job-mobile-card__meta text-caption text-medium-emphasis">
              <span>{{ t('jobOrder.jobList.headers.modifiedBy') }}: {{ row.modifiedBy || '-' }}</span>
              <span>{{ t('jobOrder.jobList.headers.modifiedOn') }}: {{ format(row.modifiedOn) }}</span>
            </div>

            <div class="job-mobile-card__actions">
              <v-menu location="bottom end">
                <template #activator="{ props }">
                  <v-btn v-bind="props" variant="text" size="small" class="text-none">
                    {{ t('jobOrder.jobList.actions.more') }}
                    <v-icon end size="16">mdi-chevron-down</v-icon>
                  </v-btn>
                </template>
                <v-list density="compact" class="toolbar-menu-list">
                  <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
                    <v-list-item-title>{{ t('jobOrder.jobList.actions.checkbox') }}</v-list-item-title>
                  </v-list-item>
                  <v-list-item prepend-icon="mdi-paperclip" :disabled="attachmentAndPrintDisabled" @click.stop="openAttachmentDialog">
                    <v-list-item-title>{{ t('jobForm.actions.attachment') }}</v-list-item-title>
                  </v-list-item>
                  <v-list-item prepend-icon="mdi-printer" :disabled="attachmentAndPrintDisabled" @click.stop="printList">
                    <v-list-item-title>{{ t('jobOrder.jobList.actions.print') }}</v-list-item-title>
                  </v-list-item>
                  <v-list-item prepend-icon="mdi-file-delimited-outline" :disabled="displayedRows.length === 0" @click.stop="exportToCsv">
                    <v-list-item-title>{{ t('jobOrder.jobList.actions.export') }}</v-list-item-title>
                  </v-list-item>
                  <v-list-item prepend-icon="mdi-file-plus" @click.stop="openNew">
                    <v-list-item-title>{{ t('jobOrder.jobList.actions.newOrder') }}</v-list-item-title>
                  </v-list-item>
                </v-list>
              </v-menu>
            </div>
          </v-card>
        </div>

        <div v-else class="job-table-shell">
          <v-data-table
            :headers="headers"
            :items="displayedRows"
            :loading="loading"
            v-model="selectedOrderIds"
            :show-select="checkboxMode"
            item-value="orderId"
            density="compact"
            fixed-header
            height="62vh"
            class="job-list-table"
            @click:row="onRowClick"
          >
            <template #[`item.ln`]="{ index }">{{ index + 1 }}</template>

            <template #[`header.orderType`]>
              <span class="sr-only">{{ t('jobOrder.jobList.headers.orderType') }}</span>
              <v-icon size="14" color="primary">mdi-tag-outline</v-icon>
            </template>

            <template #[`header.status`]>
              <span class="sr-only">{{ t('jobOrder.jobList.headers.status') }}</span>
              <v-icon size="14" color="primary">mdi-flag</v-icon>
            </template>

            <template #[`header.attachProduct`]>
              <span class="sr-only">{{ t('jobOrder.jobList.headers.attachProduct') }}</span>
              <v-icon size="14" color="primary">mdi-paperclip</v-icon>
            </template>

            <template #[`header.attachCustomer`]>
              <span class="sr-only">{{ t('jobOrder.jobList.headers.attachCustomer') }}</span>
              <v-icon size="14" color="primary">mdi-paperclip</v-icon>
            </template>

            <template #[`item.orderType`]="{ item }">
              <div class="d-flex justify-center">
                <v-icon size="16" :color="orderTypeMeta(item.orderType).color">{{ orderTypeMeta(item.orderType).icon }}</v-icon>
              </div>
            </template>

            <template #[`item.orderNumber`]="{ item }">
              <v-btn variant="text" color="primary" density="comfortable" class="px-0 text-none" @click.stop="openEditor(item)">
                {{ compositeOrderNumber(item) }}
              </v-btn>
            </template>

            <template #[`item.status`]="{ item }">
              <div class="d-flex justify-center">
                <v-icon size="16" :color="statusColor(item.status)">mdi-flag</v-icon>
              </div>
            </template>

            <template #[`item.attachProduct`]="{ item }">
              <div class="d-flex justify-center">
                <v-icon size="14" :color="item.attachmentProductCount > 0 ? 'success' : 'error'">
                  {{ item.attachmentProductCount > 0 ? 'mdi-paperclip' : 'mdi-circle-outline' }}
                </v-icon>
              </div>
            </template>

            <template #[`item.attachCustomer`]="{ item }">
              <div class="d-flex justify-center">
                <v-icon size="14" :color="item.attachmentCustomerCount > 0 ? 'success' : 'error'">
                  {{ item.attachmentCustomerCount > 0 ? 'mdi-paperclip' : 'mdi-circle-outline' }}
                </v-icon>
              </div>
            </template>

            <template #[`item.orderedOn`]="{ item }">{{ format(item.orderedOn) }}</template>
            <template #[`item.requiredOn`]="{ item }">{{ format(item.requiredOn) }}</template>
            <template #[`item.completedOn`]="{ item }">{{ format(item.completedOn) }}</template>
            <template #[`item.modifiedOn`]="{ item }">{{ format(item.modifiedOn) }}</template>
            <template #[`item.modifiedBy`]="{ item }">{{ item.modifiedBy || '-' }}</template>
            <template #[`item.invoiceRef`]="{ item }">{{ item.invoiceRef || '-' }}</template>
            <template #[`item.invoiceAmount`]="{ item }">{{ formatCurrency(item.invoiceAmount) }}</template>
            <template #[`item.productStyle`]="{ item }">{{ item.productStyle || '-' }}</template>
          </v-data-table>
        </div>
      </v-card-text>
    </v-card>

    <v-dialog v-model="formOpen" max-width="min(100%, 760px)" scrollable>
      <JobOrderForm
        v-if="formOpen"
        :job="formJob"
        @saved="handleSaved"
        @cancel="formOpen = false"
        @attachment="handleAttachment"
        @print-order="handlePrintOrder"
        @workflow="handleWorkflow"
        @product-details-edit="handleProductDetailsEdit"
      />
    </v-dialog>

    <JobOrderActionDialogs
      :job="formJob"
      v-model:attachment-open="attachmentDialogOpen"
      v-model:product-details-open="productDetailsDialogOpen"
      @updated="handleActionUpdated"
      @error="showActionNotice"
    />

    <JobOrderPrintManagerDialog
      v-model="printManagerOpen"
      :order-id="printManagerJob?.orderId ?? null"
      :order-number="printManagerJob?.orderNumber ?? ''"
      :style-titles="printManagerJob?.styleTitles"
    />

    <v-snackbar v-model="saveSuccess" color="success" timeout="3000">
      {{ t('jobOrder.saved') }}
      <template #actions>
        <v-btn variant="text" @click="saveSuccess = false">{{ t('jobOrder.dismiss') }}</v-btn>
      </template>
    </v-snackbar>

    <v-snackbar v-model="actionNoticeOpen" color="info" timeout="3200">
      {{ actionNoticeMessage }}
    </v-snackbar>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useDisplay, useTheme } from 'vuetify'
import JobOrderActionDialogs from '@/components/forms/JobOrderActionDialogs.vue'
import JobOrderForm from '@/components/forms/JobOrderForm.vue'
import JobOrderPrintManagerDialog from '@/components/forms/JobOrderPrintManagerDialog.vue'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { useViewSettings } from '@/composables/useColumnPersistence'
import { getJobDetail } from '@/services/jobs'
import { deleteJobOrder, getJobList } from '@/services/jobOrders'
import type { JobDetail, JobOrderRecord } from '@/types/api'

type JobListViewMode = 'detail' | 'card'

const rows = ref<JobOrderRecord[]>([])
const loading = ref(false)
const deleting = ref(false)
const errorMessage = ref('')
const lookup = ref('')
const commonQuery = ref(0)
const selectedOrderIds = ref<string[]>([])
const activeOrderId = ref<string | null>(null)
const defaultColumnKeys = [
  'orderType',
  'ln',
  'orderNumber',
  'status',
  'orderedOn',
  'customerName',
  'orderTitle',
  'attachProduct',
  'customerRef',
  'attachCustomer',
  'orderedBy',
  'productStyle',
  'invoiceAmount',
  'invoiceRef',
  'requiredOn',
  'modifiedOn',
  'modifiedBy',
  'completedOn',
]
const viewSettings = useViewSettings('joblist', {
  visibleColumns: defaultColumnKeys,
  sortKey: 'orderNumber',
  sortDirection: 'desc',
  checkboxMode: false,
  viewMode: 'detail',
})
const visibleColumnKeys = viewSettings.visibleColumns
const sortKey = viewSettings.sortKey
const sortDirection = viewSettings.sortDirection
const checkboxMode = viewSettings.checkboxMode
const viewMode = viewSettings.viewMode
const formOpen = ref(false)
const formJob = ref<JobDetail | null>(null)
const saveSuccess = ref(false)
const actionNoticeOpen = ref(false)
const actionNoticeMessage = ref('')
const attachmentDialogOpen = ref(false)
const productDetailsDialogOpen = ref(false)
const printManagerOpen = ref(false)
const printManagerJob = ref<JobDetail | null>(null)

const { t } = useI18n({ useScope: 'global' })
const { format, DATE_FORMATS } = useGlobalDateFormatter()
const { formatCurrency: formatCurrencyByLocale, formatNumber } = useLocaleFormatters()
const theme = useTheme()
const display = useDisplay()
const router = useRouter()
const isDark = computed(() => theme.global.current.value.dark)
const isPhoneLayout = computed(() => display.smAndDown.value)
const detailViewLabel = computed(() => t('jobOrder.jobList.actions.detailView'))
const cardViewLabel = computed(() => t('jobOrder.jobList.actions.cardView'))
const isCardView = computed(() => viewMode.value === 'card')

const commonQueryItems = computed(() => [
  { value: 0, label: t('jobOrder.jobList.commonQueryItems.none') },
  { value: 1, label: t('jobOrder.jobList.commonQueryItems.ordered30') },
  { value: 2, label: t('jobOrder.jobList.commonQueryItems.ordered90') },
])

const allHeaders = computed(() => [
  { title: t('jobOrder.jobList.headers.orderType'), key: 'orderType', width: '52px', sortable: false },
  { title: t('jobOrder.jobList.headers.ln'), key: 'ln', width: '52px', sortable: false },
  { title: t('jobOrder.jobList.headers.order'), key: 'orderNumber', width: '132px' },
  { title: t('jobOrder.jobList.headers.status'), key: 'status', width: '72px', sortable: false },
  { title: t('jobOrder.jobList.headers.orderedOn'), key: 'orderedOn', width: '122px' },
  { title: t('jobOrder.jobList.headers.customer'), key: 'customerName', minWidth: '220px' },
  { title: t('jobOrder.jobList.headers.orderTitle'), key: 'orderTitle', minWidth: '240px' },
  { title: t('jobOrder.jobList.headers.attachProduct'), key: 'attachProduct', width: '72px', sortable: false },
  { title: t('jobOrder.jobList.headers.customerRef'), key: 'customerRef', width: '160px' },
  { title: t('jobOrder.jobList.headers.attachCustomer'), key: 'attachCustomer', width: '72px', sortable: false },
  { title: t('jobOrder.jobList.headers.orderedBy'), key: 'orderedBy', width: '100px' },
  { title: t('jobOrder.jobList.headers.quotation'), key: 'productStyle', width: '120px' },
  { title: t('jobOrder.jobList.headers.invoiceAmount'), key: 'invoiceAmount', width: '132px', align: 'end' as const },
  { title: t('jobOrder.jobList.headers.invoiceRef'), key: 'invoiceRef', width: '110px' },
  { title: t('jobOrder.jobList.headers.requiredOn'), key: 'requiredOn', width: '122px' },
  { title: t('jobOrder.jobList.headers.modifiedOn'), key: 'modifiedOn', width: '122px' },
  { title: t('jobOrder.jobList.headers.modifiedBy'), key: 'modifiedBy', width: '100px' },
  { title: t('jobOrder.jobList.headers.completedOn'), key: 'completedOn', width: '122px' },
])

const headers = computed(() => allHeaders.value.filter((header) => visibleColumnKeys.value.includes(String(header.key))))

const sortableColumns = computed(() =>
  allHeaders.value
    .filter((header) => header.sortable !== false && header.key !== 'status' && header.key !== 'attachProduct' && header.key !== 'attachCustomer')
    .map((header) => ({ key: String(header.key), title: String(header.title) })),
)

const columnOptions = computed(() => allHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title) })))

const hasActiveFilters = computed(() => lookup.value.trim().length > 0 || commonQuery.value > 0)
const showInitialWindowNotice = computed(() => !hasActiveFilters.value && rows.value.length > 0)

const hasSingleSelection = computed(() => selectedOrderIds.value.length === 1)
const attachmentAndPrintDisabled = computed(() => !hasSingleSelection.value)

const displayedRows = computed(() => {
  const result = [...rows.value]
  const key = (sortKey.value ?? 'orderNumber') as keyof JobOrderRecord
  const direction = sortDirection.value ?? 'desc'

  result.sort((lhs, rhs) => {
    const leftValue = valueForSort(lhs, key)
    const rightValue = valueForSort(rhs, key)

    if (leftValue == null && rightValue == null) return 0
    if (leftValue == null) return direction === 'asc' ? -1 : 1
    if (rightValue == null) return direction === 'asc' ? 1 : -1

    if (typeof leftValue === 'number' && typeof rightValue === 'number') {
      return direction === 'asc' ? leftValue - rightValue : rightValue - leftValue
    }

    const left = String(leftValue)
    const right = String(rightValue)
    const compareOptions: Intl.CollatorOptions = { numeric: true, sensitivity: 'base' }
    return direction === 'asc' ? left.localeCompare(right, undefined, compareOptions) : right.localeCompare(left, undefined, compareOptions)
  })

  return result
})

const activeRow = computed(() => rows.value.find((row) => row.orderId === activeOrderId.value) ?? null)

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''
  selectedOrderIds.value = []
  try {
    rows.value = await getJobList({
      lookup: lookup.value.trim() || undefined,
      commonQuery: commonQuery.value,
    })

    if (activeOrderId.value && !rows.value.some((row) => row.orderId === activeOrderId.value)) {
      activeOrderId.value = rows.value[0]?.orderId ?? null
    }

    if (!activeOrderId.value && rows.value.length > 0) {
      activeOrderId.value = rows.value[0]?.orderId ?? null
    }
  } catch {
    errorMessage.value = t('jobOrder.jobList.loadFailed')
  } finally {
    loading.value = false
  }
}

async function applyLookup() {
  await load()
}

async function refreshList() {
  lookup.value = ''
  commonQuery.value = 0
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

function toggleSelected(orderId: string) {
  if (selectedOrderIds.value.includes(orderId)) {
    selectedOrderIds.value = selectedOrderIds.value.filter((id) => id !== orderId)
    return
  }

  selectedOrderIds.value = [...selectedOrderIds.value, orderId]
}

function setViewMode(mode: JobListViewMode) {
  viewMode.value = mode
}

async function onRowClick(event: Event, payload: unknown) {
  const row = payload as { item?: JobOrderRecord | { raw?: JobOrderRecord } }
  const item = row?.item
  const record = item && typeof item === 'object' && 'raw' in item ? item.raw : (item as JobOrderRecord | undefined)
  if (!record) {
    return
  }

  if ((event.target as HTMLElement | null)?.closest('a,button,[role="button"],input,label,.v-selection-control')) {
    return
  }

  if (checkboxMode.value) {
    activeOrderId.value = record.orderId
    toggleSelected(record.orderId)
    return
  }

  await openEditor(record)
}

async function openEditor(record: JobOrderRecord) {
  activeOrderId.value = record.orderId
  try {
    formJob.value = await getJobDetail(record.orderId)
    formOpen.value = true
  } catch {
    errorMessage.value = t('jobOrder.openEditFailed')
  }
}

async function openAttachmentDialog() {
  let targetOrderId: string | null = null

  if (selectedOrderIds.value.length !== 1) {
    showActionNotice(t('jobOrder.jobList.noSelection'))
    return
  }

  targetOrderId = selectedOrderIds.value[0] ?? null

  if (!targetOrderId) {
    showActionNotice(t('jobOrder.jobList.noSelection'))
    return
  }

  try {
    const job = await getJobDetail(targetOrderId)
    handleAttachment(job)
  } catch {
    showActionNotice(t('jobOrder.openEditFailed'))
  }
}

async function handleSaved() {
  formOpen.value = false
  saveSuccess.value = true
  await load()
}

async function printList() {
  let targetOrderId: string | null = null

  if (selectedOrderIds.value.length !== 1) {
    showActionNotice(t('jobOrder.jobList.noSelection'))
    return
  }

  targetOrderId = selectedOrderIds.value[0] ?? null

  if (!targetOrderId) {
    showActionNotice(t('jobOrder.jobList.noSelection'))
    return
  }

  try {
    printManagerJob.value = await getJobDetail(targetOrderId)
    printManagerOpen.value = true
  } catch {
    showActionNotice(t('jobOrder.openEditFailed'))
  }
}

function exportToCsv() {
  const exportCols = headers.value.filter((header) => !['orderType', 'status', 'attachProduct', 'attachCustomer'].includes(String(header.key)))
  const headerRow = exportCols.map((header) => `"${String(header.title).replace(/"/g, '""')}"`).join(',')
  const dateKeys = new Set(['orderedOn', 'requiredOn', 'completedOn', 'modifiedOn'])

  const dataRows = displayedRows.value.map((row) =>
    exportCols
      .map((header) => {
        const key = String(header.key)

        if (key === 'ln') {
          return '""'
        }

        if (key === 'orderNumber') {
          return `"${compositeOrderNumber(row).replace(/"/g, '""')}"`
        }

        const value = row[key as keyof JobOrderRecord]
        if (value == null || value === '') return '""'
        if (dateKeys.has(key)) return `"${format(value as string, DATE_FORMATS.ISO_DATE)}"`
        if (typeof value === 'number' && key === 'invoiceAmount') return `"${formatCurrency(value)}"`
        return `"${String(value).replace(/"/g, '""')}"`
      })
      .join(','),
  )

  const csv = '\uFEFF' + [headerRow, ...dataRows].join('\r\n')
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = `job-list-${new Date().toISOString().slice(0, 10)}.csv`
  anchor.click()
  URL.revokeObjectURL(url)
}

async function confirmBatchDelete() {
  const idsToDelete = [...selectedOrderIds.value]
  const total = idsToDelete.length
  if (total === 0) return

  const message = t('jobOrder.jobList.batchDeleteConfirm', { count: total })
  if (!window.confirm(message)) return

  deleting.value = true
  let succeeded = 0
  let failed = 0
  for (const id of idsToDelete) {
    try {
      await deleteJobOrder(id)
      succeeded++
    } catch {
      failed++
    }
  }

  deleting.value = false
  selectedOrderIds.value = []
  await load()
  if (failed > 0) {
    errorMessage.value = t('jobOrder.jobList.batchDeleteResult', { succeeded, failed, total })
  }
}

function valueForSort(row: JobOrderRecord, key: keyof JobOrderRecord) {
  if (key === 'orderNumber') {
    return compositeOrderNumber(row)
  }

  if (key === 'jobNumber') {
    const numeric = Number.parseInt(row.jobNumber, 10)
    return Number.isFinite(numeric) ? numeric : row.jobNumber
  }

  return row[key]
}

function compositeOrderNumber(row: JobOrderRecord) {
  return row.jobNumber ? `${row.orderNumber}-${row.jobNumber}` : row.orderNumber
}




function formatCurrency(value: number) {
  return formatCurrencyByLocale(value)
}

function statusColor(status: number) {
  if (status <= 0) return 'grey'
  if (status === 1) return 'amber'
  if (status === 2) return 'success'
  return 'error'
}

function orderTypeMeta(orderType: number) {
  switch (orderType) {
    case 1:
      return { icon: 'mdi-tag-text-outline', color: 'error' }
    case 2:
      return { icon: 'mdi-label-outline', color: 'warning' }
    case 3:
      return { icon: 'mdi-shape-outline', color: 'secondary' }
    default:
      return { icon: 'mdi-tag-outline', color: 'success' }
  }
}

function openNew() {
  formJob.value = null
  formOpen.value = true
}

function showActionNotice(message: string) {
  actionNoticeMessage.value = message
  actionNoticeOpen.value = true
}

function handleAttachment(job: JobDetail) {
  formJob.value = job
  attachmentDialogOpen.value = true
}

function handleProductDetailsEdit(job: JobDetail) {
  formJob.value = job
  productDetailsDialogOpen.value = true
}

async function handlePrintOrder(job: JobDetail) {
  printManagerJob.value = job
  printManagerOpen.value = true
}

function handleWorkflow(job: JobDetail) {
  void router.push({ name: 'admin-workflow', query: { orderId: job.orderId } })
}

async function handleActionUpdated() {
  if (!formJob.value) return

  try {
    formJob.value = await getJobDetail(formJob.value.orderId)
    await load()
  } catch {
    showActionNotice(t('jobOrder.reloadAfterSaveFailed'))
  }
}
</script>

<style scoped>
.job-list-page {
  min-height: 0;
  --job-list-header-bg: rgba(195, 216, 248, 0.92);
  --job-list-header-fg: inherit;
}

.job-list-page--dark {
  --job-list-header-bg: rgba(52, 74, 104, 0.95);
  --job-list-header-fg: rgba(239, 246, 255, 0.98);
}

.job-list-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
  background: linear-gradient(180deg, rgba(224, 237, 255, 0.92), rgba(241, 247, 255, 0.96));
}

.filter-bar {
  display: grid;
  gap: 12px;
  grid-template-columns: minmax(240px, 1fr) minmax(180px, 260px) auto auto;
  align-items: center;
  margin-bottom: 16px;
}

.toolbar-new-order-btn {
  min-width: 168px;
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

.job-list-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.job-list-table__count {
  display: inline-flex;
  align-items: center;
  margin-inline-end: auto;
  white-space: nowrap;
}

.job-list-table :deep(.v-table__wrapper > table > thead > tr > th),
.job-list-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--job-list-header-bg) !important;
  color: var(--job-list-header-fg) !important;
}

.job-list-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.job-list-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.job-list-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.job-list-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

.job-list-table :deep(tbody td) {
  font-size: 12px;
}

.job-table-shell {
  overflow-x: auto;
}

.job-mobile-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .job-mobile-list {
    grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
    align-items: start;
  }
}

.job-mobile-card {
  display: grid;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgba(255, 255, 255, 0.72);
  cursor: pointer;
}

.job-mobile-card:active {
  background: rgba(255, 255, 255, 0.92);
}

.job-mobile-card__header,
.job-mobile-card__footer,
.job-mobile-card__meta,
.job-mobile-card__actions {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
}

.job-mobile-card__body {
  display: grid;
  gap: 0.45rem;
}

.job-mobile-card__metrics {
  display: grid;
  gap: 0.3rem;
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }

  .toolbar-bar {
    align-items: stretch;
  }
}

@media (max-width: 600px) {
  .toolbar-bar {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .job-mobile-card__header,
  .job-mobile-card__footer,
  .job-mobile-card__meta,
  .job-mobile-card__actions {
    flex-direction: column;
  }
}
</style>