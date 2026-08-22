<template>
  <v-card v-draggable-dialog class="billing-client-record-dialog">
    <v-card-title class="d-flex align-center ga-3 pb-2">
      <div>
        <h2 class="text-h6 mb-1">
          {{ isRecordMode ? t('billing.clients.form.editTitle') : t('billing.clients.form.newTitle') }}
        </h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          {{ selectedCustomerName || '-' }}
        </p>
      </div>
      <v-spacer />
      <v-btn variant="tonal" icon="mdi-close" :disabled="migrating" @click="emit('cancel')" />
    </v-card-title>

    <v-card-text class="pt-2">
      <v-sheet v-if="!isRecordMode" rounded="lg" border class="pa-4 mb-4 migrate-sheet">
        <div class="text-subtitle-2 font-weight-bold mb-3">
          {{ t('billing.clients.form.migrateCustomer') }}
        </div>
        <v-row dense>
          <v-col cols="12">
            <v-autocomplete
              v-model="selectedMigrateCustomerId"
              v-model:search="migrateSearchText"
              :items="migratableCustomers"
              item-title="customerName"
              item-value="customerId"
              :label="t('billing.clients.form.migrateCustomerSelect')"
              variant="outlined"
              density="compact"
              clearable
              hide-no-data
              no-filter
              :loading="loadingMigrate"
              @update:model-value="onMigrateCustomerSelected"
              @update:search="handleMigrateSearch"
            >
              <template #item="{ props: itemProps, item }">
                <v-list-item v-bind="itemProps">
                  <template #prepend>
                    <v-icon
                      size="16"
                      :style="{ color: isSynced(item.raw) ? 'rgb(var(--v-theme-primary))' : 'rgb(var(--v-theme-on-surface-variant))' }"
                    >mdi-connection</v-icon>
                  </template>
                </v-list-item>
              </template>
            </v-autocomplete>
          </v-col>
        </v-row>
      </v-sheet>

      <template v-if="selectedCustomer">
        <v-alert v-if="selectedCustomerBillingSynced" type="info" variant="tonal" class="mb-3">
          {{ t('billing.clients.form.alreadySyncedNotice') }}
        </v-alert>

        <v-row dense>
          <v-col cols="12" md="6">
            <v-text-field
              :model-value="draft.customerName"
              :label="t('billing.clients.form.customerName')"
              variant="outlined"
              density="compact"
              readonly
            />
          </v-col>
          <v-col cols="12" md="3">
            <v-text-field
              :model-value="draft.customerCode"
              :label="t('billing.clients.form.customerCode')"
              variant="outlined"
              density="compact"
              readonly
            />
          </v-col>
          <v-col cols="12" md="3">
            <v-text-field
              :model-value="groupName"
              :label="t('billing.clients.form.group')"
              variant="outlined"
              density="compact"
              readonly
            />
          </v-col>
        </v-row>

        <v-row dense>
          <v-col cols="12">
            <v-textarea
              :model-value="draft.billTo"
              :label="t('billing.clients.form.billTo')"
              variant="outlined"
              density="compact"
              rows="4"
              auto-grow
              readonly
            />
          </v-col>
        </v-row>

        <v-row dense>
          <v-col cols="12">
            <v-textarea
              :model-value="shipToText"
              :label="t('billing.clients.form.shipToAddress')"
              variant="outlined"
              density="compact"
              rows="4"
              auto-grow
              readonly
            />
          </v-col>
        </v-row>

        <v-divider class="my-3" />

        <div class="text-subtitle-2 font-weight-bold mb-2">{{ t('billing.clients.form.readiness') }}</div>
        <v-list density="compact" class="readiness-list">
          <v-list-item v-for="check in readinessChecks" :key="check.label">
            <template #prepend>
              <v-icon :color="check.satisfied ? 'success' : 'error'" size="18">
                {{ check.satisfied ? 'mdi-check-circle' : 'mdi-alert-circle' }}
              </v-icon>
            </template>
            <v-list-item-title class="text-body-2">{{ check.label }}</v-list-item-title>
          </v-list-item>
        </v-list>

        <div v-if="canEditCustomer" class="d-flex flex-wrap align-center ga-2 mt-3">
          <span v-if="!isMigrationReady" class="text-caption text-medium-emphasis flex-grow-1">{{ t('billing.clients.form.editHint') }}</span>
          <v-btn size="small" variant="outlined" prepend-icon="mdi-pencil-outline" @click="openEditCustomer">
            {{ t('billing.clients.form.editCustomer') }}
          </v-btn>
        </div>
      </template>
    </v-card-text>

    <v-alert v-if="errorMessage" type="error" variant="tonal" class="mx-6 mb-2">
      {{ errorMessage }}
    </v-alert>

    <v-card-actions class="pa-4 d-flex ga-2 responsive-dialog-actions">
      <v-spacer />
      <v-btn variant="text" :disabled="migrating" @click="emit('cancel')">
        {{ t('billing.clients.form.cancel') }}
      </v-btn>
      <v-btn
        color="primary"
        variant="flat"
        :prepend-icon="selectedCustomerBillingSynced ? 'mdi-cloud-sync-outline' : 'mdi-cloud-upload-outline'"
        :loading="migrating"
        :disabled="!canMigrate"
        @click="handleMigrate"
      >
        {{ selectedCustomerBillingSynced ? t('billing.clients.form.update') : t('billing.clients.form.migrate') }}
      </v-btn>
    </v-card-actions>

    <v-dialog v-model="editDialogOpen" max-width="min(100%, 920px)" scrollable>
      <AdminCustomerRecordDialog
        :customer-id="editCustomerId"
        @saved="handleCustomerEdited"
        @deleted="handleCustomerDeleted"
        @cancel="editDialogOpen = false"
      />
    </v-dialog>
  </v-card>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminCustomerRecordDialog from '@/components/forms/AdminCustomerRecordDialog.vue'
