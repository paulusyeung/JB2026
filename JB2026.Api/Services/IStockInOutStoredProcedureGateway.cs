namespace JB2026.Api.Services;

public interface IStockInOutStoredProcedureGateway
{
    Task<StockInOutStoredProcedureRecord?> SelectAsync(Guid inOutId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateStockInOutStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateStockInOutStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid inOutId, CancellationToken cancellationToken = default);
}

public sealed record StockInOutStoredProcedureRecord(
    Guid InOutId,
    Guid? ProductId,
    DateTime InOutDate,
    string? Reference,
    int Qty,
    DateTime CreatedOn,
    Guid CreatedBy,
    DateTime ModifiedOn,
    Guid ModifiedBy);

public sealed record CreateStockInOutStoredProcedureRequest(
    Guid? ProductId,
    DateTime? InOutDate,
    string? Reference,
    int Qty,
    DateTime? CreatedOn,
    Guid? CreatedBy,
    DateTime? ModifiedOn,
    Guid? ModifiedBy);

public sealed record UpdateStockInOutStoredProcedureRequest(
    Guid InOutId,
    Guid? ProductId,
    DateTime? InOutDate,
    string? Reference,
    int Qty,
    DateTime? CreatedOn,
    Guid? CreatedBy,
    DateTime? ModifiedOn,
    Guid? ModifiedBy);
