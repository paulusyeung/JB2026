import { apiClient } from './api'
import type { AppSettings } from '@/types/api'

export async function getSettings(): Promise<AppSettings> {
  const response = await apiClient.get<AppSettings>('/api/v2/settings')
  return response.data
}

export async function updateSettings(payload: AppSettings): Promise<AppSettings> {
  const response = await apiClient.put<AppSettings>('/api/v2/settings', payload)
  return response.data
}