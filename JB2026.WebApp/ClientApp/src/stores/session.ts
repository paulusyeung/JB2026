import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import axios from 'axios'
import { getCurrentUser, signIn, revokeToken } from '@/services/auth'
import type { TokenResponse, UserProfile } from '@/types/api'

const SESSION_STORAGE_KEY = 'jb2026.sessionProfile'
const TOKEN_STORAGE_KEY = 'jb2026.accessToken'
const REFRESH_TOKEN_STORAGE_KEY = 'jb2026.refreshToken'

export const useSessionStore = defineStore('session', () => {
  const accessToken = ref(localStorage.getItem(TOKEN_STORAGE_KEY) ?? '')
  const refreshToken = ref(localStorage.getItem(REFRESH_TOKEN_STORAGE_KEY) ?? '')
  const profile = ref<UserProfile | null>(readStoredProfile())
  const loading = ref(false)
  const errorKey = ref('')

  const isAuthenticated = computed(() => accessToken.value.length > 0)

  async function login(username: string, password: string, keepMeSignedIn: boolean = false) {
    loading.value = true
    errorKey.value = ''

    try {
      const response = await signIn(username, password, keepMeSignedIn)
      applyTokenResponse(response)
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

  async function bootstrapProfile() {
    if (!isAuthenticated.value) {
      profile.value = null
      return null
    }

    try {
      const currentUser = await getCurrentUser()
      profile.value = currentUser
      localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(currentUser))
      return currentUser
    } catch {
      logout()
      return null
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
    errorKey.value = ''
    localStorage.removeItem(TOKEN_STORAGE_KEY)
    localStorage.removeItem(REFRESH_TOKEN_STORAGE_KEY)
    localStorage.removeItem(SESSION_STORAGE_KEY)
  }

  return {
    accessToken,
    refreshToken,
    profile,
    loading,
    errorKey,
    isAuthenticated,
    bootstrapProfile,
    login,
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