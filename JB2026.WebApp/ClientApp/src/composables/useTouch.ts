import { ref, onMounted, onUnmounted } from 'vue'

export function useTouch() {
  const isTouchDevice = ref(false)
  const safeAreaInsetBottom = ref(0)
  const legacyNavigator = navigator as Navigator & { msMaxTouchPoints?: number }

  const updateSafeArea = () => {
    // Get the CSS variable for safe area inset bottom
    const inset = getComputedStyle(document.documentElement)
      .getPropertyValue('--safe-area-inset-bottom')
      .trim()
    
    if (inset) {
      safeAreaInsetBottom.value = parseInt(inset, 10) || 0
    } else {
      // Fallback for browsers that don't support the CSS variable directly
      // but we can check the window.screen.height vs window.innerHeight
      safeAreaInsetBottom.value = window.innerHeight < window.screen.height ? 20 : 0
    }
  }

  onMounted(() => {
    // Detect touch capability
    isTouchDevice.value = (
      'ontouchstart' in window ||
      navigator.maxTouchPoints > 0 ||
      (legacyNavigator.msMaxTouchPoints ?? 0) > 0
    )
    
    updateSafeArea()
    window.addEventListener('resize', updateSafeArea)
  })

  onUnmounted(() => {
    window.removeEventListener('resize', updateSafeArea)
  })

  return {
    isTouchDevice,
    safeAreaInsetBottom
  }
}