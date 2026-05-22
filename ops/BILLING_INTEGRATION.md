# Invoice Ninja Billing Integration - Operations Guide

## Overview

JB2026 integrates with Invoice Ninja to provide modern invoicing capabilities. This integration replaces the legacy invoice path and makes Invoice Ninja the billing source of truth for invoices.

## Configuration

### Environment Variables / Secrets

The following configuration must be provided via secrets provider (environment variables in production):

#### Core API Configuration
- **`INVOICE_NINJA_API_KEY`** *(required)* - Invoice Ninja service account API key. Must be stored securely and never exposed to the frontend.
- **`INVOICE_NINJA_BASE_URL`** *(required)* - Base URL for Invoice Ninja API (e.g., `https://invoicing.example.com/api/v1`).

#### Custom Field Mappings
Invoice Ninja custom fields must be mapped to configuration keys. These keys tell the integration which custom-field slot (e.g., `custom_value1`) corresponds to each logical field.

```
IN_CF_CLIENT_BILL_TO      - Client custom field for billing address (e.g., custom_value1)
IN_CF_CLIENT_SHIP_TO      - Client custom field for shipping addresses (e.g., custom_value2)
IN_CF_CLIENT_FAX          - Client custom field for fax (optional, omit if not needed)
IN_CF_CONTACT_FULL_NAME   - Client contact custom field for full name (optional, omit if not needed)
IN_CF_PRODUCT_UNIT        - Product/line item custom field for unit (optional)
IN_CF_PRODUCT_PO_NO       - Product/line item custom field for P.O. number (e.g., custom_value1 or custom_value2)
IN_CF_INVOICE_JOB_NO      - Invoice custom field for Job number reference (e.g., custom_value1)
```

### Configuration File (appsettings.json)

The application configuration uses a structured `Billing:InvoiceNinja` section:

```json
{
  "Billing": {
    "InvoiceNinja": {
      "ApiKey": "__set_via_secret_provider__",
      "BaseUrl": "__set_via_secret_provider__",
      "CustomFields": {
        "ClientBillTo": "__set_via_environment__",
        "ClientShipTo": "__set_via_environment__",
        "ClientFax": "__set_via_environment__",
        "ContactFullName": "__set_via_environment__",
        "ProductUnit": "__set_via_environment__",
        "ProductPoNo": "__set_via_environment__",
        "InvoiceJobNo": "__set_via_environment__"
      },
      "HttpClientTimeoutSeconds": 30,
      "RetryMaxAttempts": 3,
      "RetryBackoffMultiplier": 2.0
    }
  }
}
```

## Customer Mapping Contract

When a JB2026 customer is synchronized to Invoice Ninja, the following mapping is applied:

| JB2026 Field | Invoice Ninja Field | Type | Required | Notes |
|--------------|-------------------|------|----------|-------|
| `customerName` | `name` | Native field | Yes | Primary client display name |
| `customerCode` | `id_number` | Native field | No | Secondary reconciliation key |
| `billTo` | Custom field (mapped via `IN_CF_CLIENT_BILL_TO`) | Custom field | Yes | Freeform billing address block |
| `shipToAddresses` | Custom field (mapped via `IN_CF_CLIENT_SHIP_TO`) | Custom field | Yes | Formatted as multi-address block: entries separated by blank lines; each entry formatted as `<name>\n<address>` |

### Notes

- **Email, Currency, Payment Terms**: Currently not synced from JB2026. Customers should be configured directly in Invoice Ninja or email/currency can be added to customer metadata in a future release.
- **Fax**: Not synced in v1. Omit `IN_CF_CLIENT_FAX` from configuration until customer metadata gains a fax field.
- **Primary Contact**: Not synced in v1. Deferred until customer metadata includes `primaryContactName`.

## Invoice Generation Mapping

When generating an invoice from a Job Order, the following mapping is applied:

| JB2026 Source | Invoice Ninja Target | Type | Required | Notes |
|---------------|-------------------|------|----------|-------|
| `jobNumber` | Invoice custom field (mapped via `IN_CF_INVOICE_JOB_NO`) | Custom field | Yes | Job reference for traceability |
| `poNumber` | Line item custom field (mapped via `IN_CF_PRODUCT_PO_NO`) | Custom field | Yes | Same P.O. applied to all lines in first-release |
| Combined `orderTitle` + `productDetails` | Line item description | Native field | Yes | Provides line-level detail |
| `qty` | Line item quantity | Native field | Yes | |
| Calculated from job data | Line item unit cost | Native field | Yes | |

### Notes

- **Unit**: Not mapped in v1. Omit `IN_CF_PRODUCT_UNIT` or leave empty until unit source is identified in workflow/product/job metadata.
- **Line Items**: Ad-hoc line items only in v1. Full product catalog sync is deferred.

## Idempotent Customer Synchronization

To prevent duplicate clients in Invoice Ninja:

1. **First Sync**: When syncing a customer for the first time, JB2026 creates a new Invoice Ninja client and persists the external `invoiceNinjaClientId` in the local customer metadata.
2. **Subsequent Syncs**: On repeat sync operations (e.g., after customer updates), JB2026 checks the persisted `invoiceNinjaClientId`. If present and the client exists in Invoice Ninja, the record is updated. Otherwise, a new client is created.
3. **Reconciliation**: The `customerCode` can be used as a secondary reconciliation key if the persisted ID is missing.

