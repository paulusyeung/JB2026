namespace JB2026.Api.Services;

public interface IInvoiceSubItemStoredProcedureGateway
{
    Task<InvoiceSubItemStoredProcedureRecord?> SelectAsync(Guid subItemId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateInvoiceSubItemStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateInvoiceSubItemStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid subItemId, CancellationToken cancellationToken = default);
}

public sealed record InvoiceSubItemStoredProcedureRecord(
    Guid SubItemId,
    Guid ItemId,
    int SubLineNumber,
    string? Description,
    decimal? Quantity,
    string? UoM,
    decimal? Price,
    decimal? Amount);

public sealed record CreateInvoiceSubItemStoredProcedureRequest(
    Guid ItemId,
    int SubLineNumber,
    string? Description,
    decimal? Quantity,
    string? UoM,
    decimal? Price,
    decimal? Amount);

public sealed record UpdateInvoiceSubItemStoredProcedureRequest(
    Guid SubItemId,
    Guid ItemId,
    int SubLineNumber,
    string? Description,
    decimal? Quantity,
    string? UoM,
    decimal? Price,
    decimal? Amount);
