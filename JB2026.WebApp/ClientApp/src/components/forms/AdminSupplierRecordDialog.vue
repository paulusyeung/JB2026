<template>
  <v-card class="supplier-record-dialog">
    <v-card-title class="d-flex align-center ga-3 pb-2">
      <div>
        <h2 class="text-h6 mb-1">
          {{ isNew ? t('admin.supplier.form.newTitle') : t('admin.supplier.form.editTitle') }}
        </h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          {{ isNew ? t('admin.supplier.actions.newSupplier') : draft.supplierName || '-' }}
        </p>
      </div>
      <v-spacer />
      <v-btn variant="text" icon="mdi-close" @click="emit('cancel')" />
    </v-card-title>

    <v-card-text class="pt-2">
      <div class="d-flex flex-wrap ga-2 mb-4">
        <v-btn size="small" color="primary" prepend-icon="mdi-content-save" :loading="saving" @click="handleSave()">
          {{ t('admin.supplier.form.save') }}
        </v-btn>
        <v-btn size="small" variant="tonal" prepend-icon="mdi-content-save-check" :loading="saving" @click="handleSave(true)">
          {{ t('admin.supplier.form.saveClose') }}
        </v-btn>
        <v-btn
          size="small"
          variant="outlined"
          prepend-icon="mdi-delete"
          :loading="deleting"
          :disabled="isNew"
          @click="handleDelete"
        >
          {{ t('admin.supplier.form.delete') }}
        </v-btn>
      </div>

      <v-row dense>
        <v-col cols="12" md="8">
          <v-text-field
            v-model="draft.supplierName"
            :label="t('admin.supplier.form.supplierName')"
            variant="outlined"
            density="compact"
            maxlength="64"
            :rules="[requiredName]"
          />
        </v-col>
        <v-col cols="12" md="4">
          <v-text-field
            v-model="draft.supplierCode"
            :label="t('admin.supplier.form.supplierCode')"
            variant="outlined"
            density="compact"
            maxlength="64"
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12">
          <v-textarea
            v-model="draft.billTo"
            :label="t('admin.supplier.form.billTo')"
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
            :label="t('admin.supplier.form.shipToName')"
            variant="outlined"
            density="compact"
            clearable
            hide-details
          />
        </v-col>
        <v-col cols="12" md="6" class="d-flex align-center ga-2">
          <v-btn size="small" variant="outlined" prepend-icon="mdi-plus" @click="addShipToEntry">
            {{ t('admin.supplier.form.addShipTo') }}
          </v-btn>
          <v-btn
            size="small"
            variant="outlined"
            color="error"
            prepend-icon="mdi-delete"
            :disabled="!selectedShipToName"
            @click="deleteShipToEntry"
          >
            {{ t('admin.supplier.form.deleteShipTo') }}
          </v-btn>
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12">
          <v-text-field
            v-model="shipToDraftName"
            :label="t('admin.supplier.form.shipToName')"
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
            :label="t('admin.supplier.form.shipToAddress')"
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
          {{ t('admin.supplier.form.saveShipTo') }}
        </v-btn>
      </div>
    </v-card-text>

    <v-alert v-if="errorMessage" type="error" variant="tonal" class="mx-6 mb-2">
      {{ errorMessage }}
    </v-alert>

    <v-card-actions class="pa-4 d-flex ga-2 responsive-dialog-actions">
      <v-spacer />
      <v-btn variant="text" :disabled="saving || deleting" @click="emit('cancel')">
        {{ t('admin.supplier.form.cancel') }}
      </v-btn>
    </v-card-actions>
  </v-card>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createAdminSupplier,
  deleteAdminSupplier,
  getAdminSupplier,
  updateAdminSupplier,
} from '@/services/admin'
import type { AdminSupplierRecord, SupplierShipToAddress } from '@/types/api'

const props = defineProps<{
  supplierId: string | null
}>()

