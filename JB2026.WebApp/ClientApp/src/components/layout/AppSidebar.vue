<template>
  <v-navigation-drawer permanent rail-width="82" width="290" class="app-sidebar">
    <div class="brand-lockup">
      <div class="brand-mark">JB</div>
      <div>
        <p class="eyebrow mb-1">Migration Slice Host</p>
        <h1 class="brand-name">JB2026</h1>
      </div>
    </div>

    <v-list nav density="comfortable">
      <v-list-item
        v-for="item in items"
        :key="item.to"
        :prepend-icon="item.icon"
        :title="item.title"
        :to="item.to"
        rounded="xl"
      />
    </v-list>

    <template #append>
      <div class="sidebar-card">
        <p class="eyebrow mb-2">Coexistence</p>
        <p class="mb-2">Disabled slices stay on the legacy route until the flag flips.</p>
        <v-btn block variant="tonal" color="primary" href="/">Control Plane</v-btn>
      </div>
    </template>
  </v-navigation-drawer>
</template>

<script setup lang="ts">
const items = [
  { title: 'Dashboard', to: '/dashboard', icon: 'mdi-view-dashboard-outline' },
  { title: 'Jobs', to: '/jobs', icon: 'mdi-briefcase-outline' },
  { title: 'Quotations', to: '/quotations', icon: 'mdi-file-document-multiple-outline' },
  { title: 'Rich Text', to: '/editor', icon: 'mdi-text-box-edit-outline' },
  { title: 'Scheduler', to: '/scheduler', icon: 'mdi-calendar-clock-outline' },
]
</script>