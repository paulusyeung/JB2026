# Design — mobile-ux-transformation

## Context

`clientapp-mobile-readiness` established:

- Tier 1: `ListMobileCard` + `isPhoneLayout` (`display.smAndDown`) across list/admin views.
- Tier 3: `ScheduleView` stacks three panels vertically on phone, hides print Qty/Color/Size, shows desktop-preferred alert; pivot views have static summary cards + notices.
- Documentation: `JB2026.WebApp/ClientApp/MOBILE_LIMITATIONS.md` lists scheduler, FullCalendar, and pivot routes as desktop-preferred.

Stub artifacts exist from an earlier draft: `useTouch.ts` (file may be malformed), `AdaptiveRow.vue` (table/card dual render). This design aligns implementation with existing patterns and HTML constraints.

**Stakeholders**: floor supervisors and operators scheduling jobs on phones/tablets; desktop users unchanged.

## Goals / Non-Goals

**Goals:**

- Complete move-job flow on ≤430px width without scrolling between Available and Scheduled panels.
- Show Qty, Color, Size on scheduled mobile rows/cards.
- Touch targets ≥44px for primary scheduler actions on phone.
- Remove desktop-preferred messaging where acceptance criteria are met.
- Extend automated mobile regression for the scheduler path.

**Non-Goals:**

- Full feature parity for FullCalendar dense scheduling on phone.
- Replacing Vuetify, pivot library, or backend `saveScheduleBatch` contract.
- New Pinia module unless existing local state in `ScheduleView` is insufficient.
- Optimistic *server* save (batch API success before UI settle)—only local transfer feedback before explicit Save.

## Decisions

### D1: `useDisplay` drives layout; `useTouch` drives affordances only

| Signal | Source | Use |
|--------|--------|-----|
| Phone/tablet layout | `useDisplay()` (`smAndDown`, `xs`) | Bottom sheet, card vs table container, toolbar collapse |
| Touch capability | `useTouch().isTouchDevice` | Larger hit areas, active states (optional) |
| Safe area | CSS `env(safe-area-inset-*)` + composable read | Bottom sheet padding |

**Rationale:** Touch laptops and iPad + keyboard break if touch implies mobile layout.

**Alternative rejected:** Combined `useDisplay && useTouch` gate for all mobile UI.

### D2: Split render trees for schedule lists—not `<tr>`/`<v-card>` in one component

On mobile, **do not** render cards inside `<tbody>`. Pattern (matches `JobListView`):

```text
v-if="isPhoneLayout"
  ListMobileCard (Available | Scheduled)
v-else
  <table> … desktop columns, resize handles …
```

**Rationale:** Valid HTML, reuses `ListMobileCard`, avoids maintaining `AdaptiveRow` parallel to list cards.

**Alternative rejected:** `AdaptiveRow` as universal table row wrapper (invalid DOM, duplicates `ListMobileCard`).

### D3: Scheduler mobile workflow = scheduled-first + bottom sheet

```text
[Toolbar: Save | Machine filter | Refresh]
[Scheduled list — cards on phone, table on desktop]
[FAB or primary btn: "Add jobs"] → v-bottom-sheet (Available list + selection)
  → JobActionMenu: pick M1–M5 / actions → apply → dismiss sheet
[Verify in scheduled list]
```

Available panel **removed from document flow** on `isPhoneLayout`. Transfer column **removed** on phone; replaced by `JobActionMenu` (bottom sheet or `v-menu` with `min-height: 44px` items).

**Rationale:** Eliminates scroll gap; preserves focus on scheduled work.

### D4: Local transfer feedback (Type A optimistic UI)

Moves between Available ↔ Scheduled update in-memory arrays immediately on user action. Explicit **Save** still calls `saveScheduleBatch`. On Save failure: toast + reload or rollback snapshot taken at Save click.

**Rationale:** Perceived latency is from transfer + scroll, not Save round-trip alone.

**Alternative rejected:** Optimistic Save without server ack (batch validation/concurrency risk).

### D5: Pivot mobile = actionable summary, not new KPI widget

Extend existing `pivot-summary-card` in `JobStatsView` / `SmlInvoiceStatsView` (and `SmlRtfStatsView` if present) so summary tiles set filters or scroll/focus pivot sections. Remove `mobilePreferredNotice` when summary + horizontal pivot scroll meets criteria.

**Rationale:** Summary cards already exist; gap is interactivity and notice removal.

### D6: FullCalendar (`SchedulerView.vue`) — separate acceptance bar

Keep simplified `initialView` on phone. Remove desktop-preferred banner only if month/list view is operable without misleading "full scheduler" promise. Document remaining limitations in `MOBILE_LIMITATIONS.md` if any.

### D7: Component map

| Artifact | Action |
|----------|--------|
| `ListMobileCard.vue` | Extend if needed: checkbox selection, `#actions` slot for transfer |
| `JobActionMenu.vue` | **New** — machine target + move actions, 44px targets |
| `useTouch.ts` | Fix file; singleton or shared state; safe-area helper |
| `AdaptiveRow.vue` | **Delete or defer** unless a view needs desktop slot-fidelity not covered by ListMobileCard |
| `ScheduleView.vue` | Primary integration surface |

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| Duplicate card systems (`AdaptiveRow` vs `ListMobileCard`) | D2: standardize on ListMobileCard |
| Save failure after local moves confuses users | Snapshot at Save; clear error; optional reload |
| Bottom sheet + keyboard covers actions | `env(safe-area-inset-bottom)` padding; test 360/390/430 |
| Removing notices before UX is ready | Per-route checklist; keep SchedulerView notice until D6 met |
| Playwright env deps (known blocker) | Document; run in CI with browsers installed |
| i18n drift | Add keys under `scheduler.*`; remove unused `mobilePreferredNotice` per locale |

## Migration Plan

1. Phase 1: Fix `useTouch`, extend `ListMobileCard` if needed, add `JobActionMenu`.
2. Phase 2: `ScheduleView` mobile tree + bottom sheet + column visibility + local transfer feedback.
3. Phase 3: Pivot summary actions + notice audit + `SchedulerView` + docs/tests.
4. Rollback: feature flags unnecessary; revert view-level `isPhoneLayout` branches.

## Open Questions

- **Q1:** Should `SchedulePendingView` light-toolbar (workflow/urgency circles) get a mobile overflow menu in this change, or a follow-up? *(Default: follow-up unless trivial reuse of pending-toolbar pattern.)*
- **Q2:** Delete `AdaptiveRow.vue` in Phase 1 or leave unused until confirmed no consumer? *(Default: delete if zero imports after ScheduleView uses ListMobileCard.)*
