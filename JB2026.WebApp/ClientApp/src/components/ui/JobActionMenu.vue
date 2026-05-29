<template>
  <v-menu :activator="activator" location="bottom right" transition="slide-y-transition">
    <v-card min-width="200" class="action-menu-card">
      <v-card-text class="pa-2">
        <div class="text-overline mb-1 px-2 text-center">{{ title }}</div>
        
        <!-- Machine Transfers -->
        <v-list density="compact">
          <v-list-item
            v-for="mc in machines"
            :key="mc.id"
            :value="mc.id"
            @click="$emit('action', 'transfer', mc.id)"
            :color="mc.color"
            class="machine-item"
          >
            <template v-slot:prepend>
              <v-avatar :color="mc.color" size="24" class="text-caption font-weight-bold">
                {{ mc.id }}
              </v-avatar>
            </template>
            <v-list-item-title>{{ `Move to M${mc.id}` }}</v-list-item-title>
          </v-list-item>
        </v-list>

        <v-divider class="my-1" />

        <!-- Other Actions -->
        <v-list density="compact">
          <v-list-item
            v-for="action in otherActions"
            :key="action.id"
            @click="$emit('action', 'custom', action.id)"
            :color="action.color"
          >
            <template v-slot:prepend>
              <v-icon size="20">{{ action.icon }}</v-icon>
            </template>
            <v-list-item-title>{{ action.label }}</v-list-item-title>
          </v-list-item>
        </v-list>
      </v-card-text>
    </v-card>
  </v-menu>
</template>

<script setup lang="ts">
import type { ComponentPublicInstance } from 'vue'

interface Machine {
  id: string;
  color: string;
}

interface CustomAction {
  id: string;
  label: string;
  icon: string;
  color?: string;
}

defineProps<{
  activator?: Element | ComponentPublicInstance | 'parent';
  title: string;
  machines: Machine[];
  otherActions: CustomAction[];
}>();

defineEmits(['action']);
</script>

<style scoped>
.action-menu-card {
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0,0,0,0.1);
}

.machine-item :deep(.v-list-item__overlay) {
  opacity: 0.1;
}

.machine-item {
  transition: background-color 0.2s;
}
</style>