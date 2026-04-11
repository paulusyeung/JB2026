import { apiClient } from './api'
import type {
  AdminUser,
  AdminWorkflowListItem,
  AdminWorkflowFormListItem,
  AdminCustomerListItem,
  AdminOrderTypeWorkflowPayload,
  UpdateAdminOrderTypeWorkflowsRequest,
} from '@/types/api'

export interface AdminWorkflowsQuery {
  lookup?: string
  shortcut?: string
  take?: number
}

export interface AdminUsersQuery {
  lookup?: string
  take?: number
}

export async function getAdminUsers(query: AdminUsersQuery = {}): Promise<AdminUser[]> {
  const response = await apiClient.get<AdminUser[]>('/api/v2/admin/users', {
    params: {
      lookup: query.lookup ?? '',
      take: query.take ?? 500,
    },
  })
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

export interface AdminCustomersQuery {
  lookup?: string
  take?: number
}

export async function getAdminCustomers(query: AdminCustomersQuery = {}): Promise<AdminCustomerListItem[]> {
  const response = await apiClient.get<AdminCustomerListItem[]>('/api/v2/admin/customers', {
    params: {
      lookup: query.lookup ?? '',
      take: query.take ?? 500,
    },
  })

  return response.data
}

export async function getAdminOrderTypeWorkflows(orderType: number): Promise<AdminOrderTypeWorkflowPayload> {
  const response = await apiClient.get<AdminOrderTypeWorkflowPayload>('/api/v2/admin/order-type/workflows', {
    params: { orderType },
  })
  return response.data
}

export async function updateAdminOrderTypeWorkflows(request: UpdateAdminOrderTypeWorkflowsRequest): Promise<void> {
  await apiClient.put('/api/v2/admin/order-type/workflows', request)
}