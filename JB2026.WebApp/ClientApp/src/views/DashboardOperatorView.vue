<template>
  <section class="page-section schedule-page">
    <v-card rounded="xl" elevation="0" class="panel-card schedule-card">
      <!-- Toolbar -->
<!--       <v-card-title class="d-flex flex-wrap align-center ga-2 pa-3">
        <div class="flex-grow-1">
          <h3 class="text-h6 mb-0">{{ t('scheduler.schedule.title') }}</h3>
        </div>

        <v-btn color="primary" prepend-icon="mdi-content-save" :loading="saving" @click="confirmSave">
          {{ t('common.save') }}
        </v-btn>
 -->
        <!-- Machine filter -->
<!--         <v-btn-toggle
          v-model="machineFilter"
          mandatory
          density="compact"
          variant="outlined"
          :class="['machine-toggle', { 'machine-toggle--scroll': isPhoneLayout }]"
        >
          <v-btn value="0" size="small">{{ t('scheduler.schedule.machine.all') }}</v-btn>
          <v-btn value="1" size="small">M1</v-btn>
          <v-btn value="2" size="small">M2</v-btn>
          <v-btn value="3" size="small">M3</v-btn>
          <v-btn value="4" size="small">M4</v-btn>
          <v-btn value="5" size="small">M5</v-btn>
        </v-btn-toggle>

        <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="load">
          {{ t('common.refresh') }}
        </v-btn>
      </v-card-title>

      <v-divider />
 -->
      <v-card-text class="pa-2" style="padding: 12px !important;">
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-2">{{ errorMessage }}</v-alert>
        <v-alert
          v-if="isNarrowPhoneLayout"
          type="info"
          variant="tonal"
          density="compact"
          class="mb-2"
        >
          Desktop preferred for scheduling. Mobile mode shows a reduced view.
        </v-alert>

        <div :class="['schedule-layout', { 'schedule-layout--phone': isPhoneLayout }]">
          <!-- Scheduled panel -->
          <div class="schedule-panel scheduled-panel">
            <div class="d-flex align-center mb-1">
              <div class="panel-header text-caption font-weight-bold text-medium-emphasis">
                {{ t('scheduler.schedule.scheduled.title') }} ({{ scheduledDisplay.length }})
              </div>
