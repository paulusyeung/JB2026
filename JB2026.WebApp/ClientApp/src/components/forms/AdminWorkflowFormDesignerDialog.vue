<template>
  <v-card class="workflow-form-designer-dialog" style="min-height: 600px">
    <v-card-title class="d-flex align-center ga-3 pb-2">
      <div>
        <h2 class="text-h6 mb-1">{{ t('admin.workflowForms.designer.title') }} ({{ record.formName }})</h2>
      </div>
      <v-spacer />
      <v-btn variant="text" icon="mdi-close" @click="emit('cancel')" />
    </v-card-title>

    <v-card-text class="pa-2">
      <div class="d-flex flex-wrap ga-2 mb-3">
        <v-btn size="small" color="primary" prepend-icon="mdi-content-save" :loading="saving" @click="handleSave()">
          {{ t('admin.workflowForms.designer.save') }}
        </v-btn>
        <v-btn size="small" variant="tonal" prepend-icon="mdi-content-save-check" :loading="saving" @click="handleSave(true)">
          {{ t('admin.workflowForms.designer.saveClose') }}
        </v-btn>
      </div>

      <v-alert v-if="errorMessage" type="error" variant="tonal" class="mb-2">{{ errorMessage }}</v-alert>

      <div class="designer-layout">
        <!-- Toolbox -->
        <div class="designer-toolbox">
          <div class="text-caption text-medium-emphasis mb-2 font-weight-bold">{{ t('admin.workflowForms.designer.toolbox') }}</div>
          <div v-for="ct in controlTypes" :key="ct.key" class="toolbox-btn mb-1">
            <v-btn
              block
              variant="outlined"
              size="small"
              @click="promptAddControl(ct.type, ct.multiline)"
            >
              {{ ct.label }}
            </v-btn>
          </div>
        </div>

        <!-- Canvas -->
        <div
          ref="canvasRef"
          class="designer-canvas"
          @click.self="deselectControl"
        >
          <div
            v-for="ctrl in controls"
            :key="ctrl.name"
            class="canvas-control"
            :class="{
              'canvas-control--selected': selectedName === ctrl.name,
              'canvas-control--label': ctrl.type === 'Label',
              'canvas-control--textbox': ctrl.type === 'TextBox',
              'canvas-control--multiline': ctrl.type === 'TextBox' && ctrl.multiline,
              'canvas-control--combo': ctrl.type === 'ComboBox',
              'canvas-control--listbox': ctrl.type === 'ListBox' || ctrl.type === 'CheckedListBox',
              'canvas-control--checkbox': ctrl.type === 'CheckBox' || ctrl.type === 'RadioButton',
            }"
            :style="{
              left: ctrl.x + 'px',
              top: ctrl.y + 'px',
              width: ctrl.width + 'px',
              height: ctrl.height + 'px',
            }"
            @click.stop="selectControl(ctrl.name)"
            @mousedown="startDrag($event, ctrl)"
          >
            <span class="canvas-control__text">{{ ctrl.type === 'Label' ? ctrl.text || ctrl.name : ctrl.name }}</span>
          </div>
        </div>

        <!-- Properties Panel -->
        <div class="designer-properties">
          <div class="text-caption text-medium-emphasis mb-2 font-weight-bold">{{ t('admin.workflowForms.designer.properties') }}</div>

          <v-text-field
            :model-value="selectedControl?.name ?? ''"
            :label="t('admin.workflowForms.designer.name')"
            variant="outlined"
            density="compact"
            readonly
            class="mb-2"
          />

          <v-row dense class="mb-1">
            <v-col cols="6">
              <v-text-field
                :model-value="selectedControl?.width?.toString() ?? ''"
                :label="t('admin.workflowForms.designer.sizeWidth')"
                variant="outlined"
                density="compact"
                :readonly="!selectedControl"
                @update:model-value="updateProp('width', $event)"
              />
            </v-col>
            <v-col cols="6">
              <v-text-field
                :model-value="selectedControl?.height?.toString() ?? ''"
                :label="t('admin.workflowForms.designer.sizeHeight')"
                variant="outlined"
                density="compact"
                :readonly="!selectedControl || !heightEditable"
                @update:model-value="updateProp('height', $event)"
              />
            </v-col>
          </v-row>

          <v-row dense class="mb-1">
            <v-col cols="6">
              <v-text-field
                :model-value="selectedControl?.x?.toString() ?? ''"
                :label="t('admin.workflowForms.designer.locationX')"
                variant="outlined"
                density="compact"
                :readonly="!selectedControl"
                @update:model-value="updateProp('x', $event)"
              />
            </v-col>
            <v-col cols="6">
              <v-text-field
                :model-value="selectedControl?.y?.toString() ?? ''"
                :label="t('admin.workflowForms.designer.locationY')"
                variant="outlined"
                density="compact"
                :readonly="!selectedControl"
                @update:model-value="updateProp('y', $event)"
              />
            </v-col>
          </v-row>

          <v-text-field
            :model-value="selectedControl?.text ?? ''"
            :label="t('admin.workflowForms.designer.text')"
            variant="outlined"
            density="compact"
            :readonly="!selectedControl || !textEditable"
            class="mb-2"
            @update:model-value="updateProp('text', $event)"
          />

          <v-textarea
            :model-value="selectedItemsText"
            :label="t('admin.workflowForms.designer.items')"
            variant="outlined"
            density="compact"
            rows="4"
            :readonly="!selectedControl || !itemsEditable"
            @update:model-value="updateItems($event)"
          />

          <div class="d-flex ga-2 mt-3">
            <v-btn
              size="small"
              variant="outlined"
              color="error"
              prepend-icon="mdi-delete"
              :disabled="!selectedControl"
              @click="deleteSelected"
            >
              {{ t('admin.workflowForms.designer.delete') }}
            </v-btn>
          </div>

          <v-divider class="my-3" />

          <v-select
            :model-value="selectedName"
            :items="controlSelectItems"
            item-title="label"
            item-value="name"
            :label="t('admin.workflowForms.designer.controlSelect')"
            variant="outlined"
            density="compact"
            clearable
            @update:model-value="selectControl($event)"
          />
        </div>
      </div>
    </v-card-text>

    <!-- Add control name dialog -->
    <v-dialog v-model="addControlDialog" max-width="min(100%, 340px)" scrollable persistent>
      <v-card>
        <v-card-title class="text-subtitle-1 pb-1">{{ pendingControlType }}</v-card-title>
        <v-card-text>
          <v-text-field
            v-model="pendingControlName"
            :label="t('admin.workflowForms.designer.addControlName')"
            variant="outlined"
            density="compact"
            autofocus
            @keydown.enter="confirmAddControl"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="addControlDialog = false">{{ t('admin.workflowForms.form.cancel') }}</v-btn>
          <v-btn color="primary" @click="confirmAddControl">OK</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-card>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { updateAdminWorkflowForm } from '@/services/admin'
