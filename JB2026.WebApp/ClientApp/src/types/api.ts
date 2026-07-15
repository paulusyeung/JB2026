export interface UserProfile {
  userId: string
  username: string
  displayName: string
  role: string
}

export interface CrmCompany {
  id: string
  name: string
  accountOwner: string
  domainName: string
  address: string
  createdOn: string
  createdBy: string
  updatedOn: string
  updatedBy: string
  people: string
  opportunities: string
}

export interface AdminUser {
  userId: string
  username: string
  displayName: string
  role: string
  primaryRec: boolean
  userAlias: string
  userPassword: string
  email: string
  crmSynced: boolean
  createdOn: string
  createdBy: string
  modifiedOn: string
  modifiedBy: string
}

export interface AdminUserRecord {
  userId: string
  username: string
  userAlias: string
  userPassword: string
  userRole: number
  role: string
  primaryRec: boolean
  email: string
  createdOn: string
  createdBy: string
  modifiedOn: string
  modifiedBy: string
}

export interface CreateAdminUserRequest {
  username: string
  userAlias: string
  userPassword: string
  userRole: number
  email: string
}

export interface UpdateAdminUserRequest {
  username: string
  userAlias: string
  userPassword: string
  userRole: number
  email: string
}

export interface AdminWorkflowListItem {
  workflowId: string
  workflowName: string
  workTitle: string
  workInstruction: string
}

export interface AdminWorkflowRecord {
  workflowId: string
  workflowName: string
  workTitle: string
  workInstruction: string
}

export interface CreateAdminWorkflowRequest {
  workflowName: string
  workTitle: string
  workInstruction: string
}

export interface UpdateAdminWorkflowRequest {
  workflowName: string
  workTitle: string
  workInstruction: string
}

export interface AdminWorkflowAssignedFormItem {
  workflowFormId: string
  formId: string
  seqNumber: number
  formName: string
  formNameChs: string
  formNameCht: string
  metadataXml: string | null
}

export interface UpdateAdminWorkflowFormsRequest {
  formIds: string[]
}

export interface AdminWorkflowFormListItem {
  formId: string
  formName: string
  formNameChs: string
  formNameCht: string
}

export interface AdminWorkflowFormRecord {
  formId: string
  formObjectEnum: number
  formName: string
  formNameChs: string
  formNameCht: string
  metadataXml: string | null
}

export interface CreateAdminWorkflowFormRequest {
  formName: string
  formNameChs: string
  formNameCht: string
}

export interface UpdateAdminWorkflowFormRequest {
  formName: string
  formNameChs: string
  formNameCht: string
  metadataXml: string | null
}

export interface AdminCustomerListItem {
  customerId: string
  customerName: string
  loginAccount: string
  loginPassword: string
  customerCode: string
  invoiceNinjaClientId: string
  billingSyncStatus: string
  createdOn: string
  createdBy: string
  modifiedOn: string
  modifiedBy: string
}

export interface CustomerShipToAddress {
  name: string
  address: string
}

export interface AdminCustomerRecord {
  customerId: string
  customerName: string
  loginAccount: string
  loginPassword: string
  customerCode: string
  billTo: string
  group: string
  shipToAddresses: CustomerShipToAddress[]
  createdOn: string
  createdBy: string
  modifiedOn: string
  modifiedBy: string
}

export interface CreateAdminCustomerRequest {
  customerName: string
  loginAccount: string
  loginPassword: string
  customerCode: string
  billTo: string
  group: string
  shipToAddresses: CustomerShipToAddress[]
}

export interface UpdateAdminCustomerRequest {
  customerName: string
  loginAccount: string
  loginPassword: string
  customerCode: string
  billTo: string
  group: string
  shipToAddresses: CustomerShipToAddress[]
}

export interface MergeAdminCustomersRequest {
  targetCustomerId: string
  customerIds: string[]
}

export interface AdminSupplierListItem {
  supplierId: string
  supplierName: string
  loginAccount: string
  loginPassword: string
  supplierCode: string
  createdOn: string
  createdBy: string
  modifiedOn: string
  modifiedBy: string
}

