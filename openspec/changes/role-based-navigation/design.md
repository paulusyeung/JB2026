# Design: Role-Based User Navigation/Menu Access Control

## Overview
The role-based navigation will be implemented by extending the existing menu structure and adding a filtering layer that checks the current user's role against required roles for each menu item.

## Architecture

### 1. Data Model Extension
We will update `src/components/layout/menuHelper.ts` to include a `roles` property in the `MenuItem` type.

```typescript
export type MenuItem = {
  title: string
  icon?: string
  to?: string
  children?: MenuItem[]
  roles?: string[] // Optional: If specified, only these roles can see this item
}
```

### 2. State Management
The `useSessionStore` already contains the `profile` with the `role` property. We will use this in the navigation components.

### 3. Filtering Logic
We will implement a `filterMenuItems` function in `menuHelper.ts` that:
- Check if an item has a `roles` property.
- If it does, check if the current user's role is in that list.
- If it has `children`, recursively filter the children.
- Only return the item if it passed its own role check AND (it has no children OR at least one child remains after filtering).

### 4. Integration
`AppSidebar.vue` will pass the user's role from `sessionStore` to `buildLegacyMenuItems`.

## Components

### menuHelper.ts
- Add `roles` to `MenuItem`.
- Update `buildLegacyMenuItems` to accept `userRole: string | undefined`.
- Implement `filterMenuItems(items: MenuItem[], role: string | undefined): MenuItem[]`.
- Assign roles to administrative and sensitive items (e.g., `'Admin'` for Admin section, `'Settings'` for Settings).

### AppSidebar.vue
- Access `sessionStore.profile.role`.
- Pass role to `buildLegacyMenuItems`.

### Router Integration (Optional but Recommended)
- Update `src/router/index.ts` to check roles in `beforeEach` guard for routes associated with restricted menu items to prevent direct navigation.

## Security Considerations
- This is a UI-level control. Backend API endpoints MUST still perform their own role/permission checks.
- If the user role is `undefined` or null (not fully loaded yet), we should show a minimal safe set of menu items (e.g., only Dashboard).
