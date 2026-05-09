export const OBJECT_TYPE_VIEW_SETTINGS = 1

// Each view gets a fixed ObjectId GUID so server records remain stable across releases.
const VIEW_OBJECT_IDS: Record<string, string> = {
  joblist: '9f3d0ad6-b8b2-42f8-98cd-311ef7e7b328',
  orderlist: 'c5e7a2d1-4f6b-47e9-8a3c-2b1d9f8e6c5a',
  stock: '4e86c95f-1db7-45b4-a3c1-d82c49648d0f',
  smlrtflist: 'b7c4f8a9-3e5d-4b2c-9f1e-7d6c8a3b5e2f',
}

export function getViewObjectId(viewId: string): string | null {
  return VIEW_OBJECT_IDS[viewId] ?? null
}
