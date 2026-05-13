export interface ThemeConfig {
  dark: boolean;
  colors: {
    background: string;
    surface: string;
    surfaceVariant: string;
    primary: string;
    secondary: string;
    accent: string;
    success: string;
    warning: string;
    error: string;
    info: string;
  };
}

export interface ThemePair {
  id: string;
  light: ThemeConfig;
  dark: ThemeConfig;
}

export const themeRegistry: ThemePair[] = [
  {
    id: 'nature',
    light: {
      dark: false,
      colors: {
        background: '#f5f4ee',
        surface: '#fffdf8',
        surfaceVariant: '#ece4d5',
        primary: '#9f4f2a',
        secondary: '#284b63',
        accent: '#c9923d',
        success: '#487a52',
        warning: '#c4812f',
        error: '#9c2f2f',
        info: '#406882',
      },
    },
    dark: {
      dark: true,
      colors: {
        background: '#161916',
        surface: '#1e241f',
        surfaceVariant: '#2a322b',
        primary: '#e29a60',
        secondary: '#8cb9d4',
        accent: '#d8ab58',
        success: '#7ec08c',
        warning: '#e0ae53',
        error: '#ef8a8a',
        info: '#7fb2cf',
      },
    },
  },
  {
    id: 'indigo',
    light: {
      dark: false,
      colors: {
        background: '#f8fafc',
        surface: '#ffffff',
        surfaceVariant: '#e2e8f0',
        primary: '#1e40af',
        secondary: '#0ea5e9',
        accent: '#f59e0b',
        success: '#10b981',
        warning: '#f59e0b',
        error: '#ef4444',
        info: '#3b82f6',
      },
    },
    dark: {
      dark: true,
      colors: {
        background: '#020617',
        surface: '#0f172a',
        surfaceVariant: '#1e293b',
        primary: '#38bdf8',
        secondary: '#7dd3fc',
        accent: '#818cf8',
        success: '#34d399',
        warning: '#fbbf24',
        error: '#fb7185',
        info: '#60a5fa',
      },
    },
  },
  {
    id: 'rose',
    light: {
      dark: false,
      colors: {
        background: '#fff5f8',
        surface: '#ffffff',
        surfaceVariant: '#ffe4e6',
        primary: '#e11d48',
        secondary: '#fb7185',
        accent: '#c026d3',
        success: '#10b981',
        warning: '#f59e0b',
        error: '#ef4444',
        info: '#3b82f6',
      },
    },
    dark: {
      dark: true,
      colors: {
        background: '#2d0a1a',
        surface: '#4a102e',
        surfaceVariant: '#701a75',
        primary: '#fb7185',
        secondary: '#f472b6',
        accent: '#e879f9',
        success: '#34d399',
        warning: '#fbbf24',
        error: '#f87171',
        info: '#818cf8',
      },
    },
  },
  {
    id: 'slate',
    light: {
      dark: false,
      colors: {
        background: '#f8fafc',
        surface: '#ffffff',
        surfaceVariant: '#e2e8f0',
        primary: '#475569',
        secondary: '#94a3b8',
        accent: '#0ea5e9',
        success: '#10b981',
        warning: '#f59e0b',
        error: '#ef4444',
        info: '#3b82f6',
      },
    },
    dark: {
      dark: true,
      colors: {
        background: '#0f172a',
        surface: '#1e293b',
        surfaceVariant: '#334155',
        primary: '#94a3b8',
        secondary: '#cbd5e1',
        accent: '#38bdf8',
        success: '#34d399',
        warning: '#fbbf24',
        error: '#ef8a8a',
        info: '#60a5fa',
      },
    },
  },
  {
    id: 'forest',
    light: {
      dark: false,
      colors: {
        background: '#f0f4f0',
        surface: '#ffffff',
        surfaceVariant: '#dce6dc',
        primary: '#2d5a27',
        secondary: '#4a7c44',
        accent: '#8b4513',
        success: '#487a52',
        warning: '#c4812f',
        error: '#9c2f2f',
        info: '#406882',
      },
    },
    dark: {
      dark: true,
      colors: {
        background: '#161916',
        surface: '#1e241f',
        surfaceVariant: '#2a322b',
        primary: '#e29a60',
        secondary: '#8cb9d4',
        accent: '#d8ab58',
        success: '#7ec08c',
        warning: '#e0ae53',
        error: '#ef8a8a',
        info: '#7fb2cf',
      },
    },
  },
  {
    id: 'midnight',
    light: {
      dark: false,
      colors: {
        background: '#e0f2fe',
        surface: '#ffffff',
        surfaceVariant: '#bae6fd',
        primary: '#0369a1',
        secondary: '#0ea5e9',
        accent: '#0284c7',
        success: '#10b981',
        warning: '#f59e0b',
        error: '#ef4444',
        info: '#3b82f6',
      },
    },
    dark: {
      dark: true,
      colors: {
        background: '#020617',
        surface: '#0f172a',
        surfaceVariant: '#1e293b',
        primary: '#38bdf8',
        secondary: '#7dd3fc',
        accent: '#818cf8',
        success: '#34d399',
        warning: '#fbbf24',
        error: '#fb7185',
        info: '#60a5fa',
      },
    },
  },
  {
    id: 'amethyst',
    light: {
      dark: false,
      colors: {
        background: '#faf5ff',
        surface: '#ffffff',
        surfaceVariant: '#f3e8ff',
        primary: '#7e22ce',
        secondary: '#a855f7',
        accent: '#d946ef',
        success: '#10b981',
        warning: '#f59e0b',
        error: '#ef4444',
        info: '#3b82f6',
      },
    },
    dark: {
      dark: true,
      colors: {
        background: '#1a1024',
        surface: '#261a35',
        surfaceVariant: '#352648',
        primary: '#c084fc',
        secondary: '#a855f7',
        accent: '#f0abfc',
        success: '#4ade80',
        warning: '#fbbf24',
        error: '#f87171',
        info: '#818cf8',
      },
    },
  },
  {
    id: 'obsidian',
    light: {
      dark: false,
      colors: {
        background: '#f8fafc',
        surface: '#ffffff',
        surfaceVariant: '#e2e8f0',
        primary: '#0f172a',
        secondary: '#334155',
        accent: '#64748b',
        success: '#10b981',
        warning: '#f59e0b',
        error: '#ef4444',
        info: '#3b82f6',
      },
    },
    dark: {
      dark: true,
      colors: {
        background: '#000000',
        surface: '#121212',
        surfaceVariant: '#262626',
        primary: '#fbbf24',
        secondary: '#78350f',
        accent: '#eab308',
        success: '#34d399',
        warning: '#fbbf24',
        error: '#ef8a8a',
        info: '#60a5fa',
      },
    },
  },
];