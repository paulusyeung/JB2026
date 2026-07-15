using JB2026.Api.Models;

namespace JB2026.Api.Services.TwentyCrm;

public interface ITwentyCrmService
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CrmCompanyResponse>> GetCompaniesAsync(string? currentUserEmail = null, string? lookup = null, CancellationToken cancellationToken = default);
}
