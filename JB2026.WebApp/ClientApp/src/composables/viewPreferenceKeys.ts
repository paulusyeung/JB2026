export const OBJECT_TYPE_VIEW_SETTINGS = 1

// Each view gets a fixed ObjectId GUID so server records remain stable across releases.
const VIEW_OBJECT_IDS: Record<string, string> = {
  stock: '4e86c95f-1db7-45b4-a3c1-d82c49648d0f',
}

export function getViewObjectId(viewId: string): string | null {
  return VIEW_OBJECT_IDS[viewId] ?? null
}
