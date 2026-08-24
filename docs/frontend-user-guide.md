# JB2026 Frontend — User Guide

> Audience: end users and administrators of the JB2026 web application.
> Scope: the browser frontend (Vue 3 SPA) and how it connects to the backend
> services, Invoice Ninja (billing), and Twenty CRM.
>
> Anchors such as `<!-- SCREENSHOT: ... -->` mark places where a screen
> capture should be inserted later.

---

## 1. Application Overview

JB2026 is a business management web application used to run job/order
operations, inventory, billing, and customer-relationship workflows. It is the
modernized successor to the legacy JB2015 system and is organized around a set
of functional modules accessible from the left-hand navigation sidebar.

Key characteristics:

- **Single-page application (SPA).** Pages load inside one shell; navigation is
  near-instant and the sidebar, top bar, and content area stay consistent.
- **Role-based navigation.** The menu you see is filtered by your permissions
  (see *RBAC* section). Users with broader roles see more modules.
- **Multi-language UI.** The interface supports English and Traditional/Simplified
  Chinese, selected in the top bar.
- **Responsive layout.** On desktop the sidebar is a fixed rail; on phones/tablets
  it collapses into a slide-in drawer toggled from the top bar.
- **Backend-backed.** All data, printing, and integrations are performed by the
  JB2026 API; the frontend is a client of that API.

<!-- SCREENSHOT: Application shell (sidebar + top bar + content) -->

---

## 2. Navigation and Layout

The screen is divided into three areas:

1. **Sidebar (left).** Brand lockup ("JB2026") followed by the menu. Menu groups
   expand to reveal their items. Collapsed mode shows icons only (hover for
   tooltips).
2. **Top bar.** Search, language switcher, notifications, and the user menu
   (profile, sign out). On mobile it holds the sidebar toggle.
3. **Content area.** The selected page. List pages share a common toolbar and
   pagination pattern (see *List Views* section).

Your effective permissions determine which menu items appear. A menu group is
shown only when at least one of its items is permitted.

<!-- SCREENSHOT: Top bar with language switcher and user menu -->

---

## 3. Dashboard

Two dashboard entries sit at the top of the sidebar above the module groups.

### 3.1 Dashboard

The primary landing page after sign-in (for users with access). Provides an
at-a-glance overview of operational metrics, recent activity, and quick links
into the modules.

<!-- SCREENSHOT: Dashboard -->

### 3.2 Dashboard (Operator)

A streamlined operator-oriented view focused on day-to-day execution tasks
(schedule, pending jobs, quick actions) rather than management metrics. This is
the landing page for operator-level roles.

<!-- SCREENSHOT: Dashboard (Operator) -->

---

## 4. Job Order

The Job Order module covers the order-to-production lifecycle: orders, jobs,
scheduling, and operational reporting.

### 4.1 Order List

A searchable, paginated list of customer orders. Use it to find an order, open
its detail, and drill into the associated job(s). Supports the standard list
toolbar (search, column visibility, sort, bulk-select, create, export/print).

<!-- SCREENSHOT: Job Order > Order List -->

### 4.2 Job List

Lists production jobs with status, allowing you to open job details, print job
documents (e.g., job sheets), and perform bulk actions on selected jobs.

<!-- SCREENSHOT: Job Order > Job List -->

### 4.3 Job Stats

Aggregated statistics and charts about jobs (volumes, status distribution,
throughput) to support supervision and planning.

<!-- SCREENSHOT: Job Order > Job Stats -->

### 4.4 Job Schedule

A schedule breakdown by workflow stage. Each sub-item is a focused view of jobs
in that state:

- **Pending** — jobs awaiting scheduling/start.
- **Schedule** — jobs that are scheduled.
- **Completed** — finished jobs.
- **Packing** — jobs in the packing stage.
- **Packing (OnAir)** — packing jobs flagged as "on air" / in active dispatch.

<!-- SCREENSHOT: Job Order > Job Schedule (e.g. Pending) -->

### 4.5 Reports

- **Exceptional Report** — a report of exceptional/exception cases requiring
  attention (e.g., anomalies, overrides, or flagged records).

<!-- SCREENSHOT: Reports > Exceptional Report -->

---

## 5. Stock

### 5.1 Product

The product/inventory catalog. Search, view, create, and maintain product
records used across orders, jobs, and billing.

<!-- SCREENSHOT: Stock > Product -->

---

## 6. Billing

The Billing module connects JB2026 to Invoice Ninja for client, invoice, and
statement management. (See the dedicated *Invoice Ninja Integration* section for
the sync relationship.)