<!--               <div class="d-flex align-center ga-1 light-toolbar" style="flex: 1; justify-content: center;">

                <span class="text-body-small text-high-emphasis">@1:</span>
                <v-btn v-for="c in lightColors1" :key="`a1-${c.value}`"
                  icon size="small" density="compact" :color="c.color" variant="tonal"
                  @click="setStep1Status(c.value)">
                  <v-icon size="16">mdi-circle</v-icon>
                </v-btn>
                <v-divider vertical class="mx-1" />
                <span class="text-body-small text-high-emphasis">@2:</span>
                <v-btn v-for="c in lightColors2" :key="`a2-${c.value}`"
                  icon size="small" density="compact" :color="c.color" variant="tonal"
                  @click="setStep2Status(c.value)">
                  <v-icon size="16">mdi-circle</v-icon>
                </v-btn>
                <v-divider vertical class="mx-1" />
                <v-tooltip :text="t('scheduler.schedule.urgency.red')" location="bottom">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" icon size="small" density="compact" color="error" variant="tonal"
                      @click="toggleUrgency(4)">
                      <v-icon size="16">mdi-bell-alert</v-icon>
                    </v-btn>
                  </template>
                </v-tooltip>
                <v-tooltip :text="t('scheduler.schedule.urgency.yellow')" location="bottom">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" icon size="small" density="compact" color="warning" variant="tonal"
                      @click="toggleUrgency(2)">
                      <v-icon size="16">mdi-bell</v-icon>
                    </v-btn>
                  </template>
                </v-tooltip>
              </div> -->
            </div>
            <div class="list-container">
              <template v-if="!isPhoneLayout">
                <table class="schedule-table">
                  <colgroup>
                    <col class="col-check" />
                    <col class="col-num" />
                    <col class="col-order" />
                    <col class="col-dday" />
                    <col :style="{ width: `${scheduledColumnWidths.customer}px` }" />
                    <col :style="{ width: `${scheduledColumnWidths.title}px` }" />
                    <col class="col-machine" />
                    <col class="col-light" />
                    <col class="col-light" />
                    <col class="col-light" />
                    <col class="col-print-time" />
                    <col :style="{ width: `${scheduledColumnWidths.printQty}px` }" />
                    <col :style="{ width: `${scheduledColumnWidths.printColor}px` }" />
                    <col :style="{ width: `${scheduledColumnWidths.printSize}px` }" />
                  </colgroup>
                  <thead>
                    <tr>
                      <th class="col-check"><v-checkbox-btn v-model="allScheduledChecked" density="compact" hide-details @click="toggleAllScheduled" /></th>
                      <th class="col-num text-center">#</th>
                      <th class="col-order">{{ t('scheduler.schedule.columns.order') }}</th>
                      <th class="col-dday text-center"><v-icon size="16">mdi-calendar-clock</v-icon></th>
                      <th class="col-customer resizable-header" :style="{ width: `${scheduledColumnWidths.customer}px` }">
                        <div class="header-content">
                          {{ t('scheduler.schedule.columns.customer') }}
                          <span class="resize-handle" @mousedown.prevent="startResize($event, 'customer')" />
                        </div>
                      </th>
                      <th class="col-title resizable-header" :style="{ width: `${scheduledColumnWidths.title}px` }">
                        <div class="header-content">
                          {{ t('scheduler.schedule.columns.title') }}
                          <span class="resize-handle" @mousedown.prevent="startResize($event, 'title')" />
                        </div>
                      </th>
                      <th class="col-machine text-center">M</th>
                      <th class="col-light text-center">@1</th>
                      <th class="col-light text-center">@2</th>
                      <th class="col-light text-center">
                        <v-icon size="16">mdi-bell</v-icon>
                      </th>
                      <th class="col-print-time text-center">
                        <v-icon size="16">mdi-clock-outline</v-icon>
                      </th>
                      <th class="col-print-qty resizable-header" :style="{ width: `${scheduledColumnWidths.printQty}px` }">
                        <div class="header-content">
                          {{ t('scheduler.schedule.columns.printQty') }}
                          <span class="resize-handle" @mousedown.prevent="startResize($event, 'printQty')" />
                        </div>
                      </th>
                      <th class="col-print-color resizable-header" :style="{ width: `${scheduledColumnWidths.printColor}px` }">
                        <div class="header-content">
                          {{ t('scheduler.schedule.columns.printColor') }}
                          <span class="resize-handle" @mousedown.prevent="startResize($event, 'printColor')" />
                        </div>
                      </th>
                      <th class="col-print-size resizable-header" :style="{ width: `${scheduledColumnWidths.printSize}px` }">
                        <div class="header-content">
                          {{ t('scheduler.schedule.columns.printSize') }}
                          <span class="resize-handle" @mousedown.prevent="startResize($event, 'printSize')" />
                        </div>
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr
                      v-for="(item, index) in scheduledDisplay"
                      :key="item.orderId"
                      :class="{ 'row-selected': checkedScheduled.includes(item.orderId) }"
                      @click="toggleScheduledCheck(item.orderId)"
                    >
                      <td class="col-check"><v-checkbox-btn :model-value="checkedScheduled.includes(item.orderId)" density="compact" hide-details @click.stop="toggleScheduledCheck(item.orderId)" /></td>
                      <td class="col-num text-center">{{ index + 1 }}</td>
                      <td class="col-order">
                        <v-btn
                          variant="text"
                          color="primary"
                          density="compact"
                          class="px-0 text-none order-link"
                          @click.stop="openOrderForm(item.orderId)"
                        >
                          {{ item.orderNumber }}
                        </v-btn>
                      </td>
                      <td class="col-dday text-center" :class="{ 'dday-overdue': isOverdue(item.requiredOn), 'dday-flash': isOverdue(item.requiredOn) }">
                        {{ dDay(item.requiredOn) !== null ? dDay(item.requiredOn) : '-' }}
                      </td>
                      <td class="col-customer">{{ item.customerName }}</td>
                      <td class="col-title">{{ item.orderTitle }}</td>
                      <td class="col-machine text-center">
                        <v-btn icon size="small" variant="flat" density="compact" :color="machineColor(item.machineNumber)" class="machine-chip"><span class="text-caption font-weight-bold">{{ item.machineNumber || '-' }}</span></v-btn>
                      </td>
                      <td class="col-light text-center">
                        <v-icon size="16" :color="workflowColor(item.step1Status)">mdi-circle</v-icon>
                      </td>
                      <td class="col-light text-center">
                        <v-icon size="16" :color="workflowColor(item.step2Status)">mdi-circle</v-icon>
                      </td>
                      <td class="col-light text-center">
                        <v-icon v-if="urgencyIcon(item.urgencyLevel)" size="16" :color="urgencyColor(item.urgencyLevel)">{{ urgencyIcon(item.urgencyLevel) }}</v-icon>
                        <span v-else>-</span>
                      </td>
                      <td class="col-print-time">{{ formatPrintTime(item.soNumber) }}</td>
                      <td class="col-print-qty">{{ item.printQty }}</td>
                      <td class="col-print-color">{{ item.printColor }}</td>
                      <td class="col-print-size">{{ item.printSize }}</td>
                    </tr>
                  </tbody>
                </table>
              </template>
              <template v-else>
                <div class="pa-2">
                  <AdaptiveRow
                    v-for="item in scheduledDisplay"
                    :key="item.orderId"
                    :is-mobile="true"
                    :selected="checkedScheduled.includes(item.orderId)"
                    :fields="getScheduledFields(item)"
                    @click="toggleScheduledCheck(item.orderId)"
                    @toggle-check="toggleScheduledCheck(item.orderId)"
                  >
                    <template #mobile-header>
                      <span class="text-subtitle-2 font-weight-bold text-primary">{{ item.orderNumber }}</span>
                    </template>
                  </AdaptiveRow>
                </div>
              </template>
            </div>
          </div>

          <!-- Right action column -->
          <div :class="['action-col', { 'action-col--phone': isPhoneLayout }, 'd-flex', 'flex-column', 'align-center', 'ga-1']">
            <!-- <v-tooltip :text="t('scheduler.schedule.actions.moveTop')" location="left">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" @click="moveScheduled('top')">
                  <v-icon size="16">mdi-chevron-double-up</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
            <v-tooltip :text="t('scheduler.schedule.actions.moveUp')" location="left">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" @click="moveScheduled('up')">
                  <v-icon size="16">mdi-chevron-up</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
            <v-tooltip :text="t('scheduler.schedule.actions.moveDown')" location="left">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" @click="moveScheduled('down')">
                  <v-icon size="16">mdi-chevron-down</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
            <v-tooltip :text="t('scheduler.schedule.actions.moveBottom')" location="left">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" @click="moveScheduled('bottom')">
                  <v-icon size="16">mdi-chevron-double-down</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
            <v-divider class="my-1 w-100" />
            <v-tooltip v-for="mc in [1,2,3,4,5]" :key="`chg${mc}`" :text="`M${mc}`" location="left">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="flat" density="compact"
                  :color="machineColor(String(mc))" class="machine-btn"
                  @click="changeMachine(mc)">
                  <span class="text-caption font-weight-bold">{{ mc }}</span>
                </v-btn>
              </template>
            </v-tooltip>
            <v-divider class="my-1 w-100" /> -->
            <v-tooltip :text="t('scheduler.schedule.actions.completed')" location="left">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="48" variant="text" density="compact" color="success"
                  @click="markCompleted">
                  <v-icon size="48">mdi-check-circle-outline</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
          </div>
        </div>
      </v-card-text>
    </v-card>

    <!-- Save confirmation dialog -->
    <v-dialog v-model="saveDialog" max-width="360">
      <v-card>
        <v-card-title>{{ t('common.confirmation') }}</v-card-title>
        <v-card-text>{{ t('scheduler.schedule.saveConfirm') }}</v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="saveDialog = false">{{ t('common.cancel') }}</v-btn>
          <v-btn color="primary" :loading="saving" @click="executeSave">{{ t('common.save') }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Job Order Editor Dialog -->
    <v-dialog v-model="formOpen" max-width="760" scrollable>
      <JobOrderForm
        v-if="formJob"
        :job="formJob"
        @saved="handleFormSaved"
        @cancel="formOpen = false"
        @attachment="handleAttachment"
        @print-order="handlePrintOrder"
        @workflow="handleWorkflow"
        @product-details-edit="handleProductDetailsEdit"
        @remarks-edit="handleRemarksEdit"
      />
    </v-dialog>

    <JobOrderActionDialogs
      :job="formJob"
      v-model:attachment-open="attachmentDialogOpen"
      v-model:product-details-open="productDetailsDialogOpen"
      v-model:remarks-open="remarksDialogOpen"
      @updated="handleActionUpdated"
      @error="showActionNotice"
    />

    <JobOrderPrintManagerDialog
      v-model="printManagerOpen"
      :order-id="printManagerJob?.orderId ?? null"
      :order-number="printManagerJob?.orderNumber ?? ''"
      :style-titles="printManagerJob?.styleTitles"
    />

    <v-snackbar v-model="actionNoticeOpen" color="info" timeout="3200">
      {{ actionNoticeMessage }}
    </v-snackbar>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useDisplay } from 'vuetify'
