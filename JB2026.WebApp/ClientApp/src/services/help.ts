import { apiClient } from './api'
import type { HelpArticle } from '@/types/api'

export async function getHelpArticles(): Promise<HelpArticle[]> {
  const response = await apiClient.get<HelpArticle[]>('/api/v2/help/articles')
  return response.data
}