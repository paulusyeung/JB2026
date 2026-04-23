import { onMounted, ref, watch } from 'vue'
import { getViewObjectId, OBJECT_TYPE_VIEW_SETTINGS } from '@/composables/viewPreferenceKeys'
import { getUserPreference, saveUserPreference } from '@/services/userPreferences'

const STORAGE_PREFIX = 'view-settings-'

interface ViewSettings {
  visibleColumns: string[]
  sortKey?: string
  sortDirection?: 'asc' | 'desc'
  checkboxMode?: boolean
  viewMode?: 'detail' | 'card'
}

const SAVE_DEBOUNCE_MS = 500

/**
 * Composable to persist view settings with a local-first migration path.
 *
 * Migration path: load localStorage immediately for responsive startup, then overlay
 * server-backed preferences if available. Every change still updates localStorage,
 * while server writes are debounced to reduce API chatter.
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
  viewMode?: 'detail' | 'card'
}) {
  const storageKey = `${STORAGE_PREFIX}${viewId}`
  const objectId = getViewObjectId(viewId)
  let saveTimer: ReturnType<typeof setTimeout> | null = null
  
  const visibleColumns = ref<string[]>([])
  const sortKey = ref<string | undefined>(defaults.sortKey)
  const sortDirection = ref<'asc' | 'desc' | undefined>(defaults.sortDirection)
  const checkboxMode = ref<boolean | undefined>(defaults.checkboxMode)
  const viewMode = ref<'detail' | 'card' | undefined>(defaults.viewMode)

  function parseSettings(raw: string | null): ViewSettings | null {
    if (!raw) {
      return null
    }

    try {
      const parsed = JSON.parse(raw) as Partial<ViewSettings>
      return {
        visibleColumns: Array.isArray(parsed.visibleColumns) && parsed.visibleColumns.length > 0
          ? parsed.visibleColumns
          : [...defaults.visibleColumns],
        sortKey: parsed.sortKey ?? defaults.sortKey,
        sortDirection: parsed.sortDirection ?? defaults.sortDirection,
        checkboxMode: parsed.checkboxMode ?? defaults.checkboxMode,
        viewMode: parsed.viewMode ?? defaults.viewMode,
      }
    } catch {
      return null
    }
  }

  function loadFromLocalStorage(): ViewSettings {
    const stored = localStorage.getItem(storageKey)
    const parsed = parseSettings(stored)
    if (parsed) {
      return parsed
    }

    return {
      visibleColumns: [...defaults.visibleColumns],
      sortKey: defaults.sortKey,
      sortDirection: defaults.sortDirection,
      checkboxMode: defaults.checkboxMode,
      viewMode: defaults.viewMode,
    }
  }

  function saveToLocalStorage(settings: ViewSettings) {
    localStorage.setItem(storageKey, JSON.stringify(settings))
  }

  function applySettings(settings: ViewSettings) {
    visibleColumns.value = settings.visibleColumns
    sortKey.value = settings.sortKey
    sortDirection.value = settings.sortDirection
    checkboxMode.value = settings.checkboxMode
    viewMode.value = settings.viewMode
  }

  async function loadFromServerAndOverlay() {
    if (!objectId) {
      return
    }

    try {
      const response = await getUserPreference(OBJECT_TYPE_VIEW_SETTINGS, objectId)
      const parsed = parseSettings(response.metadata)
      if (!parsed) {
        return
      }

      applySettings(parsed)
      saveToLocalStorage(parsed)
    } catch {
      // localStorage already contains the fallback state.
    }
  }

  function scheduleServerSave(settings: ViewSettings) {
    if (!objectId) {
      return
    }

    if (saveTimer) {
      clearTimeout(saveTimer)
    }

    saveTimer = setTimeout(async () => {
      try {
        await saveUserPreference(OBJECT_TYPE_VIEW_SETTINGS, objectId, {
          metadata: JSON.stringify(settings),
        })
      } catch {
        // localStorage remains the fallback when save fails.
      }
    }, SAVE_DEBOUNCE_MS)
  }

  // Initialize immediately
  applySettings(loadFromLocalStorage())

  onMounted(() => {
    void loadFromServerAndOverlay()
  })

  // Watch for changes and save
  watch(
    [visibleColumns, sortKey, sortDirection, checkboxMode, viewMode],
    () => {
      const settings: ViewSettings = {
        visibleColumns: visibleColumns.value,
        sortKey: sortKey.value,
        sortDirection: sortDirection.value,
        checkboxMode: checkboxMode.value,
        viewMode: viewMode.value,
      }

      saveToLocalStorage(settings)
      scheduleServerSave(settings)
    },
    { deep: true }
  )

  return { visibleColumns, sortKey, sortDirection, checkboxMode, viewMode }
}