<template>
  <v-card class="workflow-form-record-dialog">
    <v-card-title class="d-flex align-center ga-3 pb-2">
      <div>
        <h2 class="text-h6 mb-1">
          {{ isNew ? t('admin.workflowForms.form.newTitle') : t('admin.workflowForms.form.editTitle') }}
        </h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          {{ isNew ? t('admin.workflowForms.form.newTitle') : (draft.formName || '-') }}
        </p>
      </div>
      <v-spacer />
      <v-btn variant="text" icon="mdi-close" @click="emit('cancel')" />
    </v-card-title>

    <v-card-text class="pt-2">
      <div class="d-flex flex-wrap ga-2 mb-4">
        <v-btn size="small" color="primary" prepend-icon="mdi-content-save" :loading="saving" @click="handleSave()">
          {{ t('admin.workflowForms.form.save') }}
        </v-btn>
        <v-btn size="small" variant="tonal" prepend-icon="mdi-content-copy" :loading="duplicating" :disabled="isNew" @click="handleSaveDup">
          {{ t('admin.workflowForms.form.saveDup') }}
        </v-btn>
        <v-btn size="small" variant="tonal" prepend-icon="mdi-content-save-check" :loading="saving" @click="handleSave(true)">
          {{ t('admin.workflowForms.form.saveClose') }}
        </v-btn>
        <v-btn
          size="small"
          variant="outlined"
          prepend-icon="mdi-delete"
          :loading="deleting"
          :disabled="isNew"
          @click="handleDelete"
        >
          {{ t('admin.workflowForms.form.delete') }}
        </v-btn>
        <v-btn
          size="small"
          variant="outlined"
          prepend-icon="mdi-pencil-ruler"
          :disabled="isNew"
          @click="openDesigner"
        >
          {{ t('admin.workflowForms.form.editForm') }}
        </v-btn>
      </div>

      <v-row dense>
        <v-col cols="12">
          <v-text-field
            v-model="draft.formName"
            :label="t('admin.workflowForms.form.formName')"
            variant="outlined"
            density="compact"
            maxlength="10"
            :rules="[required]"
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12">
          <v-text-field
            v-model="draft.formNameChs"
            :label="t('admin.workflowForms.form.formNameChs')"
            variant="outlined"
            density="compact"
            maxlength="10"
            :rules="[required]"
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12">
          <v-text-field
            v-model="draft.formNameCht"
            :label="t('admin.workflowForms.form.formNameCht')"
            variant="outlined"
            density="compact"
            maxlength="10"
            :rules="[required]"
          />
        </v-col>
      </v-row>
    </v-card-text>

    <v-alert v-if="errorMessage" type="error" variant="tonal" class="mx-6 mb-2">
      {{ errorMessage }}
    </v-alert>

    <v-card-actions class="pa-4 d-flex ga-2">
      <v-spacer />
      <v-btn variant="text" :disabled="saving || deleting || duplicating" @click="emit('cancel')">
        {{ t('admin.workflowForms.form.cancel') }}
      </v-btn>
    </v-card-actions>

    <!-- Designer dialog -->
    <v-dialog v-model="designerOpen" max-width="1200" scrollable persistent>
      <AdminWorkflowFormDesignerDialog
        v-if="designerOpen && currentRecord"
        :record="currentRecord"
        @saved="handleDesignerSaved"
        @cancel="designerOpen = false"
      />
    </v-dialog>
  </v-card>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createAdminWorkflowForm,
  updateAdminWorkflowForm,
  deleteAdminWorkflowForm,
  duplicateAdminWorkflowForm,
} from '@/services/admin'
import type { AdminWorkflowFormListItem, AdminWorkflowFormRecord } from '@/types/api'
import AdminWorkflowFormDesignerDialog from '@/components/forms/AdminWorkflowFormDesignerDialog.vue'

const props = defineProps<{
  item: AdminWorkflowFormListItem | null
}>()

