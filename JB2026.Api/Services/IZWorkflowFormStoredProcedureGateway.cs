namespace JB2026.Api.Services;

public interface IZWorkflowFormStoredProcedureGateway
{
    Task<ZWorkflowFormStoredProcedureRecord?> SelectAsync(Guid workflowFormId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateZWorkflowFormStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateZWorkflowFormStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid workflowFormId, CancellationToken cancellationToken = default);
}

public sealed record ZWorkflowFormStoredProcedureRecord(
    Guid WorkflowFormId,
    Guid? WorkflowId,
    Guid? FormId,
    int SeqNumber);

public sealed record CreateZWorkflowFormStoredProcedureRequest(
    Guid? WorkflowId,
    Guid? FormId,
    int SeqNumber);

public sealed record UpdateZWorkflowFormStoredProcedureRequest(
    Guid WorkflowFormId,
    Guid? WorkflowId,
    Guid? FormId,
    int SeqNumber);
