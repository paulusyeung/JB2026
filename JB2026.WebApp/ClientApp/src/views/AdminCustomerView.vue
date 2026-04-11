<template>
  <section class="page-section admin-customer-page" :class="{ 'admin-customer-page--dark': isDark }">
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

          <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="refreshList">
            {{ t('common.refresh') }}
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

          <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
            {{ t('admin.customer.actions.checkbox') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-eye-outline" @click="showUnavailable('admin.customer.actions.views')">
            {{ t('admin.customer.actions.views') }}
          </v-btn>

          <v-divider vertical class="mx-1" />

          <v-btn variant="outlined" size="small" prepend-icon="mdi-refresh" :loading="loading" @click="refreshList">
            {{ t('admin.customer.actions.refresh') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-tune" @click="showUnavailable('admin.customer.actions.preference')">
            {{ t('admin.customer.actions.preference') }}
          </v-btn>

          <v-btn variant="outlined" size="small" color="primary" prepend-icon="mdi-account-plus" @click="showUnavailable('admin.customer.actions.newCustomer')">
            {{ t('admin.customer.actions.newCustomer') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-open-in-new" :disabled="!selectedCustomerId" @click="openPopup">
            {{ t('admin.customer.actions.popup') }}
          </v-btn>

          <span v-if="checkboxMode" class="text-caption text-medium-emphasis">
            {{ t('admin.customer.actions.selected', { count: selectedCustomerIds.length }) }}
          </span>
        </div>

        <v-data-table
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
          @dblclick="openPopup"
        >
          <template #[`item.icon`]>
            <v-icon size="14" color="secondary">mdi-account-group</v-icon>
          </template>

          <template #[`item.createdOn`]='{ item }'>{{ formatDateCell(item.createdOn) }}</template>
          <template #[`item.modifiedOn`]='{ item }'>{{ formatDateCell(item.modifiedOn) }}</template>
        </v-data-table>

        <div class="text-caption text-medium-emphasis mt-2">
          {{ t('admin.customer.rows', { count: displayedRows.length }) }}
        </div>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useTheme } from 'vuetify'
import { getAdminCustomers } from '@/services/admin'
import type { AdminCustomerListItem } from '@/types/api'

type AdminCustomerDisplayItem = AdminCustomerListItem & {
  icon: string
  ln: number
}

const rows = ref<AdminCustomerListItem[]>([])
const loading = ref(false)
const lookup = ref('')
const errorMessage = ref('')
const checkboxMode = ref(false)
const selectedCustomerIds = ref<string[]>([])
const sortDirection = ref<'asc' | 'desc'>('asc')
const sortKey = ref('customerName')
const visibleColumnKeys = ref<string[]>([
  'icon',
  'customerName',
  'ln',
  'loginAccount',
  'loginPassword',
  'customerCode',
  'createdOn',
  'createdBy',
  'modifiedOn',
  'modifiedBy',
])

const { t } = useI18n({ useScope: 'global' })
const theme = useTheme()
const isDark = computed(() => theme.global.current.value.dark)

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

const headers = computed(() => allHeaders.value.filter((h) => visibleColumnKeys.value.includes(String(h.key))))

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
    icon: 'mdi-account-group',
    ln: index + 1,
  }))
})

const selectedCustomerId = computed(() => selectedCustomerIds.value[0] ?? null)

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

async function refreshList() {
  lookup.value = ''
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
  if (checkboxMode.value) return
  selectedCustomerIds.value = [payload.item.customerId]
}

function openPopup() {
  if (!selectedCustomerId.value) {
    errorMessage.value = t('admin.customer.messages.selectRecordFirst')
    return
  }

  errorMessage.value = t('admin.customer.messages.popupUnavailable')
}

function showUnavailable(actionKey: string) {
  errorMessage.value = t('admin.customer.messages.actionUnavailable', { action: t(actionKey) })
}

function formatDateCell(value: string): string {
  if (!value) return '-'
  const normalized = value.replace('T', ' ')
  return normalized.length >= 16 ? normalized.slice(0, 16) : normalized
}
</script>

<style scoped>
.admin-customer-page {
  min-height: 0;
  --admin-customer-header-bg: rgba(195, 216, 248, 0.92);
  --admin-customer-header-fg: inherit;
}

.admin-customer-page--dark {
  --admin-customer-header-bg: rgba(52, 74, 104, 0.95);
  --admin-customer-header-fg: rgba(239, 246, 255, 0.98);
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
</style>
