using JB2026.Api.Models;

namespace JB2026.Api.Services;

public interface IJobOrderPrintComposer
{
    Task<JobOrderPrintDocument?> ComposeAsync(Guid orderId, JobOrderPrintRequest request, CancellationToken cancellationToken = default);
}