import { getOnAirSchedule, saveScheduleBatch } from '@/services/scheduler'
import { getJobDetail } from '@/services/jobs'
import type { JobDetail, JobScheduleOnAirItem } from '@/types/api'
import AdaptiveRow from '@/components/ui/AdaptiveRow.vue'
import JobOrderForm from '@/components/forms/JobOrderForm.vue'
import JobOrderActionDialogs from '@/components/forms/JobOrderActionDialogs.vue'
import JobOrderPrintManagerDialog from '@/components/forms/JobOrderPrintManagerDialog.vue'

const { t } = useI18n({ useScope: 'global' })
const router = useRouter()
const display = useDisplay()
const isPhoneLayout = computed(() => display.smAndDown.value)
const isNarrowPhoneLayout = computed(() => display.xs.value && display.width.value <= 430)

// ─── state ────────────────────────────────────────────────────────────────────
const loading = ref(false)
const saving = ref(false)
const errorMessage = ref('')
const saveDialog = ref(false)
const formOpen = ref(false)
const formJob = ref<JobDetail | null>(null)
const machineFilter = ref('0')
const attachmentDialogOpen = ref(false)
const productDetailsDialogOpen = ref(false)
const remarksDialogOpen = ref(false)
const actionNoticeOpen = ref(false)
const actionNoticeMessage = ref('')
const printManagerOpen = ref(false)
const printManagerJob = ref<JobDetail | null>(null)

