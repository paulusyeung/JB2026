export const jobOrderMessages = {
      title: '工单登记',
      subtitle: '专用工单界面，后端来自 /api/v2/job-orders。',
      search: '搜索订单/客户',
      selectedOrder: '已选订单',
      requiredQty: '要求: {date} - 数量: {qty}',
      headers: {
        order: '订单',
        jobNumber: '作业 #',
        customer: '客户',
        title: '标题',
        ordered: '下单日期',
        required: '要求日期',
        qty: '数量',
      },
      loadFailed: '无法加载工单，请检查 API 可用性。',
    } as const
