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
        <FullCalendar :options="calendarOptions" />
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import FullCalendar from '@fullcalendar/vue3'
import dayGridPlugin from '@fullcalendar/daygrid'
import interactionPlugin from '@fullcalendar/interaction'
import timeGridPlugin from '@fullcalendar/timegrid'
import type { EventDropArg, EventInput } from '@fullcalendar/core'
import { getScheduleRange, updateScheduleTime } from '@/services/scheduler'

const calendarEvents = ref<EventInput[]>([])
const errorMessage = ref('')
const { t } = useI18n({ useScope: 'global' })

const calendarOptions = computed(() => ({
  plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
  initialView: 'timeGridWeek',
  editable: true,
  height: 'auto',
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