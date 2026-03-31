import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import axios from 'axios'
import { getCurrentUser, signIn } from '@/services/auth'
import type { TokenResponse, UserProfile } from '@/types/api'

const SESSION_STORAGE_KEY = 'jb2026.sessionProfile'
const TOKEN_STORAGE_KEY = 'jb2026.accessToken'

export const useSessionStore = defineStore('session', () => {
  const accessToken = ref(localStorage.getItem(TOKEN_STORAGE_KEY) ?? '')
  const profile = ref<UserProfile | null>(readStoredProfile())
  const loading = ref(false)
  const errorKey = ref('')

  const isAuthenticated = computed(() => accessToken.value.length > 0)

  async function login(username: string, password: string) {
    loading.value = true
    errorKey.value = ''

    try {
      const response = await signIn(username, password)
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
  }

  function loginWithDevelopmentDefaults() {
    return login(import.meta.env.VITE_DEV_USERNAME ?? 'admin', import.meta.env.VITE_DEV_PASSWORD ?? 'password123')
  }

  function logout() {
    accessToken.value = ''
    profile.value = null
    errorKey.value = ''
    localStorage.removeItem(TOKEN_STORAGE_KEY)
    localStorage.removeItem(SESSION_STORAGE_KEY)
  }

  return {
    accessToken,
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