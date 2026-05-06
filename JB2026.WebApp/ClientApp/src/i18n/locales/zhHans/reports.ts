export const reportsMessages = {
      title: '报表运行器',
      subtitle: '通过现代报表契约运行异常报价报表。',
      startDate: '开始日期',
      runReport: '运行报表',
      rows: '行数: {count}',
      totalA: '总额 A: {amount}',
      exceptional: {
        title: '异常报表',
        subtitle: '按月份显示工单异常清单（旧版样式）。',
        month: '月份',
        rows: '共 {count} 条',
        loadFailed: '无法加载异常报表，请检查 API 可用性。',
      },
      headers: {
        quote: '报价',
        customer: '客户',
        title: '标题',
        quotedOn: '报价日期',
        quotedBy: '报价人',
        totalA: '总额 A',
      },
      runFailed: '无法运行报表，请检查 API 可用性。',
    } as const
