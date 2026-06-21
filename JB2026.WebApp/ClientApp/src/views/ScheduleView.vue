<template>
  <section class="page-section schedule-page">
    <v-card rounded="xl" elevation="0" class="panel-card schedule-card">
      <!-- Toolbar -->
      <v-card-title class="d-flex flex-wrap align-center ga-2 pa-3">
        <div class="flex-grow-1">
          <h3 class="text-h6 mb-0">{{ t('scheduler.schedule.title') }}</h3>
        </div>

        <v-btn color="primary" prepend-icon="mdi-content-save" :loading="saving" @click="confirmSave">
          {{ t('common.save') }}
        </v-btn>

        <!-- Machine filter -->
        <v-btn-toggle
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

        <v-btn 
          v-if="isPhoneLayout" 
          color="primary" 
          prepend-icon="mdi-plus" 
          @click="availableSheet = true"
        >
          {{ t('scheduler.schedule.available.title') }}
        </v-btn>

        <v-btn 
          v-if="isPhoneLayout && checkedScheduled.length > 0" 
          color="secondary" 
          prepend-icon="mdi-palette-swatch" 
          @click="statusSheet = true"
        >
          {{ t('common.updateStatus') || 'Update Status' }}
        </v-btn>
      </v-card-title>

      <v-divider />

      <v-card-text class="pa-2">
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
          <!-- Available panel -->
          <div v-if="!isPhoneLayout" class="schedule-panel">
            <div class="panel-header text-caption font-weight-bold text-medium-emphasis mb-1">
              {{ t('scheduler.schedule.available.title') }} ({{ availableDisplay.length }})
            </div>
            <div class="list-container">
              <template v-if="!isPhoneLayout">
                <table class="schedule-table">
                  <colgroup>
                    <col class="col-check" />
                    <col class="col-num" />
                    <col class="col-order" />
                    <col :style="{ width: `${availableColumnWidths.customer}px` }" />
                    <col :style="{ width: `${availableColumnWidths.title}px` }" />
                  </colgroup>
                  <thead>
                    <tr>
                      <th class="col-check"><v-checkbox-btn v-model="allAvailableChecked" density="compact" hide-details @click="toggleAllAvailable" /></th>
                      <th class="col-num">#</th>
                      <th class="col-order">{{ t('scheduler.schedule.columns.order') }}</th>
                      <th class="col-customer resizable-header" :style="{ width: `${availableColumnWidths.customer}px` }">
                        <div class="header-content">
                          {{ t('scheduler.schedule.columns.customer') }}
                          <span class="resize-handle" @mousedown.prevent="startResize($event, 'available', 'customer')" />
                        </div>
                      </th>
                      <th class="col-title resizable-header" :style="{ width: `${availableColumnWidths.title}px` }">
                        <div class="header-content">
                          {{ t('scheduler.schedule.columns.title') }}
                          <span class="resize-handle" @mousedown.prevent="startResize($event, 'available', 'title')" />
                        </div>
                      </th>
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
                      <td class="col-customer">{{ item.customerName }}</td>
                      <td class="col-title">{{ item.orderTitle }}</td>
                    </tr>
                  </tbody>
                </table>
              </template>
              <template v-else>
                <div class="pa-2">
                  <AdaptiveRow
                    v-for="item in availableDisplay"
                    :key="item.orderId"
                    :is-mobile="true"
                    :selected="checkedAvailable.includes(item.orderId)"
                    :fields="getAvailableFields(item)"
                    @click="toggleAvailableCheck(item.orderId)"
                    @toggle-check="toggleAvailableCheck(item.orderId)"
                  >
                    <template #mobile-header>
                      <v-btn
                        variant="text"
                        color="primary"
                        density="compact"
                        class="px-0 text-none order-link"
                        @click.stop="openOrderForm(item.orderId)"
                      >
                        {{ item.orderNumber }}
                      </v-btn>
                    </template>
                  </AdaptiveRow>
                </div>
              </template>
            </div>
          </div>

          <!-- Transfer button column -->
          <div :class="['transfer-col', { 'transfer-col--phone': isPhoneLayout }, 'd-flex', 'flex-column', 'align-center', 'justify-center', 'ga-1']">
            <v-tooltip v-for="mc in [1,2,3,4,5]" :key="mc" :text="`→ M${mc}`" location="right">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" :color="machineColor(String(mc))"
                  @click="moveToScheduled(mc)">
                  <span class="text-caption font-weight-bold">{{ mc }}</span>
                </v-btn>
              </template>
            </v-tooltip>
            <v-divider class="my-1 w-100" />
            <v-tooltip :text="t('scheduler.schedule.actions.selectAll')" location="right">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" @click="moveAllToScheduled">
                  <v-icon size="16">mdi-chevron-double-right</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
            <v-tooltip :text="t('scheduler.schedule.actions.unselectOne')" location="right">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" @click="moveToAvailable(false)">
                  <v-icon size="16">mdi-chevron-left</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
            <v-tooltip :text="t('scheduler.schedule.actions.unselectAll')" location="right">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" @click="moveToAvailable(true)">
                  <v-icon size="16">mdi-chevron-double-left</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
            <v-divider class="my-1 w-100" />
            <v-tooltip :text="t('scheduler.schedule.actions.unresolved')" location="right">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" color="warning"
                  @click="unresolveSelected">
                  <v-icon size="16">mdi-alert-circle-outline</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
          </div>

          <!-- Scheduled panel -->
          <div class="schedule-panel scheduled-panel">
            <!-- Workflow light toolbar -->
            <div class="d-flex align-center ga-1 mb-1 light-toolbar">
              <span class="text-caption text-medium-emphasis">@1:</span>
              <v-btn v-for="c in lightColors1" :key="`a1-${c.value}`"
                icon size="x-small" density="compact" :color="c.color" variant="tonal"
                @click="setStep1Status(c.value)">
                <v-icon size="12">mdi-circle</v-icon>
              </v-btn>
              <v-divider vertical class="mx-1" />
              <span class="text-caption text-medium-emphasis">@2:</span>
              <v-btn v-for="c in lightColors2" :key="`a2-${c.value}`"
                icon size="x-small" density="compact" :color="c.color" variant="tonal"
                @click="setStep2Status(c.value)">
                <v-icon size="12">mdi-circle</v-icon>
              </v-btn>
              <v-divider vertical class="mx-1" />
              <v-tooltip :text="t('scheduler.schedule.urgency.red')" location="bottom">
                <template #activator="{ props }">
                  <v-btn v-bind="props" icon size="x-small" density="compact" color="error" variant="tonal"
                    @click="toggleUrgency(4)">
                    <v-icon size="12">mdi-bell-alert</v-icon>
                  </v-btn>
                </template>
              </v-tooltip>
              <v-tooltip :text="t('scheduler.schedule.urgency.yellow')" location="bottom">
                <template #activator="{ props }">
                  <v-btn v-bind="props" icon size="x-small" density="compact" color="warning" variant="tonal"
                    @click="toggleUrgency(2)">
                    <v-icon size="12">mdi-bell</v-icon>
                  </v-btn>
                </template>
              </v-tooltip>
            </div>

            <div class="panel-header text-caption font-weight-bold text-medium-emphasis mb-1">
              {{ t('scheduler.schedule.scheduled.title') }} ({{ scheduledDisplay.length }})
            </div>
            <div class="list-container">
              <template v-if="!isPhoneLayout">
                <table class="schedule-table">
                  <colgroup>
                    <col class="col-check" />
                    <col class="col-num" />
                    <col class="col-order" />
                    <col :style="{ width: `${scheduledColumnWidths.customer}px` }" />
                    <col :style="{ width: `${scheduledColumnWidths.title}px` }" />
                    <col class="col-machine" />
                    <col class="col-light" />
                    <col class="col-light" />
                    <col class="col-light" />
                    <col v-if="!isPhoneLayout" :style="{ width: `${scheduledColumnWidths.printQty}px` }" />
                    <col v-if="!isPhoneLayout" :style="{ width: `${scheduledColumnWidths.printColor}px` }" />
                    <col v-if="!isPhoneLayout" :style="{ width: `${scheduledColumnWidths.printSize}px` }" />
                  </colgroup>
                  <thead>
                    <tr>
                      <th class="col-check"><v-checkbox-btn v-model="allScheduledChecked" density="compact" hide-details @click="toggleAllScheduled" /></th>
                      <th class="col-num">#</th>
                      <th class="col-order">{{ t('scheduler.schedule.columns.order') }}</th>
                      <th class="col-customer resizable-header" :style="{ width: `${scheduledColumnWidths.customer}px` }">
                        <div class="header-content">
                          {{ t('scheduler.schedule.columns.customer') }}
                          <span class="resize-handle" @mousedown.prevent="startResize($event, 'scheduled', 'customer')" />
                        </div>
                      </th>
                      <th class="col-title resizable-header" :style="{ width: `${scheduledColumnWidths.title}px` }">
                        <div class="header-content">
                          {{ t('scheduler.schedule.columns.title') }}
                          <span class="resize-handle" @mousedown.prevent="startResize($event, 'scheduled', 'title')" />
                        </div>
                      </th>
                      <th class="col-machine">M</th>
                      <th class="col-light">@1</th>
                      <th class="col-light">@2</th>
                      <th class="col-light">
                        <v-icon size="14">mdi-bell</v-icon>
                      </th>
                      <th v-if="!isPhoneLayout" class="col-print-qty resizable-header" :style="{ width: `${scheduledColumnWidths.printQty}px` }">
                        <div class="header-content">
                          {{ t('scheduler.schedule.columns.printQty') }}
                          <span class="resize-handle" @mousedown.prevent="startResize($event, 'scheduled', 'printQty')" />
                        </div>
                      </th>
                      <th v-if="!isPhoneLayout" class="col-print-color resizable-header" :style="{ width: `${scheduledColumnWidths.printColor}px` }">
                        <div class="header-content">
                          {{ t('scheduler.schedule.columns.printColor') }}
                          <span class="resize-handle" @mousedown.prevent="startResize($event, 'scheduled', 'printColor')" />
                        </div>
                      </th>
                      <th v-if="!isPhoneLayout" class="col-print-size resizable-header" :style="{ width: `${scheduledColumnWidths.printSize}px` }">
                        <div class="header-content">
                          {{ t('scheduler.schedule.columns.printSize') }}
                          <span class="resize-handle" @mousedown.prevent="startResize($event, 'scheduled', 'printSize')" />
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
                      <td class="col-customer">{{ item.customerName }}</td>
                      <td class="col-title">{{ item.orderTitle }}</td>
                      <td class="col-machine text-center">
                        <v-chip size="x-small" :color="machineColor(item.machineNumber)" variant="tonal">{{ item.machineNumber || '-' }}</v-chip>
                      </td>
                      <td class="col-light text-center">
                        <v-icon size="14" :color="workflowColor(item.step1Status)">mdi-circle</v-icon>
                      </td>
                      <td class="col-light text-center">
                        <v-icon size="14" :color="workflowColor(item.step2Status)">mdi-circle</v-icon>
                      </td>
                      <td class="col-light text-center">
                        <v-icon v-if="urgencyIcon(item.urgencyLevel)" size="14" :color="urgencyColor(item.urgencyLevel)">{{ urgencyIcon(item.urgencyLevel) }}</v-icon>
                        <span v-else>-</span>
                      </td>
                      <td v-if="!isPhoneLayout" class="col-print-qty">{{ item.printQty }}</td>
                      <td v-if="!isPhoneLayout" class="col-print-color">{{ item.printColor }}</td>
                      <td v-if="!isPhoneLayout" class="col-print-size">{{ item.printSize }}</td>
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
            <v-tooltip :text="t('scheduler.schedule.actions.moveTop')" location="left">
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
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact"
                  :color="machineColor(String(mc))"
                  @click="changeMachine(mc)">
                  <span class="text-caption font-weight-bold">{{ mc }}</span>
                </v-btn>
              </template>
            </v-tooltip>
            <v-divider class="my-1 w-100" />
            <v-tooltip :text="t('scheduler.schedule.actions.completed')" location="left">
              <template #activator="{ props }">
                <v-btn v-bind="props" icon size="small" variant="outlined" density="compact" color="success"
                  @click="markCompleted">
                  <v-icon size="16">mdi-check-circle-outline</v-icon>
                </v-btn>
              </template>
            </v-tooltip>
          </div>
        </div>
      </v-card-text>
    </v-card>

    <!-- Mobile Bottom Sheet for Available Jobs -->
    <v-bottom-sheet v-model="availableSheet" max-width="600">
      <v-card title="Available Jobs" class="pa-4">
        <v-card-text>
          <div class="d-flex align-center justify-space-between mb-4">
            <span class="text-subtitle-1 font-weight-bold">
              {{ t('scheduler.schedule.available.title') }} ({{ availableDisplay.length }})
            </span>
            <v-btn icon="mdi-close" variant="text" @click="availableSheet = false" />
          </div>
          
          <div class="list-container" style="border: none; height: 60vh; overflow-y: auto;">
            <div class="pa-2">
              <AdaptiveRow
                v-for="item in availableDisplay"
                :key="item.orderId"
                :is-mobile="true"
                :selected="checkedAvailable.includes(item.orderId)"
                :fields="getAvailableFields(item)"
                @click="toggleAvailableCheck(item.orderId)"
                @toggle-check="toggleAvailableCheck(item.orderId)"
              >
                <template #mobile-header>
                  <v-btn
                    variant="text"
                    color="primary"
                    density="compact"
                    class="px-0 text-none order-link"
                    @click.stop="openOrderForm(item.orderId)"
                  >
                    {{ item.orderNumber }}
                  </v-btn>
                </template>
              </AdaptiveRow>
            </div>
          </div>
        </v-card-text>
        <v-divider />
        <v-card-actions class="pa-4">
          <v-btn variant="text" block @click="availableSheet = false">{{ t('common.cancel') }}</v-btn>
          <v-btn color="primary" variant="elevated" block @click="moveAllToScheduled">
            {{ t('scheduler.schedule.actions.selectAll') }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-bottom-sheet>

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

    <!-- Mobile Bottom Sheet for Status Updates -->
    <v-bottom-sheet v-model="statusSheet" max-width="500">
      <v-card class="pa-4">
        <v-card-title class="d-flex align-center justify-space-between">
          <span class="text-h6">Update Status ({{ checkedScheduled.length }} jobs)</span>
          <v-btn icon="mdi-close" variant="text" @click="statusSheet = false" />
        </v-card-title>
        <v-card-text>
          <WorkflowStatusPicker
            :groups="[
              {
                id: 'step1',
                label: '@1 Status',
                options: [
                  { value: 0, text: 'Error', color: 'error' },
                  { value: 1, text: 'Warning', color: 'warning' },
                  { value: 3, text: 'Info', color: 'info' },
                  { value: 2, text: 'Success', color: 'success' },
                ]
              },
              {
                id: 'step2',
                label: '@2 Status',
                options: [
                  { value: 0, text: 'Error', color: 'error' },
                  { value: 1, text: 'Warning', color: 'warning' },
                  { value: 2, text: 'Success', color: 'success' },
                ]
              },
              {
                id: 'urgency',
                label: 'Urgency',
                options: [
                  { value: 4, text: 'Critical', color: 'error' },
                  { value: 2, text: 'High', color: 'warning' },
                  { value: 0, text: 'Normal', color: 'grey' },
                ]
              }
            ]"
            @select="handleStatusChange"
          />
        </v-card-text>
        <v-divider />
        <v-card-actions class="pa-4">
          <v-btn variant="text" block @click="statusSheet = false">Close</v-btn>
        </v-card-actions>
      </v-card>
    </v-bottom-sheet>

    <!-- Job Order Editor Dialog -->
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
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useDisplay } from 'vuetify'
import { getAvailableSchedule, getOnAirSchedule, saveScheduleBatch } from '@/services/scheduler'
import { getJobDetail } from '@/services/jobs'
import type { JobDetail, JobScheduleAvailableItem, JobScheduleOnAirItem } from '@/types/api'
import AdaptiveRow from '@/components/ui/AdaptiveRow.vue'
import WorkflowStatusPicker from '@/components/ui/WorkflowStatusPicker.vue'
import JobOrderForm from '@/components/forms/JobOrderForm.vue'