const scheduledItems = ref<ScheduledItemState[]>([])
const cancelledOrderIds = ref<Set<string>>(new Set())

const scheduledColumnWidths = ref({
  customer: 170,
  title: 260,
  printQty: 100,
  printColor: 125,
  printSize: 130,
})

const checkedScheduled = ref<string[]>([])

// ─── interfaces ───────────────────────────────────────────────────────────────
interface ScheduledItemState extends JobScheduleOnAirItem {
  // Overrides from the original on-air response so we can mutate them locally
  machineNumber: string
  step1Status: number | null
  step2Status: number | null
  urgencyLevel: number
}

// ─── toolbar options ──────────────────────────────────────────────────────────
const lightColors1 = [
  { value: 0, color: 'error' },
  { value: 1, color: 'warning' },
  { value: 3, color: 'info' },
  { value: 2, color: 'success' },
]
const lightColors2 = [
  { value: 0, color: 'error' },
  { value: 1, color: 'warning' },
  { value: 2, color: 'success' },
]

// ─── computed ─────────────────────────────────────────────────────────────────
const scheduledDisplay = computed(() => {
  if (machineFilter.value === '0') return scheduledItems.value
  return scheduledItems.value.filter((item) => item.machineNumber === machineFilter.value)
})

const allScheduledChecked = computed(
  () => scheduledDisplay.value.length > 0 && checkedScheduled.value.length === scheduledDisplay.value.length,
)

