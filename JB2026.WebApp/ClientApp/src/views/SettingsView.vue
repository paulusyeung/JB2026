<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">Settings</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">System configuration for the modern slice host.</p>
        </div>
      </v-card-title>
      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>
        <v-alert v-if="savedMessage" type="success" variant="tonal" class="mb-3">{{ savedMessage }}</v-alert>

        <v-form @submit.prevent="save">
          <v-row dense>
            <v-col cols="12" md="6">
              <v-text-field v-model="model.companyName" label="Company Name" variant="outlined" density="comfortable" />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field v-model="model.timeZone" label="Time Zone" variant="outlined" density="comfortable" />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field v-model="model.currencyCode" label="Currency" variant="outlined" density="comfortable" />
            </v-col>
          </v-row>

          <v-checkbox v-model="model.enableLegacyFallback" label="Enable legacy fallback" color="primary" hide-details />

          <div class="mt-4 d-flex justify-end">
            <v-btn color="primary" type="submit" :loading="loading">Save settings</v-btn>
          </div>
        </v-form>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getSettings, updateSettings } from '@/services/settings'
import type { AppSettings } from '@/types/api'

const loading = ref(false)
const errorMessage = ref('')
const savedMessage = ref('')
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
    errorMessage.value = 'Unable to load settings. Please verify API availability.'
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
    savedMessage.value = 'Settings saved successfully.'
  } catch {
    errorMessage.value = 'Unable to save settings. Please verify API availability.'
  } finally {
    loading.value = false
  }
}
</script>