import type { AdminWorkflowFormRecord } from '@/types/api'

type ControlType = 'Label' | 'TextBox' | 'ComboBox' | 'ListBox' | 'CheckedListBox' | 'CheckBox' | 'RadioButton'

interface FormControl {
  name: string
  type: ControlType
  width: number
  height: number
  x: number
  y: number
  text: string
  items: string[]
  multiline: boolean
}

const DEFAULT_SIZES: Record<ControlType, { width: number; height: number }> = {
  Label: { width: 80, height: 20 },
  TextBox: { width: 100, height: 20 },
  ComboBox: { width: 100, height: 20 },
  ListBox: { width: 300, height: 60 },
  CheckedListBox: { width: 300, height: 60 },
  CheckBox: { width: 100, height: 20 },
  RadioButton: { width: 100, height: 20 },
}

const props = defineProps<{
  record: AdminWorkflowFormRecord
}>()

const emit = defineEmits<{
  (e: 'saved', record: AdminWorkflowFormRecord): void
  (e: 'cancel'): void
}>()

const { t } = useI18n({ useScope: 'global' })

const saving = ref(false)
const errorMessage = ref('')
const controls = ref<FormControl[]>([])
const selectedName = ref<string | null>(null)
const canvasRef = ref<HTMLElement | null>(null)
const addControlDialog = ref(false)
const pendingControlType = ref<ControlType>('Label')
const pendingControlName = ref('')

