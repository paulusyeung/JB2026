<template>
  <v-card class="dashboard-filters pa-4" rounded="xl" elevation="0">
    <v-row align="center" dense>
      <v-col cols="12" sm="4" md="3">
        <v-select
          v-model="filters.dateRange"
          label="Date Range"
          :items="dateOptions"
          variant="outlined"
          density="compact"
          hide-details
          prepend-inner-icon="mdi-calendar"
        />
      </v-col>
      <v-col v-if="hasStatusFilter" cols="12" sm="4" md="3">
        <v-select
          v-model="filters.status"
          label="Status"
          :items="statusOptions"
          variant="outlined"
          density="compact"
          hide-details
          prepend-inner-icon="mdi-filter-variant"
          multiple
          chips
          closable-chips
        />
      </v-col>
      <v-col cols="12" sm="4" md="4">
        <v-text-field
          v-model="filters.search"
          label="Search jobs, quotes..."
          variant="outlined"
          density="compact"
          hide-details
          prepend-inner-icon="mdi-magnify"
          clearable
        />
      </v-col>
      <v-spacer />
      <v-col cols="auto">
        <v-btn icon="mdi-refresh" variant="text" @click="$emit('refresh')" color="primary" />
      </v-col>
    </v-row>
  </v-card>
</template>

<script setup lang="ts">
import { reactive, watch, computed } from 'vue';

const props = defineProps<{
  initialFilters?: {
    dateRange: string;
    status: string[];
    search: string;
  };
}>();

const emit = defineEmits(['update:filters', 'refresh']);

const filters = reactive({
  dateRange: props.initialFilters?.dateRange || 'Last 30 Days',
  status: props.initialFilters?.status || [],
  search: props.initialFilters?.search || '',
});

const hasStatusFilter = computed(() => {
  return 'status' in (props.initialFilters || {});
});

const dateOptions = [
  'Today',
  'Last 7 Days',
  'Last 30 Days',
  'Last 90 Days',
  'This Year',
  'All Time'
];

const statusOptions = [
  'Pending',
  'In Progress',
  'Completed',
  'Cancelled',
  'On Hold'
];

watch(filters, (newFilters) => {
  emit('update:filters', { ...newFilters });
}, { deep: true });
</script>

