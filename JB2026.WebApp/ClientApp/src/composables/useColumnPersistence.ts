import { onMounted, ref, watch } from 'vue'
import { getViewObjectId, OBJECT_TYPE_VIEW_SETTINGS } from '@/composables/viewPreferenceKeys'
import { getUserPreference, saveUserPreference } from '@/services/userPreferences'

const STORAGE_PREFIX = 'view-settings-'

interface CriteriaSettings {
  [id: string]: { enabled: boolean; days: number }
}

interface ViewSettings {
  visibleColumns: string[]
  columnOrder?: string[]
  sortKey?: string
  sortDirection?: 'asc' | 'desc'
  checkboxMode?: boolean
  viewMode?: 'detail' | 'card' | 'table'
  itemsPerPage?: number
  ignoreGuest?: boolean
  criteria?: CriteriaSettings
  startOn?: string
  endOn?: string
}

const SAVE_DEBOUNCE_MS = 500
const DEFAULT_ITEMS_PER_PAGE = 10

/**
 * Composable to persist view settings with a local-first migration path.
 *
 * Migration path: load localStorage immediately for responsive startup, then overlay
 * server-backed preferences if available. Every change still updates localStorage,
 * while server writes are debounced to reduce API chatter.
 *
 * @param viewId - A unique identifier for the view (e.g., 'stock', 'orders').
 * @param defaults - Default settings for the view.
 * @returns An object containing refs for visibleColumns, columnOrder, sortKey, sortDirection, and checkboxMode.
 */
export function useViewSettings(viewId: string, defaults: {
  visibleColumns: string[]
  columnOrder?: string[]
  sortKey?: string
  sortDirection?: 'asc' | 'desc'
  checkboxMode?: boolean
  viewMode?: 'detail' | 'card' | 'table'
  itemsPerPage?: number
  ignoreGuest?: boolean
  criteria?: CriteriaSettings
  startOn?: string
  endOn?: string
}) {
  const storageKey = `${STORAGE_PREFIX}${viewId}`
  const objectId = getViewObjectId(viewId)
  let saveTimer: ReturnType<typeof setTimeout> | null = null

  const visibleColumns = ref<string[]>([])
  const columnOrder = ref<string[]>([])
  const sortKey = ref<string | undefined>(defaults.sortKey)
  const sortDirection = ref<'asc' | 'desc' | undefined>(defaults.sortDirection)
  const checkboxMode = ref<boolean | undefined>(defaults.checkboxMode)
  const viewMode = ref<'detail' | 'card' | 'table' | undefined>(defaults.viewMode)
  const itemsPerPage = ref<number | undefined>(defaults.itemsPerPage ?? DEFAULT_ITEMS_PER_PAGE)
  const ignoreGuest = ref<boolean | undefined>(defaults.ignoreGuest)
  const criteria = ref<CriteriaSettings | undefined>(defaults.criteria)
  const startOn = ref<string | undefined>(defaults.startOn)
  const endOn = ref<string | undefined>(defaults.endOn)

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
        columnOrder: Array.isArray(parsed.columnOrder) && parsed.columnOrder.length > 0
          ? parsed.columnOrder
          : [...(defaults.columnOrder ?? defaults.visibleColumns)],
        sortKey: parsed.sortKey ?? defaults.sortKey,
        sortDirection: parsed.sortDirection ?? defaults.sortDirection,
        checkboxMode: parsed.checkboxMode ?? defaults.checkboxMode,
        viewMode: parsed.viewMode ?? defaults.viewMode,
        itemsPerPage: parsed.itemsPerPage ?? defaults.itemsPerPage ?? DEFAULT_ITEMS_PER_PAGE,
        ignoreGuest: parsed.ignoreGuest ?? defaults.ignoreGuest,
        criteria: parsed.criteria ?? defaults.criteria,
        startOn: parsed.startOn ?? defaults.startOn,
        endOn: parsed.endOn ?? defaults.endOn,
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
      columnOrder: [...(defaults.columnOrder ?? defaults.visibleColumns)],
      sortKey: defaults.sortKey,
      sortDirection: defaults.sortDirection,
      checkboxMode: defaults.checkboxMode,
      viewMode: defaults.viewMode,
      itemsPerPage: defaults.itemsPerPage ?? DEFAULT_ITEMS_PER_PAGE,
      ignoreGuest: defaults.ignoreGuest,
      criteria: defaults.criteria,
      startOn: defaults.startOn,
      endOn: defaults.endOn,
    }
  }

  function saveToLocalStorage(settings: ViewSettings) {
    localStorage.setItem(storageKey, JSON.stringify(settings))
  }

  function applySettings(settings: ViewSettings) {
    visibleColumns.value = settings.visibleColumns
    columnOrder.value = settings.columnOrder ?? []
    sortKey.value = settings.sortKey
    sortDirection.value = settings.sortDirection
    checkboxMode.value = settings.checkboxMode
    viewMode.value = settings.viewMode
    itemsPerPage.value = settings.itemsPerPage
    ignoreGuest.value = settings.ignoreGuest
    criteria.value = settings.criteria
    startOn.value = settings.startOn
    endOn.value = settings.endOn
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

  let resolveReady: () => void = () => {}
  const ready = new Promise<void>((resolve) => {
    resolveReady = resolve
  })

  // Initialize immediately from localStorage for a responsive startup.
  applySettings(loadFromLocalStorage())

  onMounted(async () => {
    await loadFromServerAndOverlay()
    resolveReady()
  })

  // Watch for changes and save
  watch(
    [visibleColumns, columnOrder, sortKey, sortDirection, checkboxMode, viewMode, itemsPerPage, ignoreGuest, criteria, startOn, endOn],
    () => {
      const settings: ViewSettings = {
        visibleColumns: visibleColumns.value,
        columnOrder: columnOrder.value,
        sortKey: sortKey.value,
        sortDirection: sortDirection.value,
        checkboxMode: checkboxMode.value,
        viewMode: viewMode.value,
        itemsPerPage: itemsPerPage.value,
        ignoreGuest: ignoreGuest.value,
        criteria: criteria.value,
        startOn: startOn.value,
        endOn: endOn.value,
      }

      saveToLocalStorage(settings)
      scheduleServerSave(settings)
    },
    { deep: true }
  )

  return { visibleColumns, columnOrder, sortKey, sortDirection, checkboxMode, viewMode, itemsPerPage, ignoreGuest, criteria, startOn, endOn, ready }
}