import { apiClient } from './api'
import type { JobOrderRecord } from '@/types/api'

export async function getJobOrders(): Promise<JobOrderRecord[]> {
  const response = await apiClient.get<JobOrderRecord[]>('/api/v2/job-orders')
  return response.data
}

export async function getJobOrder(id: string): Promise<JobOrderRecord> {
  const response = await apiClient.get<JobOrderRecord>(`/api/v2/job-orders/${id}`)
  return response.data
}