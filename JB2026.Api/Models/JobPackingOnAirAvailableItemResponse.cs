namespace JB2026.Api.Models;

public sealed class JobPackingOnAirAvailableItemResponse
{
    public Guid OrderId { get; set; }
    public int OrderType { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string OrderTitle { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
}