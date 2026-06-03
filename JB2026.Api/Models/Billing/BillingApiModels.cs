namespace JB2026.Api.Models.Billing;

/// <summary>
/// Response for connectivity check endpoint.
/// </summary>
public class BillingConnectivityResponse
{
    /// <summary>
    /// Indicates if Invoice Ninja is reachable and configured.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Descriptive status message.
    /// </summary>
    public string StatusMessage { get; set; } = string.Empty;
}

/// <summary>
/// Request to sync a customer to Invoice Ninja.
/// </summary>
public class SyncCustomerRequest
{
    /// <summary>
    /// JB2026 customer entity ID (used for metadata persistence).
    /// </summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// JB2026 customer code (e.g., "CUST-001") for reconciliation.
    /// </summary>
    public string CustomerCode { get; set; } = string.Empty;

    /// <summary>
    /// Customer display name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Billing address block (freeform text).
    /// </summary>
    public string BillTo { get; set; } = string.Empty;

    /// <summary>
    /// List of ship-to address entries.
    /// </summary>
    public List<string> ShipToAddresses { get; set; } = new();

    /// <summary>
    /// Pre-existing Invoice Ninja client ID if already synced (for idempotent updates).
    /// If null, a new client will be created.
    /// </summary>
    public string? ExistingInvoiceNinjaClientId { get; set; }
}

/// <summary>
/// Response from customer sync operation.
/// </summary>
public class SyncCustomerResponse
{
    /// <summary>
    /// Invoice Ninja client ID to persist in JB2026 customer metadata.
    /// </summary>
    public string InvoiceNinjaClientId { get; set; } = string.Empty;

    /// <summary>
    /// Sync timestamp for audit purposes.
    /// </summary>
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Metadata XML snippet to merge into Customer.MetadataXml.
    /// The caller should persist this by merging it into the customer record.
    /// </summary>
    public string MetadataToMerge { get; set; } = string.Empty;
}

/// <summary>
/// Request to generate an invoice from a Job Order.
/// </summary>
public class GenerateInvoiceRequest
{
    /// <summary>
    /// JB2026 Job Order ID to persist billing summary fields (invoiceRef/invoiceAmount) after generation.
    /// Optional for backward compatibility with existing clients.
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Invoice Ninja client ID (from synced customer metadata).
    /// </summary>
    public string InvoiceNinjaClientId { get; set; } = string.Empty;

    /// <summary>
    /// Job Order number (for custom field mapping).
    /// </summary>
    public string JobNumber { get; set; } = string.Empty;

    /// <summary>
    /// P.O. Number from the Job Order (for line item custom field mapping).
    /// </summary>
    public string PoNumber { get; set; } = string.Empty;

    /// <summary>
    /// Line items to include in the invoice.
    /// </summary>
    public List<InvoiceLineItemData> LineItems { get; set; } = new();
}

/// <summary>
/// Request to preview an invoice before creation (shows resolved custom fields without committing to Invoice Ninja).
/// </summary>
public class PreviewInvoiceRequest
{
    /// <summary>
    /// Customer name (for display in preview).
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Billing address block (will be mapped to IN custom field).
    /// </summary>
    public string BillTo { get; set; } = string.Empty;

    /// <summary>
    /// Ship-to block (will be mapped to IN custom field).
    /// </summary>
    public string ShipTo { get; set; } = string.Empty;

    /// <summary>
    /// Job Order number (will be mapped to invoice custom field).
    /// </summary>
    public string JobNumber { get; set; } = string.Empty;

    /// <summary>
    /// P.O. Number (will be mapped to line item custom field).
    /// </summary>
    public string PoNumber { get; set; } = string.Empty;

    /// <summary>
    /// Line items to include in the preview.
    /// </summary>
    public List<InvoiceLineItemData> LineItems { get; set; } = new();
}

/// <summary>
/// Preview of resolved custom fields for invoice creation confirmation dialog.
/// </summary>
public class InvoicePreviewResolvedFields
{
    /// <summary>
    /// Bill To custom field value (from BillingOptions.InvoiceNinja.CustomFields.ClientBillTo config).
    /// </summary>
    public string? BillToCustomField { get; set; }

    /// <summary>
    /// Ship To custom field value (from BillingOptions.InvoiceNinja.CustomFields.ClientShipTo config).
    /// </summary>
    public string? ShipToCustomField { get; set; }

    /// <summary>
    /// Job No. custom field value (from BillingOptions.InvoiceNinja.CustomFields.InvoiceJobNo config).
    /// </summary>
    public string? JobNoCustomField { get; set; }

    /// <summary>
    /// P.O.No. custom field value for line items (from BillingOptions.InvoiceNinja.CustomFields.ProductPoNo config).
    /// </summary>
    public string? PoNoCustomField { get; set; }

