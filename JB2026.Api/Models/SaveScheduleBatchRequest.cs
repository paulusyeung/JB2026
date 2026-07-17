namespace JB2026.Api.Models;

public sealed class SaveScheduleBatchRequest
{
    public int OrderType { get; set; }
    public List<SaveScheduleBatchItem> ScheduledItems { get; set; } = [];
    public List<Guid> CancelledOrderIds { get; set; } = [];
    public List<Guid> CompletedOrderIds { get; set; } = [];
}

public sealed class SaveScheduleBatchItem
{
    public Guid OrderId { get; set; }
    public string MachineNumber { get; set; } = string.Empty;
    public int Step1Status { get; set; }
    public int Step2Status { get; set; }
    public int UrgencyLevel { get; set; }
}
