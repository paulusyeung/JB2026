import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { getOrderList } from '@/services/jobOrders'
import type { JobOrderRecord } from '@/types/api'

export const useOrdersStore = defineStore('orders', () => {
  const rows = ref<JobOrderRecord[]>([])
  const loading = ref(false)
  const filter = ref('')
  
  const rowCount = computed(() => rows.value.length)

  async function load(params: { 
    lookup?: string; 
    commonQuery?: number; 
    startsWith?: string; 
    take?: number;
    startOn?: string;
    endOn?: string;
  } = {}) {
    loading.value = true
    try {
      rows.value = await getOrderList(params)
    } finally {
      loading.value = false
    }
  }

  return {
    rows,
    loading,
    filter,
    rowCount,
    load
  }
})
