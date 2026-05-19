# Customer Billing Sync Spec

## Overview

This capability manages idempotent mapping and synchronization of JB2026 customers to Invoice Ninja clients, including configured custom fields that match the organization's Invoice Ninja company setup.

## Requirements

### Identifier and reconciliation

- The system MUST persist `invoiceNinjaClientId` in JB2026 customer metadata after a successful sync.
- The system MUST use `invoiceNinjaClientId` as the primary key for subsequent sync and invoice-generation flows.
- The system MAY use `customerCode` as a secondary reconciliation key when no persisted external ID exists.
- The system MUST NOT rely on email as a link key (`AdminCustomerRecord` does not carry email).

### Sync behavior

- Customer sync MUST be idempotent: repeated sync for the same customer updates the same Invoice Ninja client when `invoiceNinjaClientId` is known.
- When generating an invoice for a job whose customer lacks `invoiceNinjaClientId`, the backend MUST auto-sync that customer before invoice creation (transparent to the user when successful).
- If auto-sync fails, the backend MUST return a problem response the frontend can show in-context (e.g., on the job row).

### Native field mapping

| JB2026 field | Invoice Ninja field |
|--------------|---------------------|
| `customerName` | Client name |
| `customerCode` | `id_number` or equivalent reconciliation field |

### Custom field mapping (client)

Configured via backend settings (`IN_CF_CLIENT_*`). Labels in Invoice Ninja admin are expected to be: **Bill To**, **Ship To**, **Fax**.

| Logical | JB2026 source | v1 |
|---------|---------------|-----|
| Bill To | `billTo` | Required when configured |
| Ship To | All `shipToAddresses` formatted as `"<name>\n<address>"` blocks separated by a blank line | Required when configured |
| Fax | *(not in JB2026 today)* | Omit until customer metadata supports `fax` |

### Client contacts

- v1 does NOT require client-contact sync.
- When `IN_CF_CONTACT_FULL_NAME` is configured and JB2026 gains a primary contact name on the customer record, sync SHOULD upsert one primary Invoice Ninja contact with that name.

### Update trigger

- Manual "Sync with Billing" from `AdminCustomerView` MUST trigger customer sync.
- Customer create/update in admin MAY trigger sync when billing integration is enabled (optional; manual sync remains required minimum).

## Acceptance Criteria

- [ ] Syncing a customer with `billTo` and ship-to addresses populates the configured Invoice Ninja client custom fields.
- [ ] Re-syncing the same customer does not create a duplicate Invoice Ninja client.
- [ ] Auto-sync during invoice generation works when `invoiceNinjaClientId` is missing but customer data is mappable.
- [ ] Sync failure returns an actionable error without creating a duplicate client.