const { t } = useI18n({ useScope: 'global' })
const display = useDisplay()
const isPhoneLayout = computed(() => display.smAndDown.value)
const isNarrowPhoneLayout = computed(() => display.xs.value && display.width.value <= 430)

// ─── state ────────────────────────────────────────────────────────────────────
const loading = ref(false)
const saving = ref(false)
const errorMessage = ref('')
const saveDialog = ref(false)
const availableSheet = ref(false)
const statusSheet = ref(false)
const formOpen = ref(false)
const formJob = ref<JobDetail | null>(null)
const machineFilter = ref('0')

const allAvailableItems = ref<JobScheduleAvailableItem[]>([])
const scheduledItems = ref<ScheduledItemState[]>([])
const cancelledOrderIds = ref<Set<string>>(new Set())

const availableColumnWidths = ref({
  customer: 170,
  title: 220,
})

const scheduledColumnWidths = ref({
  customer: 170,
  title: 260,
  printQty: 100,
  printColor: 125,
  printSize: 130,
})

const checkedAvailable = ref<string[]>([])
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
const scheduledOrderIds = computed(() => new Set(scheduledItems.value.map((i) => i.orderId)))

const availableDisplay = computed(() =>
  allAvailableItems.value.filter((item) => !scheduledOrderIds.value.has(item.orderId)),
)

