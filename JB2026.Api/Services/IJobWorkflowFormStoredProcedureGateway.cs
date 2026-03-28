namespace JB2026.Api.Services;

public interface IJobWorkflowFormStoredProcedureGateway
{
    Task<JobWorkflowFormStoredProcedureRecord?> SelectAsync(Guid jobWorkflowFormId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateJobWorkflowFormStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateJobWorkflowFormStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid jobWorkflowFormId, CancellationToken cancellationToken = default);
}

public sealed record JobWorkflowFormStoredProcedureRecord(
    Guid JobWorkflowFormId,
    Guid JobWorkflowId,
    Guid? FormId,
    int? SeqNumber,
    string? MetadataXml);

public sealed record CreateJobWorkflowFormStoredProcedureRequest(
    Guid JobWorkflowId,
    Guid? FormId,
    int? SeqNumber,
    string? MetadataXml);

public sealed record UpdateJobWorkflowFormStoredProcedureRequest(
    Guid JobWorkflowFormId,
    Guid JobWorkflowId,
    Guid? FormId,
    int? SeqNumber,
    string? MetadataXml);
