import { ref, watch } from 'vue'

const STORAGE_PREFIX = 'view-columns-'

/**
 * Composable to persist column visibility preferences to localStorage.
 * 
 * @param viewId - A unique identifier for the view (e.g., 'stock', 'orders').
 * @param defaultColumns - The default list of column keys to show if none are saved.
 * @returns A ref containing the currently visible column keys.
 */
export function useColumnPersistence(viewId: string, defaultColumns: string[]) {
  const storageKey = `${STORAGE_PREFIX}${viewId}`
  const visibleColumns = ref<string[]>([])

  function load(): string[] {
    const stored = localStorage.getItem(storageKey)
    if (stored) {
      try {
        const parsed = JSON.parse(stored)
        if (Array.isArray(parsed) && parsed.length > 0) {
          return parsed
        }
      } catch {
        // Ignore parse errors
      }
    }
    return [...defaultColumns]
  }

  function save(columns: string[]) {
    localStorage.setItem(storageKey, JSON.stringify(columns))
  }

  // Initialize immediately
  visibleColumns.value = load()

  // Watch for changes and save
  watch(visibleColumns, (newVal) => {
    save(newVal)
  }, { deep: true })

  return visibleColumns
}