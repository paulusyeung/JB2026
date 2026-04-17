# Proposal: Role-Based User Navigation/Menu Access Control

## Objective
Implement a role-based access control (RBAC) mechanism for the navigation menu in the ClientApp. This will ensure that menu items and categories are only visible to users with the appropriate permissions, enhancing security and simplifying the user interface.

## Problem
Currently, the ClientApp navigation menu displays all available modules and features to every authenticated user, regardless of their role. This leads to:
1.  **Security Risks**: Users can see navigation paths to administrative or sensitive areas, even if the backend might block the final request.
2.  **User Experience (UX) Clutter**: Users are presented with many menu items that are not relevant to their job function, making it harder to find what they need.
3.  **Inconsistency**: The frontend doesn't reflect the user's actual permissions, creating a disjointed experience.

## Solution
We will implement a client-side role filtering mechanism within the `menuHelper.ts` and `AppSidebar.vue`.

Key components:
- **Role Definition**: Use the existing `role` field in `UserProfile` (e.g., "Admin", "User", "Manager").
- **Menu Item Metadata**: Extend the `MenuItem` type in `menuHelper.ts` to include an optional `roles` array.
- **Filtering Logic**: Update `buildLegacyMenuItems` to accept the current user's role and filter items accordingly.
- **Recursive Filtering**: Ensure that parent categories are hidden if all their children are restricted for the current user.
- **Default Access**: Define a default behavior (e.g., if no roles are specified, the item is public or requires basic authentication).

## Impact
- **Improved Security**: Reduces the attack surface by hiding sensitive entry points.
- **Better UX**: Users only see what they are authorized to use.
- **Maintainability**: Centralized menu definition with role assignments makes it easy to update permissions.

## Non-Goals
- Implementing a full-blown backend RBAC (this proposal assumes the backend already has some form of validation).
- Adding complex permission-based logic (e.g., "can-edit" vs "can-view") within the menu itself.
- Changing the visual style of the menu.
