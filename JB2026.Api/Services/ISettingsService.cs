using JB2026.Api.Models;

namespace JB2026.Api.Services;

public interface ISettingsService
{
    SettingsResponse Get();

    SettingsResponse Update(UpdateSettingsRequest request);

    /// <summary>
    /// Reserves and returns the next order number, advancing the persisted counter in the
    /// same operation. Callers must treat the returned value as the only authoritative
    /// order number; never derive one on the client.
    /// </summary>
    Task<string> AllocateNextOrderNumberAsync(CancellationToken cancellationToken = default);
}