export interface SupplierShipToAddress {
  name: string
  address: string
}

export interface AdminSupplierRecord {
  supplierId: string
  supplierName: string
  loginAccount: string
  loginPassword: string
  supplierCode: string
  billTo: string
  shipToAddresses: SupplierShipToAddress[]
  createdOn: string
  createdBy: string
  modifiedOn: string
  modifiedBy: string
}

export interface CreateAdminSupplierRequest {
  supplierName: string
  loginAccount: string
  loginPassword: string
  supplierCode: string
  billTo: string
  shipToAddresses: SupplierShipToAddress[]
}

export interface UpdateAdminSupplierRequest {
  supplierName: string
  loginAccount: string
  loginPassword: string
  supplierCode: string
  billTo: string
  shipToAddresses: SupplierShipToAddress[]
}

export interface AdminQuotationItemListItem {
  itemId: string
  itemGroupId: string
  itemGroupZone: string
  zone: string
  groupNameEn: string
  groupNameCht: string
  groupNameChs: string
  itemIndex: number
  itemNameEn: string
  itemNameCht: string
  itemNameChs: string
  mandatory: boolean
  fixed: boolean
  unitCost: number
  minimum: string
  unitCostType: number
  costRounding: number
  createdOn: string
  createdBy: string
  modifiedOn: string
  modifiedBy: string
}

export interface AdminQuotationItemGroupListItem {
  itemGroupId: string
  zone: string
  groupNameEn: string
  groupNameCht: string
  groupNameChs: string
  createdOn: string
  createdBy: string
  modifiedOn: string
  modifiedBy: string
}


export interface CreateAdminQuotationItemGroupRequest {
  zone: string
  groupNameEn: string
  groupNameCht: string
  groupNameChs: string
}

export interface UpdateAdminQuotationItemGroupRequest {
  zone: string
  groupNameEn: string
  groupNameCht: string
  groupNameChs: string
}

export interface CreateAdminQuotationItemRequest {
  itemGroupId: string
  itemIndex: number
  itemNameEn: string
  itemNameCht: string
  itemNameChs: string
  mandatory: boolean
  fixed: boolean
  unitCost: number
  unitCostType: number
  minimum: string
  costRounding: number
}

export interface UpdateAdminQuotationItemRequest {
  itemGroupId: string
  itemIndex: number
  itemNameEn: string
  itemNameCht: string
  itemNameChs: string
  mandatory: boolean
  fixed: boolean
  unitCost: number
  unitCostType: number
  minimum: string
  costRounding: number
}

export interface AdminOrderTypeWorkflowItem {
  workflowId: string
  workflowName: string
}

export interface AdminOrderTypeWorkflowPayload {
  availableWorkflows: AdminOrderTypeWorkflowItem[]
  selectedWorkflows: AdminOrderTypeWorkflowItem[]
}

export interface UpdateAdminOrderTypeWorkflowsRequest {
  orderType: number
  workflowIds: string[]
}

export interface TokenResponse {
  accessToken: string
  expiresAtUtc: string
  tokenType: string
  user: UserProfile
  refreshToken?: string
}

export interface JobListItem {
  orderId: string
  orderNumber: string
  customerName: string
  customerRef: string
  orderTitle: string
  orderedBy: string
  orderedOn: string
  requiredOn: string
  qty: number
  status: number
}

export interface JobAttachment {
  attachmentId: string
  fileName: string
  fileExtension: string
  attachmentType: string
  uploadedBy: string
  uploadedOn: string
}

export interface JobDetail extends JobListItem {
  paymentTerms: string
  remarks: string
  productDetails: string
  productStyle: string
  productCode: string
  outputRef: string
  invoiceRef: string
  invoiceAmount: number
  styleTitles: string[]
  attachments: JobAttachment[]
  soNumber?: string
  originalSONumber?: string
  workflowAttributes?: Record<string, string>
}

export interface JobOrderPrintRequest {
  layout: string
  noPicture: boolean
  noProductDetails: boolean
  selectedWorkflowIndices: number[]
}

