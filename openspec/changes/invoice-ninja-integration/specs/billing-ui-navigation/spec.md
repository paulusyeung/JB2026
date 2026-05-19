# Billing UI Navigation Spec

## Overview

This capability introduces a dedicated navigation structure and user interface for managing billing and invoicing functions within the ClientApp, aligned with first-release backend capabilities (read + create, not full IN editing).

## Requirements

### Navigation group

- A new top-level navigation group named **Billing** MUST be added to the main menu.

### Views

- **Invoices View**: List invoices with status and total amounts (`VDataTable`).
- **Invoice Detail View**: Read-only summary in v1 (number, status, dates, totals, line summaries). Line-item editing is deferred; link to Invoice Ninja for detailed edits when available.
- **Billing Settings View**: Connection status, health check, and documentation of which custom-field env keys are configured (ops visibility; values not secret).

### Integration points

- **Customer View** (`AdminCustomerView`, after supplier copy-paste fix): "Sync with Billing" action and billing sync status indicator.
- **Job Order / Job List**: "Generate Invoice" opens preview confirmation dialog, then creates invoice in-place without leaving the list when possible.

### Preview dialog (invoice generation)

- MUST show: customer name, Bill To, Ship To (resolved), Job No., line items (description, qty), P.O.No. per line when mapped, total.
- SHOULD show Unit when a value exists; otherwise omit or show "—".
- MUST require explicit user confirmation before create.

### UX

- Use existing `VDataTable` for invoice lists.
- Use `VSnackbar` (or equivalent) for API failures.
- Show graceful "Service Unavailable" when the billing proxy or Invoice Ninja is unreachable.

## Acceptance Criteria

- [ ] The Billing menu group is visible and navigable.
- [ ] Users can open the invoice list and a read-only invoice detail view.
- [ ] Generate Invoice from Job List/Order flow shows preview with Job No. and P.O.No. before commit.
- [ ] UI handles billing service unavailability without exposing API keys or raw IN errors.
