import { apiClient } from './api'
import type { PaperlessNgxDocument } from '@/types/api'

export interface CompanyFilesResponse {
  baseUrl: string
  documents: PaperlessNgxDocument[]
}

export async function getCompanyPaperlessFiles(companyId: string, companyName: string): Promise<CompanyFilesResponse> {
  const response = await apiClient.get<CompanyFilesResponse>(`/api/v2/crm/companies/${companyId}/files`, {
    params: { name: companyName },
  })
  return response.data
}