const scheduledDisplay = computed(() => {
  if (machineFilter.value === '0') return scheduledItems.value
  return scheduledItems.value.filter((item) => item.machineNumber === machineFilter.value)
})

const allAvailableChecked = computed(
  () => availableDisplay.value.length > 0 && checkedAvailable.value.length === availableDisplay.value.length,
)

const allScheduledChecked = computed(
  () => scheduledDisplay.value.length > 0 && checkedScheduled.value.length === scheduledDisplay.value.length,
)

// ─── field mappers for adaptive rows ──────────────────────────────────────────
function getAvailableFields(item: JobScheduleAvailableItem) {
  return [
    { key: 'order', label: t('scheduler.schedule.columns.order'), value: item.orderNumber },
    { key: 'customer', label: t('scheduler.schedule.columns.customer'), value: item.customerName },
    { key: 'title', label: t('scheduler.schedule.columns.title'), value: item.orderTitle },
  ]
}

function getScheduledFields(item: ScheduledItemState) {
  return [
    { key: 'order', label: t('scheduler.schedule.columns.order'), value: item.orderNumber },
    { key: 'customer', label: t('scheduler.schedule.columns.customer'), value: item.customerName },
    { key: 'title', label: t('scheduler.schedule.columns.title'), value: item.orderTitle },
    { key: 'machine', label: 'Machine', value: `M${item.machineNumber}` },
    { key: 'qty', label: t('scheduler.schedule.columns.printQty'), value: item.printQty },
    { key: 'color', label: t('scheduler.schedule.columns.printColor'), value: item.printColor },
    { key: 'size', label: t('scheduler.schedule.columns.printSize'), value: item.printSize },
  ]
}

