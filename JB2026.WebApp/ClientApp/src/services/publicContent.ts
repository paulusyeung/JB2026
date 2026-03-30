import { apiClient } from './api'
import type { PublicContentItem } from '@/types/api'

export async function getPublicContent(): Promise<PublicContentItem[]> {
  const response = await apiClient.get<PublicContentItem[]>('/api/v2/public/content')
  return response.data
}