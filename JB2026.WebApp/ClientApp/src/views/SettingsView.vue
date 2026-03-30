<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('settings.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('settings.subtitle') }}</p>
        </div>
      </v-card-title>
      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>
        <v-alert v-if="savedMessage" type="success" variant="tonal" class="mb-3">{{ savedMessage }}</v-alert>

        <v-form @submit.prevent="save">
          <v-row dense>
            <v-col cols="12" md="6">
              <v-text-field
                v-model="model.companyName"
                :label="t('settings.fields.companyName')"
                variant="outlined"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field v-model="model.timeZone" :label="t('settings.fields.timeZone')" variant="outlined" density="comfortable" />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field v-model="model.currencyCode" :label="t('settings.fields.currency')" variant="outlined" density="comfortable" />
            </v-col>
          </v-row>

          <v-checkbox v-model="model.enableLegacyFallback" :label="t('settings.fields.enableLegacyFallback')" color="primary" hide-details />

          <div class="mt-4 d-flex justify-end">
            <v-btn color="primary" type="submit" :loading="loading">{{ t('settings.actions.save') }}</v-btn>
          </div>
        </v-form>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { getSettings, updateSettings } from '@/services/settings'
import type { AppSettings } from '@/types/api'

const loading = ref(false)
const errorMessage = ref('')
const savedMessage = ref('')
const { t } = useI18n({ useScope: 'global' })
const model = ref<AppSettings>({
  companyName: '',
  timeZone: '',
  currencyCode: '',
  enableLegacyFallback: false,
})

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''
  savedMessage.value = ''
  try {
    model.value = await getSettings()
  } catch {
    errorMessage.value = t('settings.messages.loadFailed')
  } finally {
    loading.value = false
  }
}

async function save() {
  loading.value = true
  errorMessage.value = ''
  savedMessage.value = ''

  try {
    model.value = await updateSettings(model.value)
    savedMessage.value = t('settings.messages.saveSuccess')
  } catch {
    errorMessage.value = t('settings.messages.saveFailed')
  } finally {
    loading.value = false
  }
}
</script>