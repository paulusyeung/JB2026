<template>
  <section class="page-section jobs-layout">
    <JobsTable />

    <v-card rounded="xl" elevation="0" class="panel-card detail-panel">
      <v-card-title>
        <h3 class="text-h6 mb-1">Job detail</h3>
        <p class="text-body-2 text-medium-emphasis mb-0">Matches the representative master-detail workflow from the Phase 2 UI spike.</p>
      </v-card-title>
      <v-card-text v-if="jobsStore.selectedJob">
        <div class="detail-group">
          <div>
            <p class="eyebrow mb-1">Order</p>
            <h4 class="text-h5">{{ jobsStore.selectedJob.orderNumber }}</h4>
          </div>
          <v-chip color="secondary" variant="tonal">Status {{ jobsStore.selectedJob.status }}</v-chip>
        </div>

        <v-row class="mt-2" dense>
          <v-col cols="12" sm="6">
            <label class="field-label">Customer</label>
            <v-text-field :model-value="jobsStore.selectedJob.customerName" variant="outlined" readonly hide-details />
          </v-col>
          <v-col cols="12" sm="6">
            <label class="field-label">Required On</label>
            <v-text-field :model-value="formatDate(jobsStore.selectedJob.requiredOn)" variant="outlined" readonly hide-details />
          </v-col>
          <v-col cols="12">
            <label class="field-label">Remarks</label>
            <v-textarea :model-value="jobsStore.selectedJob.remarks" variant="outlined" rows="4" readonly hide-details />
          </v-col>
        </v-row>

        <v-divider class="my-4" />

        <h5 class="text-subtitle-1 mb-3">Styles</h5>
        <v-chip-group column>
          <v-chip v-for="style in jobsStore.selectedJob.styleTitles" :key="style" color="primary" variant="outlined">
            {{ style }}
          </v-chip>
        </v-chip-group>

        <h5 class="text-subtitle-1 mt-5 mb-3">Attachments</h5>
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
</template>

<script setup lang="ts">
import JobsTable from '@/components/grids/JobsTable.vue'
import { useJobsStore } from '@/stores/jobs'

const jobsStore = useJobsStore()

function formatDate(value: string) {
  return new Date(value).toLocaleDateString()
}
</script>