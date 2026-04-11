import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { getQuotations, searchQuotations } from '@/services/quotations'
import type { QuotationListItem } from '@/types/api'

export const useQuotationsStore = defineStore('quotations', () => {
  const rows = ref<QuotationListItem[]>([])
  const loading = ref(false)
  const keyword = ref('')
  const page = ref(1)
  const itemsPerPage = ref(10)
  const sortBy = ref<Array<{ key: string, order: 'asc' | 'desc' }>>([{ key: 'quotedOn', order: 'desc' }])

  const rowCount = computed(() => rows.value.length)

  async function load(date = new Date()) {
    loading.value = true
    try {
      const startOn = date.toISOString().slice(0, 10)
      rows.value = await getQuotations({ startOn, days: 30 })
    } finally {
      loading.value = false
    }
  }

  async function search() {
    const trimmed = keyword.value.trim()
    if (trimmed.length < 3) {
      return
    }

    loading.value = true
    try {
      rows.value = await searchQuotations(trimmed)
    } finally {
      loading.value = false
    }
  }

  return {
    rows,
    loading,
    keyword,
    page,
    itemsPerPage,
    sortBy,
    rowCount,
    load,
    search,
  }
})