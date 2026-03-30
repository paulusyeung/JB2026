import { apiClient } from './api'
import type { AdminUser } from '@/types/api'

export async function getAdminUsers(): Promise<AdminUser[]> {
  const response = await apiClient.get<AdminUser[]>('/api/v2/admin/users')
  return response.data
}