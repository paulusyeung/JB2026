<template>
  <v-card v-draggable-dialog class="company-record-dialog">
    <v-card-title class="d-flex align-center ga-3 pb-2">
      <div>
        <h2 class="text-h6 mb-1">
          {{ t('crm.companies.form.editTitle') }}
        </h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          {{ draft.name || '-' }}
        </p>
      </div>
      <v-spacer />
      <v-btn variant="text" icon="mdi-close" @click="emit('cancel')" />
    </v-card-title>

    <v-card-text class="pt-2">
      <div class="d-flex flex-wrap ga-2 mb-4">
        <v-btn size="small" color="primary" prepend-icon="mdi-content-save" :loading="saving" @click="handleSave()">
          {{ t('crm.companies.form.save') }}
        </v-btn>
        <v-btn size="small" variant="tonal" prepend-icon="mdi-content-save-check" :loading="saving" @click="handleSave(true)">
          {{ t('crm.companies.form.saveClose') }}
        </v-btn>
      </div>

      <v-row dense>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="draft.name"
            :label="t('crm.companies.form.name')"
            variant="outlined"
            density="compact"
            maxlength="256"
            :rules="[requiredName]"
          />
        </v-col>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="draft.domainName"
            :label="t('crm.companies.form.domainName')"
            variant="outlined"
            density="compact"
            maxlength="256"
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="draft.address.street1"
            :label="t('crm.companies.form.addressStreet1')"
            variant="outlined"
            density="compact"
            maxlength="256"
          />
        </v-col>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="draft.address.street2"
            :label="t('crm.companies.form.addressStreet2')"
            variant="outlined"
            density="compact"
            maxlength="256"
          />
        </v-col>
        <v-col cols="12" md="3">
          <v-text-field
            v-model="draft.address.city"
            :label="t('crm.companies.form.city')"
            variant="outlined"
            density="compact"
            maxlength="128"
          />
        </v-col>
        <v-col cols="12" md="3">
          <v-text-field
            v-model="draft.address.state"
            :label="t('crm.companies.form.state')"
            variant="outlined"
            density="compact"
            maxlength="128"
          />
        </v-col>
        <v-col cols="12" md="3">
          <v-text-field
            v-model="draft.address.postcode"
            :label="t('crm.companies.form.postcode')"
            variant="outlined"
            density="compact"
            maxlength="32"
          />
        </v-col>
        <v-col cols="12" md="3">
          <v-select
            v-model="draft.address.country"
            :items="effectiveCountryOptions"
            :label="t('crm.companies.form.country')"
            variant="outlined"
            density="compact"
            clearable
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="6">
          <v-select
            v-model="draft.accountOwnerId"
            :items="memberOptions"
            item-title="displayName"
            item-value="id"
            :label="t('crm.companies.headers.accountOwner')"
            variant="outlined"
            density="compact"
            clearable
            :loading="loadingMembers"
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="6">
          <label class="text-body-2 text-medium-emphasis mb-1 d-block">{{ t('crm.companies.headers.people') }}</label>
          <div v-if="draft.peopleItems.length" class="mb-2">
            <v-chip
              v-for="person in draft.peopleItems"
              :key="person.id"
              size="small"
              label
              closable
              class="ma-1"
              @click:close="removePerson(person.id)"
            >{{ person.name }}</v-chip>
          </div>
          <v-autocomplete
            v-model="selectedPersonId"
            :items="availablePeople"
            item-title="name"
            item-value="id"
            :label="t('crm.companies.form.addPerson')"
            variant="outlined"
            density="compact"
            clearable
            hide-no-data
            :loading="loadingPeople"
            @update:model-value="addPerson"
          />
        </v-col>
        <v-col cols="12" md="6">
          <label class="text-body-2 text-medium-emphasis mb-1 d-block">{{ t('crm.companies.headers.opportunities') }}</label>
          <div v-if="draft.opportunityItems.length" class="mb-2">
            <v-chip
              v-for="opp in draft.opportunityItems"
              :key="opp.id"
              size="small"
              label
              color="primary"
              variant="tonal"
              closable
              class="ma-1"
              @click:close="removeOpportunity(opp.id)"
            >{{ opp.name }}</v-chip>
          </div>
          <v-autocomplete
            v-model="selectedOpportunityId"
            :items="availableOpportunities"
            item-title="name"
            item-value="id"
            :label="t('crm.companies.form.addOpportunity')"
            variant="outlined"
            density="compact"
            clearable
            hide-no-data
            :loading="loadingOpportunities"
            @update:model-value="addOpportunity"
          />
        </v-col>
      </v-row>
    </v-card-text>

    <v-alert v-if="errorMessage" type="error" variant="tonal" class="mx-6 mb-2">
      {{ errorMessage }}
    </v-alert>

    <v-card-actions class="pa-4 d-flex ga-2 responsive-dialog-actions">
      <v-spacer />
      <v-btn variant="text" :disabled="saving" @click="emit('cancel')">
        {{ t('crm.companies.form.cancel') }}
      </v-btn>
    </v-card-actions>
  </v-card>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { getCrmCompany, getCrmMembers, getCrmOpportunities, getCrmPeople, updateCrmCompany } from '@/services/crm'
