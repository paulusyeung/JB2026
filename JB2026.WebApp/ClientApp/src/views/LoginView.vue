<template>
  <section class="page-section auth-page">
    <v-card rounded="xl" elevation="0" class="auth-card">
      <v-card-text class="pa-8">
        <p class="eyebrow mb-2">{{ t('auth.eyebrow') }}</p>
        <h1 class="text-h4 mb-3">{{ t('auth.title') }}</h1>
        <p class="text-body-1 text-medium-emphasis mb-6">
          {{ t('auth.description') }}
        </p>

        <v-form @submit.prevent="handleSubmit">
          <v-text-field v-model="username" :label="t('auth.username')" variant="outlined" autocomplete="username" />
          <v-text-field
            v-model="password"
            :label="t('auth.password')"
            variant="outlined"
            :type="showPassword ? 'text' : 'password'"
            :append-inner-icon="showPassword ? 'mdi-eye-off' : 'mdi-eye'"
            @click:append-inner="showPassword = !showPassword"
            autocomplete="current-password"
          />
          <v-checkbox
            v-model="keepMeSignedIn"
            :label="t('auth.keepMeSignedIn')"
            density="comfortable"
            hide-details
            class="mb-4"
          />
          <v-alert v-if="session.errorKey" type="error" variant="tonal" class="mb-4">
            {{ t(session.errorKey) }}
          </v-alert>
          <v-btn block color="primary" type="submit" :loading="session.loading">{{ t('auth.signIn') }}</v-btn>
        </v-form>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useSessionStore } from '@/stores/session'
import { resolveDashboardLandingRoute } from '@/components/layout/menuHelper'

const router = useRouter()
const route = useRoute()
const session = useSessionStore()
const { t } = useI18n({ useScope: 'global' })

const username = ref('')
const password = ref('')
const showPassword = ref(false)
const keepMeSignedIn = ref(false)

async function handleSubmit() {
  await session.login(username.value, password.value, keepMeSignedIn.value)
  await router.replace(String(route.query.redirect ?? resolveDashboardLandingRoute(session.rbac)))
}
</script>