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

    public IReadOnlyList<JobOrderResponse> GetJobOrders(int take)
    {
        return _jobs.Values
            .OrderByDescending(job => job.OrderedOn)
            .Take(take)
            .Select(MapOrder)
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
            Status = request.Status,
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
            Status = request.Status,
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
            StyleTitles = job.StyleTitles,
            Attachments = job.Attachments
                .Select(attachment => new JobAttachmentResponse
                {
                    FileName = attachment.FileName,
                    ContentType = attachment.ContentType,
                    Length = attachment.Length
                })
                .ToList()
        };
    }

    private static JobOrderResponse MapOrder(JobRecord job)
    {
        return new JobOrderResponse
        {
            OrderId = job.OrderId,
            OrderNumber = job.OrderNumber,
            JobNumber = job.JobNumber,
            CustomerName = job.CustomerName,
            CustomerRef = job.CustomerRef,
            OrderTitle = job.OrderTitle,
            OrderedBy = job.OrderedBy,
            OrderedOn = job.OrderedOn,
            RequiredOn = job.RequiredOn,
            Qty = job.Qty,
            PaymentTerms = job.PaymentTerms,
            Remarks = job.Remarks,
            Status = job.Status,
            CreatedBy = job.CreatedBy,
            CreatedOn = job.CreatedOn,
            ModifiedBy = job.ModifiedBy,
            ModifiedOn = job.ModifiedOn
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
        public required int Status { get; init; }
        public required string CreatedBy { get; init; }
        public required DateTime CreatedOn { get; init; }
        public string? ModifiedBy { get; init; }
        public DateTime? ModifiedOn { get; init; }
        public required string[] StyleTitles { get; init; }
        public required IReadOnlyList<JobAttachmentRecord> Attachments { get; init; }
        public string CompositeOrderNumber => $"{OrderNumber}-{JobNumber}";
    }

    private sealed record JobAttachmentRecord(string FileName, string ContentType, long Length);
}
