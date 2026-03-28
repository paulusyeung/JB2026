namespace JB2026.Api.Services;

public interface IUserInfoStoredProcedureGateway
{
    Task<UserInfoStoredProcedureRecord?> SelectAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateUserInfoStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateUserInfoStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record UserInfoStoredProcedureRecord(
    Guid UserId,
    bool PrimaryRec,
    string? UserName,
    string? UserPassword,
    string? UserAlias,
    int UserRole,
    string? MetadataXml,
    DateTime CreatedOn,
    Guid CreatedBy,
    DateTime ModifiedOn,
    Guid ModifiedBy,
    bool Retired,
    DateTime? RetiredOn,
    Guid? RetiredBy);

public sealed record CreateUserInfoStoredProcedureRequest(
    bool PrimaryRec,
    string? UserName,
    string? UserPassword,
    string? UserAlias,
    int UserRole,
    string? MetadataXml,
    DateTime CreatedOn,
    Guid CreatedBy,
    DateTime ModifiedOn,
    Guid ModifiedBy,
    bool Retired,
    DateTime RetiredOn,
    Guid RetiredBy);

public sealed record UpdateUserInfoStoredProcedureRequest(
    Guid UserId,
    bool PrimaryRec,
    string? UserName,
    string? UserPassword,
    string? UserAlias,
    int UserRole,
    string? MetadataXml,
    DateTime CreatedOn,
    Guid CreatedBy,
    DateTime ModifiedOn,
    Guid ModifiedBy,
    bool Retired,
    DateTime RetiredOn,
    Guid RetiredBy);