// Drag state
let dragCtrl: FormControl | null = null
let dragOffsetX = 0
let dragOffsetY = 0

const controlTypes = computed(() => [
  { key: 'Label', type: 'Label' as ControlType, label: t('admin.workflowForms.designer.controlTypes.label'), multiline: false },
  { key: 'TextBox', type: 'TextBox' as ControlType, label: t('admin.workflowForms.designer.controlTypes.textBox'), multiline: false },
  { key: 'MultilineTextBox', type: 'TextBox' as ControlType, label: t('admin.workflowForms.designer.controlTypes.multilineTextBox'), multiline: true },
  { key: 'ComboBox', type: 'ComboBox' as ControlType, label: t('admin.workflowForms.designer.controlTypes.dropDownList'), multiline: false },
  { key: 'ListBox', type: 'ListBox' as ControlType, label: t('admin.workflowForms.designer.controlTypes.listBox'), multiline: false },
  { key: 'CheckedListBox', type: 'CheckedListBox' as ControlType, label: t('admin.workflowForms.designer.controlTypes.checkedListBox'), multiline: false },
  { key: 'CheckBox', type: 'CheckBox' as ControlType, label: t('admin.workflowForms.designer.controlTypes.checkBox'), multiline: false },
  { key: 'RadioButton', type: 'RadioButton' as ControlType, label: t('admin.workflowForms.designer.controlTypes.radioButton'), multiline: false },
])

// Track which toolbox button was multiline
let pendingMultiline = false

const selectedControl = computed(() => controls.value.find((c) => c.name === selectedName.value) ?? null)

const heightEditable = computed(() => {
  const c = selectedControl.value
  if (!c) return false
  if (c.type === 'Label' || c.type === 'CheckBox' || c.type === 'RadioButton' || c.type === 'ComboBox') return false
  if (c.type === 'TextBox' && !c.multiline) return false
  return true
})

const textEditable = computed(() => {
  const c = selectedControl.value
  if (!c) return false
  return c.type === 'Label' || c.type === 'CheckBox' || c.type === 'RadioButton'
})

const itemsEditable = computed(() => {
  const c = selectedControl.value
  if (!c) return false
  return c.type === 'ComboBox' || c.type === 'ListBox' || c.type === 'CheckedListBox'
})

const selectedItemsText = computed(() => {
  const c = selectedControl.value
  if (!c || !itemsEditable.value) return ''
  return c.items.join('\n')
})

const controlSelectItems = computed(() =>
  controls.value.map((c) => ({ name: c.name, label: `${c.name} - ${c.type}` })),
)

onMounted(() => {
  loadFromMetadataXml(props.record.metadataXml)
  window.addEventListener('mousemove', onMouseMove)
  window.addEventListener('mouseup', onMouseUp)
})

onBeforeUnmount(() => {
  window.removeEventListener('mousemove', onMouseMove)
  window.removeEventListener('mouseup', onMouseUp)
})

watch(
  () => props.record,
  (r) => loadFromMetadataXml(r.metadataXml),
)

// ─── XML parsing ────────────────────────────────────────────────────────────