    /// <summary>
    /// Indicates if all required custom field slots are configured.
    /// </summary>
    public bool AllCustomFieldsConfigured { get; set; }
}

/// <summary>
/// Response for invoice preview (before confirmation and creation).
/// </summary>
public class PreviewInvoiceResponse
{
    /// <summary>
    /// Customer name for display in preview dialog.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Total invoice amount (sum of line items).
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Line items as they will appear on the invoice.
    /// </summary>
    public List<InvoiceLineItemData> LineItems { get; set; } = new();

    /// <summary>
    /// Resolved custom field values that will be sent to Invoice Ninja.
    /// Shows Bill To, Ship To, Job No., P.O.No. mapped according to configuration.
    /// </summary>
    public InvoicePreviewResolvedFields ResolvedCustomFields { get; set; } = new();

    /// <summary>
    /// Configuration warnings (e.g., missing custom field mappings).
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Response for invoice list retrieval.
/// </summary>
public class ListInvoicesResponse
{
    /// <summary>
    /// Invoice summaries for billing list screens.
    /// </summary>
    public List<InvoiceBillingSummary> Invoices { get; set; } = new();
}

/// <summary>
/// Line item data for invoice generation.
/// </summary>
public class InvoiceLineItemData
{
    /// <summary>
    /// Line item description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit cost.
    /// </summary>
    public decimal UnitCost { get; set; }
}

/// <summary>
/// Response from invoice generation operation.
/// Contains the billing summary to persist in Job Order metadata.
/// </summary>
public class GenerateInvoiceResponse
{
    /// <summary>
    /// Billing summary with external invoice details.
    /// </summary>
    public InvoiceBillingSummary BillingSummary { get; set; } = new();

    /// <summary>
    /// Timestamp of invoice creation.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response for invoice summary retrieval.
/// </summary>
public class GetInvoiceSummaryResponse
{
    /// <summary>
    /// Billing summary if invoice exists; null if not found.
    /// </summary>
    public InvoiceBillingSummary? BillingSummary { get; set; }
}

/// <summary>
/// Response for invoice status refresh.
/// </summary>
public class RefreshInvoiceStatusResponse
{
    /// <summary>
    /// Updated billing summary; null if invoice not found.
    /// </summary>
    public InvoiceBillingSummary? BillingSummary { get; set; }

    /// <summary>
    /// Timestamp of refresh operation.
    /// </summary>
    public DateTime RefreshedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response for invoice send action (Draft → Sent transition).
/// </summary>
public class SendInvoiceResponse
{
    /// <summary>
    /// Updated billing summary with new status (Sent).
    /// </summary>
    public InvoiceBillingSummary BillingSummary { get; set; } = new();

    /// <summary>
    /// Timestamp when the send operation completed.
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Error response for failed billing operations.
/// </summary>
public class BillingErrorResponse
{
    /// <summary>
    /// Error code (e.g., "INVALID_CONFIG", "SYNC_FAILED", "INVOICE_NOT_FOUND").
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional details (e.g., Invoice Ninja HTTP status code).
    /// </summary>
    public object? Details { get; set; }
}

/// <summary>
/// Supported date-range presets for billing statement generation.
/// </summary>
public static class BillingStatementDateRangePresets
{
    public const string AllOutstanding = "All Outstanding";
    public const string ThisMonth = "This Month";
    public const string LastMonth = "Last Month";
    public const string ThisQuarter = "This Quarter";
    public const string ThisYear = "This Year";
}

/// <summary>
/// Supported status values for billing statement generation.
/// </summary>
public static class BillingStatementStatuses
{
    public const string All = "All";
    public const string Paid = "Paid";
    public const string Unpaid = "Unpaid";
}

/// <summary>
/// Request body and query model for billing statement launch and retrieval.
/// </summary>
public class BillingStatementLaunchRequest
{
    public string ExternalClientId { get; set; } = string.Empty;

    public string DateRangePreset { get; set; } = BillingStatementDateRangePresets.AllOutstanding;

    public string Status { get; set; } = BillingStatementStatuses.All;

    public bool IncludeCredits { get; set; }

    public bool IncludePayments { get; set; }

    public bool IncludeAging { get; set; }
}

/// <summary>
/// Response returned when a statement launch URL is prepared.
/// </summary>
public class BillingStatementLaunchResponse
{
    public string LaunchUrl { get; set; } = string.Empty;
}

/// <summary>
/// Normalized billing statement document returned by the service layer.
/// </summary>
public class BillingStatementDocument
{
    public byte[] Content { get; set; } = [];

    public string ContentType { get; set; } = "application/pdf";

    public string FileName { get; set; } = "client-statement.pdf";
}

// ── Invoice Editor DTOs ──────────────────────────────────────────────────────

/// <summary>
/// A selectable Invoice Ninja client for the invoice editor client picker.
/// </summary>
public class BillingClientOption
{
    /// <summary>Invoice Ninja client ID.</summary>
    public string ExternalClientId { get; set; } = string.Empty;

