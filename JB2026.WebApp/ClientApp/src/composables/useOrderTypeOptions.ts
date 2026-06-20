import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  ORDER_TYPE_VALUES,
  getOrderTypeMeta,
  type OrderTypeMeta,
} from '@/utils/orderType'

export interface OrderTypeOption extends OrderTypeMeta {
  label: string
}

export function useOrderTypeOptions() {
  const { t } = useI18n({ useScope: 'global' })

  const orderTypeOptions = computed<OrderTypeOption[]>(() =>
    ORDER_TYPE_VALUES.map((value) => {
      const meta = getOrderTypeMeta(value)
      return {
        ...meta,
        label: t(`orderTypes.${meta.i18nKey}`),
      }
    }),
  )

  function orderTypeLabel(orderType: number): string {
    const meta = getOrderTypeMeta(orderType)
    return t(`orderTypes.${meta.i18nKey}`)
  }

  return {
    orderTypeOptions,
    getOrderTypeMeta,
    orderTypeLabel,
  }
}
