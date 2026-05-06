<template>
  <section class="page-section exceptional-list-page" :class="{ 'exceptional-list-page--dark': isDark }">
    <v-card rounded="xl" elevation="0" class="panel-card exceptional-list-card">
      <v-card-title class="reports-toolbar d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('reports.exceptional.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('reports.exceptional.subtitle') }}</p>
        </div>
        <v-spacer />
        <v-text-field
          v-model="selectedMonth"
          class="reports-toolbar__month"
          :label="t('reports.exceptional.month')"
          type="month"
          density="comfortable"
          variant="solo-filled"
          hide-details
        />
        <v-btn color="primary" prepend-icon="mdi-refresh" :loading="loading" @click="refreshList">
          {{ t('common.refresh') }}
        </v-btn>
      </v-card-title>

      <v-card-text class="pb-0">
        <div class="toolbar-bar mb-2">
          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-view-column">
                {{ t('jobOrder.orderList.actions.columns') }}
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
                {{ t('jobOrder.orderList.actions.sorting') }}
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
                :label="t('jobOrder.orderList.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('jobOrder.orderList.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('jobOrder.orderList.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="toggleCheckboxMode">
            {{ t('jobOrder.pending.actions.checkbox') }}
          </v-btn>

          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                {{ t('jobOrder.pending.actions.views') }}
              </v-btn>
            </template>
            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>{{ t('jobOrder.pending.actions.detailView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>{{ t('jobOrder.pending.actions.cardView') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>
        </div>
      </v-card-text>

      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">
          {{ errorMessage }}
        </v-alert>

        <div v-if="isCardView" class="exceptional-card-list mt-2">
          <v-card
            v-for="item in sortedRows"
            :key="item.orderId"
            rounded="lg"
            elevation="0"
            class="exceptional-card"
            style="position: relative"
          >
            <v-checkbox-btn
              v-if="checkboxMode"
              :model-value="selectedOrderIds.includes(item.orderId)"
              density="compact"
              hide-details
              style="position: absolute; top: 4px; right: 4px"
              @click.stop="toggleSelectedOrder(item.orderId)"
            />
            <div class="exceptional-card__header">
              <div>
                <div
                  class="text-subtitle-2 font-weight-bold text-primary exceptional-order-link"
                  @click.stop="openEditor(item)"
                >{{ item.orderNumber }}-{{ item.jobNumber }}</div>
                <div class="text-caption text-medium-emphasis">{{ item.customerName }}</div>
              </div>
            </div>
            <div class="text-body-2 mt-1">{{ item.orderTitle }}</div>
            <div class="exceptional-card__chips mt-2">
              <v-chip size="x-small" variant="tonal">{{ t('jobOrder.jobList.headers.orderedOn') }}: {{ format(item.orderedOn) }}</v-chip>
              <v-chip size="x-small" variant="tonal">{{ t('jobOrder.jobList.headers.requiredOn') }}: {{ format(item.requiredOn) }}</v-chip>
            </div>
          </v-card>
        </div>

        <v-data-table
          v-else
          :headers="headers"
          :items="sortedRows"
          :loading="loading"
          v-model="selectedOrderIds"
          :show-select="checkboxMode"
          item-value="orderId"
          density="compact"
          fixed-header
          height="62vh"
          class="exceptional-list-table"
        >
          <template #[`item.ln`]="{ index }">{{ index + 1 }}</template>
          <template #[`item.orderNumber`]="{ item }">
            <span class="text-primary exceptional-order-link" @click.stop="openEditor(item)">{{ item.orderNumber }}-{{ item.jobNumber }}</span>
          </template>
          <template #[`item.attachProduct`]="{ item }">
            <div class="d-flex justify-center">
              <v-icon size="14" :color="item.attachmentProductCount > 0 ? 'success' : 'error'">
                mdi-circle
              </v-icon>
            </div>
          </template>
          <template #[`item.attachCustomer`]="{ item }">
            <div class="d-flex justify-center">
              <v-icon size="14" :color="item.attachmentCustomerCount > 0 ? 'success' : 'error'">
                mdi-circle
              </v-icon>
            </div>
          </template>
          <template #[`item.orderedOn`]="{ item }">{{ format(item.orderedOn) }}</template>
          <template #[`item.requiredOn`]="{ item }">{{ format(item.requiredOn) }}</template>
          <template #[`item.modifiedOn`]="{ item }">{{ format(item.modifiedOn) }}</template>
          <template #[`item.completedOn`]="{ item }">{{ format(item.completedOn) }}</template>
          <template #[`item.invoiceAmount`]="{ item }">
            {{ item.invoiceAmount === 0 ? '' : formatCurrency(item.invoiceAmount) }}
          </template>

          <template #footer.prepend>
            <div class="exceptional-list-table__footer text-caption text-medium-emphasis flex-grow-1">
              {{ t('reports.exceptional.rows', { count: sortedRows.length }) }}
            </div>
          </template>
        </v-data-table>
      </v-card-text>
    </v-card>
  </section>

  <v-dialog v-model="formOpen" max-width="760" scrollable>
    <JobOrderForm
      v-if="formJob"
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

  <v-snackbar v-model="actionNoticeOpen" color="info" timeout="3200">
    {{ actionNoticeMessage }}
  </v-snackbar>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useTheme } from 'vuetify'
import { useViewSettings } from '@/composables/useColumnPersistence'
import JobOrderActionDialogs from '@/components/forms/JobOrderActionDialogs.vue'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import JobOrderPrintManagerDialog from '@/components/forms/JobOrderPrintManagerDialog.vue'
import { getJobList } from '@/services/jobOrders'
import { getJobDetail } from '@/services/jobs'
import JobOrderForm from '@/components/forms/JobOrderForm.vue'
import type { JobDetail, JobOrderRecord } from '@/types/api'

const rows = ref<JobOrderRecord[]>([])
const loading = ref(false)
const errorMessage = ref('')
const formOpen = ref(false)
const formJob = ref<JobDetail | null>(null)
const actionNoticeOpen = ref(false)
const actionNoticeMessage = ref('')
const attachmentDialogOpen = ref(false)
const productDetailsDialogOpen = ref(false)
const printManagerOpen = ref(false)
const printManagerJob = ref<JobDetail | null>(null)
const selectedMonth = ref(toMonthString(new Date()))
const selectedOrderIds = ref<string[]>([])

const {
  visibleColumns: visibleColumnKeys,
  sortKey,
  sortDirection,
  checkboxMode,
  viewMode,
} = useViewSettings('exceptional-report', {
  visibleColumns: [
    'ln',
    'orderNumber',
    'orderedOn',
    'customerName',
    'orderTitle',
    'attachProduct',
    'customerRef',
    'attachCustomer',
    'orderedBy',
    'invoiceAmount',
    'invoiceRef',
    'requiredOn',
    'modifiedOn',
    'modifiedBy',
    'completedOn',
  ],
  sortKey: 'orderNumber',
  sortDirection: 'asc',
  checkboxMode: false,
  viewMode: 'detail',
})
const { t } = useI18n({ useScope: 'global' })
const router = useRouter()
const { format } = useGlobalDateFormatter()
const { formatCurrency } = useLocaleFormatters()
const theme = useTheme()
const isDark = computed(() => theme.global.current.value.dark)
const isCardView = computed(() => viewMode.value === 'card')

const allHeaders = computed(() => [
  { title: t('jobOrder.jobList.headers.ln'), key: 'ln', width: '56px', sortable: false },
  { title: t('jobOrder.jobList.headers.order'), key: 'orderNumber', minWidth: '130px' },
  { title: t('jobOrder.jobList.headers.orderedOn'), key: 'orderedOn', width: '120px' },
  { title: t('jobOrder.jobList.headers.customer'), key: 'customerName', minWidth: '180px' },
  { title: t('jobOrder.jobList.headers.orderTitle'), key: 'orderTitle', minWidth: '180px' },
  { title: t('jobOrder.jobList.headers.attachProduct'), key: 'attachProduct', width: '64px', sortable: false, align: 'center' as const },
  { title: t('jobOrder.jobList.headers.customerRef'), key: 'customerRef', minWidth: '140px' },
  { title: t('jobOrder.jobList.headers.attachCustomer'), key: 'attachCustomer', width: '64px', sortable: false, align: 'center' as const },
  { title: t('jobOrder.jobList.headers.orderedBy'), key: 'orderedBy', width: '110px' },
  { title: t('jobOrder.jobList.headers.invoiceAmount'), key: 'invoiceAmount', width: '130px', align: 'end' as const },
  { title: t('jobOrder.jobList.headers.invoiceRef'), key: 'invoiceRef', width: '130px' },
  { title: t('jobOrder.jobList.headers.requiredOn'), key: 'requiredOn', width: '120px' },
  { title: t('jobOrder.jobList.headers.modifiedOn'), key: 'modifiedOn', width: '120px' },
  { title: t('jobOrder.jobList.headers.modifiedBy'), key: 'modifiedBy', width: '120px' },
  { title: t('jobOrder.jobList.headers.completedOn'), key: 'completedOn', width: '120px' },
])

const headers = computed(() => allHeaders.value.filter((header) => visibleColumnKeys.value.includes(String(header.key))))

const sortableColumns = computed(() =>
  allHeaders.value
    .filter((header) => header.sortable !== false && header.key !== 'attachProduct' && header.key !== 'attachCustomer')
    .map((header) => ({ key: String(header.key), title: String(header.title) })),
)

const columnOptions = computed(() => allHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title) })))