// ─── lifecycle ────────────────────────────────────────────────────────────────
onMounted(() => load())

// Reload on-air list when machine filter changes (available stays the same)
watch(machineFilter, () => {
  checkedScheduled.value = []
})

// ─── load ─────────────────────────────────────────────────────────────────────
async function load() {
  loading.value = true
  errorMessage.value = ''
  checkedAvailable.value = []
  checkedScheduled.value = []
  cancelledOrderIds.value = new Set()

  try {
    const [available, onAir] = await Promise.all([
      getAvailableSchedule(0),
      getOnAirSchedule(0),
    ])

    allAvailableItems.value = available
    scheduledItems.value = onAir.map((item) => ({ ...item }))
  } catch {
    errorMessage.value = t('scheduler.schedule.loadFailed')
  } finally {
    loading.value = false
  }
}

// ─── checkbox helpers ─────────────────────────────────────────────────────────
function toggleAvailableCheck(orderId: string) {
  const idx = checkedAvailable.value.indexOf(orderId)
  if (idx >= 0) checkedAvailable.value.splice(idx, 1)
  else checkedAvailable.value.push(orderId)
}

function toggleScheduledCheck(orderId: string) {
  const idx = checkedScheduled.value.indexOf(orderId)
  if (idx >= 0) checkedScheduled.value.splice(idx, 1)
  else checkedScheduled.value.push(orderId)
}

