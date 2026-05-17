## Context

The `ClientApp` is a Vue.js frontend supported by a .NET backend. It already exposes invoice-related fields in job/order/report workflows, but those values come from a legacy-oriented billing path and do not represent a modern external billing authority. Invoice Ninja provides a robust API for invoicing, but calling it directly from the frontend would expose sensitive API keys and encounter CORS issues. The current system already has a structured service layer in the frontend and a clear API boundary in `JB2026.Api`, which should own the entire integration.

## Goals / Non-Goals

**Goals:**
- Implement a secure, headless integration with the Invoice Ninja API.
- Replace the legacy invoice path by making Invoice Ninja the billing source of truth for invoices.
- Enable customer synchronization, invoice generation, and invoice status retrieval within `ClientApp`.
- Create a dedicated "Billing" section in the navigation menu while preserving key entry points in existing Customer and Job Order workflows.
- Ensure durable mapping between JB2026 customers/jobs and Invoice Ninja clients/invoices.
- Centralize API authentication and request policy in the backend to protect credentials.

**Non-Goals:**
- Migrating existing financial data from other systems.
- Customizing the Invoice Ninja PDF templates via `ClientApp` (this will be done in the Invoice Ninja dashboard).
- Implementing a full accounting suite (e.g., general ledger) within `ClientApp`.
- Rebuilding all Invoice Ninja editing capabilities inside JB2026 in the first delivery.
- Integrating the SML invoice track (`SmlInvoiceListView`, `SmlInvoiceStatsView`). SML invoices are scheduled for retirement and will not be migrated to Invoice Ninja.

## Decisions

### 1. Backend Proxy Architecture
- **Decision**: Implement Invoice Ninja integration endpoints inside `JB2026.Api`, exposed under a billing-focused API surface.
- **Rationale**: Security and consistency. API keys must never reach the client browser, and the existing frontend already routes business operations through `JB2026.Api`.
- **Alternatives**: Direct frontend-to-API calls (Rejected: Security risk), using a standalone middleware product (Rejected: unnecessary complexity for current scope).

### 2. Data Mapping Strategy
- **Decision**: Use idempotent customer synchronization backed by locally persisted external identifiers.
- **Rationale**: A pure "check then create" flow is prone to duplicate clients. JB2026 should persist `invoiceNinjaClientId`, sync timestamps, and sync status in local metadata so the backend can reliably reconcile customers.
- **Alternatives**: Email-only matching (Rejected: the current customer model does not reliably carry email as the primary key), real-time webhooks (Rejected: higher complexity to implement and maintain).

### 3. UI Integration Pattern
- **Decision**: Add a new navigation group "Billing" for billing-centric screens, but keep operational billing actions in Customer and Job Order flows.
- **Rationale**: Invoice browsing and connection status fit a dedicated billing area, while customer sync and invoice generation are operational actions that should remain close to the screens where users already work.
- **Alternatives**: Billing-only navigation with no in-context actions (Rejected: increases workflow friction), billing-only tabs in existing views (Rejected: overloads already dense screens).

### 4. Authentication Flow
- **Decision**: Use a service-account API key stored in backend secure configuration and accessed only through typed backend services.
- **Rationale**: Simplifies the integration as the application acts on behalf of the organization rather than individual users.

### 5. Billing Source Of Truth
- **Decision**: Treat Invoice Ninja as the source of truth for invoice number, status, due date, totals, and payment state.
- **Rationale**: The change is intended to replace the legacy invoice path, not coexist indefinitely with competing invoice authorities.
- **Alternatives**: Dual-write or dual-read between legacy invoice storage and Invoice Ninja (Rejected: high divergence risk and unclear ownership).

### 6. Read Model Strategy
- **Decision**: Maintain a local billing read model for Job Orders and related screens, containing the external invoice identifier plus the minimal invoice summary needed by existing UI surfaces.
- **Rationale**: Several current screens already display invoice number and amount. Pulling those values live from Invoice Ninja on every render would be brittle and would make rollout harder.
- **Alternatives**: Live-only reads from Invoice Ninja (Rejected: operational fragility and performance risk), leaving existing invoice fields untouched (Rejected: inconsistent replacement story).

