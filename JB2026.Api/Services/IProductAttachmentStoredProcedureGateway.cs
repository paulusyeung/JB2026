namespace JB2026.Api.Services;

public interface IProductAttachmentStoredProcedureGateway
{
    Task<ProductAttachmentStoredProcedureRecord?> SelectAsync(Guid attachmentId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateProductAttachmentStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateProductAttachmentStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid attachmentId, CancellationToken cancellationToken = default);
}

public sealed record ProductAttachmentStoredProcedureRecord(
    Guid AttachmentId,
    Guid ProductId,
    int AttachmentIndex,
    string? OriginalFileName);

public sealed record CreateProductAttachmentStoredProcedureRequest(
    Guid ProductId,
    int AttachmentIndex,
    string? OriginalFileName);

public sealed record UpdateProductAttachmentStoredProcedureRequest(
    Guid AttachmentId,
    Guid ProductId,
    int AttachmentIndex,
    string? OriginalFileName);
