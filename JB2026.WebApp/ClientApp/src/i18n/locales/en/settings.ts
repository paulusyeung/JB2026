export const settingsMessages = {
      title: 'Settings',
      subtitle: 'System configuration for the modern slice host.',
      fields: {
        companyName: 'Company Name',
        timeZone: 'Time Zone',
        currency: 'Currency',
        enableLegacyFallback: 'Enable legacy fallback',
      },
      actions: {
        save: 'Save settings',
      },
      messages: {
        loadFailed: 'Unable to load settings. Please verify API availability.',
        saveSuccess: 'Settings saved successfully.',
        saveFailed: 'Unable to save settings. Please verify API availability.',
      },
    } as const
