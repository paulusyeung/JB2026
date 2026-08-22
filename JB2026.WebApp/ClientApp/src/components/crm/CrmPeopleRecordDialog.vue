<template>
  <v-card v-draggable-dialog class="person-record-dialog">
    <v-card-title class="d-flex align-center ga-3 pb-2">
      <div>
        <h2 class="text-h6 mb-1">
          {{ t('crm.people.form.editTitle') }}
        </h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          {{ draft.name || '-' }}
        </p>
      </div>
      <v-spacer />
      <v-btn variant="tonal" icon="mdi-close" @click="emit('cancel')" />
    </v-card-title>

    <v-card-text class="pt-2">
      <div class="d-flex flex-wrap ga-2 mb-4">
        <v-btn size="small" color="primary" prepend-icon="mdi-content-save" :loading="saving" @click="handleSave()">
          {{ t('crm.people.form.save') }}
        </v-btn>
        <v-btn size="small" variant="tonal" prepend-icon="mdi-content-save-check" :loading="saving" @click="handleSave(true)">
          {{ t('crm.people.form.saveClose') }}
        </v-btn>
      </div>

      <v-row dense>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="draft.name"
            :label="t('crm.people.form.name')"
            variant="outlined"
            density="compact"
            maxlength="256"
            :rules="[requiredName]"
          />
        </v-col>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="draft.jobTitle"
            :label="t('crm.people.form.jobTitle')"
            variant="outlined"
            density="compact"
            maxlength="256"
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="6">
          <label class="text-body-2 text-medium-emphasis mb-1 d-block">{{ t('crm.people.form.email') }}</label>
          <div v-if="draft.emails.length" class="mb-2">
            <v-chip
              v-for="(email, idx) in draft.emails"
              :key="idx"
              size="small"
              label
              closable
              class="ma-1"
              @click:close="removeAt(draft.emails, idx)"
            >{{ email }}</v-chip>
          </div>
          <v-text-field
            v-model="newEmail"
            :label="t('crm.people.form.addEmail')"
            variant="outlined"
            density="compact"
            type="email"
            @keydown.enter.prevent="addEmail"
            @blur="addEmail"
          />
        </v-col>
        <v-col cols="12" md="6">
          <label class="text-body-2 text-medium-emphasis mb-1 d-block">
            {{ t('crm.people.form.phone') }}
            <span class="text-caption text-medium-emphasis">({{ t('crm.people.form.phoneHint') }})</span>
          </label>
          <div v-if="draft.phones.length" class="mb-2">
            <v-chip
              v-for="(phone, idx) in draft.phones"
              :key="idx"
              size="small"
              label
              closable
              class="ma-1"
              @click:close="removeAt(draft.phones, idx)"
            >{{ phone }}</v-chip>
          </div>
          <v-text-field
            v-model="newPhone"
            :label="t('crm.people.form.addPhone')"
            variant="outlined"
            density="compact"
            :rules="[validPhone]"
            :error-messages="phoneError"
            @keydown.enter.prevent="addPhone"
            @update:model-value="onPhoneInput"
            @blur="addPhone"
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="6">
          <v-select
            v-model="draft.companyId"
            :items="companyOptions"
            item-title="name"
            item-value="id"
            :label="t('crm.people.form.company')"
            variant="outlined"
            density="compact"
            clearable
            :loading="loadingCompanies"
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
        {{ t('crm.people.form.cancel') }}
      </v-btn>
    </v-card-actions>
  </v-card>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { getCrmCompanies, getCrmPeople, updateCrmPerson, createCrmPerson } from '@/services/crm'
import { countryFromCallingCodePrefix, formatPartialNumber, parsePhoneNumberFromString, toDigits, validateNationalNumber } from '@/utils/phoneParser'
import type { CrmCompany, CrmPerson } from '@/types/api'

const props = defineProps<{
  personId: string | null
}>()

const emit = defineEmits<{
  (e: 'saved', person: CrmPerson): void
  (e: 'cancel'): void
}>()

const { t } = useI18n({ useScope: 'global' })

const saving = ref(false)
const errorMessage = ref('')
const newEmail = ref('')
const newPhone = ref('')
const newPhoneCountryCode = ref('US')
const phoneError = ref('')
const companies = ref<CrmCompany[]>([])
const loadingCompanies = ref(false)

const companyOptions = computed(() =>
  [...companies.value].sort((a, b) => a.name.localeCompare(b.name)),
)

