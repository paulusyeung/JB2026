<template>
  <section class="page-section billing-statement-page">
    <v-card rounded="xl" elevation="0" class="panel-card billing-statement-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3 pb-2">
        <div>
          <h3 class="text-h6 mb-1">{{ t('billing.statement.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('billing.statement.subtitle') }}</p>
        </div>
      </v-card-title>

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('billing.statement.lookup')"
            prepend-inner-icon="mdi-magnify"
            variant="solo-filled"
            hide-details
            clearable
            @keydown.enter="applyLookup"
          />

          <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loading" @click="applyLookup">
            {{ t('common.search') }}
          </v-btn>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ errorMessage }}</v-alert>

        <div class="toolbar-bar mb-2">
          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-view-column">
                {{ t('billing.statement.actions.columns') }}
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
                {{ t('billing.statement.actions.sorting') }}
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
                :label="t('billing.statement.actions.sortBy')"
                hide-details
              />
              <v-btn-toggle v-model="sortDirection" mandatory divided class="mt-3" density="compact">
                <v-btn value="asc">{{ t('billing.statement.actions.asc') }}</v-btn>
                <v-btn value="desc">{{ t('billing.statement.actions.desc') }}</v-btn>
              </v-btn-toggle>
            </v-card>
          </v-menu>

          <template v-if="!isPhoneLayout">
            <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
              {{ t('billing.statement.actions.checkbox') }}
            </v-btn>

            <v-menu location="bottom">
              <template #activator="{ props }">
                <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                  {{ t('billing.statement.actions.views') }}
                </v-btn>
              </template>
              <v-list density="compact" class="toolbar-menu-list">
                <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                  <v-list-item-title>{{ t('billing.statement.actions.detailView') }}</v-list-item-title>
                </v-list-item>
                <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                  <v-list-item-title>{{ t('billing.statement.actions.cardView') }}</v-list-item-title>
                </v-list-item>
              </v-list>
            </v-menu>

            <v-divider vertical class="mx-1" />

            <v-btn
              :disabled="!canOpenStatement"
              variant="outlined"
              size="small"
              color="primary"
              prepend-icon="mdi-file-document-outline"
              @click="handleStatement"
            >
              {{ t('billing.statement.actions.statement') }}
              <v-tooltip activator="parent" location="top">
                {{ canOpenStatement ? '' : t('billing.statement.messages.selectSingleClient') }}
              </v-tooltip>
            </v-btn>
          </template>

          <v-menu v-else location="bottom end">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                {{ t('billing.statement.actions.views') }}
              </v-btn>
            </template>

            <v-list density="compact" class="toolbar-menu-list">
              <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="checkboxMode = !checkboxMode">
                <v-list-item-title>{{ t('billing.statement.actions.checkbox') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-table" :active="viewMode === 'detail'" @click="setViewMode('detail')">
                <v-list-item-title>{{ t('billing.statement.actions.detailView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item prepend-icon="mdi-view-grid-outline" :active="viewMode === 'card'" @click="setViewMode('card')">
                <v-list-item-title>{{ t('billing.statement.actions.cardView') }}</v-list-item-title>
              </v-list-item>
              <v-list-item :disabled="!canOpenStatement" prepend-icon="mdi-file-document-outline" @click="handleStatement">
                <v-list-item-title>{{ t('billing.statement.actions.statement') }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>

          <span v-if="checkboxMode" class="text-caption text-medium-emphasis">
            {{ t('billing.statement.actions.selected', { count: selectedClientIds.length }) }}
          </span>
        </div>

        <ListMobileCard
          v-if="isPhoneLayout"
          :items="displayedRows"
          :columns="mobileColumns"
          item-key="externalClientId"
          :checkbox-mode="checkboxMode"
          :selected-ids="selectedClientIds"
          :on-select="handleMobileSelect"
        />

        <div v-else-if="isCardView" class="statement-card-list">
          <v-card
            v-for="row in displayedRows"
            :key="row.externalClientId"
            rounded="lg"
            elevation="0"
            class="statement-card"
          >
            <div v-if="checkboxMode" class="statement-card__checkbox-anchor">
              <v-checkbox-btn
                :model-value="selectedClientIds.includes(row.externalClientId)"
                density="compact"
                hide-details
                @click.stop="handleCardCheckbox(row.externalClientId)"
              />
            </div>
            <div class="statement-card__header">
              <div class="d-flex align-center ga-2">
                <v-icon size="18" color="primary">mdi-account</v-icon>
                <div>
                  <div class="text-subtitle-2 font-weight-bold">{{ row.clientName }}</div>
                  <div class="text-caption text-medium-emphasis">{{ row.clientCode || t('billing.statement.labels.empty') }}</div>
                </div>
              </div>
            </div>
            <div class="statement-card__footer text-caption text-medium-emphasis">
              <span>{{ t('billing.statement.headers.outstandingBalance') }}: {{ formatOutstandingBalance(row.outstandingBalance) }}</span>
            </div>
          </v-card>
        </div>

        <v-data-table
          v-else
          v-model="selectedClientIds"
          :headers="headers"
          :items="displayedRows"
          :loading="loading"
          item-value="externalClientId"
          :show-select="checkboxMode"
          density="compact"
          fixed-header
          height="62vh"
          class="billing-statement-table"
        >
          <template #[`item.icon`]>
            <v-icon size="14" color="primary">mdi-account</v-icon>
          </template>

          <template #[`item.outstandingBalance`]="{ item }">
            <span class="billing-statement-balance">{{ formatOutstandingBalance(item.outstandingBalance) }}</span>
          </template>
        </v-data-table>
      </v-card-text>
    </v-card>

    <BillingStatementRequestDialog
      :model-value="statementDialogOpen"
      :client-name="selectedStatementClient?.clientName ?? ''"
      :submitting="statementLaunchLoading"
      :error-message="statementDialogErrorMessage"
      @update:model-value="handleStatementDialogToggle"
      @submit="handleStatementProceed"
    />
  </section>
</template>

<script setup lang="ts">
import axios from 'axios'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import BillingStatementRequestDialog from '@/components/billing/BillingStatementRequestDialog.vue'
import ListMobileCard, { type ListMobileCardColumn } from '@/components/grids/ListMobileCard.vue'
import { useResponsiveList } from '@/composables/useResponsiveList'
import { useViewSettings } from '@/composables/useColumnPersistence'
import {
  createBillingStatementLaunch,
  downloadBillingStatementDocument,
  type BillingStatementLaunchRequest,
  listBillingClients,
  type BillingStatementClient,
} from '@/services/billing'

type BillingStatementViewMode = 'detail' | 'card'

type BillingStatementDisplayItem = BillingStatementClient & {
  icon: string
  ln: number
  clientName: string
  clientCode: string
}

const { t } = useI18n({ useScope: 'global' })
const { isPhoneLayout, isColumnVisible } = useResponsiveList()

const rows = ref<BillingStatementClient[]>([])
const loading = ref(false)
const lookup = ref('')
const errorMessage = ref('')
const selectedClientIds = ref<string[]>([])
const statementDialogOpen = ref(false)
const statementLaunchLoading = ref(false)
const statementDialogErrorMessage = ref('')
const viewSettings = useViewSettings('billing-statement', {
  visibleColumns: ['icon', 'ln', 'clientName', 'clientCode', 'outstandingBalance'],
  sortKey: 'clientName',
  sortDirection: 'asc',
  checkboxMode: false,
  viewMode: 'detail',
})

const visibleColumnKeys = viewSettings.visibleColumns
const sortKey = viewSettings.sortKey
const sortDirection = viewSettings.sortDirection
const checkboxMode = viewSettings.checkboxMode
const viewMode = viewSettings.viewMode

const isCardView = computed(() => viewMode.value === 'card')

const allHeaders = computed(() => [
  { title: '', key: 'icon', width: '32px', sortable: false },
  { title: '#', key: 'ln', width: '54px', sortable: false },
  { title: t('billing.statement.headers.clientName'), key: 'clientName', minWidth: '220px' },
  { title: t('billing.statement.headers.clientCode'), key: 'clientCode', minWidth: '130px' },
  { title: t('billing.statement.headers.outstandingBalance'), key: 'outstandingBalance', minWidth: '180px' },
])

const headers = computed(() =>
  allHeaders.value.filter((header) =>
    visibleColumnKeys.value.includes(String(header.key)) &&
    isColumnVisible(String(header.key), {
      hideOnPhone: [],
      hideOnTablet: [],
    }),
  ),
)

const columnOptions = computed(() => allHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title || header.key) })))

const sortableColumns = computed(() =>
  allHeaders.value
    .filter((header) => header.sortable !== false)
    .map((header) => ({ key: String(header.key), title: String(header.title || header.key) })),
)

const mobileColumns = computed<ListMobileCardColumn<BillingStatementDisplayItem>[]>(() => [
  { key: 'ln', label: '#', section: 'header' },
  { key: 'clientName', label: t('billing.statement.headers.clientName'), section: 'header', emphasis: true },
  { key: 'clientCode', label: t('billing.statement.headers.clientCode'), section: 'header' },
  {
    key: 'outstandingBalance',
    label: t('billing.statement.headers.outstandingBalance'),
    section: 'body',
    formatter: (item) => formatOutstandingBalance(item.outstandingBalance),
  },
])

const displayedRows = computed<BillingStatementDisplayItem[]>(() => {
  const result = rows.value.map((item, index) => ({
    ...item,
    icon: 'mdi-account',
    ln: index + 1,
    clientName: item.displayName || item.name || t('billing.statement.labels.empty'),
    clientCode: item.idNumber || '',
  }))

  const activeSortKey = sortKey.value ?? 'clientName'
  const direction = sortDirection.value === 'desc' ? -1 : 1

  result.sort((left, right) => compareClients(left, right, activeSortKey) * direction)
  return result
})

const canOpenStatement = computed(() => checkboxMode.value && selectedClientIds.value.length === 1)
const selectedStatementClient = computed(() =>
  displayedRows.value.find((row) => row.externalClientId === selectedClientIds.value[0]),
)

const balanceFormatter = new Intl.NumberFormat('en-US', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

onMounted(async () => {
  await loadClients()
})

async function loadClients() {
  loading.value = true
  errorMessage.value = ''

  try {
    rows.value = await listBillingClients(lookup.value.trim() || undefined)
  } catch (error) {
    if (axios.isAxiosError<{ message?: string }>(error)) {
      errorMessage.value = error.response?.data?.message || error.message || t('billing.statement.messages.loadFailed')
    } else if (error instanceof Error) {
      errorMessage.value = error.message || t('billing.statement.messages.loadFailed')
    } else {
      errorMessage.value = t('billing.statement.messages.loadFailed')
    }
  } finally {
    loading.value = false
  }
}

async function applyLookup() {
  await loadClients()
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

function setViewMode(mode: BillingStatementViewMode) {
  viewMode.value = mode
}

function handleCardCheckbox(externalClientId: string) {
  if (selectedClientIds.value.includes(externalClientId)) {
    selectedClientIds.value = selectedClientIds.value.filter((id) => id !== externalClientId)
    return
  }

  selectedClientIds.value = [...selectedClientIds.value, externalClientId]
}

function handleMobileSelect(item: Record<string, unknown>, selected: boolean) {
  const externalClientId = String(item.externalClientId ?? '')
  if (!externalClientId) return

  if (selected) {
    selectedClientIds.value = [...new Set([...selectedClientIds.value, externalClientId])]
    return
  }

  selectedClientIds.value = selectedClientIds.value.filter((id) => id !== externalClientId)
}

function handleStatement() {
  if (!canOpenStatement.value) {
    return
  }

  statementDialogErrorMessage.value = ''
  statementDialogOpen.value = true
}

function handleStatementDialogToggle(open: boolean) {
  statementDialogOpen.value = open

  if (!open) {
    statementDialogErrorMessage.value = ''
  }
}

function openStatementPreviewWindow() {
  const previewWindow = window.open('', '_blank')

  if (!previewWindow) {
    return null
  }

  previewWindow.document.title = t('billing.statement.messages.previewTitle')
  previewWindow.document.body.innerHTML = `<p style="font-family: sans-serif; padding: 16px;">${t('billing.statement.messages.previewLoading')}</p>`
  return previewWindow
}

function renderStatementPreview(previewWindow: Window, documentBlob: Blob) {
  const objectUrl = URL.createObjectURL(documentBlob)

  previewWindow.document.title = t('billing.statement.messages.previewTitle')
  previewWindow.document.body.innerHTML = ''
  previewWindow.document.body.style.margin = '0'

  const iframe = previewWindow.document.createElement('iframe')
  iframe.src = objectUrl
  iframe.title = t('billing.statement.messages.previewTitle')
  iframe.style.border = '0'
  iframe.style.width = '100vw'
  iframe.style.height = '100vh'

  previewWindow.document.body.appendChild(iframe)
  previewWindow.addEventListener('beforeunload', () => URL.revokeObjectURL(objectUrl), { once: true })
}

async function extractStatementLaunchErrorMessage(error: unknown) {
  if (!axios.isAxiosError(error)) {
    if (error instanceof Error) {
      return error.message || t('billing.statement.messages.launchFailed')
    }

    return t('billing.statement.messages.launchUnexpected')
  }

  const responseData = error.response?.data
  if (responseData instanceof Blob) {
    try {
      const text = await responseData.text()
      const parsed = JSON.parse(text) as { message?: string }
      if (parsed.message) {
        return parsed.message
      }
    } catch {
      // Fall back to the normal axios message below.
    }
  }

  return error.response?.data?.message || error.message || t('billing.statement.messages.launchFailed')
}

async function handleStatementProceed(request: BillingStatementLaunchRequest) {
  const selectedClientId = selectedClientIds.value[0]
  if (!selectedClientId) {
    statementDialogErrorMessage.value = t('billing.statement.messages.selectSingleClient')
    return
  }

  const previewWindow = openStatementPreviewWindow()
  if (!previewWindow) {
    statementDialogErrorMessage.value = t('billing.statement.messages.previewBlocked')
    return
  }

  statementLaunchLoading.value = true
  statementDialogErrorMessage.value = ''

  try {
    const launchUrl = await createBillingStatementLaunch({
      ...request,
      externalClientId: selectedClientId,
    })
    const statementDocument = await downloadBillingStatementDocument(launchUrl)

    renderStatementPreview(previewWindow, statementDocument)
  } catch (error) {
    previewWindow.close()
    statementDialogErrorMessage.value = await extractStatementLaunchErrorMessage(error)
  } finally {
    statementLaunchLoading.value = false
  }
}

function formatOutstandingBalance(value: number) {
  return `$${balanceFormatter.format(value ?? 0)}`
}

function compareClients(left: BillingStatementDisplayItem, right: BillingStatementDisplayItem, key: string) {
  switch (key) {
    case 'outstandingBalance':
      return left.outstandingBalance - right.outstandingBalance
    case 'clientCode':
      return left.clientCode.localeCompare(right.clientCode)
    case 'externalClientId':
      return left.externalClientId.localeCompare(right.externalClientId)
    case 'clientName':
    default:
      return left.clientName.localeCompare(right.clientName)
  }
}
</script>

<style scoped>
.billing-statement-page {
  min-height: 0;
  --billing-statement-header-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%);
  --billing-statement-header-fg: rgb(var(--v-theme-on-surface-variant));
}

.billing-statement-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
  background: linear-gradient(180deg, rgba(224, 237, 255, 0.92), rgba(241, 247, 255, 0.96));
}

.filter-bar {
  display: grid;
  gap: 12px;
  grid-template-columns: minmax(260px, 1fr) auto;
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

.billing-statement-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.billing-statement-table :deep(.v-table__wrapper > table > thead > tr > th),
.billing-statement-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: var(--billing-statement-header-bg) !important;
  color: var(--billing-statement-header-fg) !important;
}

.billing-statement-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.billing-statement-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.billing-statement-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.billing-statement-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

.billing-statement-balance {
  display: inline-block;
  text-align: left;
  width: 100%;
}

@media (max-width: 960px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}

.statement-card-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .statement-card-list {
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    align-items: start;
  }
}

.statement-card {
  position: relative;
  display: grid;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgba(255, 255, 255, 0.72);
}

.statement-card__checkbox-anchor {
  position: absolute;
  top: 0.75rem;
  right: 0.75rem;
  z-index: 1;
}

.statement-card__header,
.statement-card__body,
.statement-card__footer {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
}

.statement-card__body {
  display: grid;
  gap: 0.45rem;
}
</style>