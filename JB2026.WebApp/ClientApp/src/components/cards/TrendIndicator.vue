<template>
  <div v-if="value !== undefined" class="trend-indicator d-flex align-center" :class="trendClass">
    <v-icon :icon="trendIcon" size="16" class="mr-1" />
    <span class="text-caption font-weight-bold">{{ formattedValue }}</span>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  value?: number;
  inverse?: boolean; // If true, negative values are considered positive (e.g., error rate)
}>();

const isPositive = computed(() => (props.value || 0) > 0);
const isNegative = computed(() => (props.value || 0) < 0);

const color = computed(() => {
  if (isPositive.value) return props.inverse ? 'error' : 'success';
  if (isNegative.value) return props.inverse ? 'success' : 'error';
  return 'medium-emphasis';
});

const trendClass = computed(() => `text-${color.value}`);

const trendIcon = computed(() => {
  if (isPositive.value) return 'mdi-arrow-up';
  if (isNegative.value) return 'mdi-arrow-down';
  return 'mdi-minus';
});

const formattedValue = computed(() => {
  if (props.value === undefined) return '';
  const absValue = Math.abs(props.value);
  return `${absValue}%`;
});
</script>

<style scoped>
.trend-indicator {
  display: inline-flex;
}
</style>
