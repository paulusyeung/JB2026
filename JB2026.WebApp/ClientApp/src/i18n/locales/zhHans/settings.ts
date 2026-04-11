export const settingsMessages = {
      title: '设置',
      subtitle: '沿用旧版系统参数配置，包含编号、查询默认值与 Gmail 集成。',
      fields: {
        ownerName: 'Owner 名称',
        nextOrderNumber: '下一个订单编号',
        nextProductNumber: '下一个产品编号',
        nextQuotationNumber: '下一个报价编号',
        commonQuery: '常用查询',
        completedQuery: '完成查询',
        scheduleQueryRange: '显示已完成作业于',
        daysUnit: '天',
        gmailAccount: 'Gmail 账号',
        gmailPassword: 'Gmail 密码',
      },
      actions: {
        save: '保存',
      },
      commonQueryOptions: {
        none: '无',
        ordered7: '最近 7 天下单',
        ordered30: '最近 30 天下单',
        ordered90: '最近 90 天下单',
      },
      completedQueryOptions: {
        none: '无',
        completed7: '最近 7 天完成',
        completed30: '最近 30 天完成',
        completed90: '最近 90 天完成',
      },
      messages: {
        loadFailed: '无法加载设置，请检查 API 可用性。',
        saveSuccess: '设置已成功保存。',
        saveFailed: '无法保存设置，请检查 API 可用性。',
        scheduleRangeInvalid: '范围至少需要 1 天。',
      },
    } as const
