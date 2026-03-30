export interface UserProfile {
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

export interface UpdateJobScheduleTimeRequest {
  startOn: string
  endOn: string | null
}

/** Shape used by the create/edit job order form (Slice B DevExpress replacement). */
export interface JobOrderFormData {
  orderId: string | null
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