### Customer Metadata Structure

Invoice Ninja-related metadata is stored in the `Customer.MetadataXml` field:

```xml
<Metadata>
  <invoiceNinjaClientId>12345</invoiceNinjaClientId>
  <invoiceNinjaClientSyncedAt>2026-05-20T10:30:00Z</invoiceNinjaClientSyncedAt>
  <invoiceNinjaClientSyncStatus>success</invoiceNinjaClientSyncStatus>
</Metadata>
```

## API Endpoints

The backend exposes billing endpoints under `/api/v2/billing/`:

- **GET** `/api/v2/billing/connectivity` - Check connectivity to Invoice Ninja
- **POST** `/api/v2/billing/customers/sync` - Sync a customer to Invoice Ninja
- **POST** `/api/v2/billing/invoices/generate` - Generate an invoice from a Job Order
- **GET** `/api/v2/billing/invoices/{id}/summary` - Retrieve invoice summary
- **POST** `/api/v2/billing/invoices/{id}/refresh` - Refresh invoice status

All endpoints require JWT authentication.

### Error Codes

The billing API returns standardized error responses with codes for specific failure modes:

| Error Code | HTTP Status | Description |
|-----------|------------|-------------|
| `INVALID_API_KEY` | 401 | API key is invalid or expired |
| `INVALID_CONFIG` | 500 | Required configuration is missing |
| `RATE_LIMITED` | 429 | Invoice Ninja rate limit exceeded |
| `SERVICE_UNAVAILABLE` | 503 | Invoice Ninja service is temporarily down |
| `NOT_FOUND` | 404 | Requested resource not found in Invoice Ninja |
| `SYNC_FAILED` | 500 | Customer sync operation failed |
| `INVOICE_GENERATION_FAILED` | 500 | Invoice generation failed |
| `CONNECTIVITY_CHECK_FAILED` | 500 | Connectivity check failed |

## Troubleshooting

### Configuration Validation

Check configuration validity by calling the connectivity endpoint:

```bash
curl -H "Authorization: Bearer <JWT_TOKEN>" \
  https://localhost:5001/api/v2/billing/connectivity
```

Expected response on success:
```json
{
  "isConnected": true,
  "statusMessage": "Invoice Ninja is reachable and configured correctly."
}
```

### Common Issues

1. **Invalid API Key**: Verify the `INVOICE_NINJA_API_KEY` is a valid service account key for your Invoice Ninja instance.
2. **Unreachable Base URL**: Verify `INVOICE_NINJA_BASE_URL` is correct and accessible from the deployment environment.
3. **Custom Field Misconfiguration**: Verify all required custom field mappings point to valid fields in your Invoice Ninja company settings. Field names like `custom_value1` are examples; actual field keys depend on your Invoice Ninja company configuration.
4. **Rate Limiting**: The integration implements exponential backoff for retries. If rate limits are consistently hit, consider:
   - Increasing `RetryMaxAttempts` in configuration
   - Implementing request throttling upstream
   - Contacting Invoice Ninja support to adjust rate limits for your account

## Security Considerations

- **API Key Storage**: The Invoice Ninja API key must be stored securely (e.g., AWS Secrets Manager, Azure Key Vault) and never hardcoded or logged.
- **Key Rotation**: Rotate API keys periodically. Update configuration without restarting by using a secret provider that supports live updates.
- **Redacted Logging**: API keys are never logged by the integration. All logged URLs and payloads omit sensitive credentials.
- **Frontend Isolation**: Billing endpoints are protected by JWT authentication. The frontend must call the backend proxy; direct calls to Invoice Ninja from the browser are not allowed and would be blocked by CORS.

## Monitoring

Monitor the following for operational health:

- **Connectivity Check**: Periodically call `GET /api/v2/billing/connectivity` to ensure Invoice Ninja is reachable.
- **Sync Success Rate**: Track successful customer sync operations to identify configuration or connectivity issues.
- **Invoice Generation Success Rate**: Monitor invoice generation failures to catch malformed job data or Invoice Ninja API changes.
- **Error Rates**: Watch for spikes in 401 (auth), 429 (rate limit), or 503 (service unavailable) errors.

## First Release Scope

The first release includes:

1. ✓ Connectivity checks and validation
2. ✓ Customer synchronization with Bill To and Ship To custom fields
3. ✓ Invoice generation from Job Orders with Job No. and P.O.No. custom fields
4. ✓ Invoice status retrieval and refresh

**Not included in v1:**

- Full external invoice editing workflows (limited to generation and status reads)
- Void/delete operations (deferred until confirmed as business-critical)
- Full product catalog synchronization
- Fax and primary contact name mapping (deferred until customer metadata is extended)
- Unit field mapping (deferred until unit source is identified)

## Future Enhancements

Post-v1 enhancements may include:

- Customer metadata extensions (fax, primary contact name, email, currency)
- Unit field mapping from workflow/product/job metadata
- Per-job ship-to selection on invoice generation
- Full product catalog synchronization
- Extended invoice editing capabilities
