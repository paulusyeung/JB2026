import type { Directive } from 'vue'

/**
 * v-draggable-dialog
 *
 * Apply to a v-card inside a v-dialog.
 * The card's first .v-card-title element acts as the drag handle.
 * The card is repositioned via CSS transform so Vuetify's centering
 * is kept as the origin — no absolute positioning needed.
 */
export const vDraggableDialog: Directive<HTMLElement> = {
  mounted(el) {
    const handle = el.querySelector<HTMLElement>('.v-card-title')
    if (!handle) return

    handle.style.cursor = 'move'
    handle.style.userSelect = 'none'

    let startX = 0
    let startY = 0
    let offsetX = 0
    let offsetY = 0

    function onMouseMove(e: MouseEvent) {
      offsetX = e.clientX - startX
      offsetY = e.clientY - startY
      el.style.transform = `translate(${offsetX}px, ${offsetY}px)`
    }

    function onMouseUp() {
      document.removeEventListener('mousemove', onMouseMove)
      document.removeEventListener('mouseup', onMouseUp)
    }

    handle.addEventListener('mousedown', (e: MouseEvent) => {
      // Ignore clicks on buttons inside the title (e.g. close button)
      if ((e.target as HTMLElement).closest('button')) return

      startX = e.clientX - offsetX
      startY = e.clientY - offsetY

      document.addEventListener('mousemove', onMouseMove)
      document.addEventListener('mouseup', onMouseUp)
    })
  },
}
