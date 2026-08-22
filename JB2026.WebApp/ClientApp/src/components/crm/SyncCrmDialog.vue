<template>
  <v-card rounded="lg" elevation="0" class="sync-crm-dialog">
    <v-card-title class="d-flex align-center ga-3 pb-2">
      <v-icon color="primary" size="28">mdi-cloud-sync</v-icon>
      <div>
        <h2 class="text-h6 mb-1">{{ t('admin.user.actions.syncCrm') }}</h2>
        <p class="text-body-2 text-medium-emphasis mb-0">{{ userEmail }}</p>
      </div>
      <v-spacer />
      <v-btn variant="tonal" icon="mdi-close" @click="emit('cancel')" />
    </v-card-title>

    <v-card-text class="pt-2">
      <p class="text-body-1 mb-4">
        {{ t('admin.user.messages.syncCrmConfirm', { email: userEmail }) }}
      </p>

      <v-alert v-if="resultMessage" :type="resultSuccess ? 'success' : 'error'" variant="tonal" class="mb-2">
        {{ resultMessage }}
      </v-alert>
    </v-card-text>

    <v-card-actions class="pa-4 d-flex ga-2">
      <v-spacer />
      <v-btn v-if="!resultMessage" variant="text" :disabled="syncing" @click="emit('cancel')">
        {{ t('common.cancel') }}
      </v-btn>
      <v-btn v-if="!resultMessage" color="primary" variant="flat" prepend-icon="mdi-check" disabled>
        {{ t('admin.user.actions.syncCrmProceed') }}
      </v-btn>
      <v-btn v-else variant="text" @click="emitDone">
        {{ t('common.close') }}
      </v-btn>
    </v-card-actions>
  </v-card>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'


const props = defineProps<{ userId: string; userEmail: string }>()

const emit = defineEmits<{
  (e: 'cancel'): void
  (e: 'done'): void
}>()

const { t } = useI18n({ useScope: 'global' })

const syncing = ref(false)
const resultMessage = ref('')
const resultSuccess = ref(false)

function emitDone() {
  emit('done')
}
</script>

<style scoped>
.sync-crm-dialog {
  min-width: 400px;
  max-width: 520px;
}
</style>
