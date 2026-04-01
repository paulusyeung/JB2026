# Task List: Port Legacy Menu to ClientApp

## Overview
Implement hierarchical menu structure in AppSidebar supporting nested categories with expand/collapse functionality. Total estimated effort: 8-10 hours.

## Phase 1: Preparation & Structure

- [x] **T1.1** Create MenuHelper.ts utility with MenuItem interface and menu data constants
  - Define MenuItem interface with title, icon, to, children properties
  - Export menuItemsTree constant with full legacy menu hierarchy
  - Add helper functions for recursive rendering/traversal

- [x] **T1.2** Update i18n configuration with all menu item labels
  - Add translations for all 30+ menu items (Job Order, Quotation List, Schedule, etc.)
  - Add section headers (Job Order, Stock, Admin, Settings)
  - Ensure consistency with existing naming conventions
  - Estimated: 1-2 hours

- [x] **T1.3** Plan routing structure and create placeholder pages for legacy routes
  - Create page components for all legacy sections (/job-order, /admin, /stock, /settings)
  - Create placeholder leaf pages for each menu item destination
  - Register routes in router configuration
  - Estimated: 1.5-2 hours

## Phase 2: Component Implementation

- [x] **T2.1** Refactor AppSidebar.vue to support nested menu groups
  - Convert legacyCoreItems to use v-list-group with nested v-list-item
  - Add expand/collapse state management
  - Ensure existing new items (Dashboard, Jobs, etc.) remain at top
  - Estimated: 2-3 hours

- [x] **T2.2** Implement MenuItemRenderer.vue helper component for recursive rendering
  - Accept MenuItem array as prop
  - Render v-list-item for items with 'to' property
  - Render v-list-group for items with 'children' property
  - Handle icon display and styling
  - Estimated: 1.5-2 hours

- [x] **T2.3** Style and test expandable groups
  - Verify Vuetify defaults handle spacing/hierarchy  
  - Test expand/collapse animations
  - Ensure icon rotation on parent expansion
  - Mobile responsive testing
  - Estimated: 1 hour

## Phase 3: Integration & Testing

- [x] **T3.1** Update legacy route handlers to call new ClientApp pages
  - Verify all 30+ menu items route correctly
  - Handle missing implementations with placeholder pages
  - Test navigation flow for each branch
  - Estimated: 1-1.5 hours

- [ ] **T3.2** Manual testing and QA
  - Test each menu item navigation
  - Verify expand/collapse state per category
  - Test on desktop and mobile breakpoints
  - Verify icon alignment and styling
  - Check keyboard navigation (Tab, Enter)
  - Estimated: 1-1.5 hours

- [x] **T3.3** Performance review and optimization
  - Check sidebar rendering performance with full menu
  - Optimize if menu generation is slow
  - Estimated: 0.5-1 hour

## Phase 4: Documentation & Handoff

- [x] **T4.1** Document menu structure and routing for future contributors
  - Create MENU_STRUCTURE.md with visual guide
  - Document how to add new menu items
  - Update component README

- [x] **T4.2** Create implementation notes and known issues
  - Document any workarounds or edge cases
  - Note mobile behavior considerations

## Acceptance Criteria

- All legacy menu categories visible in sidebar with correct hierarchy
- All menu items route to appropriate pages/sections
- Expand/collapse works smoothly for multi-level categories
- Icons display correctly for all items
- Responsive design works on mobile devices
- No console errors or warnings
- All existing navigation still functions
- Menu structure matches legacy app organization exactly
