import { appMessages } from './app'
import { commonMessages } from './common'
import { routesMessages } from './routes'
import { topbarMessages } from './topbar'
import { sidebarMessages } from './sidebar'
import { authMessages } from './auth'
import { dashboardMessages } from './dashboard'
import { helpMessages } from './help'
import { adminMessages } from './admin'
import { publicContentMessages } from './publicContent'
import { settingsMessages } from './settings'
import { stockMessages } from './stock'
import { smlMessages } from './sml'
import { reportsMessages } from './reports'
import { billingMessages } from './billing'
import { jobsMessages } from './jobs'
import { jobOrderMessages } from './jobOrder'
import { quotationsMessages } from './quotations'
import { schedulerMessages } from './scheduler'
import { editorMessages } from './editor'
import { jobFormMessages } from './jobForm'
import { legacySliceMessages } from './legacySlice'
import { orderTypesMessages } from './orderTypes'
import { themeMessages } from './theme'
import { crmMessages } from './crm'

export const zhHantMessages = {
  app: appMessages,
  common: commonMessages,
  routes: routesMessages,
  topbar: topbarMessages,
  sidebar: sidebarMessages,
  auth: authMessages,
  dashboard: dashboardMessages,
  help: helpMessages,
  admin: adminMessages,
  publicContent: publicContentMessages,
  settings: settingsMessages,
  stock: stockMessages,
  sml: smlMessages,
  reports: reportsMessages,
  billing: billingMessages,
  jobs: jobsMessages,
  jobOrder: jobOrderMessages,
  quotations: quotationsMessages,
  scheduler: schedulerMessages,
  editor: editorMessages,
  jobForm: jobFormMessages,
  legacySlice: legacySliceMessages,
  orderTypes: orderTypesMessages,
  theme: themeMessages,
  crm: crmMessages,
} as const
