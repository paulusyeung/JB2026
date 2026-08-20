<template>
  <section class="page-section system-monitor-page">
    <div class="d-flex flex-wrap align-center ga-3">
      <h2 class="text-h5">
        {{ t('routes.systemMonitor') }}
      </h2>
      <v-spacer />
      <v-btn
        variant="tonal"
        prepend-icon="mdi-refresh"
        :loading="loading"
        @click="load"
      >
        {{ t('systemMonitor.refresh') }}
      </v-btn>
    </div>

    <v-alert
      v-if="errorMessage"
      type="warning"
      variant="tonal"
      class="mb-3"
    >
      {{ errorMessage }}
    </v-alert>

    <div class="system-monitor-grid">
      <div class="system-monitor-pane">
        <BillingSettingsView class="h-100" />
      </div>

      <div class="system-monitor-pane">
        <SettingsParameterCard
          :title="t('systemMonitor.crmTitle')"
          icon="mdi-account-group-outline"
          :configured="settings?.crm.configured ?? false"
          :loading="loading"
          :error-message="errorMessage"
          :rows="crmRows"
        />
      </div>

      <div class="system-monitor-pane">
        <SettingsParameterCard
          :title="t('systemMonitor.dmsTitle')"
          icon="mdi-file-cabinet"
          :configured="settings?.dms.configured ?? false"
          :loading="loading"
          :error-message="errorMessage"
          :rows="dmsRows"
        />
      </div>

      <div class="system-monitor-pane">
        <SettingsParameterCard
          :title="t('systemMonitor.emailTitle')"
          icon="mdi-email-outline"
          :configured="settings?.email.configured ?? false"
          :loading="loading"
          :error-message="errorMessage"
          :rows="emailRows"
        />
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import BillingSettingsView from '@/views/BillingSettingsView.vue'
import SettingsParameterCard from '@/components/settings/SettingsParameterCard.vue'
import { getSystemMonitorSettings, type SystemMonitorSettings } from '@/services/systemMonitor'

const { t } = useI18n({ useScope: 'global' })

const loading = ref(false)
const errorMessage = ref('')
const settings = ref<SystemMonitorSettings | null>(null)

const placeholder = '—'

const crmRows = computed(() => {
  const crm = settings.value?.crm
  return [
    { label: t('systemMonitor.fields.baseUrl'), value: crm?.baseUrl || placeholder },
    { label: t('systemMonitor.fields.apiKey'), value: crm?.apiKey || placeholder },
    { label: t('systemMonitor.fields.httpClientTimeoutSeconds'), value: crm ? String(crm.httpClientTimeoutSeconds) : placeholder },
  ]
})

const dmsRows = computed(() => {
  const dms = settings.value?.dms
  return [
    { label: t('systemMonitor.fields.baseUrl'), value: dms?.baseUrl || placeholder },
    { label: t('systemMonitor.fields.apiToken'), value: dms?.apiToken || placeholder },
    { label: t('systemMonitor.fields.defaultUser'), value: dms?.defaultUser || placeholder },
    { label: t('systemMonitor.fields.httpClientTimeoutSeconds'), value: dms ? String(dms.httpClientTimeoutSeconds) : placeholder },
  ]
})

const emailRows = computed(() => {
  const email = settings.value?.email
  return [
    { label: t('systemMonitor.fields.baseUrl'), value: email?.baseUrl || placeholder },
    { label: t('systemMonitor.fields.fallbackAccountEmail'), value: email?.fallbackAccountEmail || placeholder },
    { label: t('systemMonitor.fields.fallbackAccountPassword'), value: email?.fallbackAccountPassword || placeholder },
    { label: t('systemMonitor.fields.imapPort'), value: email ? String(email.imapPort) : placeholder },
    { label: t('systemMonitor.fields.useSsl'), value: email ? String(email.useSsl) : placeholder },
    { label: t('systemMonitor.fields.httpClientTimeoutSeconds'), value: email ? String(email.httpClientTimeoutSeconds) : placeholder },
  ]
})

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    settings.value = await getSystemMonitorSettings()
  } catch {
    settings.value = null
    errorMessage.value = t('systemMonitor.loadFailed')
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.system-monitor-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1.5rem;
}

.system-monitor-pane {
  min-width: 0;
}

@media (max-width: 960px) {
  .system-monitor-grid {
    grid-template-columns: 1fr;
  }
}
</style>