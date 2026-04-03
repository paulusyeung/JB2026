import { apiClient } from './api'
import type { JobScheduleCalendarItem, JobSchedulePendingItem, UpdateJobScheduleTimeRequest } from '@/types/api'

export interface ScheduleRangeQuery {
  startOn: string
  days: number
}

export interface PendingScheduleQuery {
  lookup?: string
  commonQuery?: number
  startsWith?: string
  take?: number
}

export async function getScheduleRange(query: ScheduleRangeQuery): Promise<JobScheduleCalendarItem[]> {
  const response = await apiClient.get<JobScheduleCalendarItem[]>('/api/v2/job-schedules/range', {
    params: query,
  })

  return response.data
}

export async function getPendingSchedule(query: PendingScheduleQuery): Promise<JobSchedulePendingItem[]> {
  const response = await apiClient.get<JobSchedulePendingItem[]>('/api/v2/job-schedules/pending', {
    params: query,
  })

  return response.data
}

export async function updateScheduleTime(
  scheduleId: string,
  request: UpdateJobScheduleTimeRequest,
): Promise<void> {
  await apiClient.patch(`/api/v2/job-schedules/${scheduleId}/time`, request)
}
