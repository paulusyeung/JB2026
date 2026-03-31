export const settingsMessages = {
      title: '設定',
      subtitle: '現代切片主機的系統設定。',
      fields: {
        companyName: '公司名稱',
        timeZone: '時區',
        currency: '貨幣',
        enableLegacyFallback: '啟用遺留回退',
      },
      actions: {
        save: '儲存設定',
      },
      messages: {
        loadFailed: '無法載入設定，請確認 API 可用性。',
        saveSuccess: '設定已成功儲存。',
        saveFailed: '無法儲存設定，請確認 API 可用性。',
      },
    } as const
