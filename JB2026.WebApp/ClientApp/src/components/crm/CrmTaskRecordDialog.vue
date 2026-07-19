<template>
  <v-card v-draggable-dialog class="task-record-dialog">
    <v-card-title class="d-flex align-center ga-3 pb-2">
      <div>
        <h2 class="text-h6 mb-1">
          {{ props.taskId ? t('crm.tasks.form.editTitle') : t('crm.tasks.form.newTitle') }}
        </h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          {{ draft.title || '-' }}
        </p>
      </div>
      <v-spacer />
      <v-btn variant="text" icon="mdi-close" @click="emit('cancel')" />
    </v-card-title>

    <v-card-text class="pt-2">
      <div class="d-flex flex-wrap ga-2 mb-4">
        <v-btn size="small" color="primary" prepend-icon="mdi-content-save" :loading="saving" @click="handleSave()">
          {{ t('crm.tasks.form.save') }}
        </v-btn>
        <v-btn size="small" variant="tonal" prepend-icon="mdi-content-save-check" :loading="saving" @click="handleSave(true)">
          {{ t('crm.tasks.form.saveClose') }}
        </v-btn>
      </div>

      <v-row dense>
        <v-col cols="12" md="8">
          <v-text-field
            v-model="draft.title"
            :label="t('crm.tasks.headers.title')"
            variant="outlined"
            density="compact"
            maxlength="256"
            :rules="[requiredTitle]"
          />
        </v-col>
        <v-col cols="12" md="4">
          <v-select
            v-model="draft.status"
            :items="statusOptions"
            item-title="label"
            item-value="value"
            :label="t('crm.tasks.headers.status')"
            variant="outlined"
            density="compact"
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12">
          <div class="text-body-2 mb-1">{{ t('crm.tasks.headers.body') }}</div>
          <div class="task-body-editor">
            <ckeditor :editor="editor" v-model="draft.body" :config="editorConfig" />
          </div>
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="6">
          <v-menu v-model="dueDatePickerOpen" :close-on-content-click="false">
            <template #activator="{ props: menuProps }">
              <v-text-field
                :model-value="draft.dueDate ? globalFormat.format(draft.dueDate) : ''"
                :label="t('crm.tasks.headers.dueDate')"
                variant="outlined"
                density="compact"
                readonly
                append-inner-icon="mdi-calendar"
                v-bind="menuProps"
              />
            </template>
            <v-date-picker
              :model-value="draft.dueDate ? new Date(draft.dueDate + 'T12:00:00') : undefined"
              hide-header
              @update:model-value="onDueDatePicked"
            />
          </v-menu>
        </v-col>
        <v-col cols="12" md="6">
          <v-select
            v-model="draft.assigneeId"
            :items="assigneeOptions"
            item-title="displayName"
            item-value="id"
            :label="t('crm.tasks.headers.assignee')"
            variant="outlined"
            density="compact"
            clearable
          />
        </v-col>
      </v-row>
    </v-card-text>

    <v-alert v-if="errorMessage" type="error" variant="tonal" class="mx-6 mb-2">
      {{ errorMessage }}
    </v-alert>

    <v-card-actions class="pa-4 d-flex ga-2 responsive-dialog-actions">
      <v-spacer />
      <v-btn variant="text" :disabled="saving" @click="emit('cancel')">
        {{ t('crm.tasks.form.cancel') }}
      </v-btn>
    </v-card-actions>
  </v-card>
</template>

<script setup lang="ts">
import { reactive, ref, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { Ckeditor } from '@ckeditor/ckeditor5-vue'

import ClassicEditor from '@ckeditor/ckeditor5-build-classic'
import { getCrmTask, updateCrmTask, createCrmTask, getCrmMembers, getCrmTaskStatusOptions } from '@/services/crm'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import type { CrmTask, CrmStageOption } from '@/types/api'

const props = defineProps<{
  taskId: string | null
}>()

const emit = defineEmits<{
  (e: 'saved', task: CrmTask): void
  (e: 'cancel'): void
}>()

const { t } = useI18n({ useScope: 'global' })

const globalFormat = useGlobalDateFormatter()
const saving = ref(false)
const errorMessage = ref('')
const dueDatePickerOpen = ref(false)
const assigneeOptions = ref<{ id: string; displayName: string }[]>([])

const statusOptions = ref<CrmStageOption[]>([])

const ckeditor = Ckeditor
const editor = ClassicEditor
const editorConfig = {
  licenseKey: 'GPL',
  toolbar: {
    items: [
      'undo', 'redo', '|',
      'bold', 'italic', 'link', '|',
      'bulletedList', 'numberedList', '|',
      'blockQuote',
    ],
    shouldNotGroupWhenFull: true,
  },
}

onMounted(async () => {
  try {
    const members = await getCrmMembers()
    assigneeOptions.value = members.map(m => ({ id: m.id, displayName: m.displayName }))
  } catch {
    assigneeOptions.value = []
  }

  try {
    statusOptions.value = await getCrmTaskStatusOptions()
  } catch {
    statusOptions.value = []
  }
})

const draft = reactive({
  title: '',
  body: '',
  status: '',
  dueDate: '',
  assigneeId: null as string | null,
})

const requiredTitle = (value: string) => value.trim().length > 0 || t('crm.tasks.form.requiredTitle')

watch(
  () => props.taskId,
  async (taskId) => {
    await loadRecord(taskId)
  },
  { immediate: true },
)

async function loadRecord(taskId: string | null) {
  errorMessage.value = ''

  if (!taskId) {
    draft.title = ''
    draft.body = ''
    draft.status = ''
    draft.dueDate = ''
    draft.assigneeId = null
    return
  }

  try {
    const task = await getCrmTask(taskId)
    draft.title = task.title
    draft.body = task.body
    draft.status = task.status
    draft.dueDate = task.dueDate ? task.dueDate.slice(0, 10) : ''
    draft.assigneeId = task.assigneeId || null
  } catch {
    errorMessage.value = t('crm.tasks.messages.loadRecordFailed')
  }
}

function onDueDatePicked(date: unknown) {
  if (date instanceof Date) {
    const y = date.getFullYear()
    const m = String(date.getMonth() + 1).padStart(2, '0')
    const d = String(date.getDate()).padStart(2, '0')
    draft.dueDate = `${y}-${m}-${d}`
  }
  dueDatePickerOpen.value = false
}

async function handleSave(closeAfter = false) {
  if (!draft.title.trim()) {
    errorMessage.value = t('crm.tasks.form.requiredTitle')
    return
  }

  saving.value = true
  errorMessage.value = ''

  const payload = {
    title: draft.title.trim(),
    body: draft.body.trim(),
    status: draft.status,
    dueDate: draft.dueDate.trim() || null,
    assigneeId: draft.assigneeId,
  }

  try {
    const result = props.taskId
      ? await updateCrmTask(props.taskId, payload)
      : await createCrmTask(payload)

    emit('saved', result)

    if (closeAfter) {
      emit('cancel')
    }
  } catch (err) {
    const axiosErr = err as { response?: { data?: { message?: string } } }
    const serverMsg = axiosErr.response?.data?.message
    errorMessage.value = serverMsg || t('crm.tasks.messages.saveFailed')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.task-body-editor :deep(.ck-editor__editable) {
  min-height: 140px;
}

.task-body-editor :deep(.ck.ck-editor__main) {
  border-bottom-left-radius: 8px;
  border-bottom-right-radius: 8px;
}
</style>
