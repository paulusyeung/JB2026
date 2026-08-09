<template>
  <v-card
    class="metric-card"
    rounded="xl"
    elevation="0"
    :class="{ 'metric-card--clickable': clickable }"
    @click="emit('click')"
  >
    <v-card-text>
      <div class="d-flex justify-space-between align-start mb-4">
        <div>
          <p class="eyebrow mb-2">{{ label }}</p>
          <div class="d-flex align-center">
            <h3 class="text-h4 font-weight-bold mr-3">{{ value }}</h3>
            <TrendIndicator v-if="trend !== undefined" :value="trend" :inverse="inverse" />
          </div>
        </div>
        <v-icon :icon="icon" :color="statusColor || 'primary'" size="28" />
      </div>
      <p class="text-body-2 text-medium-emphasis mb-0">{{ helper }}</p>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import TrendIndicator from './TrendIndicator.vue';

defineProps<{
  label: string
  value: string
  helper: string
  icon: string
  trend?: number
  inverse?: boolean
  statusColor?: string
  clickable?: boolean
}>()

const emit = defineEmits<{
  (e: 'click'): void
}>()
</script>

<style scoped>
.metric-card--clickable {
  cursor: pointer;
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.metric-card--clickable:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12) !important;
}
</style>