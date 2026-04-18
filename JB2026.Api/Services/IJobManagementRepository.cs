using JB2026.Api.Models;

namespace JB2026.Api.Services;

public interface IJobManagementRepository
{
    IReadOnlyList<JobListItemResponse> GetRange(DateOnly startOn, int days);

    JobDetailResponse? GetJobDetail(Guid orderId);

    IReadOnlyList<string> GetStyleTitles(Guid orderId);

    IReadOnlyList<JobOrderResponse> GetJobOrders(int take);

    IReadOnlyList<JobOrderResponse> GetJobList(string? lookup, int commonQuery, string? startsWith, int take);

    IReadOnlyList<JobOrderResponse> GetOrderList(string? lookup, int commonQuery, string? startsWith, DateOnly? startOn = null, DateOnly? endOn = null);

    IReadOnlyList<JobStatsResponse> GetJobStats(DateOnly? startOn, DateOnly? endOn);

    JobOrderResponse? GetJobOrder(Guid orderId);

    Task<JobOrderResponse> CreateJobOrder(CreateJobOrderRequest request, string actor);

    Task<JobOrderResponse?> UpdateJobOrder(Guid orderId, UpdateJobOrderRequest request, string actor);

    Task<JobOrderResponse?> DeleteJobOrder(Guid orderId);
}