function formatPrintTime(value: string | undefined | null): string {
  if (!value) return ''
  const num = parseFloat(value)
  if (isNaN(num)) return value
  return num.toFixed(1)
}

function dDay(requiredOn: string | null | undefined): number | null {
  if (!requiredOn) return null
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const req = new Date(requiredOn)
  req.setHours(0, 0, 0, 0)
  const diff = Math.floor((req.getTime() - today.getTime()) / (1000 * 60 * 60 * 24))
  return diff
}

function isOverdue(requiredOn: string | null | undefined): boolean {
  const day = dDay(requiredOn)
  return day !== null && day <= 0
}

// ─── field mappers for adaptive rows ──────────────────────────────────────────
function getScheduledFields(item: ScheduledItemState) {
  return [
    { key: 'order', label: t('scheduler.schedule.columns.order'), value: item.orderNumber },
    { key: 'dday', label: 'D-Day', value: dDay(item.requiredOn) !== null ? String(dDay(item.requiredOn)) : '-' },
    { key: 'customer', label: t('scheduler.schedule.columns.customer'), value: item.customerName },
    { key: 'title', label: t('scheduler.schedule.columns.title'), value: item.orderTitle },
    { key: 'printTime', label: 'Print Time', value: formatPrintTime(item.soNumber) },
    { key: 'machine', label: 'Machine', value: `M${item.machineNumber}` },
    { key: 'qty', label: t('scheduler.schedule.columns.printQty'), value: item.printQty },
    { key: 'color', label: t('scheduler.schedule.columns.printColor'), value: item.printColor },
    { key: 'size', label: t('scheduler.schedule.columns.printSize'), value: item.printSize },
  ]
}

// ─── lifecycle ────────────────────────────────────────────────────────────────
onMounted(() => load())

// Reload on-air list when machine filter changes
watch(machineFilter, () => {
  checkedScheduled.value = []
})

// ─── load ─────────────────────────────────────────────────────────────────────
async function load() {
  loading.value = true
  errorMessage.value = ''
  checkedScheduled.value = []
  cancelledOrderIds.value = new Set()

  try {
    const onAir = await getOnAirSchedule(0)
    scheduledItems.value = onAir.map((item) => ({ ...item }))
  } catch {
    errorMessage.value = t('scheduler.schedule.loadFailed')
  } finally {
    loading.value = false
  }
}

// ─── checkbox helpers ─────────────────────────────────────────────────────────
function toggleScheduledCheck(orderId: string) {
  const idx = checkedScheduled.value.indexOf(orderId)
  if (idx >= 0) checkedScheduled.value.splice(idx, 1)
  else checkedScheduled.value.push(orderId)
}

function toggleAllScheduled() {
  if (allScheduledChecked.value) {
    checkedScheduled.value = []
  } else {
    checkedScheduled.value = scheduledDisplay.value.map((i) => i.orderId)
  }
}

