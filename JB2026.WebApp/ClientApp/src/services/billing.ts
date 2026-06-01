import { apiClient } from './api'

/**
 * Response from connectivity check endpoint.
 */
export interface BillingConnectivityResponse {
  isConnected: boolean
  statusMessage: string
}

/**
 * Request to sync a customer to Invoice Ninja.
 */
export interface SyncCustomerRequest {
  customerId: string
  customerCode?: string
  customerName?: string
  billTo?: string
  shipToAddresses?: string[]
  existingInvoiceNinjaClientId?: string
}

/**
 * Response from customer sync operation.
 */
export interface SyncCustomerResponse {
  invoiceNinjaClientId: string
  syncedAt: string
  metadataToMerge: string
}

/**
 * Line item data for invoice generation.
 */
export interface InvoiceLineItemData {
  description: string
  quantity: number
  unitCost: number
}

/**
 * Request to generate an invoice from a Job Order.
 */
export interface GenerateInvoiceRequest {
  orderId?: string
  invoiceNinjaClientId: string
  jobNumber: string
  poNumber?: string
  lineItems: InvoiceLineItemData[]
}

/**
 * Request to preview invoice payload before creation.
 */
export interface PreviewInvoiceRequest {
  customerName: string
  billTo: string
  shipTo: string
  jobNumber: string
  poNumber?: string
  lineItems: InvoiceLineItemData[]
}

/**
 * Resolved fields for invoice preview.
 */
export interface InvoicePreviewResolvedFields {
  billToCustomField?: string
  shipToCustomField?: string
  jobNoCustomField?: string
  poNoCustomField?: string
  allCustomFieldsConfigured: boolean
}

/**
 * Response from invoice preview endpoint.
 */
export interface PreviewInvoiceResponse {
  customerName: string
  totalAmount: number
  lineItems: InvoiceLineItemData[]
  resolvedCustomFields: InvoicePreviewResolvedFields
  warnings: string[]
}

/**
 * Invoice billing summary.
 */
export interface InvoiceBillingSummary {
  externalInvoiceId: string
  invoiceNumber: string
  clientName: string
  invoiceDate?: string
  amount: number
  status: string
  dueDate?: string
  lastSyncedAt?: string
}

/**
 * Response from invoice generation operation.
 */
export interface GenerateInvoiceResponse {
  billingSummary: InvoiceBillingSummary
  createdAt: string
}

/**
 * Response for invoice summary retrieval.
 */
export interface GetInvoiceSummaryResponse {
  billingSummary: InvoiceBillingSummary | null
}

/**
 * Response for invoice status refresh.
 */
export interface RefreshInvoiceStatusResponse {
  billingSummary: InvoiceBillingSummary | null
  refreshedAt: string
}

/**
 * Response for sending a draft invoice.
 */
export interface SendInvoiceResponse {
  billingSummary: InvoiceBillingSummary
  sentAt: string
}

/**
 * Response for invoice list retrieval.
 */
export interface ListInvoicesResponse {
  invoices: InvoiceBillingSummary[]
}

/**
 * Error response for failed billing operations.
 */
export interface BillingErrorResponse {
  errorCode: string
  message: string
  details?: any
}

/**
 * Checks connectivity to Invoice Ninja and validates configuration.
 */
export async function checkBillingConnectivity(): Promise<BillingConnectivityResponse> {
  const response = await apiClient.get<BillingConnectivityResponse>('/api/v2/billing/connectivity')
  return response.data
}

/**
 * Synchronizes a JB2026 customer to Invoice Ninja.
 * If the customer was previously synced, it will be updated.
 * Otherwise, a new Invoice Ninja client will be created.
 *
 * @param request Customer sync request with mapping data.
 * @returns Invoice Ninja client ID and metadata to persist in customer record.
 */
export async function syncCustomerToBilling(request: SyncCustomerRequest): Promise<SyncCustomerResponse> {
  const response = await apiClient.post<SyncCustomerResponse>('/api/v2/billing/customers/sync', request)
  return response.data
}

/**
 * Generates an invoice in Invoice Ninja from a Job Order.
 * Pre-condition: The associated customer must already be synced to Invoice Ninja.
 *
 * @param request Invoice generation request with job and line item data.
 * @returns Billing summary with external invoice ID to persist in job metadata.
 */
