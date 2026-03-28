import { apiClient } from './api'
import type { JobDetail, JobListItem, JobOrderFormData } from '@/types/api'

export interface JobQuery {
  startOn: string
  days: number
}

export async function getJobs(query: JobQuery): Promise<JobListItem[]> {
  const response = await apiClient.get<JobListItem[]>('/api/v2/jobs/range', {
    params: query,
  })

  return response.data
}

export async function getJobDetail(id: string): Promise<JobDetail> {
  const response = await apiClient.get<JobDetail>(`/api/v2/jobs/${id}`)
  return response.data
}

/**
 * Persist a job order form (create or update).
 * Routes to POST /api/v2/jobs when orderId is null, PATCH /api/v2/jobs/{id} otherwise.
 */
export async function saveJob(data: JobOrderFormData): Promise<void> {
  if (data.orderId) {
    await apiClient.patch(`/api/v2/jobs/${data.orderId}`, data)
  } else {
    await apiClient.post('/api/v2/jobs', data)
  }
}