function loadFromMetadataXml(xml: string | null) {
  controls.value = []
  if (!xml) return

  try {
    const parser = new DOMParser()
    const doc = parser.parseFromString(xml, 'application/xml')
    const records = doc.querySelectorAll('record')

    records.forEach((rec) => {
      const name = rec.getAttribute('id') ?? ''
      const type = (rec.getAttribute('Type') ?? 'Label') as ControlType
      const width = parseInt(rec.getAttribute('Size.Width') ?? '80', 10) || 80
      const height = parseInt(rec.getAttribute('Size.Height') ?? '20', 10) || 20
      const x = parseInt(rec.getAttribute('Location.X') ?? '10', 10) || 10
      const y = parseInt(rec.getAttribute('Location.Y') ?? '10', 10) || 10
      const text = rec.getAttribute('Text') ?? ''
      const rawItems = rec.getAttribute('Items') ?? ''
      const items = rawItems ? rawItems.split(';').filter(Boolean) : []
      const multiline = rec.getAttribute('Text.Multiline') === 'True'

      if (name) {
        controls.value.push({ name, type, width, height, x, y, text, items, multiline })
      }
    })
  } catch {
    // silently ignore parse errors — start with empty canvas
  }
}

function serializeToMetadataXml(): string {
  const doc = document.implementation.createDocument(null, 'Metadata', null)
  const root = doc.documentElement

  for (const ctrl of controls.value) {
    const rec = doc.createElement('record')
    rec.setAttribute('id', ctrl.name)
    rec.setAttribute('Type', ctrl.type)
    rec.setAttribute('Size.Width', String(ctrl.width))
    rec.setAttribute('Size.Height', String(ctrl.height))
    rec.setAttribute('Location.X', String(ctrl.x))
    rec.setAttribute('Location.Y', String(ctrl.y))
    rec.setAttribute('Text', ctrl.text)
    rec.setAttribute('Items', ctrl.items.join(';'))
    if (ctrl.type === 'TextBox') {
      rec.setAttribute('Text.Multiline', ctrl.multiline ? 'True' : 'False')
    }
    root.appendChild(rec)
  }

  return new XMLSerializer().serializeToString(doc)
}

// ─── Control management ──────────────────────────────────────────────────────

function promptAddControl(type: ControlType, multiline = false) {
  pendingControlType.value = type
  pendingMultiline = multiline
  pendingControlName.value = ''
  addControlDialog.value = true
}


function confirmAddControl() {
  const name = pendingControlName.value.replace(/\s+/g, '')
  if (!name) return

  addControlDialog.value = false

  const type = pendingControlType.value
  const size = DEFAULT_SIZES[type]
  const typePrefix: Record<ControlType, string> = {
    Label: 'lbl',
    TextBox: 'txt',
    ComboBox: 'cbo',
    ListBox: 'lst',
    CheckedListBox: 'ckl',
    CheckBox: 'ckb',
    RadioButton: 'rad',
  }

  const fullName = typePrefix[type] + name
  const multiline = pendingMultiline || type === 'ListBox' || type === 'CheckedListBox'

  const ctrl: FormControl = {
    name: fullName,
    type,
    width: multiline && type === 'TextBox' ? 300 : size.width,
    height: multiline && type === 'TextBox' ? 60 : size.height,
    x: 140,
    y: 200,
    text: type === 'Label' ? name : (type === 'CheckBox' || type === 'RadioButton' ? 'Option' : ''),
    items: type === 'ComboBox' || type === 'ListBox' || type === 'CheckedListBox' ? ['Item One', 'Item Two', 'Item Three'] : [],
    multiline: type === 'TextBox' ? pendingMultiline : false,
  }

  controls.value.push(ctrl)
  selectedName.value = fullName
}

function selectControl(name: string | null | undefined) {
  selectedName.value = name ?? null
}

function deselectControl() {
  selectedName.value = null
}

function deleteSelected() {
  if (!selectedName.value) return
  controls.value = controls.value.filter((c) => c.name !== selectedName.value)
  selectedName.value = null
}

