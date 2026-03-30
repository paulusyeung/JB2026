import { apiClient } from './api'
import type { SmlStatsResponse } from '@/types/api'

export interface SmlStatsQuery {
  startOn: string
  days?: number
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