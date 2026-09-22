export const reportsMessages = {
      exceptional: {
        title: '異常報表',
        startDate: '開始日期',
        endDate: '結束日期',
        rows: '共 {count} 筆',
        totalInvoice: '總計: {amount}',
        loadFailed: '無法載入異常報表，請確認 API 可用性。',
        empty: '沒有符合所選條件的異常記錄。',
        criteria: {
          title: '篩選條件',
          days: '天數',
          matchHint: '顯示符合任一啟用條件的記錄。',
          reasons: '原因',
          notScheduled: '未排期',
          notCompleted: '未完成',
          noInvoice: '無發票',
          noInvoiceAfterCompleted: '完成後無發票',
          noCOGS: '無成本',
        },
      },
    } as const