### 7. Sync Gate Strategy
- **Decision**: When the user initiates "Generate Invoice" for a job order, the backend should auto-sync the associated customer to Invoice Ninja if a persisted `invoiceNinjaClientId` is not yet present, before proceeding to invoice creation — transparent to the user.
- **Rationale**: Requiring users to pre-sync customers via `AdminCustomerView` before generating invoices from the job list introduces disruptive context switching. The sync operation is fast and idempotent; absorbing it into the invoice generation flow produces a smoother experience without hiding important state.
- **Alternatives**: Hard gate requiring explicit prior sync (Rejected: workflow friction, forces navigation away from the job list), presenting a sync CTA modal on mismatch (Acceptable as a fallback if auto-sync fails, not as the primary path).
- **Note**: If auto-sync fails (e.g., unmappable customer record), the backend must return a clear error that the frontend surfaces in-context on the job row rather than as a generic error page.

### 8. Invoice Preview Before Creation
- **Decision**: Before committing an invoice to Invoice Ninja, display a confirmation dialog showing the resolved customer, line items derived from the Job Order, and the calculated total. The user must explicitly confirm before the invoice is created.
- **Rationale**: Invoice Ninja invoices created and then voided produce audit trail noise. The line-item mapping from a Job Order is deterministic but not always obvious to the user; a preview step builds trust and catches mapping errors before they become billing errors. This is especially important during rollout while users are learning the new flow.
- **Alternatives**: Direct creation with a post-creation void option (Rejected: audit trail pollution), editable preview with full line-item editing (Deferred: increases scope; the confirmation dialog may expose a link to the invoice in Invoice Ninja for detailed editing after creation).

## Data Contract Direction

- Customer linkage should use a persisted `invoiceNinjaClientId` as the primary external mapping key.
- `customerCode` can be used as a secondary reconciliation key when external mapping is missing.
- Job Orders should persist an external invoice identifier and last-known invoice summary needed by the UI.
- The first version should define one deterministic line-item mapping path from Job Orders to Invoice Ninja invoices, rather than mixing multiple generation strategies.

## Delivery Shape

Recommended first delivery:

1. Connectivity and health check
2. Customer sync with stored external client IDs
3. Generate invoice from Job Order
4. Retrieve invoice summary/status for Billing and existing job/order views
5. Defer broad external invoice editing and destructive actions unless business-critical

## Risks / Trade-offs

- **[Risk] API Rate Limiting**: Invoice Ninja may rate-limit requests if the volume is high. $\rightarrow$ **Mitigation**: Use typed `HttpClient` configuration, retries with backoff for safe reads, and local cached billing summaries where appropriate.
- **[Risk] Duplicate Clients**: Incomplete matching rules may create duplicate customers in Invoice Ninja. $\rightarrow$ **Mitigation**: Persist external IDs locally and make sync idempotent.
- **[Risk] Transition Inconsistency**: Existing job/order/report screens may continue showing stale legacy invoice values. $\rightarrow$ **Mitigation**: Add an explicit transition task to move those screens onto the new billing read model.
- **[Risk] Data Divergence**: A customer might be edited in Invoice Ninja directly, leading to a mismatch with `ClientApp`. $\rightarrow$ **Mitigation**: Implement a manual refresh/reconcile action in the Customer view.
- **[Risk] Dependency on External Service**: If Invoice Ninja is down, billing functions fail. $\rightarrow$ **Mitigation**: Implement graceful error handling, connectivity checks, and "Service Unavailable" UI states in the frontend.
- **[Risk] Customer Model Field Gaps**: `AdminCustomerRecord` does not carry email, currency, or payment terms — fields that Invoice Ninja client profiles typically require. First-delivery sync will map available fields (`customerName`, `customerCode`, `billTo`) and leave gaps with IN defaults. $\rightarrow$ **Mitigation**: Document the mapping contract explicitly in task 2.1 and create a follow-up task to assess which missing fields should be added to the customer data model for a richer IN client profile.