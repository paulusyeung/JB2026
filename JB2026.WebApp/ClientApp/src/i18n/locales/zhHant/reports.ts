export const reportsMessages = {
      title: '報表執行器',
      subtitle: '透過現代報表契約執行異常報價報表。',
      startDate: '開始日期',
      runReport: '執行報表',
      rows: '筆數: {count}',
      totalA: '總額 A: {amount}',
      exceptional: {
        title: '異常報表',
        subtitle: '依月份顯示工單異常清單（舊版樣式）。',
        month: '月份',
        rows: '共 {count} 筆',
        loadFailed: '無法載入異常報表，請確認 API 可用性。',
      },
      headers: {
        quote: '報價',
        customer: '客戶',
        title: '標題',
        quotedOn: '報價日期',
        quotedBy: '報價人',
        totalA: '總額 A',
      },
      runFailed: '無法執行報表，請確認 API 可用性。',
    } as const
