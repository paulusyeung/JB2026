export const reportsMessages = {
      title: 'Reports runner',
      subtitle: 'Runs the exceptional quotation report through the modern report contract.',
      startDate: 'Start date',
      runReport: 'Run report',
      rows: 'Rows: {count}',
      totalA: 'Total A: {amount}',
      exceptional: {
        title: 'Exceptional Report',
        subtitle: 'Legacy-style monthly exceptional list for job orders.',
        month: 'Month',
        rows: '{count} records',
        loadFailed: 'Unable to load exceptional report. Please verify API availability.',
      },
      headers: {
        quote: 'Quote',
        customer: 'Customer',
        title: 'Title',
        quotedOn: 'Quoted On',
        quotedBy: 'Quoted By',
        totalA: 'Total A',
      },
      runFailed: 'Unable to run report. Please verify API availability.',
    } as const
