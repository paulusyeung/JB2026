<template>
  <section class="page-section order-type-page" :class="{ 'order-type-page--dark': isDark }">
    <v-card rounded="xl" elevation="0" class="panel-card order-type-card">
      <v-card-title class="pb-2">
        <h3 class="text-h6 mb-1">{{ t('admin.orderType.title') }}</h3>
        <p class="text-body-2 text-medium-emphasis mb-0">{{ t('admin.orderType.subtitle') }}</p>
      </v-card-title>

      <v-card-text>
        <div class="toolbar-row mb-3">
          <v-select
            v-model="selectedOrderType"
            :items="orderTypeOptions"
            item-title="label"
            item-value="value"
            :label="t('admin.orderType.orderType')"
            density="comfortable"
            variant="outlined"
            hide-details
            class="order-type-select"
            @update:model-value="onOrderTypeChange"
          />

          <v-btn color="primary" prepend-icon="mdi-content-save" :loading="saving" :disabled="!canSave" @click="saveMappings">
            {{ t('admin.orderType.actions.save') }}
          </v-btn>
        </div>

        <v-alert v-if="message" type="success" variant="tonal" class="mb-3">{{ message }}</v-alert>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>

        <div class="lists-grid">
          <v-card variant="outlined" class="list-panel">
            <v-card-title class="text-subtitle-2 py-2">{{ t('admin.orderType.availableWorkflow') }}</v-card-title>
            <v-divider />
            <v-list class="workflow-list" density="compact">
              <v-list-item
                v-for="item in availableWorkflows"
                :key="item.workflowId"
                :active="selectedAvailableId === item.workflowId"
                @click="selectedAvailableId = item.workflowId"
              >
                <v-list-item-title>{{ item.workflowName }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-card>

          <div class="list-actions">
            <v-btn icon="mdi-chevron-right" size="small" variant="outlined" @click="moveRight(false)" />
            <v-btn icon="mdi-chevron-double-right" size="small" variant="outlined" @click="moveRight(true)" />
            <v-btn icon="mdi-chevron-left" size="small" variant="outlined" @click="moveLeft(false)" />
            <v-btn icon="mdi-chevron-double-left" size="small" variant="outlined" @click="moveLeft(true)" />
          </div>

          <v-card variant="outlined" class="list-panel">
            <v-card-title class="text-subtitle-2 py-2">{{ t('admin.orderType.selectedWorkflow') }}</v-card-title>
            <v-divider />
            <v-list class="workflow-list" density="compact">
              <v-list-item
                v-for="item in selectedWorkflows"
                :key="item.workflowId"
                :active="selectedSelectedId === item.workflowId"
                @click="selectedSelectedId = item.workflowId"
              >
                <v-list-item-title>{{ item.workflowName }}</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-card>

          <div class="order-actions">
            <v-btn icon="mdi-chevron-double-up" size="small" variant="outlined" @click="moveUp(true)" />
            <v-btn icon="mdi-chevron-up" size="small" variant="outlined" @click="moveUp(false)" />
            <v-btn icon="mdi-chevron-down" size="small" variant="outlined" @click="moveDown(false)" />
            <v-btn icon="mdi-chevron-double-down" size="small" variant="outlined" @click="moveDown(true)" />
          </div>
        </div>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useTheme } from 'vuetify'
import { getAdminOrderTypeWorkflows, updateAdminOrderTypeWorkflows } from '@/services/admin'
import type { AdminOrderTypeWorkflowItem } from '@/types/api'

const { t } = useI18n({ useScope: 'global' })
const theme = useTheme()
const isDark = computed(() => theme.global.current.value.dark)

const selectedOrderType = ref<number | null>(null)
const availableWorkflows = ref<AdminOrderTypeWorkflowItem[]>([])
const selectedWorkflows = ref<AdminOrderTypeWorkflowItem[]>([])
const selectedAvailableId = ref<string | null>(null)
const selectedSelectedId = ref<string | null>(null)
const saving = ref(false)
const loading = ref(false)
const errorMessage = ref('')
const message = ref('')

const orderTypeOptions = computed(() => [
  { value: 0, label: t('admin.orderType.options.printing') },
  { value: 1, label: t('admin.orderType.options.printedLabel') },
  { value: 2, label: t('admin.orderType.options.wovenLabel') },
  { value: 3, label: t('admin.orderType.options.other') },
])

const canSave = computed(() => selectedOrderType.value !== null && selectedWorkflows.value.length > 0 && !saving.value && !loading.value)

onMounted(async () => {
  if (selectedOrderType.value === null) {
    selectedOrderType.value = orderTypeOptions.value[0]?.value ?? 0
  }

  await onOrderTypeChange()
})

