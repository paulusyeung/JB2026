import { apiClient } from './api'
import type { QuotationListItem } from '@/types/api'

export interface QuotationQuery {
  startOn: string
  days: number
}

export interface UpsertQuotationRequest {
  quoteNumber: number
  quoteNumberIndex: number
  customerName: string
  printTitle: string
  quotedOn: string
  quotedBy: string
  totalCostA: number
  unitCostA: number
  status: number
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

export async function createQuotation(request: UpsertQuotationRequest): Promise<QuotationListItem> {
  const response = await apiClient.post<QuotationListItem>('/api/v2/quotations', request)
  return response.data
}

export async function updateQuotation(headerId: string, request: UpsertQuotationRequest): Promise<QuotationListItem> {
  const response = await apiClient.put<QuotationListItem>(`/api/v2/quotations/${headerId}`, request)
  return response.data
}