export const settingsMessages = {
      title: '設定',
      subtitle: '沿用舊版系統參數設定，包含編號、查詢預設與 Gmail 整合。',
      fields: {
        ownerName: 'Owner 名稱',
        nextOrderNumber: '下一個訂單編號',
        nextProductNumber: '下一個產品編號',
        nextQuotationNumber: '下一個報價編號',
        commonQuery: '常用查詢',
        completedQuery: '完成查詢',
        scheduleQueryRange: '顯示已完成作業於',
        daysUnit: '天',
        gmailAccount: 'Gmail 帳號',
        gmailPassword: 'Gmail 密碼',
      },
      actions: {
        save: '儲存',
      },
      commonQueryOptions: {
        none: '無',
        ordered7: '最近 7 天下單',
        ordered30: '最近 30 天下單',
        ordered90: '最近 90 天下單',
      },
      completedQueryOptions: {
        none: '無',
        completed7: '最近 7 天完成',
        completed30: '最近 30 天完成',
        completed90: '最近 90 天完成',
      },
      messages: {
        loadFailed: '無法載入設定，請確認 API 可用性。',
        saveSuccess: '設定已成功儲存。',
        saveFailed: '無法儲存設定，請確認 API 可用性。',
        scheduleRangeInvalid: '範圍至少需為 1 天。',
      },
    } as const