export interface QuotationListItem {
  headerId: string
  machineType: string
  quoteNumber: number
  quoteNumberIndex: number
  quoteNumberIndexPair: string
  quotedOn: string
  quotedBy: string
  approvedOn: string | null
  approvedBy: string | null
  printTitle: string
  customerName: string
  printsSize: string
  printsColor: string
  printsQty: number
  materialName: string
  materialCost: number
  totalCostA: number
  unitCostA: number
  status: number
  createdOn: string
  createdBy: string
  modifiedOn: string
  modifiedBy: string
}

export interface StockProductListItem {
  productId: string
  stockNumber: string
  productCode: string
  productName: string
  balance: number
  sellingPrice: number
  cogs: number
  remarks: string
  attachmentCount: number
  createdOn: string
  createdBy: string
  modifiedOn: string
  modifiedBy: string
}

export interface StockProductRecord {
  productId: string
  customerCode: string
  categoryCode: string
  sequenceNumber: string
  stockNumber: string
  productCode: string
  productName: string
  productionInfo: string
  remarks: string
  sellingPrice: number
  cogs: number
  balance: number
  createdOn: string
  createdBy: string
  modifiedOn: string
  modifiedBy: string
}

export interface StockProductRecordUpsertRequest {
  customerCode: string
  categoryCode: string
  sequenceNumber: string
  productCode: string
  productName: string
  productionInfo?: string
  remarks?: string
  sellingPrice: number
  cogs: number
}

export interface StockProductMovementHistoryItem {
  inOutId: string
  inOutDate: string
  reference: string
  qty: number
  runningBalance: number
  modifiedOn: string
  modifiedBy: string
}

export interface StockProductCodeValidationResponse {
  isUnique: boolean
}

export interface StockProductNextNumberResponse {
  customerCode: string
  categoryCode: string
  sequenceNumber: string
  stockNumber: string
}

export interface StockInOutTransactionRequest {
  inOutDate: string
  reference?: string
  qty: number
}

export interface StockInOutTransactionResult {
  inOutId: string
  productId: string
  newBalance: number
}

export interface StockProductDeleteResult {
  productId: string
  outcome: 'retired' | 'hardDeleted'
}

export interface StockProductAttachment {
  attachmentId: string
  productId: string
  attachmentIndex: number
  fileName: string
  fileExtension: string
  fileSizeBytes: number
  existsOnDisk: boolean
}

export interface StockProductAttachmentDeleteRequest {
  attachmentIds: string[]
}

export interface StockProductAttachmentDeleteResult {
  productId: string
  requestedCount: number
  deletedCount: number
}

export interface SmlMonthlyStat {
  year: number
  month: number
  count: number
  amount: number
}

export interface SmlTopCustomerStat {
  customerName: string
  count: number
  amount: number
}

export interface SmlStatsResponse {
  generatedAtUtc: string
  rowCount: number
  totalAmount: number
  monthly: SmlMonthlyStat[]
  topCustomers: SmlTopCustomerStat[]
}

export interface SmlRtfListItem {
  lineNumber: number
  productCode: string
  productDescription: string
  price: string
  qty: string
  amount: string
}

export interface SmlRtfListHeader {
  headerId: string
  rtfFileName: string
  purchaseOrder: string
  rowNumber: number
  customerPO: string
  orderedBy: string
  orderedOn: string
  originalPO: string
  salesOrder: string
  originalSO: string
  dnCount: number
  invoiceCount: number
  invoiceNumber: string
  isLabelPrinted: boolean
  createdOn: string
  createdBy: string
  items: SmlRtfListItem[]
}

export interface SmlRtfListResponse {
  generatedAtUtc: string
  rowCount: number
  headers: SmlRtfListHeader[]
}

export interface SmlRtfStatsRow {
  purchaseOrder: string
  customerPO: string
  orderedOn: string
  orderedBy: string
  originalPO: string
  salesOrder: string
  originalSO: string
  productCode: string
  price: string
  qty: string
  year: number
  month: number
  amount: number
}

export interface SmlRtfStatsResponse {
  generatedAtUtc: string
  rowCount: number
  rows: SmlRtfStatsRow[]
}

export interface SmlInvoiceListItem {
  lineNumber: number
  description: string
  quantity: number
  unit: string
  price: number
  amount: number
}

