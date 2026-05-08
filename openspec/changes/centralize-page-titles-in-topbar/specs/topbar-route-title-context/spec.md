## ADDED Requirements

### Requirement: Topbar eyebrow shows app name
The authenticated app shell topbar SHALL display `common.appName` on the eyebrow line.

#### Scenario: Eyebrow text is app name
- **WHEN** an authenticated user views any route rendered inside the app shell
- **THEN** the topbar eyebrow line shows the localized `common.appName` label (currently `JB2026`)

### Requirement: Topbar title reflects active route name
The authenticated app shell topbar SHALL display the current route title on the main title line, resolved from route metadata title key through i18n.

#### Scenario: Topbar title changes on navigation
- **WHEN** the user navigates from one authenticated route to another route with a different metadata title key
- **THEN** the topbar main title updates to the destination route's localized label

#### Scenario: Topbar title changes on locale switch
- **WHEN** the user changes locale while staying on the same authenticated route
- **THEN** the topbar main title updates to the route label in the selected locale

#### Scenario: Topbar title falls back when title key missing
- **WHEN** an authenticated route does not provide a metadata title key
- **THEN** the topbar main title falls back to `common.appName`

### Requirement: Route labels use canonical namespace
Route identity labels used for topbar and document title SHALL use canonical `routes.*` translation keys from route metadata.

#### Scenario: View-local title keys do not override route identity
- **WHEN** a view includes local feature title keys (for example `jobOrder.*.title`)
- **THEN** topbar and document title continue using route metadata labels (`routes.*`)

### Requirement: Authenticated views avoid duplicate page banners
Route-backed authenticated views SHALL NOT render an additional top-level page title/subtitle intro banner if the topbar already provides page context.

Definition: a duplicate page banner means the top-of-page intro title/subtitle block in the main view card that repeats route identity.

#### Scenario: Job list no longer shows duplicate local page banner
- **WHEN** the user opens the Job List route
- **THEN** page content begins with functional controls/content and does not render a duplicate local title/subtitle banner block

#### Scenario: Section headings remain intact
- **WHEN** a view contains non-page section headings inside content cards (for example dashboard chart titles)
- **THEN** those section headings remain and are not removed as duplicate page banners
