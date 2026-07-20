<template>
  <section class="customer-360-page">
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
              <h4 class="text-h6 mb-2">{{ company.name }}</h4>

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

              <div class="info-row">
                <v-icon size="small" class="mr-1">mdi-account-multiple</v-icon>
                <span class="text-body-2">{{ company.people.length }} people</span>
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
  </section>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { getCrmCompanies, getCrmCompany } from '@/services/crm'
import type { CrmCompany } from '@/types/api'

const { t } = useI18n({ useScope: 'global' })

const companies = ref<CrmCompany[]>([])
const company = ref<CrmCompany | null>(null)
const selectedCompanyId = ref<string | null>(null)
const companySearch = ref('')
const loadingCompanies = ref(false)
const loadingCompany = ref(false)
const activeTab = ref('job-orders')

async function loadCompanies(lookup?: string) {
  loadingCompanies.value = true
  try {
    companies.value = await getCrmCompanies({ lookup })
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
    return
  }
  loadingCompany.value = true
  try {
    company.value = await getCrmCompany(id)
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
</script>

<style scoped>
.customer-360-page {
  display: flex;
  gap: 1rem;
  height: calc(100vh - 7rem);
}

.left-pane {
  width: 25rem;
  min-width: 18rem;
  display: flex;
  flex-direction: column;
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

.info-row {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.tab-content {
  padding: 1.5rem;
}
</style>
