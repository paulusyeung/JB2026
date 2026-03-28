namespace JB2026.Api.Services;

public interface ISmlRtfHeaderStoredProcedureGateway
{
    Task<SmlRtfHeaderStoredProcedureRecord?> SelectAsync(Guid headerId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateSmlRtfHeaderStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateSmlRtfHeaderStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid headerId, CancellationToken cancellationToken = default);
}

public sealed record SmlRtfHeaderStoredProcedureRecord(
    Guid HeaderId,
    string? RtfFileName,
    string? PurchaseOrder,
    string? CustomerPO,
    DateTime OrderedOn,
    string? OrderedBy,
    string? OriginalPO,
    string? SalesOrder,
    string? OriginalSO,
    string? Remarks,
    DateTime CreatedOn,
    Guid? CreatedBy,
    DateTime ModifiedOn,
    Guid ModifiedBy,
    bool Retired,
    DateTime RetiredOn,
    Guid? RetiredBy);

public sealed record CreateSmlRtfHeaderStoredProcedureRequest(
    string? RtfFileName,
    string? PurchaseOrder,
    string? CustomerPO,
    DateTime OrderedOn,
    string? OrderedBy,
    string? OriginalPO,
    string? SalesOrder,
    string? OriginalSO,
    string? Remarks,
    DateTime CreatedOn,
    Guid? CreatedBy,
    DateTime ModifiedOn,
    Guid ModifiedBy,
    bool Retired,
    DateTime RetiredOn,
    Guid? RetiredBy);

public sealed record UpdateSmlRtfHeaderStoredProcedureRequest(
    Guid HeaderId,
    string? RtfFileName,
    string? PurchaseOrder,
    string? CustomerPO,
    DateTime OrderedOn,
    string? OrderedBy,
    string? OriginalPO,
    string? SalesOrder,
    string? OriginalSO,
    string? Remarks,
    DateTime CreatedOn,
    Guid? CreatedBy,
    DateTime ModifiedOn,
    Guid ModifiedBy,
    bool Retired,
    DateTime RetiredOn,
    Guid? RetiredBy);
