import colors from 'vuetify/util/colors'

export interface ThemeConfig {
  dark: boolean;
  colors: {
    background: string;
    surface: string;
    'surface-variant': string;
    'on-surface-variant': string;
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

export const materialPalette = [
  'red',
  'pink',
  'purple',
  'deepPurple',
  'indigo',
  'blue',
  'lightBlue',
  'cyan',
  'teal',
  'green',
  'lightGreen',
  'lime',
  'yellow',
  'amber',
  'orange',
  'deepOrange',
  'brown',
  'blueGrey',
  'grey',
] as const

export type MaterialPaletteKey = (typeof materialPalette)[number]

export const themeRegistry: ThemePair[] = materialPalette.map(name => ({
  id: name,
  light: {
    dark: false,
    colors: {
      background: colors.grey.lighten5,
      surface: colors.shades.white,
      'surface-variant': colors[name].lighten5,
      'on-surface-variant': colors.grey.darken4,
      primary: colors[name].darken2,
      secondary: colors[name].lighten1,
      accent: colors[name].base,
      success: colors.green.darken2,
      warning: colors.amber.darken1,
      error: colors.red.darken2,
      info: colors.lightBlue.darken2,
    },
  },
  dark: {
    dark: true,
    colors: {
      background: colors.grey.darken4,
      surface: colors.grey.darken3,
      'surface-variant': colors.grey.darken2,
      'on-surface-variant': colors.grey.lighten4,
      primary: colors[name].lighten3,
      secondary: colors[name].lighten1,
      accent: colors[name].lighten4,
      success: colors.green.lighten2,
      warning: colors.amber.lighten1,
      error: colors.red.lighten3,
      info: colors.lightBlue.lighten2,
    },
  },
}))
