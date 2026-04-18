# Design: Modern Dashboard Redesign

## UI Architecture
The dashboard will transition to a dynamic, modular grid layout.

### 1. Enhanced Metrics Row
- **KpiCard v2**: Updates to include `trend` and `percentage` props.
- Visual cues (colors/icons) for positive/negative trends.

### 2. Main Analytics Area
- **Interactive Charts**: Chart.js implementation with toggleable views (Daily/Weekly/Monthly).
- **Global Filters**: A persistent filter bar for date ranges and status.

### 3. Sidebar / Secondary Panels
- **Recent Activity Timeline**: A `v-timeline` component showing latest updates from the audit log or store history.
- **Detailed Slice Health**: Expanded list with more metadata (creation date, usage stats) and toggles.

## Implementation Details

### Components
- **TrendIndicator.vue**: New helper component for KPI cards.
- **ActivityTimeline.vue**: New component for the activity feed.
- **DashboardFilters.vue**: New component for interactive state management.

### Data Flow
- Unified `reload` function with better error handling.
- Optimized computed properties for trend calculation (comparing current period vs. previous).

### Styling
- Responsive layout using `v-container`, `v-row`, and `v-col` with breakpoints.
- Consistent color palettes for light/dark mode using theme variables.
