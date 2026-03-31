export const settingsMessages = {
      title: '设置',
      subtitle: '现代切片主机的系统配置。',
      fields: {
        companyName: '公司名称',
        timeZone: '时区',
        currency: '货币',
        enableLegacyFallback: '启用遗留回退',
      },
      actions: {
        save: '保存设置',
      },
      messages: {
        loadFailed: '无法加载设置，请检查 API 可用性。',
        saveSuccess: '设置已成功保存。',
        saveFailed: '无法保存设置，请检查 API 可用性。',
      },
    } as const
