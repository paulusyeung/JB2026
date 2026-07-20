<template>
  <v-card v-draggable-dialog class="opportunity-record-dialog">
    <v-card-title class="d-flex align-center ga-3 pb-2">
      <div>
        <h2 class="text-h6 mb-1">
          {{ props.opportunityId ? t('crm.opportunities.form.editTitle') : t('crm.opportunities.form.newTitle') }}
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
          {{ t('crm.opportunities.form.save') }}
        </v-btn>
        <v-btn size="small" variant="tonal" prepend-icon="mdi-content-save-check" :loading="saving" @click="handleSave(true)">
          {{ t('crm.opportunities.form.saveClose') }}
        </v-btn>
      </div>

      <v-row dense>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="draft.name"
            :label="t('crm.opportunities.headers.name')"
            variant="outlined"
            density="compact"
            maxlength="256"
            :rules="[requiredName]"
          />
        </v-col>
        <v-col cols="12" md="6">
          <v-select
            v-model="draft.stage"
            :items="stageOptions"
            item-title="label"
            item-value="value"
            :label="t('crm.opportunities.headers.stage')"
            variant="outlined"
            density="compact"
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="6">
          <v-menu v-model="closeDatePickerOpen" :close-on-content-click="false">
            <template #activator="{ props: menuProps }">
              <v-text-field
                :model-value="draft.closeDate ? globalFormat.format(draft.closeDate) : ''"
                :label="t('crm.opportunities.headers.closeDate')"
                variant="outlined"
                density="compact"
                readonly
                append-inner-icon="mdi-calendar"
                v-bind="menuProps"
              />
            </template>
            <v-date-picker
              :model-value="draft.closeDate ? new Date(draft.closeDate + 'T12:00:00') : undefined"
              hide-header
              @update:model-value="onCloseDatePicked"
            />
          </v-menu>
        </v-col>
        <v-col cols="12" md="6">
          <div class="d-flex ga-2 align-start">
            <v-text-field
              v-model="draft.amount"
              :label="t('crm.opportunities.headers.amount')"
              variant="outlined"
              density="compact"
              type="number"
              step="0.01"
              class="flex-grow-1"
            />
            <v-select
              v-model="draft.currencyCode"
              :items="currencyOptions"
              item-title="label"
              item-value="code"
              label="CCY"
              variant="outlined"
              density="compact"
              class="currency-select"
              hide-details
            />
          </div>
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="4">
          <v-select
            v-model="draft.companyId"
            :items="companyOptions"
            item-title="name"
            item-value="id"
            :label="t('crm.opportunities.headers.company')"
            variant="outlined"
            density="compact"
            clearable
          />
        </v-col>
        <v-col cols="12" md="4">
          <v-select
            v-model="draft.pointOfContactId"
            :items="peopleOptions"
            item-title="name"
            item-value="id"
            :label="t('crm.opportunities.headers.pointOfContact')"
            variant="outlined"
            density="compact"
            clearable
          />
        </v-col>
        <v-col cols="12" md="4">
          <v-select
            v-model="draft.ownerId"
            :items="ownerOptions"
            item-title="name"
            item-value="id"
            :label="t('crm.opportunities.headers.owner')"
            variant="outlined"
            density="compact"
            clearable
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
        {{ t('crm.opportunities.form.cancel') }}
      </v-btn>
    </v-card-actions>
  </v-card>
</template>

