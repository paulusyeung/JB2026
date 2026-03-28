namespace JB2026.Api.Services;

public interface ISupplierStoredProcedureGateway
{
    Task<SupplierStoredProcedureRecord?> SelectAsync(Guid supplierId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateSupplierStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateSupplierStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid supplierId, CancellationToken cancellationToken = default);
}

public sealed record SupplierStoredProcedureRecord(
    Guid SupplierId,
    string? SupplierName,
    string? LoginAccount,
    string? LoginPassword,
    string? MetadataXml,
    DateTime CreatedOn,
    Guid CreatedBy,
    DateTime ModifiedOn,
    Guid ModifiedBy,
    bool Retired,
    DateTime? RetiredOn,
    Guid? RetiredBy);

public sealed record CreateSupplierStoredProcedureRequest(
    string? SupplierName,
    string? LoginAccount,
    string? LoginPassword,
    string? MetadataXml,
    DateTime? CreatedOn,
    Guid? CreatedBy,
    DateTime? ModifiedOn,
    Guid? ModifiedBy,
    bool Retired,
    DateTime? RetiredOn,
    Guid? RetiredBy);

public sealed record UpdateSupplierStoredProcedureRequest(
    Guid SupplierId,
    string? SupplierName,
    string? LoginAccount,
    string? LoginPassword,
    string? MetadataXml,
    DateTime? CreatedOn,
    Guid? CreatedBy,
    DateTime? ModifiedOn,
    Guid? ModifiedBy,
    bool Retired,
    DateTime? RetiredOn,
    Guid? RetiredBy);
