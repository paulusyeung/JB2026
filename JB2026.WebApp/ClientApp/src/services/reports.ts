import { apiClient } from './api'
import type { ReportRunResponse, RunReportRequest } from '@/types/api'

export async function runReport(request: RunReportRequest): Promise<ReportRunResponse> {
  const response = await apiClient.post<ReportRunResponse>('/api/v2/reports/run', request)
  return response.data
}