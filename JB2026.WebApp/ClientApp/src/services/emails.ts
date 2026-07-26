import { apiClient } from './api'
import type { EmailMessage, EmailDetail } from '@/types/api'

export async function searchEmails(lookup?: string): Promise<EmailMessage[]> {
  const params = lookup ? { lookup } : undefined
  const { data } = await apiClient.get<EmailMessage[]>('/api/v2/email/search', { params })
  return data
}

export async function getEmailDetail(id: string, folder: string): Promise<EmailDetail> {
  const { data } = await apiClient.get<EmailDetail>(`/api/v2/email/${encodeURIComponent(id)}`, {
    params: { folder },
  })
  return data
}

export async function downloadAttachment(id: string, folder: string, fileName: string): Promise<void> {
  const res = await apiClient.get(`/api/v2/email/${encodeURIComponent(id)}/download`, {
    params: { folder, fileName },
    responseType: 'blob',
  })

  const blobUrl = URL.createObjectURL(res.data as Blob)
  const a = document.createElement('a')
  a.href = blobUrl
  a.download = fileName
  a.click()
  URL.revokeObjectURL(blobUrl)
}
