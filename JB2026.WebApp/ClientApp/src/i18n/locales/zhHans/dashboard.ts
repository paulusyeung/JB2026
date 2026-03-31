export const dashboardMessages = {
      eyebrow: '切片 A',
      title: '只读列表和仪表板已在 SPA 主机后方上线。',
      description: '该壳层演示了 Vuetify 布局、功能开关、Chart.js 报表，以及 jobs/quotations API 接口。',
      kpi: {
        enabledSlicesLabel: '已启用切片',
        enabledSlicesHelper: '当前由服务端开关控制并由 SPA 承载的路由',
        jobsLoadedLabel: '已加载作业',
        jobsLoadedHelper: '来自 /api/v2/jobs/range 的作业列表响应',
        quotationsLoadedLabel: '已加载报价',
        quotationsLoadedHelper: '来自 /api/v2/quotations 的报价列表响应',
      },
      sliceHealth: {
        title: '切片健康状态',
        subtitle: '标记来自 /ui/feature-flags。',
        enabled: '已启用',
        legacy: '遗留',
      },
      volumeTrend: {
        title: '数量趋势',
        subtitle: '使用 Chart.js 替换遗留仪表板图表区块。',
        labels: {
          featureFlags: '功能开关',
          jobs: '作业',
          quotations: '报价',
        },
        datasetLabel: '当前数量',
      },
    } as const
