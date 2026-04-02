import { apiClient } from './api'
import type { JobOrderRecord } from '@/types/api'

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
}): Promise<JobOrderRecord[]> {
  const response = await apiClient.get<JobOrderRecord[]>('/api/v2/job-orders', {
    params: {
      lookup: params.lookup,
      commonQuery: params.commonQuery,
      startsWith: params.startsWith,
      take: params.take,
    },
  })

  return response.data
}

export async function getJobOrder(id: string): Promise<JobOrderRecord> {
  const response = await apiClient.get<JobOrderRecord>(`/api/v2/job-orders/${id}`)
  return response.data
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