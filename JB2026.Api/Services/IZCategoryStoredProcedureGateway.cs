namespace JB2026.Api.Services;

public interface IZCategoryStoredProcedureGateway
{
    Task<ZCategoryStoredProcedureRecord?> SelectAsync(Guid categoryId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateZCategoryStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateZCategoryStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid categoryId, CancellationToken cancellationToken = default);
}

public sealed record ZCategoryStoredProcedureRecord(
    Guid CategoryId,
    string? CategoryCode,
    string? CategoryName,
    DateTime CreatedOn,
    Guid CreatedBy,
    DateTime ModifiedOn,
    Guid ModifiedBy,
    bool Retired,
    DateTime? RetiredOn,
    Guid? RetiredBy);

public sealed record CreateZCategoryStoredProcedureRequest(
    string? CategoryCode,
    string? CategoryName,
    DateTime CreatedOn,
    Guid CreatedBy,
    DateTime ModifiedOn,
    Guid ModifiedBy,
    bool Retired,
    DateTime? RetiredOn,
    Guid? RetiredBy);

public sealed record UpdateZCategoryStoredProcedureRequest(
    Guid CategoryId,
    string? CategoryCode,
    string? CategoryName,
    DateTime CreatedOn,
    Guid CreatedBy,
    DateTime ModifiedOn,
    Guid ModifiedBy,
    bool Retired,
    DateTime? RetiredOn,
    Guid? RetiredBy);
