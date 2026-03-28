namespace JB2026.Api.Services;

public interface IProductStoredProcedureGateway
{
    Task<ProductStoredProcedureRecord?> SelectAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateProductStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateProductStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid productId, CancellationToken cancellationToken = default);
}

public sealed record ProductStoredProcedureRecord(
    Guid ProductId,
    Guid? CategoryId,
    string? StockNumber,
    string? ProductCode,
    string? ProductName,
    string? Description,
    string? Remarks,
    int MOQ,
    int Balance,
    decimal SellingPrice,
    decimal COGS,
    DateTime CreatedOn,
    Guid CreatedBy,
    DateTime ModifiedOn,
    Guid ModifiedBy,
    bool Retired,
    DateTime? RetiredOn,
    Guid? RetiredBy);

public sealed record CreateProductStoredProcedureRequest(
    Guid? CategoryId,
    string? StockNumber,
    string? ProductCode,
    string? ProductName,
    string? Description,
    string? Remarks,
    int MOQ,
    int Balance,
    decimal SellingPrice,
    decimal COGS,
    DateTime? CreatedOn,
    Guid? CreatedBy,
    DateTime? ModifiedOn,
    Guid? ModifiedBy,
    bool Retired,
    DateTime? RetiredOn,
    Guid? RetiredBy);

public sealed record UpdateProductStoredProcedureRequest(
    Guid ProductId,
    Guid? CategoryId,
    string? StockNumber,
    string? ProductCode,
    string? ProductName,
    string? Description,
    string? Remarks,
    int MOQ,
    int Balance,
    decimal SellingPrice,
    decimal COGS,
    DateTime? CreatedOn,
    Guid? CreatedBy,
    DateTime? ModifiedOn,
    Guid? ModifiedBy,
    bool Retired,
    DateTime? RetiredOn,
    Guid? RetiredBy);
