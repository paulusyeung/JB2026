namespace JB2026.Api.Services.Billing;

using JB2026.Api.Models.Billing;
using JB2026.Api.Options;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

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
        string? existingInvoiceNinjaClientId);

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
    /// Previews invoice payload before creation, including resolved custom field values and warnings.
    /// </summary>
    /// <param name="request">Preview request payload.</param>
    /// <returns>Resolved preview response.</returns>
    Task<PreviewInvoiceResponse> PreviewInvoiceAsync(PreviewInvoiceRequest request);

    /// <summary>
    /// Lists invoice summaries from Invoice Ninja for billing list screens.
    /// </summary>
    /// <returns>Invoice summary list.</returns>
    Task<IReadOnlyList<InvoiceBillingSummary>> ListInvoicesAsync();

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
        string? existingInvoiceNinjaClientId)
    {
        _logger.LogInformation("Syncing customer {CustomerCode} ({CustomerId}) to Invoice Ninja", customerCode, jb2026CustomerId);

        try
        {
            // If we have an existing external ID, try to fetch and update the client
            if (!string.IsNullOrWhiteSpace(existingInvoiceNinjaClientId))
            {
                try
                {
                    var existing = await _invoiceNinjaClient.GetAsync<InvoiceNinjaClientResponse>($"/clients/{existingInvoiceNinjaClientId}");
                    if (existing != null)
                    {
                        _logger.LogDebug("Found existing Invoice Ninja client {InvoiceNinjaClientId}, updating", existingInvoiceNinjaClientId);

                        var updateRequest = BuildClientUpdatePayload(customerName, customerCode, billTo, shipToAddresses);
                        var updated = await _invoiceNinjaClient.PutAsync<InvoiceNinjaClientResponse>(
                            $"/clients/{existingInvoiceNinjaClientId}",
                            updateRequest);

                        await PersistCustomerBillingClientIdAsync(jb2026CustomerId, updated.Id);

                        return updated.Id;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch existing Invoice Ninja client {InvoiceNinjaClientId}, will create new", existingInvoiceNinjaClientId);
                }
            }

            var reconciledClientId = await TryFindInvoiceNinjaClientIdByCustomerCodeAsync(customerCode);
            if (!string.IsNullOrWhiteSpace(reconciledClientId))
            {
                _logger.LogInformation(
                    "Reconciled existing Invoice Ninja client {InvoiceNinjaClientId} using customer code {CustomerCode}",
                    reconciledClientId,
                    customerCode);

                var updateRequest = BuildClientUpdatePayload(customerName, customerCode, billTo, shipToAddresses);
                var updated = await _invoiceNinjaClient.PutAsync<InvoiceNinjaClientResponse>(
                    $"/clients/{reconciledClientId}",
                    updateRequest);

                await PersistCustomerBillingClientIdAsync(jb2026CustomerId, updated.Id);
                return updated.Id;
            }

            // Create a new client
            _logger.LogDebug("Creating new Invoice Ninja client for customer {CustomerCode}", customerCode);
            var createRequest = BuildClientCreatePayload(customerName, customerCode, billTo, shipToAddresses);
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

            // Build invoice custom fields (Job No.)
            var invoiceCustomValues = new Dictionary<string, string?>
            {
                { customFields.InvoiceJobNo, jobNumber }
            };

            // Build line items with custom fields
            var inlineItems = lineItems.Select(item => new CreateInvoiceLineItemRequest
            {
                Description = item.Description,
                Quantity = item.Quantity,
                Cost = item.UnitCost,
                CustomValues = new Dictionary<string, string?>
                {
                    { customFields.ProductPoNo, poNumber }
                }
            }).ToList();

            var createRequest = new CreateInvoiceNinjaInvoiceRequest
            {
                ClientId = invoiceNinjaClientId,
                CustomValues = invoiceCustomValues,
                LineItems = inlineItems
            };

            var created = await _invoiceNinjaClient.PostAsync<InvoiceNinjaInvoiceResponse>(
                "/invoices",
                new { invoice = createRequest });

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
            var invoice = await _invoiceNinjaClient.GetAsync<InvoiceNinjaInvoiceResponse>($"/invoices/{externalInvoiceId}");
            if (invoice == null)
            {
                _logger.LogWarning("Invoice {ExternalInvoiceId} not found in Invoice Ninja", externalInvoiceId);
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

    public async Task<IReadOnlyList<InvoiceBillingSummary>> ListInvoicesAsync()
    {
        try
        {
            var invoices = await _invoiceNinjaClient.GetAsync<List<InvoiceNinjaInvoiceResponse>>("/invoices?include=client");
            if (invoices == null)
            {
                return Array.Empty<InvoiceBillingSummary>();
            }

            return invoices.Select(MapToInvoiceBillingSummary).ToList();
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

    private static (string CustomerCode, string BillTo, List<string> ShipToAddresses, string InvoiceNinjaClientId) ParseCustomerSyncMetadata(string? metadataRaw)
    {
        if (string.IsNullOrWhiteSpace(metadataRaw))
        {
            return (string.Empty, string.Empty, new List<string>(), string.Empty);
        }

        var invoiceNinjaClientId = CustomerBillingMetadataHelper.ExtractBillingMetadata(metadataRaw).InvoiceNinjaClientId ?? string.Empty;
        var customerCode = string.Empty;
        var billTo = string.Empty;
        var shipToAddresses = new List<string>();

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

        return (customerCode.Trim(), billTo.Trim(), shipToAddresses, invoiceNinjaClientId.Trim());
    }

    private Dictionary<string, object?> BuildClientCreatePayload(
        string customerName,
        string customerCode,
        string billTo,
        List<string> shipToAddresses)
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

        return payload;
    }

    private Dictionary<string, object?> BuildClientUpdatePayload(
        string customerName,
        string customerCode,
        string billTo,
        List<string> shipToAddresses)
    {
        return BuildClientCreatePayload(customerName, customerCode, billTo, shipToAddresses);
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
        if (!string.IsNullOrWhiteSpace(invoice.Status))
        {
            return invoice.Status;
        }

        return invoice.StatusId switch
        {
            "1" => "Draft",
            "2" => "Sent",
            "3" => "Viewed",
            "4" => "Approved",
            "5" => "Partial",
            "6" => "Paid",
            "7" => "Unpaid",
            "8" => "Overdue",
            _ => string.Empty
        };
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
}
