"# Billing UI Navigation Spec

## Overview
This capability introduces a dedicated navigation structure and user interface for managing billing and invoicing functions within the ClientApp.

## Requirements
- **Navigation Group**: A new top-level navigation group named \"Billing\" must be added to the main menu.
- **Views**:
    - **Invoices View**: A list view showing all invoices, their status, and total amounts.
    - **Invoice Detail View**: A detailed view for a single invoice with the ability to edit line items.
    - **Billing Settings View**: A view to manage the connection to Invoice Ninja (e.g., status check, manual sync trigger).
- **Integration Points**:
    - **Customer View**: Add a \"Billing Profile\" link that redirects to the Invoice Detail view for that customer.
    - **Job Order View**: Add a \"Generate Invoice\" button that opens a pre-filled invoice creation dialog.
- **UX/UI**:
    - Use existing `VDataTable` components for invoice lists.
    - Implement loading states and error notifications using `VSnackbar` for API failures.

## Acceptance Criteria
- [ ] The \"Billing\" menu group is visible and accessible in the main navigation.
- [ ] Users can navigate to the Invoices list and view individual invoice details.
- [ ] The \"Generate Invoice\" button in Job Order view correctly triggers the invoice creation flow.
- [ ] UI handles \"Service Unavailable\" states gracefully when the backend proxy fails."