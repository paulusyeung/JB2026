namespace JB2026.Api.Services;

public interface ISystemInfoStoredProcedureGateway
{
    Task<SystemInfoStoredProcedureRecord?> SelectAsync(Guid systemId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateSystemInfoStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateSystemInfoStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid systemId, CancellationToken cancellationToken = default);
}

public sealed record SystemInfoStoredProcedureRecord(
    Guid SystemId,
    string? OwnerName,
    string? MetadataXml);

public sealed record CreateSystemInfoStoredProcedureRequest(
    string? OwnerName,
    string? MetadataXml);

public sealed record UpdateSystemInfoStoredProcedureRequest(
    Guid SystemId,
    string? OwnerName,
    string? MetadataXml);
