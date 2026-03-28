import { apiClient } from './api'
import type { JobScheduleCalendarItem, UpdateJobScheduleTimeRequest } from '@/types/api'

export interface ScheduleRangeQuery {
  startOn: string
  days: number
}

export async function getScheduleRange(query: ScheduleRangeQuery): Promise<JobScheduleCalendarItem[]> {
  const response = await apiClient.get<JobScheduleCalendarItem[]>('/api/v2/job-schedules/range', {
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
