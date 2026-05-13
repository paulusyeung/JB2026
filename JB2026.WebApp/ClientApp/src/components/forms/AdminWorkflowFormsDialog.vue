<template>
  <v-card class="workflow-forms-dialog legacy-dialog">
    <v-card-title class="legacy-titlebar d-flex align-center py-2 px-3">
      <span class="text-subtitle-1">{{ t('admin.workflow.workflowFormsDialog.title') }}</span>
      <v-spacer />
      <v-btn size="small" variant="text" icon="mdi-close" @click="emit('cancel')" />
    </v-card-title>

    <v-card-text class="pa-3 legacy-content">
      <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>

      <v-row dense class="mb-2">
        <v-col cols="12" md="2" class="legacy-label">{{ t('admin.workflow.form.workflowName') }}:</v-col>
        <v-col cols="12" md="4">
          <v-text-field :model-value="workflowName" readonly density="compact" variant="outlined" hide-details />
        </v-col>
      </v-row>

      <div class="legacy-toolbar d-flex ga-2 mb-3">
        <v-btn class="legacy-btn" size="small" color="primary" prepend-icon="mdi-content-save" :loading="saving" @click="save">
          {{ t('admin.workflow.workflowFormsDialog.save') }}
        </v-btn>
      </div>

      <v-row>
        <v-col cols="12" md="3">
          <div class="legacy-group-title">{{ t('admin.workflow.workflowFormsDialog.available') }}</div>
          <v-list class="selector-list" density="compact" nav :disabled="loading">
            <v-list-item
              v-for="item in availableForms"
              :key="item.formId"
              :active="selectedAvailableId === item.formId"
              @click="selectedAvailableId = item.formId"
              @dblclick="addSelected"
            >
              <v-list-item-title>{{ displayFormName(item) }}</v-list-item-title>
            </v-list-item>
          </v-list>
          <v-btn block class="mt-2 legacy-btn" prepend-icon="mdi-arrow-right" @click="addSelected" :disabled="loading">
            {{ t('admin.workflow.workflowFormsDialog.add') }}
          </v-btn>
        </v-col>

        <v-col cols="12" md="9">
          <div class="legacy-group-title">{{ t('admin.workflow.workflowFormsDialog.selected') }}</div>
          <div class="selected-container">
            <v-card
              v-for="(item, index) in assignedForms"
              :key="item.formId"
              class="mb-3 form-panel"
              :class="{ 'form-panel--active': selectedAssignedIndex === index }"
              variant="flat"
              @click="selectedAssignedIndex = index"
            >
              <div class="form-panel-body">
                <div class="panel-canvas">
                  <div
                    v-for="ctrl in parseControls(item.metadataXml)"
                    :key="ctrl.name"
                    class="preview-control"
                    :style="controlStyle(ctrl)"
                  >
                    <template v-if="ctrl.type === 'Label'">{{ ctrl.text || ctrl.name }}</template>
                    <template v-else-if="ctrl.type === 'TextBox'">
                      <textarea v-if="ctrl.multiline" disabled :value="ctrl.text"></textarea>
                      <input v-else disabled :value="ctrl.text" />
                    </template>
                    <template v-else-if="ctrl.type === 'ComboBox'">
                      <select disabled>
                        <option v-for="opt in ctrl.items" :key="opt">{{ opt }}</option>
                      </select>
                    </template>
                    <template v-else-if="ctrl.type === 'ListBox' || ctrl.type === 'CheckedListBox'">
                      <select multiple disabled>
                        <option v-for="opt in ctrl.items" :key="opt">{{ opt }}</option>
                      </select>
                    </template>
                    <template v-else-if="ctrl.type === 'CheckBox'">
                      <label><input type="checkbox" disabled /> {{ ctrl.text || 'Option' }}</label>
                    </template>
                    <template v-else-if="ctrl.type === 'RadioButton'">
                      <label><input type="radio" disabled /> {{ ctrl.text || 'Option' }}</label>
                    </template>
                  </div>
                </div>

                <div class="panel-side-actions">
                  <v-btn icon="mdi-close" size="x-small" variant="outlined" @click.stop="removeAt(index)" />
                  <v-btn icon="mdi-chevron-up" size="x-small" variant="outlined" @click.stop="moveUp(index)" />
                  <v-btn icon="mdi-chevron-down" size="x-small" variant="outlined" @click.stop="moveDown(index)" />
                </div>
              </div>
            </v-card>

            <div v-if="assignedForms.length === 0" class="selected-empty-box" />
          </div>
        </v-col>
      </v-row>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { getAdminWorkflowForms, getAdminWorkflowAssignedForms, saveAdminWorkflowAssignedForms } from '@/services/admin'
