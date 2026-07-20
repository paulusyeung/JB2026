<template>
  <section class="customer-360-page" :class="{ 'is-dragging': isDragging }">
    <div class="resize-overlay" v-if="isDragging" @mousemove="onMouseMove" @mouseup="stopResize" />
    <div class="left-pane">
      <v-card rounded="xl" elevation="0" class="panel-card company-select-card">
        <v-card-text>
          <v-autocomplete
            v-model="selectedCompanyId"
            :items="companies"
            item-title="name"
            item-value="id"
            :label="t('crm.companies.lookup')"
            prepend-inner-icon="mdi-domain"
            variant="solo-filled"
            density="comfortable"
            hide-details
            clearable
            :loading="loadingCompanies"
            :search="companySearch"
            @update:search="onCompanySearch"
            @update:model-value="onCompanySelected"
          />

          <v-divider class="my-3" />

          <div v-if="loadingCompany" class="d-flex justify-center py-6">
            <v-progress-circular indeterminate size="24" />
          </div>

          <template v-else-if="company">
            <div class="company-info">
              <div class="company-info-header">
                <h4 class="text-h6 mb-0">{{ company.name }}</h4>
                <v-btn icon="mdi-pencil" variant="flat" size="small" color="default" class="edit-btn" @click="openEditDialog" />
              </div>

              <div class="info-row">
                <v-icon size="small" class="mr-1">mdi-account-tie</v-icon>
                <span class="text-body-2">{{ company.accountOwner }}</span>
              </div>

              <div v-if="company.domainName" class="info-row">
                <v-icon size="small" class="mr-1">mdi-web</v-icon>
                <span class="text-body-2">{{ company.domainName }}</span>
              </div>

              <div v-if="company.formattedAddress" class="info-row">
                <v-icon size="small" class="mr-1">mdi-map-marker</v-icon>
                <span class="text-body-2">{{ company.formattedAddress }}</span>
              </div>

              <div v-if="company.people.length === 0" class="info-row">
                <v-icon size="small" class="mr-1">mdi-account-multiple</v-icon>
                <span class="text-body-2">0 people</span>
              </div>
              <div v-else class="people-section">
                <div class="people-label">
                  <v-icon size="small">mdi-account-multiple</v-icon>
                  <span class="text-body-2">{{ company.people.length }} people</span>
                </div>
                <div class="people-cards">
                  <div v-for="person in company.people" :key="person.id" class="person-card">
                    <div class="person-card-header">
                      <span class="text-body-2 font-weight-medium">{{ person.name }}</span>
                      <v-btn icon="mdi-pencil" variant="flat" size="x-small" color="default" class="edit-btn" @click="openPersonDialog(person.id)" />
                    </div>
                    <div class="person-card-body">
                      <div v-if="getPerson(person.id)?.jobTitle" class="person-detail">
                        <v-icon size="x-small">mdi-badge-account-outline</v-icon>
                        <span class="text-caption">{{ getPerson(person.id)?.jobTitle }}</span>
                      </div>
                      <div v-for="email in getPerson(person.id)?.emails ?? []" :key="email" class="person-detail">
                        <v-icon size="x-small">mdi-email-outline</v-icon>
                        <span class="text-caption">{{ email }}</span>
                      </div>
                      <div v-for="phone in getPerson(person.id)?.phones ?? []" :key="phone" class="person-detail">
                        <v-icon size="x-small">mdi-phone-outline</v-icon>
                        <span class="text-caption">{{ phone }}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <div class="info-row">
                <v-icon size="small" class="mr-1">mdi-trending-up</v-icon>
                <span class="text-body-2">{{ company.opportunities.length }} opportunities</span>
              </div>

              <div class="info-row">
                <v-icon size="small" class="mr-1">mdi-calendar</v-icon>
                <span class="text-body-2">Created {{ formatDate(company.createdOn) }}</span>
              </div>
            </div>
          </template>

          <div v-else class="text-center py-6 text-medium-emphasis text-body-2">
            {{ t('customer360.selectCompany') }}
          </div>
        </v-card-text>
      </v-card>
    </div>

    <div class="splitter" @mousedown="startResize" />
    <div class="right-pane">
      <v-card rounded="xl" elevation="0" class="panel-card detail-tabs-card">
        <v-tabs v-model="activeTab" fixed-tabs bg-color="transparent" color="primary">
          <v-tab value="job-orders">
            <v-icon start>mdi-briefcase-outline</v-icon>
            {{ t('customer360.tabs.jobOrders') }}
          </v-tab>
          <v-tab value="invoices">
            <v-icon start>mdi-receipt-text-outline</v-icon>
            {{ t('customer360.tabs.invoices') }}
          </v-tab>
          <v-tab value="opportunities">
            <v-icon start>mdi-trending-up</v-icon>
            {{ t('customer360.tabs.opportunities') }}
          </v-tab>
          <v-tab value="tasks">
            <v-icon start>mdi-format-list-checks</v-icon>
            {{ t('customer360.tabs.tasks') }}
          </v-tab>
          <v-tab value="emails">
            <v-icon start>mdi-email-outline</v-icon>
            {{ t('customer360.tabs.emails') }}
          </v-tab>
          <v-tab value="files">
            <v-icon start>mdi-file-outline</v-icon>
            {{ t('customer360.tabs.files') }}
          </v-tab>
          <v-tab value="calendar">
            <v-icon start>mdi-calendar-outline</v-icon>
            {{ t('customer360.tabs.calendar') }}
          </v-tab>
          <v-tab value="timeline">
            <v-icon start>mdi-timeline-outline</v-icon>
            {{ t('customer360.tabs.timeline') }}
          </v-tab>
        </v-tabs>

        <v-divider />

        <v-tabs-window v-model="activeTab">
          <v-tabs-window-item value="job-orders">
            <div class="tab-content">
              <p class="text-body-2 text-medium-emphasis">{{ t('customer360.placeholders.jobOrders') }}</p>
            </div>
          </v-tabs-window-item>
          <v-tabs-window-item value="invoices">
            <div class="tab-content">
              <p class="text-body-2 text-medium-emphasis">{{ t('customer360.placeholders.invoices') }}</p>
            </div>
          </v-tabs-window-item>
          <v-tabs-window-item value="opportunities">
            <div class="tab-content opportunities-tab-content">
              <div class="filter-bar">
                <v-text-field
                  v-model="oppLookup"
                  density="comfortable"
                  :label="t('crm.opportunities.lookup')"
                  prepend-inner-icon="mdi-magnify"
                  variant="solo-filled"
                  hide-details
                  clearable
                  @keydown.enter="applyOppLookup"
                />
                <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loadingOpportunities" @click="applyOppLookup">
                  {{ t('common.search') }}
                </v-btn>
                <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loadingOpportunities" @click="refreshOppList">
                  {{ t('common.refresh') }}
                </v-btn>
              </div>

              <v-alert v-if="oppErrorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ oppErrorMessage }}</v-alert>

              <div class="toolbar-bar mb-2">
                <v-menu location="bottom">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-view-column">
                      {{ t('crm.opportunities.actions.columns') }}
                    </v-btn>
                  </template>
                  <v-list density="compact" class="toolbar-menu-list">
                    <v-list-item v-for="column in oppColumnOptions" :key="column.key" @click="toggleOppColumn(column.key)">
                      <template #prepend>
                        <v-checkbox-btn :model-value="oppVisibleColumnKeys.includes(column.key)" />
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
                      v-model="oppSortKey"
                      :items="oppSortableColumns"
                      item-title="title"
                      item-value="key"
                      density="compact"
                      variant="outlined"
                      :label="t('crm.opportunities.actions.sortBy')"
                      hide-details
                    />
                    <v-btn-toggle v-model="oppSortDirection" mandatory divided class="mt-3" density="compact">
                      <v-btn value="asc">{{ t('crm.opportunities.actions.asc') }}</v-btn>
                      <v-btn value="desc">{{ t('crm.opportunities.actions.desc') }}</v-btn>
                    </v-btn-toggle>
                  </v-card>
                </v-menu>

                <template v-if="!isPhoneLayout">
                  <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="oppCheckboxMode = !oppCheckboxMode">
                    {{ t('crm.opportunities.actions.checkbox') }}
                  </v-btn>

                  <v-menu location="bottom">
                    <template #activator="{ props }">
                      <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                        {{ t('crm.opportunities.actions.views') }}
                      </v-btn>
                    </template>
                    <v-list density="compact" class="toolbar-menu-list">
                      <v-list-item prepend-icon="mdi-table" :active="oppViewMode === 'detail'" @click="setOppViewMode('detail')">
                        <v-list-item-title>{{ t('crm.opportunities.actions.detailView') }}</v-list-item-title>
                      </v-list-item>
                      <v-list-item prepend-icon="mdi-view-grid-outline" :active="oppViewMode === 'card'" @click="setOppViewMode('card')">
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
                    <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="oppCheckboxMode = !oppCheckboxMode">
                      <v-list-item-title>{{ t('crm.opportunities.actions.checkbox') }}</v-list-item-title>
                    </v-list-item>
                    <v-list-item prepend-icon="mdi-table" :active="oppViewMode === 'detail'" @click="setOppViewMode('detail')">
                      <v-list-item-title>{{ t('crm.opportunities.actions.detailView') }}</v-list-item-title>
                    </v-list-item>
                    <v-list-item prepend-icon="mdi-view-grid-outline" :active="oppViewMode === 'card'" @click="setOppViewMode('card')">
                      <v-list-item-title>{{ t('crm.opportunities.actions.cardView') }}</v-list-item-title>
                    </v-list-item>
                  </v-list>
                </v-menu>

                <v-divider vertical class="mx-1" />

                <v-btn variant="outlined" size="small" color="primary" prepend-icon="mdi-plus-circle-outline" @click="openNewOpportunity">
                  {{ t('crm.opportunities.actions.newOpportunity') }}
                </v-btn>

                <span v-if="oppCheckboxMode" class="text-caption text-medium-emphasis">
                  {{ t('crm.opportunities.actions.selected', { count: oppSelectedIds.length }) }}
                </span>
              </div>

              <div v-if="!company" class="text-center py-6 text-medium-emphasis text-body-2">
                {{ t('customer360.selectCompany') }}
              </div>

              <template v-else-if="company.opportunities.length === 0">
                <div class="text-center py-6 text-medium-emphasis text-body-2">
                  {{ t('crm.opportunities.messages.noOpportunities') }}
                </div>
              </template>

              <template v-else>
                <ListMobileCard
                  v-if="isPhoneLayout"
                  :items="oppDisplayedRows"
                  :columns="oppMobileColumns"
                  item-key="id"
                  :checkbox-mode="oppCheckboxMode"
                  :selected-ids="oppSelectedIds"
                  :on-select="handleOppMobileSelect"
                  :on-card-click="(item) => onOppMobileCardClick(item)"
                />

                <div v-else-if="isOppCardView" class="opportunity-card-list">
                  <v-card
                    v-for="row in oppDisplayedRows"
                    :key="row.id"
                    rounded="lg"
                    elevation="0"
                    class="opportunity-card"
                  >
                    <v-checkbox-btn
                      v-if="oppCheckboxMode"
                      :model-value="oppSelectedIds.includes(row.id)"
                      density="compact"
                      hide-details
                      class="opportunity-card__checkbox"
                      @click="handleOppCardCheckbox(row.id)"
                    />
                    <div class="opportunity-card__header">
                      <div class="d-flex align-center ga-2">
                        <v-icon size="18" color="primary">mdi-trending-up</v-icon>
                        <div>
                          <span class="text-subtitle-2 font-weight-bold">{{ row.name }}</span>
                          <v-chip v-if="row.stage" size="x-small" label color="primary" variant="tonal" class="ml-1">
                            {{ oppStageLabel(row.stage) }}
                          </v-chip>
                          <div v-if="row.company" class="text-caption text-medium-emphasis">{{ row.company }}</div>
                        </div>
                      </div>
                    </div>
                    <div class="opportunity-card__body">
                      <span class="text-caption">
                        {{ t('crm.opportunities.headers.amount') }}: {{ row.amount || '-' }}
                      </span>
                      <span class="text-caption">
                        {{ t('crm.opportunities.headers.owner') }}: {{ row.owner || '-' }}
                      </span>
                    </div>
                    <div class="opportunity-card__footer text-caption text-medium-emphasis">
                      <span>{{ t('crm.opportunities.headers.updatedBy') }}: {{ row.updatedBy || '-' }}</span>
                      <span>{{ t('crm.opportunities.headers.updatedOn') }}: {{ oppFormat(row.updatedOn) }}</span>
                    </div>
                  </v-card>
                </div>

                <v-data-table
                  v-else
                  :headers="oppHeaders"
                  :items="oppDisplayedRows"
                  :loading="loadingOpportunities"
                  item-value="id"
                  v-model="oppSelectedIds"
                  :show-select="oppCheckboxMode"
                  density="compact"
                  fixed-header
                  height="45vh"
                  class="opportunities-table"
                >
                  <template #[`item.name`]='{ item }'>
                    <a class="text-body-2 text-primary text-decoration-none cursor-pointer" @click.stop="openOpportunityPopup(item.id)">{{ item.name }}</a>
                  </template>

                  <template #[`item.stage`]='{ item }'>
                    <v-chip v-if="item.stage" size="x-small" label color="primary" variant="tonal">{{ oppStageLabel(item.stage) }}</v-chip>
                    <span v-else class="text-medium-emphasis">-</span>
                  </template>

                  <template #[`item.closeDate`]='{ item }'>
                    <template v-if="item.closeDate">{{ oppFormat(item.closeDate) }}</template>
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

                  <template #[`item.createdOn`]='{ item }'>{{ oppFormat(item.createdOn) }}</template>
                  <template #[`item.updatedOn`]='{ item }'>{{ oppFormat(item.updatedOn) }}</template>
                </v-data-table>
              </template>
            </div>
          </v-tabs-window-item>
          <v-tabs-window-item value="tasks">
            <div class="tab-content">
              <p class="text-body-2 text-medium-emphasis">{{ t('customer360.placeholders.tasks') }}</p>
            </div>
          </v-tabs-window-item>
          <v-tabs-window-item value="emails">
            <div class="tab-content">
              <p class="text-body-2 text-medium-emphasis">{{ t('customer360.placeholders.emails') }}</p>
            </div>
          </v-tabs-window-item>
          <v-tabs-window-item value="files">
            <div class="tab-content">
              <p class="text-body-2 text-medium-emphasis">{{ t('customer360.placeholders.files') }}</p>
            </div>
          </v-tabs-window-item>
          <v-tabs-window-item value="calendar">
            <div class="tab-content">
              <p class="text-body-2 text-medium-emphasis">{{ t('customer360.placeholders.calendar') }}</p>
            </div>
          </v-tabs-window-item>
          <v-tabs-window-item value="timeline">
            <div class="tab-content">
              <p class="text-body-2 text-medium-emphasis">{{ t('customer360.placeholders.timeline') }}</p>
            </div>
          </v-tabs-window-item>
        </v-tabs-window>
      </v-card>
    </div>
    <v-dialog v-model="dialogOpen" max-width="min(100%, 760px)" scrollable>
      <CrmCompanyRecordDialog
        :company-id="editingCompanyId"
        @saved="handleSaved"
        @cancel="dialogOpen = false"
      />
    </v-dialog>

    <v-dialog v-model="personDialogOpen" max-width="min(100%, 760px)" scrollable>
      <CrmPeopleRecordDialog
        :person-id="editingPersonId"
        @saved="handlePersonSaved"
        @cancel="personDialogOpen = false"
      />
    </v-dialog>

    <v-dialog v-model="oppDialogOpen" max-width="min(100%, 760px)" scrollable>
      <CrmOpportunityRecordDialog
        :opportunity-id="editingOpportunityId"
        :initial-company-id="company?.id ?? null"
        @saved="handleOppSaved"
        @cancel="oppDialogOpen = false"
      />
    </v-dialog>

    <v-snackbar v-model="oppSaveSuccess" color="success" timeout="3000">
      {{ oppSuccessMessage }}
      <template #actions>
        <v-btn variant="text" @click="oppSaveSuccess = false">{{ t('common.cancel') }}</v-btn>
      </template>
    </v-snackbar>
  </section>
