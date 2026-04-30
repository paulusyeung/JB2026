using JB2026.Api.Models;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class EfJobManagementRepository : IJobManagementRepository
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    private static readonly Func<JB5LegacyReadContext, int, IEnumerable<JobOrder>> CompiledGetJobOrders =
        EF.CompileQuery((JB5LegacyReadContext db, int take) =>
            db.JobOrders
                .AsNoTracking()
                .Include(order => order.JobSchedules)
                .Include(order => order.JobWorkflows)
                .Include(order => order.JobAttachments)
                .OrderByDescending(order => order.OrderedOn)
                .Take(take));

    private static readonly Func<JB5LegacyReadContext, Guid, JobOrder?> CompiledGetJobOrderById =
        EF.CompileQuery((JB5LegacyReadContext db, Guid orderId) =>
            db.JobOrders
                .AsNoTracking()
                .Include(order => order.JobSchedules)
                .Include(order => order.JobWorkflows)
                .Include(order => order.JobAttachments)
                .FirstOrDefault(order => order.OrderId == orderId));

    private static readonly Func<JB5LegacyReadContext, DateTime, DateTime, IEnumerable<JobOrder>> CompiledGetRange =
        EF.CompileQuery((JB5LegacyReadContext db, DateTime lowerBoundExclusive, DateTime upperBoundExclusive) =>
            db.JobOrders
                .AsNoTracking()
                .Where(order => order.OrderedOn.HasValue
                    && order.OrderedOn.Value < upperBoundExclusive
                    && order.OrderedOn.Value > lowerBoundExclusive)
                .OrderByDescending(order => order.OrderNumber)
                .Select(order => order));

    private static readonly Func<JB5LegacyWriteContext, Guid, JobOrder?> CompiledGetWriteJobOrderById =
        EF.CompileQuery((JB5LegacyWriteContext db, Guid orderId) =>
            db.JobOrders.FirstOrDefault(order => order.OrderId == orderId));

    public EfJobManagementRepository(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public IReadOnlyList<JobListItemResponse> GetRange(DateOnly startOn, int days)
    {
        var start = startOn.ToDateTime(TimeOnly.MinValue);
        var items = CompiledGetRange(_readContext, start.AddDays(-days), start.AddDays(1))
            .Select(MapListItem)
            .ToList();

        return items;
    }

    public JobDetailResponse? GetJobDetail(Guid orderId)
    {
        var job = CompiledGetJobOrderById(_readContext, orderId);
        return job is null ? null : MapDetail(job);
    }

    public IReadOnlyList<string> GetStyleTitles(Guid orderId)
    {
        var job = CompiledGetJobOrderById(_readContext, orderId);
        if (job is null)
        {
            return Array.Empty<string>();
        }

        return job.JobWorkflows
            .OrderBy(workflow => workflow.WorkIndex)
            .Select(workflow => workflow.WorkTitle)
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Select(title => title!)
            .ToList();
    }

    public IReadOnlyList<JobOrderResponse> GetOrderList(string? lookup, int commonQuery, string? startsWith, int take, DateOnly? startOn = null, DateOnly? endOn = null)
    {
        var today = DateTime.Today;

        var query = _readContext.vwOrderDetailLists
            .AsNoTracking()
            .Where(order => !order.Retired && order.Status >= 0);

        query = commonQuery switch
        {
            1 => query.Where(o => o.OrderedOn <= today && o.OrderedOn >= today.AddDays(-7)),
            2 => query.Where(o => o.OrderedOn <= today && o.OrderedOn >= today.AddDays(-30)),
            3 => query.Where(o => o.RequiredOn >= today && o.RequiredOn <= today.AddDays(7)),
            4 => query.Where(o => o.RequiredOn >= today && o.RequiredOn <= today.AddDays(30)),
            _ => query
        };

        if (startOn.HasValue)
        {
            var lower = startOn.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(o => o.OrderedOn.HasValue && o.OrderedOn.Value >= lower);
        }

        if (endOn.HasValue)
        {
            var upper = endOn.Value.ToDateTime(TimeOnly.MinValue).AddDays(1);
            query = query.Where(o => o.OrderedOn.HasValue && o.OrderedOn.Value < upper);
        }

        if (!string.IsNullOrEmpty(startsWith) && startsWith != "All")
        {
            if (startsWith == "0-9")
                query = query.Where(o => o.OrderNumber != null && !EF.Functions.Like(o.OrderNumber, "[A-Za-z]%"));
            else
                query = query.Where(o => o.OrderNumber != null && o.OrderNumber.StartsWith(startsWith));
        }

        if (!string.IsNullOrWhiteSpace(lookup))
        {
            query = query.Where(o =>
                (o.OrderNumber != null && o.OrderNumber.Contains(lookup)) ||
                (o.CustomerName != null && o.CustomerName.Contains(lookup)) ||
                (o.CustomerRef != null && o.CustomerRef.Contains(lookup)) ||
                (o.OrderTitle != null && o.OrderTitle.Contains(lookup)));
        }

        return query
            .OrderByDescending(o => o.OrderNumber)
            .ThenBy(o => o.JobNumber)
            .Take(take)
            .Select(MapOrder)
            .ToList();
    }

    public IReadOnlyList<JobOrderResponse> GetJobList(string? lookup, int commonQuery, string? startsWith, int take, DateOnly? startOn = null, DateOnly? endOn = null)
    {
        var userDisplayNameLookup = BuildUserDisplayNameLookup();
        var today = DateTime.Today;

        var query = _readContext.JobOrders
            .AsNoTracking()
            .Include(order => order.JobSchedules)
            .Include(order => order.JobWorkflows)
            .Include(order => order.JobAttachments)
            .Where(order => !order.Retired && order.JobNumber.HasValue && order.JobNumber.Value > 0);

        query = commonQuery switch
        {
            1 => query.Where(o => o.Status >= 2 && o.OrderedOn >= today.AddDays(-30) && o.OrderedOn < today.AddDays(1)),
            2 => query.Where(o => o.Status >= 2 && o.OrderedOn >= today.AddDays(-90) && o.OrderedOn < today.AddDays(1)),
            _ => query
        };

        if (startOn.HasValue)
        {
            var lower = startOn.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(o => o.OrderedOn.HasValue && o.OrderedOn.Value >= lower);
        }

        if (endOn.HasValue)
        {
            var upper = endOn.Value.ToDateTime(TimeOnly.MinValue).AddDays(1);
            query = query.Where(o => o.OrderedOn.HasValue && o.OrderedOn.Value < upper);
        }

        if (!string.IsNullOrWhiteSpace(startsWith) && !string.Equals(startsWith, "All", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(startsWith, "0-9", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(o => o.OrderNumber != null && !EF.Functions.Like(o.OrderNumber, "[A-Za-z]%"));
            }
            else
            {
                query = query.Where(o => o.OrderNumber != null && o.OrderNumber.StartsWith(startsWith));
            }
        }

        if (!string.IsNullOrWhiteSpace(lookup))
        {
            query = query.Where(o =>
                (o.OrderNumber != null && o.OrderNumber.Contains(lookup)) ||
                (o.CustomerName != null && o.CustomerName.Contains(lookup)) ||
                (o.CustomerRef != null && o.CustomerRef.Contains(lookup)) ||
                (o.OrderTitle != null && o.OrderTitle.Contains(lookup)));
        }

        return query
            .OrderByDescending(o => o.OrderNumber)
            .ThenBy(o => o.JobNumber)
            .Take(take)
            .Select(o => MapOrder(o, userDisplayNameLookup))
            .ToList();
    }

    public IReadOnlyList<JobOrderResponse> GetJobOrders(int take)
    {
        var userDisplayNameLookup = BuildUserDisplayNameLookup();
        return _readContext.JobOrders
            .AsNoTracking()
            .Where(order => !order.Retired && (order.JobNumber == null || order.JobNumber == 0))
            .OrderByDescending(order => order.OrderedOn)
            .Take(take)
            .Select(order => MapOrder(order, userDisplayNameLookup))
            .ToList();
    }

    public IReadOnlyList<JobStatsResponse> GetJobStats(DateOnly? startOn, DateOnly? endOn)
    {
        var query = _readContext.vwJobStatGrossProfits.AsNoTracking().AsQueryable();

        if (startOn.HasValue)
        {
            var lower = startOn.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(item => item.InvDate.HasValue && item.InvDate.Value >= lower);
        }

        if (endOn.HasValue)
        {
            var upperExclusive = endOn.Value.ToDateTime(TimeOnly.MinValue).AddDays(1);
            query = query.Where(item => item.InvDate.HasValue && item.InvDate.Value < upperExclusive);
        }

        return query
            .OrderBy(item => item.InvDate)
            .ThenBy(item => item.InvNumber)
            .Select(item => new JobStatsResponse
            {
                JobNumber = item.JobNumber ?? string.Empty,
                CustomerName = item.CustomerName ?? string.Empty,
                Brand = item.OrderTitle ?? string.Empty,
                PurchaseOrder = item.PurchaseOrder ?? string.Empty,
                SalesRep = item.SalesRep ?? string.Empty,
                GrossProfit = item.GrossProfit ?? 0m,
                Cost = item.Cost ?? 0m,
                InvoiceAmount = item.InvoiceAmount ?? 0m,
                InvNumber = item.InvNumber ?? string.Empty,
                InvDate = item.InvDate,
                Year = item.InvDate.HasValue ? item.InvDate.Value.Year : null,
                Month = item.InvDate.HasValue ? item.InvDate.Value.Month : null,
            })
            .ToList();
    }

    public JobOrderResponse? GetJobOrder(Guid orderId)
    {
        var userDisplayNameLookup = BuildUserDisplayNameLookup();
        var job = CompiledGetJobOrderById(_readContext, orderId);
        return job is null ? null : MapOrder(job, userDisplayNameLookup);
    }

    public async Task<JobOrderResponse> CreateJobOrder(CreateJobOrderRequest request, string actor)
    {
        var actorId = ParseActorGuidOrFallback(actor);
        var now = DateTime.UtcNow;

        var order = new JobOrder
        {
            OrderId = Guid.NewGuid(),
            OrderType = 0,
            OrderNumber = request.OrderNumber,
            JobNumber = int.TryParse(request.JobNumber, out var jobNumber) ? jobNumber : null,
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
            CreatedBy = actorId,
            CreatedOn = now,
            ModifiedBy = actorId,
            ModifiedOn = now,
            Retired = false
        };

        _writeContext.JobOrders.Add(order);
        await _writeContext.SaveChangesAsync();

        var userDisplayNameLookup = BuildUserDisplayNameLookup();
        return MapOrder(order, userDisplayNameLookup);
    }

    public async Task<JobOrderResponse?> UpdateJobOrder(Guid orderId, UpdateJobOrderRequest request, string actor)
    {
        var order = CompiledGetWriteJobOrderById(_writeContext, orderId);
        if (order is null)
        {
            return null;
        }

        order.CustomerName = request.CustomerName;
        order.CustomerRef = request.CustomerRef;
        order.OrderTitle = request.OrderTitle;
        order.RequiredOn = request.RequiredOn;
        order.Qty = request.Qty;
        order.PaymentTerms = request.PaymentTerms;
        order.Remarks = request.Remarks;
        if (request.ProductDetails is not null)
        {
            order.ProductDetails = request.ProductDetails;
        }
        order.Status = request.Status;
        order.ModifiedBy = ParseActorGuidOrFallback(actor);
        order.ModifiedOn = DateTime.UtcNow;

        await _writeContext.SaveChangesAsync();

        var userDisplayNameLookup = BuildUserDisplayNameLookup();
        return MapOrder(order, userDisplayNameLookup);
    }

    public async Task<JobOrderResponse?> DeleteJobOrder(Guid orderId)
    {
        var order = CompiledGetWriteJobOrderById(_writeContext, orderId);
        if (order is null)
        {
            return null;
        }

        _writeContext.JobOrders.Remove(order);
        await _writeContext.SaveChangesAsync();

        var userDisplayNameLookup = BuildUserDisplayNameLookup();
        return MapOrder(order, userDisplayNameLookup);
    }

    private static JobListItemResponse MapListItem(JobOrder job)
    {
        return new JobListItemResponse
        {
            OrderId = job.OrderId,
            OrderNumber = BuildCompositeOrderNumber(job.OrderNumber, job.JobNumber),
            CustomerName = job.CustomerName ?? string.Empty,
            CustomerRef = job.CustomerRef ?? string.Empty,
            OrderTitle = job.OrderTitle ?? string.Empty,
            OrderedBy = job.OrderedBy ?? string.Empty,
            OrderedOn = job.OrderedOn ?? DateTime.MinValue,
            RequiredOn = job.RequiredOn ?? DateTime.MinValue,
            Qty = job.Qty ?? 0m,
            Status = job.Status
        };
    }

    private static JobDetailResponse MapDetail(JobOrder job)
    {
        return new JobDetailResponse
        {
            OrderId = job.OrderId,
            OrderNumber = BuildCompositeOrderNumber(job.OrderNumber, job.JobNumber),
            CustomerName = job.CustomerName ?? string.Empty,
            CustomerRef = job.CustomerRef ?? string.Empty,
            OrderTitle = job.OrderTitle ?? string.Empty,
            OrderedBy = job.OrderedBy ?? string.Empty,
            OrderedOn = job.OrderedOn ?? DateTime.MinValue,
            RequiredOn = job.RequiredOn ?? DateTime.MinValue,
            Status = job.Status,
            Qty = job.Qty ?? 0m,
            PaymentTerms = job.PaymentTerms ?? string.Empty,
            Remarks = job.Remarks ?? string.Empty,
            ProductDetails = job.ProductDetails ?? string.Empty,
            StyleTitles = job.JobWorkflows
                .OrderBy(workflow => workflow.WorkIndex)
                .Select(workflow => workflow.WorkTitle)
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Select(title => title!)
                .ToArray(),
            Attachments = job.JobAttachments
                .OrderBy(attachment => attachment.AttachmentIndex)
                .Select(attachment => new JobAttachmentResponse
                {
                    AttachmentId = attachment.AttachmentId,
                    FileName = attachment.OriginalFileName ?? string.Empty,
                    ContentType = "application/octet-stream",
                    Length = 0
                })
                .ToList()
        };
    }

    private static JobOrderResponse MapOrder(JobOrder job, IReadOnlyDictionary<Guid, string>? userDisplayNameLookup = null)
    {
        var createdBy = job.CreatedBy.ToString();
        if (userDisplayNameLookup is not null && userDisplayNameLookup.TryGetValue(job.CreatedBy, out var createdByDisplayName))
        {
            createdBy = createdByDisplayName;
        }

        var modifiedBy = job.ModifiedBy.ToString();
        if (userDisplayNameLookup is not null && userDisplayNameLookup.TryGetValue(job.ModifiedBy, out var modifiedByDisplayName))
        {
            modifiedBy = modifiedByDisplayName;
        }

        return new JobOrderResponse
        {
            OrderId = job.OrderId,
            OrderType = job.OrderType,
            OrderNumber = job.OrderNumber ?? string.Empty,
            JobNumber = job.JobNumber?.ToString() ?? string.Empty,
            CustomerName = job.CustomerName ?? string.Empty,
            CustomerRef = job.CustomerRef ?? string.Empty,
            OrderTitle = job.OrderTitle ?? string.Empty,
            ProductCode = job.ProductCode ?? string.Empty,
            ProductStyle = job.ProductStyle ?? string.Empty,
            ProductDetails = job.ProductDetails ?? string.Empty,
            OutputRef = job.OutputRef ?? string.Empty,
            InvoiceRef = job.InvoiceRef ?? string.Empty,
            InvoiceAmount = job.InvoiceAmount ?? 0m,
            AttachmentProductCount = job.JobAttachments.Count(attachment => attachment.AttachmentIndex == 0),
            AttachmentCustomerCount = job.JobAttachments.Count(attachment => attachment.AttachmentIndex == 1),
            OrderedBy = job.OrderedBy ?? string.Empty,
            OrderedOn = job.OrderedOn ?? DateTime.MinValue,
            RequiredOn = job.RequiredOn ?? DateTime.MinValue,
            CompletedOn = job.CompletedOn,
            Qty = job.Qty ?? 0m,
            PaymentTerms = job.PaymentTerms ?? string.Empty,
            Remarks = job.Remarks ?? string.Empty,
            Status = job.Status,
            CreatedBy = createdBy,
            CreatedOn = job.CreatedOn,
            ModifiedBy = modifiedBy,
            ModifiedOn = job.ModifiedOn
        };
    }

    private static JobOrderResponse MapOrder(vwOrderDetailList order)
    {
        return new JobOrderResponse
        {
            OrderId = order.OrderId,
            OrderType = order.OrderType,
            OrderNumber = order.OrderNumber ?? string.Empty,
            JobNumber = order.JobNumber?.ToString() ?? string.Empty,
            CustomerName = order.CustomerName ?? string.Empty,
            CustomerRef = order.CustomerRef ?? string.Empty,
            OrderTitle = order.OrderTitle ?? string.Empty,
            ProductCode = order.ProductCode ?? string.Empty,
            ProductStyle = order.ProductStyle ?? string.Empty,
            ProductDetails = order.ProductDetails ?? string.Empty,
            OutputRef = order.OutputRef ?? string.Empty,
            InvoiceRef = order.InvoiceRef ?? string.Empty,
            InvoiceAmount = order.InvoiceAmount,
            AttachmentProductCount = order.Attachment_ProductCode ?? 0,
            AttachmentCustomerCount = order.Attachment_CustomerRef ?? 0,
            OrderedBy = order.OrderedBy ?? string.Empty,
            OrderedOn = order.OrderedOn ?? DateTime.MinValue,
            RequiredOn = order.RequiredOn ?? DateTime.MinValue,
            CompletedOn = order.CompletedOn == default ? null : order.CompletedOn,
            Qty = order.Qty,
            PaymentTerms = order.PaymentTerms ?? string.Empty,
            Remarks = order.Remarks ?? string.Empty,
            Status = order.Status,
            CreatedBy = order.CreatedBy ?? string.Empty,
            CreatedOn = order.CreatedOn,
            ModifiedBy = order.ModifiedBy,
            ModifiedOn = order.ModifiedOn
        };
    }

    private Dictionary<Guid, string> BuildUserDisplayNameLookup()
    {
        return _readContext.vwUserList_Actives
            .AsNoTracking()
            .Select(user => new
            {
                user.UserId,
                user.UserAlias,
                user.UserName,
            })
            .ToList()
            .GroupBy(user => user.UserId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var user = group.First();
                    var displayName = string.IsNullOrWhiteSpace(user.UserAlias)
                        ? user.UserName ?? string.Empty
                        : user.UserAlias;

                    return string.IsNullOrWhiteSpace(displayName)
                        ? group.Key.ToString()
                        : displayName.Trim();
                });
    }

    private static string BuildCompositeOrderNumber(string? orderNumber, int? jobNumber)
    {
        var lhs = orderNumber ?? string.Empty;
        return jobNumber.HasValue ? $"{lhs}-{jobNumber.Value:00}" : lhs;
    }

    private static Guid ParseActorGuidOrFallback(string actor)
    {
        return Guid.TryParse(actor, out var actorId) ? actorId : Guid.NewGuid();
    }
}
