<template>
  <section class="page-section companies-page">
    <v-card rounded="xl" elevation="0" class="panel-card companies-card">

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('crm.companies.lookup')"
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
                {{ t('crm.companies.actions.columns') }}
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
                {{ t('crm.companies.actions.sorting') }}
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
                :label="t('crm.companies.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('crm.companies.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('crm.companies.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <template v-if="!isPhoneLayout">
            <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
              {{ t('crm.companies.actions.checkbox') }}
            </v-btn>

            <v-menu location="bottom">
              <template #activator="{ props }">
                <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                  {{ t('crm.companies.actions.views') }}
                </v-btn>
              </template>
              <v-list density="compact" class="toolbar-menu-list">
                <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                  <v-list-item-title>{{ t('crm.companies.actions.detailView') }}</v-list-item-title>
                </v-list-item>
                <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                  <v-list-item-title>{{ t('crm.companies.actions.cardView') }}</v-list-item-title>
                </v-list-item>
              </v-list>
            </v-menu>
          </template>

          <v-menu v-else location="bottom end">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                {{ t('crm.companies.actions.views') }}
              </v-btn>
            </template>

            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
                <v-list-item-title>{{ t('crm.companies.actions.checkbox') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>{{ t('crm.companies.actions.detailView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>{{ t('crm.companies.actions.cardView') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <v-divider vertical class="mx-1" />

          <v-btn variant="outlined" size="small" color="primary" prepend-icon="mdi-plus-circle-outline" @click="openNewCompany">
            {{ t('crm.companies.actions.newCompany') }}
          </v-btn>

          <span v-if="checkboxMode" class="text-caption text-medium-emphasis">
            {{ t('crm.companies.actions.selected', { count: selectedCompanyIds.length }) }}
          </span>
        </div>

        <ListMobileCard
          v-if="isPhoneLayout"
          :items="displayedRows"
          :columns="mobileColumns"
          item-key="id"
          :checkbox-mode="checkboxMode"
          :selected-ids="selectedCompanyIds"
          :on-select="handleMobileSelect"
          :on-card-click="(item) => onMobileCardClick(item)"
        />

        <div v-else-if="isCardView" class="company-card-list">
          <v-card
            v-for="row in displayedRows"
            :key="row.id"
            rounded="lg"
            elevation="0"
            class="company-card"
            @click="openPopup(row.id)"
          >
            <v-checkbox-btn
              v-if="checkboxMode"
              :model-value="selectedCompanyIds.includes(row.id)"
              density="compact"
              hide-details
              class="company-card__checkbox"
              @click.stop="handleCardCheckbox(row.id)"
            />
            <div class="company-card__header">
              <div class="d-flex align-center ga-2">
                <v-icon size="18" color="primary">mdi-domain</v-icon>
                <div>
                  <div class="text-subtitle-2 font-weight-bold">{{ row.name }}</div>
                  <div v-if="row.domainName" class="text-caption text-medium-emphasis">{{ row.domainName }}</div>
                </div>
              </div>
            </div>
            <div class="company-card__body">
              <span class="text-caption">{{ t('crm.companies.headers.accountOwner') }}: {{ row.accountOwner || '-' }}</span>
            </div>
            <div class="company-card__footer text-caption text-medium-emphasis">
              <span>{{ t('crm.companies.headers.updatedBy') }}: {{ row.updatedBy || '-' }}</span>
              <span>{{ t('crm.companies.headers.updatedOn') }}: {{ format(row.updatedOn) }}</span>
            </div>
          </v-card>
        </div>

        <v-data-table
          v-else
          :headers="headers"
          :items="displayedRows"
          :loading="loading"
          item-value="id"
          v-model="selectedCompanyIds"
          :show-select="checkboxMode"
          density="compact"
          fixed-header
          height="62vh"
          class="companies-table"
        >
          <template #[`item.name`]='{ item }'>
            <a class="text-body-2 text-primary text-left text-decoration-none cursor-pointer" @click.stop="openPopup(item.id)">{{ item.name }}</a>
          </template>

          <template #[`item.people`]='{ item }'>
            <template v-if="item.people && item.people.length">
              <v-chip
                v-for="(person, idx) in item.people"
                :key="idx"
                size="small"
                label
                class="ma-1"
              >{{ person.name }}</v-chip>
            </template>
            <span v-else class="text-medium-emphasis">-</span>
          </template>

          <template #[`item.opportunities`]='{ item }'>
            <template v-if="item.opportunities && item.opportunities.length">
              <v-chip
                v-for="(opp, idx) in item.opportunities"
                :key="idx"
                size="small"
                label
                color="primary"
                variant="tonal"
                class="ma-1"
              >{{ opp.name }}</v-chip>
            </template>
            <span v-else class="text-medium-emphasis">-</span>
          </template>

          <template #[`item.createdOn`]='{ item }'>{{ format(item.createdOn) }}</template>
          <template #[`item.updatedOn`]='{ item }'>{{ format(item.updatedOn) }}</template>
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
      <CrmCompanyRecordDialog
        :company-id="editingCompanyId"
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
import CrmCompanyRecordDialog from '@/components/crm/CrmCompanyRecordDialog.vue'
import { useResponsiveList } from '@/composables/useResponsiveList'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { getCrmCompanies } from '@/services/crm'
import type { CrmCompany } from '@/types/api'

type CompaniesViewMode = 'detail' | 'card'

type CompaniesDisplayItem = CrmCompany & {
  ln: number
}

const rows = ref<CrmCompany[]>([])
const loading = ref(false)
const lookup = ref('')
const errorMessage = ref('')
const viewSettings = useViewSettings('crm-companies', {
  visibleColumns: ['name', 'accountOwner', 'domainName', 'address', 'people', 'opportunities', 'createdOn', 'createdBy', 'updatedOn', 'updatedBy'],
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
const selectedCompanyIds = ref<string[]>([])
const dialogOpen = ref(false)
const editingCompanyId = ref<string | null>(null)
const saveSuccess = ref(false)
const successMessage = ref('')

const { t } = useI18n({ useScope: 'global' })
const { isPhoneLayout, isColumnVisible } = useResponsiveList()
const { format } = useGlobalDateFormatter()

const isCardView = computed(() => viewMode.value === 'card')

const allHeaders = computed(() => [
  { title: t('crm.companies.headers.name'), key: 'name', minWidth: '180px' },
  { title: t('crm.companies.headers.accountOwner'), key: 'accountOwner', minWidth: '140px' },
  { title: t('crm.companies.headers.domainName'), key: 'domainName', minWidth: '160px' },
  { title: t('crm.companies.headers.address'), key: 'formattedAddress', minWidth: '200px' },
  { title: t('crm.companies.headers.people'), key: 'people', minWidth: '140px' },
  { title: t('crm.companies.headers.opportunities'), key: 'opportunities', minWidth: '160px' },
  { title: t('crm.companies.headers.createdOn'), key: 'createdOn', minWidth: '135px' },
  { title: t('crm.companies.headers.createdBy'), key: 'createdBy', minWidth: '100px' },
  { title: t('crm.companies.headers.updatedOn'), key: 'updatedOn', minWidth: '135px' },
  { title: t('crm.companies.headers.updatedBy'), key: 'updatedBy', minWidth: '100px' },
])

const headers = computed(() =>
  allHeaders.value.filter((h) =>
    visibleColumnKeys.value.includes(String(h.key)) &&
    isColumnVisible(String(h.key), {
      hideOnPhone: ['address', 'createdOn', 'createdBy', 'updatedOn', 'updatedBy'],
      hideOnTablet: [],
    }),
  ),
)

const mobileColumns = computed<ListMobileCardColumn<CompaniesDisplayItem>[]>(() => [
  { key: 'name', label: t('crm.companies.headers.name'), section: 'header', emphasis: true },
  { key: 'domainName', label: t('crm.companies.headers.domainName'), section: 'header' },
  { key: 'accountOwner', label: t('crm.companies.headers.accountOwner'), section: 'body' },
  { key: 'createdBy', label: t('crm.companies.headers.createdBy'), section: 'footer' },
  {
    key: 'updatedOn',
    label: t('crm.companies.headers.updatedOn'),
    section: 'footer',
    formatter: (item) => format(item.updatedOn),
  },
])

const sortableColumns = computed(() =>
  allHeaders.value
    .filter((h) => h.sortable !== false)
    .map((h) => ({ key: String(h.key), title: String(h.title || h.key) })),
)

const columnOptions = computed(() => allHeaders.value.map((h) => ({ key: String(h.key), title: String(h.title || h.key) })))

const displayedRows = computed<CompaniesDisplayItem[]>(() => {
  const key = sortKey.value as keyof CrmCompany
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
    rows.value = await getCrmCompanies({
      lookup: lookup.value.trim(),
    })
  } catch {
    errorMessage.value = t('crm.companies.messages.loadFailed')
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

function onMobileCardClick(item: CompaniesDisplayItem) {
  if (checkboxMode.value) {
    selectedCompanyIds.value = [item.id]
    return
  }

  selectedCompanyIds.value = [item.id]
}

function handleMobileSelect(item: Record<string, unknown>, selected: boolean) {
  const id = String(item.id ?? '')
  if (!id) return

  if (selected) {
    selectedCompanyIds.value = [...new Set([...selectedCompanyIds.value, id])]
    return
  }

  selectedCompanyIds.value = selectedCompanyIds.value.filter((cid) => cid !== id)
}

function openPopup(id: string) {
  editingCompanyId.value = id
  dialogOpen.value = true
  errorMessage.value = ''
}

function openNewCompany() {
  editingCompanyId.value = null
  dialogOpen.value = true
  errorMessage.value = ''
}

async function handleSaved(company: CrmCompany) {
  await load()
  selectedCompanyIds.value = [company.id]
  editingCompanyId.value = company.id
  successMessage.value = t('crm.companies.messages.saveSuccess')
  saveSuccess.value = true
}

function setViewMode(mode: CompaniesViewMode) {
  viewMode.value = mode
}

function handleCardCheckbox(id: string) {
  if (selectedCompanyIds.value.includes(id)) {
    selectedCompanyIds.value = selectedCompanyIds.value.filter((cid) => cid !== id)
    return
  }
  selectedCompanyIds.value = [...selectedCompanyIds.value, id]
}

</script>

<style scoped>
.companies-page {
  min-height: 0;
  --companies-header-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%);
  --companies-header-fg: rgb(var(--v-theme-on-surface-variant));
}

.companies-card {
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

.companies-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.companies-table :deep(.v-table__wrapper > table > thead > tr > th),
.companies-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--companies-header-bg) !important;
  color: var(--companies-header-fg) !important;
}

.companies-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.companies-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.companies-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.companies-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}

.company-card-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .company-card-list {
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    align-items: start;
  }
}

.company-card {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgba(255, 255, 255, 0.72);
  cursor: pointer;
}

.company-card:active {
  background: rgba(255, 255, 255, 0.92);
}

.company-card__checkbox {
  grid-column: 2;
  grid-row: 1;
  align-self: start;
  justify-self: end;
}

.company-card__header {
  grid-column: 1;
  grid-row: 1;
}

.company-card__body,
.company-card__footer {
  grid-column: 1 / -1;
}

.company-card__header,
.company-card__footer {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
}

.company-card__body {
  display: grid;
  gap: 0.45rem;
}
</style>
