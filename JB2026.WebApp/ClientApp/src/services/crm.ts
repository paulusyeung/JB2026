import { apiClient } from './api'
import type { CreateCrmCompanyRequest, CrmCatalogItem, CrmCompany, CrmCompanyCreated, CrmMember, CrmMigratableCustomer, CrmPerson, UpdateCrmCompanyRequest, UpdateCrmPersonRequest } from '@/types/api'

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

export async function getCrmOpportunities(lookup = ''): Promise<CrmCatalogItem[]> {
  const response = await apiClient.get<CrmCatalogItem[]>('/api/v2/crm/opportunities', {
    params: { lookup },
  })
  return response.data
}
