import { computed, type ComputedRef, type Ref } from 'vue'

export interface ColumnOrderHeader {
  key: PropertyKey
}

/**
 * Composable that resolves the display order of a view's table columns from a persisted order.
 *
 * The persisted order is the full list of column keys the view offers, including hidden
 * columns, so hiding and re-showing a column restores the position the user gave it. Stored
 * keys the view no longer offers are ignored, and columns missing from the stored order are
 * appended in the view's default order.
 *
 * @param columnOrder - Ref holding the persisted column keys, from `useViewSettings`.
 * @param allHeaders - The view's full header list, in default order.
 * @param defaultKeys - The view's default column keys, used by `resetColumnOrder`.
 * @returns The ordered headers, the ordered keys, and move/reset actions.
 */
export function useColumnOrder<T extends ColumnOrderHeader>(
  columnOrder: Ref<string[]>,
  allHeaders: Ref<readonly T[]> | ComputedRef<readonly T[]>,
  defaultKeys: readonly string[],
) {
  const orderedHeaders = computed<T[]>(() => {
    const position = new Map<string, number>()
    columnOrder.value.forEach((key, index) => position.set(key, index))

    return [...allHeaders.value].sort((lhs, rhs) => {
      const leftPosition = position.get(String(lhs.key)) ?? Number.MAX_SAFE_INTEGER
      const rightPosition = position.get(String(rhs.key)) ?? Number.MAX_SAFE_INTEGER
      if (leftPosition !== rightPosition) return leftPosition - rightPosition
      return allHeaders.value.indexOf(lhs) - allHeaders.value.indexOf(rhs)
    })
  })

  const orderedColumnKeys = computed(() => orderedHeaders.value.map((header) => String(header.key)))

  function moveColumn(sourceKey: string, targetKey: string) {
    if (sourceKey === targetKey) return

    const order = [...orderedColumnKeys.value]
    const fromIndex = order.indexOf(sourceKey)
    const targetIndex = order.indexOf(targetKey)
    if (fromIndex < 0 || targetIndex < 0) return

    order.splice(targetIndex, 0, ...order.splice(fromIndex, 1))
    columnOrder.value = order
  }

  function resetColumnOrder() {
    columnOrder.value = [...defaultKeys]
  }

  return { orderedHeaders, orderedColumnKeys, moveColumn, resetColumnOrder }
}
