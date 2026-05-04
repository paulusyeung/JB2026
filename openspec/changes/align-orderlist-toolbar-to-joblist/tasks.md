## 1. Baseline Mapping

- [x] 1.1 Record the first-four toolbar pattern from **`JobListView.vue`** (desktop + phone bar + overflow): columns, sorting, checkbox, views; capture markup/styling references (icons, `v-menu` structure, `:active` on view rows)
- [x] 1.2 Map OrderList to that baseline; document implementation of **views** (`viewMode`, detail vs card rendering) and any OrderList-specific layout work for card mode

## 2. OrderList Toolbar Alignment

- [x] 2.1 Update **OrderList** desktop toolbar so the first four controls are columns → sorting → checkbox → **views**, copying structure/styling from **`JobListView.vue`**; insert vertical divider and remaining actions using the same post-divider pattern as the baseline
- [x] 2.2 Update OrderList **phone** overflow so checkbox + detail/card view items match **`JobListView.vue`** order and active states; keep columns + sort on the bar as in the baseline
- [x] 2.3 Align any additional overflow/toolbar items after the view-mode entries with **`JobListView.vue`** where OrderList has equivalent actions; verify batch delete / conditional actions still behave correctly

## 3. Labels and Icon Parity

- [x] 3.1 Reuse i18n keys from **`jobOrder.jobList.actions`** (and related jobList strings) for shared labels such as **views**, detail/card titles, where copy must match JobList
- [x] 3.2 Add or adjust keys under `jobOrder.orderList` or locales only when OrderList-specific wording is required; keep `en`, `zhHans`, `zhHant` in sync
- [x] 3.3 Verify first-four icons and labels match **`JobListView.vue`** on desktop and on mobile for aligned overflow rows

## 4. Behavior Validation

- [x] 4.1 Verify columns control still opens and updates visible columns
- [x] 4.2 Verify sorting control still updates sort field and direction
- [x] 4.3 Verify checkbox mode still toggles selection UI and selected-count behavior
- [x] 4.4 Verify **views**: detail vs card switches presentation without breaking list data, selection, or navigation; compare UX to JobList card/detail behavior

## 5. QA and Cleanup

- [x] 5.1 Manual parity pass: side-by-side **`JobListView.vue`** vs OrderList on desktop and phone (first four + overflow entries)
- [x] 5.2 Remove unused imports/template fragments introduced during alignment
- [x] 5.3 Capture before/after notes or screenshots for reviewer signoff

## Implementation Notes (2026-05-05)

- Baseline capture from **`JobListView.vue`**: first-four toolbar controls are columns (`mdi-view-column`) -> sorting (`mdi-sort`) -> checkbox (`mdi-checkbox-multiple-marked-outline`) -> views (`mdi-eye-outline`) with a `v-menu` containing detail/card `v-list-item` rows using `:active` state.
- OrderList mapping: introduced `viewMode` (`detail`/`card`) and `setViewMode` in **`OrderListView.vue`**, plus computed labels from `jobOrder.jobList.actions.detailView` and `jobOrder.jobList.actions.cardView`.
- Desktop parity: OrderList first four now match JobList order and control structure before divider; downstream actions preserve existing OrderList semantics.
- Phone parity: top overflow now includes checkbox then detail/card view rows with active state; columns/sorting remain on the toolbar bar.
- View rendering parity: OrderList now switches between table/detail mode and card presentation via `isCardView`.
- i18n reuse: shared view-related labels were reused from `jobOrder.jobList.actions`; no locale-file key additions were required.
- Before/after summary: fourth control changed from print to views; mobile overflow gained detail/card entries after checkbox to match JobList pattern.
