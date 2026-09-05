import axios from 'axios'
import { useSessionStore } from '@/stores/session'
import { refreshToken } from './auth'

const baseURL = import.meta.env.VITE_API_BASE_URL || undefined

export const apiClient = axios.create({
  baseURL,
  withCredentials: false,
})

// Track refresh state to prevent multiple concurrent refresh requests
let isRefreshing = false
let failedQueue: Array<{ resolve: (token: string) => void; reject: (error: any) => void }> = []

function processQueue(token: string | null) {
  failedQueue.forEach((prom) => {
    if (token) {
      prom.resolve(token)
    } else {
      prom.reject(new Error('Refresh failed'))
    }
  })
  failedQueue = []
}

apiClient.interceptors.request.use((config) => {
  // Skip auth header for login/refresh/revoke endpoints, but NOT for 2FA management endpoints
  const isUnauthenticatedAuthEndpoint = config.url?.match(/\/api\/v2\/auth\/(token|refresh|revoke)/)
  const token = localStorage.getItem('jb2026.accessToken')
  if (token && !isUnauthenticatedAuthEndpoint) {
    config.headers.Authorization = `Bearer ${token}`
  }

  console.log('[Auth] request', config.method?.toUpperCase(), config.url, !!token)
  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config

    if (error.response?.status === 401) {
      console.log('[Auth] 401 on', originalRequest?.method?.toUpperCase(), originalRequest?.url, 'retry=', !!originalRequest?._retry, 'refreshing=', isRefreshing)
    }

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      // Skip refresh logic for 2FA verify endpoint (invalid code, not expired session)
      const is2faVerifyEndpoint = originalRequest.url?.includes('/api/v2/auth/2fa/verify')
      if (is2faVerifyEndpoint) {
        return Promise.reject(error)
      }

      if (isRefreshing) {
        console.log('[Auth] queueing request')
        return new Promise((resolve, reject) => {
          failedQueue.push({
            resolve: (token: string) => {
              originalRequest.headers.Authorization = `Bearer ${token}`
              resolve(apiClient(originalRequest))
            },
            reject,
          })
        })
      }

      originalRequest._retry = true
      isRefreshing = true

      try {
        const storedRefreshToken = localStorage.getItem('jb2026.refreshToken')
        console.log('[Auth] refresh: token present=', !!storedRefreshToken)
        
        if (!storedRefreshToken) {
          console.log('[Auth] no refresh token, redirecting to login')
          clearSessionAndRedirectToLogin()
          return Promise.reject(error)
        }

        const response = await refreshToken(storedRefreshToken)
        console.log('[Auth] refresh succeeded')
        
        const newAccessToken = response.accessToken
        localStorage.setItem('jb2026.accessToken', newAccessToken)
        
        if (response.refreshToken) {
          localStorage.setItem('jb2026.refreshToken', response.refreshToken)
        }

        const sessionStore = useSessionStore()
        sessionStore.accessToken = newAccessToken
        if (response.refreshToken) {
          sessionStore.refreshToken = response.refreshToken
        }

        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`

        processQueue(newAccessToken)
        isRefreshing = false

        return apiClient(originalRequest)
      } catch (refreshError) {
        console.log('[Auth] refresh failed:', refreshError)
        clearSessionAndRedirectToLogin()
        processQueue(null)
        return Promise.reject(refreshError)
      }
    }

    return Promise.reject(error)
  },
)

function clearSessionAndRedirectToLogin() {
  localStorage.removeItem('jb2026.accessToken')
  localStorage.removeItem('jb2026.refreshToken')
  localStorage.removeItem('jb2026.sessionProfile')

  const redirect = `${window.location.pathname}${window.location.search}`
  if (!window.location.pathname.endsWith('/login')) {
    window.location.assign(`/app/login?redirect=${encodeURIComponent(redirect)}`)
  }
}