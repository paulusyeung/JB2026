<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title>
        <p class="eyebrow mb-2">Slice C</p>
        <h3 class="text-h5 mb-1">Scheduler baseline</h3>
        <p class="text-body-2 text-medium-emphasis mb-0">
          FullCalendar is wired for resource-style scheduling, but the Phase 6 spec still needs a licensing decision for the premium timeline/resource plugins.
        </p>
      </v-card-title>
      <v-card-text>
        <FullCalendar :options="calendarOptions" />
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import FullCalendar from '@fullcalendar/vue3'
import dayGridPlugin from '@fullcalendar/daygrid'
import interactionPlugin from '@fullcalendar/interaction'
import timeGridPlugin from '@fullcalendar/timegrid'

const calendarOptions = {
  plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
  initialView: 'timeGridWeek',
  editable: true,
  height: 'auto',
  events: [
    {
      id: 'demo-1',
      title: 'Press check',
      start: new Date().toISOString(),
      end: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    },
  ],
  eventDrop(info: { event: { id: string; startStr: string; endStr: string | null } }) {
    console.info('Persist move through API', {
      id: info.event.id,
      start: info.event.startStr,
      end: info.event.endStr,
    })
  },
}
</script>