import type { CrmAddress, CrmCatalogItem, CrmCompany, CrmMember, CrmRelationItem } from '@/types/api'

const props = defineProps<{
  companyId: string | null
}>()

const emit = defineEmits<{
  (e: 'saved', company: CrmCompany): void
  (e: 'cancel'): void
}>()

const { t } = useI18n({ useScope: 'global' })

const saving = ref(false)
const errorMessage = ref('')
const members = ref<CrmMember[]>([])
const loadingMembers = ref(false)
const allPeople = ref<CrmCatalogItem[]>([])
const allOpportunities = ref<CrmCatalogItem[]>([])
const loadingPeople = ref(false)
const loadingOpportunities = ref(false)
const selectedPersonId = ref<string | null>(null)
const selectedOpportunityId = ref<string | null>(null)

const memberOptions = computed(() =>
  [...members.value].sort((a, b) => a.displayName.localeCompare(b.displayName)),
)

const availablePeople = computed(() =>
  allPeople.value.filter(p => !draft.peopleItems.some(item => item.id === p.id)),
)

const availableOpportunities = computed(() =>
  allOpportunities.value.filter(o => !draft.opportunityItems.some(item => item.id === o.id)),
)

const countryOptions = [
  'Argentina', 'Australia', 'Austria', 'Bangladesh', 'Belgium', 'Brazil', 'Canada',
  'Chile', 'China', 'Colombia', 'Czechia', 'Denmark', 'Egypt', 'Finland', 'France',
  'Germany', 'Greece', 'Hong Kong', 'Hungary', 'Iceland', 'India', 'Indonesia', 'Ireland',
  'Israel', 'Italy', 'Japan', 'Kenya', 'Kuwait', 'Luxembourg', 'Malaysia', 'Mexico',
  'Netherlands', 'New Zealand', 'Nigeria', 'Norway', 'Pakistan', 'Philippines', 'Poland',
  'Portugal', 'Qatar', 'Romania', 'Russia', 'Saudi Arabia', 'Singapore', 'South Africa',
  'South Korea', 'Spain', 'Sweden', 'Switzerland', 'Taiwan', 'Thailand', 'Turkey',
  'Ukraine', 'United Arab Emirates', 'United Kingdom', 'United States', 'Vietnam',
]

const effectiveCountryOptions = computed(() => {
  const current = draft.address.country?.trim()
  if (current && !countryOptions.includes(current))
    return [current, ...countryOptions]
  return countryOptions
})

function emptyAddress(): CrmAddress {
  return { street1: '', street2: '', city: '', state: '', postcode: '', country: '' }
}

const draft = reactive({
  name: '',
  domainName: '',
  address: emptyAddress(),
  accountOwnerId: null as string | null,
  accountOwner: '',
  peopleItems: [] as CrmRelationItem[],
  opportunityItems: [] as CrmRelationItem[],
})