export async function generateInvoice(request: GenerateInvoiceRequest): Promise<GenerateInvoiceResponse> {
  const response = await apiClient.post<GenerateInvoiceResponse>('/api/v2/billing/invoices/generate', request)
  return response.data
}

/**
 * Generates an invoice directly from a JB2026 Job Order using synchronized billing metadata.
 */
export async function generateInvoiceFromJobOrder(orderId: string): Promise<GenerateInvoiceResponse> {
  const response = await apiClient.post<GenerateInvoiceResponse>(`/api/v2/billing/invoices/generate-from-job/${orderId}`)
  return response.data
}

/**
 * Previews invoice payload and resolved custom fields before creation.
 */
export async function previewInvoice(request: PreviewInvoiceRequest): Promise<PreviewInvoiceResponse> {
  const response = await apiClient.post<PreviewInvoiceResponse>('/api/v2/billing/invoices/preview', request)
  return response.data
}

/**
 * Lists invoice summaries for billing list screens.
 */
export async function listInvoices(): Promise<InvoiceBillingSummary[]> {
  const response = await apiClient.get<ListInvoicesResponse>('/api/v2/billing/invoices')
  return response.data.invoices
}

/**
 * Retrieves the billing summary for an Invoice Ninja invoice by its external ID.
 * Used for displaying invoice status in billing and job/order screens.
 *
 * @param externalInvoiceId Invoice Ninja invoice ID.
 * @returns Billing summary if found; null if not found.
 */
export async function getInvoiceSummary(externalInvoiceId: string): Promise<InvoiceBillingSummary | null> {
  const response = await apiClient.get<GetInvoiceSummaryResponse>(
    `/api/v2/billing/invoices/${externalInvoiceId}/summary`
  )
  return response.data.billingSummary
}

/**
 * Refreshes the status of an Invoice Ninja invoice by fetching the latest data.
 *
 * @param externalInvoiceId Invoice Ninja invoice ID.
 * @returns Updated billing summary if found; null if not found.
 */
export async function refreshInvoiceStatus(externalInvoiceId: string): Promise<InvoiceBillingSummary | null> {
  const response = await apiClient.post<RefreshInvoiceStatusResponse>(
    `/api/v2/billing/invoices/${externalInvoiceId}/refresh`
  )
  return response.data.billingSummary
}

/**
 * Sends a draft invoice via Invoice Ninja, transitioning it from Draft to Sent.
 *
 * @param externalInvoiceId Invoice Ninja invoice ID.
 * @returns Updated billing summary with status Sent.
 * @throws Error if the invoice is not in Draft status or the request fails.
 */
export async function sendInvoice(externalInvoiceId: string): Promise<InvoiceBillingSummary> {
  const response = await apiClient.post<SendInvoiceResponse>(
    `/api/v2/billing/invoices/${externalInvoiceId}/send`
  )
  return response.data.billingSummary
}

/**
 * Downloads the invoice PDF from Invoice Ninja for the given invoice ID.
 *
 * @param externalInvoiceId Invoice Ninja invoice ID.
 * @returns Promise<Blob> containing the PDF file data.
 * @throws Error if the invoice is not found or download fails.
 */
export async function downloadInvoicePdf(externalInvoiceId: string): Promise<Blob> {
  const response = await apiClient.get<Blob>(
    `/api/v2/billing/invoices/${externalInvoiceId}/download/pdf`,
    { responseType: 'blob' }
  )
  return response.data
}

/**
 * Downloads the delivery note PDF from Invoice Ninja for the given invoice ID.
 *
 * @param externalInvoiceId Invoice Ninja invoice ID.
 * @returns Promise<Blob> containing the PDF file data.
 * @throws Error if the invoice is not found or delivery note is not available.
 */
export async function downloadDeliveryNote(externalInvoiceId: string): Promise<Blob> {
  const response = await apiClient.get<Blob>(
    `/api/v2/billing/invoices/${externalInvoiceId}/download/delivery-note`,
    { responseType: 'blob' }
  )
  return response.data
}

// ── Invoice Editor Types & Functions ─────────────────────────────────────────

/**
 * A selectable Invoice Ninja client for billing screens.
 */
export interface BillingClientOption {
  externalClientId: string
  name: string
  displayName: string
  idNumber: string
  outstandingBalance: number
}

export type BillingStatementClient = BillingClientOption

/**
 * Response for the billing client list endpoint.
 */
