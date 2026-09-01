import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import axios from 'axios'
import { getCurrentUser, signIn, revokeToken, verifyTwoFactor } from '@/services/auth'
import { getEffectiveRbac } from '@/services/rbac'
import type { TokenResponse, UserProfile } from '@/types/api'

const SESSION_STORAGE_KEY = 'jb2026.sessionProfile'
const TOKEN_STORAGE_KEY = 'jb2026.accessToken'
const REFRESH_TOKEN_STORAGE_KEY = 'jb2026.refreshToken'

export const useSessionStore = defineStore('session', () => {
  const accessToken = ref(localStorage.getItem(TOKEN_STORAGE_KEY) ?? '')
  const refreshToken = ref(localStorage.getItem(REFRESH_TOKEN_STORAGE_KEY) ?? '')
  const profile = ref<UserProfile | null>(readStoredProfile())
  const rbac = ref<Record<string, boolean> | null>(null)
  const loading = ref(false)
  const errorKey = ref('')
  const twoFactorToken = ref<string | null>(null)
  const requiresTwoFactor = ref(false)

  const isAuthenticated = computed(() => accessToken.value.length > 0)

  async function login(username: string, password: string, keepMeSignedIn: boolean = false) {
    loading.value = true
    errorKey.value = ''

    try {
      const response = await signIn(username, password, keepMeSignedIn)

      // Check if 2FA is required
      if (response.requires2fa && response.twoFactorToken) {
        twoFactorToken.value = response.twoFactorToken
        requiresTwoFactor.value = true
        return response
      }

      applyTokenResponse(response)
      await loadRbac()
      return response
    } catch (error) {
      if (axios.isAxiosError(error)) {
        if (error.response?.status === 401) {
          errorKey.value = 'auth.errors.authenticationFailed'
        } else {
          errorKey.value = 'auth.errors.apiUnavailable'
        }
      } else {
        errorKey.value = 'auth.errors.authenticationFailed'
      }

      throw error
    } finally {
      loading.value = false
    }
  }

  async function verifyTwoFactorCode(code: string) {
    if (!twoFactorToken.value) {
      errorKey.value = 'auth.errors.twoFactorRequired'
      return
    }

    loading.value = true
    errorKey.value = ''

    try {
      const response = await verifyTwoFactor(twoFactorToken.value, code)

      // Clear 2FA state
      twoFactorToken.value = null
      requiresTwoFactor.value = false

      // Apply the full token response
      applyTokenResponse(response)
      await loadRbac()
      return response
    } catch (error) {
      if (axios.isAxiosError(error)) {
        if (error.response?.status === 401) {
          errorKey.value = 'auth.errors.invalidTwoFactorCode'
        } else if (error.response?.status === 429) {
          errorKey.value = 'auth.errors.twoFactorRateLimit'
        } else if (error.response?.status === 401 && error.response?.data?.detail?.includes('expired')) {
          // Token expired - need to restart login
          twoFactorToken.value = null
          requiresTwoFactor.value = false
          errorKey.value = 'auth.errors.twoFactorTokenExpired'
        } else {
          errorKey.value = 'auth.errors.apiUnavailable'
        }
      } else {
        errorKey.value = 'auth.errors.invalidTwoFactorCode'
      }

      throw error
    } finally {
      loading.value = false
    }
  }

  function clearTwoFactorState() {
    twoFactorToken.value = null
    requiresTwoFactor.value = false
    errorKey.value = ''
  }

  async function bootstrapProfile() {
    if (!isAuthenticated.value) {
      profile.value = null
      rbac.value = null
      return null
    }

    try {
      const currentUser = await getCurrentUser()
      profile.value = currentUser
      localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(currentUser))
      await loadRbac()
      return currentUser
    } catch {
      logout()
      return null
    }
  }

  async function loadRbac() {
    try {
      const response = await getEffectiveRbac()
      rbac.value = response.values
    } catch {
      // If RBAC cannot be resolved, fall back to showing everything.
      rbac.value = null
    }
  }

  function applyTokenResponse(response: TokenResponse) {
    accessToken.value = response.accessToken
    profile.value = response.user
    localStorage.setItem(TOKEN_STORAGE_KEY, response.accessToken)
    localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(response.user))
    
    // Store refresh token if provided
    if (response.refreshToken) {
      refreshToken.value = response.refreshToken
      localStorage.setItem(REFRESH_TOKEN_STORAGE_KEY, response.refreshToken)
    }
  }

  function loginWithDevelopmentDefaults() {
    return login(import.meta.env.VITE_DEV_USERNAME ?? 'admin', import.meta.env.VITE_DEV_PASSWORD ?? 'password123')
  }

  async function logout() {
    // Revoke the refresh token on the server if it exists
    if (refreshToken.value) {
      try {
        await revokeToken(refreshToken.value)
      } catch (error) {
        // Log but don't fail logout if revoke fails
        console.error('Failed to revoke refresh token:', error)
      }
    }

    accessToken.value = ''
    refreshToken.value = ''
    profile.value = null
    rbac.value = null
    errorKey.value = ''
    twoFactorToken.value = null
    requiresTwoFactor.value = false
    localStorage.removeItem(TOKEN_STORAGE_KEY)
    localStorage.removeItem(REFRESH_TOKEN_STORAGE_KEY)
    localStorage.removeItem(SESSION_STORAGE_KEY)
  }

  return {
    accessToken,
    refreshToken,
    profile,
    rbac,
    loading,
    errorKey,
    twoFactorToken,
    requiresTwoFactor,
    isAuthenticated,
    bootstrapProfile,
    loadRbac,
    login,
    verifyTwoFactorCode,
    clearTwoFactorState,
    loginWithDevelopmentDefaults,
    logout,
  }
})

function readStoredProfile(): UserProfile | null {
  const storedProfile = localStorage.getItem(SESSION_STORAGE_KEY)
  if (!storedProfile) {
    return null
  }

  try {
    return JSON.parse(storedProfile) as UserProfile
  } catch {
    localStorage.removeItem(SESSION_STORAGE_KEY)
    return null
  }
}