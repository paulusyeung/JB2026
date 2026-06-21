using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using JB2026.Api.Models;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/job-schedules")]
public sealed class JobSchedulesController : ControllerBase
{
    private static readonly DateTime LegacyEmptyDate = new(1900, 1, 1);
    private const int PackingStatusDraft = 0;
    private const int PackingStatusCompleted = 1;
    private const int PackingWorkflowIndex = 2;
    private const int WorkflowStatusRed = 0;
    private const int WorkflowStatusYellow = 1;
    private const int WorkflowStatusGreen = 2;

    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;
    private readonly IJobScheduleStoredProcedureGateway _gateway;
    private readonly IJobPackingOnAirStoredProcedureGateway _packingOnAirGateway;

    public JobSchedulesController(
        JB5LegacyReadContext readContext,
        JB5LegacyWriteContext writeContext,
        IJobScheduleStoredProcedureGateway gateway,
        IJobPackingOnAirStoredProcedureGateway packingOnAirGateway)
    {
        _readContext = readContext;
        _writeContext = writeContext;
        _gateway = gateway;
        _packingOnAirGateway = packingOnAirGateway;
    }

    [HttpGet("range")]
    [ProducesResponseType(typeof(IReadOnlyList<JobScheduleCalendarItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobScheduleCalendarItemResponse>>> GetRange(
        [FromQuery] DateOnly startOn,
        [FromQuery] int days,
        CancellationToken cancellationToken)
    {
        if (days is <= 0 or > 31)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(days)] = ["Days must be between 1 and 31."]
            }));
        }

        var start = startOn.ToDateTime(TimeOnly.MinValue);
        var end = start.AddDays(days);

        var records = await _readContext.JobSchedules
            .AsNoTracking()
            .Include(schedule => schedule.Order)
            .Where(schedule =>
                schedule.ScheduledOn.HasValue &&
                schedule.ScheduledOn.Value >= start &&
                schedule.ScheduledOn.Value < end &&
                schedule.Cancelled != true)
            .OrderBy(schedule => schedule.ScheduledOn)
            .Select(schedule => new JobScheduleCalendarItemResponse
            {
                ScheduleId = schedule.ScheduleId,
                OrderId = schedule.OrderId,
                Title = BuildTitle(schedule.Order.OrderNumber, schedule.Order.OrderTitle),
                StartOn = schedule.ScheduledOn!.Value,
                EndOn = schedule.CompletedOn,
                Status = schedule.Status,
                Priority = schedule.Priority,
                MachineNumber = schedule.MachineNumber
            })
            .Take(500)
            .ToListAsync(cancellationToken);

        return Ok(records);
    }

    [HttpGet("pending")]
    [ProducesResponseType(typeof(IReadOnlyList<JobSchedulePendingItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobSchedulePendingItemResponse>>> GetPending(
        [FromQuery] string? lookup,
        [FromQuery] int? commonQuery,
        [FromQuery] string? startsWith,
        [FromQuery] int take = 500,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var safeTake = Math.Clamp(take, 1, 2000);
            var today = DateTime.Today;
            var allowJobOrdersFallback = string.Equals(
                _readContext.Database.ProviderName,
                "Microsoft.EntityFrameworkCore.InMemory",
                StringComparison.Ordinal);

            // Try legacy pending view first
            List<PendingRow> baseRows;
            try
            {
                var viewQuery = _readContext.vwJobSchedule_PendingLists
                    .AsNoTracking();

                viewQuery = commonQuery.GetValueOrDefault() switch
                {
                    1 => viewQuery.Where(item => item.OrderedOn.HasValue && item.OrderedOn.Value >= today.AddDays(-30) && item.OrderedOn.Value < today.AddDays(1)),
                    2 => viewQuery.Where(item => item.OrderedOn.HasValue && item.OrderedOn.Value >= today.AddDays(-90) && item.OrderedOn.Value < today.AddDays(1)),
                    _ => viewQuery
                };

                if (!string.IsNullOrWhiteSpace(lookup))
                {
                    var keyword = lookup.Trim();
                    viewQuery = viewQuery.Where(item =>
                        (item.OrderNumber != null && item.OrderNumber.Contains(keyword)) ||
                        (item.JobOrderNumber != null && item.JobOrderNumber.Contains(keyword)) ||
                        (item.CustomerName != null && item.CustomerName.Contains(keyword)) ||
                        (item.OrderTitle != null && item.OrderTitle.Contains(keyword)));
                }

                var viewRows = await viewQuery
                    .OrderByDescending(item => item.JobOrderNumber)
                    .ThenByDescending(item => item.OrderNumber)
                    .Take(safeTake)
                    .ToListAsync(cancellationToken);

                baseRows = viewRows.Select(item => new PendingRow
                {
                    OrderId = item.OrderId,
                    OrderType = item.OrderType,
                    OrderNumber = item.OrderNumber,
                    JobNumber = item.JobNumber,
                    JobOrderNumber = item.JobOrderNumber,
                    CustomerName = item.CustomerName,
                    OrderTitle = item.OrderTitle,
                    Status = item.Status,
                    OrderedOn = item.OrderedOn,
                    RequiredOn = item.RequiredOn,
                }).ToList();
            }
            catch when (allowJobOrdersFallback)
            {
                // The in-memory test provider does not materialize SQL views.
                baseRows = new List<PendingRow>();
            }

            // Fallback to JobOrders if view is empty or unavailable
            if (allowJobOrdersFallback && baseRows.Count == 0)
            {
                var fallbackQuery = _readContext.JobOrders
                    .AsNoTracking()
                    .Where(item => item.Status == 1 && !item.Retired);

                fallbackQuery = commonQuery.GetValueOrDefault() switch
                {
                    1 => fallbackQuery.Where(item => item.OrderedOn.HasValue && item.OrderedOn.Value >= today.AddDays(-30) && item.OrderedOn.Value < today.AddDays(1)),
                    2 => fallbackQuery.Where(item => item.OrderedOn.HasValue && item.OrderedOn.Value >= today.AddDays(-90) && item.OrderedOn.Value < today.AddDays(1)),
                    _ => fallbackQuery
                };

                if (!string.IsNullOrWhiteSpace(lookup))
                {
                    var keyword = lookup.Trim();
                    fallbackQuery = fallbackQuery.Where(item =>
                        (item.OrderNumber != null && item.OrderNumber.Contains(keyword)) ||
                        (item.CustomerName != null && item.CustomerName.Contains(keyword)) ||
                        (item.OrderTitle != null && item.OrderTitle.Contains(keyword)));
                }

                var fallbackResults = await fallbackQuery
                    .OrderByDescending(item => item.OrderNumber)
                    .Take(safeTake)
                    .ToListAsync(cancellationToken);

                baseRows = fallbackResults.Select(item => new PendingRow
                {
                    OrderId = item.OrderId,
                    OrderType = item.OrderType,
                    OrderNumber = item.OrderNumber,
                    JobNumber = item.JobNumber,
                    JobOrderNumber = null,
                    CustomerName = item.CustomerName,
                    OrderTitle = item.OrderTitle,
                    Status = item.Status,
                    OrderedOn = item.OrderedOn,
                    RequiredOn = item.RequiredOn,
                }).ToList();
            }

            if (!string.IsNullOrWhiteSpace(startsWith))
            {
                baseRows = baseRows
                    .Where(item => MatchesStartsWith(item, startsWith))
                    .ToList();
            }

            // Empty result is valid
            if (baseRows.Count == 0)
            {
                return Ok(Array.Empty<JobSchedulePendingItemResponse>());
            }

            // Now fetch workflow and urgency data
            var orderIds = baseRows.Select(item => item.OrderId).Distinct().ToArray();

            var workflowRows = await WhereOrderIdIn(
                    _readContext.JobWorkflows
                        .AsNoTracking(),
                    workflow => workflow.OrderId,
                    orderIds)
                .Where(workflow => workflow.WorkIndex >= 0 && workflow.WorkIndex <= 2)
                .ToListAsync(cancellationToken);

            var workflowMap = workflowRows
                .GroupBy(row => row.OrderId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderBy(row => row.WorkIndex)
                        .ToDictionary(row => row.WorkIndex, row => row.WorkStatus ?? 0));

            var scheduleRows = await WhereOrderIdIn(
                    _readContext.JobSchedules
                        .AsNoTracking(),
                    schedule => schedule.OrderId,
                    orderIds)
                .Where(schedule => schedule.Cancelled != true)
                .ToListAsync(cancellationToken);

            var urgencyMap = scheduleRows
                .GroupBy(row => row.OrderId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(row => row.ScheduledOn ?? DateTime.MinValue)
                            .Select(row => row.UrgencyLevel)
                        .FirstOrDefault());

            var result = baseRows.Select(item =>
            {
                workflowMap.TryGetValue(item.OrderId, out var steps);
                var urgency = urgencyMap.TryGetValue(item.OrderId, out var mappedUrgency)
                    ? mappedUrgency
                    : -1;

                return new JobSchedulePendingItemResponse
                {
                    OrderId = item.OrderId,
                    OrderType = item.OrderType,
                    OrderNumber = string.IsNullOrWhiteSpace(item.JobOrderNumber)
                        ? BuildCompositeOrderNumber(item.OrderNumber, item.JobNumber)
                        : item.JobOrderNumber!,
                    CustomerName = item.CustomerName ?? string.Empty,
                    OrderTitle = item.OrderTitle ?? string.Empty,
                    Status = item.Status,
                    OrderedOn = item.OrderedOn,
                    RequiredOn = item.RequiredOn,
                    UrgencyLevel = urgency,
                    Step1Status = steps != null && steps.TryGetValue(0, out var step1) ? step1 : null,
                    Step2Status = steps != null && steps.TryGetValue(1, out var step2) ? step2 : null,
                    Step3Status = steps != null && steps.TryGetValue(2, out var step3) ? step3 : null,
                };
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpGet("completed")]
    [ProducesResponseType(typeof(IReadOnlyList<JobScheduleCompletedItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobScheduleCompletedItemResponse>>> GetCompleted(
        [FromQuery] string? lookup,
        [FromQuery] int? commonQuery,
        [FromQuery] string? machine,
        [FromQuery] string? startsWith,
        [FromQuery] int take = 500,
        CancellationToken cancellationToken = default)
    {
        var safeTake = Math.Clamp(take, 1, 2000);
        var today = DateTime.Today;

        var query = _readContext.vwJobScheduleLists
            .AsNoTracking()
            .Where(item =>
                item.Status == 1 &&
                item.CompletedOn.HasValue);

        query = commonQuery.GetValueOrDefault() switch
        {
            1 => query.Where(item =>
                item.CompletedOn.HasValue &&
                item.CompletedOn.Value >= today.AddDays(-7) &&
                item.CompletedOn.Value < today.AddDays(1)),
            2 => query.Where(item =>
                item.CompletedOn.HasValue &&
                item.CompletedOn.Value >= today.AddDays(-30) &&
                item.CompletedOn.Value < today.AddDays(1)),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(lookup))
        {
            var keyword = lookup.Trim();
            query = query.Where(item =>
                (item.OrderNumber != null && item.OrderNumber.Contains(keyword)) ||
                (item.CustomerName != null && item.CustomerName.Contains(keyword)) ||
                (item.OrderTitle != null && item.OrderTitle.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(machine) && machine != "0")
        {
            query = query.Where(item => item.MachineNumber == machine);
        }

        if (!string.IsNullOrWhiteSpace(startsWith))
        {
            var prefix = startsWith.Trim();
            if (prefix == "9")
            {
                query = query.Where(item =>
                    item.OrderNumber != null &&
                    item.OrderNumber.Length > 0 &&
                    item.OrderNumber[0] >= '0' &&
                    item.OrderNumber[0] <= '9');
            }
            else
            {
                query = query.Where(item => item.OrderNumber != null && item.OrderNumber.StartsWith(prefix));
            }
        }

        var rows = await query
            .OrderByDescending(item => item.OrderNumber)
            .Take(safeTake)
            .ToListAsync(cancellationToken);

        var result = rows
            .Where(item => item.OrderId.HasValue)
            .Select(item => new JobScheduleCompletedItemResponse
            {
                OrderId = item.OrderId!.Value,
                OrderType = item.OrderType ?? 0,
                OrderNumber = item.OrderNumber ?? string.Empty,
                CustomerName = item.CustomerName ?? string.Empty,
                OrderTitle = item.OrderTitle ?? string.Empty,
                Status = item.Status ?? 0,
                MachineNumber = item.MachineNumber ?? string.Empty,
                OrderedOn = item.OrderedOn,
                RequiredOn = item.RequiredOn,
                ScheduledOn = item.ScheduledOn,
                CompletedOn = item.CompletedOn,
            })
            .ToList();

        return Ok(result);
    }

    [HttpGet("packing")]
    [ProducesResponseType(typeof(IReadOnlyList<JobSchedulePackingItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobSchedulePackingItemResponse>>> GetPacking(
        [FromQuery] string? lookup,
        [FromQuery] int? commonQuery,
        [FromQuery] string? startsWith,
        [FromQuery] int take = 500,
        CancellationToken cancellationToken = default)
    {
        var safeTake = Math.Clamp(take, 1, 2000);
        var today = DateTime.Today;

        var query = _readContext.vwJobOrder_PackingLists
            .AsNoTracking()
            .Where(item => item.Status == 1);

        query = commonQuery.GetValueOrDefault() switch
        {
            1 => query.Where(item => item.OrderedOn.HasValue && item.OrderedOn.Value >= today.AddDays(-30) && item.OrderedOn.Value < today.AddDays(1)),
            2 => query.Where(item => item.OrderedOn.HasValue && item.OrderedOn.Value >= today.AddDays(-90) && item.OrderedOn.Value < today.AddDays(1)),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(lookup))
        {
            var keyword = lookup.Trim();
            query = query.Where(item =>
                (item.OrderNumber != null && item.OrderNumber.Contains(keyword)) ||
                (item.JobOrderNumber != null && item.JobOrderNumber.Contains(keyword)) ||
                (item.CustomerName != null && item.CustomerName.Contains(keyword)) ||
                (item.OrderTitle != null && item.OrderTitle.Contains(keyword)));
        }

        var rows = await query
            .OrderByDescending(item => item.JobOrderNumber)
            .ThenByDescending(item => item.OrderNumber)
            .Take(safeTake * 2)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return Ok(Array.Empty<JobSchedulePackingItemResponse>());
        }

        var orderIds = rows.Select(item => item.OrderId).Distinct().ToList();

        var workflowRows = await WhereOrderIdIn(
                _readContext.JobWorkflows
                    .AsNoTracking(),
                workflow => workflow.OrderId,
                orderIds)
            .Where(workflow => workflow.WorkIndex >= 0 && workflow.WorkIndex <= 2)
            .ToListAsync(cancellationToken);

        var workflowMap = workflowRows
            .GroupBy(row => row.OrderId)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(row => row.WorkIndex, row => row.WorkStatus));

        var orderRemarks = await WhereOrderIdIn(
                _readContext.JobOrders
                    .AsNoTracking(),
                order => order.OrderId,
                orderIds)
            .Select(order => new { order.OrderId, order.Remarks })
            .ToListAsync(cancellationToken);

        var remarksMap = orderRemarks.ToDictionary(item => item.OrderId, item => item.Remarks ?? string.Empty);

        var result = rows
            .Where(row =>
            {
                if (!workflowMap.TryGetValue(row.OrderId, out var steps))
                {
                    return false;
                }

                var hasStep1 = steps.ContainsKey(0);
                var hasStep2 = steps.ContainsKey(1);
                var hasStep3 = steps.ContainsKey(2);

                var step1 = hasStep1 ? steps[0] : null;
                var step2 = hasStep2 ? steps[1] : null;

                var singleStepPacking = hasStep1 && !hasStep2 && !hasStep3 && step1 != 3;
                var threeStepPacking = hasStep1 && hasStep2 && hasStep3 && step2 != 3;
                return singleStepPacking || threeStepPacking;
            })
            .Select(row =>
            {
                workflowMap.TryGetValue(row.OrderId, out var steps);
                remarksMap.TryGetValue(row.OrderId, out var remarks);

                return new JobSchedulePackingItemResponse
                {
                    OrderId = row.OrderId,
                    OrderType = row.OrderType,
                    OrderNumber = string.IsNullOrWhiteSpace(row.JobOrderNumber)
                        ? BuildCompositeOrderNumber(row.OrderNumber, row.JobNumber)
                        : row.JobOrderNumber!,
                    CustomerName = row.CustomerName ?? string.Empty,
                    OrderTitle = row.OrderTitle ?? string.Empty,
                    Status = row.Status,
                    OrderedOn = row.OrderedOn,
                    RequiredOn = row.RequiredOn,
                    Step1Status = steps != null && steps.TryGetValue(0, out var step1) ? step1 : null,
                    Step2Status = steps != null && steps.TryGetValue(1, out var step2) ? step2 : null,
                    Step3Status = steps != null && steps.TryGetValue(2, out var step3) ? step3 : null,
                    Remarks = remarks ?? string.Empty,
                };
            })
            .Where(item => MatchesStartsWith(new PendingRow
            {
                OrderId = item.OrderId,
                OrderType = item.OrderType,
                OrderNumber = item.OrderNumber,
                JobNumber = null,
                JobOrderNumber = item.OrderNumber,
                CustomerName = item.CustomerName,
                OrderTitle = item.OrderTitle,
                Status = item.Status,
                OrderedOn = item.OrderedOn,
                RequiredOn = item.RequiredOn,
            }, startsWith ?? string.Empty))
            .Take(safeTake)
            .ToList();

        return Ok(result);
    }

    [HttpGet("packing-on-air/available")]
    [ProducesResponseType(typeof(IReadOnlyList<JobPackingOnAirAvailableItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobPackingOnAirAvailableItemResponse>>> GetPackingOnAirAvailable(
        [FromQuery] int orderType = 0,
        CancellationToken cancellationToken = default)
    {
        var rows = await _readContext.vwAvailableJobPackingLists
            .AsNoTracking()
            .Where(item => item.OrderType == orderType)
            .OrderByDescending(item => item.OrderNumber)
            .Take(1000)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return Ok(Array.Empty<JobPackingOnAirAvailableItemResponse>());
        }

        var orderIds = rows.Select(item => item.OrderId).Distinct().ToList();

        var workflowRows = await WhereOrderIdIn(
                _readContext.JobWorkflows
                    .AsNoTracking(),
                workflow => workflow.OrderId,
                orderIds)
            .Where(workflow => workflow.WorkIndex == 0 || workflow.WorkIndex == 1)
            .ToListAsync(cancellationToken);

        var workflowMap = workflowRows
            .GroupBy(workflow => workflow.OrderId)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(workflow => workflow.WorkIndex, workflow => workflow.WorkStatus));

        var remarksRows = await WhereOrderIdIn(
                _readContext.JobOrders
                    .AsNoTracking(),
                order => order.OrderId,
                orderIds)
            .Select(order => new { order.OrderId, order.Remarks })
            .ToListAsync(cancellationToken);

        var remarksMap = remarksRows.ToDictionary(item => item.OrderId, item => item.Remarks ?? string.Empty);

        var result = rows
            .Where(row =>
            {
                if (!workflowMap.TryGetValue(row.OrderId, out var steps))
                {
                    return false;
                }

                var step1Ready = steps.TryGetValue(0, out var step1) && step1 == WorkflowStatusGreen;
                var step2Ready = steps.TryGetValue(1, out var step2) && step2 == WorkflowStatusGreen;
                return step1Ready && step2Ready;
            })
            .Select(row =>
            {
                remarksMap.TryGetValue(row.OrderId, out var remarks);

                return new JobPackingOnAirAvailableItemResponse
                {
                    OrderId = row.OrderId,
                    OrderType = row.OrderType,
                    OrderNumber = row.OrderNumber ?? string.Empty,
                    CustomerName = row.CustomerName ?? string.Empty,
                    OrderTitle = row.OrderTitle ?? string.Empty,
                    Remarks = remarks ?? string.Empty,
                };
            })
            .ToList();

        return Ok(result);
    }

    [HttpGet("packing-on-air")]
    [ProducesResponseType(typeof(IReadOnlyList<JobPackingOnAirItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobPackingOnAirItemResponse>>> GetPackingOnAir(
        [FromQuery] int orderType = 0,
        CancellationToken cancellationToken = default)
    {
        var rows = await _readContext.vwAvailableJobPackingOnAirLists
            .AsNoTracking()
            .Where(item => item.OrderType == orderType)
            .OrderBy(item => item.Priority)
            .ThenByDescending(item => item.OrderNumber)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var orderIds = rows
            .Select(item => item.OrderId)
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Distinct()
            .ToList();

        var remarksRows = orderIds.Count == 0
            ? []
            : await WhereOrderIdIn(
                    _readContext.JobOrders
                        .AsNoTracking(),
                    order => order.OrderId,
                    orderIds)
                .Select(order => new { order.OrderId, order.Remarks })
                .ToListAsync(cancellationToken);

        var remarksMap = remarksRows.ToDictionary(item => item.OrderId, item => item.Remarks ?? string.Empty);

        var result = rows
            .Where(row => row.OrderId.HasValue)
            .Select(row =>
            {
                var orderId = row.OrderId!.Value;
                remarksMap.TryGetValue(orderId, out var remarks);

                return new JobPackingOnAirItemResponse
                {
                    OnAirId = row.OnAirId,
                    OrderId = orderId,
                    OrderType = row.OrderType ?? 0,
                    OrderNumber = row.OrderNumber ?? string.Empty,
                    CustomerName = row.CustomerName ?? string.Empty,
                    OrderTitle = row.OrderTitle ?? string.Empty,
                    Priority = row.Priority ?? 0,
                    Remarks = remarks ?? string.Empty,
                };
            })
            .ToList();

        return Ok(result);
    }

    [HttpPost("packing-on-air/batch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SavePackingOnAirBatch(
        [FromBody] SavePackingOnAirBatchRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var currentUserId = ResolveCurrentUserId() ?? Guid.Empty;

        for (var index = 0; index < request.SelectedItems.Count; index++)
        {
            var item = request.SelectedItems[index];

            var existing = await _readContext.JobPackingOnAirs
                .AsNoTracking()
                .Where(record => record.OrderId == item.OrderId)
                .OrderByDescending(record => record.OnAiredOn)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is null)
            {
                await _packingOnAirGateway.InsertAsync(new CreateJobPackingOnAirStoredProcedureRequest(
                    OrderId: item.OrderId,
                    OnAiredOn: now,
                    OnAiredBy: currentUserId,
                    Priority: index,
                    Status: PackingStatusDraft,
                    CompletedOn: LegacyEmptyDate,
                    CompletedBy: null,
                    Cancelled: false,
                    CancelledOn: LegacyEmptyDate,
                    CancelledBy: null,
                    RescheduledCount: 0,
                    RescheduledOn: LegacyEmptyDate,
                    RescheduledBy: null), cancellationToken);
            }
            else
            {
                await _packingOnAirGateway.UpdateAsync(new UpdateJobPackingOnAirStoredProcedureRequest(
                    OnAirId: existing.OnAirId,
                    OrderId: existing.OrderId,
                    OnAiredOn: now,
                    OnAiredBy: currentUserId,
                    Priority: index,
                    Status: PackingStatusDraft,
                    CompletedOn: LegacyEmptyDate,
                    CompletedBy: null,
                    Cancelled: false,
                    CancelledOn: LegacyEmptyDate,
                    CancelledBy: null,
                    RescheduledCount: existing.RescheduledCount,
                    RescheduledOn: existing.RescheduledOn,
                    RescheduledBy: existing.RescheduledBy), cancellationToken);
            }

            await UpdatePackingWorkflowStatusAsync(item.OrderId, WorkflowStatusYellow, now, cancellationToken);
        }

        foreach (var orderId in request.CancelledOrderIds.Distinct())
        {
            var existing = await _readContext.JobPackingOnAirs
                .AsNoTracking()
                .Where(record => record.OrderId == orderId)
                .OrderByDescending(record => record.OnAiredOn)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is null)
            {
                continue;
            }

            await _packingOnAirGateway.UpdateAsync(new UpdateJobPackingOnAirStoredProcedureRequest(
                OnAirId: existing.OnAirId,
                OrderId: existing.OrderId,
                OnAiredOn: existing.OnAiredOn,
                OnAiredBy: existing.OnAiredBy,
                Priority: existing.Priority,
                Status: existing.Status,
                CompletedOn: existing.CompletedOn,
                CompletedBy: existing.CompletedBy,
                Cancelled: true,
                CancelledOn: now,
                CancelledBy: currentUserId,
                RescheduledCount: existing.RescheduledCount,
                RescheduledOn: existing.RescheduledOn,
                RescheduledBy: existing.RescheduledBy), cancellationToken);

            await UpdatePackingWorkflowStatusAsync(orderId, WorkflowStatusRed, now, cancellationToken);
        }

        await _writeContext.SaveChangesAsync(cancellationToken);

        return Ok(new { saved = request.SelectedItems.Count, cancelled = request.CancelledOrderIds.Count });
    }

    [HttpPost("packing-on-air/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CompletePackingOnAir(
        [FromBody] CompletePackingOnAirRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var currentUserId = ResolveCurrentUserId();
        var completed = 0;

        foreach (var orderId in request.OrderIds.Distinct())
        {
            var existing = await _readContext.JobPackingOnAirs
                .AsNoTracking()
                .Where(record => record.OrderId == orderId && record.Cancelled != true)
                .OrderByDescending(record => record.OnAiredOn)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is null)
            {
                continue;
            }

            await _packingOnAirGateway.UpdateAsync(new UpdateJobPackingOnAirStoredProcedureRequest(
                OnAirId: existing.OnAirId,
                OrderId: existing.OrderId,
                OnAiredOn: existing.OnAiredOn,
                OnAiredBy: existing.OnAiredBy,
                Priority: existing.Priority,
                Status: PackingStatusCompleted,
                CompletedOn: now,
                CompletedBy: currentUserId,
                Cancelled: existing.Cancelled,
                CancelledOn: existing.CancelledOn,
                CancelledBy: existing.CancelledBy,
                RescheduledCount: existing.RescheduledCount,
                RescheduledOn: existing.RescheduledOn,
                RescheduledBy: existing.RescheduledBy), cancellationToken);

            await UpdatePackingWorkflowStatusAsync(orderId, WorkflowStatusGreen, now, cancellationToken);
            completed++;
        }

        await _writeContext.SaveChangesAsync(cancellationToken);
        return Ok(new { completed });
    }

    [HttpPatch("{id:guid}/time")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateTime(Guid id, [FromBody] UpdateJobScheduleTimeRequest request, CancellationToken cancellationToken)
    {
        if (request.StartOn is null)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.StartOn)] = ["StartOn is required."]
            }));
        }

        var existing = await _gateway.SelectAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Schedule not found",
                Detail = $"No schedule exists for id '{id}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var updated = new UpdateJobScheduleStoredProcedureRequest(
            ScheduleId: existing.ScheduleId,
            OrderId: existing.OrderId,
            ScheduledOn: request.StartOn,
            Status: existing.Status,
            Priority: existing.Priority,
            MachineNumber: existing.MachineNumber,
            CompletedOn: request.EndOn,
            ShouldReview: existing.ShouldReview,
            UrgencyLevel: existing.UrgencyLevel,
            Cancelled: existing.Cancelled,
            CancelledOn: existing.CancelledOn,
            CancelledBy: existing.CancelledBy,
            RescheduledCount: (existing.RescheduledCount ?? 0) + 1,
            RescheduledBy: existing.RescheduledBy,
            RescheduledOn: DateTime.UtcNow);

        await _gateway.UpdateAsync(updated, cancellationToken);
        return NoContent();
    }

    [HttpGet("available")]
    [ProducesResponseType(typeof(IReadOnlyList<JobScheduleAvailableItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobScheduleAvailableItemResponse>>> GetAvailable(
        [FromQuery] int orderType = 0,
        CancellationToken cancellationToken = default)
    {
        var rows = await _readContext.vwJobSchedule_AvailableLists
            .AsNoTracking()
            .Where(item => item.OrderType == orderType)
            .OrderByDescending(item => item.OrderNumber)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var result = rows.Select(item => new JobScheduleAvailableItemResponse
        {
            OrderId = item.OrderId,
            OrderType = item.OrderType,
            OrderNumber = item.OrderNumber ?? string.Empty,
            CustomerName = item.CustomerName ?? string.Empty,
            OrderTitle = item.OrderTitle ?? string.Empty,
        }).ToList();

        return Ok(result);
    }

    [HttpGet("on-air")]
    [ProducesResponseType(typeof(IReadOnlyList<JobScheduleOnAirItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobScheduleOnAirItemResponse>>> GetOnAir(
        [FromQuery] int orderType = 0,
        [FromQuery] string? machine = null,
        CancellationToken cancellationToken = default)
    {
        var query = _readContext.vwJobSchedule_OnAirLists
            .AsNoTracking()
            .Where(item => item.OrderType == orderType);

        if (!string.IsNullOrWhiteSpace(machine) && machine != "0")
        {
            query = query.Where(item => item.MachineNumber == machine);
        }

        var rows = await query
            .OrderBy(item => item.MachineNumber)
            .ThenByDescending(item => item.UrgencyLevel)
            .ThenBy(item => item.Priority)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return Ok(Array.Empty<JobScheduleOnAirItemResponse>());
        }

        var orderIds = rows.Select(item => item.OrderId ?? Guid.Empty).Where(id => id != Guid.Empty).Distinct().ToList();

        var workflowRows = await WhereOrderIdIn(
                _readContext.JobWorkflows
                    .AsNoTracking(),
                workflow => workflow.OrderId,
                orderIds)
            .Where(wf => wf.WorkIndex == 0 || wf.WorkIndex == 1)
            .ToListAsync(cancellationToken);

        var workflowMap = workflowRows
            .GroupBy(wf => wf.OrderId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(wf => wf.WorkIndex, wf => wf.WorkStatus));

        var orderDetails = await WhereOrderIdIn(
                _readContext.JobOrders
                    .AsNoTracking(),
                order => order.OrderId,
                orderIds)
            .Select(o => new { o.OrderId, o.ProductDetails, o.OrderTitle, o.SONumber })
            .ToListAsync(cancellationToken);

        var printMap = orderDetails.ToDictionary(o => o.OrderId, o => ExtractPrintInfo(o.ProductDetails, o.OrderTitle));
        var soMap = orderDetails.Where(o => !string.IsNullOrWhiteSpace(o.SONumber)).ToDictionary(o => o.OrderId, o => o.SONumber);

        var result = rows.Select(row =>
        {
            var orderId = row.OrderId ?? Guid.Empty;
            workflowMap.TryGetValue(orderId, out var steps);
            printMap.TryGetValue(orderId, out var print);
            soMap.TryGetValue(orderId, out var soNumber);

            return new JobScheduleOnAirItemResponse
            {
                ScheduleId = row.ScheduleId,
                OrderId = orderId,
                OrderType = row.OrderType ?? 0,
                OrderNumber = row.OrderNumber ?? string.Empty,
                CustomerName = row.CustomerName ?? string.Empty,
                OrderTitle = row.OrderTitle ?? string.Empty,
                Priority = row.Priority ?? 0,
                MachineNumber = row.MachineNumber ?? string.Empty,
                UrgencyLevel = row.UrgencyLevel,
                Step1Status = steps != null && steps.TryGetValue(0, out var s1) ? s1 : null,
                Step2Status = steps != null && steps.TryGetValue(1, out var s2) ? s2 : null,
                PrintQty = print?[2] ?? string.Empty,
                PrintColor = print?[1] ?? string.Empty,
                PrintSize = print?[0] ?? string.Empty,
                SONumber = soNumber,
            };
        }).ToList();

        return Ok(result);
    }

    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveBatch([FromBody] SaveScheduleBatchRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var currentUserId = ResolveCurrentUserId();

        // Upsert each scheduled item
        for (var i = 0; i < request.ScheduledItems.Count; i++)
        {
            var item = request.ScheduledItems[i];

            var existingSchedule = await _readContext.JobSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.OrderId == item.OrderId && s.Cancelled != true, cancellationToken);

            if (existingSchedule is not null)
            {
                await _gateway.UpdateAsync(new UpdateJobScheduleStoredProcedureRequest(
                    ScheduleId: existingSchedule.ScheduleId,
                    OrderId: item.OrderId,
                    ScheduledOn: existingSchedule.ScheduledOn ?? now,
                    Status: existingSchedule.Status,
                    Priority: i,
                    MachineNumber: item.MachineNumber,
                    CompletedOn: existingSchedule.CompletedOn,
                    ShouldReview: existingSchedule.ShouldReview,
                    UrgencyLevel: item.UrgencyLevel,
                    Cancelled: false,
                    CancelledOn: existingSchedule.CancelledOn,
                    CancelledBy: existingSchedule.CancelledBy,
                    RescheduledCount: existingSchedule.RescheduledCount,
                    RescheduledBy: existingSchedule.RescheduledBy,
                    RescheduledOn: existingSchedule.RescheduledOn), cancellationToken);
            }
            else
            {
                await _gateway.InsertAsync(new CreateJobScheduleStoredProcedureRequest(
                    OrderId: item.OrderId,
                    ScheduledOn: now,
                    Status: 1,
                    Priority: i,
                    MachineNumber: item.MachineNumber,
                    CompletedOn: null,
                    ShouldReview: false,
                    UrgencyLevel: item.UrgencyLevel,
                    Cancelled: false,
                    CancelledOn: null,
                    CancelledBy: null,
                    RescheduledCount: 0,
                    RescheduledBy: null,
                    RescheduledOn: null), cancellationToken);
            }

            // Update workflow step statuses
            var step1Wf = await _writeContext.JobWorkflows
                .FirstOrDefaultAsync(wf => wf.OrderId == item.OrderId && wf.WorkIndex == 0, cancellationToken);
            if (step1Wf is not null)
            {
                step1Wf.WorkStatus = item.Step1Status;
                step1Wf.ModifiedOn = now;
            }

            var step2Wf = await _writeContext.JobWorkflows
                .FirstOrDefaultAsync(wf => wf.OrderId == item.OrderId && wf.WorkIndex == 1, cancellationToken);
            if (step2Wf is not null)
            {
                step2Wf.WorkStatus = item.Step2Status;
                step2Wf.ModifiedOn = now;
            }
        }

        await _writeContext.SaveChangesAsync(cancellationToken);

        // Cancel removed items
        foreach (var orderId in request.CancelledOrderIds)
        {
            var cancelSchedule = await _readContext.JobSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.OrderId == orderId && s.Cancelled != true, cancellationToken);

            if (cancelSchedule is not null)
            {
                await _gateway.UpdateAsync(new UpdateJobScheduleStoredProcedureRequest(
                    ScheduleId: cancelSchedule.ScheduleId,
                    OrderId: orderId,
                    ScheduledOn: cancelSchedule.ScheduledOn,
                    Status: cancelSchedule.Status,
                    Priority: cancelSchedule.Priority,
                    MachineNumber: cancelSchedule.MachineNumber,
                    CompletedOn: cancelSchedule.CompletedOn,
                    ShouldReview: cancelSchedule.ShouldReview,
                    UrgencyLevel: cancelSchedule.UrgencyLevel,
                    Cancelled: true,
                    CancelledOn: now,
                    CancelledBy: currentUserId,
                    RescheduledCount: cancelSchedule.RescheduledCount,
                    RescheduledBy: cancelSchedule.RescheduledBy,
                    RescheduledOn: cancelSchedule.RescheduledOn), cancellationToken);
            }
        }

        return Ok(new { saved = request.ScheduledItems.Count, cancelled = request.CancelledOrderIds.Count });
    }

    [HttpPatch("pending/{orderId:guid}/workflow")]
    [ProducesResponseType(typeof(PendingWorkflowUpdateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PendingWorkflowUpdateResponse>> UpdatePendingWorkflow(
        Guid orderId,
        [FromBody] UpdatePendingWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        if (request.StepIndex is < 0 or > 2)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.StepIndex)] = ["StepIndex must be 0, 1, or 2."]
            }));
        }

        if (request.TargetStatus is < 0 or > 3)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.TargetStatus)] = ["TargetStatus must be 0 (red), 1 (yellow), 2 (green), or 3 (blue)."]
            }));
        }

        var workflow = await _writeContext.JobWorkflows
            .FirstOrDefaultAsync(
                wf => wf.OrderId == orderId && wf.WorkIndex == request.StepIndex,
                cancellationToken);

        if (workflow is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Workflow step not found",
                Detail = $"No workflow step {request.StepIndex} exists for order '{orderId}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        workflow.WorkStatus = request.TargetStatus;
        workflow.ModifiedOn = DateTime.Now;
        await _writeContext.SaveChangesAsync(cancellationToken);

        // Re-read all steps to return a normalized response
        var allSteps = await _writeContext.JobWorkflows
            .Where(wf => wf.OrderId == orderId && wf.WorkIndex >= 0 && wf.WorkIndex <= 2)
            .ToListAsync(cancellationToken);

        var stepMap = allSteps.ToDictionary(s => s.WorkIndex, s => s.WorkStatus);

        return Ok(new PendingWorkflowUpdateResponse
        {
            OrderId = orderId,
            Step1Status = stepMap.TryGetValue(0, out var s1) ? s1 : null,
            Step2Status = stepMap.TryGetValue(1, out var s2) ? s2 : null,
            Step3Status = stepMap.TryGetValue(2, out var s3) ? s3 : null,
        });
    }

    [HttpPatch("pending/{orderId:guid}/urgency")]
    [ProducesResponseType(typeof(PendingUrgencyUpdateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PendingUrgencyUpdateResponse>> UpdatePendingUrgency(
        Guid orderId,
        [FromBody] UpdatePendingUrgencyRequest request,
        CancellationToken cancellationToken)
    {
        const int UrgencyNeutral = -1;
        const int UrgencyYellow = 2;
        const int UrgencyRed = 4;

        int targetLevel;
        if (string.Equals(request.TargetColor, "red", StringComparison.OrdinalIgnoreCase))
        {
            targetLevel = UrgencyRed;
        }
        else if (string.Equals(request.TargetColor, "yellow", StringComparison.OrdinalIgnoreCase))
        {
            targetLevel = UrgencyYellow;
        }
        else
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.TargetColor)] = ["TargetColor must be 'red' or 'yellow'."]
            }));
        }

        var schedule = await _writeContext.JobSchedules
            .Where(s => s.OrderId == orderId && s.Cancelled != true)
            .OrderByDescending(s => s.ScheduledOn ?? DateTime.MinValue)
            .FirstOrDefaultAsync(cancellationToken);

        if (schedule is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Schedule not found",
                Detail = $"No active schedule exists for order '{orderId}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        // Toggle: if current urgency matches target, revert to neutral
        var currentUrgency = schedule.UrgencyLevel;
        var newUrgency = currentUrgency == targetLevel ? UrgencyNeutral : targetLevel;

        schedule.UrgencyLevel = newUrgency;
        await _writeContext.SaveChangesAsync(cancellationToken);

        return Ok(new PendingUrgencyUpdateResponse
        {
            OrderId = orderId,
            UrgencyLevel = newUrgency,
        });
    }

    [HttpPost("completed/reschedule")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RescheduleCompleted(
        [FromBody] RescheduleCompletedSchedulesRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = ResolveCurrentUserId();
        var now = DateTime.UtcNow;
        var updated = 0;

        foreach (var orderId in request.OrderIds.Distinct())
        {
            var schedule = await _readContext.JobSchedules
                .AsNoTracking()
                .Where(item =>
                    item.OrderId == orderId &&
                    item.Cancelled != true &&
                    item.CompletedOn.HasValue)
                .OrderByDescending(item => item.CompletedOn)
                .ThenByDescending(item => item.ScheduledOn)
                .FirstOrDefaultAsync(cancellationToken);

            if (schedule is null)
            {
                continue;
            }

            await _gateway.UpdateAsync(new UpdateJobScheduleStoredProcedureRequest(
                ScheduleId: schedule.ScheduleId,
                OrderId: schedule.OrderId,
                ScheduledOn: schedule.ScheduledOn,
                Status: schedule.Status,
                Priority: schedule.Priority,
                MachineNumber: schedule.MachineNumber,
                CompletedOn: null,
                ShouldReview: schedule.ShouldReview,
                UrgencyLevel: schedule.UrgencyLevel,
                Cancelled: true,
                CancelledOn: now,
                CancelledBy: currentUserId,
                RescheduledCount: (schedule.RescheduledCount ?? 0) + 1,
                RescheduledBy: currentUserId,
                RescheduledOn: now), cancellationToken);

            updated++;
        }

        return Ok(new { updated });
    }

    private Guid? ResolveCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private async Task UpdatePackingWorkflowStatusAsync(
        Guid orderId,
        int status,
        DateTime modifiedOn,
        CancellationToken cancellationToken)
    {
        var workflow = await _writeContext.JobWorkflows
            .FirstOrDefaultAsync(
                item => item.OrderId == orderId && item.WorkIndex == PackingWorkflowIndex,
                cancellationToken);

        if (workflow is null)
        {
            return;
        }

        workflow.WorkStatus = status;
        workflow.ModifiedOn = modifiedOn;
    }

    private static string[] ExtractPrintInfo(string? productDetails, string? orderTitle)
    {
        var plainText = StripHtml(productDetails);
        var info0 = GetLabeledValue(plainText, ["印張尺寸", "尺寸", "size"]);
        var info1 = GetLabeledValue(plainText, ["顏色", "color"]);
        var info2 = GetLabeledValue(plainText, ["石數", "數量", "qty", "quantity", "名稱", "name"]);
        if (string.IsNullOrWhiteSpace(info2))
        {
            info2 = orderTitle ?? string.Empty;
        }

        return [info0, info1, info2];
    }

    private static string StripHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var noTags = Regex.Replace(input, "<.*?>", " ", RegexOptions.Singleline);
        return WebUtility.HtmlDecode(noTags);
    }

    private static string GetLabeledValue(string text, string[] labels)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        foreach (var line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            foreach (var label in labels)
            {
                if (!trimmed.StartsWith(label, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var parts = trimmed.Split(new[] { ':', '：' }, 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                {
                    return parts[1].Trim();
                }
            }
        }

        return string.Empty;
    }

    private static string BuildTitle(string? orderNumber, string? orderTitle)
    {
        var left = string.IsNullOrWhiteSpace(orderNumber) ? "(No Number)" : orderNumber.Trim();
        var right = string.IsNullOrWhiteSpace(orderTitle) ? "Untitled Job" : orderTitle.Trim();
        return $"{left} · {right}";
    }

    private static string BuildCompositeOrderNumber(string? orderNumber, int? jobNumber)
    {
        var orderPart = string.IsNullOrWhiteSpace(orderNumber) ? "(No Number)" : orderNumber.Trim();
        if (!jobNumber.HasValue || jobNumber.Value <= 0)
        {
            return orderPart;
        }

        return $"{orderPart}-{jobNumber.Value}";
    }

    private static bool MatchesStartsWith(PendingRow item, string startsWith)
    {
        var prefix = startsWith.Trim();
        if (prefix.Length == 0)
        {
            return true;
        }

        var candidate = string.IsNullOrWhiteSpace(item.JobOrderNumber)
            ? item.OrderNumber
            : item.JobOrderNumber;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        candidate = candidate.Trim();
        if (string.Equals(prefix, "9", StringComparison.Ordinal))
        {
            return char.IsDigit(candidate[0]);
        }

        return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static IQueryable<TEntity> WhereOrderIdIn<TEntity>(
        IQueryable<TEntity> source,
        Expression<Func<TEntity, Guid>> orderIdSelector,
        IReadOnlyList<Guid> orderIds)
    {
        if (orderIds.Count == 0)
        {
            return source.Where(_ => false);
        }

        var parameter = orderIdSelector.Parameters[0];
        Expression body = Expression.Equal(orderIdSelector.Body, Expression.Constant(orderIds[0]));

        for (var index = 1; index < orderIds.Count; index++)
        {
            body = Expression.OrElse(
                body,
                Expression.Equal(orderIdSelector.Body, Expression.Constant(orderIds[index])));
        }

        return source.Where(Expression.Lambda<Func<TEntity, bool>>(body, parameter));
    }

    private sealed class PendingRow
    {
        public required Guid OrderId { get; init; }

        public required int OrderType { get; init; }

        public string? OrderNumber { get; init; }

        public int? JobNumber { get; init; }

        public string? JobOrderNumber { get; init; }

        public string? CustomerName { get; init; }

        public string? OrderTitle { get; init; }

        public int Status { get; init; }

        public DateTime? OrderedOn { get; init; }

        public DateTime? RequiredOn { get; init; }
    }
}
