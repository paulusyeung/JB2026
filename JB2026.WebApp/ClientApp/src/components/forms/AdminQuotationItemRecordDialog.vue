<template>
  <v-card class="quotation-item-record-dialog">
    <v-card-title class="d-flex align-center ga-3 pb-2">
      <div>
        <h2 class="text-h6 mb-1">
          {{ isNew ? t('admin.quotationItem.form.newTitle') : t('admin.quotationItem.form.editTitle') }}
        </h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          {{ isNew ? t('admin.quotationItem.actions.newItem') : draft.itemNameEn || '-' }}
        </p>
      </div>
      <v-spacer />
      <v-btn variant="text" icon="mdi-close" @click="emit('cancel')" />
    </v-card-title>

    <v-card-text class="pt-2">
      <div class="d-flex flex-wrap ga-2 mb-4">
        <v-btn size="small" color="primary" prepend-icon="mdi-content-save" :loading="saving" @click="handleSave()">
          {{ t('admin.quotationItem.form.save') }}
        </v-btn>
        <v-btn size="small" variant="tonal" prepend-icon="mdi-content-save-check" :loading="saving" @click="handleSave(true)">
          {{ t('admin.quotationItem.form.saveClose') }}
        </v-btn>
        <v-btn
          size="small"
          variant="outlined"
          prepend-icon="mdi-delete"
          :loading="deleting"
          :disabled="isNew"
          @click="handleDelete"
        >
          {{ t('admin.quotationItem.form.delete') }}
        </v-btn>
      </div>

      <v-row dense>
        <v-col cols="12" md="8">
          <v-select
            v-model="draft.itemGroupId"
            :items="groupOptions"
            item-title="label"
            item-value="value"
            :label="t('admin.quotationItem.form.itemGroup')"
            variant="outlined"
            density="compact"
            :loading="loadingGroups"
            :rules="[requiredGroup]"
          />
        </v-col>
        <v-col cols="12" sm="6" md="2">
          <v-text-field
            v-model="draft.itemIndex"
            :label="t('admin.quotationItem.form.itemIndex')"
            variant="outlined"
            density="compact"
            inputmode="numeric"
          />
        </v-col>
        <v-col cols="12" sm="6" md="2" class="d-flex align-center">
          <div class="d-flex flex-wrap ga-3 pt-1">
            <v-checkbox v-model="draft.mandatory" density="compact" hide-details :label="t('admin.quotationItem.form.mandatory')" />
            <v-checkbox v-model="draft.fixed" density="compact" hide-details :label="t('admin.quotationItem.form.fixed')" />
          </div>
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12">
          <v-text-field
            v-model="draft.itemNameEn"
            :label="t('admin.quotationItem.form.itemNameEn')"
            variant="outlined"
            density="compact"
            maxlength="64"
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="draft.itemNameCht"
            :label="t('admin.quotationItem.form.itemNameCht')"
            variant="outlined"
            density="compact"
            maxlength="64"
          />
        </v-col>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="draft.itemNameChs"
            :label="t('admin.quotationItem.form.itemNameChs')"
            variant="outlined"
            density="compact"
            maxlength="64"
          />
        </v-col>
      </v-row>

      <div class="d-flex flex-wrap ga-2 mt-1 mb-3">
        <v-btn size="x-small" variant="text" prepend-icon="mdi-translate" @click="toTraditionalFromSimplified">
          Chs -> Cht
        </v-btn>
        <v-btn size="x-small" variant="text" prepend-icon="mdi-translate" @click="toSimplifiedFromTraditional">
          Cht -> Chs
        </v-btn>
      </div>

      <v-row dense>
        <v-col cols="12" md="4">
          <v-text-field
            v-model="draft.unitCost"
            :label="t('admin.quotationItem.form.unitCost')"
            variant="outlined"
            density="compact"
            inputmode="decimal"
          />
        </v-col>
        <v-col cols="12" md="4">
          <v-select
            v-model="draft.unitCostType"
            :items="unitCostTypeOptions"
            item-title="title"
            item-value="value"
            :label="t('admin.quotationItem.form.unitCostType')"
            variant="outlined"
            density="compact"
          />
        </v-col>
        <v-col cols="12" md="4">
          <v-text-field
            v-model="draft.minimum"
            :label="t('admin.quotationItem.form.minimum')"
            variant="outlined"
            density="compact"
            maxlength="32"
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="4">
          <v-text-field
            v-model="draft.costRounding"
            :label="t('admin.quotationItem.form.costRounding')"
            variant="outlined"
            density="compact"
            inputmode="decimal"
          />
        </v-col>
      </v-row>
    </v-card-text>

    <v-alert v-if="errorMessage" type="error" variant="tonal" class="mx-6 mb-2">
      {{ errorMessage }}
    </v-alert>

    <v-card-actions class="pa-4 d-flex ga-2">
      <v-spacer />
      <v-btn variant="text" :disabled="saving || deleting" @click="emit('cancel')">
        {{ t('admin.quotationItem.form.cancel') }}
      </v-btn>
    </v-card-actions>
  </v-card>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createAdminQuotationItem,
  deleteAdminQuotationItem,
  getAdminQuotationItemGroups,
  updateAdminQuotationItem,
} from '@/services/admin'
import type { AdminQuotationItemGroupListItem, AdminQuotationItemListItem } from '@/types/api'

const props = defineProps<{
  item: AdminQuotationItemListItem | null
}>()

