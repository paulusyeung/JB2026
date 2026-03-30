import { apiClient } from './api'
import type { StockProductListItem } from '@/types/api'

export interface StockProductsQuery {
  keyword?: string
  take?: number
}

export async function getStockProducts(query: StockProductsQuery = {}): Promise<StockProductListItem[]> {
  const response = await apiClient.get<StockProductListItem[]>('/api/v2/stock/products', {
    params: {
      keyword: query.keyword ?? '',
      take: query.take ?? 100,
    },
  })

  return response.data
}