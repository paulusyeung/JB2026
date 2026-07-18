<template>
  <section class="page-section people-page">
    <v-card rounded="xl" elevation="0" class="panel-card people-card">

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('crm.people.lookup')"
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
                {{ t('crm.people.actions.columns') }}
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
                {{ t('crm.people.actions.sorting') }}
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
                :label="t('crm.people.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('crm.people.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('crm.people.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <template v-if="!isPhoneLayout">
            <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
              {{ t('crm.people.actions.checkbox') }}
            </v-btn>

            <v-menu location="bottom">
              <template #activator="{ props }">
                <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                  {{ t('crm.people.actions.views') }}
                </v-btn>
              </template>
              <v-list density="compact" class="toolbar-menu-list">
                <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                  <v-list-item-title>{{ t('crm.people.actions.detailView') }}</v-list-item-title>
                </v-list-item>
                <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                  <v-list-item-title>{{ t('crm.people.actions.cardView') }}</v-list-item-title>
                </v-list-item>
              </v-list>
            </v-menu>
          </template>

          <v-menu v-else location="bottom end">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                {{ t('crm.people.actions.views') }}
              </v-btn>
            </template>

            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
                <v-list-item-title>{{ t('crm.people.actions.checkbox') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>{{ t('crm.people.actions.detailView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>{{ t('crm.people.actions.cardView') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <v-divider vertical class="mx-1" />

          <v-btn variant="outlined" size="small" color="primary" prepend-icon="mdi-plus-circle-outline" @click="openNewPerson">
            {{ t('crm.people.actions.newPerson') }}
          </v-btn>

          <span v-if="checkboxMode" class="text-caption text-medium-emphasis">
            {{ t('crm.people.actions.selected', { count: selectedIds.length }) }}
          </span>
        </div>

        <ListMobileCard
          v-if="isPhoneLayout"
          :items="displayedRows"
          :columns="mobileColumns"
          item-key="id"
          :checkbox-mode="checkboxMode"
          :selected-ids="selectedIds"
          synced-key="syncedToCrm"
          :on-select="handleMobileSelect"
          :on-card-click="(item) => onMobileCardClick(item)"
        />

        <div v-else-if="isCardView" class="people-card-list">
          <v-card
            v-for="row in displayedRows"
            :key="row.id"
            rounded="lg"
            elevation="0"
            class="people-card"
          >
            <v-checkbox-btn
              v-if="checkboxMode"
              :model-value="selectedIds.includes(row.id)"
              density="compact"
              hide-details
              class="people-card__checkbox"
              @click="handleCardCheckbox(row.id)"
            />
            <v-icon
              v-if="row.syncedToCrm"
              class="people-card__synced"
              size="16"
              color="success"
              :title="t('crm.people.messages.syncedTooltip')"
            >mdi-link-variant</v-icon>
            <div class="people-card__header">
              <div class="d-flex align-center ga-2">
                <v-icon size="18" color="primary">mdi-account-outline</v-icon>
                <div>
                  <div class="d-flex align-center ga-1">
                    <a class="text-subtitle-2 font-weight-bold text-primary text-decoration-none cursor-pointer" @click.stop="openPopup(row.id)">{{ row.name }}</a>
                  </div>
                  <div v-if="row.companies.length" class="d-flex flex-wrap ga-1 mt-1">
                    <v-chip
                      v-for="(company, idx) in row.companies"
                      :key="idx"
                      size="small"
                      label
                    >{{ company }}</v-chip>
                  </div>
                  <div v-else class="text-caption text-medium-emphasis">-</div>
                </div>
              </div>
            </div>
            <div class="people-card__body">
              <span class="text-caption">{{ row.jobTitle || '-' }}</span>
              <div v-if="row.emails.length" class="d-flex flex-wrap ga-1">
                <v-chip
                  v-for="(email, idx) in row.emails"
                  :key="idx"
                  size="small"
                  label
                  prepend-icon="mdi-email-outline"
                >{{ email }}</v-chip>
              </div>
              <div v-else class="text-caption text-medium-emphasis">-</div>
              <div v-if="row.phones.length" class="d-flex flex-wrap ga-1">
                <v-chip
                  v-for="(phone, idx) in row.phones"
                  :key="idx"
                  size="small"
                  label
                  prepend-icon="mdi-phone-outline"
                >{{ formatPhoneDisplay(phone) }}</v-chip>
              </div>
            </div>
            <div class="people-card__footer text-caption text-medium-emphasis">
              <span>{{ t('crm.people.headers.updatedBy') }}: {{ row.updatedBy || '-' }}</span>
              <span>{{ t('crm.people.headers.updatedOn') }}: {{ format(row.updatedOn) }}</span>
            </div>
          </v-card>
        </div>

        <v-data-table
          v-else
          :headers="headers"
          :items="displayedRows"
          :loading="loading"
          item-value="id"
          v-model="selectedIds"
          :show-select="checkboxMode"
          density="compact"
          fixed-header
          height="62vh"
          class="people-table"
        >
          <template #[`header.synced`]>
            <v-icon
              size="18"
              :title="t('crm.people.messages.syncedTooltip')"
            >mdi-link-variant</v-icon>
          </template>

          <template #[`item.synced`]='{ item }'>
            <v-icon
              v-if="item.syncedToCrm"
              size="18"
              color="success"
              :title="t('crm.people.messages.syncedTooltip')"
            >mdi-link-variant</v-icon>
          </template>

          <template #[`item.name`]='{ item }'>
            <a class="text-body-2 text-primary text-decoration-none cursor-pointer" @click.stop="openPopup(item.id)">{{ item.name }}</a>
          </template>

          <template #[`item.emails`]='{ item }'>
            <template v-if="item.emails && item.emails.length">
              <v-chip
                v-for="(email, idx) in item.emails"
                :key="idx"
                size="small"
                label
                class="ma-1"
              >{{ email }}</v-chip>
            </template>
            <span v-else class="text-medium-emphasis">-</span>
          </template>

          <template #[`item.phones`]='{ item }'>
            <template v-if="item.phones && item.phones.length">
              <v-chip
                v-for="(phone, idx) in item.phones"
                :key="idx"
                size="small"
                label
                class="ma-1"
              >{{ formatPhoneDisplay(phone) }}</v-chip>
            </template>
            <span v-else class="text-medium-emphasis">-</span>
          </template>

          <template #[`item.companies`]='{ item }'>
            <template v-if="item.companies && item.companies.length">
              <v-chip
                v-for="(company, idx) in item.companies"
                :key="idx"
                size="small"
                label
                color="primary"
                variant="tonal"
                class="ma-1"
              >{{ company }}</v-chip>
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
      <CrmPeopleRecordDialog
        :person-id="editingPersonId"
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
import CrmPeopleRecordDialog from '@/components/crm/CrmPeopleRecordDialog.vue'
import { useResponsiveList } from '@/composables/useResponsiveList'
import { getCrmPeople } from '@/services/crm'
import type { CrmPerson } from '@/types/api'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { formatPhoneDisplay } from '@/utils/phoneParser'

type PeopleDisplayItem = CrmPerson & {
  ln: number
}

const rows = ref<CrmPerson[]>([])
const loading = ref(false)
const lookup = ref('')
const errorMessage = ref('')
const viewSettings = useViewSettings('crm-people', {
  visibleColumns: ['synced', 'name', 'emails', 'phones', 'jobTitle', 'companies', 'createdOn', 'createdBy', 'updatedOn', 'updatedBy'],
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

const { t } = useI18n({ useScope: 'global' })
const { isPhoneLayout, isColumnVisible } = useResponsiveList()
const { format } = useGlobalDateFormatter()

const allHeaders = computed(() => [
  { title: t('crm.people.headers.synced'), key: 'synced', minWidth: '44px', width: '44px', sortable: false },
  { title: t('crm.people.headers.name'), key: 'name', minWidth: '180px' },
  { title: t('crm.people.headers.email'), key: 'emails', minWidth: '200px' },
  { title: t('crm.people.headers.phone'), key: 'phones', minWidth: '140px' },
  { title: t('crm.people.headers.jobTitle'), key: 'jobTitle', minWidth: '180px' },
  { title: t('crm.people.headers.company'), key: 'companies', minWidth: '180px' },
  { title: t('crm.people.headers.createdOn'), key: 'createdOn', minWidth: '135px' },
  { title: t('crm.people.headers.createdBy'), key: 'createdBy', minWidth: '120px' },
  { title: t('crm.people.headers.updatedOn'), key: 'updatedOn', minWidth: '135px' },
  { title: t('crm.people.headers.updatedBy'), key: 'updatedBy', minWidth: '120px' },
])

const headers = computed(() =>
  allHeaders.value.filter((h) =>
    (h.key === 'synced' || visibleColumnKeys.value.includes(String(h.key))) &&
    isColumnVisible(String(h.key), {
      hideOnPhone: ['phones', 'jobTitle', 'companies', 'createdOn', 'createdBy', 'updatedOn', 'updatedBy'],
      hideOnTablet: [],
    }),
  ),
)

const mobileColumns = computed<ListMobileCardColumn<PeopleDisplayItem>[]>(() => [
  { key: 'name', label: t('crm.people.headers.name'), section: 'header', emphasis: true },
  { key: 'companies', label: t('crm.people.headers.company'), section: 'header' },
  { key: 'emails', label: t('crm.people.headers.email'), section: 'body' },
  { key: 'phones', label: t('crm.people.headers.phone'), section: 'body' },
  { key: 'jobTitle', label: t('crm.people.headers.jobTitle'), section: 'body' },
  { key: 'createdBy', label: t('crm.people.headers.createdBy'), section: 'footer' },
  {
    key: 'updatedOn',
    label: t('crm.people.headers.updatedOn'),
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

const displayedRows = computed<PeopleDisplayItem[]>(() => {
  const key = sortKey.value as keyof CrmPerson
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

const isCardView = computed(() => viewMode.value === 'card')

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    rows.value = await getCrmPeople(lookup.value.trim())
  } catch {
    errorMessage.value = t('crm.people.messages.loadFailed')
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

function onMobileCardClick(item: PeopleDisplayItem) {
  if (checkboxMode.value) {
    handleMobileSelect(item, !selectedIds.value.includes(item.id))
    return
  }

  selectedIds.value = [item.id]
}

function handleMobileSelect(item: PeopleDisplayItem | Record<string, unknown>, selected: boolean) {
  const id = String(item.id ?? '')
  if (!id) return

  if (selected) {
    selectedIds.value = [...new Set([...selectedIds.value, id])]
    return
  }

  selectedIds.value = selectedIds.value.filter((pid) => pid !== id)
}

function setViewMode(mode: 'detail' | 'card') {
  viewMode.value = mode
}

function handleCardCheckbox(id: string) {
  if (selectedIds.value.includes(id)) {
    selectedIds.value = selectedIds.value.filter((pid) => pid !== id)
    return
  }
  selectedIds.value = [...selectedIds.value, id]
}

const dialogOpen = ref(false)
const editingPersonId = ref<string | null>(null)
const saveSuccess = ref(false)
const successMessage = ref('')

function openPopup(id: string) {
  editingPersonId.value = id
  dialogOpen.value = true
  errorMessage.value = ''
}

function openNewPerson() {
  editingPersonId.value = null
  dialogOpen.value = true
  errorMessage.value = ''
}

async function handleSaved(person: CrmPerson) {
  await load()
  selectedIds.value = [person.id]
  editingPersonId.value = person.id
  successMessage.value = t('crm.people.messages.saveSuccess')
  saveSuccess.value = true
}
</script>

<style scoped>
.people-page {
  min-height: 0;
  --people-header-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%);
  --people-header-fg: rgb(var(--v-theme-on-surface-variant));
}

.people-card {
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

.people-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.people-table :deep(.v-table__wrapper > table > thead > tr > th),
.people-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--people-header-bg) !important;
  color: var(--people-header-fg) !important;
}

.people-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.people-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.people-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.people-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}

.people-card-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .people-card-list {
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    align-items: start;
  }
}

.people-card {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgba(255, 255, 255, 0.72);
}

.people-card__checkbox {
  grid-column: 2;
  grid-row: 1;
  align-self: start;
  justify-self: end;
}

.people-card__synced {
  grid-column: 2;
  grid-row: 1;
  align-self: start;
  justify-self: end;
  margin-top: 2.25rem;
}

.people-card__header {
  grid-column: 1;
  grid-row: 1;
}
</style>
