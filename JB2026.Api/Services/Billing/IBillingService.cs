namespace JB2026.Api.Services.Billing;

using JB2026.Api.Models.Billing;
using JB2026.Api.Options;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Linq.Expressions;

/// <summary>
/// High-level interface for billing operations with Invoice Ninja.
/// Handles business logic like customer mapping, invoice generation, and error handling.
/// </summary>
public interface IBillingService
{
    /// <summary>
    /// Checks connectivity to Invoice Ninja and validates configuration.
    /// </summary>
    /// <returns>Tuple of (isConnected, statusMessage).</returns>
    Task<(bool isConnected, string statusMessage)> CheckConnectivityAsync();

    /// <summary>
    /// Synchronizes a JB2026 customer to Invoice Ninja.
    /// If a persisted external client ID exists and the client is found in Invoice Ninja, updates it.
    /// Otherwise, creates a new Invoice Ninja client.
    /// </summary>
    /// <param name="jb2026CustomerId">JB2026 customer entity ID (for metadata storage).</param>
    /// <param name="customerCode">JB2026 customer code (for reconciliation).</param>
    /// <param name="customerName">Customer display name.</param>
    /// <param name="billTo">Billing address block.</param>
    /// <param name="shipToAddresses">List of ship-to addresses; serialized as per design.md §10.</param>
    /// <param name="existingInvoiceNinjaClientId">Pre-existing IN client ID if available from metadata; null if not synced yet.</param>
    /// <returns>Invoice Ninja client ID to be persisted in JB2026 metadata.</returns>
    Task<string> SyncCustomerAsync(
        string jb2026CustomerId,
        string customerCode,
        string customerName,
        string billTo,
        List<string> shipToAddresses,
        string? existingInvoiceNinjaClientId,
        string? group = null);

    /// <summary>
    /// Builds a sync payload from an existing JB2026 customer record.
    /// Supports customerId-only sync requests from frontend workflows.
    /// </summary>
    /// <param name="customerId">JB2026 customer ID.</param>
    /// <returns>Resolved sync request fields from customer metadata.</returns>
    Task<SyncCustomerRequest> BuildSyncCustomerRequestFromCustomerIdAsync(string customerId);

    /// <summary>
    /// Generates an invoice in Invoice Ninja from a Job Order.
    /// Pre-condition: Customer must already be synced to Invoice Ninja (invoiceNinjaClientId must be set in metadata).
    /// </summary>
    /// <param name="invoiceNinjaClientId">Invoice Ninja client ID (from synced metadata).</param>
    /// <param name="jobNumber">Job Order number (for custom field mapping).</param>
    /// <param name="poNumber">P.O. Number (for line item custom field mapping).</param>
    /// <param name="lineItems">Line items to include in the invoice.</param>
    /// <returns>Billing summary with external invoice ID to persist in Job Order metadata.</returns>
    Task<InvoiceBillingSummary> GenerateInvoiceAsync(
        string invoiceNinjaClientId,
        string jobNumber,
        string poNumber,
        List<GenerateInvoiceLineItem> lineItems);

    /// <summary>
    /// Retrieves a summary of an Invoice Ninja invoice by its external ID.
    /// Used for displaying invoice status in billing and job/order screens.
    /// </summary>
    /// <param name="externalInvoiceId">Invoice Ninja invoice ID.</param>
    /// <returns>Billing summary suitable for UI display, or null if not found.</returns>
    Task<InvoiceBillingSummary?> GetInvoiceSummaryAsync(string externalInvoiceId);

    /// <summary>
    /// Refreshes the status of an Invoice Ninja invoice by fetching the latest data.
    /// </summary>
    /// <param name="externalInvoiceId">Invoice Ninja invoice ID.</param>
    /// <returns>Updated billing summary, or null if not found.</returns>
    Task<InvoiceBillingSummary?> RefreshInvoiceStatusAsync(string externalInvoiceId);

    /// <summary>
    /// Sends a draft invoice to Invoice Ninja, transitioning it from Draft to Sent status.
    /// The invoice must be in Draft status; otherwise a BillingException is thrown.
    /// After a successful send, best-effort updates the linked job order's invoice fields.
    /// </summary>
    /// <param name="externalInvoiceId">Invoice Ninja invoice ID.</param>
    /// <param name="modifiedBy">ID of the user performing the Mark Sent action (for ModifiedBy audit field).</param>
    /// <returns>Updated billing summary with status Sent.</returns>
    Task<InvoiceBillingSummary> SendInvoiceAsync(string externalInvoiceId, Guid modifiedBy);

    /// <summary>
    /// Previews invoice payload before creation, including resolved custom field values and warnings.
    /// </summary>
    /// <param name="request">Preview request payload.</param>
    /// <returns>Resolved preview response.</returns>
    Task<PreviewInvoiceResponse> PreviewInvoiceAsync(PreviewInvoiceRequest request);

    /// <summary>
    /// Lists invoice summaries from Invoice Ninja for billing list screens.
    /// </summary>
    /// <returns>Invoice summary list.</returns>
    Task<IReadOnlyList<InvoiceBillingSummary>> ListInvoicesAsync(string? lookup = null, string? invoiceLookup = null);

    /// <summary>
    /// Downloads the invoice PDF document from Invoice Ninja for the given invoice ID.
    /// Internally fetches the invitation_key from the invoice's invitations array before downloading.
    /// </summary>
    /// <param name="externalInvoiceId">Invoice Ninja invoice ID.</param>
    /// <returns>PDF file content as byte array.</returns>
    Task<byte[]> DownloadInvoicePdfAsync(string externalInvoiceId);

    /// <summary>
    /// Downloads the delivery note PDF document from Invoice Ninja for the given invoice ID.
    /// </summary>
    /// <param name="externalInvoiceId">Invoice Ninja invoice ID.</param>
    /// <returns>Delivery note PDF file content as byte array.</returns>
    Task<byte[]> DownloadDeliveryNoteAsync(string externalInvoiceId);

    /// <summary>
    /// Builds a generate-invoice payload from a JB2026 job order and synced customer metadata.
    /// </summary>
    /// <param name="orderId">Job order ID.</param>
    /// <returns>Resolved invoice generation request payload.</returns>
    Task<GenerateInvoiceRequest> BuildGenerateInvoiceRequestFromJobOrderAsync(Guid orderId);

    /// <summary>
    /// Persists billing summary values back to JB2026 Job Order fields used by legacy lists.
    /// </summary>
    /// <param name="orderId">Target job order ID.</param>
    /// <param name="summary">Billing summary returned by Invoice Ninja generation.</param>
    /// <returns>True when update was applied; false when order was not found.</returns>
    Task<bool> PersistJobOrderBillingSummaryAsync(Guid orderId, InvoiceBillingSummary summary);

    /// <summary>
    /// Updates invoice amount and audit fields on the Job Order whose InvoiceRef matches the given
    /// external invoice ID. Called after a draft invoice is marked as sent so that the job record
    /// stays in sync with the Invoice Ninja invoice state.
    /// </summary>
    /// <param name="invoiceRef">External Invoice Ninja invoice ID stored in the job's InvoiceRef field.</param>
    /// <param name="invoiceNumber">Human-readable Invoice Ninja invoice number (e.g. "0023") to store as Invoice No on the job order.</param>
    /// <param name="invoiceAmount">Confirmed total amount from the sent invoice.</param>
    /// <param name="modifiedBy">ID of the user performing the Mark Sent action.</param>
    /// <returns>True when the job order was found and updated; false otherwise.</returns>
    Task<bool> UpdateJobOrderInvoiceDataByRefAsync(string invoiceRef, string invoiceNumber, decimal invoiceAmount, Guid modifiedBy);

    /// <summary>
    /// Lists Invoice Ninja clients matching an optional search query for the editor client picker.
    /// </summary>
    /// <param name="query">Optional search term; returns up to 100 clients when null/empty.</param>
    /// <returns>Matching client options.</returns>
    Task<IReadOnlyList<BillingClientOption>> GetBillingClientsAsync(string? query);

    /// <summary>
    /// Lists Invoice Ninja group settings for the admin customer dialog.
    /// </summary>
    /// <returns>Group options.</returns>
    Task<IReadOnlyList<BillingGroupOption>> GetBillingGroupsAsync();

    /// <summary>
    /// Validates and normalizes a client statement launch request.
    /// </summary>
    /// <param name="request">Raw statement request from the UI.</param>
    /// <returns>Normalized request safe to encode into a launch URL.</returns>
    Task<BillingStatementLaunchRequest> PrepareClientStatementLaunchAsync(BillingStatementLaunchRequest request);

