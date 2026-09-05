namespace JB2026.Api.Controllers;

using JB2026.Api.Models;
using JB2026.Api.Models.Billing;
using JB2026.Api.Services;
using JB2026.Api.Services.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

/// <summary>
/// API controller for billing operations with Invoice Ninja.
/// Provides endpoints for connectivity checks, customer synchronization, invoice generation, and status retrieval.
/// </summary>
[ApiController]
[Route("api/v2/[controller]")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;
    private readonly ICurrentUserProfileService _currentUserProfileService;
    private readonly IPaperlessNgxService _paperlessNgxService;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        IBillingService billingService,
        ICurrentUserProfileService currentUserProfileService,
        IPaperlessNgxService paperlessNgxService,
        ILogger<BillingController> logger)
    {
        _billingService = billingService;
        _currentUserProfileService = currentUserProfileService;
        _paperlessNgxService = paperlessNgxService;
        _logger = logger;
    }

    /// <summary>
    /// Checks connectivity to Invoice Ninja and validates configuration.
    /// </summary>
    /// <returns>Connectivity status and message.</returns>
    /// <response code="200">Connectivity check completed successfully.</response>
    /// <response code="401">Unauthorized.</response>
    [HttpGet("connectivity")]
    [ProducesResponseType(typeof(BillingConnectivityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BillingConnectivityResponse>> CheckConnectivity()
    {
        _logger.LogInformation("Checking Invoice Ninja connectivity");

        try
        {
            var (isConnected, message) = await _billingService.CheckConnectivityAsync();

            var response = new BillingConnectivityResponse
            {
                IsConnected = isConnected,
                StatusMessage = message
            };

            _logger.LogInformation("Connectivity check completed: {IsConnected}", isConnected);
            return Ok(response);
        }
        catch (BillingException ex)
        {
            _logger.LogError(ex, "Billing service failed: {ErrorCode} - {ErrorMessage}", ex.ErrorCode, ex.Message);
            var statusCode = ex.InvoiceNinjaStatusCode switch
            {
                401 => StatusCodes.Status401Unauthorized,
                404 => StatusCodes.Status404NotFound,
                429 => StatusCodes.Status429TooManyRequests,
                503 => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connectivity check failed: {ErrorMessage}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "CONNECTIVITY_CHECK_FAILED",
                Message = "Failed to check Invoice Ninja connectivity.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Synchronizes a JB2026 customer to Invoice Ninja.
    /// If the customer was previously synced (invoiceNinjaClientId provided), it will be updated.
    /// Otherwise, a new Invoice Ninja client will be created.
    /// </summary>
    /// <param name="request">Customer sync request with mapping data.</param>
    /// <returns>Invoice Ninja client ID to persist in customer metadata.</returns>
    /// <response code="200">Customer synced successfully.</response>
    /// <response code="400">Invalid request (missing required fields).</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="500">Sync operation failed.</response>
    [HttpPost("customers/sync")]
    [ProducesResponseType(typeof(SyncCustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SyncCustomerResponse>> SyncCustomer([FromBody] SyncCustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            return BadRequest(new BillingErrorResponse
            {
                ErrorCode = "INVALID_REQUEST",
                Message = "CustomerId is required."
            });
        }

        SyncCustomerRequest effectiveRequest = request;

        try
        {
            var hasMissingCoreFields = string.IsNullOrWhiteSpace(request.CustomerCode)
                || string.IsNullOrWhiteSpace(request.CustomerName)
                || string.IsNullOrWhiteSpace(request.BillTo);

            if (hasMissingCoreFields)
            {
                effectiveRequest = await _billingService.BuildSyncCustomerRequestFromCustomerIdAsync(request.CustomerId);
                _logger.LogDebug("Loaded sync request from DB: Group=[{Group}]", effectiveRequest.Group ?? "(null)");
            }

            _logger.LogInformation("Syncing customer {CustomerCode} with Group=[{Group}]", effectiveRequest.CustomerCode, effectiveRequest.Group ?? "(null)");

            var invoiceNinjaClientId = await _billingService.SyncCustomerAsync(
                effectiveRequest.CustomerId,
                effectiveRequest.CustomerCode,
                effectiveRequest.CustomerName,
                effectiveRequest.BillTo,
                effectiveRequest.ShipToAddresses ?? new List<string>(),
                effectiveRequest.ExistingInvoiceNinjaClientId,
                effectiveRequest.Group);

            var billingMetadata = CustomerBillingMetadataHelper.MarkSyncSuccessful(
                null, // Caller should provide existing metadata if updating
                invoiceNinjaClientId);

            var response = new SyncCustomerResponse
            {
                InvoiceNinjaClientId = invoiceNinjaClientId,
                SyncedAt = DateTime.UtcNow,
                MetadataToMerge = CustomerBillingMetadataHelper.MergeBillingMetadata(null, billingMetadata)
            };

            _logger.LogInformation("Customer {CustomerCode} synced successfully", effectiveRequest.CustomerCode);
            return Ok(response);
        }
        catch (BillingException ex)
        {
            _logger.LogError(ex, "Failed to sync customer {CustomerCode}: {ErrorCode} - {ErrorMessage}", effectiveRequest.CustomerCode, ex.ErrorCode, ex.Message);
            var statusCode = ex.InvoiceNinjaStatusCode switch
            {
                401 => StatusCodes.Status401Unauthorized,
                404 => StatusCodes.Status404NotFound,
                429 => StatusCodes.Status429TooManyRequests,
                503 => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status400BadRequest
            };

            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync customer {CustomerId}: {ErrorMessage}", request.CustomerId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "SYNC_FAILED",
                Message = $"Failed to sync customer {request.CustomerId} to Invoice Ninja.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Generates an invoice in Invoice Ninja from a Job Order.
    /// Pre-condition: The associated customer must already be synced to Invoice Ninja.
    /// </summary>
    /// <param name="request">Invoice generation request with job and line item data.</param>
    /// <returns>Billing summary with external invoice ID to persist in job metadata.</returns>
    /// <response code="200">Invoice generated successfully.</response>
    /// <response code="400">Invalid request (missing required fields or customer not synced).</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="500">Invoice generation failed.</response>
    [HttpPost("invoices/generate")]
    [ProducesResponseType(typeof(GenerateInvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GenerateInvoiceResponse>> GenerateInvoice([FromBody] GenerateInvoiceRequest request)
    {
        _logger.LogInformation("Generating invoice for job {JobNumber}", request.JobNumber);

        if (string.IsNullOrWhiteSpace(request.InvoiceNinjaClientId) ||
            string.IsNullOrWhiteSpace(request.JobNumber) ||
            request.LineItems == null || request.LineItems.Count == 0)
        {
            _logger.LogWarning("Generate invoice request missing required fields or line items");
            return BadRequest(new BillingErrorResponse
            {
                ErrorCode = "INVALID_REQUEST",
                Message = "InvoiceNinjaClientId, JobNumber, and at least one LineItem are required."
            });
        }

        try
        {
            var lineItems = request.LineItems.Select(item => new GenerateInvoiceLineItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost
            }).ToList();

            var billingSummary = await _billingService.GenerateInvoiceAsync(
                request.InvoiceNinjaClientId,
                request.JobNumber,
                request.PoNumber ?? string.Empty,
                lineItems);

            var response = new GenerateInvoiceResponse
            {
                BillingSummary = billingSummary,
                CreatedAt = DateTime.UtcNow
            };

            if (request.OrderId.HasValue)
            {
                var persisted = await _billingService.PersistJobOrderBillingSummaryAsync(request.OrderId.Value, billingSummary);
                if (!persisted)
                {
                    _logger.LogWarning("Invoice generated for job {JobNumber} but billing summary could not be persisted to order {OrderId}",
                        request.JobNumber,
                        request.OrderId.Value);
                }
            }

            _logger.LogInformation("Invoice generated successfully for job {JobNumber}", request.JobNumber);
            return Ok(response);
        }
        catch (BillingException ex)
        {
            _logger.LogError(ex, "Failed to generate invoice for job {JobNumber}: {ErrorCode} - {ErrorMessage}", request.JobNumber, ex.ErrorCode, ex.Message);
            var statusCode = ex.InvoiceNinjaStatusCode switch
            {
                401 => StatusCodes.Status401Unauthorized,
                404 => StatusCodes.Status404NotFound,
                429 => StatusCodes.Status429TooManyRequests,
                503 => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate invoice for job {JobNumber}: {ErrorMessage}", request.JobNumber, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "INVOICE_GENERATION_FAILED",
                Message = $"Failed to generate invoice for job {request.JobNumber}.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Generates an invoice directly from a JB2026 job order using synchronized customer billing metadata.
    /// </summary>
    /// <param name="orderId">Job order ID.</param>
    /// <returns>Billing summary with persisted invoice linkage fields.</returns>
    [HttpPost("invoices/generate-from-job/{orderId:guid}")]
    [ProducesResponseType(typeof(GenerateInvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GenerateInvoiceResponse>> GenerateInvoiceFromJobOrder(Guid orderId)
    {
        _logger.LogInformation("Generating invoice from job order {OrderId}", orderId);

        try
        {
            var mappedRequest = await _billingService.BuildGenerateInvoiceRequestFromJobOrderAsync(orderId);

            var lineItems = mappedRequest.LineItems.Select(item => new GenerateInvoiceLineItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost
            }).ToList();

            var billingSummary = await _billingService.GenerateInvoiceAsync(
                mappedRequest.InvoiceNinjaClientId,
                mappedRequest.JobNumber,
                mappedRequest.PoNumber ?? string.Empty,
                lineItems);

            await _billingService.PersistJobOrderBillingSummaryAsync(orderId, billingSummary);

            return Ok(new GenerateInvoiceResponse
            {
                BillingSummary = billingSummary,
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (BillingException ex)
        {
            _logger.LogError(ex, "Failed to generate invoice from job order {OrderId}: {ErrorCode} - {ErrorMessage}", orderId, ex.ErrorCode, ex.Message);
            var statusCode = ex.InvoiceNinjaStatusCode switch
            {
                401 => StatusCodes.Status401Unauthorized,
                404 => StatusCodes.Status404NotFound,
                429 => StatusCodes.Status429TooManyRequests,
                503 => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status400BadRequest
            };

            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate invoice from job order {OrderId}: {ErrorMessage}", orderId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "INVOICE_GENERATION_FAILED",
                Message = $"Failed to generate invoice from job order {orderId}.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Previews an invoice before creation and returns resolved custom field values.
    /// </summary>
    /// <param name="request">Preview payload for invoice generation confirmation.</param>
    /// <returns>Preview response with resolved custom fields and totals.</returns>
    [HttpPost("invoices/preview")]
    [ProducesResponseType(typeof(PreviewInvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PreviewInvoiceResponse>> PreviewInvoice([FromBody] PreviewInvoiceRequest request)
    {
        if (request.LineItems == null || request.LineItems.Count == 0 || string.IsNullOrWhiteSpace(request.JobNumber))
        {
            return BadRequest(new BillingErrorResponse
            {
                ErrorCode = "INVALID_REQUEST",
                Message = "JobNumber and at least one LineItem are required."
            });
        }

        try
        {
            var preview = await _billingService.PreviewInvoiceAsync(request);
            return Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preview invoice for job {JobNumber}: {ErrorMessage}", request.JobNumber, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "INVOICE_PREVIEW_FAILED",
                Message = "Failed to preview invoice.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Retrieves the billing summary for an Invoice Ninja invoice by its external ID.
    /// Used for displaying invoice status in billing and job/order screens.
    /// </summary>
    /// <param name="externalInvoiceId">Invoice Ninja invoice ID.</param>
    /// <returns>Billing summary if found; 404 if not found.</returns>
    /// <response code="200">Invoice summary retrieved successfully.</response>
    /// <response code="404">Invoice not found.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="500">Summary retrieval failed.</response>
    [HttpGet("invoices/{externalInvoiceId}/summary")]
    [ProducesResponseType(typeof(GetInvoiceSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetInvoiceSummaryResponse>> GetInvoiceSummary(string externalInvoiceId)
    {
        _logger.LogInformation("Retrieving invoice summary for {ExternalInvoiceId}", externalInvoiceId);

        try
        {
            var summary = await _billingService.GetInvoiceSummaryAsync(externalInvoiceId);

            if (summary == null)
            {
                _logger.LogDebug("Invoice {ExternalInvoiceId} not found", externalInvoiceId);
                return NotFound(new BillingErrorResponse
                {
                    ErrorCode = "INVOICE_NOT_FOUND",
                    Message = $"Invoice {externalInvoiceId} not found in Invoice Ninja."
                });
            }

            var response = new GetInvoiceSummaryResponse
            {
                BillingSummary = summary
            };

            _logger.LogInformation("Invoice summary retrieved for {ExternalInvoiceId}", externalInvoiceId);
            return Ok(response);
        }
        catch (BillingException ex)
        {
            _logger.LogError(ex, "Failed to retrieve invoice summary for {ExternalInvoiceId}: {ErrorCode} - {ErrorMessage}", externalInvoiceId, ex.ErrorCode, ex.Message);
            var statusCode = ex.InvoiceNinjaStatusCode switch
            {
                401 => StatusCodes.Status401Unauthorized,
                404 => StatusCodes.Status404NotFound,
                429 => StatusCodes.Status429TooManyRequests,
                503 => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve invoice summary for {ExternalInvoiceId}: {ErrorMessage}", externalInvoiceId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "SUMMARY_RETRIEVAL_FAILED",
                Message = $"Failed to retrieve invoice summary for {externalInvoiceId}.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Refreshes the status of an Invoice Ninja invoice by fetching the latest data.
    /// </summary>
    /// <param name="externalInvoiceId">Invoice Ninja invoice ID.</param>
    /// <returns>Updated billing summary if found; 404 if not found.</returns>
    /// <response code="200">Invoice status refreshed successfully.</response>
    /// <response code="404">Invoice not found.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="500">Refresh operation failed.</response>
    [HttpPost("invoices/{externalInvoiceId}/refresh")]
    [ProducesResponseType(typeof(RefreshInvoiceStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RefreshInvoiceStatusResponse>> RefreshInvoiceStatus(string externalInvoiceId)
    {
        _logger.LogInformation("Refreshing invoice status for {ExternalInvoiceId}", externalInvoiceId);

        try
        {
            var summary = await _billingService.RefreshInvoiceStatusAsync(externalInvoiceId);

            if (summary == null)
            {
                _logger.LogWarning("Invoice {ExternalInvoiceId} not found during refresh", externalInvoiceId);
                return NotFound(new BillingErrorResponse
                {
                    ErrorCode = "INVOICE_NOT_FOUND",
                    Message = $"Invoice {externalInvoiceId} not found in Invoice Ninja."
                });
            }

            var response = new RefreshInvoiceStatusResponse
            {
                BillingSummary = summary,
                RefreshedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Invoice status refreshed for {ExternalInvoiceId}", externalInvoiceId);
            return Ok(response);
        }
        catch (BillingException ex)
        {
            _logger.LogError(ex, "Failed to refresh invoice status for {ExternalInvoiceId}: {ErrorCode} - {ErrorMessage}", externalInvoiceId, ex.ErrorCode, ex.Message);
            var statusCode = ex.InvoiceNinjaStatusCode switch
            {
                401 => StatusCodes.Status401Unauthorized,
                404 => StatusCodes.Status404NotFound,
                429 => StatusCodes.Status429TooManyRequests,
                503 => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh invoice status for {ExternalInvoiceId}: {ErrorMessage}", externalInvoiceId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "REFRESH_FAILED",
                Message = $"Failed to refresh invoice status for {externalInvoiceId}.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Lists invoices for the billing list screens.
    /// </summary>
    /// <returns>Invoice summary list.</returns>
    [HttpGet("invoices")]
    [ProducesResponseType(typeof(ListInvoicesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ListInvoicesResponse>> ListInvoices([FromQuery] string? lookup = null, [FromQuery] string? invoiceLookup = null)
    {
        try
        {
            var invoices = await _billingService.ListInvoicesAsync(lookup, invoiceLookup);
            return Ok(new ListInvoicesResponse
            {
                Invoices = invoices.ToList()
            });
        }
        catch (BillingException ex)
        {
            _logger.LogError(ex, "Failed to list invoices: {ErrorCode} - {ErrorMessage}", ex.ErrorCode, ex.Message);
            var statusCode = ex.InvoiceNinjaStatusCode switch
            {
                401 => StatusCodes.Status401Unauthorized,
                404 => StatusCodes.Status404NotFound,
                429 => StatusCodes.Status429TooManyRequests,
                503 => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list invoices: {ErrorMessage}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "INVOICE_LIST_FAILED",
                Message = "Failed to list invoices.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Sends a draft invoice via Invoice Ninja, transitioning its status from Draft to Sent.
    /// The invoice must be in Draft status; otherwise, a 400 error is returned.
    /// </summary>
    /// <param name="externalInvoiceId">Invoice Ninja invoice ID.</param>
    /// <returns>Updated billing summary with status Sent.</returns>
    /// <response code="200">Invoice sent successfully.</response>
    /// <response code="400">Invoice is not in Draft status or invalid request.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="404">Invoice not found.</response>
    /// <response code="500">Send operation failed.</response>
    [HttpPost("invoices/{externalInvoiceId}/send")]
    [ProducesResponseType(typeof(SendInvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SendInvoiceResponse>> SendInvoice(string externalInvoiceId)
    {
        if (string.IsNullOrWhiteSpace(externalInvoiceId))
        {
            return BadRequest(new BillingErrorResponse
            {
                ErrorCode = "INVALID_REQUEST",
                Message = "Invoice ID is required."
            });
        }

        _logger.LogInformation("Sending invoice {ExternalInvoiceId}", externalInvoiceId);

        try
        {
            var currentUser = _currentUserProfileService.GetCurrentUser();
            var modifiedBy = currentUser?.UserId ?? Guid.Empty;

            var billingSummary = await _billingService.SendInvoiceAsync(externalInvoiceId, modifiedBy);

            var response = new SendInvoiceResponse
            {
                BillingSummary = billingSummary,
                SentAt = DateTime.UtcNow
            };

            _logger.LogInformation("Invoice {ExternalInvoiceId} sent successfully", externalInvoiceId);
            return Ok(response);
        }
        catch (BillingException ex)
        {
            _logger.LogError(ex, "Failed to send invoice {ExternalInvoiceId}: {ErrorCode} - {ErrorMessage}", externalInvoiceId, ex.ErrorCode, ex.Message);
            var statusCode = ex.InvoiceNinjaStatusCode switch
            {
                401 => StatusCodes.Status401Unauthorized,
                404 => StatusCodes.Status404NotFound,
                429 => StatusCodes.Status429TooManyRequests,
                503 => StatusCodes.Status503ServiceUnavailable,
                400 => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invoice {ExternalInvoiceId}: {ErrorMessage}", externalInvoiceId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "INVOICE_SEND_FAILED",
                Message = $"Failed to send invoice {externalInvoiceId}.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Downloads the invoice PDF document from Invoice Ninja for the specified invoice.
    /// </summary>
    /// <param name="externalInvoiceId">The Invoice Ninja invoice ID.</param>
    /// <returns>The PDF file for download.</returns>
    /// <response code="200">Invoice PDF downloaded successfully.</response>
    /// <response code="404">Invoice not found.</response>
    /// <response code="401">Unauthorized.</response>
[HttpGet("invoices/{externalInvoiceId}/download/pdf")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadInvoicePdf(string externalInvoiceId)
    {
        _logger.LogInformation("Downloading invoice PDF for invoice {ExternalInvoiceId}", externalInvoiceId);

        try
        {
            var pdfBytes = await _billingService.DownloadInvoicePdfAsync(externalInvoiceId);

            var fileName = $"invoice-{externalInvoiceId}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (BillingException ex)
        {
            _logger.LogWarning(ex, "Failed to download invoice PDF {ExternalInvoiceId}: {ErrorCode}", externalInvoiceId, ex.ErrorCode);

            var statusCode = ex.ErrorCode switch
            {
                "NOT_FOUND" => StatusCodes.Status404NotFound,
                "INVALID_REQUEST" => StatusCodes.Status400BadRequest,
                "INVALID_API_KEY" => StatusCodes.Status401Unauthorized,
                "SERVICE_UNAVAILABLE" => StatusCodes.Status503ServiceUnavailable,
                "RATE_LIMITED" => StatusCodes.Status429TooManyRequests,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download invoice PDF {ExternalInvoiceId}: {ErrorMessage}", externalInvoiceId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "PDF_DOWNLOAD_FAILED",
                Message = $"Failed to download invoice PDF {externalInvoiceId}.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Downloads the delivery note PDF document from Invoice Ninja for the specified invoice.
    /// </summary>
    /// <param name="externalInvoiceId">The Invoice Ninja invoice ID.</param>
    /// <returns>The delivery note PDF file for download.</returns>
    /// <response code="200">Delivery note PDF downloaded successfully.</response>
    /// <response code="404">Invoice or delivery note not found.</response>
    /// <response code="401">Unauthorized.</response>
[HttpGet("invoices/{externalInvoiceId}/download/delivery-note")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadDeliveryNote(string externalInvoiceId)
    {
        _logger.LogInformation("Downloading delivery note for invoice {ExternalInvoiceId}", externalInvoiceId);

        try
        {
            var deliveryNoteBytes = await _billingService.DownloadDeliveryNoteAsync(externalInvoiceId);

            var fileName = $"delivery-note-{externalInvoiceId}.pdf";
            return File(deliveryNoteBytes, "application/pdf", fileName);
        }
        catch (BillingException ex)
        {
            _logger.LogWarning(ex, "Failed to download delivery note {ExternalInvoiceId}: {ErrorCode}", externalInvoiceId, ex.ErrorCode);

            var statusCode = ex.ErrorCode switch
            {
                "NOT_FOUND" => StatusCodes.Status404NotFound,
                "INVALID_REQUEST" => StatusCodes.Status400BadRequest,
                "INVALID_API_KEY" => StatusCodes.Status401Unauthorized,
                "SERVICE_UNAVAILABLE" => StatusCodes.Status503ServiceUnavailable,
                "RATE_LIMITED" => StatusCodes.Status429TooManyRequests,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download delivery note {ExternalInvoiceId}: {ErrorMessage}", externalInvoiceId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "DELIVERY_NOTE_DOWNLOAD_FAILED",
                Message = $"Failed to download delivery note {externalInvoiceId}.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Uploads the invoice PDF to the DMS (Paperless-ngx).
    /// Skips upload if a document with the same title (invoice number) already exists.
    /// </summary>
    /// <param name="externalInvoiceId">The Invoice Ninja invoice ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Upload result indicating whether the document already existed.</returns>
    /// <response code="200">Upload processed (may report already exists).</response>
    /// <response code="404">Invoice not found.</response>
    /// <response code="401">Unauthorized.</response>
    [HttpPost("invoices/{externalInvoiceId}/upload-to-dms")]
    [ProducesResponseType(typeof(UploadToDmsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UploadToDmsResponse>> UploadInvoiceToDms(
        string externalInvoiceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalInvoiceId))
        {
            return BadRequest(new BillingErrorResponse
            {
                ErrorCode = "INVALID_REQUEST",
                Message = "Invoice ID is required."
            });
        }

        _logger.LogInformation("Uploading invoice {ExternalInvoiceId} to DMS", externalInvoiceId);

        try
        {
            var summary = await _billingService.GetInvoiceSummaryAsync(externalInvoiceId);
            if (summary is null)
            {
                return NotFound(new BillingErrorResponse
                {
                    ErrorCode = "NOT_FOUND",
                    Message = $"Invoice {externalInvoiceId} not found."
                });
            }

            var pdfBytes = await _billingService.DownloadInvoicePdfAsync(externalInvoiceId);
            var title = string.IsNullOrWhiteSpace(summary.InvoiceNumber)
                ? externalInvoiceId
                : summary.InvoiceNumber;
            var fileName = $"invoice-{title}.pdf";

            var currentUser = _currentUserProfileService.GetCurrentUser();
            var tagName = currentUser?.DisplayName ?? currentUser?.Username;

            var result = await _paperlessNgxService.UploadInvoiceAsync(
                title,
                fileName,
                pdfBytes,
                summary.ClientName,
                tagName,
                cancellationToken);

            _logger.LogInformation("Invoice {ExternalInvoiceId} DMS upload result alreadyExists={AlreadyExists} documentId={DocumentId}",
                externalInvoiceId, result.AlreadyExists, result.DocumentId);

            return Ok(new UploadToDmsResponse
            {
                AlreadyExists = result.AlreadyExists,
                DocumentId = result.DocumentId,
                Title = title
            });
        }
        catch (BillingException ex)
        {
            _logger.LogWarning(ex, "Failed to upload invoice {ExternalInvoiceId} to DMS: {ErrorCode}", externalInvoiceId, ex.ErrorCode);

            var statusCode = ex.ErrorCode switch
            {
                "NOT_FOUND" => StatusCodes.Status404NotFound,
                "INVALID_REQUEST" => StatusCodes.Status400BadRequest,
                "INVALID_API_KEY" => StatusCodes.Status401Unauthorized,
                "SERVICE_UNAVAILABLE" => StatusCodes.Status503ServiceUnavailable,
                "RATE_LIMITED" => StatusCodes.Status429TooManyRequests,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload invoice {ExternalInvoiceId} to DMS: {ErrorMessage}", externalInvoiceId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "INVOICE_DMS_UPLOAD_FAILED",
                Message = $"Failed to upload invoice {externalInvoiceId} to DMS.",
                Details = null
            });
        }
    }

    // ── Invoice Editor Endpoints ──────────────────────────────────────────────

    /// <summary>
    /// Lists Invoice Ninja clients for the editor client picker, with optional search query.
    /// </summary>
    [HttpGet("clients")]
    [ProducesResponseType(typeof(ListBillingClientsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBillingClients([FromQuery] string? query = null)
    {
        try
        {
            var clients = await _billingService.GetBillingClientsAsync(query);
            return Ok(new ListBillingClientsResponse { Clients = clients.ToList() });
        }
        catch (BillingException ex)
        {
            _logger.LogWarning(ex, "Failed to list billing clients: {ErrorCode}", ex.ErrorCode);
            var statusCode = ex.InvoiceNinjaStatusCode switch
            {
                401 => StatusCodes.Status401Unauthorized,
                429 => StatusCodes.Status429TooManyRequests,
                503 => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error listing billing clients: {ErrorMessage}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "LIST_CLIENTS_FAILED",
                Message = "Failed to list billing clients.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Lists Invoice Ninja group settings for the admin customer dialog.
    /// </summary>
    [HttpGet("groups")]
    [ProducesResponseType(typeof(ListBillingGroupsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBillingGroups()
    {
        try
        {
            var groups = await _billingService.GetBillingGroupsAsync();
            return Ok(new ListBillingGroupsResponse { Groups = groups.ToList() });
        }
        catch (BillingException ex)
        {
            _logger.LogWarning(ex, "Failed to list billing groups: {ErrorCode}", ex.ErrorCode);
            var statusCode = ex.InvoiceNinjaStatusCode switch
            {
                401 => StatusCodes.Status401Unauthorized,
                429 => StatusCodes.Status429TooManyRequests,
                503 => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error listing billing groups: {ErrorMessage}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "LIST_GROUPS_FAILED",
                Message = "Failed to list billing groups.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Validates a client statement request and returns a launch URL for opening it in a new tab.
    /// </summary>
    [HttpPost("statements/client")]
    [ProducesResponseType(typeof(BillingStatementLaunchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateClientStatementLaunch([FromBody] BillingStatementLaunchRequest request)
    {
        try
        {
            var normalized = await _billingService.PrepareClientStatementLaunchAsync(request);
            return Ok(new BillingStatementLaunchResponse
            {
                LaunchUrl = BuildClientStatementLaunchUrl(normalized)
            });
        }
        catch (BillingException ex)
        {
            _logger.LogWarning(ex, "Failed to create client statement launch URL for client {ExternalClientId}: {ErrorCode}", request.ExternalClientId, ex.ErrorCode);
            var statusCode = ex.InvoiceNinjaStatusCode switch
            {
                400 => StatusCodes.Status400BadRequest,
                401 => StatusCodes.Status401Unauthorized,
                404 => StatusCodes.Status404NotFound,
                429 => StatusCodes.Status429TooManyRequests,
                503 => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating client statement launch URL for client {ExternalClientId}: {ErrorMessage}", request.ExternalClientId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "STATEMENT_LAUNCH_FAILED",
                Message = "Failed to create billing statement launch URL.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Retrieves the generated client statement from Invoice Ninja and serves it inline.
    /// </summary>
    [HttpGet("statements/client")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetClientStatement([FromQuery] BillingStatementLaunchRequest request)
    {
        try
        {
            var statement = await _billingService.GetClientStatementAsync(request);
            Response.Headers.ContentDisposition = $"inline; filename=\"{statement.FileName}\"";
            return File(statement.Content, statement.ContentType);
        }
        catch (BillingException ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve client statement for client {ExternalClientId}: {ErrorCode}", request.ExternalClientId, ex.ErrorCode);
            var statusCode = ex.InvoiceNinjaStatusCode switch
            {
                400 => StatusCodes.Status400BadRequest,
                401 => StatusCodes.Status401Unauthorized,
                404 => StatusCodes.Status404NotFound,
                429 => StatusCodes.Status429TooManyRequests,
                503 => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving client statement for client {ExternalClientId}: {ErrorMessage}", request.ExternalClientId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "STATEMENT_RETRIEVAL_FAILED",
                Message = "Failed to retrieve client statement.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Returns a normalized invoice editor DTO for viewing or editing an existing invoice.
    /// </summary>
    [HttpGet("invoices/{externalInvoiceId}")]
    [ProducesResponseType(typeof(GetInvoiceEditorDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInvoiceEditorDetail(string externalInvoiceId)
    {
        try
        {
            var dto = await _billingService.GetInvoiceEditorDetailAsync(externalInvoiceId);
            return Ok(new GetInvoiceEditorDetailResponse { Invoice = dto });
        }
        catch (BillingException ex)
        {
            _logger.LogWarning(ex, "Failed to get invoice editor detail {ExternalInvoiceId}: {ErrorCode}", externalInvoiceId, ex.ErrorCode);
            var statusCode = ex.InvoiceNinjaStatusCode switch
            {
                404 => StatusCodes.Status404NotFound,
                401 => StatusCodes.Status401Unauthorized,
                429 => StatusCodes.Status429TooManyRequests,
                503 => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get invoice editor detail {ExternalInvoiceId}: {ErrorMessage}", externalInvoiceId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "GET_INVOICE_DETAIL_FAILED",
                Message = $"Failed to retrieve invoice {externalInvoiceId}.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Resolves canonical job numbers into billing invoice editor autofill rows.
    /// </summary>
    [HttpPost("invoices/autofill-lookup")]
    [ProducesResponseType(typeof(LookupInvoiceEditorAutofillResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LookupInvoiceEditorAutofill([FromBody] LookupInvoiceEditorAutofillRequest request)
    {
        if (request.CanonicalJobNumbers == null || request.CanonicalJobNumbers.Count == 0)
        {
            return BadRequest(new BillingErrorResponse
            {
                ErrorCode = "INVALID_REQUEST",
                Message = "At least one canonical job number is required."
            });
        }

        try
        {
            var jobs = await _billingService.LookupInvoiceEditorAutofillAsync(request.CanonicalJobNumbers);
            return Ok(new LookupInvoiceEditorAutofillResponse { Jobs = jobs.ToList() });
        }
        catch (BillingException ex)
        {
            _logger.LogWarning(ex, "Failed to lookup invoice editor autofill jobs: {ErrorCode}", ex.ErrorCode);
            var statusCode = ex.InvoiceNinjaStatusCode switch
            {
                401 => StatusCodes.Status401Unauthorized,
                429 => StatusCodes.Status429TooManyRequests,
                503 => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error looking up invoice editor autofill jobs: {ErrorMessage}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "LOOKUP_INVOICE_AUTOFILL_FAILED",
                Message = "Failed to resolve invoice editor autofill jobs.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Creates a new invoice in Invoice Ninja from the editor form.
    /// </summary>
    [HttpPost("invoices")]
    [ProducesResponseType(typeof(SaveInvoiceEditorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceEditorRequest request)
    {
        try
        {
            var summary = await _billingService.CreateInvoiceFromEditorAsync(request);
            return StatusCode(StatusCodes.Status201Created, new SaveInvoiceEditorResponse { BillingSummary = summary });
        }
        catch (BillingException ex)
        {
            _logger.LogWarning(ex, "Failed to create invoice from editor: {ErrorCode}", ex.ErrorCode);
            var statusCode = ex.InvoiceNinjaStatusCode switch
            {
                401 => StatusCodes.Status401Unauthorized,
                429 => StatusCodes.Status429TooManyRequests,
                503 => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating invoice from editor: {ErrorMessage}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "CREATE_INVOICE_FAILED",
                Message = "Failed to create invoice.",
                Details = null
            });
        }
    }

    /// <summary>
    /// Updates a draft invoice in Invoice Ninja from the editor form.
    /// Returns 400 when the invoice is no longer in Draft status.
    /// </summary>
    [HttpPut("invoices/{externalInvoiceId}")]
    [ProducesResponseType(typeof(SaveInvoiceEditorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BillingErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateInvoice(string externalInvoiceId, [FromBody] UpdateInvoiceEditorRequest request)
    {
        try
        {
            var summary = await _billingService.UpdateInvoiceFromEditorAsync(externalInvoiceId, request);
            return Ok(new SaveInvoiceEditorResponse { BillingSummary = summary });
        }
        catch (BillingException ex)
        {
            _logger.LogWarning(ex, "Failed to update invoice {ExternalInvoiceId}: {ErrorCode}", externalInvoiceId, ex.ErrorCode);
            var statusCode = ex.InvoiceNinjaStatusCode switch
            {
                404 => StatusCodes.Status404NotFound,
                401 => StatusCodes.Status401Unauthorized,
                429 => StatusCodes.Status429TooManyRequests,
                503 => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, new BillingErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                Details = ex.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating invoice {ExternalInvoiceId}: {ErrorMessage}", externalInvoiceId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BillingErrorResponse
            {
                ErrorCode = "UPDATE_INVOICE_FAILED",
                Message = $"Failed to update invoice {externalInvoiceId}.",
                Details = null
            });
        }
    }

    private string BuildClientStatementLaunchUrl(BillingStatementLaunchRequest request)
    {
        var query = QueryString.Create(new List<KeyValuePair<string, string?>>
        {
            new("externalClientId", request.ExternalClientId),
            new("dateRangePreset", request.DateRangePreset),
            new("status", request.Status),
            new("includeCredits", request.IncludeCredits.ToString().ToLowerInvariant()),
            new("includePayments", request.IncludePayments.ToString().ToLowerInvariant()),
            new("includeAging", request.IncludeAging.ToString().ToLowerInvariant()),
        });

        var pathBase = Request.PathBase.HasValue ? Request.PathBase.Value : string.Empty;
        return $"{pathBase}/api/v2/billing/statements/client{query}";
    }
}

