"<template>
  <!-- Desktop: Table Row -->
  <tr v-if=\"!isMobile\"
      :class=\"['adaptive-row-tr', { 'row-selected': selected }]\"
      @click=\"$emit('click')\">
    <slot name=\"desktop-cells\">\n      <!-- Default cells will be provided by parent -->
    </slot>
  </tr>

  <!-- Mobile: Card -->
  <v-card v-else 
          flat 
          class=\"adaptive-row-card mb-2\" 
          :color=\"selected ? 'primary-lighten-5' : 'surface'\"
          @click=\"$emit('click')\">
    <v-card-text class=\"pa-3\">
      <div class=\"d-flex justify-space-between align-center mb-2\">
        <slot name=\"mobile-header\">\n          <span class=\"text-subtitle-2 font-weight-bold\">Item Details</span>\n        </slot>
        <div class=\"d-flex align-center ga-1\">
          <slot name=\"mobile-actions\" />
        <v-checkbox-btn 
          :model-value=\"selected\" 
          density=\"compact\" 
          hide-details 
          @click.stop=\"$emit('toggle-check')\" 
        />
      </div>
        </div>

      <div class=\"adaptive-grid\">
        <div v-for=\"field in fields\" :key=\"field.key\" class=\"grid-item\">
          <span class=\"field-label\">{{ field.label }}:</span>
          <span class=\"field-value\">
            <slot :name=\"`field-${field.key}`\" :field=\"field\">
              {{ field.value }}
            </slot>
          </span>
        </div>
      </div>
    </v-card-text>
  </v-card>
</template>

<script setup lang=\"ts\">
interface Field {
  key: string;
  label: string;
  value: any;
}

defineProps<{
  isMobile: boolean;
  selected: boolean;
  fields: Field[];
}>();

defineEmits(['click', 'toggle-check']);
</script>

<style scoped>
.adaptive-row-tr {
  cursor: pointer;
  transition: background-color 0.2s;
}

.row-selected {
  background-color: rgba(var(--v-theme-primary), 0.1) !important;
}

.adaptive-row-card {
  cursor: pointer;
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  transition: transform 0.1s, box-shadow 0.2s;
}

.adaptive-row-card:active {
  transform: scale(0.98);
}

.adaptive-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 8px 16px;
}

.grid-item {
  display: flex;
  flex-direction: column;
}

.field-label {
  font-size: 10px;
  text-transform: uppercase;
  color: rgba(var(--v-medium-emphasis-color), var(--v-medium-emphasis-opacity));
  font-weight: 600;
  margin-bottom: 2px;
}

.field-value {
  font-size: 13px;
  font-weight: 500;
  word-break: break-word;
}

/* Special handling for full-width fields if needed */
.grid-item.full-width {
  grid-column: span 2;
}
</style>"