    /// <summary>
    /// Retrieves a client statement document from Invoice Ninja using the normalized billing request.
    /// </summary>
    /// <param name="request">Normalized statement request.</param>
    /// <returns>Statement document content and metadata.</returns>
    Task<BillingStatementDocument> GetClientStatementAsync(BillingStatementLaunchRequest request);

    /// <summary>
    /// Returns a normalized editor DTO for an existing invoice (for edit or read-only view).
    /// </summary>
    /// <param name="externalInvoiceId">Invoice Ninja invoice ID.</param>
    /// <returns>Editor DTO with client, date, job number, and line items.</returns>
    Task<InvoiceEditorDto> GetInvoiceEditorDetailAsync(string externalInvoiceId);

    /// <summary>
    /// Resolves canonical job numbers into billing invoice editor autofill rows.
    /// </summary>
    /// <param name="canonicalJobNumbers">Canonical user-facing job numbers such as orderNumber-jobSuffix.</param>
    /// <returns>Resolved, unresolved, or manual-review row payloads for invoice editor autofill.</returns>
    Task<IReadOnlyList<InvoiceEditorAutofillLookupItemDto>> LookupInvoiceEditorAutofillAsync(IReadOnlyList<string> canonicalJobNumbers);

    /// <summary>
    /// Creates a new invoice in Invoice Ninja from the editor form.
    /// </summary>
    /// <param name="request">Editor form payload.</param>
    /// <returns>Billing summary for the newly created invoice.</returns>
    Task<InvoiceBillingSummary> CreateInvoiceFromEditorAsync(CreateInvoiceEditorRequest request);

    /// <summary>
    /// Updates a draft invoice in Invoice Ninja from the editor form.
    /// Throws BillingException when the invoice is no longer in Draft status.
    /// </summary>
    /// <param name="externalInvoiceId">Invoice Ninja invoice ID to update.</param>
    /// <param name="request">Editor form payload.</param>
    /// <returns>Updated billing summary.</returns>
    Task<InvoiceBillingSummary> UpdateInvoiceFromEditorAsync(string externalInvoiceId, UpdateInvoiceEditorRequest request);
}

/// <summary>
/// Line item data for invoice generation from a Job Order.
/// </summary>
public class GenerateInvoiceLineItem
{
    /// <summary>
    /// Line item description (combined orderTitle + productDetails).
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
/// Implementation of billing operations with Invoice Ninja.
/// </summary>
public class BillingService : IBillingService
{
    private readonly IInvoiceNinjaHttpClient _invoiceNinjaClient;
    private readonly IOptions<BillingOptions> _billingOptions;
    private readonly JB5LegacyReadContext? _readContext;
    private readonly JB5LegacyWriteContext? _writeContext;
    private readonly ISettingsService? _settingsService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BillingService> _logger;

    public BillingService(
        IInvoiceNinjaHttpClient invoiceNinjaClient,
        IOptions<BillingOptions> billingOptions,
        IServiceProvider serviceProvider,
        ILogger<BillingService> logger)
    {
        _invoiceNinjaClient = invoiceNinjaClient;
        _billingOptions = billingOptions;
        _readContext = serviceProvider.GetService<JB5LegacyReadContext>();
        _writeContext = serviceProvider.GetService<JB5LegacyWriteContext>();
        _settingsService = serviceProvider.GetService<ISettingsService>();
        _timeProvider = serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;
        _logger = logger;
    }

    public async Task<(bool isConnected, string statusMessage)> CheckConnectivityAsync()
    {
        var (isValid, validationError) = _invoiceNinjaClient.ValidateConfiguration();
        if (!isValid)
        {
            return (false, $"Configuration error: {validationError}");
        }

        var isConnected = await _invoiceNinjaClient.IsConnectedAsync();
        var message = isConnected ? "Invoice Ninja is reachable and configured correctly." : "Unable to reach Invoice Ninja API. Check base URL and credentials.";

        return (isConnected, message);
    }

    public async Task<string> SyncCustomerAsync(
        string jb2026CustomerId,
        string customerCode,
        string customerName,
        string billTo,
        List<string> shipToAddresses,
        string? existingInvoiceNinjaClientId,
        string? group = null)
    {
        _logger.LogInformation("Syncing customer {CustomerCode} ({CustomerId}) to Invoice Ninja with group=[{Group}]", customerCode, jb2026CustomerId, group ?? "(null)");

        try
        {
            // If we have an existing external ID, try to fetch and update the client
            if (!string.IsNullOrWhiteSpace(existingInvoiceNinjaClientId))
            {
                InvoiceNinjaClientResponse? existing = null;
                try
                {
                    existing = await _invoiceNinjaClient.GetAsync<InvoiceNinjaClientResponse>($"/clients/{existingInvoiceNinjaClientId}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch existing Invoice Ninja client {InvoiceNinjaClientId}, will try reconcile or create new", existingInvoiceNinjaClientId);
                }

                if (existing != null)
                {
                    _logger.LogDebug("Found existing Invoice Ninja client {InvoiceNinjaClientId}, updating", existingInvoiceNinjaClientId);

                    var updateRequest = BuildClientUpdatePayload(customerName, customerCode, billTo, shipToAddresses, group);
                    var updated = await _invoiceNinjaClient.PutAsync<InvoiceNinjaClientResponse>(
                        $"/clients/{existingInvoiceNinjaClientId}",
                        updateRequest);

                    await PersistCustomerBillingClientIdAsync(jb2026CustomerId, updated.Id);

                    // Workaround: IN v5 PUT /clients/{id} does not reliably persist group_settings_id
                    if (!string.IsNullOrWhiteSpace(group))
                    {
                        await AssignGroupBulkAsync(updated.Id, group);
                    }

                    return updated.Id;
                }
            }

            var reconciledClientId = await TryFindInvoiceNinjaClientIdByCustomerCodeAsync(customerCode);
            if (!string.IsNullOrWhiteSpace(reconciledClientId))
            {
                _logger.LogInformation(
                    "Reconciled existing Invoice Ninja client {InvoiceNinjaClientId} using customer code {CustomerCode}",
                    reconciledClientId,
                    customerCode);

                var updateRequest = BuildClientUpdatePayload(customerName, customerCode, billTo, shipToAddresses, group);
                var updated = await _invoiceNinjaClient.PutAsync<InvoiceNinjaClientResponse>(
                    $"/clients/{reconciledClientId}",
                    updateRequest);

                await PersistCustomerBillingClientIdAsync(jb2026CustomerId, updated.Id);

                // Workaround: IN v5 PUT /clients/{id} does not reliably persist group_settings_id
                if (!string.IsNullOrWhiteSpace(group))
                {
                    await AssignGroupBulkAsync(updated.Id, group);
                }

                return updated.Id;
            }

            // Create a new client
            _logger.LogDebug("Creating new Invoice Ninja client for customer {CustomerCode}", customerCode);
            var createRequest = BuildClientCreatePayload(customerName, customerCode, billTo, shipToAddresses, group);
            var created = await _invoiceNinjaClient.PostAsync<InvoiceNinjaClientResponse>(
                "/clients",
                createRequest);

            await PersistCustomerBillingClientIdAsync(jb2026CustomerId, created.Id);

            _logger.LogInformation("Customer {CustomerCode} synced to Invoice Ninja with ID {InvoiceNinjaClientId}", customerCode, created.Id);
            return created.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync customer {CustomerCode} to Invoice Ninja: {ErrorMessage}", customerCode, ex.Message);
            throw;
        }
    }

    public async Task<SyncCustomerRequest> BuildSyncCustomerRequestFromCustomerIdAsync(string customerId)
    {
        if (_readContext is null)
        {
            throw new BillingException("DATA_CONTEXT_UNAVAILABLE", "Customer sync lookup is unavailable in the current runtime mode.");
        }

        if (!Guid.TryParse(customerId, out var customerGuid))
        {
            throw new BillingException("INVALID_REQUEST", "CustomerId is invalid.", 400);
        }

        var customer = await _readContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == customerGuid && !c.Retired);

        if (customer is null)
        {
            throw new BillingException("CUSTOMER_NOT_FOUND", $"Customer '{customerId}' was not found.", 404);
        }

        var metadata = ParseCustomerSyncMetadata(customer.MetadataXml);
        if (string.IsNullOrWhiteSpace(metadata.CustomerCode) || string.IsNullOrWhiteSpace(metadata.BillTo))
        {
            throw new BillingException(
                "INVALID_REQUEST",
                "Customer metadata is missing required CustomerCode or BillTo fields for billing sync.",
                400);
        }

        return new SyncCustomerRequest
        {
            CustomerId = customer.CustomerId.ToString(),
            CustomerCode = metadata.CustomerCode,
            CustomerName = customer.CustomerName?.Trim() ?? string.Empty,
            BillTo = metadata.BillTo,
            ShipToAddresses = metadata.ShipToAddresses,
            ExistingInvoiceNinjaClientId = metadata.InvoiceNinjaClientId,
            Group = metadata.Group,
        };
    }

