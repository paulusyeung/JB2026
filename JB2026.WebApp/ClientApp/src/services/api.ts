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
  const token = localStorage.getItem('jb2026.accessToken')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config

    // Handle 401 Unauthorized responses
    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      if (isRefreshing) {
        // Queue the request to retry after refresh completes
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
        
        if (!storedRefreshToken) {
          // No refresh token, must login again
          clearSessionAndRedirectToLogin()
          return Promise.reject(error)
        }

        // Call refresh endpoint
        const response = await refreshToken(storedRefreshToken)
        
        // Update stored tokens
        const newAccessToken = response.accessToken
        localStorage.setItem('jb2026.accessToken', newAccessToken)
        
        if (response.refreshToken) {
          localStorage.setItem('jb2026.refreshToken', response.refreshToken)
        }

        // Update session store with new tokens
        const sessionStore = useSessionStore()
        sessionStore.accessToken = newAccessToken
        if (response.refreshToken) {
          sessionStore.refreshToken = response.refreshToken
        }

        // Update the original request with new token
        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`

        // Process queued requests with new token
        processQueue(newAccessToken)
        isRefreshing = false

        // Retry original request
        return apiClient(originalRequest)
      } catch (refreshError) {
        // Refresh failed, clear session and redirect
        clearSessionAndRedirectToLogin()
        processQueue(null)
        isRefreshing = false
        return Promise.reject(refreshError)
      }
    }

    // For other errors or if already retried, just reject
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