### 6.1 Invoices

Lists invoices sourced from Invoice Ninja. From here you can create a new
invoice (which is created in Invoice Ninja), view invoice detail, **Mark Sent**,
**Download** the invoice PDF or delivery note, and **Upload to DMS** (Paperless-ngx).
A "last synced" timestamp is shown per invoice.

<!-- SCREENSHOT: Billing > Invoices -->

### 6.2 Statement

Generate a client statement. Select a client, choose **Statement**, then
**Proceed**; JB2026 asks Invoice Ninja to produce a PDF statement that is
streamed back to the browser.

<!-- SCREENSHOT: Billing > Statement -->

### 6.3 Invoice Stats

Charts and aggregates over invoices (counts, amounts, status trends) for
financial oversight.

<!-- SCREENSHOT: Billing > Invoice Stats -->

### 6.4 Clients

Lists billing clients (from Invoice Ninja). Shows sync status for each client
and provides access to client records. Use the client record dialog to
**Migrate** (create) or **Update** a client in Invoice Ninja when needed.

<!-- SCREENSHOT: Billing > Clients -->

---

## 7. CRM

The CRM module integrates with Twenty CRM for companies, people, opportunities,
tasks, and a 360° customer view. (See the dedicated *Twenty CRM Integration*
section for the sync relationship.)

### 7.1 Visualization

Visual maps/graphs of CRM relationships (companies, people, links) to help
explore the account landscape.

<!-- SCREENSHOT: CRM > Visualization -->

### 7.2 Customer 360

A read-only 360° view of a customer, aggregating job orders, invoices,
opportunities, tasks, files, emails, and timeline in tabbed panels. This screen
is for viewing, not for linking/syncing (linking is done from **Companies**).

<!-- SCREENSHOT: CRM > Customer 360 -->

### 7.3 Companies

The companies list in Twenty CRM. Create a **New Company** (pushed to Twenty
CRM), edit existing companies, and — when creating — optionally **Migrate
Customer** to link a JB2026 customer (which flags it as synced). Synced
companies show a green link icon.

<!-- SCREENSHOT: CRM > Companies -->

### 7.4 People

People (contacts) in Twenty CRM, linked to companies. Create, edit, and view
people records.

<!-- SCREENSHOT: CRM > People -->

### 7.5 Opportunities

Sales opportunities in Twenty CRM. Create and manage opportunities, linking
them to companies/contacts/owners.

<!-- SCREENSHOT: CRM > Opportunities -->

### 7.6 Tasks

CRM tasks in Twenty CRM. Create and manage tasks, assigning them to workspace
members and linking them to companies/people/opportunities.

<!-- SCREENSHOT: CRM > Tasks -->

### 7.7 Staff Members

Lists workspace members from Twenty CRM (used as account owners/assignees) and
lets you run an email existence check ("Sync to CRM") for a staff member. This is
a read-only check, not a data push.

<!-- SCREENSHOT: CRM > Staff Members -->

---

## 8. Admin

Administrative configuration of reference data and users. (RBAC itself is
managed under *Settings*, not here.)

### 8.1 Order Type

Maintain the catalog of order types used to classify orders/jobs.

<!-- SCREENSHOT: Admin > Order Type -->

### 8.2 User

Manage user accounts and assign each user a **role** (Guest, Operator,
Supervisor, Manager, Admin). This screen sets role membership only; individual
permission overrides are edited in the RBAC Editor.

<!-- SCREENSHOT: Admin > User -->

### 8.3 Customer

Manage JB2026 customer records. This screen is also the primary place to
**Sync Billing** — push a selected customer (with a Customer Code) to Invoice
Ninja as a client. Sync status (synced / error) is shown inline.

<!-- SCREENSHOT: Admin > Customer -->

### 8.4 Supplier

Maintain the supplier catalog used in procurement and inventory workflows.

<!-- SCREENSHOT: Admin > Supplier -->

---

## 9. Settings

System-level configuration and access control.

### 9.1 System Monitor

A read-only dashboard showing integration health and settings for connected
systems (CRM, DMS, Email, Billing/Invoice Ninja). Use it to confirm that
external integrations are configured and reachable.

<!-- SCREENSHOT: Settings > System Monitor -->

### 9.2 System Parameters

Editable system parameters that tune application behavior (e.g., operational
thresholds and integration options).

<!-- SCREENSHOT: Settings > System Parameters -->

### 9.3 RBAC Editor

The Role-Based Access Control editor. From here an administrator edits **Group
RBAC** (per role) and **individual User RBAC**, controlling which menu/route
each role or user can access. See the *RBAC* section for the precedence rules.

