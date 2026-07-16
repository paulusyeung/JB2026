using JB2026.Api.Models;

namespace JB2026.Api.Services.TwentyCrm;

public interface ITwentyCrmService
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CrmCompanyResponse>> GetCompaniesAsync(string? currentUserEmail = null, string? lookup = null, CancellationToken cancellationToken = default);

    Task<CrmCompanyResponse?> GetCompanyByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<CrmCompanyResponse?> UpdateCompanyAsync(string id, UpdateCrmCompanyRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CrmMemberResponse>> GetWorkspaceMembersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CrmCatalogItem>> GetPeopleAsync(string? lookup = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CrmCatalogItem>> GetOpportunitiesAsync(string? lookup = null, CancellationToken cancellationToken = default);
}
