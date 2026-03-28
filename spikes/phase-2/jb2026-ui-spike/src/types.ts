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
  attachmentType: string
  fileName: string
}

export interface JobDetail extends JobListItem {
  paymentTerms: string
  remarks: string
  styleTitles: string[]
  attachments: JobAttachment[]
}

export interface TokenResponse {
  accessToken: string
  expiresAtUtc: string
}