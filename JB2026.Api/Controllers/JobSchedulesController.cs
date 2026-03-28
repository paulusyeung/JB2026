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
    private readonly IJobScheduleStoredProcedureGateway _gateway;

    public JobSchedulesController(JB5LegacyReadContext readContext, IJobScheduleStoredProcedureGateway gateway)
    {
        _readContext = readContext;
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

    private static string BuildTitle(string? orderNumber, string? orderTitle)
    {
        var left = string.IsNullOrWhiteSpace(orderNumber) ? "(No Number)" : orderNumber.Trim();
        var right = string.IsNullOrWhiteSpace(orderTitle) ? "Untitled Job" : orderTitle.Trim();
        return $"{left} · {right}";
    }
}
