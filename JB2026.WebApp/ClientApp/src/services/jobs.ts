import { apiClient } from './api'
import type { JobDetail, JobListItem } from '@/types/api'

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