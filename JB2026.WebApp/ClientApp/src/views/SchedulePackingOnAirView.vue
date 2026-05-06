<template>
  <section class="page-section packing-on-air-page">
    <v-card rounded="xl" elevation="0" class="panel-card packing-on-air-card">
      <v-card-title class="d-flex flex-wrap align-center ga-2 pa-3">
        <div class="flex-grow-1">
          <h3 class="text-h6 mb-0">{{ t('scheduler.packingOnAir.title') }}</h3>
        </div>

        <v-btn color="primary" prepend-icon="mdi-content-save" :loading="saving" :disabled="selectedItems.length === 0" @click="saveDialog = true">
          {{ t('common.save') }}
        </v-btn>

        <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="load">
          {{ t('common.refresh') }}
        </v-btn>
      </v-card-title>

      <v-divider />

      <v-card-text class="pa-2">
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-2">{{ errorMessage }}</v-alert>

        <div class="packing-on-air-layout">
          <div class="packing-panel">
            <div class="panel-header text-caption font-weight-bold text-medium-emphasis mb-1">
              {{ t('scheduler.packingOnAir.available.title') }} ({{ availableDisplay.length }})
            </div>
            <div v-if="isPhoneLayout" class="packing-mobile-list">
              <v-card
                v-for="(item, index) in availableDisplay"
                :key="item.orderId"
                rounded="lg"
                elevation="0"
                class="packing-mobile-card"
                @click="toggleAvailableCheck(item.orderId)"
              >
                <div class="packing-mobile-card__header">
                  <div>
                    <div class="text-subtitle-2 font-weight-bold text-primary">
                      {{ index + 1 }}.
                      <v-btn
                        variant="text"
                        color="primary"
                        density="compact"
                        class="px-0 text-none packing-order-link"
                        @click.stop="openOrderForm(item.orderId)"
                      >
                        {{ item.orderNumber }}
                      </v-btn>
                    </div>
                    <div class="text-caption text-medium-emphasis">{{ item.customerName }}</div>
                  </div>
                  <v-checkbox-btn
                    :model-value="checkedAvailable.includes(item.orderId)"
                    density="compact"
                    hide-details
                    @click.stop="toggleAvailableCheck(item.orderId)"
                  />
                </div>
                <div class="text-body-2 mt-1">{{ item.orderTitle }}</div>
              </v-card>
            </div>
            <div v-else class="list-container">
              <table class="packing-table">
                <colgroup>
                  <col class="col-check" />
                  <col class="col-num" />
                  <col class="col-order" />
                  <col class="col-customer" />
                  <col class="col-title" />
                </colgroup>
                <thead>
                  <tr>
                    <th class="col-check"><v-checkbox-btn :model-value="allAvailableChecked" density="compact" hide-details @click="toggleAllAvailable" /></th>
                    <th class="col-num">#</th>
                    <th class="col-order">{{ t('scheduler.packingOnAir.columns.order') }}</th>
                    <th class="col-customer">{{ t('scheduler.packingOnAir.columns.customer') }}</th>
                    <th class="col-title">{{ t('scheduler.packingOnAir.columns.title') }}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="(item, index) in availableDisplay"
                    :key="item.orderId"
                    :class="{ 'row-selected': checkedAvailable.includes(item.orderId) }"
                    @click="toggleAvailableCheck(item.orderId)"
                  >
                    <td class="col-check"><v-checkbox-btn :model-value="checkedAvailable.includes(item.orderId)" density="compact" hide-details @click.stop="toggleAvailableCheck(item.orderId)" /></td>
                    <td class="col-num text-center">{{ index + 1 }}</td>
                    <td class="col-order text-primary font-weight-medium">
                      <v-btn
                        variant="text"
                        color="primary"
                        density="compact"
                        class="px-0 text-none packing-order-link"
                        @click.stop="openOrderForm(item.orderId)"
                      >
                        {{ item.orderNumber }}
                      </v-btn>
                    </td>
                    <td class="col-customer">{{ item.customerName }}</td>
                    <td class="col-title">{{ item.orderTitle }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <div class="transfer-col d-flex flex-column align-center justify-center ga-1">
            <v-tooltip :text="t('scheduler.packingOnAir.actions.selectOne')" location="right">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" color="primary" @click="moveRight(false)">
                  <v-icon size="16">mdi-chevron-right</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
            <v-tooltip :text="t('scheduler.packingOnAir.actions.selectAll')" location="right">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" color="primary" @click="moveRight(true)">
                  <v-icon size="16">mdi-chevron-double-right</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
            <v-divider class="my-1 w-100" />
            <v-tooltip :text="t('scheduler.packingOnAir.actions.unselectOne')" location="right">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" @click="moveLeft(false)">
                  <v-icon size="16">mdi-chevron-left</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
            <v-tooltip :text="t('scheduler.packingOnAir.actions.unselectAll')" location="right">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" @click="moveLeft(true)">
                  <v-icon size="16">mdi-chevron-double-left</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
          </div>

          <div class="packing-panel selected-panel">
            <div class="panel-header text-caption font-weight-bold text-medium-emphasis mb-1">
              {{ t('scheduler.packingOnAir.selected.title') }} ({{ selectedItems.length }})
            </div>
            <div v-if="isPhoneLayout" class="packing-mobile-list">
              <v-card
                v-for="(item, index) in selectedItems"
                :key="item.orderId"
                rounded="lg"
                elevation="0"
                class="packing-mobile-card"
                @click="toggleSelectedCheck(item.orderId)"
              >
                <div class="packing-mobile-card__header">
                  <div>
                    <div class="text-subtitle-2 font-weight-bold text-primary">{{ index + 1 }}. {{ item.orderNumber }}</div>
                    <div class="text-caption text-medium-emphasis">{{ item.customerName }}</div>
                  </div>
                  <v-checkbox-btn
                    :model-value="checkedSelected.includes(item.orderId)"
                    density="compact"
                    hide-details
                    @click.stop="toggleSelectedCheck(item.orderId)"
                  />
                </div>
                <div class="text-body-2 mt-1">{{ item.orderTitle }}</div>
                <div class="text-caption text-medium-emphasis mt-1">
                  {{ t('scheduler.packingOnAir.columns.remarks') }}: {{ item.remarks || '-' }}
                </div>
              </v-card>
            </div>
            <div v-else class="list-container">
              <table class="packing-table">
                <colgroup>
                  <col class="col-check" />
                  <col class="col-num" />
                  <col class="col-order" />
                  <col class="col-customer" />
                  <col class="col-title" />
                  <col class="col-remarks" />
                </colgroup>
                <thead>
                  <tr>
                    <th class="col-check"><v-checkbox-btn :model-value="allSelectedChecked" density="compact" hide-details @click="toggleAllSelected" /></th>
                    <th class="col-num">#</th>
                    <th class="col-order">{{ t('scheduler.packingOnAir.columns.order') }}</th>
                    <th class="col-customer">{{ t('scheduler.packingOnAir.columns.customer') }}</th>
                    <th class="col-title">{{ t('scheduler.packingOnAir.columns.title') }}</th>
                    <th class="col-remarks">{{ t('scheduler.packingOnAir.columns.remarks') }}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="(item, index) in selectedItems"
                    :key="item.orderId"
                    :class="{ 'row-selected': checkedSelected.includes(item.orderId) }"
                    @click="toggleSelectedCheck(item.orderId)"
                  >
                    <td class="col-check"><v-checkbox-btn :model-value="checkedSelected.includes(item.orderId)" density="compact" hide-details @click.stop="toggleSelectedCheck(item.orderId)" /></td>
                    <td class="col-num text-center">{{ index + 1 }}</td>
                    <td class="col-order text-primary font-weight-medium">{{ item.orderNumber }}</td>
                    <td class="col-customer">{{ item.customerName }}</td>
                    <td class="col-title">{{ item.orderTitle }}</td>
                    <td class="col-remarks">{{ item.remarks }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <div class="action-col d-flex flex-column align-center ga-1">
            <v-tooltip :text="t('scheduler.packingOnAir.actions.moveTop')" location="left">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" @click="moveSelected('top')">
                  <v-icon size="16">mdi-chevron-double-up</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
            <v-tooltip :text="t('scheduler.packingOnAir.actions.moveUp')" location="left">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" @click="moveSelected('up')">
                  <v-icon size="16">mdi-chevron-up</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
            <v-tooltip :text="t('scheduler.packingOnAir.actions.moveDown')" location="left">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" @click="moveSelected('down')">
                  <v-icon size="16">mdi-chevron-down</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
            <v-tooltip :text="t('scheduler.packingOnAir.actions.moveBottom')" location="left">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" @click="moveSelected('bottom')">
                  <v-icon size="16">mdi-chevron-double-down</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
            <v-divider class="my-1 w-100" />
            <v-tooltip :text="t('scheduler.packingOnAir.actions.completed')" location="left">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" color="success" :disabled="checkedSelected.length === 0" @click="markCompleted">
                  <v-icon size="16">mdi-check-circle-outline</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
          </div>
        </div>
      </v-card-text>
    </v-card>

    <v-dialog v-model="saveDialog" max-width="360">
      <v-card>
        <v-card-title>{{ t('common.confirmation') }}</v-card-title>
        <v-card-text>{{ t('scheduler.packingOnAir.saveConfirm') }}</v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="saveDialog = false">{{ t('common.cancel') }}</v-btn>
          <v-btn color="primary" :loading="saving" @click="save">{{ t('common.save') }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="formOpen" max-width="760" scrollable>
      <JobOrderForm
        v-if="formJob"
        :job="formJob"
        @saved="handleFormSaved"
        @cancel="formOpen = false"
      />
    </v-dialog>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useDisplay } from 'vuetify'
import JobOrderForm from '@/components/forms/JobOrderForm.vue'
import { getJobDetail } from '@/services/jobs'
import {
  completePackingOnAir,
  getPackingOnAir,
  getPackingOnAirAvailable,
  savePackingOnAirBatch,
} from '@/services/scheduler'
import type { JobDetail, JobPackingOnAirAvailableItem, JobPackingOnAirItem } from '@/types/api'

type SelectedState = JobPackingOnAirItem

const { t } = useI18n({ useScope: 'global' })
const display = useDisplay()
const isPhoneLayout = computed(() => display.smAndDown.value)

const loading = ref(false)
const saving = ref(false)
const errorMessage = ref('')
const saveDialog = ref(false)
const formOpen = ref(false)
const formJob = ref<JobDetail | null>(null)

const availableItems = ref<JobPackingOnAirAvailableItem[]>([])
const selectedItems = ref<SelectedState[]>([])
const checkedAvailable = ref<string[]>([])
const checkedSelected = ref<string[]>([])
const cancelledOrderIds = ref<Set<string>>(new Set())

const selectedOrderIds = computed(() => new Set(selectedItems.value.map((item) => item.orderId)))
const availableDisplay = computed(() => availableItems.value.filter((item) => !selectedOrderIds.value.has(item.orderId)))

const allAvailableChecked = computed(
  () => availableDisplay.value.length > 0 && checkedAvailable.value.length === availableDisplay.value.length,
)

const allSelectedChecked = computed(
  () => selectedItems.value.length > 0 && checkedSelected.value.length === selectedItems.value.length,
)

onMounted(() => {
  void load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''
  checkedAvailable.value = []
  checkedSelected.value = []
  cancelledOrderIds.value = new Set()

  try {
    const [available, selected] = await Promise.all([
      getPackingOnAirAvailable(0),
      getPackingOnAir(0),
    ])

    availableItems.value = available
    selectedItems.value = selected.map((item) => ({ ...item }))
  } catch {
    errorMessage.value = t('scheduler.packingOnAir.loadFailed')
  } finally {
    loading.value = false
  }
}

function toggleAvailableCheck(orderId: string) {
  const index = checkedAvailable.value.indexOf(orderId)
  if (index >= 0) checkedAvailable.value.splice(index, 1)
  else checkedAvailable.value.push(orderId)
}

function toggleSelectedCheck(orderId: string) {
  const index = checkedSelected.value.indexOf(orderId)
  if (index >= 0) checkedSelected.value.splice(index, 1)
  else checkedSelected.value.push(orderId)
}

function toggleAllAvailable() {
  if (allAvailableChecked.value) {
    checkedAvailable.value = []
    return
  }

  checkedAvailable.value = availableDisplay.value.map((item) => item.orderId)
}

function toggleAllSelected() {
  if (allSelectedChecked.value) {
    checkedSelected.value = []
    return
  }

  checkedSelected.value = selectedItems.value.map((item) => item.orderId)
}

function moveRight(moveAll: boolean) {
  const toMove = moveAll
    ? availableDisplay.value
    : availableDisplay.value.filter((item) => checkedAvailable.value.includes(item.orderId))

  for (const item of toMove) {
    cancelledOrderIds.value.delete(item.orderId)
    selectedItems.value.push({
      onAirId: '',
      orderId: item.orderId,
      orderType: item.orderType,
      orderNumber: item.orderNumber,
      customerName: item.customerName,
      orderTitle: item.orderTitle,
      priority: selectedItems.value.length,
      remarks: item.remarks,
    })
  }

  checkedAvailable.value = []
}

function moveLeft(moveAll: boolean) {
  const idsToRemove = moveAll
    ? selectedItems.value.map((item) => item.orderId)
    : [...checkedSelected.value]

  for (const orderId of idsToRemove) {
    cancelledOrderIds.value.add(orderId)
  }

  selectedItems.value = selectedItems.value.filter((item) => !idsToRemove.includes(item.orderId))
  checkedSelected.value = []
}

function moveSelected(direction: 'top' | 'up' | 'down' | 'bottom') {
  if (checkedSelected.value.length === 0) {
    return
  }

  const list = [...selectedItems.value]
  const ids = checkedSelected.value

  if (direction === 'top') {
    let insertAt = 0
    for (const id of ids) {
      const index = list.findIndex((item) => item.orderId === id)
      if (index < 0) continue
      const [item] = list.splice(index, 1)
      if (!item) continue
      list.splice(insertAt, 0, item)
      insertAt++
    }
  } else if (direction === 'up') {
    for (const id of ids) {
      const index = list.findIndex((item) => item.orderId === id)
      if (index <= 0) continue
      const current = list[index]
      const previous = list[index - 1]
      if (!current || !previous) continue
      list[index - 1] = current
      list[index] = previous
    }
  } else if (direction === 'down') {
    for (const id of [...ids].reverse()) {
      const index = list.findIndex((item) => item.orderId === id)
      if (index < 0 || index >= list.length - 1) continue
      const current = list[index]
      const next = list[index + 1]
      if (!current || !next) continue
      list[index] = next
      list[index + 1] = current
    }
  } else {
    let insertAt = list.length
    for (const id of [...ids].reverse()) {
      const index = list.findIndex((item) => item.orderId === id)
      if (index < 0) continue
      const [item] = list.splice(index, 1)
      if (!item) continue
      insertAt--
      list.splice(insertAt, 0, item)
    }
  }

  selectedItems.value = list.map((item, index) => ({ ...item, priority: index }))
}

async function save() {
  saving.value = true
  errorMessage.value = ''

  try {
    await savePackingOnAirBatch({
      orderType: 0,
      selectedItems: selectedItems.value.map((item) => ({ orderId: item.orderId })),
      cancelledOrderIds: [...cancelledOrderIds.value],
    })

    saveDialog.value = false
    await load()
  } catch {
    errorMessage.value = t('scheduler.packingOnAir.saveFailed')
  } finally {
    saving.value = false
  }
}

async function markCompleted() {
  if (checkedSelected.value.length === 0) {
    return
  }

  loading.value = true
  errorMessage.value = ''

  try {
    await completePackingOnAir({ orderIds: [...checkedSelected.value] })
    await load()
  } catch {
    errorMessage.value = t('scheduler.packingOnAir.completeFailed')
  } finally {
    loading.value = false
  }
}

async function openOrderForm(orderId: string) {
  try {
    formJob.value = await getJobDetail(orderId)
    formOpen.value = true
  } catch {
    errorMessage.value = t('jobOrder.openEditFailed')
  }
}

async function handleFormSaved() {
  formOpen.value = false
  await load()
}
</script>

<style scoped>
.packing-on-air-page {
  min-height: 0;
}

.packing-on-air-card {
  overflow: hidden;
}

.packing-on-air-layout {
  display: flex;
  gap: 4px;
  height: calc(100vh - 200px);
  min-height: 400px;
}

.packing-panel {
  flex: 1 1 0;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.selected-panel {
  flex: 1.2 1 0;
}

.transfer-col,
.action-col {
  flex: 0 0 36px;
  padding-top: 46px;
}

.panel-header {
  flex-shrink: 0;
}

.list-container {
  flex: 1 1 0;
  overflow: auto;
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 8px;
}

.packing-table {
  width: 100%;
  border-collapse: collapse;
  table-layout: fixed;
  font-size: 12px;
}

.packing-table thead tr {
  position: sticky;
  top: 0;
  background: rgb(var(--v-theme-surface));
  z-index: 1;
}

.packing-table th,
.packing-table td {
  padding: 2px 4px;
  white-space: nowrap;
  border-bottom: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 0;
}

.packing-table th {
  font-weight: 600;
  text-align: left;
  font-size: 11px;
}

.packing-table tbody tr {
  cursor: pointer;
}

.packing-table tbody tr:hover {
  background: rgba(var(--v-theme-primary), 0.04);
}

.packing-table tbody tr.row-selected {
  background: rgba(var(--v-theme-primary), 0.1);
}

.col-check { width: 34px; min-width: 34px; }
.col-num { width: 34px; min-width: 34px; text-align: center; }
.col-order { width: 92px; min-width: 92px; }
.col-customer { width: 180px; min-width: 180px; }
.col-title { min-width: 180px; }
.col-remarks { width: 220px; min-width: 220px; }

.packing-mobile-list {
  display: grid;
  gap: 10px;
}

.packing-mobile-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  padding: 10px;
}

.packing-mobile-card__header {
  display: flex;
  justify-content: space-between;
  gap: 8px;
}

.packing-order-link {
  min-width: 0;
}

@media (max-width: 960px) {
  .packing-on-air-layout {
    flex-direction: column;
    height: auto;
  }

  .transfer-col,
  .action-col {
    flex-direction: row !important;
    justify-content: center;
    padding-top: 0;
  }
}
</style>