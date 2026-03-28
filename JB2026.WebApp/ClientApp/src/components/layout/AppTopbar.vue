<template>
  <header class="topbar">
    <div>
      <p class="eyebrow mb-1">Phase 6</p>
      <h2 class="text-h5 mb-0">UI Modernization Workspace</h2>
    </div>

    <div class="topbar-actions">
      <div class="text-right" v-if="session.profile">
        <div class="text-subtitle-2">{{ session.profile.displayName }}</div>
        <div class="text-caption text-medium-emphasis">{{ session.profile.role }}</div>
      </div>
      <v-btn v-if="session.isAuthenticated" variant="outlined" color="primary" @click="session.logout()">
        Sign Out
      </v-btn>
      <v-chip color="secondary" variant="flat">SPA host</v-chip>
    </div>
  </header>
</template>

<script setup lang="ts">
import { useSessionStore } from '@/stores/session'

const session = useSessionStore()
</script>