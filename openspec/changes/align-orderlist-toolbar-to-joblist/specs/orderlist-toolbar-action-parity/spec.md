## ADDED Requirements

### Requirement: OrderList first four desktop toolbar actions SHALL match JobListView.vue

The OrderList desktop toolbar SHALL present its first four controls in this **exact order**, matching **`JobListView.vue`** in interaction type, structure, and Vuetify styling (variants, icons, menus):

1. **Columns** — column visibility menu (`mdi-view-column`).
2. **Sorting** — sort field and direction (`mdi-sort`).
3. **Checkbox** — toggle selection mode (`mdi-checkbox-multiple-marked-outline`).
4. **Views** — menu (`mdi-eye-outline`) with **detail** and **card** entries, active row state, and behavior aligned with JobList’s `viewMode` / `setViewMode` pattern.

#### Scenario: Desktop first-four order and views are aligned

- **WHEN** a user opens OrderList on a non-phone layout
- **THEN** the first four toolbar controls are columns, then sorting, then checkbox, then views, consistent with `JobListView.vue`

#### Scenario: Views fourth slot matches JobList interaction model

- **WHEN** the user opens the views menu on OrderList
- **THEN** detail and card options are offered with the same interaction model as `JobListView.vue` (including active indication on the selected mode), and changing mode updates OrderList’s presentation accordingly

#### Scenario: Existing downstream actions are preserved in behavior

- **WHEN** the first four actions are aligned on OrderList desktop
- **THEN** actions after the first four (after the same divider pattern as `JobListView.vue`) continue to perform their existing OrderList functions (print, export, new, delete / batch delete as applicable), subject to any deliberate reordering for JobList markup parity

---

### Requirement: OrderList phone toolbar and overflow SHALL match JobListView.vue for the shared pattern

On phone layout, OrderList SHALL match **`JobListView.vue`**: **columns** and **sorting** remain as visible bar controls; the overflow menu SHALL present **checkbox** and **view-mode** items (detail/card) in the same order and with the same active-state behavior as JobList. Remaining overflow items SHALL follow `JobListView.vue` ordering for comparable actions (e.g. attachment, print, export, new, delete) where OrderList implements those actions.

#### Scenario: Mobile overflow aligns with baseline for checkbox and views

- **WHEN** a user opens OrderList on phone layout and opens the overflow menu
- **THEN** the menu entries for checkbox mode and detail/card views match `JobListView.vue` in order, labels/icons, and active states for the view rows

#### Scenario: Columns and sort remain on the bar on phone

- **WHEN** a user uses OrderList on phone layout
- **THEN** columns and sorting remain accessible as toolbar activators outside the overflow menu, as on `JobListView.vue`

---

### Requirement: Aligned first-four actions SHALL preserve functional parity with the baseline

For **columns**, **sorting**, and **checkbox**, OrderList SHALL preserve existing behavior (visibility toggles, sort updates, selection UI). **Views** SHALL provide a meaningful detail vs card switch for OrderList content, consistent with JobList’s intent.

#### Scenario: Columns control remains functional

- **WHEN** the user triggers the columns action from OrderList
- **THEN** column visibility controls are shown and toggles still update visible columns

#### Scenario: Sorting control remains functional

- **WHEN** the user triggers sorting from OrderList
- **THEN** sort key and sort direction can be changed and row ordering updates accordingly

#### Scenario: Checkbox mode control remains functional

- **WHEN** the user toggles checkbox mode from OrderList
- **THEN** row selection UI enables/disables and selection state behavior remains correct for OrderList

---

### Requirement: Alignment SHALL be visually recognizable vs JobList

The first-four group SHALL be visually consistent with **`JobListView.vue`** (labels, icons, button variants) so users can transfer workflow between JobList and OrderList.

#### Scenario: Visual parity on desktop

- **WHEN** users compare `JobListView.vue` and OrderList desktop toolbars
- **THEN** the first four actions present consistent labels and icons for columns, sorting, checkbox, and views

#### Scenario: Visual parity in mobile overflow for aligned entries

- **WHEN** users compare overflow menus for checkbox and view modes on JobList vs OrderList on phone
- **THEN** corresponding entries are visually consistent with `JobListView.vue`