export interface ListBillingClientsResponse {
  clients: BillingClientOption[]
}

/**
 * A single line item as returned by the invoice editor detail endpoint.
 */
export interface InvoiceEditorLineItem {
  id?: string
  poNumber: string
  description: string
  qty: number
  unit: string
  unitCost: number
  lineTotal: number
}

/**
 * Normalized invoice DTO returned by the editor detail endpoint.
 */
export interface InvoiceEditorDto {
  externalInvoiceId?: string
  status?: string
  client?: BillingClientOption
  invoiceDate?: string
  jobNumber: string
  lineItems: InvoiceEditorLineItem[]
  totalAmount: number
}

/**
 * Response for GET /api/v2/billing/invoices/{externalInvoiceId}.
 */
export interface GetInvoiceEditorDetailResponse {
  invoice: InvoiceEditorDto
}

export type InvoiceEditorAutofillLookupStatus =
  | 'Resolved'
  | 'Unresolved'
  | 'ResolvedButMissingSection1'

export interface InvoiceEditorAutofillLookupItem {
  canonicalJobNumber: string
  orderId?: string
  purchaseOrder: string
  productDetails: string
  description: string
  status: InvoiceEditorAutofillLookupStatus
  message: string
}

export interface LookupInvoiceEditorAutofillResponse {
  jobs: InvoiceEditorAutofillLookupItem[]
}

/**
 * A single line item in a create or update invoice editor request.
 */
export interface InvoiceEditorLineItemRequest {
  poNumber: string
  description: string
  qty: number
  unit: string
  unitCost: number
}

/**
 * Request body for creating a new invoice via the editor.
 */
export interface CreateInvoiceRequest {
  externalClientId: string
  invoiceDate?: string
  jobNumber: string
  lineItems: InvoiceEditorLineItemRequest[]
}

/**
 * Request body for updating a draft invoice via the editor.
 */
export interface UpdateInvoiceRequest {
  externalClientId: string
  invoiceDate?: string
  jobNumber: string
  lineItems: InvoiceEditorLineItemRequest[]
}

/**
 * Response from create or update invoice editor operations.
 */
export interface SaveInvoiceResponse {
  billingSummary: InvoiceBillingSummary
}

/**
 * Lists Invoice Ninja clients for billing screens.
 *
 * @param query Optional search term; returns up to 100 clients when omitted.
 */
export async function listBillingClients(query?: string): Promise<BillingClientOption[]> {
  const params = query ? { query } : {}
  const response = await apiClient.get<ListBillingClientsResponse>('/api/v2/billing/clients', { params })
  return response.data.clients
}

/**
 * Retrieves a normalized invoice editor DTO for editing or read-only view.
 *
 * @param externalInvoiceId Invoice Ninja invoice ID.
 */
export async function getInvoiceEditorDetail(externalInvoiceId: string): Promise<InvoiceEditorDto> {
  const response = await apiClient.get<GetInvoiceEditorDetailResponse>(
    `/api/v2/billing/invoices/${externalInvoiceId}`,
  )
  return response.data.invoice
}

export async function lookupInvoiceEditorAutofill(
  canonicalJobNumbers: string[],
): Promise<InvoiceEditorAutofillLookupItem[]> {
  const response = await apiClient.post<LookupInvoiceEditorAutofillResponse>(
    '/api/v2/billing/invoices/autofill-lookup',
    { canonicalJobNumbers },
  )
  return response.data.jobs
}

/**
 * Creates a new invoice in Invoice Ninja from the editor form.
 *
 * @param request Editor form payload.
 */
export async function createInvoice(request: CreateInvoiceRequest): Promise<InvoiceBillingSummary> {
  const response = await apiClient.post<SaveInvoiceResponse>('/api/v2/billing/invoices', request)
  return response.data.billingSummary
}

/**
 * Updates a draft invoice in Invoice Ninja from the editor form.
 *
 * @param externalInvoiceId Invoice Ninja invoice ID.
 * @param request Editor form payload.
 */
export async function updateInvoice(
  externalInvoiceId: string,
  request: UpdateInvoiceRequest,
): Promise<InvoiceBillingSummary> {
  const response = await apiClient.put<SaveInvoiceResponse>(
    `/api/v2/billing/invoices/${externalInvoiceId}`,
    request,
  )
  return response.data.billingSummary
}

