"<template>
  <div class=\"status-picker\">
    <div v-for=\"group in groups\" :key=\"group.id\" class=\"status-group mb-4\">
      <div class=\"text-caption font-weight-bold text-medium-emphasis mb-2 ml-1\">
        {{ group.label }}
      </div>
      <div class=\"d-flex ga-2 flex-wrap\">
        <v-btn
          v-for=\"option in group.options\"
          :key=\"option.value\"
          variant=\"tonal\"
          size=\"small\"
          :color=\"option.color\"
          class=\"status-btn\"
          @click=\"$emit('select', group.id, option.value)\"
        >
          <v-icon start size=\"16\">mdi-circle</v-icon>
          {{ option.text }}
        </v-btn>
      </div>
    </div>
  </div>
</template>

<script setup lang=\"ts\">
interface StatusOption {
  value: number | string;
  text: string;
  color: string;
}

interface StatusGroup {
  id: string;
  label: string;
  options: StatusOption[];
}

defineProps<{
  groups: StatusGroup[];
}>();

defineEmits(['select']);
</script>

<style scoped>
.status-btn {
  text-transform: none !important;
  font-weight: 500;
  border-radius: 8px !important;
}
.status-group {
  border-left: 3px solid rgba(var(--v-theme-primary), 0.3);
  padding-left: 12px;
}
</style>"