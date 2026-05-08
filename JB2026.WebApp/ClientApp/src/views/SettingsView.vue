<template>
  <section class="page-section settings-page">
    <v-card rounded="xl" elevation="0" class="panel-card settings-card">

      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>
        <v-alert v-if="savedMessage" type="success" variant="tonal" class="mb-3">{{ savedMessage }}</v-alert>

        <v-form @submit.prevent="save" class="settings-form">
          <div class="field-grid">
            <label class="field-label" for="owner-name">{{ t('settings.fields.ownerName') }}</label>
            <v-text-field
              id="owner-name"
              v-model="model.ownerName"
              variant="outlined"
              density="compact"
              hide-details
            />

            <label class="field-label" for="next-order-number">{{ t('settings.fields.nextOrderNumber') }}</label>
            <v-text-field
              id="next-order-number"
              v-model="model.nextOrderNumber"
              variant="outlined"
              density="compact"
              hide-details
            />

            <label class="field-label" for="next-product-number">{{ t('settings.fields.nextProductNumber') }}</label>
            <v-text-field
              id="next-product-number"
              v-model="model.nextProductNumber"
              variant="outlined"
              density="compact"
              hide-details
            />

            <label class="field-label" for="next-quotation-number">{{ t('settings.fields.nextQuotationNumber') }}</label>
            <v-text-field
              id="next-quotation-number"
              v-model="model.nextQuotationNumber"
              variant="outlined"
              density="compact"
              hide-details
            />

            <label class="field-label" for="common-query">{{ t('settings.fields.commonQuery') }}</label>
            <v-select
              id="common-query"
              v-model="model.commonQueryIndex"
              :items="commonQueryOptions"
              item-title="label"
              item-value="value"
              variant="outlined"
              density="compact"
              hide-details
            />

            <label class="field-label" for="completed-query">{{ t('settings.fields.completedQuery') }}</label>
            <v-select
              id="completed-query"
              v-model="model.completedQueryIndex"
              :items="completedQueryOptions"
              item-title="label"
              item-value="value"
              variant="outlined"
              density="compact"
              hide-details
            />

            <label class="field-label" for="schedule-range">{{ t('settings.fields.scheduleQueryRange') }}</label>
            <div class="d-flex align-center ga-2">
              <v-text-field
                id="schedule-range"
                v-model.number="model.scheduleQueryRange"
                type="number"
                min="1"
                variant="outlined"
                density="compact"
                hide-details
                class="range-input"
              />
              <span class="text-body-2">{{ t('settings.fields.daysUnit') }}</span>
            </div>

            <label class="field-label" for="gmail-account">{{ t('settings.fields.gmailAccount') }}</label>
            <v-text-field
              id="gmail-account"
              v-model="model.gmailAccount"
              variant="outlined"
              density="compact"
              hide-details
            />

            <label class="field-label" for="gmail-password">{{ t('settings.fields.gmailPassword') }}</label>
            <v-text-field
              id="gmail-password"
              v-model="model.gmailPassword"
              variant="outlined"
              density="compact"
              hide-details
            />

            <v-divider class="my-2" style="grid-column: 1 / -1" />

            <label class="field-label" for="date-format">{{ t('settings.fields.dateFormat') }}</label>
            <v-select
              id="date-format"
              v-model="currentFormat"
              :items="dateFormatOptions"
              item-title="label"
              item-value="value"
              variant="outlined"
              density="compact"
              hide-details
            />
          </div>

          <div class="mt-4">
            <v-btn color="primary" type="submit" :loading="loading">{{ t('settings.actions.save') }}</v-btn>
          </div>
        </v-form>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { getSettings, updateSettings } from '@/services/settings'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import type { AppSettings } from '@/types/api'

const loading = ref(false)
const errorMessage = ref('')
const savedMessage = ref('')
const { t } = useI18n({ useScope: 'global' })
const { currentFormat, DATE_FORMATS } = useGlobalDateFormatter()

