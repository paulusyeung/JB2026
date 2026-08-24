<template>
  <v-card class="workflow-record-dialog legacy-dialog">
    <v-card-title class="legacy-titlebar d-flex align-center py-2 px-3">
      <span class="text-subtitle-1">{{ isNew ? t('admin.workflow.form.newTitle') : t('admin.workflow.form.editTitle') }}</span>
      <v-spacer />
      <v-btn size="small" variant="tonal" icon="mdi-close" @click="emit('cancel')" />
    </v-card-title>

    <v-card-text class="pa-3 legacy-content">
      <div class="legacy-toolbar d-flex flex-wrap ga-2 mb-3">
        <v-btn size="small" color="primary" prepend-icon="mdi-content-save" :loading="saving" @click="handleSave()">
          {{ t('admin.workflow.form.save') }}
        </v-btn>
        <v-btn size="small" variant="tonal" prepend-icon="mdi-content-save-check" :loading="saving" @click="handleSave(true)">
          {{ t('admin.workflow.form.saveClose') }}
        </v-btn>
        <v-btn size="small" variant="outlined" prepend-icon="mdi-delete" :loading="deleting" :disabled="isNew" @click="handleDelete">
          {{ t('admin.workflow.form.delete') }}
        </v-btn>
        <v-btn size="small" variant="outlined" prepend-icon="mdi-form-select" :disabled="isNew" @click="workflowFormsOpen = true">
          {{ t('admin.workflow.form.workflowForms') }}
        </v-btn>
      </div>

      <v-row dense class="legacy-form-grid">
        <v-col cols="12" md="2" class="legacy-label">{{ t('admin.workflow.form.workflowName') }}:</v-col>
        <v-col cols="12" md="10">
          <v-text-field
            v-model="draft.workflowName"
            maxlength="64"
            variant="outlined"
            density="compact"
            hide-details="auto"
            :rules="[required]"
          />
        </v-col>

        <v-col cols="12" md="2" class="legacy-label">{{ t('admin.workflow.form.workTitle') }}:</v-col>
        <v-col cols="12" md="10">
          <v-textarea
            v-model="draft.workTitle"
            rows="3"
            maxlength="512"
            variant="outlined"
            density="compact"
            no-resize
            hide-details="auto"
            :rules="[required]"
          />
        </v-col>

        <v-col cols="12" md="2" class="legacy-label">{{ t('admin.workflow.form.workInstruction') }}:</v-col>
        <v-col cols="12" md="10">
          <v-textarea
            v-model="draft.workInstruction"
            rows="3"
            maxlength="512"
            variant="outlined"
            density="compact"
            no-resize
            hide-details="auto"
            :rules="[required]"
          />
        </v-col>
      </v-row>

      <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mt-3">{{ errorMessage }}</v-alert>
    </v-card-text>

    <v-card-actions class="pa-4 d-flex ga-2 responsive-dialog-actions">
      <v-spacer />
      <v-btn variant="text" @click="emit('cancel')">{{ t('admin.workflow.form.cancel') }}</v-btn>
    </v-card-actions>

    <v-dialog v-model="workflowFormsOpen" max-width="min(100%, 1280px)" scrollable>
      <AdminWorkflowFormsDialog
        v-if="workflowFormsOpen && currentWorkflowId"
        :workflow-id="currentWorkflowId"
        :workflow-name="draft.workflowName"
        @saved="handleWorkflowFormsSaved"
        @cancel="workflowFormsOpen = false"
      />
    </v-dialog>

    <v-snackbar v-model="workflowFormsSaved" color="success" timeout="3000">
      {{ t('admin.workflow.workflowFormsDialog.saveSuccess') }}
      <template #actions>
        <v-btn variant="text" @click="workflowFormsSaved = false">{{ t('common.cancel') }}</v-btn>
      </template>
    </v-snackbar>
  </v-card>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { createAdminWorkflow, deleteAdminWorkflow, updateAdminWorkflow } from '@/services/admin'
