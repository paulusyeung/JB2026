export const dashboardMessages = {
      eyebrow: 'Slice A',
      title: 'Read-only lists and dashboards are live behind the SPA host.',
      description: 'This shell exercises Vuetify layout, feature flags, Chart.js reporting, and the jobs/quotations API surfaces.',
      kpi: {
        enabledSlicesLabel: 'Enabled slices',
        enabledSlicesHelper: 'Server-side flags currently serving SPA routes',
        jobsLoadedLabel: 'Jobs loaded',
        jobsLoadedHelper: 'Active job list response from /api/v2/jobs/range',
        quotationsLoadedLabel: 'Quotations loaded',
        quotationsLoadedHelper: 'Quotation list response from /api/v2/quotations',
      },
      sliceHealth: {
        title: 'Slice health',
        subtitle: 'Flags pulled from /ui/feature-flags.',
        enabled: 'Enabled',
        legacy: 'Legacy',
      },
      volumeTrend: {
        title: 'Volume trend',
        subtitle: 'Chart.js replacement for the legacy dashboard chart block.',
        labels: {
          featureFlags: 'Feature Flags',
          jobs: 'Jobs',
          quotations: 'Quotations',
        },
        datasetLabel: 'Current volume',
      },
    } as const
