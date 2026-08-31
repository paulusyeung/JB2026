export const ORDER_TYPE = {
  OffsetPrint: 0,
  DigitalPrint: 1,
  WovenLabel: 2,
  Others: 3,
} as const

export const ORDER_TYPE_VALUES = [
  ORDER_TYPE.OffsetPrint,
  ORDER_TYPE.DigitalPrint,
  ORDER_TYPE.WovenLabel,
  ORDER_TYPE.Others,
] as const

export type OrderTypeValue = (typeof ORDER_TYPE_VALUES)[number]

export type OrderTypeI18nKey = 'offsetPrint' | 'digitalPrint' | 'wovenLabel' | 'others'

export interface OrderTypeMeta {
  value: OrderTypeValue
  icon: string
  color: string
  i18nKey: OrderTypeI18nKey
}

const ORDER_TYPE_META: Record<OrderTypeValue, Omit<OrderTypeMeta, 'value'>> = {
  [ORDER_TYPE.OffsetPrint]: {
    icon: 'mdi-tag-outline',
    color: 'success',
    i18nKey: 'offsetPrint',
  },
  [ORDER_TYPE.DigitalPrint]: {
    icon: 'mdi-tag-text-outline',
    color: 'error',
    i18nKey: 'digitalPrint',
  },
  [ORDER_TYPE.WovenLabel]: {
    icon: 'mdi-label-outline',
    color: 'primary',
    i18nKey: 'wovenLabel',
  },
  [ORDER_TYPE.Others]: {
    icon: 'mdi-shape-outline',
    color: 'secondary',
    i18nKey: 'others',
  },
}

export function normalizeOrderType(orderType: number): OrderTypeValue {
  return ORDER_TYPE_VALUES.includes(orderType as OrderTypeValue)
    ? (orderType as OrderTypeValue)
    : ORDER_TYPE.OffsetPrint
}

export function getOrderTypeMeta(orderType: number): OrderTypeMeta {
  const value = normalizeOrderType(orderType)
  return { value, ...ORDER_TYPE_META[value] }
}
