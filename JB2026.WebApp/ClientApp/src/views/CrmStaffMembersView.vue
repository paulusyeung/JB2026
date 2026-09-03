<template>
  <section class="page-section staff-members-page">
    <v-card rounded="xl" elevation="0" class="panel-card staff-members-card">

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('admin.user.lookup')"
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
                {{ t('admin.user.actions.columns') }}
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
                {{ t('admin.user.actions.sorting') }}
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
                :label="t('admin.user.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('admin.user.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('admin.user.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <template v-if="!isPhoneLayout">
            <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
              {{ t('admin.user.actions.checkbox') }}
            </v-btn>

            <v-menu location="bottom">
              <template #activator="{ props }">
                <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                  {{ t('admin.user.actions.views') }}
                </v-btn>
              </template>
              <v-list density="compact" class="toolbar-menu-list">
                <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                  <v-list-item-title>{{ t('admin.user.actions.detailView') }}</v-list-item-title>
                </v-list-item>
                <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                  <v-list-item-title>{{ t('admin.user.actions.cardView') }}</v-list-item-title>
                </v-list-item>
              </v-list>
            </v-menu>

            <v-divider vertical class="mx-1" />

            <v-btn variant="outlined" size="small" color="primary" :disabled="!canSyncToCrm" prepend-icon="mdi-cloud-sync" @click="syncToCrm">
              {{ t('admin.user.actions.syncCrm') }}
            </v-btn>
          </template>

          <v-menu v-else location="bottom end">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                {{ t('admin.user.actions.views') }}
              </v-btn>
            </template>

            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
                <v-list-item-title>{{ t('admin.user.actions.checkbox') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>{{ t('admin.user.actions.detailView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>{{ t('admin.user.actions.cardView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-cloud-sync" :disabled="!canSyncToCrm" @click="syncToCrm">
                <v-list-item-title>{{ t('admin.user.actions.syncCrm') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <span v-if="checkboxMode" class="text-caption text-medium-emphasis">
            {{ t('admin.user.actions.selected', { count: selectedUserIds.length }) }}
          </span>
        </div>

        <ListMobileCard
          v-if="isPhoneLayout"
          :items="displayedRows"
          :columns="mobileColumns"
          item-key="userId"
          :checkbox-mode="checkboxMode"
          :selected-ids="selectedUserIds"
          :on-select="handleMobileSelect"
          :on-card-click="(item) => onMobileCardClick(item)"
        />

        <div v-else-if="isCardView" class="user-card-list">
          <v-card
            v-for="row in displayedRows"
            :key="row.userId"
            rounded="lg"
            elevation="0"
            class="user-card"
            @click="openPopup(row.userId)"
          >
            <v-checkbox-btn
              v-if="checkboxMode"
              :model-value="selectedUserIds.includes(row.userId)"
              density="compact"
              hide-details
              class="user-card__checkbox"
              @click.stop="handleCardCheckbox(row.userId)"
            />
            <div class="user-card__header">
              <div class="d-flex align-center ga-2">
                <v-icon size="18" :color="row.crmSynced ? 'pink' : row.primaryRec ? 'warning' : 'secondary'">
                  {{ row.crmSynced ? 'mdi-account-sync' : row.primaryRec ? 'mdi-account-key' : 'mdi-account' }}
                </v-icon>
                <div>
                  <div class="text-subtitle-2 font-weight-bold">{{ row.userAlias }}</div>
                  <div class="text-caption text-medium-emphasis">{{ row.username }}</div>
                </div>
              </div>
            </div>
            <div class="user-card__body">
              <span class="text-caption">{{ t('admin.user.headers.userRole') }}: {{ row.role || '-' }}</span>
            </div>
            <div class="user-card__footer text-caption text-medium-emphasis">
              <span>{{ t('admin.user.headers.modifiedBy') }}: {{ row.modifiedBy || '-' }}</span>
              <span>{{ t('admin.user.headers.modifiedOn') }}: {{ formatDateCell(row.modifiedOn) }}</span>
            </div>
          </v-card>
        </div>

        <div v-else class="staff-members-table-shell">
        <v-data-table
          :headers="headers"
          :items="displayedRows"
          :loading="loading"
          item-value="userId"
          v-model="selectedUserIds"
          :show-select="checkboxMode"
          density="compact"
          fixed-header
          height="100%"
          class="staff-members-table"
          v-model:items-per-page="itemsPerPage"
          :items-per-page-options="[10, 15, 20, 25, 50, -1]"
          @click:row="onDataRowClick"
        >
          <template #[`item.icon`]='{ item }'>
            <v-icon size="14" :color="item.crmSynced ? 'pink' : item.primaryRec ? 'warning' : 'secondary'">
              {{ item.crmSynced ? 'mdi-account-sync' : item.primaryRec ? 'mdi-account-key' : 'mdi-account' }}
            </v-icon>
          </template>

          <template #[`item.username`]='{ item }'>
            <v-btn variant="text" color="primary" density="compact" class="px-0 text-none" style="min-width: auto" @click.stop="openPopup(item.userId)">
              {{ item.username }}
            </v-btn>
          </template>

          <template #[`item.createdOn`]='{ item }'>{{ formatDateCell(item.createdOn) }}</template>
          <template #[`item.modifiedOn`]='{ item }'>{{ formatDateCell(item.modifiedOn) }}</template>
        </v-data-table>
        </div>

      </v-card-text>
    </v-card>

    <v-dialog v-model="dialogOpen" max-width="min(100%, 760px)" scrollable>
      <StaffMemberRecordDialog
        :user-id="editingUserId"
        @saved="handleSaved"
        @deleted="handleDeleted"
        @cancel="dialogOpen = false"
      />
    </v-dialog>

    <v-dialog v-model="crmDialogOpen" max-width="520px">
      <SyncCrmDialog
        :user-id="crmSyncingUserId"
        :user-email="crmSyncingUserEmail"
        @cancel="crmDialogOpen = false"
        @done="crmDialogOpen = false; load()"
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
import { useViewSettings } from '@/composables/useColumnPersistence'
import ListMobileCard, { type ListMobileCardColumn } from '@/components/grids/ListMobileCard.vue'
import { useResponsiveList } from '@/composables/useResponsiveList'
import StaffMemberRecordDialog from '@/components/crm/StaffMemberRecordDialog.vue'
import SyncCrmDialog from '@/components/crm/SyncCrmDialog.vue'
import { getAdminUsers } from '@/services/admin'
import type { AdminUser, AdminUserRecord } from '@/types/api'

type StaffMembersViewMode = 'detail' | 'card'

type StaffMembersDisplayItem = AdminUser & {
  icon: string
  ln: number
}

const rows = ref<AdminUser[]>([])
const loading = ref(false)
const lookup = ref('')
const errorMessage = ref('')
const viewSettings = useViewSettings('staff-members', {
  visibleColumns: ['icon', 'username', 'ln', 'userAlias', 'email', 'role', 'createdOn', 'createdBy', 'modifiedOn', 'modifiedBy'],
  sortKey: 'userAlias',
  sortDirection: 'asc',
  checkboxMode: false,
  viewMode: 'detail',
  itemsPerPage: 10,
})
const visibleColumnKeys = viewSettings.visibleColumns
const sortKey = viewSettings.sortKey
const sortDirection = viewSettings.sortDirection
const checkboxMode = viewSettings.checkboxMode
const viewMode = viewSettings.viewMode
const itemsPerPage = viewSettings.itemsPerPage
const selectedUserIds = ref<string[]>([])
const dialogOpen = ref(false)
const editingUserId = ref<string | null>(null)
const saveSuccess = ref(false)
const successMessage = ref('')
const crmDialogOpen = ref(false)
const crmSyncingUserId = ref('')
const crmSyncingUserEmail = ref('')

const { t } = useI18n({ useScope: 'global' })
const { isPhoneLayout, isColumnVisible } = useResponsiveList()

const isCardView = computed(() => viewMode.value === 'card')

const allHeaders = computed(() => [
  { title: '', key: 'icon', width: '32px', sortable: false },
  { title: t('admin.user.headers.username'), key: 'username', minWidth: '140px' },
  { title: '#', key: 'ln', width: '54px', sortable: false },
  { title: t('admin.user.headers.userAlias'), key: 'userAlias', minWidth: '160px' },
  { title: t('admin.user.headers.email'), key: 'email', minWidth: '200px' },
  { title: t('admin.user.headers.userRole'), key: 'role', minWidth: '110px' },
  { title: t('admin.user.headers.createdOn'), key: 'createdOn', minWidth: '135px' },
  { title: t('admin.user.headers.createdBy'), key: 'createdBy', minWidth: '100px' },
  { title: t('admin.user.headers.modifiedOn'), key: 'modifiedOn', minWidth: '135px' },
  { title: t('admin.user.headers.modifiedBy'), key: 'modifiedBy', minWidth: '100px' },
])

const headers = computed(() =>
  allHeaders.value.filter((h) =>
    visibleColumnKeys.value.includes(String(h.key)) &&
    isColumnVisible(String(h.key), {
      hideOnPhone: ['email', 'createdOn', 'createdBy', 'modifiedOn', 'modifiedBy'],
      hideOnTablet: [],
    }),
  ),
)

const mobileColumns = computed<ListMobileCardColumn<StaffMembersDisplayItem>[]>(() => [
  { key: 'userAlias', label: t('admin.user.headers.userAlias'), section: 'header', emphasis: true },
  { key: 'username', label: t('admin.user.headers.username'), section: 'header' },
  { key: 'role', label: t('admin.user.headers.userRole'), section: 'body' },
  { key: 'createdBy', label: t('admin.user.headers.createdBy'), section: 'footer' },
  {
    key: 'modifiedOn',
    label: t('admin.user.headers.modifiedOn'),
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

const displayedRows = computed<StaffMembersDisplayItem[]>(() => {
  const key = sortKey.value as keyof AdminUser
  const result = [...rows.value]

  result.sort((lhs, rhs) => {
    const left = String(lhs[key] ?? '')
    const right = String(rhs[key] ?? '')
    return sortDirection.value === 'asc' ? left.localeCompare(right) : right.localeCompare(left)
  })

  return result.map((item, index) => ({
    ...item,
    icon: item.crmSynced ? 'mdi-account-sync' : item.primaryRec ? 'mdi-account-key' : 'mdi-account',
    ln: index + 1,
  }))
})

const selectedUserId = computed(() => selectedUserIds.value[0] ?? null)

const selectedRecord = computed(() =>
  rows.value.find((r) => r.userId === selectedUserId.value) ?? null,
)

const canSyncToCrm = computed(() => {
  if (selectedUserIds.value.length !== 1) return false
  const rec = selectedRecord.value
  return rec !== null && rec.email.trim().length > 0
})

function syncToCrm() {
  if (!canSyncToCrm.value) return
  const rec = selectedRecord.value
  if (!rec) return
  crmSyncingUserId.value = rec.userId
  crmSyncingUserEmail.value = rec.email
  crmDialogOpen.value = true
}

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    rows.value = await getAdminUsers({
      lookup: lookup.value.trim(),
      take: 500,
      excludeGuest: true,
    })
  } catch {
    errorMessage.value = t('admin.user.messages.loadFailed')
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

function onMobileCardClick(item: StaffMembersDisplayItem) {
  if (checkboxMode.value) {
    selectedUserIds.value = [item.userId]
    return
  }

  selectedUserIds.value = [item.userId]
  openPopup(item.userId)
}

function handleMobileSelect(item: Record<string, unknown>, selected: boolean) {
  const userId = String(item.userId ?? '')
  if (!userId) return

  if (selected) {
    selectedUserIds.value = [...new Set([...selectedUserIds.value, userId])]
    return
  }

  selectedUserIds.value = selectedUserIds.value.filter((id) => id !== userId)
}

function openPopup(userId = selectedUserId.value ?? editingUserId.value) {
  if (!userId) {
    errorMessage.value = t('admin.user.messages.selectRecordFirst')
    return
  }

  editingUserId.value = userId
  dialogOpen.value = true
  errorMessage.value = ''
}

function openNewUser() {
  editingUserId.value = null
  dialogOpen.value = true
  errorMessage.value = ''
}

function setViewMode(mode: StaffMembersViewMode) {
  viewMode.value = mode
}

function onDataRowClick(_event: MouseEvent, row: { item: StaffMembersDisplayItem }) {
  const userId = row.item.userId
  if (selectedUserIds.value.includes(userId)) {
    selectedUserIds.value = selectedUserIds.value.filter((id) => id !== userId)
    return
  }
  selectedUserIds.value = [...selectedUserIds.value, userId]
}

function handleCardCheckbox(userId: string) {
  if (selectedUserIds.value.includes(userId)) {
    selectedUserIds.value = selectedUserIds.value.filter((id) => id !== userId)
    return
  }
  selectedUserIds.value = [...selectedUserIds.value, userId]
}

async function handleSaved(user: AdminUserRecord) {
  await load()
  selectedUserIds.value = [user.userId]
  editingUserId.value = user.userId
  successMessage.value = t('admin.user.messages.saveSuccess')
  saveSuccess.value = true
}

async function handleDeleted(id: string) {
  await load()
  selectedUserIds.value = selectedUserIds.value.filter((userId) => userId !== id)
  successMessage.value = t('admin.user.messages.deleteSuccess')
  saveSuccess.value = true
}

function formatDateCell(value: string): string {
  if (!value) return '-'
  const normalized = value.replace('T', ' ')
  return normalized.length >= 16 ? normalized.slice(0, 16) : normalized
}
</script>

<style scoped>
.staff-members-page {
  min-height: 0;
  --admin-user-header-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%);
  --admin-user-header-fg: rgb(var(--v-theme-on-surface-variant));
}

.staff-members-card {
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

.staff-members-table-shell {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 260px);
  min-height: 400px;
  overflow-x: auto;
}

.staff-members-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.staff-members-table :deep(.v-table__wrapper > table > thead > tr > th),
.staff-members-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--admin-user-header-bg) !important;
  color: var(--admin-user-header-fg) !important;
}

.staff-members-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.staff-members-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.staff-members-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.staff-members-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}

.user-card-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .user-card-list {
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    align-items: start;
  }
}

.user-card {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgb(var(--v-theme-surface));
  cursor: pointer;
}

.user-card:active {
  background: color-mix(in srgb, rgb(var(--v-theme-surface)) 92%, rgb(var(--v-theme-primary)) 8%);
}

.user-card__checkbox {
  grid-column: 2;
  grid-row: 1;
  align-self: start;
  justify-self: end;
}

.user-card__header {
  grid-column: 1;
  grid-row: 1;
}

.user-card__body,
.user-card__footer {
  grid-column: 1 / -1;
}

.user-card__header,
.user-card__footer {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
}

.user-card__body {
  display: grid;
  gap: 0.45rem;
}
</style>
