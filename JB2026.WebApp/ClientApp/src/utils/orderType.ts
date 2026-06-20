export const ORDER_TYPE = {
  Printing: 0,
  PrintedLabel: 1,
  WovenLabel: 2,
  Other: 3,
} as const

export const ORDER_TYPE_VALUES = [
  ORDER_TYPE.Printing,
  ORDER_TYPE.PrintedLabel,
  ORDER_TYPE.WovenLabel,
  ORDER_TYPE.Other,
] as const

export type OrderTypeValue = (typeof ORDER_TYPE_VALUES)[number]

export type OrderTypeI18nKey = 'printing' | 'printedLabel' | 'wovenLabel' | 'other'

export interface OrderTypeMeta {
  value: OrderTypeValue
  icon: string
  color: string
  i18nKey: OrderTypeI18nKey
}

const ORDER_TYPE_META: Record<OrderTypeValue, Omit<OrderTypeMeta, 'value'>> = {
  [ORDER_TYPE.Printing]: {
    icon: 'mdi-tag-outline',
    color: 'success',
    i18nKey: 'printing',
  },
  [ORDER_TYPE.PrintedLabel]: {
    icon: 'mdi-tag-text-outline',
    color: 'error',
    i18nKey: 'printedLabel',
  },
  [ORDER_TYPE.WovenLabel]: {
    icon: 'mdi-label-outline',
    color: 'warning',
    i18nKey: 'wovenLabel',
  },
  [ORDER_TYPE.Other]: {
    icon: 'mdi-shape-outline',
    color: 'secondary',
    i18nKey: 'other',
  },
}

export function normalizeOrderType(orderType: number): OrderTypeValue {
  return ORDER_TYPE_VALUES.includes(orderType as OrderTypeValue)
    ? (orderType as OrderTypeValue)
    : ORDER_TYPE.Printing
}

export function getOrderTypeMeta(orderType: number): OrderTypeMeta {
  const value = normalizeOrderType(orderType)
  return { value, ...ORDER_TYPE_META[value] }
}
