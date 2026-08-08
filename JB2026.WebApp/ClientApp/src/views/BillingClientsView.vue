<template>
  <section class="page-section billing-clients-page">
    <v-card rounded="xl" elevation="0" class="panel-card billing-clients-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3 pb-2">
        <div>
          <h3 class="text-h6 mb-1">{{ t('billing.clients.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('billing.clients.subtitle') }}</p>
        </div>
      </v-card-title>

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('billing.clients.lookup')"
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
                {{ t('billing.clients.actions.columns') }}
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
                {{ t('billing.clients.actions.sorting') }}
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
                :label="t('billing.clients.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('billing.clients.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('billing.clients.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <template v-if="!isPhoneLayout">
            <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
              {{ t('billing.clients.actions.checkbox') }}
            </v-btn>

            <v-menu location="bottom">
              <template #activator="{ props }">
                <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                  {{ t('billing.clients.actions.views') }}
                </v-btn>
              </template>
              <v-list density="compact" class="toolbar-menu-list">
                <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                  <v-list-item-title>{{ t('billing.clients.actions.detailView') }}</v-list-item-title>
                </v-list-item>
                <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                  <v-list-item-title>{{ t('billing.clients.actions.cardView') }}</v-list-item-title>
                </v-list-item>
              </v-list>
            </v-menu>

            <v-divider vertical class="mx-1" />

            <v-btn variant="outlined" size="small" color="primary" prepend-icon="mdi-plus-circle-outline" @click="openNewClient">
              {{ t('billing.clients.actions.newClient') }}
            </v-btn>
          </template>

          <v-menu v-else location="bottom end">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                {{ t('billing.clients.actions.views') }}
              </v-btn>
            </template>

            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
                <v-list-item-title>{{ t('billing.clients.actions.checkbox') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>{{ t('billing.clients.actions.detailView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>{{ t('billing.clients.actions.cardView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-plus-circle-outline" @click="openNewClient">
                <v-list-item-title>{{ t('billing.clients.actions.newClient') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <span v-if="checkboxMode" class="text-caption text-medium-emphasis">
            {{ t('billing.clients.actions.selected', { count: selectedClientIds.length }) }}
          </span>

          <v-spacer />
        </div>

        <ListMobileCard
          v-if="isPhoneLayout"
          :items="displayedRows"
          :columns="mobileColumns"
          item-key="externalClientId"
          :checkbox-mode="checkboxMode"
          :selected-ids="selectedClientIds"
          :on-select="handleMobileSelect"
        />

        <div v-else-if="isCardView" class="billing-clients-card-list">
          <v-card
            v-for="row in displayedRows"
            :key="row.externalClientId"
            rounded="lg"
            elevation="0"
            class="billing-client-card"
            @click="openClientRecord(row)"
          >
            <div v-if="checkboxMode" class="billing-client-card__checkbox-anchor" @click.stop>
              <v-checkbox-btn
                class="billing-client-card__checkbox"
                :model-value="selectedClientIds.includes(row.externalClientId)"
                density="compact"
                hide-details
                @click.stop="handleCardCheckbox(row.externalClientId)"
              />
            </div>
            <div class="billing-client-card__header">
              <div class="d-flex align-center ga-2">
                <v-icon size="18" color="primary">mdi-account-sync</v-icon>
                <div>
                  <div class="billing-client-card__title">{{ row.clientName }}</div>
                  <div class="billing-client-card__subtitle">{{ row.clientCode || t('billing.clients.labels.empty') }}</div>
                </div>
              </div>
            </div>
            <div class="billing-client-card__body">
              <span class="billing-client-card__meta">
                <span class="billing-client-card__label">{{ t('billing.clients.headers.externalClientId') }}</span>
                {{ row.externalClientId || '-' }}
              </span>
            </div>
            <div class="billing-client-card__footer">
              <span class="billing-client-card__meta">
                <span class="billing-client-card__label">{{ t('billing.clients.headers.outstandingBalance') }}</span>
                <span class="billing-client-card__balance">{{ formatOutstandingBalance(row.outstandingBalance) }}</span>
              </span>
            </div>
          </v-card>
        </div>

        <v-data-table
          v-else
          :headers="headers"
          :items="displayedRows"
          :loading="loading"
          item-value="externalClientId"
          v-model="selectedClientIds"
          :show-select="checkboxMode"
          density="compact"
          fixed-header
          height="62vh"
          class="billing-clients-table"
          @click:row="onRowClick"
        >
          <template #[`item.icon`]>
            <v-icon size="16" color="primary">mdi-account-sync</v-icon>
          </template>

          <template #[`item.clientName`]="{ item }">
            <a
              href="#"
              class="billing-clients-name-link"
              @click.prevent.stop="openClientRecord(item)"
            >{{ item.clientName }}</a>
          </template>

          <template #[`item.outstandingBalance`]="{ item }">
            <span class="billing-clients-balance">{{ formatOutstandingBalance(item.outstandingBalance) }}</span>
          </template>
        </v-data-table>

      </v-card-text>
    </v-card>

    <v-snackbar v-model="saveSuccess" color="success" timeout="3000">
      {{ successMessage }}
      <template #actions>
        <v-btn variant="text" @click="saveSuccess = false">{{ t('common.cancel') }}</v-btn>
      </template>
    </v-snackbar>

    <v-dialog v-model="dialogOpen" max-width="min(100%, 760px)" scrollable>
      <BillingClientRecordDialog
        :key="dialogKey"
        :customer-id="recordCustomerId ?? undefined"
        :external-client-id="recordExternalClientId ?? undefined"
        @saved="handleSaved"
        @cancel="dialogOpen = false"
      />
    </v-dialog>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import BillingClientRecordDialog from '@/components/billing/BillingClientRecordDialog.vue'
import ListMobileCard, { type ListMobileCardColumn } from '@/components/grids/ListMobileCard.vue'
import { useResponsiveList } from '@/composables/useResponsiveList'
import { useViewSettings } from '@/composables/useColumnPersistence'
import { listBillingClients, type BillingClientOption, type SyncCustomerResponse } from '@/services/billing'
import { getAdminCustomers } from '@/services/admin'

type BillingClientsViewMode = 'detail' | 'card'

type BillingClientDisplayItem = BillingClientOption & {
  icon: string
  ln: number
  clientName: string
  clientCode: string
}

const rows = ref<BillingClientOption[]>([])
const loading = ref(false)
const lookup = ref('')
const errorMessage = ref('')
const selectedClientIds = ref<string[]>([])
const dialogOpen = ref(false)
const dialogKey = ref(0)
const recordCustomerId = ref<string | null>(null)
const recordExternalClientId = ref<string | null>(null)
const saveSuccess = ref(false)
const successMessage = ref('')
const viewSettings = useViewSettings('billing-clients', {
  visibleColumns: ['icon', 'ln', 'clientName', 'clientCode', 'outstandingBalance'],
  sortKey: 'clientName',
  sortDirection: 'asc',
  checkboxMode: false,
  viewMode: 'detail',
})

const visibleColumnKeys = viewSettings.visibleColumns
const sortKey = viewSettings.sortKey
const sortDirection = viewSettings.sortDirection
const checkboxMode = viewSettings.checkboxMode
const viewMode = viewSettings.viewMode

const { t } = useI18n({ useScope: 'global' })
const { isPhoneLayout, isColumnVisible } = useResponsiveList()

const isCardView = computed(() => viewMode.value === 'card')

const allHeaders = computed(() => [
  { title: '', key: 'icon', width: '32px', sortable: false },
  { title: '#', key: 'ln', width: '54px', sortable: false },
  { title: t('billing.clients.headers.clientName'), key: 'clientName', minWidth: '220px' },
  { title: t('billing.clients.headers.clientCode'), key: 'clientCode', minWidth: '130px' },
  { title: t('billing.clients.headers.externalClientId'), key: 'externalClientId', minWidth: '130px' },
  { title: t('billing.clients.headers.outstandingBalance'), key: 'outstandingBalance', minWidth: '180px' },
])

const headers = computed(() =>
  allHeaders.value.filter((h) =>
    visibleColumnKeys.value.includes(String(h.key)) &&
    isColumnVisible(String(h.key), {
      hideOnPhone: ['externalClientId'],
      hideOnTablet: [],
    }),
  ),
)

const mobileColumns = computed<ListMobileCardColumn<BillingClientDisplayItem>[]>(() => [
  { key: 'ln', label: '#', section: 'header' },
  { key: 'clientName', label: t('billing.clients.headers.clientName'), section: 'header', emphasis: true },
  { key: 'clientCode', label: t('billing.clients.headers.clientCode'), section: 'header' },
  { key: 'externalClientId', label: t('billing.clients.headers.externalClientId'), section: 'body' },
  {
    key: 'outstandingBalance',
    label: t('billing.clients.headers.outstandingBalance'),
    section: 'footer',
    formatter: (item) => formatOutstandingBalance(item.outstandingBalance),
  },
])

const sortableColumns = computed(() =>
  allHeaders.value
    .filter((h) => h.sortable !== false)
    .map((h) => ({ key: String(h.key), title: String(h.title || h.key) })),
)

const columnOptions = computed(() => allHeaders.value.map((h) => ({ key: String(h.key), title: String(h.title || h.key) })))

const displayedRows = computed<BillingClientDisplayItem[]>(() => {
  const result = rows.value.map((item, index) => ({
    ...item,
    icon: 'mdi-account',
    ln: index + 1,
    clientName: item.displayName || item.name || t('billing.clients.labels.empty'),
    clientCode: item.idNumber || '',
  }))

  const activeSortKey = sortKey.value ?? 'clientName'
  const direction = sortDirection.value === 'desc' ? -1 : 1
  result.sort((left, right) => compareClients(left, right, activeSortKey) * direction)
  return result
})

const balanceFormatter = new Intl.NumberFormat('en-US', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    rows.value = await listBillingClients(lookup.value.trim() || undefined)
  } catch (error) {
    const errorMsg = error instanceof Error ? error.message : ''
    errorMessage.value = errorMsg || t('billing.clients.messages.loadFailed')
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

function setViewMode(mode: BillingClientsViewMode) {
  viewMode.value = mode
}

function onRowClick(_event: Event, payload: { item: BillingClientDisplayItem }) {
  selectedClientIds.value = [payload.item.externalClientId]
}

function handleCardCheckbox(externalClientId: string) {
  if (selectedClientIds.value.includes(externalClientId)) {
    selectedClientIds.value = selectedClientIds.value.filter((id) => id !== externalClientId)
    return
  }

  selectedClientIds.value = [...selectedClientIds.value, externalClientId]
}

function handleMobileSelect(item: Record<string, unknown>, selected: boolean) {
  const externalClientId = String(item.externalClientId ?? '')
  if (!externalClientId) return

  if (selected) {
    selectedClientIds.value = [...new Set([...selectedClientIds.value, externalClientId])]
    return
  }

  selectedClientIds.value = selectedClientIds.value.filter((id) => id !== externalClientId)
}

function openNewClient() {
  recordCustomerId.value = null
  recordExternalClientId.value = null
  dialogKey.value += 1
  dialogOpen.value = true
  errorMessage.value = ''
}

async function openClientRecord(row: BillingClientDisplayItem) {
  recordCustomerId.value = null
  recordExternalClientId.value = null

  const name = row.name || row.clientName
  if (name) {
    try {
      const matches = await getAdminCustomers({ lookup: name, take: 100 })
      const target = matches.find((customer) => customer.invoiceNinjaClientId === row.externalClientId)
        ?? matches.find((customer) => customer.customerName.localeCompare(name, undefined, { sensitivity: 'base' }) === 0)
      if (target) {
        recordCustomerId.value = target.customerId
        recordExternalClientId.value = row.externalClientId
      }
    } catch {
      // Leave record unset; dialog falls back to the normal migrate flow.
    }
  }

  dialogKey.value += 1
  dialogOpen.value = true
  errorMessage.value = ''
}

async function handleSaved(_result: SyncCustomerResponse, customerName: string) {
  dialogOpen.value = false
  await load()
  const migrated = rows.value.find((row) => row.displayName === customerName || row.name === customerName)
  if (migrated) {
    selectedClientIds.value = [migrated.externalClientId]
  }
  successMessage.value = t('billing.clients.messages.migrateSuccess')
  saveSuccess.value = true
}

function formatOutstandingBalance(value: number) {
  return `$${balanceFormatter.format(value ?? 0)}`
}

function compareClients(left: BillingClientDisplayItem, right: BillingClientDisplayItem, key: string) {
  switch (key) {
    case 'outstandingBalance':
      return left.outstandingBalance - right.outstandingBalance
    case 'clientCode':
      return left.clientCode.localeCompare(right.clientCode)
    case 'externalClientId':
      return left.externalClientId.localeCompare(right.externalClientId)
    case 'clientName':
    default:
      return left.clientName.localeCompare(right.clientName)
  }
}
</script>

<style scoped>
.billing-clients-page {
  min-height: 0;
  --billing-clients-header-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%);
  --billing-clients-header-fg: rgb(var(--v-theme-on-surface-variant));
}

.billing-clients-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
  background: linear-gradient(180deg, rgba(224, 237, 255, 0.92), rgba(241, 247, 255, 0.96));
}

.filter-bar {
  display: grid;
  gap: 12px;
  grid-template-columns: minmax(260px, 1fr) auto;
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

.billing-clients-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.billing-clients-table :deep(.v-table__wrapper > table > thead > tr > th),
.billing-clients-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--billing-clients-header-bg) !important;
  color: var(--billing-clients-header-fg) !important;
}

.billing-clients-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.billing-clients-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.billing-clients-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.billing-clients-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

.billing-clients-balance {
  display: inline-block;
  text-align: right;
  width: 100%;
}

.billing-clients-name-link {
  color: rgb(var(--v-theme-primary));
  text-decoration: none;
  font-weight: 500;
}

.billing-clients-name-link:hover {
  text-decoration: underline;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}

.billing-clients-card-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .billing-clients-card-list {
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    align-items: start;
  }
}

.billing-client-card {
  position: relative;
  display: grid;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.18);
  background: rgb(var(--v-theme-surface));
  color: rgba(var(--v-theme-on-surface), 0.92);
  cursor: pointer;
  overflow: hidden;
}

.billing-client-card__checkbox-anchor {
  position: absolute;
  top: 0.35rem;
  right: 0.35rem;
  z-index: 1;
  display: flex;
  align-items: flex-start;
  justify-content: flex-end;
}

.billing-client-card__checkbox {
  margin: 0;
}

.billing-client-card:active {
  background: color-mix(in srgb, rgb(var(--v-theme-surface)) 92%, rgb(var(--v-theme-primary)) 8%);
}

.billing-client-card__header,
.billing-client-card__body,
.billing-client-card__footer {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
}

.billing-client-card__body {
  display: grid;
  gap: 0.45rem;
}

.billing-client-card__title {
  font-size: 0.9375rem;
  font-weight: 700;
  line-height: 1.3;
  color: rgba(var(--v-theme-on-surface), 0.95);
}

.billing-client-card__subtitle {
  margin-top: 0.1rem;
  font-size: 0.75rem;
  line-height: 1.3;
  color: rgba(var(--v-theme-on-surface), 0.72);
}

.billing-client-card__meta {
  display: inline-flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 0.35rem;
  font-size: 0.8125rem;
  line-height: 1.35;
  color: rgba(var(--v-theme-on-surface), 0.86);
}

.billing-client-card__label {
  color: rgba(var(--v-theme-on-surface), 0.62);
  font-weight: 500;
}

.billing-client-card__label::after {
  content: ':';
}

.billing-client-card__balance {
  font-weight: 600;
  color: rgba(var(--v-theme-on-surface), 0.92);
  font-variant-numeric: tabular-nums;
}
</style>