export interface SmlInvoiceListRow {
  headerId: string
  invoiceNumber: string
  rowNumber: number
  customerName: string
  invoiceDate: string
  invoiceAmount: number
  icNumber: string
  createdOn: string
  createdBy: string
  items: SmlInvoiceListItem[]
}

export interface SmlInvoiceListResponse {
  generatedAtUtc: string
  rowCount: number
  rows: SmlInvoiceListRow[]
}

export interface SmlInvoiceStatsRow {
  customerName: string
  invoiceNumber: string
  invoiceDate: string | null
  invoiceAmount: number
  createdOn: string | null
  createdBy: string
  purchaseOrder: string
  productCode: string
  qty: number
  unit: string
  price: number
  amount: number
  year: number
  month: number
}

export interface SmlInvoiceStatsResponse {
  generatedAtUtc: string
  rowCount: number
  rows: SmlInvoiceStatsRow[]
}

export interface AppSettings {
  companyName: string
  timeZone: string
  currencyCode: string
  enableLegacyFallback: boolean
  ownerName: string
  nextOrderNumber: string
  nextProductNumber: string
  nextQuotationNumber: string
  commonQueryIndex: number
  completedQueryIndex: number
  scheduleQueryRange: number
  gmailAccount: string
  gmailPassword: string
  dateFormatPreference: string
}

export interface PublicContentItem {
  slug: string
  title: string
  summary: string
  urlPath: string
}

export interface HelpArticle {
  articleId: string
  title: string
  category: string
  content: string
}

export interface UiFeatureFlag {
  key: string
  displayName: string
  enabled: boolean
  prefixes: string[]
}

export interface JobScheduleCalendarItem {
  scheduleId: string
  orderId: string
  title: string
  startOn: string
  endOn: string | null
  status: number | null
  priority: number | null
  machineNumber: string | null
}

export interface JobSchedulePendingItem {
  orderId: string
  orderType: number
  orderNumber: string
  customerName: string
  orderTitle: string
  status: number
  orderedOn: string | null
  requiredOn: string | null
  urgencyLevel: number
  step1Status: number | null
  step2Status: number | null
  step3Status: number | null
}

export interface UpdatePendingWorkflowRequest {
  stepIndex: number
  targetStatus: number
}

export interface UpdatePendingUrgencyRequest {
  targetColor: 'red' | 'yellow'
}

export interface PendingWorkflowUpdateResponse {
  orderId: string
  step1Status: number | null
  step2Status: number | null
  step3Status: number | null
}

export interface PendingUrgencyUpdateResponse {
  orderId: string
  urgencyLevel: number
}

export interface JobSchedulePackingItem {
  orderId: string
  orderType: number
  orderNumber: string
  customerName: string
  orderTitle: string
  status: number
  orderedOn: string | null
  requiredOn: string | null
  step1Status: number | null
  step2Status: number | null
  step3Status: number | null
  remarks: string
}

export interface JobScheduleCompletedItem {
  orderId: string
  orderType: number
  orderNumber: string
  customerName: string
  orderTitle: string
  status: number
  machineNumber: string
  orderedOn: string | null
  requiredOn: string | null
  scheduledOn: string | null
  completedOn: string | null
}

export interface UpdateJobScheduleTimeRequest {
  startOn: string
  endOn: string | null
}

/** Shape used by the create/edit job order form (Slice B DevExpress replacement). */
export interface JobScheduleAvailableItem {
  orderId: string
  orderType: number
  orderNumber: string
  customerName: string
  orderTitle: string
  requiredOn: string | null
}

export interface JobPackingOnAirAvailableItem {
  orderId: string
  orderType: number
  orderNumber: string
  customerName: string
  orderTitle: string
  remarks: string
}

export interface JobPackingOnAirItem {
  onAirId: string
  orderId: string
  orderType: number
  orderNumber: string
  customerName: string
  orderTitle: string
  priority: number
  remarks: string
}

