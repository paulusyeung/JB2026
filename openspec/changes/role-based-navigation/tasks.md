# Tasks: Role-Based User Navigation/Menu Access Control

## Preparation
- [ ] Review `src/components/layout/menuHelper.ts` and `src/components/layout/AppSidebar.vue`. <!-- id: 1 -->

## Implementation

### Phase 1: Update Types and Logic
- [x] Add `roles?: string[]` to `MenuItem` type in `src/components/layout/menuHelper.ts`. <!-- id: 2 -->
- [x] Implement `filterMenuItems` recursive function in `src/components/layout/menuHelper.ts`. <!-- id: 3 -->
- [x] Update `buildLegacyMenuItems` to accept `userRole` and return filtered items. <!-- id: 4 -->

### Phase 2: Define Roles in Menu
- [x] Assign `roles: ['Admin']` to the "Admin" section in `buildLegacyMenuItems`. <!-- id: 5 -->
- [x] Assign `roles: ['Admin']` or appropriate roles to "Settings". <!-- id: 6 -->
- [ ] (Optional) Refine roles for specific sub-items if needed (e.g., Reports). <!-- id: 7 -->

### Phase 3: Update Components
- [x] Update `src/components/layout/AppSidebar.vue` to reactively pass the user's role to `buildLegacyMenuItems`. <!-- id: 8 -->

### Phase 4: Route Guarding (Recommended)
- [x] Update `src/router/index.ts` to implement a global `beforeEach` guard that checks `meta.roles` for protected routes. <!-- id: 9 -->
- [x] Add `meta: { roles: ['Admin'] }` to admin routes in `src/router/index.ts`. <!-- id: 10 -->

### Bug Fixes
- [x] Fix sign-out navigation to redirect to login page.
- [x] Fix role comparison to be case-insensitive and handle numeric roles (e.g., role '4' for Admin).

## Verification
- [ ] Log in as an Admin and verify all menu items are visible. <!-- id: 11 -->
- [ ] Log in as a regular User and verify "Admin" and "Settings" are hidden. <!-- id: 12 -->
- [ ] Verify that a direct navigation to a hidden route is blocked (if Phase 4 is implemented). <!-- id: 13 -->
