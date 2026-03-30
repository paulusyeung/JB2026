<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('admin.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('admin.subtitle') }}</p>
        </div>
        <v-spacer />
        <v-btn color="primary" :loading="loading" @click="load">{{ t('common.refresh') }}</v-btn>
      </v-card-title>
      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>

        <v-chip v-if="currentUser" class="mb-4" color="secondary" variant="tonal">
          {{ t('admin.signedInAs', { name: currentUser.displayName, role: currentUser.role }) }}
        </v-chip>

        <v-data-table :headers="headers" :items="users" :loading="loading" item-value="userId" />
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { getCurrentUser } from '@/services/auth'
import { getAdminUsers } from '@/services/admin'
import type { AdminUser, UserProfile } from '@/types/api'

const loading = ref(false)
const errorMessage = ref('')
const users = ref<AdminUser[]>([])
const currentUser = ref<UserProfile | null>(null)
const { t } = useI18n({ useScope: 'global' })

const headers = computed(() => [
  { title: t('admin.headers.username'), key: 'username' },
  { title: t('admin.headers.displayName'), key: 'displayName' },
  { title: t('admin.headers.role'), key: 'role' },
])

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    const [me, adminUsers] = await Promise.all([getCurrentUser(), getAdminUsers()])
    currentUser.value = me
    users.value = adminUsers
  } catch {
    errorMessage.value = t('admin.loadFailed')
  } finally {
    loading.value = false
  }
}
</script>