const sortedRows = computed(() => {
  const result = [...rows.value]
  const key = sortKey.value as keyof JobOrderRecord

  result.sort((lhs, rhs) => {
    if (key === 'orderNumber') {
      const leftOrder = `${lhs.orderNumber}-${lhs.jobNumber}`
      const rightOrder = `${rhs.orderNumber}-${rhs.jobNumber}`
      return sortDirection.value === 'asc' ? leftOrder.localeCompare(rightOrder) : rightOrder.localeCompare(leftOrder)
    }

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

watch(selectedMonth, async () => {
  await load()
})

onMounted(async () => {
  await load()
})

async function refreshList() {
  await load()
}

async function openEditor(record: JobOrderRecord) {
  try {
    formJob.value = await getJobDetail(record.orderId)
    formOpen.value = true
  } catch {
    errorMessage.value = t('jobOrder.openEditFailed')
  }
}

async function handleSaved() {
  formOpen.value = false
  await load()
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

function handlePrintOrder(job: JobDetail) {
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
    showActionNotice(t('jobOrder.pending.loadFailed'))
  }
}

function setViewMode(mode: 'detail' | 'card') {
  viewMode.value = mode
}

function toggleCheckboxMode() {
  checkboxMode.value = !checkboxMode.value
}

function toggleSelectedOrder(orderId: string) {
  if (selectedOrderIds.value.includes(orderId)) {
    selectedOrderIds.value = selectedOrderIds.value.filter((id) => id !== orderId)
    return
  }

  selectedOrderIds.value = [...selectedOrderIds.value, orderId]
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

function getMonthBounds(value: string) {
  const [yearText, monthText] = value.split('-')
  if (!yearText || !monthText) {
    const now = new Date()
    return getMonthBounds(toMonthString(now))
  }

  const year = Number.parseInt(yearText, 10)
  const month = Number.parseInt(monthText, 10)
  if (!Number.isFinite(year) || !Number.isFinite(month) || month < 1 || month > 12) {
    const now = new Date()
    return getMonthBounds(toMonthString(now))
  }

  const firstDay = new Date(year, month - 1, 1)
  const lastDay = new Date(year, month, 0)
  return {
    startOn: toDateOnly(firstDay),
    endOn: toDateOnly(lastDay),
  }
}

async function load() {
  loading.value = true
  errorMessage.value = ''
  selectedOrderIds.value = []

  try {
    const bounds = getMonthBounds(selectedMonth.value)
    rows.value = await getJobList({
      startOn: bounds.startOn,
      endOn: bounds.endOn,
      take: 500,
    })
  } catch {
    errorMessage.value = t('reports.exceptional.loadFailed')
  } finally {
    loading.value = false
  }
}

function toDateOnly(value: Date) {
  const y = value.getFullYear()
  const m = `${value.getMonth() + 1}`.padStart(2, '0')
  const d = `${value.getDate()}`.padStart(2, '0')
  return `${y}-${m}-${d}`
}

function toMonthString(value: Date) {
  const y = value.getFullYear()
  const m = `${value.getMonth() + 1}`.padStart(2, '0')
  return `${y}-${m}`
}
</script>

<style scoped>
.exceptional-list-page {
  min-height: 0;
  --exceptional-list-header-bg: rgba(195, 216, 248, 0.92);
  --exceptional-list-header-fg: inherit;
}

.exceptional-list-page--dark {
  --exceptional-list-header-bg: rgba(52, 74, 104, 0.95);
  --exceptional-list-header-fg: rgba(239, 246, 255, 0.98);
}

.exceptional-list-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
  background: linear-gradient(180deg, rgba(224, 237, 255, 0.92), rgba(241, 247, 255, 0.96));
}

.reports-toolbar__month {
  min-width: 220px;
}

.toolbar-bar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
}

.toolbar-menu-list {
  max-height: 340px;
  overflow: auto;
}

.exceptional-list-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.exceptional-list-table :deep(.v-table__wrapper > table > thead > tr > th),
.exceptional-list-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--exceptional-list-header-bg) !important;
  color: var(--exceptional-list-header-fg) !important;
}

.exceptional-list-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.exceptional-list-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.exceptional-list-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.exceptional-list-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

.exceptional-list-table :deep(tbody td) {
  font-size: 12px;
}

.exceptional-list-table__footer {
  padding: 8px 12px;
  text-align: left;
}

.exceptional-card-list {
  display: grid;
  gap: 12px;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .exceptional-card-list {
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    align-items: start;
  }
}

.exceptional-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  padding: 12px;
}

.exceptional-order-link {
  cursor: pointer;
}

.exceptional-order-link:hover {
  text-decoration: underline;
}

.exceptional-card__header {
  display: flex;
  gap: 8px;
}

.exceptional-card__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

@media (max-width: 960px) {
  .reports-toolbar__month {
    width: 100%;
  }
}
</style>
