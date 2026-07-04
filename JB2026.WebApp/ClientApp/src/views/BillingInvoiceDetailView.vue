<template>
  <section class="page-section billing-invoice-detail-page">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <v-btn variant="text" prepend-icon="mdi-arrow-left" @click="goBack">Back</v-btn>
        <span class="text-h6">Invoice Detail</span>
        <v-spacer />
        <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="refresh">
          Refresh Status
        </v-btn>
      </v-card-title>

      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>

        <v-skeleton-loader v-if="loading && !summary" type="table-heading, list-item-three-line" />

        <v-row v-else-if="summary" class="ga-2">
          <v-col cols="12" md="6">
            <v-card variant="outlined">
              <v-card-title class="text-subtitle-1">Invoice</v-card-title>
              <v-card-text class="d-grid ga-2">
                <div><strong>ID:</strong> {{ summary.externalInvoiceId }}</div>
                <div><strong>Number:</strong> {{ summary.invoiceNumber || '-' }}</div>
                <div>
                  <strong>Status:</strong>
                  <v-chip size="small" class="ml-2" :color="statusColor(summary.status)" variant="tonal">
                    {{ summary.status || 'Unknown' }}
                  </v-chip>
                </div>
                <div><strong>Amount:</strong> {{ formatCurrency(summary.amount) }}</div>
                <div><strong>Due Date:</strong> {{ summary.dueDate ? format(summary.dueDate) : '-' }}</div>
                <div>
                  <strong>Last Synced:</strong>
                  {{ summary.lastSyncedAt ? format(summary.lastSyncedAt, DATE_FORMATS.SHORT_DATETIME) : '-' }}
                </div>
              </v-card-text>
            </v-card>
          </v-col>
        </v-row>

        <v-alert v-else type="info" variant="tonal">No billing summary found for this invoice.</v-alert>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { getInvoiceSummary, refreshInvoiceStatus, type InvoiceBillingSummary } from '@/services/billing'

const route = useRoute()
const router = useRouter()
const { formatCurrency } = useLocaleFormatters()
const { format, DATE_FORMATS } = useGlobalDateFormatter()

const loading = ref(false)
const errorMessage = ref('')
const summary = ref<InvoiceBillingSummary | null>(null)

onMounted(async () => {
  await loadSummary()
})

async function loadSummary() {
  const externalInvoiceId = String(route.params.externalInvoiceId ?? '')
  if (!externalInvoiceId) {
    errorMessage.value = 'Missing invoice ID.'
    return
  }

  loading.value = true
  errorMessage.value = ''
  try {
    summary.value = await getInvoiceSummary(externalInvoiceId)
  } catch {
    errorMessage.value = 'Failed to load invoice summary.'
  } finally {
    loading.value = false
  }
}

async function refresh() {
  const externalInvoiceId = String(route.params.externalInvoiceId ?? '')
  if (!externalInvoiceId) {
    errorMessage.value = 'Missing invoice ID.'
    return
  }

  loading.value = true
  errorMessage.value = ''
  try {
    summary.value = await refreshInvoiceStatus(externalInvoiceId)
  } catch {
    errorMessage.value = 'Failed to refresh invoice status.'
  } finally {
    loading.value = false
  }
}

function goBack() {
  void router.push({ name: 'billing-invoices' })
}

function statusColor(status: string) {
  const normalized = status.toLowerCase()
  if (normalized.includes('paid')) return 'success'
  if (normalized.includes('overdue')) return 'error'
  if (normalized.includes('sent') || normalized.includes('view')) return 'info'
  if (normalized === 'cancelled' || normalized === 'reversed') return 'default'
  return 'warning'
}
</script>
