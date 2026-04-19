import { apiClient } from './api'
import type { JobOrderRecord, JobStatsRecord } from '@/types/api'

interface CreateJobOrderRequest {
  orderNumber: string
  jobNumber: string
  customerName: string
  customerRef: string
  orderTitle: string
  orderedOn: string
  requiredOn: string
  qty: number
  paymentTerms: string
  remarks: string
  status: number
}

interface UpdateJobOrderRequest {
  customerName: string
  customerRef: string
  orderTitle: string
  requiredOn: string
  qty: number
  paymentTerms: string
  remarks: string
  status: number
}

export async function getJobOrders(): Promise<JobOrderRecord[]> {
  const response = await apiClient.get<JobOrderRecord[]>('/api/v2/job-orders')
  return response.data
}

export async function getOrderList(params: {
  lookup?: string
  commonQuery?: number
  startsWith?: string
  take?: number
  startOn?: string
  endOn?: string
}): Promise<JobOrderRecord[]> {
  const response = await apiClient.get<JobOrderRecord[]>('/api/v2/job-orders', {
    params: {
      lookup: params.lookup,
      commonQuery: params.commonQuery,
      startsWith: params.startsWith,
      take: params.take,
      startOn: params.startOn,
      endOn: params.endOn,
      listType: 'order',
    },
  })

  return response.data
}

export async function getJobList(params: {
  lookup?: string
  commonQuery?: number
  startsWith?: string
  take?: number
  startOn?: string
  endOn?: string
}): Promise<JobOrderRecord[]> {
  const response = await apiClient.get<JobOrderRecord[]>('/api/v2/job-orders', {
    params: {
      lookup: params.lookup,
      commonQuery: params.commonQuery,
      startsWith: params.startsWith,
      take: params.take,
      startOn: params.startOn,
      endOn: params.endOn,
      listType: 'job',
    },
  })

  return response.data
}

export async function getJobStats(params: {
  startOn?: string
  endOn?: string
}): Promise<JobStatsRecord[]> {
  const response = await apiClient.get<JobStatsRecord[]>('/api/v2/job-orders/stats', {
    params: {
      startOn: params.startOn,
      endOn: params.endOn,
    },
  })

  return response.data
}

export async function getJobOrder(id: string): Promise<JobOrderRecord> {
  const response = await apiClient.get<JobOrderRecord>(`/api/v2/job-orders/${id}`)
  return response.data
}

export async function getJobPreviewBlob(orderId: string, fileName: string, attachmentType?: string): Promise<Blob> {
  const response = await apiClient.get(`/api/Job/preview/${orderId}/${encodeURIComponent(fileName)}`, {
    params: {
      attachmentType,
    },
    responseType: 'blob',
  })

  return response.data as Blob
}

export async function deleteJobOrder(id: string): Promise<void> {
  await apiClient.delete(`/api/v2/job-orders/${id}`)
}

export async function createJobOrder(data: CreateJobOrderRequest): Promise<JobOrderRecord> {
  const response = await apiClient.post<JobOrderRecord>('/api/v2/job-orders', data)
  return response.data
}

export async function updateJobOrder(id: string, data: UpdateJobOrderRequest): Promise<JobOrderRecord> {
  const response = await apiClient.put<JobOrderRecord>(`/api/v2/job-orders/${id}`, data)
  return response.data
}