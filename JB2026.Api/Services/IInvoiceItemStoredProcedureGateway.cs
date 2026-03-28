namespace JB2026.Api.Services;

public interface IInvoiceItemStoredProcedureGateway
{
    Task<InvoiceItemStoredProcedureRecord?> SelectAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateInvoiceItemStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateInvoiceItemStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid itemId, CancellationToken cancellationToken = default);
}

public sealed record InvoiceItemStoredProcedureRecord(
    Guid ItemId,
    Guid HeaderId,
    Guid? SmlRtfHeaderId,
    int LineNumber,
    string? Notes);

public sealed record CreateInvoiceItemStoredProcedureRequest(
    Guid HeaderId,
    Guid? SmlRtfHeaderId,
    int LineNumber,
    string? Notes);

public sealed record UpdateInvoiceItemStoredProcedureRequest(
    Guid ItemId,
    Guid HeaderId,
    Guid? SmlRtfHeaderId,
    int LineNumber,
    string? Notes);