const emit = defineEmits<{
  (e: 'saved', item: AdminQuotationItemListItem): void
  (e: 'deleted', id: string): void
  (e: 'cancel'): void
}>()

const { t, locale } = useI18n({ useScope: 'global' })

const saving = ref(false)
const deleting = ref(false)
const loadingGroups = ref(false)
const errorMessage = ref('')
const itemGroups = ref<AdminQuotationItemGroupListItem[]>([])

const draft = reactive({
  itemGroupId: '',
  itemIndex: '0',
  itemNameEn: '',
  itemNameCht: '',
  itemNameChs: '',
  mandatory: false,
  fixed: false,
  unitCost: '0.0000',
  unitCostType: 0,
  minimum: '',
  costRounding: '0.000',
})

const isNew = computed(() => props.item === null)

const groupOptions = computed(() =>
  itemGroups.value.map((group) => ({
    value: group.itemGroupId,
    label: `${group.zone} - ${getLocalizedName(group.groupNameEn, group.groupNameCht, group.groupNameChs)}`,
  })),
)

const unitCostTypeOptions = computed(() => [
  { value: 0, title: t('admin.quotationItem.costTypes.none') },
  { value: 1, title: t('admin.quotationItem.costTypes.numberOfPages') },
  { value: 2, title: t('admin.quotationItem.costTypes.numberOfSheets') },
  { value: 3, title: t('admin.quotationItem.costTypes.area') },
  { value: 4, title: t('admin.quotationItem.costTypes.color1') },
  { value: 5, title: t('admin.quotationItem.costTypes.color2') },
  { value: 6, title: t('admin.quotationItem.costTypes.quantity') },
])

watch(
  () => props.item,
  (item) => {
    draft.itemGroupId = item?.itemGroupId ?? ''
    draft.itemIndex = String(item?.itemIndex ?? 0)
    draft.itemNameEn = item?.itemNameEn ?? ''
    draft.itemNameCht = item?.itemNameCht ?? ''
    draft.itemNameChs = item?.itemNameChs ?? ''
    draft.mandatory = item?.mandatory ?? false
    draft.fixed = item?.fixed ?? false
    draft.unitCost = formatDecimal(item?.unitCost ?? 0, 4)
    draft.unitCostType = item?.unitCostType ?? 0
    draft.minimum = item?.minimum ?? ''
    draft.costRounding = formatDecimal(item?.costRounding ?? 0, 3)
    errorMessage.value = ''
  },
  { immediate: true },
)

onMounted(async () => {
  await loadGroups()
})

const requiredGroup = (value: string) => value.trim().length > 0 || t('admin.quotationItem.form.requiredGroup')

async function loadGroups() {
  loadingGroups.value = true

  try {
    itemGroups.value = await getAdminQuotationItemGroups({ take: 1000 })
  } catch {
    errorMessage.value = t('admin.quotationItem.messages.loadGroupsFailed')
  } finally {
    loadingGroups.value = false
  }
}

async function handleSave(closeAfter = false) {
  if (!draft.itemGroupId.trim()) {
    errorMessage.value = t('admin.quotationItem.form.requiredGroup')
    return
  }

  saving.value = true
  errorMessage.value = ''

  try {
    const request = {
      itemGroupId: draft.itemGroupId,
      itemIndex: parseInteger(draft.itemIndex),
      itemNameEn: draft.itemNameEn.trim(),
      itemNameCht: draft.itemNameCht.trim(),
      itemNameChs: draft.itemNameChs.trim(),
      mandatory: draft.mandatory,
      fixed: draft.fixed,
      unitCost: parseDecimal(draft.unitCost),
      unitCostType: draft.unitCostType,
      minimum: draft.minimum.trim(),
      costRounding: parseDecimal(draft.costRounding),
    }

    const result = isNew.value
      ? await createAdminQuotationItem(request)
      : await updateAdminQuotationItem(props.item!.itemId, request)

    emit('saved', result)

    if (closeAfter) {
      emit('cancel')
    }
  } catch {
    errorMessage.value = t('admin.quotationItem.messages.saveFailed')
  } finally {
    saving.value = false
  }
}

async function handleDelete() {
  if (!props.item) {
    return
  }

  if (!window.confirm(t('admin.quotationItem.messages.deleteConfirm'))) {
    return
  }

  deleting.value = true
  errorMessage.value = ''

  try {
    await deleteAdminQuotationItem(props.item.itemId)
    emit('deleted', props.item.itemId)
    emit('cancel')
  } catch {
    errorMessage.value = t('admin.quotationItem.messages.deleteFailed')
  } finally {
    deleting.value = false
  }
}

function toTraditionalFromSimplified() {
  draft.itemNameCht = draft.itemNameChs
}

function toSimplifiedFromTraditional() {
  draft.itemNameChs = draft.itemNameCht
}

function getLocalizedName(english: string, traditional: string, simplified: string) {
  switch (locale.value) {
    case 'zh-Hant':
      return traditional || english || simplified
    case 'zh-Hans':
      return simplified || english || traditional
    default:
      return english || traditional || simplified
  }
}

function parseInteger(value: string) {
  const parsed = Number.parseInt(value.trim(), 10)
  return Number.isFinite(parsed) ? parsed : 0
}

function parseDecimal(value: string) {
  const parsed = Number(value.trim())
  return Number.isFinite(parsed) ? parsed : 0
}

function formatDecimal(value: number, fractionDigits: number) {
  return value.toFixed(fractionDigits)
}
</script>