export const schedulerMessages = {
      eyebrow: '切片 C',
      title: '排程基線',
      subtitle: 'FullCalendar 現已透過 API 持久化拖放更新，時間軸/資源進階決策仍待確認。',
      persistFailed: '無法儲存排程更新，已還原此次移動。',
      loadFailedFallback: '無法載入排程，改為顯示回退事件。',
      noSchedulesSample: '找不到排程 - 範例事件',
      fallbackEvent: '排程回退事件',
      schedule: {
        title: '作業排程 - 排排程',
        loadFailed: '無法載入排程資料，請確認 API 可用性。',
        saveFailed: '無法儲存排程，請再試一次。',
        saveConfirm: '儲存排程？',
        machine: {
          all: '全部',
        },
        available: {
          title: '可用工單',
        },
        scheduled: {
          title: '已選工單',
        },
        columns: {
          order: '工單號',
          customer: '客戶',
          title: '標題',
          printQty: '印量',
          printColor: '印色',
          printSize: '印張尺寸',
        },
        actions: {
          selectAll: '全選',
          unselectOne: '取消選取',
          unselectAll: '全部取消',
          unresolved: '未解決',
          moveTop: '移至頂端',
          moveUp: '上移',
          moveDown: '下移',
          moveBottom: '移至底部',
          completed: '標記完成',
        },
        urgency: {
          red: '緊急（紅）',
          yellow: '緊急（黃）',
        },
      },
    } as const
