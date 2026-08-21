import { apiClient } from './api'
import type { RbacValues } from '@/types/api'

export async function getGroupRbac(role: string): Promise<RbacValues> {
  const response = await apiClient.get<RbacValues>('/api/v2/settings/rbac/group', { params: { role } })
  return response.data
}

export async function saveGroupRbac(role: string, values: Record<string, boolean>): Promise<RbacValues> {
  const response = await apiClient.put<RbacValues>('/api/v2/settings/rbac/group', { values }, { params: { role } })
  return response.data
}

export async function getUserRbac(userId: string): Promise<RbacValues> {
  const response = await apiClient.get<RbacValues>(`/api/v2/settings/rbac/user/${userId}`)
  return response.data
}

export async function getEffectiveRbac(): Promise<RbacValues> {
  const response = await apiClient.get<RbacValues>('/api/v2/settings/rbac/effective')
  return response.data
}

export async function saveUserRbac(userId: string, values: Record<string, boolean>): Promise<RbacValues> {
  const response = await apiClient.put<RbacValues>(`/api/v2/settings/rbac/user/${userId}`, { values })
  return response.data
}
