<template>
  <section class="page-section tasks-page">
    <v-card rounded="xl" elevation="0" class="panel-card tasks-card">

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('crm.tasks.lookup')"
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
                {{ t('crm.tasks.actions.columns') }}
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
                {{ t('crm.tasks.actions.sorting') }}
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
                :label="t('crm.tasks.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('crm.tasks.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('crm.tasks.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <template v-if="!isPhoneLayout">
            <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
              {{ t('crm.tasks.actions.checkbox') }}
            </v-btn>

            <v-menu location="bottom">
              <template #activator="{ props }">
                <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                  {{ t('crm.tasks.actions.views') }}
                </v-btn>
              </template>
              <v-list density="compact" class="toolbar-menu-list">
                <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                  <v-list-item-title>{{ t('crm.tasks.actions.detailView') }}</v-list-item-title>
                </v-list-item>
                <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                  <v-list-item-title>{{ t('crm.tasks.actions.cardView') }}</v-list-item-title>
                </v-list-item>
              </v-list>
            </v-menu>
          </template>

          <v-menu v-else location="bottom end">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                {{ t('crm.tasks.actions.views') }}
              </v-btn>
            </template>

            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
                <v-list-item-title>{{ t('crm.tasks.actions.checkbox') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>{{ t('crm.tasks.actions.detailView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>{{ t('crm.tasks.actions.cardView') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <v-divider vertical class="mx-1" />

          <v-btn variant="outlined" size="small" color="primary" prepend-icon="mdi-plus-circle-outline" @click="openNewTask">
            {{ t('crm.tasks.actions.newTask') }}
          </v-btn>

          <span v-if="checkboxMode" class="text-caption text-medium-emphasis">
            {{ t('crm.tasks.actions.selected', { count: selectedIds.length }) }}
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

        <div v-else-if="isCardView" class="task-card-list">
          <v-card
            v-for="row in displayedRows"
            :key="row.id"
            rounded="lg"
            elevation="0"
            class="task-card"
            @click="handleCardClick(row)"
          >
            <v-checkbox-btn
              v-if="checkboxMode"
              :model-value="selectedIds.includes(row.id)"
              density="compact"
              hide-details
              class="task-card__checkbox"
              @click.stop="handleCardCheckbox(row.id)"
            />
            <div class="task-card__header">
              <div class="d-flex align-center ga-2">
                <v-icon size="18" color="primary">mdi-format-list-checks</v-icon>
                <div>
                  <a class="text-subtitle-2 font-weight-bold text-primary text-decoration-none cursor-pointer" @click.stop="openPopup(row.id)">{{ row.title }}</a>
                  <v-chip v-if="row.status" size="x-small" label :color="statusColor(row.status)" variant="tonal" class="ml-1">
                    {{ statusLabel(row.status) }}
                  </v-chip>
                  <div v-if="row.dueDate" class="text-caption text-medium-emphasis">
                    {{ t('crm.tasks.headers.dueDate') }}: {{ format(row.dueDate) }}
                  </div>
                </div>
              </div>
            </div>
            <div class="task-card__body">
              <span class="text-caption">
                {{ t('crm.tasks.headers.assignee') }}: 
                <v-chip v-if="row.assignee" size="small" label color="primary" variant="tonal">{{ row.assignee }}</v-chip>
                <span v-else>-</span>
              </span>
              <span class="text-caption" v-if="row.relations?.length">
                <v-chip
                  v-for="rel in row.relations"
                  :key="rel.id"
                  size="small"
                  label
                  color="secondary"
                  variant="tonal"
                  class="mr-1"
                >
                  {{ rel.name }}
                </v-chip>
              </span>
            </div>
            <div class="task-card__footer text-caption text-medium-emphasis">
              <span>{{ t('crm.tasks.headers.updatedBy') }}: {{ row.updatedBy || '-' }}</span>
              <span>{{ t('crm.tasks.headers.updatedOn') }}: {{ format(row.updatedOn) }}</span>
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
          class="tasks-table"
        >
          <template #[`item.title`]='{ item }'>
            <a class="text-body-2 text-primary text-decoration-none cursor-pointer" @click.stop="openPopup(item.id)">{{ item.title }}</a>
          </template>

          <template #[`item.status`]='{ item }'>
            <v-chip v-if="item.status" size="x-small" label :color="statusColor(item.status)" variant="tonal">{{ statusLabel(item.status) }}</v-chip>
            <span v-else class="text-medium-emphasis">-</span>
          </template>

          <template #[`item.body`]='{ item }'>
            <span class="text-medium-emphasis text-truncate d-inline-block" style="max-width: 200px">
              {{ item.body ? stripHtml(item.body) : '-' }}
            </span>
          </template>

          <template #[`item.dueDate`]='{ item }'>
            <template v-if="item.dueDate">{{ format(item.dueDate) }}</template>
            <span v-else class="text-medium-emphasis">-</span>
          </template>

          <template #[`item.assignee`]='{ item }'>
            <v-chip v-if="item.assignee" size="small" label color="primary" variant="tonal">{{ item.assignee }}</v-chip>
            <span v-else class="text-medium-emphasis">-</span>
          </template>

          <template #[`item.relations`]='{ item }'>
            <template v-if="item.relations?.length">
              <v-chip
                v-for="rel in item.relations"
                :key="rel.id"
                size="small"
                label
                color="secondary"
                variant="tonal"
                class="mr-1"
              >
                {{ rel.name }}
              </v-chip>
            </template>
            <span v-else class="text-medium-emphasis">-</span>
          </template>

          <template #[`item.createdOn`]='{ item }'>{{ format(item.createdOn) }}</template>
          <template #[`item.createdBy`]='{ item }'>{{ item.createdBy || '-' }}</template>
          <template #[`item.updatedOn`]='{ item }'>{{ format(item.updatedOn) }}</template>
          <template #[`item.updatedBy`]='{ item }'>{{ item.updatedBy || '-' }}</template>
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
      <CrmTaskRecordDialog
        :task-id="editingTaskId"
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
import CrmTaskRecordDialog from '@/components/crm/CrmTaskRecordDialog.vue'
import { useResponsiveList } from '@/composables/useResponsiveList'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { getCrmTasks, getCrmTaskStatusOptions } from '@/services/crm'
import type { CrmTask } from '@/types/api'

type TasksViewMode = 'detail' | 'card'

type TasksDisplayItem = CrmTask & {
  ln: number
}

const rows = ref<CrmTask[]>([])
const loading = ref(false)
const lookup = ref('')
const errorMessage = ref('')
const viewSettings = useViewSettings('crm-tasks', {
  visibleColumns: ['title', 'status', 'body', 'dueDate', 'assignee', 'relations', 'createdOn', 'createdBy', 'updatedOn', 'updatedBy'],
  sortKey: 'title',
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
const editingTaskId = ref<string | null>(null)
const saveSuccess = ref(false)
const successMessage = ref('')

const { t } = useI18n({ useScope: 'global' })
const { isPhoneLayout, isColumnVisible } = useResponsiveList()
const { format } = useGlobalDateFormatter()

const isCardView = computed(() => viewMode.value === 'card')

const statusLabelMap = ref<Record<string, string>>({})

getCrmTaskStatusOptions().then(opts => {
  statusLabelMap.value = Object.fromEntries(opts.map(o => [o.value, o.label]))
}).catch(() => {})

function statusColor(status: string): string {
  switch (status) {
    case 'COMPLETED': return 'green'
    case 'IN_PROGRESS': return 'info'
    default: return 'default'
  }
}

function statusLabel(status: string): string {
  return statusLabelMap.value[status] || status
}

function stripHtml(html: string): string {
  const doc = new DOMParser().parseFromString(html, 'text/html')
  return doc.body.textContent?.trim() || ''
}

const allHeaders = computed(() => [
  { title: t('crm.tasks.headers.title'), key: 'title', minWidth: '220px' },
  { title: t('crm.tasks.headers.status'), key: 'status', minWidth: '100px' },
  { title: t('crm.tasks.headers.body'), key: 'body', minWidth: '200px' },
  { title: t('crm.tasks.headers.dueDate'), key: 'dueDate', minWidth: '135px' },
  { title: t('crm.tasks.headers.assignee'), key: 'assignee', minWidth: '140px' },
  { title: t('crm.tasks.headers.relations'), key: 'relations', minWidth: '180px' },
  { title: t('crm.tasks.headers.createdOn'), key: 'createdOn', minWidth: '135px' },
  { title: t('crm.tasks.headers.createdBy'), key: 'createdBy', minWidth: '120px' },
  { title: t('crm.tasks.headers.updatedOn'), key: 'updatedOn', minWidth: '135px' },
  { title: t('crm.tasks.headers.updatedBy'), key: 'updatedBy', minWidth: '120px' },
])

const headers = computed(() =>
  allHeaders.value.filter((h) =>
    visibleColumnKeys.value.includes(String(h.key)) &&
    isColumnVisible(String(h.key), {
      hideOnPhone: ['body', 'dueDate', 'assignee', 'relations', 'createdOn', 'createdBy', 'updatedOn', 'updatedBy'],
      hideOnTablet: [],
    }),
  ),
)

const mobileColumns = computed<ListMobileCardColumn<TasksDisplayItem>[]>(() => [
  { key: 'title', label: t('crm.tasks.headers.title'), section: 'header', emphasis: true },
  { key: 'status', label: t('crm.tasks.headers.status'), section: 'header' },
  { key: 'assignee', label: t('crm.tasks.headers.assignee'), section: 'body' },
  { key: 'dueDate', label: t('crm.tasks.headers.dueDate'), section: 'body', formatter: (item) => item.dueDate ? format(item.dueDate) : '-' },
  { key: 'createdBy', label: t('crm.tasks.headers.createdBy'), section: 'footer' },
  {
    key: 'updatedOn',
    label: t('crm.tasks.headers.updatedOn'),
    section: 'footer',
    formatter: (item) => format(item.updatedOn),
  },
])

const sortableColumns = computed(() =>
  allHeaders.value.map((h) => ({ key: String(h.key), title: String(h.title || h.key) })),
)

const columnOptions = computed(() => allHeaders.value.map((h) => ({ key: String(h.key), title: String(h.title || h.key) })))

const displayedRows = computed<TasksDisplayItem[]>(() => {
  const key = sortKey.value as keyof CrmTask
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
    rows.value = await getCrmTasks(lookup.value.trim())
  } catch {
    errorMessage.value = t('crm.tasks.messages.loadFailed')
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

function onMobileCardClick(item: TasksDisplayItem) {
  if (checkboxMode.value) {
    handleMobileSelect(item, !selectedIds.value.includes(item.id))
    return
  }

  openPopup(item.id)
}

function handleMobileSelect(item: TasksDisplayItem | Record<string, unknown>, selected: boolean) {
  const id = String(item.id ?? '')
  if (!id) return

  if (selected) {
    selectedIds.value = [...new Set([...selectedIds.value, id])]
    return
  }

  selectedIds.value = selectedIds.value.filter((pid) => pid !== id)
}

function setViewMode(mode: TasksViewMode) {
  viewMode.value = mode
}

function handleCardClick(row: TasksDisplayItem) {
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
  editingTaskId.value = id
  dialogOpen.value = true
  errorMessage.value = ''
}

function openNewTask() {
  editingTaskId.value = null
  dialogOpen.value = true
  errorMessage.value = ''
}

async function handleSaved(task: CrmTask) {
  await load()
  selectedIds.value = [task.id]
  editingTaskId.value = task.id
  successMessage.value = t('crm.tasks.messages.saveSuccess')
  saveSuccess.value = true
}
</script>

<style scoped>
.tasks-page {
  min-height: 0;
  --tasks-header-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%);
  --tasks-header-fg: rgb(var(--v-theme-on-surface-variant));
}

.tasks-card {
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

.tasks-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.tasks-table :deep(.v-table__wrapper > table > thead > tr > th),
.tasks-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--tasks-header-bg) !important;
  color: var(--tasks-header-fg) !important;
}

.tasks-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.tasks-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.tasks-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.tasks-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}

.task-card-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .task-card-list {
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    align-items: start;
  }
}

.task-card {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgb(var(--v-theme-surface));
}

.task-card__checkbox {
  grid-column: 2;
  grid-row: 1;
  align-self: start;
  justify-self: end;
}

.task-card__header {
  grid-column: 1;
  grid-row: 1;
}

.task-card__body,
.task-card__footer {
  grid-column: 1 / -1;
}

.task-card__header,
.task-card__footer {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
}

.task-card__body {
  display: grid;
  gap: 0.45rem;
}
</style>
