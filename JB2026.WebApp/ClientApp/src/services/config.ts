import { apiClient } from './api'

export interface PaperlessNgxConfigStatus {
  configured: boolean
}

export async function getPaperlessNgxConfigStatus(): Promise<PaperlessNgxConfigStatus> {
  const response = await apiClient.get<PaperlessNgxConfigStatus>('/api/v2/config/paperless-ngx')
  return response.data
}