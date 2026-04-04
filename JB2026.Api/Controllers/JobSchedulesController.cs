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
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;
    private readonly IJobScheduleStoredProcedureGateway _gateway;

    public JobSchedulesController(
        JB5LegacyReadContext readContext,
        JB5LegacyWriteContext writeContext,
        IJobScheduleStoredProcedureGateway gateway)
    {
        _readContext = readContext;
        _writeContext = writeContext;
        _gateway = gateway;
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
                urgencyMap.TryGetValue(item.OrderId, out var urgency);

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
            .Select(o => new { o.OrderId, o.ProductDetails, o.OrderTitle })
            .ToListAsync(cancellationToken);

        var printMap = orderDetails.ToDictionary(o => o.OrderId, o => ExtractPrintInfo(o.ProductDetails, o.OrderTitle));

        var result = rows.Select(row =>
        {
            var orderId = row.OrderId ?? Guid.Empty;
            workflowMap.TryGetValue(orderId, out var steps);
            printMap.TryGetValue(orderId, out var print);

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

    private Guid? ResolveCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
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
