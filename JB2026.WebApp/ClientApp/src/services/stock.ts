import { apiClient } from './api'
import type {
  StockProductAttachment,
  StockProductAttachmentDeleteResult,
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

export async function printProductRecord(productId: string): Promise<Blob> {
  const response = await apiClient.get<Blob>(`/api/v2/stock/products/${productId}/print`, {
    responseType: 'blob',
  })
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

export async function getProductAttachments(productId: string): Promise<StockProductAttachment[]> {
  const response = await apiClient.get<StockProductAttachment[]>(`/api/v2/stock/products/${productId}/attachments`)
  return response.data
}

export async function uploadProductAttachments(productId: string, files: File[]): Promise<StockProductAttachment[]> {
  const formData = new FormData()
  for (const file of files) {
    formData.append('files', file)
  }

  const response = await apiClient.post<StockProductAttachment[]>(
    `/api/v2/stock/products/${productId}/attachments`,
    formData,
  )

  return response.data
}

export async function deleteProductAttachments(productId: string, attachmentIds: string[]): Promise<StockProductAttachmentDeleteResult> {
  const response = await apiClient.delete<StockProductAttachmentDeleteResult>(`/api/v2/stock/products/${productId}/attachments`, {
    data: {
      attachmentIds,
    },
  })
  return response.data
}

export async function getProductAttachmentBlob(productId: string, attachmentId: string, inline = false): Promise<Blob> {
  const response = await apiClient.get(`/api/v2/stock/products/${productId}/attachments/${attachmentId}`, {
    params: {
      inline,
    },
    responseType: 'blob',
  })

  return response.data as Blob
}

export function mapStockInOutError(error: unknown): string {
  if (error && typeof error === 'object' && 'response' in error) {
    const response = (error as { response?: { status?: number; data?: { errors?: Record<string, string[]> } } }).response
    if (response?.status === 400 && response.data?.errors) {
      const messages = Object.values(response.data.errors).flat()
      if (messages.length > 0) {
        return messages[0] ?? 'stock.stockInOut.errors.saveFailed'
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
      customerCode: parts[0] ?? '',
      categoryCode: parts[1] ?? '',
      sequenceNumber: parts[2] ?? '',
    }
  }

  // No dashes found - try to extract components from concatenated format
  // Pattern: first 3 chars = customer, next 3 chars = category, rest = sequence
  if (normalized.length >= 6) {
    return {
      customerCode: normalized.substring(0, 3).trim(),
      categoryCode: normalized.substring(3, 6).trim(),
      sequenceNumber: normalized.substring(6).trim(),
    }
  }

  // If less than 6 characters, can't determine format
  return {
    customerCode: '',
    categoryCode: '',
    sequenceNumber: normalized,
  }
}

export function mapStockAttachmentError(error: unknown): string {
  if (error && typeof error === 'object' && 'response' in error) {
    const response = (error as { response?: { status?: number; data?: { errors?: Record<string, string[]> } } }).response
    if (response?.status === 400 && response.data?.errors) {
      const messages = Object.values(response.data.errors).flat()
      if (messages.length > 0) {
        const message = messages[0] ?? ''
        if (message.toLowerCase().includes('25mb')) {
          return 'stock.attachments.errors.fileTooLarge'
        }
        return 'stock.attachments.errors.validationFailed'
      }
    }

    if (response?.status === 404) {
      return 'stock.attachments.errors.productNotFound'
    }

    if (response?.status) {
      const detail = (response.data as { detail?: string; title?: string })?.detail
        ?? (response.data as { detail?: string; title?: string })?.title
        ?? JSON.stringify(response.data).slice(0, 200)
      return `stock.attachments.errors.generalFailure (HTTP ${response.status}: ${detail})`
    }
  }

  return 'stock.attachments.errors.generalFailure'
}