const emit = defineEmits<{
  (e: 'saved', supplier: AdminSupplierRecord): void
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

const draft = reactive({
  supplierName: '',
  supplierCode: '',
  loginAccount: '',
  loginPassword: '',
  billTo: '',
  shipToAddresses: [] as SupplierShipToAddress[],
})

const isNew = computed(() => !props.supplierId)

const shipToNameOptions = computed(() => draft.shipToAddresses.map((entry) => entry.name))

const requiredName = (value: string) => value.trim().length > 0 || t('admin.supplier.form.requiredSupplierName')

watch(
  () => props.supplierId,
  async (supplierId) => {
    await loadRecord(supplierId)
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

async function loadRecord(supplierId: string | null) {
  errorMessage.value = ''

  if (!supplierId) {
    draft.supplierName = ''
    draft.supplierCode = ''
    draft.loginAccount = ''
    draft.loginPassword = ''
    draft.billTo = ''
    draft.shipToAddresses = []
    selectedShipToName.value = null
    shipToDraftName.value = ''
    shipToDraftAddress.value = ''
    return
  }

  try {
    const supplier = await getAdminSupplier(supplierId)
    draft.supplierName = supplier.supplierName
    draft.supplierCode = supplier.supplierCode
    draft.loginAccount = supplier.loginAccount
    draft.loginPassword = supplier.loginPassword
    draft.billTo = supplier.billTo
    draft.shipToAddresses = supplier.shipToAddresses.map((entry) => ({ ...entry }))

    const firstShipToEntry = draft.shipToAddresses[0]
    if (firstShipToEntry) {
      selectedShipToName.value = firstShipToEntry.name
    } else {
      selectedShipToName.value = null
      shipToDraftName.value = ''
      shipToDraftAddress.value = ''
    }
  } catch {
    errorMessage.value = t('admin.supplier.messages.loadRecordFailed')
  }
}

function addShipToEntry() {
  const baseName = t('admin.supplier.form.newShipToName')
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
    errorMessage.value = t('admin.supplier.form.requiredShipToName')
    return
  }

  const existingIndex = draft.shipToAddresses.findIndex((entry) => entry.name === (selectedShipToName.value ?? ''))

  if (existingIndex >= 0) {
    const conflictIndex = draft.shipToAddresses.findIndex((entry, index) => index !== existingIndex && entry.name === name)
    if (conflictIndex >= 0) {
      errorMessage.value = t('admin.supplier.form.duplicateShipToName')
      return
    }

    const next = [...draft.shipToAddresses]
    next[existingIndex] = { name, address }
    draft.shipToAddresses = next
    selectedShipToName.value = name
  } else {
    if (draft.shipToAddresses.some((entry) => entry.name === name)) {
      errorMessage.value = t('admin.supplier.form.duplicateShipToName')
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
  if (!draft.supplierName.trim()) {
    errorMessage.value = t('admin.supplier.form.requiredSupplierName')
    return
  }

  const invalidShipTo = draft.shipToAddresses.find((entry) => !entry.name.trim())
  if (invalidShipTo) {
    errorMessage.value = t('admin.supplier.form.requiredShipToName')
    return
  }

  saving.value = true
  errorMessage.value = ''

  try {
    const request = {
      supplierName: draft.supplierName.trim(),
      loginAccount: draft.loginAccount.trim(),
      loginPassword: draft.loginPassword.trim(),
      supplierCode: draft.supplierCode.trim(),
      billTo: draft.billTo.trim(),
      shipToAddresses: draft.shipToAddresses
        .map((entry) => ({
          name: entry.name.trim(),
          address: entry.address.trim(),
        }))
        .filter((entry) => entry.name.length > 0 || entry.address.length > 0),
    }

    const result = isNew.value
      ? await createAdminSupplier(request)
      : await updateAdminSupplier(props.supplierId!, request)

    emit('saved', result)

    if (closeAfter) {
      emit('cancel')
    }
  } catch {
    errorMessage.value = t('admin.supplier.messages.saveFailed')
  } finally {
    saving.value = false
  }
}

async function handleDelete() {
  if (!props.supplierId) {
    return
  }

  if (!window.confirm(t('admin.supplier.messages.deleteConfirm'))) {
    return
  }

  deleting.value = true
  errorMessage.value = ''

  try {
    await deleteAdminSupplier(props.supplierId)
    emit('deleted', props.supplierId)
    emit('cancel')
  } catch {
    errorMessage.value = t('admin.supplier.messages.deleteFailed')
  } finally {
    deleting.value = false
  }
}
</script>
