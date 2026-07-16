<template>
  <section class="page-section job-list-page">
    <v-card rounded="xl" elevation="0" class="panel-card job-list-card">


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

          <v-menu v-model="startDatePickerOpen" :close-on-content-click="false">
            <template #activator="{ props: menuProps }">
              <v-text-field
                :model-value="startDate ? format(startDate) : ''"
                :label="t('jobOrder.jobList.filters.startDate')"
                variant="solo-filled"
                density="comfortable"
                readonly
                append-inner-icon="mdi-calendar"
                v-bind="menuProps"
                hide-details
                clearable
                @click:clear="startDate = ''"
              />
            </template>
            <v-date-picker
              :model-value="startDate ? new Date(startDate + 'T12:00:00') : undefined"
              hide-header
              @update:model-value="onStartDatePicked"
            />
          </v-menu>

          <v-menu v-model="endDatePickerOpen" :close-on-content-click="false">
            <template #activator="{ props: menuProps }">
              <v-text-field
                :model-value="endDate ? format(endDate) : ''"
                :label="t('jobOrder.jobList.filters.endDate')"
                variant="solo-filled"
                density="comfortable"
                readonly
                append-inner-icon="mdi-calendar"
                v-bind="menuProps"
                hide-details
                clearable
                @click:clear="endDate = ''"
              />
            </template>
            <v-date-picker
              :model-value="endDate ? new Date(endDate + 'T12:00:00') : undefined"
              hide-header
              @update:model-value="onEndDatePicked"
            />
          </v-menu>

          <v-select
            v-model="statusFilter"
            :items="statusItems"
            item-title="label"
            item-value="value"
            :label="t('jobOrder.jobList.filters.status')"
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

        <v-snackbar v-model="errorSnackbarOpen" color="warning" timeout="4000" location="top">
          {{ errorMessage }}
          <template #actions>
            <v-btn variant="text" @click="errorSnackbarOpen = false">{{ t('common.close') }}</v-btn>
          </template>
        </v-snackbar>

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
            <v-checkbox-btn
              v-if="checkboxMode"
              :model-value="selectedOrderIds.includes(row.orderId)"
              density="compact"
              hide-details
              class="job-mobile-card__checkbox"
              @click.stop="toggleSelected(row.orderId)"
            />

            <div class="job-mobile-card__header">
              <div class="d-flex align-center ga-2">
                <v-icon size="18" :color="getOrderTypeMeta(row.orderType).color">
                  {{ getOrderTypeMeta(row.orderType).icon }}
                </v-icon>
                <div>
                  <div class="text-subtitle-2 font-weight-bold">{{ compositeOrderNumber(row) }}</div>
                  <div class="text-caption text-medium-emphasis">{{ row.customerName || '-' }}</div>
                </div>
              </div>
            </div>

            <div class="job-mobile-card__body">
              <div class="d-flex align-center ga-2 mb-2">
                <v-chip size="small" :color="statusColor(row.status)" variant="tonal">
                  <v-tooltip :text="statusLabel(row.status)" location="top">
                    <template v-slot:activator="{ props }">
                      <v-icon v-bind="props" start size="12" :color="statusColor(row.status)">{{ statusIcon(row.status) }}</v-icon>
                    </template>
                  </v-tooltip>
                  {{ row.status }}
                </v-chip>
                <span class="text-caption">{{ row.orderTitle || '-' }}</span>
              </div>

              <div class="job-mobile-card__metrics">
                <span class="text-caption">{{ t('jobOrder.jobList.headers.quotation') }}: {{ row.productStyle || '-' }}</span>
                <span class="text-caption font-weight-medium">{{ t('jobOrder.jobList.headers.invoiceAmount') }}: {{ formatCurrency(invoiceAmountForRow(row)) }}</span>
                <div class="d-flex align-center ga-2">
                  <v-chip size="x-small" :color="billingStatusColor(row)" variant="tonal">
                    {{ billingStatusLabel(row) }}
                  </v-chip>
                  <v-btn
                    v-if="canGenerateInvoice(row)"
                    size="x-small"
                    color="primary"
                    variant="text"
                    @click.stop="openInvoicePreview(row)"
                  >
                    {{ t('jobOrder.jobList.actions.generateInvoice') }}
                  </v-btn>
                </div>
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
                <v-icon size="16" :color="getOrderTypeMeta(item.orderType).color">{{ getOrderTypeMeta(item.orderType).icon }}</v-icon>
              </div>
            </template>

            <template #[`item.orderNumber`]="{ item }">
              <v-btn variant="text" color="primary" density="comfortable" class="px-0 text-none" @click.stop="openEditor(item)">
                {{ compositeOrderNumber(item) }}
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
            <template #[`item.invoiceStatus`]="{ item }">
              <div class="d-flex align-center ga-2">
                <v-chip size="x-small" :color="billingStatusColor(item)" variant="tonal">
                  {{ billingStatusLabel(item) }}
                </v-chip>
                <v-btn
                  v-if="canGenerateInvoice(item)"
                  size="x-small"
                  color="primary"
                  variant="text"
                  @click.stop="openInvoicePreview(item)"
                >
                  {{ t('jobOrder.jobList.actions.generateInvoice') }}
                </v-btn>
              </div>
            </template>
            <template #[`item.invoiceAmount`]="{ item }">{{ formatCurrency(invoiceAmountForRow(item)) }}</template>
            <template #[`item.productStyle`]="{ item }">{{ item.productStyle || '-' }}</template>
          </v-data-table>
        </div>
      </v-card-text>
    </v-card>

    <v-dialog v-model="invoicePreviewOpen" max-width="720">
      <v-card>
        <v-card-title>{{ t('jobOrder.jobList.actions.generateInvoice') }}</v-card-title>
        <v-card-text>
          <div v-if="invoiceTargetRow" class="d-grid ga-3">
            <v-text-field
              v-model="invoicePreviewForm.invoiceNinjaClientId"
              :label="t('jobOrder.jobList.actions.billingClientId')"
              variant="outlined"
              density="comfortable"
              hide-details="auto"
            />
            <v-text-field
              v-model="invoicePreviewForm.poNumber"
              :label="t('jobOrder.jobList.actions.poNumber')"
              variant="outlined"
              density="comfortable"
              hide-details
            />

            <v-alert v-if="invoicePreviewError" type="warning" variant="tonal">{{ invoicePreviewError }}</v-alert>

            <v-card variant="outlined" class="pa-3">
              <div class="text-body-2"><strong>{{ t('jobOrder.jobList.headers.customer') }}:</strong> {{ invoiceTargetRow.customerName || '-' }}</div>
              <div class="text-body-2"><strong>{{ t('jobOrder.jobList.headers.order') }}:</strong> {{ compositeOrderNumber(invoiceTargetRow) }}</div>
              <div class="text-body-2"><strong>{{ t('jobOrder.jobList.actions.previewTotal') }}:</strong> {{ formatCurrency(invoicePreviewTotal) }}</div>
            </v-card>

            <v-card v-if="invoicePreviewResponse" variant="outlined" class="pa-3">
              <div class="text-subtitle-2 mb-2">{{ t('jobOrder.jobList.actions.previewResolvedFields') }}</div>
              <div class="text-body-2"><strong>Bill To:</strong> {{ invoicePreviewResponse.resolvedCustomFields.billToCustomField || '-' }}</div>
              <div class="text-body-2"><strong>Ship To:</strong> {{ invoicePreviewResponse.resolvedCustomFields.shipToCustomField || '-' }}</div>
              <div class="text-body-2"><strong>Job No.:</strong> {{ invoicePreviewResponse.resolvedCustomFields.jobNoCustomField || '-' }}</div>
              <div class="text-body-2"><strong>P.O.No.:</strong> {{ invoicePreviewResponse.resolvedCustomFields.poNoCustomField || '-' }}</div>
              <v-alert
                v-for="warning in invoicePreviewResponse.warnings"
                :key="warning"
                type="info"
                variant="tonal"
                class="mt-2"
              >
                {{ warning }}
              </v-alert>
            </v-card>
          </div>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="closeInvoicePreview">{{ t('common.cancel') }}</v-btn>
          <v-btn color="primary" variant="outlined" :loading="invoicePreviewLoading" @click="requestInvoicePreview">
            {{ t('jobOrder.jobList.actions.previewInvoice') }}
          </v-btn>
          <v-btn color="primary" :loading="invoiceGenerateLoading" :disabled="!invoicePreviewResponse" @click="confirmGenerateInvoice">
            {{ t('jobOrder.jobList.actions.confirmGenerate') }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

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
        @remarks-edit="handleRemarksEdit"
      />
    </v-dialog>

    <JobOrderActionDialogs
      :job="formJob"
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
import { useDisplay } from 'vuetify'
import JobOrderActionDialogs from '@/components/forms/JobOrderActionDialogs.vue'
import JobOrderForm from '@/components/forms/JobOrderForm.vue'
import JobOrderPrintManagerDialog from '@/components/forms/JobOrderPrintManagerDialog.vue'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { getOrderTypeMeta } from '@/utils/orderType'
import { useViewSettings } from '@/composables/useColumnPersistence'
import { getJobDetail } from '@/services/jobs'
import { deleteJobOrder, getJobList } from '@/services/jobOrders'
import {
  generateInvoice,
  getInvoiceSummary,
  previewInvoice,
  type InvoiceBillingSummary,
  type PreviewInvoiceResponse,
} from '@/services/billing'
import type { JobDetail, JobOrderRecord } from '@/types/api'
import { statusIcon, statusColor, statusLabel } from '@/composables/useJobStatus'

type JobListViewMode = 'detail' | 'card'

const rows = ref<JobOrderRecord[]>([])
const loading = ref(false)
const deleting = ref(false)
const errorMessage = ref('')
const errorSnackbarOpen = ref(false)
const lookup = ref('')
const startDate = ref('')
const endDate = ref('')
const statusFilter = ref(-1)
const startDatePickerOpen = ref(false)
const endDatePickerOpen = ref(false)
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
  'invoiceStatus',
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
const remarksDialogOpen = ref(false)
const printManagerOpen = ref(false)
const printManagerJob = ref<JobDetail | null>(null)
const invoicePreviewOpen = ref(false)
const invoicePreviewLoading = ref(false)
const invoiceGenerateLoading = ref(false)
const invoicePreviewError = ref('')
const invoiceTargetRow = ref<JobOrderRecord | null>(null)
const invoicePreviewResponse = ref<PreviewInvoiceResponse | null>(null)
const invoicePreviewForm = ref({
  invoiceNinjaClientId: '',
  poNumber: '',
})
const invoiceSummaryByOrderId = ref<Record<string, InvoiceBillingSummary>>({})

const { t } = useI18n({ useScope: 'global' })
const { format, DATE_FORMATS } = useGlobalDateFormatter()
const { formatCurrency: formatCurrencyByLocale } = useLocaleFormatters()

function formatCurrency(value: number) {
  return value === 0 ? '' : formatCurrencyByLocale(value)
}

function toIsoDate(date: Date): string {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

function onStartDatePicked(date: Date | null) {
  if (date) {
    startDate.value = toIsoDate(date)
  }
  startDatePickerOpen.value = false
}

function onEndDatePicked(date: Date | null) {
  if (date) {
    endDate.value = toIsoDate(date)
  }
  endDatePickerOpen.value = false
}
const display = useDisplay()
const router = useRouter()
const isPhoneLayout = computed(() => display.smAndDown.value)
const detailViewLabel = computed(() => t('jobOrder.jobList.actions.detailView'))
const cardViewLabel = computed(() => t('jobOrder.jobList.actions.cardView'))
const isCardView = computed(() => viewMode.value === 'card')

const statusItems = computed(() => [
  { value: -1, label: t('jobOrder.jobList.filters.allStatuses') },
  { value: 0, label: t('jobOrder.status.notStarted') },
  { value: 1, label: t('jobOrder.status.inProgress') },
  { value: 2, label: t('jobOrder.status.paused') },
  { value: 3, label: t('jobOrder.status.completed') },
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
  { title: t('jobOrder.jobList.headers.invoiceStatus'), key: 'invoiceStatus', width: '220px', sortable: false },
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
  errorSnackbarOpen.value = false
  selectedOrderIds.value = []
  try {
    rows.value = await getJobList({
      lookup: lookup.value.trim() || undefined,
      startOn: startDate.value || undefined,
      endOn: endDate.value || undefined,
      status: statusFilter.value >= 0 ? statusFilter.value : undefined,
    })
    await hydrateInvoiceSummaries(rows.value)

    if (activeOrderId.value && !rows.value.some((row) => row.orderId === activeOrderId.value)) {
      activeOrderId.value = rows.value[0]?.orderId ?? null
    }

    if (!activeOrderId.value && rows.value.length > 0) {
      activeOrderId.value = rows.value[0]?.orderId ?? null
    }

  } catch {
    errorMessage.value = t('jobOrder.jobList.loadFailed')
    errorSnackbarOpen.value = true
  } finally {
    loading.value = false
  }
}

async function hydrateInvoiceSummaries(jobRows: JobOrderRecord[]) {
  const withInvoiceRefs = jobRows.filter((row) => !!row.invoiceRef)
  await Promise.all(
    withInvoiceRefs.map(async (row) => {
      try {
        const summary = await getInvoiceSummary(row.invoiceRef)
        if (summary) {
          invoiceSummaryByOrderId.value[row.orderId] = summary
        }
      } catch {
        // Keep legacy invoice values if summary lookup fails.
      }
    }),
  )
}

async function applyLookup() {
  await load()
}

async function refreshList() {
  lookup.value = ''
  startDate.value = ''
  endDate.value = ''
  statusFilter.value = -1
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
    errorSnackbarOpen.value = true
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

function billingStatusLabel(row: JobOrderRecord) {
  const summary = invoiceSummaryByOrderId.value[row.orderId]
  if (summary?.status) return summary.status
  if (row.invoiceRef) return t('jobOrder.jobList.actions.legacyInvoiced')
  return t('jobOrder.jobList.actions.notInvoiced')
}

function billingStatusColor(row: JobOrderRecord) {
  const status = billingStatusLabel(row).toLowerCase()
  if (status.includes('paid')) return 'success'
  if (status.includes('overdue')) return 'error'
  if (status.includes('sent') || status.includes('view')) return 'info'
  if (status.includes('not invoiced')) return 'grey'
  return 'warning'
}

function invoiceAmountForRow(row: JobOrderRecord) {
  const summary = invoiceSummaryByOrderId.value[row.orderId]
  return summary?.amount ?? row.invoiceAmount
}

function canGenerateInvoice(row: JobOrderRecord) {
  return !row.invoiceRef
}

const invoicePreviewTotal = computed(() => {
  const row = invoiceTargetRow.value
  if (!row) return 0
  return row.invoiceAmount > 0 ? row.invoiceAmount : 0
})

function openInvoicePreview(row: JobOrderRecord) {
  invoiceTargetRow.value = row
  invoicePreviewForm.value = {
    invoiceNinjaClientId: '',
    poNumber: '',
  }
  invoicePreviewResponse.value = null
  invoicePreviewError.value = ''
  invoicePreviewOpen.value = true
}

function closeInvoicePreview() {
  invoicePreviewOpen.value = false
  invoicePreviewResponse.value = null
  invoicePreviewError.value = ''
  invoiceTargetRow.value = null
}

function buildInvoiceLineItem(row: JobOrderRecord) {
  const quantity = row.qty > 0 ? row.qty : 1
  const unitCost = row.invoiceAmount > 0 ? row.invoiceAmount / quantity : 0
  return {
    description: row.orderTitle || t('jobOrder.jobList.actions.defaultLineDescription'),
    quantity,
    unitCost,
  }
}

async function requestInvoicePreview() {
  const row = invoiceTargetRow.value
  if (!row) return
  if (!invoicePreviewForm.value.invoiceNinjaClientId.trim()) {
    invoicePreviewError.value = t('jobOrder.jobList.actions.billingClientIdRequired')
    return
  }

  invoicePreviewLoading.value = true
  invoicePreviewError.value = ''
  try {
    invoicePreviewResponse.value = await previewInvoice({
      customerName: row.customerName,
      billTo: '',
      shipTo: '',
      jobNumber: row.jobNumber,
      poNumber: invoicePreviewForm.value.poNumber,
      lineItems: [buildInvoiceLineItem(row)],
    })
  } catch {
    invoicePreviewError.value = t('jobOrder.jobList.actions.previewFailed')
  } finally {
    invoicePreviewLoading.value = false
  }
}

async function confirmGenerateInvoice() {
  const row = invoiceTargetRow.value
  if (!row) return
  if (!invoicePreviewForm.value.invoiceNinjaClientId.trim()) {
    invoicePreviewError.value = t('jobOrder.jobList.actions.billingClientIdRequired')
    return
  }

  invoiceGenerateLoading.value = true
  invoicePreviewError.value = ''
  try {
    const created = await generateInvoice({
      orderId: row.orderId,
      invoiceNinjaClientId: invoicePreviewForm.value.invoiceNinjaClientId.trim(),
      jobNumber: row.jobNumber,
      poNumber: invoicePreviewForm.value.poNumber,
      lineItems: [buildInvoiceLineItem(row)],
    })

    invoiceSummaryByOrderId.value[row.orderId] = created.billingSummary
    rows.value = rows.value.map((item) =>
      item.orderId === row.orderId
        ? {
            ...item,
            invoiceRef: created.billingSummary.externalInvoiceId,
            invoiceAmount: created.billingSummary.amount,
          }
        : item,
    )
    showActionNotice(t('jobOrder.jobList.actions.invoiceGenerated'))
    closeInvoicePreview()
  } catch {
    invoicePreviewError.value = t('jobOrder.jobList.actions.generateFailed')
  } finally {
    invoiceGenerateLoading.value = false
  }
}

function compositeOrderNumber(row: JobOrderRecord) {
  return row.jobNumber ? `${row.orderNumber}-${row.jobNumber}` : row.orderNumber
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

function handleRemarksEdit(job: JobDetail) {
  formJob.value = job
  remarksDialogOpen.value = true
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
  --job-list-header-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%);
  --job-list-header-fg: rgb(var(--v-theme-on-surface-variant));
}

.job-list-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
  background: linear-gradient(180deg, rgba(224, 237, 255, 0.92), rgba(241, 247, 255, 0.96));
}

.filter-bar {
  display: grid;
  gap: 12px;
  grid-template-columns: minmax(200px, 1fr) minmax(150px, 200px) minmax(150px, 200px) minmax(130px, 160px) auto auto;
  align-items: center;
  margin-bottom: 16px;
}

.toolbar-new-order-btn {
  min-width: 168px;
  display: none;  /* 2026.06.24 paulus: hiden, add new job from OrderRecordDialog */
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
  grid-template-columns: 1fr auto;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgb(var(--v-theme-surface));
  cursor: pointer;
}

.job-mobile-card__checkbox {
  grid-column: 2;
  grid-row: 1;
  align-self: start;
  justify-self: end;
}

.job-mobile-card__header {
  grid-column: 1;
  grid-row: 1;
}

.job-mobile-card__body,
.job-mobile-card__footer,
.job-mobile-card__meta,
.job-mobile-card__actions {
  grid-column: 1 / -1;
}

.job-mobile-card:active {
  background: rgba(var(--v-theme-surface), 0.8);
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