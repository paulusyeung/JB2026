namespace JB2026.Api.Services;

public interface IZFormStoredProcedureGateway
{
    Task<ZFormStoredProcedureRecord?> SelectAsync(Guid formId, CancellationToken cancellationToken = default);

    Task<Guid> InsertAsync(CreateZFormStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UpdateZFormStoredProcedureRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid formId, CancellationToken cancellationToken = default);
}

public sealed record ZFormStoredProcedureRecord(
    Guid FormId,
    int FormObjectEnum,
    string? FormName,
    string? FormName_Chs,
    string? FormName_Cht,
    string? MetadataXml);

public sealed record CreateZFormStoredProcedureRequest(
    int FormObjectEnum,
    string? FormName,
    string? FormName_Chs,
    string? FormName_Cht,
    string? MetadataXml);

public sealed record UpdateZFormStoredProcedureRequest(
    Guid FormId,
    int FormObjectEnum,
    string? FormName,
    string? FormName_Chs,
    string? FormName_Cht,
    string? MetadataXml);
