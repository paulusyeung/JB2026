<template>
  <v-card class="dashboard-filters pa-4" rounded="xl" elevation="0">
    <v-row align="center" dense>
      <v-col cols="12" sm="4" md="3">
        <v-select
          v-model="filters.dateRange"
          :label="t('dashboard.filters.dateRangeLabel')"
          :items="dateOptions"
          item-title="title"
          item-value="value"
          variant="outlined"
          density="compact"
          hide-details
          prepend-inner-icon="mdi-calendar"
        />
      </v-col>
      <v-col v-if="hasStatusFilter" cols="12" sm="4" md="3">
        <v-select
          v-model="filters.status"
          :label="t('dashboard.filters.statusLabel')"
          :items="statusOptions"
          item-title="title"
          item-value="value"
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
          :label="t('dashboard.filters.searchLabel')"
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
import { useI18n } from 'vue-i18n';

type DateRangeKey = 'today' | 'last7Days' | 'last30Days' | 'last90Days' | 'thisYear' | 'allTime';
type StatusKey = 'pending' | 'inProgress' | 'completed' | 'cancelled' | 'onHold';

const props = defineProps<{
  initialFilters?: {
    dateRange: DateRangeKey;
    status: StatusKey[];
    search: string;
  };
}>();

const emit = defineEmits(['update:filters', 'refresh']);

const { t } = useI18n({ useScope: 'global' });

const filters = reactive({
  dateRange: props.initialFilters?.dateRange || 'last30Days',
  status: props.initialFilters?.status || [],
  search: props.initialFilters?.search || '',
});

const hasStatusFilter = computed(() => {
  return 'status' in (props.initialFilters || {});
});

const dateOptions = computed(() => [
  { title: t('dashboard.filters.dateRanges.today'), value: 'today' },
  { title: t('dashboard.filters.dateRanges.last7Days'), value: 'last7Days' },
  { title: t('dashboard.filters.dateRanges.last30Days'), value: 'last30Days' },
  { title: t('dashboard.filters.dateRanges.last90Days'), value: 'last90Days' },
  { title: t('dashboard.filters.dateRanges.thisYear'), value: 'thisYear' },
  { title: t('dashboard.filters.dateRanges.allTime'), value: 'allTime' }
] satisfies Array<{ title: string; value: DateRangeKey }>);

const statusOptions = computed(() => [
  { title: t('dashboard.filters.statuses.pending'), value: 'pending' },
  { title: t('dashboard.filters.statuses.inProgress'), value: 'inProgress' },
  { title: t('dashboard.filters.statuses.completed'), value: 'completed' },
  { title: t('dashboard.filters.statuses.cancelled'), value: 'cancelled' },
  { title: t('dashboard.filters.statuses.onHold'), value: 'onHold' }
] satisfies Array<{ title: string; value: StatusKey }>);

watch(filters, (newFilters) => {
  emit('update:filters', { ...newFilters });
}, { deep: true });
</script>