// ─── reorder actions ──────────────────────────────────────────────────────────
function moveScheduled(direction: 'top' | 'up' | 'down' | 'bottom') {
  const ids = checkedScheduled.value
  if (ids.length === 0) return

  const list = [...scheduledItems.value]

  if (direction === 'top') {
    let insertAt = 0
    for (const id of ids) {
      const idx = list.findIndex((i) => i.orderId === id)
      if (idx < 0) continue
      const [item] = list.splice(idx, 1)
      if (!item) continue
      list.splice(insertAt, 0, item)
      insertAt++
    }
  } else if (direction === 'up') {
    for (const id of ids) {
      const idx = list.findIndex((i) => i.orderId === id)
      if (idx > 0) {
        const current = list[idx]
        const previous = list[idx - 1]
        if (!current || !previous) continue
        list[idx - 1] = current
        list[idx] = previous
      }
    }
  } else if (direction === 'down') {
    for (const id of [...ids].reverse()) {
      const idx = list.findIndex((i) => i.orderId === id)
      if (idx >= 0 && idx < list.length - 1) {
        const current = list[idx]
        const next = list[idx + 1]
        if (!current || !next) continue
        list[idx] = next
        list[idx + 1] = current
      }
    }
  } else {
    let insertAt = list.length
    for (const id of [...ids].reverse()) {
      const idx = list.findIndex((i) => i.orderId === id)
      if (idx < 0) continue
      const [item] = list.splice(idx, 1)
      if (!item) continue
      insertAt--
      list.splice(insertAt, 0, item)
    }
  }

  scheduledItems.value = list
}

// ─── machine change ───────────────────────────────────────────────────────────
function changeMachine(mc: number) {
  const mcStr = String(mc)
  for (const item of scheduledItems.value) {
    if (checkedScheduled.value.includes(item.orderId)) {
      item.machineNumber = mcStr
    }
  }
  checkedScheduled.value = []
}

// ─── workflow light toggles ───────────────────────────────────────────────────
function setStep1Status(status: number) {
  for (const item of scheduledItems.value) {
    if (checkedScheduled.value.includes(item.orderId)) {
      item.step1Status = item.step1Status === status ? null : status
    }
  }
  checkedScheduled.value = []
}

function setStep2Status(status: number) {
  for (const item of scheduledItems.value) {
    if (checkedScheduled.value.includes(item.orderId)) {
      item.step2Status = item.step2Status === status ? null : status
    }
  }
  checkedScheduled.value = []
}

function toggleUrgency(level: number) {
  for (const item of scheduledItems.value) {
    if (checkedScheduled.value.includes(item.orderId)) {
      item.urgencyLevel = item.urgencyLevel === level ? 0 : level
    }
  }
  checkedScheduled.value = []
}

// ─── mark completed ───────────────────────────────────────────────────────────
async function markCompleted() {
  const ids = [...checkedScheduled.value]
  if (ids.length === 0) return

  const removed = new Set(ids)
  scheduledItems.value = scheduledItems.value.filter((item) => !removed.has(item.orderId))
  checkedScheduled.value = []

  try {
    await saveScheduleBatch({
      orderType: 0,
      scheduledItems: scheduledItems.value.map((item) => ({
        orderId: item.orderId,
        machineNumber: item.machineNumber,
        step1Status: item.step1Status ?? 0,
        step2Status: item.step2Status ?? 0,
        urgencyLevel: item.urgencyLevel,
      })),
      cancelledOrderIds: [...cancelledOrderIds.value],
      completedOrderIds: ids,
    })
  } catch {
    errorMessage.value = t('scheduler.schedule.completeFailed')
    await load()
  }
}

// ─── save ─────────────────────────────────────────────────────────────────────
function confirmSave() {
  if (scheduledItems.value.length === 0) return
  saveDialog.value = true
}