<!-- SCREENSHOT: Settings > RBAC Editor -->

---

## 10. List Views: Toolbar and Pagination

Most modules present data in a shared **list/table** pattern (built on Vuetify
data tables). The following toolbar buttons and pagination control appear
consistently across list screens (exact buttons vary slightly by screen).

### 10.1 Toolbar buttons

| Button | Icon | Purpose |
| --- | --- | --- |
| Refresh | `mdi-refresh` | Reload the current list from the server. |
| Columns | `mdi-view-column` | Dropdown checklist to show/hide table columns; your layout is remembered. |
| Sort | `mdi-sort` | Menu to choose the sort field and ascending/descending order. |
| Bulk-select | `mdi-checkbox-multiple-marked-outline` | Toggle selection checkboxes on each row for bulk actions. |
| View mode | `mdi-eye-outline` | Switch between table view and card (mobile-friendly) view. |
| Create / New | `mdi-plus-circle-outline` (or `mdi-file-plus`) | Open the create dialog for a new record (e.g., New Invoice, New Company). |
| Print | `mdi-printer` | Print the current list/view via the browser. |
| Export / Download | `mdi-file-delimited-outline` or `mdi-download-circle-outline` | Export the list to CSV, or (billing) download invoice PDF / delivery note / upload to DMS. |
| Bulk Delete | `mdi-delete` | Appears only when rows are selected via bulk-select; deletes the selection. |
| More | `mdi-dots-horizontal` | On small screens, collapses the above actions into an overflow menu. |

Column visibility, sort order, view mode, and rows-per-page are persisted per
user (local storage + server preferences), so your layout is restored next
visit.

### 10.2 Row interactions

- **Click a row** (or its ID/link) to open the detail/edit dialog.
- **Sort** by clicking a sortable column header (where supported).
- **Select** rows with the checkboxes (after enabling bulk-select) for bulk
  actions.

### 10.3 Pagination control

List footers provide standard pagination:

- **Page navigation:** first, previous, next, and last page buttons, plus the
  current page indicator.
- **Total count:** shown as "X–Y of Z" (e.g., "1–10 of 243").
- **Rows per page:** a selector with options `10, 15, 20, 25, 50, All`
  (`All` fetches everything). The chosen page size is persisted.

<!-- SCREENSHOT: Example list view toolbar + pagination footer -->

---

## 11. Invoice Ninja Integration

### 11.1 Relationship

JB2026 integrates with **Invoice Ninja** as its **billing/invoincing system of
record**. The division of responsibility is:

- **JB2026 → Invoice Ninja (push):** Customers are synced into Invoice Ninja as
  **clients**. When you create or update a client, JB2026 calls the Invoice
  Ninja API (`POST /clients` to create, `PUT /clients/{id}` to update) and
  reconciles by Customer Code when no stored client ID exists.
- **Invoice Ninja → JB2026 (pull, read-only):** Invoices, invoice status, and
  client statements are **read from** Invoice Ninja. JB2026 stores only
  reference IDs (`externalClientId`, `externalInvoiceId`); the canonical data
  lives in Invoice Ninja.

There is **no background/automatic sync** — every sync is triggered on demand by
a user action.

**Field mapping** (configurable custom fields in Invoice Ninja):

| JB2026 | Invoice Ninja |
| --- | --- |
| Customer name | Client `name` |
| Customer code | Client `id_number` |
| Bill To | Client custom field (e.g. `custom_value1`) |
| Ship To | Client custom field |
| Group | Client `group_settings_id` |
| Job No. (on invoice) | Invoice custom field |
| P.O. No. (line item) | Product custom field |

### 11.2 Required configuration

The integration must be configured on the backend (`Billing:InvoiceNinja`
section / `Billing__BaseUrl` and an API key). If not configured, billing
screens show "not configured" errors and the **Billing Settings → Check
Connectivity** button reports Not Connected.

### 11.3 How to sync a customer to Invoice Ninja

1. Open **Admin → Customer** and select a customer that has a **Customer Code**.
2. Click **Sync Billing** (cloud-upload icon, or the overflow menu). A spinner
   shows progress.
3. On success, the connection icon turns colored and the status reads
   "synced"; the Invoice Ninja client ID and sync time are stored on the
   customer. On failure, a red chip shows the error.
4. (Alternative) When creating an invoice for a not-yet-synced client, use
   **New Client → Migrate** in the invoice editor to push the client first.

### 11.4 Invoices and statements

