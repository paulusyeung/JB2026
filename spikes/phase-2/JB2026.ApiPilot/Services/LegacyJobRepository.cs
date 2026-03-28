using JB2026.ApiPilot.Models;

namespace JB2026.ApiPilot.Services;

public sealed class LegacyJobRepository
{
    private readonly IReadOnlyList<SeedJob> _jobs = LegacyJobSeed.Create();

    public IReadOnlyList<JobListItem> GetRange(DateOnly startOn, int days)
    {
        var start = startOn.ToDateTime(TimeOnly.MinValue);
        var results = _jobs
            .Where(job => job.OrderedOn < start.AddDays(1) && job.OrderedOn > start.AddDays(-days))
            .Select(job => new JobListItem
            {
                OrderId = job.OrderId,
                OrderNumber = job.CompositeOrderNumber,
                CustomerName = job.CustomerName,
                CustomerRef = job.CustomerRef,
                OrderTitle = job.OrderTitle,
                OrderedBy = job.OrderedBy,
                OrderedOn = job.OrderedOn,
                RequiredOn = job.RequiredOn,
                Qty = job.Qty,
                Status = job.Status
            })
            .OrderByDescending(job => job.OrderNumber)
            .ToList();

        return results;
    }

    public JobDetail? GetById(Guid id)
    {
        var job = _jobs.SingleOrDefault(item => item.OrderId == id);
        return job is null
            ? null
            : new JobDetail
            {
                OrderId = job.OrderId,
                OrderNumber = job.CompositeOrderNumber,
                CustomerName = job.CustomerName,
                CustomerRef = job.CustomerRef,
                OrderTitle = job.OrderTitle,
                OrderedBy = job.OrderedBy,
                OrderedOn = job.OrderedOn,
                RequiredOn = job.RequiredOn,
                Qty = job.Qty,
                PaymentTerms = job.PaymentTerms,
                Remarks = job.Remarks,
                Status = job.Status,
                StyleTitles = job.StyleTitles,
                Attachments = job.Attachments
            };
    }
}