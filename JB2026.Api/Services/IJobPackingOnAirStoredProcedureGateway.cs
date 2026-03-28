namespace JB2026.Api.Services;

public interface IJobPackingOnAirStoredProcedureGateway
{
    Task<JobPackingOnAirStoredProcedureRecord?> SelectAsync(Guid onAirId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateJobPackingOnAirStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateJobPackingOnAirStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid onAirId, CancellationToken cancellationToken = default);
}

public sealed record JobPackingOnAirStoredProcedureRecord(
    Guid OnAirId,
    Guid OrderId,
    DateTime OnAiredOn,
    Guid OnAiredBy,
    int? Priority,
    int? Status,
    DateTime CompletedOn,
    Guid? CompletedBy,
    bool? Cancelled,
    DateTime CancelledOn,
    Guid? CancelledBy,
    int? RescheduledCount,
    DateTime RescheduledOn,
    Guid? RescheduledBy);

public sealed record CreateJobPackingOnAirStoredProcedureRequest(
    Guid OrderId,
    DateTime OnAiredOn,
    Guid OnAiredBy,
    int? Priority,
    int? Status,
    DateTime CompletedOn,
    Guid? CompletedBy,
    bool? Cancelled,
    DateTime CancelledOn,
    Guid? CancelledBy,
    int? RescheduledCount,
    DateTime RescheduledOn,
    Guid? RescheduledBy);

public sealed record UpdateJobPackingOnAirStoredProcedureRequest(
    Guid OnAirId,
    Guid OrderId,
    DateTime OnAiredOn,
    Guid OnAiredBy,
    int? Priority,
    int? Status,
    DateTime CompletedOn,
    Guid? CompletedBy,
    bool? Cancelled,
    DateTime CancelledOn,
    Guid? CancelledBy,
    int? RescheduledCount,
    DateTime RescheduledOn,
    Guid? RescheduledBy);
