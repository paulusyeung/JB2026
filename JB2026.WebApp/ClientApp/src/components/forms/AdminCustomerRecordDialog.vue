<template>
  <v-card class="customer-record-dialog">
    <v-card-title class="d-flex align-center ga-3 pb-2">
      <div>
        <h2 class="text-h6 mb-1">
          {{ isNew ? t('admin.customer.form.newTitle') : t('admin.customer.form.editTitle') }}
        </h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          {{ isNew ? t('admin.customer.actions.newCustomer') : draft.customerName || '-' }}
        </p>
      </div>
      <v-spacer />
      <v-btn variant="tonal" icon="mdi-close" @click="emit('cancel')" />
    </v-card-title>

    <v-card-text class="pt-2">
      <div class="d-flex flex-wrap ga-2 mb-4">
        <v-btn size="small" color="primary" prepend-icon="mdi-content-save" :loading="saving" @click="handleSave()">
          {{ t('admin.customer.form.save') }}
        </v-btn>
        <v-btn size="small" variant="tonal" prepend-icon="mdi-content-save-check" :loading="saving" @click="handleSave(true)">
          {{ t('admin.customer.form.saveClose') }}
        </v-btn>
        <v-btn
          size="small"
          variant="outlined"
          prepend-icon="mdi-delete"
          :loading="deleting"
          :disabled="isNew"
          @click="handleDelete"
        >
          {{ t('admin.customer.form.delete') }}
        </v-btn>
      </div>

      <v-row dense>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="draft.customerName"
            :label="t('admin.customer.form.customerName')"
            variant="outlined"
            density="compact"
            maxlength="64"
            :rules="[requiredName]"
          />
        </v-col>
        <v-col cols="12" md="3">
          <v-text-field
            v-model="draft.customerCode"
            :label="t('admin.customer.form.customerCode')"
            variant="outlined"
            density="compact"
            maxlength="64"
            @update:model-value="val => draft.customerCode = String(val ?? '').toUpperCase()"
          />
        </v-col>
        <v-col cols="12" md="3">
          <v-select
            v-model="draft.group"
            :items="groupOptions"
            item-title="title"
            item-value="value"
            :label="t('admin.customer.form.group')"
            variant="outlined"
            density="compact"
            clearable
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12">
          <v-textarea
            v-model="draft.billTo"
            :label="t('admin.customer.form.billTo')"
            variant="outlined"
            density="compact"
            rows="4"
            auto-grow
            maxlength="4000"
          />
        </v-col>
      </v-row>

      <v-divider class="my-2" />

      <v-row dense align="center">
        <v-col cols="12" md="6" class="d-flex align-center">
          <v-select
            v-model="selectedShipToName"
            :items="shipToNameOptions"
            :label="t('admin.customer.form.shipToName')"
            variant="outlined"
            density="compact"
            clearable
            hide-details
          />
        </v-col>
        <v-col cols="12" md="6" class="d-flex align-center ga-2">
          <v-btn size="small" variant="outlined" prepend-icon="mdi-plus" @click="addShipToEntry">
            {{ t('admin.customer.form.addShipTo') }}
          </v-btn>
          <v-btn
            size="small"
            variant="outlined"
            color="error"
            prepend-icon="mdi-delete"
            :disabled="!selectedShipToName"
            @click="deleteShipToEntry"
          >
            {{ t('admin.customer.form.deleteShipTo') }}
          </v-btn>
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12">
          <v-text-field
            v-model="shipToDraftName"
            :label="t('admin.customer.form.shipToName')"
            variant="outlined"
            density="compact"
            maxlength="128"
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12">
          <v-textarea
            v-model="shipToDraftAddress"
            :label="t('admin.customer.form.shipToAddress')"
            variant="outlined"
            density="compact"
            rows="4"
            auto-grow
            maxlength="4000"
          />
        </v-col>
      </v-row>

      <div class="d-flex justify-end">
        <v-btn size="small" variant="outlined" prepend-icon="mdi-content-save" @click="saveShipToEntry">
          {{ t('admin.customer.form.saveShipTo') }}
        </v-btn>
      </div>
    </v-card-text>

    <v-alert v-if="errorMessage" type="error" variant="tonal" class="mx-6 mb-2">
      {{ errorMessage }}
    </v-alert>

    <v-card-actions class="pa-4 d-flex ga-2 responsive-dialog-actions">
      <v-spacer />
      <v-btn variant="text" :disabled="saving || deleting" @click="emit('cancel')">
        {{ t('admin.customer.form.cancel') }}
      </v-btn>
    </v-card-actions>
  </v-card>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createAdminCustomer,
  deleteAdminCustomer,
  getAdminCustomer,
  getAdminCustomers,
  updateAdminCustomer,
} from '@/services/admin'
import { listBillingGroups } from '@/services/billing'
import type { AdminCustomerRecord, CustomerShipToAddress } from '@/types/api'