    public async Task<InvoiceBillingSummary> GenerateInvoiceAsync(
        string invoiceNinjaClientId,
        string jobNumber,
        string poNumber,
        List<GenerateInvoiceLineItem> lineItems)
    {
        _logger.LogInformation("Generating invoice for client {InvoiceNinjaClientId} from job {JobNumber}", invoiceNinjaClientId, jobNumber);

        try
        {
            var options = _billingOptions.Value.InvoiceNinja;
            var customFields = options.CustomFields;

            // Build line items with custom fields
            var inlineItems = lineItems.Select(item =>
            {
                var li = new CreateInvoiceLineItemRequest
                {
                    Description = item.Description,
                    Quantity = item.Quantity,
                    Cost = item.UnitCost,
                };
                if (!string.IsNullOrWhiteSpace(customFields.ProductPoNo))
                    li.SetCustomValue(customFields.ProductPoNo, poNumber);
                return li;
            }).ToList();

            // Build invoice with custom fields (Job No.)
            var createRequest = new CreateInvoiceNinjaInvoiceRequest
            {
                ClientId = invoiceNinjaClientId,
                LineItems = inlineItems
            };
            if (!string.IsNullOrWhiteSpace(customFields.InvoiceJobNo))
                createRequest.SetCustomValue(customFields.InvoiceJobNo, jobNumber);

            var created = await _invoiceNinjaClient.PostAsync<InvoiceNinjaInvoiceResponse>(
                "/invoices",
                createRequest);

            _logger.LogInformation("Invoice {InvoiceNumber} created in Invoice Ninja for job {JobNumber}", created.Number, jobNumber);

            return MapToInvoiceBillingSummary(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate invoice for client {InvoiceNinjaClientId}, job {JobNumber}: {ErrorMessage}",
                invoiceNinjaClientId, jobNumber, ex.Message);
            throw;
        }
    }

    public async Task<InvoiceBillingSummary?> GetInvoiceSummaryAsync(string externalInvoiceId)
    {
        try
        {
            var invoice = await _invoiceNinjaClient.GetAsync<InvoiceNinjaInvoiceResponse>($"/invoices/{externalInvoiceId}?include=client");
            if (invoice == null)
            {
                _logger.LogDebug("Invoice {ExternalInvoiceId} not found in Invoice Ninja", externalInvoiceId);
                return null;
            }

            return MapToInvoiceBillingSummary(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve invoice {ExternalInvoiceId}: {ErrorMessage}", externalInvoiceId, ex.Message);
            throw;
        }
    }

    public async Task<InvoiceBillingSummary?> RefreshInvoiceStatusAsync(string externalInvoiceId)
    {
        _logger.LogDebug("Refreshing invoice status for {ExternalInvoiceId}", externalInvoiceId);
        return await GetInvoiceSummaryAsync(externalInvoiceId);
    }

    public async Task<InvoiceBillingSummary> SendInvoiceAsync(string externalInvoiceId, Guid modifiedBy)
    {
        _logger.LogInformation("Sending invoice {ExternalInvoiceId} via Invoice Ninja", externalInvoiceId);

        try
        {
            // Fetch the current invoice to validate it's in Draft status
            var invoice = await _invoiceNinjaClient.GetAsync<InvoiceNinjaInvoiceResponse>($"/invoices/{externalInvoiceId}");
            if (invoice == null)
            {
                throw BillingException.NotFound($"Invoice {externalInvoiceId}");
            }

            var currentStatus = ResolveInvoiceStatus(invoice);
            if (!string.Equals(currentStatus, "Draft", StringComparison.OrdinalIgnoreCase))
            {
                throw BillingException.InvalidRequest(
                    $"Invoice {externalInvoiceId} is in status '{currentStatus}' and cannot be sent. Only Draft invoices can be sent.",
                    400);
            }

            // Use Invoice Ninja v5 bulk action endpoint to mark invoice as sent
            // This is the documented way to send invoices in Invoice Ninja v5
            var bulkActionPayload = new
            {
                action = "mark_sent",
                ids = new[] { externalInvoiceId }
            };

            await _invoiceNinjaClient.PostAsync<dynamic>("/invoices/bulk", bulkActionPayload);

            // Fetch the updated invoice to return current billing summary
            var updatedInvoice = await _invoiceNinjaClient.GetAsync<InvoiceNinjaInvoiceResponse>($"/invoices/{externalInvoiceId}");
            if (updatedInvoice == null)
            {
                throw BillingException.NotFound($"Invoice {externalInvoiceId} after send");
            }

            _logger.LogInformation("Invoice {ExternalInvoiceId} sent successfully via bulk action endpoint", externalInvoiceId);

            // Best-effort: update the linked job order's invoice data so it stays in sync.
            // Failures are logged but do not affect the invoice send result.
            try
            {
                await TryUpdateJobOrderFromInvoiceAsync(externalInvoiceId, updatedInvoice, modifiedBy);
            }
            catch (Exception jobEx)
            {
                _logger.LogWarning(jobEx, "Non-fatal: failed to update job order for invoice {ExternalInvoiceId}", externalInvoiceId);
            }

            return MapToInvoiceBillingSummary(updatedInvoice);
        }
        catch (BillingException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invoice {ExternalInvoiceId}: {ErrorMessage}", externalInvoiceId, ex.Message);
            throw BillingException.HttpError(0, $"Failed to send invoice {externalInvoiceId}.", ex);
        }
    }

    /// <summary>
    /// Attempts to find and update the job order linked to the given Invoice Ninja invoice.
    /// Uses two lookup strategies:
    ///   1. InvoiceRef == externalInvoiceId  (set during invoice generation via PersistJobOrderBillingSummaryAsync)
    ///   2. JobNumber from the invoice's job-number custom field (covers editor-created invoices)
    /// Silently returns if no matching job order is found.
    /// </summary>
    private async Task TryUpdateJobOrderFromInvoiceAsync(
        string externalInvoiceId,
        InvoiceNinjaInvoiceResponse invoice,
        Guid modifiedBy)
    {
        if (_writeContext is null)
        {
            _logger.LogWarning("Skipping job order invoice sync: write context unavailable for invoice {ExternalInvoiceId}", externalInvoiceId);
            return;
        }

        JB2026.EfCore.Models.JobOrder? order = null;

        // Strategy 1: look up by InvoiceRef set during invoice generation
        order = await _writeContext.JobOrders.FirstOrDefaultAsync(x => x.InvoiceRef == externalInvoiceId);

        if (order is null)
        {
            _logger.LogInformation(
                "InvoiceRef lookup missed for {ExternalInvoiceId}; trying job-number custom field fallback",
                externalInvoiceId);

            var customFields = _billingOptions.Value.InvoiceNinja.CustomFields;
            var jobNumberStr = !string.IsNullOrWhiteSpace(customFields.InvoiceJobNo)
                ? invoice.GetCustomValue(customFields.InvoiceJobNo)
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(jobNumberStr))
            {
                // Strategy 2a: integer job number stored as plain string (e.g. "5")
                if (int.TryParse(jobNumberStr, out var jobNumberInt))
                {
                    order = await _writeContext.JobOrders.FirstOrDefaultAsync(x => x.JobNumber == jobNumberInt);
                }

                // Strategy 2b: canonical "OrderNumber-JobNumber" format (e.g. "A001-5")
                if (order is null &&
                    BillingInvoiceAutofillHelper.TryParseCanonicalJobNumber(jobNumberStr, out var jobRef) &&
                    jobRef is not null)
                {
                    order = await _writeContext.JobOrders.FirstOrDefaultAsync(
                        x => x.OrderNumber == jobRef.OrderNumber && x.JobNumber == jobRef.JobSuffix);
                }

                // Strategy 2c: multi-job expression from the invoice editor (e.g. "A001-1/2, A002-3").
                if (order is null)
                {
                    var firstCanonicalJobNumber = BillingInvoiceAutofillHelper
                        .ParseCanonicalJobNumberExpression(jobNumberStr)
                        .FirstOrDefault();

                    if (BillingInvoiceAutofillHelper.TryParseCanonicalJobNumber(firstCanonicalJobNumber, out var parsedFirstJobRef)
                        && parsedFirstJobRef is not null)
                    {
                        order = await _writeContext.JobOrders.FirstOrDefaultAsync(
                            x => x.OrderNumber == parsedFirstJobRef.OrderNumber && x.JobNumber == parsedFirstJobRef.JobSuffix);
                    }
                }
            }
        }

        if (order is null)
        {
            _logger.LogWarning(
                "No job order found for invoice {ExternalInvoiceId}; invoice data not synced to job record",
                externalInvoiceId);
            return;
        }

        order.InvoiceRef = invoice.Number;
        order.InvoiceAmount = invoice.Amount;
        order.ModifiedOn = DateTime.UtcNow;
        order.ModifiedBy = modifiedBy;

        await _writeContext.SaveChangesAsync();

        _logger.LogInformation(
            "Updated job order {OrderId} with invoice number {InvoiceNumber} and amount {Amount} after mark-sent for invoice {ExternalInvoiceId}",
            order.OrderId, invoice.Number, invoice.Amount, externalInvoiceId);
    }

