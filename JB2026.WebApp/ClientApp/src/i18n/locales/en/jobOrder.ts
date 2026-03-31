export const jobOrderMessages = {
      title: 'Job order register',
      subtitle: 'Dedicated Job Order surface backed by /api/v2/job-orders.',
      search: 'Search order/customer',
      selectedOrder: 'Selected order',
      requiredQty: 'Required: {date} - Qty: {qty}',
      headers: {
        order: 'Order',
        jobNumber: 'Job #',
        customer: 'Customer',
        title: 'Title',
        ordered: 'Ordered',
        required: 'Required',
        qty: 'Qty',
      },
      loadFailed: 'Unable to load job orders. Please verify API availability.',
    } as const
