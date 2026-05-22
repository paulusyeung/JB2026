<template>
  <section class="page-section billing-settings-page">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <span class="text-h6">Billing Settings</span>
        <v-spacer />
        <v-btn variant="tonal" prepend-icon="mdi-heart-pulse" :loading="loading" @click="checkConnectivity">
          Check Connectivity
        </v-btn>
      </v-card-title>

      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>

        <v-card variant="outlined" class="mb-4">
          <v-card-title class="text-subtitle-1">Invoice Ninja Connectivity</v-card-title>
          <v-card-text>
            <div class="d-flex align-center ga-2 mb-2">
              <v-chip :color="isConnected ? 'success' : 'warning'" variant="tonal" size="small">
                {{ isConnected ? 'Connected' : 'Not Connected' }}
              </v-chip>
            </div>
            <div class="text-body-2">{{ statusMessage || 'Run connectivity check to validate integration.' }}</div>
          </v-card-text>
        </v-card>

        <v-card variant="outlined">
          <v-card-title class="text-subtitle-1">Configured Custom-Field Keys (Ops Reference)</v-card-title>
          <v-card-text>
            <v-list density="compact" lines="one">
              <v-list-item title="IN_CF_CLIENT_BILL_TO" subtitle="Client Bill To custom field key" />
              <v-list-item title="IN_CF_CLIENT_SHIP_TO" subtitle="Client Ship To custom field key" />
              <v-list-item title="IN_CF_CLIENT_FAX" subtitle="Client Fax custom field key (future metadata)" />
              <v-list-item title="IN_CF_CONTACT_FULL_NAME" subtitle="Client contact full name custom field key (future metadata)" />
              <v-list-item title="IN_CF_PRODUCT_UNIT" subtitle="Line item unit custom field key (post-v1 follow-up)" />
              <v-list-item title="IN_CF_PRODUCT_PO_NO" subtitle="Line item P.O.No custom field key" />
              <v-list-item title="IN_CF_INVOICE_JOB_NO" subtitle="Invoice Job No custom field key" />
            </v-list>
          </v-card-text>
        </v-card>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { checkBillingConnectivity } from '@/services/billing'

const loading = ref(false)
const errorMessage = ref('')
const isConnected = ref(false)
const statusMessage = ref('')

async function checkConnectivity() {
  loading.value = true
  errorMessage.value = ''
  try {
    const result = await checkBillingConnectivity()
    isConnected.value = result.isConnected
    statusMessage.value = result.statusMessage
  } catch {
    errorMessage.value = 'Failed to check billing connectivity.'
    isConnected.value = false
  } finally {
    loading.value = false
  }
}
</script>
