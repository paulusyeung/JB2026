<template>
  <section class="page-section order-list-page">
    <v-card rounded="xl" elevation="0" class="panel-card order-list-card">


      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('jobOrder.orderList.lookup')"
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
            :label="t('jobOrder.orderList.commonQuery')"
            variant="solo-filled"
            density="comfortable"
            hide-details
          />

          <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loading" @click="applyLookup">
            {{ t('jobOrder.orderList.search') }}
          </v-btn>

          <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="refreshList">
            {{ t('common.refresh') }}
          </v-btn>
        </div>

        <v-snackbar v-model="snackbarOpen" :timeout="4000" color="warning" variant="tonal">
          {{ errorMessage }}
        </v-snackbar>

        <v-snackbar v-model="actionNoticeOpen" color="info" timeout="3200">
          {{ actionNoticeMessage }}
        </v-snackbar>

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

          <template v-if="!isPhoneLayout">
            <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
              {{ t('jobOrder.orderList.actions.checkbox') }}
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

            <v-btn variant="outlined" size="small" prepend-icon="mdi-printer" @click="printList">
              {{ t('jobOrder.orderList.actions.print') }}
            </v-btn>

            <v-btn variant="outlined" size="small" prepend-icon="mdi-file-delimited-outline" :disabled="rows.length === 0" @click="exportToCsv">
              {{ t('jobOrder.orderList.actions.export') }}
            </v-btn>

            <v-btn
              variant="outlined"
              color="primary"
              size="small"
              prepend-icon="mdi-file-plus"
              class="toolbar-new-order-btn"
              @click="openCreate"
            >
              {{ t('jobOrder.orderList.actions.newOrder') }}
            </v-btn>

            <v-btn
              v-if="checkboxMode && selectedOrderIds.length > 0"
              variant="tonal"
              color="error"
              size="small"
              prepend-icon="mdi-delete"
              :loading="deleting"
              @click="confirmBatchDelete"
            >
              {{ t('jobOrder.orderList.actions.deleteSelected') }}
            </v-btn>

            <span class="text-caption text-medium-emphasis" v-if="checkboxMode">
              {{ t('jobOrder.orderList.actions.selected', { count: selectedOrderIds.length }) }}
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
                <v-list-item-title>{{ t('jobOrder.orderList.actions.checkbox') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>{{ detailViewLabel }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>{{ cardViewLabel }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-printer" @click="printList">
                <v-list-item-title>{{ t('jobOrder.orderList.actions.print') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-file-delimited-outline" :disabled="rows.length === 0" @click="exportToCsv">
                <v-list-item-title>{{ t('jobOrder.orderList.actions.export') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-file-plus" @click="openCreate">
                <v-list-item-title>{{ t('jobOrder.orderList.actions.newOrder') }}</v-list-item-title>
              </v-list-item>
              <v-list-item v-if="checkboxMode && selectedOrderIds.length > 0" prepend-icon="mdi-delete" :loading="deleting" @click="confirmBatchDelete">
                <v-list-item-title>{{ t('jobOrder.orderList.actions.deleteSelected') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>
        </div>

        <div v-if="isCardView" class="order-mobile-list" :class="{ 'order-mobile-list--desktop': !isPhoneLayout }">
          <div v-if="showCardLoading" class="order-mobile-list__loading" role="status" aria-live="polite">
            <v-icon class="order-mobile-list__spinner" color="primary" size="40">mdi-loading</v-icon>
          </div>

          <template v-else>
            <v-card
              v-for="master in masterRows"
              :key="master.orderId"
              rounded="lg"
              elevation="0"
              class="order-mobile-card"
            >
            <div class="order-mobile-card__header">
              <div>
                <div class="text-subtitle-2 font-weight-bold">{{ master.orderNumber }}</div>
                <div class="text-caption text-medium-emphasis">{{ master.customerName || '-' }}</div>
              </div>

              <div class="d-flex align-center ga-2">
                <v-checkbox-btn
                  v-if="checkboxMode"
                  :model-value="selectedOrderIds.includes(master.orderId)"
                  density="compact"
                  hide-details
                  @click.stop="toggleSelected(master.orderId)"
                />
                <v-btn
                  v-if="hasDetailRows(master)"
                  icon
                  variant="text"
                  size="small"
                  @click.stop="toggleExpandRow(master)"
                >
                  <v-icon size="18">{{ isRowExpanded(master) ? 'mdi-chevron-up' : 'mdi-chevron-down' }}</v-icon>
                </v-btn>
              </div>
            </div>

            <div class="order-mobile-card__body">
              <div class="d-flex align-center ga-2 mb-2">
                <v-chip size="small" :color="statusColor(master.status)" variant="tonal">
                      <v-tooltip :text="statusLabel(master.status)" location="top">
                    <template v-slot:activator="{ props }">
                      <v-icon v-bind="props" start size="12" :color="statusColor(master.status)">{{ statusIcon(master.status) }}</v-icon>
                    </template>
                  </v-tooltip>
                  {{ master.status }}
                </v-chip>
                <span class="text-caption">{{ master.orderTitle || '-' }}</span>
              </div>

              <div class="order-mobile-card__metrics">
                <span class="text-caption">{{ t('jobOrder.record.fields.brand') }}: {{ master.orderTitle || '-' }}</span>
                <span class="text-caption">{{ t('jobOrder.record.fields.requiredOn') }}: {{ format(master.requiredOn) }}</span>
                <span class="text-caption font-weight-medium">{{ t('jobOrder.record.fields.invoiceAmount') }}: {{ formatQty(invoiceAmountForRow(master)) || '-' }}</span>
                <v-chip size="x-small" :color="billingStatusColor(master)" variant="tonal">
                  {{ billingStatusLabel(master) }}
                </v-chip>
              </div>
            </div>

            <div class="order-mobile-card__meta text-caption text-medium-emphasis">
              <span>{{ t('jobOrder.orderList.headers.orderedBy') }}: {{ master.orderedBy || '-' }}</span>
              <span>{{ t('jobOrder.record.fields.orderedOn') }}: {{ format(master.orderedOn) }}</span>
            </div>

            <div class="order-mobile-card__actions">
              <v-btn variant="text" size="small" class="text-none" @click="openEdit(master)">
                {{ master.orderNumber }}
              </v-btn>
              <v-menu location="bottom end">
                <template #activator="{ props }">
                  <v-btn v-bind="props" variant="text" size="small" class="text-none">
                    {{ t('jobOrder.jobList.actions.more') }}
                    <v-icon end size="16">mdi-chevron-down</v-icon>
                  </v-btn>
                </template>
                <v-list density="compact" class="toolbar-menu-list">
                  <v-list-item prepend-icon="mdi-open-in-app" @click="openEdit(master)">
                    <v-list-item-title>{{ master.orderNumber }}</v-list-item-title>
                  </v-list-item>
                  <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
                    <v-list-item-title>{{ t('jobOrder.orderList.actions.checkbox') }}</v-list-item-title>
                  </v-list-item>
                  <v-list-item prepend-icon="mdi-printer" @click="printList">
                    <v-list-item-title>{{ t('jobOrder.orderList.actions.print') }}</v-list-item-title>
                  </v-list-item>
                  <v-list-item prepend-icon="mdi-file-delimited-outline" :disabled="rows.length === 0" @click="exportToCsv">
                    <v-list-item-title>{{ t('jobOrder.orderList.actions.export') }}</v-list-item-title>
                  </v-list-item>
                  <v-list-item prepend-icon="mdi-file-plus" @click="openCreate">
                    <v-list-item-title>{{ t('jobOrder.orderList.actions.newOrder') }}</v-list-item-title>
                  </v-list-item>
                </v-list>
              </v-menu>
            </div>

            <v-expand-transition>
              <div v-if="hasDetailRows(master) && isRowExpanded(master)" class="order-mobile-card__details">
                <div
                  v-for="detail in detailRowsFor(master)"
                  :key="detail.orderId"
                  class="order-mobile-card__detail-row"
                  @click="openEdit(detail)"
                >
                  <div class="d-flex justify-space-between align-start ga-2">
                    <div>
                      <div class="text-body-2 font-weight-medium">{{ detail.orderNumber }}-{{ detail.jobNumber }}</div>
                      <div class="text-caption text-medium-emphasis">{{ detail.orderTitle || '-' }}</div>
                    </div>
                    <v-chip size="x-small" :color="statusColor(detail.status)" variant="tonal">
                      {{ detail.status }}
                    </v-chip>
                  </div>
                  <div class="text-caption text-medium-emphasis mt-1">
                    {{ t('jobOrder.record.fields.requiredOn') }}: {{ format(detail.requiredOn) }}
                  </div>
                </div>
              </div>
            </v-expand-transition>
          </v-card>
          </template>
        </div>

        <v-data-table
          v-else
          :headers="masterHeaders"
          :items="masterRows"
          :loading="loading"
          v-model:expanded="expandedMasterIds"
          v-model="selectedOrderIds"
          :show-select="checkboxMode"
          item-value="orderId"
          density="compact"
          fixed-header
          height="62vh"
          class="order-list-table"
          @click:row="onRowClick"
        >
          <template #[`item.ln`]="{ index }">{{ index + 1 }}</template>

          <template #[`item.expander`]="{ item }">
            <v-btn
              v-if="hasDetailRows(item)"
              variant="text"
              density="comfortable"
              size="x-small"
              icon
              @click.stop="toggleExpandRow(item)"
            >
              <v-icon size="16">{{ isRowExpanded(item) ? 'mdi-minus-box-outline' : 'mdi-plus-box-outline' }}</v-icon>
            </v-btn>
          </template>

          <template #[`item.orderNumber`]="{ item }">
            <v-btn variant="text" color="primary" density="comfortable" class="px-0 text-none" @click.stop="openEdit(item)">
              {{ item.orderNumber }}
            </v-btn>
          </template>

          <template #[`item.status`]="{ item }">
            <div class="d-flex justify-center">
              <v-tooltip :text="statusLabel(item.status)" location="top">
                <template v-slot:activator="{ props }">
                  <v-icon v-bind="props" size="16" :color="statusColor(item.status)">{{ statusIcon(item.status) }}</v-icon>
                </template>
              </v-tooltip>
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
          <template #[`item.invoiceAmount`]="{ item }">{{ invoiceAmountForRow(item) === 0 ? '' : formatQty(invoiceAmountForRow(item)) }}</template>
          <template #[`item.invoiceStatus`]="{ item }">
            <v-chip size="x-small" :color="billingStatusColor(item)" variant="tonal">
              {{ billingStatusLabel(item) }}
            </v-chip>
          </template>

          <template #expanded-row="{ item }">
            <tr>
              <td :colspan="masterHeaders.length + (checkboxMode ? 1 : 0)" class="pa-0">
                <v-data-table
                  :headers="detailHeaders"
                  :items="detailRowsFor(item)"
                  density="compact"
                  hide-default-footer
                  class="detail-grid"
                  @click:row="onDetailRowClick"
                >
                  <template #[`header.status`]>
                    <v-icon size="14" color="primary">mdi-flag</v-icon>
                  </template>

                  <template #[`header.attachProduct`]>
                    <div class="d-flex justify-center">
                      <v-icon size="14" color="grey darken-3">mdi-paperclip</v-icon>
                    </div>
                  </template>
<!-- 
                  <template #[`header.attachCustomer`]>
                    <v-icon size="16" color="grey darken-3">mdi-paperclip</v-icon>
                  </template>
 -->
                  <template #[`item.orderNumber`]="{ item: detail }">
                    <v-btn variant="text" color="primary" density="comfortable" class="px-0 text-none" @click.stop="openDetailItem(detail)">
                      {{ detail.orderNumber }}-{{ detail.jobNumber }}
                    </v-btn>
                  </template>

                  <template #[`item.status`]="{ item: detail }">
                    <div class="d-flex justify-center">
                      <v-tooltip :text="statusLabel(detail.status)" location="top">
                        <template v-slot:activator="{ props }">
                          <v-icon v-bind="props" size="16" :color="statusColor(detail.status)">{{ statusIcon(detail.status) }}</v-icon>
                        </template>
                      </v-tooltip>
                    </div>
                  </template>

                  <template #[`item.attachProduct`]="{ item: detail }">
                    <div class="d-flex justify-center">
                      <v-icon size="14" :color="detail.attachmentProductCount > 0 ? 'success' : 'error'">
                        {{ detail.attachmentProductCount > 0 ? 'mdi-paperclip' : 'mdi-circle-outline' }}
                      </v-icon>
                    </div>
                  </template>
<!-- 
                  <template #[`item.attachCustomer`]="{ item: detail }">
                    <div class="d-flex justify-center">
                      <v-icon size="14" :color="detail.attachmentCustomerCount > 0 ? 'success' : 'error'">
                        {{ detail.attachmentCustomerCount > 0 ? 'mdi-paperclip' : 'mdi-circle-outline' }}
                      </v-icon>
                    </div>
                  </template>
 -->
                  <template #[`item.orderedOn`]="{ item: detail }">{{ format(detail.orderedOn) }}</template>
                  <template #[`item.requiredOn`]="{ item: detail }">{{ format(detail.requiredOn) }}</template>
                  <template #[`item.completedOn`]="{ item: detail }">{{ format(detail.completedOn) }}</template>
                  <template #[`item.modifiedOn`]="{ item: detail }">{{ format(detail.modifiedOn) }}</template>
                  <template #[`item.modifiedBy`]="{ item: detail }">{{ detail.modifiedBy || '-' }}</template>
                  <template #[`item.invoiceAmount`]="{ item: detail }">{{ invoiceAmountForRow(detail) === 0 ? '' : formatQty(invoiceAmountForRow(detail)) }}</template>
                  <template #[`item.invoiceStatus`]="{ item: detail }">
                    <v-chip size="x-small" :color="billingStatusColor(detail)" variant="tonal">
                      {{ billingStatusLabel(detail) }}
                    </v-chip>
                  </template>
                </v-data-table>
              </td>
            </tr>
          </template>
        </v-data-table>
      </v-card-text>
    </v-card>

    <v-dialog v-model="formOpen" max-width="min(100%, 1080px)" scrollable>
      <OrderRecordDialog
        v-if="formOpen"
        :order="formJob ?? undefined"
        :all-orders="rows"
        @saved="handleSaved"
        @deleted="handleDeleted"
        @open-order="handleOpenOrder"
        @open-job-form="handleOpenJobForm"
        @add-new-job="handleAddNewJob"
        @cancel="formOpen = false"
      />
    </v-dialog>

    <v-dialog v-model="jobFormOpen" max-width="min(100%, 1200px)" scrollable>
      <JobOrderForm
        v-if="jobFormOpen"
        :job="jobFormJob"
        @saved="handleJobSaved"
        @cancel="handleJobFormCancelled"
        @attachment="handleAttachment"
        @print-order="handlePrintOrder"
        @workflow="handleWorkflow"
        @product-details-edit="handleProductDetailsEdit"
        @remarks-edit="handleRemarksEdit"
      />
    </v-dialog>

    <JobOrderActionDialogs
      :job="jobFormJob"
      v-model:attachment-open="attachmentDialogOpen"
      v-model:product-details-open="productDetailsDialogOpen"
      v-model:remarks-open="remarksDialogOpen"
      @updated="handleActionUpdated"
      @error="showActionNotice"
    />

    <JobOrderPrintManagerDialog
      v-model="printManagerOpen"
      :order-id="printManagerJob?.orderId ?? null"
      :order-number="printManagerJob?.orderNumber ?? ''"
      :style-titles="printManagerJob?.styleTitles"
    />
  </section>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useDisplay } from 'vuetify'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { useViewSettings } from '@/composables/useColumnPersistence'
import { deleteJobOrder, getJobOrder, getOrderList } from '@/services/jobOrders'
import { getJobDetail } from '@/services/jobs'
import { getInvoiceSummary, type InvoiceBillingSummary } from '@/services/billing'
import OrderRecordDialog from '@/components/forms/OrderRecordDialog.vue'
import JobOrderForm from '@/components/forms/JobOrderForm.vue'
import JobOrderActionDialogs from '@/components/forms/JobOrderActionDialogs.vue'
import JobOrderPrintManagerDialog from '@/components/forms/JobOrderPrintManagerDialog.vue'
import type { JobDetail, JobOrderRecord } from '@/types/api'
import { statusIcon, statusColor, statusLabel } from '@/composables/useJobStatus'

type OrderListViewMode = 'detail' | 'card'

const rows = ref<JobOrderRecord[]>([])
const loading = ref(false)
const cardViewPreparing = ref(false)
const errorMessage = ref('')
const snackbarOpen = ref(false)
const lookup = ref('')
const commonQuery = ref(0)
const selectedOrderIds = ref<string[]>([])
const expandedMasterIds = ref<string[]>([])
const formOpen = ref(false)
const formJob = ref<JobOrderRecord | null>(null)
const deleting = ref(false)

const jobFormOpen = ref(false)
const jobFormJob = ref<JobDetail | null>(null)
const productDetailsDialogOpen = ref(false)
const remarksDialogOpen = ref(false)
const attachmentDialogOpen = ref(false)
const printManagerOpen = ref(false)
const printManagerJob = ref<JobDetail | null>(null)
const actionNoticeOpen = ref(false)
const actionNoticeMessage = ref('')
const pendingOrderIdForRefresh = ref<string | null>(null)
const invoiceSummaryByOrderId = ref<Record<string, InvoiceBillingSummary>>({})

const {
  visibleColumns: visibleColumnKeys,
  sortKey,
  sortDirection,
  checkboxMode,
  viewMode,
} = useViewSettings('orderlist', {
  visibleColumns: [
    'expander',
    'orderNumber',
    'status',
    'orderedOn',
    'customerName',
    'orderTitle',
    'attachProduct',
    'customerRef',
    'attachCustomer',
    'orderedBy',
    'invoiceAmount',
    'invoiceStatus',
    'modifiedBy',
    'modifiedOn',
    'requiredOn',
    'completedOn',
  ],
  sortKey: 'orderNumber',
  sortDirection: 'desc',
  checkboxMode: false,
  viewMode: 'detail',
})

const { t } = useI18n({ useScope: 'global' })
const { format, DATE_FORMATS } = useGlobalDateFormatter()
const router = useRouter()
const display = useDisplay()
const isPhoneLayout = computed(() => display.smAndDown.value)
const detailViewLabel = computed(() => t('jobOrder.jobList.actions.detailView'))
const cardViewLabel = computed(() => t('jobOrder.jobList.actions.cardView'))
const isCardView = computed(() => viewMode.value === 'card')
const showCardLoading = computed(() => loading.value || cardViewPreparing.value)

const commonQueryItems = computed(() => [
  { value: 0, label: t('jobOrder.orderList.commonQueryItems.none') },
  { value: 1, label: t('jobOrder.orderList.commonQueryItems.ordered7') },
  { value: 2, label: t('jobOrder.orderList.commonQueryItems.ordered30') },
  { value: 3, label: t('jobOrder.orderList.commonQueryItems.required7') },
  { value: 4, label: t('jobOrder.orderList.commonQueryItems.required30') },
])

const masterHeaders = computed(() => [
  { title: '', key: 'expander', width: '42px', sortable: false },
  { title: '#', key: 'ln', width: '48px', sortable: false },
  { title: t('jobOrder.record.fields.orderNumber'), key: 'orderNumber', width: '130px' },
  { title: t('jobOrder.record.fields.customerName'), key: 'customerName', minWidth: '240px' },
  { title: t('jobOrder.record.fields.orderTitle'), key: 'orderTitle', minWidth: '280px' },
  { title: t('jobOrder.record.fields.requiredOn'), key: 'requiredOn', width: '120px' },
  { title: t('jobOrder.record.fields.invoiceAmount'), key: 'invoiceAmount', align: 'end' as const, width: '120px' },
  { title: t('jobOrder.orderList.headers.salesRep'), key: 'orderedBy', width: '100px' },
  { title: t('jobOrder.record.fields.orderedOn'), key: 'orderedOn', width: '120px' },
])

const allHeaders = computed(() => [
  { title: '', key: 'expander', width: '42px', sortable: false },
  { title: t('jobOrder.record.fields.jobNumber'), key: 'orderNumber', width: '130px' },
  { title: t('jobOrder.orderList.headers.status'), key: 'status', width: '70px' },
  { title: t('jobOrder.orderList.headers.orderedOn'), key: 'orderedOn', width: '120px' },
  { title: t('jobOrder.orderList.headers.customer'), key: 'customerName', minWidth: '240px' },
  { title: t('jobOrder.orderList.headers.orderTitle'), key: 'orderTitle', minWidth: '280px' },
  { title: '', key: 'attachProduct', width: '72px', sortable: false, icon: 'mdi-paperclip' },
  { title: t('jobOrder.orderList.headers.customerRef'), key: 'customerRef', width: '160px' },
  { title: '', key: 'attachCustomer', width: '72px', sortable: false, icon: 'mdi-paperclip' },
  { title: t('jobOrder.orderList.headers.orderedBy'), key: 'orderedBy', width: '100px' },
  { title: t('jobOrder.orderList.headers.invoiceAmount'), key: 'invoiceAmount', align: 'end' as const, width: '120px' },
  { title: t('jobOrder.jobList.headers.invoiceStatus'), key: 'invoiceStatus', width: '160px', sortable: false },
  { title: t('jobOrder.orderList.headers.modifiedBy'), key: 'modifiedBy', width: '100px' },
  { title: t('jobOrder.orderList.headers.modifiedOn'), key: 'modifiedOn', width: '120px' },
  { title: t('jobOrder.orderList.headers.requiredOn'), key: 'requiredOn', width: '120px' },
  { title: t('jobOrder.orderList.headers.completedOn'), key: 'completedOn', width: '120px' },
])

const headers = computed(() => allHeaders.value.filter((header) => visibleColumnKeys.value.includes(String(header.key))))

const sortableColumns = computed(() =>
  allHeaders.value
    .filter((header) => header.sortable !== false && header.key !== 'status' && header.key !== 'attachProduct' && header.key !== 'attachCustomer')
    .map((header) => ({ key: String(header.key), title: String(header.title) })),
)

const columnOptions = computed(() => allHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title) })))
// Patch: Show clip icon for attachProduct/attachCustomer headers in detail table
const detailHeaders = computed(() => {
  return headers.value
    .filter((h) => h.key !== 'expander')
    .map((h) => {
      if (h.key === 'attachProduct' || h.key === 'attachCustomer') {
        return { ...h, title: '', icon: 'mdi-paperclip' }
      }
      return h
    })
})

const displayedRows = computed(() => {
  const result = [...rows.value]
  const key = sortKey.value as keyof JobOrderRecord

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

const masterGroups = computed(() => {
  const groups = new Map<string, JobOrderRecord[]>()

  for (const row of displayedRows.value) {
    const key = getMasterKey(row)
    if (!groups.has(key)) {
      groups.set(key, [])
    }
    groups.get(key)!.push(row)
  }

  const normalized = new Map<string, { master: JobOrderRecord, details: JobOrderRecord[] }>()
  for (const [key, group] of groups.entries()) {
    const sortedGroup = [...group].sort((lhs, rhs) => {
      const leftJob = Number.parseInt(lhs.jobNumber, 10)
      const rightJob = Number.parseInt(rhs.jobNumber, 10)
      return (Number.isFinite(leftJob) ? leftJob : 0) - (Number.isFinite(rightJob) ? rightJob : 0)
    })
    const first = sortedGroup[0]
    if (!first) {
      continue
    }
    const master = sortedGroup.find((row) => row.jobNumber === '1') ?? first
    const details = sortedGroup
    normalized.set(key, { master, details })
  }

  return normalized
})

const masterRows = computed(() => [...masterGroups.value.values()].map((entry) => entry.master))

watch([commonQuery], async () => {
  await load()
})

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''
  snackbarOpen.value = false
  selectedOrderIds.value = []
  expandedMasterIds.value = []
  try {
    rows.value = await getOrderList({
      lookup: lookup.value.trim() || undefined,
      commonQuery: commonQuery.value,
      take: 500,
    })
    await hydrateInvoiceSummaries(rows.value)
  } catch (e) {
    errorMessage.value = t('jobOrder.orderList.loadFailed')
    snackbarOpen.value = true
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

async function hydrateInvoiceSummaries(orderRows: JobOrderRecord[]) {
  const withInvoiceRefs = orderRows.filter((row) => !!row.invoiceRef)
  await Promise.all(
    withInvoiceRefs.map(async (row) => {
      try {
        const summary = await getInvoiceSummary(row.invoiceRef)
        if (summary) {
          invoiceSummaryByOrderId.value[row.orderId] = summary
        }
      } catch {
        // Keep legacy values if summary fetch fails.
      }
    }),
  )
}

async function onRowClick(_event: Event, payload: { item: JobOrderRecord }) {
  if (checkboxMode.value) {
    return
  }

  await openEdit(payload.item)
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

function getMasterKey(row: JobOrderRecord) {
  return row.orderNumber
}

function detailRowsFor(row: JobOrderRecord) {
  const details = masterGroups.value.get(getMasterKey(row))?.details
  if (details && details.length > 0) {
    return details
  }
  return [row]
}

function hasDetailRows(row: JobOrderRecord) {
  return detailRowsFor(row).length >= 1
}

function isRowExpanded(row: JobOrderRecord) {
  return expandedMasterIds.value.includes(row.orderId)
}

function toggleExpandRow(row: JobOrderRecord) {
  if (isRowExpanded(row)) {
    expandedMasterIds.value = expandedMasterIds.value.filter((id) => id !== row.orderId)
    return
  }
  expandedMasterIds.value = [...expandedMasterIds.value, row.orderId]
}

function toggleSelected(orderId: string) {
  if (selectedOrderIds.value.includes(orderId)) {
    selectedOrderIds.value = selectedOrderIds.value.filter((id) => id !== orderId)
    return
  }

  selectedOrderIds.value = [...selectedOrderIds.value, orderId]
}

async function setViewMode(mode: OrderListViewMode) {
  if (mode === viewMode.value) return

  if (mode === 'card') {
    cardViewPreparing.value = true
    viewMode.value = mode
    // Paint the spinner and keep it spinning briefly before mounting cards
    // (heavy card DOM would otherwise freeze animations on the same turn).
    await nextTick()
    await new Promise<void>((resolve) => {
      requestAnimationFrame(() => requestAnimationFrame(() => resolve()))
    })
    await new Promise<void>((resolve) => setTimeout(resolve, 280))
    cardViewPreparing.value = false
    return
  }

  cardViewPreparing.value = false
  viewMode.value = mode
}

async function onDetailRowClick(_event: Event, payload: { item: JobOrderRecord }) {
  await openDetailItem(payload.item)
}

async function openDetailItem(record: JobOrderRecord) {
  const jobNum = parseInt(record.jobNumber, 10)
  if (!jobNum || jobNum === 0) {
    await openEdit(record)
  } else {
    await openJobForm(record.orderId)
  }
}

async function openJobForm(orderId: string) {
  try {
    const latest = await getJobDetail(orderId)
    jobFormJob.value = latest
    jobFormOpen.value = true
  } catch {
    errorMessage.value = t('jobOrder.openEditFailed')
    snackbarOpen.value = true
  }
}

function handleAddNewJob(orderContext: { orderId: string; orderNumber: string; customerName: string; orderedBy: string; orderTitle: string; orderedOn: string; requiredOn: string; orderType: number; customerRef: string; jobCount: number }) {
  pendingOrderIdForRefresh.value = orderContext.orderId
  formOpen.value = false

  const baseJob = {
    orderId: null,
    orderNumber: orderContext.orderNumber,
    customerName: orderContext.customerName,
    customerRef: orderContext.customerRef ?? '',
    orderTitle: orderContext.orderTitle ?? '',
    orderedBy: orderContext.orderedBy,
    orderedOn: orderContext.orderedOn?.slice(0, 10) ?? new Date().toISOString().slice(0, 10),
    requiredOn: orderContext.requiredOn?.slice(0, 10) ?? new Date().toISOString().slice(0, 10),
    qty: 1,
    status: 0,
    paymentTerms: '',
    remarks: '',
    productDetails: '',
    productStyle: '',
    productCode: '',
    outputRef: '',
    invoiceRef: '',
    invoiceAmount: 0,
    styleTitles: [],
    attachments: [],
    soNumber: '',
    originalSONumber: '',
  }

  if (orderContext.jobCount === 0) {
    // Case 1: Empty table - edit the existing order record, changing jobNumber from 0 to 1
    getJobDetail(orderContext.orderId).then((detail) => {
      jobFormJob.value = {
        ...detail,
        orderNumber: `${orderContext.orderNumber}-1`,
      }
      jobFormOpen.value = true
    }).catch(() => {
      // Fallback: construct a minimal JobDetail in edit mode
      jobFormJob.value = {
        orderId: orderContext.orderId,
        orderNumber: `${orderContext.orderNumber}-1`,
        customerName: orderContext.customerName,
        customerRef: orderContext.customerRef ?? '',
        orderTitle: orderContext.orderTitle ?? '',
        orderedBy: orderContext.orderedBy,
        orderedOn: orderContext.orderedOn?.slice(0, 10) ?? new Date().toISOString().slice(0, 10),
        requiredOn: orderContext.requiredOn?.slice(0, 10) ?? new Date().toISOString().slice(0, 10),
        qty: 1,
        status: 0,
        paymentTerms: '',
        remarks: '',
        productDetails: '',
        productStyle: '',
        productCode: '',
        outputRef: '',
        invoiceRef: '',
        invoiceAmount: 0,
        styleTitles: [],
        attachments: [],
        soNumber: '',
        originalSONumber: '',
      } as unknown as JobDetail
      jobFormOpen.value = true
    })
  } else {
    // Case 2: Non-empty - create mode with header fields pre-filled
    jobFormJob.value = {
      ...baseJob,
      jobNumber: String(orderContext.jobCount + 1),
    } as unknown as JobDetail
    jobFormOpen.value = true
  }
}

function handleOpenJobForm(orderId: string) {
  openJobForm(orderId)
}

async function handleJobSaved() {
  await load()
  jobFormOpen.value = false
  jobFormJob.value = null
  if (pendingOrderIdForRefresh.value) {
    await handleOpenOrder(pendingOrderIdForRefresh.value)
    pendingOrderIdForRefresh.value = null
  }
}

async function handleJobFormCancelled() {
  jobFormOpen.value = false
  jobFormJob.value = null
  if (pendingOrderIdForRefresh.value) {
    await handleOpenOrder(pendingOrderIdForRefresh.value)
    pendingOrderIdForRefresh.value = null
  }
}

function handleProductDetailsEdit(job: JobDetail) {
  jobFormJob.value = job
  productDetailsDialogOpen.value = true
}

function handleRemarksEdit(job: JobDetail) {
  jobFormJob.value = job
  remarksDialogOpen.value = true
}

async function handleActionUpdated() {
  if (!jobFormJob.value) return
  try {
    const latest = await getJobDetail(jobFormJob.value.orderId!)
    const currentOrderNumber = jobFormJob.value.orderNumber
    if (currentOrderNumber?.includes('-')) {
      latest.orderNumber = currentOrderNumber
    }
    jobFormJob.value = latest
  } catch {
    // ignore
  }
}

function handleAttachment(job: JobDetail) {
  jobFormJob.value = job
  attachmentDialogOpen.value = true
}

function handlePrintOrder(job: JobDetail) {
  jobFormJob.value = job
  printManagerJob.value = job
  printManagerOpen.value = true
}

function handleWorkflow(job: JobDetail) {
  void router.push({ name: 'admin-workflow', query: { orderId: job.orderId } })
}

function showActionNotice(message: string) {
  actionNoticeMessage.value = message
  actionNoticeOpen.value = true
}

async function openEdit(record: JobOrderRecord) {
  try {
    const latest = await getJobOrder(record.orderId)
    formJob.value = latest
    formOpen.value = true
  } catch {
    errorMessage.value = t('jobOrder.openEditFailed')
    snackbarOpen.value = true
  }
}

function openCreate() {
  formJob.value = null
  formOpen.value = true
}

async function handleSaved(orderId: string) {
  await load()
  await handleOpenOrder(orderId)
}

async function handleDeleted() {
  formOpen.value = false
  formJob.value = null
  await load()
}

async function handleOpenOrder(orderId: string) {
  const latest = await getJobOrder(orderId)
  formJob.value = latest
}

function printList() {
  window.print()
}

function exportToCsv() {
  const exportCols = headers.value.filter((h) => h.key !== 'status' && h.key !== 'attachProduct' && h.key !== 'attachCustomer')
  const headerRow = exportCols.map((h) => `"${String(h.title).replace(/"/g, '""')}"`).join(',')
  const dateKeys = new Set(['orderedOn', 'requiredOn', 'completedOn', 'modifiedOn'])

  const dataRows = displayedRows.value.map((row) =>
    exportCols
      .map((h) => {
        const key = h.key as keyof JobOrderRecord
        const val = row[key]
        if (val == null) return '""'
        if (dateKeys.has(String(key))) return `"${format(val as string, DATE_FORMATS.ISO_DATE)}"`
        return `"${String(val).replace(/"/g, '""')}"`
      })
      .join(','),
  )

  const csv = '\uFEFF' + [headerRow, ...dataRows].join('\r\n')
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = `order-list-${new Date().toISOString().slice(0, 10)}.csv`
  anchor.click()
  URL.revokeObjectURL(url)
}

async function confirmBatchDelete() {
  const message = t('jobOrder.orderList.batchDeleteConfirm', { count: selectedOrderIds.value.length })
  if (!window.confirm(message)) return

  deleting.value = true
  let failed = 0
  for (const id of selectedOrderIds.value) {
    try {
      await deleteJobOrder(id)
    } catch {
      failed++
    }
  }
  deleting.value = false
  selectedOrderIds.value = []
  await load()
  if (failed > 0) {
    errorMessage.value = t('jobOrder.orderList.batchDeleteFailed')
    snackbarOpen.value = true
  }
}



function formatQty(value: number) {
  if (value === 0) return ''
  return '$' + value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function invoiceAmountForRow(row: JobOrderRecord) {
  const summary = invoiceSummaryByOrderId.value[row.orderId]
  return summary?.amount ?? row.invoiceAmount
}

function billingStatusLabel(row: JobOrderRecord) {
  const summary = invoiceSummaryByOrderId.value[row.orderId]
  if (summary?.status) return summary.status
  return row.invoiceRef ? t('jobOrder.jobList.actions.legacyInvoiced') : t('jobOrder.jobList.actions.notInvoiced')
}

function billingStatusColor(row: JobOrderRecord) {
  const status = billingStatusLabel(row).toLowerCase()
  if (status.includes('paid')) return 'success'
  if (status.includes('overdue')) return 'error'
  if (status.includes('sent') || status.includes('view')) return 'info'
  if (status.includes('not invoiced')) return 'grey'
  return 'warning'
}

</script>

<style scoped>
.order-list-page {
  min-height: 0;
  --order-list-header-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%);
  --order-list-header-fg: rgb(var(--v-theme-on-surface-variant));
}

.toolbar-new-order-btn {
  min-width: 168px;
}

.order-list-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
  background: linear-gradient(180deg, rgba(224, 237, 255, 0.9), rgba(240, 247, 255, 0.95));
}

.filter-bar {
  display: grid;
  gap: 12px;
  grid-template-columns: minmax(240px, 1fr) minmax(180px, 260px) auto auto;
  align-items: center;
  margin-bottom: 16px;
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

.order-list-table :deep(.v-table__wrapper > table > thead > tr > th),
.order-list-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--order-list-header-bg) !important;
  color: var(--order-list-header-fg) !important;
}

.order-list-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.order-list-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.order-list-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.order-list-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.order-list-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

.order-list-table :deep(tbody td) {
  font-size: 12px;
}

.detail-grid {
  border-top: 1px solid rgba(var(--v-theme-primary), 0.2);
  background: rgba(220, 232, 247, 0.55);
  margin-left: 48px;
}

.detail-grid :deep(tbody tr:nth-child(odd)) {
  background: rgba(227, 236, 248, 0.7);
}

.detail-grid :deep(tbody td) {
  font-size: 12px;
}

.order-mobile-list {
  display: grid;
  gap: 12px;
}

.order-mobile-list--desktop {
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  align-items: start;
}

.order-mobile-list__loading {
  grid-column: 1 / -1;
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 12rem;
  padding: 2rem;
}

.order-mobile-list__spinner {
  animation: order-card-spin 0.75s linear infinite;
  will-change: transform;
}

@keyframes order-card-spin {
  to {
    transform: rotate(360deg);
  }
}

.order-mobile-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.18);
  background: rgb(var(--v-theme-surface));
  color: rgba(var(--v-theme-on-surface), 0.92);
  padding: 12px;
}

.order-mobile-card :deep(.text-medium-emphasis) {
  color: rgba(var(--v-theme-on-surface), 0.72) !important;
  opacity: 1;
}

.order-mobile-card :deep(.text-caption) {
  color: rgba(var(--v-theme-on-surface), 0.86);
}

.order-mobile-card__header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 12px;
}

.order-mobile-card__header .text-subtitle-2 {
  color: rgba(var(--v-theme-on-surface), 0.95);
}

.order-mobile-card__body {
  margin-top: 8px;
}

.order-mobile-card__metrics {
  display: grid;
  gap: 4px;
}

.order-mobile-card__meta {
  display: flex;
  flex-wrap: wrap;
  gap: 8px 16px;
  margin-top: 8px;
}

.order-mobile-card__actions {
  margin-top: 8px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.order-mobile-card__details {
  margin-top: 10px;
  border-top: 1px solid rgba(var(--v-theme-primary), 0.2);
  padding-top: 10px;
  display: grid;
  gap: 8px;
}

.order-mobile-card__detail-row {
  border: 1px solid rgba(var(--v-theme-primary), 0.18);
  border-radius: 10px;
  padding: 8px;
  background: color-mix(in srgb, rgb(var(--v-theme-surface)) 94%, rgb(var(--v-theme-primary)) 6%);
  color: rgba(var(--v-theme-on-surface), 0.92);
  cursor: pointer;
}

.order-mobile-card__detail-row:active {
  background: color-mix(in srgb, rgb(var(--v-theme-surface)) 88%, rgb(var(--v-theme-primary)) 12%);
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}
</style>
