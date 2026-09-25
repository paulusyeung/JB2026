using System.Data.Common;

namespace JB2026.Api.Services;

public interface ISystemInfoStoredProcedureGateway
{
    Task<SystemInfoStoredProcedureRecord?> SelectAsync(Guid systemId, CancellationToken cancellationToken = default);
    Task<SystemInfoStoredProcedureRecord?> SelectFirstAsync(CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateSystemInfoStoredProcedureRequest request, CancellationToken cancellationToken = default, DbTransaction? transaction = null);

    Task<bool> UpdateAsync(UpdateSystemInfoStoredProcedureRequest request, DbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid systemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the single SystemInfo row under an exclusive lock held until the supplied
    /// transaction completes, so a caller can safely read-modify-write the metadata
    /// document without losing a concurrent increment.
    /// </summary>
    Task<SystemInfoStoredProcedureRecord?> SelectFirstForUpdateAsync(DbTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Read-modify-write of the SystemInfo metadata document inside a single transaction, so
    /// concurrent writers of different settings in the same XML blob cannot overwrite each
    /// other's changes. <paramref name="mutate"/> receives the current XML (null when the row
    /// does not exist yet) and returns the XML to persist. Returns the XML that was read, or
    /// null when no row existed and one was inserted.
    /// </summary>
    Task<string?> MutateMetadataAsync(Func<string?, string?> mutate, CancellationToken cancellationToken = default);
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

