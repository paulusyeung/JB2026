<template>
  <v-card class="metric-card" rounded="xl" elevation="0">
    <v-card-text>
      <div class="d-flex justify-space-between align-start mb-4">
        <div>
          <p class="eyebrow mb-2">{{ label }}</p>
          <h3 class="text-h4 font-weight-bold">{{ value }}</h3>
        </div>
        <v-icon :icon="icon" color="primary" size="28" />
      </div>
      <p class="text-body-2 text-medium-emphasis mb-0">{{ helper }}</p>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
defineProps<{
  label: string
  value: string
  helper: string
  icon: string
}>()
</script>