const requiredName = (value: string) => value.trim().length > 0 || t('crm.companies.form.requiredName')

watch(
  () => props.companyId,
  async (companyId) => {
    await loadMembers()
    await loadCatalogs()
    await loadRecord(companyId)
  },
  { immediate: true },
)

async function loadMembers() {
  loadingMembers.value = true
  try {
    members.value = await getCrmMembers()
  } catch {
    members.value = []
  } finally {
    loadingMembers.value = false
  }
}

async function loadCatalogs() {
  loadingPeople.value = true
  loadingOpportunities.value = true
  try {
    const [people, opportunities] = await Promise.all([
      getCrmPeople(),
      getCrmOpportunities(),
    ])
    allPeople.value = [...people].sort((a, b) => a.name.localeCompare(b.name))
    allOpportunities.value = [...opportunities].sort((a, b) => a.name.localeCompare(b.name))
  } catch {
    allPeople.value = []
    allOpportunities.value = []
  } finally {
    loadingPeople.value = false
    loadingOpportunities.value = false
  }
}

function addPerson(id: string | null) {
  if (!id)
    return
  const catalog = allPeople.value.find(p => p.id === id)
  if (catalog && !draft.peopleItems.some(item => item.id === id)) {
    draft.peopleItems.push({ id: catalog.id, name: catalog.name })
  }
  selectedPersonId.value = null
}

function removePerson(id: string) {
  draft.peopleItems = draft.peopleItems.filter(item => item.id !== id)
}

function addOpportunity(id: string | null) {
  if (!id)
    return
  const catalog = allOpportunities.value.find(o => o.id === id)
  if (catalog && !draft.opportunityItems.some(item => item.id === id)) {
    draft.opportunityItems.push({ id: catalog.id, name: catalog.name })
  }
  selectedOpportunityId.value = null
}

function removeOpportunity(id: string) {
  draft.opportunityItems = draft.opportunityItems.filter(item => item.id !== id)
}

async function loadRecord(companyId: string | null) {
  errorMessage.value = ''

  if (!companyId) {
    draft.name = ''
    draft.domainName = ''
    draft.address = emptyAddress()
    draft.accountOwnerId = null
    draft.accountOwner = ''
    draft.peopleItems = []
    draft.opportunityItems = []
    return
  }

  try {
    const company = await getCrmCompany(companyId)
    draft.name = company.name
    draft.domainName = company.domainName
    draft.address = { ...company.address }
    draft.accountOwnerId = company.accountOwnerId || null
    draft.accountOwner = company.accountOwner
    draft.peopleItems = company.people
    draft.opportunityItems = company.opportunities
  } catch {
    errorMessage.value = t('crm.companies.messages.loadRecordFailed')
  }
}

async function handleSave(closeAfter = false) {
  if (!draft.name.trim()) {
    errorMessage.value = t('crm.companies.form.requiredName')
    return
  }

  if (!props.companyId) {
    return
  }

  saving.value = true
  errorMessage.value = ''

  try {
    const result = await updateCrmCompany(props.companyId, {
      name: draft.name.trim(),
      domainName: draft.domainName.trim(),
      address: {
        street1: draft.address.street1.trim(),
        street2: draft.address.street2.trim(),
        city: draft.address.city.trim(),
        state: draft.address.state.trim(),
        postcode: draft.address.postcode.trim(),
        country: draft.address.country.trim(),
      },
      accountOwnerId: draft.accountOwnerId,
      peopleIds: draft.peopleItems.map(item => item.id),
      opportunityIds: draft.opportunityItems.map(item => item.id),
    })

    emit('saved', result)

    if (closeAfter) {
      emit('cancel')
    }
  } catch (err) {
    const axiosErr = err as { response?: { data?: { message?: string } } }
    const serverMsg = axiosErr.response?.data?.message
    errorMessage.value = serverMsg || t('crm.companies.messages.saveFailed')
  } finally {
    saving.value = false
  }
}
</script>