export interface JobScheduleOnAirItem {
  scheduleId: string
  orderId: string
  orderType: number
  orderNumber: string
  customerName: string
  orderTitle: string
  priority: number
  machineNumber: string
  urgencyLevel: number
  step1Status: number | null
  step2Status: number | null
  printQty: string
  printColor: string
  printSize: string
  soNumber?: string
  requiredOn: string | null
}

export interface SaveScheduleBatchItem {
  orderId: string
  machineNumber: string
  step1Status: number
  step2Status: number
  urgencyLevel: number
}

export interface SaveScheduleBatchRequest {
  orderType: number
  scheduledItems: SaveScheduleBatchItem[]
  cancelledOrderIds: string[]
}

export interface SavePackingOnAirBatchItem {
  orderId: string
}

export interface SavePackingOnAirBatchRequest {
  orderType: number
  selectedItems: SavePackingOnAirBatchItem[]
  cancelledOrderIds: string[]
}

export interface CompletePackingOnAirRequest {
  orderIds: string[]
}

export interface RescheduleCompletedSchedulesRequest {
  orderIds: string[]
}

export interface OrderTypeWorkflowAttribute {
  workIndex: number
  workflowName: string
  options: string[]
}

export interface JobOrderFormData {
  orderId: string | null
  orderNumber: string
  jobNumber: string
  orderTitle: string
  customerName: string
  customerRef: string
  orderedBy: string
  orderedOn: string
  requiredOn: string
  qty: number
  status: number
  orderType: number
  paymentTerms: string
  remarks: string
  productDetails?: string
  soNumber?: string
  originalSONumber?: string
  productStyle?: string
  productCode?: string
  outputRef?: string
  invoiceRef?: string
  invoiceAmount?: number
  workflowAttributes: Record<string, string>
}

export interface JobOrderRecord {
  orderId: string
  orderType: number
  orderNumber: string
  jobNumber: string
  customerName: string
  customerRef: string
  orderTitle: string
  productCode: string
  productStyle: string
  productDetails: string
  outputRef: string
  invoiceRef: string
  invoiceAmount: number
  attachmentProductCount: number
  attachmentCustomerCount: number
  orderedBy: string
  orderedOn: string
  requiredOn: string
  completedOn: string | null
  qty: number
  paymentTerms: string
  remarks: string
  status: number
  createdBy: string
  createdOn: string
  modifiedBy: string | null
  modifiedOn: string | null
  soNumber?: string
  originalSONumber?: string
}

export interface JobStatsRecord {
  jobNumber: string
  customerName: string
  brand: string
  purchaseOrder: string
  salesRep: string
  grossProfit: number
  cost: number
  invoiceAmount: number
  invNumber: string
  invDate: string | null
  year: number | null
  month: number | null
}

export interface LegacySliceSampleRoute {
  path: string
  description: string
}

export interface LegacySliceCatalogItem {
  key: string
  displayName: string
  modernPath: string
  legacyFolder: string
  sampleRoutes: LegacySliceSampleRoute[]
}

export interface LegacySliceViewModel extends LegacySliceCatalogItem {
  enabled: boolean
  prefixes: string[]
}

export type LegacyRouteHandlingMode = 'spa' | 'legacy-redirect' | 'legacy-placeholder' | 'unmanaged'

export interface LegacySliceSampleRouteStatus {
  path: string
  description: string
  handlingMode: LegacyRouteHandlingMode
  resolvedTargetUrl: string | null
}

export interface LegacySliceRouteStatus {
  key: string
  routes: LegacySliceSampleRouteStatus[]
}

export interface LegacySliceReadinessSummary {
  key: string
  enabled: boolean
  legacyBaseConfigured: boolean
  totalSampleRoutes: number
  spaRoutes: number
  legacyRedirectRoutes: number
  legacyPlaceholderRoutes: number
  unmanagedRoutes: number
  apiDependencies: LegacySliceApiDependency[]
  blockers: string[]
}

export interface LegacySliceApiDependency {
  name: string
  method: string
  route: string
  implemented: boolean
  notes: string
}

export interface LegacySliceActionPlanStep {
  order: number
  title: string
  details: string
}

export interface LegacySliceActionPlan {
  key: string
  generatedAtUtc: string
  steps: LegacySliceActionPlanStep[]
}