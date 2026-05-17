## Why

The application currently exposes invoice references and amounts in several legacy-oriented workflows, but it does not have a modern external billing system that is the clear source of truth. Integrating Invoice Ninja allows the business to replace the legacy invoice path with a supported invoicing platform while keeping billing workflows inside the application.

## What Changes

- **Headless Invoicing Replacement**: Introduce a bridge between the application and the Invoice Ninja API, with Invoice Ninja becoming the billing authority for invoices.
- **Customer Synchronization**: Allow customer records to be synchronized to Invoice Ninja and persist the external client identifier inside JB2026 metadata.
- **Invoice Generation & Read Models**: Generate Invoice Ninja invoices from Job Orders and expose a local billing read model so existing job/order/report views can show Invoice Ninja-backed invoice status.
- **Billing Navigation**: Add a dedicated navigation group for billing screens while also integrating billing actions into existing Customer and Job Order workflows.
- **Backend Proxy**: Introduce secure API endpoints in `JB2026.Api` to handle Invoice Ninja authentication and prevent API key exposure on the frontend.
- **AdminCustomerView Fix**: `AdminCustomerView.vue` currently renders supplier data due to a copy-paste error (imports `AdminSupplierRecordDialog`, calls `getAdminSuppliers`). This view must be corrected to use the already-built `AdminCustomerRecordDialog` and customer service before any billing affordances can be layered onto it.
- **Enhanced Job List Billing UX**: Transition `invoiceRef`/`invoiceAmount` columns in `JobListView` to Invoice Ninja-backed values expressed as a color-coded status chip (Draft / Sent / Viewed / Paid / Overdue) with an inline "Generate Invoice" action for uninvoiced orders, keeping the user on the job list throughout the billing preparation workflow.

## Capabilities

### New Capabilities
- `invoice-ninja-auth`: Secure authentication and token management between the application backend and Invoice Ninja API.
- `customer-billing-sync`: Logic for mapping and synchronizing ClientApp customers to Invoice Ninja clients.
- `invoice-lifecycle-mgmt`: CRUD operations for creating, updating, and retrieving invoices and invoice items.
- `billing-ui-navigation`: A new navigation structure and set of views for managing billing functions.

### Modified Capabilities
- `admin-customer-mgmt`: Modified to fix the broken `AdminCustomerView` (currently renders supplier data) and include triggers for billing synchronization.
- `job-order-mgmt`: Modified to generate invoices and surface Invoice Ninja billing status in existing job/order workflows.
- `reporting-read-models`: Modified so invoice number, amount, and status can transition from legacy values to Invoice Ninja-backed values.

## Impact

- **Frontend**: New billing views and services in `JB2026.WebApp/ClientApp/src/`, plus updates to existing Customer and Job Order screens.
- **Backend**: New proxy/controllers/services in `JB2026.Api` to act as the Invoice Ninja integration layer.
- **Infrastructure**: Requirement for a hosted Invoice Ninja instance, API configuration, and connectivity validation.
- **Data Model**: Customer and job/order metadata will need persistent external identifiers and sync state to avoid duplicate Invoice Ninja records. The current `AdminCustomerRecord` model is also missing fields that are standard in an Invoice Ninja client profile (email, currency, payment terms); the first delivery should document which fields are being mapped with their available values and flag the gaps for a follow-up task.
- **Security**: Introduction of new API keys and secure storage for billing credentials.

## Transition Notes

- Invoice Ninja is intended to replace the legacy invoice path as the billing source of truth.
- Existing invoice fields that appear in job/order/report screens must be transitioned to Invoice Ninja-backed values or clearly marked as temporary legacy data during rollout.
- The first delivery should prioritize connection, customer sync, invoice generation, and invoice status retrieval before expanding to broader invoice editing workflows.
- Fixing `AdminCustomerView` is a prerequisite for all customer-side billing affordances; it must be resolved before or as part of the customer synchronization task group.
- The SML invoice track (`SmlInvoiceListView`, `SmlInvoiceStatsView`, `/api/v2/sml/invoice-*`) is explicitly out of scope. SML invoices will be retired as part of the final SML feature removal and are not subject to Invoice Ninja migration.