const props = defineProps<{
  customerId: string | null
}>()

const emit = defineEmits<{
  (e: 'saved', customer: AdminCustomerRecord): void
  (e: 'deleted', id: string): void
  (e: 'cancel'): void
}>()

const { t } = useI18n({ useScope: 'global' })

const saving = ref(false)
const deleting = ref(false)
const errorMessage = ref('')

const selectedShipToName = ref<string | null>(null)
const shipToDraftName = ref('')
const shipToDraftAddress = ref('')

const groupOptions = ref<{ title: string; value: string }[]>([])

const draft = reactive({
  customerName: '',
  customerCode: '',
  loginAccount: '',
  loginPassword: '',
  billTo: '',
  group: '',
  shipToAddresses: [] as CustomerShipToAddress[],
})

const isNew = computed(() => !props.customerId)

const shipToNameOptions = computed(() => draft.shipToAddresses.map((entry) => entry.name))

const requiredName = (value: string) => value.trim().length > 0 || t('admin.customer.form.requiredCustomerName')

onMounted(async () => {
  try {
    const groups = await listBillingGroups()
    groupOptions.value = groups.map((group) => ({
      title: group.name,
      value: group.externalGroupId,
    }))
  } catch {
    // Groups are optional; silently ignore fetch failures.
  }
})

watch(
  () => props.customerId,
  async (customerId) => {
    await loadRecord(customerId)
  },
  { immediate: true },
)

watch(selectedShipToName, (name) => {
  if (!name) {
    shipToDraftName.value = ''
    shipToDraftAddress.value = ''
    return
  }

  const entry = draft.shipToAddresses.find((item) => item.name === name)
  shipToDraftName.value = entry?.name ?? name
  shipToDraftAddress.value = entry?.address ?? ''
})

async function loadRecord(customerId: string | null) {
  errorMessage.value = ''

  if (!customerId) {
    draft.customerName = ''
    draft.customerCode = ''
    draft.loginAccount = ''
    draft.loginPassword = ''
    draft.billTo = ''
    draft.group = ''
    draft.shipToAddresses = []
    selectedShipToName.value = null
    shipToDraftName.value = ''
    shipToDraftAddress.value = ''
    return
  }

  try {
    const customer = await getAdminCustomer(customerId)
    draft.customerName = customer.customerName
    draft.customerCode = customer.customerCode
    draft.loginAccount = customer.loginAccount
    draft.loginPassword = customer.loginPassword
    draft.billTo = customer.billTo
    draft.group = customer.group
    draft.shipToAddresses = (customer.shipToAddresses ?? []).map((entry) => ({ ...entry }))

    const firstShipToEntry = draft.shipToAddresses[0]
    if (firstShipToEntry) {
      selectedShipToName.value = firstShipToEntry.name
    } else {
      selectedShipToName.value = null
      shipToDraftName.value = ''
      shipToDraftAddress.value = ''
    }
  } catch {
    errorMessage.value = t('admin.customer.messages.loadRecordFailed')
  }
}

function addShipToEntry() {
  const baseName = t('admin.customer.form.newShipToName')
  let nextName = baseName
  let suffix = 1

  while (draft.shipToAddresses.some((entry) => entry.name === nextName)) {
    suffix += 1
    nextName = `${baseName} ${suffix}`
  }

  draft.shipToAddresses = [
    ...draft.shipToAddresses,
    {
      name: nextName,
      address: '',
    },
  ]

  selectedShipToName.value = nextName
}