function toggleAllAvailable() {
  if (allAvailableChecked.value) {
    checkedAvailable.value = []
  } else {
    checkedAvailable.value = availableDisplay.value.map((i) => i.orderId)
  }
}

function toggleAllScheduled() {
  if (allScheduledChecked.value) {
    checkedScheduled.value = []
  } else {
    checkedScheduled.value = scheduledDisplay.value.map((i) => i.orderId)
  }
}

// ─── transfer actions ─────────────────────────────────────────────────────────
function moveToScheduled(machineNumber: number) {
  const mc = String(machineNumber)
  const toMove = allAvailableItems.value.filter(
    (item) => checkedAvailable.value.includes(item.orderId) && !scheduledOrderIds.value.has(item.orderId),
  )

  for (const item of toMove) {
    cancelledOrderIds.value.delete(item.orderId)
    scheduledItems.value.push({
      ...item,
      scheduleId: '',
      priority: scheduledItems.value.length,
      machineNumber: mc,
      urgencyLevel: 0,
      step1Status: null,
      step2Status: null,
      printQty: '',
      printColor: '',
      printSize: '',
    })
  }

  checkedAvailable.value = []
}

function moveAllToScheduled() {
  const mc = machineFilter.value === '0' ? '1' : machineFilter.value
  const toMove = availableDisplay.value.filter((item) => !scheduledOrderIds.value.has(item.orderId))

  for (const item of toMove) {
    cancelledOrderIds.value.delete(item.orderId)
    scheduledItems.value.push({
      ...item,
      scheduleId: '',
      priority: scheduledItems.value.length,
      machineNumber: mc,
      urgencyLevel: 0,
      step1Status: null,
      step2Status: null,
      printQty: '',
      printColor: '',
      printSize: '',
    })
  }

  checkedAvailable.value = []
}