async function executeSave() {
  saving.value = true
  errorMessage.value = ''

  // Snapshot for rollback
  const snapshot = {
    scheduled: JSON.parse(JSON.stringify(scheduledItems.value)),
    cancelled: new Set(cancelledOrderIds.value)
  }

  try {
    await saveScheduleBatch({
      orderType: 0,
      scheduledItems: scheduledItems.value.map((item) => ({
        orderId: item.orderId,
        machineNumber: item.machineNumber,
        step1Status: item.step1Status ?? 0,
        step2Status: item.step2Status ?? 0,
        urgencyLevel: item.urgencyLevel,
      })),
      cancelledOrderIds: [...cancelledOrderIds.value],
      completedOrderIds: [],
    })

    // Success: Clear cancelled IDs and close dialog without full reload
    cancelledOrderIds.value = new Set()
    saveDialog.value = false
  } catch {
    // Rollback to snapshot on failure
    scheduledItems.value = snapshot.scheduled
    cancelledOrderIds.value = snapshot.cancelled

    errorMessage.value = t('scheduler.schedule.saveFailed')
    saveDialog.value = false
  } finally {
    saving.value = false
  }
}

// ─── job order form toolbar handlers ──────────────────────────────────────────
function showActionNotice(message: string) {
  actionNoticeMessage.value = message
  actionNoticeOpen.value = true
}

function handleAttachment(job: JobDetail) {
  formJob.value = job
  attachmentDialogOpen.value = true
}

function handleProductDetailsEdit(job: JobDetail) {
  formJob.value = job
  productDetailsDialogOpen.value = true
}

function handleRemarksEdit(job: JobDetail) {
  formJob.value = job
  remarksDialogOpen.value = true
}

function handlePrintOrder(job: JobDetail) {
  printManagerJob.value = job
  printManagerOpen.value = true
}

function handleWorkflow(job: JobDetail) {
  void router.push({ name: 'admin-workflow', query: { orderId: job.orderId } })
}

async function handleActionUpdated() {
  if (!formJob.value) return
  try {
    formJob.value = await getJobDetail(formJob.value.orderId)
  } catch {
    // ignore
  }
}

