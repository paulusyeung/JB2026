namespace JB2026.Api.Services;

public interface IZWorkflowStoredProcedureGateway
{
    Task<ZWorkflowStoredProcedureRecord?> SelectAsync(Guid workflowId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateZWorkflowStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateZWorkflowStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid workflowId, CancellationToken cancellationToken = default);
}

public sealed record ZWorkflowStoredProcedureRecord(
    Guid WorkflowId,
    string? WorkflowName,
    string? WorkTitle,
    string? WorkInstruction);

public sealed record CreateZWorkflowStoredProcedureRequest(
    string? WorkflowName,
    string? WorkTitle,
    string? WorkInstruction);

public sealed record UpdateZWorkflowStoredProcedureRequest(
    Guid WorkflowId,
    string? WorkflowName,
    string? WorkTitle,
    string? WorkInstruction);