    public Task<PreviewInvoiceResponse> PreviewInvoiceAsync(PreviewInvoiceRequest request)
    {
        var options = _billingOptions.Value.InvoiceNinja;
        var customFields = options.CustomFields;

        var total = request.LineItems.Sum(item => item.Quantity * item.UnitCost);
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(customFields.ClientBillTo))
        {
            warnings.Add("Client Bill To custom field mapping is not configured.");
        }

        if (string.IsNullOrWhiteSpace(customFields.ClientShipTo))
        {
            warnings.Add("Client Ship To custom field mapping is not configured.");
        }

        if (string.IsNullOrWhiteSpace(customFields.InvoiceJobNo))
        {
            warnings.Add("Invoice Job No custom field mapping is not configured.");
        }

        if (string.IsNullOrWhiteSpace(customFields.ProductPoNo))
        {
            warnings.Add("Product P.O.No custom field mapping is not configured.");
        }

        var response = new PreviewInvoiceResponse
        {
            CustomerName = request.CustomerName,
            TotalAmount = total,
            LineItems = request.LineItems,
            ResolvedCustomFields = new InvoicePreviewResolvedFields
            {
                BillToCustomField = request.BillTo,
                ShipToCustomField = request.ShipTo,
                JobNoCustomField = request.JobNumber,
                PoNoCustomField = request.PoNumber,
                AllCustomFieldsConfigured = warnings.Count == 0
            },
            Warnings = warnings
        };