async function onOrderTypeChange() {
  message.value = ''
  errorMessage.value = ''

  if (selectedOrderType.value === null) {
    availableWorkflows.value = []
    selectedWorkflows.value = []
    selectedAvailableId.value = null
    selectedSelectedId.value = null
    return
  }

  loading.value = true
  try {
    const payload = await getAdminOrderTypeWorkflows(selectedOrderType.value)
    availableWorkflows.value = payload.availableWorkflows
    selectedWorkflows.value = payload.selectedWorkflows
    selectedAvailableId.value = null
    selectedSelectedId.value = null
  } catch {
    errorMessage.value = t('admin.orderType.messages.loadFailed')
  } finally {
    loading.value = false
  }
}

function moveRight(moveAll: boolean) {
  if (moveAll) {
    if (availableWorkflows.value.length === 0) return
    selectedWorkflows.value = [...selectedWorkflows.value, ...availableWorkflows.value]
    availableWorkflows.value = []
    selectedAvailableId.value = null
    return
  }

  if (!selectedAvailableId.value) return
  const index = availableWorkflows.value.findIndex((item) => item.workflowId === selectedAvailableId.value)
  if (index < 0) return
  const [item] = availableWorkflows.value.splice(index, 1)
  selectedWorkflows.value.push(item)
  selectedAvailableId.value = null
}

function moveLeft(moveAll: boolean) {
  if (moveAll) {
    if (selectedWorkflows.value.length === 0) return
    availableWorkflows.value = [...availableWorkflows.value, ...selectedWorkflows.value]
    selectedWorkflows.value = []
    selectedSelectedId.value = null
    return
  }

  if (!selectedSelectedId.value) return
  const index = selectedWorkflows.value.findIndex((item) => item.workflowId === selectedSelectedId.value)
  if (index < 0) return
  const [item] = selectedWorkflows.value.splice(index, 1)
  availableWorkflows.value.push(item)
  selectedSelectedId.value = null
}

function moveUp(toTop: boolean) {
  if (!selectedSelectedId.value) return

  const index = selectedWorkflows.value.findIndex((item) => item.workflowId === selectedSelectedId.value)
  if (index < 0) return

  if (toTop) {
    const [item] = selectedWorkflows.value.splice(index, 1)
    selectedWorkflows.value.unshift(item)
    return
  }

  if (index === 0) return
  const [item] = selectedWorkflows.value.splice(index, 1)
  selectedWorkflows.value.splice(index - 1, 0, item)
}

function moveDown(toBottom: boolean) {
  if (!selectedSelectedId.value) return

  const index = selectedWorkflows.value.findIndex((item) => item.workflowId === selectedSelectedId.value)
  if (index < 0) return

  if (toBottom) {
    const [item] = selectedWorkflows.value.splice(index, 1)
    selectedWorkflows.value.push(item)
    return
  }

  if (index >= selectedWorkflows.value.length - 1) return
  const [item] = selectedWorkflows.value.splice(index, 1)
  selectedWorkflows.value.splice(index + 1, 0, item)
}

async function saveMappings() {
  message.value = ''
  errorMessage.value = ''

  if (selectedOrderType.value === null) {
    errorMessage.value = t('admin.orderType.messages.mustSelectOrderType')
    return
  }

  if (selectedWorkflows.value.length === 0) {
    errorMessage.value = t('admin.orderType.messages.mustSelectWorkflow')
    return
  }

  saving.value = true
  try {
    await updateAdminOrderTypeWorkflows({
      orderType: selectedOrderType.value,
      workflowIds: selectedWorkflows.value.map((item) => item.workflowId),
    })
    message.value = t('admin.orderType.messages.saveSuccess')
    await onOrderTypeChange()
  } catch {
    errorMessage.value = t('admin.orderType.messages.saveFailed')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.order-type-page {
  min-height: 0;
}

.order-type-card {
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
  background: linear-gradient(180deg, rgba(224, 237, 255, 0.92), rgba(241, 247, 255, 0.96));
}

.order-type-page--dark .order-type-card {
  background: linear-gradient(180deg, rgba(34, 48, 68, 0.94), rgba(26, 38, 56, 0.96));
}

.toolbar-row {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
  align-items: center;
}

.order-type-select {
  max-width: 280px;
}

.lists-grid {
  display: grid;
  grid-template-columns: minmax(260px, 1fr) auto minmax(260px, 1fr) auto;
  gap: 16px;
  align-items: center;
}

.list-panel {
  min-height: 420px;
}

.workflow-list {
  height: 380px;
  overflow: auto;
}

.list-actions,
.order-actions {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

@media (max-width: 1100px) {
  .lists-grid {
    grid-template-columns: 1fr;
  }

  .list-actions,
  .order-actions {
    flex-direction: row;
    justify-content: center;
  }

  .list-panel {
    min-height: 280px;
  }

  .workflow-list {
    height: 240px;
  }
}
</style>
