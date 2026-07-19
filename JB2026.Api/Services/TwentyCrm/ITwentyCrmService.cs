using JB2026.Api.Models;
using JB2026.EfCore.Data;

namespace JB2026.Api.Services.TwentyCrm;

public interface ITwentyCrmService
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CrmCompanyResponse>> GetCompaniesAsync(string? currentUserEmail = null, string? lookup = null, JB5LegacyReadContext? readContext = null, CancellationToken cancellationToken = default);

    Task<HashSet<string>> GetAllCompanyNamesAsync(CancellationToken cancellationToken = default);

    Task<CrmCompanyResponse?> GetCompanyByIdAsync(string id, JB5LegacyReadContext? readContext = null, CancellationToken cancellationToken = default);

    Task<CrmCompanyResponse?> UpdateCompanyAsync(string id, UpdateCrmCompanyRequest request, CancellationToken cancellationToken = default);

    Task<CrmCompanyCreatedResponse?> CreateCompanyAsync(CreateCrmCompanyRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CrmMemberResponse>> GetWorkspaceMembersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CrmPersonResponse>> GetPeopleAsync(string? lookup = null, CancellationToken cancellationToken = default);

    Task<CrmPersonResponse?> UpdatePersonAsync(string id, UpdateCrmPersonRequest request, CancellationToken cancellationToken = default);

    Task<CrmPersonResponse?> CreatePersonAsync(UpdateCrmPersonRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CrmOpportunityResponse>> GetOpportunitiesAsync(string? lookup = null, CancellationToken cancellationToken = default);

    Task<CrmOpportunityResponse?> GetOpportunityByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<CrmOpportunityResponse?> UpdateOpportunityAsync(string id, UpdateCrmOpportunityRequest request, CancellationToken cancellationToken = default);

    Task<CrmOpportunityResponse?> CreateOpportunityAsync(UpdateCrmOpportunityRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CrmStageOption>> GetOpportunityStageOptionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CrmStageOption>> GetTaskStatusOptionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CrmTaskResponse>> GetTasksAsync(string? lookup = null, CancellationToken cancellationToken = default);

    Task<CrmTaskResponse?> GetTaskByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<CrmTaskResponse?> UpdateTaskAsync(string id, UpdateCrmTaskRequest request, CancellationToken cancellationToken = default);

    Task<CrmTaskResponse?> CreateTaskAsync(UpdateCrmTaskRequest request, CancellationToken cancellationToken = default);
}