function saveShipToEntry() {
  const name = shipToDraftName.value.trim()
  const address = shipToDraftAddress.value.trim()

  if (!name) {
    errorMessage.value = t('admin.customer.form.requiredShipToName')
    return
  }

  const existingIndex = draft.shipToAddresses.findIndex((entry) => entry.name === (selectedShipToName.value ?? ''))

  if (existingIndex >= 0) {
    const conflictIndex = draft.shipToAddresses.findIndex((entry, index) => index !== existingIndex && entry.name === name)
    if (conflictIndex >= 0) {
      errorMessage.value = t('admin.customer.form.duplicateShipToName')
      return
    }

    const next = [...draft.shipToAddresses]
    next[existingIndex] = { name, address }
    draft.shipToAddresses = next
    selectedShipToName.value = name
  } else {
    if (draft.shipToAddresses.some((entry) => entry.name === name)) {
      errorMessage.value = t('admin.customer.form.duplicateShipToName')
      return
    }

    draft.shipToAddresses = [...draft.shipToAddresses, { name, address }]
    selectedShipToName.value = name
  }

  errorMessage.value = ''
}

function deleteShipToEntry() {
  if (!selectedShipToName.value) {
    return
  }

  draft.shipToAddresses = draft.shipToAddresses.filter((entry) => entry.name !== selectedShipToName.value)

  const firstShipToEntry = draft.shipToAddresses[0]
  if (firstShipToEntry) {
    selectedShipToName.value = firstShipToEntry.name
  } else {
    selectedShipToName.value = null
    shipToDraftName.value = ''
    shipToDraftAddress.value = ''
  }
}

async function handleSave(closeAfter = false) {
  if (!draft.customerName.trim()) {
    errorMessage.value = t('admin.customer.form.requiredCustomerName')
    return
  }

  const invalidShipTo = draft.shipToAddresses.find((entry) => !entry.name.trim())
  if (invalidShipTo) {
    errorMessage.value = t('admin.customer.form.requiredShipToName')
    return
  }

  saving.value = true
  errorMessage.value = ''

  try {
    const customerCode = draft.customerCode.trim()
    if (customerCode) {
      const customers = await getAdminCustomers({ take: 1000 })
      const isTaken = customers.some(
        (customer) =>
          customer.customerId !== props.customerId &&
          customer.customerCode.trim().toUpperCase() === customerCode.toUpperCase(),
      )
      if (isTaken) {
        errorMessage.value = t('admin.customer.messages.duplicateCustomerCode')
        return
      }
    }

    const request = {
      customerName: draft.customerName.trim(),
      loginAccount: draft.loginAccount.trim(),
      loginPassword: draft.loginPassword.trim(),
      customerCode: draft.customerCode.trim(),
      billTo: draft.billTo.trim(),
      group: draft.group.trim(),
      shipToAddresses: draft.shipToAddresses
        .map((entry) => ({
          name: entry.name.trim(),
          address: entry.address.trim(),
        }))
        .filter((entry) => entry.name.length > 0 || entry.address.length > 0),
    }

    const result = isNew.value
      ? await createAdminCustomer(request)
      : await updateAdminCustomer(props.customerId!, request)

    emit('saved', result)

    if (closeAfter) {
      emit('cancel')
    }
  } catch {
    errorMessage.value = t('admin.customer.messages.saveFailed')
  } finally {
    saving.value = false
  }
}

async function handleDelete() {
  if (!props.customerId) {
    return
  }

  if (!window.confirm(t('admin.customer.messages.deleteConfirm'))) {
    return
  }

  deleting.value = true
  errorMessage.value = ''

  try {
    await deleteAdminCustomer(props.customerId)
    emit('deleted', props.customerId)
    emit('cancel')
  } catch {
    errorMessage.value = t('admin.customer.messages.deleteFailed')
  } finally {
    deleting.value = false
  }
}
</script>
