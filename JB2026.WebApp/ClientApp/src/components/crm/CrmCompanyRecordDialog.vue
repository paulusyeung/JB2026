<template>
  <v-card class="company-record-dialog">
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
        <v-col cols="12">
          <v-textarea
            v-model="draft.address"
            :label="t('crm.companies.form.address')"
            variant="outlined"
            density="compact"
            maxlength="512"
            rows="3"
            auto-grow
          />
        </v-col>
      </v-row>

      <v-divider class="my-3" />

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
          <template v-if="draft.peopleItems.length">
            <v-chip
              v-for="(person, idx) in draft.peopleItems"
              :key="idx"
              size="small"
              label
              class="ma-1"
            >{{ person.name }}</v-chip>
          </template>
          <span v-else class="text-medium-emphasis">-</span>
        </v-col>
        <v-col cols="12" md="6">
          <label class="text-body-2 text-medium-emphasis mb-1 d-block">{{ t('crm.companies.headers.opportunities') }}</label>
          <template v-if="draft.opportunityItems.length">
            <v-chip
              v-for="(opp, idx) in draft.opportunityItems"
              :key="idx"
              size="small"
              label
              color="primary"
              variant="tonal"
              class="ma-1"
            >{{ opp.name }}</v-chip>
          </template>
          <span v-else class="text-medium-emphasis">-</span>
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
import { getCrmCompany, getCrmMembers, updateCrmCompany } from '@/services/crm'
import type { CrmCompany, CrmMember, CrmRelationItem } from '@/types/api'

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

const memberOptions = computed(() =>
  [...members.value].sort((a, b) => a.displayName.localeCompare(b.displayName)),
)

const draft = reactive({
  name: '',
  domainName: '',
  address: '',
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

async function loadRecord(companyId: string | null) {
  errorMessage.value = ''

  if (!companyId) {
    draft.name = ''
    draft.domainName = ''
    draft.address = ''
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
    draft.address = company.address
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
      address: draft.address.trim(),
      accountOwnerId: draft.accountOwnerId,
      peopleIds: null,
      opportunityIds: null,
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
