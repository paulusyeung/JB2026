<template>
  <section class="page-section admin-customer-page">
    <v-card rounded="xl" elevation="0" class="panel-card admin-customer-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3 pb-2">
        <div>
          <h3 class="text-h6 mb-1">{{ t('admin.customer.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('admin.customer.subtitle') }}</p>
        </div>
      </v-card-title>

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('admin.customer.lookup')"
            prepend-inner-icon="mdi-magnify"
            variant="solo-filled"
            hide-details
            clearable
            @keydown.enter="applyLookup"
          />

          <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loading" @click="applyLookup">
            {{ t('common.search') }}
          </v-btn>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ errorMessage }}</v-alert>

        <div class="toolbar-bar mb-2">
          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-view-column">
                {{ t('admin.customer.actions.columns') }}
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
                {{ t('admin.customer.actions.sorting') }}
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
                :label="t('admin.customer.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('admin.customer.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('admin.customer.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <template v-if="!isPhoneLayout">
            <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
              {{ t('admin.customer.actions.checkbox') }}
            </v-btn>

            <v-menu location="bottom">
              <template #activator="{ props }">
                <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                  {{ t('admin.customer.actions.views') }}
                </v-btn>
              </template>
              <v-list density="compact" class="toolbar-menu-list">
                <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                  <v-list-item-title>{{ t('admin.customer.actions.detailView') }}</v-list-item-title>
                </v-list-item>
                <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                  <v-list-item-title>{{ t('admin.customer.actions.cardView') }}</v-list-item-title>
                </v-list-item>
              </v-list>
            </v-menu>

            <v-divider vertical class="mx-1" />

            <v-btn variant="outlined" size="small" color="primary" prepend-icon="mdi-account-plus" @click="openNewCustomer">
              {{ t('admin.customer.actions.newCustomer') }}
            </v-btn>

            <v-btn
              :disabled="!selectedCustomerId || !canSyncSelectedCustomer"
              :loading="!!selectedCustomerId && syncingCustomerId === selectedCustomerId"
              variant="outlined"
              size="small"
              color="primary"
              prepend-icon="mdi-cloud-upload-outline"
              class="sync-billing-btn"
              @click="syncSelectedCustomer"
            >
              {{ t('admin.customer.actions.syncBilling') }}
              <v-tooltip activator="parent" location="top">
                {{ !selectedCustomerId ? t('admin.customer.messages.selectRecordFirst') : (!canSyncSelectedCustomer ? t('admin.customer.messages.syncRequiresCode') : '') }}
              </v-tooltip>
            </v-btn>

            <v-btn
              :disabled="!canMergeSelectedCustomers"
              :loading="merging"
              variant="outlined"
              size="small"
              color="primary"
              prepend-icon="mdi-account-multiple-outline"
              @click="openMergeDialog"
            >
              {{ t('admin.customer.actions.merge') }}
            </v-btn>
          </template>

          <v-menu v-else location="bottom end">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                {{ t('admin.customer.actions.views') }}
              </v-btn>
            </template>

            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
                <v-list-item-title>{{ t('admin.customer.actions.checkbox') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>{{ t('admin.customer.actions.detailView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>{{ t('admin.customer.actions.cardView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-account-plus" @click="openNewCustomer">
                <v-list-item-title>{{ t('admin.customer.actions.newCustomer') }}</v-list-item-title>
              </v-list-item>
              <v-list-item
                :disabled="!selectedCustomerId || !canSyncSelectedCustomer"
                prepend-icon="mdi-cloud-upload-outline"
                @click="syncSelectedCustomer"
              >
                <v-list-item-title>{{ t('admin.customer.actions.syncBilling') }}</v-list-item-title>
              </v-list-item>
              <v-list-item
                :disabled="!canMergeSelectedCustomers"
                prepend-icon="mdi-account-multiple-outline"
                @click="openMergeDialog"
              >
                <v-list-item-title>{{ t('admin.customer.actions.merge') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <span v-if="checkboxMode" class="text-caption text-medium-emphasis">
            {{ t('admin.customer.actions.selected', { count: selectedCustomerIds.length }) }}
          </span>

          <v-spacer />

          <div v-if="selectedCustomerBillingStatus" class="d-flex align-center ga-2">
            <v-icon v-if="selectedCustomerBillingStatus.synced" color="success" size="16">mdi-check-circle</v-icon>
            <v-icon v-else-if="selectedCustomerBillingStatus.error" color="error" size="16">mdi-alert-circle</v-icon>
            <span class="text-caption text-medium-emphasis">
              {{ selectedCustomerBillingStatus.error ? t('admin.customer.messages.billingError') : t('admin.customer.messages.billingSynced') }}
            </span>
          </div>
        </div>

        <ListMobileCard
          v-if="isPhoneLayout"
          :items="displayedRows"
          :columns="mobileColumns"
          item-key="customerId"
          :checkbox-mode="checkboxMode"
          :selected-ids="selectedCustomerIds"
          :on-select="handleMobileSelect"
          :on-card-click="(item) => onMobileCardClick(item)"
        />

        <div v-else-if="isCardView" class="customer-card-list">
          <v-card
            v-for="row in displayedRows"
            :key="row.customerId"
            rounded="lg"
            elevation="0"
            class="customer-card"
            @click="openPopup(row.customerId)"
          >
            <div class="customer-card__header">
              <div class="d-flex align-center ga-2">
                <v-icon
                  size="18"
                  :style="{ color: isBackendBillingSynced(row) ? 'rgb(var(--v-theme-primary))' : 'rgb(var(--v-theme-on-surface-variant))' }"
                >mdi-account</v-icon>
                <div>
                  <div class="text-subtitle-2 font-weight-bold">{{ row.customerName }}</div>
                  <div class="text-caption text-medium-emphasis">{{ row.customerCode || '-' }}</div>
                </div>
              </div>
              <v-checkbox-btn
                v-if="checkboxMode"
                :model-value="selectedCustomerIds.includes(row.customerId)"
                density="compact"
                hide-details
                @click.stop="handleCardCheckbox(row.customerId)"
              />
            </div>
            <div class="customer-card__body">
              <span class="text-caption">{{ t('admin.customer.headers.loginAccount') }}: {{ row.loginAccount || '-' }}</span>
            </div>
            <div class="customer-card__footer text-caption text-medium-emphasis">
              <span>{{ t('admin.customer.headers.modifiedBy') }}: {{ row.modifiedBy || '-' }}</span>
              <span>{{ t('admin.customer.headers.modifiedOn') }}: {{ formatDateCell(row.modifiedOn) }}</span>
            </div>
          </v-card>
        </div>

        <v-data-table
          v-else
          :headers="headers"
          :items="displayedRows"
          :loading="loading"
          item-value="customerId"
          v-model="selectedCustomerIds"
          :show-select="checkboxMode"
          density="compact"
          fixed-header
          height="62vh"
          class="admin-customer-table"
          @click:row="onRowClick"
        >
          <template #[`item.icon`]='{ item }'>
            <v-icon
              size="14"
              :style="{ color: isBackendBillingSynced(item) ? 'rgb(var(--v-theme-primary))' : 'rgb(var(--v-theme-on-surface-variant))' }"
            >mdi-connection</v-icon>
          </template>

          <template #[`item.createdOn`]='{ item }'>{{ formatDateCell(item.createdOn) }}</template>
          <template #[`item.modifiedOn`]='{ item }'>{{ formatDateCell(item.modifiedOn) }}</template>
        </v-data-table>

      </v-card-text>
    </v-card>

    <v-dialog v-model="dialogOpen" max-width="min(100%, 920px)" scrollable>
      <AdminCustomerRecordDialog
        :customer-id="editingCustomerId"
        @saved="handleSaved"
        @deleted="handleDeleted"
        @cancel="dialogOpen = false"
      />
    </v-dialog>

    <v-dialog v-model="mergeDialogOpen" max-width="480" :persistent="merging">
      <v-card>
        <v-card-title class="text-h6">{{ t('admin.customer.merge.dialogTitle') }}</v-card-title>
        <v-card-text>
          <p class="text-body-2 text-medium-emphasis mb-4">{{ t('admin.customer.merge.hint') }}</p>
          <v-radio-group v-model="mergeTargetId" hide-details>
            <v-radio
              v-for="customer in mergeSelectedCustomers"
              :key="customer.customerId"
              :value="customer.customerId"
              :label="customer.customerName + (customer.customerCode ? ' (' + customer.customerCode + ')' : '')"
            />
          </v-radio-group>
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn variant="text" :disabled="merging" @click="mergeDialogOpen = false">
            {{ t('admin.customer.merge.cancel') }}
          </v-btn>
          <v-btn
            color="primary"
            variant="flat"
            :disabled="!mergeTargetId"
            :loading="merging"
            @click="confirmMerge"
          >
            {{ t('admin.customer.merge.confirm') }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-snackbar v-model="saveSuccess" color="success" timeout="3000">
      {{ successMessage }}
      <template #actions>
        <v-btn variant="text" @click="saveSuccess = false">{{ t('common.cancel') }}</v-btn>
      </template>
    </v-snackbar>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import ListMobileCard, { type ListMobileCardColumn } from '@/components/grids/ListMobileCard.vue'
import { useResponsiveList } from '@/composables/useResponsiveList'
import { useViewSettings } from '@/composables/useColumnPersistence'
import AdminCustomerRecordDialog from '@/components/forms/AdminCustomerRecordDialog.vue'
import { getAdminCustomers, mergeAdminCustomers } from '@/services/admin'
import { syncCustomerToBilling } from '@/services/billing'
import type { AdminCustomerListItem, AdminCustomerRecord } from '@/types/api'

type AdminCustomerViewMode = 'detail' | 'card'

type AdminCustomerDisplayItem = AdminCustomerListItem & {
  icon: string
  ln: number
}

type CustomerBillingSyncStatus = {
  synced: boolean
  syncedAt?: string
  error?: string
}

const rows = ref<AdminCustomerListItem[]>([])
const loading = ref(false)
const lookup = ref('')
const errorMessage = ref('')
const viewSettings = useViewSettings('admin-customer', {
  visibleColumns: ['icon', 'customerName', 'ln', 'loginAccount', 'loginPassword', 'customerCode'],
  sortKey: 'customerName',
  sortDirection: 'asc',
  checkboxMode: false,
  viewMode: 'detail',
})
const visibleColumnKeys = viewSettings.visibleColumns
const sortKey = viewSettings.sortKey
const sortDirection = viewSettings.sortDirection
const checkboxMode = viewSettings.checkboxMode
const viewMode = viewSettings.viewMode
const selectedCustomerIds = ref<string[]>([])
const dialogOpen = ref(false)
const editingCustomerId = ref<string | null>(null)
const saveSuccess = ref(false)
const successMessage = ref('')
const syncingCustomerId = ref<string | null>(null)
const billingStatus = ref<{ [customerId: string]: CustomerBillingSyncStatus }>({})
const mergeDialogOpen = ref(false)
const mergeTargetId = ref<string | null>(null)
const merging = ref(false)

const { t } = useI18n({ useScope: 'global' })
const { isPhoneLayout, isColumnVisible } = useResponsiveList()

const isCardView = computed(() => viewMode.value === 'card')

const selectedCustomerBillingStatus = computed(() => {
  const customerId = selectedCustomerId.value
  if (!customerId) return null
  return billingStatus.value[customerId] ?? null
})

const allHeaders = computed(() => [
  { title: '', key: 'icon', width: '32px', sortable: false },
  { title: t('admin.customer.headers.customerName'), key: 'customerName', minWidth: '220px' },
  { title: '#', key: 'ln', width: '54px', sortable: false },
  { title: t('admin.customer.headers.loginAccount'), key: 'loginAccount', minWidth: '130px' },
  { title: t('admin.customer.headers.loginPassword'), key: 'loginPassword', minWidth: '120px' },
  { title: t('admin.customer.headers.customerCode'), key: 'customerCode', minWidth: '130px' },
  { title: t('admin.customer.headers.createdOn'), key: 'createdOn', minWidth: '135px' },
  { title: t('admin.customer.headers.createdBy'), key: 'createdBy', minWidth: '100px' },
  { title: t('admin.customer.headers.modifiedOn'), key: 'modifiedOn', minWidth: '135px' },
  { title: t('admin.customer.headers.modifiedBy'), key: 'modifiedBy', minWidth: '100px' },
])

const headers = computed(() =>
  allHeaders.value.filter((h) =>
    visibleColumnKeys.value.includes(String(h.key)) &&
    isColumnVisible(String(h.key), {
      hideOnPhone: ['loginPassword', 'createdOn', 'createdBy', 'modifiedOn', 'modifiedBy'],
      hideOnTablet: ['loginPassword'],
    }),
  ),
)

const mobileColumns = computed<ListMobileCardColumn<AdminCustomerDisplayItem>[]>(() => [
  { key: 'customerName', label: t('admin.customer.headers.customerName'), section: 'header', emphasis: true },
  { key: 'customerCode', label: t('admin.customer.headers.customerCode'), section: 'header' },
  { key: 'loginAccount', label: t('admin.customer.headers.loginAccount'), section: 'body' },
  { key: 'createdBy', label: t('admin.customer.headers.createdBy'), section: 'footer' },
  {
    key: 'modifiedOn',
    label: t('admin.customer.headers.modifiedOn'),
    section: 'footer',
    formatter: (item) => formatDateCell(item.modifiedOn),
  },
])

const sortableColumns = computed(() =>
  allHeaders.value
    .filter((h) => h.sortable !== false)
    .map((h) => ({ key: String(h.key), title: String(h.title || h.key) })),
)

const columnOptions = computed(() => allHeaders.value.map((h) => ({ key: String(h.key), title: String(h.title || h.key) })))

const displayedRows = computed<AdminCustomerDisplayItem[]>(() => {
  const key = sortKey.value as keyof AdminCustomerListItem
  const result = [...rows.value]

  result.sort((lhs, rhs) => {
    const left = String(lhs[key] ?? '')
    const right = String(rhs[key] ?? '')
    return sortDirection.value === 'asc' ? left.localeCompare(right) : right.localeCompare(left)
  })

  return result.map((item, index) => ({
    ...item,
    icon: 'mdi-account',
    ln: index + 1,
  }))
})

const selectedCustomerId = computed(() => selectedCustomerIds.value[0] ?? null)

const canMergeSelectedCustomers = computed(() => selectedCustomerIds.value.length >= 2 && !merging.value)

const mergeSelectedCustomers = computed(() =>
  rows.value.filter((r) => selectedCustomerIds.value.includes(r.customerId)),
)

const canSyncSelectedCustomer = computed(() => {
  const customerId = selectedCustomerId.value
  if (!customerId) return false
  const customer = rows.value.find(r => r.customerId === customerId)
  return !!(customer && customer.customerCode)
})

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    rows.value = await getAdminCustomers({
      lookup: lookup.value.trim(),
      take: 500,
    })
  } catch {
    errorMessage.value = t('admin.customer.messages.loadFailed')
  } finally {
    loading.value = false
  }
}

async function applyLookup() {
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

function onRowClick(_event: Event, payload: { item: AdminCustomerListItem }) {
  selectedCustomerIds.value = [payload.item.customerId]
  openPopup(payload.item.customerId)
}

function onMobileCardClick(item: AdminCustomerDisplayItem) {
  if (checkboxMode.value) {
    selectedCustomerIds.value = [item.customerId]
    return
  }

  selectedCustomerIds.value = [item.customerId]
  openPopup(item.customerId)
}

function handleMobileSelect(item: Record<string, unknown>, selected: boolean) {
  const customerId = String(item.customerId ?? '')
  if (!customerId) return

  if (selected) {
    selectedCustomerIds.value = [...new Set([...selectedCustomerIds.value, customerId])]
    return
  }

  selectedCustomerIds.value = selectedCustomerIds.value.filter((id) => id !== customerId)
}

function openPopup(customerId = selectedCustomerId.value ?? editingCustomerId.value) {
  if (!customerId) {
    errorMessage.value = t('admin.customer.messages.selectRecordFirst')
    return
  }

  editingCustomerId.value = customerId
  dialogOpen.value = true
  errorMessage.value = ''
}

function openNewCustomer() {
  editingCustomerId.value = null
  dialogOpen.value = true
  errorMessage.value = ''
}

async function handleSaved(customer: AdminCustomerRecord) {
  await load()
  selectedCustomerIds.value = [customer.customerId]
  editingCustomerId.value = customer.customerId
  successMessage.value = t('admin.customer.messages.saveSuccess')
  saveSuccess.value = true
}

async function handleDeleted(id: string) {
  await load()
  selectedCustomerIds.value = selectedCustomerIds.value.filter((customerId) => customerId !== id)
  successMessage.value = t('admin.customer.messages.deleteSuccess')
  saveSuccess.value = true
}

function setViewMode(mode: AdminCustomerViewMode) {
  viewMode.value = mode
}

function handleCardCheckbox(customerId: string) {
  if (selectedCustomerIds.value.includes(customerId)) {
    selectedCustomerIds.value = selectedCustomerIds.value.filter((id) => id !== customerId)
    return
  }
  selectedCustomerIds.value = [...selectedCustomerIds.value, customerId]
}

function formatDateCell(value: string): string {
  if (!value) return '-'
  const normalized = value.replace('T', ' ')
  return normalized.length >= 16 ? normalized.slice(0, 16) : normalized
}

function isBackendBillingSynced(item: AdminCustomerListItem): boolean {
  return item.billingSyncStatus === 'success' && !!item.invoiceNinjaClientId
}

function openMergeDialog() {
  mergeTargetId.value = null
  mergeDialogOpen.value = true
  errorMessage.value = ''
}

async function confirmMerge() {
  const targetId = mergeTargetId.value
  if (!targetId || selectedCustomerIds.value.length < 2) return

  merging.value = true
  errorMessage.value = ''

  try {
    await mergeAdminCustomers({
      targetCustomerId: targetId,
      customerIds: selectedCustomerIds.value,
    })
    mergeDialogOpen.value = false
    await load()
    selectedCustomerIds.value = rows.value.some((r) => r.customerId === targetId) ? [targetId] : []
    successMessage.value = t('admin.customer.messages.mergeSuccess')
    saveSuccess.value = true
  } catch (err) {
    const errorMsg = err instanceof Error ? err.message : 'Unknown error'
    errorMessage.value = t('admin.customer.messages.mergeFailed', { error: errorMsg })
  } finally {
    merging.value = false
  }
}

async function syncSelectedCustomer() {
  const customerId = selectedCustomerId.value
  if (!customerId) return

  syncingCustomerId.value = customerId
  errorMessage.value = ''

  try {
    await syncCustomerToBilling({ customerId })
    billingStatus.value[customerId] = {
      synced: true,
      syncedAt: new Date().toISOString(),
    }
    successMessage.value = t('admin.customer.messages.billingSyncSuccess')
    saveSuccess.value = true
  } catch (err) {
    const errorMsg = err instanceof Error ? err.message : 'Unknown error'
    billingStatus.value[customerId] = {
      synced: false,
      error: errorMsg,
    }
    errorMessage.value = t('admin.customer.messages.billingSyncFailed', { error: errorMsg })
  } finally {
    syncingCustomerId.value = null
  }
}
</script>

<style scoped>
.admin-customer-page {
  min-height: 0;
  --admin-customer-header-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%);
  --admin-customer-header-fg: rgb(var(--v-theme-on-surface-variant));
}

.admin-customer-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
  background: linear-gradient(180deg, rgba(224, 237, 255, 0.92), rgba(241, 247, 255, 0.96));
}

.filter-bar {
  display: grid;
  gap: 12px;
  grid-template-columns: minmax(260px, 1fr) auto auto;
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

.sync-billing-btn.v-btn--disabled :is(.v-btn__loader, .v-progress-circular) {
  display: none !important;
}

.admin-customer-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.admin-customer-table :deep(.v-table__wrapper > table > thead > tr > th),
.admin-customer-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--admin-customer-header-bg) !important;
  color: var(--admin-customer-header-fg) !important;
}

.admin-customer-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.admin-customer-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.admin-customer-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.admin-customer-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}

.customer-card-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .customer-card-list {
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    align-items: start;
  }
}

.customer-card {
  display: grid;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgba(255, 255, 255, 0.72);
  cursor: pointer;
}

.customer-card:active {
  background: rgba(255, 255, 255, 0.92);
}

.customer-card__header,
.customer-card__body,
.customer-card__footer {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
}

.customer-card__body {
  display: grid;
  gap: 0.45rem;
}

</style>
