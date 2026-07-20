import { apiClient } from './api'
import type { CreateCrmCompanyRequest, CrmCompany, CrmCompanyCreated, CrmMember, CrmMigratableCustomer, CrmOpportunity, CrmPerson, CrmStageOption, CrmTask, UpdateCrmCompanyRequest, UpdateCrmPersonRequest } from '@/types/api'

export interface CrmCompaniesQuery {
  lookup?: string
}

export async function getCrmCompanies(query: CrmCompaniesQuery = {}): Promise<CrmCompany[]> {
  const response = await apiClient.get<CrmCompany[]>('/api/v2/crm/companies', {
    params: {
      lookup: query.lookup ?? '',
    },
  })
  return response.data
}

export async function getCrmCompany(id: string): Promise<CrmCompany> {
  const response = await apiClient.get<CrmCompany>(`/api/v2/crm/companies/${id}`)
  return response.data
}

export async function updateCrmCompany(id: string, request: UpdateCrmCompanyRequest): Promise<CrmCompany> {
  const response = await apiClient.put<CrmCompany>(`/api/v2/crm/companies/${id}`, request)
  return response.data
}

export async function getCrmMigratableCustomers(): Promise<CrmMigratableCustomer[]> {
  const response = await apiClient.get<CrmMigratableCustomer[]>('/api/v2/crm/migratable-customers')
  return response.data
}

export async function createCrmCompany(request: CreateCrmCompanyRequest): Promise<CrmCompanyCreated> {
  const response = await apiClient.post<CrmCompanyCreated>('/api/v2/crm/companies', request)
  return response.data
}

export async function getCrmMembers(): Promise<CrmMember[]> {
  const response = await apiClient.get<CrmMember[]>('/api/v2/crm/members')
  return response.data
}

export async function getCrmPeople(lookup = ''): Promise<CrmPerson[]> {
  const response = await apiClient.get<CrmPerson[]>('/api/v2/crm/people', {
    params: { lookup },
  })
  return response.data
}

export async function updateCrmPerson(id: string, request: UpdateCrmPersonRequest): Promise<CrmPerson> {
  const response = await apiClient.put<CrmPerson>(`/api/v2/crm/people/${id}`, request)
  return response.data
}

export async function createCrmPerson(request: UpdateCrmPersonRequest): Promise<CrmPerson> {
  const response = await apiClient.post<CrmPerson>('/api/v2/crm/people', request)
  return response.data
}

export async function getCrmOpportunities(lookup = ''): Promise<CrmOpportunity[]> {
  const response = await apiClient.get<CrmOpportunity[]>('/api/v2/crm/opportunities', {
    params: { lookup },
  })
  return response.data
}

export async function getCrmOpportunity(id: string): Promise<CrmOpportunity> {
  const response = await apiClient.get<CrmOpportunity>(`/api/v2/crm/opportunities/${id}`)
  return response.data
}

export async function getCrmOpportunityStageOptions(): Promise<CrmStageOption[]> {
  const response = await apiClient.get<CrmStageOption[]>('/api/v2/crm/opportunities/stage-options')
  return response.data
}

export async function getCrmTaskStatusOptions(): Promise<CrmStageOption[]> {
  const response = await apiClient.get<CrmStageOption[]>('/api/v2/crm/tasks/status-options')
  return response.data
}

export async function createCrmOpportunity(request: {
  name: string
  stage: string
  closeDate: string | null
  amount: number | null
  currencyCode: string
  companyId?: string | null
  pointOfContactId?: string | null
  ownerId?: string | null
}): Promise<CrmOpportunity> {
  const response = await apiClient.post<CrmOpportunity>('/api/v2/crm/opportunities', request)
  return response.data
}

export async function updateCrmOpportunity(id: string, request: {
  name: string
  stage: string
  closeDate: string | null
  amount: number | null
  currencyCode: string
  companyId?: string | null
  pointOfContactId?: string | null
  ownerId?: string | null
}): Promise<CrmOpportunity> {
  const response = await apiClient.put<CrmOpportunity>(`/api/v2/crm/opportunities/${id}`, request)
  return response.data
}

export async function getCrmTasks(lookup = ''): Promise<CrmTask[]> {
  const response = await apiClient.get<CrmTask[]>('/api/v2/crm/tasks', {
    params: { lookup },
  })
  return response.data
}

export async function getCrmTask(id: string): Promise<CrmTask> {
  const response = await apiClient.get<CrmTask>(`/api/v2/crm/tasks/${id}`)
  return response.data
}

export interface CrmTaskRelationRequest {
  id: string
  type: string
}

export async function createCrmTask(request: {
  title: string
  body: string
  status: string
  dueDate: string | null
  assigneeId?: string | null
  relations?: CrmTaskRelationRequest[] | null
}): Promise<CrmTask> {
  const response = await apiClient.post<CrmTask>('/api/v2/crm/tasks', request)
  return response.data
}

export async function updateCrmTask(id: string, request: {
  title: string
  body: string
  status: string
  dueDate: string | null
  assigneeId?: string | null
  relations?: CrmTaskRelationRequest[] | null
}): Promise<CrmTask> {
  const response = await apiClient.put<CrmTask>(`/api/v2/crm/tasks/${id}`, request)
  return response.data
}
