<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title>
        <p class="eyebrow mb-2">{{ t('scheduler.eyebrow') }}</p>
        <h3 class="text-h5 mb-1">{{ t('scheduler.title') }}</h3>
        <p class="text-body-2 text-medium-emphasis mb-0">
          {{ t('scheduler.subtitle') }}
        </p>
      </v-card-title>
      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-4">
          {{ errorMessage }}
        </v-alert>
        <v-alert
          v-if="isNarrowPhoneLayout"
          type="info"
          variant="tonal"
          density="compact"
          class="mb-4"
        >
          {{ t('scheduler.mobilePreferredNotice') }}
        </v-alert>
        <div :class="['scheduler-calendar', { 'scheduler-calendar--phone': isPhoneLayout }]">
          <FullCalendar :options="calendarOptions" />
        </div>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useDisplay } from 'vuetify'
import FullCalendar from '@fullcalendar/vue3'
import dayGridPlugin from '@fullcalendar/daygrid'
import interactionPlugin from '@fullcalendar/interaction'
import timeGridPlugin from '@fullcalendar/timegrid'
import type { EventDropArg, EventInput } from '@fullcalendar/core'
import { getScheduleRange, updateScheduleTime } from '@/services/scheduler'

const calendarEvents = ref<EventInput[]>([])
const errorMessage = ref('')
const { t } = useI18n({ useScope: 'global' })
const display = useDisplay()

const isPhoneLayout = computed(() => display.smAndDown.value)
const isTabletLayout = computed(() => display.mdAndDown.value)
const isNarrowPhoneLayout = computed(() => display.xs.value && display.width.value <= 430)

const calendarOptions = computed(() => ({
  plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
  initialView: isPhoneLayout.value ? 'dayGridMonth' : 'timeGridWeek',
  headerToolbar: isPhoneLayout.value
    ? {
      left: 'prev,next',
      center: 'title',
      right: '',
    }
    : isTabletLayout.value
      ? {
        left: 'prev,next today',
        center: 'title',
        right: 'timeGridWeek,dayGridMonth',
      }
      : {
        left: 'prev,next today',
        center: 'title',
        right: 'dayGridMonth,timeGridWeek,timeGridDay',
      },
  editable: true,
  height: isPhoneLayout.value ? '62vh' : 'auto',
  events: calendarEvents.value,
  eventDrop: async (info: EventDropArg) => {
    try {
      await updateScheduleTime(info.event.id, {
        startOn: info.event.start?.toISOString() ?? info.event.startStr,
        endOn: info.event.end ? info.event.end.toISOString() : null,
      })
      errorMessage.value = ''
    } catch (error) {
      info.revert()
      errorMessage.value = t('scheduler.persistFailed')
      console.error(error)
    }
  },
}))

onMounted(async () => {
  const start = new Date()
  start.setDate(start.getDate() - 1)

  try {
    const rows = await getScheduleRange({
      startOn: start.toISOString().slice(0, 10),
      days: 14,
    })

    calendarEvents.value = rows.map((row) => ({
      id: row.scheduleId,
      title: row.title,
      start: row.startOn,
      end: row.endOn ?? undefined,
    }))

    if (calendarEvents.value.length === 0) {
      calendarEvents.value = [
        {
          id: '00000000-0000-0000-0000-000000000001',
          title: t('scheduler.noSchedulesSample'),
          start: new Date().toISOString(),
          end: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
        },
      ]
    }
  } catch (error) {
    errorMessage.value = t('scheduler.loadFailedFallback')
    calendarEvents.value = [
      {
        id: '00000000-0000-0000-0000-000000000001',
        title: t('scheduler.fallbackEvent'),
        start: new Date().toISOString(),
        end: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      },
    ]
    console.error(error)
  }
})
</script>

<style scoped>
.scheduler-calendar {
  width: 100%;
}

.scheduler-calendar--phone :deep(.fc .fc-toolbar) {
  row-gap: 8px;
}

.scheduler-calendar--phone :deep(.fc .fc-toolbar-title) {
  font-size: 1.05rem;
}

.scheduler-calendar--phone :deep(.fc .fc-button) {
  padding: 0.28rem 0.45rem;
  font-size: 0.78rem;
}
</style>