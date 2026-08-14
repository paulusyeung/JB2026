<template>
  <section class="page-section workflow-forms-page">
    <v-card rounded="xl" elevation="0" class="panel-card workflow-forms-card">


      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('admin.workflowForms.lookup')"
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
                {{ t('admin.workflowForms.actions.columns') }}
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
                {{ t('admin.workflowForms.actions.sorting') }}
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
                :label="t('admin.workflowForms.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('admin.workflowForms.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('admin.workflowForms.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <template v-if="!isPhoneLayout">
            <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
              {{ t('admin.workflowForms.actions.checkbox') }}
            </v-btn>

            <v-btn variant="outlined" size="small" prepend-icon="mdi-eye-outline" @click="showUnavailable('admin.workflowForms.actions.views')">
              {{ t('admin.workflowForms.actions.views') }}
            </v-btn>

            <v-divider vertical class="mx-1" />

            <v-btn variant="outlined" size="small" prepend-icon="mdi-plus" color="primary" @click="openNew">
              {{ t('admin.workflowForms.actions.newForm') }}
            </v-btn>

            <v-btn variant="outlined" size="small" prepend-icon="mdi-open-in-new" :disabled="!selectedFormId" @click="openEdit">
              {{ t('admin.workflowForms.actions.popup') }}
            </v-btn>
          </template>

          <v-menu v-else location="bottom end">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                {{ t('admin.workflowForms.actions.views') }}
              </v-btn>
            </template>

            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
                <v-list-item-title>{{ t('admin.workflowForms.actions.checkbox') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-eye-outline" @click="showUnavailable('admin.workflowForms.actions.views')">
                <v-list-item-title>{{ t('admin.workflowForms.actions.views') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-plus" @click="openNew">
                <v-list-item-title>{{ t('admin.workflowForms.actions.newForm') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-open-in-new" :disabled="!selectedFormId" @click="openEdit">
                <v-list-item-title>{{ t('admin.workflowForms.actions.popup') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <span v-if="checkboxMode" class="text-caption text-medium-emphasis">
            {{ t('admin.workflowForms.actions.selected', { count: selectedFormIds.length }) }}
          </span>
        </div>

        <ListMobileCard
          v-if="isPhoneLayout"
          :items="displayedRows"
          :columns="mobileColumns"
          item-key="formId"
          :checkbox-mode="checkboxMode"
          :selected-ids="selectedFormIds"
          :on-select="handleMobileSelect"
          :on-card-click="(item) => onMobileCardClick(item)"
        />

        <div v-else class="workflow-forms-table-shell">
        <v-data-table
          :headers="headers"
          :items="displayedRows"
          :loading="loading"
          item-value="formId"
          v-model="selectedFormIds"
          :show-select="checkboxMode"
          density="compact"
          fixed-header
          height="100%"
          class="workflow-forms-table"
          @click:row="onRowClick"
          @dblclick="openEdit"
        >
          <template #[`item.icon`]>
            <v-icon size="14" color="secondary">mdi-form-select</v-icon>
          </template>
        </v-data-table>
        </div>

        <div class="text-caption text-medium-emphasis mt-2">
          {{ t('admin.workflowForms.rows', { count: displayedRows.length }) }}
        </div>
      </v-card-text>
    </v-card>

    <v-dialog v-model="dialogOpen" max-width="min(100%, 560px)" scrollable>
      <AdminWorkflowFormRecordDialog
        :item="editingItem"
        @saved="handleSaved"
        @deleted="handleDeleted"
        @duplicated="handleDuplicated"
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
import { getAdminWorkflowForms } from '@/services/admin'
import type { AdminWorkflowFormListItem } from '@/types/api'
import AdminWorkflowFormRecordDialog from '@/components/forms/AdminWorkflowFormRecordDialog.vue'

type WorkflowFormDisplayItem = AdminWorkflowFormListItem & {
  icon: string
  ln: number
}

const rows = ref<AdminWorkflowFormListItem[]>([])
const loading = ref(false)
const errorMessage = ref('')
const lookup = ref('')
const checkboxMode = ref(false)
const selectedFormIds = ref<string[]>([])
const sortDirection = ref<'asc' | 'desc'>('asc')
const sortKey = ref('formName')
const visibleColumnKeys = ref<string[]>(['icon', 'formName', 'ln', 'formNameChs', 'formNameCht'])
const dialogOpen = ref(false)
const editingItem = ref<AdminWorkflowFormListItem | null>(null)
const saveSuccess = ref(false)
const successMessage = ref('')

const { t } = useI18n({ useScope: 'global' })
const { isPhoneLayout, isColumnVisible } = useResponsiveList()

const allHeaders = computed(() => [
  { title: '', key: 'icon', width: '32px', sortable: false },
  { title: t('admin.workflowForms.headers.formName'), key: 'formName', minWidth: '120px' },
  { title: '#', key: 'ln', width: '54px' },
  { title: t('admin.workflowForms.headers.formNameChs'), key: 'formNameChs', minWidth: '180px' },
  { title: t('admin.workflowForms.headers.formNameCht'), key: 'formNameCht', minWidth: '180px' },
])

const headers = computed(() =>
  allHeaders.value.filter((h) =>
    visibleColumnKeys.value.includes(String(h.key)) &&
    isColumnVisible(String(h.key), {
      hideOnPhone: ['formNameCht', 'formNameChs'],
    }),
  ),
)

const mobileColumns = computed<ListMobileCardColumn<WorkflowFormDisplayItem>[]>(() => [
  { key: 'formName', label: t('admin.workflowForms.headers.formName'), section: 'header', emphasis: true },
  { key: 'formNameChs', label: t('admin.workflowForms.headers.formNameChs'), section: 'body' },
  { key: 'formNameCht', label: t('admin.workflowForms.headers.formNameCht'), section: 'footer' },
])

const sortableColumns = computed(() =>
  allHeaders.value
    .filter((h) => h.sortable !== false)
    .map((h) => ({ key: String(h.key), title: String(h.title || h.key) })),
)

const columnOptions = computed(() => allHeaders.value.map((h) => ({ key: String(h.key), title: String(h.title || h.key) })))

const displayedRows = computed<WorkflowFormDisplayItem[]>(() => {
  const key = sortKey.value as keyof AdminWorkflowFormListItem
  const result = [...rows.value]

  result.sort((lhs, rhs) => {
    const left = String(lhs[key] ?? '')
    const right = String(rhs[key] ?? '')
    return sortDirection.value === 'asc' ? left.localeCompare(right) : right.localeCompare(left)
  })

  return result.map((item, index) => ({
    ...item,
    icon: 'mdi-form-select',
    ln: index + 1,
  }))
})

const selectedFormId = computed(() => selectedFormIds.value[0] ?? null)

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    rows.value = await getAdminWorkflowForms({
      lookup: lookup.value.trim(),
      take: 500,
    })
  } catch {
    errorMessage.value = t('admin.workflowForms.messages.loadFailed')
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

function showUnavailable(actionKey: string) {
  successMessage.value = t('common.unavailable', { action: t(actionKey) })
  saveSuccess.value = true
}

function toggleColumn(columnKey: string) {
  if (visibleColumnKeys.value.includes(columnKey)) {
    if (visibleColumnKeys.value.length > 1) {
      visibleColumnKeys.value = visibleColumnKeys.value.filter((k) => k !== columnKey)
    }
    return
  }

  visibleColumnKeys.value = [...visibleColumnKeys.value, columnKey]
}

function onRowClick(_event: Event, payload: { item: AdminWorkflowFormListItem }) {
  if (checkboxMode.value) return
  selectedFormIds.value = [payload.item.formId]
  editingItem.value = payload.item
  dialogOpen.value = true
}

function onMobileCardClick(item: WorkflowFormDisplayItem) {
  if (checkboxMode.value) {
    selectedFormIds.value = [item.formId]
    return
  }

  selectedFormIds.value = [item.formId]
  editingItem.value = item
  dialogOpen.value = true
}

function handleMobileSelect(item: Record<string, unknown>, selected: boolean) {
  const formId = String(item.formId ?? '')
  if (!formId) return

  if (selected) {
    selectedFormIds.value = [...new Set([...selectedFormIds.value, formId])]
    return
  }

  selectedFormIds.value = selectedFormIds.value.filter((id) => id !== formId)
}

function openNew() {
  editingItem.value = null
  dialogOpen.value = true
}

function openEdit() {
  if (!selectedFormId.value) {
    errorMessage.value = t('admin.workflowForms.messages.selectRecordFirst')
    return
  }
  const found = rows.value.find((r) => r.formId === selectedFormId.value) ?? null
  editingItem.value = found
  dialogOpen.value = true
}

function handleSaved(item: AdminWorkflowFormListItem) {
  const idx = rows.value.findIndex((r) => r.formId === item.formId)
  if (idx >= 0) {
    rows.value[idx] = item
  } else {
    rows.value = [item, ...rows.value]
    selectedFormIds.value = [item.formId]
  }
  successMessage.value = t('admin.workflowForms.form.save')
  saveSuccess.value = true
}

function handleDeleted(id: string) {
  rows.value = rows.value.filter((r) => r.formId !== id)
  selectedFormIds.value = selectedFormIds.value.filter((fid) => fid !== id)
  dialogOpen.value = false
}

function handleDuplicated(item: AdminWorkflowFormListItem) {
  rows.value = [item, ...rows.value]
  selectedFormIds.value = [item.formId]
  editingItem.value = item
}
</script>

<style scoped>
.workflow-forms-page {
  min-height: 0;
  --wf-header-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%);
  --wf-header-fg: rgb(var(--v-theme-on-surface-variant));
}

.workflow-forms-card {
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

.workflow-forms-table-shell {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 250px);
  min-height: 400px;
  overflow-x: auto;
}

.workflow-forms-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.workflow-forms-table :deep(.v-table__wrapper > table > thead > tr > th),
.workflow-forms-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--wf-header-bg) !important;
  color: var(--wf-header-fg) !important;
}

.workflow-forms-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.workflow-forms-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.workflow-forms-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.workflow-forms-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}
</style>