import { getAdminCustomer, getAdminCustomers } from '@/services/admin'
import { listBillingGroups, syncCustomerToBilling, type BillingGroupOption, type SyncCustomerResponse } from '@/services/billing'
import type { AdminCustomerListItem, AdminCustomerRecord, CustomerShipToAddress } from '@/types/api'

const emit = defineEmits<{
  (e: 'saved', result: SyncCustomerResponse, customerName: string): void
  (e: 'cancel'): void
}>()

const props = defineProps<{
  customerId?: string
  externalClientId?: string
}>()

const { t } = useI18n({ useScope: 'global' })

const migrating = ref(false)
const errorMessage = ref('')
const loadingMigrate = ref(false)
const migratableCustomers = ref<AdminCustomerListItem[]>([])
const migrateSearchText = ref('')
let migrateSearchTimer: ReturnType<typeof setTimeout> | null = null
const billingGroups = ref<BillingGroupOption[]>([])
const selectedMigrateCustomerId = ref<string | null>(null)
const selectedCustomerListItem = ref<AdminCustomerListItem | null>(null)
const selectedCustomer = ref<AdminCustomerRecord | null>(null)
const selectedCustomerBillingSynced = ref(false)
const editDialogOpen = ref(false)

const draft = reactive({
  customerName: '',
  customerCode: '',
  group: '',
  billTo: '',
  shipToAddresses: [] as CustomerShipToAddress[],
})

const isRecordMode = computed(() => !!props.customerId)

const selectedCustomerName = computed(() =>
  selectedCustomer.value?.customerName ??
  migratableCustomers.value.find((c) => c.customerId === selectedMigrateCustomerId.value)?.customerName ??
  '',
)

const groupName = computed(() => {
  const groupId = draft.group
  if (!groupId) {
    return ''
  }
  return billingGroups.value.find((group) => group.externalGroupId === groupId)?.name ?? groupId
})

const shipToText = computed(() =>
  draft.shipToAddresses
    .map((entry) => entry.address.trim())
    .filter(Boolean)
    .join('\n\n'),
)

const readinessChecks = computed(() => [
  { label: t('billing.clients.form.requiredName'), satisfied: draft.customerName.trim().length > 0 },
  { label: t('billing.clients.form.requiredCode'), satisfied: draft.customerCode.trim().length > 0 },
  { label: t('billing.clients.form.requiredBillTo'), satisfied: draft.billTo.trim().length > 0 },
])

const isMigrationReady = computed(() => readinessChecks.value.every((check) => check.satisfied))

const editCustomerId = computed(() => selectedCustomer.value?.customerId ?? null)

const canEditCustomer = computed(() => !!selectedCustomer.value)

const canMigrate = computed(() =>
  !!selectedMigrateCustomerId.value &&
  isMigrationReady.value,
)

function isSynced(customer: AdminCustomerListItem): boolean {
  return customer.billingSyncStatus === 'success' && !!customer.invoiceNinjaClientId
}

onMounted(async () => {
  await loadBillingGroups()
  if (props.customerId) {
    await selectCustomerById(props.customerId, props.externalClientId)
  } else {
    await searchCustomers()
  }
})

async function loadBillingGroups() {
  try {
    billingGroups.value = await listBillingGroups()
  } catch {
    billingGroups.value = []
  }
}

