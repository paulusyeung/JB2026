export const reportsMessages = {
      exceptional: {
        title: 'Exceptional Report',
        startDate: 'Start date',
        endDate: 'End date',
        rows: '{count} records',
        totalInvoice: 'Total: {amount}',
        loadFailed: 'Unable to load exceptional report. Please verify API availability.',
        empty: 'No exceptional records match the selected criteria.',
        criteria: {
          title: 'Criteria',
          days: 'days',
          matchHint: 'Rows matching any enabled criterion are shown.',
          reasons: 'Reasons',
          notScheduled: 'Not scheduled',
          notCompleted: 'Not completed',
          noInvoice: 'No invoice',
          noInvoiceAfterCompleted: 'No invoice after completion',
          noCOGS: 'No COGS',
        },
      },
    } as const
