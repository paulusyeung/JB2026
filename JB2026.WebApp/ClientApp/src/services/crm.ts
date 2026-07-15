import { apiClient } from './api'
import type { CrmCompany } from '@/types/api'

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
