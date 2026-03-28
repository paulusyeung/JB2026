namespace JB2026.Api.Services;

public interface ISmlRtfSubItemStoredProcedureGateway
{
    Task<SmlRtfSubItemStoredProcedureRecord?> SelectAsync(Guid subItemId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateSmlRtfSubItemStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateSmlRtfSubItemStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid subItemId, CancellationToken cancellationToken = default);
}

public sealed record SmlRtfSubItemStoredProcedureRecord(
    Guid SubItemId,
    Guid ItemId,
    int SubLineNumber,
    string? Start_End,
    string? ReferenceNumber,
    string? LabelSize,
    string? Qty);

public sealed record CreateSmlRtfSubItemStoredProcedureRequest(
    Guid ItemId,
    int SubLineNumber,
    string? Start_End,
    string? ReferenceNumber,
    string? LabelSize,
    string? Qty);

public sealed record UpdateSmlRtfSubItemStoredProcedureRequest(
    Guid SubItemId,
    Guid ItemId,
    int SubLineNumber,
    string? Start_End,
    string? ReferenceNumber,
    string? LabelSize,
    string? Qty);
