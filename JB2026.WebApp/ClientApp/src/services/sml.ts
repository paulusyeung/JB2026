import { apiClient } from './api'
import type { SmlInvoiceListResponse, SmlRtfListResponse, SmlStatsResponse } from '@/types/api'

export interface SmlStatsQuery {
  startOn: string
  days?: number
  take?: number
}

export interface SmlRtfListQuery {
  lookup?: string
  commonQuery?: number
  shortcut?: string
  take?: number
}

export interface SmlInvoiceListQuery {
  lookup?: string
  commonQuery?: number
  take?: number
}

export async function getSmlStats(query: SmlStatsQuery): Promise<SmlStatsResponse> {
  const response = await apiClient.get<SmlStatsResponse>('/api/v2/sml/stats', {
    params: {
      startOn: query.startOn,
      days: query.days ?? 31,
      take: query.take ?? 500,
    },
  })

  return response.data
}

export async function getSmlRtfList(query: SmlRtfListQuery): Promise<SmlRtfListResponse> {
  const response = await apiClient.get<SmlRtfListResponse>('/api/v2/sml/rtf-list', {
    params: {
      lookup: query.lookup,
      commonQuery: query.commonQuery ?? 1,
      shortcut: query.shortcut ?? 'All',
      take: query.take ?? 500,
    },
  })

  return response.data
}

export async function getSmlInvoiceList(query: SmlInvoiceListQuery): Promise<SmlInvoiceListResponse> {
  const response = await apiClient.get<SmlInvoiceListResponse>('/api/v2/sml/invoice-list', {
    params: {
      lookup: query.lookup,
      commonQuery: query.commonQuery ?? 1,
      take: query.take ?? 500,
    },
  })

  return response.data
}