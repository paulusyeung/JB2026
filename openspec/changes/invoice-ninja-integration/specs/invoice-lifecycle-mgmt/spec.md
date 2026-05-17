"# Invoice Lifecycle Management Spec

## Overview
This capability provides the core CRUD operations for managing invoices and their associated line items via the Invoice Ninja API.

## Requirements
- **Invoice Creation**:
    - Ability to create a new invoice for a synchronized client.
    - Support for adding multiple line items (products/services) to an invoice.
    - Ability to set invoice dates and due dates.
- **Invoice Retrieval**:
    - Fetch a list of invoices with basic status (Draft, Sent, Paid, Overdue).
    - Fetch detailed view of a single invoice including line items.
- **Invoice Updates**:
    - Ability to modify line items or update the status of an invoice.
- **Invoice Deletion**:
    - Ability to void or delete draft invoices.
- **Data Mapping**:
    - `JobOrder.OrderTitle` $\rightarrow$ `invoice.client_invoice_number` (or custom reference).
    - `JobOrder.Qty` $\rightarrow$ `invoice_item.qty`.

## Acceptance Criteria
- [ ] A user can create a new invoice for an existing client from the ClientApp.
- [ ] A user can view a list of all invoices and their current payment status.
- [ ] A user can update an invoice's line items and save changes.
- [ ] A user can void a draft invoice."