namespace JB2026.Api.Services.Billing;

using JB2026.Api.Models.Billing;
using JB2026.EfCore.Models;

/// <summary>
/// Deterministic mapping from Job Order to Invoice Ninja invoice request data.
/// Implements the mapping contract defined in design.md §2 - Invoice Ninja custom field configuration.
/// </summary>
public static class JobOrderInvoiceMappingHelper
{
    /// <summary>
    /// Maps a Job Order to an invoice generation request with resolved custom fields.
    /// 
    /// Mapping rules (v1):
    /// - Job No. (invoice custom field) ← JobNumber
    /// - P.O.No. (line item custom field) ← PONumber (same for all lines)
    /// - Line description ← OrderTitle + ProductDetails
    /// - Line quantity ← Qty
    /// - Line unit cost ← invoiceAmount / Qty (if qty > 0)
    /// - Bill To (client custom field) ← BillTo parameter
    /// - Ship To (client custom field) ← ShipTo parameter
    /// </summary>
    /// <param name="job">Job Order to map.</param>
    /// <param name="invoiceNinjaClientId">Pre-synced Invoice Ninja client ID.</param>
    /// <param name="billTo">Bill To address block (from customer metadata).</param>
    /// <param name="shipTo">Ship To address block (from customer metadata).</param>
    /// <returns>Invoice generation request ready to send to Invoice Ninja.</returns>
    /// <exception cref="ArgumentException">If job data is invalid (e.g., no JobNumber or Qty <= 0).</exception>
    public static GenerateInvoiceRequest MapJobOrderToInvoiceRequest(
        JobOrder job,
        string invoiceNinjaClientId,
        string billTo,
        string shipTo)
    {
        if (string.IsNullOrWhiteSpace(invoiceNinjaClientId))
            throw new ArgumentException("InvoiceNinjaClientId cannot be empty.", nameof(invoiceNinjaClientId));

        if (job is null)
            throw new ArgumentNullException(nameof(job));

        if (!job.JobNumber.HasValue || job.JobNumber.Value == 0)
            throw new ArgumentException("Job Order must have a valid JobNumber.", nameof(job));

        if (!job.Qty.HasValue || job.Qty.Value <= 0)
            throw new ArgumentException("Job Order Qty must be greater than zero.", nameof(job));

        var jobNumber = job.JobNumber.Value;
        var quantity = job.Qty.Value;

        // Build line item description from OrderTitle and ProductDetails
        var lineDescription = BuildLineDescription(job.OrderTitle, job.ProductDetails);

        // Calculate unit cost if total invoice amount is available; otherwise use 0 and require manual entry
        var unitCost = job.InvoiceAmount > 0
            ? job.InvoiceAmount.Value / quantity
            : 0m;

        var request = new GenerateInvoiceRequest
        {
            InvoiceNinjaClientId = invoiceNinjaClientId,
            JobNumber = jobNumber.ToString(),
            PoNumber = job.PONumber ?? string.Empty,
            LineItems = new()
            {
                new InvoiceLineItemData
                {
                    Description = lineDescription,
                    Quantity = quantity,
                    UnitCost = unitCost
                }
            }
        };

        return request;
    }

    /// <summary>
    /// Maps a Job Order to an invoice preview request, including custom field values.
    /// </summary>
    /// <param name="job">Job Order to preview.</param>
    /// <param name="billTo">Bill To address block.</param>
    /// <param name="shipTo">Ship To address block.</param>
    /// <returns>Preview request with resolved custom field values.</returns>
    public static PreviewInvoiceRequest MapJobOrderToPreviewRequest(
        JobOrder job,
        string billTo,
        string shipTo)
    {
        if (job is null)
            throw new ArgumentNullException(nameof(job));

        if (!job.JobNumber.HasValue || job.JobNumber.Value == 0)
            throw new ArgumentException("Job Order must have a valid JobNumber.", nameof(job));

        if (!job.Qty.HasValue || job.Qty.Value <= 0)
            throw new ArgumentException("Job Order Qty must be greater than zero.", nameof(job));

        var jobNumber = job.JobNumber.Value;
        var quantity = job.Qty.Value;

        var lineDescription = BuildLineDescription(job.OrderTitle, job.ProductDetails);
        var unitCost = job.InvoiceAmount > 0
            ? job.InvoiceAmount.Value / quantity
            : 0m;

        var preview = new PreviewInvoiceRequest
        {
            CustomerName = job.CustomerName ?? string.Empty,
            BillTo = billTo,
            ShipTo = shipTo,
            JobNumber = jobNumber.ToString(),
            PoNumber = job.PONumber ?? string.Empty,
            LineItems = new()
            {
                new InvoiceLineItemData
                {
                    Description = lineDescription,
                    Quantity = quantity,
                    UnitCost = unitCost
                }
            }
        };

        return preview;
    }

    /// <summary>
    /// Builds the line item description from OrderTitle and ProductDetails.
    /// If ProductDetails is present, includes it; otherwise uses OrderTitle alone.
    /// </summary>
    private static string BuildLineDescription(string? orderTitle, string? productDetails)
    {
        var parts = new[]
        {
            (orderTitle ?? string.Empty).Trim(),
            (productDetails ?? string.Empty).Trim()
        }.Where(p => !string.IsNullOrEmpty(p)).ToList();

        return parts.Count > 0 ? string.Join(" - ", parts) : "(No description)";
    }

    /// <summary>
    /// Calculates the total amount from a Job Order's line items.
    /// For v1 (single line item), this is simply Qty * UnitCost.
    /// </summary>
    public static decimal CalculateInvoiceTotal(JobOrder job)
    {
        if (job?.Qty == null || job.InvoiceAmount == null)
            return 0m;

        return job.InvoiceAmount.Value > 0 ? job.InvoiceAmount.Value : 0m;
    }
}