const emit = defineEmits<{
  (e: 'saved', item: AdminWorkflowFormListItem): void
  (e: 'deleted', id: string): void
  (e: 'duplicated', item: AdminWorkflowFormListItem): void
  (e: 'cancel'): void
}>()

const { t } = useI18n({ useScope: 'global' })

const saving = ref(false)
const deleting = ref(false)
const duplicating = ref(false)
const errorMessage = ref('')
const designerOpen = ref(false)
const currentRecord = ref<AdminWorkflowFormRecord | null>(null)

const draft = reactive({
  formName: '',
  formNameChs: '',
  formNameCht: '',
})

const isNew = computed(() => props.item === null)

watch(
  () => props.item,
  (item) => {
    draft.formName = item?.formName ?? ''
    draft.formNameChs = item?.formNameChs ?? ''
    draft.formNameCht = item?.formNameCht ?? ''
    errorMessage.value = ''
    currentRecord.value = null
  },
  { immediate: true },
)

const required = (value: string) => value.trim().length > 0 || t('admin.workflowForms.form.required')

async function handleSave(closeAfter = false) {
  if (!draft.formName.trim() || !draft.formNameChs.trim() || !draft.formNameCht.trim()) {
    errorMessage.value = t('admin.workflowForms.form.required')
    return
  }

  saving.value = true
  errorMessage.value = ''

  try {
    let result: AdminWorkflowFormRecord

    if (isNew.value) {
      result = await createAdminWorkflowForm({
        formName: draft.formName.trim(),
        formNameChs: draft.formNameChs.trim(),
        formNameCht: draft.formNameCht.trim(),
      })
    } else {
      result = await updateAdminWorkflowForm(props.item!.formId, {
        formName: draft.formName.trim(),
        formNameChs: draft.formNameChs.trim(),
        formNameCht: draft.formNameCht.trim(),
        metadataXml: currentRecord.value?.metadataXml ?? null,
      })
    }

    currentRecord.value = result

    emit('saved', {
      formId: result.formId,
      formName: result.formName,
      formNameChs: result.formNameChs,
      formNameCht: result.formNameCht,
    })

    if (closeAfter) {
      emit('cancel')
    }
  } catch {
    errorMessage.value = t('admin.workflowForms.messages2.saveFailed')
  } finally {
    saving.value = false
  }
}

async function handleSaveDup() {
  if (!props.item) return

  duplicating.value = true
  errorMessage.value = ''

  try {
    const result = await duplicateAdminWorkflowForm(props.item.formId)

    emit('duplicated', {
      formId: result.formId,
      formName: result.formName,
      formNameChs: result.formNameChs,
      formNameCht: result.formNameCht,
    })
  } catch {
    errorMessage.value = t('admin.workflowForms.messages2.saveFailed')
  } finally {
    duplicating.value = false
  }
}

async function handleDelete() {
  if (!props.item) return

  if (!window.confirm(t('admin.workflowForms.form.deleteConfirm'))) return

  deleting.value = true
  errorMessage.value = ''

  try {
    await deleteAdminWorkflowForm(props.item.formId)
    emit('deleted', props.item.formId)
  } catch {
    errorMessage.value = t('admin.workflowForms.messages2.deleteFailed')
  } finally {
    deleting.value = false
  }
}

function openDesigner() {
  if (!props.item) return

  // Build a record from what we know (with current metadataXml if we loaded it)
  currentRecord.value = {
    formId: props.item.formId,
    formObjectEnum: 0,
    formName: draft.formName,
    formNameChs: draft.formNameChs,
    formNameCht: draft.formNameCht,
    metadataXml: currentRecord.value?.metadataXml ?? null,
  }

  designerOpen.value = true
}

function handleDesignerSaved(updatedRecord: AdminWorkflowFormRecord) {
  currentRecord.value = updatedRecord
  designerOpen.value = false
}
</script>
