import { computed, ref } from 'vue'

export function useVirtualScrollThreshold(threshold = 500) {
  const totalRows = ref(0)
  const prefersVirtualScroll = computed(() => totalRows.value > threshold)

  function setRowCount(count: number) {
    totalRows.value = count
  }

  return {
    totalRows,
    prefersVirtualScroll,
    setRowCount,
  }
}