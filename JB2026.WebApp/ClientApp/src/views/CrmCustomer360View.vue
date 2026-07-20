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
            <div class="tab-content">
              <p class="text-body-2 text-medium-emphasis">{{ t('customer360.placeholders.opportunities') }}</p>
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
  </section>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { getCrmCompanies, getCrmCompany, getCrmPeople } from '@/services/crm'
import CrmCompanyRecordDialog from '@/components/crm/CrmCompanyRecordDialog.vue'
import CrmPeopleRecordDialog from '@/components/crm/CrmPeopleRecordDialog.vue'
import type { CrmCompany, CrmPerson } from '@/types/api'

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
</style>