function moveToAvailable(all: boolean) {
  const idsToRemove = all
    ? scheduledDisplay.value.map((i) => i.orderId)
    : checkedScheduled.value

  for (const orderId of idsToRemove) {
    cancelledOrderIds.value.add(orderId)
  }

  scheduledItems.value = scheduledItems.value.filter((item) => !idsToRemove.includes(item.orderId))
  checkedScheduled.value = []
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

function handleStatusChange(groupId: string, value: number | string) {
  const numericValue = typeof value === 'number' ? value : Number(value)
  if (Number.isNaN(numericValue)) {
    return
  }

  if (groupId === 'step1') {
    setStep1Status(numericValue)
  } else if (groupId === 'step2') {
    setStep2Status(numericValue)
  } else if (groupId === 'urgency') {
    toggleUrgency(numericValue)
  }

  statusSheet.value = false
}

// ─── unresolved ───────────────────────────────────────────────────────────────
function unresolveSelected() {
  // Remove checked available items from allAvailableItems (they get reset externally)
  // The legacy behaviour was to reset workflow lights via the backend - we'll just reload
  checkedAvailable.value = []
  load()
}

// ─── mark completed ───────────────────────────────────────────────────────────
function markCompleted() {
  const ids = [...checkedScheduled.value]
  scheduledItems.value = scheduledItems.value.filter((item) => !ids.includes(item.orderId))
  checkedScheduled.value = []
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
    available: JSON.parse(JSON.stringify(allAvailableItems.value)),
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
    })

    // Success: Clear cancelled IDs and close dialog without full reload
    cancelledOrderIds.value = new Set()
    saveDialog.value = false
  } catch (err) {
    // Rollback to snapshot on failure
    scheduledItems.value = snapshot.scheduled
    allAvailableItems.value = snapshot.available
    cancelledOrderIds.value = snapshot.cancelled
    
    errorMessage.value = t('scheduler.schedule.saveFailed')
    saveDialog.value = false
  } finally {
    saving.value = false
  }
}