const draft = reactive({
  name: '',
  jobTitle: '',
  emails: [] as string[],
  phones: [] as string[],
  companyId: null as string | null,
})

const requiredName = (value: string) => value.trim().length > 0 || t('crm.people.form.requiredName')

const validPhone = (value: string) => {
  if (!value?.trim())
    return true
  const result = validateNationalNumber(
    resolveCountry(value),
    value,
    (args) => t('crm.people.form.invalidPhone', args),
  )
  return result.valid || true
}

// Resolve the country for parsing/validation: a "+" prefix lets libphonenumber
// infer it; a leading international calling code in the raw digits (e.g.
// "85212345678") is matched automatically; otherwise the default applies.
function resolveCountry(value: string): string {
  const parsed = parsePhoneNumberFromString((value || '').trim())
  if (parsed?.country)
    return parsed.country

  const inferred = countryFromCallingCodePrefix(toDigits(value))
  return inferred ?? newPhoneCountryCode.value
}

function onPhoneInput(value: string) {
  phoneError.value = ''
  if (!value)
    return
  // Live-format using the inferred (or default) country while the user types.
  const country = resolveCountry(value)
  const formatted = formatPartialNumber(country, value)
  if (formatted !== value)
    newPhone.value = formatted
}

watch(
  () => props.personId,
  async (personId) => {
    await loadCompanies()
    await loadRecord(personId)
  },
  { immediate: true },
)

async function loadCompanies() {
  loadingCompanies.value = true
  try {
    companies.value = await getCrmCompanies()
  } catch {
    companies.value = []
  } finally {
    loadingCompanies.value = false
  }
}

function removeAt(list: string[], index: number) {
  list.splice(index, 1)
}

function addEmail() {
  const value = newEmail.value.trim()
  if (value && !draft.emails.includes(value))
    draft.emails.push(value)
  newEmail.value = ''
}

function addPhone() {
  const raw = newPhone.value.trim()
  if (!raw) {
    phoneError.value = ''
    return
  }

  const result = validateNationalNumber(
    resolveCountry(raw),
    raw,
    (args) => t('crm.people.form.invalidPhone', args),
  )
  if (!result.valid || !result.e164) {
    phoneError.value = result.message ?? t('crm.people.form.invalidPhone', { country: newPhoneCountryCode.value })
    return
  }

  if (!draft.phones.includes(result.e164)) {
    draft.phones.push(result.e164)
    phoneError.value = ''
  }
  newPhone.value = ''
}

async function loadRecord(personId: string | null) {
  errorMessage.value = ''

  if (!personId) {
    draft.name = ''
    draft.jobTitle = ''
    draft.emails = []
    draft.phones = []
    draft.companyId = null
    newPhoneCountryCode.value = 'US'
    phoneError.value = ''
    return
  }

  try {
    const people = await getCrmPeople()
    const person = people.find(p => p.id === personId)
    if (!person)
      return

    draft.name = person.name
    draft.jobTitle = person.jobTitle
    draft.emails = [...person.emails]
    draft.phones = [...person.phones]
    draft.companyId = companies.value.find(c => person.companies.includes(c.name))?.id ?? null
  } catch {
    errorMessage.value = t('crm.people.messages.loadRecordFailed')
  }
}

async function handleSave(closeAfter = false) {
  if (!draft.name.trim()) {
    errorMessage.value = t('crm.people.form.requiredName')
    return
  }

  saving.value = true
  errorMessage.value = ''

  // Flush any text still typed in the add fields so it isn't lost on save.
  addEmail()
  addPhone()

  try {
    const payload = {
      name: draft.name.trim(),
      jobTitle: draft.jobTitle.trim(),
      emails: draft.emails.map(e => e.trim()).filter(Boolean),
      phones: draft.phones.map(p => p.trim()).filter(Boolean),
      companyId: draft.companyId,
    }

    const result = props.personId
      ? await updateCrmPerson(props.personId, payload)
      : await createCrmPerson(payload)

    emit('saved', result)

    if (closeAfter) {
      emit('cancel')
    }
  } catch (err) {
    const axiosErr = err as {
      response?: { status?: number; data?: { message?: string } | string }
      message?: string
    }
    const data = axiosErr.response?.data
    const serverMsg =
      (typeof data === 'string' ? data : data?.message) ||
      axiosErr.message ||
      t('crm.people.messages.saveFailed')
    errorMessage.value = serverMsg
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.person-record-dialog {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
}
</style>
