<template>
  <v-card class="user-record-dialog">
    <v-card-title class="d-flex align-center ga-3 pb-2">
      <div>
        <h2 class="text-h6 mb-1">
          {{ isNew ? t('admin.user.form.newTitle') : t('admin.user.form.editTitle') }}
        </h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          {{ isNew ? t('admin.user.actions.newUser') : draft.userAlias || draft.username || '-' }}
        </p>
      </div>
      <v-spacer />
      <v-btn variant="tonal" icon="mdi-close" @click="emit('cancel')" />
    </v-card-title>

    <v-card-text class="pt-2">
      <div class="d-flex flex-wrap ga-2 mb-4">
        <v-btn size="small" color="primary" prepend-icon="mdi-content-save" :loading="saving" @click="handleSave()">
          {{ t('admin.user.form.save') }}
        </v-btn>
        <v-btn size="small" variant="tonal" prepend-icon="mdi-content-save-check" :loading="saving" @click="handleSave(true)">
          {{ t('admin.user.form.saveClose') }}
        </v-btn>
        <v-btn
          size="small"
          variant="outlined"
          prepend-icon="mdi-delete"
          :loading="deleting"
          :disabled="isNew"
          @click="handleDelete"
        >
          {{ t('admin.user.form.delete') }}
        </v-btn>
      </div>

      <v-row dense>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="draft.username"
            :label="t('admin.user.form.username')"
            variant="outlined"
            density="compact"
            maxlength="64"
            :rules="[requiredUsername]"
          />
        </v-col>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="draft.userAlias"
            :label="t('admin.user.form.userAlias')"
            variant="outlined"
            density="compact"
            maxlength="64"
          />
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="draft.userPassword"
            :label="t('admin.user.form.userPassword')"
            variant="outlined"
            density="compact"
            maxlength="64"
          />
        </v-col>
        <v-col cols="12" md="6">
          <v-select
            v-model="draft.userRole"
            :items="userRoleOptions"
            item-title="title"
            item-value="value"
            :label="t('admin.user.form.userRole')"
            variant="outlined"
            density="compact"
          />
        </v-col>
      </v-row>
    </v-card-text>

    <v-alert v-if="errorMessage" type="error" variant="tonal" class="mx-6 mb-2">
      {{ errorMessage }}
    </v-alert>

    <v-card-actions class="pa-4 d-flex ga-2 responsive-dialog-actions">
      <v-spacer />
      <v-btn variant="text" :disabled="saving || deleting" @click="emit('cancel')">
        {{ t('admin.user.form.cancel') }}
      </v-btn>
    </v-card-actions>
  </v-card>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createAdminUser,
  deleteAdminUser,
  getAdminUser,
  updateAdminUser,
} from '@/services/admin'
import type { AdminUserRecord } from '@/types/api'

const props = defineProps<{
  userId: string | null
}>()

const emit = defineEmits<{
  (e: 'saved', user: AdminUserRecord): void
  (e: 'deleted', id: string): void
  (e: 'cancel'): void
}>()

const { t } = useI18n({ useScope: 'global' })

const saving = ref(false)
const deleting = ref(false)
const errorMessage = ref('')

const draft = reactive({
  username: '',
  userAlias: '',
  userPassword: '',
  userRole: 0,
})

const isNew = computed(() => !props.userId)

const userRoleOptions = computed(() => [
  { value: 0, title: t('admin.user.form.roles.guest') },
  { value: 1, title: t('admin.user.form.roles.operator') },
  { value: 2, title: t('admin.user.form.roles.supervisor') },
  { value: 3, title: t('admin.user.form.roles.manager') },
  { value: 4, title: t('admin.user.form.roles.admin') },
])

const requiredUsername = (value: string) => value.trim().length > 0 || t('admin.user.form.requiredUsername')

watch(
  () => props.userId,
  async (userId) => {
    await loadRecord(userId)
  },
  { immediate: true },
)

async function loadRecord(userId: string | null) {
  errorMessage.value = ''

  if (!userId) {
    draft.username = ''
    draft.userAlias = ''
    draft.userPassword = ''
    draft.userRole = 0
    return
  }

  try {
    const user = await getAdminUser(userId)
    draft.username = user.username
    draft.userAlias = user.userAlias
    draft.userPassword = user.userPassword
    draft.userRole = user.userRole
  } catch {
    errorMessage.value = t('admin.user.messages.loadRecordFailed')
  }
}

async function handleSave(closeAfter = false) {
  if (!draft.username.trim()) {
    errorMessage.value = t('admin.user.form.requiredUsername')
    return
  }

  saving.value = true
  errorMessage.value = ''

  try {
    const request = {
      username: draft.username.trim(),
      userAlias: draft.userAlias.trim(),
      userPassword: draft.userPassword.trim(),
      userRole: draft.userRole,
    }

    const result = isNew.value
      ? await createAdminUser(request)
      : await updateAdminUser(props.userId!, request)

    emit('saved', result)

    if (closeAfter) {
      emit('cancel')
    }
  } catch {
    errorMessage.value = t('admin.user.messages.saveFailed')
  } finally {
    saving.value = false
  }
}

async function handleDelete() {
  if (!props.userId) {
    return
  }

  if (!window.confirm(t('admin.user.messages.deleteConfirm'))) {
    return
  }

  deleting.value = true
  errorMessage.value = ''

  try {
    await deleteAdminUser(props.userId)
    emit('deleted', props.userId)
    emit('cancel')
  } catch {
    errorMessage.value = t('admin.user.messages.deleteFailed')
  } finally {
    deleting.value = false
  }
}
</script>
