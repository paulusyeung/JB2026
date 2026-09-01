<template>
  <v-card v-draggable-dialog class="user-record-dialog">
    <v-card-title class="d-flex align-center ga-3 pb-2">
      <div>
        <h2 class="text-h6 mb-1">
          {{ isNew ? t('crm.staffMember.form.newTitle') : t('crm.staffMember.form.editTitle') }}
        </h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          {{ isNew ? t('crm.staffMember.actions.new') : draft.userAlias || draft.username || '-' }}
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
        <v-divider vertical class="mx-1" />
        <v-btn
          size="small"
          variant="outlined"
          color="primary"
          prepend-icon="mdi-cloud-sync"
          :disabled="!canSyncToCrm"
          @click="syncToCrm"
        >
          {{ t('admin.user.actions.syncCrm') }}
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

      <v-row dense>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="draft.email"
            :label="t('crm.staffMember.form.email')"
            variant="outlined"
            density="compact"
            maxlength="254"
            type="email"
            :rules="[requiredEmail, uniqueEmail]"
          />
        </v-col>
      </v-row>

      <v-divider class="my-4" />

      <div class="d-flex align-center ga-3 mb-3">
        <v-icon icon="mdi-shield-lock" color="primary" />
        <div>
          <h3 class="text-subtitle-1 font-weight-bold">{{ twoFactorEnabled ? t('auth.twoFactor.status.enabled') : t('auth.twoFactor.status.disabled') }}</h3>
        </div>
        <v-spacer />
        <v-btn
          v-if="!twoFactorEnabled"
          size="small"
          variant="tonal"
          color="primary"
          prepend-icon="mdi-shield-plus"
          :disabled="isNew"
          @click="twoFactorSetupDialogOpen = true"
        >
          {{ t('auth.twoFactor.status.enableButton') }}
        </v-btn>
        <v-btn
          v-else
          size="small"
          variant="tonal"
          color="error"
          prepend-icon="mdi-shield-minus"
          :disabled="isNew"
          @click="twoFactorDisableDialogOpen = true"
        >
          {{ t('auth.twoFactor.status.disableButton') }}
        </v-btn>
      </div>
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

    <v-dialog v-model="crmDialogOpen" max-width="520px">
      <SyncCrmDialog
        :user-id="props.userId ?? ''"
        :user-email="draft.email"
        @cancel="crmDialogOpen = false"
        @done="crmDialogOpen = false; loadRecord(props.userId)"
      />
    </v-dialog>

    <v-dialog v-model="twoFactorSetupDialogOpen" max-width="480px">
      <v-card>
        <v-card-title>{{ t('auth.twoFactor.setup.title') }}</v-card-title>
        <v-card-text>
          <p class="text-body-2 text-medium-emphasis mb-4">
            {{ t('auth.twoFactor.setup.description') }}
          </p>
          <div v-if="twoFactorSetupData" class="text-center mb-4">
            <qrcode-vue :value="twoFactorSetupData.provisioningUri" :size="200" level="M" />
            <p class="text-body-2 mt-2">{{ t('auth.twoFactor.setup.scanQr') }}</p>
          </div>
          <v-alert v-if="twoFactorSetupData?.recoveryCodes" type="info" variant="tonal" class="mb-4">
            <p class="text-body-2 font-weight-bold mb-2">{{ t('auth.twoFactor.setup.recoveryCodes') }}</p>
            <p class="text-body-2 font-family-monospace">{{ twoFactorSetupData.recoveryCodes.join(', ') }}</p>
          </v-alert>
          <v-otp-input
            v-model="twoFactorConfirmCode"
            :length="6"
            variant="outlined"
            class="mb-4"
          />
          <v-alert v-if="twoFactorError" type="error" variant="tonal" class="mb-4">
            {{ twoFactorError }}
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="twoFactorSetupDialogOpen = false">{{ t('auth.twoFactor.cancel') }}</v-btn>
          <v-btn
            color="primary"
            :loading="twoFactorLoading"
            :disabled="twoFactorConfirmCode.length !== 6"
            @click="handleTwoFactorConfirm"
          >
            {{ t('auth.twoFactor.setup.confirm') }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="twoFactorDisableDialogOpen" max-width="480px">
      <v-card>
        <v-card-title>{{ t('auth.twoFactor.disable.title') }}</v-card-title>
        <v-card-text>
          <p class="text-body-2 text-medium-emphasis mb-4">
            {{ t('auth.twoFactor.disable.description') }}
          </p>
          <v-text-field
            v-model="twoFactorDisablePassword"
            :label="t('auth.twoFactor.disable.password')"
            variant="outlined"
            type="password"
            class="mb-4"
          />
          <v-otp-input
            v-model="twoFactorDisableCode"
            :length="6"
            variant="outlined"
            class="mb-4"
          />
          <v-alert v-if="twoFactorError" type="error" variant="tonal" class="mb-4">
            {{ twoFactorError }}
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="twoFactorDisableDialogOpen = false">{{ t('auth.twoFactor.cancel') }}</v-btn>
          <v-btn
            color="error"
            :loading="twoFactorLoading"
            :disabled="!twoFactorDisablePassword || twoFactorDisableCode.length !== 6"
            @click="handleTwoFactorDisable"
          >
            {{ t('auth.twoFactor.disable.disable') }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
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
import { setupTwoFactor, confirmTwoFactor, disableTwoFactor, getTwoFactorStatus } from '@/services/auth'
import SyncCrmDialog from '@/components/crm/SyncCrmDialog.vue'
import QrcodeVue from 'qrcode.vue'
import type { AdminUserRecord, TwoFactorSetupResponse } from '@/types/api'

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
const crmDialogOpen = ref(false)
const twoFactorEnabled = ref(false)
const twoFactorSetupDialogOpen = ref(false)
const twoFactorDisableDialogOpen = ref(false)
const twoFactorSetupData = ref<TwoFactorSetupResponse | null>(null)
const twoFactorConfirmCode = ref('')
const twoFactorDisablePassword = ref('')
const twoFactorDisableCode = ref('')
const twoFactorLoading = ref(false)
const twoFactorError = ref('')

const draft = reactive({
  username: '',
  userAlias: '',
  userPassword: '',
  userRole: 0,
  email: '',
})

const isNew = computed(() => !props.userId)

const canSyncToCrm = computed(() => !isNew.value && draft.email.trim().length > 0)

function syncToCrm() {
  if (!canSyncToCrm.value) return
  crmDialogOpen.value = true
}

const userRoleOptions = computed(() =>
  [
    { value: 0, title: t('admin.user.form.roles.guest') },
    { value: 1, title: t('admin.user.form.roles.operator') },
    { value: 2, title: t('admin.user.form.roles.supervisor') },
    { value: 3, title: t('admin.user.form.roles.manager') },
    { value: 4, title: t('admin.user.form.roles.admin') },
  ].sort((a, b) => a.title.localeCompare(b.title)),
)

const requiredUsername = (value: string) => value.trim().length > 0 || t('admin.user.form.requiredUsername')
const requiredEmail = (value: string) => {
  if (!value.trim().length) return true
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(value.trim()) || t('crm.staffMember.form.invalidEmail')
}

let allUsersCache: { userId: string; email: string }[] = []
async function loadAllUsers() {
  try {
    const { getAdminUsers } = await import('@/services/admin')
    const users = await getAdminUsers({ take: 1000 })
    allUsersCache = users.map(u => ({ userId: u.userId, email: (u.email ?? '').trim().toLowerCase() }))
  } catch {
    allUsersCache = []
  }
}

const uniqueEmail = async (value: string) => {
  if (!value.trim().length) return true
  const trimmed = value.trim().toLowerCase()
  if (allUsersCache.length === 0) await loadAllUsers()
  const dup = allUsersCache.find(u =>
    u.email === trimmed && u.userId !== props.userId
  )
  return dup ? t('crm.staffMember.form.emailInUse') : true
}

watch(
  () => props.userId,
  async (userId) => {
    await loadRecord(userId)
  },
  { immediate: true },
)

watch(twoFactorSetupDialogOpen, (isOpen) => {
  if (isOpen) {
    handleTwoFactorSetup()
  } else {
    twoFactorSetupData.value = null
    twoFactorConfirmCode.value = ''
    twoFactorError.value = ''
  }
})

watch(twoFactorDisableDialogOpen, (isOpen) => {
  if (!isOpen) {
    twoFactorDisablePassword.value = ''
    twoFactorDisableCode.value = ''
    twoFactorError.value = ''
  }
})

async function loadRecord(userId: string | null) {
  errorMessage.value = ''

  if (!userId) {
    draft.username = ''
    draft.userAlias = ''
    draft.userPassword = ''
    draft.userRole = 0
    draft.email = ''
    twoFactorEnabled.value = false
    return
  }

  try {
    const user = await getAdminUser(userId)
    draft.username = user.username
    draft.userAlias = user.userAlias
    draft.userPassword = user.userPassword
    draft.userRole = user.userRole
    draft.email = user.email

    // Load 2FA status
    try {
      const status = await getTwoFactorStatus()
      twoFactorEnabled.value = status.enabled
    } catch {
      // 2FA status might not be available for all users
      twoFactorEnabled.value = false
    }
  } catch {
    errorMessage.value = t('admin.user.messages.loadRecordFailed')
  }
}

async function handleTwoFactorSetup() {
  twoFactorLoading.value = true
  twoFactorError.value = ''

  try {
    const response = await setupTwoFactor()
    twoFactorSetupData.value = response
  } catch {
    twoFactorError.value = t('auth.errors.apiUnavailable')
  } finally {
    twoFactorLoading.value = false
  }
}

async function handleTwoFactorConfirm() {
  if (twoFactorConfirmCode.value.length !== 6) return

  twoFactorLoading.value = true
  twoFactorError.value = ''

  try {
    await confirmTwoFactor(twoFactorConfirmCode.value)
    twoFactorEnabled.value = true
    twoFactorSetupDialogOpen.value = false
    twoFactorSetupData.value = null
    twoFactorConfirmCode.value = ''
  } catch {
    twoFactorError.value = t('auth.errors.invalidTwoFactorCode')
  } finally {
    twoFactorLoading.value = false
  }
}

async function handleTwoFactorDisable() {
  if (!twoFactorDisablePassword.value || twoFactorDisableCode.value.length !== 6) return

  twoFactorLoading.value = true
  twoFactorError.value = ''

  try {
    await disableTwoFactor(twoFactorDisablePassword.value, twoFactorDisableCode.value)
    twoFactorEnabled.value = false
    twoFactorDisableDialogOpen.value = false
    twoFactorDisablePassword.value = ''
    twoFactorDisableCode.value = ''
  } catch {
    twoFactorError.value = t('auth.errors.invalidTwoFactorCode')
  } finally {
    twoFactorLoading.value = false
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
      email: draft.email.trim(),
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
