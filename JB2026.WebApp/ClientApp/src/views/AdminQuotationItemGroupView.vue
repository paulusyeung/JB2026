<template>
  <section class="page-section quotation-item-group-page" :class="{ 'quotation-item-group-page--dark': isDark }">
    <v-card rounded="xl" elevation="0" class="panel-card quotation-item-group-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3 pb-2">
        <div>
          <h3 class="text-h6 mb-1">{{ t('admin.quotationItemGroup.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('admin.quotationItemGroup.subtitle') }}</p>
        </div>
      </v-card-title>

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('admin.quotationItemGroup.lookup')"
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
                {{ t('admin.quotationItemGroup.actions.columns') }}
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
                {{ t('admin.quotationItemGroup.actions.sorting') }}
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
                :label="t('admin.quotationItemGroup.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('admin.quotationItemGroup.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('admin.quotationItemGroup.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
            {{ t('admin.quotationItemGroup.actions.checkbox') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-eye-outline" @click="showUnavailable('admin.quotationItemGroup.actions.views')">
            {{ t('admin.quotationItemGroup.actions.views') }}
          </v-btn>

          <v-divider vertical class="mx-1" />

          <v-btn variant="outlined" size="small" prepend-icon="mdi-refresh" :loading="loading" @click="refreshList">
            {{ t('admin.quotationItemGroup.actions.refresh') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-tune" disabled>
            {{ t('admin.quotationItemGroup.actions.preference') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-open-in-new" :disabled="!selectedItemGroupId" @click="openPopup">
            {{ t('admin.quotationItemGroup.actions.popup') }}
          </v-btn>

          <v-btn color="primary" size="small" prepend-icon="mdi-plus" @click="openNewItemGroup">
            {{ t('admin.quotationItemGroup.actions.newItemGroup') }}
          </v-btn>

          <span v-if="checkboxMode" class="text-caption text-medium-emphasis">
            {{ t('admin.quotationItemGroup.actions.selected', { count: selectedItemGroupIds.length }) }}
          </span>
        </div>

        <v-data-table
          v-model="selectedItemGroupIds"
          :headers="headers"
          :items="displayedRows"
          :loading="loading"
          item-value="itemGroupId"
          :show-select="checkboxMode"
          density="compact"
          fixed-header
          height="62vh"
          class="quotation-item-group-table"
          @click:row="onRowClick"
        >
          <template #[`item.icon`]>
            <v-icon size="14" color="secondary">mdi-shape-plus-outline</v-icon>
          </template>

          <template #[`item.createdOn`]="{ item }">{{ formatDateTime(item.createdOn) }}</template>
          <template #[`item.modifiedOn`]="{ item }">{{ formatDateTime(item.modifiedOn) }}</template>
        </v-data-table>

        <div class="text-caption text-medium-emphasis mt-2">
          {{ t('admin.quotationItemGroup.rows', { count: displayedRows.length }) }}
        </div>
      </v-card-text>
    </v-card>

    <v-dialog v-model="dialogOpen" max-width="720" scrollable>
      <AdminQuotationItemGroupRecordDialog
        :item-group="editingItemGroup"
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
import { useTheme } from 'vuetify'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import AdminQuotationItemGroupRecordDialog from '@/components/forms/AdminQuotationItemGroupRecordDialog.vue'
import { getAdminQuotationItemGroups } from '@/services/admin'
import type { AdminQuotationItemGroupListItem } from '@/types/api'

type SortDirection = 'asc' | 'desc'

type AdminQuotationItemGroupDisplayItem = AdminQuotationItemGroupListItem & {
  icon: string
  ln: number
  originalOrder: number
}

const rows = ref<AdminQuotationItemGroupListItem[]>([])
const loading = ref(false)
const lookup = ref('')
const errorMessage = ref('')
const checkboxMode = ref(false)
const selectedItemGroupIds = ref<string[]>([])
const dialogOpen = ref(false)
const editingItemGroup = ref<AdminQuotationItemGroupListItem | null>(null)
const saveSuccess = ref(false)
const successMessage = ref('')
const sortDirection = ref<SortDirection>('asc')
const sortKey = ref('originalOrder')
const visibleColumnKeys = ref<string[]>([
  'icon',
  'zone',
  'ln',
  'groupNameEn',
  'groupNameCht',
  'groupNameChs',
  'createdOn',
])

const { t } = useI18n({ useScope: 'global' })
const { activeLocale } = useLocaleFormatters()
const theme = useTheme()
const isDark = computed(() => theme.global.current.value.dark)

const allHeaders = computed(() => [
  { title: '', key: 'icon', width: '32px', sortable: false },
  { title: t('admin.quotationItemGroup.headers.zone'), key: 'zone', minWidth: '120px' },
  { title: '#', key: 'ln', width: '54px', sortable: false },
  { title: t('admin.quotationItemGroup.headers.groupNameEn'), key: 'groupNameEn', minWidth: '180px' },
  { title: t('admin.quotationItemGroup.headers.groupNameCht'), key: 'groupNameCht', minWidth: '180px' },
  { title: t('admin.quotationItemGroup.headers.groupNameChs'), key: 'groupNameChs', minWidth: '180px' },
  { title: t('admin.quotationItemGroup.headers.createdOn'), key: 'createdOn', minWidth: '170px' },
  { title: t('admin.quotationItemGroup.headers.createdBy'), key: 'createdBy', minWidth: '100px' },
  { title: t('admin.quotationItemGroup.headers.modifiedOn'), key: 'modifiedOn', minWidth: '170px' },
  { title: t('admin.quotationItemGroup.headers.modifiedBy'), key: 'modifiedBy', minWidth: '100px' },
])

const headers = computed(() => allHeaders.value.filter((header) => visibleColumnKeys.value.includes(String(header.key))))

const columnOptions = computed(() => allHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title || header.key) })))

const sortOptions = computed(() => [
  { key: 'originalOrder', title: t('admin.quotationItemGroup.actions.legacyOrder') },
  ...allHeaders.value
    .filter((header) => header.sortable !== false)
    .map((header) => ({ key: String(header.key), title: String(header.title || header.key) })),
])

const displayedRows = computed<AdminQuotationItemGroupDisplayItem[]>(() => {
  const result = rows.value.map((item, index) => ({
    ...item,
    icon: 'mdi-shape-plus-outline',
    ln: index + 1,
    originalOrder: index + 1,
  }))
  const currentSortKey = sortKey.value as keyof AdminQuotationItemGroupDisplayItem

  result.sort((left, right) => compareValues(left[currentSortKey], right[currentSortKey], sortDirection.value))

  return result.map((item, index) => ({
    ...item,
    ln: index + 1,
  }))
})

const selectedItemGroupId = computed(() => selectedItemGroupIds.value[0] ?? null)

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    rows.value = await getAdminQuotationItemGroups({
      lookup: lookup.value.trim(),
      take: 500,
    })
  } catch {
    errorMessage.value = t('admin.quotationItemGroup.messages.loadFailed')
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

function onRowClick(_event: Event, payload: { item: AdminQuotationItemGroupDisplayItem }) {
  if (checkboxMode.value) {
    return
  }

  selectedItemGroupIds.value = [payload.item.itemGroupId]
  openPopup(payload.item.itemGroupId)
}

function openPopup(itemGroupId = selectedItemGroupId.value) {
  if (!itemGroupId) {
    errorMessage.value = t('admin.quotationItemGroup.messages.selectRecordFirst')
    return
  }

  const row = rows.value.find((item) => item.itemGroupId === itemGroupId)
  if (!row) {
    errorMessage.value = t('admin.quotationItemGroup.messages.selectRecordFirst')
    return
  }

  editingItemGroup.value = { ...row }
  dialogOpen.value = true
}

function openNewItemGroup() {
  editingItemGroup.value = null
  dialogOpen.value = true
  errorMessage.value = ''
}

async function handleSaved(item: AdminQuotationItemGroupListItem) {
  await load()
  selectedItemGroupIds.value = [item.itemGroupId]
  successMessage.value = t('admin.quotationItemGroup.messages.saveSuccess')
  saveSuccess.value = true
}

async function handleDeleted(id: string) {
  await load()
  selectedItemGroupIds.value = selectedItemGroupIds.value.filter((itemId) => itemId !== id)
  successMessage.value = t('admin.quotationItemGroup.messages.deleteSuccess')
  saveSuccess.value = true
}

function showUnavailable(actionKey: string) {
  errorMessage.value = t('admin.quotationItemGroup.messages.actionUnavailable', { action: t(actionKey) })
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
</script>

<style scoped>
.quotation-item-group-page {
  min-height: 0;
  --quotation-item-group-header-bg: rgba(195, 216, 248, 0.92);
  --quotation-item-group-header-fg: inherit;
}

.quotation-item-group-page--dark {
  --quotation-item-group-header-bg: rgba(52, 74, 104, 0.95);
  --quotation-item-group-header-fg: rgba(239, 246, 255, 0.98);
}

.quotation-item-group-card {
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

.quotation-item-group-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.quotation-item-group-table :deep(.v-table__wrapper > table > thead > tr > th),
.quotation-item-group-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--quotation-item-group-header-bg) !important;
  color: var(--quotation-item-group-header-fg) !important;
}

.quotation-item-group-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.quotation-item-group-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.quotation-item-group-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.quotation-item-group-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}
</style>