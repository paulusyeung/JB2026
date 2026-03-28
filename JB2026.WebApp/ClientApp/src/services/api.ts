import axios from 'axios'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? '/'

export const apiClient = axios.create({
  baseURL,
  withCredentials: false,
})

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('jb2026.accessToken')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('jb2026.accessToken')
      localStorage.removeItem('jb2026.sessionProfile')
    }

    return Promise.reject(error)
  },
)