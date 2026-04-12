<template>
  <v-card class="item-group-record-dialog">
    <v-card-title class="d-flex align-center ga-3 pb-2">
      <div>
        <h2 class="text-h6 mb-1">
          {{ isNew ? t('admin.quotationItemGroup.form.newTitle') : t('admin.quotationItemGroup.form.editTitle') }}
        </h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          {{ isNew ? t('admin.quotationItemGroup.actions.newItemGroup') : draft.groupNameEn || '-' }}
        </p>
      </div>
      <v-spacer />
      <v-btn variant="text" icon="mdi-close" @click="emit('cancel')" />
    </v-card-title>

    <v-card-text class="pt-2">
      <div class="d-flex flex-wrap ga-2 mb-4">
        <v-btn size="small" color="primary" prepend-icon="mdi-content-save" :loading="saving" @click="handleSave()">
          {{ t('admin.quotationItemGroup.form.save') }}
        </v-btn>
        <v-btn size="small" variant="tonal" prepend-icon="mdi-content-save-check" :loading="saving" @click="handleSave(true)">
          {{ t('admin.quotationItemGroup.form.saveClose') }}
        </v-btn>
        <v-btn
          size="small"
          variant="outlined"
          prepend-icon="mdi-delete"
          :loading="deleting"
          :disabled="isNew"
          @click="handleDelete"
        >
          {{ t('admin.quotationItemGroup.form.delete') }}
        </v-btn>
      </div>

      <v-row dense>
        <v-col cols="12" md="3">
          <v-select
            v-model="draft.zone"
            :items="zoneOptions"
            item-title="label"
            item-value="value"
            :label="t('admin.quotationItemGroup.form.zone')"
            variant="outlined"
            density="compact"
            :rules="[required]"
          />
        </v-col>
        <v-col cols="12" md="9">
          <v-text-field
            v-model="draft.groupNameEn"
            :label="t('admin.quotationItemGroup.form.groupNameEn')"
            variant="outlined"
            density="compact"
            maxlength="64"
            :rules="[required]"
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="draft.groupNameCht"
            :label="t('admin.quotationItemGroup.form.groupNameCht')"
            variant="outlined"
            density="compact"
            maxlength="64"
          />
        </v-col>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="draft.groupNameChs"
            :label="t('admin.quotationItemGroup.form.groupNameChs')"
            variant="outlined"
            density="compact"
            maxlength="64"
          />
        </v-col>
      </v-row>

      <div class="d-flex flex-wrap ga-2 mt-1">
        <v-btn size="x-small" variant="text" prepend-icon="mdi-translate" @click="toTraditionalFromSimplified">
          Chs -> Cht
        </v-btn>
        <v-btn size="x-small" variant="text" prepend-icon="mdi-translate" @click="toSimplifiedFromTraditional">
          Cht -> Chs
        </v-btn>
      </div>
    </v-card-text>

    <v-alert v-if="errorMessage" type="error" variant="tonal" class="mx-6 mb-2">
      {{ errorMessage }}
    </v-alert>

    <v-card-actions class="pa-4 d-flex ga-2">
      <v-spacer />
      <v-btn variant="text" :disabled="saving || deleting" @click="emit('cancel')">
        {{ t('admin.quotationItemGroup.form.cancel') }}
      </v-btn>
    </v-card-actions>
  </v-card>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createAdminQuotationItemGroup,
  deleteAdminQuotationItemGroup,
  updateAdminQuotationItemGroup,
} from '@/services/admin'
import type { AdminQuotationItemGroupListItem } from '@/types/api'

const props = defineProps<{
  itemGroup: AdminQuotationItemGroupListItem | null
}>()

const emit = defineEmits<{
  (e: 'saved', item: AdminQuotationItemGroupListItem): void
  (e: 'deleted', id: string): void
  (e: 'cancel'): void
}>()

const { t } = useI18n({ useScope: 'global' })

const saving = ref(false)
const deleting = ref(false)
const errorMessage = ref('')

const draft = reactive({
  zone: 'A',
  groupNameEn: '',
  groupNameCht: '',
  groupNameChs: '',
})

const isNew = computed(() => props.itemGroup === null)

const zoneOptions = [
  { value: 'A', label: 'A - Paper' },
  { value: 'B', label: 'B - Print' },
  { value: 'C', label: 'C - Post-Print' },
  { value: 'D', label: 'D - Extra Cost' },
  { value: 'E', label: 'E - Shipping' },
]

watch(
  () => props.itemGroup,
  (item) => {
    draft.zone = item?.zone?.trim() || 'A'
    draft.groupNameEn = item?.groupNameEn ?? ''
    draft.groupNameCht = item?.groupNameCht ?? ''
    draft.groupNameChs = item?.groupNameChs ?? ''
    errorMessage.value = ''
  },
  { immediate: true },
)

const required = (value: string) => value.trim().length > 0 || t('admin.quotationItemGroup.form.required')

async function handleSave(closeAfter = false) {
  if (!draft.zone.trim() || !draft.groupNameEn.trim()) {
    errorMessage.value = t('admin.quotationItemGroup.form.required')
    return
  }

  saving.value = true
  errorMessage.value = ''

  try {
    const request = {
      zone: draft.zone.trim().toUpperCase(),
      groupNameEn: draft.groupNameEn.trim(),
      groupNameCht: draft.groupNameCht.trim(),
      groupNameChs: draft.groupNameChs.trim(),
    }

    const result = isNew.value
      ? await createAdminQuotationItemGroup(request)
      : await updateAdminQuotationItemGroup(props.itemGroup!.itemGroupId, request)

    emit('saved', result)

    if (closeAfter) {
      emit('cancel')
    }
  } catch {
    errorMessage.value = t('admin.quotationItemGroup.messages.saveFailed')
  } finally {
    saving.value = false
  }
}

async function handleDelete() {
  if (!props.itemGroup) {
    return
  }

  if (!window.confirm(t('admin.quotationItemGroup.messages.deleteConfirm'))) {
    return
  }

  deleting.value = true
  errorMessage.value = ''

  try {
    await deleteAdminQuotationItemGroup(props.itemGroup.itemGroupId)
    emit('deleted', props.itemGroup.itemGroupId)
    emit('cancel')
  } catch {
    errorMessage.value = t('admin.quotationItemGroup.messages.deleteFailed')
  } finally {
    deleting.value = false
  }
}

function toTraditionalFromSimplified() {
  draft.groupNameCht = draft.groupNameChs
}

function toSimplifiedFromTraditional() {
  draft.groupNameChs = draft.groupNameCht
}
</script>
