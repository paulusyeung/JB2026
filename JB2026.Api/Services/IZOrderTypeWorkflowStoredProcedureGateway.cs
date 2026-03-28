namespace JB2026.Api.Services;

public interface IZOrderTypeWorkflowStoredProcedureGateway
{
    Task<ZOrderTypeWorkflowStoredProcedureRecord?> SelectAsync(Guid orderTypeWorkflowId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateZOrderTypeWorkflowStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateZOrderTypeWorkflowStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid orderTypeWorkflowId, CancellationToken cancellationToken = default);
}

public sealed record ZOrderTypeWorkflowStoredProcedureRecord(
    Guid OrderTypeWorkflowId,
    Guid? WorkflowId,
    int OrderType,
    int WorkIndex);

public sealed record CreateZOrderTypeWorkflowStoredProcedureRequest(
    Guid? WorkflowId,
    int OrderType,
    int WorkIndex);

public sealed record UpdateZOrderTypeWorkflowStoredProcedureRequest(
    Guid OrderTypeWorkflowId,
    Guid? WorkflowId,
    int OrderType,
    int WorkIndex);
