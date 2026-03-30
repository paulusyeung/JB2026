<template>
  <section class="page-section jobs-layout">
    <JobsTable />

    <v-card rounded="xl" elevation="0" class="panel-card detail-panel">
      <v-card-title class="d-flex align-start ga-2">
        <div class="flex-grow-1">
          <h3 class="text-h6 mb-1">{{ t('jobs.detailTitle') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('jobs.detailSubtitle') }}</p>
        </div>
        <div class="d-flex ga-2 flex-shrink-0 pt-1">
          <v-btn
            size="small"
            variant="outlined"
            prepend-icon="mdi-plus"
            @click="openCreate"
          >{{ t('jobs.new') }}</v-btn>
          <v-btn
            v-if="jobsStore.selectedJob"
            size="small"
            color="primary"
            variant="tonal"
            prepend-icon="mdi-pencil"
            @click="openEdit"
          >{{ t('jobs.edit') }}</v-btn>
        </div>
      </v-card-title>
      <v-card-text v-if="jobsStore.selectedJob">
        <div class="detail-group">
          <div>
            <p class="eyebrow mb-1">{{ t('jobs.order') }}</p>
            <h4 class="text-h5">{{ jobsStore.selectedJob.orderNumber }}</h4>
          </div>
          <v-chip color="secondary" variant="tonal">{{ t('jobs.status', { value: jobsStore.selectedJob.status }) }}</v-chip>
        </div>

        <v-row class="mt-2" dense>
          <v-col cols="12" sm="6">
            <label class="field-label">{{ t('jobs.customer') }}</label>
            <v-text-field :model-value="jobsStore.selectedJob.customerName" variant="outlined" readonly hide-details />
          </v-col>
          <v-col cols="12" sm="6">
            <label class="field-label">{{ t('jobs.requiredOn') }}</label>
            <v-text-field :model-value="formatDate(jobsStore.selectedJob.requiredOn)" variant="outlined" readonly hide-details />
          </v-col>
          <v-col cols="12">
            <label class="field-label">{{ t('jobs.remarks') }}</label>
            <v-textarea :model-value="jobsStore.selectedJob.remarks" variant="outlined" rows="4" readonly hide-details />
          </v-col>
        </v-row>

        <v-divider class="my-4" />

        <h5 class="text-subtitle-1 mb-3">{{ t('jobs.styles') }}</h5>
        <v-chip-group column>
          <v-chip v-for="style in jobsStore.selectedJob.styleTitles" :key="style" color="primary" variant="outlined">
            {{ style }}
          </v-chip>
        </v-chip-group>

        <h5 class="text-subtitle-1 mt-5 mb-3">{{ t('jobs.attachments') }}</h5>
        <v-list lines="two">
          <v-list-item
            v-for="attachment in jobsStore.selectedJob.attachments"
            :key="`${attachment.fileName}-${attachment.uploadedOn}`"
            :title="attachment.fileName"
            :subtitle="`${attachment.attachmentType} • ${attachment.uploadedBy}`"
            prepend-icon="mdi-paperclip"
          />
        </v-list>
      </v-card-text>
      <v-card-text v-else>
        <v-skeleton-loader type="article" />
      </v-card-text>
    </v-card>
  </section>

  <!-- Create / Edit job order dialog (Slice B — DevExpress form replacement) -->
  <v-dialog v-model="formOpen" max-width="760" scrollable>
    <JobOrderForm
      :job="formJob"
      @saved="handleSaved"
      @cancel="formOpen = false"
    />
  </v-dialog>

  <!-- Save-success snackbar -->
  <v-snackbar v-model="saveSuccess" color="success" timeout="3000">
    {{ t('jobs.saved') }}
    <template #actions>
      <v-btn variant="text" @click="saveSuccess = false">{{ t('jobs.dismiss') }}</v-btn>
    </template>
  </v-snackbar>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import JobsTable from '@/components/grids/JobsTable.vue'
import JobOrderForm from '@/components/forms/JobOrderForm.vue'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { useJobsStore } from '@/stores/jobs'
import type { JobDetail } from '@/types/api'

const jobsStore = useJobsStore()
const { t } = useI18n({ useScope: 'global' })
const { formatDate: formatDateByLocale } = useLocaleFormatters()

const formOpen = ref(false)
const formJob = ref<JobDetail | null>(null)
const saveSuccess = ref(false)

function openCreate() {
  formJob.value = null
  formOpen.value = true
}

function openEdit() {
  formJob.value = jobsStore.selectedJob
  formOpen.value = true
}

async function handleSaved() {
  formOpen.value = false
  saveSuccess.value = true
  // Reload the selected job to reflect any changes
  if (jobsStore.selectedJob) {
    await jobsStore.select(jobsStore.selectedJob.orderId)
  }
}

function formatDate(value: string) {
  return formatDateByLocale(value)
}
</script>