async function searchCustomers(query = '') {
  loadingMigrate.value = true
  try {
    migratableCustomers.value = await getAdminCustomers({
      lookup: query.trim(),
      take: 100,
    })
    ensureSelectedCustomerInList()
  } catch {
    migratableCustomers.value = []
    if (selectedCustomerListItem.value) {
      migratableCustomers.value = [selectedCustomerListItem.value]
    }
    errorMessage.value = t('billing.clients.form.loadCustomersFailed')
  } finally {
    loadingMigrate.value = false
  }
}

function ensureSelectedCustomerInList() {
  if (!selectedCustomerListItem.value) {
    return
  }
  const exists = migratableCustomers.value.some(
    (c) => c.customerId === selectedCustomerListItem.value!.customerId,
  )
  if (!exists) {
    migratableCustomers.value = [selectedCustomerListItem.value, ...migratableCustomers.value]
  }
}

function handleMigrateSearch(search: string) {
  if (selectedCustomer.value && search === selectedCustomer.value.customerName) {
    return
  }
  if (migrateSearchTimer) {
    clearTimeout(migrateSearchTimer)
  }
  migrateSearchTimer = setTimeout(() => {
    void searchCustomers(search)
  }, 300)
}

function clearDraft() {
  selectedCustomerListItem.value = null
  selectedCustomer.value = null
  selectedCustomerBillingSynced.value = false
  draft.customerName = ''
  draft.customerCode = ''
  draft.group = ''
  draft.billTo = ''
  draft.shipToAddresses = []
}

async function selectCustomerById(customerId: string, externalClientId?: string) {
  selectedMigrateCustomerId.value = customerId
  selectedCustomerListItem.value = {
    customerId,
    customerName: '',
    loginAccount: '',
    loginPassword: '',
    customerCode: '',
    invoiceNinjaClientId: externalClientId ?? '',
    billingSyncStatus: externalClientId ? 'success' : '',
    createdOn: '',
    createdBy: '',
    modifiedOn: '',
    modifiedBy: '',
  }
  selectedCustomerBillingSynced.value = !!externalClientId
  try {
    const record = await getAdminCustomer(customerId)
    applyCustomerRecord(record)
  } catch {
    errorMessage.value = t('billing.clients.messages.loadRecordFailed')
  }
}

async function onMigrateCustomerSelected(customerId: string | null) {
  errorMessage.value = ''
  clearDraft()
  if (!customerId) {
    return
  }

  const customer = migratableCustomers.value.find((c) => c.customerId === customerId)
  selectedCustomerListItem.value = customer ?? null
  selectedCustomerBillingSynced.value = customer
    ? customer.billingSyncStatus === 'success' && !!customer.invoiceNinjaClientId
    : false

  try {
    const record = await getAdminCustomer(customerId)
    applyCustomerRecord(record)
  } catch {
    errorMessage.value = t('billing.clients.messages.loadRecordFailed')
  }
}

function openEditCustomer() {
  errorMessage.value = ''
  editDialogOpen.value = true
}

function applyCustomerRecord(record: AdminCustomerRecord) {
  selectedCustomer.value = record
  draft.customerName = record.customerName
  draft.customerCode = record.customerCode
  draft.group = record.group
  draft.billTo = record.billTo
  draft.shipToAddresses = (record.shipToAddresses ?? []).map((entry) => ({ ...entry }))
}

function handleCustomerEdited(record: AdminCustomerRecord) {
  applyCustomerRecord(record)
  editDialogOpen.value = false
  errorMessage.value = ''
}

function handleCustomerDeleted() {
  editDialogOpen.value = false
  selectedMigrateCustomerId.value = null
  clearDraft()
  errorMessage.value = ''
}

async function handleMigrate() {
  const customerId = selectedMigrateCustomerId.value
  if (!customerId || !canMigrate.value) {
    return
  }

  migrating.value = true
  errorMessage.value = ''

  try {
    const result = await syncCustomerToBilling({
      customerId,
      customerCode: draft.customerCode.trim(),
      customerName: draft.customerName.trim(),
      billTo: draft.billTo.trim(),
      shipToAddresses: draft.shipToAddresses
        .map((entry) => entry.address.trim())
        .filter((address) => address.length > 0),
      existingInvoiceNinjaClientId: selectedCustomerListItem.value?.invoiceNinjaClientId || undefined,
      group: draft.group.trim(),
    })

    emit('saved', result, draft.customerName.trim())
  } catch (err) {
    const axiosErr = err as { response?: { data?: { message?: string } } }
    const serverMsg = axiosErr.response?.data?.message
    const fallback = err instanceof Error ? err.message : ''
    errorMessage.value = serverMsg || t('billing.clients.messages.migrateFailed', { error: fallback })
  } finally {
    migrating.value = false
  }
}
</script>

<style scoped>
.migrate-sheet {
  border-color: rgba(var(--v-theme-primary), 0.2);
}

.readiness-list {
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  border-radius: 8px;
}
</style>
