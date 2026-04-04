namespace JB2026.Api.Models;

public sealed class SavePackingOnAirBatchRequest
{
    public int OrderType { get; set; }
    public List<SavePackingOnAirBatchItem> SelectedItems { get; set; } = [];
    public List<Guid> CancelledOrderIds { get; set; } = [];
}

public sealed class SavePackingOnAirBatchItem
{
    public Guid OrderId { get; set; }
}

public sealed class CompletePackingOnAirRequest
{
    public List<Guid> OrderIds { get; set; } = [];
}