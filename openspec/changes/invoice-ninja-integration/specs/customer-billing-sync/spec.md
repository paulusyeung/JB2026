"# Customer Billing Sync Spec

## Overview
This capability manages the mapping and synchronization of customers between the ClientApp database and the Invoice Ninja client list.

## Requirements
- **Unique Identifier**: The system must use a consistent identifier (e.g., Email or a custom field) to link ClientApp customers to Invoice Ninja clients.
- **Lazy Synchronization**: 
    - When a customer is accessed in the Billing view, the system must check if they exist in Invoice Ninja.
    - If they do not exist, a new client must be created automatically using the ClientApp data.
- **Field Mapping**:
    - `CustomerName` $\rightarrow$ `client_name`
    - `CustomerEmail` $\rightarrow$ `email`
    - `CustomerAddress` $\rightarrow$ `address`
- **Update Trigger**: Changes to customer contact details in the Admin view should optionally trigger an update to the Invoice Ninja client record.

## Acceptance Criteria
- [ ] Creating a customer in ClientApp allows for successful synchronization to Invoice Ninja.
- [ ] Updating a customer's email in ClientApp updates the corresponding client in Invoice Ninja.
- [ ] No duplicate clients are created for the same customer."