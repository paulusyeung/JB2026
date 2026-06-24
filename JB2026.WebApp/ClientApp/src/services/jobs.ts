import { apiClient } from './api'
import type { JobDetail, JobListItem, JobOrderFormData, JobOrderPrintRequest } from '@/types/api'

interface CreateJobRequest {
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
  orderType: number
  soNumber?: string
  originalSONumber?: string
  workflowAttributes?: Record<string, string>
}

interface UpdateJobRequest {
  customerName: string
  customerRef: string
  orderTitle: string
  requiredOn: string
  qty: number
  paymentTerms: string
  remarks: string
  productDetails?: string
  status: number
  orderType: number
  soNumber?: string
  originalSONumber?: string
  workflowAttributes?: Record<string, string>
}

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

export async function getJobPdfBlob(id: string): Promise<Blob> {
  const response = await apiClient.get(`/api/Job/pdf/${id}`, {
    responseType: 'blob',
  })

  return response.data as Blob
}

export async function printJobOrder(id: string, options: JobOrderPrintRequest): Promise<Blob> {
  const response = await apiClient.post(`/api/v2/jobs/${id}/print`, options, {
    responseType: 'blob',
  })

  // Check if the response is actually an error (backend returns JSON error as blob)
  if (response.status !== 200) {
    const text = await (response.data as Blob).text()
    try {
      const error = JSON.parse(text)
      throw new Error(error.title || error.detail || 'Print failed')
    } catch {
      throw new Error('Unable to generate the order PDF')
    }
  }

  return response.data as Blob
}

export async function uploadJobAttachment(orderId: string, file: File): Promise<void> {
  const formData = new FormData()
  formData.append('files', file)

  await apiClient.post(`/api/v2/jobs/${orderId}/attachments`, formData)
}

export async function deleteJobAttachments(orderId: string, attachmentIds: string[]): Promise<void> {
  await apiClient.delete(`/api/v2/jobs/${orderId}/attachments`, {
    data: {
      attachmentIds,
    },
  })
}

/**
 * Persist a job order form (create or update).
 * Routes to POST /api/v2/jobs when orderId is null, PATCH /api/v2/jobs/{id} otherwise.
 */
export async function saveJob(data: JobOrderFormData): Promise<void> {
  if (data.orderId) {
    const payload: UpdateJobRequest = {
      customerName: data.customerName,
      customerRef: data.customerRef,
      orderTitle: data.orderTitle,
      requiredOn: data.requiredOn,
      qty: data.qty,
      paymentTerms: data.paymentTerms,
      remarks: data.remarks,
      productDetails: data.productDetails,
      status: data.status,
      orderType: data.orderType,
      soNumber: data.soNumber,
      originalSONumber: data.originalSONumber,
      workflowAttributes: data.workflowAttributes,
    }

    await apiClient.patch(`/api/v2/jobs/${data.orderId}`, payload)
  } else {
    const payload: CreateJobRequest = {
      orderNumber: data.orderNumber,
      jobNumber: data.jobNumber,
      customerName: data.customerName,
      customerRef: data.customerRef,
      orderTitle: data.orderTitle,
      orderedOn: data.orderedOn,
      requiredOn: data.requiredOn,
      qty: data.qty,
      paymentTerms: data.paymentTerms,
      remarks: data.remarks,
      status: data.status,
      orderType: data.orderType,
      soNumber: data.soNumber,
      originalSONumber: data.originalSONumber,
      workflowAttributes: data.workflowAttributes,
    }

    await apiClient.post('/api/v2/jobs', payload)
  }
}