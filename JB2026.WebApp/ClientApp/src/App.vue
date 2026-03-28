<template>
  <v-app>
    <v-layout class="app-shell">
      <AppSidebar />
      <v-main>
        <div class="app-frame">
          <AppTopbar />
          <router-view />
        </div>
      </v-main>
    </v-layout>
  </v-app>
</template>

<script setup lang="ts">
import AppSidebar from '@/components/layout/AppSidebar.vue'
import AppTopbar from '@/components/layout/AppTopbar.vue'
</script>