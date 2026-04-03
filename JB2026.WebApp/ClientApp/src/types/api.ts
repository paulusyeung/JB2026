export interface UserProfile {
  userId: string
  username: string
  displayName: string
  role: string
}

export interface AdminUser {
  userId: string
  username: string
  displayName: string
  role: string
}

export interface TokenResponse {
  accessToken: string
  expiresAtUtc: string
  tokenType: string
  user: UserProfile
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
  fileName: string
  fileExtension: string
  attachmentType: string
  uploadedBy: string
  uploadedOn: string
}

export interface JobDetail extends JobListItem {
  paymentTerms: string
  remarks: string
  styleTitles: string[]
  attachments: JobAttachment[]
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
}

export interface RunReportRequest {
  reportName: string
  startOn: string
  days: number
  take: number
}

export interface ReportRunResponse {
  reportName: string
  generatedAtUtc: string
  totalRows: number
  totalCostA: number
  rows: QuotationListItem[]
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

export interface AppSettings {
  companyName: string
  timeZone: string
  currencyCode: string
  enableLegacyFallback: boolean
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

export interface UpdateJobScheduleTimeRequest {
  startOn: string
  endOn: string | null
}

/** Shape used by the create/edit job order form (Slice B DevExpress replacement). */
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
  paymentTerms: string
  remarks: string
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