<template>
  <section class="page-section workflow-page" :class="{ 'workflow-page--dark': isDark }">
    <v-card rounded="xl" elevation="0" class="panel-card workflow-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3 pb-2">
        <div>
          <h3 class="text-h6 mb-1">{{ t('admin.workflow.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('admin.workflow.subtitle') }}</p>
        </div>
      </v-card-title>

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('admin.workflow.lookup')"
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
                {{ t('admin.workflow.actions.columns') }}
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
                {{ t('admin.workflow.actions.sorting') }}
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
                :label="t('admin.workflow.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('admin.workflow.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('admin.workflow.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
            {{ t('admin.workflow.actions.checkbox') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-eye-outline" @click="showUnavailable('admin.workflow.actions.views')">
            {{ t('admin.workflow.actions.views') }}
          </v-btn>

          <v-divider vertical class="mx-1" />

          <v-btn variant="outlined" size="small" prepend-icon="mdi-plus" color="primary" @click="openNew">
            {{ t('admin.workflow.actions.newWorkflow') }}
          </v-btn>

          <v-btn variant="outlined" size="small" prepend-icon="mdi-open-in-new" :disabled="!selectedWorkflowId" @click="openEdit">
            {{ t('admin.workflow.actions.popup') }}
          </v-btn>

          <span class="text-caption text-medium-emphasis" v-if="checkboxMode">
            {{ t('admin.workflow.actions.selected', { count: selectedWorkflowIds.length }) }}
          </span>
        </div>

        <v-data-table
          :headers="headers"
          :items="displayedRows"
          :loading="loading"
          item-value="workflowId"
          v-model="selectedWorkflowIds"
          :show-select="checkboxMode"
          density="compact"
          fixed-header
          height="62vh"
          class="workflow-table"
          @click:row="onRowClick"
          @dblclick="openEdit"
        >
          <template #[`item.icon`]>
            <v-icon size="14" color="secondary">mdi-cog</v-icon>
          </template>
        </v-data-table>

        <div class="text-caption text-medium-emphasis mt-2">
          {{ t('admin.workflow.rows', { count: displayedRows.length }) }}
        </div>
      </v-card-text>
    </v-card>

    <v-dialog v-model="dialogOpen" max-width="760" scrollable>
      <AdminWorkflowRecordDialog
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
import { useTheme } from 'vuetify'
import { getAdminWorkflows } from '@/services/admin'
import type { AdminWorkflowListItem } from '@/types/api'
import AdminWorkflowRecordDialog from '@/components/forms/AdminWorkflowRecordDialog.vue'

type WorkflowDisplayItem = AdminWorkflowListItem & {
  icon: string
  ln: number
}

const rows = ref<AdminWorkflowListItem[]>([])
const loading = ref(false)
const errorMessage = ref('')
const lookup = ref('')
const checkboxMode = ref(false)
const selectedWorkflowIds = ref<string[]>([])
const sortDirection = ref<'asc' | 'desc'>('asc')
const sortKey = ref('workflowName')
const visibleColumnKeys = ref<string[]>(['icon', 'workflowName', 'ln', 'workTitle', 'workInstruction'])
const dialogOpen = ref(false)
const editingItem = ref<AdminWorkflowListItem | null>(null)
const saveSuccess = ref(false)
const successMessage = ref('')

const { t } = useI18n({ useScope: 'global' })
const theme = useTheme()
const isDark = computed(() => theme.global.current.value.dark)

const allHeaders = computed(() => [
  { title: '', key: 'icon', width: '32px', sortable: false },
  { title: t('admin.workflow.headers.workName'), key: 'workflowName', minWidth: '180px' },
  { title: '#', key: 'ln', width: '54px' },
  { title: t('admin.workflow.headers.workTitle'), key: 'workTitle', minWidth: '280px' },
  { title: t('admin.workflow.headers.workInstruction'), key: 'workInstruction', minWidth: '320px' },
])

const headers = computed(() => allHeaders.value.filter((header) => visibleColumnKeys.value.includes(String(header.key))))

const sortableColumns = computed(() =>
  allHeaders.value
    .filter((header) => header.sortable !== false)
    .map((header) => ({ key: String(header.key), title: String(header.title || header.key) })),
)

const columnOptions = computed(() => allHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title || header.key) })))

const displayedRows = computed<WorkflowDisplayItem[]>(() => {
  const key = sortKey.value as keyof AdminWorkflowListItem
  const result = [...rows.value]

  result.sort((lhs, rhs) => {
    const left = String(lhs[key] ?? '')
    const right = String(rhs[key] ?? '')
    return sortDirection.value === 'asc' ? left.localeCompare(right) : right.localeCompare(left)
  })

  return result.map((item, index) => ({
    ...item,
    icon: 'mdi-cog',
    ln: index + 1,
  }))
})

const selectedWorkflowId = computed(() => selectedWorkflowIds.value[0] ?? null)

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    rows.value = await getAdminWorkflows({
      lookup: lookup.value.trim(),
      shortcut: 'All',
      take: 500,
    })
  } catch {
    errorMessage.value = t('admin.workflow.messages.loadFailed')
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

function onRowClick(_event: Event, payload: { item: AdminWorkflowListItem }) {
  if (checkboxMode.value) return
  selectedWorkflowIds.value = [payload.item.workflowId]
  editingItem.value = payload.item
  dialogOpen.value = true
}

function openNew() {
  editingItem.value = null
  dialogOpen.value = true
}

function openEdit() {
  if (!selectedWorkflowId.value) {
    errorMessage.value = t('admin.workflow.messages.selectRecordFirst')
    return
  }

  editingItem.value = rows.value.find((item) => item.workflowId === selectedWorkflowId.value) ?? null
  dialogOpen.value = true
}

function handleSaved(item: AdminWorkflowListItem) {
  const idx = rows.value.findIndex((x) => x.workflowId === item.workflowId)
  if (idx >= 0) {
    rows.value[idx] = item
  } else {
    rows.value = [item, ...rows.value]
  }

  selectedWorkflowIds.value = [item.workflowId]
  successMessage.value = t('admin.workflow.form.save')
  saveSuccess.value = true
}

function handleDeleted(id: string) {
  rows.value = rows.value.filter((x) => x.workflowId !== id)
  selectedWorkflowIds.value = selectedWorkflowIds.value.filter((x) => x !== id)
  dialogOpen.value = false
}

function showUnavailable(actionKey: string) {
  errorMessage.value = t('admin.workflow.messages.actionUnavailable', { action: t(actionKey) })
}
</script>

<style scoped>
.workflow-page {
  min-height: 0;
  --workflow-header-bg: rgba(195, 216, 248, 0.92);
  --workflow-header-fg: inherit;
}

.workflow-page--dark {
  --workflow-header-bg: rgba(52, 74, 104, 0.95);
  --workflow-header-fg: rgba(239, 246, 255, 0.98);
}

.workflow-card {
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

.workflow-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.workflow-table :deep(.v-table__wrapper > table > thead > tr > th),
.workflow-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--workflow-header-bg) !important;
  color: var(--workflow-header-fg) !important;
}

.workflow-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.workflow-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.workflow-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.workflow-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}
</style>
