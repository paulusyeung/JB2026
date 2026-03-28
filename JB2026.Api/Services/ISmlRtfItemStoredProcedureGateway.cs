namespace JB2026.Api.Services;

public interface ISmlRtfItemStoredProcedureGateway
{
    Task<SmlRtfItemStoredProcedureRecord?> SelectAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateSmlRtfItemStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateSmlRtfItemStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid itemId, CancellationToken cancellationToken = default);
}

public sealed record SmlRtfItemStoredProcedureRecord(
    Guid ItemId,
    Guid HeaderId,
    int LineNumber,
    string? ProductCode,
    string? ProductDescription,
    string? Price,
    string? Discount,
    string? Qty,
    string? Amount,
    string? PostProcess);

public sealed record CreateSmlRtfItemStoredProcedureRequest(
    Guid HeaderId,
    int LineNumber,
    string? ProductCode,
    string? ProductDescription,
    string? Price,
    string? Discount,
    string? Qty,
    string? Amount,
    string? PostProcess);

public sealed record UpdateSmlRtfItemStoredProcedureRequest(
    Guid ItemId,
    Guid HeaderId,
    int LineNumber,
    string? ProductCode,
    string? ProductDescription,
    string? Price,
    string? Discount,
    string? Qty,
    string? Amount,
    string? PostProcess);