const model = ref<AppSettings>({
  companyName: 'JB2026 Printing',
  timeZone: 'Asia/Kuala_Lumpur',
  currencyCode: 'MYR',
  enableLegacyFallback: true,
  ownerName: '',
  nextOrderNumber: '',
  nextProductNumber: '',
  nextQuotationNumber: '',
  commonQueryIndex: 0,
  completedQueryIndex: 0,
  scheduleQueryRange: 1,
  gmailAccount: '',
  gmailPassword: '',
  dateFormatPreference: DATE_FORMATS.SHORT_DATE,
})

const dateFormatOptions = computed(() => [
  { value: DATE_FORMATS.SHORT_DATE, label: t('settings.dateFormatOptions.shortDate') },
  { value: DATE_FORMATS.SHORT_DATETIME, label: t('settings.dateFormatOptions.shortDateTime') },
  { value: DATE_FORMATS.LONG_DATE, label: t('settings.dateFormatOptions.longDate') },
  { value: DATE_FORMATS.LONG_DATETIME, label: t('settings.dateFormatOptions.longDateTime') },
  { value: DATE_FORMATS.ISO_DATE, label: t('settings.dateFormatOptions.isoDate') },
  { value: DATE_FORMATS.ISO_DATETIME, label: t('settings.dateFormatOptions.isoDateTime') },
])

const commonQueryOptions = computed(() => [
  { value: 0, label: t('settings.commonQueryOptions.none') },
  { value: 1, label: t('settings.commonQueryOptions.ordered7') },
  { value: 2, label: t('settings.commonQueryOptions.ordered30') },
  { value: 3, label: t('settings.commonQueryOptions.ordered90') },
])

const completedQueryOptions = computed(() => [
  { value: 0, label: t('settings.completedQueryOptions.none') },
  { value: 1, label: t('settings.completedQueryOptions.completed7') },
  { value: 2, label: t('settings.completedQueryOptions.completed30') },
  { value: 3, label: t('settings.completedQueryOptions.completed90') },
])

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''
  savedMessage.value = ''
  try {
    model.value = await getSettings()
    if (dateFormatOptions.value.some((option) => option.value === model.value.dateFormatPreference)) {
      currentFormat.value = model.value.dateFormatPreference as typeof currentFormat.value
    }
  } catch {
    errorMessage.value = t('settings.messages.loadFailed')
  } finally {
    loading.value = false
  }
}

async function save() {
  if (!Number.isFinite(model.value.scheduleQueryRange) || model.value.scheduleQueryRange <= 0) {
    errorMessage.value = t('settings.messages.scheduleRangeInvalid')
    savedMessage.value = ''
    return
  }

  loading.value = true
  errorMessage.value = ''
  savedMessage.value = ''

  try {
    model.value.dateFormatPreference = currentFormat.value
    model.value = await updateSettings(model.value)
    if (dateFormatOptions.value.some((option) => option.value === model.value.dateFormatPreference)) {
      currentFormat.value = model.value.dateFormatPreference as typeof currentFormat.value
    }
    savedMessage.value = t('settings.messages.saveSuccess')
  } catch {
    errorMessage.value = t('settings.messages.saveFailed')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.settings-page {
  min-height: 0;
}

.settings-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
  background: linear-gradient(180deg, rgba(224, 237, 255, 0.92), rgba(241, 247, 255, 0.96));
}

.settings-form {
  max-width: 720px;
}

.field-grid {
  display: grid;
  grid-template-columns: 180px minmax(220px, 1fr);
  gap: 10px 16px;
  align-items: center;
}

.field-label {
  font-size: 15px;
  color: rgba(var(--v-theme-on-surface), 0.88);
}

.range-input {
  max-width: 120px;
}

@media (max-width: 900px) {
  .field-grid {
    grid-template-columns: 1fr;
    gap: 6px;
  }
}
</style>