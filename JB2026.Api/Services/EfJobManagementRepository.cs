using System.Globalization;
using System.Linq.Expressions;
using JB2026.Api.Models;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using JB2026.EfCore.Notifications;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class EfJobManagementRepository : IJobManagementRepository
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;
    private readonly ILogger<EfJobManagementRepository> _logger;
    private readonly JobLifecycleEventPublisher? _jobLifecycleEventPublisher;

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
                    .ThenInclude(w => w.Workflow)
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

    public EfJobManagementRepository(
        JB5LegacyReadContext readContext,
        JB5LegacyWriteContext writeContext,
        ILogger<EfJobManagementRepository> logger,
        JobLifecycleEventPublisher? jobLifecycleEventPublisher = null)
    {
        _readContext = readContext;
        _writeContext = writeContext;
        _logger = logger;
        _jobLifecycleEventPublisher = jobLifecycleEventPublisher;
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

    public IReadOnlyList<JobOrderResponse> GetOrderList(string? lookup, int commonQuery, string? startsWith, int take, DateOnly? startOn = null, DateOnly? endOn = null, string? lookupField = null)
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
            query = ApplyLookup(query, lookup, lookupField);
        }

        return query
            .OrderByDescending(o => o.OrderNumber)
            .ThenBy(o => o.JobNumber)
            .Take(take)
            .Select(MapOrder)
            .ToList();
    }

    public IReadOnlyList<JobOrderResponse> GetJobList(string? lookup, int commonQuery, string? startsWith, int take, DateOnly? startOn = null, DateOnly? endOn = null, int? status = null, string? lookupField = null)
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
            1 => query.Where(o => o.OrderedOn >= today.AddDays(-30) && o.OrderedOn < today.AddDays(1)),
            2 => query.Where(o => o.OrderedOn >= today.AddDays(-90) && o.OrderedOn < today.AddDays(1)),
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

        if (status.HasValue)
        {
            if (status.Value >= 2)
            {
                query = query.Where(o => o.Status == status.Value
                    || (o.CompletedOn.HasValue && o.CompletedOn.Value != new DateTime(1900, 1, 1)));
            }
            else
            {
                query = query.Where(o => o.Status == status.Value);
            }
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
            query = ApplyLookup(query, lookup, lookupField);
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

    private static IQueryable<T> ApplyLookup<T>(IQueryable<T> query, string lookup, string? lookupField)
        where T : class
    {
        var token = lookup;

        var predicates = new Dictionary<string, Expression<Func<T, bool>>>
        {
            ["ordernumber"] = o => (EF.Property<string>(o, "OrderNumber") != null && EF.Property<string>(o, "OrderNumber")!.Contains(token)),
            ["customername"] = o => (EF.Property<string>(o, "CustomerName") != null && EF.Property<string>(o, "CustomerName")!.Contains(token)),
            ["customerref"] = o => (EF.Property<string>(o, "CustomerRef") != null && EF.Property<string>(o, "CustomerRef")!.Contains(token)),
            ["ordertitle"] = o => (EF.Property<string>(o, "OrderTitle") != null && EF.Property<string>(o, "OrderTitle")!.Contains(token)),
            ["orderedby"] = o => (EF.Property<string>(o, "OrderedBy") != null && EF.Property<string>(o, "OrderedBy")!.Contains(token)),
        };

        if (!string.IsNullOrWhiteSpace(lookupField) && predicates.TryGetValue(lookupField.Trim().ToLowerInvariant(), out var single))
        {
            return query.Where(single);
        }

        var parameter = Expression.Parameter(typeof(T), "o");
        var body = predicates.Values
            .Skip(1)
            .Aggregate(
                (Expression)Expression.Invoke(predicates.Values.First(), parameter),
                (acc, predicate) => Expression.OrElse(acc, Expression.Invoke(predicate, parameter)));

        return query.Where(Expression.Lambda<Func<T, bool>>(body, parameter));
    }

    public IReadOnlyList<JobStatsResponse> GetJobStats(DateOnly? startOn, DateOnly? endOn)
    {
        var query = _readContext.JobOrders
            .AsNoTracking()
            .Where(order => order.InvoiceAmount.HasValue && order.InvoiceAmount.Value != 0)
            .Select(order => new
            {
                OrderNumber = order.OrderNumber,
                JobNumber = order.JobNumber,
                CustomerName = order.CustomerName,
                OrderTitle = order.OrderTitle,
                CustomerRef = order.CustomerRef,
                OrderedBy = order.OrderedBy,
                InvoiceRef = order.InvoiceRef,
                OriginalSONumber = order.OriginalSONumber,
                InvoiceAmount = order.InvoiceAmount,
                InvDate = order.CompletedOn.HasValue
                    ? (order.CompletedOn.Value.Year == 1900 ? order.RequiredOn : order.CompletedOn)
                    : (DateTime?)null,
            });

        if (startOn.HasValue)
        {
            var lower = startOn.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(row => row.InvDate.HasValue && row.InvDate.Value >= lower);
        }

        if (endOn.HasValue)
        {
            var upperExclusive = endOn.Value.ToDateTime(TimeOnly.MinValue).AddDays(1);
            query = query.Where(row => row.InvDate.HasValue && row.InvDate.Value < upperExclusive);
        }

        return query
            .OrderBy(row => row.InvDate)
            .ThenBy(row => row.InvoiceRef)
            .ToList()
            .Select(row =>
            {
                var invoiceAmount = row.InvoiceAmount ?? 0m;
                var cost = ParseCostFromOriginalSoNumber(row.OriginalSONumber);
                var grossProfit = invoiceAmount <= 0m
                    ? 0m
                    : Math.Round((invoiceAmount - cost) / invoiceAmount * 100m, 2, MidpointRounding.AwayFromZero);

                return new JobStatsResponse
                {
                    JobNumber = BuildCompositeOrderNumber(row.OrderNumber, row.JobNumber),
                    CustomerName = row.CustomerName ?? string.Empty,
                    Brand = row.OrderTitle ?? string.Empty,
                    PurchaseOrder = row.CustomerRef ?? string.Empty,
                    SalesRep = row.OrderedBy ?? string.Empty,
                    GrossProfit = grossProfit,
                    Cost = cost,
                    InvoiceAmount = invoiceAmount,
                    InvNumber = row.InvoiceRef ?? string.Empty,
                    InvDate = row.InvDate.HasValue ? DateOnly.FromDateTime(row.InvDate.Value) : null,
                    Year = row.InvDate.HasValue ? row.InvDate.Value.Year : null,
                    Month = row.InvDate.HasValue ? row.InvDate.Value.Month : null,
                };
            })
            .ToList();
    }

    private static decimal ParseCostFromOriginalSoNumber(string? originalSONumber)
    {
        if (string.IsNullOrEmpty(originalSONumber))
        {
            return 0m;
        }

        if (!originalSONumber.All(character => char.IsAsciiDigit(character) || character == '.'))
        {
            return 0m;
        }

        return decimal.TryParse(originalSONumber, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var cost)
            ? cost
            : 0m;
    }

    public JobOrderResponse? GetJobOrder(Guid orderId)
    {
        var userDisplayNameLookup = BuildUserDisplayNameLookup();
        var job = CompiledGetJobOrderById(_readContext, orderId);
        return job is null ? null : MapOrder(job, userDisplayNameLookup);
    }

    public async Task<JobOrderResponse> CreateJobOrder(CreateJobOrderRequest request, string actor)
    {
        var actorId = await ResolveUserGuidAsync(actor) ?? Guid.NewGuid();
        var now = DateTime.UtcNow;
        var requestJobNumber = int.TryParse(request.JobNumber, out var parsedJobNumber) ? parsedJobNumber : (int?)null;

        var existingOrder = await _writeContext.JobOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(order =>
                order.OrderNumber == request.OrderNumber &&
                order.JobNumber == requestJobNumber &&
                !order.Retired);

        if (existingOrder is not null)
        {
            _logger.LogWarning(
                "Duplicate create request ignored for OrderNumber={OrderNumber}, JobNumber={JobNumber}, OrderId={OrderId}",
                request.OrderNumber,
                request.JobNumber,
                existingOrder.OrderId);

            var existingUserDisplayNameLookup = BuildUserDisplayNameLookup();
            return MapOrder(existingOrder, existingUserDisplayNameLookup);
        }

        var order = new JobOrder
        {
            OrderId = Guid.NewGuid(),
            OrderType = request.OrderType,
            OrderNumber = request.OrderNumber,
            JobNumber = requestJobNumber,
            CustomerName = request.CustomerName,
            CustomerRef = request.CustomerRef,
            OrderTitle = request.OrderTitle,
            OrderedBy = request.OrderedBy,
            OrderedOn = request.OrderedOn,
            RequiredOn = request.RequiredOn,
            Qty = request.Qty,
            PaymentTerms = request.PaymentTerms,
            Remarks = request.Remarks,
            Status = request.Status,
            SONumber = request.SONumber,
            OriginalSONumber = request.OriginalSONumber,
            ProductStyle = request.ProductStyle,
            ProductCode = request.ProductCode,
            OutputRef = request.OutputRef,
            InvoiceRef = request.InvoiceRef,
            InvoiceAmount = request.InvoiceAmount,
            CreatedBy = actorId,
            CreatedOn = now,
            ModifiedBy = actorId,
            ModifiedOn = now,
            Retired = false,
            RetiredOn = new DateTime(1900, 1, 1),
            RetiredBy = Guid.Empty
        };

        _writeContext.JobOrders.Add(order);
        await _writeContext.SaveChangesAsync();

        if (_jobLifecycleEventPublisher is not null)
        {
            await _jobLifecycleEventPublisher.PublishOrderEventAsync(JobLifecycleEventType.OrderCreated, order.OrderId, CancellationToken.None);

            if (!string.IsNullOrWhiteSpace(request.OriginalSONumber))
            {
                await _jobLifecycleEventPublisher.PublishOrderEventAsync(JobLifecycleEventType.CogsFilled, order.OrderId, CancellationToken.None);
            }
        }

        var steps = await _readContext.Z_OrderTypeWorkflows
            .AsNoTracking()
            .Where(mapping => mapping.OrderType == request.OrderType && mapping.WorkflowId.HasValue)
            .Include(mapping => mapping.Workflow)
            .Where(mapping => mapping.Workflow != null)
            .OrderBy(mapping => mapping.WorkIndex)
            .ToListAsync();

        if (steps.Count > 0)
        {
            var workflowAttributes = request.WorkflowAttributes ?? new Dictionary<string, string>();

            foreach (var step in steps)
            {
                var name = step.Workflow!.WorkflowName ?? string.Empty;
                workflowAttributes.TryGetValue(name, out var value);

                _writeContext.JobWorkflows.Add(new JobWorkflow
                {
                    JobWorkflowId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    WorkflowId = step.WorkflowId,
                    WorkIndex = step.WorkIndex,
                    WorkTitle = value,
                    WorkStatus = 0,
                    WorkInstruction = null,
                    WorkNotes = null,
                    ModifiedOn = now,
                    ModifiedBy = actorId,
                });
            }

            await _writeContext.SaveChangesAsync();
        }

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

        _logger.LogWarning(
            "DBG UpdateJobOrder: orderId={OrderId}, JobNumber={JobNumber}, SONumber={SONumber}, Qty={Qty}, ProductDetails={ProductDetails}, ProductStyle={ProductStyle}, Status={Status}",
            orderId, request.JobNumber, request.SONumber, request.Qty, request.ProductDetails, request.ProductStyle, request.Status);

        try
        {
            const string logSql = """
INSERT INTO Log4Net ([Date], [Thread], [Level], [Logger], [Message], [Exception])
VALUES ({0}, {1}, {2}, {3}, {4}, {5})
""";
            var logMessage =
                $"UpdateJobOrder: orderId={orderId}, JobNumber={request.JobNumber}, SONumber={request.SONumber}, Qty={request.Qty}, ProductDetails={request.ProductDetails}, ProductStyle={request.ProductStyle}, Status={request.Status}";
            await _writeContext.Database.ExecuteSqlRawAsync(
                logSql,
                DateTime.UtcNow,
                Environment.CurrentManagedThreadId.ToString(),
                "INFO",
                "EfJobManagementRepository",
                logMessage,
                string.Empty);
        }
        catch
        {
            // Logging failure should not block the update
        }

        var wasCompleted = order.Status == 2;
        var hadInvoiceRef = !string.IsNullOrWhiteSpace(order.InvoiceRef);
        var hadOriginalSO = order.OriginalSONumber;

        order.OrderNumber = request.OrderNumber;
        order.CustomerName = request.CustomerName;
        order.CustomerRef = request.CustomerRef;
        order.OrderTitle = request.OrderTitle;
        order.RequiredOn = request.RequiredOn;
        if (request.OrderedOn is not null)
        {
            order.OrderedOn = request.OrderedOn;
        }
        order.CompletedOn = request.CompletedOn;
        order.Qty = request.Qty;
        if (request.PaymentTerms is not null)
        {
            order.PaymentTerms = request.PaymentTerms;
        }
        order.Remarks = request.Remarks;
        if (request.ProductDetails is not null)
        {
            order.ProductDetails = request.ProductDetails;
        }
        order.Status = request.Status;
        if (request.CompletedOn.HasValue && request.CompletedOn.Value != new DateTime(1900, 1, 1))
        {
            order.Status = 2;
        }
        order.OrderType = request.OrderType;
        if (request.SONumber is not null)
        {
            order.SONumber = request.SONumber;
        }
        if (request.OriginalSONumber is not null)
        {
            order.OriginalSONumber = request.OriginalSONumber;
        }
        if (request.ProductStyle is not null)
        {
            order.ProductStyle = request.ProductStyle;
        }
        if (request.ProductCode is not null)
        {
            order.ProductCode = request.ProductCode;
        }
        if (request.OutputRef is not null)
        {
            order.OutputRef = request.OutputRef;
        }
        if (request.InvoiceRef is not null)
        {
            order.InvoiceRef = request.InvoiceRef;
        }
        if (request.InvoiceAmount is not null)
        {
            order.InvoiceAmount = request.InvoiceAmount;
        }
        if (!string.IsNullOrEmpty(request.JobNumber))
        {
            order.JobNumber = int.TryParse(request.JobNumber, out var jobNumber) ? jobNumber : (int?)null;
        }
        var actorId = await ResolveUserGuidAsync(actor) ?? Guid.NewGuid();
        order.ModifiedBy = actorId;
        order.ModifiedOn = DateTime.UtcNow;

        await _writeContext.SaveChangesAsync();

        if (_jobLifecycleEventPublisher is not null)
        {
            var completedNow = order.Status == 2 && !wasCompleted;
            var invoicedNow = !hadInvoiceRef && !string.IsNullOrWhiteSpace(order.InvoiceRef);

            if (completedNow)
            {
                await _jobLifecycleEventPublisher.PublishOrderEventAsync(JobLifecycleEventType.Completed, orderId, CancellationToken.None);
            }

            if (invoicedNow)
            {
                await _jobLifecycleEventPublisher.PublishOrderEventAsync(JobLifecycleEventType.Invoiced, orderId, CancellationToken.None);
            }

            var cogsNow = order.OriginalSONumber;
            if (!string.IsNullOrWhiteSpace(cogsNow) && cogsNow != hadOriginalSO)
            {
                await _jobLifecycleEventPublisher.PublishOrderEventAsync(JobLifecycleEventType.CogsFilled, orderId, CancellationToken.None);
            }
        }

        if (request.WorkflowAttributes is not null)
        {
            var lookup = await BuildAttributeLookupAsync(request.OrderType);
            var seenIds = new HashSet<Guid>();
            var addedRows = new List<JobWorkflow>();
            var now = DateTime.UtcNow;

            foreach (var (name, value) in request.WorkflowAttributes)
            {
                if (!lookup.TryGetValue(name, out var attr))
                {
                    _logger.LogWarning("Unknown workflow attribute '{Name}' for order type {OrderType}", name, request.OrderType);
                    continue;
                }

                seenIds.Add(attr.WorkflowId);

                var existing = await _writeContext.JobWorkflows
                    .FirstOrDefaultAsync(jw => jw.OrderId == orderId && jw.WorkIndex == attr.WorkIndex);

                if (existing is null)
                {
                    var row = new JobWorkflow
                    {
                        JobWorkflowId = Guid.NewGuid(),
                        OrderId = orderId,
                        WorkflowId = attr.WorkflowId,
                        WorkIndex = attr.WorkIndex,
                        WorkTitle = value,
                        WorkStatus = null,
                        WorkInstruction = null,
                        WorkNotes = null,
                        ModifiedOn = now,
                        ModifiedBy = actorId,
                    };
                    addedRows.Add(row);
                    _writeContext.JobWorkflows.Add(row);
                }
                else
                {
                    existing.WorkflowId = attr.WorkflowId;
                    existing.WorkTitle = value;
                    existing.WorkStatus = null;
                    existing.WorkInstruction = null;
                    existing.WorkNotes = null;
                    existing.ModifiedOn = now;
                    existing.ModifiedBy = actorId;
                }
            }

            try
            {
                await _writeContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (SqlUniqueViolation.IsUniqueIndexViolation(ex))
            {
                // A concurrent request already inserted one of the (OrderId, WorkIndex)
                // rows between our check and insert. Reload and merge those as updates.
                _writeContext.ChangeTracker.Clear();
                var freshMap = (await _writeContext.JobWorkflows
                        .Where(jw => jw.OrderId == orderId)
                        .ToListAsync())
                    .ToDictionary(jw => jw.WorkIndex);

                foreach (var row in addedRows)
                {
                    if (freshMap.TryGetValue(row.WorkIndex, out var winner))
                    {
                        winner.WorkflowId = row.WorkflowId;
                        winner.WorkTitle = row.WorkTitle;
                        winner.WorkStatus = row.WorkStatus;
                        winner.WorkInstruction = row.WorkInstruction;
                        winner.WorkNotes = row.WorkNotes;
                        winner.ModifiedOn = row.ModifiedOn;
                        winner.ModifiedBy = row.ModifiedBy;
                    }
                    else
                    {
                        _writeContext.JobWorkflows.Add(row);
                    }
                }

                await _writeContext.SaveChangesAsync();
            }

            var orphaned = await _writeContext.JobWorkflows
                .Where(jw => jw.OrderId == orderId && jw.WorkStatus == null)
                .ToListAsync();

            var toRemove = orphaned.Where(o => o.WorkflowId.HasValue && !seenIds.Contains(o.WorkflowId.Value)).ToList();
            if (toRemove.Count > 0)
            {
                _writeContext.JobWorkflows.RemoveRange(toRemove);
            }

            await _writeContext.SaveChangesAsync();
        }

        var userDisplayNameLookup = BuildUserDisplayNameLookup();
        return MapOrder(order, userDisplayNameLookup);
    }

    public async Task<JobOrderResponse?> DeleteJobOrder(Guid orderId)
    {
        var order = await _writeContext.JobOrders
            .Include(o => o.JobWorkflows)
                .ThenInclude(w => w.JobWorkflowForms)
            .Include(o => o.JobAttachments)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order is null)
        {
            return null;
        }

        // Delete JobWorkflowForms before JobWorkflows (FK constraint)
        var workflowForms = order.JobWorkflows.SelectMany(w => w.JobWorkflowForms).ToList();
        if (workflowForms.Count > 0)
        {
            _writeContext.JobWorkflowForms.RemoveRange(workflowForms);
        }

        if (order.JobWorkflows.Count > 0)
        {
            _writeContext.JobWorkflows.RemoveRange(order.JobWorkflows);
        }

        if (order.JobAttachments.Count > 0)
        {
            _writeContext.JobAttachments.RemoveRange(order.JobAttachments);
        }

        _writeContext.JobOrders.Remove(order);

        // Rebuild sibling job numbers if this order had a job number
        if (order.JobNumber.HasValue && order.JobNumber.Value > 0 && !string.IsNullOrEmpty(order.OrderNumber))
        {
            var siblings = await _writeContext.JobOrders
                .Where(o => o.OrderNumber == order.OrderNumber
                    && o.OrderId != order.OrderId
                    && o.JobNumber.HasValue
                    && o.JobNumber.Value > order.JobNumber.Value)
                .ToListAsync();

            foreach (var sibling in siblings)
            {
                sibling.JobNumber = sibling.JobNumber!.Value - 1;
            }
        }

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
            ProductStyle = job.ProductStyle ?? string.Empty,
            ProductCode = job.ProductCode ?? string.Empty,
            OutputRef = job.OutputRef ?? string.Empty,
            InvoiceRef = job.InvoiceRef ?? string.Empty,
            InvoiceAmount = job.InvoiceAmount ?? 0m,
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
                    Length = 0,
                    AttachmentType = attachment.AttachmentType
                })
                .ToList(),
            SONumber = job.SONumber,
            OriginalSONumber = job.OriginalSONumber,
            WorkflowAttributes = job.JobWorkflows
                .Where(w => w.Workflow != null && w.WorkStatus == null && !string.IsNullOrWhiteSpace(w.WorkTitle))
                .ToDictionary(
                    w => w.Workflow!.WorkflowName ?? string.Empty,
                    w => w.WorkTitle!),
            Step1Status = job.JobWorkflows
                .Where(w => w.WorkIndex == 0)
                .Select(w => (int?)w.WorkStatus)
                .FirstOrDefault(),
            Step2Status = job.JobWorkflows
                .Where(w => w.WorkIndex == 1)
                .Select(w => (int?)w.WorkStatus)
                .FirstOrDefault(),
            Step3Status = job.JobWorkflows
                .Where(w => w.WorkIndex == 2)
                .Select(w => (int?)w.WorkStatus)
                .FirstOrDefault(),
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

        var activeSchedules = job.JobSchedules.Where(schedule => schedule.Cancelled != true).ToList();

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
            ModifiedOn = job.ModifiedOn,
            SONumber = job.SONumber,
            OriginalSONumber = job.OriginalSONumber,
            ScheduledOn = activeSchedules
                .Select(schedule => schedule.ScheduledOn)
                .Where(scheduledOn => scheduledOn.HasValue)
                .Min(scheduledOn => scheduledOn),
            HasActiveSchedule = activeSchedules.Count > 0
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
            ModifiedOn = order.ModifiedOn,
            SONumber = order.SONumber,
            ScheduledOn = null,
            HasActiveSchedule = false
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
        return jobNumber.HasValue ? $"{lhs}-{jobNumber.Value}" : lhs;
    }

    private async Task<Guid?> ResolveUserGuidAsync(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor))
            return null;

        if (Guid.TryParse(actor, out var guid))
            return guid;

        var user = await _readContext.vwUserList_Actives
            .AsNoTracking()
            .Where(u => u.UserName == actor || u.UserAlias == actor)
            .Select(u => (Guid?)u.UserId)
            .FirstOrDefaultAsync();

        return user;
    }

    private async Task<Dictionary<string, (Guid WorkflowId, int WorkIndex)>> BuildAttributeLookupAsync(int orderType)
    {
        return await _readContext.Z_OrderTypeWorkflows
            .AsNoTracking()
            .Where(m => m.OrderType == orderType && m.WorkflowId.HasValue)
            .Include(m => m.Workflow)
            .Where(m => m.Workflow != null)
            .ToDictionaryAsync(
                m => m.Workflow!.WorkflowName ?? string.Empty,
                m => (m.WorkflowId!.Value, m.WorkIndex));
    }
}
