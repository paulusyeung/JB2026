<template>
  <section class="page-section auth-page">
    <v-card rounded="xl" elevation="0" class="auth-card">
      <v-card-text class="pa-8">
        <template v-if="!session.requiresTwoFactor">
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
        </template>

        <template v-else>
          <p class="eyebrow mb-2">{{ t('auth.twoFactor.eyebrow') }}</p>
          <h1 class="text-h4 mb-3">{{ t('auth.twoFactor.title') }}</h1>
          <p class="text-body-1 text-medium-emphasis mb-6">
            {{ t('auth.twoFactor.description') }}
          </p>

          <v-form @submit.prevent="handleTwoFactorSubmit">
            <v-otp-input
              v-model="twoFactorCode"
              :length="6"
              variant="outlined"
              class="mb-4"
              @finish="handleTwoFactorSubmit"
            />
            <v-alert v-if="session.errorKey" type="error" variant="tonal" class="mb-4">
              {{ t(session.errorKey) }}
            </v-alert>
            <v-btn block color="primary" type="submit" :loading="session.loading" :disabled="twoFactorCode.length !== 6">
              {{ t('auth.twoFactor.verify') }}
            </v-btn>
            <v-btn block variant="text" class="mt-2" @click="handleCancelTwoFactor">
              {{ t('auth.twoFactor.cancel') }}
            </v-btn>
          </v-form>
        </template>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
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
const twoFactorCode = ref('')

async function handleSubmit() {
  await session.login(username.value, password.value, keepMeSignedIn.value)

  // If 2FA is required, don't navigate - stay on the login page
  if (!session.requiresTwoFactor) {
    await router.replace(String(route.query.redirect ?? resolveDashboardLandingRoute(session.rbac)))
  }
}

async function handleTwoFactorSubmit() {
  if (twoFactorCode.value.length !== 6) return

  try {
    await session.verifyTwoFactorCode(twoFactorCode.value)
    await router.replace(String(route.query.redirect ?? resolveDashboardLandingRoute(session.rbac)))
  } catch {
    // Error is handled by the session store
    twoFactorCode.value = ''
  }
}

function handleCancelTwoFactor() {
  session.clearTwoFactorState()
  twoFactorCode.value = ''
  password.value = ''
}

// Watch for token expiry - if requiresTwoFactor becomes false, user needs to restart
watch(
  () => session.requiresTwoFactor,
  (newValue) => {
    if (!newValue && session.errorKey === 'auth.errors.twoFactorTokenExpired') {
      // Token expired, user needs to enter credentials again
      twoFactorCode.value = ''
    }
  }
)
</script>