</template>

<script setup lang="ts">
import { ref, watch, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { getCrmCompanies, getCrmCompany, getCrmPeople, getCrmOpportunities, getCrmOpportunityStageOptions } from '@/services/crm'
import CrmCompanyRecordDialog from '@/components/crm/CrmCompanyRecordDialog.vue'
import CrmPeopleRecordDialog from '@/components/crm/CrmPeopleRecordDialog.vue'
import CrmOpportunityRecordDialog from '@/components/crm/CrmOpportunityRecordDialog.vue'
import ListMobileCard, { type ListMobileCardColumn } from '@/components/grids/ListMobileCard.vue'
import { useViewSettings } from '@/composables/useColumnPersistence'
import { useResponsiveList } from '@/composables/useResponsiveList'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import type { CrmCompany, CrmPerson, CrmOpportunity } from '@/types/api'

const STORAGE_KEY = 'customer-360-left-pane-width'
const MIN_WIDTH_PX = 280
const MAX_WIDTH_PX = 600

const leftPaneWidth = ref(loadStoredWidth())
const isDragging = ref(false)

function loadStoredWidth(): number {
  const stored = localStorage.getItem(STORAGE_KEY)
  if (stored) {
    const parsed = parseFloat(stored)
    if (!isNaN(parsed)) return Math.max(MIN_WIDTH_PX, Math.min(MAX_WIDTH_PX, parsed))
  }
  return 400
}

onMounted(() => {
  document.documentElement.style.setProperty('--left-pane-width-fallback', leftPaneWidth.value + 'px')
})

function startResize(e: MouseEvent) {
  e.preventDefault()
  isDragging.value = true
}

function onMouseMove(e: MouseEvent) {
  const clamped = Math.max(MIN_WIDTH_PX, Math.min(MAX_WIDTH_PX, e.clientX))
  leftPaneWidth.value = clamped
  document.documentElement.style.setProperty('--left-pane-width-fallback', clamped + 'px')
}

function stopResize() {
  isDragging.value = false
  localStorage.setItem(STORAGE_KEY, String(leftPaneWidth.value))
}

const dialogOpen = ref(false)
const editingCompanyId = ref<string | null>(null)

function openEditDialog() {
  editingCompanyId.value = company.value?.id ?? null
  dialogOpen.value = true
}

function handleSaved(saved: CrmCompany) {
  company.value = saved
  dialogOpen.value = false
}

const personDialogOpen = ref(false)
const editingPersonId = ref<string | null>(null)

function openPersonDialog(personId: string) {
  editingPersonId.value = personId
  personDialogOpen.value = true
}

function handlePersonSaved(saved: CrmPerson) {
  personDialogOpen.value = false
}

const { t } = useI18n({ useScope: 'global' })

const companies = ref<CrmCompany[]>([])
const company = ref<CrmCompany | null>(null)
const people = ref<CrmPerson[]>([])
const selectedCompanyId = ref<string | null>(null)
const companySearch = ref('')
const loadingCompanies = ref(false)
const loadingCompany = ref(false)
const activeTab = ref('job-orders')

async function loadCompanies(lookup?: string) {
  loadingCompanies.value = true
  try {
    const data = await getCrmCompanies({ lookup })
    companies.value = data.sort((a, b) => a.name.localeCompare(b.name))
  } finally {
    loadingCompanies.value = false
  }
}

function onCompanySearch(val: string | null | undefined) {
  companySearch.value = val ?? ''
}

async function onCompanySelected(id: string | null) {
  if (!id) {
    company.value = null
    people.value = []
    return
  }
  loadingCompany.value = true
  try {
    const [c, allPeople] = await Promise.all([
      getCrmCompany(id),
      getCrmPeople(),
    ])
    company.value = c
    const ids = new Set(c.people.map(p => p.id))
    people.value = allPeople.filter(p => ids.has(p.id))
  } finally {
    loadingCompany.value = false
  }
}

watch(companySearch, (val) => {
  loadCompanies(val)
}, { debounce: 300 })

loadCompanies()

function formatDate(dateStr: string): string {
  if (!dateStr) return ''
  const d = new Date(dateStr)
  return d.toLocaleDateString()
}

function getPerson(id: string): CrmPerson | undefined {
  return people.value.find(p => p.id === id)
}

// --- Opportunities tab ---

type OppDisplayItem = CrmOpportunity & {
  ln: number
}

const oppRows = ref<CrmOpportunity[]>([])
const loadingOpportunities = ref(false)
const oppLookup = ref('')
const oppErrorMessage = ref('')
const oppStageLabelMap = ref<Record<string, string>>({})
const oppDialogOpen = ref(false)
const editingOpportunityId = ref<string | null>(null)
const oppSaveSuccess = ref(false)
const oppSuccessMessage = ref('')
const oppSelectedIds = ref<string[]>([])

const oppViewSettings = useViewSettings('crm-customer360-opportunities', {
  visibleColumns: ['name', 'stage', 'closeDate', 'amount', 'company', 'pointOfContact', 'owner', 'createdOn', 'createdBy', 'updatedOn', 'updatedBy'],
  sortKey: 'name',
  sortDirection: 'asc',
  checkboxMode: false,
  viewMode: 'detail',
})
const oppVisibleColumnKeys = oppViewSettings.visibleColumns
const oppSortKey = oppViewSettings.sortKey
const oppSortDirection = oppViewSettings.sortDirection
const oppCheckboxMode = oppViewSettings.checkboxMode
const oppViewMode = oppViewSettings.viewMode

const { isPhoneLayout, isColumnVisible: oppIsColumnVisible } = useResponsiveList()
const { format: oppFormat } = useGlobalDateFormatter()

getCrmOpportunityStageOptions().then(opts => {
  oppStageLabelMap.value = Object.fromEntries(opts.map(o => [o.value, o.label]))
}).catch(() => {})

function oppStageLabel(value: string): string {
  return oppStageLabelMap.value[value] || value
}

const isOppCardView = computed(() => oppViewMode.value === 'card')

const allOppHeaders = computed(() => [
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

const oppHeaders = computed(() =>
  allOppHeaders.value.filter((h) =>
    oppVisibleColumnKeys.value.includes(String(h.key)) &&
    oppIsColumnVisible(String(h.key), {
      hideOnPhone: ['closeDate', 'amount', 'company', 'pointOfContact', 'owner', 'createdOn', 'createdBy', 'updatedOn', 'updatedBy'],
      hideOnTablet: [],
    }),
  ),
)

const oppMobileColumns = computed<ListMobileCardColumn<OppDisplayItem>[]>(() => [
  { key: 'name', label: t('crm.opportunities.headers.name'), section: 'header', emphasis: true },
  { key: 'stage', label: t('crm.opportunities.headers.stage'), section: 'header' },
  { key: 'company', label: t('crm.opportunities.headers.company'), section: 'body' },
  { key: 'owner', label: t('crm.opportunities.headers.owner'), section: 'body' },
  { key: 'createdBy', label: t('crm.opportunities.headers.createdBy'), section: 'footer' },
  {
    key: 'updatedOn',
    label: t('crm.opportunities.headers.updatedOn'),
    section: 'footer',
    formatter: (item) => oppFormat(item.updatedOn),
  },
])

const oppSortableColumns = computed(() =>
  allOppHeaders.value.map((h) => ({ key: String(h.key), title: String(h.title || h.key) })),
)

const oppColumnOptions = computed(() => allOppHeaders.value.map((h) => ({ key: String(h.key), title: String(h.title || h.key) })))

const oppDisplayedRows = computed<OppDisplayItem[]>(() => {
  const key = oppSortKey.value as keyof CrmOpportunity
  const result = [...oppRows.value]

  result.sort((lhs, rhs) => {
    const left = String(lhs[key] ?? '')
    const right = String(rhs[key] ?? '')
    return oppSortDirection.value === 'asc' ? left.localeCompare(right) : right.localeCompare(left)
  })

  return result.map((item, index) => ({
    ...item,
    ln: index + 1,
  }))
})

async function loadOpportunities() {
  if (!company.value) {
    oppRows.value = []
    return
  }
  loadingOpportunities.value = true
  oppErrorMessage.value = ''
  try {
    const all = await getCrmOpportunities(oppLookup.value.trim())
    oppRows.value = all.filter(o => o.companyId === company.value!.id)
  } catch {
    oppErrorMessage.value = t('crm.opportunities.messages.loadFailed')
  } finally {
    loadingOpportunities.value = false
  }
}

watch(company, () => {
  loadOpportunities()
})

async function applyOppLookup() {
  await loadOpportunities()
}

async function refreshOppList() {
  oppLookup.value = ''
  await loadOpportunities()
}

function toggleOppColumn(columnKey: string) {
  if (oppVisibleColumnKeys.value.includes(columnKey)) {
    if (oppVisibleColumnKeys.value.length > 1) {
      oppVisibleColumnKeys.value = oppVisibleColumnKeys.value.filter((key) => key !== columnKey)
    }
    return
  }
  oppVisibleColumnKeys.value = [...oppVisibleColumnKeys.value, columnKey]
}

function setOppViewMode(mode: 'detail' | 'card') {
  oppViewMode.value = mode
}

function handleOppCardCheckbox(id: string) {
  if (oppSelectedIds.value.includes(id)) {
    oppSelectedIds.value = oppSelectedIds.value.filter((pid) => pid !== id)
    return
  }
  oppSelectedIds.value = [...oppSelectedIds.value, id]
}

function onOppMobileCardClick(item: OppDisplayItem) {
  if (oppCheckboxMode.value) {
    handleOppMobileSelect(item, !oppSelectedIds.value.includes(item.id))
    return
  }
  openOpportunityPopup(item.id)
}

function handleOppMobileSelect(item: OppDisplayItem | Record<string, unknown>, selected: boolean) {
  const id = String(item.id ?? '')
  if (!id) return
  if (selected) {
    oppSelectedIds.value = [...new Set([...oppSelectedIds.value, id])]
    return
  }
  oppSelectedIds.value = oppSelectedIds.value.filter((pid) => pid !== id)
}

function openOpportunityPopup(id: string) {
  editingOpportunityId.value = id
  oppDialogOpen.value = true
  oppErrorMessage.value = ''
}

function openNewOpportunity() {
  editingOpportunityId.value = null
  oppDialogOpen.value = true
  oppErrorMessage.value = ''
}

async function handleOppSaved(opportunity: CrmOpportunity) {
  await loadOpportunities()
  oppSelectedIds.value = [opportunity.id]
  editingOpportunityId.value = opportunity.id
  oppSuccessMessage.value = t('crm.opportunities.messages.saveSuccess')
  oppSaveSuccess.value = true
}
</script>

<style scoped>
.customer-360-page {
  display: flex;
  height: calc(100vh - 7rem);
  position: relative;
}

.customer-360-page.is-dragging {
  cursor: col-resize;
  user-select: none;
}

.resize-overlay {
  position: fixed;
  inset: 0;
  z-index: 9999;
  cursor: col-resize;
}

.left-pane {
  width: var(--left-pane-width-fallback, 400px);
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
}

.splitter {
  width: 4px;
  flex-shrink: 0;
  cursor: col-resize;
  background: transparent;
  transition: background 0.15s;
  margin: 0 2px;
  border-radius: 2px;
}

.splitter:hover,
.is-dragging .splitter {
  background: rgb(var(--v-theme-primary));
}

.right-pane {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.company-select-card {
  flex: 1;
}

.detail-tabs-card {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.detail-tabs-card :deep(.v-tabs-window) {
  flex: 1;
  overflow-y: auto;
}

.company-info {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.company-info-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}

.edit-btn {
  opacity: 0.6;
  transition: opacity 0.15s;
}

.edit-btn:hover {
  opacity: 1;
}

.people-section {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.people-label {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.people-cards {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}

.person-card {
  border: 1px solid rgba(var(--v-theme-on-surface), 0.12);
  border-radius: 8px;
  padding: 0.4rem 0.6rem;
}

.person-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.25rem;
}

.person-card-body {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  margin-top: 0.25rem;
}

.person-detail {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.info-row {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.tab-content {
  padding: 1.5rem;
}

.opportunities-tab-content {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.opportunities-tab-content .filter-bar {
  display: grid;
  gap: 12px;
  grid-template-columns: minmax(200px, 1fr) auto auto;
  align-items: center;
}

.opportunities-tab-content .toolbar-bar {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
}

.opportunities-tab-content .toolbar-menu-list {
  max-height: 340px;
  overflow: auto;
}

.opportunities-tab-content .opportunities-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.opportunities-tab-content .opportunities-table :deep(.v-table__wrapper > table > thead > tr > th),
.opportunities-tab-content .opportunities-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%) !important;
  color: rgb(var(--v-theme-on-surface-variant)) !important;
}

.opportunities-tab-content .opportunities-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.opportunities-tab-content .opportunities-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.opportunities-tab-content .opportunities-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.opportunities-tab-content .opportunities-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
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
  background: rgba(255, 255, 255, 0.72);
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

@media (max-width: 960px) {
  .opportunities-tab-content .filter-bar {
    grid-template-columns: 1fr;
  }
}
</style>