// ─── job order form ──────────────────────────────────────────────────────────
async function openOrderForm(orderId: string) {
  toggleScheduledCheck(orderId)
  return
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

// ─── display helpers ──────────────────────────────────────────────────────────
function workflowColor(status: number | null) {
  if (status == null) return 'grey-lighten-2'
  if (status === 0) return 'error'
  if (status === 1) return 'warning'
  if (status === 2) return 'success'
  if (status === 3) return 'blue'
  return 'grey-lighten-2'
}

function urgencyIcon(level: number) {
  if (level === 4) return 'mdi-bell-alert'
  if (level === 2) return 'mdi-bell'
  return ''
}

function urgencyColor(level: number) {
  if (level === 4) return 'error'
  if (level === 2) return 'warning'
  return 'grey'
}

function machineColor(mc: string) {
  const colors: Record<string, string> = {
    '1': 'success',
    '2': 'blue',
    '3': 'error',
    '4': 'warning',
    '5': 'grey',
  }
  return colors[mc] ?? 'grey'
}

type ScheduledResizableColumn = 'customer' | 'title' | 'printQty' | 'printColor' | 'printSize'

const minScheduledWidths: Record<ScheduledResizableColumn, number> = {
  customer: 120,
  title: 160,
  printQty: 80,
  printColor: 90,
  printSize: 100,
}

function startResize(event: MouseEvent, column: ScheduledResizableColumn) {
  const startX = event.clientX
  const startWidth = scheduledColumnWidths.value[column]

  const onMove = (moveEvent: MouseEvent) => {
    const delta = moveEvent.clientX - startX
    scheduledColumnWidths.value[column] = Math.max(minScheduledWidths[column], startWidth + delta)
  }

  const onUp = () => {
    window.removeEventListener('mousemove', onMove)
    window.removeEventListener('mouseup', onUp)
    document.body.classList.remove('is-resizing-columns')
  }

  document.body.classList.add('is-resizing-columns')
  window.addEventListener('mousemove', onMove)
  window.addEventListener('mouseup', onUp)
}
</script>

<style scoped>
.schedule-page {
  min-height: 0;
}

.schedule-card {
  overflow: hidden;
}

.machine-toggle {
  flex-shrink: 0;
}

.machine-toggle--scroll {
  max-width: 100%;
  overflow-x: auto;
  white-space: nowrap;
}

.machine-toggle--scroll :deep(.v-btn) {
  flex: 0 0 auto;
}

.schedule-layout {
  display: flex;
  gap: 4px;
  height: calc(100vh - 142px);
  min-height: 400px;
}

.schedule-panel {
  flex: 1 1 0;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.scheduled-panel {
  flex: 1 1 0;
}

.action-col {
  flex: 0 0 36px;
  padding-top: 46px;
}

.action-col :deep(.machine-btn),
.schedule-table :deep(.machine-chip) {
  color: #fff !important;
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

.schedule-table {
  width: 100%;
  border-collapse: collapse;
  table-layout: fixed;
  font-size: 12px;
}

.schedule-table thead tr {
  position: sticky;
  top: 0;
  background: rgb(var(--v-theme-surface));
  z-index: 1;
}

.schedule-table th,
.schedule-table td {
  padding: 2px 4px;
  white-space: nowrap;
  border-bottom: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 0;
}

.schedule-table th {
  font-weight: 600;
  text-align: left;
  font-size: 11px;
}

.header-content {
  display: flex;
  align-items: center;
  gap: 4px;
}

.resizable-header {
  position: relative;
}

.resize-handle {
  margin-left: auto;
  width: 6px;
  align-self: stretch;
  cursor: col-resize;
  border-right: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
}

:global(body.is-resizing-columns) {
  user-select: none;
  cursor: col-resize;
}

.schedule-table tbody tr {
  cursor: pointer;
}

.schedule-table tbody tr:hover {
  background: rgba(var(--v-theme-primary), 0.04);
}

.schedule-table tbody tr.row-selected {
  background: rgba(var(--v-theme-primary), 0.1);
}

.col-check { width: 34px; min-width: 34px; }
.col-num   { width: 34px; min-width: 34px; text-align: center; }
.col-order { width: 92px; min-width: 92px; }
.col-customer { width: 150px; min-width: 150px; }
.col-title    { min-width: 140px; }
.col-machine  { width: 34px; min-width: 34px; text-align: center; font-size: 0.9rem !important; }
.col-light    { width: 34px; min-width: 34px; text-align: center; font-size: 0.9rem !important; }
.col-dday { width: 38px; min-width: 38px; text-align: center; }
.col-dday.text-center { text-align: center; }
.col-print-time { width: 34px; min-width: 34px; text-align: right; }
.col-print-qty { width: 88px; min-width: 88px; }
.col-print-color { width: 110px; min-width: 110px; }
.col-print-size { width: 118px; min-width: 118px; }

.light-toolbar {
  flex-shrink: 0;
  flex-wrap: wrap;
}

.dday-overdue {
  color: rgb(var(--v-theme-error)) !important;
}

@keyframes dday-flash {
  0%, 100% { color: rgb(var(--v-theme-error)); }
  50% { color: transparent; }
}

.dday-flash {
  animation: dday-flash 1s infinite;
}

.schedule-layout--phone {
  flex-direction: column;
  height: auto;
  min-height: 0;
  gap: 8px;
}

.schedule-layout--phone .schedule-panel {
  min-height: 260px;
}

.action-col--phone {
  flex-direction: row !important;
  justify-content: flex-start;
  flex-wrap: nowrap;
  width: 100%;
  padding-top: 0;
  overflow-x: auto;
}
</style>