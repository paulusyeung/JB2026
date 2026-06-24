using System.Collections.Concurrent;
using JB2026.Api.Models;

namespace JB2026.Api.Services;

public sealed class InMemoryJobManagementRepository : IJobManagementRepository
{
    private readonly ConcurrentDictionary<Guid, JobRecord> _jobs;

    public InMemoryJobManagementRepository()
    {
        _jobs = new ConcurrentDictionary<Guid, JobRecord>(CreateSeedData().ToDictionary(job => job.OrderId));
    }

    public IReadOnlyList<JobListItemResponse> GetRange(DateOnly startOn, int days)
    {
        var start = startOn.ToDateTime(TimeOnly.MinValue);
        return _jobs.Values
            .Where(job => job.OrderedOn < start.AddDays(1) && job.OrderedOn > start.AddDays(-days))
            .Select(MapListItem)
            .OrderByDescending(job => job.OrderNumber)
            .ToList();
    }

    public JobDetailResponse? GetJobDetail(Guid orderId)
    {
        return _jobs.TryGetValue(orderId, out var job) ? MapDetail(job) : null;
    }

    public IReadOnlyList<string> GetStyleTitles(Guid orderId)
    {
        return _jobs.TryGetValue(orderId, out var job) ? job.StyleTitles : Array.Empty<string>();
    }

