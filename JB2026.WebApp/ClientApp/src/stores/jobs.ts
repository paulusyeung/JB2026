import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { getJobDetail, getJobs } from '@/services/jobs'
import type { JobDetail, JobListItem } from '@/types/api'

export const useJobsStore = defineStore('jobs', () => {
  const rows = ref<JobListItem[]>([])
  const selectedJob = ref<JobDetail | null>(null)
  const loading = ref(false)
  const filter = ref('')
  const page = ref(1)
  const itemsPerPage = ref(10)
  const sortBy = ref([{ key: 'requiredOn', order: 'desc' as const }])

  const filteredRows = computed(() => {
    const trimmed = filter.value.trim().toLowerCase()
    if (!trimmed) {
      return rows.value
    }

    return rows.value.filter((row) =>
      [row.orderNumber, row.customerName, row.customerRef, row.orderTitle]
        .join(' ')
        .toLowerCase()
        .includes(trimmed),
    )
  })

  async function load(date = new Date()) {
    loading.value = true

    try {
      const startOn = date.toISOString().slice(0, 10)
      rows.value = await getJobs({ startOn, days: 14 })
      if (rows.value.length > 0 && !selectedJob.value) {
        await select(rows.value[0].orderId)
      }
    } finally {
      loading.value = false
    }
  }

  async function select(id: string) {
    selectedJob.value = await getJobDetail(id)
  }

  return {
    rows,
    selectedJob,
    loading,
    filter,
    page,
    itemsPerPage,
    sortBy,
    filteredRows,
    load,
    select,
  }
})