import type { AdminWorkflowListItem } from '@/types/api'
import AdminWorkflowFormsDialog from '@/components/forms/AdminWorkflowFormsDialog.vue'

const props = defineProps<{
  item: AdminWorkflowListItem | null
}>()

const emit = defineEmits<{
  (e: 'saved', item: AdminWorkflowListItem): void
  (e: 'deleted', id: string): void
  (e: 'cancel'): void
}>()

const { t } = useI18n({ useScope: 'global' })

const saving = ref(false)
const deleting = ref(false)
const errorMessage = ref('')
const workflowFormsOpen = ref(false)
const workflowFormsSaved = ref(false)

const currentWorkflowId = ref<string | null>(null)

const draft = reactive({
  workflowName: '',
  workTitle: '',
  workInstruction: '',
})

const isNew = computed(() => props.item === null)

watch(
  () => props.item,
  (item) => {
    draft.workflowName = item?.workflowName ?? ''
    draft.workTitle = item?.workTitle ?? ''
    draft.workInstruction = item?.workInstruction ?? ''
    currentWorkflowId.value = item?.workflowId ?? null
    errorMessage.value = ''
    workflowFormsOpen.value = false
  },
  { immediate: true },
)

const required = (value: string) => value.trim().length > 0 || t('admin.workflow.form.required')

async function handleSave(closeAfter = false) {
  if (!draft.workflowName.trim() || !draft.workTitle.trim() || !draft.workInstruction.trim()) {
    errorMessage.value = t('admin.workflow.form.required')
    return
  }

  saving.value = true
  errorMessage.value = ''

  try {
    const payload = {
      workflowName: draft.workflowName.trim(),
      workTitle: draft.workTitle.trim(),
      workInstruction: draft.workInstruction.trim(),
    }

    const result = isNew.value
      ? await createAdminWorkflow(payload)
      : await updateAdminWorkflow(currentWorkflowId.value!, payload)

    currentWorkflowId.value = result.workflowId
    emit('saved', {
      workflowId: result.workflowId,
      workflowName: result.workflowName,
      workTitle: result.workTitle,
      workInstruction: result.workInstruction,
    })

    if (closeAfter) {
      emit('cancel')
    }
  } catch {
    errorMessage.value = t('admin.workflow.messages.saveFailed')
  } finally {
    saving.value = false
  }
}

async function handleDelete() {
  if (!currentWorkflowId.value) {
    return
  }

  if (!window.confirm(t('admin.workflow.form.deleteConfirm'))) {
    return
  }

  deleting.value = true
  errorMessage.value = ''

  try {
    await deleteAdminWorkflow(currentWorkflowId.value)
    emit('deleted', currentWorkflowId.value)
  } catch {
    errorMessage.value = t('admin.workflow.messages.deleteFailed')
  } finally {
    deleting.value = false
  }
}

function handleWorkflowFormsSaved() {
  workflowFormsOpen.value = false
  workflowFormsSaved.value = true
}
</script>

<style scoped>
.legacy-dialog {
  background: rgb(var(--v-theme-surface));
}

.legacy-titlebar {
  background: linear-gradient(180deg, rgba(var(--v-theme-primary), 0.12), rgba(var(--v-theme-primary), 0.22));
  border-bottom: 1px solid rgba(var(--v-theme-on-surface), 0.2);
}

.legacy-content {
  background: rgb(var(--v-theme-surface));
}

.legacy-toolbar {
  border: 1px solid rgba(var(--v-theme-on-surface), 0.2);
  background: linear-gradient(180deg, rgba(var(--v-theme-primary), 0.06), rgba(var(--v-theme-primary), 0.14));
  padding: 6px;
}

.legacy-form-grid {
  align-items: start;
}

.legacy-label {
  padding-top: 8px;
  font-size: 13px;
  color: rgba(var(--v-theme-on-surface), 0.82);
}
</style>
