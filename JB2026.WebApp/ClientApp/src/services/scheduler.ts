import { apiClient } from './api'
import type {
  CompletePackingOnAirRequest,
  JobPackingOnAirAvailableItem,
  JobPackingOnAirItem,
  JobScheduleAvailableItem,
  JobScheduleCalendarItem,
  JobScheduleCompletedItem,
  JobScheduleOnAirItem,
  JobSchedulePackingItem,
  JobSchedulePendingItem,
  RescheduleCompletedSchedulesRequest,
  SavePackingOnAirBatchRequest,
  SaveScheduleBatchRequest,
  UpdateJobScheduleTimeRequest,
} from '@/types/api'

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

export interface CompletedScheduleQuery {
  lookup?: string
  commonQuery?: number
  machine?: string
  startsWith?: string
  take?: number
}

export interface PackingScheduleQuery {
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

export async function getCompletedSchedule(query: CompletedScheduleQuery): Promise<JobScheduleCompletedItem[]> {
  const response = await apiClient.get<JobScheduleCompletedItem[]>('/api/v2/job-schedules/completed', {
    params: query,
  })

  return response.data
}

export async function getPackingSchedule(query: PackingScheduleQuery): Promise<JobSchedulePackingItem[]> {
  const response = await apiClient.get<JobSchedulePackingItem[]>('/api/v2/job-schedules/packing', {
    params: query,
  })

  return response.data
}

export async function getPackingOnAirAvailable(orderType = 0): Promise<JobPackingOnAirAvailableItem[]> {
  const response = await apiClient.get<JobPackingOnAirAvailableItem[]>('/api/v2/job-schedules/packing-on-air/available', {
    params: { orderType },
  })

  return response.data
}

export async function getPackingOnAir(orderType = 0): Promise<JobPackingOnAirItem[]> {
  const response = await apiClient.get<JobPackingOnAirItem[]>('/api/v2/job-schedules/packing-on-air', {
    params: { orderType },
  })

  return response.data
}

export async function savePackingOnAirBatch(request: SavePackingOnAirBatchRequest): Promise<void> {
  await apiClient.post('/api/v2/job-schedules/packing-on-air/batch', request)
}

export async function completePackingOnAir(request: CompletePackingOnAirRequest): Promise<void> {
  await apiClient.post('/api/v2/job-schedules/packing-on-air/complete', request)
}

export async function updateScheduleTime(
  scheduleId: string,
  request: UpdateJobScheduleTimeRequest,
): Promise<void> {
  await apiClient.patch(`/api/v2/job-schedules/${scheduleId}/time`, request)
}

export async function getAvailableSchedule(orderType = 0): Promise<JobScheduleAvailableItem[]> {
  const response = await apiClient.get<JobScheduleAvailableItem[]>('/api/v2/job-schedules/available', {
    params: { orderType },
  })
  return response.data
}

export async function getOnAirSchedule(orderType = 0, machine?: string): Promise<JobScheduleOnAirItem[]> {
  const response = await apiClient.get<JobScheduleOnAirItem[]>('/api/v2/job-schedules/on-air', {
    params: { orderType, machine },
  })
  return response.data
}

export async function saveScheduleBatch(request: SaveScheduleBatchRequest): Promise<void> {
  await apiClient.post('/api/v2/job-schedules/batch', request)
}

export async function rescheduleCompletedOrders(request: RescheduleCompletedSchedulesRequest): Promise<void> {
  await apiClient.post('/api/v2/job-schedules/completed/reschedule', request)
}