<script setup lang="ts">
import { reactive, ref, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { getCrmOpportunity, updateCrmOpportunity, createCrmOpportunity, getCrmOpportunityStageOptions, getCrmCompanies, getCrmPeople, getCrmMembers } from '@/services/crm'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import type { CrmOpportunity, CrmStageOption } from '@/types/api'

const props = defineProps<{
  opportunityId: string | null
  initialCompanyId?: string | null
}>()

const emit = defineEmits<{
  (e: 'saved', opportunity: CrmOpportunity): void
  (e: 'cancel'): void
}>()

const { t } = useI18n({ useScope: 'global' })

const globalFormat = useGlobalDateFormatter()
const saving = ref(false)
const errorMessage = ref('')
const stageOptions = ref<CrmStageOption[]>([])
const closeDatePickerOpen = ref(false)
const companyOptions = ref<{ id: string; name: string }[]>([])
const peopleOptions = ref<{ id: string; name: string }[]>([])
const ownerOptions = ref<{ id: string; name: string }[]>([])

onMounted(async () => {
  try {
    stageOptions.value = await getCrmOpportunityStageOptions()
  } catch {
    stageOptions.value = []
  }
  try {
    const companies = await getCrmCompanies()
    companyOptions.value = companies
      .map(c => ({ id: c.id, name: c.name }))
      .sort((a, b) => a.name.localeCompare(b.name))
  } catch {
    companyOptions.value = []
  }
  try {
    const people = await getCrmPeople()
    peopleOptions.value = people
      .map(p => ({ id: p.id, name: p.name }))
      .sort((a, b) => a.name.localeCompare(b.name))
  } catch {
    peopleOptions.value = []
  }
  try {
    const members = await getCrmMembers()
    ownerOptions.value = members.map(m => ({ id: m.id, name: m.displayName }))
  } catch {
    ownerOptions.value = []
  }
})

const draft = reactive({
  name: '',
  stage: '',
  closeDate: '',
  amount: null as number | null,
  currencyCode: 'USD',
  companyId: null as string | null,
  pointOfContactId: null as string | null,
  ownerId: null as string | null,
})

const currencyOptions = [
  { code: 'AED', label: 'AED - UAE Dirham' },
  { code: 'AFN', label: 'AFN - Afghan Afghani' },
  { code: 'ALL', label: 'ALL - Albanian Lek' },
  { code: 'AMD', label: 'AMD - Armenian Dram' },
  { code: 'ANG', label: 'ANG - Netherlands Antillean Guilder' },
  { code: 'AOA', label: 'AOA - Angolan Kwanza' },
  { code: 'ARS', label: 'ARS - Argentine Peso' },
  { code: 'AUD', label: 'AUD - Australian Dollar' },
  { code: 'AWG', label: 'AWG - Aruban Florin' },
  { code: 'AZN', label: 'AZN - Azerbaijani Manat' },
  { code: 'BAM', label: 'BAM - Bosnia-Herzegovina Convertible Mark' },
  { code: 'BBD', label: 'BBD - Barbadian Dollar' },
  { code: 'BDT', label: 'BDT - Bangladeshi Taka' },
  { code: 'BGN', label: 'BGN - Bulgarian Lev' },
  { code: 'BHD', label: 'BHD - Bahraini Dinar' },
  { code: 'BIF', label: 'BIF - Burundian Franc' },
  { code: 'BMD', label: 'BMD - Bermudian Dollar' },
  { code: 'BND', label: 'BND - Brunei Dollar' },
  { code: 'BOB', label: 'BOB - Bolivian Boliviano' },
  { code: 'BRL', label: 'BRL - Brazilian Real' },
  { code: 'BSD', label: 'BSD - Bahamian Dollar' },
  { code: 'BTN', label: 'BTN - Bhutanese Ngultrum' },
  { code: 'BWP', label: 'BWP - Botswana Pula' },
  { code: 'BYN', label: 'BYN - Belarusian Ruble' },
  { code: 'BZD', label: 'BZD - Belize Dollar' },
  { code: 'CAD', label: 'CAD - Canadian Dollar' },
  { code: 'CDF', label: 'CDF - Congolese Franc' },
  { code: 'CHF', label: 'CHF - Swiss Franc' },
  { code: 'CLP', label: 'CLP - Chilean Peso' },
  { code: 'CNY', label: 'CNY - Chinese Yuan' },
  { code: 'COP', label: 'COP - Colombian Peso' },
  { code: 'CRC', label: 'CRC - Costa Rican Colón' },
  { code: 'CUP', label: 'CUP - Cuban Peso' },
  { code: 'CVE', label: 'CVE - Cape Verdean Escudo' },
  { code: 'CZK', label: 'CZK - Czech Koruna' },
  { code: 'DJF', label: 'DJF - Djiboutian Franc' },
  { code: 'DKK', label: 'DKK - Danish Krone' },
  { code: 'DOP', label: 'DOP - Dominican Peso' },
  { code: 'DZD', label: 'DZD - Algerian Dinar' },
  { code: 'EGP', label: 'EGP - Egyptian Pound' },
  { code: 'ERN', label: 'ERN - Eritrean Nakfa' },
  { code: 'ETB', label: 'ETB - Ethiopian Birr' },
  { code: 'EUR', label: 'EUR - Euro' },
  { code: 'FJD', label: 'FJD - Fijian Dollar' },
  { code: 'FKP', label: 'FKP - Falkland Islands Pound' },
  { code: 'GBP', label: 'GBP - British Pound' },
  { code: 'GEL', label: 'GEL - Georgian Lari' },
  { code: 'GHS', label: 'GHS - Ghanaian Cedi' },
  { code: 'GIP', label: 'GIP - Gibraltar Pound' },
  { code: 'GMD', label: 'GMD - Gambian Dalasi' },
  { code: 'GNF', label: 'GNF - Guinean Franc' },
  { code: 'GTQ', label: 'GTQ - Guatemalan Quetzal' },
  { code: 'GYD', label: 'GYD - Guyanese Dollar' },
  { code: 'HKD', label: 'HKD - Hong Kong Dollar' },
  { code: 'HNL', label: 'HNL - Honduran Lempira' },
  { code: 'HRK', label: 'HRK - Croatian Kuna' },
  { code: 'HTG', label: 'HTG - Haitian Gourde' },
  { code: 'HUF', label: 'HUF - Hungarian Forint' },
  { code: 'IDR', label: 'IDR - Indonesian Rupiah' },
  { code: 'ILS', label: 'ILS - Israeli New Shekel' },
  { code: 'INR', label: 'INR - Indian Rupee' },
  { code: 'IQD', label: 'IQD - Iraqi Dinar' },
  { code: 'IRR', label: 'IRR - Iranian Rial' },
  { code: 'ISK', label: 'ISK - Icelandic Króna' },
  { code: 'JMD', label: 'JMD - Jamaican Dollar' },
  { code: 'JOD', label: 'JOD - Jordanian Dinar' },
  { code: 'JPY', label: 'JPY - Japanese Yen' },
  { code: 'KES', label: 'KES - Kenyan Shilling' },
  { code: 'KGS', label: 'KGS - Kyrgyzstani Som' },
  { code: 'KHR', label: 'KHR - Cambodian Riel' },
  { code: 'KMF', label: 'KMF - Comorian Franc' },
  { code: 'KPW', label: 'KPW - North Korean Won' },
  { code: 'KRW', label: 'KRW - South Korean Won' },
  { code: 'KWD', label: 'KWD - Kuwaiti Dinar' },
  { code: 'KYD', label: 'KYD - Cayman Islands Dollar' },
  { code: 'KZT', label: 'KZT - Kazakhstani Tenge' },
  { code: 'LAK', label: 'LAK - Lao Kip' },
  { code: 'LBP', label: 'LBP - Lebanese Pound' },
  { code: 'LKR', label: 'LKR - Sri Lankan Rupee' },
  { code: 'LRD', label: 'LRD - Liberian Dollar' },
  { code: 'LSL', label: 'LSL - Lesotho Loti' },
  { code: 'LYD', label: 'LYD - Libyan Dinar' },
  { code: 'MAD', label: 'MAD - Moroccan Dirham' },
  { code: 'MDL', label: 'MDL - Moldovan Leu' },
  { code: 'MGA', label: 'MGA - Malagasy Ariary' },
  { code: 'MKD', label: 'MKD - Macedonian Denar' },
  { code: 'MMK', label: 'MMK - Myanmar Kyat' },
  { code: 'MNT', label: 'MNT - Mongolian Tögrög' },
  { code: 'MOP', label: 'MOP - Macanese Pataca' },
  { code: 'MRU', label: 'MRU - Mauritanian Ouguiya' },
  { code: 'MUR', label: 'MUR - Mauritian Rupee' },
  { code: 'MVR', label: 'MVR - Maldivian Rufiyaa' },
  { code: 'MWK', label: 'MWK - Malawian Kwacha' },
  { code: 'MXN', label: 'MXN - Mexican Peso' },
  { code: 'MYR', label: 'MYR - Malaysian Ringgit' },
  { code: 'MZN', label: 'MZN - Mozambican Metical' },
  { code: 'NAD', label: 'NAD - Namibian Dollar' },
  { code: 'NGN', label: 'NGN - Nigerian Naira' },
  { code: 'NIO', label: 'NIO - Nicaraguan Córdoba' },
  { code: 'NOK', label: 'NOK - Norwegian Krone' },
  { code: 'NPR', label: 'NPR - Nepalese Rupee' },
  { code: 'NZD', label: 'NZD - New Zealand Dollar' },
  { code: 'OMR', label: 'OMR - Omani Rial' },
  { code: 'PAB', label: 'PAB - Panamanian Balboa' },
  { code: 'PEN', label: 'PEN - Peruvian Sol' },
  { code: 'PGK', label: 'PGK - Papua New Guinean Kina' },
  { code: 'PHP', label: 'PHP - Philippine Peso' },
  { code: 'PKR', label: 'PKR - Pakistani Rupee' },
  { code: 'PLN', label: 'PLN - Polish Zloty' },
  { code: 'PYG', label: 'PYG - Paraguayan Guarani' },
  { code: 'QAR', label: 'QAR - Qatari Riyal' },
  { code: 'RON', label: 'RON - Romanian Leu' },
  { code: 'RSD', label: 'RSD - Serbian Dinar' },
  { code: 'RUB', label: 'RUB - Russian Ruble' },
  { code: 'RWF', label: 'RWF - Rwandan Franc' },
  { code: 'SAR', label: 'SAR - Saudi Riyal' },
  { code: 'SBD', label: 'SBD - Solomon Islands Dollar' },
  { code: 'SCR', label: 'SCR - Seychellois Rupee' },
  { code: 'SDG', label: 'SDG - Sudanese Pound' },
  { code: 'SEK', label: 'SEK - Swedish Krona' },
  { code: 'SGD', label: 'SGD - Singapore Dollar' },
  { code: 'SHP', label: 'SHP - Saint Helena Pound' },
  { code: 'SLL', label: 'SLL - Sierra Leonean Leone' },
  { code: 'SOS', label: 'SOS - Somali Shilling' },
  { code: 'SRD', label: 'SRD - Surinamese Dollar' },
  { code: 'SSP', label: 'SSP - South Sudanese Pound' },
  { code: 'STN', label: 'STN - São Tomé and Príncipe Dobra' },
  { code: 'SVC', label: 'SVC - Salvadoran Colón' },
  { code: 'SYP', label: 'SYP - Syrian Pound' },
  { code: 'SZL', label: 'SZL - Eswatini Lilangeni' },
  { code: 'THB', label: 'THB - Thai Baht' },
  { code: 'TJS', label: 'TJS - Tajikistani Somoni' },
  { code: 'TMT', label: 'TMT - Turkmenistani Manat' },
  { code: 'TND', label: 'TND - Tunisian Dinar' },
  { code: 'TOP', label: 'TOP - Tongan Paʻanga' },
  { code: 'TRY', label: 'TRY - Turkish Lira' },
  { code: 'TTD', label: 'TTD - Trinidad and Tobago Dollar' },
  { code: 'TWD', label: 'TWD - New Taiwan Dollar' },
  { code: 'TZS', label: 'TZS - Tanzanian Shilling' },
  { code: 'UAH', label: 'UAH - Ukrainian Hryvnia' },
  { code: 'UGX', label: 'UGX - Ugandan Shilling' },
  { code: 'USD', label: 'USD - US Dollar' },
  { code: 'UYU', label: 'UYU - Uruguayan Peso' },
  { code: 'UZS', label: 'UZS - Uzbekistani Som' },
  { code: 'VES', label: 'VES - Venezuelan Bolívar' },
  { code: 'VND', label: 'VND - Vietnamese Dong' },
  { code: 'VUV', label: 'VUV - Vanuatu Vatu' },
  { code: 'WST', label: 'WST - Samoan Tala' },
  { code: 'XAF', label: 'XAF - Central African CFA Franc' },
  { code: 'XCD', label: 'XCD - East Caribbean Dollar' },
  { code: 'XOF', label: 'XOF - West African CFA Franc' },
  { code: 'XPF', label: 'XPF - CFP Franc' },
  { code: 'YER', label: 'YER - Yemeni Rial' },
  { code: 'ZAR', label: 'ZAR - South African Rand' },
  { code: 'ZMW', label: 'ZMW - Zambian Kwacha' },
  { code: 'ZWL', label: 'ZWL - Zimbabwean Dollar' },
]

const requiredName = (value: string) => value.trim().length > 0 || t('crm.opportunities.form.requiredName')
const validCurrency = (value: string) => true

watch(
  () => props.opportunityId,
  async (opportunityId) => {
    await loadRecord(opportunityId)
  },
  { immediate: true },
)

async function loadRecord(opportunityId: string | null) {
  errorMessage.value = ''

  if (!opportunityId) {
    draft.name = ''
    draft.stage = ''
    draft.closeDate = ''
    draft.amount = null
    draft.currencyCode = 'USD'
    draft.companyId = props.initialCompanyId ?? null
    draft.pointOfContactId = null
    draft.ownerId = null
    return
  }

  try {
    const opportunity = await getCrmOpportunity(opportunityId)
    draft.name = opportunity.name
    draft.stage = opportunity.stage
    draft.closeDate = opportunity.closeDate ? opportunity.closeDate.slice(0, 10) : ''
    draft.amount = opportunity.amount ? parseFloat(opportunity.amount.replace(/[^0-9.-]/g, '')) : null
    draft.currencyCode = opportunity.currencyCode || 'USD'
    draft.companyId = opportunity.companyId || null
    draft.pointOfContactId = opportunity.pointOfContactId || null
    draft.ownerId = opportunity.ownerId || null
  } catch {
    errorMessage.value = t('crm.opportunities.messages.loadRecordFailed')
  }
}

function onCloseDatePicked(date: unknown) {
  if (date instanceof Date) {
    const y = date.getFullYear()
    const m = String(date.getMonth() + 1).padStart(2, '0')
    const d = String(date.getDate()).padStart(2, '0')
    draft.closeDate = `${y}-${m}-${d}`
  }
  closeDatePickerOpen.value = false
}

async function handleSave(closeAfter = false) {
  if (!draft.name.trim()) {
    errorMessage.value = t('crm.opportunities.form.requiredName')
    return
  }

  saving.value = true
  errorMessage.value = ''

  const payload = {
    name: draft.name.trim(),
    stage: draft.stage.trim(),
    closeDate: draft.closeDate.trim() || null,
    amount: draft.amount,
    currencyCode: draft.currencyCode,
    companyId: draft.companyId,
    pointOfContactId: draft.pointOfContactId,
    ownerId: draft.ownerId,
  }

  try {
    const result = props.opportunityId
      ? await updateCrmOpportunity(props.opportunityId, payload)
      : await createCrmOpportunity(payload)

    emit('saved', result)

    if (closeAfter) {
      emit('cancel')
    }
  } catch (err) {
    const axiosErr = err as { response?: { data?: { message?: string } } }
    const serverMsg = axiosErr.response?.data?.message
    errorMessage.value = serverMsg || t('crm.opportunities.messages.saveFailed')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.currency-select {
  min-width: 220px;
  max-width: 220px;
}
</style>