- **Invoices:** create in **Billing → Invoices → New Invoice**; line items can
  be autofilled from JB2026 job numbers. Then **Mark Sent**, **Download PDF**,
  or **Upload to DMS**.
- **Statement:** in **Billing → Statement**, select a client, click
  **Statement → Proceed** to generate and preview the Invoice Ninja PDF.
- Verify health any time via **Billing Settings → Check Connectivity**.

<!-- SCREENSHOT: Admin > Customer sync status chip -->
<!-- SCREENSHOT: Billing > Invoices list -->

---

## 12. Twenty CRM Integration

### 12.1 Relationship

JB2026 integrates with **Twenty CRM** for customer-relationship data. The flow
is **user-triggered** (no automatic background sync):

- **JB2026 → Twenty CRM (push):** Companies, People, Opportunities, and Tasks
  created/edited in JB2026 are pushed to Twenty CRM via its GraphQL API.
- **Twenty CRM → JB2026 (pull, read-only):** JB2026 reads companies, people,
  opportunities, tasks, timelines, and workspace members to display them.
- The only data persisted on the JB2026 side is a **`SyncedToCRM` flag** on the
  JB2026 **customer** record. It is a *link indicator* (set when a Twenty
  company is created from a JB2026 customer), not a two-way sync engine.

### 12.2 Required configuration

Configure `TwentyCrm:ApiKey` and `TwentyCrm:BaseUrl` (env `TwentyCrm__ApiKey`,
`TwentyCrm__BaseUrl`). When unconfigured, every CRM screen simply shows empty
lists (no error), because the service short-circuits.

### 12.3 How to sync / link to Twenty CRM

1. Ensure `TwentyCrm:ApiKey` and `TwentyCrm:BaseUrl` are set.
2. Open **CRM → Companies** and click **New Company**.
3. (Optional) In the dialog, use **Migrate Customer** to pick the matching
   JB2026 customer. Saving pushes the company to Twenty CRM and, if a customer
   was chosen, flags that customer `SyncedToCRM = "1"`.
4. Edit existing companies with the pencil action; changes update Twenty CRM.
5. Push **People / Opportunities / Tasks** via their respective **New** buttons,
   linking them to a company within the dialog.
6. Verify the green "Synced to Twenty CRM" link icon on the company in the
   Companies list.

> Note: **CRM → Customer 360** is a read-only viewer (no sync action).
> **CRM → Staff Members → Sync to CRM** performs only an email existence check
> against Twenty CRM workspace members, not a data push.

<!-- SCREENSHOT: CRM > Companies with "Migrate Customer" dialog -->
<!-- SCREENSHOT: CRM > Companies synced link icon -->

---

## 13. RBAC: Groups vs. Individual Users

JB2026 controls access with **Role-Based Access Control** at two levels:

- **Group RBAC** — permissions defined for a whole **role** (Guest, Operator,
  Supervisor, Manager, Admin). Stored once per role and applied to everyone in
  that role.
- **Individual User RBAC** — permissions defined for a **single user**,
  overriding the role baseline.

Both levels describe **route/page access** (which menu items and pages a role or
user may open); they do not govern feature toggles or data scopes.

### 13.1 How the two relate (precedence)

Effective permissions follow a **"first non-empty wins"** rule:

1. If the user has **any** stored individual RBAC entries, **those entries
   fully replace the group/role permissions**. The role's settings are ignored
   entirely for that user.
2. If the user has **no** individual entries, the **role's Group RBAC** applies.
3. If neither is set, access is **fail-open** — everything is visible.

Important: because a user's own entries *replace* the group set wholesale (they
are not merged key-by-key), a group **deny** does **not** block a user who has
their own entries — only the user's explicit values matter. Any route not present
in the user's set defaults to visible.

### 13.2 Managing RBAC

Open **Settings → RBAC Editor**:

- Pick a **role** → **Edit Group RBAC** to set page access for everyone in that
  role (a tree of menu routes with per-page checkboxes, plus Toggle All / Reset
  / Save).
- Pick a **user** (scoped to a role) → **Edit User RBAC** to override access for
  that individual. When a user has no entries yet, the editor seeds it from the
  group's values as a starting point (the backend still applies the
  user-wins rule above).

User **role assignment** (which group a user belongs to) is done in **Admin →
User**, not in the RBAC Editor.

The resulting effective permission set drives:
- which **sidebar menu items** are visible,
- which **route** you can navigate to (others are blocked),
- your **landing dashboard** after sign-in.

<!-- SCREENSHOT: Settings > RBAC Editor (role tree) -->
<!-- SCREENSHOT: Settings > RBAC Editor (user override) -->
