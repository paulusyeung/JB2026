<template>
  <v-dialog
    :model-value="modelValue"
    max-width="min(100%, 720px)"
    persistent
    scrollable
    @update:model-value="onVisibilityChanged"
  >
    <v-card v-draggable-dialog class="job-timeline-dialog">
      <v-card-title class="d-flex align-center ga-2 flex-wrap">
        <v-icon size="20" color="primary">mdi-timeline-clock-outline</v-icon>
        <div class="text-h6">{{ t('jobOrder.jobList.timeline.title') }}</div>
        <v-chip size="small" color="primary" variant="tonal">{{ orderNumber || '-' }}</v-chip>
        <v-spacer />
        <v-btn size="small" icon="mdi-close" variant="tonal" :aria-label="t('common.close')" @click="closeDialog" />
      </v-card-title>

      <v-divider />

      <v-card-text>
        <p class="job-timeline-dialog__subtitle text-body-2 text-medium-emphasis mb-3">
          {{ t('jobOrder.jobList.timeline.subtitle', { order: orderNumber || '-' }) }}
        </p>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>

        <v-progress-linear v-if="loading" color="primary" indeterminate class="mb-1" />

        <v-alert v-else-if="entries.length === 0" type="info" variant="tonal">
          {{ t('jobOrder.jobList.timeline.empty') }}
        </v-alert>

        <ol v-else class="job-timeline">
          <li v-for="entry in entries" :key="entry.fcmHistoryId" class="job-timeline__item">
            <div class="job-timeline__time text-caption">{{ formatTimestamp(entry.deliveredOn) }}</div>
            <div class="job-timeline__rail">
              <span class="job-timeline__dot" />
            </div>
            <div class="job-timeline__body">
              <div class="job-timeline__message">{{ entry.messageBody || '-' }}</div>
              <div v-if="entry.messageTitle" class="job-timeline__title text-caption">
                {{ entry.messageTitle }}
              </div>
            </div>
          </li>
        </ol>
      </v-card-text>

      <v-divider />

      <v-card-actions class="responsive-dialog-actions">
        <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="load">
          {{ t('jobOrder.jobList.timeline.refresh') }}
        </v-btn>
        <v-spacer />
        <v-btn color="primary" variant="outlined" @click="closeDialog">
          {{ t('common.close') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { getJobTimeline } from '@/services/jobOrders'
import type { JobTimelineItem } from '@/types/api'

const props = defineProps<{
  modelValue: boolean
  orderId: string | null
  orderNumber: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
}>()

const { t } = useI18n({ useScope: 'global' })
const { formatWithSeconds } = useGlobalDateFormatter()

const entries = ref<JobTimelineItem[]>([])
const loading = ref(false)
const errorMessage = ref('')

function formatTimestamp(value: string) {
  return formatWithSeconds(value)
}

function onVisibilityChanged(value: boolean) {
  emit('update:modelValue', value)
}

function closeDialog() {
  onVisibilityChanged(false)
}

async function load() {
  const orderId = props.orderId
  if (!orderId) {
    entries.value = []
    errorMessage.value = ''
    return
  }

  loading.value = true
  errorMessage.value = ''
  try {
    entries.value = await getJobTimeline(orderId)
  } catch {
    entries.value = []
    errorMessage.value = t('jobOrder.jobList.timeline.loadFailed')
  } finally {
    loading.value = false
  }
}

watch(
  () => [props.modelValue, props.orderId] as const,
  ([open, orderId]) => {
    // Keep entries while closing: the dialog stays mounted during the leave
    // transition, so clearing here would flash the empty-state alert.
    if (!open) return

    if (orderId) {
      void load()
    }
  },
  { immediate: true },
)
</script>

<style scoped>
.job-timeline-dialog__subtitle {
  margin-block-start: 0;
}

.job-timeline {
  list-style: none;
  margin: 0;
  padding: 0;
}

.job-timeline__item {
  display: grid;
  grid-template-columns: minmax(0, auto) 22px minmax(0, 1fr);
  align-items: start;
}

.job-timeline__time {
  padding: 2px 12px 18px 0;
  text-align: right;
  white-space: nowrap;
  font-variant-numeric: tabular-nums;
  color: rgba(var(--v-theme-on-surface), 0.72);
}

.job-timeline__rail {
  position: relative;
  align-self: stretch;
  display: flex;
  justify-content: center;
}

.job-timeline__rail::before {
  content: '';
  position: absolute;
  inset-block: 0;
  width: 2px;
  background: rgba(var(--v-theme-primary), 0.22);
}

.job-timeline__item:first-child .job-timeline__rail::before {
  inset-block-start: 10px;
}

.job-timeline__item:last-child .job-timeline__rail::before {
  inset-block-end: calc(100% - 10px);
}

.job-timeline__dot {
  position: relative;
  margin-block-start: 4px;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: rgb(var(--v-theme-primary));
  box-shadow: 0 0 0 3px rgba(var(--v-theme-primary), 0.16);
}

.job-timeline__body {
  min-width: 0;
  padding: 0 0 18px 12px;
}

.job-timeline__message {
  font-size: 13px;
  line-height: 1.45;
  color: var(--shell-ink, rgb(var(--v-theme-on-surface)));
  overflow-wrap: anywhere;
}

.job-timeline__title {
  margin-block-start: 2px;
  color: rgba(var(--v-theme-on-surface), 0.6);
}

@media (max-width: 600px) {
  .job-timeline__item {
    grid-template-columns: 22px minmax(0, 1fr);
  }

  .job-timeline__rail {
    grid-column: 1;
    grid-row: 1 / span 2;
  }

  .job-timeline__time {
    grid-column: 2;
    grid-row: 1;
    padding: 0 0 2px 10px;
    text-align: left;
    white-space: normal;
  }

  .job-timeline__body {
    grid-column: 2;
    grid-row: 2;
    padding: 0 0 16px 10px;
  }
}
</style>