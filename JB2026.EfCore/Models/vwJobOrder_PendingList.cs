using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwJobOrder_PendingList
{
    public Guid OrderId { get; set; }

    public int OrderType { get; set; }

    public string? OrderNumber { get; set; }

    public int? JobNumber { get; set; }

    public string? JobOrderNumber { get; set; }

    public string? CustomerName { get; set; }

    public string? OrderTitle { get; set; }

    public DateTime? OrderedOn { get; set; }

    public DateTime? RequiredOn { get; set; }

    public int Status { get; set; }

    public int ScheduleCount { get; set; }
}
