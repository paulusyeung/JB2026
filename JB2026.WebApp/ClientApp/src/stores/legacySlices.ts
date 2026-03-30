import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { getLegacySliceViewModels } from '@/services/legacySlices'
import type { LegacySliceViewModel } from '@/types/api'

export const useLegacySlicesStore = defineStore('legacySlices', () => {
  const slices = ref<LegacySliceViewModel[]>([])
  const loading = ref(false)

  const migratedCount = computed(() => slices.value.filter((slice) => slice.enabled).length)

  async function load() {
    loading.value = true
    try {
      slices.value = await getLegacySliceViewModels()
    } finally {
      loading.value = false
    }
  }

  function getByKey(key: string): LegacySliceViewModel | null {
    return slices.value.find((slice) => slice.key === key) ?? null
  }

  return {
    slices,
    loading,
    migratedCount,
    load,
    getByKey,
  }
})
