namespace JB2026.Api.Services;

public interface IInvoiceHeaderStoredProcedureGateway
{
    Task<InvoiceHeaderStoredProcedureRecord?> SelectAsync(Guid headerId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateInvoiceHeaderStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateInvoiceHeaderStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid headerId, CancellationToken cancellationToken = default);
}

public sealed record InvoiceHeaderStoredProcedureRecord(
    Guid HeaderId,
    Guid? CustomerId,
    string? BillTo,
    string? ShipTo,
    DateTime InvoiceDate,
    string? InvoiceNumber,
    decimal? InvoiceAmount,
    string? ICNumber,
    DateTime CreatedOn,
    Guid CreatedBy,
    DateTime ModifiedOn,
    Guid ModifiedBy,
    bool Retired,
    DateTime? RetiredOn,
    Guid? RetiredBy);

public sealed record CreateInvoiceHeaderStoredProcedureRequest(
    Guid? CustomerId,
    string? BillTo,
    string? ShipTo,
    DateTime? InvoiceDate,
    string? InvoiceNumber,
    decimal? InvoiceAmount,
    string? ICNumber,
    DateTime? CreatedOn,
    Guid? CreatedBy,
    DateTime? ModifiedOn,
    Guid? ModifiedBy,
    bool Retired,
    DateTime? RetiredOn,
    Guid? RetiredBy);

public sealed record UpdateInvoiceHeaderStoredProcedureRequest(
    Guid HeaderId,
    Guid? CustomerId,
    string? BillTo,
    string? ShipTo,
    DateTime? InvoiceDate,
    string? InvoiceNumber,
    decimal? InvoiceAmount,
    string? ICNumber,
    DateTime? CreatedOn,
    Guid? CreatedBy,
    DateTime? ModifiedOn,
    Guid? ModifiedBy,
    bool Retired,
    DateTime? RetiredOn,
    Guid? RetiredBy);
