export const reportsMessages = {
      exceptional: {
        title: '异常报表',
        startDate: '开始日期',
        endDate: '结束日期',
        rows: '共 {count} 条',
        totalInvoice: '总计: {amount}',
        loadFailed: '无法加载异常报表，请检查 API 可用性。',
        empty: '没有符合所选条件的异常记录。',
        criteria: {
          title: '筛选条件',
          days: '天数',
          matchHint: '显示匹配任一所选条件的记录。',
          reasons: '原因',
          notScheduled: '未排期',
          notCompleted: '未完成',
          noInvoice: '无发票',
          noInvoiceAfterCompleted: '完成后无发票',
          noCOGS: '无成本',
        },
      },
    } as const
