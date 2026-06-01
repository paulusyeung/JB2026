<template>
  <v-dialog
    :model-value="modelValue"
    max-width="520"
    scrollable
    persistent
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card>
      <v-card-title class="d-flex align-center ga-2">
        <span class="text-h6">{{ t('billing.statement.dialog.title') }}</span>
        <v-spacer />
        <v-btn
          variant="text"
          icon="mdi-close"
          size="small"
          :aria-label="t('billing.statement.dialog.actions.close')"
          @click="emit('update:modelValue', false)"
        />
      </v-card-title>

      <v-divider />

      <v-card-text class="pt-4">
        <p class="text-body-2 text-medium-emphasis mb-4">
          {{ t('billing.statement.dialog.subtitle', { client: clientName || t('billing.statement.labels.empty') }) }}
        </p>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" density="compact" class="mb-4">
          {{ errorMessage }}
        </v-alert>

        <v-select
          v-model="form.dateRangePreset"
          :items="dateRangeOptions"
          item-title="label"
          item-value="value"
          :label="t('billing.statement.dialog.fields.dateRange')"
          density="compact"
          variant="outlined"
          class="mb-4"
        />

        <v-select
          v-model="form.status"
          :items="statusOptions"
          item-title="label"
          item-value="value"
          :label="t('billing.statement.dialog.fields.status')"
          density="compact"
          variant="outlined"
          class="mb-4"
        />

        <div class="text-subtitle-2 mb-2">{{ t('billing.statement.dialog.fields.selectors') }}</div>
        <div class="statement-request-dialog__selectors">
          <v-checkbox
            v-model="form.includeCredits"
            :label="t('billing.statement.dialog.selectors.credits')"
            density="compact"
            hide-details
          />
          <v-checkbox
            v-model="form.includePayments"
            :label="t('billing.statement.dialog.selectors.payments')"
            density="compact"
            hide-details
          />
          <v-checkbox
            v-model="form.includeAging"
            :label="t('billing.statement.dialog.selectors.aging')"
            density="compact"
            hide-details
          />
        </div>
      </v-card-text>

      <v-divider />

      <v-card-actions class="px-4 py-3">
        <v-spacer />
        <v-btn variant="text" @click="emit('update:modelValue', false)">
          {{ t('billing.statement.dialog.actions.cancel') }}
        </v-btn>
        <v-btn color="primary" variant="elevated" :loading="submitting" @click="submit">
          {{ t('billing.statement.dialog.actions.proceed') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  BILLING_STATEMENT_DATE_RANGE_PRESETS,
  BILLING_STATEMENT_STATUSES,
  type BillingStatementLaunchRequest,
} from '@/services/billing'

const props = defineProps<{
  modelValue: boolean
  clientName: string
  submitting: boolean
  errorMessage: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'submit', value: BillingStatementLaunchRequest): void
}>()

const { t } = useI18n({ useScope: 'global' })

const form = reactive<BillingStatementLaunchRequest>({
  externalClientId: '',
  dateRangePreset: BILLING_STATEMENT_DATE_RANGE_PRESETS.allOutstanding,
  status: BILLING_STATEMENT_STATUSES.all,
  includeCredits: false,
  includePayments: false,
  includeAging: true,
})

const dateRangeOptions = computed(() => [
  { label: t('billing.statement.dialog.presets.allOutstanding'), value: BILLING_STATEMENT_DATE_RANGE_PRESETS.allOutstanding },
  { label: t('billing.statement.dialog.presets.thisMonth'), value: BILLING_STATEMENT_DATE_RANGE_PRESETS.thisMonth },
  { label: t('billing.statement.dialog.presets.lastMonth'), value: BILLING_STATEMENT_DATE_RANGE_PRESETS.lastMonth },
  { label: t('billing.statement.dialog.presets.thisQuarter'), value: BILLING_STATEMENT_DATE_RANGE_PRESETS.thisQuarter },
  { label: t('billing.statement.dialog.presets.thisYear'), value: BILLING_STATEMENT_DATE_RANGE_PRESETS.thisYear },
])

const statusOptions = computed(() => [
  { label: t('billing.statement.dialog.presets.all'), value: BILLING_STATEMENT_STATUSES.all },
  { label: t('billing.statement.dialog.presets.paid'), value: BILLING_STATEMENT_STATUSES.paid },
  { label: t('billing.statement.dialog.presets.unpaid'), value: BILLING_STATEMENT_STATUSES.unpaid },
])

watch(
  () => props.modelValue,
  (open) => {
    if (!open) {
      return
    }

    form.externalClientId = ''
    form.dateRangePreset = BILLING_STATEMENT_DATE_RANGE_PRESETS.allOutstanding
    form.status = BILLING_STATEMENT_STATUSES.all
    form.includeCredits = false
    form.includePayments = false
    form.includeAging = true
  },
)

function submit() {
  emit('submit', { ...form })
}
</script>

<style scoped>
.statement-request-dialog__selectors {
  display: grid;
  gap: 0.25rem;
}
</style>