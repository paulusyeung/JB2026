import { apiClient } from './api'
import type {
  AdminUser,
  AdminUserRecord,
  AdminWorkflowListItem,
  AdminWorkflowRecord,
  CreateAdminWorkflowRequest,
  UpdateAdminWorkflowRequest,
  AdminWorkflowAssignedFormItem,
  UpdateAdminWorkflowFormsRequest,
  AdminWorkflowFormListItem,
  AdminWorkflowFormRecord,
  CreateAdminWorkflowFormRequest,
  UpdateAdminWorkflowFormRequest,
  AdminCustomerListItem,
  AdminCustomerRecord,
  AdminQuotationItemGroupListItem,
  AdminQuotationItemListItem,
  AdminSupplierListItem,
  AdminSupplierRecord,
  MergeAdminCustomersRequest,
  AdminOrderTypeWorkflowPayload,
  UpdateAdminOrderTypeWorkflowsRequest,
  CreateAdminUserRequest,
  UpdateAdminUserRequest,
  CreateAdminCustomerRequest,
  UpdateAdminCustomerRequest,
  CreateAdminSupplierRequest,
  UpdateAdminSupplierRequest,
  CreateAdminQuotationItemRequest,
  UpdateAdminQuotationItemRequest,
  CreateAdminQuotationItemGroupRequest,
  UpdateAdminQuotationItemGroupRequest,
} from '@/types/api'

export interface AdminWorkflowsQuery {
  lookup?: string
  shortcut?: string
  take?: number
}

export interface AdminUsersQuery {
  lookup?: string
  take?: number
  excludeGuest?: boolean
  role?: string
}

export async function getAdminUsers(query: AdminUsersQuery = {}): Promise<AdminUser[]> {
  const response = await apiClient.get<AdminUser[]>('/api/v2/admin/users', {
    params: {
      lookup: query.lookup ?? '',
      take: query.take ?? 500,
      excludeGuest: query.excludeGuest ?? false,
      role: query.role ?? '',
    },
  })
  return response.data
}

export async function getAdminUser(id: string): Promise<AdminUserRecord> {
  const response = await apiClient.get<AdminUserRecord>(`/api/v2/admin/users/${id}`)
  return response.data
}

export async function createAdminUser(request: CreateAdminUserRequest): Promise<AdminUserRecord> {
  const response = await apiClient.post<AdminUserRecord>('/api/v2/admin/users', request)
  return response.data
}

export async function updateAdminUser(id: string, request: UpdateAdminUserRequest): Promise<AdminUserRecord> {
  const response = await apiClient.put<AdminUserRecord>(`/api/v2/admin/users/${id}`, request)
  return response.data
}

