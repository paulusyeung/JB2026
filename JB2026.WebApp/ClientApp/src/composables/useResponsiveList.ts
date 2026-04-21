import { computed } from 'vue'
import { useDisplay } from 'vuetify'

type ResponsiveColumnVisibility = {
  hideOnPhone?: string[]
  hideOnTablet?: string[]
}

export function useResponsiveList() {
  const display = useDisplay()
  const isPhoneLayout = computed(() => display.smAndDown.value)
  const isTabletLayout = computed(() => display.mdAndDown.value)

  function isColumnVisible(columnKey: string, options?: ResponsiveColumnVisibility) {
    if (isPhoneLayout.value && options?.hideOnPhone?.includes(columnKey)) {
      return false
    }

    if (!isPhoneLayout.value && isTabletLayout.value && options?.hideOnTablet?.includes(columnKey)) {
      return false
    }

    return true
  }

  return {
    isPhoneLayout,
    isTabletLayout,
    isColumnVisible,
  }
}