import type { AdminWorkflowAssignedFormItem, AdminWorkflowFormListItem } from '@/types/api'

const props = defineProps<{
  workflowId: string
  workflowName: string
}>()

const emit = defineEmits<{
  (e: 'saved'): void
  (e: 'cancel'): void
}>()

type PreviewControl = {
  name: string
  type: string
  x: number
  y: number
  width: number
  height: number
  text: string
  items: string[]
  multiline: boolean
}

const { t, locale } = useI18n({ useScope: 'global' })

const saving = ref(false)
const loading = ref(false)
const errorMessage = ref('')
const allForms = ref<AdminWorkflowFormListItem[]>([])
const assignedForms = ref<AdminWorkflowAssignedFormItem[]>([])
const selectedAvailableId = ref<string | null>(null)
const selectedAssignedIndex = ref(-1)

const assignedFormIdSet = computed(() => new Set(assignedForms.value.map((item) => item.formId)))

const availableForms = computed(() => allForms.value.filter((item) => !assignedFormIdSet.value.has(item.formId)))

void load()

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    const [forms, assigned] = await Promise.all([
      getAdminWorkflowForms({ take: 1000 }),
      getAdminWorkflowAssignedForms(props.workflowId),
    ])

    allForms.value = forms
    assignedForms.value = [...assigned].sort((a, b) => a.seqNumber - b.seqNumber)

    const firstAvailable = availableForms.value[0]
    if (firstAvailable) {
      selectedAvailableId.value = firstAvailable.formId
    }
    selectedAssignedIndex.value = assignedForms.value.length > 0 ? 0 : -1
  } catch {
    errorMessage.value = t('admin.workflow.workflowFormsDialog.loadFailed')
  } finally {
    loading.value = false
  }
}

function displayFormName(item: AdminWorkflowFormListItem) {
  return locale.value === 'zhHans'
    ? `${item.formName} - ${item.formNameChs}`
    : `${item.formName} - ${item.formNameCht}`
}

function addSelected() {
  if (!selectedAvailableId.value) {
    errorMessage.value = t('admin.workflow.workflowFormsDialog.selectWorkflowForm')
    return
  }

  const item = allForms.value.find((x) => x.formId === selectedAvailableId.value)
  if (!item) {
    return
  }

  assignedForms.value = [
    ...assignedForms.value,
    {
      workflowFormId: crypto.randomUUID(),
      formId: item.formId,
      seqNumber: assignedForms.value.length,
      formName: item.formName,
      formNameChs: item.formNameChs,
      formNameCht: item.formNameCht,
      metadataXml: null,
    },
  ]

  selectedAssignedIndex.value = assignedForms.value.length - 1
  selectedAvailableId.value = availableForms.value[0]?.formId ?? null
}

function removeAt(index: number) {
  if (index < 0 || index >= assignedForms.value.length) {
    return
  }

  assignedForms.value = assignedForms.value.filter((_, i) => i !== index)
  selectedAssignedIndex.value = Math.min(index, assignedForms.value.length - 1)
}

function moveUp(index: number) {
  if (index <= 0 || index >= assignedForms.value.length) {
    return
  }

  const next = [...assignedForms.value]
  ;[next[index - 1], next[index]] = [next[index]!, next[index - 1]!]
  assignedForms.value = next
  selectedAssignedIndex.value = index - 1
}

function moveDown(index: number) {
  if (index < 0 || index >= assignedForms.value.length - 1) {
    return
  }

  const next = [...assignedForms.value]
  ;[next[index], next[index + 1]] = [next[index + 1]!, next[index]!]
  assignedForms.value = next
  selectedAssignedIndex.value = index + 1
}