    public IReadOnlyList<JobOrderResponse> GetOrderList(string? lookup, int commonQuery, string? startsWith, int take, DateOnly? startOn = null, DateOnly? endOn = null)
    {
        var today = DateTime.Today;

        var query = _jobs.Values.AsEnumerable()
            .Where(j => j.Status >= 0);

        query = commonQuery switch
        {
            1 => query.Where(j => j.OrderedOn <= today && j.OrderedOn >= today.AddDays(-7)),
            2 => query.Where(j => j.OrderedOn <= today && j.OrderedOn >= today.AddDays(-30)),
            3 => query.Where(j => j.RequiredOn >= today && j.RequiredOn <= today.AddDays(7)),
            4 => query.Where(j => j.RequiredOn >= today && j.RequiredOn <= today.AddDays(30)),
            _ => query
        };

        if (startOn.HasValue)
        {
            var lower = startOn.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(j => j.OrderedOn >= lower);
        }

        if (endOn.HasValue)
        {
            var upper = endOn.Value.ToDateTime(TimeOnly.MinValue).AddDays(1);
            query = query.Where(j => j.OrderedOn < upper);
        }

        if (!string.IsNullOrEmpty(startsWith) && startsWith != "All")
        {
            if (startsWith == "0-9")
                query = query.Where(j => j.OrderNumber.Length == 0 ||
                    string.Compare(j.OrderNumber[..1], "A", StringComparison.OrdinalIgnoreCase) < 0);
            else
                query = query.Where(j => j.OrderNumber.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(lookup))
        {
            var token = lookup.ToLowerInvariant();
            query = query.Where(j =>
                j.OrderNumber.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                j.CustomerName.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                j.CustomerRef.Contains(token, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .OrderByDescending(j => j.OrderNumber)
            .ThenBy(j => j.JobNumber)
            .Take(take)
            .Select(MapOrder)
            .ToList();
    }

    public IReadOnlyList<JobOrderResponse> GetJobList(string? lookup, int commonQuery, string? startsWith, int take, DateOnly? startOn = null, DateOnly? endOn = null)
    {
        var today = DateTime.Today;

        var query = _jobs.Values.AsEnumerable()
            .Where(job => int.TryParse(job.JobNumber, out var jobNumber) && jobNumber > 0);

        query = commonQuery switch
        {
            1 => query.Where(j => j.Status >= 2 && j.OrderedOn <= today.AddDays(1) && j.OrderedOn >= today.AddDays(-30)),
            2 => query.Where(j => j.Status >= 2 && j.OrderedOn <= today.AddDays(1) && j.OrderedOn >= today.AddDays(-90)),
            _ => query
        };

        if (startOn.HasValue)
        {
            var lower = startOn.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(j => j.OrderedOn >= lower);
        }

        if (endOn.HasValue)
        {
            var upper = endOn.Value.ToDateTime(TimeOnly.MinValue).AddDays(1);
            query = query.Where(j => j.OrderedOn < upper);
        }

        if (!string.IsNullOrEmpty(startsWith) && !string.Equals(startsWith, "All", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(startsWith, "0-9", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(j => j.OrderNumber.Length == 0 || !char.IsLetter(j.OrderNumber[0]));
            }
            else
            {
                query = query.Where(j => j.OrderNumber.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (!string.IsNullOrWhiteSpace(lookup))
        {
            query = query.Where(j =>
                j.OrderNumber.Contains(lookup, StringComparison.OrdinalIgnoreCase) ||
                j.CompositeOrderNumber.Contains(lookup, StringComparison.OrdinalIgnoreCase) ||
                j.CustomerName.Contains(lookup, StringComparison.OrdinalIgnoreCase) ||
                j.CustomerRef.Contains(lookup, StringComparison.OrdinalIgnoreCase) ||
                j.OrderTitle.Contains(lookup, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .OrderByDescending(j => j.OrderNumber)
            .ThenBy(j => j.JobNumber)
            .Take(take)
            .Select(MapOrder)
            .ToList();
    }

    public IReadOnlyList<JobOrderResponse> GetJobOrders(int take)
    {
        return _jobs.Values
            .OrderByDescending(job => job.OrderedOn)
            .Take(take)
            .Select(MapOrder)
            .ToList();
    }

    public IReadOnlyList<JobStatsResponse> GetJobStats(DateOnly? startOn, DateOnly? endOn)
    {
        var query = _jobs.Values.AsEnumerable();

        if (startOn.HasValue)
        {
            var lower = startOn.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(job => job.OrderedOn >= lower);
        }

        if (endOn.HasValue)
        {
            var upperExclusive = endOn.Value.ToDateTime(TimeOnly.MinValue).AddDays(1);
            query = query.Where(job => job.OrderedOn < upperExclusive);
        }

        return query
            .OrderBy(job => job.OrderedOn)
            .Select(job =>
            {
                var invoiceAmount = Math.Round(job.Qty * 1.8m, 2);
                var cost = Math.Round(job.Qty * 1.15m, 2);
                var grossProfit = invoiceAmount <= 0m ? 0m : Math.Round((invoiceAmount - cost) / invoiceAmount, 4);

                return new JobStatsResponse
                {
                    JobNumber = job.CompositeOrderNumber,
                    CustomerName = job.CustomerName,
                    Brand = job.OrderTitle,
                    PurchaseOrder = job.CustomerRef,
                    SalesRep = job.OrderedBy,
                    GrossProfit = grossProfit,
                    Cost = cost,
                    InvoiceAmount = invoiceAmount,
                    InvNumber = job.OrderNumber,
                    InvDate = job.OrderedOn,
                    Year = job.OrderedOn.Year,
                    Month = job.OrderedOn.Month,
                };
            })
            .ToList();
    }

    public JobOrderResponse? GetJobOrder(Guid orderId)
    {
        return _jobs.TryGetValue(orderId, out var job) ? MapOrder(job) : null;
    }

    public Task<JobOrderResponse> CreateJobOrder(CreateJobOrderRequest request, string actor)
    {
        var orderId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var record = new JobRecord
        {
            OrderId = orderId,
            OrderType = request.OrderType,
            OrderNumber = request.OrderNumber,
            JobNumber = request.JobNumber,
            CustomerName = request.CustomerName,
            CustomerRef = request.CustomerRef,
            OrderTitle = request.OrderTitle,
            OrderedBy = actor,
            OrderedOn = request.OrderedOn,
            RequiredOn = request.RequiredOn,
            Qty = request.Qty,
            PaymentTerms = request.PaymentTerms,
            Remarks = request.Remarks,
            ProductDetails = string.Empty,
            ProductStyle = request.ProductStyle,
            ProductCode = request.ProductCode,
            OutputRef = request.OutputRef,
            InvoiceRef = request.InvoiceRef,
            InvoiceAmount = request.InvoiceAmount,
            Status = request.Status,
            SONumber = request.SONumber,
            OriginalSONumber = request.OriginalSONumber,
            WorkflowAttributes = request.WorkflowAttributes,
            CreatedBy = actor,
            CreatedOn = timestamp,
            StyleTitles = [request.OrderTitle],
            Attachments = []
        };

        _jobs[orderId] = record;
        return Task.FromResult(MapOrder(record));
    }

    public Task<JobOrderResponse?> UpdateJobOrder(Guid orderId, UpdateJobOrderRequest request, string actor)
    {
        if (!_jobs.TryGetValue(orderId, out var current))
        {
            return Task.FromResult<JobOrderResponse?>(null);
        }

        var updated = current with
        {
            CustomerName = request.CustomerName,
            CustomerRef = request.CustomerRef,
            OrderTitle = request.OrderTitle,
            RequiredOn = request.RequiredOn,
            Qty = request.Qty,
            PaymentTerms = request.PaymentTerms,
            Remarks = request.Remarks,
            ProductDetails = request.ProductDetails ?? current.ProductDetails,
            ProductStyle = request.ProductStyle ?? current.ProductStyle,
            ProductCode = request.ProductCode ?? current.ProductCode,
            OutputRef = request.OutputRef ?? current.OutputRef,
            InvoiceRef = request.InvoiceRef ?? current.InvoiceRef,
            InvoiceAmount = request.InvoiceAmount ?? current.InvoiceAmount,
            Status = request.Status,
            OrderType = request.OrderType,
            SONumber = request.SONumber,
            OriginalSONumber = request.OriginalSONumber,
            WorkflowAttributes = request.WorkflowAttributes,
            ModifiedBy = actor,
            ModifiedOn = DateTime.UtcNow,
            StyleTitles = [request.OrderTitle, .. current.StyleTitles.Skip(1)]
        };

        _jobs[orderId] = updated;
        return Task.FromResult<JobOrderResponse?>(MapOrder(updated));
    }

    public Task<JobOrderResponse?> DeleteJobOrder(Guid orderId)
    {
        var result = _jobs.TryRemove(orderId, out var removed) ? MapOrder(removed) : null;
        return Task.FromResult(result);
    }

    private static JobListItemResponse MapListItem(JobRecord job)
    {
        return new JobListItemResponse
        {
            OrderId = job.OrderId,
            OrderNumber = job.CompositeOrderNumber,
            CustomerName = job.CustomerName,
            CustomerRef = job.CustomerRef,
            OrderTitle = job.OrderTitle,
            OrderedBy = job.OrderedBy,
            OrderedOn = job.OrderedOn,
            RequiredOn = job.RequiredOn,
            Qty = job.Qty,
            Status = job.Status
        };
    }

    private static JobDetailResponse MapDetail(JobRecord job)
    {
        return new JobDetailResponse
        {
            OrderId = job.OrderId,
            OrderNumber = job.CompositeOrderNumber,
            CustomerName = job.CustomerName,
            CustomerRef = job.CustomerRef,
            OrderTitle = job.OrderTitle,
            OrderedBy = job.OrderedBy,
            OrderedOn = job.OrderedOn,
            RequiredOn = job.RequiredOn,
            Status = job.Status,
            Qty = job.Qty,
            PaymentTerms = job.PaymentTerms,
            Remarks = job.Remarks,
            ProductDetails = job.ProductDetails,
            ProductStyle = job.ProductStyle ?? string.Empty,
            ProductCode = job.ProductCode ?? string.Empty,
            OutputRef = job.OutputRef ?? string.Empty,
            InvoiceRef = job.InvoiceRef ?? string.Empty,
            InvoiceAmount = job.InvoiceAmount ?? 0m,
            StyleTitles = job.StyleTitles,
            Attachments = job.Attachments
                .Select(attachment => new JobAttachmentResponse
                {
                    AttachmentId = Guid.Empty,
                    FileName = attachment.FileName,
                    ContentType = attachment.ContentType,
                    Length = attachment.Length
                })
                .ToList(),
            SONumber = job.SONumber,
            OriginalSONumber = job.OriginalSONumber,
            WorkflowAttributes = job.WorkflowAttributes
        };
    }

    private static JobOrderResponse MapOrder(JobRecord job)
    {
        return new JobOrderResponse
        {
            OrderId = job.OrderId,
            OrderType = job.OrderType,
            OrderNumber = job.OrderNumber,
            JobNumber = job.JobNumber,
            CustomerName = job.CustomerName,
            CustomerRef = job.CustomerRef,
            OrderTitle = job.OrderTitle,
            ProductCode = job.ProductCode ?? string.Empty,
            ProductStyle = job.ProductStyle ?? string.Empty,
            ProductDetails = job.ProductDetails,
            OutputRef = job.OutputRef ?? string.Empty,
            InvoiceRef = job.InvoiceRef ?? string.Empty,
            InvoiceAmount = job.InvoiceAmount ?? 0m,
            AttachmentProductCount = 0,
            AttachmentCustomerCount = 0,
            OrderedBy = job.OrderedBy,
            OrderedOn = job.OrderedOn,
            RequiredOn = job.RequiredOn,
            CompletedOn = null,
            Qty = job.Qty,
            PaymentTerms = job.PaymentTerms,
            Remarks = job.Remarks,
            Status = job.Status,
            CreatedBy = job.CreatedBy,
            CreatedOn = job.CreatedOn,
            ModifiedBy = job.ModifiedBy,
            ModifiedOn = job.ModifiedOn,
            SONumber = job.SONumber,
            OriginalSONumber = job.OriginalSONumber
        };
    }

    private static IReadOnlyList<JobRecord> CreateSeedData()
    {
        return
        [
            new JobRecord
            {
                OrderId = Guid.Parse("1e84b2e5-3f73-4d60-9d0d-08dc50c00001"),
                OrderNumber = "JB260301",
                JobNumber = "01",
                CustomerName = "Northwind Print Co.",
                CustomerRef = "NW-PO-2201",
                OrderTitle = "Retail Packaging Artwork",
                OrderedBy = "admin",
                OrderedOn = new DateTime(2026, 3, 21, 9, 0, 0, DateTimeKind.Utc),
                RequiredOn = new DateTime(2026, 3, 29, 9, 0, 0, DateTimeKind.Utc),
                Qty = 1200m,
                PaymentTerms = "30 days",
                Remarks = "Priority print run for launch window.",
                ProductDetails = "<p>Carton front</p><p>Carton reverse</p><p>Insert leaflet</p>",
                Status = 2,
                CreatedBy = "admin",
                CreatedOn = new DateTime(2026, 3, 21, 9, 0, 0, DateTimeKind.Utc),
                StyleTitles = ["Carton front", "Carton reverse", "Insert leaflet"],
                Attachments =
                [
                    new JobAttachmentRecord("packaging-proof.pdf", "application/pdf", 184320),
                    new JobAttachmentRecord("die-line.ai", "application/postscript", 92160)
                ]
            },
            new JobRecord
            {
                OrderId = Guid.Parse("1e84b2e5-3f73-4d60-9d0d-08dc50c00002"),
                OrderNumber = "JB260302",
                JobNumber = "03",
                CustomerName = "Litware Agency",
                CustomerRef = "LT-BR-884",
                OrderTitle = "Campaign Poster Refresh",
                OrderedBy = "admin",
                OrderedOn = new DateTime(2026, 3, 24, 9, 0, 0, DateTimeKind.Utc),
                RequiredOn = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc),
                Qty = 640m,
                PaymentTerms = "COD",
                Remarks = "Customer requested satin stock.",
                ProductDetails = "<p>A1 poster</p><p>A3 handbill</p>",
                Status = 1,
                CreatedBy = "admin",
                CreatedOn = new DateTime(2026, 3, 24, 9, 0, 0, DateTimeKind.Utc),
                StyleTitles = ["A1 poster", "A3 handbill"],
                Attachments =
                [
                    new JobAttachmentRecord("poster-layout.png", "image/png", 532480)
                ]
            },
            new JobRecord
            {
                OrderId = Guid.Parse("1e84b2e5-3f73-4d60-9d0d-08dc50c00003"),
                OrderNumber = "JB260303",
                JobNumber = "02",
                CustomerName = "Adventure Works",
                CustomerRef = "AW-CAT-998",
                OrderTitle = "Quarterly Product Catalogue",
                OrderedBy = "admin",
                OrderedOn = new DateTime(2026, 3, 27, 9, 0, 0, DateTimeKind.Utc),
                RequiredOn = new DateTime(2026, 4, 10, 9, 0, 0, DateTimeKind.Utc),
                Qty = 2500m,
                PaymentTerms = "45 days",
                Remarks = "Awaiting final proof approval.",
                ProductDetails = "<p>Cover</p><p>Section dividers</p><p>Product spreads</p>",
                Status = 0,
                CreatedBy = "admin",
                CreatedOn = new DateTime(2026, 3, 27, 9, 0, 0, DateTimeKind.Utc),
                StyleTitles = ["Cover", "Section dividers", "Product spreads"],
                Attachments =
                [
                    new JobAttachmentRecord("catalogue-proof.zip", "application/zip", 2097152)
                ]
            }
        ];
    }

    private sealed record JobRecord
    {
        public required Guid OrderId { get; init; }
        public int OrderType { get; init; }
        public required string OrderNumber { get; init; }
        public required string JobNumber { get; init; }
        public required string CustomerName { get; init; }
        public required string CustomerRef { get; init; }
        public required string OrderTitle { get; init; }
        public required string OrderedBy { get; init; }
        public required DateTime OrderedOn { get; init; }
        public required DateTime RequiredOn { get; init; }
        public required decimal Qty { get; init; }
        public required string PaymentTerms { get; init; }
        public required string Remarks { get; init; }
        public required string ProductDetails { get; init; }
        public required int Status { get; init; }
        public required string CreatedBy { get; init; }
        public required DateTime CreatedOn { get; init; }
        public string? ModifiedBy { get; init; }
        public DateTime? ModifiedOn { get; init; }
        public string? SONumber { get; init; }
        public string? OriginalSONumber { get; init; }
        public string? ProductStyle { get; init; }
        public string? ProductCode { get; init; }
        public string? OutputRef { get; init; }
        public string? InvoiceRef { get; init; }
        public decimal? InvoiceAmount { get; init; }
        public Dictionary<string, string>? WorkflowAttributes { get; init; }
        public required string[] StyleTitles { get; init; }
        public required IReadOnlyList<JobAttachmentRecord> Attachments { get; init; }
        public string CompositeOrderNumber => $"{OrderNumber}-{JobNumber}";
    }

    private sealed record JobAttachmentRecord(string FileName, string ContentType, long Length);
}
