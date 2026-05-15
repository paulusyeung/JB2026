
# OpenSpec Proposal: Mobile-First UX Transformation




































## 1. Design Document

### 1.1 Context & Goals
The current application uses a "Desktop-Preferred" approach, which results in a degraded mobile experience characterized by "scroll gaps" and high interaction friction. The goal is to transition to an **Adaptive Workflow** model where the UI changes its fundamental interaction pattern based on the device, rather than just its layout.

### 1.2 UX Strategy
- **Context Preservation:** Instead of stacking panels vertically (causing loss of focus), we use **Bottom Sheets** for secondary actions (e.g., selecting available jobs).
- **Information Density:** Replace rigid HTML tables with **Adaptive Cards** on mobile to prevent data hiding and horizontal scrolling.
- **Touch-First Interaction:** Replace dense button rows with **Action Menus** and increase touch targets to $\ge 44\text{px}$.
- **Cognitive Load Reduction:** Remove "Desktop Preferred" warnings and replace them with a seamless, optimized mobile experience.

### 1.3 User Flow Changes (Scheduler)
- **Current:** Scroll to Available $\rightarrow$ Select $\rightarrow$ Click Transfer $\rightarrow$ Scroll to Scheduled $\rightarrow$ Verify.
- **Proposed:** View Scheduled $\rightarrow$ Open "Add Job" Bottom Sheet $\rightarrow$ Select $\rightarrow$ Confirm $\rightarrow$ Sheet closes $\rightarrow$ Verify in Scheduled view.

---

## 2. Technical Specification

### 2.1 Component Architecture
- **`AdaptiveRow.vue`**: A wrapper component that accepts slot data.
    - *Desktop Mode*: Renders as a standard `<tr>` with `<td>` elements.
    - *Mobile Mode*: Renders as a `v-card` with a structured grid of labels and values.
- **`useTouch` Composable**: A utility to detect not just screen width, but touch capability and safe-area insets for mobile devices.
- **`JobActionMenu.vue`**: A replacement for the machine-transfer button row, utilizing `v-menu` or `v-bottom-sheet` for selection.

### 2.2 Data Handling
- **Optimistic UI**: Implement a local state update in Pinia/Vue before the `saveScheduleBatch` API call completes, with a rollback mechanism on failure.
- **Viewport Management**: Use Vuetify's `useDisplay` combined with `useTouch` to trigger the switch between Table and Card views.

### 2.3 View-Specific Changes
- **`ScheduleView.vue`**:
    - Move "Available" panel into a `v-bottom-sheet`.
    - Implement `AdaptiveRow` for both lists.
- **`SmlInvoiceStatsView.vue` / `JobStatsView.vue`**:
    - Implement a "KPI Header" (Summary Cards) that acts as a filter/entry point for the detailed pivot data.

---

## 3. Implementation Task List
### Phase 1: Foundation & Adaptive Components



- [ ] **Task 1.1**: Create `src/composables/useTouch.ts` for advanced device detection.
- [ ] **Task 1.2**: Develop `src/components/ui/AdaptiveRow.vue` supporting Table/Card duality.
- [ ] **Task 1.3**: Refactor `ScheduleView.vue` to integrate `AdaptiveRow` for the "Available" and "Scheduled" lists.






### Phase 2: Scheduler Workflow Overhaul
- [ ] **Task 2.1**: Implement the "Available Jobs" `v-bottom-sheet` for mobile viewports.
- [ ] **Task 2.2**: Replace the machine-transfer button row with a touch-friendly `JobActionMenu`.
- [ ] **Task 2.3**: Refactor the "Light Toolbar" (status circles) into a mobile-accessible selection menu.
### Phase 3: Analytics & Global Polish
- [ ] **Task 3.1**: Implement "Summary-First" KPI headers in `SmlInvoiceStatsView.vue` and `JobStatsView.vue`.
- [ ] **Task 3.2**: Audit all "Desktop Preferred" notices and replace them with optimized mobile UI.
- [ ] **Task 3.3**: Implement optimistic UI updates for the scheduling save process to eliminate perceived latency.

---

## 4. Success Criteria
- **Zero "Desktop Preferred" warnings** remaining in the application.
- **Reduction in scroll distance** during the "Move Job" workflow on mobile.
- **100% visibility of critical data** (Qty, Color, Size) on mobile without horizontal scrolling.
