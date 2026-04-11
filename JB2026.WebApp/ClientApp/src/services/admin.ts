import { apiClient } from './api'
import type { AdminUser, AdminWorkflowListItem, AdminWorkflowFormListItem } from '@/types/api'

export interface AdminWorkflowsQuery {
  lookup?: string
  shortcut?: string
  take?: number
}

export async function getAdminUsers(): Promise<AdminUser[]> {
  const response = await apiClient.get<AdminUser[]>('/api/v2/admin/users')
  return response.data
}

export async function getAdminWorkflows(query: AdminWorkflowsQuery = {}): Promise<AdminWorkflowListItem[]> {
  const response = await apiClient.get<AdminWorkflowListItem[]>('/api/v2/admin/workflows', {
    params: {
      lookup: query.lookup ?? '',
      shortcut: query.shortcut ?? '',
      take: query.take ?? 500,
    },
  })

  return response.data
}

export interface AdminWorkflowFormsQuery {
  lookup?: string
  take?: number
}

export async function getAdminWorkflowForms(query: AdminWorkflowFormsQuery = {}): Promise<AdminWorkflowFormListItem[]> {
  const response = await apiClient.get<AdminWorkflowFormListItem[]>('/api/v2/admin/workflow-forms', {
    params: {
      lookup: query.lookup ?? '',
      take: query.take ?? 500,
    },
  })

  return response.data
}