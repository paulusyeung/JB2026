<template>
  <section class="page-section admin-supplier-page">
    <v-card rounded="xl" elevation="0" class="panel-card admin-supplier-card">


      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('admin.supplier.lookup')"
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
                {{ t('admin.supplier.actions.columns') }}
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
                {{ t('admin.supplier.actions.sorting') }}
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
                :label="t('admin.supplier.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('admin.supplier.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('admin.supplier.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <template v-if="!isPhoneLayout">
            <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
              {{ t('admin.supplier.actions.checkbox') }}
            </v-btn>

            <v-menu location="bottom">
              <template #activator="{ props }">
                <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                  {{ t('admin.supplier.actions.views') }}
                </v-btn>
              </template>
              <v-list density="compact" class="toolbar-menu-list">
                <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                  <v-list-item-title>{{ t('admin.supplier.actions.detailView') }}</v-list-item-title>
                </v-list-item>
                <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                  <v-list-item-title>{{ t('admin.supplier.actions.cardView') }}</v-list-item-title>
                </v-list-item>
              </v-list>
            </v-menu>

            <v-divider vertical class="mx-1" />

            <v-btn variant="outlined" size="small" color="primary" prepend-icon="mdi-account-plus" @click="openNewSupplier">
              {{ t('admin.supplier.actions.newSupplier') }}
            </v-btn>
          </template>

          <v-menu v-else location="bottom end">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                {{ t('admin.supplier.actions.views') }}
              </v-btn>
            </template>

            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
                <v-list-item-title>{{ t('admin.supplier.actions.checkbox') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>{{ t('admin.supplier.actions.detailView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>{{ t('admin.supplier.actions.cardView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-account-plus" @click="openNewSupplier">
                <v-list-item-title>{{ t('admin.supplier.actions.newSupplier') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <span v-if="checkboxMode" class="text-caption text-medium-emphasis">
            {{ t('admin.supplier.actions.selected', { count: selectedSupplierIds.length }) }}
          </span>
        </div>

        <ListMobileCard
          v-if="isPhoneLayout"
          :items="displayedRows"
          :columns="mobileColumns"
          item-key="supplierId"
          :checkbox-mode="checkboxMode"
          :selected-ids="selectedSupplierIds"
          :on-select="handleMobileSelect"
          :on-card-click="(item) => onMobileCardClick(item)"
        />

        <div v-else-if="isCardView" class="supplier-card-list">
          <v-card
            v-for="row in displayedRows"
            :key="row.supplierId"
            rounded="lg"
            elevation="0"
            class="supplier-card"
            @click="openPopup(row.supplierId)"
          >
            <div class="supplier-card__header">
              <div class="d-flex align-center ga-2">
                <v-icon size="18" color="secondary">mdi-truck-delivery</v-icon>
                <div>
                  <div class="text-subtitle-2 font-weight-bold">{{ row.supplierName }}</div>
                  <div class="text-caption text-medium-emphasis">{{ row.supplierCode || '-' }}</div>
                </div>
              </div>
              <v-checkbox-btn
                v-if="checkboxMode"
                :model-value="selectedSupplierIds.includes(row.supplierId)"
                density="compact"
                hide-details
                @click.stop="handleCardCheckbox(row.supplierId)"
              />
            </div>
            <div class="supplier-card__body">
              <span class="text-caption">{{ t('admin.supplier.headers.loginAccount') }}: {{ row.loginAccount || '-' }}</span>
            </div>
            <div class="supplier-card__footer text-caption text-medium-emphasis">
              <span>{{ t('admin.supplier.headers.modifiedBy') }}: {{ row.modifiedBy || '-' }}</span>
              <span>{{ t('admin.supplier.headers.modifiedOn') }}: {{ formatDateCell(row.modifiedOn) }}</span>
            </div>
          </v-card>
        </div>

        <v-data-table
          v-else
          :headers="headers"
          :items="displayedRows"
          :loading="loading"
          item-value="supplierId"
          v-model="selectedSupplierIds"
          :show-select="checkboxMode"
          density="compact"
          fixed-header
          height="62vh"
          class="admin-supplier-table"
          @click:row="onRowClick"
          @dblclick="openPopup"
        >
          <template #[`item.icon`]>
            <v-icon size="14" color="secondary">mdi-truck-delivery</v-icon>
          </template>

          <template #[`item.createdOn`]='{ item }'>{{ formatDateCell(item.createdOn) }}</template>
          <template #[`item.modifiedOn`]='{ item }'>{{ formatDateCell(item.modifiedOn) }}</template>
        </v-data-table>

      </v-card-text>
    </v-card>

    <v-dialog v-model="dialogOpen" max-width="min(100%, 920px)" scrollable>
      <AdminSupplierRecordDialog
        :supplier-id="editingSupplierId"
        @saved="handleSaved"
        @deleted="handleDeleted"
        @cancel="dialogOpen = false"
      />
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
import AdminSupplierRecordDialog from '@/components/forms/AdminSupplierRecordDialog.vue'
import { getAdminSuppliers } from '@/services/admin'
import type { AdminSupplierListItem, AdminSupplierRecord } from '@/types/api'

type AdminSupplierViewMode = 'detail' | 'card'

type AdminSupplierDisplayItem = AdminSupplierListItem & {
  icon: string
  ln: number
}

const rows = ref<AdminSupplierListItem[]>([])
const loading = ref(false)
const lookup = ref('')
const errorMessage = ref('')
const viewSettings = useViewSettings('admin-supplier', {
  visibleColumns: ['icon', 'supplierName', 'ln', 'loginAccount', 'loginPassword', 'supplierCode'],
  sortKey: 'supplierName',
  sortDirection: 'asc',
  checkboxMode: false,
  viewMode: 'detail',
})
const visibleColumnKeys = viewSettings.visibleColumns
const sortKey = viewSettings.sortKey
const sortDirection = viewSettings.sortDirection
const checkboxMode = viewSettings.checkboxMode
const viewMode = viewSettings.viewMode
const selectedSupplierIds = ref<string[]>([])
const dialogOpen = ref(false)
const editingSupplierId = ref<string | null>(null)
const saveSuccess = ref(false)
const successMessage = ref('')

const { t } = useI18n({ useScope: 'global' })
const { isPhoneLayout, isColumnVisible } = useResponsiveList()

const isCardView = computed(() => viewMode.value === 'card')

const allHeaders = computed(() => [
  { title: '', key: 'icon', width: '32px', sortable: false },
  { title: t('admin.supplier.headers.supplierName'), key: 'supplierName', minWidth: '220px' },
  { title: '#', key: 'ln', width: '54px', sortable: false },
  { title: t('admin.supplier.headers.loginAccount'), key: 'loginAccount', minWidth: '130px' },
  { title: t('admin.supplier.headers.loginPassword'), key: 'loginPassword', minWidth: '120px' },
  { title: t('admin.supplier.headers.supplierCode'), key: 'supplierCode', minWidth: '130px' },
  { title: t('admin.supplier.headers.createdOn'), key: 'createdOn', minWidth: '135px' },
  { title: t('admin.supplier.headers.createdBy'), key: 'createdBy', minWidth: '100px' },
  { title: t('admin.supplier.headers.modifiedOn'), key: 'modifiedOn', minWidth: '135px' },
  { title: t('admin.supplier.headers.modifiedBy'), key: 'modifiedBy', minWidth: '100px' },
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

const mobileColumns = computed<ListMobileCardColumn<AdminSupplierDisplayItem>[]>(() => [
  { key: 'supplierName', label: t('admin.supplier.headers.supplierName'), section: 'header', emphasis: true },
  { key: 'supplierCode', label: t('admin.supplier.headers.supplierCode'), section: 'header' },
  { key: 'loginAccount', label: t('admin.supplier.headers.loginAccount'), section: 'body' },
  { key: 'createdBy', label: t('admin.supplier.headers.createdBy'), section: 'footer' },
  {
    key: 'modifiedOn',
    label: t('admin.supplier.headers.modifiedOn'),
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

const displayedRows = computed<AdminSupplierDisplayItem[]>(() => {
  const key = sortKey.value as keyof AdminSupplierListItem
  const result = [...rows.value]

  result.sort((lhs, rhs) => {
    const left = String(lhs[key] ?? '')
    const right = String(rhs[key] ?? '')
    return sortDirection.value === 'asc' ? left.localeCompare(right) : right.localeCompare(left)
  })

  return result.map((item, index) => ({
    ...item,
    icon: 'mdi-truck-delivery',
    ln: index + 1,
  }))
})

const selectedSupplierId = computed(() => selectedSupplierIds.value[0] ?? null)

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    rows.value = await getAdminSuppliers({
      lookup: lookup.value.trim(),
      take: 500,
    })
  } catch {
    errorMessage.value = t('admin.supplier.messages.loadFailed')
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

function onRowClick(_event: Event, payload: { item: AdminSupplierListItem }) {
  if (checkboxMode.value) return
  selectedSupplierIds.value = [payload.item.supplierId]
  openPopup(payload.item.supplierId)
}

function onMobileCardClick(item: AdminSupplierDisplayItem) {
  if (checkboxMode.value) {
    selectedSupplierIds.value = [item.supplierId]
    return
  }

  selectedSupplierIds.value = [item.supplierId]
  openPopup(item.supplierId)
}

function handleMobileSelect(item: Record<string, unknown>, selected: boolean) {
  const supplierId = String(item.supplierId ?? '')
  if (!supplierId) return

  if (selected) {
    selectedSupplierIds.value = [...new Set([...selectedSupplierIds.value, supplierId])]
    return
  }

  selectedSupplierIds.value = selectedSupplierIds.value.filter((id) => id !== supplierId)
}

function openPopup(supplierId = selectedSupplierId.value ?? editingSupplierId.value) {
  if (!supplierId) {
    errorMessage.value = t('admin.supplier.messages.selectRecordFirst')
    return
  }

  editingSupplierId.value = supplierId
  dialogOpen.value = true
  errorMessage.value = ''
}

function openNewSupplier() {
  editingSupplierId.value = null
  dialogOpen.value = true
  errorMessage.value = ''
}

async function handleSaved(supplier: AdminSupplierRecord) {
  await load()
  selectedSupplierIds.value = [supplier.supplierId]
  editingSupplierId.value = supplier.supplierId
  successMessage.value = t('admin.supplier.messages.saveSuccess')
  saveSuccess.value = true
}

async function handleDeleted(id: string) {
  await load()
  selectedSupplierIds.value = selectedSupplierIds.value.filter((supplierId) => supplierId !== id)
  successMessage.value = t('admin.supplier.messages.deleteSuccess')
  saveSuccess.value = true
}

function setViewMode(mode: AdminSupplierViewMode) {
  viewMode.value = mode
}

function handleCardCheckbox(supplierId: string) {
  if (selectedSupplierIds.value.includes(supplierId)) {
    selectedSupplierIds.value = selectedSupplierIds.value.filter((id) => id !== supplierId)
    return
  }
  selectedSupplierIds.value = [...selectedSupplierIds.value, supplierId]
}

function formatDateCell(value: string): string {
  if (!value) return '-'
  const normalized = value.replace('T', ' ')
  return normalized.length >= 16 ? normalized.slice(0, 16) : normalized
}
</script>

<style scoped>
.admin-supplier-page {
  min-height: 0;
  --admin-supplier-header-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%);
  --admin-supplier-header-fg: rgb(var(--v-theme-on-surface-variant));
}

.admin-supplier-card {
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

.admin-supplier-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.admin-supplier-table :deep(.v-table__wrapper > table > thead > tr > th),
.admin-supplier-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--admin-supplier-header-bg) !important;
  color: var(--admin-supplier-header-fg) !important;
}

.admin-supplier-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.admin-supplier-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.admin-supplier-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.admin-supplier-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}

.supplier-card-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .supplier-card-list {
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    align-items: start;
  }
}

.supplier-card {
  display: grid;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgba(255, 255, 255, 0.72);
  cursor: pointer;
}

.supplier-card:active {
  background: rgba(255, 255, 255, 0.92);
}

.supplier-card__header,
.supplier-card__footer {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
}

.supplier-card__body {
  display: grid;
  gap: 0.45rem;
}
</style>
