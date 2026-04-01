# Port Legacy Menu to ClientApp

## Objective
Implement a comprehensive hierarchical menu structure in the ClientApp's sidebar navigation that mirrors the legacy application's menu organization, providing users with organized access to all major functional areas during the coexistence phase.

## Problem
The current ClientApp sidebar presents a flat list of navigation items organized into two simple groups (core modules and derived areas). This lacks the hierarchical structure and logical grouping that users are familiar with from the legacy application, making navigation less intuitive and potentially causing user confusion during the transition.

## Solution
Expand AppSidebar to support expandable/collapsible menu categories with the exact hierarchy from the legacy app:
- **Job Order** (parent section with subsections: Quotation List, Order List, Job List, Job Stats, Job Schedule, SML, Reports)
- **Stock** (Product management)
- **Admin** (Workflow, Workflow Forms, Order Type, User, Customer, Supplier, Quotation Item management, FCM Console)
- **Settings** (System Parameters)

This creates a familiar, intuitive navigation experience that aligns with user mental models from the legacy system.

## Impact
- Enhanced usability during tool transition
- Reduced user confusion and support burden
- Organized access to all major business functions
- Maintains clear separation between new features and legacy functionality

## Non-Goals
- Implementing actual functionality for menu items (only routing)
- Redesigning the legacy menu structure
- Adding new business features
- Changing top-level navigation bars or headers
