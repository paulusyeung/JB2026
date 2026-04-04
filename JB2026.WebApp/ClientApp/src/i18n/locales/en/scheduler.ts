export const schedulerMessages = {
      eyebrow: 'Slice C',
      title: 'Scheduler baseline',
      subtitle: 'FullCalendar now persists drag-and-drop updates through API calls while the premium timeline/resource decision remains open.',
      persistFailed: 'Unable to persist schedule update. The move was reverted.',
      loadFailedFallback: 'Unable to load schedules. Showing fallback event.',
      noSchedulesSample: 'No schedules found - sample event',
      fallbackEvent: 'Scheduler fallback event',
      schedule: {
        title: 'Job Schedule - Schedule',
        loadFailed: 'Unable to load schedule data. Please verify API availability.',
        saveFailed: 'Unable to save schedule. Please try again.',
        saveConfirm: 'Save schedule?',
        machine: {
          all: 'All',
        },
        available: {
          title: 'Available Job Orders',
        },
        scheduled: {
          title: 'Selected Job Orders',
        },
        columns: {
          order: 'Job Order',
          customer: 'Customer',
          title: 'Title',
          printQty: 'Print Qty',
          printColor: 'Print Color',
          printSize: 'Print Size',
        },
        actions: {
          selectAll: 'Select All',
          unselectOne: 'Unselect',
          unselectAll: 'Unselect All',
          unresolved: 'Unresolved',
          moveTop: 'Move to Top',
          moveUp: 'Move Up',
          moveDown: 'Move Down',
          moveBottom: 'Move to Bottom',
          completed: 'Mark Completed',
        },
        urgency: {
          red: 'Urgent (Red)',
          yellow: 'Urgent (Yellow)',
        },
      },
    } as const
