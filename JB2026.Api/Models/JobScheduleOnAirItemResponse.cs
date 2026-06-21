namespace JB2026.Api.Models;

public sealed class JobScheduleOnAirItemResponse
{
    public Guid ScheduleId { get; set; }
    public Guid OrderId { get; set; }
    public int OrderType { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string OrderTitle { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string MachineNumber { get; set; } = string.Empty;
    public int UrgencyLevel { get; set; }
    public int? Step1Status { get; set; }
    public int? Step2Status { get; set; }
    public string PrintQty { get; set; } = string.Empty;
    public string PrintColor { get; set; } = string.Empty;
    public string PrintSize { get; set; } = string.Empty;
    public string? SONumber { get; set; }
}
