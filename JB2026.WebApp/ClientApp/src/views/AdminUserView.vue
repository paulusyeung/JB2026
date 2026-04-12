<template>
  <section class="page-section admin-user-page" :class="{ 'admin-user-page--dark': isDark }">
    <v-card rounded="xl" elevation="0" class="panel-card admin-user-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3 pb-2">
        <div>
          <h3 class="text-h6 mb-1">{{ t('admin.user.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('admin.user.subtitle') }}</p>
        </div>
      </v-card-title>

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

          <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
            {{ t('admin.user.actions.checkbox') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-eye-outline" @click="showUnavailable('admin.user.actions.views')">
            {{ t('admin.user.actions.views') }}
          </v-btn>

          <v-btn variant="outlined" size="small" color="primary" prepend-icon="mdi-account-plus" @click="openNewUser">
            {{ t('admin.user.actions.newUser') }}
          </v-btn>

          <v-divider vertical class="mx-1" />

          <v-btn variant="outlined" size="small" prepend-icon="mdi-refresh" :loading="loading" @click="refreshList">
            {{ t('admin.user.actions.refresh') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-tune" @click="showUnavailable('admin.user.actions.preference')">
            {{ t('admin.user.actions.preference') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-open-in-new" :disabled="!selectedUserId" @click="openPopup">
            {{ t('admin.user.actions.popup') }}
          </v-btn>

          <span v-if="checkboxMode" class="text-caption text-medium-emphasis">
            {{ t('admin.user.actions.selected', { count: selectedUserIds.length }) }}
          </span>
        </div>

        <v-data-table
          :headers="headers"
          :items="displayedRows"
          :loading="loading"
          item-value="userId"
          v-model="selectedUserIds"
          :show-select="checkboxMode"
          density="compact"
          fixed-header
          height="62vh"
          class="admin-user-table"
          @click:row="onRowClick"
          @dblclick="openPopup"
        >
          <template #[`item.icon`]='{ item }'>
            <v-icon size="14" :color="item.primaryRec ? 'warning' : 'secondary'">
              {{ item.primaryRec ? 'mdi-account-key' : 'mdi-account' }}
            </v-icon>
          </template>

          <template #[`item.createdOn`]='{ item }'>{{ formatDateCell(item.createdOn) }}</template>
          <template #[`item.modifiedOn`]='{ item }'>{{ formatDateCell(item.modifiedOn) }}</template>
        </v-data-table>

        <div class="text-caption text-medium-emphasis mt-2">
          {{ t('admin.user.rows', { count: displayedRows.length }) }}
        </div>
      </v-card-text>
    </v-card>

    <v-dialog v-model="dialogOpen" max-width="760" scrollable>
      <AdminUserRecordDialog
        :user-id="editingUserId"
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
import AdminUserRecordDialog from '@/components/forms/AdminUserRecordDialog.vue'
import { getAdminUsers } from '@/services/admin'
import type { AdminUser, AdminUserRecord } from '@/types/api'

type AdminUserDisplayItem = AdminUser & {
  icon: string
  ln: number
}

const rows = ref<AdminUser[]>([])
const loading = ref(false)
const lookup = ref('')
const errorMessage = ref('')
const checkboxMode = ref(false)
const selectedUserIds = ref<string[]>([])
const dialogOpen = ref(false)
const editingUserId = ref<string | null>(null)
const saveSuccess = ref(false)
const successMessage = ref('')
const sortDirection = ref<'asc' | 'desc'>('asc')
const sortKey = ref('userAlias')
const visibleColumnKeys = ref<string[]>([
  'icon',
  'username',
  'ln',
  'userAlias',
  'userPassword',
  'role',
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
  { title: t('admin.user.headers.username'), key: 'username', minWidth: '140px' },
  { title: '#', key: 'ln', width: '54px', sortable: false },
  { title: t('admin.user.headers.userAlias'), key: 'userAlias', minWidth: '160px' },
  { title: t('admin.user.headers.userPassword'), key: 'userPassword', minWidth: '110px' },
  { title: t('admin.user.headers.userRole'), key: 'role', minWidth: '110px' },
  { title: t('admin.user.headers.createdOn'), key: 'createdOn', minWidth: '135px' },
  { title: t('admin.user.headers.createdBy'), key: 'createdBy', minWidth: '100px' },
  { title: t('admin.user.headers.modifiedOn'), key: 'modifiedOn', minWidth: '135px' },
  { title: t('admin.user.headers.modifiedBy'), key: 'modifiedBy', minWidth: '100px' },
])

const headers = computed(() => allHeaders.value.filter((h) => visibleColumnKeys.value.includes(String(h.key))))

const sortableColumns = computed(() =>
  allHeaders.value
    .filter((h) => h.sortable !== false)
    .map((h) => ({ key: String(h.key), title: String(h.title || h.key) })),
)

const columnOptions = computed(() => allHeaders.value.map((h) => ({ key: String(h.key), title: String(h.title || h.key) })))

const displayedRows = computed<AdminUserDisplayItem[]>(() => {
  const key = sortKey.value as keyof AdminUser
  const result = [...rows.value]

  result.sort((lhs, rhs) => {
    const left = String(lhs[key] ?? '')
    const right = String(rhs[key] ?? '')
    return sortDirection.value === 'asc' ? left.localeCompare(right) : right.localeCompare(left)
  })

  return result.map((item, index) => ({
    ...item,
    icon: item.primaryRec ? 'mdi-account-key' : 'mdi-account',
    ln: index + 1,
  }))
})

const selectedUserId = computed(() => selectedUserIds.value[0] ?? null)

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

function onRowClick(_event: Event, payload: { item: AdminUser }) {
  if (checkboxMode.value) return
  selectedUserIds.value = [payload.item.userId]
  openPopup(payload.item.userId)
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

function showUnavailable(actionKey: string) {
  errorMessage.value = t('admin.user.messages.actionUnavailable', { action: t(actionKey) })
}

function formatDateCell(value: string): string {
  if (!value) return '-'
  const normalized = value.replace('T', ' ')
  return normalized.length >= 16 ? normalized.slice(0, 16) : normalized
}
</script>

<style scoped>
.admin-user-page {
  min-height: 0;
  --admin-user-header-bg: rgba(195, 216, 248, 0.92);
  --admin-user-header-fg: inherit;
}

.admin-user-page--dark {
  --admin-user-header-bg: rgba(52, 74, 104, 0.95);
  --admin-user-header-fg: rgba(239, 246, 255, 0.98);
}

.admin-user-card {
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

.admin-user-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.admin-user-table :deep(.v-table__wrapper > table > thead > tr > th),
.admin-user-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--admin-user-header-bg) !important;
  color: var(--admin-user-header-fg) !important;
}

.admin-user-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.admin-user-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.admin-user-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.admin-user-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}
</style>