        return Task.FromResult(response);
    }

    public async Task<IReadOnlyList<InvoiceBillingSummary>> ListInvoicesAsync(string? lookup = null, string? invoiceLookup = null)
    {
        try
        {
            var invoices = await _invoiceNinjaClient.GetAsync<List<InvoiceNinjaInvoiceResponse>>("/invoices?include=client");
            if (invoices == null)
            {
                return Array.Empty<InvoiceBillingSummary>();
            }

            var summaries = invoices.Select(MapToInvoiceBillingSummary);

            if (!string.IsNullOrWhiteSpace(lookup))
            {
                var token = lookup.Trim();
                summaries = summaries.Where(summary =>
                    !string.IsNullOrWhiteSpace(summary.ClientName) &&
                    summary.ClientName.Contains(token, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(invoiceLookup))
            {
                var invoiceToken = invoiceLookup.Trim();
                summaries = summaries.Where(summary =>
                    !string.IsNullOrWhiteSpace(summary.InvoiceNumber) &&
                    summary.InvoiceNumber.Contains(invoiceToken, StringComparison.OrdinalIgnoreCase));
            }

            return summaries.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list invoices from Invoice Ninja: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    public async Task<GenerateInvoiceRequest> BuildGenerateInvoiceRequestFromJobOrderAsync(Guid orderId)
    {
        if (_readContext is null)
        {
            throw new BillingException("DATA_CONTEXT_UNAVAILABLE", "Job-order invoice generation is unavailable in the current runtime mode.");
        }

        var job = await _readContext.JobOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == orderId && !x.Retired);

        if (job is null)
        {
            throw new BillingException("JOB_ORDER_NOT_FOUND", $"Job order '{orderId}' was not found.", 404);
        }

        var metadata = await ResolveCustomerBillingMetadataForJobAsync(job.CustomerRef, job.CustomerName);
        if (string.IsNullOrWhiteSpace(metadata.InvoiceNinjaClientId))
        {
            throw new BillingException(
                "CUSTOMER_NOT_SYNCED",
                $"Customer '{job.CustomerRef ?? job.CustomerName ?? "(unknown)"}' is not synced with Invoice Ninja.",
                400);
        }

        var mapped = JobOrderInvoiceMappingHelper.MapJobOrderToInvoiceRequest(
            job,
            metadata.InvoiceNinjaClientId,
            metadata.BillTo,
            metadata.ShipTo);

        mapped.OrderId = orderId;
        return mapped;
    }

    public async Task<bool> PersistJobOrderBillingSummaryAsync(Guid orderId, InvoiceBillingSummary summary)
    {
        if (_writeContext is null)
        {
            _logger.LogWarning("Skipping Job Order billing persistence because write context is unavailable");
            return false;
        }

        var order = await _writeContext.JobOrders.FirstOrDefaultAsync(x => x.OrderId == orderId);
        if (order is null)
        {
            return false;
        }

        order.InvoiceRef = summary.ExternalInvoiceId;
        order.InvoiceAmount = summary.Amount;
        order.ModifiedOn = DateTime.UtcNow;

        await _writeContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateJobOrderInvoiceDataByRefAsync(string invoiceRef, string invoiceNumber, decimal invoiceAmount, Guid modifiedBy)
    {
        if (_writeContext is null)
        {
            _logger.LogWarning("Skipping job order invoice data update because write context is unavailable");
            return false;
        }

        var order = await _writeContext.JobOrders.FirstOrDefaultAsync(x => x.InvoiceRef == invoiceRef);
        if (order is null)
        {
            _logger.LogWarning("No job order with InvoiceRef {InvoiceRef} found; skipping invoice data update", invoiceRef);
            return false;
        }

        order.InvoiceRef = invoiceNumber;
        order.InvoiceAmount = invoiceAmount;
        order.ModifiedOn = DateTime.UtcNow;
        order.ModifiedBy = modifiedBy;

        await _writeContext.SaveChangesAsync();

        _logger.LogInformation(
            "Updated job order {OrderId} invoice data (invoiceNumber={InvoiceNumber}, amount={Amount}) after mark-sent for InvoiceRef {InvoiceRef}",
            order.OrderId,
            invoiceNumber,
            invoiceAmount,
            invoiceRef);

        return true;
    }

    public async Task<IReadOnlyList<BillingClientOption>> GetBillingClientsAsync(string? query)
    {
        var endpoint = string.IsNullOrWhiteSpace(query)
            ? "/clients?per_page=100"
            : $"/clients?filter={Uri.EscapeDataString(query)}&per_page=20";

        var clients = await _invoiceNinjaClient.GetAsync<List<InvoiceNinjaClientResponse>>(endpoint);
        if (clients == null) return Array.Empty<BillingClientOption>();

        return clients
            .Select(c => new BillingClientOption
            {
                ExternalClientId = c.Id,
                Name = c.Name,
                DisplayName = !string.IsNullOrWhiteSpace(c.DisplayName) ? c.DisplayName : c.Name,
                IdNumber = c.IdNumber,
                OutstandingBalance = c.Balance,
            })
            .ToList();
    }

    public async Task<IReadOnlyList<BillingGroupOption>> GetBillingGroupsAsync()
    {
        var groups = await _invoiceNinjaClient.GetAsync<List<InvoiceNinjaGroupResponse>>("/group_settings?per_page=200");
        if (groups == null) return Array.Empty<BillingGroupOption>();

        return groups
            .Select(g => new BillingGroupOption
            {
                ExternalGroupId = g.Id,
                Name = g.Name,
            })
            .ToList();
    }

    public async Task<BillingStatementLaunchRequest> PrepareClientStatementLaunchAsync(BillingStatementLaunchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalized = new BillingStatementLaunchRequest
        {
            ExternalClientId = request.ExternalClientId?.Trim() ?? string.Empty,
            DateRangePreset = NormalizeStatementDateRangePreset(request.DateRangePreset),
            Status = NormalizeStatementStatus(request.Status),
            IncludeCredits = request.IncludeCredits,
            IncludePayments = request.IncludePayments,
            IncludeAging = request.IncludeAging,
        };

        ValidateStatementLaunchRequest(normalized);

        var client = await _invoiceNinjaClient.GetAsync<InvoiceNinjaClientResponse>($"/clients/{normalized.ExternalClientId}");
        if (client == null)
        {
            throw BillingException.NotFound($"Client {normalized.ExternalClientId}");
        }

        return normalized;
    }

    public async Task<BillingStatementDocument> GetClientStatementAsync(BillingStatementLaunchRequest request)
    {
        var normalized = await PrepareClientStatementLaunchAsync(request);
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var today = ResolveStatementToday(utcNow);
        var (startDate, endDate) = ResolveStatementDateRange(normalized.DateRangePreset, today);

        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["client_id"] = normalized.ExternalClientId,
        };

        if (normalized.IncludeCredits)
        {
            payload["show_credits_table"] = true;
        }

        if (normalized.IncludePayments)
        {
            payload["show_payments_table"] = true;
        }

        if (normalized.IncludeAging)
        {
            payload["show_aging_table"] = true;
        }

        if (!string.IsNullOrWhiteSpace(startDate))
        {
            payload["start_date"] = startDate;
        }

        if (!string.IsNullOrWhiteSpace(endDate))
        {
            payload["end_date"] = endDate;
        }

        var response = await _invoiceNinjaClient.PostStreamAsync("/client_statement", payload);
        if (response.Content.Length == 0)
        {
            throw BillingException.NotFound($"Client statement for {normalized.ExternalClientId}");
        }

        return new BillingStatementDocument
        {
            Content = response.Content,
            ContentType = string.IsNullOrWhiteSpace(response.ContentType) ? "application/pdf" : response.ContentType,
            FileName = string.IsNullOrWhiteSpace(response.FileName)
                ? BuildClientStatementFileName(normalized.ExternalClientId, response.ContentType)
                : response.FileName,
        };
    }

    public async Task<InvoiceEditorDto> GetInvoiceEditorDetailAsync(string externalInvoiceId)
    {
        var invoice = await _invoiceNinjaClient.GetAsync<InvoiceNinjaInvoiceResponse>(
            $"/invoices/{externalInvoiceId}?include=client");
        if (invoice == null)
            throw BillingException.NotFound($"Invoice {externalInvoiceId}");

        var customFields = _billingOptions.Value.InvoiceNinja.CustomFields;

        BillingClientOption? clientOption = null;
        if (invoice.Client != null)
        {
            clientOption = new BillingClientOption
            {
                ExternalClientId = invoice.Client.Id,
                Name = invoice.Client.Name,
                DisplayName = !string.IsNullOrWhiteSpace(invoice.Client.DisplayName)
                    ? invoice.Client.DisplayName
                    : invoice.Client.Name,
                IdNumber = invoice.Client.IdNumber,
                OutstandingBalance = invoice.Client.Balance,
            };
        }
        else if (!string.IsNullOrWhiteSpace(invoice.ClientId))
        {
            clientOption = new BillingClientOption
            {
                ExternalClientId = invoice.ClientId,
                DisplayName = invoice.ClientId
            };
        }

        var jobNumber = !string.IsNullOrWhiteSpace(customFields.InvoiceJobNo)
            ? invoice.GetCustomValue(customFields.InvoiceJobNo)
            : string.Empty;

        var lineItems = invoice.LineItems.Select((li, i) => new InvoiceEditorLineItemDto
        {
            Id = $"line-{i}",
            PoNumber = !string.IsNullOrWhiteSpace(customFields.ProductPoNo) ? li.GetCustomValue(customFields.ProductPoNo) : string.Empty,
            Description = li.Description,
            Qty = li.Quantity,
            Unit = !string.IsNullOrWhiteSpace(customFields.ProductUnit) ? li.GetCustomValue(customFields.ProductUnit) : string.Empty,
            UnitCost = li.Cost,
            LineTotal = Math.Round(li.Quantity * li.Cost, 2)
        }).ToList();

        return new InvoiceEditorDto
        {
            ExternalInvoiceId = invoice.Id,
            Status = ResolveInvoiceStatus(invoice),
            Client = clientOption,
            InvoiceDate = string.IsNullOrWhiteSpace(invoice.InvoiceDate) ? null : invoice.InvoiceDate,
            DueDate = string.IsNullOrWhiteSpace(invoice.DueDate) ? null : invoice.DueDate,
            JobNumber = jobNumber,
            LineItems = lineItems,
            TotalAmount = lineItems.Sum(l => l.LineTotal)
        };
    }

    public async Task<IReadOnlyList<InvoiceEditorAutofillLookupItemDto>> LookupInvoiceEditorAutofillAsync(IReadOnlyList<string> canonicalJobNumbers)
    {
        if (_readContext is null)
        {
            throw new BillingException("DATA_CONTEXT_UNAVAILABLE", "Invoice editor autofill is unavailable in the current runtime mode.");
        }

        if (canonicalJobNumbers.Count == 0)
        {
            return Array.Empty<InvoiceEditorAutofillLookupItemDto>();
        }

        var orderedReferences = new List<CanonicalJobReference>();
        var invalidItems = new List<InvoiceEditorAutofillLookupItemDto>();
        var seenCanonical = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var jobNumber in canonicalJobNumbers)
        {
            var trimmed = jobNumber?.Trim() ?? string.Empty;
            if (trimmed.Length == 0 || !seenCanonical.Add(trimmed))
            {
                continue;
            }

            if (!BillingInvoiceAutofillHelper.TryParseCanonicalJobNumber(trimmed, out var reference) || reference is null)
            {
                invalidItems.Add(new InvoiceEditorAutofillLookupItemDto
                {
                    CanonicalJobNumber = BillingInvoiceAutofillHelper.SanitizeForJson(trimmed),
                    Status = InvoiceEditorAutofillLookupStatuses.Unresolved,
                    Message = "Unsupported canonical job number format."
                });
                continue;
            }

            orderedReferences.Add(reference);
        }

        var orderNumbers = orderedReferences
            .Select(item => BillingInvoiceAutofillHelper.NormalizeOrderNumber(item.OrderNumber))
            .Where(item => item.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var jobSuffixes = orderedReferences.Select(item => item.JobSuffix).Distinct().ToList();

        var suffixPredicate = BuildJobSuffixPredicate(jobSuffixes);

        var jobs = await _readContext.JobOrders
            .AsNoTracking()
            .Where(job => !job.Retired)
            .Where(suffixPredicate)
            .Select(job => new
            {
                job.OrderId,
                job.OrderNumber,
                job.JobNumber,
                job.CustomerRef,
                job.PONumber,
                job.OriginalPONumber,
                job.ProductDetails,
            })
            .ToListAsync();

        var matchingJobs = jobs
            .Where(job => orderNumbers.Contains(BillingInvoiceAutofillHelper.NormalizeOrderNumber(job.OrderNumber)))
            .ToList();

        var jobsByCanonical = matchingJobs
            .Where(job => !string.IsNullOrWhiteSpace(job.OrderNumber) && job.JobNumber.HasValue)
            .GroupBy(
                job => BillingInvoiceAutofillHelper.BuildCanonicalLookupKey(job.OrderNumber, job.JobNumber!.Value),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var resolvedItems = orderedReferences.Select(reference =>
        {
            var lookupKey = BillingInvoiceAutofillHelper.BuildCanonicalLookupKey(reference.OrderNumber, reference.JobSuffix);
            if (!jobsByCanonical.TryGetValue(lookupKey, out var job))
            {
                return new InvoiceEditorAutofillLookupItemDto
                {
                    CanonicalJobNumber = BillingInvoiceAutofillHelper.SanitizeForJson(reference.CanonicalJobNumber),
                    Status = InvoiceEditorAutofillLookupStatuses.Unresolved,
                    Message = "Job number could not be resolved."
                };
            }

            var description = BillingInvoiceAutofillHelper.ExtractSectionOneDescription(job.ProductDetails);
            var missingSectionOne = string.IsNullOrWhiteSpace(description);
            var purchaseOrder = !string.IsNullOrWhiteSpace(job.CustomerRef)
                ? job.CustomerRef
                : !string.IsNullOrWhiteSpace(job.PONumber)
                    ? job.PONumber
                    : job.OriginalPONumber;

            return new InvoiceEditorAutofillLookupItemDto
            {
                CanonicalJobNumber = BillingInvoiceAutofillHelper.SanitizeForJson(reference.CanonicalJobNumber),
                OrderId = job.OrderId,
                PurchaseOrder = BillingInvoiceAutofillHelper.SanitizeForJson(purchaseOrder),
                ProductDetails = BillingInvoiceAutofillHelper.SanitizeForJson(job.ProductDetails),
                Description = BillingInvoiceAutofillHelper.SanitizeForJson(description),
                Status = missingSectionOne
                    ? InvoiceEditorAutofillLookupStatuses.ResolvedButMissingSection1
                    : InvoiceEditorAutofillLookupStatuses.Resolved,
                Message = missingSectionOne ? "Section 1 could not be extracted. Manual review required." : string.Empty,
            };
        });

        return resolvedItems.Concat(invalidItems).ToList();
    }

    private static Expression<Func<JB2026.EfCore.Models.JobOrder, bool>> BuildJobSuffixPredicate(IReadOnlyCollection<int> jobSuffixes)
    {
        if (jobSuffixes.Count == 0)
        {
            return job => false;
        }

        var parameter = Expression.Parameter(typeof(JB2026.EfCore.Models.JobOrder), "job");
        var jobNumber = Expression.Property(parameter, nameof(JB2026.EfCore.Models.JobOrder.JobNumber));
        var hasValue = Expression.Property(jobNumber, nameof(Nullable<int>.HasValue));
        var value = Expression.Property(jobNumber, nameof(Nullable<int>.Value));

        Expression? matchesAnySuffix = null;
        foreach (var suffix in jobSuffixes)
        {
            var equalsSuffix = Expression.Equal(value, Expression.Constant(suffix));
            matchesAnySuffix = matchesAnySuffix is null
                ? equalsSuffix
                : Expression.OrElse(matchesAnySuffix, equalsSuffix);
        }

        var body = Expression.AndAlso(hasValue, matchesAnySuffix!);
        return Expression.Lambda<Func<JB2026.EfCore.Models.JobOrder, bool>>(body, parameter);
    }

    public async Task<InvoiceBillingSummary> CreateInvoiceFromEditorAsync(CreateInvoiceEditorRequest request)
    {
        ValidateEditorRequest(request.ExternalClientId, request.InvoiceDate, request.DueDate, request.LineItems);

        var customFields = _billingOptions.Value.InvoiceNinja.CustomFields;

        var lineItems = request.LineItems.Select(item =>
        {
            var li = new CreateInvoiceLineItemRequest
            {
                Description = item.Description,
                Quantity = item.Qty,
                Cost = item.UnitCost,
            };
            if (!string.IsNullOrWhiteSpace(customFields.ProductPoNo))
                li.SetCustomValue(customFields.ProductPoNo, item.PoNumber);
            if (!string.IsNullOrWhiteSpace(customFields.ProductUnit))
                li.SetCustomValue(customFields.ProductUnit, item.Unit);
            return li;
        }).ToList();

        var createRequest = new CreateInvoiceNinjaInvoiceRequest
        {
            ClientId = request.ExternalClientId,
            Date = request.InvoiceDate,
            DueDate = request.DueDate,
            LineItems = lineItems
        };
        if (!string.IsNullOrWhiteSpace(customFields.InvoiceJobNo))
            createRequest.SetCustomValue(customFields.InvoiceJobNo, request.JobNumber);

        var created = await _invoiceNinjaClient.PostAsync<InvoiceNinjaInvoiceResponse>(
            "/invoices",
            createRequest);

        _logger.LogInformation("Invoice {InvoiceNumber} created via editor", created.Number);
        return MapToInvoiceBillingSummary(created);
    }

    public async Task<InvoiceBillingSummary> UpdateInvoiceFromEditorAsync(
        string externalInvoiceId,
        UpdateInvoiceEditorRequest request)
    {
        ValidateEditorRequest(request.ExternalClientId, request.InvoiceDate, request.DueDate, request.LineItems);

        var existing = await _invoiceNinjaClient.GetAsync<InvoiceNinjaInvoiceResponse>($"/invoices/{externalInvoiceId}");
        if (existing == null)
            throw BillingException.NotFound($"Invoice {externalInvoiceId}");

        var currentStatus = ResolveInvoiceStatus(existing);
        if (!string.Equals(currentStatus, "Draft", StringComparison.OrdinalIgnoreCase))
            throw BillingException.InvalidRequest(
                $"Invoice {externalInvoiceId} is in status '{currentStatus}' and cannot be edited. Only Draft invoices can be updated.",
                400);

        var customFields = _billingOptions.Value.InvoiceNinja.CustomFields;

        var lineItems = request.LineItems.Select(item =>
        {
            var li = new CreateInvoiceLineItemRequest
            {
                Description = item.Description,
                Quantity = item.Qty,
                Cost = item.UnitCost,
            };
            if (!string.IsNullOrWhiteSpace(customFields.ProductPoNo))
                li.SetCustomValue(customFields.ProductPoNo, item.PoNumber);
            if (!string.IsNullOrWhiteSpace(customFields.ProductUnit))
                li.SetCustomValue(customFields.ProductUnit, item.Unit);
            return li;
        }).ToList();

        var updateRequest = new CreateInvoiceNinjaInvoiceRequest
        {
            ClientId = request.ExternalClientId,
            Date = request.InvoiceDate,
            DueDate = request.DueDate,
            LineItems = lineItems
        };
        if (!string.IsNullOrWhiteSpace(customFields.InvoiceJobNo))
            updateRequest.SetCustomValue(customFields.InvoiceJobNo, request.JobNumber);

        var updated = await _invoiceNinjaClient.PutAsync<InvoiceNinjaInvoiceResponse>(
            $"/invoices/{externalInvoiceId}",
            updateRequest);

        _logger.LogInformation("Invoice {ExternalInvoiceId} updated via editor", externalInvoiceId);
        return MapToInvoiceBillingSummary(updated);
    }

    private static void ValidateEditorRequest(
        string externalClientId,
        string? invoiceDate,
        string? dueDate,
        List<InvoiceEditorLineItemRequest> lineItems)
    {
        if (string.IsNullOrWhiteSpace(externalClientId))
            throw BillingException.InvalidRequest("Client selection is required.", 400);
        if (string.IsNullOrWhiteSpace(invoiceDate))
            throw BillingException.InvalidRequest("Invoice date is required.", 400);
        if (string.IsNullOrWhiteSpace(dueDate))
            throw BillingException.InvalidRequest("Due date is required.", 400);
        if (lineItems == null || lineItems.Count == 0)
            throw BillingException.InvalidRequest("At least one line item is required.", 400);
        foreach (var item in lineItems)
        {
            if (item.Qty < 0)
                throw BillingException.InvalidRequest("Line item quantity cannot be negative.", 400);
            if (item.UnitCost < 0)
                throw BillingException.InvalidRequest("Line item unit cost cannot be negative.", 400);
        }
    }

    private static void ValidateStatementLaunchRequest(BillingStatementLaunchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ExternalClientId))
        {
            throw BillingException.InvalidRequest("Client selection is required.", 400);
        }

        if (!string.Equals(request.Status, BillingStatementStatuses.All, StringComparison.Ordinal))
        {
            throw BillingException.InvalidRequest(
                "The selected status option is not currently supported for statement generation.",
                400);
        }
    }

    private static string NormalizeStatementDateRangePreset(string? preset)
    {
        var value = preset?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return BillingStatementDateRangePresets.AllOutstanding;
        }

        return value.ToLowerInvariant() switch
        {
            "all outstanding" => BillingStatementDateRangePresets.AllOutstanding,
            "this month" => BillingStatementDateRangePresets.ThisMonth,
            "last month" => BillingStatementDateRangePresets.LastMonth,
            "this quarter" => BillingStatementDateRangePresets.ThisQuarter,
            "this year" => BillingStatementDateRangePresets.ThisYear,
            _ => throw BillingException.InvalidRequest($"Unsupported statement date range preset '{value}'.", 400),
        };
    }

    private static string NormalizeStatementStatus(string? status)
    {
        var value = status?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return BillingStatementStatuses.All;
        }

        return value.ToLowerInvariant() switch
        {
            "all" => BillingStatementStatuses.All,
            "paid" => BillingStatementStatuses.Paid,
            "unpaid" => BillingStatementStatuses.Unpaid,
            _ => throw BillingException.InvalidRequest($"Unsupported statement status '{value}'.", 400),
        };
    }

    private DateTime ResolveStatementToday(DateTime utcNow)
    {
        var timeZoneId = _settingsService?.Get().TimeZone?.Trim();
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return utcNow.Date;
        }

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone).Date;
        }
        catch (TimeZoneNotFoundException ex)
        {
            _logger.LogWarning(ex, "Falling back to UTC for billing statement date range because timezone {TimeZoneId} was not found.", timeZoneId);
        }
        catch (InvalidTimeZoneException ex)
        {
            _logger.LogWarning(ex, "Falling back to UTC for billing statement date range because timezone {TimeZoneId} is invalid.", timeZoneId);
        }

        return utcNow.Date;
    }

    private static (string? StartDate, string? EndDate) ResolveStatementDateRange(string preset, DateTime today)
    {
        return preset switch
        {
            BillingStatementDateRangePresets.AllOutstanding => FormatDateRange(
                new DateTime(2000, 1, 1),
                today),
            BillingStatementDateRangePresets.ThisMonth => FormatDateRange(
                new DateTime(today.Year, today.Month, 1),
                new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month))),
            BillingStatementDateRangePresets.LastMonth => ResolveLastMonthDateRange(today),
            BillingStatementDateRangePresets.ThisQuarter => ResolveThisQuarterDateRange(today),
            BillingStatementDateRangePresets.ThisYear => FormatDateRange(
                new DateTime(today.Year, 1, 1),
                new DateTime(today.Year, 12, 31)),
            _ => throw BillingException.InvalidRequest($"Unsupported statement date range preset '{preset}'.", 400),
        };
    }

    private static (string StartDate, string EndDate) ResolveLastMonthDateRange(DateTime today)
    {
        var previousMonth = today.Month == 1 ? 12 : today.Month - 1;
        var previousYear = today.Month == 1 ? today.Year - 1 : today.Year;

        return FormatDateRange(
            new DateTime(previousYear, previousMonth, 1),
            new DateTime(previousYear, previousMonth, DateTime.DaysInMonth(previousYear, previousMonth)));
    }

    private static (string StartDate, string EndDate) ResolveThisQuarterDateRange(DateTime today)
    {
        var quarterStartMonth = ((today.Month - 1) / 3) * 3 + 1;
        var quarterEndMonth = quarterStartMonth + 2;

        return FormatDateRange(
            new DateTime(today.Year, quarterStartMonth, 1),
            new DateTime(today.Year, quarterEndMonth, DateTime.DaysInMonth(today.Year, quarterEndMonth)));
    }

    private static (string StartDate, string EndDate) FormatDateRange(DateTime startDate, DateTime endDate)
    {
        return (startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
    }

    private static string BuildClientStatementFileName(string externalClientId, string? contentType)
    {
        var extension = string.Equals(contentType, "text/html", StringComparison.OrdinalIgnoreCase) ? "html" : "pdf";
        return $"client-statement-{externalClientId}.{extension}";
    }

    private async Task<(string InvoiceNinjaClientId, string BillTo, string ShipTo)> ResolveCustomerBillingMetadataForJobAsync(
        string? customerRef,
        string? customerName)
    {
        if (_readContext is null)
        {
            return (string.Empty, string.Empty, string.Empty);
        }

        var candidates = await _readContext.Customers
            .AsNoTracking()
            .Where(c => !c.Retired)
            .ToListAsync();

        foreach (var customer in candidates)
        {
            var parsed = ParseCustomerMetadata(customer.MetadataXml);

            var matchesCode = !string.IsNullOrWhiteSpace(customerRef)
                && string.Equals(parsed.CustomerCode, customerRef, StringComparison.OrdinalIgnoreCase);
            var matchesName = !string.IsNullOrWhiteSpace(customerName)
                && string.Equals(customer.CustomerName, customerName, StringComparison.OrdinalIgnoreCase);

            if (!matchesCode && !matchesName)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(parsed.InvoiceNinjaClientId))
            {
                return (parsed.InvoiceNinjaClientId, parsed.BillTo, parsed.ShipTo);
            }
        }

        return (string.Empty, string.Empty, string.Empty);
    }

    private static (string CustomerCode, string BillTo, string ShipTo, string InvoiceNinjaClientId) ParseCustomerMetadata(string? metadataRaw)
    {
        if (string.IsNullOrWhiteSpace(metadataRaw))
        {
            return (string.Empty, string.Empty, string.Empty, string.Empty);
        }

        var invoiceNinjaClientId = CustomerBillingMetadataHelper.ExtractBillingMetadata(metadataRaw).InvoiceNinjaClientId ?? string.Empty;
        var customerCode = string.Empty;
        var billTo = string.Empty;
        var shipTo = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(metadataRaw);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                var root = document.RootElement;

                if (root.TryGetProperty("CustomerCode", out var customerCodeElement) && customerCodeElement.ValueKind == JsonValueKind.String)
                {
                    customerCode = customerCodeElement.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("BillTo", out var billToElement) && billToElement.ValueKind == JsonValueKind.String)
                {
                    billTo = billToElement.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("ShipToAddresses", out var shipToArray) && shipToArray.ValueKind == JsonValueKind.Array)
                {
                    var shipToParts = shipToArray.EnumerateArray()
                        .Where(entry => entry.ValueKind == JsonValueKind.Object)
                        .Select(entry =>
                        {
                            var name = entry.TryGetProperty("Name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                                ? nameElement.GetString() ?? string.Empty
                                : string.Empty;
                            var address = entry.TryGetProperty("Address", out var addressElement) && addressElement.ValueKind == JsonValueKind.String
                                ? addressElement.GetString() ?? string.Empty
                                : string.Empty;

                            var merged = string.Join("\n", new[] { name, address }.Where(x => !string.IsNullOrWhiteSpace(x)));
                            return merged.Trim();
                        })
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();

                    shipTo = string.Join("\n\n", shipToParts);
                }

                if (string.IsNullOrWhiteSpace(invoiceNinjaClientId)
                    && root.TryGetProperty("invoiceNinjaClientId", out var clientIdElement)
                    && clientIdElement.ValueKind == JsonValueKind.String)
                {
                    invoiceNinjaClientId = clientIdElement.GetString() ?? string.Empty;
                }
            }
        }
        catch
        {
            // Metadata may not be JSON; keep fallback values.
        }

        return (customerCode.Trim(), billTo.Trim(), shipTo.Trim(), invoiceNinjaClientId.Trim());
    }

    private static (string CustomerCode, string BillTo, List<string> ShipToAddresses, string InvoiceNinjaClientId, string Group) ParseCustomerSyncMetadata(string? metadataRaw)
    {
        if (string.IsNullOrWhiteSpace(metadataRaw))
        {
            return (string.Empty, string.Empty, new List<string>(), string.Empty, string.Empty);
        }

        var invoiceNinjaClientId = CustomerBillingMetadataHelper.ExtractBillingMetadata(metadataRaw).InvoiceNinjaClientId ?? string.Empty;
        var customerCode = string.Empty;
        var billTo = string.Empty;
        var shipToAddresses = new List<string>();
        var group = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(metadataRaw);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                var root = document.RootElement;

                if (root.TryGetProperty("CustomerCode", out var customerCodeElement) && customerCodeElement.ValueKind == JsonValueKind.String)
                {
                    customerCode = customerCodeElement.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("BillTo", out var billToElement) && billToElement.ValueKind == JsonValueKind.String)
                {
                    billTo = billToElement.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("Group", out var groupElement) && groupElement.ValueKind == JsonValueKind.String)
                {
                    group = groupElement.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("ShipToAddresses", out var shipToArray) && shipToArray.ValueKind == JsonValueKind.Array)
                {
                    shipToAddresses = shipToArray.EnumerateArray()
                        .Where(entry => entry.ValueKind == JsonValueKind.Object)
                        .Select(entry =>
                        {
                            var name = entry.TryGetProperty("Name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                                ? nameElement.GetString() ?? string.Empty
                                : string.Empty;
                            var address = entry.TryGetProperty("Address", out var addressElement) && addressElement.ValueKind == JsonValueKind.String
                                ? addressElement.GetString() ?? string.Empty
                                : string.Empty;
                            return string.Join("\n", new[] { name, address }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
                        })
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();
                }

                if (string.IsNullOrWhiteSpace(invoiceNinjaClientId)
                    && root.TryGetProperty("invoiceNinjaClientId", out var clientIdElement)
                    && clientIdElement.ValueKind == JsonValueKind.String)
                {
                    invoiceNinjaClientId = clientIdElement.GetString() ?? string.Empty;
                }
            }
        }
        catch
        {
            // Metadata may not be JSON; keep fallback values.
        }

        return (customerCode.Trim(), billTo.Trim(), shipToAddresses, invoiceNinjaClientId.Trim(), group.Trim());
    }

    private Dictionary<string, object?> BuildClientCreatePayload(
        string customerName,
        string customerCode,
        string billTo,
        List<string> shipToAddresses,
        string? groupSettingsId = null)
    {
        var options = _billingOptions.Value.InvoiceNinja;
        var customFields = options.CustomFields;

        var shipToBlock = string.Join("\n\n", shipToAddresses);

        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = customerName,
            ["id_number"] = customerCode,
            ["email"] = string.Empty,
            ["currency_id"] = string.Empty,
        };

        if (!string.IsNullOrWhiteSpace(customFields.ClientBillTo))
        {
            payload[customFields.ClientBillTo] = billTo;
        }

        if (!string.IsNullOrWhiteSpace(customFields.ClientShipTo))
        {
            payload[customFields.ClientShipTo] = shipToBlock;
        }

        if (string.IsNullOrWhiteSpace(groupSettingsId))
        {
            payload["group_settings_id"] = null;
            _logger.LogDebug("Setting group_settings_id to null (clear group) in client payload");
        }
        else
        {
            payload["group_settings_id"] = groupSettingsId;
            _logger.LogDebug("Added group_settings_id [{GroupId}] to client payload", groupSettingsId);
        }

        return payload;
    }

    private Dictionary<string, object?> BuildClientUpdatePayload(
        string customerName,
        string customerCode,
        string billTo,
        List<string> shipToAddresses,
        string? groupSettingsId = null)
    {
        return BuildClientCreatePayload(customerName, customerCode, billTo, shipToAddresses, groupSettingsId);
    }

    /// <summary>
    /// Workaround: IN v5 PUT /clients/{id} does not reliably persist group_settings_id.
    /// Uses the bulk assign_group endpoint which does a direct DB update.
    /// </summary>
    private async Task AssignGroupBulkAsync(string clientId, string groupSettingsId)
    {
        try
        {
            var bulkPayload = new Dictionary<string, object?>
            {
                ["action"] = "assign_group",
                ["ids"] = new List<string> { clientId },
                ["group_settings_id"] = groupSettingsId
            };
            await _invoiceNinjaClient.PostAsync<List<InvoiceNinjaClientResponse>>("/clients/bulk", bulkPayload);
            _logger.LogDebug("assign_group bulk action succeeded for client {ClientId}", clientId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "assign_group bulk action failed for client {ClientId} — group may not be persisted in IN", clientId);
        }
    }

    private async Task<string?> TryFindInvoiceNinjaClientIdByCustomerCodeAsync(string customerCode)
    {
        if (string.IsNullOrWhiteSpace(customerCode))
        {
            return null;
        }

        try
        {
            var encodedCode = Uri.EscapeDataString(customerCode.Trim());
            var bySearch = await _invoiceNinjaClient.GetAsync<List<InvoiceNinjaClientResponse>>($"/clients?search={encodedCode}");

            var matched = bySearch?.FirstOrDefault(client =>
                string.Equals(client.IdNumber?.Trim(), customerCode.Trim(), StringComparison.OrdinalIgnoreCase));

            if (matched != null)
            {
                return matched.Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to reconcile Invoice Ninja client by customer code {CustomerCode}", customerCode);
        }

        return null;
    }

    private async Task PersistCustomerBillingClientIdAsync(string jb2026CustomerId, string invoiceNinjaClientId)
    {
        if (_writeContext is null || string.IsNullOrWhiteSpace(jb2026CustomerId) || string.IsNullOrWhiteSpace(invoiceNinjaClientId))
        {
            return;
        }

        if (!Guid.TryParse(jb2026CustomerId, out var customerGuid))
        {
            _logger.LogWarning("Unable to persist Invoice Ninja client ID because customerId is invalid: {CustomerId}", jb2026CustomerId);
            return;
        }

        var customer = await _writeContext.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerGuid && !c.Retired);
        if (customer is null)
        {
            _logger.LogWarning("Unable to persist Invoice Ninja client ID because customer was not found: {CustomerId}", jb2026CustomerId);
            return;
        }

        var updatedMetadata = CustomerBillingMetadataHelper.MarkSyncSuccessful(customer.MetadataXml, invoiceNinjaClientId);
        customer.MetadataXml = CustomerBillingMetadataHelper.MergeBillingMetadata(customer.MetadataXml, updatedMetadata);
        customer.ModifiedOn = DateTime.UtcNow;

        await _writeContext.SaveChangesAsync();
    }

    private InvoiceBillingSummary MapToInvoiceBillingSummary(InvoiceNinjaInvoiceResponse invoice)
    {
        return new InvoiceBillingSummary
        {
            ExternalInvoiceId = invoice.Id,
            InvoiceNumber = invoice.Number,
            ClientName = ResolveClientName(invoice),
            InvoiceDate = ParseInvoiceDate(invoice.InvoiceDate),
            Amount = invoice.Amount,
            Status = ResolveInvoiceStatus(invoice),
            DueDate = ParseInvoiceDueDate(invoice.DueDate),
            LastSyncedAt = DateTime.UtcNow
        };
    }

    private static string ResolveClientName(InvoiceNinjaInvoiceResponse invoice)
    {
        if (!string.IsNullOrWhiteSpace(invoice.Client?.DisplayName))
        {
            return invoice.Client.DisplayName;
        }

        return invoice.Client?.Name ?? string.Empty;
    }

    private static string ResolveInvoiceStatus(InvoiceNinjaInvoiceResponse invoice)
    {
        if (invoice.IsDeleted)
        {
            return "Deleted";
        }

        if (!string.IsNullOrWhiteSpace(invoice.StatusId))
        {
            return invoice.StatusId.Trim() switch
            {
                "1" => "Draft",
                "2" => "Sent",
                "3" => "Partial",
                "4" => "Paid",
                "5" => "Cancelled",
                "6" => "Reversed",
                "-1" => "Overdue",
                "-2" => "Unpaid",
                _ => !string.IsNullOrWhiteSpace(invoice.Status) ? invoice.Status : string.Empty
            };
        }

        return !string.IsNullOrWhiteSpace(invoice.Status) ? invoice.Status : string.Empty;
    }

    private static DateTime? ParseInvoiceDueDate(string dueDate)
    {
        if (string.IsNullOrWhiteSpace(dueDate))
        {
            return null;
        }

        return DateTime.TryParse(dueDate, out var parsed)
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
            : null;
    }

    private static DateTime? ParseInvoiceDate(string invoiceDate)
    {
        if (string.IsNullOrWhiteSpace(invoiceDate))
        {
            return null;
        }

        return DateTime.TryParse(invoiceDate, out var parsed)
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
            : null;
    }

    private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
        return dateTime;
    }

    public async Task<byte[]> DownloadInvoicePdfAsync(string externalInvoiceId)
    {
        try
        {
            _logger.LogInformation("Downloading invoice PDF for invoice {ExternalInvoiceId}", externalInvoiceId);

            // Fetch the invoice with invitations included to extract invitation_key
            var invoice = await _invoiceNinjaClient.GetAsync<InvoiceNinjaInvoiceResponse>($"/invoices/{externalInvoiceId}?include=invitations");
            if (invoice == null)
            {
                throw BillingException.NotFound($"Invoice {externalInvoiceId}");
            }

            // Extract invitation_key from the first invitation in the array
            if (invoice.Invitations == null || invoice.Invitations.Count == 0)
            {
                throw BillingException.InvalidRequest(
                    $"Invoice {externalInvoiceId} has no invitations available for download.",
                    400);
            }

            var invitationKey = invoice.Invitations[0].Key;
            if (string.IsNullOrWhiteSpace(invitationKey))
            {
                throw BillingException.InvalidRequest(
                    $"Invoice {externalInvoiceId} invitation key is empty or invalid.",
                    400);
            }

            // Download the PDF using the invitation_key with the correct endpoint
            var pdfBytes = await _invoiceNinjaClient.GetStreamAsync($"/invoice/{invitationKey}/download");
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                throw BillingException.NotFound($"Invoice PDF for {externalInvoiceId} not found or empty");
            }

            _logger.LogInformation("Invoice PDF downloaded successfully for invoice {ExternalInvoiceId}, {ByteCount} bytes", externalInvoiceId, pdfBytes.Length);
            return pdfBytes;
        }
        catch (BillingException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download invoice PDF for {ExternalInvoiceId}: {ErrorMessage}", externalInvoiceId, ex.Message);
            throw BillingException.HttpError(0, $"Failed to download invoice PDF for {externalInvoiceId}.", ex);
        }
    }

    public async Task<byte[]> DownloadDeliveryNoteAsync(string externalInvoiceId)
    {
        try
        {
            _logger.LogInformation("Downloading delivery note for invoice {ExternalInvoiceId}", externalInvoiceId);

            // Verify the invoice exists first
            var invoice = await _invoiceNinjaClient.GetAsync<InvoiceNinjaInvoiceResponse>($"/invoices/{externalInvoiceId}");
            if (invoice == null)
            {
                throw BillingException.NotFound($"Invoice {externalInvoiceId}");
            }

            // Download the delivery note
            var deliveryNoteBytes = await _invoiceNinjaClient.GetStreamAsync($"/invoices/{externalInvoiceId}/delivery_note");
            if (deliveryNoteBytes == null || deliveryNoteBytes.Length == 0)
            {
                throw BillingException.NotFound($"Delivery note for invoice {externalInvoiceId} not found or not available");
            }

            _logger.LogInformation("Delivery note downloaded successfully for invoice {ExternalInvoiceId}, {ByteCount} bytes", externalInvoiceId, deliveryNoteBytes.Length);
            return deliveryNoteBytes;
        }
        catch (BillingException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download delivery note for {ExternalInvoiceId}: {ErrorMessage}", externalInvoiceId, ex.Message);
            throw BillingException.HttpError(0, $"Failed to download delivery note for {externalInvoiceId}.", ex);
        }
    }
}
