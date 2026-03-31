export const dashboardMessages = {
      eyebrow: '切片 A',
      title: '唯讀清單與儀表板已在 SPA 主機後方上線。',
      description: '此殼層涵蓋 Vuetify 版面、功能旗標、Chart.js 報表，以及 jobs/quotations API 介面。',
      kpi: {
        enabledSlicesLabel: '已啟用切片',
        enabledSlicesHelper: '目前由伺服器旗標切換並由 SPA 提供的路由',
        jobsLoadedLabel: '已載入作業',
        jobsLoadedHelper: '來自 /api/v2/jobs/range 的作業清單回應',
        quotationsLoadedLabel: '已載入報價',
        quotationsLoadedHelper: '來自 /api/v2/quotations 的報價清單回應',
      },
      sliceHealth: {
        title: '切片健康度',
        subtitle: '旗標來源為 /ui/feature-flags。',
        enabled: '已啟用',
        legacy: '遺留',
      },
      volumeTrend: {
        title: '數量趨勢',
        subtitle: '使用 Chart.js 取代遺留儀表板圖表區塊。',
        labels: {
          featureFlags: '功能旗標',
          jobs: '作業',
          quotations: '報價',
        },
        datasetLabel: '目前數量',
      },
    } as const