// ─── job order form ──────────────────────────────────────────────────────────
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

type ResizeTable = 'available' | 'scheduled'
type AvailableResizableColumn = 'customer' | 'title'
type ScheduledResizableColumn = 'customer' | 'title' | 'printQty' | 'printColor' | 'printSize'
type ResizableColumn = AvailableResizableColumn | ScheduledResizableColumn

const minAvailableWidths: Record<AvailableResizableColumn, number> = {
  customer: 120,
  title: 140,
}

const minScheduledWidths: Record<ScheduledResizableColumn, number> = {
  customer: 120,
  title: 160,
  printQty: 80,
  printColor: 90,
  printSize: 100,
}

function startResize(event: MouseEvent, table: ResizeTable, column: ResizableColumn) {
  const startX = event.clientX

  if (table === 'available') {
    const key = column as AvailableResizableColumn
    const startWidth = availableColumnWidths.value[key]

    const onMove = (moveEvent: MouseEvent) => {
      const delta = moveEvent.clientX - startX
      availableColumnWidths.value[key] = Math.max(minAvailableWidths[key], startWidth + delta)
    }

    const onUp = () => {
      window.removeEventListener('mousemove', onMove)
      window.removeEventListener('mouseup', onUp)
      document.body.classList.remove('is-resizing-columns')
    }

    document.body.classList.add('is-resizing-columns')
    window.addEventListener('mousemove', onMove)
    window.addEventListener('mouseup', onUp)
    return
  }

  const key = column as ScheduledResizableColumn
  const startWidth = scheduledColumnWidths.value[key]

  const onMove = (moveEvent: MouseEvent) => {
    const delta = moveEvent.clientX - startX
    scheduledColumnWidths.value[key] = Math.max(minScheduledWidths[key], startWidth + delta)
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
  height: calc(100vh - 200px);
  min-height: 400px;
}

.schedule-panel {
  flex: 1 1 0;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.scheduled-panel {
  flex: 2 1 0;
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
.col-machine  { width: 52px; min-width: 52px; }
.col-light    { width: 34px; min-width: 34px; text-align: center; }
.col-print-qty { width: 88px; min-width: 88px; }
.col-print-color { width: 110px; min-width: 110px; }
.col-print-size { width: 118px; min-width: 118px; }

.light-toolbar {
  flex-shrink: 0;
  flex-wrap: wrap;
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

.transfer-col--phone,
.action-col--phone {
  flex-direction: row !important;
  justify-content: flex-start;
  flex-wrap: nowrap;
  width: 100%;
  padding-top: 0;
  overflow-x: auto;
}
</style>
