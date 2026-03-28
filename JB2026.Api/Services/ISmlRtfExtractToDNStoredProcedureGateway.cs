namespace JB2026.Api.Services;

public interface ISmlRtfExtractToDNStoredProcedureGateway
{
    Task<SmlRtfExtractToDNStoredProcedureRecord?> SelectAsync(Guid dnId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateSmlRtfExtractToDNStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateSmlRtfExtractToDNStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid dnId, CancellationToken cancellationToken = default);
}

public sealed record SmlRtfExtractToDNStoredProcedureRecord(
    Guid DNId,
    Guid HeaderId,
    string? DNNumber,
    DateTime DNDate,
    int? DNType,
    DateTime CreatedOn,
    Guid CreatedBy);

public sealed record CreateSmlRtfExtractToDNStoredProcedureRequest(
    Guid HeaderId,
    string? DNNumber,
    DateTime DNDate,
    int? DNType,
    DateTime CreatedOn,
    Guid CreatedBy);

public sealed record UpdateSmlRtfExtractToDNStoredProcedureRequest(
    Guid DNId,
    Guid HeaderId,
    string? DNNumber,
    DateTime DNDate,
    int? DNType,
    DateTime CreatedOn,
    Guid CreatedBy);
