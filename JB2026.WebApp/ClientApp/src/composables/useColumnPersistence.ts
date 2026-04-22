import { ref, watch } from 'vue'

const STORAGE_PREFIX = 'view-settings-'

interface ViewSettings {
  visibleColumns: string[]
  sortKey?: string
  sortDirection?: 'asc' | 'desc'
  checkboxMode?: boolean
}

/**
 * Composable to persist view settings (columns, sorting, checkbox mode) to localStorage.
 * 
 * @param viewId - A unique identifier for the view (e.g., 'stock', 'orders').
 * @param defaults - Default settings for the view.
 * @returns An object containing refs for visibleColumns, sortKey, sortDirection, and checkboxMode.
 */
export function useViewSettings(viewId: string, defaults: {
  visibleColumns: string[]
  sortKey?: string
  sortDirection?: 'asc' | 'desc'
  checkboxMode?: boolean
}) {
  const storageKey = `${STORAGE_PREFIX}${viewId}`
  
  const visibleColumns = ref<string[]>([])
  const sortKey = ref<string | undefined>(defaults.sortKey)
  const sortDirection = ref<'asc' | 'desc' | undefined>(defaults.sortDirection)
  const checkboxMode = ref<boolean | undefined>(defaults.checkboxMode)

  function load(): ViewSettings {
    const stored = localStorage.getItem(storageKey)
    if (stored) {
      try {
        const parsed = JSON.parse(stored) as Partial<ViewSettings>
        return {
          visibleColumns: Array.isArray(parsed.visibleColumns) && parsed.visibleColumns.length > 0
            ? parsed.visibleColumns
            : [...defaults.visibleColumns],
          sortKey: parsed.sortKey ?? defaults.sortKey,
          sortDirection: parsed.sortDirection ?? defaults.sortDirection,
          checkboxMode: parsed.checkboxMode ?? defaults.checkboxMode,
        }
      } catch {
        // Ignore parse errors
      }
    }
    return {
      visibleColumns: [...defaults.visibleColumns],
      sortKey: defaults.sortKey,
      sortDirection: defaults.sortDirection,
      checkboxMode: defaults.checkboxMode,
    }
  }

  function save(settings: ViewSettings) {
    localStorage.setItem(storageKey, JSON.stringify(settings))
  }

  // Initialize immediately
  const loaded = load()
  visibleColumns.value = loaded.visibleColumns
  if (loaded.sortKey !== undefined) sortKey.value = loaded.sortKey
  if (loaded.sortDirection !== undefined) sortDirection.value = loaded.sortDirection
  if (loaded.checkboxMode !== undefined) checkboxMode.value = loaded.checkboxMode

  // Watch for changes and save
  watch(
    [visibleColumns, sortKey, sortDirection, checkboxMode],
    () => {
      save({
        visibleColumns: visibleColumns.value,
        sortKey: sortKey.value,
        sortDirection: sortDirection.value,
        checkboxMode: checkboxMode.value,
      })
    },
    { deep: true }
  )

  return { visibleColumns, sortKey, sortDirection, checkboxMode }
}