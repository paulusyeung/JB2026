namespace JB2026.Api.Services;

public interface ICustomerStoredProcedureGateway
{
    Task<CustomerStoredProcedureRecord?> SelectAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateCustomerStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateCustomerStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid customerId, CancellationToken cancellationToken = default);
}

public sealed record CustomerStoredProcedureRecord(
    Guid CustomerId,
    string? CustomerName,
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

public sealed record CreateCustomerStoredProcedureRequest(
    string? CustomerName,
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

public sealed record UpdateCustomerStoredProcedureRequest(
    Guid CustomerId,
    string? CustomerName,
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