    /// <summary>Invoice Ninja client name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-readable display name shown in the selector.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Optional external client code/number.</summary>
    public string IdNumber { get; set; } = string.Empty;

    /// <summary>Raw outstanding balance from Invoice Ninja.</summary>
    public decimal OutstandingBalance { get; set; }
}

/// <summary>
/// Response for client list/search used by the invoice editor.
/// </summary>
public class ListBillingClientsResponse
{
    public List<BillingClientOption> Clients { get; set; } = new();
}

/// <summary>
/// A single line item as returned by the invoice editor detail endpoint.
/// </summary>
public class InvoiceEditorLineItemDto
{
    /// <summary>Opaque row identifier (client-facing only).</summary>
    public string? Id { get; set; }

    public string PoNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }

    /// <summary>Server-computed line total (Qty × UnitCost).</summary>
    public decimal LineTotal { get; set; }
}

/// <summary>
/// Normalized invoice DTO returned by the editor detail endpoint.
/// </summary>
public class InvoiceEditorDto
{
    public string? ExternalInvoiceId { get; set; }
    public string? Status { get; set; }
    public BillingClientOption? Client { get; set; }

    /// <summary>ISO date string (e.g. "2026-05-23").</summary>
    public string? InvoiceDate { get; set; }

    /// <summary>ISO date string (e.g. "2026-05-23").</summary>
    public string? DueDate { get; set; }

    public string JobNumber { get; set; } = string.Empty;
    public List<InvoiceEditorLineItemDto> LineItems { get; set; } = new();

    /// <summary>Sum of all line totals.</summary>
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Response for GET /api/v2/billing/invoices/{externalInvoiceId}.
/// </summary>
public class GetInvoiceEditorDetailResponse
{
    public InvoiceEditorDto Invoice { get; set; } = new();
}

/// <summary>
/// Lookup request for resolving canonical job numbers into billing invoice editor autofill rows.
/// </summary>
public class LookupInvoiceEditorAutofillRequest
{
    public List<string> CanonicalJobNumbers { get; set; } = new();
}

/// <summary>
/// Autofill lookup status values for billing invoice editor rows.
/// </summary>
public static class InvoiceEditorAutofillLookupStatuses
{
    public const string Resolved = "Resolved";
    public const string Unresolved = "Unresolved";
    public const string ResolvedButMissingSection1 = "ResolvedButMissingSection1";
}

/// <summary>
/// A single resolved or unresolved job lookup result for invoice editor autofill.
/// </summary>
public class InvoiceEditorAutofillLookupItemDto
{
    public string CanonicalJobNumber { get; set; } = string.Empty;

    public Guid? OrderId { get; set; }

    public string PurchaseOrder { get; set; } = string.Empty;

    public string ProductDetails { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = InvoiceEditorAutofillLookupStatuses.Resolved;

    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response for invoice editor autofill lookup.
/// </summary>
public class LookupInvoiceEditorAutofillResponse
{
    public List<InvoiceEditorAutofillLookupItemDto> Jobs { get; set; } = new();
}

/// <summary>
/// A single line item within a create or update invoice editor request.
/// </summary>
public class InvoiceEditorLineItemRequest
{
    public string PoNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
}

/// <summary>
/// Request body for POST /api/v2/billing/invoices (create).
/// </summary>
public class CreateInvoiceEditorRequest
{
    public string ExternalClientId { get; set; } = string.Empty;

    /// <summary>ISO date string (e.g. "2026-05-23").</summary>
    public string? InvoiceDate { get; set; }

    /// <summary>ISO date string (e.g. "2026-05-23").</summary>
    public string? DueDate { get; set; }

    public string JobNumber { get; set; } = string.Empty;
    public List<InvoiceEditorLineItemRequest> LineItems { get; set; } = new();
}

/// <summary>
/// Request body for PUT /api/v2/billing/invoices/{externalInvoiceId} (update draft).
/// </summary>
public class UpdateInvoiceEditorRequest
{
    public string ExternalClientId { get; set; } = string.Empty;

    /// <summary>ISO date string (e.g. "2026-05-23").</summary>
    public string? InvoiceDate { get; set; }

    /// <summary>ISO date string (e.g. "2026-05-23").</summary>
    public string? DueDate { get; set; }

    public string JobNumber { get; set; } = string.Empty;
    public List<InvoiceEditorLineItemRequest> LineItems { get; set; } = new();
}

/// <summary>
/// Response from a create or update invoice editor operation.
/// </summary>
public class SaveInvoiceEditorResponse
{
    public InvoiceBillingSummary BillingSummary { get; set; } = new();
}
