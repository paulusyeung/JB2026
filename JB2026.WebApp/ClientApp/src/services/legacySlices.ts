import { getFeatureFlags } from '@/services/featureFlags'
import type { LegacySliceActionPlan, LegacySliceCatalogItem, LegacySliceReadinessSummary, LegacySliceRouteStatus, LegacySliceViewModel } from '@/types/api'

const legacySliceCatalog: LegacySliceCatalogItem[] = [
  {
    key: 'job-order',
    displayName: 'Job Order',
    modernPath: '/job-order',
    legacyFolder: 'JobOrder',
    sampleRoutes: [
      { path: '/JobOrder/JobStatsPage.aspx', description: 'Job stats dashboard (legacy WebForms)' },
      { path: '/JobOrder/OrderList_MasterDetailPage.aspx', description: 'Master-detail order list' },
    ],
  },
  {
    key: 'sml',
    displayName: 'SML',
    modernPath: '/sml',
    legacyFolder: 'SML',
    sampleRoutes: [
      { path: '/SML/Stats/InvoiceStatsPage.aspx', description: 'Invoice statistics page' },
      { path: '/SML/Stats/RtfStatsPage.aspx', description: 'RTF statistics page' },
    ],
  },
  {
    key: 'stock',
    displayName: 'Stock',
    modernPath: '/stock',
    legacyFolder: 'Stock',
    sampleRoutes: [
      { path: '/Stock', description: 'Stock legacy module root' },
      { path: '/Stock/Product', description: 'Product area entry' },
    ],
  },
  {
    key: 'reports',
    displayName: 'Reports',
    modernPath: '/reports',
    legacyFolder: 'Reports',
    sampleRoutes: [
      { path: '/Reports', description: 'Legacy reporting root' },
    ],
  },
  {
    key: 'admin',
    displayName: 'Admin',
    modernPath: '/admin',
    legacyFolder: 'Admin',
    sampleRoutes: [
      { path: '/Admin', description: 'Administrative module root' },
    ],
  },
  {
    key: 'public',
    displayName: 'Public',
    modernPath: '/public',
    legacyFolder: 'Public',
    sampleRoutes: [
      { path: '/Public', description: 'Public-facing module root' },
    ],
  },
  {
    key: 'settings',
    displayName: 'Settings',
    modernPath: '/settings',
    legacyFolder: 'Settings',
    sampleRoutes: [
      { path: '/Settings', description: 'Settings module root' },
    ],
  },
  {
    key: 'help',
    displayName: 'Help',
    modernPath: '/help',
    legacyFolder: 'Help',
    sampleRoutes: [
      { path: '/Help', description: 'Legacy help and user guidance' },
    ],
  },
]

export function getLegacySliceCatalog(): LegacySliceCatalogItem[] {
  return legacySliceCatalog
}

export async function getLegacySliceViewModels(): Promise<LegacySliceViewModel[]> {
  try {
    return await getLegacySliceViewModelsFromApi()
  } catch {
    const flags = await getFeatureFlags()
    const flagLookup = new Map(flags.map((flag) => [flag.key, flag]))

    return legacySliceCatalog.map((slice) => {
      const flag = flagLookup.get(slice.key)

      return {
        ...slice,
        enabled: flag?.enabled ?? false,
        prefixes: flag?.prefixes ?? [],
      }
    })
  }
}

async function getLegacySliceViewModelsFromApi(): Promise<LegacySliceViewModel[]> {
  const response = await fetch('/ui/legacy-slices', {
    headers: {
      Accept: 'application/json',
    },
  })

  if (!response.ok) {
    throw new Error(`Failed to load legacy slices: ${response.status}`)
  }

  return response.json() as Promise<LegacySliceViewModel[]>
}

export async function getLegacySliceRouteStatus(sliceKey: string): Promise<LegacySliceRouteStatus> {
  const response = await fetch(`/ui/legacy-slices/${encodeURIComponent(sliceKey)}/status`, {
    headers: {
      Accept: 'application/json',
    },
  })

  if (!response.ok) {
    throw new Error(`Failed to load legacy slice route status: ${response.status}`)
  }

  return response.json() as Promise<LegacySliceRouteStatus>
}

export async function getLegacySliceReadinessSummary(sliceKey: string): Promise<LegacySliceReadinessSummary> {
  const response = await fetch(`/ui/legacy-slices/${encodeURIComponent(sliceKey)}/readiness`, {
    headers: {
      Accept: 'application/json',
    },
  })

  if (!response.ok) {
    throw new Error(`Failed to load legacy slice readiness summary: ${response.status}`)
  }

  return response.json() as Promise<LegacySliceReadinessSummary>
}

export async function getLegacySliceActionPlan(sliceKey: string): Promise<LegacySliceActionPlan> {
  const response = await fetch(`/ui/legacy-slices/${encodeURIComponent(sliceKey)}/action-plan`, {
    headers: {
      Accept: 'application/json',
    },
  })

  if (!response.ok) {
    throw new Error(`Failed to load legacy slice action plan: ${response.status}`)
  }

  return response.json() as Promise<LegacySliceActionPlan>
}
