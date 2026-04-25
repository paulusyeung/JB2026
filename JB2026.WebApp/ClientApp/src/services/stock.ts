import { apiClient } from './api'
import type {
  StockInOutTransactionRequest,
  StockInOutTransactionResult,
  StockProductCodeValidationResponse,
  StockProductDeleteResult,
  StockProductListItem,
  StockProductMovementHistoryItem,
  StockProductNextNumberResponse,
  StockProductRecord,
  StockProductRecordUpsertRequest,
} from '@/types/api'

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

export async function getProductRecord(productId: string): Promise<StockProductRecord> {
  const response = await apiClient.get<StockProductRecord>(`/api/v2/stock/products/${productId}`)
  return response.data
}

export async function createProductRecord(payload: StockProductRecordUpsertRequest): Promise<StockProductRecord> {
  const response = await apiClient.post<StockProductRecord>('/api/v2/stock/products', payload)
  return response.data
}

export async function updateProductRecord(productId: string, payload: StockProductRecordUpsertRequest): Promise<StockProductRecord> {
  const response = await apiClient.put<StockProductRecord>(`/api/v2/stock/products/${productId}`, payload)
  return response.data
}

export async function deleteProductRecord(productId: string): Promise<StockProductDeleteResult> {
  const response = await apiClient.delete<StockProductDeleteResult>(`/api/v2/stock/products/${productId}`)
  return response.data
}

export async function getProductStockMovements(productId: string): Promise<StockProductMovementHistoryItem[]> {
  const response = await apiClient.get<StockProductMovementHistoryItem[]>(`/api/v2/stock/products/${productId}/movements`)
  return response.data
}

export async function getNextProductNumber(customerCode: string, categoryCode: string): Promise<StockProductNextNumberResponse> {
  const response = await apiClient.get<StockProductNextNumberResponse>('/api/v2/stock/products/next-number', {
    params: {
      customerCode,
      categoryCode,
    },
  })

  return response.data
}

export async function validateProductCodeUniqueness(productCode: string, excludeProductId?: string): Promise<boolean> {
  const response = await apiClient.get<StockProductCodeValidationResponse>('/api/v2/stock/products/validate-code', {
    params: {
      productCode,
      excludeProductId,
    },
  })

  return response.data.isUnique
}

export async function createStockInOutTransaction(
  productId: string,
  request: StockInOutTransactionRequest,
): Promise<StockInOutTransactionResult> {
  const response = await apiClient.post<StockInOutTransactionResult>(
    `/api/v2/stock/products/${productId}/transactions`,
    request,
  )
  return response.data
}

export function mapStockInOutError(error: unknown): string {
  if (error && typeof error === 'object' && 'response' in error) {
    const response = (error as { response?: { status?: number; data?: { errors?: Record<string, string[]> } } }).response
    if (response?.status === 400 && response.data?.errors) {
      const messages = Object.values(response.data.errors).flat()
      if (messages.length > 0) {
        return messages[0]
      }
    }
    if (response?.status === 404) {
      return 'stock.stockInOut.errors.productNotFound'
    }
  }
  return 'stock.stockInOut.errors.saveFailed'
}

export function composeStockNumber(customerCode: string, categoryCode: string, sequenceNumber: string): string {
  const customer = customerCode.trim().toUpperCase()
  const category = categoryCode.trim().toUpperCase()
  const sequence = sequenceNumber.trim().padStart(4, '0')
  return `${customer}-${category}-${sequence}`
}

export function parseStockNumber(stockNumber: string): { customerCode: string; categoryCode: string; sequenceNumber: string } {
  const normalized = stockNumber.trim()
  if (!normalized) {
    return {
      customerCode: '',
      categoryCode: '',
      sequenceNumber: '',
    }
  }

  const parts = normalized.split('-').map((part) => part.trim()).filter(Boolean)
  if (parts.length >= 3) {
    return {
      customerCode: parts[0],
      categoryCode: parts[1],
      sequenceNumber: parts[2],
    }
  }

  return {
    customerCode: '',
    categoryCode: '',
    sequenceNumber: normalized,
  }
}