<template>
  <section class="page-section quotation-item-page">
    <v-card rounded="xl" elevation="0" class="panel-card quotation-item-card">


      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('admin.quotationItem.lookup')"
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
                {{ t('admin.quotationItem.actions.columns') }}
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
                {{ t('admin.quotationItem.actions.sorting') }}
              </v-btn>
            </template>
            <v-card min-width="280" class="pa-3">
              <v-select
                v-model="sortKey"
                :items="sortOptions"
                item-title="title"
                item-value="key"
                density="compact"
                variant="outlined"
                :label="t('admin.quotationItem.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('admin.quotationItem.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('admin.quotationItem.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <template v-if="!isPhoneLayout">
            <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
              {{ t('admin.quotationItem.actions.checkbox') }}
            </v-btn>

            <v-btn variant="outlined" size="small" prepend-icon="mdi-eye-outline" @click="showUnavailable('admin.quotationItem.actions.views')">
              {{ t('admin.quotationItem.actions.views') }}
            </v-btn>

            <v-divider vertical class="mx-1" />

            <v-btn variant="outlined" size="small" prepend-icon="mdi-refresh" :loading="loading" @click="refreshList">
              {{ t('admin.quotationItem.actions.refresh') }}
            </v-btn>

            <v-btn variant="outlined" size="small" prepend-icon="mdi-tune" disabled>
              {{ t('admin.quotationItem.actions.preference') }}
            </v-btn>

            <v-btn variant="outlined" size="small" prepend-icon="mdi-open-in-new" :disabled="!selectedItemId" @click="openPopup()">
              {{ t('admin.quotationItem.actions.popup') }}
            </v-btn>

            <v-btn color="primary" size="small" prepend-icon="mdi-plus" @click="openNewItem">
              {{ t('admin.quotationItem.actions.newItem') }}
            </v-btn>
          </template>

          <v-menu v-else location="bottom end">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                {{ t('admin.quotationItem.actions.views') }}
              </v-btn>
            </template>

            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
                <v-list-item-title>{{ t('admin.quotationItem.actions.checkbox') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-eye-outline" @click="showUnavailable('admin.quotationItem.actions.views')">
                <v-list-item-title>{{ t('admin.quotationItem.actions.views') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-refresh" :disabled="loading" @click="refreshList">
                <v-list-item-title>{{ t('admin.quotationItem.actions.refresh') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-tune" disabled>
                <v-list-item-title>{{ t('admin.quotationItem.actions.preference') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-open-in-new" :disabled="!selectedItemId" @click="openPopup()">
                <v-list-item-title>{{ t('admin.quotationItem.actions.popup') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-plus" @click="openNewItem">
                <v-list-item-title>{{ t('admin.quotationItem.actions.newItem') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <span v-if="checkboxMode" class="text-caption text-medium-emphasis">
            {{ t('admin.quotationItem.actions.selected', { count: selectedItemIds.length }) }}
          </span>
        </div>

        <ListMobileCard
          v-if="isPhoneLayout"
          :items="displayedRows"
          :columns="mobileColumns"
          item-key="itemId"
          :checkbox-mode="checkboxMode"
          :selected-ids="selectedItemIds"
          :on-select="handleMobileSelect"
          :on-card-click="(item) => onMobileCardClick(item)"
        />

        <div v-else class="quotation-item-table-shell">
        <v-data-table
          v-model="selectedItemIds"
          :headers="headers"
          :items="displayedRows"
          :loading="loading"
          item-value="itemId"
          :show-select="checkboxMode"
          density="compact"
          fixed-header
          height="100%"
          class="quotation-item-table"
          @click:row="onRowClick"
          @dblclick="openPopup"
        >
          <template #[`item.icon`]>
            <v-icon size="14" color="secondary">mdi-tag-outline</v-icon>
          </template>

          <template #[`item.unitCost`]='{ item }'>{{ formatUnitCost(item.unitCost) }}</template>
          <template #[`item.createdOn`]='{ item }'>{{ formatDateTime(item.createdOn) }}</template>
          <template #[`item.modifiedOn`]='{ item }'>{{ formatDateTime(item.modifiedOn) }}</template>
        </v-data-table>
        </div>

        <div class="text-caption text-medium-emphasis mt-2">
          {{ t('admin.quotationItem.rows', { count: displayedRows.length }) }}
        </div>
      </v-card-text>
    </v-card>

    <v-dialog v-model="dialogOpen" max-width="min(100%, 860px)" scrollable>
      <AdminQuotationItemRecordDialog
        :item="editingItem"
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
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import AdminQuotationItemRecordDialog from '@/components/forms/AdminQuotationItemRecordDialog.vue'
import { getAdminQuotationItems } from '@/services/admin'
import type { AdminQuotationItemListItem } from '@/types/api'

type SortDirection = 'asc' | 'desc'

type AdminQuotationItemDisplayItem = AdminQuotationItemListItem & {
  icon: string
  ln: number
  originalOrder: number
  localizedGroupName: string
  localizedItemName: string
  unitCostTypeLabel: string
}

const rows = ref<AdminQuotationItemListItem[]>([])
const loading = ref(false)
const lookup = ref('')
const errorMessage = ref('')
const checkboxMode = ref(false)
const selectedItemIds = ref<string[]>([])
const dialogOpen = ref(false)
const editingItem = ref<AdminQuotationItemListItem | null>(null)
const saveSuccess = ref(false)
const successMessage = ref('')
const sortDirection = ref<SortDirection>('asc')
const sortKey = ref('originalOrder')
const visibleColumnKeys = ref<string[]>([
  'icon',
  'localizedGroupName',
  'ln',
  'itemIndex',
  'localizedItemName',
  'unitCost',
  'minimum',
  'unitCostTypeLabel',
  'createdOn',
  'createdBy',
  'modifiedOn',
  'modifiedBy',
])

const { t, locale } = useI18n({ useScope: 'global' })
const { activeLocale, formatNumber } = useLocaleFormatters()
const { isPhoneLayout, isColumnVisible } = useResponsiveList()

const allHeaders = computed(() => [
  { title: '', key: 'icon', width: '32px', sortable: false },
  { title: t('admin.quotationItem.headers.groupName'), key: 'localizedGroupName', minWidth: '170px' },
  { title: '#', key: 'ln', width: '54px', sortable: false },
  { title: t('admin.quotationItem.headers.itemIndex'), key: 'itemIndex', width: '90px' },
  { title: t('admin.quotationItem.headers.itemName'), key: 'localizedItemName', minWidth: '220px' },
  { title: t('admin.quotationItem.headers.unitCost'), key: 'unitCost', minWidth: '130px' },
  { title: t('admin.quotationItem.headers.minimum'), key: 'minimum', minWidth: '120px' },
  { title: t('admin.quotationItem.headers.unitCostType'), key: 'unitCostTypeLabel', minWidth: '160px' },
  { title: t('admin.quotationItem.headers.createdOn'), key: 'createdOn', minWidth: '170px' },
  { title: t('admin.quotationItem.headers.createdBy'), key: 'createdBy', minWidth: '100px' },
  { title: t('admin.quotationItem.headers.modifiedOn'), key: 'modifiedOn', minWidth: '170px' },
  { title: t('admin.quotationItem.headers.modifiedBy'), key: 'modifiedBy', minWidth: '100px' },
])

const headers = computed(() =>
  allHeaders.value.filter((header) =>
    visibleColumnKeys.value.includes(String(header.key)) &&
    isColumnVisible(String(header.key), {
      hideOnPhone: ['createdOn', 'createdBy', 'modifiedOn', 'modifiedBy'],
      hideOnTablet: ['createdBy', 'modifiedBy'],
    }),
  ),
)

const mobileColumns = computed<ListMobileCardColumn<AdminQuotationItemDisplayItem>[]>(() => [
  { key: 'localizedItemName', label: t('admin.quotationItem.headers.itemName'), section: 'header', emphasis: true },
  { key: 'localizedGroupName', label: t('admin.quotationItem.headers.groupName'), section: 'header' },
  { key: 'itemIndex', label: t('admin.quotationItem.headers.itemIndex'), section: 'body' },
  {
    key: 'unitCost',
    label: t('admin.quotationItem.headers.unitCost'),
    section: 'body',
    formatter: (item) => formatUnitCost(item.unitCost),
  },
  { key: 'unitCostTypeLabel', label: t('admin.quotationItem.headers.unitCostType'), section: 'footer' },
])

const columnOptions = computed(() => allHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title || header.key) })))

const sortOptions = computed(() => [
  { key: 'originalOrder', title: t('admin.quotationItem.actions.legacyOrder') },
  ...allHeaders.value
    .filter((header) => header.sortable !== false)
    .map((header) => ({ key: String(header.key), title: String(header.title || header.key) })),
])

const localizedRows = computed<AdminQuotationItemDisplayItem[]>(() =>
  rows.value.map((item, index) => ({
    ...item,
    icon: 'mdi-tag-outline',
    ln: index + 1,
    originalOrder: index + 1,
    localizedGroupName: getLocalizedName(item.groupNameEn, item.groupNameCht, item.groupNameChs),
    localizedItemName: getLocalizedName(item.itemNameEn, item.itemNameCht, item.itemNameChs),
    unitCostTypeLabel: getUnitCostTypeLabel(item.unitCostType),
  })),
)

const displayedRows = computed<AdminQuotationItemDisplayItem[]>(() => {
  const result = [...localizedRows.value]
  const currentSortKey = sortKey.value as keyof AdminQuotationItemDisplayItem

  result.sort((left, right) => compareValues(left[currentSortKey], right[currentSortKey], sortDirection.value))

  return result.map((item, index) => ({
    ...item,
    ln: index + 1,
  }))
})

const selectedItemId = computed(() => selectedItemIds.value[0] ?? null)

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    rows.value = await getAdminQuotationItems({
      lookup: lookup.value.trim(),
      take: 500,
    })
  } catch {
    errorMessage.value = t('admin.quotationItem.messages.loadFailed')
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

function onRowClick(_event: Event, payload: { item: AdminQuotationItemDisplayItem }) {
  if (checkboxMode.value) {
    return
  }

  selectedItemIds.value = [payload.item.itemId]
  openPopup(payload.item.itemId)
}

function onMobileCardClick(item: AdminQuotationItemDisplayItem) {
  if (checkboxMode.value) {
    selectedItemIds.value = [item.itemId]
    return
  }

  selectedItemIds.value = [item.itemId]
  openPopup(item.itemId)
}

function handleMobileSelect(item: Record<string, unknown>, selected: boolean) {
  const itemId = String(item.itemId ?? '')
  if (!itemId) return

  if (selected) {
    selectedItemIds.value = [...new Set([...selectedItemIds.value, itemId])]
    return
  }

  selectedItemIds.value = selectedItemIds.value.filter((id) => id !== itemId)
}

function openPopup(itemId = selectedItemId.value) {
  if (!itemId) {
    errorMessage.value = t('admin.quotationItem.messages.selectRecordFirst')
    return
  }

  const row = rows.value.find((item) => item.itemId === itemId)
  if (!row) {
    errorMessage.value = t('admin.quotationItem.messages.selectRecordFirst')
    return
  }

  editingItem.value = { ...row }
  dialogOpen.value = true
  errorMessage.value = ''
}

function openNewItem() {
  editingItem.value = null
  dialogOpen.value = true
  errorMessage.value = ''
}

async function handleSaved(item: AdminQuotationItemListItem) {
  await load()
  const refreshed = rows.value.find((row) => row.itemId === item.itemId) ?? item
  editingItem.value = { ...refreshed }
  selectedItemIds.value = [item.itemId]
  successMessage.value = t('admin.quotationItem.messages.saveSuccess')
  saveSuccess.value = true
}

async function handleDeleted(id: string) {
  await load()
  selectedItemIds.value = selectedItemIds.value.filter((itemId) => itemId !== id)
  successMessage.value = t('admin.quotationItem.messages.deleteSuccess')
  saveSuccess.value = true
}

function showUnavailable(actionKey: string) {
  errorMessage.value = t('admin.quotationItem.messages.actionUnavailable', { action: t(actionKey) })
}

function getLocalizedName(english: string, traditional: string, simplified: string) {
  switch (locale.value) {
    case 'zh-Hant':
      return traditional || english || simplified
    case 'zh-Hans':
      return simplified || english || traditional
    default:
      return english || traditional || simplified
  }
}

function getUnitCostTypeLabel(unitCostType: number) {
  switch (unitCostType) {
    case 1:
      return t('admin.quotationItem.costTypes.numberOfPages')
    case 2:
      return t('admin.quotationItem.costTypes.numberOfSheets')
    case 3:
      return t('admin.quotationItem.costTypes.area')
    case 4:
      return t('admin.quotationItem.costTypes.color1')
    case 5:
      return t('admin.quotationItem.costTypes.color2')
    case 6:
      return t('admin.quotationItem.costTypes.quantity')
    default:
      return t('admin.quotationItem.costTypes.none')
  }
}

function compareValues(left: unknown, right: unknown, direction: SortDirection) {
  const multiplier = direction === 'asc' ? 1 : -1

  if (typeof left === 'number' && typeof right === 'number') {
    return (left - right) * multiplier
  }

  const leftDate = asTimestamp(left)
  const rightDate = asTimestamp(right)
  if (leftDate !== null && rightDate !== null) {
    return (leftDate - rightDate) * multiplier
  }

  return String(left ?? '').localeCompare(String(right ?? ''), activeLocale.value) * multiplier
}

function asTimestamp(value: unknown) {
  if (typeof value !== 'string' || value.length === 0) {
    return null
  }

  const parsed = Date.parse(value)
  return Number.isNaN(parsed) ? null : parsed
}

function formatDateTime(value: string) {
  if (!value) {
    return '-'
  }

  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) {
    return value
  }

  return new Intl.DateTimeFormat(activeLocale.value, {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(parsed)
}

function formatUnitCost(value: number) {
  return formatNumber(value, {
    minimumFractionDigits: 4,
    maximumFractionDigits: 4,
  })
}
</script>

<style scoped>
.quotation-item-page {
  min-height: 0;
  --quotation-item-header-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%);
  --quotation-item-header-fg: rgb(var(--v-theme-on-surface-variant));
}

.quotation-item-card {
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

.quotation-item-table-shell {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 250px);
  min-height: 400px;
  overflow-x: auto;
}

.quotation-item-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.quotation-item-table :deep(.v-table__wrapper > table > thead > tr > th),
.quotation-item-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--quotation-item-header-bg) !important;
  color: var(--quotation-item-header-fg) !important;
}

.quotation-item-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.quotation-item-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.quotation-item-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.quotation-item-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}
</style>