export const OBJECT_TYPE_VIEW_SETTINGS = 1

// Each view gets a fixed ObjectId GUID so server records remain stable across releases.
const VIEW_OBJECT_IDS: Record<string, string> = {
  joblist: '9f3d0ad6-b8b2-42f8-98cd-311ef7e7b328',
  orderlist: 'c5e7a2d1-4f6b-47e9-8a3c-2b1d9f8e6c5a',
  stock: '4e86c95f-1db7-45b4-a3c1-d82c49648d0f',
  smlrtflist: 'b7c4f8a9-3e5d-4b2c-9f1e-7d6c8a3b5e2f',
  'crm-companies': 'a1b2c3d4-5e6f-7a8b-9c0d-1e2f3a4b5c6d',
  'crm-people': 'f1e2d3c4-b5a6-47c8-9d0e-1f2a3b4c5d6e',
  'crm-opportunities': 'dab2eaf7-4eec-455f-931c-5189f6f892ed',
  'crm-tasks': '93bce14b-2c12-49c1-93cc-32d10e1af4ee',
  'staff-members': 'c77772fa-9a40-4f85-9fc9-f7674ca47803',
  'admin-user': '74673387-9bc7-4775-a579-b90506a15d96',
  'admin-customer': '2b947331-9176-491e-aeed-828c7298bb3e',
  'admin-supplier': '3d5d0bb1-a74d-49c5-be44-7c027ee086cc',
  'billing-clients': 'bf449ad8-d005-4a74-a960-ce646a3c1f9d',
  'billing-invoices': 'aeabd90c-256c-4c9d-b21e-b2bb1511704a',
  'billing-statement': 'b761632b-a1e8-4e30-983a-9655fbb5f225',
  'pending-schedule': '3116c65a-dac5-472c-a8f8-07d4569ec40e',
  'completed-schedule': '3262c4ba-da7f-4442-aa70-461a6d0401df',
  'packing-schedule': '21523084-0ae5-478f-8e55-2adb5323e9d5',
  'exceptional-report': '2aa58ae4-0605-4c7b-896e-a17924e2b0a7',
  'crm-customer360-job-orders': 'a9cf4fde-1153-460e-bdff-c579216d8c42',
  'crm-customer360-invoices': '20e25c59-fd5c-4561-845b-ff420aab9b10',
  'crm-customer360-opportunities': '1609fa4f-f55b-4a4e-858a-517672bfcd3b',
  'crm-customer360-tasks': 'f21bb8dc-14bd-49d0-908d-9786cd096327',
  'crm-customer360-files': '4efef5ec-5e46-47cd-8533-b18a7537511c',
  'crm-customer360-emails': 'b1f73a2e-cc8f-420e-9c40-354c59598b0a',
}

export function getViewObjectId(viewId: string): string | null {
  return VIEW_OBJECT_IDS[viewId] ?? null
}