function updateProp(prop: 'width' | 'height' | 'x' | 'y' | 'text', value: string) {
  if (!selectedName.value) return
  const ctrl = controls.value.find((c) => c.name === selectedName.value)
  if (!ctrl) return

  if (prop === 'text') {
    ctrl.text = value
  } else {
    const num = parseInt(value, 10)
    if (!isNaN(num) && num >= 0) {
      ctrl[prop] = num
    }
  }
}

function updateItems(text: string) {
  if (!selectedName.value) return
  const ctrl = controls.value.find((c) => c.name === selectedName.value)
  if (!ctrl) return
  ctrl.items = text
    .split('\n')
    .map((s) => s.trim())
    .filter(Boolean)
}

// ─── Drag to move ────────────────────────────────────────────────────────────

function startDrag(event: MouseEvent, ctrl: FormControl) {
  event.preventDefault()
  dragCtrl = ctrl
  dragOffsetX = event.clientX - ctrl.x
  dragOffsetY = event.clientY - ctrl.y
}

function onMouseMove(event: MouseEvent) {
  if (!dragCtrl) return
  dragCtrl.x = Math.max(0, event.clientX - dragOffsetX)
  dragCtrl.y = Math.max(0, event.clientY - dragOffsetY)
}

function onMouseUp() {
  dragCtrl = null
}

// ─── Save ────────────────────────────────────────────────────────────────────

async function handleSave(closeAfter = false) {
  saving.value = true
  errorMessage.value = ''

  try {
    const metadataXml = serializeToMetadataXml()

    const updated = await updateAdminWorkflowForm(props.record.formId, {
      formName: props.record.formName,
      formNameChs: props.record.formNameChs,
      formNameCht: props.record.formNameCht,
      metadataXml,
    })

    emit('saved', updated)

    if (closeAfter) {
      emit('cancel')
    }
  } catch {
    errorMessage.value = t('admin.workflowForms.designer.saveFailed')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.designer-layout {
  display: flex;
  gap: 8px;
  min-height: 480px;
}

.designer-toolbox {
  width: 148px;
  flex-shrink: 0;
  border: 1px solid rgba(var(--v-theme-outline), 0.3);
  border-radius: 6px;
  padding: 8px;
  overflow-y: auto;
}

.designer-canvas {
  flex: 1;
  border: 1px solid rgba(var(--v-theme-outline), 0.3);
  border-radius: 6px;
  position: relative;
  min-height: 480px;
  background: repeating-linear-gradient(
    0deg,
    transparent,
    transparent 19px,
    rgba(var(--v-theme-outline), 0.08) 19px,
    rgba(var(--v-theme-outline), 0.08) 20px
  ),
  repeating-linear-gradient(
    90deg,
    transparent,
    transparent 19px,
    rgba(var(--v-theme-outline), 0.08) 19px,
    rgba(var(--v-theme-outline), 0.08) 20px
  );
  overflow: hidden;
}

.designer-properties {
  width: 200px;
  flex-shrink: 0;
  border: 1px solid rgba(var(--v-theme-outline), 0.3);
  border-radius: 6px;
  padding: 8px;
  overflow-y: auto;
}

.canvas-control {
  position: absolute;
  border: 1px dashed rgba(var(--v-theme-primary), 0.5);
  cursor: move;
  user-select: none;
  display: flex;
  align-items: center;
  padding: 2px 4px;
  font-size: 11px;
  background: rgba(var(--v-theme-surface), 0.5);
  border-radius: 2px;
  box-sizing: border-box;
  overflow: hidden;
}

.canvas-control--selected {
  border-color: rgb(var(--v-theme-primary));
  border-style: solid;
  box-shadow: 0 0 0 2px rgba(var(--v-theme-primary), 0.25);
}

.canvas-control--label {
  border-color: rgba(var(--v-theme-error), 0.6);
  background: rgba(var(--v-theme-error), 0.12);
}

.canvas-control__text {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  pointer-events: none;
}

.toolbox-btn .v-btn {
  font-size: 11px;
  text-transform: none;
}
</style>
