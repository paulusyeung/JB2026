namespace JB2026.Api.Services;

public interface IJobAttachmentStoredProcedureGateway
{
    Task<JobAttachmentStoredProcedureRecord?> SelectAsync(Guid attachmentId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateJobAttachmentStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateJobAttachmentStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid attachmentId, CancellationToken cancellationToken = default);
}

public sealed record JobAttachmentStoredProcedureRecord(
    Guid AttachmentId,
    Guid? OrderId,
    int AttachmentType,
    int AttachmentIndex,
    string? OriginalFileName);

public sealed record CreateJobAttachmentStoredProcedureRequest(
    Guid? OrderId,
    int AttachmentType,
    int AttachmentIndex,
    string? OriginalFileName);

public sealed record UpdateJobAttachmentStoredProcedureRequest(
    Guid AttachmentId,
    Guid? OrderId,
    int AttachmentType,
    int AttachmentIndex,
    string? OriginalFileName);
