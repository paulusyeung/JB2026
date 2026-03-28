namespace JB2026.Api.Services;

public interface IJobWorkflowStoredProcedureGateway
{
    Task<JobWorkflowStoredProcedureRecord?> SelectAsync(Guid jobWorkflowId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateJobWorkflowStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateJobWorkflowStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid jobWorkflowId, CancellationToken cancellationToken = default);
}

public sealed record JobWorkflowStoredProcedureRecord(
    Guid JobWorkflowId,
    Guid OrderId,
    Guid? WorkflowId,
    int WorkIndex,
    string? WorkTitle,
    string? WorkInstruction,
    int? WorkStatus,
    string? WorkNotes,
    DateTime? ModifiedOn,
    Guid? ModifiedBy);

public sealed record CreateJobWorkflowStoredProcedureRequest(
    Guid OrderId,
    Guid? WorkflowId,
    int WorkIndex,
    string? WorkTitle,
    string? WorkInstruction,
    int? WorkStatus,
    string? WorkNotes,
    DateTime? ModifiedOn,
    Guid? ModifiedBy);

public sealed record UpdateJobWorkflowStoredProcedureRequest(
    Guid JobWorkflowId,
    Guid OrderId,
    Guid? WorkflowId,
    int WorkIndex,
    string? WorkTitle,
    string? WorkInstruction,
    int? WorkStatus,
    string? WorkNotes,
    DateTime? ModifiedOn,
    Guid? ModifiedBy);
