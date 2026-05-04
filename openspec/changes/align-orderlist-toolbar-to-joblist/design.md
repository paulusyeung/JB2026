## Context

OrderList and JobList are both high-frequency operational pages, but their early-toolbar action sequence diverges. **`JobListView.vue`** defines the baseline for the first control group: **columns**, **sorting**, **checkbox mode**, and **views** (detail vs card). OrderList currently uses **print** as the fourth desktop action and omits a JobList-style **views** control and matching overflow entries. This creates user friction because operators often switch between both pages during the same session.

The change is scoped to frontend composition in Vue/Vuetify and existing i18n resources. OrderList must gain **view-mode state and UI** comparable to JobList (not label-only shims). No API contract changes are required.

## Goals / Non-Goals

**Goals:**
- First four OrderList actions SHALL be **columns → sorting → checkbox → views**, matching `JobListView.vue` in order, component patterns (menus, activators, list items), and styling.
- Implement **views** so users can switch OrderList between **detail** and **card** presentations in line with JobList semantics (including active states on menu rows).
- Keep parity for **phone overflow**: same ordering and interaction model for checkbox and view-mode entries as `JobListView.vue` (columns and sort stay on the bar, not inside overflow).
- Preserve existing OrderList data-loading and non-toolbar business rules; minimize unrelated refactors.

**Non-Goals:**
- Redesigning the full OrderList page outside toolbar + view-mode presentation wiring.
- Changing backend endpoints, DTOs, or persistence behavior.
- Introducing a shared cross-route toolbar component in this slice (still duplicate JobList patterns in OrderList for speed/clarity).

## Decisions

1. **First four are fixed and named:** columns, sorting, checkbox, views — **canonical baseline is `JobListView.vue`** (not an abstract “JobList” description).
   - Rationale: One source of truth for structure, icons (`mdi-view-column`, `mdi-sort`, `mdi-checkbox-multiple-marked-outline`, `mdi-eye-outline` + detail/card rows), and Vuetify variants.
   - Alternative: “Equivalent actions where they exist.”
   - Why not: Product decision is explicit parity including **views** as the fourth control.

2. **Views is a real mode switch on OrderList**, not a cosmetic menu.
   - Rationale: Matches JobList behavior and spec scenarios (detail vs card).
   - Implementation note: Introduce `viewMode` (or equivalent) and rendering branches analogous to JobList; reuse labels/icons/active styling from the baseline file.

3. Apply parity in both **desktop toolbar** and **phone overflow** for the segments that JobList places there (checkbox first in overflow, then detail/card rows with `:active`, then subsequent items per baseline).
   - Rationale: Same discovery model as `JobListView.vue` at each breakpoint.

4. **Reuse existing Vuetify composition** by copying the JobList toolbar block structure and adapting bindings to OrderList state.
   - Rationale: Visual and behavioral alignment with minimal drift.

5. **After the first four**, follow `JobListView.vue` for divider placement and the **relative** order of remaining toolbar actions; preserve OrderList-specific behavior (e.g. conditional batch delete) while aligning markup/styles.

## Risks / Trade-offs

- [Risk] OrderList desktop “card” presentation may require non-trivial layout work to match JobList card UX.
  → Mitigation: Scope card view to parity with JobList’s card list behavior where feasible; document any intentional gaps in tasks if discovered during implementation.

- [Risk] i18n mix of `orderList` vs `jobList` namespaces for shared strings (e.g. views).
  → Mitigation: Prefer shared `jobList.actions` keys for identical copy; keep OrderList-specific strings under `orderList` when semantics differ.

- [Risk] Toolbar attachment ordering vs OrderList (JobList puts attachment before print).
  → Mitigation: Align post-divider strip to `JobListView.vue` when adding or reordering for parity; call out in QA.

- [Trade-off] Duplicated toolbar markup between views.
  → Benefit: Fast delivery and explicit diffability against `JobListView.vue`.
  → Cost: Future refactors may want a shared primitive later.
