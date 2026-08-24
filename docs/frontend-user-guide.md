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

A breakdown of jobs by where they are in the production flow. It has five
sub-views, each a focused list of jobs in one state:

- **Pending** — jobs waiting to be scheduled or started.
- **Schedule** — jobs that have been placed on a machine's schedule.
- **Completed** — finished jobs (read-only history).
- **Packing** — jobs in the packing stage.
- **Packing (OnAir)** — packing jobs flagged as actively being dispatched.

Across these views you will see small **colored circle buttons** (the "status
lights") and **bell buttons**. These are quick tags you click to show where a
job stands:

- The circles come in four colors — red, yellow, green, blue — representing the
  stages of a workflow step (for example, not-started, in-progress, done, or a
  special state). Your administrator defines what each color means for each
  step. They are grouped as **@1**, **@2**, and **@3** (the three workflow
  steps a job moves through).
- The **red bell** and **yellow bell** mark a job as *urgent* (red = critical,
  yellow = high priority) so it stands out on everyone's screen.

> Most of these quick-tag buttons only work after you have **selected one or
> more jobs** (turn on the bulk-select checkbox, then tick the jobs). The
> buttons stay greyed out until something is selected.

Clicking a job's **order number** (or its row/card) anywhere in these views
opens that job's detail form, where you can edit it, change any workflow step,
add remarks, attach files, and print.

#### 4.4.1 Pending

A list of jobs waiting to be scheduled.

- **Search box + quick-filter dropdown + Search / Refresh** — find jobs by
  order number or customer, or pick a preset filter (e.g. "this week") and
  refresh the list.
- **Standard toolbar** — show/hide **Columns**, **Sort** the list, turn on
  **bulk-select** (checkbox), switch **View** (table or card), and **Export**
  the list.
- **@1 status lights** (selection required) — set the first workflow step for
  the selected jobs.
- **@2 status lights** (selection required) — set the second workflow step for
  the selected jobs.
- **Red / yellow urgency bells** (selection required) — flag the selected jobs
  as critical / high priority.

<!-- SCREENSHOT: Job Order > Job Schedule > Pending -->

#### 4.4.2 Schedule (Scheduled)

A two-panel planner: **Available** jobs on the left, **Scheduled** jobs on the
right. You place jobs from Available onto a machine's schedule, arrange their
running order, then save.

**How to schedule jobs (walkthrough):**

1. The left panel lists jobs not yet scheduled. Tick the jobs you want to
   schedule (or turn on the bulk-select checkbox to grab several at once).
2. Press one of the **machine buttons M1–M5** in the middle column to move the
   selected jobs into the right panel and assign them to that machine.
3. In the right panel, use the **action column** to put the jobs in running
   order (top / up / down / bottom) and, if needed, reassign a job to a
   different machine with its M1–M5 buttons.
4. As work happens, tag progress with the **@1 / @2 status lights** and the
   **urgency bells**, or press the **green check** to mark jobs **Completed**.
5. Press **Save** to commit the schedule. Nothing is stored on the server until
   you save — closing the screen without saving discards your changes.

> **The machine filter does two jobs.** The **All / M1–M5** toggle at the top
> both *filters* the right panel (so you only see one machine's scheduled jobs)
> **and** acts as the target for the **→→ (move all)** button: jobs moved with
> "move all" are assigned to whatever machine is selected in that filter (or M1
> when "All" is chosen).

> The **@1 / @2 circles and red / yellow bells** here are the same status lights
> described in §4.4.1 (Pending) — only here they are applied to jobs that are
> already on the schedule.

- **Transfer buttons** (between the two panels):
  - **Machine buttons M1–M5** — move the selected Available jobs into Scheduled
    and assign them to that machine.
  - **→→ (move all)** — move every Available job into Scheduled, assigned to the
    machine currently chosen in the machine filter (or M1 if "All").
  - **← (send back)** — move the selected Scheduled jobs back to Available.
  - **←← (send all back)** — move every Scheduled job back to Available.
  - **Minus button** — discard your unsaved changes and reload the lists.
- **Scheduled-panel "light toolbar"** (selection required): @1 circles set the
  first step, @2 circles set the second step, and the red / yellow bells mark
  urgency — all applied to the checked Scheduled jobs.
- **Action column** (on each Scheduled job):
  - **Move to top / up / down / bottom** — reorder the selected Scheduled jobs
    to set the running sequence.
  - **Machine buttons M1–M5** — change which machine the selected Scheduled
    jobs are assigned to.
  - **Green check** — mark the selected Scheduled jobs as **Completed** (they
    leave the schedule and are saved as finished).
- **Top toolbar** — **Save** (commit the schedule you built), a **machine
  filter** toggle (All / M1–M5), and **Refresh**.
- **On a phone** — an **Available** button opens the available list as a sheet,
  and an **Update Status** button opens a sheet to set the workflow lights and
  urgency.

<!-- SCREENSHOT: Job Order > Job Schedule > Schedule (Scheduled) -->

#### 4.4.3 Packing

A list of jobs in the packing stage.

- **Search box + quick-filter dropdown + Search / Refresh** — locate packing
  jobs and refresh.
- **Standard toolbar** — **Columns**, **Sort** (ascending / descending),
  **bulk-select** checkbox, **View** mode (Detail table or Card), and
  **Export to CSV** (download the current list).
- **@1 status lights** (selection required) — set the first workflow step for
  the selected packing jobs. (The @2 and @3 steps show as read-only tags here;
  open the job to change them.)
- Clicking a row / order number / card opens the job's detail form.

<!-- SCREENSHOT: Job Order > Job Schedule > Packing -->

#### 4.4.4 Packing (OnAir)

A two-list dispatcher for packing that is actively being sent out.

- **Two lists** — **Available** (not yet on-air) on the left and **Selected**
  (the on-air packing queue) on the right.
- **Move buttons** (between the lists):
  - **→** move the selected Available jobs into Selected.
  - **→→** move all Available jobs into Selected.
  - **←** move the selected Selected jobs back to Available.
  - **←←** move all Selected jobs back to Available.
- **Reorder buttons** (on the Selected list) — move the selected jobs to the
  **top / up / down / bottom** to arrange the packing sequence.
- **Green check** — mark the selected jobs as **Completed**.
- **Save** — commit the on-air packing queue.
- Clicking an order number opens its detail form.

<!-- SCREENSHOT: Job Order > Job Schedule > Packing (OnAir) -->

#### 4.4.5 Completed

A read-only list of finished jobs, with the standard list toolbar (search,
show/hide columns, sort, bulk-select, export) for reviewing history.

<!-- SCREENSHOT: Job Order > Job Schedule > Completed -->

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

A read-only list of **billing clients sourced from Invoice Ninja**. Use it to
browse clients, see their outstanding balance, and open a client's record to
view or link it to a JB2026 customer.

- **Filter bar:** a lookup box plus **Search** (and Enter-to-search).
- **Toolbar:** Columns, Sort, Checkbox (bulk-select), View mode (Detail table /
  Card), and a primary **+ New Client** button. On phones these collapse into a
  "More" menu.
- **Columns:** an icon, a row number `#`, **Client Name** (a link), **Client
  Code**, **External Client ID** (the Invoice Ninja ID), and **Outstanding
  Balance** (formatted as a currency amount).
- **Card / mobile view:** shows the name, code, external ID, and balance.
- **Open a record:** click the client name (or a row) to open the **client
  record dialog**. The screen tries to find the matching JB2026 customer (by
  Invoice Ninja client ID, then by name) and, if found, lets you **Migrate**
  (create) or **Update** that client in Invoice Ninja. After a save the list
  reloads and a success message appears.
- **+ New Client:** opens the **New Client** dialog to push a JB2026 customer
  into Invoice Ninja as a client (detailed below).

#### Inside the "+ New Client" dialog

Despite the label, **"+ New Client" does not let you type a free-form client**.
It opens the *New Client* dialog whose real purpose is to **migrate an existing
JB2026 customer into Invoice Ninja as a client** — the client's data always
originates from a JB2026 customer record.

How it works:

1. **Migrate Customer panel (top).** An autocomplete labeled *"Select a JB2026
   customer to migrate"*. Typing searches JB2026 customers (debounced); each
   option shows a connection icon that is colored when that customer is already
   linked to an Invoice Ninja client.
2. **Read-only customer summary.** Once a customer is selected, the dialog loads
   its record and shows *Customer Name*, *Customer Code*, *Group* (resolved from
   the billing group list), *Bill To*, and *Ship To* addresses. These fields are
   **read-only** here.
3. **Migration Readiness checklist.** Verifies three required fields exist on the
   JB2026 customer: **Customer name**, **Customer code**, and **Bill To address**.
   Each shows a green check (present) or red alert (missing).
4. **Already-synced behavior.** If the chosen customer is already synced (status
   "success" with an Invoice Ninja client ID), an info notice appears and the
   action button switches from **Migrate** (cloud-upload icon) to **Update**
   (cloud-sync icon) — updating the existing Invoice Ninja client rather than
   creating a new one.
5. **Edit Customer.** If required data is missing, a pencil **Edit Customer**
   button opens the Admin Customer record dialog so you can complete the
   customer, then return. All data fixes happen there, not in this dialog.
6. **Migrate / Update** (primary button) is enabled only when a customer is
   selected **and** all readiness checks pass. It calls the sync service, which
   **creates** (`POST /clients`) or **updates** (`PUT /clients/{id}`) the client
   in Invoice Ninja, mapping name → `name`, code → `id_number`, Bill To / Ship To
   → custom fields, and group → `group_settings_id`. On success the list reloads
   and a "Customer synced to Invoice Ninja successfully." message appears.

> Important: a client that is **not** backed by a JB2026 customer cannot be
> created from this screen — the integration is customer-driven. To simply view
> an already-synced client, open it from the list instead (which uses the same
> dialog in *Client Record* mode, pre-filled from the matched customer).

<!-- SCREENSHOT: Billing > Clients (list with toolbar) -->
<!-- SCREENSHOT: Billing > Clients (New Client / Migrate Customer dialog) -->
<!-- SCREENSHOT: Billing > Clients (Migrate readiness checklist + Edit Customer) -->

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

The list of **companies in Twenty CRM**. This is the primary screen for pushing
companies to Twenty CRM and for linking them to JB2026 customers.

- **Filter bar:** lookup box plus **Search** and **Refresh**.
- **Toolbar:** Columns, Sort, Checkbox (bulk-select), View mode (Detail table /
  Card), and a primary **+ New Company** button.
- **Columns:** a **Synced** indicator, **Name** (link), **Account Owner**,
  **Domain**, **Address**, **People** (chips), **Opportunities** (chips),
  **Created/Updated** On & By.
- **Synced indicator:** a green `mdi-link-variant` icon appears when the company
  matches a JB2026 customer flagged `SyncedToCRM` (i.e., it was created from or
  linked to a JB2026 customer).
- **Open / edit:** click the company name (or card) to open the **company record
  dialog** for viewing and editing in Twenty CRM. Within that dialog you can
  **Migrate Customer** to pick a matching JB2026 customer; saving pushes the
  company to Twenty CRM and links the customer.
- **+ New Company:** opens the **company record dialog** in *new* mode (detailed
  below). Unlike the billing client dialog, the fields here are **editable** and
  you may create a standalone company **or** link a JB2026 customer.

#### Inside the "+ New Company" dialog

The dialog (titled *Edit Company* for both new and existing records) is fully
editable. For a new company it offers an optional **Migrate Customer** step, then
editable company fields and relationship pickers.

1. **Migrate Customer panel (only in new mode).** An autocomplete *"Select
   JB2026 Customer"* lists JB2026 customers available to migrate. Selecting one
   **prefills** the company **Name** (from the customer name) and the **Address**
   (parsed from the customer's Bill To; if no country is found it defaults to
   *Hong Kong*). The option shows a connection icon colored when the customer is
   already billing-synced. If you skip this, you create a plain Twenty CRM
   company with no JB2026 link.
2. **Company fields (editable):**
   - **Name** — required (`Company name is required`), max 256 chars.
   - **Domain** — optional, max 256 chars.
   - **Address** — Address Line 1/2, City, State, Postcode, and **Country**
     (a dropdown of ~60 countries; if the current value isn't in the list it is
     kept and shown first).
   - **Account Owner** — a dropdown of Twenty CRM workspace members
     (`getCrmMembers`); leave empty or pick an owner.
3. **People.** A chip list of linked people plus an autocomplete to add existing
   people (from the CRM people catalog). A `+` button opens the **New Person**
   dialog (which reloads the people list on save) so you can create a person and
   link it in one step. Chips are removable.
4. **Opportunities.** Same pattern as People: chips for linked opportunities, an
   autocomplete to add existing opportunities, and a `+` button to create a new
   opportunity (pre-linked to this company via `initial-company-id`) and link it.
5. **Save / Save & Close.** Two buttons: **Save** creates the company and keeps
   the dialog open; **Save & Close** creates then closes. The create payload
   includes `customerId` = the selected Migrate Customer (when set), so the
   backend flags that JB2026 customer `SyncedToCRM = "1"`. If no customer was
   selected, a standalone company is created. Editing an existing company instead
   calls **update** with name, domain, address, owner, and the people/opportunity
   ID lists.

> Note: the company dialog is the opposite of the billing client dialog — here
> the data is editable inline (no separate customer-edit step is required), and a
> company can exist without a JB2026 customer.

<!-- SCREENSHOT: CRM > Companies (list with synced link icon) -->
<!-- SCREENSHOT: CRM > Companies (New Company dialog: Migrate Customer + fields) -->
<!-- SCREENSHOT: CRM > Companies (People/Opportunities chips + add) -->

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

This screen reuses the **JB2026 user list** (the same data as *Admin → User*)
but is surfaced under CRM so you can check each user against Twenty CRM. It is
**read-oriented** — there is no user-creation here (create/edit users in
*Admin → User*).

- **Filter bar:** lookup box plus **Search** and **Refresh**.
- **Toolbar:** Columns, Sort, Checkbox (bulk-select), View mode (Detail table /
  Card), and a primary **Sync to CRM** button (cloud-sync icon).
- **Sync to CRM** is available once you select exactly one person who has an
  email address (otherwise the button stays greyed out). The same button also
  appears inside a person's **record dialog**, so you can use it from either
  place.

#### About the "Sync to CRM" button

**Where to find it.** On the *CRM → Staff Members* screen, the **Sync to CRM**
button sits in the toolbar — it becomes clickable once you have selected a single
person with an email address. You will also see the same button when you open a
person's record. Either way opens the same small confirmation window.

**What it is meant to do.** Its job is to check whether the selected staff
member's email address is already known to Twenty CRM — in other words, "is this
person already in our CRM?" It does **not** copy or change any information; it
simply tells you whether the email is already there. (Despite the "Sync" name, it
does not send or create anything.)

**A note about the current version.** In this version, the button is not yet
connected to Twenty CRM. When you open the confirmation window you will see the
person's email and the question *"Sync user <email> to Twenty CRM?"*, but the
**Proceed** button is disabled and does nothing — you can only close the window.
So using it right now has no effect. This is expected to become active in a
future update; until then, please treat it as a placeholder.

**Setup needed (for administrators).** For the check to work once it is enabled,
the connection to Twenty CRM must first be set up by an administrator. If that
connection is missing, the check cannot run.
- **Columns / cards:** an icon that is color-coded — **pink** (`mdi-account-sync`)
  when the user is CRM-synced, **amber** (`mdi-account-key`) when the user is a
  primary record, otherwise **grey** (`mdi-account`); plus **Username** (link),
  **Alias**, **Email**, **Role**, and **Created/Modified** On & By.
- **Open a record:** click the username (or a row) to open the **staff member
  record dialog** (view/edit the user; deletion is also available there).

<!-- SCREENSHOT: CRM > Staff Members > Staff Members (list with Sync to CRM) -->
<!-- SCREENSHOT: CRM > Staff Members (Sync to CRM dialog) -->

---

## 8. Admin

Administrative configuration of reference data and users. (RBAC itself is
managed under *Settings*, not here.)

### 8.1 Order Type

Despite the name, this screen is **not** a customer-facing catalog editor. It is
the **Order Type Workflow Assignment** tool: it defines which **workflows** are
available for each order type, and in what order they appear when a job of that
type is created.

JB2026 has three fixed order types (each shown with its own icon/color across
the app): **Printing** (0), **Digital Printing** (1), and **Others** (2).

How to use it:

1. Pick an **Order Type** from the dropdown at the top. On open, the first order
   type is selected automatically and its current mapping is loaded.
2. Two side-by-side lists appear:
   - **Available Workflow** — workflows not currently assigned to this order type.
   - **Selected Workflow** — workflows assigned to this order type (the ones
     that will show on a job of this type).
3. Move workflows between the lists with the arrow buttons between the panels:
   - `▶` / `⏩` move the selected (or all) workflows from Available → Selected.
   - `◀` / `⏪` move them back from Selected → Available.
4. Within **Selected Workflow**, reorder with the up/down arrows on the right:
   `⏫` / `▲` move an item up (top), `▼` / `⏬` move it down (bottom). The order
   here is the order users will see when creating a job of this type.
5. Click **Save** to persist the mapping. You must have at least one workflow
   selected (otherwise a "Please select at least one workflow" warning appears).
   Success/failure are shown as banners; the list reloads after a successful save.

Effect elsewhere: when a user creates a job in the **Job Order** module and
chooses an order type, the workflows/attributes assigned here determine which
workflow steps and fields appear on that job form.

> Note: the order-type *definitions* (Printing / Digital Printing / Others) are
> fixed in code; this screen only manages the workflow assignment per type, not
> the list of order types itself.

<!-- SCREENSHOT: Admin > Order Type (dual-list workflow assignment) -->

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

The access-control editor. From here an administrator sets which pages each
role, or each individual person, can open. See the *RBAC* section for how the
two work together.

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

JB2026 works together with **Invoice Ninja**, which it uses to handle invoicing
and billing. Here is how the two share the work:

- **From JB2026 to Invoice Ninja:** Your customers are sent across to Invoice
  Ninja as *clients*. When you add or update a client in JB2026, the same client
  is created or updated in Invoice Ninja (matched by the customer code when one
  already exists there).
- **From Invoice Ninja to JB2026:** Invoices, their status, and customer
  statements are read from Invoice Ninja and shown in JB2026. JB2026 keeps only
  a link to each invoice or client rather than a full copy of the details.

Nothing happens automatically in the background — you start every update yourself
by clicking a button.

**What gets copied across** (the billing administrator sets up matching fields
in Invoice Ninja):

| In JB2026 | In Invoice Ninja (the client) |
| --- | --- |
| Customer name | Client name |
| Customer code | Client reference number |
| Bill To | A custom field you set up (such as "Bill To") |
| Ship To | A custom field you set up |
| Group | Client group |
| Job No. (on an invoice) | A custom field on the invoice |
| P.O. No. (on a line item) | A custom field on the product |

### 11.2 Required setup

The link to Invoice Ninja must be set up by an administrator — they provide the
Invoice Ninja address and an access key. If it isn't set up, the billing screens
will show a "not configured" message, and the **Billing Settings → Check
Connectivity** button will report "Not Connected".

### 11.3 How to link a customer to Invoice Ninja

1. Open **Admin → Customer** and select a customer that has a **Customer Code**.
2. Click **Sync Billing** (cloud-upload icon, or the overflow menu). A spinner
   shows progress.
3. On success, the connection icon turns colored and the status reads "synced";
   JB2026 notes that the customer is now linked and records the time. On failure,
   a red message shows the error.
4. (Alternative) When creating an invoice for a client that isn't linked yet, use
   **New Client → Migrate** in the invoice editor to link the client first.

### 11.4 Invoices and statements

- **Invoices:** create one in **Billing → Invoices → New Invoice**; the line
  items can be filled in automatically from JB2026 job numbers. Then **Mark
  Sent**, **Download PDF**, or **Upload to DMS** (the document storage system).
- **Statement:** in **Billing → Statement**, select a client, then click
  **Statement → Proceed** to create and preview the PDF from Invoice Ninja.
- You can check the connection at any time via **Billing Settings → Check
  Connectivity**.

<!-- SCREENSHOT: Admin > Customer sync status chip -->
<!-- SCREENSHOT: Billing > Invoices list -->

---

## 12. Twenty CRM Integration

### 12.1 Relationship

JB2026 works together with **Twenty CRM** to keep customer-relationship
information. You start every update yourself — there is no automatic background
sync.

- **From JB2026 to Twenty CRM:** When you add or change a company, person,
  opportunity, or task in JB2026, that information is sent to Twenty CRM.
- **From Twenty CRM to JB2026:** JB2026 reads companies, people, opportunities,
  tasks, timelines, and member lists from Twenty CRM and displays them.
- When you link a JB2026 customer to a company in Twenty CRM, JB2026 simply
  marks that customer as "linked". This is only a note showing the two are
  connected — it does not keep the two systems constantly in sync.

### 12.2 Required setup

An administrator must set up the connection to Twenty CRM (an address and an
access key). If that is missing, the CRM screens will simply appear empty, with
no error message.

### 12.3 How to link to Twenty CRM

1. Make sure the Twenty CRM connection is set up (see above).
2. Open **CRM → Companies** and click **New Company**.
3. (Optional) In the dialog, use **Migrate Customer** to pick the matching
   JB2026 customer. Saving sends the company to Twenty CRM and, if you chose a
   customer, marks that customer as linked.
4. To change an existing company, use the pencil action; your changes are sent to
   Twenty CRM.
5. Add **People / Opportunities / Tasks** using their **New** buttons, linking
   them to a company within the dialog.
6. You'll see a green "Synced to Twenty CRM" link icon on the company in the
   Companies list once it's linked.

> Note: **CRM → Customer 360** is for looking only — you can't link or sync from
> there. The **Staff Members → Sync to CRM** button (described earlier) only
> checks whether an email is already in Twenty CRM; it does not send any data.

<!-- SCREENSHOT: CRM > Companies with "Migrate Customer" dialog -->
<!-- SCREENSHOT: CRM > Companies synced link icon -->

---

## 13. RBAC: Roles vs. Individual People

JB2026 controls who can see what using **role-based access** — that is, what a
person can open is decided by their role. There are two layers:

- **Role access** — settings applied to a whole role (Guest, Operator,
  Supervisor, Manager, Admin). Everyone with that role gets the same access.
- **Individual access** — settings applied to one specific person, which can
  differ from their role.

Both layers decide **which pages and menu items a person can open**. They do not
control individual buttons or which data someone sees.

### 13.1 How the two layers work together

1. If a person has their own individual settings, those settings are used on
   their own and completely replace the role's settings.
2. If a person has no individual settings, the role's settings are used.
3. If neither is set, everything is shown (the safest default).

Worth remembering: because a person's own settings replace the role's settings
entirely (rather than mixing them together), a restriction set at the role level
will not block someone who has their own settings — only that person's own
choices matter. Any page not specifically mentioned in their settings is shown by
default.

### 13.2 Managing access

Open **Settings → RBAC Editor**:

- Choose a **role**, then **Edit Group Access** to set which pages everyone in
  that role can open (a list of menu items with checkboxes, plus Select All /
  Reset / Save).
- Choose a **person** (filtered by role), then **Edit User Access** to set
  different access for that one person. If the person has no settings yet, the
  editor starts them off with a copy of the role's settings (but the rule above
  still applies — the person's own choices win).

A person's **role** (which group they belong to) is set in **Admin → User**, not
here.

The final access settings decide:
- which **menu items** appear in the sidebar,
- which **pages** you can open (others are blocked),
- which **dashboard** you land on after signing in.

<!-- SCREENSHOT: Settings > RBAC Editor (role tree) -->
<!-- SCREENSHOT: Settings > RBAC Editor (user override) -->