export async function deleteAdminUser(id: string): Promise<void> {
  await apiClient.delete(`/api/v2/admin/users/${id}`)
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

export async function getAdminWorkflow(id: string): Promise<AdminWorkflowRecord> {
  const response = await apiClient.get<AdminWorkflowRecord>(`/api/v2/admin/workflows/${id}`)
  return response.data
}

export async function createAdminWorkflow(request: CreateAdminWorkflowRequest): Promise<AdminWorkflowRecord> {
  const response = await apiClient.post<AdminWorkflowRecord>('/api/v2/admin/workflows', request)
  return response.data
}

export async function updateAdminWorkflow(id: string, request: UpdateAdminWorkflowRequest): Promise<AdminWorkflowRecord> {
  const response = await apiClient.put<AdminWorkflowRecord>(`/api/v2/admin/workflows/${id}`, request)
  return response.data
}

export async function deleteAdminWorkflow(id: string): Promise<void> {
  await apiClient.delete(`/api/v2/admin/workflows/${id}`)
}

export async function getAdminWorkflowAssignedForms(workflowId: string): Promise<AdminWorkflowAssignedFormItem[]> {
  const response = await apiClient.get<AdminWorkflowAssignedFormItem[]>(`/api/v2/admin/workflows/${workflowId}/workflow-forms`)
  return response.data
}

export async function saveAdminWorkflowAssignedForms(workflowId: string, request: UpdateAdminWorkflowFormsRequest): Promise<void> {
  await apiClient.put(`/api/v2/admin/workflows/${workflowId}/workflow-forms`, request)
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

export async function getAdminWorkflowForm(id: string): Promise<AdminWorkflowFormRecord> {
  const response = await apiClient.get<AdminWorkflowFormRecord>(`/api/v2/admin/workflow-forms/${id}`)
  return response.data
}

export async function createAdminWorkflowForm(request: CreateAdminWorkflowFormRequest): Promise<AdminWorkflowFormRecord> {
  const response = await apiClient.post<AdminWorkflowFormRecord>('/api/v2/admin/workflow-forms', request)
  return response.data
}

export async function updateAdminWorkflowForm(id: string, request: UpdateAdminWorkflowFormRequest): Promise<AdminWorkflowFormRecord> {
  const response = await apiClient.put<AdminWorkflowFormRecord>(`/api/v2/admin/workflow-forms/${id}`, request)
  return response.data
}

export async function deleteAdminWorkflowForm(id: string): Promise<void> {
  await apiClient.delete(`/api/v2/admin/workflow-forms/${id}`)
}

export async function duplicateAdminWorkflowForm(id: string): Promise<AdminWorkflowFormRecord> {
  const response = await apiClient.post<AdminWorkflowFormRecord>(`/api/v2/admin/workflow-forms/${id}/duplicate`)
  return response.data
}

export interface AdminCustomersQuery {
  lookup?: string
  take?: number
}

export interface AdminSuppliersQuery {
  lookup?: string
  take?: number
}

export interface AdminQuotationItemsQuery {
  lookup?: string
  shortcut?: string
  take?: number
}

export interface AdminQuotationItemGroupsQuery {
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

export async function getAdminCustomer(id: string): Promise<AdminCustomerRecord> {
  const response = await apiClient.get<AdminCustomerRecord>(`/api/v2/admin/customers/${id}`)
  return response.data
}

export async function createAdminCustomer(request: CreateAdminCustomerRequest): Promise<AdminCustomerRecord> {
  const response = await apiClient.post<AdminCustomerRecord>('/api/v2/admin/customers', request)
  return response.data
}

export async function updateAdminCustomer(id: string, request: UpdateAdminCustomerRequest): Promise<AdminCustomerRecord> {
  const response = await apiClient.put<AdminCustomerRecord>(`/api/v2/admin/customers/${id}`, request)
  return response.data
}

export async function deleteAdminCustomer(id: string): Promise<void> {
  await apiClient.delete(`/api/v2/admin/customers/${id}`)
}

export async function mergeAdminCustomers(request: MergeAdminCustomersRequest): Promise<void> {
  await apiClient.post('/api/v2/admin/customers/merge', request)
}

export async function getAdminSuppliers(query: AdminSuppliersQuery = {}): Promise<AdminSupplierListItem[]> {
  const response = await apiClient.get<AdminSupplierListItem[]>('/api/v2/admin/suppliers', {
    params: {
      lookup: query.lookup ?? '',
      take: query.take ?? 500,
    },
  })

  return response.data
}

export async function getAdminSupplier(id: string): Promise<AdminSupplierRecord> {
  const response = await apiClient.get<AdminSupplierRecord>(`/api/v2/admin/suppliers/${id}`)
  return response.data
}

export async function createAdminSupplier(request: CreateAdminSupplierRequest): Promise<AdminSupplierRecord> {
  const response = await apiClient.post<AdminSupplierRecord>('/api/v2/admin/suppliers', request)
  return response.data
}

export async function updateAdminSupplier(id: string, request: UpdateAdminSupplierRequest): Promise<AdminSupplierRecord> {
  const response = await apiClient.put<AdminSupplierRecord>(`/api/v2/admin/suppliers/${id}`, request)
  return response.data
}

export async function deleteAdminSupplier(id: string): Promise<void> {
  await apiClient.delete(`/api/v2/admin/suppliers/${id}`)
}

export async function getAdminQuotationItems(query: AdminQuotationItemsQuery = {}): Promise<AdminQuotationItemListItem[]> {
  const response = await apiClient.get<AdminQuotationItemListItem[]>('/api/v2/admin/quotation-items', {
    params: {
      lookup: query.lookup ?? '',
      shortcut: query.shortcut ?? 'All',
      take: query.take ?? 500,
    },
  })

  return response.data
}

export async function createAdminQuotationItem(request: CreateAdminQuotationItemRequest): Promise<AdminQuotationItemListItem> {
  const response = await apiClient.post<AdminQuotationItemListItem>('/api/v2/admin/quotation-items', request)
  return response.data
}

export async function updateAdminQuotationItem(id: string, request: UpdateAdminQuotationItemRequest): Promise<AdminQuotationItemListItem> {
  const response = await apiClient.put<AdminQuotationItemListItem>(`/api/v2/admin/quotation-items/${id}`, request)
  return response.data
}

export async function deleteAdminQuotationItem(id: string): Promise<void> {
  await apiClient.delete(`/api/v2/admin/quotation-items/${id}`)
}

export async function getAdminQuotationItemGroups(query: AdminQuotationItemGroupsQuery = {}): Promise<AdminQuotationItemGroupListItem[]> {
  const response = await apiClient.get<AdminQuotationItemGroupListItem[]>('/api/v2/admin/quotation-item-groups', {
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

export async function createAdminQuotationItemGroup(request: CreateAdminQuotationItemGroupRequest): Promise<AdminQuotationItemGroupListItem> {
  const response = await apiClient.post<AdminQuotationItemGroupListItem>('/api/v2/admin/quotation-item-groups', request)
  return response.data
}

export async function updateAdminQuotationItemGroup(id: string, request: UpdateAdminQuotationItemGroupRequest): Promise<AdminQuotationItemGroupListItem> {
  const response = await apiClient.put<AdminQuotationItemGroupListItem>(`/api/v2/admin/quotation-item-groups/${id}`, request)
  return response.data
}

export async function deleteAdminQuotationItemGroup(id: string): Promise<void> {
  await apiClient.delete(`/api/v2/admin/quotation-item-groups/${id}`)
}
