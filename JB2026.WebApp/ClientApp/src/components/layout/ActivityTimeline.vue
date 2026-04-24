<template>
  <v-card class="activity-timeline fill-height" rounded="xl" elevation="0">
    <v-card-title class="d-flex align-center py-4 px-6">
      <v-icon icon="mdi-history" class="mr-2" color="primary" />
      <span class="text-h6 font-weight-bold">{{ t('dashboard.activity.title') }}</span>
    </v-card-title>
    <v-divider />
    <v-card-text class="pa-0">
      <v-list v-if="items.length > 0" lines="two" class="bg-transparent">
        <v-list-item v-for="(item, index) in items" :key="index" :prepend-icon="getIcon(item.type)"
          :title="item.title" :subtitle="item.timestamp">
          <template v-slot:append>
            <v-chip size="x-small" :color="getStatusColor(item)" variant="tonal">
              {{ item.status }}
            </v-chip>
          </template>
        </v-list-item>
      </v-list>
      <div v-else class="d-flex flex-column align-center justify-center py-10 text-medium-emphasis">
        <v-icon icon="mdi-tray-blank" size="48" class="mb-2" />
        <p>{{ t('dashboard.activity.empty') }}</p>
      </div>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'

export interface ActivityItem {
  type: 'job' | 'quote' | 'invoice' | 'system'
  title: string
  status: string
  statusTone?: 'success' | 'warning' | 'error' | 'primary'
  timestamp: string
}

defineProps<{
  items: ActivityItem[]
}>()

const { t } = useI18n({ useScope: 'global' })

const getIcon = (type: string) => {
  switch (type) {
    case 'job': return 'mdi-briefcase-outline';
    case 'quote': return 'mdi-file-document-outline';
    case 'invoice': return 'mdi-receipt-outline';
    case 'system': return 'mdi-cog-outline';
    default: return 'mdi-bell-outline';
  }
};

const getStatusColor = (item: ActivityItem) => {
  if (item.statusTone) return item.statusTone;

  const s = item.status.toLowerCase();
  if (s.includes('complete') || s.includes('paid') || s.includes('approved')) return 'success';
  if (s.includes('pending') || s.includes('waiting')) return 'warning';
  if (s.includes('urgent') || s.includes('error') || s.includes('failed')) return 'error';
  return 'primary';
};
</script>

<style scoped>
.activity-timeline {
  max-height: 600px;
  overflow-y: auto;
}
</style>
