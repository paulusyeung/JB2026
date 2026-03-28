using JB2026.Api.Models;

namespace JB2026.Api.Services;

public interface IJobManagementRepository
{
    IReadOnlyList<JobListItemResponse> GetRange(DateOnly startOn, int days);

    JobDetailResponse? GetJobDetail(Guid orderId);

    IReadOnlyList<string> GetStyleTitles(Guid orderId);

    IReadOnlyList<JobOrderResponse> GetJobOrders(int take);

    JobOrderResponse? GetJobOrder(Guid orderId);

    Task<JobOrderResponse> CreateJobOrder(CreateJobOrderRequest request, string actor);

    Task<JobOrderResponse?> UpdateJobOrder(Guid orderId, UpdateJobOrderRequest request, string actor);

    Task<JobOrderResponse?> DeleteJobOrder(Guid orderId);
}
