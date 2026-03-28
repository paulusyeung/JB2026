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