import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { getFeatureFlags } from '@/services/featureFlags'
import type { UiFeatureFlag } from '@/types/api'

export const useFeatureFlagsStore = defineStore('featureFlags', () => {
  const flags = ref<UiFeatureFlag[]>([])
  const loading = ref(false)

  const enabledCount = computed(() => flags.value.filter((flag) => flag.enabled).length)

  async function load() {
    loading.value = true
    try {
      flags.value = await getFeatureFlags()
    } finally {
      loading.value = false
    }
  }

  return {
    flags,
    loading,
    enabledCount,
    load,
  }
})