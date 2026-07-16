import { apiClient } from './api'
import type { CrmCatalogItem, CrmCompany, CrmMember, UpdateCrmCompanyRequest } from '@/types/api'

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

export async function getCrmMembers(): Promise<CrmMember[]> {
  const response = await apiClient.get<CrmMember[]>('/api/v2/crm/members')
  return response.data
}

export async function getCrmPeople(lookup = ''): Promise<CrmCatalogItem[]> {
  const response = await apiClient.get<CrmCatalogItem[]>('/api/v2/crm/people', {
    params: { lookup },
  })
  return response.data
}

export async function getCrmOpportunities(lookup = ''): Promise<CrmCatalogItem[]> {
  const response = await apiClient.get<CrmCatalogItem[]>('/api/v2/crm/opportunities', {
    params: { lookup },
  })
  return response.data
}
