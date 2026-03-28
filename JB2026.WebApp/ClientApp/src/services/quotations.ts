import { apiClient } from './api'
import type { QuotationListItem } from '@/types/api'

export interface QuotationQuery {
  startOn: string
  days: number
}

export async function getQuotations(query: QuotationQuery): Promise<QuotationListItem[]> {
  const response = await apiClient.get<QuotationListItem[]>('/api/v2/quotations', {
    params: query,
  })

  return response.data
}

export async function searchQuotations(keyword: string): Promise<QuotationListItem[]> {
  const response = await apiClient.get<QuotationListItem[]>(`/api/v2/quotations/search/${encodeURIComponent(keyword)}`)
  return response.data
}