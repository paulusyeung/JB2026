export const schedulerMessages = {
      eyebrow: '切片 C',
      title: '排程基线',
      subtitle: 'FullCalendar 已通过 API 持久化拖拽更新时间，时间线/资源高级功能仍在评估。',
      persistFailed: '无法保存排程更新，移动已回滚。',
      loadFailedFallback: '无法加载排程，正在显示回退事件。',
      noSchedulesSample: '未找到排程 - 示例事件',
      fallbackEvent: '排程回退事件',
      schedule: {
        title: '作业排程 - 排程',
        loadFailed: '无法加载排程数据，请确认 API 可用性。',
        saveFailed: '无法保存排程，请重试。',
        saveConfirm: '保存排程？',
        machine: {
          all: '全部',
        },
        available: {
          title: '可用工单',
        },
        scheduled: {
          title: '已选工单',
        },
        columns: {
          order: '工单号',
          customer: '客户',
          title: '标题',
          printQty: '印量',
          printColor: '印色',
          printSize: '印张尺寸',
        },
        actions: {
          selectAll: '全选',
          unselectOne: '取消选取',
          unselectAll: '全部取消',
          unresolved: '未解决',
          moveTop: '移至顶端',
          moveUp: '上移',
          moveDown: '下移',
          moveBottom: '移至底部',
          completed: '标记完成',
        },
        urgency: {
          red: '紧急（红）',
          yellow: '紧急（黄）',
        },
      },
    } as const
