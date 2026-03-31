export const jobOrderMessages = {
      title: '工單登錄',
      subtitle: '專用工單介面，後端來自 /api/v2/job-orders。',
      search: '搜尋訂單/客戶',
      selectedOrder: '已選工單',
      requiredQty: '需求: {date} - 數量: {qty}',
      headers: {
        order: '訂單',
        jobNumber: '作業 #',
        customer: '客戶',
        title: '標題',
        ordered: '下單日期',
        required: '需求日期',
        qty: '數量',
      },
      loadFailed: '無法載入工單，請確認 API 可用性。',
    } as const
