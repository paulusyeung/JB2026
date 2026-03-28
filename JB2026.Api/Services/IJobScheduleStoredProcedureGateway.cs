namespace JB2026.Api.Services;

public interface IJobScheduleStoredProcedureGateway
{
    Task<JobScheduleStoredProcedureRecord?> SelectAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateJobScheduleStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateJobScheduleStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid scheduleId, CancellationToken cancellationToken = default);
}

public sealed record JobScheduleStoredProcedureRecord(
    Guid ScheduleId,
    Guid OrderId,
    DateTime? ScheduledOn,
    int? Status,
    int? Priority,
    string? MachineNumber,
    DateTime? CompletedOn,
    bool? ShouldReview,
    int UrgencyLevel,
    bool? Cancelled,
    DateTime? CancelledOn,
    Guid? CancelledBy,
    int? RescheduledCount,
    Guid? RescheduledBy,
    DateTime? RescheduledOn);

public sealed record CreateJobScheduleStoredProcedureRequest(
    Guid OrderId,
    DateTime? ScheduledOn,
    int? Status,
    int? Priority,
    string? MachineNumber,
    DateTime? CompletedOn,
    bool? ShouldReview,
    int UrgencyLevel,
    bool? Cancelled,
    DateTime? CancelledOn,
    Guid? CancelledBy,
    int? RescheduledCount,
    Guid? RescheduledBy,
    DateTime? RescheduledOn);

public sealed record UpdateJobScheduleStoredProcedureRequest(
    Guid ScheduleId,
    Guid OrderId,
    DateTime? ScheduledOn,
    int? Status,
    int? Priority,
    string? MachineNumber,
    DateTime? CompletedOn,
    bool? ShouldReview,
    int UrgencyLevel,
    bool? Cancelled,
    DateTime? CancelledOn,
    Guid? CancelledBy,
    int? RescheduledCount,
    Guid? RescheduledBy,
    DateTime? RescheduledOn);