async function save() {
  saving.value = true
  errorMessage.value = ''

  try {
    await saveAdminWorkflowAssignedForms(props.workflowId, {
      formIds: assignedForms.value.map((item) => item.formId),
    })
    emit('saved')
  } catch {
    errorMessage.value = t('admin.workflow.workflowFormsDialog.saveFailed')
  } finally {
    saving.value = false
  }
}

function parseControls(metadataXml: string | null): PreviewControl[] {
  if (!metadataXml) {
    return []
  }

  try {
    const doc = new DOMParser().parseFromString(metadataXml, 'application/xml')
    const records = Array.from(doc.querySelectorAll('Metadata > record'))

    return records.map((record) => {
      const itemsAttr = record.getAttribute('Items') || ''
      return {
        name: record.getAttribute('id') || crypto.randomUUID(),
        type: record.getAttribute('Type') || 'Label',
        x: Number(record.getAttribute('Location.X') || 0),
        y: Number(record.getAttribute('Location.Y') || 0),
        width: Number(record.getAttribute('Size.Width') || 80),
        height: Number(record.getAttribute('Size.Height') || 20),
        text: record.getAttribute('Text') || '',
        items: itemsAttr ? itemsAttr.split(';') : [],
        multiline: (record.getAttribute('Text.Multiline') || '').toLowerCase() === 'true',
      }
    })
  } catch {
    return []
  }
}

function controlStyle(control: PreviewControl) {
  return {
    left: `${control.x}px`,
    top: `${control.y}px`,
    width: `${Math.max(40, control.width)}px`,
    height: `${Math.max(20, control.height)}px`,
  }
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

.legacy-group-title {
  margin-bottom: 6px;
  color: rgb(var(--v-theme-primary));
  font-size: 13px;
}

.legacy-label {
  padding-top: 8px;
  font-size: 13px;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.legacy-btn {
  text-transform: none !important;
  letter-spacing: 0;
  font-weight: 600;
}

:deep(.v-field) {
  border-radius: 0;
}

:deep(.v-field__input) {
  min-height: 28px;
  padding-top: 3px;
  padding-bottom: 3px;
}

.selector-list {
  min-height: 560px;
  max-height: 560px;
  overflow: auto;
  border: 1px solid rgba(var(--v-theme-on-surface, 0, 0, 0), 0.2);
  background: rgba(var(--v-theme-on-surface, 0, 0, 0), 0.04);
}

.selected-container {
  min-height: 560px;
  max-height: 560px;
  overflow: auto;
  padding-right: 4px;
  border: 1px solid rgba(var(--v-theme-on-surface, 0, 0, 0), 0.2);
  background: rgba(var(--v-theme-on-surface, 0, 0, 0), 0.06);
}

.form-panel {
  cursor: pointer;
  border: 1px solid rgba(var(--v-theme-on-surface, 0, 0, 0), 0.28);
  border-radius: 0;
  background: rgba(var(--v-theme-on-surface, 0, 0, 0), 0.04);
}

.form-panel--active {
  border-color: rgb(var(--v-theme-primary, 25, 118, 210));
}

.form-panel-body {
  display: grid;
  grid-template-columns: 1fr 28px;
  gap: 8px;
  padding: 8px;
}

.panel-canvas {
  position: relative;
  min-height: 240px;
  max-height: 240px;
  overflow: auto;
  background: rgb(var(--v-theme-surface, 245, 245, 245));
  border: 1px solid rgba(var(--v-theme-on-surface, 0, 0, 0), 0.45);
}

.panel-side-actions {
  display: flex;
  flex-direction: column;
  gap: 6px;
  align-items: center;
}

.selected-empty-box {
  min-height: 240px;
  border: 1px solid rgba(var(--v-theme-on-surface, 0, 0, 0), 0.45);
  margin: 12px;
  background: rgba(var(--v-theme-on-surface, 0, 0, 0), 0.03);
}

.preview-control {
  position: absolute;
  box-sizing: border-box;
  font-size: 12px;
  display: flex;
  align-items: center;
}

.preview-control input,
.preview-control textarea,
.preview-control select {
  width: 100%;
  height: 100%;
  font-size: 12px;
  border: 1px solid rgba(var(--v-theme-on-surface, 0, 0, 0), 0.4);
  background: rgb(var(--v-theme-surface, 245, 245, 245));
  color: rgba(var(--v-theme-on-surface, 0, 0, 0), 0.92);
  padding: 2px;
}
</style>
