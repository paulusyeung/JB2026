<template>
  <section class="page-section opportunities-page">
    <v-card rounded="xl" elevation="0" class="panel-card opportunities-card">

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('crm.opportunities.lookup')"
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
                {{ t('crm.opportunities.actions.columns') }}
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
                {{ t('crm.opportunities.actions.sorting') }}
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
                :label="t('crm.opportunities.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('crm.opportunities.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('crm.opportunities.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <template v-if="!isPhoneLayout">
            <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
              {{ t('crm.opportunities.actions.checkbox') }}
            </v-btn>

            <v-menu location="bottom">
              <template #activator="{ props }">
                <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                  {{ t('crm.opportunities.actions.views') }}
                </v-btn>
              </template>
              <v-list density="compact" class="toolbar-menu-list">
                <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                  <v-list-item-title>{{ t('crm.opportunities.actions.detailView') }}</v-list-item-title>
                </v-list-item>
                <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                  <v-list-item-title>{{ t('crm.opportunities.actions.cardView') }}</v-list-item-title>
                </v-list-item>
              </v-list>
            </v-menu>
          </template>

          <v-menu v-else location="bottom end">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                {{ t('crm.opportunities.actions.views') }}
              </v-btn>
            </template>

            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
                <v-list-item-title>{{ t('crm.opportunities.actions.checkbox') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>{{ t('crm.opportunities.actions.detailView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>{{ t('crm.opportunities.actions.cardView') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <v-divider vertical class="mx-1" />

          <v-btn variant="outlined" size="small" color="primary" prepend-icon="mdi-plus-circle-outline" @click="openNewOpportunity">
            {{ t('crm.opportunities.actions.newOpportunity') }}
          </v-btn>

          <span v-if="checkboxMode" class="text-caption text-medium-emphasis">
            {{ t('crm.opportunities.actions.selected', { count: selectedIds.length }) }}
          </span>
        </div>

        <ListMobileCard
          v-if="isPhoneLayout"
          :items="displayedRows"
          :columns="mobileColumns"
          item-key="id"
          :checkbox-mode="checkboxMode"
          :selected-ids="selectedIds"
          :on-select="handleMobileSelect"
          :on-card-click="(item) => onMobileCardClick(item)"
        />

        <div v-else-if="isCardView" class="opportunity-card-list">
          <v-card
            v-for="row in displayedRows"
            :key="row.id"
            rounded="lg"
            elevation="0"
            class="opportunity-card"
            @click="handleCardClick(row)"
          >
            <v-checkbox-btn
              v-if="checkboxMode"
              :model-value="selectedIds.includes(row.id)"
              density="compact"
              hide-details
              class="opportunity-card__checkbox"
              @click.stop="handleCardCheckbox(row.id)"
            />
            <div class="opportunity-card__header">
              <div class="d-flex align-center ga-2">
                <v-icon size="18" color="primary">mdi-trending-up</v-icon>
                <div>
                  <a class="text-subtitle-2 font-weight-bold text-primary text-decoration-none cursor-pointer" @click.stop="openPopup(row.id)">{{ row.name }}</a>
                  <v-chip v-if="row.stage" size="x-small" label color="primary" variant="tonal" class="ml-1">
                    {{ stageLabel(row.stage) }}
                  </v-chip>
                  <div v-if="row.company" class="text-caption text-medium-emphasis">{{ row.company }}</div>
                </div>
              </div>
            </div>
            <div class="opportunity-card__body">
              <span class="text-caption text-right">
                {{ t('crm.opportunities.headers.amount') }}: {{ row.amount || '-' }}
              </span>
              <span class="text-caption">
                {{ t('crm.opportunities.headers.owner') }}: {{ row.owner || '-' }}
              </span>
            </div>
            <div class="opportunity-card__footer text-caption text-medium-emphasis">
              <span>{{ t('crm.opportunities.headers.updatedBy') }}: {{ row.updatedBy || '-' }}</span>
              <span>{{ t('crm.opportunities.headers.updatedOn') }}: {{ format(row.updatedOn) }}</span>
            </div>
          </v-card>
        </div>

        <div v-else class="opportunities-table-shell">
        <v-data-table
          :headers="headers"
          :items="displayedRows"
          :loading="loading"
          item-value="id"
          v-model="selectedIds"
          :show-select="checkboxMode"
          density="compact"
          fixed-header
          height="100%"
          class="opportunities-table"
        >
          <template #[`item.name`]='{ item }'>
            <a class="text-body-2 text-primary text-decoration-none cursor-pointer" @click.stop="openPopup(item.id)">{{ item.name }}</a>
          </template>

          <template #[`item.stage`]='{ item }'>
            <v-chip v-if="item.stage" size="x-small" label color="primary" variant="tonal">{{ stageLabel(item.stage) }}</v-chip>
            <span v-else class="text-medium-emphasis">-</span>
          </template>

          <template #[`item.closeDate`]='{ item }'>
            <template v-if="item.closeDate">{{ format(item.closeDate) }}</template>
            <span v-else class="text-medium-emphasis">-</span>
          </template>

          <template #[`item.amount`]='{ item }'>
            <span class="text-right" style="display:block">{{ item.amount || '-' }}</span>
          </template>

          <template #[`item.company`]='{ item }'>
            <v-chip v-if="item.company" size="small" label color="primary" variant="tonal">{{ item.company }}</v-chip>
            <span v-else class="text-medium-emphasis">-</span>
          </template>

          <template #[`item.pointOfContact`]='{ item }'>
            <v-chip v-if="item.pointOfContact" size="small" label>{{ item.pointOfContact }}</v-chip>
            <span v-else class="text-medium-emphasis">-</span>
          </template>

          <template #[`item.owner`]='{ item }'>
            {{ item.owner || '-' }}
          </template>

          <template #[`item.createdOn`]='{ item }'>{{ format(item.createdOn) }}</template>
          <template #[`item.updatedOn`]='{ item }'>{{ format(item.updatedOn) }}</template>
        </v-data-table>
        </div>

      </v-card-text>
    </v-card>

    <v-snackbar v-model="saveSuccess" color="success" timeout="3000">
      {{ successMessage }}
      <template #actions>
        <v-btn variant="text" @click="saveSuccess = false">{{ t('common.cancel') }}</v-btn>
      </template>
    </v-snackbar>

    <v-dialog v-model="dialogOpen" max-width="min(100%, 760px)" scrollable>
      <CrmOpportunityRecordDialog
        :opportunity-id="editingOpportunityId"
        @saved="handleSaved"
        @cancel="dialogOpen = false"
      />
    </v-dialog>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useViewSettings } from '@/composables/useColumnPersistence'
import ListMobileCard, { type ListMobileCardColumn } from '@/components/grids/ListMobileCard.vue'
import CrmOpportunityRecordDialog from '@/components/crm/CrmOpportunityRecordDialog.vue'
import { useResponsiveList } from '@/composables/useResponsiveList'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { getCrmOpportunities, getCrmOpportunityStageOptions } from '@/services/crm'
import type { CrmOpportunity } from '@/types/api'

type OpportunitiesViewMode = 'detail' | 'card'

type OpportunitiesDisplayItem = CrmOpportunity & {
  ln: number
}

const stageLabelMap = ref<Record<string, string>>({})

getCrmOpportunityStageOptions().then(opts => {
  stageLabelMap.value = Object.fromEntries(opts.map(o => [o.value, o.label]))
}).catch(() => {})

function stageLabel(value: string): string {
  return stageLabelMap.value[value] || value
}

const rows = ref<CrmOpportunity[]>([])
const loading = ref(false)
const lookup = ref('')
const errorMessage = ref('')
const viewSettings = useViewSettings('crm-opportunities', {
  visibleColumns: ['name', 'stage', 'closeDate', 'amount', 'company', 'pointOfContact', 'owner', 'createdOn', 'createdBy', 'updatedOn', 'updatedBy'],
  sortKey: 'name',
  sortDirection: 'asc',
  checkboxMode: false,
  viewMode: 'detail',
})
const visibleColumnKeys = viewSettings.visibleColumns
const sortKey = viewSettings.sortKey
const sortDirection = viewSettings.sortDirection
const checkboxMode = viewSettings.checkboxMode
const viewMode = viewSettings.viewMode
const selectedIds = ref<string[]>([])
const dialogOpen = ref(false)
const editingOpportunityId = ref<string | null>(null)
const saveSuccess = ref(false)
const successMessage = ref('')

const { t } = useI18n({ useScope: 'global' })
const { isPhoneLayout, isColumnVisible } = useResponsiveList()
const { format } = useGlobalDateFormatter()

const isCardView = computed(() => viewMode.value === 'card')

const allHeaders = computed(() => [
  { title: t('crm.opportunities.headers.name'), key: 'name', minWidth: '180px' },
  { title: t('crm.opportunities.headers.stage'), key: 'stage', minWidth: '100px' },
  { title: t('crm.opportunities.headers.closeDate'), key: 'closeDate', minWidth: '135px' },
  { title: t('crm.opportunities.headers.amount'), key: 'amount', minWidth: '120px' },
  { title: t('crm.opportunities.headers.company'), key: 'company', minWidth: '160px' },
  { title: t('crm.opportunities.headers.pointOfContact'), key: 'pointOfContact', minWidth: '160px' },
  { title: t('crm.opportunities.headers.owner'), key: 'owner', minWidth: '140px' },
  { title: t('crm.opportunities.headers.createdOn'), key: 'createdOn', minWidth: '135px' },
  { title: t('crm.opportunities.headers.createdBy'), key: 'createdBy', minWidth: '120px' },
  { title: t('crm.opportunities.headers.updatedOn'), key: 'updatedOn', minWidth: '135px' },
  { title: t('crm.opportunities.headers.updatedBy'), key: 'updatedBy', minWidth: '120px' },
])

const headers = computed(() =>
  allHeaders.value.filter((h) =>
    visibleColumnKeys.value.includes(String(h.key)) &&
    isColumnVisible(String(h.key), {
      hideOnPhone: ['closeDate', 'amount', 'company', 'pointOfContact', 'owner', 'createdOn', 'createdBy', 'updatedOn', 'updatedBy'],
      hideOnTablet: [],
    }),
  ),
)

const mobileColumns = computed<ListMobileCardColumn<OpportunitiesDisplayItem>[]>(() => [
  { key: 'name', label: t('crm.opportunities.headers.name'), section: 'header', emphasis: true },
  { key: 'stage', label: t('crm.opportunities.headers.stage'), section: 'header' },
  { key: 'company', label: t('crm.opportunities.headers.company'), section: 'body' },
  { key: 'owner', label: t('crm.opportunities.headers.owner'), section: 'body' },
  { key: 'createdBy', label: t('crm.opportunities.headers.createdBy'), section: 'footer' },
  {
    key: 'updatedOn',
    label: t('crm.opportunities.headers.updatedOn'),
    section: 'footer',
    formatter: (item) => format(item.updatedOn),
  },
])

const sortableColumns = computed(() =>
  allHeaders.value.map((h) => ({ key: String(h.key), title: String(h.title || h.key) })),
)

const columnOptions = computed(() => allHeaders.value.map((h) => ({ key: String(h.key), title: String(h.title || h.key) })))

const displayedRows = computed<OpportunitiesDisplayItem[]>(() => {
  const key = sortKey.value as keyof CrmOpportunity
  const result = [...rows.value]

  result.sort((lhs, rhs) => {
    const left = String(lhs[key] ?? '')
    const right = String(rhs[key] ?? '')
    return sortDirection.value === 'asc' ? left.localeCompare(right) : right.localeCompare(left)
  })

  return result.map((item, index) => ({
    ...item,
    ln: index + 1,
  }))
})

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    rows.value = await getCrmOpportunities(lookup.value.trim())
  } catch {
    errorMessage.value = t('crm.opportunities.messages.loadFailed')
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

function onMobileCardClick(item: OpportunitiesDisplayItem) {
  if (checkboxMode.value) {
    handleMobileSelect(item, !selectedIds.value.includes(item.id))
    return
  }

  openPopup(item.id)
}

function handleMobileSelect(item: OpportunitiesDisplayItem | Record<string, unknown>, selected: boolean) {
  const id = String(item.id ?? '')
  if (!id) return

  if (selected) {
    selectedIds.value = [...new Set([...selectedIds.value, id])]
    return
  }

  selectedIds.value = selectedIds.value.filter((pid) => pid !== id)
}

function setViewMode(mode: OpportunitiesViewMode) {
  viewMode.value = mode
}

function handleCardClick(row: OpportunitiesDisplayItem) {
  if (checkboxMode.value) {
    handleCardCheckbox(row.id)
    return
  }
  openPopup(row.id)
}

function handleCardCheckbox(id: string) {
  if (selectedIds.value.includes(id)) {
    selectedIds.value = selectedIds.value.filter((pid) => pid !== id)
    return
  }
  selectedIds.value = [...selectedIds.value, id]
}

function openPopup(id: string) {
  editingOpportunityId.value = id
  dialogOpen.value = true
  errorMessage.value = ''
}

function openNewOpportunity() {
  editingOpportunityId.value = null
  dialogOpen.value = true
  errorMessage.value = ''
}

async function handleSaved(opportunity: CrmOpportunity) {
  await load()
  selectedIds.value = [opportunity.id]
  editingOpportunityId.value = opportunity.id
  successMessage.value = t('crm.opportunities.messages.saveSuccess')
  saveSuccess.value = true
}
</script>

<style scoped>
.opportunities-page {
  min-height: 0;
  --opportunities-header-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%);
  --opportunities-header-fg: rgb(var(--v-theme-on-surface-variant));
}

.opportunities-card {
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

.opportunities-table-shell {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 250px);
  min-height: 400px;
  overflow-x: auto;
}

.opportunities-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.opportunities-table :deep(.v-table__wrapper > table > thead > tr > th),
.opportunities-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--opportunities-header-bg) !important;
  color: var(--opportunities-header-fg) !important;
}

.opportunities-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.opportunities-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.opportunities-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.opportunities-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}

.opportunity-card-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .opportunity-card-list {
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    align-items: start;
  }
}

.opportunity-card {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgb(var(--v-theme-surface));
}

.opportunity-card__checkbox {
  grid-column: 2;
  grid-row: 1;
  align-self: start;
  justify-self: end;
}

.opportunity-card__header {
  grid-column: 1;
  grid-row: 1;
}

.opportunity-card__body,
.opportunity-card__footer {
  grid-column: 1 / -1;
}

.opportunity-card__header,
.opportunity-card__footer {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
}

.opportunity-card__body {
  display: grid;
  gap: 0.45rem;
}
</style>
