# Design: Port Legacy Menu to ClientApp

## Architecture Overview

The solution enhances the existing AppSidebar component (JB2026.WebApp/ClientApp/src/components/layout/AppSidebar.vue) to support hierarchical, expandable menu items using Vuetify's v-list and v-list-group components.

## Menu Structure

```
Dashboard (direct route)
Jobs (direct route)
Quotations (direct route)
Editor (direct route)
Scheduler (direct route)

─────── LEGACY CORE MODULES ─────────

Job Order (expandable)
  ├─ Quotation List
  ├─ Order List
  ├─ Job List
  ├─ Job Stats
  └─ Job Schedule (expandable)
      ├─ Pending
      ├─ Schedule
      ├─ Completed
      └─ Packing (OnAir)
  └─ SML (expandable)
      ├─ RTF List
      ├─ Invoice List
      ├─ RTF Stats
      └─ Invoice Stats
  └─ Reports (expandable)
      ├─ Exceptional Report

Stock (expandable)
  └─ Product

Admin (expandable)
  ├─ Workflow
  ├─ Workflow Forms
  ├─ Order Type
  ├─ User
  ├─ Customer
  ├─ Supplier
  ├─ Quotation (expandable)
  │   ├─ Item Group
  │   └─ Item
  └─ FCM Console

Settings (expandable)
  └─ System Parameters
```

## Components & Data Structure

### 1. MenuItem Interface
```typescript
interface MenuItem {
  title: string
  icon?: string
  to?: string // If provided, this is a link item
  children?: MenuItem[] // If provided, this is expandable
}
```

### 2. Changes to AppSidebar.vue

**Data Structure:**
- Create a `legacyMenuItems` computed property that returns the hierarchical menu structure
- Use v-list-group for expandable items
- Use v-list-item for leaf items

**Template Logic:**
- Render top-level items (Dashboard, Jobs, etc.) as direct v-list-items
- Use v-list-group for "Legacy Core Modules" section with collapsible categories
- Recursively render nested menu items using a helper component or template

**Icon Strategy:**
- Parent items use category icons (mdi-folder, mdi-briefcase, etc.)
- Leaf items inherit parent icon or use specific icons
- Icons already maintained in i18n strings

### 3. Routing Conventions

New routes follow RESTful patterns:
- `/job-order/*` — Job Order subsections
- `/stock/*` — Stock subsections
- `/admin/*` — Admin subsections
- `/settings/*` — Settings subsections

Specific leaves:
- `/job-order/quotation-list`
- `/job-order/order-list`
- `/job-order/job-list`
- `/job-order/job-stats`
- `/job-order/schedule/pending`
- `/job-order/schedule/completed`
- `/job-order/schedule/scheduled`
- `/job-order/schedule/packing`
- `/job-order/sml/rtf-list`
- `/job-order/sml/invoice-list`
- `/job-order/sml/rtf-stats`
- `/job-order/sml/invoice-stats`
- `/job-order/reports/exceptional`
- `/stock/product`
- `/admin/workflow`
- `/admin/workflow-forms`
- `/admin/order-type`
- `/admin/user`
- `/admin/customer`
- `/admin/supplier`
- `/admin/quotation/item-group`
- `/admin/quotation/item`
- `/admin/fcm-console`
- `/settings/system-parameters`

### 4. i18n Updates

Extend i18n translations to include:
- All new route names from legacy menu
- Category labels
- Parent/child menu item labels

## Implementation Notes

- **Reusability:** Consider extracting menu rendering into a shared component for future enhancements
- **Lazy Loading:** Routes can be lazy-loaded as separate views/pages as needed
- **State Management:** No global state needed initially; sidebar state managed locally in AppSidebar
- **Responsive Design:** Existing Vuetify responsive classes should work; test on mobile
- **Accessibility:** Ensure ARIA labels are added to expandable groups

## Testing Strategy

- Manual navigation testing through each menu item
- Verify routing to correct pages
- Check icon display and alignment
- Test expand/collapse animation and state retention
- Validate responsive behavior on mobile devices

## Contributor Guide

To add a new legacy menu entry in ClientApp:

1. Add the menu node in `JB2026.WebApp/ClientApp/src/components/layout/menuHelper.ts`.
  - Use `children` for expandable groups.
  - Use `to` for clickable leaf routes.
2. Add route labels in all locale route bundles:
  - `src/i18n/locales/en/routes.ts`
  - `src/i18n/locales/zhHans/routes.ts`
  - `src/i18n/locales/zhHant/routes.ts`
3. Register the route in `src/router/index.ts`.
  - For not-yet-migrated pages, map to `LegacyMenuPlaceholderView.vue`.
4. Run `pnpm run typecheck` from `JB2026.WebApp/ClientApp`.

## Implementation Notes and Known Issues

- `MenuItemRenderer.vue` renders recursively and now applies depth-based indentation and ARIA labels.
- Existing project lint configuration reports many formatting warnings outside this change scope; no new compile errors were introduced by this menu migration.
- Placeholder pages intentionally avoid business behavior until each corresponding legacy module is ported.
- Performance risk is low for current menu size because rendering complexity is linear to item count and data is static per locale.
